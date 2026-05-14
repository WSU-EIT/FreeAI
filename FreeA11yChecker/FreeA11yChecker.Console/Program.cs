// FreeA11yChecker.Console -- Standalone Accessibility Scanner CLI
//
// Thin wrapper around FreeA11yChecker.Scanner shared library.
// Parses CLI args and appsettings.json, then delegates all scanning
// to ScannerEngine. No inline scanning code here.
//
// Usage:
//   dotnet run                                  -> Interactive menu
//   dotnet run -- scan                          -> Scan sites from appsettings.json
//   dotnet run -- scan --url https://example.com
//   dotnet run -- scan --url https://x.com --pages /,/about,/contact
//   dotnet run -- scan --url https://x.com --user admin --pass admin
//   dotnet run -- help

using System.Diagnostics;
using System.Text.Json;
using FreeA11yChecker.Scanner;
using FreeA11yChecker.Scanner.Models;
using Microsoft.Extensions.Configuration;

// ── Configuration ──

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    // Same UserSecretsId as the FreeA11yChecker web app — credentials set once apply to both.
    .AddUserSecrets("0cbc4331-d04d-4e10-97b7-24a8798f626c")
    .AddCommandLine(args)
    .Build();

ScanConfig config = LoadConfig(configuration);

// Tee Console output to a timestamped file under {OutputDir}/_logs/. Decided location
// is canonical — Claude Code can find it without being told. User screenshots a problem,
// agents look in `runs/latest/_logs/` for the most recent file.
FreeA11yChecker.Console.RunLogger.Start(config.OutputDir);

// ── Route: CLI or Interactive ──

string? action = args.FirstOrDefault(a => !a.StartsWith("--"));

if (!string.IsNullOrEmpty(action)) {
    return await RunAction(action, config, args);
}

return await RunInteractive(config);

// ================================================================
// Interactive Menu
// ================================================================

static async Task<int> RunInteractive(ScanConfig config)
{
    while (true) {
        Console.Clear();
        PrintBanner();
        Console.WriteLine("  1. Scan sites (from appsettings.json)");
        Console.WriteLine("  2. Quick scan (enter URL)");
        Console.WriteLine("  3. View last results summary");
        Console.WriteLine("  4. Help");
        Console.WriteLine("  Q. Quit");
        Console.WriteLine();
        Console.Write("  Select: ");

        string? input = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (input == "Q") { Console.WriteLine("\n  Goodbye.\n"); return 0; }

        switch (input) {
            case "1":
                await RunScan(config);
                break;
            case "2":
                Console.Write("\n  Enter URL: ");
                string? url = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(url)) {
                    Console.Write("  Pages (comma-separated, or Enter for /): ");
                    string? pages = Console.ReadLine()?.Trim();
                    Console.Write("  Username (or Enter to skip): ");
                    string? user = Console.ReadLine()?.Trim();
                    string? pass = null;
                    if (!string.IsNullOrEmpty(user)) {
                        Console.Write("  Password: ");
                        pass = Console.ReadLine()?.Trim();
                    }

                    List<PageConfig> pageConfigs = string.IsNullOrEmpty(pages)
                        ? [new PageConfig { Path = "/" }]
                        : pages.Split(',').Select(p => new PageConfig { Path = p.Trim() }).ToList();

                    CredentialConfig? creds = !string.IsNullOrEmpty(user)
                        ? new CredentialConfig { Username = user, Password = pass ?? "" }
                        : null;

                    ScanConfig quickConfig = new ScanConfig {
                        SettleDelayMs = config.SettleDelayMs,
                        TimeoutMs = config.TimeoutMs,
                        Headless = config.Headless,
                        OutputDir = config.OutputDir,
                        WcagLevel = config.WcagLevel,
                        MaxConcurrency = config.MaxConcurrency,
                        Sites = new Dictionary<string, SiteConfig> {
                            [url] = new SiteConfig {
                                BaseUrl = url,
                                Pages = pageConfigs,
                                Credentials = creds
                            }
                        }
                    };
                    await RunScan(quickConfig);
                }
                break;
            case "3":
                ShowLastResults(config.OutputDir);
                break;
            case "4":
                ShowHelp();
                break;
        }

        Console.WriteLine("\n  Press Enter to continue...");
        Console.ReadLine();
    }
}

// ================================================================
// CLI Dispatcher
// ================================================================

async Task<int> RunAction(string action, ScanConfig config, string[] args)
{
    PrintBanner();

    return action.ToLowerInvariant() switch {
        "scan" => await RunScanFromArgs(config, args),
        "crawl" => await RunCrawlFromArgs(config, args),
        "analyze-source" => RunAnalyzeSourceStub(args),
        "handoff" => RunHandoff(config, args),
        "report" => ShowLastResults(config.OutputDir),
        "help" => ShowHelp(),
        _ => ShowHelp()
    };
}

