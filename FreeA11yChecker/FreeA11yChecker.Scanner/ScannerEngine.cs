using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Core scanning orchestrator. Provides three entry points:
/// ScanAll (all sites), ScanSite (one site), ScanPage (one page).
/// Each page goes through a 23-step pipeline: navigate, expand, capture,
/// run 4 engines, merge with consensus, generate overlay screenshots,
/// CVD simulations, screen reader view, and save results.
/// </summary>
public static class ScannerEngine
{
    // ================================================================
    // ScanAll — Top-level entry point for scanning all configured sites
    // ================================================================

    /// <summary>
    /// Scans all sites defined in the configuration. Launches a browser,
    /// iterates each site, and returns a combined result.
    /// </summary>
    public static async Task<RunScanResult> ScanAll(ScanConfig Config, Action<ScanProgress>? OnProgress = null,
        Action<string, PageScanResult>? OnPageComplete = null)
    {
        RunScanResult output = new RunScanResult();
        output.StartedAt = DateTime.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();

        // First, make sure the output directory exists.
        Directory.CreateDirectory(Config.OutputDir);

        // Launch browser — use installed Edge to avoid WAF bot detection.
        IPlaywright playwright = await Playwright.CreateAsync();
        IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = Config.Headless,
            Channel = "msedge",
            Args = new[] { "--disable-blink-features=AutomationControlled" },
        });

        try {
            // Scan each configured site.
            foreach (KeyValuePair<string, SiteConfig> siteEntry in Config.Sites) {
                SiteConfig siteConfig = siteEntry.Value;
                string baseUrl = (!string.IsNullOrEmpty(siteConfig.BaseUrl) ? siteConfig.BaseUrl : siteEntry.Key).TrimEnd('/');
                siteConfig.BaseUrl = baseUrl;

                string siteDir = Path.Combine(Config.OutputDir, SanitizePath(new Uri(baseUrl).Host));
                Directory.CreateDirectory(siteDir);

                // Forward OnPageComplete to ScanSite with the host pre-bound so callers
                // get (host, page) pairs without having to re-parse the URL each time.
                string hostForCallback = "unknown";
                try { hostForCallback = new Uri(baseUrl).Host; } catch { }
                Action<PageScanResult>? perPage = OnPageComplete == null ? null
                    : (PageScanResult p) => OnPageComplete(hostForCallback, p);
                SiteScanResult siteResult = await ScanSite(browser, siteConfig, Config, siteDir, OnProgress, perPage);
                output.Sites[baseUrl] = siteResult;
            }
        } finally {
            await browser.CloseAsync();
            playwright.Dispose();
        }

        stopwatch.Stop();
        output.TotalDurationMs = (int)stopwatch.ElapsedMilliseconds;
        output.CompletedAt = DateTime.UtcNow;

        return output;
    }

    // ================================================================
    // ScanSite — Scan all pages within a single site
    // ================================================================

    /// <summary>
    /// Scans all pages in a site. Creates a browser context, handles authentication
    /// if credentials are configured, then scans each page sequentially.
    /// </summary>
    public static async Task<SiteScanResult> ScanSite(IBrowser Browser, SiteConfig Site, ScanConfig Config,
        string OutputDir, Action<ScanProgress>? OnProgress = null,
        Action<PageScanResult>? OnPageComplete = null)
    {
        SiteScanResult output = new SiteScanResult();
        output.BaseUrl = Site.BaseUrl;
        Stopwatch stopwatch = Stopwatch.StartNew();

        // STORAGE-STATE REUSE — the canonical fix for "iter 1 auth works, iter 3 auth
        // fails and breaks the rest of the crawl". Save Playwright's storage state
        // (cookies + localStorage) to disk after a successful auth. On subsequent
        // crawl iterations, load that file at context-creation time and SKIP the auth
        // flow entirely — the cookies are already there. Auth is run once per crawl
        // (or once per cold cache), not once per iteration.
        //
        // Lives in runs/.cookies/ (NOT runs/latest/) so the fresh-cli task that wipes
        // runs/latest does NOT delete the auth state. Cookies are scoped per host.
        string siteHostForCookie = "unknown";
        try { siteHostForCookie = new Uri(Site.BaseUrl).Host; } catch { }
        string cookiesDir = Path.Combine(Directory.GetParent(OutputDir)?.FullName ?? OutputDir, ".cookies");
        Directory.CreateDirectory(cookiesDir);
        string storageStatePath = Path.Combine(cookiesDir, $"{siteHostForCookie}.json");
        bool reusingStorageState = File.Exists(storageStatePath);

        var contextOptions = new BrowserNewContextOptions {
            IgnoreHTTPSErrors = true,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "en-US",
            TimezoneId = "America/Los_Angeles",
            ExtraHTTPHeaders = new Dictionary<string, string> {
                ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8",
                ["Accept-Language"] = "en-US,en;q=0.9",
                ["Sec-Ch-Ua"] = "\"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not.A/Brand\";v=\"99\"",
                ["Sec-Ch-Ua-Mobile"] = "?0",
                ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
                ["Upgrade-Insecure-Requests"] = "1",
            },
        };
        if (reusingStorageState) {
            contextOptions.StorageStatePath = storageStatePath;
            OnProgress?.Invoke(new ScanProgress {
                CurrentSite = Site.BaseUrl,
                Message = $"Reusing saved auth from {storageStatePath} — skipping login flow",
            });
        }

        IBrowserContext context = await Browser.NewContextAsync(contextOptions);

        // Mask automation signals so sites don't detect Playwright as a bot.
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            window.chrome = { runtime: {} };
        ");

        // Auth-flow screenshots captured during login — prepended to the first page
        // scanned for this site so they persist through the existing PageScanResult
        // pipeline (web app → EF, console → disk). Three shots prove the
        // "anonymous locked out / authenticated full access" compliance pair.
        List<ScreenshotInfo> authScreenshots = new List<ScreenshotInfo>();

        try {
            // See if we need to authenticate. Skip entirely if we loaded a saved
            // storage state — the cookies are already on the context.
            if (reusingStorageState) {
                output.AuthAttempted = true;
                output.AuthSucceeded = true; // assume reused state is valid
            } else if (Site.Credentials != null && !String.IsNullOrWhiteSpace(Site.Credentials.Username)) {
                output.AuthAttempted = true;

                // Inject the site BaseUrl so AuthHandler can build correct login URLs for
                // virtual-app deployments (e.g. host/Touchpoints) where stripping to scheme+host
                // would lose the sub-path.
                if (String.IsNullOrWhiteSpace(Site.Credentials.BaseUrl)) {
                    Site.Credentials.BaseUrl = Site.BaseUrl;
                }

                OnProgress?.Invoke(new ScanProgress {
                    CurrentSite = Site.BaseUrl,
                    Message = "Authenticating as '" + Site.Credentials.Username + "'...",
                });

                IPage authPage = await context.NewPageAsync();
                try {
                    // Forward each auth step as a ScanProgress event so the CLI / web UI
                    // sees realtime per-step messages instead of a single "Authenticating..."
                    // log followed by 30+ seconds of silence.
                    Action<string> emitAuthStep = msg => OnProgress?.Invoke(new ScanProgress {
                        CurrentSite = Site.BaseUrl,
                        Message = msg,
                    });
                    AuthHandler.AuthScreenshotResult authResult =
                        await AuthHandler.AuthenticateWithScreenshots(authPage, Site.Credentials, emitAuthStep);
                    output.AuthSucceeded = authResult.Succeeded;

                    // Build ScreenshotInfo entries for each captured frame.
                    if (authResult.AnonymousFormBytes != null) {
                        authScreenshots.Add(new ScreenshotInfo {
                            Path = "00a-login-anonymous.jpeg",
                            Label = "login-anonymous",
                            SizeBytes = authResult.AnonymousFormBytes.Length,
                            Data = authResult.AnonymousFormBytes,
                            ContentType = "image/jpeg",
                        });
                    }
                    if (authResult.CredentialsEnteredBytes != null) {
                        authScreenshots.Add(new ScreenshotInfo {
                            Path = "00b-login-credentials-entered.jpeg",
                            Label = "login-credentials-entered",
                            SizeBytes = authResult.CredentialsEnteredBytes.Length,
                            Data = authResult.CredentialsEnteredBytes,
                            ContentType = "image/jpeg",
                        });
                    }
                    if (authResult.PostAuthBytes != null) {
                        string postAuthName = authResult.PostAuthIsFailure
                            ? "00c-login-FAILED.jpeg"
                            : "00c-login-post-auth.jpeg";
                        string postAuthLabel = authResult.PostAuthIsFailure
                            ? "login-FAILED"
                            : "login-post-auth";
                        authScreenshots.Add(new ScreenshotInfo {
                            Path = postAuthName,
                            Label = postAuthLabel,
                            SizeBytes = authResult.PostAuthBytes.Length,
                            Data = authResult.PostAuthBytes,
                            ContentType = "image/jpeg",
                        });
                    }
                } finally {
                    await authPage.CloseAsync();
                }

                // SAVE STORAGE STATE if auth succeeded — subsequent crawl iterations
                // (same OutputDir / same site) will load this and skip the auth flow.
                // Mountain of complexity removed: no more re-running the 3-step Flex login
                // 5 times in a 5-iteration crawl, no more iter-3-flakes losing 25% of pages.
                if (output.AuthSucceeded) {
                    try {
                        await context.StorageStateAsync(new BrowserContextStorageStateOptions {
                            Path = storageStatePath,
                        });
                        OnProgress?.Invoke(new ScanProgress {
                            CurrentSite = Site.BaseUrl,
                            Message = $"Saved auth cookies → {storageStatePath} (will be reused on next iteration)",
                        });
                    } catch (Exception ex) {
                        OnProgress?.Invoke(new ScanProgress {
                            CurrentSite = Site.BaseUrl,
                            Message = $"Failed to save storage state: {ex.Message} — next iteration will re-auth",
                        });
                    }
                }
            }

            // Now scan each page.
            int pageIndex = 0;
            int totalPages = Site.Pages.Count;

            foreach (PageConfig pageConfig in Site.Pages) {
                pageIndex++;
                // Build full URL — handle BaseUrl that may already contain a path prefix.
                // Discovered paths are absolute from host root (e.g. "/em411/faq/"),
                // so if BaseUrl already includes that prefix, build from origin instead.
                string baseUrlTrimmed = Site.BaseUrl.TrimEnd('/');
                Uri baseUri = new Uri(baseUrlTrimmed);
                string basePath = baseUri.AbsolutePath.TrimEnd('/');
                string pagePath = pageConfig.Path.StartsWith("/") ? pageConfig.Path : "/" + pageConfig.Path;

                string fullUrl;
                if (!string.IsNullOrEmpty(basePath) && basePath != "/" &&
                    pagePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
                    fullUrl = baseUri.GetLeftPart(UriPartial.Authority) + pagePath;
                } else {
                    fullUrl = baseUrlTrimmed + pagePath;
                }

                string pageDir = Path.Combine(OutputDir, SanitizePath(pageConfig.Path));
                Directory.CreateDirectory(pageDir);

                OnProgress?.Invoke(new ScanProgress {
                    CurrentPage = pageIndex,
                    TotalPages = totalPages,
                    CurrentSite = Site.BaseUrl,
                    Message = "Scanning " + pageConfig.Path + "...",
                });

                // Open a new page for this scan.
                IPage page = await context.NewPageAsync();
                try {
                    // Forward per-step progress for this page through the existing
                    // OnProgress channel so users see live per-step status in the CLI.
                    int capturedPageIndex = pageIndex;
                    int capturedTotalPages = totalPages;
                    Action<string> emitPageStep = msg => OnProgress?.Invoke(new ScanProgress {
                        CurrentPage = capturedPageIndex,
                        TotalPages = capturedTotalPages,
                        CurrentSite = Site.BaseUrl,
                        Message = "  " + msg, // indent so step lines visually differ from page header
                    });
                    PageScanResult pageResult = await ScanPage(page, Config, fullUrl, pageDir, emitPageStep);

                    // Resiliency: if the first attempt produced an error AND zero issues across
                    // all four tools, the page likely failed to load. Retry once with a fresh page.
                    int firstIssueCount = (pageResult.AxeIssues?.Count ?? 0)
                        + (pageResult.IbmIssues?.Count ?? 0)
                        + (pageResult.HtmlCsIssues?.Count ?? 0)
                        + (pageResult.HtmlCheckIssues?.Count ?? 0);
                    if (!string.IsNullOrEmpty(pageResult.ErrorMessage) && firstIssueCount == 0) {
                        emitPageStep("  → first attempt failed, retrying once after 2s...");
                        await Task.Delay(2000);
                        try { await page.CloseAsync(); } catch { }
                        page = await context.NewPageAsync();
                        PageScanResult retryResult = await ScanPage(page, Config, fullUrl, pageDir, emitPageStep);
                        int retryIssueCount = (retryResult.AxeIssues?.Count ?? 0)
                            + (retryResult.IbmIssues?.Count ?? 0)
                            + (retryResult.HtmlCsIssues?.Count ?? 0)
                            + (retryResult.HtmlCheckIssues?.Count ?? 0);
                        if (retryIssueCount > 0 || string.IsNullOrEmpty(retryResult.ErrorMessage)) {
                            pageResult = retryResult;
                        }
                    }

                    // Prepend any auth-flow screenshots to the FIRST page scanned, so
                    // they appear before the per-page 01-page-loaded.jpeg and downstream
                    // overlays. Subsequent pages do not re-attach them.
                    if (authScreenshots.Count > 0) {
                        pageResult.Screenshots.InsertRange(0, authScreenshots);
                        authScreenshots.Clear();
                    }

                    output.Pages.Add(pageResult);

                    // Fire the per-page callback IMMEDIATELY so callers (CLI / web app)
                    // can persist artifacts to disk in real time. Without this, all
                    // pages in an iteration sit in memory until the iteration ends —
                    // a Ctrl+C mid-crawl loses everything since the last write.
                    try { OnPageComplete?.Invoke(pageResult); } catch { /* never crash scan on persistence error */ }
                } finally {
                    try { await page.CloseAsync(); } catch { }
                }
            }

            // Edge-case fix (validator-flagged): if the site had zero configured pages but auth
            // screenshots were captured, attach them to a synthesized PageScanResult so the
            // compliance-evidence pair (anonymous / credentials-entered / post-auth) is not lost.
            if (authScreenshots.Count > 0 && output.Pages.Count == 0) {
                output.Pages.Add(new PageScanResult {
                    Url = Site.BaseUrl,
                    Screenshots = new List<ScreenshotInfo>(authScreenshots),
                });
                authScreenshots.Clear();
            }

            // Cross-page consistency analysis — runs after all pages are scanned.
            if (output.Pages.Count >= 2) {
                try {
                    output.CrossPageConsistencyJson = CrossPageConsistency.Compare(output.Pages);
                } catch { }
            }
        } finally {
            await context.CloseAsync();
        }

        stopwatch.Stop();
        output.DurationMs = (int)stopwatch.ElapsedMilliseconds;

        return output;
    }

    // ================================================================
    // ScanPage — The 23-step pipeline for a single page
    // ================================================================

    /// <summary>
    /// Scans a single page through the full pipeline: navigate, expand, capture HTML,
    /// run 4 accessibility engines, merge results, generate overlay screenshots,
    /// CVD simulations, screen reader view — all in-memory, zero disk writes.
    /// </summary>
    public static async Task<PageScanResult> ScanPage(IPage Page, ScanConfig Config, string Url, string OutputDir,
        Action<string>? OnStep = null)
    {
        PageScanResult output = new PageScanResult();
        output.Url = Url;
        output.OutputDir = OutputDir;
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Artifacts built in-memory (JSON, HTML, markdown, logs).
        List<(string FileName, string ContentType, byte[] Data)> memoryArtifacts = new();

        try {
            string cacheDir = Path.Combine(AppContext.BaseDirectory, "cache");
            Directory.CreateDirectory(cacheDir);

            // Step 1: Navigate to page and wait for it to settle.
            // Use Load (window.load fired) instead of NetworkIdle. NetworkIdle requires 500ms
            // of zero network activity which never happens on chatty marketing/CMS sites with
            // analytics beacons (em.wsu.edu, sitecore-style pages) — they timeout cleanly.
            // For Blazor WASM, WaitForHydrationAsync below STILL does NetworkIdle as a
            // best-effort wait (with its own timeout + try/catch) so WASM bundles still get
            // a chance to settle before screenshots fire. Best of both: Load unblocks goto,
            // hydration step handles the SPA case.
            OnStep?.Invoke($"navigating to {Url} (Load, up to {Config.TimeoutMs}ms)");
            IResponse? response = await Page.GotoAsync(Url, new PageGotoOptions {
                WaitUntil = WaitUntilState.Load,
                Timeout = Config.TimeoutMs,
            });
            output.StatusCode = response?.Status ?? 0;

            // Capture HTTP response headers.
            if (response != null) {
                try {
                    var headers = await response.AllHeadersAsync();
                    foreach (var h in headers) {
                        output.ResponseHeaders[h.Key] = h.Value;
                    }
                } catch { }
            }

            // Wait for the page to fully hydrate (Blazor WASM, SPA frameworks, etc.)
            // before any screenshot or DOM-dependent extraction runs. Without this,
            // captures fire against the static App.razor shell showing "Loading..." text.
            OnStep?.Invoke("waiting for page hydration (Blazor / SPA settle)");
            await WaitForHydrationAsync(Page, Config);

            OnStep?.Invoke("extracting links for discovery");
            // Extract all links from the page for discovery.
            try {
                var links = await Page.EvaluateAsync<string[]>(@"() => {
                    return Array.from(document.querySelectorAll('a[href]'))
                        .map(a => a.href)
                        .filter(href => href && href.startsWith('http'));
                }");
                Uri baseUri = new Uri(Url);
                foreach (string link in links ?? Array.Empty<string>()) {
                    try {
                        Uri linkUri = new Uri(link);
                        if (linkUri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase)) {
                            string path = linkUri.AbsolutePath;
                            if (!output.DiscoveredPaths.Contains(path)) {
                                output.DiscoveredPaths.Add(path);
                            }
                        } else {
                            string origin = linkUri.GetLeftPart(UriPartial.Authority);
                            if (!output.DiscoveredExternalUrls.Contains(origin)) {
                                output.DiscoveredExternalUrls.Add(origin);
                            }
                        }
                    } catch { }
                }
            } catch { }

            // Step 2: Apply font override so all subsequent screenshots (load sequence +
            // analysis overlays + CVD) share consistent typography for visual comparison.
            try {
                await Page.EvaluateAsync(@"() => {
                    if (document.head) {
                        const style = document.createElement('style');
                        style.textContent = '* { font-family: Arial, sans-serif !important; }';
                        style.id = 'a11y-font-override';
                        document.head.appendChild(style);
                    }
                }");
            } catch { }

            // Step 2a: Capture page-load sequence — 10s @ 500ms PNG, dedup by SHA-256.
            // PNG is deterministic so identical frames collapse to one. Output is one file
            // per UNIQUE frame, named with elapsed-ms so they sort chronologically (e.g.
            // 01-page-load-00000ms.png, 01-page-load-00500ms.png, ...). Replaces the old
            // single page-loaded.jpeg — the last frame IS the loaded page.
            OnStep?.Invoke("capturing page-load sequence (10s of PNG screenshots)");
            try {
                List<ScreenshotInfo> loadSequence = await CaptureLoadSequenceAsync(
                    Page, "01-page-load", "Page Load", durationMs: 10000, intervalMs: 500);
                foreach (ScreenshotInfo frame in loadSequence) {
                    output.Screenshots.Add(frame);
                }
                OnStep?.Invoke($"  → captured {loadSequence.Count} unique frame(s) after dedup");
            } catch { }

            // Step 3: Expand collapsed content (details, accordions, tabs).
            OnStep?.Invoke("expanding collapsed content (details, accordions, tabs)");
            bool contentChanged = false;
            try { contentChanged = await ContentExpander.Expand(Page); } catch { }

            // Step 4: Screenshot — page expanded (only if content actually changed, in-memory).
            if (contentChanged) {
                try {
                    byte[] expandedData = await Page.ScreenshotAsync(new PageScreenshotOptions {
                        FullPage = true,
                        Type = ScreenshotType.Jpeg,
                        Quality = 90,
                    });
                    output.Screenshots.Add(new ScreenshotInfo {
                        Path = "02-page-expanded.jpeg",
                        Label = "page-expanded",
                        SizeBytes = expandedData.Length,
                        Data = expandedData,
                        ContentType = "image/jpeg",
                    });
                } catch { }
            }

            // Step 5: Capture HTML + lang attribute.
            string html = await Page.ContentAsync();
            output.PageTitle = await Page.TitleAsync();
            output.HtmlSizeBytes = System.Text.Encoding.UTF8.GetByteCount(html);
            memoryArtifacts.Add(("page.html", "text/html", System.Text.Encoding.UTF8.GetBytes(html)));

            // Capture html lang attribute.
            try {
                output.Language = await Page.EvaluateAsync<string>("() => document.documentElement.lang || ''") ?? "";
            } catch { }

            // Step 5b: Capture heading tree.
            try {
                output.HeadingsJson = await Page.EvaluateAsync<string>(@"() => {
                    const headings = [];
                    document.querySelectorAll('h1,h2,h3,h4,h5,h6').forEach(h => {
                        headings.push({ level: parseInt(h.tagName[1]), text: h.textContent.trim().substring(0,200), id: h.id || '' });
                    });
                    return JSON.stringify(headings);
                }") ?? "[]";
            } catch { output.HeadingsJson = "[]"; }

            // Step 5c: Capture landmark inventory.
            try {
                output.LandmarksJson = await Page.EvaluateAsync<string>(@"() => {
                    const landmarks = [];
                    const roles = ['banner','navigation','main','complementary','contentinfo','search','form','region'];
                    // Explicit role attributes.
                    document.querySelectorAll('[role]').forEach(el => {
                        if (roles.includes(el.getAttribute('role')))
                            landmarks.push({ tag: el.tagName.toLowerCase(), role: el.getAttribute('role'), label: el.getAttribute('aria-label') || '' });
                    });
                    // Implicit landmark elements.
                    const implicitMap = { HEADER:'banner', NAV:'navigation', MAIN:'main', ASIDE:'complementary', FOOTER:'contentinfo' };
                    for (const [tag, role] of Object.entries(implicitMap)) {
                        document.querySelectorAll(tag).forEach(el => {
                            if (!el.getAttribute('role'))
                                landmarks.push({ tag: tag.toLowerCase(), role: role, label: el.getAttribute('aria-label') || '' });
                        });
                    }
                    return JSON.stringify(landmarks);
                }") ?? "[]";
            } catch { output.LandmarksJson = "[]"; }

            // Step 5d: Capture media elements.
            try {
                output.MediaJson = await Page.EvaluateAsync<string>(@"() => {
                    const media = [];
                    document.querySelectorAll('video, audio').forEach(el => {
                        const tracks = [];
                        el.querySelectorAll('track').forEach(t => {
                            tracks.push({ kind: t.kind || '', srclang: t.srclang || '', label: t.label || '' });
                        });
                        media.push({
                            tag: el.tagName.toLowerCase(),
                            src: el.src || el.querySelector('source')?.src || '',
                            hasControls: el.hasAttribute('controls'),
                            hasAutoplay: el.hasAttribute('autoplay'),
                            tracks: tracks
                        });
                    });
                    return JSON.stringify(media);
                }") ?? "[]";
            } catch { output.MediaJson = "[]"; }

            // Step 5e: Capture ARIA live regions.
            try {
                output.AriaLiveRegionsJson = await Page.EvaluateAsync<string>(@"() => {
                    const regions = [];
                    document.querySelectorAll('[aria-live], [role=""alert""], [role=""status""], [role=""log""], [role=""marquee""], [role=""timer""]').forEach(el => {
                        regions.push({
                            tag: el.tagName.toLowerCase(),
                            role: el.getAttribute('role') || '',
                            ariaLive: el.getAttribute('aria-live') || '',
                            text: el.textContent.trim().substring(0,200)
                        });
                    });
                    return JSON.stringify(regions);
                }") ?? "[]";
            } catch { output.AriaLiveRegionsJson = "[]"; }

            // Step 5f: Target size measurement (WCAG 2.2 SC 2.5.8 — minimum 24×24 AA).
            try {
                output.TargetSizeJson = await Page.EvaluateAsync<string>(@"() => {
                    const issues = [];
                    const interactive = 'a[href], button, input, select, textarea, [role=""button""], [role=""link""], [role=""checkbox""], [role=""radio""], [role=""tab""], [tabindex]';
                    document.querySelectorAll(interactive).forEach(el => {
                        const rect = el.getBoundingClientRect();
                        if (rect.width > 0 && rect.height > 0 && (rect.width < 24 || rect.height < 24)) {
                            issues.push({
                                selector: el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') + (el.className ? '.' + el.className.split(' ')[0] : ''),
                                tag: el.tagName.toLowerCase(),
                                width: Math.round(rect.width * 10) / 10,
                                height: Math.round(rect.height * 10) / 10,
                                text: (el.textContent || el.value || el.getAttribute('aria-label') || '').trim().substring(0,100)
                            });
                        }
                    });
                    return JSON.stringify(issues);
                }") ?? "[]";
            } catch { output.TargetSizeJson = "[]"; }

            // Step 5g: Performance timing.
            try {
                output.PerformanceJson = await Page.EvaluateAsync<string>(@"() => {
                    const t = performance.timing;
                    const nav = t.navigationStart;
                    const paint = {};
                    performance.getEntriesByType('paint').forEach(p => { paint[p.name] = Math.round(p.startTime); });
                    return JSON.stringify({
                        ttfb: t.responseStart - nav,
                        domContentLoaded: t.domContentLoadedEventEnd - nav,
                        loadComplete: t.loadEventEnd - nav,
                        firstPaint: paint['first-paint'] || null,
                        firstContentfulPaint: paint['first-contentful-paint'] || null
                    });
                }") ?? "{}";
            } catch { output.PerformanceJson = "{}"; }

            // Step 5h: Playwright accessibility tree snapshot.
            try {
                var snapshot = await Page.Accessibility.SnapshotAsync();
                if (snapshot != null) {
                    output.AccessibilityTreeJson = JsonSerializer.Serialize(snapshot);
                }
            } catch { }

            // Step 6: Catalog images.
            OnStep?.Invoke("cataloging images (alt-text audit)");
            output.Images = await CatalogImages(Page);

            // Step 6b: SSL certificate analysis.
            OnStep?.Invoke("analyzing SSL certificate");
            try {
                output.Certificate = CertAnalyzer.AnalyzeCert(Url);
            } catch { }

            // Step 7: Run axe-core (full — captures contrast data + pass/incomplete/inapplicable counts).
            OnStep?.Invoke("running axe-core (Deque)");
            var axeResult = await AxeCoreRunner.RunFull(Page, cacheDir);
            output.AxeIssues = axeResult.Issues;
            output.AxePassCount = axeResult.PassCount;
            output.AxeIncompleteCount = axeResult.IncompleteCount;
            output.AxeInapplicableCount = axeResult.InapplicableCount;
            OnStep?.Invoke($"  → axe: {axeResult.Issues.Count} issue(s), {axeResult.PassCount} passed, {axeResult.IncompleteCount} incomplete");

            // Step 8: Run HTML regex checker (with pass-rate stats).
            OnStep?.Invoke("running HtmlChecker (regex pattern audit)");
            var htmlCheckResult = HtmlChecker.CheckWithStats(html);
            output.HtmlCheckIssues = htmlCheckResult.Issues;
            output.HtmlCheckTotalChecks = htmlCheckResult.TotalChecks;
            output.HtmlCheckPassCount = htmlCheckResult.PassCount;
            OnStep?.Invoke($"  → HtmlChecker: {htmlCheckResult.Issues.Count} issue(s), {htmlCheckResult.PassCount}/{htmlCheckResult.TotalChecks} checks passed");

            // Step 9: Run HTML_CodeSniffer (with pass/fail breakdown stats).
            OnStep?.Invoke("running HTML_CodeSniffer (Squiz)");
            var htmlCsResult = await HtmlCodeSnifferRunner.RunWithStats(Page, cacheDir);
            output.HtmlCsIssues = htmlCsResult.Issues;
            output.HtmlCsErrorCount = htmlCsResult.ErrorCount;
            output.HtmlCsWarningCount = htmlCsResult.WarningCount;
            output.HtmlCsNoticeCount = htmlCsResult.NoticeCount;
            output.HtmlCsTotalChecks = htmlCsResult.TotalRulesInStandard;
            output.HtmlCsPassCount = htmlCsResult.PassCount;
            OnStep?.Invoke($"  → HtmlCS: {htmlCsResult.Issues.Count} issue(s) ({htmlCsResult.ErrorCount} err / {htmlCsResult.WarningCount} warn / {htmlCsResult.NoticeCount} notice), {htmlCsResult.PassCount}/{htmlCsResult.TotalRulesInStandard} rules clean");

            // Step 10: Run IBM Equal Access — try/catch, skip gracefully if unavailable.
            OnStep?.Invoke("running IBM Equal Access");
            try {
                var ibmResult = await IbmEqualAccessRunner.Run(Page, cacheDir);
                output.IbmIssues = ibmResult.Issues;
                output.IbmPassCount = ibmResult.PassCount;
                output.IbmPotentialCount = ibmResult.PotentialCount;
                output.IbmManualCount = ibmResult.ManualCount;
                OnStep?.Invoke($"  → IBM: {ibmResult.Issues.Count} issue(s), {ibmResult.PassCount} passed, {ibmResult.PotentialCount} potential, {ibmResult.ManualCount} manual");
            } catch (Exception) {
                output.IbmIssues = new List<A11yIssue>();
                OnStep?.Invoke("  → IBM: skipped (CDN unavailable or load error)");
            }

            // Step 11: Merge all issues with cross-tool consensus scoring.
            List<A11yIssue> allIssues = new List<A11yIssue>();
            allIssues.AddRange(output.AxeIssues);
            allIssues.AddRange(output.HtmlCheckIssues);
            allIssues.AddRange(output.HtmlCsIssues);
            allIssues.AddRange(output.IbmIssues);
            output.Summary = ConsensusScorer.Merge(output.AxeIssues, output.HtmlCheckIssues, output.HtmlCsIssues, output.IbmIssues);

            // Steps 12-16: Overlay screenshots (in-memory).
            OnStep?.Invoke("capturing analysis overlays (axe / quickpeek / htmlcs / ibm / structure)");
            await CaptureOverlayScreenshotInMemory(Page, "03-axe-overlay", "axe-overlay",
                () => AxeCoreRunner.InjectOverlay(Page, output.AxeIssues),
                () => AxeCoreRunner.RemoveOverlay(Page), output);
            await CaptureOverlayScreenshotInMemory(Page, "04-quickpeek-overlay", "quickpeek-overlay",
                () => QuickPeekOverlay.InjectOverlay(Page),
                () => QuickPeekOverlay.RemoveOverlay(Page), output);
            await CaptureOverlayScreenshotInMemory(Page, "05-htmlcs-overlay", "htmlcs-overlay",
                () => HtmlCodeSnifferRunner.InjectOverlay(Page, output.HtmlCsIssues),
                () => HtmlCodeSnifferRunner.RemoveOverlay(Page), output);
            await CaptureOverlayScreenshotInMemory(Page, "06-ibm-overlay", "ibm-overlay",
                () => IbmEqualAccessRunner.InjectOverlay(Page, output.IbmIssues),
                () => IbmEqualAccessRunner.RemoveOverlay(Page), output);
            await CaptureOverlayScreenshotInMemory(Page, "07-structure-overlay", "structure-overlay",
                () => StructureOverlay.InjectOverlay(Page),
                () => StructureOverlay.RemoveOverlay(Page), output);
            // Wireframe blueprint — exaggerates landmarks, color-codes by role,
            // shows headings as vivid bars, labels images. Designer-style rendering.
            await CaptureOverlayScreenshotInMemory(Page, "07b-wireframe-blueprint", "wireframe-blueprint",
                () => WireframeOverlay.InjectOverlay(Page),
                () => WireframeOverlay.RemoveOverlay(Page), output);

            // Steps 17-23: CVD simulation screenshots (7 types, in-memory).
            OnStep?.Invoke("capturing color-vision deficiency simulations (7 types)");
            string[] cvdTypes = new string[] {
                "protanopia", "deuteranopia", "tritanopia",
                "achromatopsia",
                "protanomaly", "deuteranomaly", "tritanomaly",
            };
            int cvdIndex = 8;
            foreach (string cvdType in cvdTypes) {
                try {
                    byte[]? cvdData = await CvdSimulator.SimulateAndScreenshotBytes(Page, cvdType);
                    if (cvdData != null) {
                        output.Screenshots.Add(new ScreenshotInfo {
                            Path = cvdIndex.ToString("D2") + "-cvd-" + cvdType + ".png",
                            Label = "cvd-" + cvdType,
                            SizeBytes = cvdData.Length,
                            Data = cvdData,
                            ContentType = "image/png",
                        });
                    }
                } catch { }
                cvdIndex++;
            }

            // Step 24: Screen reader view screenshot (in-memory).
            OnStep?.Invoke("capturing screen reader view");
            try {
                await ScreenReaderView.InjectView(Page);
                byte[] srData = await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
                output.Screenshots.Add(new ScreenshotInfo {
                    Path = "15-screenreader-view.png",
                    Label = "screenreader-view",
                    SizeBytes = srData.Length,
                    Data = srData,
                    ContentType = "image/png",
                });
                await ScreenReaderView.RemoveView(Page);
            } catch { }

            // Step 24b: prefers-reduced-motion screenshot.
            try {
                byte[]? rmData = await CvdSimulator.CaptureReducedMotionScreenshot(Page);
                if (rmData != null) {
                    output.Screenshots.Add(new ScreenshotInfo {
                        Path = "16-reduced-motion.png",
                        Label = "reduced-motion",
                        SizeBytes = rmData.Length,
                        Data = rmData,
                        ContentType = "image/png",
                    });
                }
            } catch { }

            // Step 24c: forced-colors (high contrast) screenshot.
            try {
                byte[]? fcData = await CvdSimulator.CaptureForcedColorsScreenshot(Page);
                if (fcData != null) {
                    output.Screenshots.Add(new ScreenshotInfo {
                        Path = "17-forced-colors.png",
                        Label = "forced-colors",
                        SizeBytes = fcData.Length,
                        Data = fcData,
                        ContentType = "image/png",
                    });
                }
            } catch { }

            // ================================================================
            // Phase 3 — Expensive / Complex Checks (opt-in via Config flags)
            // ================================================================

            // Phase 3a: Keyboard navigation simulation.
            if (Config.EnableKeyboardNav) {
                try {
                    // Reload page to clean state for keyboard testing.
                    await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = Config.TimeoutMs });
                    if (Config.SettleDelayMs > 0) await Page.WaitForTimeoutAsync(Config.SettleDelayMs);

                    var kbResult = await KeyboardNavSimulator.Run(Page, Config.KeyboardNavMaxTabs);
                    output.KeyboardNavJson = kbResult.FocusOrderJson;
                    output.FocusTrapsJson = kbResult.FocusTrapsJson;
                } catch { }
            }

            // Phase 3b: Text spacing override test (WCAG 1.4.12).
            if (Config.EnableTextSpacingTest) {
                try {
                    // Reload to clean state so spacing test starts from baseline.
                    await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load, Timeout = Config.TimeoutMs });
                    if (Config.SettleDelayMs > 0) await Page.WaitForTimeoutAsync(Config.SettleDelayMs);

                    output.TextSpacingJson = await TextSpacingTest.Run(Page);
                } catch { }
            }

            // Phase 3c: Reading level analysis (WCAG 3.1.5 AAA).
            if (Config.EnableReadingLevel) {
                try {
                    output.ReadingLevelJson = await ReadingLevelAnalyzer.Run(Page);
                } catch { }
            }

            // Phase 3d: Autocomplete attribute audit (WCAG 1.3.5).
            if (Config.EnableAutocompleteAudit) {
                try {
                    output.AutocompleteJson = await AutocompleteAuditor.Run(Page);
                } catch { }
            }

            // Phase 3e: Fixed/sticky element detection (WCAG 2.4.11).
            if (Config.EnableFixedElementCheck) {
                try {
                    output.FixedElementsJson = await FixedElementDetector.Run(Page);
                } catch { }
            }

            // Phase 3f: Mobile viewport scanning — must run last as it resizes and reloads.
            if (Config.EnableMobileViewports) {
                try {
                    output.MobileViewportsJson = await MobileViewportScanner.Run(Page, Config, output);
                } catch { }
            }

            // Step 25: Build JSON artifacts in memory.
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            memoryArtifacts.Add(("a11y-axe.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(output.AxeIssues, jsonOptions)));
            memoryArtifacts.Add(("a11y-htmlcheck.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(output.HtmlCheckIssues, jsonOptions)));
            memoryArtifacts.Add(("a11y-htmlcs.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(output.HtmlCsIssues, jsonOptions)));
            memoryArtifacts.Add(("a11y-ibm.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(output.IbmIssues, jsonOptions)));
            memoryArtifacts.Add(("a11y-summary.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(output.Summary, jsonOptions)));

            memoryArtifacts.Add(("metadata.json", "application/json",
                JsonSerializer.SerializeToUtf8Bytes(new {
                    Url = output.Url,
                    PageTitle = output.PageTitle,
                    StatusCode = output.StatusCode,
                    HtmlSizeBytes = output.HtmlSizeBytes,
                    Language = output.Language,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    ImageCount = output.Images.Count,
                    ScreenshotCount = output.Screenshots.Count,
                    AxePassCount = output.AxePassCount,
                    AxeIncompleteCount = output.AxeIncompleteCount,
                    AxeInapplicableCount = output.AxeInapplicableCount,
                    ScannedAt = DateTime.UtcNow,
                }, jsonOptions)));

            string report = ReportGenerator.GeneratePageReport(output);
            memoryArtifacts.Add(("report.md", "text/markdown", System.Text.Encoding.UTF8.GetBytes(report)));

        } catch (Exception ex) {
            output.ErrorMessage = ex.Message;
            try {
                memoryArtifacts.Add(("error.log", "text/plain", System.Text.Encoding.UTF8.GetBytes(ex.ToString())));
            } catch { }
        }

        // Store artifacts in output for the caller to persist.
        output.Artifacts = memoryArtifacts;

        // Step 26: Return the result.
        stopwatch.Stop();
        output.DurationMs = (int)stopwatch.ElapsedMilliseconds;

        return output;
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    /// <summary>
    /// Waits for the page to finish hydrating before screenshots or DOM extraction runs.
    /// Combines four signals: (1) Playwright NetworkIdle, (2) a poll of body innerText
    /// until "Loading..." / "Please wait" / empty body text is gone, (3) a selector-based
    /// wait for a hydrated layout element (nav/main/navbar-brand), and (4) a final settle
    /// delay. This replaces the old "navigate, sleep N ms, screenshot" pattern that captured
    /// the static App.razor shell on Blazor WASM pages. Safe to call on the post-auth page
    /// too so the "landed" screenshot is stable.
    /// </summary>
    /// <summary>
    /// Captures a sequence of JPEG screenshots over <paramref name="durationMs"/> at
    /// <paramref name="intervalMs"/> spacing, dedupes consecutive identical frames by
    /// SHA-256 hash, and returns one <see cref="ScreenshotInfo"/> per UNIQUE frame.
    /// File names are numbered by elapsed milliseconds (e.g., <c>{prefix}-0000ms.jpeg</c>,
    /// <c>{prefix}-1500ms.jpeg</c>) so they sort chronologically in the output dir.
    ///
    /// Use cases:
    ///   1. Diagnose Blazor WASM hydration timing — watch the page transition from
    ///      blank → loading-spinner → hydrated UI without guessing the right wait.
    ///   2. Prove a page eventually rendered (or didn't) for compliance evidence.
    ///   3. Capture animation / dynamic-content states that a single screenshot misses.
    ///
    /// Dedup is content-based (SHA-256 of JPEG bytes), so visually-identical frames
    /// captured during a static-page wait are collapsed to a single output frame.
    /// </summary>
    public static async Task<List<ScreenshotInfo>> CaptureLoadSequenceAsync(
        IPage Page, string FilenamePrefix, string LabelPrefix, int durationMs = 10000, int intervalMs = 500)
    {
        List<ScreenshotInfo> output = new List<ScreenshotInfo>();
        if (durationMs <= 0 || intervalMs <= 0) return output;

        Stopwatch sw = Stopwatch.StartNew();
        string? lastHash = null;

        while (sw.ElapsedMilliseconds < durationMs) {
            int elapsedMs = (int)sw.ElapsedMilliseconds;
            byte[]? frame = null;
            try {
                // PNG is lossless AND Playwright's PNG output is deterministic for the
                // same rendered content — SHA-256 dedup of PNG bytes correctly collapses
                // identical frames. JPEG re-encoding adds quantization noise per call so
                // hash dedup never matched (that's why earlier dedup looked broken).
                frame = await Page.ScreenshotAsync(new PageScreenshotOptions {
                    FullPage = false,
                    Timeout = 5000,
                    Type = ScreenshotType.Png,
                });
            } catch { /* skip this frame on error and continue */ }

            if (frame != null && frame.Length > 0) {
                string hash = Convert.ToHexString(SHA256.HashData(frame));
                if (hash != lastHash) {
                    string filename = $"{FilenamePrefix}-{elapsedMs:D5}ms.png";
                    string label = $"{LabelPrefix} +{elapsedMs}ms";
                    output.Add(new ScreenshotInfo {
                        Path = filename,
                        Label = label,
                        SizeBytes = frame.Length,
                        Data = frame,
                        ContentType = "image/png",
                    });
                    lastHash = hash;
                }
            }

            // Sleep until next interval boundary, but bail out if duration exceeded.
            int remaining = (int)(durationMs - sw.ElapsedMilliseconds);
            if (remaining <= 0) break;
            int delay = Math.Min(intervalMs, remaining);
            await Task.Delay(delay);
        }

        return output;
    }

    public static async Task WaitForHydrationAsync(IPage Page, ScanConfig Config)
    {
        // (1) NetworkIdle — lets framework bundles / WASM download finish.
        // 30s is the historical working value; the prior 10s cap caused blank screenshots
        // on cold WASM boot. Pattern matches Examples/FreeTools/BrowserSnapshot.
        try {
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions {
                Timeout = 30000,
            });
        } catch { /* page might already be idle or timing out — continue to content checks */ }

        // (2) Poll innerText until "Loading..." / "Please wait" placeholders are gone.
        // This is the critical Blazor WASM hydration check — without it, screenshots fire
        // while .login-page / App.razor shell is still showing the boot spinner. Up to 15s.
        const int maxHydrationWaitMs = 15000;
        const int pollIntervalMs = 500;
        Stopwatch hydrationSw = Stopwatch.StartNew();
        while (hydrationSw.ElapsedMilliseconds < maxHydrationWaitMs) {
            string bodyText = string.Empty;
            try {
                bodyText = await Page.EvaluateAsync<string>(
                    "() => document.body && document.body.innerText ? document.body.innerText.toLowerCase() : ''") ?? string.Empty;
            } catch { /* page might be navigating — treat as still loading */ }

            bool stillLoading = string.IsNullOrWhiteSpace(bodyText)
                || bodyText.Contains("loading...")
                || bodyText.Contains("loading…")
                || bodyText.Contains("please wait");
            if (!stillLoading) break;
            await Task.Delay(pollIntervalMs);
        }

        // (3) Selector confirmation — broad landmark list covers FreeA11yChecker Blazor, marketing
        // sites, and SPA apps without giving up after 5s if a specific selector is missing.
        try {
            await Page.WaitForSelectorAsync(".navbar-brand, nav.navbar, main, [data-blazor-ready], header, footer, .container, .container-fluid",
                new PageWaitForSelectorOptions {
                    State = WaitForSelectorState.Visible,
                    Timeout = 5000,
                });
        } catch { /* layout never rendered — continue with whatever we got */ }

        // (4) Belt-and-suspenders: wait for any visible Blazor loading indicator to vanish.
        try {
            await Page.WaitForSelectorAsync(".loading-progress, .blazor-loading-progress, .login-page",
                new PageWaitForSelectorOptions {
                    State = WaitForSelectorState.Hidden,
                    Timeout = 3000,
                });
        } catch { /* no loading indicator present, that's fine */ }

        // (5) Final settle — minimum 1s, or Config.SettleDelayMs if larger. Lets post-
        // hydration animations / reflows complete before the screenshot fires.
        await Page.WaitForTimeoutAsync(Math.Max(1000, Config.SettleDelayMs));
    }

    /// <summary>
    /// Captures an overlay screenshot in-memory by injecting a visual overlay, taking a screenshot,
    /// then removing the overlay. Best-effort — failures are logged but do not stop the scan.
    /// </summary>
    private static async Task CaptureOverlayScreenshotInMemory(IPage Page, string FileName, string Label,
        Func<Task> InjectOverlay, Func<Task> RemoveOverlay, PageScanResult Result)
    {
        try {
            await InjectOverlay();
            byte[] data = await Page.ScreenshotAsync(new PageScreenshotOptions {
                FullPage = true,
            });
            Result.Screenshots.Add(new ScreenshotInfo {
                Path = FileName + ".png",
                Label = Label,
                SizeBytes = data.Length,
                Data = data,
                ContentType = "image/png",
            });
        } catch (Exception) {
            // Overlay injection is best-effort.
        } finally {
            try { await RemoveOverlay(); } catch { }
        }
    }

    /// <summary>
    /// Legacy disk-based overlay screenshot (used by console runner).
    /// </summary>
    private static async Task CaptureOverlayScreenshot(IPage Page, string OutputDir,
        string FileName, string Label, Func<Task> InjectOverlay, Func<Task> RemoveOverlay, PageScanResult Result)
    {
        try {
            await InjectOverlay();
            string screenshotPath = Path.Combine(OutputDir, FileName + ".png");
            await Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = screenshotPath,
                FullPage = true,
            });
            Result.Screenshots.Add(CreateScreenshotInfo(screenshotPath, Label));
        } catch (Exception) {
            // Overlay injection is best-effort.
        } finally {
            // Always remove the overlay so the next screenshot starts clean.
            try { await RemoveOverlay(); } catch { }
        }
    }

    /// <summary>
    /// Captures a CVD simulation screenshot by applying an SVG filter, taking a screenshot,
    /// then removing the filter.
    /// </summary>
    private static async Task CaptureCvdScreenshot(IPage Page, string OutputDir,
        string FileName, string CvdType, PageScanResult Result)
    {
        try {
            string screenshotPath = Path.Combine(OutputDir, FileName + ".png");
            await CvdSimulator.SimulateAndScreenshot(Page, CvdType, screenshotPath);
            Result.Screenshots.Add(CreateScreenshotInfo(screenshotPath, "cvd-" + CvdType));
        } catch (Exception) {
            // CVD simulation is best-effort.
        }
    }

    /// <summary>
    /// Captures a screen reader view screenshot by rendering a text-only accessible view.
    /// </summary>
    private static async Task CaptureScreenReaderView(IPage Page, string OutputDir, PageScanResult Result)
    {
        try {
            await ScreenReaderView.InjectView(Page);
            string screenshotPath = Path.Combine(OutputDir, "15-screenreader-view.png");
            await Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = screenshotPath,
                FullPage = true,
            });
            Result.Screenshots.Add(CreateScreenshotInfo(screenshotPath, "screenreader-view"));
            await ScreenReaderView.RemoveView(Page);
        } catch (Exception) {
            // Screen reader view is best-effort.
        }
    }

    /// <summary>
    /// Catalogs all images on the page with their URL and alt text status.
    /// </summary>
    private static async Task<List<ImageInfo>> CatalogImages(IPage Page)
    {
        List<ImageInfo> output = new List<ImageInfo>();

        try {
            string imagesJson = await Page.EvaluateAsync<string>(@"() => {
                const images = [];
                document.querySelectorAll('img').forEach(img => {
                    images.push({
                        url: img.src || '',
                        altText: img.hasAttribute('alt') ? img.getAttribute('alt') : null,
                        hasAlt: img.hasAttribute('alt')
                    });
                });
                return JSON.stringify(images);
            }");

            using (JsonDocument doc = JsonDocument.Parse(imagesJson)) {
                foreach (JsonElement imgEl in doc.RootElement.EnumerateArray()) {
                    output.Add(new ImageInfo {
                        Url = imgEl.GetProperty("url").GetString() ?? string.Empty,
                        AltText = imgEl.TryGetProperty("altText", out JsonElement altEl) && altEl.ValueKind != JsonValueKind.Null
                            ? altEl.GetString() : null,
                        HasAlt = imgEl.GetProperty("hasAlt").GetBoolean(),
                    });
                }
            }
        } catch (Exception) {
            // Image cataloging is best-effort.
        }

        return output;
    }

    /// <summary>
    /// Creates a ScreenshotInfo from a file path and label. Reads file size if the file exists.
    /// </summary>
    private static ScreenshotInfo CreateScreenshotInfo(string FilePath, string Label)
    {
        long sizeBytes = 0;
        try {
            FileInfo fileInfo = new FileInfo(FilePath);
            if (fileInfo.Exists) {
                sizeBytes = fileInfo.Length;
            }
        } catch { }

        return new ScreenshotInfo {
            Path = FilePath,
            Label = Label,
            SizeBytes = sizeBytes,
        };
    }

    /// <summary>
    /// Converts a URL path or hostname to a safe directory name.
    /// Replaces special characters with underscores.
    /// </summary>
    private static string SanitizePath(string Input)
    {
        if (String.IsNullOrWhiteSpace(Input) || Input == "/") {
            return "_root";
        }

        string sanitized = Input.Trim('/').Replace("/", "_").Replace(":", "_").Replace("?", "_")
            .Replace("&", "_").Replace("=", "_").Replace(" ", "_");

        // Remove any remaining invalid path characters.
        foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        return sanitized;
    }
}
