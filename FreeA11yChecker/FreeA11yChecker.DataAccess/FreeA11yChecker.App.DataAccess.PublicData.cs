namespace FreeA11yChecker;

public partial interface IDataAccess
{
    Task<List<DataObjects.Site>> GetPublicSites(Guid TenantId);
    Task<List<DataObjects.ScanRun>> GetPublicScanHistory(Guid SiteId, int Count = 10);
    Task<DataObjects.ScanRun?> GetPublicScanDetail(Guid ScanRunId);
    Task<DataObjects.PageScanResult?> GetPublicPageDetail(Guid PageScanResultId);
}

public partial class DataAccess
{
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
}
