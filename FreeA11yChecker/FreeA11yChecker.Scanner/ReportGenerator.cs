using System.Text;

namespace FreeA11yChecker.Scanner;

using FreeA11yChecker.Scanner.Models;

/// <summary>
/// Generates markdown reports at the page, site, and run levels.
/// All methods are pure — they take result objects and return markdown strings.
/// </summary>
public static class ReportGenerator
{
    // ========================================================================
    // Page-level report
    // ========================================================================

    /// <summary>
    /// Generate a markdown report for a single page scan.
    /// </summary>
    public static string GeneratePageReport(PageScanResult result)
    {
        var sb = new StringBuilder();
        var statusEmoji = string.IsNullOrEmpty(result.ErrorMessage) ? "\u2705" : "\u274C";
        var missingAltCount = result.Images.Count(i => i.AltText == null);

        sb.AppendLine("# Page Scan Report");
        sb.AppendLine();
        sb.AppendLine($"> **URL:** {result.Url}  ");
        sb.AppendLine($"> **Status:** {statusEmoji} {result.StatusCode}  ");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| URL | {result.Url} |");
        sb.AppendLine($"| Title | {(string.IsNullOrEmpty(result.PageTitle) ? "*(none)*" : result.PageTitle)} |");
        sb.AppendLine($"| Status | {statusEmoji} {result.StatusCode} |");
        sb.AppendLine($"| HTML Size | {FormatBytes(result.HtmlSizeBytes)} |");
        sb.AppendLine($"| Screenshots | {result.Screenshots.Count} ({FormatBytes(result.Screenshots.Sum(s => s.SizeBytes))}) |");
        sb.AppendLine($"| Images | {result.Images.Count} |");
        sb.AppendLine($"| Images Missing Alt | {(missingAltCount > 0 ? $"Warning {missingAltCount}" : "0")} |");
        sb.AppendLine($"| A11y Violations | {(result.Summary.TotalViolations > 0 ? $"Warning {result.Summary.TotalViolations}" : "0")} |");
        if (result.Summary.TotalViolations > 0)
        {
            sb.AppendLine($"| Critical | {result.Summary.CriticalCount} |");
            sb.AppendLine($"| Serious | {result.Summary.SeriousCount} |");
            sb.AppendLine($"| Moderate | {result.Summary.ModerateCount} |");
            sb.AppendLine($"| Minor | {result.Summary.MinorCount} |");
            var toolsRun = new List<string>();
            if (result.Summary.AxeCount > 0 || result.AxeIssues.Count >= 0) toolsRun.Add("axe");
            if (result.Summary.HtmlCheckCount > 0 || result.HtmlCheckIssues.Count >= 0) toolsRun.Add("htmlcheck");
            if (result.Summary.HtmlCsCount > 0 || result.HtmlCsIssues.Count >= 0) toolsRun.Add("htmlcs");
            if (result.Summary.IbmCount > 0 || result.IbmIssues.Count >= 0) toolsRun.Add("ibm");
            sb.AppendLine($"| Tools Run | {string.Join(", ", toolsRun)} |");
        }
        sb.AppendLine();

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine($"> **Error:** `{result.ErrorMessage}`");
            sb.AppendLine();
        }

