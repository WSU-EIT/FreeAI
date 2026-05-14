using System.Text.Json;
using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Injects axe-core into a Playwright page, runs WCAG 2.1 AA checks,
/// and parses the violations JSON into A11yIssue objects.
/// Caches the axe-core JS file locally for offline/repeated use.
/// </summary>
public static class AxeCoreRunner
{
    private const string AxeCdnUrl = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.2/axe.min.js";
    private const string AxeFileName = "axe.min.js";

    /// <summary>
    /// Downloads axe-core from CDN on first run and caches it locally.
    /// Returns the path to the cached file.
    /// </summary>
    public static async Task<string> EnsureAxeCore(string cacheDir)
    {
        Directory.CreateDirectory(cacheDir);
        string cachePath = Path.Combine(cacheDir, AxeFileName);

        if (!File.Exists(cachePath)) {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(30);
            byte[] data = await client.GetByteArrayAsync(AxeCdnUrl);
            await File.WriteAllBytesAsync(cachePath, data);
        }

        return cachePath;
    }

    /// <summary>
    /// Result container for axe-core analysis including violations, pass/incomplete/inapplicable counts.
    /// </summary>
    public class AxeResult
    {
        public List<A11yIssue> Issues { get; set; } = new();
        public int PassCount { get; set; }
        public int IncompleteCount { get; set; }
        public int InapplicableCount { get; set; }
    }

    /// <summary>
    /// Injects axe-core into the page, runs WCAG 2.x analysis,
    /// and returns violations with contrast data plus pass/incomplete/inapplicable counts.
    /// </summary>
    public static async Task<AxeResult> RunFull(IPage page, string cacheDir)
    {
        AxeResult result = new();

        try {
            string axePath = await EnsureAxeCore(cacheDir);
            await page.AddScriptTagAsync(new PageAddScriptTagOptions {
                Path = axePath,
            });

            // Return full results object (violations + passes + incomplete + inapplicable counts).
            string axeScript = @"
                async () => {
                    try {
                        const results = await axe.run(document, {
                            runOnly: { type: 'tag', values: ['wcag2a','wcag2aa','wcag21a','wcag21aa','wcag22aa'] }
                        });
                        return JSON.stringify({
                            violations: results.violations,
                            passCount: results.passes ? results.passes.length : 0,
                            incompleteCount: results.incomplete ? results.incomplete.length : 0,
                            inapplicableCount: results.inapplicable ? results.inapplicable.length : 0
                        });
                    } catch(e) {
                        return '{""violations"":[],""passCount"":0,""incompleteCount"":0,""inapplicableCount"":0}';
                    }
                }
            ";

            string resultJson = await page.EvaluateAsync<string>(axeScript) ?? "{\"violations\":[],\"passCount\":0,\"incompleteCount\":0,\"inapplicableCount\":0}";

            using JsonDocument doc = JsonDocument.Parse(resultJson);
            JsonElement root = doc.RootElement;

            result.PassCount = root.TryGetProperty("passCount", out JsonElement pc) ? pc.GetInt32() : 0;
            result.IncompleteCount = root.TryGetProperty("incompleteCount", out JsonElement ic) ? ic.GetInt32() : 0;
            result.InapplicableCount = root.TryGetProperty("inapplicableCount", out JsonElement ia) ? ia.GetInt32() : 0;

            if (root.TryGetProperty("violations", out JsonElement violationsEl)) {
                foreach (JsonElement violation in violationsEl.EnumerateArray()) {
                    string ruleId = violation.GetProperty("id").GetString() ?? string.Empty;
                    string impact = violation.TryGetProperty("impact", out JsonElement impactEl)
                        ? impactEl.GetString() ?? "minor" : "minor";
                    string helpUrl = violation.TryGetProperty("helpUrl", out JsonElement helpEl)
                        ? helpEl.GetString() ?? string.Empty : string.Empty;
                    string description = violation.TryGetProperty("description", out JsonElement descEl)
                        ? descEl.GetString() ?? string.Empty : string.Empty;

                    // Extract WCAG tags.
                    string wcagCriteria = string.Empty;
                    if (violation.TryGetProperty("tags", out JsonElement tagsEl)) {
                        List<string> wcagTags = new();
                        foreach (JsonElement tag in tagsEl.EnumerateArray()) {
                            string tagStr = tag.GetString() ?? string.Empty;
                            if (tagStr.StartsWith("wcag")) {
                                wcagTags.Add(tagStr);
                            }
                        }
                        wcagCriteria = string.Join(", ", wcagTags);
                    }

                    // Create an issue for each affected node.
                    if (violation.TryGetProperty("nodes", out JsonElement nodesEl)) {
                        foreach (JsonElement node in nodesEl.EnumerateArray()) {
                            string selector = string.Empty;
                            if (node.TryGetProperty("target", out JsonElement targetEl) && targetEl.GetArrayLength() > 0) {
                                selector = targetEl[0].GetString() ?? string.Empty;
                            }

                            string snippet = node.TryGetProperty("html", out JsonElement htmlEl)
                                ? htmlEl.GetString() ?? string.Empty : string.Empty;

                            string message = node.TryGetProperty("failureSummary", out JsonElement failEl)
                                ? failEl.GetString() ?? description : description;

                            A11yIssue issue = new A11yIssue {
                                Tool = "axe",
                                RuleId = ruleId,
                                Severity = impact,
                                Message = message,
                                Selector = selector,
                                Snippet = snippet.Length > 500 ? snippet[..500] : snippet,
                                HelpUrl = helpUrl,
                                WcagCriteria = wcagCriteria,
                            };

                            // Extract contrast data from node.any[].data for color-contrast rules.
                            if (ruleId == "color-contrast" || ruleId == "color-contrast-enhanced") {
                                ExtractContrastData(node, issue);
                            }

                            result.Issues.Add(issue);
                        }
                    }
                }
            }
        } catch (Exception) {
            // axe-core injection may fail on some pages; return what we have.
        }

        return result;
    }

