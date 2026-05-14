using System.Net;
using System.Text;

namespace FreeA11yChecker.Console.SourceAnalysis;

/// <summary>
/// Orchestrator: runs every analyzer in this folder against a source root and emits
/// a single self-contained HTML report. Findings from each analyzer are merged into
/// one big table, grouped by severity, then by source file. Quick-fix hints from
/// QuickFixHints attach to each finding when the issue text contains a matching cue.
/// </summary>
public static class SourceAnalysisReport
{
    public static string Run(string sourceRoot, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        string reportPath = Path.Combine(Path.GetFullPath(outputDir), "_source-analysis.html");

        // Run inventory first so summary stats are available.
        var inv = CodebaseInventory.Analyze(sourceRoot);

        // Run every static a11y scanner; merge findings into one list.
        var findings = new List<(string File, int Line, string Severity, string Issue, string Snippet, string Source)>();
        AddFindings(findings, "img/form", StaticImageFormScanner.Analyze(sourceRoot));
        AddFindings(findings, "structure", StaticStructureScanner.Analyze(sourceRoot));
        AddFindings(findings, "interactive", StaticInteractiveScanner.Analyze(sourceRoot));
        AddFindings(findings, "aria", StaticAriaScanner.Analyze(sourceRoot));

        int critical = findings.Count(f => f.Severity.Equals("critical", StringComparison.OrdinalIgnoreCase));
        int serious = findings.Count(f => f.Severity.Equals("serious", StringComparison.OrdinalIgnoreCase));
        int moderate = findings.Count(f => f.Severity.Equals("moderate", StringComparison.OrdinalIgnoreCase));
        int minor = findings.Count(f => f.Severity.Equals("minor", StringComparison.OrdinalIgnoreCase));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"en\" data-bs-theme=\"light\"><head>");
        sb.AppendLine("<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.Append("<title>Source Analysis — ").Append(HtmlEnc(Path.GetFileName(sourceRoot.TrimEnd('\\', '/')))).AppendLine("</title>");
        sb.AppendLine("<link href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css\" rel=\"stylesheet\">");
        sb.AppendLine("<style>code{font-size:0.85em;}.snippet{font-family:ui-monospace,monospace;background:#f6f8fa;padding:0.25rem;border-radius:0.25rem;font-size:0.8em;white-space:pre-wrap;word-break:break-all;}[data-bs-theme=dark] .snippet{background:#1e2228;}</style>");
        sb.AppendLine("</head><body>");

        // Navbar with theme toggle.
        sb.Append("<nav class=\"navbar navbar-expand bg-body-tertiary border-bottom sticky-top\"><div class=\"container-fluid\"><span class=\"navbar-brand fw-bold\">Source Analysis — ")
          .Append(HtmlEnc(sourceRoot)).AppendLine("</span>");
        sb.AppendLine("<button class=\"btn btn-sm btn-outline-secondary ms-auto\" onclick=\"document.documentElement.dataset.bsTheme=document.documentElement.dataset.bsTheme==='dark'?'light':'dark'\">Toggle theme</button>");
        sb.AppendLine("</div></nav>");

        sb.AppendLine("<div class=\"container-fluid p-3\">");

        // Summary cards.
        sb.AppendLine("<div class=\"row row-cols-2 row-cols-md-3 row-cols-lg-6 g-2 mb-3\">");
        Card(sb, "Total findings", findings.Count.ToString(), "primary");
        Card(sb, "Critical", critical.ToString(), critical > 0 ? "danger" : "secondary");
        Card(sb, "Serious", serious.ToString(), serious > 0 ? "warning" : "secondary");
        Card(sb, "Moderate", moderate.ToString(), moderate > 0 ? "info" : "secondary");
        Card(sb, "Minor", minor.ToString(), "secondary");
        Card(sb, "Files w/ issues", findings.Select(f => f.File).Distinct().Count().ToString(), "secondary");
        sb.AppendLine("</div>");

        // Codebase inventory section.
        sb.AppendLine("<div class=\"row row-cols-2 row-cols-md-4 g-2 mb-3\">");
        Card(sb, "Blazor routes", inv.BlazorRoutes.Count.ToString(), "secondary");
        Card(sb, "Controller routes", inv.ControllerRoutes.Count.ToString(), "secondary");
        Card(sb, "Tenant codes", inv.TenantCodes.Count.ToString(), "secondary");
        Card(sb, "Total LOC (cs+razor)", inv.TotalLines.ToString("N0"), "secondary");
        sb.AppendLine("</div>");

        // Findings table (top section).
        sb.AppendLine("<h3 class=\"mt-4\">Static A11y Findings</h3>");
        if (findings.Count == 0) {
            sb.AppendLine("<div class=\"alert alert-success\">No static findings — clean!</div>");
        } else {
            sb.AppendLine("<div class=\"table-responsive\"><table class=\"table table-sm table-striped table-hover\">");
            sb.AppendLine("<thead><tr><th>Severity</th><th>File</th><th>Line</th><th>Issue</th><th>Snippet</th><th>Hint</th></tr></thead><tbody>");
            // Sort by severity (critical first), then by file.
            int Rank(string s) => s.ToLowerInvariant() switch { "critical" => 0, "serious" => 1, "moderate" => 2, "minor" => 3, _ => 4 };
            foreach (var f in findings.OrderBy(f => Rank(f.Severity)).ThenBy(f => f.File).ThenBy(f => f.Line)) {
                string badgeClass = f.Severity.ToLowerInvariant() switch {
                    "critical" => "bg-danger",
                    "serious" => "bg-warning text-dark",
                    "moderate" => "bg-info text-dark",
                    _ => "bg-secondary",
                };
                string? hint = QuickFixHints.GetHint(GuessRuleId(f.Issue));
                sb.Append("<tr><td><span class=\"badge ").Append(badgeClass).Append("\">").Append(HtmlEnc(f.Severity)).Append("</span></td>");
                sb.Append("<td><code>").Append(HtmlEnc(f.File)).Append("</code></td>");
                sb.Append("<td class=\"text-end\"><code>").Append(f.Line).Append("</code></td>");
                sb.Append("<td>").Append(HtmlEnc(f.Issue));
                sb.Append(" <span class=\"text-muted\" style=\"font-size:0.8em;\">[").Append(HtmlEnc(f.Source)).Append("]</span></td>");
                sb.Append("<td><div class=\"snippet\">").Append(HtmlEnc(f.Snippet)).Append("</div></td>");
                sb.Append("<td>").Append(hint != null ? HtmlEnc(hint) : "<span class=\"text-muted\">—</span>").AppendLine("</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Routes section.
        sb.AppendLine("<h3 class=\"mt-4\">Discovered Routes</h3>");
        sb.AppendLine("<div class=\"row\">");
        sb.AppendLine("<div class=\"col-md-6\"><h5>Blazor @page routes</h5>");
        if (inv.BlazorRoutes.Count == 0) sb.AppendLine("<p class=\"text-muted\">None.</p>");
        else {
            sb.AppendLine("<ul class=\"list-group list-group-flush\" style=\"max-height:400px;overflow:auto;\">");
            foreach (var r in inv.BlazorRoutes) sb.Append("<li class=\"list-group-item py-1\"><code>").Append(HtmlEnc(r)).AppendLine("</code></li>");
            sb.AppendLine("</ul>");
        }
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"col-md-6\"><h5>Controller / API routes</h5>");
        if (inv.ControllerRoutes.Count == 0) sb.AppendLine("<p class=\"text-muted\">None.</p>");
        else {
            sb.AppendLine("<ul class=\"list-group list-group-flush\" style=\"max-height:400px;overflow:auto;\">");
            foreach (var r in inv.ControllerRoutes) sb.Append("<li class=\"list-group-item py-1\"><code>").Append(HtmlEnc(r)).AppendLine("</code></li>");
            sb.AppendLine("</ul>");
        }
        sb.AppendLine("</div></div>");

        // Tenant codes + file inventory.
        if (inv.TenantCodes.Count > 0) {
            sb.AppendLine("<h3 class=\"mt-4\">Tenant codes detected</h3>");
            sb.Append("<p>");
            foreach (var t in inv.TenantCodes) sb.Append("<span class=\"badge bg-info text-dark me-1\">").Append(HtmlEnc(t)).Append("</span>");
            sb.AppendLine("</p>");
        }

        sb.AppendLine("<h3 class=\"mt-4\">File inventory</h3>");
        sb.AppendLine("<table class=\"table table-sm w-auto\"><thead><tr><th>Extension</th><th class=\"text-end\">Count</th></tr></thead><tbody>");
        foreach (var kv in inv.FileCountsByExt.OrderByDescending(k => k.Value)) {
            sb.Append("<tr><td><code>").Append(HtmlEnc(kv.Key)).Append("</code></td><td class=\"text-end\">").Append(kv.Value).AppendLine("</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<p class=\"text-muted small mt-4\">Generated ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).AppendLine("</p>");
        sb.AppendLine("</div></body></html>");

        File.WriteAllText(reportPath, sb.ToString());
        return reportPath;
    }

    private static void AddFindings(
        List<(string File, int Line, string Severity, string Issue, string Snippet, string Source)> dst,
        string source,
        List<(string File, int Line, string Severity, string Issue, string Snippet)> src)
    {
        foreach (var f in src) dst.Add((f.File, f.Line, f.Severity, f.Issue, f.Snippet, source));
    }

    /// <summary>
    /// Heuristic mapping of free-text issue descriptions to the canonical axe/IBM rule
    /// IDs that QuickFixHints understands. Returns the issue text unchanged if no match —
    /// QuickFixHints will return null for unrecognized IDs and the table cell becomes "—".
    /// </summary>
    private static string GuessRuleId(string issue)
    {
        string i = issue.ToLowerInvariant();
        if (i.Contains("alt")) return "image-alt";
        if (i.Contains("label")) return "label";
        if (i.Contains("link")) return "link-name";
        if (i.Contains("button")) return "button-name";
        if (i.Contains("contrast")) return "color-contrast";
        if (i.Contains("lang")) return "html-has-lang";
        if (i.Contains("main")) return "landmark-one-main";
        if (i.Contains("heading")) return i.Contains("multiple") || i.Contains("more") ? "page-has-heading-one" : "heading-order";
        if (i.Contains("h1")) return "page-has-heading-one";
        if (i.Contains("aria-hidden")) return "aria-hidden-focus";
        if (i.Contains("aria-required")) return "aria-required-attr";
        if (i.Contains("aria-labelledby")) return "aria-labelledby";
        if (i.Contains("tabindex")) return "tabindex";
        if (i.Contains("table")) return "td-headers-attr";
        if (i.Contains("scope")) return "scope-attr-valid";
        if (i.Contains("frame") || i.Contains("iframe")) return "frame-title";
        if (i.Contains("duplicate")) return "duplicate-id";
        if (i.Contains("viewport")) return "meta-viewport";
        return issue;
    }

    private static void Card(StringBuilder sb, string label, string value, string color)
    {
        sb.Append("<div class=\"col\"><div class=\"card border-").Append(color).Append("\"><div class=\"card-body py-2\"><div class=\"text-muted small\">")
          .Append(HtmlEnc(label)).Append("</div><div class=\"fs-4 fw-bold text-").Append(color).Append("\">")
          .Append(HtmlEnc(value)).AppendLine("</div></div></div></div>");
    }

    private static string HtmlEnc(string s) => WebUtility.HtmlEncode(s ?? "");
}