/// <summary>
/// Crawl mode: iteratively scan a site, follow same-host links discovered on each page,
/// loop up to --max-depth iterations or until no new pages are found. Same-host filtering
/// is enforced both by ScannerEngine.DiscoverLinks (already returns only matching-host paths)
/// and again here as belt-and-suspenders.
///
/// Usage:
///   FreeA11yChecker.Console crawl --url https://flex.em.wsu.edu --max-depth 5
///   FreeA11yChecker.Console crawl --url https://flex.em.wsu.edu --user admin --pass xxx
/// </summary>
async Task<int> RunCrawlFromArgs(ScanConfig config, string[] args)
{
    string? urlArg = GetArg(args, "--url");
    if (string.IsNullOrEmpty(urlArg)) {
        WriteError("crawl requires --url <url>");
        return 1;
    }

    int maxDepth = int.TryParse(GetArg(args, "--max-depth"), out int md) ? md : 5;
    string? userArg = GetArg(args, "--user");
    string? passArg = GetArg(args, "--pass");
    string? seedArg = GetArg(args, "--pages");
    CredentialConfig? creds = ResolveCredentials(urlArg, userArg, passArg, configuration);

    Uri baseUri = new Uri(urlArg);
    string baseHost = baseUri.Host;

    // Seed the visit set. --pages lets the caller pre-load known routes (e.g. all
    // Flex Settings pages) so iteration 1 covers them in parallel instead of waiting
    // for link-discovery on a Blazor SPA whose menu may not be hydrated in time.
    var initialSeeds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/" };
    if (!string.IsNullOrWhiteSpace(seedArg)) {
        foreach (string p in seedArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            initialSeeds.Add(p);
    }

    // SOURCE-SEEDING: when --source-path (or App:SourceCode) is configured, parse all
    // .razor `@page` directives from the source folder and pre-seed crawl with them.
    // Static routes go straight into the seed; parameterized routes (with {id} etc.)
    // get separate handling — we record the templates and either resolve them from a
    // user-supplied --seed-ids JSON file OR surface them as "needs manual seeding"
    // in the handoff report. Modular: works for any source tree, not just Flex.
    string? srcForSeed = GetArg(args, "--source-path") ?? sourceCodePath;
    var parameterizedRouteTemplates = new List<string>();
    if (!string.IsNullOrEmpty(srcForSeed)) {
        // Resolve to absolute so relative paths from cwd-different-from-workspace work.
        string absSrc = Path.IsPathRooted(srcForSeed) ? srcForSeed : Path.GetFullPath(srcForSeed);
        if (!Directory.Exists(absSrc)) {
            FreeA11yChecker.Console.ColorOut.Warn($"  source-seed: path '{srcForSeed}' (resolved: '{absSrc}') does not exist — skipping");
        } else {
            try {
                var inv = FreeA11yChecker.Console.SourceAnalysis.CodebaseInventory.Analyze(absSrc);
                int addedStatic = 0, deferredParam = 0;
                foreach (string route in inv.BlazorRoutes) {
                    string clean = route.StartsWith("/") ? route : "/" + route;
                    if (clean.Contains('{') || clean.Contains('*')) {
                        parameterizedRouteTemplates.Add(clean);
                        deferredParam++;
                        continue;
                    }
                    if (initialSeeds.Add(clean)) addedStatic++;
                }
                FreeA11yChecker.Console.ColorOut.Success(
                    $"  source-seed from {absSrc}: {addedStatic} static route(s) added, " +
                    $"{deferredParam} parameterized route(s) deferred (need IDs), " +
                    $"{inv.BlazorRoutes.Count} total @page directives found");
                if (deferredParam > 0) {
                    FreeA11yChecker.Console.ColorOut.Detail("    parameterized routes:");
                    foreach (string p in parameterizedRouteTemplates.Take(10))
                        FreeA11yChecker.Console.ColorOut.Detail($"      {p}");
                    if (parameterizedRouteTemplates.Count > 10)
                        FreeA11yChecker.Console.ColorOut.Detail($"      ... and {parameterizedRouteTemplates.Count - 10} more");
                }
            } catch (Exception ex) {
                FreeA11yChecker.Console.ColorOut.Warn($"  source-seed failed: {ex.Message}");
            }
        }
    } else {
        FreeA11yChecker.Console.ColorOut.Warn("  source-seed skipped: no --source-path or App:SourceCode in config");
    }

    // SEED-IDs FILE: if --seed-ids points to a JSON map of {routeTemplate: [id1,id2,...]},
    // expand the parameterized routes with the supplied IDs and add to initial seed.
    // Format: { "/settings/edituser/{userid}": ["abc-123", "def-456"], ... }
    string? seedIdsPath = GetArg(args, "--seed-ids");
    if (!string.IsNullOrEmpty(seedIdsPath) && File.Exists(seedIdsPath)) {
        try {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(seedIdsPath));
            int idsAdded = 0;
            foreach (var prop in doc.RootElement.EnumerateObject()) {
                string template = prop.Name.StartsWith("/") ? prop.Name : "/" + prop.Name;
                if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Array) continue;
                foreach (var idEl in prop.Value.EnumerateArray()) {
                    string? id = idEl.GetString();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    string concrete = System.Text.RegularExpressions.Regex.Replace(template, @"\{[^}]+\}", id);
                    if (initialSeeds.Add(concrete)) idsAdded++;
                }
            }
            FreeA11yChecker.Console.ColorOut.Success($"  seed-ids: expanded {idsAdded} concrete URL(s) from {seedIdsPath}");
        } catch (Exception ex) {
            FreeA11yChecker.Console.ColorOut.Warn($"  seed-ids file load failed: {ex.Message}");
        }
    } else if (parameterizedRouteTemplates.Count > 0 && !string.IsNullOrEmpty(srcForSeed)) {
        // Auto-generate a TEMPLATE seed-ids file in the cookies dir (survives runs/latest wipe).
        // User can fill in real IDs and re-pass via --seed-ids.
        try {
            string cookiesDirForTemplate = Path.Combine(Directory.GetParent(config.OutputDir)?.FullName ?? config.OutputDir, ".cookies");
            Directory.CreateDirectory(cookiesDirForTemplate);
            string templatePath = Path.Combine(cookiesDirForTemplate, $"{baseHost}-seed-ids-template.json");
            var lines = new List<string> { "{" };
            for (int i = 0; i < parameterizedRouteTemplates.Count; i++) {
                string suffix = i < parameterizedRouteTemplates.Count - 1 ? "," : "";
                lines.Add($"  \"{parameterizedRouteTemplates[i]}\": [\"REPLACE-WITH-REAL-ID-1\", \"REPLACE-WITH-REAL-ID-2\"]{suffix}");
            }
            lines.Add("}");
            File.WriteAllText(templatePath, string.Join("\n", lines));
            FreeA11yChecker.Console.ColorOut.Step($"  to scan {parameterizedRouteTemplates.Count} parameterized route(s):");
            FreeA11yChecker.Console.ColorOut.Step($"    1. fill in real IDs at: {templatePath}");
            FreeA11yChecker.Console.ColorOut.Step($"    2. re-run with: --seed-ids \"{templatePath}\"");
        } catch { /* best-effort template gen */ }
    }

    var seedPaths = initialSeeds.OrderBy(p => p).ToArray();
    var visited = new HashSet<string>(seedPaths, StringComparer.OrdinalIgnoreCase);
    var pendingThisRound = new HashSet<string>(seedPaths, StringComparer.OrdinalIgnoreCase);
    int totalViolations = 0, totalPages = 0;
    var perPageStats = new List<(string Path, int Violations, int Critical, int Serious)>();

    Console.WriteLine($"  Crawl target: {urlArg}");
    Console.WriteLine($"  Max depth:    {maxDepth}");
    Console.WriteLine($"  Output:       {Path.GetFullPath(config.OutputDir)}");
    Console.WriteLine($"  Auth:         {(creds == null ? "anonymous" : "as " + creds.Username)}");
    Console.WriteLine();
    Stopwatch totalSw = Stopwatch.StartNew();

    for (int iter = 1; iter <= maxDepth && pendingThisRound.Count > 0; iter++) {
        var batch = pendingThisRound.OrderBy(p => p).ToList();
        pendingThisRound.Clear();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ── Iteration {iter}/{maxDepth} — {batch.Count} page(s) to scan ──");
        Console.ResetColor();

        ScanConfig iterConfig = new ScanConfig {
            SettleDelayMs = config.SettleDelayMs,
            TimeoutMs = config.TimeoutMs,
            Headless = config.Headless,
            OutputDir = config.OutputDir,
            WcagLevel = config.WcagLevel,
            MaxConcurrency = config.MaxConcurrency,
            Sites = new Dictionary<string, SiteConfig> {
                [urlArg] = new SiteConfig {
                    BaseUrl = urlArg,
                    Pages = batch.Select(p => new PageConfig { Path = p }).ToList(),
                    Credentials = creds,
                }
            }
        };

        Stopwatch iterSw = Stopwatch.StartNew();
        // PER-PAGE REAL-TIME WRITE: OnPageComplete fires the instant each page finishes
        // scanning, so a Ctrl+C mid-crawl preserves all data scanned so far. Replaces
        // the previous "wait for whole iteration, then batch-write" pattern.
        RunScanResult result = await ScannerEngine.ScanAll(iterConfig,
            progress => {
                FreeA11yChecker.Console.ColorOut.Auto($"    [{progress.CurrentPage}/{progress.TotalPages}] {progress.Message}");
            },
            (host, page) => {
                WritePageArtifactsToDisk(config.OutputDir, host, page);
            });
        iterSw.Stop();

        // Aggregate per-iteration stats from the result (artifacts already on disk).
        foreach (var siteResult in result.Sites.Values) {
            foreach (var page in siteResult.Pages) {
                int v = page.AxeIssues.Count + page.HtmlCheckIssues.Count + page.HtmlCsIssues.Count + page.IbmIssues.Count;
                int c = page.AxeIssues.Count(i => i.Severity == "critical") + page.IbmIssues.Count(i => i.Severity == "critical");
                int s = page.AxeIssues.Count(i => i.Severity == "serious") + page.IbmIssues.Count(i => i.Severity == "serious");
                totalViolations += v;
                totalPages++;
                perPageStats.Add((PathFromUrl(page.Url), v, c, s));

                // Queue newly-discovered same-host paths for the next iteration.
                foreach (string disc in page.DiscoveredPaths) {
                    if (string.IsNullOrEmpty(disc)) continue;
                    if (!disc.StartsWith("/")) continue; // expect absolute-from-root from DiscoverLinks
                    if (visited.Add(disc)) {
                        pendingThisRound.Add(disc);
                    }
                }
            }
        }

        Console.WriteLine($"    Iter {iter} done in {iterSw.Elapsed.TotalSeconds:F1}s. " +
            $"Discovered {pendingThisRound.Count} new path(s). Total visited: {visited.Count}.");
        Console.WriteLine();
    }

    totalSw.Stop();

    // Write aggregated crawl report.
    WriteCrawlSummary(config.OutputDir, baseHost, urlArg, perPageStats, visited, maxDepth, totalSw.Elapsed, creds != null);

    Console.ForegroundColor = totalViolations == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine($"  Crawl complete. {totalPages} pages scanned, {totalViolations} total violations, {totalSw.Elapsed.TotalMinutes:F1} min.");
    Console.ResetColor();
    Console.WriteLine($"  Aggregated report: {Path.Combine(Path.GetFullPath(config.OutputDir), baseHost, "_crawl-summary.html")}");

    // AUTO-HANDOFF: generate the AI-fix-pack markdown automatically. If --source-path
    // was provided, the doc includes file:line cross-references; otherwise it's a flat
    // rule-grouped issue list. Either way it's the single artifact a fixing AI needs.
    try {
        string? srcForHandoff = GetArg(args, "--source-path") ?? sourceCodePath;
        FreeA11yChecker.Console.ColorOut.Step("  Generating AI handoff doc...");
        string handoffPath = FreeA11yChecker.Console.SourceAnalysis.AiHandoffReport.Generate(
            config.OutputDir, baseHost, srcForHandoff);
        FreeA11yChecker.Console.ColorOut.Success($"  AI handoff: {handoffPath}");
    } catch (Exception ex) {
        FreeA11yChecker.Console.ColorOut.Warn($"  Handoff doc generation skipped: {ex.Message}");
    }

    return totalViolations > 0 ? 1 : 0;
}

