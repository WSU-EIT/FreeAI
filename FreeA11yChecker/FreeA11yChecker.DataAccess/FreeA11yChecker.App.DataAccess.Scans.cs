namespace FreeA11yChecker;

public partial interface IDataAccess
{
    Task<List<DataObjects.ScanRun>> GetScanRuns(List<Guid>? Ids, Guid? SiteId, Guid TenantId);
    Task<List<DataObjects.ScanRun>> GetPendingScanRuns();
    Task<int> CancelOrphanedScanRuns();
    Task<DataObjects.ScanRun> SaveScanRun(DataObjects.ScanRun Item, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.PageScanResult>> GetPageScanResults(List<Guid>? Ids, Guid? ScanRunId);
    Task<DataObjects.PageScanResult> SavePageScanResult(DataObjects.PageScanResult Item);
    Task<List<DataObjects.A11yViolation>> GetViolations(List<Guid>? Ids, Guid? PageScanResultId);
    Task SaveViolations(List<DataObjects.A11yViolation> Items);
    Task<List<DataObjects.A11yViolation>> GetViolationsByRule(Guid ScanRunId, string? CanonicalRuleId);
    Task<List<DataObjects.CrossSiteViolation>> GetCrossSiteViolations(Guid TenantId, DataObjects.CrossSiteViolationFilter filter);
    Task<List<DataObjects.ViolationSuppression>> GetViolationSuppressions(Guid TenantId);
    Task<DataObjects.ViolationSuppression> SaveViolationSuppression(DataObjects.ViolationSuppression Item, DataObjects.User? CurrentUser = null);
    Task<bool> DeleteViolationSuppression(Guid ViolationSuppressionId, Guid TenantId);
    Task<List<DataObjects.ScanRun>> GetSiteScanHistory(Guid SiteId, int Count = 20);
    Task<Dictionary<Guid, List<DataObjects.ScanRun>>> GetAllSiteScanHistory(Guid TenantId, int CountPerSite = 30);
    Task<int> PruneOldScans(Guid SiteId, int KeepCount);
    Task SaveScreenshots(List<DataObjects.ScanScreenshot> Items);
    Task SaveImages(List<DataObjects.ScanImage> Items);
    Task SaveCertificate(DataObjects.ScanCertificate Item);
    Task SaveRankedRules(List<DataObjects.ScanRankedRule> Items);
    Task SaveArtifacts(List<DataObjects.ScanArtifact> Items);
    Task<List<DataObjects.ScanScreenshot>> GetScreenshots(Guid PageScanResultId);
    Task<DataObjects.ScanScreenshot?> GetScreenshotData(Guid ScanScreenshotId);
    Task<DataObjects.ScanScreenshot?> GetScreenshotByFileName(Guid PageScanResultId, string FileName);
    Task<List<DataObjects.ScanImage>> GetImages(Guid PageScanResultId);
    Task<DataObjects.ScanCertificate?> GetCertificate(Guid PageScanResultId);
    Task<List<DataObjects.ScanRankedRule>> GetRankedRules(Guid PageScanResultId);
    Task<DataObjects.ScanArtifact?> GetArtifactData(Guid ScanArtifactId);
    Task<List<DataObjects.ScanArtifact>> GetArtifacts(Guid PageScanResultId);
}

public partial class DataAccess
{
    public async Task<List<DataObjects.ScanRun>> GetScanRuns(List<Guid>? Ids, Guid? SiteId, Guid TenantId)
    {
        List<DataObjects.ScanRun> output = new List<DataObjects.ScanRun>();

        try {
            IQueryable<EFModels.EFModels.ScanRun> query = data.ScanRuns.Where(x => x.TenantId == TenantId);

            if (SiteId != null && SiteId != Guid.Empty) {
                query = query.Where(x => x.SiteId == SiteId);
            }

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.ScanRunId));
            }

            query = query.OrderByDescending(x => x.StartedAt);

            List<EFModels.EFModels.ScanRun> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                // Load site names for denormalization
                List<Guid> siteIds = recs.Select(x => x.SiteId).Distinct().ToList();
                Dictionary<Guid, string> siteNames = await data.Sites
                    .Where(x => siteIds.Contains(x.SiteId))
                    .ToDictionaryAsync(x => x.SiteId, x => x.Name ?? String.Empty);

