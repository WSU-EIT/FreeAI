using System.Net;
using System.Text;
using System.Text.Json;

namespace FreeA11yChecker.Console.SourceAnalysis;

/// <summary>
/// Builds a single AI-feedable markdown file (`_ai-handoff.md`) per scanned host.
/// Aggregates every per-page violation across all 4 tools (axe, IBM, htmlcs, htmlcheck),
/// filters known-noise IBM PASS-style messages at report-time (the live scan still has
/// the pre-fix data on disk), groups by rule, attaches a fix hint per rule (from
/// QuickFixHints), and — if --source-path was provided — runs SourceMatcher on each
/// occurrence to attach probable .razor file:line locations.
///
/// Output is a single markdown doc designed to be pasted into a coding AI alongside
/// "Fix all of these in &lt;source-path&gt;". Doc is ordered for highest-impact-first
/// fixing: site-wide rules first (those appearing on ALL pages — likely shared
/// MainLayout / NavMenu), then per-page issues.
/// </summary>
public static class AiHandoffReport
{
    public static string Generate(string scanRootDir, string host, string? sourcePath = null)
    {
        string hostDir = Path.Combine(Path.GetFullPath(scanRootDir), host);
        if (!Directory.Exists(hostDir)) throw new DirectoryNotFoundException($"Scan host folder not found: {hostDir}");

        // Discover per-page folders (each has a11y-*.json files).
        var pageDirs = Directory.GetDirectories(hostDir)
            .Where(d => Path.GetFileName(d) != "_logs")
            .OrderBy(d => d)
            .ToList();

        // Aggregate every violation.
        var allViolations = new List<RawViolation>();
        foreach (string pageDir in pageDirs) {
            string pageSlug = Path.GetFileName(pageDir);
            foreach (string toolFile in Directory.GetFiles(pageDir, "a11y-*.json")) {
                string tool = Path.GetFileNameWithoutExtension(toolFile).Replace("a11y-", "");
                if (tool == "summary") continue; // skip aggregate, we re-aggregate ourselves
                try {
                    string json = File.ReadAllText(toolFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                    foreach (var el in doc.RootElement.EnumerateArray()) {
                        var v = ParseViolation(el, tool, pageSlug);
                        if (v != null) allViolations.Add(v);
                    }
                } catch { /* skip unparseable file */ }
            }
        }

        int totalRaw = allViolations.Count;

        // Filter IBM noise — pre-fix scans recorded PASS-style messages as "minor"
        // because of the value-array index bug. Identify by exact message AND keyword
        // heuristics. The most common pass-output is the literal "Rule Passed".
        var noiseExactMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Rule Passed", "Rule passed", "rule passed",
        };
        var noiseKeywords = new[] {
            " has a valid ", " conforms to ", " detected as ", " has a unique ",
            " is correctly ", " provides ", " is appropriate ", "appears to be valid",
            "Element with valid ", " has appropriate ", " has a description ",
        };
        int filteredOut = 0;
        allViolations = allViolations.Where(v => {
            if (v.Tool != "ibm" || v.Severity != "minor") return true;
            if (noiseExactMessages.Contains(v.Message?.Trim() ?? "")) { filteredOut++; return false; }
            string msg = " " + (v.Message ?? "") + " ";
            if (noiseKeywords.Any(kw => msg.Contains(kw, StringComparison.OrdinalIgnoreCase))) {
                filteredOut++;
                return false;
            }
            return true;
        }).ToList();

        // Group by rule. Rule key = Tool + RuleId (so axe.image-alt and ibm.image-alt
        // are separate, since their fixes might differ).
        var ruleGroups = allViolations
            .GroupBy(v => v.Tool + "::" + v.RuleId)
            .Select(g => new RuleGroup {
                Tool = g.First().Tool,
                RuleId = g.First().RuleId,
                Severity = MaxSeverity(g.Select(v => v.Severity)),
                Occurrences = g.ToList(),
                PagesAffected = g.Select(v => v.PageSlug).Distinct().Count(),
                FixHint = QuickFixHints.GetHint(g.First().RuleId),
                HelpUrl = g.Select(v => v.HelpUrl).FirstOrDefault(u => !string.IsNullOrEmpty(u)),
            })
            .OrderBy(rg => SeverityRank(rg.Severity))
            .ThenByDescending(rg => rg.PagesAffected)
            .ThenByDescending(rg => rg.Occurrences.Count)
            .ToList();

        int distinctRules = ruleGroups.Count;
        int sitewideRules = ruleGroups.Count(rg => rg.PagesAffected >= pageDirs.Count * 0.7);

        // Aggregate per-tool pass/total across all pages — the "% green" the user
        // wants visible alongside the violations. Reads each page's a11y-summary.json
        // (which the scanner writes per page) for AxePassCount, IbmPassCount,
        // HtmlCheckPassCount, HtmlCheckTotalChecks, HtmlCsPassCount, HtmlCsTotalChecks.
        var toolStats = new Dictionary<string, (int pass, int total)> {
            ["axe"] = (0, 0), ["ibm"] = (0, 0), ["htmlcheck"] = (0, 0), ["htmlcs"] = (0, 0),
        };
        foreach (string pageDir in pageDirs) {
            string summaryFile = Path.Combine(pageDir, "a11y-summary.json");
            if (!File.Exists(summaryFile)) continue;
            try {
                using var d = JsonDocument.Parse(File.ReadAllText(summaryFile));
                int axePass = GetIntProp(d.RootElement, "AxePassCount");
                int axeIncomplete = GetIntProp(d.RootElement, "AxeIncompleteCount");
                int axeInapplicable = GetIntProp(d.RootElement, "AxeInapplicableCount");
                int axeIssues = GetIntProp(d.RootElement, "AxeViolationCount");
                toolStats["axe"] = (toolStats["axe"].pass + axePass, toolStats["axe"].total + axePass + axeIncomplete + axeInapplicable + axeIssues);

                int ibmPass = GetIntProp(d.RootElement, "IbmPassCount");
                int ibmPotential = GetIntProp(d.RootElement, "IbmPotentialCount");
                int ibmManual = GetIntProp(d.RootElement, "IbmManualCount");
                int ibmIssues = allViolations.Count(v => v.Tool == "ibm" && v.PageSlug == Path.GetFileName(pageDir));
                toolStats["ibm"] = (toolStats["ibm"].pass + ibmPass, toolStats["ibm"].total + ibmPass + ibmPotential + ibmManual + ibmIssues);

                int hcPass = GetIntProp(d.RootElement, "HtmlCheckPassCount");
                int hcTotal = GetIntProp(d.RootElement, "HtmlCheckTotalChecks");
                toolStats["htmlcheck"] = (toolStats["htmlcheck"].pass + hcPass, toolStats["htmlcheck"].total + hcTotal);

                int hcsPass = GetIntProp(d.RootElement, "HtmlCsPassCount");
                int hcsTotal = GetIntProp(d.RootElement, "HtmlCsTotalChecks");
                toolStats["htmlcs"] = (toolStats["htmlcs"].pass + hcsPass, toolStats["htmlcs"].total + hcsTotal);
            } catch { /* skip unparseable */ }
        }
        int overallPass = toolStats.Values.Sum(s => s.pass);
        int overallTotal = toolStats.Values.Sum(s => s.total);
        double overallPct = overallTotal > 0 ? (overallPass * 100.0 / overallTotal) : 0;

        // Coverage gap detection: count routes from source that we DIDN'T scan.
        // Static routes we missed get listed as "you should re-run with these in --pages".
        // Parameterized routes get listed as "needs --seed-ids file".
        var sourceRoutes = new List<string>();
        var paramRoutes = new List<string>();
        if (!string.IsNullOrEmpty(sourcePath) && Directory.Exists(sourcePath)) {
            try {
                var inv = CodebaseInventory.Analyze(sourcePath);
                foreach (string r in inv.BlazorRoutes) {
                    string clean = r.StartsWith("/") ? r : "/" + r;
                    if (clean.Contains('{') || clean.Contains('*')) paramRoutes.Add(clean);
                    else sourceRoutes.Add(clean);
                }
            } catch { /* leave empty */ }
        }
        var scannedSlugs = new HashSet<string>(pageDirs.Select(d => Path.GetFileName(d)), StringComparer.OrdinalIgnoreCase);
        // Compare scanned slugs (e.g. "Settings_Users") to source routes (e.g. "/settings/users")
        // by normalizing both: lowercase, strip leading slash, replace / with _.
        string SlugifyRoute(string r) {
            string s = r.TrimStart('/').ToLowerInvariant().Replace('/', '_');
            return string.IsNullOrEmpty(s) ? "_root" : s;
        }
        var scannedSlugsLower = new HashSet<string>(scannedSlugs.Select(s => s.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);
        var missedStaticRoutes = sourceRoutes
            .Where(r => !scannedSlugsLower.Contains(SlugifyRoute(r)))
            .OrderBy(r => r)
            .ToList();

        // Build the markdown.
        var sb = new StringBuilder();
        sb.AppendLine($"# Accessibility Fix Pack — {host}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}  ");
        sb.AppendLine($"**Source root:** {(sourcePath ?? "*(not provided — no source cross-references)*")}  ");
        sb.AppendLine($"**Scan output:** `{hostDir}`  ");
        sb.AppendLine();
        sb.AppendLine($"## Summary");
        sb.AppendLine();
        sb.AppendLine($"- **{pageDirs.Count}** pages scanned");
        sb.AppendLine($"- **{totalRaw}** raw violations across all 4 tools");
        sb.AppendLine($"- **{filteredOut}** IBM PASS-style noise items filtered out at report time");
        sb.AppendLine($"- **{allViolations.Count}** real violations remaining");
        sb.AppendLine($"- **{distinctRules}** distinct rule failures");
        sb.AppendLine($"- **{sitewideRules}** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)");
        sb.AppendLine();
        sb.AppendLine($"## ✅ What's working — pass rate per tool");
        sb.AppendLine();
        sb.AppendLine($"Aggregate across all {pageDirs.Count} scanned pages. **Overall pass rate: {overallPct:F1}%** ({overallPass:N0} of {overallTotal:N0} rule checks passed).");
        sb.AppendLine();
        sb.AppendLine($"| Tool | Rules passed | Total checks | Pass rate |");
        sb.AppendLine($"|------|-------------:|-------------:|----------:|");
        foreach (var kv in toolStats) {
            double pct = kv.Value.total > 0 ? (kv.Value.pass * 100.0 / kv.Value.total) : 0;
            string bar = pct >= 95 ? "🟢" : pct >= 80 ? "🟡" : pct >= 50 ? "🟠" : "🔴";
            sb.AppendLine($"| **{kv.Key}** | {kv.Value.pass:N0} | {kv.Value.total:N0} | {bar} {pct:F1}% |");
        }
        sb.AppendLine();
        sb.AppendLine("> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.");
        sb.AppendLine();

        // Coverage gap section.
        if (sourceRoutes.Count > 0 || paramRoutes.Count > 0) {
            int totalSourceRoutes = sourceRoutes.Count + paramRoutes.Count;
            int scannedFromSource = sourceRoutes.Count - missedStaticRoutes.Count;
            double covPct = totalSourceRoutes > 0 ? (scannedFromSource * 100.0 / totalSourceRoutes) : 0;
            sb.AppendLine($"## 📊 Page coverage vs source");
            sb.AppendLine();
            sb.AppendLine($"Source declares **{totalSourceRoutes}** routes. We scanned **{pageDirs.Count}** pages " +
                $"({scannedFromSource} of {sourceRoutes.Count} static routes = **{covPct:F0}% static coverage**).");
            sb.AppendLine();
            if (missedStaticRoutes.Count > 0) {
                sb.AppendLine($"### Static routes we MISSED ({missedStaticRoutes.Count})");
                sb.AppendLine();
                sb.AppendLine("These routes exist in the source but weren't scanned. Re-run with `--pages` to add them, " +
                    "or fix link discovery so they're auto-found:");
                sb.AppendLine();
                foreach (string r in missedStaticRoutes) sb.AppendLine($"- `{r}`");
                sb.AppendLine();
                sb.AppendLine($"Quick re-run command:");
                sb.AppendLine("```");
                sb.AppendLine($"--pages \"{string.Join(",", missedStaticRoutes)}\"");
                sb.AppendLine("```");
                sb.AppendLine();
            }
            if (paramRoutes.Count > 0) {
                sb.AppendLine($"### Parameterized routes that need IDs ({paramRoutes.Count})");
                sb.AppendLine();
                sb.AppendLine("These Edit/Detail pages can't be scanned without real record IDs from the database. " +
                    "Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link " +
                    "extractor can't find them automatically. Get a sample ID for each from the running app, save to a " +
                    "JSON file, and pass via `--seed-ids`:");
                sb.AppendLine();
                foreach (string r in paramRoutes) sb.AppendLine($"- `{r}`");
                sb.AppendLine();
                sb.AppendLine("Example `seed-ids.json`:");
                sb.AppendLine("```json");
                sb.AppendLine("{");
                for (int i = 0; i < paramRoutes.Count; i++) {
                    string suffix = i < paramRoutes.Count - 1 ? "," : "";
                    sb.AppendLine($"  \"{paramRoutes[i]}\": [\"REPLACE-WITH-REAL-ID\"]{suffix}");
                }
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("Run with: `crawl --url ... --seed-ids seed-ids.json`");
                sb.AppendLine();
            }
        }

        // Suggested fix order — site-wide rules first.
        sb.AppendLine($"## Suggested fix order (highest impact first)");
        sb.AppendLine();
        sb.AppendLine("Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.");
        sb.AppendLine();
        sb.AppendLine("| # | Rule | Severity | Pages | Occurrences | Likely scope |");
        sb.AppendLine("|---|------|----------|-------|-------------|--------------|");
        int rank = 1;
        foreach (var rg in ruleGroups.Take(40)) {
            string scope = rg.PagesAffected >= pageDirs.Count * 0.7 ? "site-wide (layout)"
                : rg.PagesAffected > 1 ? "shared component"
                : "single page";
            sb.AppendLine($"| {rank++} | `{HtmlEnc(rg.RuleId)}` ({rg.Tool}) | {SeverityBadge(rg.Severity)} | {rg.PagesAffected}/{pageDirs.Count} | {rg.Occurrences.Count} | {scope} |");
        }
        sb.AppendLine();

        // Per-rule detail with fix hint + occurrences + source-cross-references.
        sb.AppendLine($"## Per-rule fix instructions");
        sb.AppendLine();

        foreach (var rg in ruleGroups) {
            sb.AppendLine($"### {SeverityIcon(rg.Severity)} `{rg.RuleId}` ({rg.Tool}) — {rg.Severity.ToUpper()}");
            sb.AppendLine();
            sb.AppendLine($"- **Pages affected:** {rg.PagesAffected} of {pageDirs.Count}");
            sb.AppendLine($"- **Total occurrences:** {rg.Occurrences.Count}");
            if (!string.IsNullOrEmpty(rg.FixHint)) {
                sb.AppendLine($"- **How to fix:** {rg.FixHint}");
            }
            if (!string.IsNullOrEmpty(rg.HelpUrl)) {
                sb.AppendLine($"- **Reference:** <{rg.HelpUrl}>");
            }
            sb.AppendLine();

            // Show 1 representative occurrence with full context.
            var exemplar = rg.Occurrences.First();
            sb.AppendLine($"**Sample violation:**");
            sb.AppendLine();
            sb.AppendLine($"- Page: `/{exemplar.PageSlug.Replace("_root", "").Replace("_", "/")}`");
            sb.AppendLine($"- Selector: `{exemplar.Selector}`");
            sb.AppendLine($"- Message: {exemplar.Message}");
            sb.AppendLine();
            sb.AppendLine("```html");
            sb.AppendLine(Truncate(exemplar.Snippet, 400));
            sb.AppendLine("```");
            sb.AppendLine();

            // Source cross-reference for the exemplar (if --source-path was provided).
            if (!string.IsNullOrEmpty(sourcePath)) {
                var candidates = SourceMatcher.FindCandidates(sourcePath, exemplar.Snippet, maxCandidates: 3);
                if (candidates.Count > 0) {
                    sb.AppendLine("**Likely source location(s)** (highest confidence first):");
                    sb.AppendLine();
                    foreach (var c in candidates) {
                        sb.AppendLine($"- `{c.FilePath}:{c.LineNumber}` — matched on {c.MatchedToken}, {c.MatchCount} hit(s) — confidence: **{c.Confidence}**");
                    }
                    sb.AppendLine();
                }
            }

            // List all OTHER pages affected by this rule (compact).
            if (rg.Occurrences.Count > 1) {
                sb.AppendLine($"**All affected pages** ({rg.Occurrences.Count} total):");
                sb.AppendLine();
                foreach (var pageGroup in rg.Occurrences.GroupBy(o => o.PageSlug)) {
                    sb.AppendLine($"- `/{pageGroup.Key.Replace("_root", "").Replace("_", "/")}` — {pageGroup.Count()} occurrence(s)");
                }
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        // Closing instructions for the consuming AI.
        sb.AppendLine("## Instructions for the fixing agent");
        sb.AppendLine();
        sb.AppendLine("1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.");
        sb.AppendLine("2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.");
        sb.AppendLine("3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.");
        sb.AppendLine("4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.");
        sb.AppendLine("5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.");

        // Write to disk.
        string outPath = Path.Combine(hostDir, "_ai-handoff.md");
        File.WriteAllText(outPath, sb.ToString());
        return outPath;
    }

    private record RawViolation(string Tool, string PageSlug, string RuleId, string Severity,
        string Message, string Selector, string Snippet, string HelpUrl);

    private record RuleGroup
    {
        public string Tool { get; init; } = "";
        public string RuleId { get; init; } = "";
        public string Severity { get; set; } = "";
        public List<RawViolation> Occurrences { get; init; } = new();
        public int PagesAffected { get; init; }
        public string? FixHint { get; init; }
        public string? HelpUrl { get; init; }
    }

    private static RawViolation? ParseViolation(JsonElement el, string tool, string pageSlug)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        return new RawViolation(
            Tool: tool,
            PageSlug: pageSlug,
            RuleId: GetStr(el, "RuleId") ?? GetStr(el, "ruleId") ?? "",
            Severity: (GetStr(el, "Severity") ?? GetStr(el, "severity") ?? "minor").ToLowerInvariant(),
            Message: GetStr(el, "Message") ?? GetStr(el, "message") ?? "",
            Selector: GetStr(el, "Selector") ?? GetStr(el, "selector") ?? "",
            Snippet: GetStr(el, "Snippet") ?? GetStr(el, "snippet") ?? GetStr(el, "HtmlSnippet") ?? "",
            HelpUrl: GetStr(el, "HelpUrl") ?? GetStr(el, "helpUrl") ?? GetStr(el, "help") ?? ""
        );
    }

    private static string? GetStr(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetIntProp(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static int SeverityRank(string s) => s.ToLowerInvariant() switch {
        "critical" => 0, "serious" => 1, "moderate" => 2, "minor" => 3, _ => 4,
    };

    private static string MaxSeverity(IEnumerable<string> sevs) {
        int min = sevs.Min(SeverityRank);
        return min switch { 0 => "critical", 1 => "serious", 2 => "moderate", _ => "minor" };
    }

    private static string SeverityIcon(string s) => s.ToLowerInvariant() switch {
        "critical" => "🔴", "serious" => "🟠", "moderate" => "🟡", "minor" => "🔵", _ => "⚪"
    };

    private static string SeverityBadge(string s) => s.ToLowerInvariant() switch {
        "critical" => "🔴 critical", "serious" => "🟠 serious", "moderate" => "🟡 moderate", "minor" => "🔵 minor", _ => s
    };

    private static string HtmlEnc(string s) => WebUtility.HtmlEncode(s ?? "");
    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 3) + "...";
}
