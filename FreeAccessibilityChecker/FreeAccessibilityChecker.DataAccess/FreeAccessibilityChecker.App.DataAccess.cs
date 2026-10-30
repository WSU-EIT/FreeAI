using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace FreeAccessibilityChecker;

// ---- FreeAccessibilityChecker.App.DataAccess.cs ----
// FreeAccessibilityChecker code for the DataAccess hooks, called from one line in each hook in DataAccess.App.cs.

public partial class DataAccess
{
    /// <summary>
    /// Removes FreeAccessibilityChecker records that belong to a record being deleted. Deleting a Site removes its scan runs,
    /// page scan results, violations and manual check results. Called as the first line inside the try block of
    /// DeleteRecordsApp; a false Result (with Messages) makes the hook return this response right away.
    /// </summary>
    private async Task<DataObjects.BooleanResponse> FreeAccessibilityCheckerDeleteRecordsApp(object Rec, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        try {
            // Cascade delete scans and related data when a site is deleted.
            if (Rec is EFModels.EFModels.Site) {
                EFModels.EFModels.Site? rec = Rec as EFModels.EFModels.Site;
                if (rec != null) {
                    // Get all scan runs for this site.
                    List<Guid> scanRunIds = await data.ScanRuns
                        .Where(x => x.SiteId == rec.SiteId)
                        .Select(x => x.ScanRunId)
                        .ToListAsync();

                    if (scanRunIds.Any()) {
                        // Delete violations via page scan results.
                        List<Guid> pageResultIds = await data.PageScanResults
                            .Where(x => scanRunIds.Contains(x.ScanRunId))
                            .Select(x => x.PageScanResultId)
                            .ToListAsync();

                        if (pageResultIds.Any()) {
                            data.A11yViolations.RemoveRange(
                                data.A11yViolations.Where(x => pageResultIds.Contains(x.PageScanResultId)));
                            await data.SaveChangesAsync();
                        }

                        // Delete page scan results.
                        data.PageScanResults.RemoveRange(
                            data.PageScanResults.Where(x => scanRunIds.Contains(x.ScanRunId)));
                        await data.SaveChangesAsync();

                        // Delete scan runs.
                        data.ScanRuns.RemoveRange(
                            data.ScanRuns.Where(x => x.SiteId == rec.SiteId));
                        await data.SaveChangesAsync();
                    }

                    // Delete manual check results.
                    data.ManualCheckResults.RemoveRange(
                        data.ManualCheckResults.Where(x => x.SiteId == rec.SiteId));
                    await data.SaveChangesAsync();
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("An Error Occurred in DeleteUserApp");
            output.Messages.AddRange(RecurseException(ex));
        }

        output.Result = output.Messages.Count == 0;

        return output;
    }

    /// <summary>
    /// Loads the site summaries for the current user's tenant into the BlazorDataModelLoader, so the dashboard can show
    /// them without another API call. Called from GetBlazorDataModelApp.
    /// </summary>
    private async Task<DataObjects.BlazorDataModelLoader> FreeAccessibilityCheckerGetBlazorDataModelApp(DataObjects.BlazorDataModelLoader blazorDataModelLoader, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BlazorDataModelLoader output = blazorDataModelLoader;

        try {
            if (CurrentUser != null && CurrentUser.TenantId != Guid.Empty) {
                // Load site list for the current tenant so the dashboard can show site summaries.
                List<DataObjects.Site> sites = await GetSites(null, CurrentUser.TenantId, CurrentUser);
                output.SiteList = sites;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetBlazorDataModelApp: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    /// <summary>
    /// Sends a SignalR update from FreeAccessibilityChecker code without letting a failed send fail the caller. SignalRUpdate
    /// posts to this app's own api/Data/SignalRUpdate endpoint, which is not listening yet while the startup seeding in
    /// FreeAccessibilityCheckerAppModifyEnd runs, so a failure is logged and ignored (stock SignalRUpdate throws).
    /// </summary>
    private async Task FreeAccessibilityCheckerSignalRUpdate(DataObjects.SignalRUpdate update)
    {
        try {
            await SignalRUpdate(update);
        } catch (Exception ex) {
            Console.WriteLine("SignalRUpdate notification failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Prunes old scan runs every 5 minutes (Iteration modulo 30, assuming 10-second intervals), keeping the 20 most
    /// recent runs per site. Called from ProcessBackgroundTasksApp; its response becomes the hook's response.
    /// </summary>
    private async Task<DataObjects.BooleanResponse> FreeAccessibilityCheckerProcessBackgroundTasksApp(Guid TenantId, long Iteration)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();
        output.Result = true;

        try {
            // Every 5 minutes (modulo 30 at 10-second intervals), prune old scans.
            if (Iteration % 30 == 0) {
                List<DataObjects.Site> sites = await GetSites(null, TenantId);
                foreach (DataObjects.Site site in sites) {
                    // Keep the 20 most recent scan runs per site.
                    int pruned = await PruneOldScans(site.SiteId, 20);
                    if (pruned > 0) {
                        Console.WriteLine("Pruned " + pruned + " old scan(s) for site " + site.Name);
                    }
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("Error in ProcessBackgroundTasksApp");
            output.Messages.AddRange(RecurseException(ex));
            output.Result = false;
        }

        return output;
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.Sites.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteSiteCredentials(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<DataObjects.BooleanResponse> DeleteSitePages(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<DataObjects.BooleanResponse> DeleteSites(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.SiteCredential>> GetSiteCredentials(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.SitePage>> GetSitePages(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.Site>> GetSites(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.SiteCredential>> SaveSiteCredentials(List<DataObjects.SiteCredential> Items, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.SitePage>> SaveSitePages(List<DataObjects.SitePage> Items, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Site>> SaveSites(List<DataObjects.Site> Items, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<DataObjects.BooleanResponse> DeleteSiteCredentials(List<Guid>? Ids, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();

        try {
            if (Ids != null && Ids.Any()) {
                List<EFModels.EFModels.SiteCredential> recs = await data.SiteCredentials.Where(x => Ids.Contains(x.SiteCredentialId)).ToListAsync();
                data.SiteCredentials.RemoveRange(recs);
                await data.SaveChangesAsync();
                output.Result = true;
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Site Credentials");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<DataObjects.BooleanResponse> DeleteSitePages(List<Guid>? Ids, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();

        try {
            if (Ids != null && Ids.Any()) {
                List<EFModels.EFModels.SitePage> recs = await data.SitePages.Where(x => Ids.Contains(x.SitePageId)).ToListAsync();
                data.SitePages.RemoveRange(recs);
                await data.SaveChangesAsync();
                output.Result = true;
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Site Pages");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<DataObjects.BooleanResponse> DeleteSites(List<Guid>? Ids, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();

        try {
            if (Ids != null && Ids.Any()) {
                DateTime now = DateTime.UtcNow;

                List<EFModels.EFModels.Site> recs = await data.Sites.Where(x => Ids.Contains(x.SiteId)).ToListAsync();

                foreach (EFModels.EFModels.Site rec in recs) {
                    rec.Deleted = true;
                    rec.DeletedAt = now;
                    rec.LastModified = now;
                    rec.LastModifiedBy = CurrentUserIdString(CurrentUser);
                }

                await data.SaveChangesAsync();
                output.Result = true;
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Deleting Sites");
            output.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    private string GetScanEncryptionKey()
    {
        string output = String.Empty;

        IConfigurationHelper? config = ConfigurationHelper;
        if (config != null) {
            string? key = config.ScanEncryptionKey;
            if (!String.IsNullOrWhiteSpace(key)) {
                output = key;
            }
        }

        return output;
    }

    public async Task<List<DataObjects.SiteCredential>> GetSiteCredentials(List<Guid>? Ids, Guid SiteId)
    {
        List<DataObjects.SiteCredential> output = new List<DataObjects.SiteCredential>();

        try {
            IQueryable<EFModels.EFModels.SiteCredential> query = data.SiteCredentials.Where(x => x.SiteId == SiteId);

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.SiteCredentialId));
            }

            List<EFModels.EFModels.SiteCredential> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.SiteCredential rec in recs) {
                    output.Add(new DataObjects.SiteCredential {
                        ActionResponse = GetNewActionResponse(true),
                        SiteCredentialId = rec.SiteCredentialId,
                        SiteId = rec.SiteId,
                        Label = rec.Label,
                        Username = rec.Username,
                        PasswordEncrypted = "********",
                        LoginUrl = rec.LoginUrl,
                        UsernameSelector = rec.UsernameSelector,
                        PasswordSelector = rec.PasswordSelector,
                        SubmitSelector = rec.SubmitSelector,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetSiteCredentials: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.SitePage>> GetSitePages(List<Guid>? Ids, Guid SiteId)
    {
        List<DataObjects.SitePage> output = new List<DataObjects.SitePage>();

        try {
            IQueryable<EFModels.EFModels.SitePage> query = data.SitePages.Where(x => x.SiteId == SiteId);

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.SitePageId));
            }

            query = query.OrderBy(x => x.SortOrder).ThenBy(x => x.Path);

            List<EFModels.EFModels.SitePage> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.SitePage rec in recs) {
                    output.Add(new DataObjects.SitePage {
                        ActionResponse = GetNewActionResponse(true),
                        SitePageId = rec.SitePageId,
                        SiteId = rec.SiteId,
                        Path = rec.Path,
                        Title = rec.Title,
                        RequiresAuth = rec.RequiresAuth,
                        Enabled = rec.Enabled,
                        SortOrder = rec.SortOrder,
                        IncludeInScan = rec.IncludeInScan,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetSitePages: " + RecurseExceptionAsString(ex));
        }

        return output;
    }
    public async Task<List<DataObjects.Site>> GetSites(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.Site> output = new List<DataObjects.Site>();

        try {
            IQueryable<EFModels.EFModels.Site> query = data.Sites.Where(x => x.TenantId == TenantId);

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.SiteId));
            }

            if (!AdminUser(CurrentUser)) {
                query = query.Where(x => !x.Deleted);
            }

            query = query.OrderBy(x => x.Name);

            List<EFModels.EFModels.Site> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.Site rec in recs) {
                    output.Add(new DataObjects.Site {
                        ActionResponse = GetNewActionResponse(true),
                        SiteId = rec.SiteId,
                        TenantId = rec.TenantId,
                        Name = rec.Name,
                        BaseUrl = rec.BaseUrl,
                        IsFreeCRMApp = rec.IsFreeCRMApp,
                        ScanScheduleCron = rec.ScanScheduleCron,
                        MaxConcurrency = rec.MaxConcurrency,
                        Enabled = rec.Enabled,
                        PublicVisible = rec.PublicVisible,
                        LastScanRunId = rec.LastScanRunId,
                        LastScanAt = rec.LastScanAt,
                        LastScanStatus = rec.LastScanStatus,
                        LastViolationCount = rec.LastViolationCount,
                        LastCriticalCount = rec.LastCriticalCount,
                        Added = rec.Added,
                        AddedBy = LastModifiedDisplayName(rec.AddedBy),
                        LastModified = rec.LastModified,
                        LastModifiedBy = LastModifiedDisplayName(rec.LastModifiedBy),
                        Deleted = rec.Deleted,
                        DeletedAt = rec.DeletedAt,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetSites: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.SiteCredential>> SaveSiteCredentials(List<DataObjects.SiteCredential> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.SiteCredential> output = new List<DataObjects.SiteCredential>();

        List<Guid> savedIds = new List<Guid>();
        Guid siteId = Guid.Empty;

        try {
            foreach (DataObjects.SiteCredential item in Items) {
                bool newRecord = false;
                EFModels.EFModels.SiteCredential? rec = null;

                if (item.SiteCredentialId == Guid.Empty) {
                    newRecord = true;
                    item.SiteCredentialId = Guid.NewGuid();

                    rec = new EFModels.EFModels.SiteCredential {
                        SiteCredentialId = item.SiteCredentialId,
                        SiteId = item.SiteId,
                    };
                } else {
                    rec = await data.SiteCredentials.FirstOrDefaultAsync(x => x.SiteCredentialId == item.SiteCredentialId);

                    if (rec == null) {
                        continue;
                    }
                }

                siteId = item.SiteId;

                rec.Label = item.Label;
                rec.Username = item.Username;
                // AuthType and TenantCode were missing from this mapping — without them
                // the EditSite UI rendered every credential as "Standard" auth and the
                // scanner couldn't differentiate tenant-aware logins from generic.
                rec.AuthType = item.AuthType;
                rec.TenantCode = item.TenantCode;
                rec.LoginUrl = item.LoginUrl;
                rec.UsernameSelector = item.UsernameSelector;
                rec.PasswordSelector = item.PasswordSelector;
                rec.SubmitSelector = item.SubmitSelector;

                // Handle password: if masked, keep existing; if new value, encrypt
                if (item.PasswordEncrypted != "********" && !String.IsNullOrEmpty(item.PasswordEncrypted)) {
                    string encryptionKey = GetScanEncryptionKey();
                    rec.PasswordEncrypted = EncryptString(item.PasswordEncrypted, encryptionKey);
                }
                // If "********" is sent, do not update the password field — keep existing value

                if (newRecord) {
                    await data.SiteCredentials.AddAsync(rec);
                }

                await data.SaveChangesAsync();
                savedIds.Add(item.SiteCredentialId);
            }

            if (savedIds.Any()) {
                output = await GetSiteCredentials(savedIds, siteId);
            }
        } catch (Exception ex) {
            DataObjects.SiteCredential errorItem = new DataObjects.SiteCredential();
            errorItem.ActionResponse.Messages.Add("Error Saving Site Credentials");
            errorItem.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorItem);
        }

        return output;
    }

    public async Task<List<DataObjects.SitePage>> SaveSitePages(List<DataObjects.SitePage> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.SitePage> output = new List<DataObjects.SitePage>();

        List<Guid> savedIds = new List<Guid>();
        Guid siteId = Guid.Empty;

        try {
            foreach (DataObjects.SitePage item in Items) {
                bool newRecord = false;
                EFModels.EFModels.SitePage? rec = null;

                // Validate path
                if (String.IsNullOrWhiteSpace(item.Path)) {
                    continue;
                }
                if (!item.Path.StartsWith("/")) {
                    item.Path = "/" + item.Path;
                }

                if (item.SitePageId == Guid.Empty) {
                    newRecord = true;
                    item.SitePageId = Guid.NewGuid();

                    rec = new EFModels.EFModels.SitePage {
                        SitePageId = item.SitePageId,
                        SiteId = item.SiteId,
                    };
                } else {
                    rec = await data.SitePages.FirstOrDefaultAsync(x => x.SitePageId == item.SitePageId);

                    if (rec == null) {
                        continue;
                    }
                }

                siteId = item.SiteId;

                rec.Path = item.Path;
                rec.Title = item.Title;
                rec.RequiresAuth = item.RequiresAuth;
                rec.Enabled = item.Enabled;
                rec.SortOrder = item.SortOrder;
                rec.IncludeInScan = item.IncludeInScan;

                if (newRecord) {
                    await data.SitePages.AddAsync(rec);
                }

                await data.SaveChangesAsync();
                savedIds.Add(item.SitePageId);
            }

            if (savedIds.Any()) {
                output = await GetSitePages(savedIds, siteId);
            }
        } catch (Exception ex) {
            DataObjects.SitePage errorItem = new DataObjects.SitePage();
            errorItem.ActionResponse.Messages.Add("Error Saving Site Pages");
            errorItem.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorItem);
        }

        return output;
    }

    public async Task<List<DataObjects.Site>> SaveSites(List<DataObjects.Site> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.Site> output = new List<DataObjects.Site>();

        List<Guid> savedIds = new List<Guid>();
        Guid tenantId = Guid.Empty;

        try {
            DateTime now = DateTime.UtcNow;

            foreach (DataObjects.Site item in Items) {
                bool newRecord = false;
                EFModels.EFModels.Site? rec = null;

                if (item.SiteId == Guid.Empty) {
                    newRecord = true;
                    item.SiteId = Guid.NewGuid();

                    rec = new EFModels.EFModels.Site {
                        SiteId = item.SiteId,
                        TenantId = item.TenantId,
                        Deleted = false,
                        Added = now,
                        AddedBy = CurrentUserIdString(CurrentUser),
                    };
                } else {
                    rec = await data.Sites.FirstOrDefaultAsync(x => x.SiteId == item.SiteId);

                    if (rec == null) {
                        continue;
                    }

                    if (rec.TenantId != item.TenantId) {
                        continue;
                    }
                }

                tenantId = item.TenantId;

                rec.Name = MaxStringLength(item.Name, 200);
                rec.BaseUrl = item.BaseUrl;
                rec.IsFreeCRMApp = item.IsFreeCRMApp;
                rec.ScanScheduleCron = item.ScanScheduleCron;
                rec.MaxConcurrency = item.MaxConcurrency;
                rec.Enabled = item.Enabled;
                rec.PublicVisible = item.PublicVisible;
                rec.LastModified = now;
                rec.LastModifiedBy = CurrentUserIdString(CurrentUser);

                if (newRecord) {
                    await data.Sites.AddAsync(rec);
                }

                await data.SaveChangesAsync();
                savedIds.Add(item.SiteId);

                // Auto-create root page "/" for new sites so the base URL is always scanned.
                if (newRecord) {
                    bool rootPageExists = await data.SitePages.AnyAsync(x => x.SiteId == item.SiteId && x.Path == "/");
                    if (!rootPageExists) {
                        var rootPage = new EFModels.EFModels.SitePage {
                            SitePageId = Guid.NewGuid(),
                            SiteId = item.SiteId,
                            Path = "/",
                            Title = "Home",
                            IncludeInScan = true,
                            Enabled = true,
                            SortOrder = 0,
                        };
                        await data.SitePages.AddAsync(rootPage);
                        await data.SaveChangesAsync();
                    }
                }
            }

            if (savedIds.Any()) {
                output = await GetSites(savedIds, tenantId, CurrentUser);

                foreach (DataObjects.Site site in output) {
                    site.ActionResponse = GetNewActionResponse(true);
                }

                await FreeAccessibilityCheckerSignalRUpdate(new DataObjects.SignalRUpdate {
                    TenantId = tenantId,
                    UpdateType = DataObjects.SignalRUpdateType.Setting,
                    Message = "SitesSaved",
                    UserId = CurrentUserId(CurrentUser),
                });
            }
        } catch (Exception ex) {
            DataObjects.Site errorItem = new DataObjects.Site();
            errorItem.ActionResponse.Messages.Add("Error Saving Sites");
            errorItem.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorItem);
        }

        return output;
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.Suppressions.cs ----
public partial interface IDataAccess
{
    Task<bool> DeleteViolationSuppression(Guid ViolationSuppressionId, Guid TenantId);
    Task<List<DataObjects.ViolationSuppression>> GetViolationSuppressions(Guid TenantId);
    Task<DataObjects.ViolationSuppression> SaveViolationSuppression(DataObjects.ViolationSuppression Item, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    /// <summary>
    /// Mutates each violation in-place: if any active suppression matches, sets IsSuppressed=true,
    /// stores the original severity, downgrades the displayed severity to "minor", and copies the reason.
    /// </summary>
    private static void ApplySuppressionsToViolations(
        List<DataObjects.A11yViolation> violations,
        List<EFModels.EFModels.ViolationSuppression> suppressions,
        Guid? siteId = null){
        if (!suppressions.Any() || !violations.Any()) return;
        foreach (var v in violations) {
            var match = suppressions.FirstOrDefault(s => SuppressionMatches(s, v.RuleId, v.Selector, siteId));
            if (match == null && !String.IsNullOrWhiteSpace(v.CanonicalRuleId)) {
                match = suppressions.FirstOrDefault(s => SuppressionMatches(s, v.CanonicalRuleId!, v.Selector, siteId));
            }
            if (match != null) {
                v.IsSuppressed = true;
                v.SuppressionReason = match.Reason;
                v.OriginalSeverity = v.Severity;
                v.Severity = "minor";
            }
        }
    }

    public async Task<bool> DeleteViolationSuppression(Guid ViolationSuppressionId, Guid TenantId)
    {
        try {
            var rec = await data.ViolationSuppressions
                .FirstOrDefaultAsync(x => x.ViolationSuppressionId == ViolationSuppressionId && x.TenantId == TenantId);
            if (rec != null) {
                data.ViolationSuppressions.Remove(rec);
                await data.SaveChangesAsync();
                return true;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in DeleteViolationSuppression: " + RecurseExceptionAsString(ex));
        }
        return false;
    }

    public async Task<List<DataObjects.ViolationSuppression>> GetViolationSuppressions(Guid TenantId)
    {
        List<DataObjects.ViolationSuppression> output = new();
        try {
            var recs = await data.ViolationSuppressions
                .Where(x => x.TenantId == TenantId)
                .OrderByDescending(x => x.LastModified)
                .ToListAsync();
            foreach (var rec in recs) {
                output.Add(new DataObjects.ViolationSuppression {
                    ActionResponse = GetNewActionResponse(true),
                    ViolationSuppressionId = rec.ViolationSuppressionId,
                    TenantId = rec.TenantId,
                    RuleId = rec.RuleId,
                    SelectorPattern = rec.SelectorPattern,
                    SiteId = rec.SiteId,
                    Reason = rec.Reason,
                    Enabled = rec.Enabled,
                    Added = rec.Added,
                    AddedBy = rec.AddedBy,
                    LastModified = rec.LastModified,
                    LastModifiedBy = rec.LastModifiedBy,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetViolationSuppressions: " + RecurseExceptionAsString(ex));
        }
        return output;
    }

    private static string GlobToLike(string glob)
    {
        // Escape SQL LIKE special chars first, then map glob wildcards.
        string escaped = glob.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return escaped.Replace("**", "%").Replace("*", "%").Replace("?", "_");
    }

    /// <summary>
    /// Loads all enabled suppressions for the tenant. Lightweight enough to call on every
    /// violation read — typical tenant has fewer than 100 suppression rules.
    /// </summary>
    private async Task<List<EFModels.EFModels.ViolationSuppression>> LoadActiveSuppressions(Guid TenantId)
    {
        try {
            return await data.ViolationSuppressions
                .Where(x => x.TenantId == TenantId && x.Enabled)
                .ToListAsync();
        } catch {
            return new List<EFModels.EFModels.ViolationSuppression>();
        }
    }

    public async Task<DataObjects.ViolationSuppression> SaveViolationSuppression(DataObjects.ViolationSuppression Item, DataObjects.User? CurrentUser = null)
    {
        DataObjects.ViolationSuppression output = Item;
        output.ActionResponse = GetNewActionResponse();

        try {
            DateTime now = DateTime.UtcNow;
            string? userName = CurrentUser?.DisplayName;
            EFModels.EFModels.ViolationSuppression? rec = null;
            bool newRecord = Item.ViolationSuppressionId == Guid.Empty;

            if (!newRecord) {
                rec = await data.ViolationSuppressions
                    .FirstOrDefaultAsync(x => x.ViolationSuppressionId == Item.ViolationSuppressionId
                        && x.TenantId == Item.TenantId);
            }

            if (rec == null) {
                rec = new EFModels.EFModels.ViolationSuppression {
                    ViolationSuppressionId = Guid.NewGuid(),
                    TenantId = Item.TenantId,
                    Added = now,
                    AddedBy = userName,
                };
                await data.ViolationSuppressions.AddAsync(rec);
                newRecord = true;
            }

            rec.RuleId = Item.RuleId;
            rec.SelectorPattern = Item.SelectorPattern;
            rec.SiteId = Item.SiteId;
            rec.Reason = Item.Reason;
            rec.Enabled = Item.Enabled;
            rec.LastModified = now;
            rec.LastModifiedBy = userName;

            await data.SaveChangesAsync();

            output.ViolationSuppressionId = rec.ViolationSuppressionId;
            output.Added = rec.Added;
            output.AddedBy = rec.AddedBy;
            output.LastModified = rec.LastModified;
            output.LastModifiedBy = rec.LastModifiedBy;
            output.ActionResponse.Result = true;
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error saving violation suppression");
            Console.WriteLine("Error in SaveViolationSuppression: " + RecurseExceptionAsString(ex));
        }
        return output;
    }

    /// <summary>
    /// Returns true if the suppression rule matches a given (RuleId, Selector, SiteId) tuple.
    /// SelectorPattern uses simple glob matching: '*' matches any chars, exact-match if no '*'.
    /// SiteId on the suppression: null = applies to all sites in tenant; otherwise must match.
    /// </summary>
    private static bool SuppressionMatches(EFModels.EFModels.ViolationSuppression s,
        string ruleId, string? selector, Guid? siteId){
        if (!s.Enabled) return false;
        if (!String.Equals(s.RuleId, ruleId, StringComparison.OrdinalIgnoreCase)) return false;
        if (s.SiteId.HasValue && siteId.HasValue && s.SiteId.Value != siteId.Value) return false;

        if (String.IsNullOrWhiteSpace(s.SelectorPattern)) return true; // any selector
        if (String.IsNullOrWhiteSpace(selector)) return false; // suppression has pattern but violation has no selector

        string pattern = s.SelectorPattern.Trim();
        if (!pattern.Contains('*')) {
            return String.Equals(pattern, selector, StringComparison.OrdinalIgnoreCase);
        }
        // Convert glob to regex.
        string regexStr = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(selector, regexStr,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.ManualChecks.cs ----
public partial interface IDataAccess
{
    Task<List<DataObjects.ManualCheckResult>> GetManualChecks(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.ManualCheckResult>> SaveManualChecks(List<DataObjects.ManualCheckResult> Items, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
    public async Task<List<DataObjects.ManualCheckResult>> GetManualChecks(List<Guid>? Ids, Guid SiteId)
    {
        List<DataObjects.ManualCheckResult> output = new List<DataObjects.ManualCheckResult>();

        try {
            IQueryable<EFModels.EFModels.ManualCheckResult> query = data.ManualCheckResults.Where(x => x.SiteId == SiteId);

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.ManualCheckResultId));
            }

            query = query.OrderBy(x => x.WcagCriterion);

            List<EFModels.EFModels.ManualCheckResult> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.ManualCheckResult rec in recs) {
                    output.Add(new DataObjects.ManualCheckResult {
                        ActionResponse = GetNewActionResponse(true),
                        ManualCheckResultId = rec.ManualCheckResultId,
                        SiteId = rec.SiteId,
                        WcagCriterion = rec.WcagCriterion,
                        Status = rec.Status,
                        Notes = rec.Notes,
                        TestedBy = rec.TestedBy,
                        TestedAt = rec.TestedAt,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetManualChecks: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.ManualCheckResult>> SaveManualChecks(List<DataObjects.ManualCheckResult> Items, DataObjects.User? CurrentUser = null)
    {
        List<DataObjects.ManualCheckResult> output = new List<DataObjects.ManualCheckResult>();

        List<Guid> savedIds = new List<Guid>();
        Guid siteId = Guid.Empty;

        try {
            DateTime now = DateTime.UtcNow;

            foreach (DataObjects.ManualCheckResult item in Items) {
                bool newRecord = false;
                EFModels.EFModels.ManualCheckResult? rec = null;

                if (item.ManualCheckResultId == Guid.Empty) {
                    newRecord = true;
                    item.ManualCheckResultId = Guid.NewGuid();

                    rec = new EFModels.EFModels.ManualCheckResult {
                        ManualCheckResultId = item.ManualCheckResultId,
                        SiteId = item.SiteId,
                    };
                } else {
                    rec = await data.ManualCheckResults.FirstOrDefaultAsync(x => x.ManualCheckResultId == item.ManualCheckResultId);

                    if (rec == null) {
                        continue;
                    }
                }

                siteId = item.SiteId;

                rec.WcagCriterion = item.WcagCriterion;
                rec.Status = item.Status;
                rec.Notes = item.Notes;

                // Auto-set TestedBy if empty and CurrentUser is available
                if (String.IsNullOrWhiteSpace(item.TestedBy) && CurrentUser != null && !String.IsNullOrWhiteSpace(CurrentUser.DisplayName)) {
                    rec.TestedBy = CurrentUser.DisplayName;
                } else {
                    rec.TestedBy = item.TestedBy;
                }

                // Auto-set TestedAt if status is not "Not Tested" and TestedAt is null
                if (item.TestedAt == null && !String.IsNullOrWhiteSpace(item.Status) && item.Status != "Not Tested") {
                    rec.TestedAt = now;
                } else {
                    rec.TestedAt = item.TestedAt;
                }

                if (newRecord) {
                    await data.ManualCheckResults.AddAsync(rec);
                }

                await data.SaveChangesAsync();
                savedIds.Add(item.ManualCheckResultId);
            }

            if (savedIds.Any()) {
                output = await GetManualChecks(savedIds, siteId);
            }
        } catch (Exception ex) {
            DataObjects.ManualCheckResult errorItem = new DataObjects.ManualCheckResult();
            errorItem.ActionResponse.Messages.Add("Error Saving Manual Checks");
            errorItem.ActionResponse.Messages.AddRange(RecurseException(ex));
            output.Add(errorItem);
        }

        return output;
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.Encryption.cs ----
public partial interface IDataAccess
{
    string DecryptString(string cipherText, string key);
    string EncryptString(string plainText, string key);
}

public partial class DataAccess
{
    // Static salt for PBKDF2 key derivation (not secret, prevents rainbow tables)
    private static readonly byte[] _scanEncryptionSalt = new byte[] {
        0xA4, 0x3B, 0x7C, 0x1E, 0x9D, 0x5F, 0x2A, 0x8B,
        0xC6, 0xE1, 0x4D, 0x73, 0x0F, 0x96, 0xB8, 0x52
    };

    private const int _scanEncryptionIterations = 100000;
    private const int _scanEncryptionNonceSize = 12;  // AES-GCM standard nonce size
    private const int _scanEncryptionTagSize = 16;     // 128-bit authentication tag

    public string EncryptString(string plainText, string key)
    {
        if (String.IsNullOrEmpty(plainText)) {
            return String.Empty;
        }

        try {
            string resolvedKey = ResolveEncryptionKey(key);
            byte[] keyBytes = DeriveKeyBytes(resolvedKey);

            byte[] nonce = new byte[_scanEncryptionNonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[_scanEncryptionTagSize];

            using (AesGcm aesGcm = new AesGcm(keyBytes, _scanEncryptionTagSize)) {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // Concatenate: nonce (12) + tag (16) + ciphertext
            byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        } catch (Exception ex) {
            Console.WriteLine("Error in EncryptString: " + RecurseExceptionAsString(ex));
            return String.Empty;
        }
    }

    public string DecryptString(string cipherText, string key)
    {
        if (String.IsNullOrEmpty(cipherText)) {
            return String.Empty;
        }

        try {
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            if (fullCipher.Length < _scanEncryptionNonceSize + _scanEncryptionTagSize) {
                return String.Empty;
            }

            // Extract nonce (first 12 bytes)
            byte[] nonce = new byte[_scanEncryptionNonceSize];
            Buffer.BlockCopy(fullCipher, 0, nonce, 0, _scanEncryptionNonceSize);

            // Extract tag (next 16 bytes)
            byte[] tag = new byte[_scanEncryptionTagSize];
            Buffer.BlockCopy(fullCipher, _scanEncryptionNonceSize, tag, 0, _scanEncryptionTagSize);

            // Extract ciphertext (remaining bytes)
            int cipherLength = fullCipher.Length - _scanEncryptionNonceSize - _scanEncryptionTagSize;
            byte[] cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(fullCipher, _scanEncryptionNonceSize + _scanEncryptionTagSize, cipherBytes, 0, cipherLength);

            string resolvedKey = ResolveEncryptionKey(key);
            byte[] keyBytes = DeriveKeyBytes(resolvedKey);

            byte[] plainBytes = new byte[cipherLength];

            using (AesGcm aesGcm = new AesGcm(keyBytes, _scanEncryptionTagSize)) {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return System.Text.Encoding.UTF8.GetString(plainBytes);
        } catch (CryptographicException) {
            // Tag verification failed — data may be tampered
            throw;
        } catch (Exception ex) {
            Console.WriteLine("Error in DecryptString: " + RecurseExceptionAsString(ex));
            return String.Empty;
        }
    }

    private string ResolveEncryptionKey(string key)
    {
        if (!String.IsNullOrWhiteSpace(key)) {
            return key;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            // DPAPI fallback for Windows development environments
            byte[] entropy = System.Text.Encoding.UTF8.GetBytes("FreeAccessibilityChecker_ScanCredentials");
            byte[] dpapiData = System.Text.Encoding.UTF8.GetBytes("FreeAccessibilityChecker_DefaultKey");
            byte[] protectedData = System.Security.Cryptography.ProtectedData.Protect(
                dpapiData, entropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(protectedData);
        }

        throw new InvalidOperationException(
            "ScanEncryptionKey must be configured in appsettings.json on non-Windows platforms.");
    }

    private byte[] DeriveKeyBytes(string resolvedKey)
    {
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(
            resolvedKey, _scanEncryptionSalt, _scanEncryptionIterations, HashAlgorithmName.SHA256)) {
            return kdf.GetBytes(32); // 256 bits
        }
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.PublicData.cs ----
public partial interface IDataAccess
{
    Task<DataObjects.PageScanResult?> GetPublicPageDetail(Guid PageScanResultId);
    Task<DataObjects.ScanRun?> GetPublicScanDetail(Guid ScanRunId);
    Task<List<DataObjects.ScanRun>> GetPublicScanHistory(Guid SiteId, int Count = 10);
    Task<List<DataObjects.Site>> GetPublicSites(Guid TenantId);
}

public partial class DataAccess
{
    public async Task<DataObjects.PageScanResult?> GetPublicPageDetail(Guid PageScanResultId)
    {
        DataObjects.PageScanResult? output = null;

        try {
            EFModels.EFModels.PageScanResult? rec = await data.PageScanResults.FirstOrDefaultAsync(x => x.PageScanResultId == PageScanResultId);

            if (rec == null) {
                return null;
            }

            // Walk up to ScanRun -> Site to verify PublicVisible
            EFModels.EFModels.ScanRun? scanRun = await data.ScanRuns.FirstOrDefaultAsync(x => x.ScanRunId == rec.ScanRunId);

            if (scanRun == null) {
                return null;
            }

            bool isPublic = await data.Sites.AnyAsync(x => x.SiteId == scanRun.SiteId && x.PublicVisible == true && x.Enabled == true && !x.Deleted);

            if (!isPublic) {
                return null;
            }

            output = new DataObjects.PageScanResult {
                ActionResponse = GetNewActionResponse(true),
                PageScanResultId = rec.PageScanResultId,
                ScanRunId = rec.ScanRunId,
                SitePageId = rec.SitePageId,
                Url = rec.Url,
                StatusCode = rec.StatusCode,
                PageTitle = rec.PageTitle,
                HtmlSizeBytes = rec.HtmlSizeBytes,
                ScreenshotPath = rec.ScreenshotPath,
                AxeViolationCount = rec.AxeViolationCount,
                HtmlCheckViolationCount = rec.HtmlCheckViolationCount,
                OverlayViolationCount = rec.OverlayViolationCount,
                TotalViolations = rec.TotalViolations,
                CriticalCount = rec.CriticalCount,
                SeriousCount = rec.SeriousCount,
                ModerateCount = rec.ModerateCount,
                MinorCount = rec.MinorCount,
                ScanDurationMs = rec.ScanDurationMs,
            };

            // Load associated violations
            List<EFModels.EFModels.A11yViolation> violations = await data.A11yViolations
                .Where(x => x.PageScanResultId == PageScanResultId)
                .OrderBy(x => x.Severity)
                .ThenBy(x => x.RuleId)
                .ToListAsync();

            if (violations != null && violations.Any()) {
                output.Violations = new List<DataObjects.A11yViolation>();

                foreach (EFModels.EFModels.A11yViolation vRec in violations) {
                    output.Violations.Add(new DataObjects.A11yViolation {
                        ActionResponse = GetNewActionResponse(true),
                        A11yViolationId = vRec.A11yViolationId,
                        PageScanResultId = vRec.PageScanResultId,
                        Tool = vRec.Tool,
                        RuleId = vRec.RuleId,
                        CanonicalRuleId = vRec.CanonicalRuleId,
                        Severity = vRec.Severity,
                        Message = vRec.Message,
                        HtmlSnippet = vRec.HtmlSnippet,
                        Selector = vRec.Selector,
                        HelpUrl = vRec.HelpUrl,
                        WcagCriteria = vRec.WcagCriteria,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPublicPageDetail: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<DataObjects.ScanRun?> GetPublicScanDetail(Guid ScanRunId)
    {
        DataObjects.ScanRun? output = null;

        try {
            EFModels.EFModels.ScanRun? rec = await data.ScanRuns.FirstOrDefaultAsync(x => x.ScanRunId == ScanRunId);

            if (rec == null) {
                return null;
            }

            // Verify parent site is publicly visible
            bool isPublic = await data.Sites.AnyAsync(x => x.SiteId == rec.SiteId && x.PublicVisible == true && x.Enabled == true && !x.Deleted);

            if (!isPublic) {
                return null;
            }

            output = new DataObjects.ScanRun {
                ActionResponse = GetNewActionResponse(true),
                ScanRunId = rec.ScanRunId,
                SiteId = rec.SiteId,
                Status = rec.Status,
                StartedAt = rec.StartedAt,
                CompletedAt = rec.CompletedAt,
                TotalViolations = rec.TotalViolations,
                CriticalCount = rec.CriticalCount,
                SeriousCount = rec.SeriousCount,
                ModerateCount = rec.ModerateCount,
                MinorCount = rec.MinorCount,
                PagesScanned = rec.PagesScanned,
            };

            // Load associated page results
            List<EFModels.EFModels.PageScanResult> pageResults = await data.PageScanResults
                .Where(x => x.ScanRunId == ScanRunId)
                .OrderBy(x => x.Url)
                .ToListAsync();

            if (pageResults != null && pageResults.Any()) {
                output.PageResults = new List<DataObjects.PageScanResult>();

                foreach (EFModels.EFModels.PageScanResult pageRec in pageResults) {
                    output.PageResults.Add(new DataObjects.PageScanResult {
                        ActionResponse = GetNewActionResponse(true),
                        PageScanResultId = pageRec.PageScanResultId,
                        ScanRunId = pageRec.ScanRunId,
                        Url = pageRec.Url,
                        PageTitle = pageRec.PageTitle,
                        TotalViolations = pageRec.TotalViolations,
                        CriticalCount = pageRec.CriticalCount,
                        SeriousCount = pageRec.SeriousCount,
                        ModerateCount = pageRec.ModerateCount,
                        MinorCount = pageRec.MinorCount,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPublicScanDetail: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.ScanRun>> GetPublicScanHistory(Guid SiteId, int Count = 10)
    {
        List<DataObjects.ScanRun> output = new List<DataObjects.ScanRun>();

        try {
            // Verify site exists and is publicly visible
            bool isPublic = await data.Sites.AnyAsync(x => x.SiteId == SiteId && x.PublicVisible == true && x.Enabled == true && !x.Deleted);

            if (!isPublic) {
                return output;
            }

            List<EFModels.EFModels.ScanRun> recs = await data.ScanRuns
                .Where(x => x.SiteId == SiteId && x.Status == "Complete")
                .OrderByDescending(x => x.StartedAt)
                .Take(Count)
                .ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.ScanRun rec in recs) {
                    output.Add(new DataObjects.ScanRun {
                        ActionResponse = GetNewActionResponse(true),
                        ScanRunId = rec.ScanRunId,
                        SiteId = rec.SiteId,
                        StartedAt = rec.StartedAt,
                        CompletedAt = rec.CompletedAt,
                        TotalViolations = rec.TotalViolations,
                        CriticalCount = rec.CriticalCount,
                        SeriousCount = rec.SeriousCount,
                        ModerateCount = rec.ModerateCount,
                        MinorCount = rec.MinorCount,
                        PagesScanned = rec.PagesScanned,
                        // Explicitly exclude: TriggeredBy, TenantId
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPublicScanHistory: " + RecurseExceptionAsString(ex));
        }

        return output;
    }
    public async Task<List<DataObjects.Site>> GetPublicSites(Guid TenantId)
    {
        List<DataObjects.Site> output = new List<DataObjects.Site>();

        try {
            List<EFModels.EFModels.Site> recs = await data.Sites
                .Where(x => x.TenantId == TenantId && x.PublicVisible == true && x.Enabled == true && !x.Deleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.Site rec in recs) {
                    output.Add(new DataObjects.Site {
                        ActionResponse = GetNewActionResponse(true),
                        SiteId = rec.SiteId,
                        Name = rec.Name,
                        BaseUrl = rec.BaseUrl,
                        LastScanAt = rec.LastScanAt,
                        LastScanStatus = rec.LastScanStatus,
                        LastViolationCount = rec.LastViolationCount,
                        LastCriticalCount = rec.LastCriticalCount,
                        // Explicitly exclude: ScanScheduleCron, MaxConcurrency, IsFreeCRMApp, credentials
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPublicSites: " + RecurseExceptionAsString(ex));
        }

        return output;
    }
}

// ---- FreeAccessibilityChecker.App.DataAccess.AuditExport.cs ----
public partial interface IDataAccess
{
    Task<string> ExportAuditCsv(Guid SiteId);
    Task<DataObjects.AuditReport> GenerateAuditReport(Guid SiteId, DataObjects.User? CurrentUser = null);
    List<DataObjects.WcagCriterionDefinition> GetWcagCriteria();
}

public partial class DataAccess
{
    public async Task<string> ExportAuditCsv(Guid SiteId)
    {
        string output = String.Empty;

        try {
            DataObjects.AuditReport report = await GenerateAuditReport(SiteId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Criterion,Name,Principle,Level,Conformance Status,Source,Remarks");

            foreach (var item in report.Criteria) {
                sb.AppendLine(
                    "\"" + (item.Criterion ?? "") + "\"," +
                    "\"" + (item.Name ?? "").Replace("\"", "\"\"") + "\"," +
                    "\"" + (item.Principle ?? "") + "\"," +
                    "\"" + (item.Level ?? "") + "\"," +
                    "\"" + (item.ConformanceLevel ?? "") + "\"," +
                    "\"" + (item.Source ?? "") + "\"," +
                    "\"" + (item.Remarks ?? "").Replace("\"", "\"\"") + "\""
                );
            }

            output = sb.ToString();
        } catch (Exception ex) {
            Console.WriteLine("Error in ExportAuditCsv: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<DataObjects.AuditReport> GenerateAuditReport(Guid SiteId, DataObjects.User? CurrentUser = null)
    {
        DataObjects.AuditReport output = new DataObjects.AuditReport();

        try {
            List<DataObjects.WcagCriterionDefinition> definitions = GetWcagCriteria();
            output.SiteId = SiteId;
            output.GeneratedAt = DateTime.UtcNow;
            output.TotalCriteria = definitions.Count;

            // Load site info
            List<DataObjects.Site> sites = await GetSites(new List<Guid> { SiteId }, Guid.Empty, CurrentUser);
            DataObjects.Site? site = sites?.FirstOrDefault();
            if (site != null) {
                output.SiteName = site.Name;
                output.SiteUrl = site.BaseUrl;
                output.LastScanAt = site.LastScanAt;
            }

            // Load manual checks
            List<DataObjects.ManualCheckResult> manualChecks = await GetManualChecks(null, SiteId);

            // Load automated violations from the latest scan
            List<DataObjects.A11yViolation> violations = new List<DataObjects.A11yViolation>();
            if (site != null && site.LastScanRunId.HasValue) {
                violations = await GetViolationsByRule(site.LastScanRunId.Value, null);
            }

            // Build per-criterion conformance using WcagCriterionStatus
            Dictionary<string, DataObjects.WcagCriterionStatus> statusMap = new();

            // Initialize all criteria as Not Evaluated
            foreach (var def in definitions) {
                statusMap[def.Criterion] = new DataObjects.WcagCriterionStatus {
                    Criterion = def.Criterion,
                    Name = def.Name,
                    Principle = def.Principle,
                    Level = def.Level,
                    CanBeAutomated = def.CanBeAutomated,
                    ConformanceLevel = "Not Evaluated",
                    Source = "None",
                };
            }

            // Map automated violations to criteria
            foreach (var violation in violations) {
                if (!String.IsNullOrWhiteSpace(violation.WcagCriteria)) {
                    var criteriaList = violation.WcagCriteria.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var criterion in criteriaList) {
                        string normalized = NormalizeCriterionNumber(criterion);
                        if (statusMap.ContainsKey(normalized)) {
                            statusMap[normalized].ConformanceLevel = "Does Not Support";
                            statusMap[normalized].Source = "Automated";
                            statusMap[normalized].AutomatedViolationCount++;
                        }
                    }
                }
            }

            // Layer manual checks on top (manual overrides automated)
            foreach (var check in manualChecks) {
                if (!String.IsNullOrWhiteSpace(check.Status) && check.Status != "Not Tested") {
                    string normalized = check.WcagCriterion ?? "";
                    if (statusMap.ContainsKey(normalized)) {
                        if (check.Status == "Pass") {
                            statusMap[normalized].ConformanceLevel = "Supports";
                        } else if (check.Status == "Fail") {
                            statusMap[normalized].ConformanceLevel = "Does Not Support";
                        } else if (check.Status == "N/A") {
                            statusMap[normalized].ConformanceLevel = "Not Applicable";
                        }
                        statusMap[normalized].Source = "Manual";
                        statusMap[normalized].ManualStatus = check.Status;
                        statusMap[normalized].Remarks = check.Notes;
                    }
                }
            }

            // For automatable criteria with no violations and a scan has run, mark as Supports
            if (site != null && site.LastScanAt.HasValue) {
                foreach (var def in definitions.Where(x => x.CanBeAutomated)) {
                    if (statusMap.ContainsKey(def.Criterion) && statusMap[def.Criterion].ConformanceLevel == "Not Evaluated") {
                        statusMap[def.Criterion].ConformanceLevel = "Supports";
                        statusMap[def.Criterion].Source = "Automated";
                    }
                }
            }

            output.Criteria = statusMap.Values.OrderBy(x => x.Criterion).ToList();
            output.EvaluatedCount = output.Criteria.Count(x => x.ConformanceLevel != "Not Evaluated");
            output.SupportsCount = output.Criteria.Count(x => x.ConformanceLevel == "Supports");
            output.DoesNotSupportCount = output.Criteria.Count(x => x.ConformanceLevel == "Does Not Support");
            output.PartialCount = output.Criteria.Count(x => x.ConformanceLevel == "Partially Supports");
            output.NotEvaluatedCount = output.Criteria.Count(x => x.ConformanceLevel == "Not Evaluated");
        } catch (Exception ex) {
            Console.WriteLine("Error in GenerateAuditReport: " + RecurseExceptionAsString(ex));
        }

        return output;
    }
    public List<DataObjects.WcagCriterionDefinition> GetWcagCriteria()
    {
        return DataObjects.WcagCriteria.All.ToList();
    }

    private static string NormalizeCriterionNumber(string raw)
    {
        string cleaned = raw.Trim().ToLower()
            .Replace("wcag", "").Replace("sc", "").Replace("-", ".").Replace("_", ".").Trim('.');

        var match = Regex.Match(cleaned, @"(\d+)\.?(\d+)\.?(\d+)");
        if (match.Success) {
            return match.Groups[1].Value + "." + match.Groups[2].Value + "." + match.Groups[3].Value;
        }
        return raw.Trim();
    }
}