    /// <summary>
    /// Extracts color contrast ratio data from axe-core node.any[].data.
    /// </summary>
    private static void ExtractContrastData(JsonElement node, A11yIssue issue)
    {
        try {
            if (!node.TryGetProperty("any", out JsonElement anyEl)) return;
            foreach (JsonElement check in anyEl.EnumerateArray()) {
                if (!check.TryGetProperty("data", out JsonElement data)) continue;
                if (data.ValueKind != JsonValueKind.Object) continue;

                if (data.TryGetProperty("fgColor", out JsonElement fg))
                    issue.ContrastForeground = fg.GetString();
                if (data.TryGetProperty("bgColor", out JsonElement bg))
                    issue.ContrastBackground = bg.GetString();
                if (data.TryGetProperty("contrastRatio", out JsonElement cr) && cr.ValueKind == JsonValueKind.Number)
                    issue.ContrastRatio = cr.GetDouble();
                if (data.TryGetProperty("expectedContrastRatio", out JsonElement ecr)) {
                    // Can be string like "4.5:1" or a number.
                    if (ecr.ValueKind == JsonValueKind.Number)
                        issue.ContrastExpected = ecr.GetDouble();
                    else if (ecr.ValueKind == JsonValueKind.String) {
                        string ecrStr = ecr.GetString() ?? "";
                        if (ecrStr.Contains(':')) ecrStr = ecrStr.Split(':')[0];
                        if (double.TryParse(ecrStr, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double ecrVal))
                            issue.ContrastExpected = ecrVal;
                    }
                }
                if (data.TryGetProperty("fontSize", out JsonElement fs))
                    issue.ContrastFontSize = fs.ToString();
                if (data.TryGetProperty("fontWeight", out JsonElement fw))
                    issue.ContrastFontWeight = fw.ToString();
                break; // Only need the first check with data.
            }
        } catch {
            // Best-effort contrast data extraction.
        }
    }

    /// <summary>
    /// Legacy wrapper — calls RunFull and returns just the issues list.
    /// </summary>
    public static async Task<List<A11yIssue>> Run(IPage page, string cacheDir)
    {
        var result = await RunFull(page, cacheDir);
        return result.Issues;
    }

    /// <summary>
    /// Draws colored outlines on violating elements with severity-based colors
    /// and a summary banner at the top of the page.
    /// </summary>
    public static async Task InjectOverlay(IPage page, List<A11yIssue> violations)
    {
        string violationsJson = JsonSerializer.Serialize(violations.Select(v => new {
            v.Selector,
            v.Severity,
            v.RuleId,
            v.Message,
        }));

        await page.EvaluateAsync(@"(data) => {
            const violations = JSON.parse(data);
            const colorMap = {
                critical: '#d32f2f',
                serious:  '#e65100',
                moderate: '#f9a825',
                minor:    '#1565c0'
            };

            // Draw outlines on violating elements.
            violations.forEach(v => {
                if (!v.Selector) return;
                try {
                    const el = document.querySelector(v.Selector);
                    if (el) {
                        const color = colorMap[v.Severity] || '#f9a825';
                        el.style.outline = '3px solid ' + color;
                        el.style.outlineOffset = '2px';
                        el.setAttribute('data-a11y-overlay', 'axe');
                    }
                } catch(e) { /* selector may be invalid */ }
            });

            // Summary banner.
            const banner = document.createElement('div');
            banner.id = 'a11y-axe-overlay-banner';
            banner.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:999999;' +
                'background:#1a1a2e;color:#fff;padding:8px 16px;font:14px/1.4 sans-serif;' +
                'display:flex;gap:16px;align-items:center;';

            const counts = { critical: 0, serious: 0, moderate: 0, minor: 0 };
            violations.forEach(v => { counts[v.Severity] = (counts[v.Severity] || 0) + 1; });

            banner.innerHTML = '<strong>axe-core</strong> ' +
                Object.entries(counts).filter(([,c]) => c > 0)
                    .map(([sev, c]) => '<span style=""color:' + (colorMap[sev] || '#fff') + '"">' + c + ' ' + sev + '</span>')
                    .join(' | ');

            document.body.prepend(banner);
        }", violationsJson);
    }

    /// <summary>
    /// Removes the axe overlay outlines and summary banner from the page.
    /// </summary>
    public static async Task RemoveOverlay(IPage page)
    {
        await page.EvaluateAsync(@"() => {
            document.querySelectorAll('[data-a11y-overlay=""axe""]').forEach(el => {
                el.style.outline = '';
                el.style.outlineOffset = '';
                el.removeAttribute('data-a11y-overlay');
            });
            const banner = document.getElementById('a11y-axe-overlay-banner');
            if (banner) banner.remove();
        }");
    }
}