        // Screenshot gallery (2-column HTML table)
        sb.AppendLine("## Screenshots");
        sb.AppendLine();
        if (result.Screenshots.Count > 0)
        {
            sb.AppendLine("<table>");
            for (int i = 0; i < result.Screenshots.Count; i += 2)
            {
                sb.AppendLine("<tr>");
                for (int j = 0; j < 2; j++)
                {
                    var idx = i + j;
                    if (idx < result.Screenshots.Count)
                    {
                        var shot = result.Screenshots[idx];
                        var fileName = System.IO.Path.GetFileName(shot.Path);
                        sb.AppendLine("<td align=\"center\" width=\"50%\">");
                        sb.AppendLine($"<a href=\"{fileName}\">");
                        sb.AppendLine($"<img src=\"{fileName}\" width=\"400\" alt=\"{shot.Label}\" />");
                        sb.AppendLine("</a>");
                        sb.AppendLine($"<br /><strong>{idx + 1}. {shot.Label}</strong>");
                        sb.AppendLine($"<br /><sub>{FormatBytes(shot.SizeBytes)}</sub>");
                        sb.AppendLine("</td>");
                    }
                    else
                    {
                        sb.AppendLine("<td></td>");
                    }
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
        }
        else
        {
            sb.AppendLine("*No screenshots captured.*");
        }
        sb.AppendLine();

        // Image index
        sb.AppendLine($"## Page Images ({result.Images.Count})");
        sb.AppendLine();
        if (result.Images.Count > 0)
        {
            sb.AppendLine("| # | Source URL | Alt Text |");
            sb.AppendLine("|--:|-----------|----------|");
            var imgNum = 0;
            foreach (var img in result.Images)
            {
                imgNum++;
                var alt = img.AltText != null ? Truncate(img.AltText, 40) : "*(missing)*";
                sb.AppendLine($"| {imgNum} | {Truncate(img.Url, 80)} | {alt} |");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("*No images found on page.*");
            sb.AppendLine();
        }

        // Accessibility section
        if (result.Summary.TotalViolations > 0)
        {
            sb.AppendLine("## Accessibility");
            sb.AppendLine();

            // Cross-tool comparison table
            sb.AppendLine("### Cross-Tool Comparison");
            sb.AppendLine();
            var toolNames = new[] { "axe", "htmlcheck", "htmlcs", "ibm" };
            var headerCols = string.Join(" | ", toolNames);
            var alignCols = string.Join("|", toolNames.Select(_ => ":---:"));
            sb.AppendLine($"| Severity | {headerCols} |");
            sb.AppendLine($"|----------|{alignCols}|");

            var toolCounts = new Dictionary<string, (int Crit, int Seri, int Mod, int Min, int Total)>
            {
                ["axe"] = CountBySeverity(result.AxeIssues),
                ["htmlcheck"] = CountBySeverity(result.HtmlCheckIssues),
                ["htmlcs"] = CountBySeverity(result.HtmlCsIssues),
                ["ibm"] = CountBySeverity(result.IbmIssues)
            };

            foreach (var sev in new[] { "critical", "serious", "moderate", "minor" })
            {
                var cols = toolNames.Select(t =>
                {
                    var c = toolCounts[t];
                    var count = sev switch
                    {
                        "critical" => c.Crit,
                        "serious" => c.Seri,
                        "moderate" => c.Mod,
                        "minor" => c.Min,
                        _ => 0
                    };
                    return count > 0 ? count.ToString() : "0";
                });
                sb.AppendLine($"| {sev} | {string.Join(" | ", cols)} |");
            }

            var totalCols = toolNames.Select(t => $"**{toolCounts[t].Total}**");
            sb.AppendLine($"| **Total** | {string.Join(" | ", totalCols)} |");
            sb.AppendLine();

            // Ranked violations
            if (result.Summary.RankedRules.Count > 0)
            {
                sb.AppendLine("### Violations by Confidence");
                sb.AppendLine();
                sb.AppendLine("<details open>");
                sb.AppendLine($"<summary><strong>{result.Summary.RankedRules.Count} rule(s) violated</strong></summary>");
                sb.AppendLine();
                sb.AppendLine($"| # | Rule | Severity | Consensus | {headerCols} | Example |");
                sb.AppendLine($"|--:|------|:--------:|:---------:|{alignCols}|---------|");

                var rank = 0;
                foreach (var rule in result.Summary.RankedRules.Take(30))
                {
                    rank++;
                    var confLabel = rule.Confidence >= 0.8 ? "high" : rule.Confidence >= 0.5 ? "medium" : "low";

                    var toolColValues = toolNames.Select(t =>
                        rule.ToolsFound.Contains(t) ? "found" : "---");

                    var snippet = rule.Instances.Count > 0 && !string.IsNullOrEmpty(rule.Instances[0].Snippet)
                        ? $"`{Truncate(rule.Instances[0].Snippet.Replace("|", "\\|").Replace("`", "'"), 60)}`"
                        : "";

                    sb.AppendLine($"| {rank} | {rule.CanonicalRuleId} | {rule.Severity} | {confLabel} {rule.ToolsFound.Count}/{toolNames.Length} | {string.Join(" | ", toolColValues)} | {snippet} |");
                }

                if (result.Summary.RankedRules.Count > 30)
                {
                    sb.AppendLine($"| | *...and {result.Summary.RankedRules.Count - 30} more* | | | | | | |");
                }

                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            sb.AppendLine("> **Note:** Automated scanning catches ~30-60% of WCAG issues. Manual keyboard and screen reader testing is still required for full compliance.");
            sb.AppendLine();
        }
        else if (result.Summary.TotalViolations == 0)
        {
            sb.AppendLine("## Accessibility");
            sb.AppendLine();
            sb.AppendLine("No violations detected.");
            sb.AppendLine();
        }

        // Files section
        sb.AppendLine("## Files");
        sb.AppendLine();
        sb.AppendLine("| File | Description |");
        sb.AppendLine("|------|-------------|");
        foreach (var shot in result.Screenshots)
        {
            var fileName = System.IO.Path.GetFileName(shot.Path);
            sb.AppendLine($"| `{fileName}` | {shot.Label} ({FormatBytes(shot.SizeBytes)}) |");
        }
        sb.AppendLine("| `metadata.json` | Machine-readable scan data |");
        sb.AppendLine("| `a11y-summary.json` | Merged cross-tool accessibility summary |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by FreeA11yChecker Scanner v1.0*");

        return sb.ToString();
    }

    // ========================================================================
    // Site-level report
    // ========================================================================

    /// <summary>
    /// Generate a markdown report for all pages within a single site.
    /// </summary>
    public static string GenerateSiteReport(SiteScanResult result)
    {
        var sb = new StringBuilder();
        var totalCount = result.Pages.Count;
        var successCount = result.Pages.Count(p => string.IsNullOrEmpty(p.ErrorMessage));
        var failedCount = totalCount - successCount;
        var successPct = totalCount > 0 ? (successCount * 100.0 / totalCount) : 0;
        var totalImages = result.Pages.Sum(p => p.Images.Count);
        var totalMissingAlt = result.Pages.Sum(p => p.Images.Count(i => i.AltText == null));
        var altPct = totalImages > 0 ? ((totalImages - totalMissingAlt) * 100.0 / totalImages) : 100;
        var a11yTotal = result.Pages.Sum(p => p.Summary.TotalViolations);
        var cleanPages = result.Pages.Count(p => p.Summary.TotalViolations == 0);
        var a11yCleanPct = totalCount > 0 ? (cleanPages * 100.0 / totalCount) : 100;

        sb.AppendLine($"# Site Report: {result.BaseUrl}");
        sb.AppendLine();
        var statusEmoji = failedCount == 0 ? "OK" : "Warning";
        sb.AppendLine($"> **Status:** {statusEmoji} {successCount}/{totalCount} pages OK  ");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Dashboard with progress bars
        sb.AppendLine("## Dashboard");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"Success Rate:     {GenerateProgressBar(successPct, 30)} {successPct:F0}%");
        sb.AppendLine($"Alt Text Cover:   {GenerateProgressBar(altPct, 30)} {altPct:F0}%");
        sb.AppendLine($"A11y Clean Pages: {GenerateProgressBar(a11yCleanPct, 30)} {a11yCleanPct:F0}%");
        sb.AppendLine("```");
        sb.AppendLine();

        // Summary metrics
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Pages Scanned | {totalCount} |");
        sb.AppendLine($"| Pages Passed | {successCount} |");
        sb.AppendLine($"| Pages Failed | {failedCount} |");
        sb.AppendLine($"| Total Images | {totalImages} |");
        sb.AppendLine($"| Images Missing Alt | {totalMissingAlt} |");
        sb.AppendLine($"| A11y Violations | {a11yTotal} |");
        if (a11yTotal > 0)
        {
            sb.AppendLine($"| Critical | {result.Pages.Sum(p => p.Summary.CriticalCount)} |");
            sb.AppendLine($"| Serious | {result.Pages.Sum(p => p.Summary.SeriousCount)} |");
            sb.AppendLine($"| Moderate | {result.Pages.Sum(p => p.Summary.ModerateCount)} |");
            sb.AppendLine($"| Minor | {result.Pages.Sum(p => p.Summary.MinorCount)} |");
        }
        sb.AppendLine();

        // Pages table
        sb.AppendLine("## Pages");
        sb.AppendLine();
        sb.AppendLine("| Status | Page | HTTP | Critical | Serious | Moderate | Minor | A11y |");
        sb.AppendLine("|:------:|------|:----:|:--------:|:-------:|:--------:|:-----:|:----:|");

        foreach (var page in result.Pages.OrderBy(p => p.Url))
        {
            var s = string.IsNullOrEmpty(page.ErrorMessage) ? "OK" : "FAIL";
            var a = page.Summary;
            var crit = a.CriticalCount > 0 ? a.CriticalCount.ToString() : "";
            var seri = a.SeriousCount > 0 ? a.SeriousCount.ToString() : "";
            var mod = a.ModerateCount > 0 ? a.ModerateCount.ToString() : "";
            var min = a.MinorCount > 0 ? a.MinorCount.ToString() : "";
            var total = a.TotalViolations > 0 ? a.TotalViolations.ToString() : "0";
            sb.AppendLine($"| {s} | {page.Url} | {page.StatusCode} | {crit} | {seri} | {mod} | {min} | {total} |");
        }
        sb.AppendLine();

        // SSL certificate info (from first page that has it)
        var certPage = result.Pages.FirstOrDefault(p => p.Certificate != null);
        if (certPage?.Certificate != null)
        {
            var cert = certPage.Certificate;
            var daysLeft = (int)(cert.Expiry - DateTime.UtcNow).TotalDays;

            sb.AppendLine("## SSL Certificate");
            sb.AppendLine();
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("|-------|-------|");
            sb.AppendLine($"| Subject | `{cert.Subject}` |");
            sb.AppendLine($"| Issuer | `{Truncate(cert.Issuer, 80)}` |");
            sb.AppendLine($"| Expires | {cert.Expiry:yyyy-MM-dd} ({daysLeft} days) |");
            sb.AppendLine($"| SANs | {cert.SubjectAlternativeNames.Count} domain(s) |");
            sb.AppendLine();

            if (cert.SubjectAlternativeNames.Count > 0)
            {
                sb.AppendLine("<details>");
                sb.AppendLine($"<summary><strong>Subject Alternative Names ({cert.SubjectAlternativeNames.Count})</strong></summary>");
                sb.AppendLine();
                foreach (var san in cert.SubjectAlternativeNames)
                {
                    sb.AppendLine($"- `{san}`");
                }
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by FreeA11yChecker Scanner v1.0*");

        return sb.ToString();
    }

    // ========================================================================
    // Run-level report
    // ========================================================================

    /// <summary>
    /// Generate a grand summary report for the entire scan run across all sites.
    /// </summary>
    public static string GenerateRunReport(RunScanResult result)
    {
        var sb = new StringBuilder();
        var siteList = result.Sites.Values.OrderBy(s => s.BaseUrl).ToList();

        var totalSites = siteList.Count;
        var totalPages = siteList.Sum(s => s.Pages.Count);
        var totalSuccess = siteList.Sum(s => s.Pages.Count(p => string.IsNullOrEmpty(p.ErrorMessage)));
        var totalFailed = totalPages - totalSuccess;
        var totalImages = siteList.Sum(s => s.Pages.Sum(p => p.Images.Count));
        var totalMissingAlt = siteList.Sum(s => s.Pages.Sum(p => p.Images.Count(i => i.AltText == null)));
        var runA11yTotal = siteList.Sum(s => s.Pages.Sum(p => p.Summary.TotalViolations));
        var runA11yCrit = siteList.Sum(s => s.Pages.Sum(p => p.Summary.CriticalCount));
        var runA11ySeri = siteList.Sum(s => s.Pages.Sum(p => p.Summary.SeriousCount));
        var runA11yMod = siteList.Sum(s => s.Pages.Sum(p => p.Summary.ModerateCount));
        var runA11yMin = siteList.Sum(s => s.Pages.Sum(p => p.Summary.MinorCount));
        var successPct = totalPages > 0 ? (totalSuccess * 100.0 / totalPages) : 0;

        var runStatus = totalFailed == 0 ? "All pages passed" : $"{totalFailed} page(s) failed";

        sb.AppendLine("# Accessibility Scanner - Run Report");
        sb.AppendLine();
        sb.AppendLine($"> **Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC  ");
        sb.AppendLine($"> **Status:** {runStatus}  ");
        sb.AppendLine($"> **Sites:** {totalSites} | **Pages:** {totalPages}  ");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Grand dashboard
        sb.AppendLine("## Dashboard");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"Page Success:     {GenerateProgressBar(successPct, 30)} {successPct:F0}%");
        var altPct = totalImages > 0 ? ((totalImages - totalMissingAlt) * 100.0 / totalImages) : 100;
        sb.AppendLine($"Alt Text Cover:   {GenerateProgressBar(altPct, 30)} {altPct:F0}%");
        var cleanPages = siteList.Sum(s => s.Pages.Count(p => p.Summary.TotalViolations == 0));
        var a11yCleanPct = totalPages > 0 ? (cleanPages * 100.0 / totalPages) : 100;
        sb.AppendLine($"A11y Clean Pages: {GenerateProgressBar(a11yCleanPct, 30)} {a11yCleanPct:F0}%");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("| Passed | Failed | A11y Issues | Critical | Serious | Moderate | Minor |");
        sb.AppendLine("|:------:|:------:|:-----------:|:--------:|:-------:|:--------:|:-----:|");
        sb.AppendLine($"| {totalSuccess} | {totalFailed} | {runA11yTotal} | {runA11yCrit} | {runA11ySeri} | {runA11yMod} | {runA11yMin} |");
        sb.AppendLine();

        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Sites | {totalSites} |");
        sb.AppendLine($"| Total Pages | {totalPages} |");
        sb.AppendLine($"| Total Images | {totalImages} |");
        sb.AppendLine($"| Total A11y Violations | {runA11yTotal} |");
        sb.AppendLine();

        // All sites table
        sb.AppendLine("## Sites");
        sb.AppendLine();
        sb.AppendLine("| Status | Site | Pages | Critical | Serious | Moderate | Minor | A11y Total |");
        sb.AppendLine("|:------:|------|:-----:|:--------:|:-------:|:--------:|:-----:|:---------:|");

        foreach (var site in siteList)
        {
            var siteFailed = site.Pages.Count(p => !string.IsNullOrEmpty(p.ErrorMessage));
            var saCrit = site.Pages.Sum(p => p.Summary.CriticalCount);
            var saSeri = site.Pages.Sum(p => p.Summary.SeriousCount);
            var saMod = site.Pages.Sum(p => p.Summary.ModerateCount);
            var saMin = site.Pages.Sum(p => p.Summary.MinorCount);
            var saTotal = site.Pages.Sum(p => p.Summary.TotalViolations);
            var s = siteFailed == 0 ? "OK" : "Warning";
            var totalBadge = saTotal > 0 ? saTotal.ToString() : "0";

            sb.AppendLine($"| {s} | {site.BaseUrl} | {site.Pages.Count} | {(saCrit > 0 ? saCrit.ToString() : "")} | {(saSeri > 0 ? saSeri.ToString() : "")} | {(saMod > 0 ? saMod.ToString() : "")} | {(saMin > 0 ? saMin.ToString() : "")} | {totalBadge} |");
        }
        sb.AppendLine();

        // Top 20 violations ranked by consensus across all sites
        var allPagesWithA11y = siteList
            .SelectMany(s => s.Pages.Where(p => p.Summary.TotalViolations > 0)
                .Select(p => (Site: s, Page: p)))
            .ToList();

        if (allPagesWithA11y.Count > 0)
        {
            sb.AppendLine("## Top Violations (all sites)");
            sb.AppendLine();

            var allRunRanked = allPagesWithA11y
                .SelectMany(x => x.Page.Summary.RankedRules
                    .Select(r => (x.Site, x.Page, Rule: r)))
                .GroupBy(x => x.Rule.CanonicalRuleId)
                .Select(g => new
                {
                    Rule = g.Key,
                    Severity = g.First().Rule.Severity,
                    Sites = g.Select(x => x.Site.BaseUrl).Distinct().Count(),
                    Pages = g.Select(x => x.Page.Url).Distinct().Count(),
                    Instances = g.Sum(x => x.Rule.Instances.Count),
                    AvgConfidence = g.Average(x => x.Rule.Confidence),
                    WcagCriteria = g.First().Rule.Instances.FirstOrDefault()?.WcagCriteria ?? "",
                    Message = g.First().Rule.Instances.FirstOrDefault()?.Message ?? ""
                })
                .OrderByDescending(r => r.AvgConfidence)
                .ThenBy(r => SeverityRank(r.Severity))
                .ThenByDescending(r => r.Instances)
                .Take(20)
                .ToList();

            if (allRunRanked.Count > 0)
            {
                sb.AppendLine($"| # | Rule | Severity | Sites | Pages | Instances | WCAG |");
                sb.AppendLine($"|--:|------|:--------:|:-----:|:-----:|:---------:|:----:|");

                var rank = 0;
                foreach (var r in allRunRanked)
                {
                    rank++;
                    sb.AppendLine($"| {rank} | {r.Rule} | {r.Severity} | {r.Sites}/{totalSites} | {r.Pages}/{totalPages} | {r.Instances:N0} | {(string.IsNullOrEmpty(r.WcagCriteria) ? "---" : r.WcagCriteria)} |");
                }
                sb.AppendLine();

                // a11y-ranked.csv content
                sb.AppendLine("### CSV Export (a11y-ranked.csv)");
                sb.AppendLine();
                sb.AppendLine("```csv");
                sb.AppendLine("Rank,Rule,Severity,Confidence,Sites,Pages,Instances,WCAG,Message");
                rank = 0;
                foreach (var r in allRunRanked)
                {
                    rank++;
                    var conf = r.AvgConfidence >= 0.8 ? "high" : r.AvgConfidence >= 0.5 ? "medium" : "low";
                    var msg = r.Message.Replace("\"", "\"\"");
                    sb.AppendLine($"{rank},\"{r.Rule}\",\"{r.Severity}\",\"{conf}\",{r.Sites},{r.Pages},{r.Instances},\"{r.WcagCriteria}\",\"{msg}\"");
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("*Generated by FreeA11yChecker Scanner v1.0*");

        return sb.ToString();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        value = value.Replace("|", "\\|");
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }

    private static string GenerateProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage * width / 100.0);
        filled = Math.Clamp(filled, 0, width);
        var empty = width - filled;
        return $"[{new string('#', filled)}{new string('.', empty)}]";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 0,
        "serious" => 1,
        "moderate" => 2,
        "minor" => 3,
        _ => 4
    };

    private static (int Crit, int Seri, int Mod, int Min, int Total) CountBySeverity(List<A11yIssue> issues)
    {
        var crit = issues.Count(i => i.Severity == "critical");
        var seri = issues.Count(i => i.Severity == "serious");
        var mod = issues.Count(i => i.Severity == "moderate");
        var min = issues.Count(i => i.Severity == "minor");
        return (crit, seri, mod, min, issues.Count);
    }
}