/// <summary>
/// Writes every in-memory artifact a page produced (HTML, JSON dumps, MD report, error log)
/// plus every screenshot to {OutputDir}/{host}/{path-slug}/. The shared ScannerEngine builds
/// these in memory; the web app persists them to EF, the console persists them to disk.
/// </summary>
static void WritePageArtifactsToDisk(string outputDir, string host, PageScanResult page)
{
    try {
        string pageSlug = SlugifyPath(PathFromUrl(page.Url));
        string pageDir = Path.Combine(Path.GetFullPath(outputDir), host, pageSlug);
        Directory.CreateDirectory(pageDir);

        foreach (var (filename, _, data) in page.Artifacts) {
            File.WriteAllBytes(Path.Combine(pageDir, filename), data);
        }
        foreach (var shot in page.Screenshots) {
            if (shot.Data != null && shot.Data.Length > 0) {
                string ext = !string.IsNullOrEmpty(shot.ContentType) && shot.ContentType.Contains("png")
                    ? ".png" : ".jpeg";
                string filename = !string.IsNullOrEmpty(shot.Path)
                    ? Path.GetFileName(shot.Path)
                    : (shot.Label ?? "screenshot") + ext;
                File.WriteAllBytes(Path.Combine(pageDir, filename), shot.Data);
            }
        }
    } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"    [warn] Failed to persist artifacts for {page.Url}: {ex.Message}");
        Console.ResetColor();
    }
}

void WriteCrawlSummary(string outputDir, string host, string baseUrl,
    List<(string Path, int Violations, int Critical, int Serious)> stats,
    HashSet<string> visited, int maxDepth, TimeSpan duration, bool authenticated)
{
    try {
        string siteDir = Path.Combine(Path.GetFullPath(outputDir), host);
        Directory.CreateDirectory(siteDir);

        // ── Legacy markdown (kept for backwards compatibility) ──
        var md = new System.Text.StringBuilder();
        md.AppendLine($"# Crawl Summary — {host}");
        md.AppendLine();
        md.AppendLine($"- **Base URL:** {baseUrl}");
        md.AppendLine($"- **Authenticated:** {(authenticated ? "yes" : "no")}");
        md.AppendLine($"- **Pages scanned:** {stats.Count}");
        md.AppendLine($"- **Total paths discovered:** {visited.Count}");
        md.AppendLine($"- **Max depth:** {maxDepth}");
        md.AppendLine($"- **Duration:** {duration.TotalMinutes:F1} min");
        md.AppendLine($"- **Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine();
        md.AppendLine("## Per-page violation counts");
        md.AppendLine();
        md.AppendLine("| Path | Total | Critical | Serious |");
        md.AppendLine("|---|---:|---:|---:|");
        foreach (var (path, v, c, s) in stats.OrderByDescending(x => x.Violations)) {
            md.AppendLine($"| `{path}` | {v} | {c} | {s} |");
        }
        md.AppendLine();
        md.AppendLine("## All discovered paths (visited or queued)");
        md.AppendLine();
        foreach (string p in visited.OrderBy(x => x)) {
            md.AppendLine($"- `{p}`");
        }
        File.WriteAllText(Path.Combine(siteDir, "_crawl-summary.md"), md.ToString());

        // ── Rich interactive HTML report ──
        WriteCrawlSummaryHtml(siteDir, host, baseUrl, stats, visited, maxDepth, duration, authenticated);
    } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [warn] Failed to write crawl summary: {ex.Message}");
        Console.ResetColor();
    }
}

