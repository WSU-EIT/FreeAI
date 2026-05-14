using FreeA11yChecker.Server.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FreeA11yChecker.Server.Controllers;

public partial class DataController
{
    // ── Sites ──────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetSites")]
    public async Task<ActionResult<List<DataObjects.Site>>> GetSites(List<Guid> Ids)
    {
        List<DataObjects.Site> output = await da.GetSites(Ids, TenantId, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveSites")]
    public async Task<ActionResult<List<DataObjects.Site>>> SaveSites(List<DataObjects.Site> Items)
    {
        List<DataObjects.Site> output = await da.SaveSites(Items, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteSites")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteSites(List<Guid> Ids)
    {
        DataObjects.BooleanResponse output = await da.DeleteSites(Ids, CurrentUser);
        return Ok(output);
    }

    // ── Site Pages ─────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetSitePages")]
    public async Task<ActionResult<List<DataObjects.SitePage>>> GetSitePages(DataObjects.SiteChildFilter filter)
    {
        List<DataObjects.SitePage> output = await da.GetSitePages(filter.Ids, filter.SiteId);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveSitePages")]
    public async Task<ActionResult<List<DataObjects.SitePage>>> SaveSitePages(List<DataObjects.SitePage> Items)
    {
        List<DataObjects.SitePage> output = await da.SaveSitePages(Items, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteSitePages")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteSitePages(List<Guid> Ids)
    {
        DataObjects.BooleanResponse output = await da.DeleteSitePages(Ids, CurrentUser);
        return Ok(output);
    }

    // ── Discover Links ─────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DiscoverLinks")]
    public async Task<ActionResult<DataObjects.DiscoverLinksResult>> DiscoverLinks(DataObjects.DiscoverLinksFilter filter)
    {
        DataObjects.DiscoverLinksResult output = new();
        try {
            // Build the list of URLs to crawl: base URL + all configured sub-pages.
            List<string> urlsToCrawl = new() { filter.Url };
            if (filter.AdditionalUrls != null && filter.AdditionalUrls.Any()) {
                urlsToCrawl.AddRange(filter.AdditionalUrls);
            }

            Uri baseUri = new Uri(filter.Url);
            HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seenExternal = new(StringComparer.OrdinalIgnoreCase);

            // Load existing pages to exclude already-added paths.
            var existingPages = await da.GetSitePages(null, filter.SiteId);
            HashSet<string> existingPaths = new(existingPages.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() {
                Headless = true,
                Channel = "msedge",
                Args = new[] { "--disable-blink-features=AutomationControlled" },
            });
            var context = await browser.NewContextAsync(new() {
                IgnoreHTTPSErrors = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1920, Height = 1080 },
                ExtraHTTPHeaders = new Dictionary<string, string> {
                    ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                    ["Accept-Language"] = "en-US,en;q=0.9",
                    ["Upgrade-Insecure-Requests"] = "1",
                },
            });
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                window.chrome = { runtime: {} };
            ");

            foreach (string crawlUrl in urlsToCrawl) {
                try {
                    var page = await context.NewPageAsync();
                    await page.GotoAsync(crawlUrl, new() { WaitUntil = Microsoft.Playwright.WaitUntilState.Load, Timeout = 30000 });
                    await page.WaitForTimeoutAsync(2000);

                    var links = await page.EvaluateAsync<DiscoverLinkInfo[]>(@"() => {
                        return Array.from(document.querySelectorAll('a[href]')).map(a => ({
                            href: a.href,
                            text: (a.textContent || '').trim().substring(0, 200)
                        })).filter(l => l.href && l.href.startsWith('http'));
                    }");

                    foreach (var link in links ?? Array.Empty<DiscoverLinkInfo>()) {
                        try {
                            Uri linkUri = new Uri(link.Href);
                            if (linkUri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase)) {
                                string path = linkUri.AbsolutePath;
                                if (!seenPaths.Contains(path) && !existingPaths.Contains(path)) {
                                    seenPaths.Add(path);
                                    output.InternalLinks.Add(new DataObjects.DiscoveredLink {
                                        Url = link.Href, Path = path, Text = link.Text, IsInternal = true
                                    });
                                }
                            } else {
                                string origin = linkUri.GetLeftPart(UriPartial.Authority);
                                if (!seenExternal.Contains(origin)) {
                                    seenExternal.Add(origin);
                                    output.ExternalLinks.Add(new DataObjects.DiscoveredLink {
                                        Url = origin, Path = linkUri.AbsolutePath, Text = link.Text, IsInternal = false
                                    });
                                }
                            }
                        } catch { }
                    }

                    await page.CloseAsync();
                } catch (Exception ex) {
                    // Log but continue crawling other pages.
                    Console.WriteLine("Error crawling " + crawlUrl + ": " + ex.Message);
                }
            }
        } catch (Exception ex) {
            output.ErrorMessage = ex.Message;
        }
        return Ok(output);
    }

    private class DiscoverLinkInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    // ── Site Credentials ───────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetSiteCredentials")]
    public async Task<ActionResult<List<DataObjects.SiteCredential>>> GetSiteCredentials(DataObjects.SiteChildFilter filter)
    {
        List<DataObjects.SiteCredential> output = await da.GetSiteCredentials(filter.Ids, filter.SiteId);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveSiteCredentials")]
    public async Task<ActionResult<List<DataObjects.SiteCredential>>> SaveSiteCredentials(List<DataObjects.SiteCredential> Items)
    {
        List<DataObjects.SiteCredential> output = await da.SaveSiteCredentials(Items, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteSiteCredentials")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteSiteCredentials(List<Guid> Ids)
    {
        DataObjects.BooleanResponse output = await da.DeleteSiteCredentials(Ids, CurrentUser);
        return Ok(output);
    }

    // ── Scan Runs ──────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetScanRuns")]
    public async Task<ActionResult<List<DataObjects.ScanRun>>> GetScanRuns(DataObjects.ScanRunFilter filter)
    {
        List<DataObjects.ScanRun> output = await da.GetScanRuns(filter.Ids, filter.SiteId, TenantId);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveScanRun")]
    public async Task<ActionResult<DataObjects.ScanRun>> SaveScanRun(DataObjects.ScanRun Item)
    {
        DataObjects.ScanRun output = await da.SaveScanRun(Item, CurrentUser);
        return Ok(output);
    }

    // ── Page Scan Results ──────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetPageScanResults")]
    public async Task<ActionResult<List<DataObjects.PageScanResult>>> GetPageScanResults(DataObjects.PageScanResultFilter filter)
    {
        List<DataObjects.PageScanResult> output = await da.GetPageScanResults(filter.Ids, filter.ScanRunId);
        return Ok(output);
    }

    // ── Screenshots ────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetScreenshots")]
    public async Task<ActionResult<List<DataObjects.ScanScreenshot>>> GetScreenshots([FromBody] Guid PageScanResultId)
    {
        List<DataObjects.ScanScreenshot> output = await da.GetScreenshots(PageScanResultId);
        return Ok(output);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("~/api/Data/ScanScreenshotFile/{PageScanResultId}/{FileName}")]
    public async Task<IActionResult> ScanScreenshotFile(Guid PageScanResultId, string FileName)
    {
        DataObjects.ScanScreenshot? screenshot = await da.GetScreenshotByFileName(PageScanResultId, FileName);
        if (screenshot == null) { return NotFound(); }
        Response.Headers.CacheControl = "public, max-age=86400";
        string contentType = screenshot.ContentType ?? "application/octet-stream";

        // Disk-first: bytes live on disk now; DB Data is only a legacy fallback.
        if (!string.IsNullOrEmpty(screenshot.Path) && System.IO.File.Exists(screenshot.Path)) {
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(screenshot.Path);
            return File(bytes, contentType, FileName);
        }
        if (screenshot.Data != null && screenshot.Data.Length > 0) {
            return File(screenshot.Data, contentType, FileName);
        }
        return NotFound();
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("~/api/Data/ScanArtifactFile/{ScanArtifactId}")]
    public async Task<IActionResult> ScanArtifactFile(Guid ScanArtifactId)
    {
        DataObjects.ScanArtifact? artifact = await da.GetArtifactData(ScanArtifactId);
        if (artifact == null) { return NotFound(); }
        string contentType = artifact.ContentType ?? "application/octet-stream";

        if (!string.IsNullOrEmpty(artifact.Path) && System.IO.File.Exists(artifact.Path)) {
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(artifact.Path);
            return File(bytes, contentType, artifact.FileName);
        }
        if (artifact.Data != null && artifact.Data.Length > 0) {
            return File(artifact.Data, contentType, artifact.FileName);
        }
        return NotFound();
    }

    // ── Images ─────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetImages")]
    public async Task<ActionResult<List<DataObjects.ScanImage>>> GetImages([FromBody] Guid PageScanResultId)
    {
        List<DataObjects.ScanImage> output = await da.GetImages(PageScanResultId);
        return Ok(output);
    }

    // ── Certificate ────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetCertificate")]
    public async Task<ActionResult<DataObjects.ScanCertificate?>> GetCertificate([FromBody] Guid PageScanResultId)
    {
        DataObjects.ScanCertificate? output = await da.GetCertificate(PageScanResultId);
        return Ok(output);
    }

    // ── Ranked Rules ───────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetRankedRules")]
    public async Task<ActionResult<List<DataObjects.ScanRankedRule>>> GetRankedRules([FromBody] Guid PageScanResultId)
    {
        List<DataObjects.ScanRankedRule> output = await da.GetRankedRules(PageScanResultId);
        return Ok(output);
    }

    // ── Violations ─────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetViolations")]
    public async Task<ActionResult<List<DataObjects.A11yViolation>>> GetViolations(DataObjects.ViolationFilter filter)
    {
        List<DataObjects.A11yViolation> output = await da.GetViolations(filter.Ids, filter.PageScanResultId);
        return Ok(output);
    }

    // ── Scan History ───────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetSiteScanHistory")]
    public async Task<ActionResult<List<DataObjects.ScanRun>>> GetSiteScanHistory(DataObjects.ScanHistoryFilter filter)
    {
        List<DataObjects.ScanRun> output = await da.GetSiteScanHistory(filter.SiteId, filter.Count);
        return Ok(output);
    }

    // ── Manual Checks ──────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetManualChecks")]
    public async Task<ActionResult<List<DataObjects.ManualCheckResult>>> GetManualChecks(DataObjects.SiteChildFilter filter)
    {
        List<DataObjects.ManualCheckResult> output = await da.GetManualChecks(filter.Ids, filter.SiteId);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveManualChecks")]
    public async Task<ActionResult<List<DataObjects.ManualCheckResult>>> SaveManualChecks(List<DataObjects.ManualCheckResult> Items)
    {
        List<DataObjects.ManualCheckResult> output = await da.SaveManualChecks(Items, CurrentUser);
        return Ok(output);
    }

    // ── Trigger Scan ───────────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/TriggerScan")]
    public async Task<ActionResult<DataObjects.ScanRun>> TriggerScan([FromBody] Guid SiteId)
    {
        DataObjects.ScanRun output = new DataObjects.ScanRun();
        output.ActionResponse = new DataObjects.BooleanResponse();

        // Validate site exists and is enabled
        List<DataObjects.Site> sites = await da.GetSites(new List<Guid> { SiteId }, TenantId, CurrentUser);

        if (sites == null || !sites.Any()) {
            output.ActionResponse.Messages.Add("Site not found");
            return Ok(output);
        }

        DataObjects.Site site = sites.First();

        if (!site.Enabled) {
            output.ActionResponse.Messages.Add("Site is not enabled");
            return Ok(output);
        }

        // Check no scan is already running
        List<DataObjects.ScanRun> existingRuns = await da.GetScanRuns(null, SiteId, TenantId);
        bool alreadyRunning = existingRuns.Any(x => x.Status == "Running" || x.Status == "Queued");

        if (alreadyRunning) {
            output.ActionResponse.Messages.Add("A scan is already running for this site");
            return Ok(output);
        }

        // Create new scan run with Queued status — the background service will
        // transition it to Running when it actually starts processing.
        DataObjects.ScanRun newRun = new DataObjects.ScanRun {
            SiteId = SiteId,
            TenantId = TenantId,
            Status = "Queued",
            TriggeredBy = CurrentUser != null ? CurrentUser.DisplayName : null,
            StartedAt = DateTime.UtcNow,
            PagesScanned = 0,
            TotalViolations = 0,
            CriticalCount = 0,
            SeriousCount = 0,
            ModerateCount = 0,
            MinorCount = 0,
        };

        output = await da.SaveScanRun(newRun, CurrentUser);

        // Fire-and-forget: queue scan execution on background service
        // The scanner service will pick this up and process it asynchronously.
        // Client tracks progress via SignalR updates.

        // Broadcast a ScanQueued event so clients can show the scan in their queued list.
        // The background service is woken via WakeSignal.Release below — not via SignalR.
        // IMPORTANT: do NOT send ScanProgress here; ScanProgress means "actively scanning"
        // and listeners use it to mark the site as Running in the UI.
        IHubContext<freeallycheckerHub, IsrHub> hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<freeallycheckerHub, IsrHub>>();
        DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
            TenantId = TenantId,
            ItemId = output.ScanRunId,
            UpdateType = DataObjects.SignalRUpdateType.ScanQueued,
            Message = "ScanQueued",
            Object = output,
        };
        await hubContext.Clients.Group(TenantId.ToString()).SignalRUpdate(update);

        // Wake the BackgroundService polling loop immediately.
        try { ScannerBackgroundService.WakeSignal.Release(); } catch (SemaphoreFullException) { }

        return Ok(output);
    }

    // ── Violations by Rule ─────────────────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetViolationsByRule")]
    public async Task<ActionResult<List<DataObjects.A11yViolation>>> GetViolationsByRule(DataObjects.ViolationsByRuleFilter filter)
    {
        List<DataObjects.A11yViolation> output = await da.GetViolationsByRule(filter.ScanRunId, filter.CanonicalRuleId);
        return Ok(output);
    }

    // ── Cross-Site Violations (compliance triage across sites) ─────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetCrossSiteViolations")]
    public async Task<ActionResult<List<DataObjects.CrossSiteViolation>>> GetCrossSiteViolations(DataObjects.CrossSiteViolationFilter filter)
    {
        List<DataObjects.CrossSiteViolation> output = await da.GetCrossSiteViolations(TenantId, filter);
        return Ok(output);
    }

    // ── Violation Suppressions (downgrade specific rules to warning) ─

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetViolationSuppressions")]
    public async Task<ActionResult<List<DataObjects.ViolationSuppression>>> GetViolationSuppressions()
    {
        List<DataObjects.ViolationSuppression> output = await da.GetViolationSuppressions(TenantId);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/SaveViolationSuppression")]
    public async Task<ActionResult<DataObjects.ViolationSuppression>> SaveViolationSuppression(DataObjects.ViolationSuppression Item)
    {
        Item.TenantId = TenantId; // Always scope to current tenant.
        DataObjects.ViolationSuppression output = await da.SaveViolationSuppression(Item, CurrentUser);
        return Ok(output);
    }

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteViolationSuppression")]
    public async Task<ActionResult<bool>> DeleteViolationSuppression([FromBody] Guid ViolationSuppressionId)
    {
        bool output = await da.DeleteViolationSuppression(ViolationSuppressionId, TenantId);
        return Ok(output);
    }

    // ── All Sites Scan History (Trends) ────────────────────────────

    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/GetAllSiteScanHistory")]
    public async Task<ActionResult<Dictionary<Guid, List<DataObjects.ScanRun>>>> GetAllSiteScanHistory(DataObjects.AllSiteScanHistoryFilter filter)
    {
        Dictionary<Guid, List<DataObjects.ScanRun>> output = await da.GetAllSiteScanHistory(TenantId, filter.CountPerSite);
        return Ok(output);
    }
}
