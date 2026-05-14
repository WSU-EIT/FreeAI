// =============================================================================
// FreeBlazorExample.ShowcaseTool
// Single-command orchestrator: starts the web app, Playwright-screenshots every
// showcase page, captures animated loading GIFs, console logs, network timing,
// and about-section text — then writes a full gallery markdown report.
//
// Usage:
//   dotnet run --project FreeBlazorExample.ShowcaseTool.csproj
//
// The app will be started automatically if not already running on port 5201.
// Output goes to:  FreeBlazorExample/runs/showcase-{date}/SHOWCASE_REPORT.md
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using ImageMagick;
using Microsoft.Playwright;

namespace FreeBlazorExample.ShowcaseTool;

// ─────────────────────────────────────────────────────────────────────────────
//  DATA TYPES
// ─────────────────────────────────────────────────────────────────────────────

record PageInfo(string Category, string Route, string Title);

record NetworkEntry(string Url, string Method, int Status, long DurationMs, long Bytes);

class PageCapture
{
    public required PageInfo Page        { get; init; }
    public required string   Slug        { get; init; }
    public required string   OutputDir   { get; init; }

    // Relative paths used in the markdown (relative to the report file location)
    public string? FullPngRel  { get; set; }
    public string? ThumbRel    { get; set; }
    public string? GifRel      { get; set; }

    // Deduplication — non-null means this page's screenshot is identical to another
    public string? DuplicateOf { get; set; }  // slug of the canonical page

    // HTTP / timing
    public int  StatusCode    { get; set; }
    public long NavigationMs  { get; set; }
    public long TotalLoadMs   { get; set; }

    // Content extracted from the live DOM
    public string PageTitle  { get; set; } = "";
    public string H1Text     { get; set; } = "";
    public string AboutText  { get; set; } = "";

    // Network traffic
    public List<NetworkEntry> NetworkRequests { get; } = [];

    // Console capture
    public List<string> ConsoleErrors   { get; } = [];
    public List<string> ConsoleWarnings { get; } = [];
    public List<string> ConsoleInfo     { get; } = [];

    // Error state
    public string? ErrorMessage { get; set; }

    public bool IsSuccess   => StatusCode >= 200 && StatusCode < 400;
    public long TotalBytes  => NetworkRequests.Sum(r => r.Bytes);
}

// ─────────────────────────────────────────────────────────────────────────────
//  MAIN PROGRAM
// ─────────────────────────────────────────────────────────────────────────────

internal class Program
{
    // ── Configuration ────────────────────────────────────────────────────────
    const string BaseUrl          = "http://localhost:5201";
    const int    GifFrameCount    = 6;          // frames captured during page load
    const int    GifIntervalMs    = 2000;       // ms between GIF frames
    const int    SettleDelayMs    = 4000;       // extra wait after NetworkIdle
    const int    GifWidthPx       = 620;        // animated GIF width
    const int    ThumbWidthPx     = 420;        // static thumbnail width
    const int    ViewportW        = 1440;
    const int    ViewportH        = 900;

    // ── Page manifest (mirrors ShowcaseExampleManifest.cs exactly) ────────────
    static readonly List<PageInfo> AllPages = BuildPageList();