/// <summary>
/// Emits a single self-contained HTML report (Bootstrap 5 via CDN, no other external deps).
/// Works when opened from disk via file:// — image paths are relative and point at the
/// per-page directories that already exist alongside this file.
/// </summary>
static void WriteCrawlSummaryHtml(string siteDir, string host, string baseUrl,
    List<(string Path, int Violations, int Critical, int Serious)> stats,
    HashSet<string> visited, int maxDepth, TimeSpan duration, bool authenticated)
{
    int totalViolations = stats.Sum(x => x.Violations);
    int totalCritical = stats.Sum(x => x.Critical);
    int totalSerious = stats.Sum(x => x.Serious);

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("<!DOCTYPE html>");
    sb.AppendLine("<html lang=\"en\" data-bs-theme=\"light\">");
    sb.AppendLine("<head>");
    sb.AppendLine("  <meta charset=\"utf-8\">");
    sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
    sb.AppendLine($"  <title>Crawl Summary — {HtmlEnc(host)}</title>");
    sb.AppendLine("  <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css\">");
    sb.AppendLine("  <style>");
    sb.AppendLine("    body { padding-bottom: 3rem; }");
    sb.AppendLine("    .stat-card { border-left: 4px solid var(--bs-primary); }");
    sb.AppendLine("    .stat-card .stat-value { font-size: 2rem; font-weight: 600; line-height: 1; }");
    sb.AppendLine("    .stat-card .stat-label { font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--bs-secondary-color); }");
    sb.AppendLine("    .thumb { cursor: pointer; transition: transform 0.15s ease; }");
    sb.AppendLine("    .thumb:hover { transform: scale(1.02); }");
    sb.AppendLine("    .thumb img { width: 100%; height: 120px; object-fit: cover; object-position: top center; background: #f0f0f0; }");
    sb.AppendLine("    .thumb .thumb-label { font-size: 0.7rem; padding: 0.25rem 0.4rem; color: var(--bs-secondary-color); word-break: break-word; }");
    sb.AppendLine("    .violation-msg { white-space: pre-wrap; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.8rem; }");
    sb.AppendLine("    .msg-short { display: inline; }");
    sb.AppendLine("    .msg-full  { display: none; }");
    sb.AppendLine("    .msg-expanded .msg-short { display: none; }");
    sb.AppendLine("    .msg-expanded .msg-full  { display: inline; }");
    sb.AppendLine("    .sev-critical { background: #dc3545; color: #fff; }");
    sb.AppendLine("    .sev-serious  { background: #fd7e14; color: #fff; }");
    sb.AppendLine("    .sev-moderate { background: #ffc107; color: #000; }");
    sb.AppendLine("    .sev-minor    { background: #6c757d; color: #fff; }");
    sb.AppendLine("    .path-link { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; font-size: 0.85rem; }");
    sb.AppendLine("    [data-bs-theme='dark'] .thumb img { background: #2a2a2a; }");
    sb.AppendLine("    .lightbox-img { max-width: 100%; max-height: 85vh; object-fit: contain; }");
    sb.AppendLine("  </style>");
    sb.AppendLine("</head>");
    sb.AppendLine("<body>");

    // ── Header bar ──
    sb.AppendLine("<nav class=\"navbar navbar-expand sticky-top bg-body-tertiary border-bottom mb-4\">");
    sb.AppendLine("  <div class=\"container-fluid\">");
    sb.AppendLine($"    <span class=\"navbar-brand mb-0 h1\">Crawl Summary — <code>{HtmlEnc(host)}</code></span>");
    sb.AppendLine("    <button id=\"themeToggle\" class=\"btn btn-sm btn-outline-secondary ms-auto\" type=\"button\" aria-label=\"Toggle dark mode\">Dark mode</button>");
    sb.AppendLine("  </div>");
    sb.AppendLine("</nav>");

    sb.AppendLine("<div class=\"container-fluid\">");

    // ── Scan summary cards ──
    sb.AppendLine("<div class=\"row g-3 mb-4\">");
    AppendStatCard(sb, "Base URL", $"<a href=\"{HtmlEnc(baseUrl)}\" target=\"_blank\" rel=\"noopener\">{HtmlEnc(baseUrl)}</a>", "primary");
    AppendStatCard(sb, "Authenticated", authenticated ? "Yes" : "No", authenticated ? "success" : "secondary");
    AppendStatCard(sb, "Pages Scanned", stats.Count.ToString("N0"), "info");
    AppendStatCard(sb, "Paths Discovered", visited.Count.ToString("N0"), "info");
    AppendStatCard(sb, "Total Violations", totalViolations.ToString("N0"), totalViolations == 0 ? "success" : "warning");
    AppendStatCard(sb, "Critical", totalCritical.ToString("N0"), totalCritical == 0 ? "secondary" : "danger");
    AppendStatCard(sb, "Serious", totalSerious.ToString("N0"), totalSerious == 0 ? "secondary" : "warning");
    AppendStatCard(sb, "Duration", $"{duration.TotalMinutes:F1} min", "secondary");
    AppendStatCard(sb, "Max Depth", maxDepth.ToString(), "secondary");
    AppendStatCard(sb, "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "secondary");
    sb.AppendLine("</div>");

    // ── Per-page accordion ──
    sb.AppendLine("<h2 class=\"h4 mb-3\">Per-page results</h2>");
    sb.AppendLine("<div class=\"accordion mb-5\" id=\"pageAccordion\">");
    int idx = 0;
    foreach (var (path, v, c, s) in stats.OrderByDescending(x => x.Violations)) {
        idx++;
        string slug = SlugifyPath(path);
        string pageDir = Path.Combine(siteDir, slug);
        string anchorId = $"page-{idx}";
        string collapseId = $"collapse-{idx}";

        sb.AppendLine($"<div class=\"accordion-item\" id=\"{anchorId}\">");
        sb.AppendLine($"  <h2 class=\"accordion-header\">");
        sb.AppendLine($"    <button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#{collapseId}\" aria-expanded=\"false\" aria-controls=\"{collapseId}\">");
        sb.AppendLine($"      <div class=\"d-flex flex-wrap gap-2 align-items-center w-100\">");
        sb.AppendLine($"        <code class=\"path-link\">{HtmlEnc(path)}</code>");
        sb.AppendLine($"        <span class=\"badge bg-primary\">{v} total</span>");
        if (c > 0) sb.AppendLine($"        <span class=\"badge sev-critical\">{c} critical</span>");
        if (s > 0) sb.AppendLine($"        <span class=\"badge sev-serious\">{s} serious</span>");
        sb.AppendLine($"      </div>");
        sb.AppendLine($"    </button>");
        sb.AppendLine($"  </h2>");
        sb.AppendLine($"  <div id=\"{collapseId}\" class=\"accordion-collapse collapse\" data-bs-parent=\"#pageAccordion\">");
        sb.AppendLine($"    <div class=\"accordion-body\">");

        if (!Directory.Exists(pageDir)) {
            sb.AppendLine("<div class=\"alert alert-warning\">Page directory not found on disk.</div>");
        } else {
            // Screenshot gallery
            AppendScreenshotGallery(sb, pageDir, slug);

            // Violations table
            AppendViolationsTable(sb, pageDir, idx);

            // Artifact file links
            AppendArtifactLinks(sb, pageDir, slug);
        }

        sb.AppendLine($"    </div>");
        sb.AppendLine($"  </div>");
        sb.AppendLine($"</div>");
    }
    sb.AppendLine("</div>");

    // ── Discovered paths list ──
    sb.AppendLine("<h2 class=\"h4 mb-3\">All discovered paths</h2>");
    sb.AppendLine("<div class=\"card mb-4\"><div class=\"card-body\">");
    sb.AppendLine("<div class=\"row row-cols-1 row-cols-md-2 row-cols-lg-3 g-1\">");
    var scannedLookup = stats.Select((x, i) => new { x.Path, Idx = i }).ToDictionary(x => x.Path, x => x.Idx);
    var ordered = stats.OrderByDescending(x => x.Violations).Select((x, i) => new { x.Path, AnchorIdx = i + 1 })
        .ToDictionary(x => x.Path, x => x.AnchorIdx);
    foreach (string p in visited.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) {
        if (ordered.TryGetValue(p, out int anchorIdx)) {
            sb.AppendLine($"  <div class=\"col\"><a href=\"#page-{anchorIdx}\" class=\"path-link text-decoration-none\"><code>{HtmlEnc(p)}</code></a> <span class=\"badge bg-success\">scanned</span></div>");
        } else {
            sb.AppendLine($"  <div class=\"col\"><code class=\"path-link\">{HtmlEnc(p)}</code> <span class=\"badge bg-secondary\">queued</span></div>");
        }
    }
    sb.AppendLine("</div>");
    sb.AppendLine("</div></div>");

    sb.AppendLine("</div>"); // container

    // ── Lightbox modal ──
    sb.AppendLine("<div class=\"modal fade\" id=\"lightboxModal\" tabindex=\"-1\" aria-hidden=\"true\">");
    sb.AppendLine("  <div class=\"modal-dialog modal-xl modal-dialog-centered\">");
    sb.AppendLine("    <div class=\"modal-content\">");
    sb.AppendLine("      <div class=\"modal-header\">");
    sb.AppendLine("        <h5 class=\"modal-title\" id=\"lightboxLabel\">Screenshot</h5>");
    sb.AppendLine("        <button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"modal\" aria-label=\"Close\"></button>");
    sb.AppendLine("      </div>");
    sb.AppendLine("      <div class=\"modal-body text-center\">");
    sb.AppendLine("        <img id=\"lightboxImage\" class=\"lightbox-img\" src=\"\" alt=\"\">");
    sb.AppendLine("      </div>");
    sb.AppendLine("      <div class=\"modal-footer\">");
    sb.AppendLine("        <a id=\"lightboxOpen\" href=\"#\" target=\"_blank\" rel=\"noopener\" class=\"btn btn-sm btn-outline-primary\">Open in new tab</a>");
    sb.AppendLine("      </div>");
    sb.AppendLine("    </div>");
    sb.AppendLine("  </div>");
    sb.AppendLine("</div>");

    // ── Scripts ──
    sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js\"></script>");
    sb.AppendLine("<script>");
    sb.AppendLine("  (function () {");
    sb.AppendLine("    var modalEl = document.getElementById('lightboxModal');");
    sb.AppendLine("    var modal = new bootstrap.Modal(modalEl);");
    sb.AppendLine("    var img = document.getElementById('lightboxImage');");
    sb.AppendLine("    var label = document.getElementById('lightboxLabel');");
    sb.AppendLine("    var openLink = document.getElementById('lightboxOpen');");
    sb.AppendLine("    document.querySelectorAll('.thumb').forEach(function (t) {");
    sb.AppendLine("      t.addEventListener('click', function () {");
    sb.AppendLine("        var src = t.getAttribute('data-src');");
    sb.AppendLine("        var cap = t.getAttribute('data-label') || 'Screenshot';");
    sb.AppendLine("        img.src = src; label.textContent = cap; openLink.href = src;");
    sb.AppendLine("        modal.show();");
    sb.AppendLine("      });");
    sb.AppendLine("    });");
    sb.AppendLine("    document.querySelectorAll('.msg-toggle').forEach(function (b) {");
    sb.AppendLine("      b.addEventListener('click', function (e) {");
    sb.AppendLine("        e.preventDefault();");
    sb.AppendLine("        var cell = b.closest('.violation-msg');");
    sb.AppendLine("        if (cell) cell.classList.toggle('msg-expanded');");
    sb.AppendLine("      });");
    sb.AppendLine("    });");
    sb.AppendLine("    var tt = document.getElementById('themeToggle');");
    sb.AppendLine("    tt.addEventListener('click', function () {");
    sb.AppendLine("      var html = document.documentElement;");
    sb.AppendLine("      var next = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';");
    sb.AppendLine("      html.setAttribute('data-bs-theme', next);");
    sb.AppendLine("      tt.textContent = next === 'dark' ? 'Light mode' : 'Dark mode';");
    sb.AppendLine("    });");
    sb.AppendLine("  })();");
    sb.AppendLine("</script>");
    sb.AppendLine("</body></html>");

    File.WriteAllText(Path.Combine(siteDir, "_crawl-summary.html"), sb.ToString());
}