                foreach (EFModels.EFModels.ScanRun rec in recs) {
                    string siteName = siteNames.ContainsKey(rec.SiteId) ? siteNames[rec.SiteId] : String.Empty;

                    output.Add(new DataObjects.ScanRun {
                        ActionResponse = GetNewActionResponse(true),
                        ScanRunId = rec.ScanRunId,
                        SiteId = rec.SiteId,
                        TenantId = rec.TenantId,
                        SiteName = siteName,
                        Status = rec.Status,
                        StartedAt = rec.StartedAt,
                        CompletedAt = rec.CompletedAt,
                        TriggeredBy = rec.TriggeredBy,
                        PagesScanned = rec.PagesScanned,
                        TotalViolations = rec.TotalViolations,
                        CriticalCount = rec.CriticalCount,
                        SeriousCount = rec.SeriousCount,
                        ModerateCount = rec.ModerateCount,
                        MinorCount = rec.MinorCount,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetScanRuns: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.ScanRun>> GetPendingScanRuns()
    {
        List<DataObjects.ScanRun> output = new List<DataObjects.ScanRun>();

        try {
            List<EFModels.EFModels.ScanRun> recs = await data.ScanRuns
                .Where(x => x.Status == "Queued")
                .OrderBy(x => x.StartedAt)
                .ToListAsync();

            if (recs != null && recs.Any()) {
                List<Guid> siteIds = recs.Select(x => x.SiteId).Distinct().ToList();
                Dictionary<Guid, string> siteNames = await data.Sites
                    .Where(x => siteIds.Contains(x.SiteId))
                    .ToDictionaryAsync(x => x.SiteId, x => x.Name ?? String.Empty);

                foreach (EFModels.EFModels.ScanRun rec in recs) {
                    string siteName = siteNames.ContainsKey(rec.SiteId) ? siteNames[rec.SiteId] : String.Empty;

                    output.Add(new DataObjects.ScanRun {
                        ActionResponse = GetNewActionResponse(true),
                        ScanRunId = rec.ScanRunId,
                        SiteId = rec.SiteId,
                        TenantId = rec.TenantId,
                        SiteName = siteName,
                        Status = rec.Status,
                        StartedAt = rec.StartedAt,
                        CompletedAt = rec.CompletedAt,
                        TriggeredBy = rec.TriggeredBy,
                        PagesScanned = rec.PagesScanned,
                        TotalViolations = rec.TotalViolations,
                        CriticalCount = rec.CriticalCount,
                        SeriousCount = rec.SeriousCount,
                        ModerateCount = rec.ModerateCount,
                        MinorCount = rec.MinorCount,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPendingScanRuns: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    /// <summary>
    /// Marks every ScanRun whose Status is "Running" or "Queued" as "Cancelled" and stamps
    /// CompletedAt = UtcNow. Intended to run once at app startup so runs that were in-flight
    /// when the app was killed don't sit forever as zombies in the Scan Monitor view.
    /// Does NOT touch Complete/Completed/Failed runs (those carry real result data and stay
    /// untouched). Returns the count cancelled. Best-effort — failure is logged, not thrown.
    /// </summary>
    public async Task<int> CancelOrphanedScanRuns()
    {
        try {
            List<EFModels.EFModels.ScanRun> orphans = await data.ScanRuns
                .Where(x => x.Status == "Running" || x.Status == "Queued")
                .ToListAsync();

            if (orphans.Count == 0) return 0;

            DateTime now = DateTime.UtcNow;
            foreach (var rec in orphans) {
                rec.Status = "Cancelled";
                rec.CompletedAt = now;
            }
            await data.SaveChangesAsync();
            return orphans.Count;
        } catch (Exception ex) {
            Console.WriteLine("Error in CancelOrphanedScanRuns: " + RecurseExceptionAsString(ex));
            return 0;
        }
    }

    public async Task<DataObjects.ScanRun> SaveScanRun(DataObjects.ScanRun Item, DataObjects.User? CurrentUser = null)
    {
        DataObjects.ScanRun output = Item;
        output.ActionResponse = GetNewActionResponse();

        try {
            DateTime now = DateTime.UtcNow;
            bool newRecord = false;
            EFModels.EFModels.ScanRun? rec = null;

            if (Item.ScanRunId == Guid.Empty) {
                newRecord = true;
                Item.ScanRunId = Guid.NewGuid();

                rec = new EFModels.EFModels.ScanRun {
                    ScanRunId = Item.ScanRunId,
                    SiteId = Item.SiteId,
                    TenantId = Item.TenantId,
                    StartedAt = now,
                };
            } else {
                rec = await data.ScanRuns.FirstOrDefaultAsync(x => x.ScanRunId == Item.ScanRunId);

                if (rec == null) {
                    output.ActionResponse.Messages.Add("Scan Run '" + Item.ScanRunId.ToString() + "' No Longer Exists");
                    return output;
                }
            }

            rec.Status = Item.Status;
            rec.TriggeredBy = Item.TriggeredBy;
            rec.PagesScanned = Item.PagesScanned;
            rec.TotalViolations = Item.TotalViolations;
            rec.CriticalCount = Item.CriticalCount;
            rec.SeriousCount = Item.SeriousCount;
            rec.ModerateCount = Item.ModerateCount;
            rec.MinorCount = Item.MinorCount;
            rec.CompletedAt = Item.CompletedAt;

            if (newRecord) {
                await data.ScanRuns.AddAsync(rec);
            }

            await data.SaveChangesAsync();

            // Update parent site with latest scan info
            EFModels.EFModels.Site? site = await data.Sites.FirstOrDefaultAsync(x => x.SiteId == rec.SiteId);
            if (site != null) {
                site.LastScanRunId = rec.ScanRunId;
                site.LastScanAt = rec.StartedAt;
                site.LastScanStatus = rec.Status;
                site.LastViolationCount = rec.TotalViolations;
                site.LastCriticalCount = rec.CriticalCount;
                site.LastModified = now;

                await data.SaveChangesAsync();
            }

            output.ScanRunId = rec.ScanRunId;
            output.ActionResponse.Result = true;

            await SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = Item.TenantId,
                ItemId = Item.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanProgress,
                Message = Item.Status ?? "Updated",
                UserId = CurrentUserId(CurrentUser),
                Object = output,
            });
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Saving Scan Run");
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.PageScanResult>> GetPageScanResults(List<Guid>? Ids, Guid? ScanRunId)
    {
        List<DataObjects.PageScanResult> output = new List<DataObjects.PageScanResult>();

        try {
            IQueryable<EFModels.EFModels.PageScanResult> query = data.PageScanResults
                .Include(x => x.ScanImages)
                .Include(x => x.ScanCertificate)
                .Include(x => x.ScanRankedRules)
                .AsQueryable();

            if (ScanRunId != null && ScanRunId != Guid.Empty) {
                query = query.Where(x => x.ScanRunId == ScanRunId);
            }

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.PageScanResultId));
            }

            query = query.OrderBy(x => x.Url);

            List<EFModels.EFModels.PageScanResult> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.PageScanResult rec in recs) {
                    output.Add(new DataObjects.PageScanResult {
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
                        ErrorMessage = rec.ErrorMessage,
                        OutputDir = rec.OutputDir,
                        Language = rec.Language,
                        ResponseHeaders = rec.ResponseHeaders,
                        AxePassCount = rec.AxePassCount,
                        AxeIncompleteCount = rec.AxeIncompleteCount,
                        AxeInapplicableCount = rec.AxeInapplicableCount,
                        IbmPassCount = rec.IbmPassCount,
                        IbmPotentialCount = rec.IbmPotentialCount,
                        IbmManualCount = rec.IbmManualCount,
                        HtmlCheckPassCount = rec.HtmlCheckPassCount,
                        HtmlCheckTotalChecks = rec.HtmlCheckTotalChecks,
                        HeadingsJson = rec.HeadingsJson,
                        LandmarksJson = rec.LandmarksJson,
                        MediaJson = rec.MediaJson,
                        AriaLiveRegionsJson = rec.AriaLiveRegionsJson,
                        TargetSizeJson = rec.TargetSizeJson,
                        PerformanceJson = rec.PerformanceJson,
                        AccessibilityTreeJson = rec.AccessibilityTreeJson,
                        KeyboardNavJson = rec.KeyboardNavJson,
                        FocusTrapsJson = rec.FocusTrapsJson,
                        TextSpacingJson = rec.TextSpacingJson,
                        ReadingLevelJson = rec.ReadingLevelJson,
                        AutocompleteJson = rec.AutocompleteJson,
                        FixedElementsJson = rec.FixedElementsJson,
                        MobileViewportsJson = rec.MobileViewportsJson,
                        Images = rec.ScanImages?.Select(i => new DataObjects.ScanImage {
                            ScanImageId = i.ScanImageId,
                            PageScanResultId = i.PageScanResultId,
                            Url = i.Url,
                            AltText = i.AltText,
                            HasAlt = i.HasAlt,
                        }).ToList() ?? new(),
                        RankedRules = rec.ScanRankedRules?.Select(r => new DataObjects.ScanRankedRule {
                            ScanRankedRuleId = r.ScanRankedRuleId,
                            PageScanResultId = r.PageScanResultId,
                            CanonicalRuleId = r.CanonicalRuleId,
                            Severity = r.Severity,
                            Consensus = r.Consensus,
                            Confidence = r.Confidence,
                            ToolsFound = r.ToolsFound,
                        }).ToList() ?? new(),
                        Certificate = rec.ScanCertificate != null ? new DataObjects.ScanCertificate {
                            ScanCertificateId = rec.ScanCertificate.ScanCertificateId,
                            PageScanResultId = rec.ScanCertificate.PageScanResultId,
                            Subject = rec.ScanCertificate.Subject,
                            Issuer = rec.ScanCertificate.Issuer,
                            Expiry = rec.ScanCertificate.Expiry,
                            SubjectAlternativeNames = rec.ScanCertificate.SubjectAlternativeNames,
                        } : null,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetPageScanResults: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<DataObjects.PageScanResult> SavePageScanResult(DataObjects.PageScanResult Item)
    {
        DataObjects.PageScanResult output = Item;
        output.ActionResponse = GetNewActionResponse();

        try {
            EFModels.EFModels.PageScanResult rec = new EFModels.EFModels.PageScanResult {
                PageScanResultId = Guid.NewGuid(),
                ScanRunId = Item.ScanRunId,
                SitePageId = Item.SitePageId,
                Url = Item.Url,
                StatusCode = Item.StatusCode,
                PageTitle = Item.PageTitle,
                HtmlSizeBytes = Item.HtmlSizeBytes,
                ScreenshotPath = Item.ScreenshotPath,
                AxeViolationCount = Item.AxeViolationCount,
                HtmlCheckViolationCount = Item.HtmlCheckViolationCount,
                OverlayViolationCount = Item.OverlayViolationCount,
                TotalViolations = Item.TotalViolations,
                CriticalCount = Item.CriticalCount,
                SeriousCount = Item.SeriousCount,
                ModerateCount = Item.ModerateCount,
                MinorCount = Item.MinorCount,
                ScanDurationMs = Item.ScanDurationMs,
                ErrorMessage = Item.ErrorMessage,
                OutputDir = Item.OutputDir,
                Language = Item.Language,
                ResponseHeaders = Item.ResponseHeaders,
                AxePassCount = Item.AxePassCount,
                AxeIncompleteCount = Item.AxeIncompleteCount,
                AxeInapplicableCount = Item.AxeInapplicableCount,
                IbmPassCount = Item.IbmPassCount,
                IbmPotentialCount = Item.IbmPotentialCount,
                IbmManualCount = Item.IbmManualCount,
                HtmlCheckPassCount = Item.HtmlCheckPassCount,
                HtmlCheckTotalChecks = Item.HtmlCheckTotalChecks,
                HeadingsJson = Item.HeadingsJson,
                LandmarksJson = Item.LandmarksJson,
                MediaJson = Item.MediaJson,
                AriaLiveRegionsJson = Item.AriaLiveRegionsJson,
                TargetSizeJson = Item.TargetSizeJson,
                PerformanceJson = Item.PerformanceJson,
                AccessibilityTreeJson = Item.AccessibilityTreeJson,
                KeyboardNavJson = Item.KeyboardNavJson,
                FocusTrapsJson = Item.FocusTrapsJson,
                TextSpacingJson = Item.TextSpacingJson,
                ReadingLevelJson = Item.ReadingLevelJson,
                AutocompleteJson = Item.AutocompleteJson,
                FixedElementsJson = Item.FixedElementsJson,
                MobileViewportsJson = Item.MobileViewportsJson,
            };

            await data.PageScanResults.AddAsync(rec);
            await data.SaveChangesAsync();

            // Update parent ScanRun with incremented counts
            EFModels.EFModels.ScanRun? scanRun = await data.ScanRuns.FirstOrDefaultAsync(x => x.ScanRunId == Item.ScanRunId);
            if (scanRun != null) {
                scanRun.PagesScanned = scanRun.PagesScanned + 1;
                scanRun.TotalViolations = scanRun.TotalViolations + Item.TotalViolations;
                scanRun.CriticalCount = scanRun.CriticalCount + Item.CriticalCount;
                scanRun.SeriousCount = scanRun.SeriousCount + Item.SeriousCount;
                scanRun.ModerateCount = scanRun.ModerateCount + Item.ModerateCount;
                scanRun.MinorCount = scanRun.MinorCount + Item.MinorCount;

                await data.SaveChangesAsync();
            }

            output.PageScanResultId = rec.PageScanResultId;
            output.ActionResponse.Result = true;
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Saving Page Scan Result");
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.A11yViolation>> GetViolations(List<Guid>? Ids, Guid? PageScanResultId)
    {
        List<DataObjects.A11yViolation> output = new List<DataObjects.A11yViolation>();

        try {
            IQueryable<EFModels.EFModels.A11yViolation> query = data.A11yViolations.AsQueryable();

            if (PageScanResultId != null && PageScanResultId != Guid.Empty) {
                query = query.Where(x => x.PageScanResultId == PageScanResultId);
            }

            if (Ids != null && Ids.Any()) {
                query = query.Where(x => Ids.Contains(x.A11yViolationId));
            }

            query = query.OrderBy(x => x.Severity).ThenBy(x => x.Tool).ThenBy(x => x.RuleId);

            List<EFModels.EFModels.A11yViolation> recs = await query.ToListAsync();

            if (recs != null && recs.Any()) {
                foreach (EFModels.EFModels.A11yViolation rec in recs) {
                    output.Add(new DataObjects.A11yViolation {
                        ActionResponse = GetNewActionResponse(true),
                        A11yViolationId = rec.A11yViolationId,
                        PageScanResultId = rec.PageScanResultId,
                        Tool = rec.Tool,
                        RuleId = rec.RuleId,
                        CanonicalRuleId = rec.CanonicalRuleId,
                        Severity = rec.Severity,
                        Message = rec.Message,
                        HtmlSnippet = rec.HtmlSnippet,
                        Selector = rec.Selector,
                        HelpUrl = rec.HelpUrl,
                        WcagCriteria = rec.WcagCriteria,
                        ContrastForeground = rec.ContrastForeground,
                        ContrastBackground = rec.ContrastBackground,
                        ContrastRatio = rec.ContrastRatio,
                        ContrastExpected = rec.ContrastExpected,
                        ContrastFontSize = rec.ContrastFontSize,
                        ContrastFontWeight = rec.ContrastFontWeight,
                    });
                }

                // Apply suppressions. Derive tenant + site from any one of the violations
                // (all loaded violations belong to the same page → same site → same tenant).
                Guid firstPageId = output.First().PageScanResultId;
                var ctx = await data.PageScanResults
                    .Where(p => p.PageScanResultId == firstPageId)
                    .Select(p => new { p.ScanRun.TenantId, p.ScanRun.SiteId })
                    .FirstOrDefaultAsync();
                if (ctx != null && ctx.TenantId != Guid.Empty) {
                    var sups = await LoadActiveSuppressions(ctx.TenantId);
                    ApplySuppressionsToViolations(output, sups, ctx.SiteId);
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetViolations: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task SaveViolations(List<DataObjects.A11yViolation> Items)
    {
        try {
            if (Items == null || !Items.Any()) { return; }

            List<EFModels.EFModels.A11yViolation> recs = new List<EFModels.EFModels.A11yViolation>();

            foreach (DataObjects.A11yViolation item in Items) {
                recs.Add(new EFModels.EFModels.A11yViolation {
                    A11yViolationId = Guid.NewGuid(),
                    PageScanResultId = item.PageScanResultId,
                    Tool = item.Tool,
                    RuleId = item.RuleId,
                    CanonicalRuleId = item.CanonicalRuleId,
                    Severity = item.Severity,
                    Message = item.Message,
                    Selector = item.Selector,
                    HtmlSnippet = item.HtmlSnippet,
                    HelpUrl = item.HelpUrl,
                    WcagCriteria = item.WcagCriteria,
                    ContrastForeground = item.ContrastForeground,
                    ContrastBackground = item.ContrastBackground,
                    ContrastRatio = item.ContrastRatio,
                    ContrastExpected = item.ContrastExpected,
                    ContrastFontSize = item.ContrastFontSize,
                    ContrastFontWeight = item.ContrastFontWeight,
                });
            }

            await data.A11yViolations.AddRangeAsync(recs);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveViolations: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task<List<DataObjects.A11yViolation>> GetViolationsByRule(Guid ScanRunId, string? CanonicalRuleId)
    {
        List<DataObjects.A11yViolation> output = new List<DataObjects.A11yViolation>();

        try {
            // Join violations to page scan results to scope by ScanRunId
            IQueryable<EFModels.EFModels.A11yViolation> query = data.A11yViolations
                .Join(data.PageScanResults,
                    v => v.PageScanResultId,
                    p => p.PageScanResultId,
                    (v, p) => new { Violation = v, PageResult = p })
                .Where(x => x.PageResult.ScanRunId == ScanRunId)
                .Select(x => x.Violation);

            if (!String.IsNullOrWhiteSpace(CanonicalRuleId)) {
                query = query.Where(x => x.CanonicalRuleId == CanonicalRuleId);
            }

            List<EFModels.EFModels.A11yViolation> recs = await query.ToListAsync();

            // Group by rule for the "violations by rule" view
            var grouped = recs
                .GroupBy(x => new { x.CanonicalRuleId, x.Severity, x.Message })
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in grouped) {
                output.Add(new DataObjects.A11yViolation {
                    ActionResponse = GetNewActionResponse(true),
                    CanonicalRuleId = group.Key.CanonicalRuleId,
                    Severity = group.Key.Severity,
                    Message = group.Key.Message,
                    AffectedPageCount = group.Select(x => x.PageScanResultId).Distinct().Count(),
                    RuleId = group.First().RuleId,
                    Tool = group.First().Tool,
                    HelpUrl = group.First().HelpUrl,
                    WcagCriteria = group.First().WcagCriteria,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetViolationsByRule: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.CrossSiteViolation>> GetCrossSiteViolations(Guid TenantId, DataObjects.CrossSiteViolationFilter filter)
    {
        List<DataObjects.CrossSiteViolation> output = new List<DataObjects.CrossSiteViolation>();

        try {
            // Resolve which sites are in scope (tenant-scoped, optionally filtered by SiteIds).
            IQueryable<EFModels.EFModels.Site> siteQuery = data.Sites
                .Where(s => s.TenantId == TenantId && !s.Deleted && s.Enabled);

            if (filter.SiteIds != null && filter.SiteIds.Any()) {
                siteQuery = siteQuery.Where(s => filter.SiteIds.Contains(s.SiteId));
            }

            List<EFModels.EFModels.Site> sites = await siteQuery.ToListAsync();
            if (!sites.Any()) return output;

            HashSet<Guid> scopedSiteIds = sites.Select(s => s.SiteId).ToHashSet();
            Dictionary<Guid, EFModels.EFModels.Site> sitesById = sites.ToDictionary(s => s.SiteId);

            // Resolve which scan runs are in scope.
            IQueryable<EFModels.EFModels.ScanRun> runQuery = data.ScanRuns
                .Where(r => scopedSiteIds.Contains(r.SiteId)
                    && (r.Status == "Complete" || r.Status == "Completed"));

            List<EFModels.EFModels.ScanRun> runs = await runQuery
                .OrderByDescending(r => r.StartedAt)
                .ToListAsync();

            if (filter.LatestScanOnly) {
                runs = runs.GroupBy(r => r.SiteId).Select(g => g.First()).ToList();
            }

            if (!runs.Any()) return output;

            HashSet<Guid> scopedRunIds = runs.Select(r => r.ScanRunId).ToHashSet();
            Dictionary<Guid, EFModels.EFModels.ScanRun> runsById = runs.ToDictionary(r => r.ScanRunId);

            // Pull page results in scope.
            IQueryable<EFModels.EFModels.PageScanResult> pageQuery = data.PageScanResults
                .Where(p => scopedRunIds.Contains(p.ScanRunId));

            // URL pattern: convert simple glob to SQL LIKE (* and ** -> %).
            if (!String.IsNullOrWhiteSpace(filter.UrlPattern)) {
                string likePattern = GlobToLike(filter.UrlPattern);
                pageQuery = pageQuery.Where(p => EF.Functions.Like(p.Url, likePattern));
            }

            List<EFModels.EFModels.PageScanResult> pages = await pageQuery.ToListAsync();
            if (!pages.Any()) return output;

            HashSet<Guid> scopedPageIds = pages.Select(p => p.PageScanResultId).ToHashSet();
            Dictionary<Guid, EFModels.EFModels.PageScanResult> pagesById = pages.ToDictionary(p => p.PageScanResultId);

            // Pull violations in scope.
            IQueryable<EFModels.EFModels.A11yViolation> viQuery = data.A11yViolations
                .Where(v => scopedPageIds.Contains(v.PageScanResultId));

            if (!String.IsNullOrWhiteSpace(filter.Severity)) {
                string sev = filter.Severity.ToLower();
                viQuery = viQuery.Where(v => v.Severity.ToLower() == sev);
            }

            if (!String.IsNullOrWhiteSpace(filter.CanonicalRuleId)) {
                viQuery = viQuery.Where(v => v.CanonicalRuleId == filter.CanonicalRuleId);
            }

            List<EFModels.EFModels.A11yViolation> violations = await viQuery.ToListAsync();

            // Load suppressions once for the tenant; apply per-site as we map.
            var suppressions = await LoadActiveSuppressions(TenantId);

            foreach (var v in violations) {
                if (!pagesById.TryGetValue(v.PageScanResultId, out var page)) continue;
                if (!runsById.TryGetValue(page.ScanRunId, out var run)) continue;
                if (!sitesById.TryGetValue(run.SiteId, out var site)) continue;

                var match = suppressions.FirstOrDefault(s => SuppressionMatches(s, v.RuleId, v.Selector, site.SiteId));
                if (match == null && !String.IsNullOrWhiteSpace(v.CanonicalRuleId)) {
                    match = suppressions.FirstOrDefault(s => SuppressionMatches(s, v.CanonicalRuleId!, v.Selector, site.SiteId));
                }

                output.Add(new DataObjects.CrossSiteViolation {
                    A11yViolationId = v.A11yViolationId,
                    SiteId = site.SiteId,
                    SiteName = site.Name,
                    SiteBaseUrl = site.BaseUrl,
                    ScanRunId = run.ScanRunId,
                    ScanDate = run.StartedAt,
                    PageScanResultId = page.PageScanResultId,
                    PageUrl = page.Url,
                    PageTitle = page.PageTitle,
                    Tool = v.Tool,
                    RuleId = v.RuleId,
                    CanonicalRuleId = v.CanonicalRuleId,
                    Severity = match != null ? "minor" : v.Severity,
                    Message = v.Message,
                    Selector = v.Selector,
                    HelpUrl = v.HelpUrl,
                    WcagCriteria = v.WcagCriteria,
                    IsSuppressed = match != null,
                    SuppressionReason = match?.Reason,
                    OriginalSeverity = match != null ? v.Severity : null,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetCrossSiteViolations: " + RecurseExceptionAsString(ex));
        }

        return output;
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

    /// <summary>
    /// Returns true if the suppression rule matches a given (RuleId, Selector, SiteId) tuple.
    /// SelectorPattern uses simple glob matching: '*' matches any chars, exact-match if no '*'.
    /// SiteId on the suppression: null = applies to all sites in tenant; otherwise must match.
    /// </summary>
    private static bool SuppressionMatches(EFModels.EFModels.ViolationSuppression s,
        string ruleId, string? selector, Guid? siteId)
    {
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

    /// <summary>
    /// Mutates each violation in-place: if any active suppression matches, sets IsSuppressed=true,
    /// stores the original severity, downgrades the displayed severity to "minor", and copies the reason.
    /// </summary>
    private static void ApplySuppressionsToViolations(
        List<DataObjects.A11yViolation> violations,
        List<EFModels.EFModels.ViolationSuppression> suppressions,
        Guid? siteId = null)
    {
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

    private static string GlobToLike(string glob)
    {
        // Escape SQL LIKE special chars first, then map glob wildcards.
        string escaped = glob.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return escaped.Replace("**", "%").Replace("*", "%").Replace("?", "_");
    }

    public async Task<List<DataObjects.ScanRun>> GetSiteScanHistory(Guid SiteId, int Count = 20)
    {
        List<DataObjects.ScanRun> output = new List<DataObjects.ScanRun>();

        try {
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
                        TenantId = rec.TenantId,
                        Status = rec.Status,
                        StartedAt = rec.StartedAt,
                        CompletedAt = rec.CompletedAt,
                        PagesScanned = rec.PagesScanned,
                        TotalViolations = rec.TotalViolations,
                        CriticalCount = rec.CriticalCount,
                        SeriousCount = rec.SeriousCount,
                        ModerateCount = rec.ModerateCount,
                        MinorCount = rec.MinorCount,
                    });
                }
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetSiteScanHistory: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<Dictionary<Guid, List<DataObjects.ScanRun>>> GetAllSiteScanHistory(Guid TenantId, int CountPerSite = 30)
    {
        Dictionary<Guid, List<DataObjects.ScanRun>> output = new Dictionary<Guid, List<DataObjects.ScanRun>>();

        try {
            // Get all completed scan runs for the tenant, ordered by site then date.
            List<EFModels.EFModels.ScanRun> recs = await data.ScanRuns
                .Where(x => x.TenantId == TenantId && (x.Status == "Complete" || x.Status == "Failed"))
                .OrderByDescending(x => x.StartedAt)
                .ToListAsync();

            // Group by site and take last N per site.
            var grouped = recs.GroupBy(x => x.SiteId);

            foreach (var group in grouped) {
                List<DataObjects.ScanRun> siteRuns = new List<DataObjects.ScanRun>();

                foreach (EFModels.EFModels.ScanRun rec in group.Take(CountPerSite)) {
                    siteRuns.Add(new DataObjects.ScanRun {
                        ActionResponse = GetNewActionResponse(true),
                        ScanRunId = rec.ScanRunId,
                        SiteId = rec.SiteId,
                        TenantId = rec.TenantId,
                        Status = rec.Status,
                        StartedAt = rec.StartedAt,
                        CompletedAt = rec.CompletedAt,
                        PagesScanned = rec.PagesScanned,
                        TotalViolations = rec.TotalViolations,
                        CriticalCount = rec.CriticalCount,
                        SeriousCount = rec.SeriousCount,
                        ModerateCount = rec.ModerateCount,
                        MinorCount = rec.MinorCount,
                        TriggeredBy = rec.TriggeredBy,
                    });
                }

                output[group.Key] = siteRuns;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetAllSiteScanHistory: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<int> PruneOldScans(Guid SiteId, int KeepCount)
    {
        int output = 0;

        try {
            List<EFModels.EFModels.ScanRun> oldRuns = await data.ScanRuns
                .Where(x => x.SiteId == SiteId)
                .OrderByDescending(x => x.StartedAt)
                .Skip(KeepCount)
                .ToListAsync();

            if (oldRuns != null && oldRuns.Any()) {
                List<Guid> oldRunIds = oldRuns.Select(x => x.ScanRunId).ToList();

                // Delete child violations via page results
                List<Guid> oldPageResultIds = await data.PageScanResults
                    .Where(x => oldRunIds.Contains(x.ScanRunId))
                    .Select(x => x.PageScanResultId)
                    .ToListAsync();

                if (oldPageResultIds.Any()) {
                    data.ScanScreenshots.RemoveRange(
                        data.ScanScreenshots.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    data.ScanImages.RemoveRange(
                        data.ScanImages.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    data.ScanCertificates.RemoveRange(
                        data.ScanCertificates.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    data.ScanRankedRules.RemoveRange(
                        data.ScanRankedRules.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    data.A11yViolations.RemoveRange(
                        data.A11yViolations.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    data.ScanArtifacts.RemoveRange(
                        data.ScanArtifacts.Where(x => oldPageResultIds.Contains(x.PageScanResultId)));
                    await data.SaveChangesAsync();
                }

                // Delete child page results
                data.PageScanResults.RemoveRange(
                    data.PageScanResults.Where(x => oldRunIds.Contains(x.ScanRunId)));
                await data.SaveChangesAsync();

                // Delete the scan runs themselves
                data.ScanRuns.RemoveRange(oldRuns);
                await data.SaveChangesAsync();

                output = oldRuns.Count;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in PruneOldScans: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task SaveScreenshots(List<DataObjects.ScanScreenshot> Items)
    {
        try {
            if (Items == null || !Items.Any()) { return; }

            List<EFModels.EFModels.ScanScreenshot> recs = Items.Select(item => new EFModels.EFModels.ScanScreenshot {
                ScanScreenshotId = Guid.NewGuid(),
                PageScanResultId = item.PageScanResultId,
                Path = item.Path,
                Label = item.Label,
                SizeBytes = item.SizeBytes,
                ContentType = item.ContentType,
                Data = item.Data,
            }).ToList();

            await data.ScanScreenshots.AddRangeAsync(recs);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveScreenshots: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task SaveImages(List<DataObjects.ScanImage> Items)
    {
        try {
            if (Items == null || !Items.Any()) { return; }

            List<EFModels.EFModels.ScanImage> recs = Items.Select(item => new EFModels.EFModels.ScanImage {
                ScanImageId = Guid.NewGuid(),
                PageScanResultId = item.PageScanResultId,
                Url = item.Url,
                AltText = item.AltText,
                HasAlt = item.HasAlt,
            }).ToList();

            await data.ScanImages.AddRangeAsync(recs);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveImages: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task SaveCertificate(DataObjects.ScanCertificate Item)
    {
        try {
            if (Item == null) { return; }

            EFModels.EFModels.ScanCertificate rec = new EFModels.EFModels.ScanCertificate {
                ScanCertificateId = Guid.NewGuid(),
                PageScanResultId = Item.PageScanResultId,
                Subject = Item.Subject,
                Issuer = Item.Issuer,
                Expiry = Item.Expiry,
                SubjectAlternativeNames = Item.SubjectAlternativeNames,
            };

            await data.ScanCertificates.AddAsync(rec);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveCertificate: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task SaveRankedRules(List<DataObjects.ScanRankedRule> Items)
    {
        try {
            if (Items == null || !Items.Any()) { return; }

            List<EFModels.EFModels.ScanRankedRule> recs = Items.Select(item => new EFModels.EFModels.ScanRankedRule {
                ScanRankedRuleId = Guid.NewGuid(),
                PageScanResultId = item.PageScanResultId,
                CanonicalRuleId = item.CanonicalRuleId,
                Severity = item.Severity,
                Consensus = item.Consensus,
                Confidence = item.Confidence,
                ToolsFound = item.ToolsFound,
            }).ToList();

            await data.ScanRankedRules.AddRangeAsync(recs);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveRankedRules: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task<List<DataObjects.ScanScreenshot>> GetScreenshots(Guid PageScanResultId)
    {
        List<DataObjects.ScanScreenshot> output = new List<DataObjects.ScanScreenshot>();

        try {
            var recs = await data.ScanScreenshots
                .Where(x => x.PageScanResultId == PageScanResultId)
                .OrderBy(x => x.Path)
                .Select(x => new { x.ScanScreenshotId, x.PageScanResultId, x.Path, x.Label, x.SizeBytes, x.ContentType })
                .ToListAsync();

            foreach (var rec in recs) {
                output.Add(new DataObjects.ScanScreenshot {
                    ScanScreenshotId = rec.ScanScreenshotId,
                    PageScanResultId = rec.PageScanResultId,
                    Path = rec.Path,
                    Label = rec.Label,
                    SizeBytes = rec.SizeBytes,
                    ContentType = rec.ContentType,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetScreenshots: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<DataObjects.ScanScreenshot?> GetScreenshotData(Guid ScanScreenshotId)
    {
        try {
            EFModels.EFModels.ScanScreenshot? rec = await data.ScanScreenshots
                .FirstOrDefaultAsync(x => x.ScanScreenshotId == ScanScreenshotId);

            if (rec != null) {
                return new DataObjects.ScanScreenshot {
                    ScanScreenshotId = rec.ScanScreenshotId,
                    PageScanResultId = rec.PageScanResultId,
                    Path = rec.Path,
                    Label = rec.Label,
                    SizeBytes = rec.SizeBytes,
                    ContentType = rec.ContentType,
                    Data = rec.Data,
                };
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetScreenshotData: " + RecurseExceptionAsString(ex));
        }

        return null;
    }

    public async Task<DataObjects.ScanScreenshot?> GetScreenshotByFileName(Guid PageScanResultId, string FileName)
    {
        try {
            EFModels.EFModels.ScanScreenshot? rec = await data.ScanScreenshots
                .FirstOrDefaultAsync(x => x.PageScanResultId == PageScanResultId && x.Path.EndsWith(FileName));

            if (rec != null) {
                return new DataObjects.ScanScreenshot {
                    ScanScreenshotId = rec.ScanScreenshotId,
                    PageScanResultId = rec.PageScanResultId,
                    Path = rec.Path,
                    Label = rec.Label,
                    SizeBytes = rec.SizeBytes,
                    ContentType = rec.ContentType,
                    Data = rec.Data,
                };
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetScreenshotByFileName: " + RecurseExceptionAsString(ex));
        }

        return null;
    }

    public async Task SaveArtifacts(List<DataObjects.ScanArtifact> Items)
    {
        try {
            if (Items == null || !Items.Any()) { return; }

            List<EFModels.EFModels.ScanArtifact> recs = Items.Select(item => new EFModels.EFModels.ScanArtifact {
                ScanArtifactId = Guid.NewGuid(),
                PageScanResultId = item.PageScanResultId,
                FileName = item.FileName,
                Path = item.Path ?? string.Empty,
                ContentType = item.ContentType,
                Data = item.Data,
                SizeBytes = item.SizeBytes,
            }).ToList();

            await data.ScanArtifacts.AddRangeAsync(recs);
            await data.SaveChangesAsync();
        } catch (Exception ex) {
            Console.WriteLine("Error in SaveArtifacts: " + RecurseExceptionAsString(ex));
        }
    }

    public async Task<DataObjects.ScanArtifact?> GetArtifactData(Guid ScanArtifactId)
    {
        try {
            EFModels.EFModels.ScanArtifact? rec = await data.ScanArtifacts
                .FirstOrDefaultAsync(x => x.ScanArtifactId == ScanArtifactId);

            if (rec != null) {
                return new DataObjects.ScanArtifact {
                    ScanArtifactId = rec.ScanArtifactId,
                    PageScanResultId = rec.PageScanResultId,
                    FileName = rec.FileName,
                    Path = rec.Path ?? string.Empty,
                    ContentType = rec.ContentType,
                    Data = rec.Data,
                    SizeBytes = rec.SizeBytes,
                };
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetArtifactData: " + RecurseExceptionAsString(ex));
        }

        return null;
    }

    public async Task<List<DataObjects.ScanArtifact>> GetArtifacts(Guid PageScanResultId)
    {
        List<DataObjects.ScanArtifact> output = new List<DataObjects.ScanArtifact>();

        try {
            var recs = await data.ScanArtifacts
                .Where(x => x.PageScanResultId == PageScanResultId)
                .OrderBy(x => x.FileName)
                .Select(x => new { x.ScanArtifactId, x.PageScanResultId, x.FileName, x.ContentType, x.SizeBytes })
                .ToListAsync();

            foreach (var rec in recs) {
                output.Add(new DataObjects.ScanArtifact {
                    ScanArtifactId = rec.ScanArtifactId,
                    PageScanResultId = rec.PageScanResultId,
                    FileName = rec.FileName,
                    ContentType = rec.ContentType,
                    SizeBytes = rec.SizeBytes,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetArtifacts: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<List<DataObjects.ScanImage>> GetImages(Guid PageScanResultId)
    {
        List<DataObjects.ScanImage> output = new List<DataObjects.ScanImage>();

        try {
            List<EFModels.EFModels.ScanImage> recs = await data.ScanImages
                .Where(x => x.PageScanResultId == PageScanResultId)
                .OrderBy(x => x.Url)
                .ToListAsync();

            foreach (var rec in recs) {
                output.Add(new DataObjects.ScanImage {
                    ScanImageId = rec.ScanImageId,
                    PageScanResultId = rec.PageScanResultId,
                    Url = rec.Url,
                    AltText = rec.AltText,
                    HasAlt = rec.HasAlt,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetImages: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    public async Task<DataObjects.ScanCertificate?> GetCertificate(Guid PageScanResultId)
    {
        try {
            EFModels.EFModels.ScanCertificate? rec = await data.ScanCertificates
                .FirstOrDefaultAsync(x => x.PageScanResultId == PageScanResultId);

            if (rec != null) {
                return new DataObjects.ScanCertificate {
                    ScanCertificateId = rec.ScanCertificateId,
                    PageScanResultId = rec.PageScanResultId,
                    Subject = rec.Subject,
                    Issuer = rec.Issuer,
                    Expiry = rec.Expiry,
                    SubjectAlternativeNames = rec.SubjectAlternativeNames,
                };
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetCertificate: " + RecurseExceptionAsString(ex));
        }

        return null;
    }

    public async Task<List<DataObjects.ScanRankedRule>> GetRankedRules(Guid PageScanResultId)
    {
        List<DataObjects.ScanRankedRule> output = new List<DataObjects.ScanRankedRule>();

        try {
            List<EFModels.EFModels.ScanRankedRule> recs = await data.ScanRankedRules
                .Where(x => x.PageScanResultId == PageScanResultId)
                .OrderByDescending(x => x.Confidence)
                .ThenByDescending(x => x.Consensus)
                .ToListAsync();

            foreach (var rec in recs) {
                output.Add(new DataObjects.ScanRankedRule {
                    ScanRankedRuleId = rec.ScanRankedRuleId,
                    PageScanResultId = rec.PageScanResultId,
                    CanonicalRuleId = rec.CanonicalRuleId,
                    Severity = rec.Severity,
                    Consensus = rec.Consensus,
                    Confidence = rec.Confidence,
                    ToolsFound = rec.ToolsFound,
                });
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetRankedRules: " + RecurseExceptionAsString(ex));
        }

        return output;
    }
}