    static List<PageInfo> BuildPageList()
    {
        var pages = new List<PageInfo>();

        // ── Full Features ────────────────────────────────────────────────────
        void FF(string seg, string title) =>
            pages.Add(new("Full Features", $"/showcase/{seg}", title));

        FF("feature101-dynamic-forms",   "Feature 101 — Dynamic Forms");
        FF("feature102-multi-view-sync", "Feature 102 — Multi-View Sync");
        FF("feature103-calendar",        "Feature 103 — Calendar & Scheduling");
        FF("feature104-user-preferences","Feature 104 — User Preferences");
        FF("feature105-agent-monitoring","Feature 105 — Agent Monitoring");
        FF("feature107-hierarchical-tree","Feature 107 — Hierarchical Tree");

        // ── Core Demos ───────────────────────────────────────────────────────
        void CD(string seg, string title) =>
            pages.Add(new("Core Demos", $"/showcase/{seg}", title));

        CD("autocomplete",     "AutoComplete");
        CD("autogrowtext",     "Auto Grow Text");
        CD("confirmationcode", "Confirmation Code");
        CD("datetimepicker",   "Date Time Picker");
        CD("deleteconfirmation","Delete Confirmation");
        CD("getinput",         "Get Input");
        CD("multiselect",      "Multi Select");
        CD("pagedrecordset",   "Paged Recordset");
        CD("stringlist",       "String List");
        CD("togglepassword",   "Toggle Password");
        CD("github-repo",      "GitHub Repo Browser");
        CD("generic-git",      "Git Repo Browser");
        CD("smartsheet",       "Smartsheet Viewer");

        // ── Variant Families (Tier 1, 2A, 2B) — 3 versions each ─────────────
        void Variants(string category, IEnumerable<(string Title, string Seg)> families)
        {
            foreach (var (title, seg) in families)
                for (int v = 1; v <= 3; v++)
                    pages.Add(new(category, $"/showcase/{seg}/v{v}", $"{title} V{v}"));
        }

        Variants("Tier 1 Variants",
        [
            ("Signature",    "signature"),
            ("InfoTip",      "infotip"),
            ("NetworkChart", "networkchart"),
            ("Wizard",       "wizard"),
            ("AboutSection", "aboutsection"),
        ]);

        Variants("Tier 2A Variants",
        [
            ("ToastContainer",  "toastcontainer"),
            ("CommandPalette",  "commandpalette"),
            ("Timer",           "timer"),
            ("PipelineTracker", "pipelinetracker"),
            ("Timeline",        "timeline"),
            ("ImageGallery",    "imagegallery"),
        ]);

        Variants("Tier 2B Variants",
        [
            ("Carousel",    "carousel"),
            ("KanbanBoard", "kanbanboard"),
            ("ExampleNav",  "examplenav"),
            ("SelectFile",  "selectfile"),
            ("RenderFiles", "renderfiles"),
        ]);

        return pages;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ENTRY POINT
    // ─────────────────────────────────────────────────────────────────────────

    static async Task<int> Main(string[] args)
    {
        PrintBanner();

        // ── 1. Locate solution ────────────────────────────────────────────────
        var solutionDir = FindSolutionDir(AppContext.BaseDirectory);
        if (solutionDir == null)
        {
            Console.Error.WriteLine("ERROR: Cannot locate FreeBlazorExample solution folder.");
            Console.Error.WriteLine("       Run from within the AllOfDanielsProjects tree.");
            return 1;
        }

        var appProject  = Path.Combine(solutionDir, "FreeBlazorExample", "FreeBlazorExample.csproj");
        var runLabel    = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
        var outputRoot  = Path.Combine(solutionDir, "runs", $"showcase-{runLabel}");
        var screensDir  = Path.Combine(outputRoot, "screenshots");
        var reportPath  = Path.Combine(outputRoot, "SHOWCASE_REPORT.md");

        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(screensDir);

        PrintConfig("Solution",   solutionDir);
        PrintConfig("App Project", appProject);
        PrintConfig("Output",      outputRoot);
        PrintConfig("Pages",       AllPages.Count.ToString());
        PrintConfig("Est. Time",   $"~{AllPages.Count * 15 / 60} minutes");
        Console.WriteLine();

        // ── 2. Start web app if not running ──────────────────────────────────
        Console.WriteLine("[1/6] Checking web app on http://localhost:5201 ...");
        Process? webProcess = null;
        if (await IsAppRespondingAsync())
        {
            Console.WriteLine("      ✅ Already running.");
        }
        else
        {
            Console.WriteLine($"      Starting: dotnet run --project {Path.GetFileName(appProject)}");
            webProcess = StartWebApp(appProject);
            Console.Write("      Waiting ");
            if (!await WaitForReadyAsync(90))
            {
                Console.Error.WriteLine("\n      ❌ App did not respond within 90 seconds.");
                webProcess?.Kill(entireProcessTree: true);
                return 1;
            }
            Console.WriteLine("  ✅ Ready.");
        }

        // ── 3. Install Playwright browsers ───────────────────────────────────
        Console.WriteLine("[2/6] Ensuring Playwright / Chromium installed...");
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Console.WriteLine("      ✅ Chromium ready.");

        // ── 4. Capture all pages (single shared context for WASM cache hits) ──
        Console.WriteLine($"[3/6] Launching Chromium and logging in...");

        var captures = new List<PageCapture>();
        // Dedup: SHA256(full screenshot bytes) → first slug that produced that image
        var screenshotHashes = new ConcurrentDictionary<string, string>();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true });