static void AppendStatCard(System.Text.StringBuilder sb, string label, string value, string colorClass)
{
    sb.AppendLine("<div class=\"col-6 col-md-4 col-lg-3 col-xl-2\">");
    sb.AppendLine($"  <div class=\"card stat-card h-100\" style=\"border-left-color: var(--bs-{colorClass});\">");
    sb.AppendLine("    <div class=\"card-body py-2\">");
    sb.AppendLine($"      <div class=\"stat-label\">{HtmlEnc(label)}</div>");
    sb.AppendLine($"      <div class=\"stat-value text-{colorClass}\">{value}</div>");
    sb.AppendLine("    </div>");
    sb.AppendLine("  </div>");
    sb.AppendLine("</div>");
}

static void AppendScreenshotGallery(System.Text.StringBuilder sb, string pageDir, string slug)
{
    string[] imageExts = new[] { ".png", ".jpeg", ".jpg", ".webp", ".gif" };
    var images = new DirectoryInfo(pageDir).GetFiles()
        .Where(f => imageExts.Contains(f.Extension.ToLowerInvariant()))
        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (images.Count == 0) {
        sb.AppendLine("<div class=\"text-muted small mb-3\">No screenshots captured.</div>");
        return;
    }

    // Group by category (mirrors FreeA11yChecker.App.ScreenshotGallery.razor)
    var groups = new Dictionary<string, List<FileInfo>> {
        ["Authentication"] = new(),
        ["Page Load Sequence"] = new(),
        ["Page Expanded"] = new(),
        ["Analysis Overlays"] = new(),
        ["Color Vision Simulation"] = new(),
        ["Screen Reader View"] = new(),
        ["Other"] = new(),
    };
    foreach (FileInfo f in images) {
        string n = f.Name.ToLowerInvariant();
        string cat =
            n.StartsWith("00") || n.Contains("login")        ? "Authentication" :
            n.StartsWith("01-page-load")                     ? "Page Load Sequence" :
            n.StartsWith("02-page-expanded")                 ? "Page Expanded" :
            (n.StartsWith("03-") || n.StartsWith("04-") || n.StartsWith("05-") ||
             n.StartsWith("06-") || n.StartsWith("07-")) && n.Contains("overlay")
                                                             ? "Analysis Overlays" :
            n.Contains("cvd-")                               ? "Color Vision Simulation" :
            n.Contains("screenreader")                       ? "Screen Reader View" :
                                                               "Other";
        groups[cat].Add(f);
    }

    sb.AppendLine("<h4 class=\"h5 mt-2\">Screenshots</h4>");
    foreach (var kv in groups) {
        if (kv.Value.Count == 0) continue;
        sb.AppendLine($"<h5 class=\"h6 mt-3 text-body-secondary\">{HtmlEnc(kv.Key)} <span class=\"badge bg-secondary\">{kv.Value.Count}</span></h5>");
        sb.AppendLine("<div class=\"row row-cols-2 row-cols-sm-3 row-cols-md-4 row-cols-lg-6 g-2 mb-3\">");
        foreach (FileInfo f in kv.Value) {
            string relPath = $"./{slug}/{f.Name}";
            string label = Path.GetFileNameWithoutExtension(f.Name);
            sb.AppendLine("  <div class=\"col\">");
            sb.AppendLine($"    <div class=\"card thumb h-100\" data-src=\"{HtmlEnc(relPath)}\" data-label=\"{HtmlEnc(label)}\" role=\"button\" tabindex=\"0\">");
            sb.AppendLine($"      <img src=\"{HtmlEnc(relPath)}\" alt=\"{HtmlEnc(label)}\" loading=\"lazy\">");
            sb.AppendLine($"      <div class=\"thumb-label\" title=\"{HtmlEnc(f.Name)}\">{HtmlEnc(label)}<br><span class=\"text-body-tertiary\">{FormatBytes(f.Length)}</span></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
        }
        sb.AppendLine("</div>");
    }
}

static void AppendViolationsTable(System.Text.StringBuilder sb, string pageDir, int pageIdx)
{
    // Try to parse every a11y-*.json as a flat array of issues.
    string[] jsonCandidates = new[] { "a11y-axe.json", "a11y-ibm.json", "a11y-htmlcheck.json", "a11y-htmlcs.json" };
    var rows = new List<(string Tool, string Severity, string RuleId, string Message, string HelpUrl, string Selector)>();
    foreach (string name in jsonCandidates) {
        string p = Path.Combine(pageDir, name);
        if (!File.Exists(p)) continue;
        try {
            string text = File.ReadAllText(p);
            if (string.IsNullOrWhiteSpace(text)) continue;
            using JsonDocument doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
            foreach (JsonElement el in doc.RootElement.EnumerateArray()) {
                string tool = el.TryGetProperty("Tool", out var t) ? t.GetString() ?? "" : "";
                string sev = el.TryGetProperty("Severity", out var s) ? s.GetString() ?? "" : "";
                string rule = el.TryGetProperty("RuleId", out var r) ? r.GetString() ?? "" : "";
                string msg = el.TryGetProperty("Message", out var m) ? m.GetString() ?? "" : "";
                string help = el.TryGetProperty("HelpUrl", out var h) ? h.GetString() ?? "" : "";
                string sel = el.TryGetProperty("Selector", out var se) ? se.GetString() ?? "" : "";
                rows.Add((tool, sev, rule, msg, help, sel));
            }
        } catch {
            // unparseable — skip; artifact link section below will still surface the file
        }
    }

    sb.AppendLine("<h4 class=\"h5 mt-3\">Violations <span class=\"badge bg-secondary\">" + rows.Count + "</span></h4>");
    if (rows.Count == 0) {
        sb.AppendLine("<div class=\"text-muted small mb-3\">No violations parsed from JSON artifacts.</div>");
        return;
    }

    // Sort by severity rank so worst issues float to the top.
    int SevRank(string s) => s?.ToLowerInvariant() switch {
        "critical" => 0, "serious" => 1, "moderate" => 2, "minor" => 3, _ => 4,
    };
    rows = rows.OrderBy(x => SevRank(x.Severity)).ThenBy(x => x.Tool).ThenBy(x => x.RuleId).ToList();

    sb.AppendLine("<div class=\"table-responsive mb-3\">");
    sb.AppendLine("<table class=\"table table-sm table-striped align-top\">");
    sb.AppendLine("<thead><tr><th style=\"width:80px\">Tool</th><th style=\"width:90px\">Severity</th><th style=\"width:180px\">Rule</th><th>Message</th></tr></thead><tbody>");
    int msgIdx = 0;
    foreach (var row in rows) {
        msgIdx++;
        string sevClass = row.Severity?.ToLowerInvariant() switch {
            "critical" => "sev-critical",
            "serious"  => "sev-serious",
            "moderate" => "sev-moderate",
            "minor"    => "sev-minor",
            _ => "bg-light text-dark",
        };
        string ruleCell = string.IsNullOrEmpty(row.HelpUrl)
            ? $"<code>{HtmlEnc(row.RuleId)}</code>"
            : $"<a href=\"{HtmlEnc(row.HelpUrl)}\" target=\"_blank\" rel=\"noopener\"><code>{HtmlEnc(row.RuleId)}</code></a>";
        string msg = row.Message ?? "";
        const int TRUNC = 200;
        string shortMsg, fullMsg;
        bool needsTrunc = msg.Length > TRUNC;
        if (needsTrunc) {
            shortMsg = HtmlEnc(msg.Substring(0, TRUNC)) + "…";
            fullMsg = HtmlEnc(msg);
        } else {
            shortMsg = HtmlEnc(msg);
            fullMsg = "";
        }
        sb.AppendLine("<tr>");
        sb.AppendLine($"  <td><span class=\"badge bg-dark\">{HtmlEnc(row.Tool)}</span></td>");
        sb.AppendLine($"  <td><span class=\"badge {sevClass}\">{HtmlEnc(row.Severity)}</span></td>");
        sb.AppendLine($"  <td>{ruleCell}</td>");
        sb.AppendLine($"  <td><div class=\"violation-msg\"><span class=\"msg-short\">{shortMsg}</span>");
        if (needsTrunc) {
            sb.AppendLine($"    <span class=\"msg-full\">{fullMsg}</span> <a href=\"#\" class=\"msg-toggle small ms-1\">toggle</a>");
        }
        if (!string.IsNullOrEmpty(row.Selector)) {
            sb.AppendLine($"    <div class=\"small text-body-secondary mt-1\">Selector: <code>{HtmlEnc(row.Selector)}</code></div>");
        }
        sb.AppendLine("  </div></td>");
        sb.AppendLine("</tr>");
    }
    sb.AppendLine("</tbody></table></div>");
}

static void AppendArtifactLinks(System.Text.StringBuilder sb, string pageDir, string slug)
{
    string[] imageExts = new[] { ".png", ".jpeg", ".jpg", ".webp", ".gif" };
    var artifacts = new DirectoryInfo(pageDir).GetFiles()
        .Where(f => !imageExts.Contains(f.Extension.ToLowerInvariant()))
        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (artifacts.Count == 0) return;

    sb.AppendLine("<h4 class=\"h5 mt-3\">Artifacts <span class=\"badge bg-secondary\">" + artifacts.Count + "</span></h4>");
    sb.AppendLine("<ul class=\"list-group list-group-flush mb-3\">");
    foreach (FileInfo f in artifacts) {
        string relPath = $"./{slug}/{f.Name}";
        sb.AppendLine("  <li class=\"list-group-item d-flex justify-content-between align-items-center px-0\">");
        sb.AppendLine($"    <a href=\"{HtmlEnc(relPath)}\" target=\"_blank\" rel=\"noopener\"><code>{HtmlEnc(f.Name)}</code></a>");
        sb.AppendLine($"    <span class=\"badge bg-light text-dark border\">{FormatBytes(f.Length)}</span>");
        sb.AppendLine("  </li>");
    }
    sb.AppendLine("</ul>");
}

static string FormatBytes(long bytes)
{
    if (bytes < 1024) return bytes + " B";
    if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F0") + " KB";
    return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
}

static string HtmlEnc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

static string PathFromUrl(string url)
{
    try { return new Uri(url).AbsolutePath; }
    catch { return "/"; }
}

static string SlugifyPath(string path)
{
    if (string.IsNullOrWhiteSpace(path) || path == "/") return "_root";
    string slug = path.Trim('/').Replace('/', '_');
    foreach (char c in Path.GetInvalidFileNameChars()) {
        slug = slug.Replace(c, '_');
    }
    return string.IsNullOrEmpty(slug) ? "_root" : slug;
}

async Task<int> RunScanFromArgs(ScanConfig config, string[] args)
{
    // See if a --url was passed on the command line.
    string? urlArg = GetArg(args, "--url");
    if (!string.IsNullOrEmpty(urlArg)) {
        string? pagesArg = GetArg(args, "--pages");
        string? userArg = GetArg(args, "--user");
        string? passArg = GetArg(args, "--pass");
        string? loginUrlArg = GetArg(args, "--login-url");
        string? userSelectorArg = GetArg(args, "--username-selector");
        string? passSelectorArg = GetArg(args, "--password-selector");
        string? submitSelectorArg = GetArg(args, "--submit-selector");

        List<PageConfig> pageConfigs = string.IsNullOrEmpty(pagesArg)
            ? [new PageConfig { Path = "/" }]
            : pagesArg.Split(',').Select(p => new PageConfig { Path = p.Trim() }).ToList();

        // Build credentials: explicit CLI args win; otherwise fall back to user-secrets
        // for known sites (Flex). Selectors are tuned for FlexCRM's Razor login form
        // (button is type="button" with @onclick — generic submit-selector defaults miss it).
        CredentialConfig? creds = ResolveCredentials(urlArg, userArg, passArg, configuration);

        // Override with any explicit CLI selector / login-url args.
        if (creds != null) {
            if (!string.IsNullOrEmpty(loginUrlArg)) creds.LoginUrl = loginUrlArg;
            if (!string.IsNullOrEmpty(userSelectorArg)) creds.UsernameSelector = userSelectorArg;
            if (!string.IsNullOrEmpty(passSelectorArg)) creds.PasswordSelector = passSelectorArg;
            if (!string.IsNullOrEmpty(submitSelectorArg)) creds.SubmitSelector = submitSelectorArg;
        }

        config = new ScanConfig {
            SettleDelayMs = config.SettleDelayMs,
            TimeoutMs = config.TimeoutMs,
            Headless = config.Headless,
            OutputDir = config.OutputDir,
            WcagLevel = config.WcagLevel,
            MaxConcurrency = config.MaxConcurrency,
            Sites = new Dictionary<string, SiteConfig> {
                [urlArg] = new SiteConfig {
                    BaseUrl = urlArg,
                    Pages = pageConfigs,
                    Credentials = creds
                }
            }
        };
    }

    return await RunScan(config);
}

/// <summary>
/// Resolves credentials for a target URL. Priority:
///   1. Explicit --user/--pass CLI args (if both provided)
///   2. User-secrets / appsettings entries for known sites:
///        flex.em.wsu.edu*  -> App:FlexAdminUsername / App:FlexAdminPassword
/// Returns null when nothing is available, signalling an unauthenticated scan.
/// </summary>
CredentialConfig? ResolveCredentials(string url, string? userArg, string? passArg, IConfiguration cfg)
{
    if (!string.IsNullOrEmpty(userArg)) {
        return new CredentialConfig {
            Username = userArg,
            Password = passArg ?? "",
            LoginUrl = url,
            TenantCode = "SFS", // default tenant for WSU testing — AuthHandler picks the row
            // Generic defaults — known to work for most Bootstrap-styled login forms.
            UsernameSelector = "#login-email, input[type='email'], input[name='username']",
            PasswordSelector = "#login-password, input[type='password']",
            SubmitSelector = "button.btn-primary, button[type='submit']",
        };
    }

    // Flex (and its /Touchpoints sub-app on the same origin) — read from user-secrets.
    bool isFlex = url.StartsWith("https://flex.em.wsu.edu", StringComparison.OrdinalIgnoreCase);
    if (isFlex) {
        string? user = cfg["App:FlexAdminUsername"];
        string? pass = cfg["App:FlexAdminPassword"];
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass)) {
            return new CredentialConfig {
                Username = user,
                Password = pass,
                // FlexCRM 3-step login flow: tenant chooser → provider chooser → email+pwd.
                // TenantCode = "SFS" tells AuthHandler to click the [SFS] row in step 1.
                // Then AuthHandler clicks "Log in with a Local Account" in step 2 (admin
                // is local; Okta is for real users and isn't worth automating).
                LoginUrl = "https://flex.em.wsu.edu/Login",
                TenantCode = "SFS",
                UsernameSelector = "#login-email",
                PasswordSelector = "#login-password",
                // Flex's button is type="button" with @onclick — generic selectors like
                // button[type='submit'] won't match. Target it by class.
                SubmitSelector = "button.btn-primary",
            };
        }
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  [auth] Flex URL detected but no App:FlexAdminUsername/Password in user-secrets — running unauthenticated.");
        Console.ResetColor();
    }

    return null;
}

