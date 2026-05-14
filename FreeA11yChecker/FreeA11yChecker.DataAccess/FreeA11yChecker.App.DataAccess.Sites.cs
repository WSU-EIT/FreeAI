namespace FreeA11yChecker;

public partial interface IDataAccess
{
    Task<List<DataObjects.Site>> GetSites(List<Guid>? Ids, Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Site>> SaveSites(List<DataObjects.Site> Items, DataObjects.User? CurrentUser = null);
    Task<DataObjects.BooleanResponse> DeleteSites(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.SitePage>> GetSitePages(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.SitePage>> SaveSitePages(List<DataObjects.SitePage> Items, DataObjects.User? CurrentUser = null);
    Task<DataObjects.BooleanResponse> DeleteSitePages(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.SiteCredential>> GetSiteCredentials(List<Guid>? Ids, Guid SiteId);
    Task<List<DataObjects.SiteCredential>> SaveSiteCredentials(List<DataObjects.SiteCredential> Items, DataObjects.User? CurrentUser = null);
    Task<DataObjects.BooleanResponse> DeleteSiteCredentials(List<Guid>? Ids, DataObjects.User? CurrentUser = null);
}

public partial class DataAccess
{
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

                await SignalRUpdate(new DataObjects.SignalRUpdate {
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
}