        // Single context → WASM cached after first page + auth session shared across all 64 pages
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = ViewportW, Height = ViewportH }
        });

        // Log in once — session (LocalStorage token) is preserved for all subsequent pages
        await LoginAsync(context);

        Console.WriteLine($"[4/6] Capturing {AllPages.Count} pages (1440×900, headless Chromium)...");
        Console.WriteLine();

        for (int i = 0; i < AllPages.Count; i++)
        {
            var page = AllPages[i];
            var slug = Slugify(page.Route);
            var pageOutDir = Path.Combine(screensDir, slug);
            Directory.CreateDirectory(pageOutDir);

            var capture = new PageCapture
            {
                Page      = page,
                Slug      = slug,
                OutputDir = pageOutDir
            };

            Console.Write($"  [{i + 1,2}/{AllPages.Count}] {TruncatePad(page.Title, 44)}");

            try
            {
                await CapturePageAsync(context, capture, screenshotHashes);

                var icon    = capture.IsSuccess ? "✅" : "❌";
                var errBadge = capture.ConsoleErrors.Count > 0
                    ? $"🔴 {capture.ConsoleErrors.Count}err" : "✅ 0err";
                Console.WriteLine(
                    $" {icon} {capture.StatusCode} {capture.TotalLoadMs,5}ms " +
                    $"{FormatBytes(capture.TotalBytes),8} {errBadge}");
                if (capture.ConsoleErrors.Count > 0)
                    foreach (var e in capture.ConsoleErrors.Take(5))
                        Console.WriteLine($"         ⚠ {(e.Length > 120 ? e[..120] + "…" : e)}");
            }
            catch (Exception ex)
            {
                capture.ErrorMessage = ex.Message;
                Console.WriteLine($" 💥 {ex.Message[..Math.Min(55, ex.Message.Length)]}");
            }

            captures.Add(capture);
        }

        // ── 5. Generate report ────────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine("[5/6] Generating SHOWCASE_REPORT.md ...");
        var report = BuildReport(captures);
        await File.WriteAllTextAsync(reportPath, report, Encoding.UTF8);
        Console.WriteLine($"      ✅ {reportPath}");

        // ── 6. Cleanup ────────────────────────────────────────────────────────
        Console.WriteLine("[6/6] Cleanup...");
        if (webProcess != null)
        {
            Console.WriteLine("      Stopping web app process...");
            try { webProcess.Kill(entireProcessTree: true); } catch { }
        }

        PrintFooter(captures, reportPath);
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE CAPTURE
    // ─────────────────────────────────────────────────────────────────────────

    static async Task CapturePageAsync(IBrowserContext context, PageCapture cap,
        ConcurrentDictionary<string, string> screenshotHashes)
    {
        var url      = BaseUrl + cap.Page.Route;
        var relBase  = $"screenshots/{cap.Slug}";
        var framesDir = Path.Combine(cap.OutputDir, "frames");
        Directory.CreateDirectory(framesDir);

        var page = await context.NewPageAsync();
        try
        {
            // ── Wire console capture ──────────────────────────────────────────
            page.Console += (_, msg) =>
            {
                var text = msg.Text;
                switch (msg.Type)
                {
                    case "error":
                    case "assert":
                        // Suppress browser-generated resource-load errors for known-benign HTTP failures.
                        // The browser logs "Failed to load resource: the server responded with a status of
                        // 4xx" when an <img> or fetch() returns a non-2xx — these duplicate what we already
                        // filter in the Response handler (logo 404, Smartsheet 401) so skip them here too.
                        var isBenignResourceErr =
                            text.StartsWith("Failed to load resource: the server responded with a status of 404") ||
                            text.StartsWith("Failed to load resource: the server responded with a status of 401");
                        if (!isBenignResourceErr)
                            lock (cap.ConsoleErrors) cap.ConsoleErrors.Add($"[{msg.Type}] {text}");
                        break;
                    case "warning":
                    case "warn":
                        lock (cap.ConsoleWarnings) cap.ConsoleWarnings.Add(text);
                        break;
                    default:
                        lock (cap.ConsoleInfo) cap.ConsoleInfo.Add($"[{msg.Type}] {text}");
                        break;
                }
            };
            page.PageError += (_, err) =>
            {
                lock (cap.ConsoleErrors) cap.ConsoleErrors.Add($"[pageerror] {err}");
            };

            // ── Wire network capture ──────────────────────────────────────────
            var requestStart = new ConcurrentDictionary<string, long>();
            page.Request += (_, req) =>
                requestStart[req.Url] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            page.Response += async (_, resp) =>
            {
                try
                {
                    var t0 = requestStart.TryGetValue(resp.Url, out var s) ? s : 0;
                    var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t0;
                    long bytes = 0;
                    try { bytes = (await resp.BodyAsync()).Length; } catch { }

                    // Surface 4xx/5xx responses as console errors — but suppress known-benign cases:
                    //   • /File/View/{guid}   — sample tenant logo/photo not in demo DB (404 expected)
                    //   • /api/Showcase/Smartsheet/sheets 401 — no session on first load (expected)
                    if (resp.Status >= 400) {
                        var isBenign = (resp.Status == 404 && resp.Url.Contains("/File/View/"))
                                    || (resp.Status == 401 && resp.Url.Contains("/api/Showcase/Smartsheet/"));
                        if (!isBenign) {
                            var shortUrl = resp.Url.Length > 100 ? "…" + resp.Url[^97..] : resp.Url;
                            lock (cap.ConsoleErrors) cap.ConsoleErrors.Add($"[error] HTTP {resp.Status} {shortUrl}");
                        }
                    }
                    lock (cap.NetworkRequests)
                        cap.NetworkRequests.Add(new NetworkEntry(
                            resp.Url, resp.Request.Method, resp.Status, ms, bytes));
                }
                catch { /* non-critical */ }
            };

            // ── Navigate (wait for DOMContentLoaded — fast even for WASM) ────
            var sw = Stopwatch.StartNew();
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout   = 45_000
            });
            cap.StatusCode    = response?.Status ?? 0;
            cap.NavigationMs  = sw.ElapsedMilliseconds;

            // ── GIF frames: capture during WASM boot sequence ─────────────────
            // Frame 0 = just after DOMContentLoaded (loading spinner)
            // Frames 1-5 = every 2s (WASM loading → rendered)
            var framePaths = new List<string>();
            for (int f = 0; f < GifFrameCount; f++)
            {
                if (f > 0) await page.WaitForTimeoutAsync(GifIntervalMs);
                var fp = Path.Combine(framesDir, $"f{f:00}.png");
                try
                {
                    // viewport-only (not full-page) — keeps frames fast and GIF compact
                    await page.ScreenshotAsync(new PageScreenshotOptions
                        { Path = fp, FullPage = false });
                    framePaths.Add(fp);
                }
                catch { /* page may still be navigating */ }
            }

            // ── Wait for full network idle + Blazor settle ────────────────────
            try
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = 30_000 });
            }
            catch { /* timeout is acceptable — page still captured */ }

            await page.WaitForTimeoutAsync(SettleDelayMs);
            cap.TotalLoadMs = sw.ElapsedMilliseconds;

            // ── Extract page metadata ─────────────────────────────────────────
            cap.PageTitle = await page.TitleAsync();
            try
            {
                var h1 = await page.QuerySelectorAsync("h1");
                if (h1 != null) cap.H1Text = (await h1.InnerTextAsync()).Trim();
            }
            catch { }

            // ── Expand and extract AboutSection components ────────────────────
            // AboutSection.razor renders: div.card.border-info > div.card-header.bg-info (clickable)
            try
            {
                var headers = await page.QuerySelectorAllAsync("div.card-header.bg-info");
                foreach (var h in headers)
                    try { await h.ClickAsync(new ElementHandleClickOptions { Force = true }); }
                    catch { }

                if (headers.Count > 0)
                    await page.WaitForTimeoutAsync(600); // wait for expand animation

                var aboutSb = new StringBuilder();
                var cards = await page.QuerySelectorAllAsync("div.card.border-info");
                foreach (var card in cards)
                {
                    var titleEl = await card.QuerySelectorAsync("div.card-header strong");
                    var bodyEl  = await card.QuerySelectorAsync("div.card-body");
                    if (titleEl != null)
                        aboutSb.AppendLine($"**{(await titleEl.InnerTextAsync()).Trim()}**");
                    if (bodyEl != null)
                    {
                        aboutSb.AppendLine((await bodyEl.InnerTextAsync()).Trim());
                        aboutSb.AppendLine();
                    }
                }
                cap.AboutText = aboutSb.ToString().Trim();
            }
            catch { }

            // ── Full-page screenshot (after about sections expanded) ───────────
            // Playwright only supports PNG/JPEG as path extensions; capture PNG then
            // convert to WebP with MagickImage for ~70% size reduction.
            var fullPng  = Path.Combine(cap.OutputDir, "full.png");
            var fullPath = Path.Combine(cap.OutputDir, "full.webp");
            await page.ScreenshotAsync(new PageScreenshotOptions
                { Path = fullPng, FullPage = true });

            // Retry if suspiciously small (Blazor sometimes delivers blank on first shot)
            if (new FileInfo(fullPng).Length < 12_000)
            {
                await page.WaitForTimeoutAsync(3000);
                await page.ScreenshotAsync(new PageScreenshotOptions
                    { Path = fullPng, FullPage = true });
            }

            // ── Deduplication: hash the raw PNG bytes ─────────────────────────
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = await File.ReadAllBytesAsync(fullPng);
                hash = Convert.ToHexString(sha.ComputeHash(bytes));
            }

            if (screenshotHashes.TryGetValue(hash, out var canonicalSlug))
            {
                // Bit-identical to an existing capture — delete and link to canonical
                File.Delete(fullPng);
                cap.DuplicateOf = canonicalSlug;
                cap.FullPngRel  = $"screenshots/{canonicalSlug}/full.webp";
            }
            else
            {
                screenshotHashes[hash] = cap.Slug;

                // Convert PNG → WebP (quality 85) for ~70% savings vs lossless PNG
                using (var img = new MagickImage(fullPng))
                {
                    img.Format  = MagickFormat.WebP;
                    img.Quality = 85;
                    img.Write(fullPath);
                }
                File.Delete(fullPng);
                cap.FullPngRel = $"{relBase}/full.webp";
            }

            // ── Thumbnail (static, 420px wide, WebP) ─────────────────────────
            var thumbPath = Path.Combine(cap.OutputDir, "thumb.webp");
            if (cap.DuplicateOf == null)
            {
                using (var thumb = new MagickImage(fullPath))
                {
                    thumb.Resize(ThumbWidthPx, 0);
                    thumb.Format  = MagickFormat.WebP;
                    thumb.Quality = 80;
                    thumb.Write(thumbPath);
                }
                cap.ThumbRel = $"{relBase}/thumb.webp";
            }
            else
            {
                cap.ThumbRel = $"screenshots/{cap.DuplicateOf}/thumb.webp";
            }

            // ── Animated GIF from frames ──────────────────────────────────────
            if (framePaths.Count > 0)
            {
                var gifPath = Path.Combine(cap.OutputDir, "loading.gif");
                try
                {
                    using var collection = new MagickImageCollection();
                    foreach (var fp in framePaths)
                    {
                        if (!File.Exists(fp)) continue;
                        var img = new MagickImage(fp);       // collection owns — no 'using'
                        img.Resize(GifWidthPx, 0);
                        img.AnimationDelay = GifIntervalMs / 10; // GIF delay is in 1/100ths of a second
                        img.GifDisposeMethod = GifDisposeMethod.Background;
                        collection.Add(img);
                    }
                    if (collection.Count > 0)
                    {
                        collection.Coalesce();
                        collection.Optimize();              // delta-encode identical regions (~30% smaller)
                        collection.OptimizeTransparency();  // further reduce transparent frame areas
                        collection.Write(gifPath);
                        cap.GifRel = $"{relBase}/loading.gif";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n      [GIF warning] {ex.Message}");
                }
                finally
                {
                    // Delete raw frame PNGs — GIF is the only deliverable
                    try { Directory.Delete(framesDir, recursive: true); } catch { }
                }
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  REPORT GENERATION
    // ─────────────────────────────────────────────────────────────────────────

    static string BuildReport(List<PageCapture> captures)
    {
        var sb  = new StringBuilder();
        var now = DateTimeOffset.Now;
        var tzAbbr = GetTimezoneAbbr();
        var successCount  = captures.Count(c => c.IsSuccess);
        var totalErrors   = captures.Sum(c => c.ConsoleErrors.Count);
        var totalBytes    = captures.Sum(c => c.TotalBytes);
        var totalRequests = captures.Sum(c => c.NetworkRequests.Count);
        var dupCount      = captures.Count(c => c.DuplicateOf != null);
        var categories    = captures.Select(c => c.Page.Category).Distinct().ToList();

        // ── Header ────────────────────────────────────────────────────────────
        sb.AppendLine("# FreeBlazorExtended — Full Showcase Report");
        sb.AppendLine();
        sb.AppendLine($"> **Generated:** {now:yyyy-MM-dd HH:mm} {tzAbbr}  ");
        sb.AppendLine($"> **App:** {BaseUrl}  ");
        sb.AppendLine($"> **Pages Captured:** {successCount} / {captures.Count}  ");
        sb.AppendLine($"> **Total JS Errors:** {totalErrors}  ");
        sb.AppendLine($"> **Total Network Requests:** {totalRequests:N0}  ");
        sb.AppendLine($"> **Total Data Transferred:** {FormatBytes(totalBytes)}  ");
        if (dupCount > 0)
            sb.AppendLine($"> **Deduplicated Screenshots:** {dupCount} pages shared an identical screenshot (saved as references)  ");
        sb.AppendLine();
        sb.AppendLine("> 🎬 **Gallery thumbnails** are animated GIFs showing the Blazor WASM boot sequence.  ");
        sb.AppendLine("> Click a thumbnail to open the **full-page screenshot**.  ");
        sb.AppendLine("> Expand **📋 Details** for about-section text, JS console output, and network traffic.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // ── Table of Contents ─────────────────────────────────────────────────
        sb.AppendLine("## 📑 Table of Contents");
        sb.AppendLine();
        foreach (var cat in categories)
        {
            var anchor = cat.ToLowerInvariant().Replace(' ', '-').Replace("─", "").Trim('-');
            sb.AppendLine($"- [{cat}](#{anchor})");
        }
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // ── Gallery by category ───────────────────────────────────────────────
        foreach (var cat in categories)
        {
            var catCaps = captures.Where(c => c.Page.Category == cat).ToList();
            var catOk   = catCaps.Count(c => c.IsSuccess);
            var catErrs = catCaps.Sum(c => c.ConsoleErrors.Count);
            var catBytes = catCaps.Sum(c => c.TotalBytes);

            sb.AppendLine($"## {cat}");
            sb.AppendLine();
            sb.AppendLine($"> {catCaps.Count} pages &nbsp;|&nbsp; " +
                          $"✅ {catOk} ok &nbsp;|&nbsp; " +
                          $"🔴 {catErrs} JS errors &nbsp;|&nbsp; " +
                          $"📦 {FormatBytes(catBytes)} transferred");
            sb.AppendLine();

            sb.AppendLine("<table>");

            for (int i = 0; i < catCaps.Count; i += 3)
            {
                sb.AppendLine("<tr>");

                for (int j = 0; j < 3; j++)
                {
                    int idx = i + j;
                    if (idx < catCaps.Count)
                        AppendPageCell(sb, catCaps[idx]);
                    else
                        sb.AppendLine("<td></td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        // ── Footer ────────────────────────────────────────────────────────────
        sb.AppendLine("## 🔧 Tool Info");
        sb.AppendLine();
        sb.AppendLine("Generated by **FreeBlazorExample.ShowcaseTool** — Microsoft.Playwright + Magick.NET");
        sb.AppendLine();
        sb.AppendLine("| Setting | Value |");
        sb.AppendLine("|---------|-------|");
        sb.AppendLine($"| App URL | {BaseUrl} |");
        sb.AppendLine("| Browser | Chromium (headless) |");
        sb.AppendLine($"| Viewport | {ViewportW}×{ViewportH} |");
        sb.AppendLine($"| GIF frames | {GifFrameCount} × {GifIntervalMs / 1000}s intervals |");
        sb.AppendLine($"| GIF width | {GifWidthPx}px |");
        sb.AppendLine($"| Thumb width | {ThumbWidthPx}px |");
        sb.AppendLine($"| Blazor settle delay | {SettleDelayMs}ms after NetworkIdle |");
        sb.AppendLine($"| About sections | Auto-expanded before final screenshot |");
        sb.AppendLine();

        return sb.ToString();
    }

    static void AppendPageCell(StringBuilder sb, PageCapture cap)
    {
        var statusIcon  = cap.IsSuccess ? "✅" : (cap.ErrorMessage != null ? "💥" : "❌");
        var errBadge    = cap.ConsoleErrors.Count > 0 ? $"🔴 {cap.ConsoleErrors.Count}" : "✅ 0";
        var warnBadge   = cap.ConsoleWarnings.Count > 0 ? $"⚠️ {cap.ConsoleWarnings.Count}" : "✅ 0";

        // The GIF is the gallery thumbnail (shows the loading boot sequence).
        // If we couldn't generate a GIF, fall back to the static thumb.
        var galleryImg  = cap.GifRel ?? cap.ThumbRel ?? cap.FullPngRel;
        var fullTarget  = cap.FullPngRel ?? galleryImg;

        sb.AppendLine("<td align=\"center\" valign=\"top\" width=\"33%\">");

        // ── Animated thumbnail linking to full screenshot ─────────────────────
        if (galleryImg != null)
        {
            sb.AppendLine($"<a href=\"{fullTarget}\">");
            sb.AppendLine($"<img src=\"{galleryImg}\" width=\"340\"" +
                          $" alt=\"{EscHtml(cap.Page.Title)}\" />");
            sb.AppendLine("</a>");
        }

        sb.AppendLine("<br/>");

        // Page title links to full screenshot
        if (fullTarget != null)
            sb.AppendLine($"<a href=\"{fullTarget}\"><strong>{EscHtml(cap.Page.Title)}</strong></a>");
        else
            sb.AppendLine($"<strong>{EscHtml(cap.Page.Title)}</strong>");

        sb.AppendLine($"<br/><code>{cap.Page.Route}</code><br/>");

        // Status line
        sb.AppendLine(
            $"{statusIcon} {cap.StatusCode} &nbsp;" +
            $"⏱ {cap.TotalLoadMs:N0}ms &nbsp;" +
            $"📦 {FormatBytes(cap.TotalBytes)} &nbsp;" +
            $"🖥 {errBadge} err");

        // Deduplication notice
        if (cap.DuplicateOf != null)
            sb.AppendLine($"<br/><sub>🔁 identical screenshot → <code>{cap.DuplicateOf}</code></sub>");

        sb.AppendLine();

        // ── Expandable details section ────────────────────────────────────────
        sb.AppendLine("<details>");
        sb.AppendLine("<summary>📋 Details</summary>");
        sb.AppendLine();

        // Error message
        if (cap.ErrorMessage != null)
        {
            sb.AppendLine($"> ❌ **Error:** `{cap.ErrorMessage}`");
            sb.AppendLine();
        }

        // About section content (extracted live from DOM)
        if (!string.IsNullOrWhiteSpace(cap.AboutText))
        {
            sb.AppendLine("**ℹ️ About This Component**");
            sb.AppendLine();
            sb.AppendLine(cap.AboutText);
        }
        else if (!string.IsNullOrWhiteSpace(cap.H1Text))
        {
            sb.AppendLine($"**{EscHtml(cap.H1Text)}**");
            sb.AppendLine();
        }

        // Metrics table
        sb.AppendLine("**📊 Metrics**");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| HTTP Status | {cap.StatusCode} |");
        sb.AppendLine($"| Navigation | {cap.NavigationMs:N0} ms |");
        sb.AppendLine($"| Full Load (incl. settle) | {cap.TotalLoadMs:N0} ms |");
        sb.AppendLine($"| Network Requests | {cap.NetworkRequests.Count:N0} |");
        sb.AppendLine($"| Data Transferred | {FormatBytes(cap.TotalBytes)} |");
        sb.AppendLine($"| JS Errors | {cap.ConsoleErrors.Count} |");
        sb.AppendLine($"| JS Warnings | {cap.ConsoleWarnings.Count} |");
        sb.AppendLine();

        // JS Errors (expandable)
        if (cap.ConsoleErrors.Count > 0)
        {
            sb.AppendLine("<details>");
            sb.AppendLine($"<summary>🔴 JS Errors ({cap.ConsoleErrors.Count})</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var err in cap.ConsoleErrors.Take(20))
                sb.AppendLine(err.Length > 200 ? err[..200] + "…" : err);
            if (cap.ConsoleErrors.Count > 20)
                sb.AppendLine($"… and {cap.ConsoleErrors.Count - 20} more");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        // JS Warnings (expandable)
        if (cap.ConsoleWarnings.Count > 0)
        {
            sb.AppendLine("<details>");
            sb.AppendLine($"<summary>⚠️ Warnings ({cap.ConsoleWarnings.Count})</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var w in cap.ConsoleWarnings.Take(15))
                sb.AppendLine(w.Length > 150 ? w[..150] + "…" : w);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        // Network log (top 15 by bytes, expandable)
        if (cap.NetworkRequests.Count > 0)
        {
            sb.AppendLine("<details>");
            sb.AppendLine($"<summary>🌐 Network ({cap.NetworkRequests.Count} requests, {FormatBytes(cap.TotalBytes)})</summary>");
            sb.AppendLine();
            sb.AppendLine("| Method | Status | Duration | Bytes | URL |");
            sb.AppendLine("|--------|:------:|:--------:|------:|-----|");
            foreach (var req in cap.NetworkRequests.OrderByDescending(r => r.Bytes).Take(15))
            {
                var shortUrl = req.Url.Length > 65 ? "…" + req.Url[^62..] : req.Url;
                sb.AppendLine(
                    $"| {req.Method} | {req.Status} | {req.DurationMs}ms | {FormatBytes(req.Bytes)} | `{shortUrl}` |");
            }
            if (cap.NetworkRequests.Count > 15)
                sb.AppendLine($"| … | … | … | … | *{cap.NetworkRequests.Count - 15} more* |");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        // Console info (debug/log — expandable, collapsed by default)
        if (cap.ConsoleInfo.Count > 0)
        {
            sb.AppendLine("<details>");
            sb.AppendLine($"<summary>🖥 Console Info ({cap.ConsoleInfo.Count} messages)</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var msg in cap.ConsoleInfo.Take(20))
                sb.AppendLine(msg.Length > 150 ? msg[..150] + "…" : msg);
            if (cap.ConsoleInfo.Count > 20)
                sb.AppendLine($"… and {cap.ConsoleInfo.Count - 20} more");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        sb.AppendLine("</details>");   // end page details
        sb.AppendLine();
        sb.AppendLine("</td>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LOGIN HELPER
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Logs in by driving the actual Blazor WASM login form through Playwright.
    ///
    /// Flow:
    ///   1. Navigate to BaseUrl  → WASM boots, detects no auth token, renders the local
    ///      login form (#login-email / #login-password visible after ~12 s WASM boot).
    ///   2. Fill in admin@local / admin and click "Log In".
    ///   3. The WASM calls api/Data/Authenticate WITH the browser's ThumbmarkJS fingerprint
    ///      in the request header, so the returned token is fingerprint-bound.
    ///   4. CookieWrite stores the token in the browser context cookie jar.
    ///   5. All subsequent context pages inherit that cookie and pass auth.
    ///
    /// Why not use the REST API directly?
    ///   api/Data/Authenticate called without a fingerprint creates a token with an empty
    ///   fingerprint embedded in its JWT payload.  When the WASM later calls
    ///   api/Data/GetUserFromToken it sends the real ThumbmarkJS fingerprint — the server
    ///   compares the two and rejects the session, so screenshots still show "Login Required".
    /// </summary>
    static async Task LoginAsync(IBrowserContext context)
    {
        const int WasmBootMs  = 40_000;   // timeout waiting for WASM to render login form

        Console.Write("      Waiting for WASM login form (first boot ~12 s)...");

        var page = await context.NewPageAsync();
        try
        {
            // Navigate to root; WASM boots, detects no token, shows the local login form.
            await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout   = 30_000,
            });

            // Wait for #login-email — only appears once WASM has booted and confirmed
            // the user is not authenticated.
            await page.WaitForSelectorAsync("#login-email",
                new PageWaitForSelectorOptions
                {
                    State   = WaitForSelectorState.Visible,
                    Timeout = WasmBootMs,
                });

            Console.Write(" form visible. Submitting credentials...");

            // Fill credentials — @bind:event="oninput" updates Blazor bindings on each keystroke.
            await page.FillAsync("#login-email",    "admin@local");
            await page.FillAsync("#login-password", "admin");

            // Wait for Blazor to re-render so the LoginDisabled binding clears.
            await page.WaitForSelectorAsync("button.btn-primary:not([disabled])",
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });

            await page.ClickAsync("button.btn-primary");

            // ProcessLogin() sets processing=true immediately (hides #login-email) then
            // makes the async Authenticate API call and only writes the cookie AFTER
            // the API response returns.  Waiting for #login-email to be hidden would
            // race with the processing spinner and close the page too early.
            // Instead, wait for the actual cookie to appear in document.cookie.
            await page.WaitForFunctionAsync(
                "() => document.cookie.includes('user-token')",
                null,
                new PageWaitForFunctionOptions { Timeout = 20_000, PollingInterval = 200 });


            Console.WriteLine(" done.");
            Console.WriteLine("      ✅ Logged in as admin@local via WASM login form.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n      ⚠️  Login failed: {ex.Message[..Math.Min(200, ex.Message.Length)]}");
            Console.WriteLine($"         Captures may show the login page instead of component content.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WEB APP HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    static async Task<bool> IsAppRespondingAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try { await client.GetAsync(BaseUrl); return true; }
        catch { return false; }
    }

    static Process StartWebApp(string appProjectPath)
    {
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName  = "dotnet",
                Arguments = $"run --project \"{appProjectPath}\" --launch-profile http",
                UseShellExecute  = false,
                CreateNoWindow   = true,
            }
        };
        proc.Start();
        return proc;
    }

    static async Task<bool> WaitForReadyAsync(int timeoutSeconds)
    {
        using var client  = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try { await client.GetAsync(BaseUrl); return true; }
            catch { Console.Write("."); await Task.Delay(2500); }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UTILITY HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walk up from startPath until we find the folder that contains
    /// FreeBlazorExample/FreeBlazorExample.csproj — that's our solution root.
    /// </summary>
    static string? FindSolutionDir(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            var probe = Path.Combine(dir.FullName, "FreeBlazorExample", "FreeBlazorExample.csproj");
            if (File.Exists(probe)) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Returns the local timezone abbreviation, e.g. "PST", "PDT", "EST", "CDT".
    /// Builds the abbreviation from the initials of the OS timezone name words
    /// ("Pacific Daylight Time" → "PDT", "Eastern Standard Time" → "EST").
    /// </summary>
    static string GetTimezoneAbbr()
    {
        var tz   = TimeZoneInfo.Local;
        var now  = DateTime.Now;
        var name = tz.IsDaylightSavingTime(now) ? tz.DaylightName : tz.StandardName;
        // Build abbreviation from first letter of each word
        var abbr = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(w => w[0]));
        // Append UTC offset for disambiguation (e.g. PST UTC-8)
        var offset    = TimeZoneInfo.Local.GetUtcOffset(now);
        var sign      = offset >= TimeSpan.Zero ? "+" : "-";
        var offsetStr = $"UTC{sign}{Math.Abs(offset.Hours)}";
        if (offset.Minutes != 0) offsetStr += $":{Math.Abs(offset.Minutes):D2}";
        return $"{abbr} ({offsetStr})";
    }

    static string Slugify(string route) =>
        route.TrimStart('/').Replace('/', '-').Replace(' ', '-').ToLowerInvariant();

    static string FormatBytes(long bytes) =>
        bytes switch
        {
            < 1024             => $"{bytes} B",
            < 1024 * 1024      => $"{bytes / 1024.0:F1} KB",
            _                  => $"{bytes / (1024.0 * 1024):F1} MB"
        };

    static string EscHtml(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    static string TruncatePad(string s, int width) =>
        s.Length > width ? s[..(width - 1)] + "…" : s.PadRight(width);

    // ─────────────────────────────────────────────────────────────────────────
    //  CONSOLE OUTPUT
    // ─────────────────────────────────────────────────────────────────────────

    static void PrintBanner()
    {
        Console.WriteLine("=================================================================");
        Console.WriteLine("  FreeBlazorExtended Showcase Tool");
        Console.WriteLine("  Playwright screenshots · Loading GIFs · Network · Console · About");
        Console.WriteLine("=================================================================");
        Console.WriteLine();
    }

    static void PrintConfig(string key, string value) =>
        Console.WriteLine($"  {key,-14} {value}");

    static void PrintFooter(List<PageCapture> captures, string reportPath)
    {
        var ok      = captures.Count(c => c.IsSuccess);
        var errors  = captures.Sum(c => c.ConsoleErrors.Count);
        var bytes   = FormatBytes(captures.Sum(c => c.TotalBytes));

        Console.WriteLine();
        Console.WriteLine("=================================================================");
        Console.WriteLine("  SHOWCASE REPORT COMPLETE");
        Console.WriteLine("=================================================================");
        Console.WriteLine($"  Pages:    {ok}/{captures.Count} captured successfully");
        Console.WriteLine($"  JS Errors: {errors} total across all pages");
        Console.WriteLine($"  Data:     {bytes} transferred in browser");
        Console.WriteLine($"  Report:   {reportPath}");
        Console.WriteLine("=================================================================");
    }
}