// ================================================================
// Scan Runner (delegates to ScannerEngine)
// ================================================================

static async Task<int> RunScan(ScanConfig config)
{
    if (!config.Sites.Any()) {
        WriteError("No sites configured. Add sites to appsettings.json or use --url.");
        return 1;
    }

    Console.WriteLine($"  Sites:       {config.Sites.Count}");
    Console.WriteLine($"  Output:      {Path.GetFullPath(config.OutputDir)}");
    Console.WriteLine($"  Settle:      {config.SettleDelayMs}ms");
    Console.WriteLine($"  Headless:    {config.Headless}");
    Console.WriteLine($"  WCAG Level:  {config.WcagLevel}");
    Console.WriteLine();

    Stopwatch sw = Stopwatch.StartNew();

    // Delegate the full scan to the shared Scanner library. Per-step coloring is
    // handled by ColorOut.Auto which infers severity from message text (red for
    // errors, yellow for warnings, green for success, cyan for steps, gray for detail).
    // OnPageComplete writes artifacts to disk in real time so Ctrl+C preserves data.
    RunScanResult result = await ScannerEngine.ScanAll(config,
        progress => {
            FreeA11yChecker.Console.ColorOut.Auto($"  [{progress.CurrentPage}/{progress.TotalPages}] {progress.Message}");
        },
        (host, page) => {
            WritePageArtifactsToDisk(config.OutputDir, host, page);
        });

    sw.Stop();

    // Persist artifacts + screenshots to disk for every page scanned. Mirrors the
    // RunCrawl behavior — the shared ScannerEngine builds artifacts in memory and
    // hands them back; the console caller is responsible for disk persistence.
    // Per-page artifacts already written in real time via OnPageComplete callback above.
    // Just compute totals for the summary line.
    int totalArtifacts = result.Sites.Values
        .SelectMany(s => s.Pages)
        .Sum(p => p.Artifacts.Count + p.Screenshots.Count);

    // Print summary.
    Console.WriteLine();
    Console.ForegroundColor = result.Sites.Values.Sum(s => s.Pages.Sum(p => (p.AxeIssues.Count + p.HtmlCheckIssues.Count + p.HtmlCsIssues.Count + p.IbmIssues.Count))) == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
    Console.WriteLine($"  Done. {result.Sites.Values.Sum(s => s.Pages.Count)} pages, {result.Sites.Values.Sum(s => s.Pages.Sum(p => (p.AxeIssues.Count + p.HtmlCheckIssues.Count + p.HtmlCsIssues.Count + p.IbmIssues.Count)))} violations, {sw.Elapsed.TotalSeconds:F1}s, {totalArtifacts} files written");
    Console.ResetColor();
    Console.WriteLine($"  Results: {Path.GetFullPath(config.OutputDir)}");

    // Print per-site breakdown.
    if (result.Sites.Values.ToList().Any()) {
        Console.WriteLine();
        Console.WriteLine("  Per-site summary:");
        foreach (SiteScanResult siteResult in result.Sites.Values.ToList()) {
            Console.ForegroundColor = siteResult.Pages.Sum(p => (p.AxeIssues.Count + p.HtmlCheckIssues.Count + p.HtmlCsIssues.Count + p.IbmIssues.Count)) == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.Write($"    {siteResult.Pages.Sum(p => (p.AxeIssues.Count + p.HtmlCheckIssues.Count + p.HtmlCsIssues.Count + p.IbmIssues.Count)),4} violations  ");
            Console.ResetColor();
            Console.WriteLine($"{siteResult.BaseUrl} ({siteResult.Pages.Count} pages)");
        }
    }

    // AUTO-HANDOFF: emit the AI-fix-pack for every scanned host. Same artifact as the
    // standalone `handoff` command — runs against the per-page JSONs we just wrote.
    foreach (SiteScanResult siteResult in result.Sites.Values) {
        string siteHost = "unknown";
        try { siteHost = new Uri(siteResult.BaseUrl).Host; } catch { }
        try {
            string? srcForHandoff = sourceCodePath;
            string handoffPath = FreeA11yChecker.Console.SourceAnalysis.AiHandoffReport.Generate(
                config.OutputDir, siteHost, srcForHandoff);
            FreeA11yChecker.Console.ColorOut.Success($"  AI handoff for {siteHost}: {handoffPath}");
        } catch (Exception ex) {
            FreeA11yChecker.Console.ColorOut.Warn($"  Handoff doc skipped for {siteHost}: {ex.Message}");
        }
    }

    return result.Sites.Values.Sum(s => s.Pages.Sum(p => (p.AxeIssues.Count + p.HtmlCheckIssues.Count + p.HtmlCsIssues.Count + p.IbmIssues.Count))) > 0 ? 1 : 0;
}

// ================================================================
// View Last Results
// ================================================================

static int ShowLastResults(string outputDir)
{
    string dir = Path.GetFullPath(outputDir);
    if (!Directory.Exists(dir)) { WriteError($"No results at {dir}"); return 1; }

    string[] summaries = Directory.GetFiles(dir, "a11y-summary.json", SearchOption.AllDirectories);
    Console.WriteLine($"\n  Last scan: {summaries.Length} pages\n");

    int totalViolations = 0;
    foreach (string file in summaries.OrderBy(f => f)) {
        string json = File.ReadAllText(file);
        using JsonDocument doc = JsonDocument.Parse(json);
        string url = doc.RootElement.TryGetProperty("Url", out JsonElement u) ? u.GetString() ?? "" : Path.GetDirectoryName(file) ?? "";
        int count = doc.RootElement.TryGetProperty("TotalViolations", out JsonElement v) ? v.GetInt32() : 0;
        totalViolations += count;

        Console.ForegroundColor = count == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.Write($"  {count,3} violations  ");
        Console.ResetColor();
        Console.WriteLine(url);
    }

    Console.WriteLine($"\n  Total: {totalViolations} violations across {summaries.Length} pages");
    return 0;
}

// ================================================================
// Help
// ================================================================

static int ShowHelp()
{
    Console.WriteLine("""
      Usage:
        FreeA11yChecker.Console                             Interactive menu
        FreeA11yChecker.Console scan                        Scan from appsettings.json
        FreeA11yChecker.Console scan --url https://x.com    Quick scan
        FreeA11yChecker.Console scan --url https://x.com --pages /,/about
        FreeA11yChecker.Console scan --url https://x.com --user admin --pass admin
        FreeA11yChecker.Console report                      Show last results
        FreeA11yChecker.Console analyze-source --path <folder>  Static source-code a11y scan
        FreeA11yChecker.Console help                        This message

      Configuration:
        Edit appsettings.json to add sites, pages, and credentials.
        CLI args override appsettings.json values.

      Engines:
        axe-core     WCAG 2.0/2.1/2.2 (downloaded from CDN on first run)
        htmlcheck    20 regex-based HTML pattern checks
        quickpeek    QuickPeek DOM overlay for visual review

      Output:
        runs/latest/{site}/{page}/
          01-page-loaded.png     Screenshot
          a11y-axe.json          axe-core results
          a11y-htmlcheck.json    htmlcheck results
          a11y-summary.json      Combined summary
          page.html              Captured HTML
          report.md              Human-readable report
    """);
    return 0;
}

// ================================================================
// Helpers
// ================================================================

static void PrintBanner()
{
    Console.WriteLine();
    Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
    Console.WriteLine("  ║  FreeA11yChecker Console Scanner v2.0           ║");
    Console.WriteLine("  ║  WCAG 2.2 AA Accessibility Testing             ║");
    Console.WriteLine("  ║  Powered by FreeA11yChecker.Scanner             ║");
    Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
    Console.WriteLine();
}

static ScanConfig LoadConfig(IConfiguration configuration)
{
    sourceCodePath = configuration["App:SourceCode"];
    gitRepoUrl = configuration["App:GitRepoUrl"];
    ScanConfig config = new ScanConfig();
    IConfigurationSection scanner = configuration.GetSection("Scanner");

    if (int.TryParse(scanner["SettleDelayMs"], out int settle)) config.SettleDelayMs = settle;
    if (int.TryParse(scanner["TimeoutMs"], out int timeout)) config.TimeoutMs = timeout;
    if (int.TryParse(scanner["MaxConcurrency"], out int concurrency)) config.MaxConcurrency = concurrency;
    if (bool.TryParse(scanner["Headless"], out bool headless)) config.Headless = headless;
    if (!string.IsNullOrEmpty(scanner["OutputDir"])) config.OutputDir = scanner["OutputDir"]!;
    if (!string.IsNullOrEmpty(scanner["WcagLevel"])) config.WcagLevel = scanner["WcagLevel"]!;

    IConfigurationSection sites = scanner.GetSection("Sites");
    foreach (IConfigurationSection site in sites.GetChildren()) {
        // IConfiguration splits ':' in keys, so "https://localhost:7046/" becomes
        // a nested section "https" -> "//localhost" -> "7046/". Reconstruct from BaseUrl
        // if present, otherwise fall back to the raw key.
        string baseUrl = site["BaseUrl"] ?? site.Key;
        SiteConfig siteConfig = new SiteConfig {
            BaseUrl = baseUrl
        };

        IConfigurationSection pages = site.GetSection("Pages");
        foreach (IConfigurationSection p in pages.GetChildren()) {
            string? val = p.Value;
            if (!string.IsNullOrEmpty(val)) {
                siteConfig.Pages.Add(new PageConfig { Path = val });
            }
        }

        IConfigurationSection creds = site.GetSection("Credentials");
        IConfigurationSection? firstCred = creds.GetChildren().FirstOrDefault();
        if (firstCred != null) {
            siteConfig.Credentials = new CredentialConfig {
                Username = firstCred["Username"] ?? "",
                Password = firstCred["Password"] ?? "",
                AuthType = firstCred["AuthType"] ?? "Generic",
                TenantCode = firstCred["TenantCode"] ?? "",
                LoginUrl = firstCred["LoginUrl"] ?? "",
                UsernameSelector = firstCred["UsernameSelector"] ?? "",
                PasswordSelector = firstCred["PasswordSelector"] ?? "",
                SubmitSelector = firstCred["SubmitSelector"] ?? ""
            };
        }

        config.Sites[baseUrl] = siteConfig;
    }

    return config;
}

static string? GetArg(string[] args, string flag)
{
    int idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static void WriteError(string msg)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  [ERROR] {msg}");
    Console.ResetColor();
}

static int RunHandoff(ScanConfig config, string[] args)
{
    string? host = GetArg(args, "--host");
    if (string.IsNullOrWhiteSpace(host)) {
        WriteError("handoff needs --host <hostname> (e.g. flex.em.wsu.edu)");
        return 1;
    }
    string? srcPath = GetArg(args, "--source-path") ?? sourceCodePath;
    if (!string.IsNullOrEmpty(srcPath) && !Directory.Exists(srcPath)) {
        FreeA11yChecker.Console.ColorOut.Warn($"Source path does not exist; report will skip source cross-references: {srcPath}");
        srcPath = null;
    }
    try {
        FreeA11yChecker.Console.ColorOut.Step($"Generating AI-handoff report for {host}...");
        if (!string.IsNullOrEmpty(srcPath)) {
            FreeA11yChecker.Console.ColorOut.Step($"  cross-referencing against source: {srcPath}");
        }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string outPath = FreeA11yChecker.Console.SourceAnalysis.AiHandoffReport.Generate(config.OutputDir, host, srcPath);
        sw.Stop();
        FreeA11yChecker.Console.ColorOut.Success($"Handoff doc written in {sw.Elapsed.TotalSeconds:F1}s: {outPath}");
        return 0;
    } catch (Exception ex) {
        WriteError($"handoff failed: {ex.Message}");
        return 1;
    }
}

static int RunAnalyzeSourceStub(string[] args)
{
    string? path = GetArg(args, "--path") ?? sourceCodePath;
    if (string.IsNullOrWhiteSpace(path)) {
        WriteError("analyze-source needs --path <folder> or App:SourceCode in config");
        return 1;
    }
    if (!Directory.Exists(path)) { WriteError($"Source path not found: {path}"); return 1; }
    string root = Path.GetFullPath(path);
    string outDir = GetArg(args, "--out") ?? Path.Combine("runs", "latest", "_source-analysis");
    Console.WriteLine($"  Source root: {root}");
    Console.WriteLine($"  Output:      {Path.GetFullPath(outDir)}");
    Console.WriteLine();
    Console.WriteLine("  Running static a11y scanners...");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    string reportPath = FreeA11yChecker.Console.SourceAnalysis.SourceAnalysisReport.Run(root, outDir);
    sw.Stop();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Done in {sw.Elapsed.TotalSeconds:F1}s. Report: {reportPath}");
    Console.ResetColor();
    return 0;
}

partial class Program
{
    static string? sourceCodePath;
    static string? gitRepoUrl;
}
