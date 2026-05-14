using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Injects HTML_CodeSniffer (HTMLCS) into a Playwright page, runs WCAG 2 AA analysis,
/// and parses the messages into A11yIssue objects.
/// Caches the HTMLCS JS file locally for offline/repeated use.
/// </summary>
public class HtmlCsRunResult
{
    public List<A11yIssue> Issues { get; set; } = new();
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int NoticeCount { get; set; }
    public int DistinctFailedRules { get; set; }
    /// <summary>HTMLCS WCAG 2.0 AA contains ~84 distinct rule codes. Constant per standard.</summary>
    public int TotalRulesInStandard { get; set; } = 84;
    /// <summary>Computed: TotalRulesInStandard - DistinctFailedRules. Floor at 0.</summary>
    public int PassCount => Math.Max(0, TotalRulesInStandard - DistinctFailedRules);
}

public static class HtmlCodeSnifferRunner
{
    private const string HtmlcsCdnUrl = "https://squizlabs.github.io/HTML_CodeSniffer/build/HTMLCS.js";
    private const string HtmlcsFileName = "HTMLCS.js";

    /// <summary>
    /// Downloads HTML_CodeSniffer from CDN on first run and caches it locally.
    /// Returns the path to the cached file.
    /// </summary>
    public static async Task<string> EnsureHtmlCodeSniffer(string cacheDir)
    {
        Directory.CreateDirectory(cacheDir);
        string cachePath = Path.Combine(cacheDir, HtmlcsFileName);

        if (!File.Exists(cachePath)) {
            using HttpClient client = new();
            client.Timeout = TimeSpan.FromSeconds(30);
            byte[] data = await client.GetByteArrayAsync(HtmlcsCdnUrl);
            await File.WriteAllBytesAsync(cachePath, data);
        }

        return cachePath;
    }

    /// <summary>
    /// Injects HTMLCS into the page, runs WCAG2AA analysis,
    /// and returns a list of accessibility issues.
    /// HTMLCS.getMessages() returns objects with { type, code, msg, element }.
    /// Type 1 = error, 2 = warning, 3 = notice.
    /// </summary>
    public static async Task<List<A11yIssue>> Run(IPage page, string cacheDir)
    {
        List<A11yIssue> issues = new();

        try {
            string htmlcsPath = await EnsureHtmlCodeSniffer(cacheDir);
            await page.AddScriptTagAsync(new PageAddScriptTagOptions {
                Path = htmlcsPath,
            });

            string htmlcsScript = @"
                async () => {
                    try {
                        await new Promise((resolve, reject) => {
                            HTMLCS.process('WCAG2AA', document, () => resolve(), () => reject());
                        });

                        const messages = HTMLCS.getMessages();
                        return JSON.stringify(messages.map(m => ({
                            type: m.type,
                            code: m.code || '',
                            msg: m.msg || '',
                            tagName: m.element ? m.element.tagName : '',
                            selector: m.element ? (m.element.id ? '#' + m.element.id : m.element.tagName.toLowerCase()) : '',
                            snippet: m.element ? m.element.outerHTML : ''
                        })));
                    } catch(e) {
                        return '[]';
                    }
                }
            ";

            string resultJson = await page.EvaluateAsync<string>(htmlcsScript) ?? "[]";

            using JsonDocument doc = JsonDocument.Parse(resultJson);
            foreach (JsonElement msg in doc.RootElement.EnumerateArray()) {
                int type = msg.TryGetProperty("type", out JsonElement typeEl) ? typeEl.GetInt32() : 3;
                string code = msg.TryGetProperty("code", out JsonElement codeEl)
                    ? codeEl.GetString() ?? string.Empty : string.Empty;
                string message = msg.TryGetProperty("msg", out JsonElement msgEl)
                    ? msgEl.GetString() ?? string.Empty : string.Empty;
                string selector = msg.TryGetProperty("selector", out JsonElement selEl)
                    ? selEl.GetString() ?? string.Empty : string.Empty;
                string snippet = msg.TryGetProperty("snippet", out JsonElement snipEl)
                    ? snipEl.GetString() ?? string.Empty : string.Empty;

                // Map HTMLCS type to severity: 1=error, 2=warning, 3=notice.
                string severity = type switch {
                    1 => "serious",
                    2 => "moderate",
                    3 => "minor",
                    _ => "minor",
                };

                issues.Add(new A11yIssue {
                    Tool = "htmlcs",
                    RuleId = code,
                    Severity = severity,
                    Message = message,
                    Selector = selector,
                    Snippet = snippet.Length > 500 ? snippet[..500] : snippet,
                    WcagCriteria = ParseWcagFromCode(code),
                });
            }
        } catch (Exception) {
            // HTMLCS injection may fail on some pages; return what we have.
        }

        return issues;
    }

    /// <summary>
    /// Same as Run() but also returns pass/fail breakdown stats (errors, warnings,
    /// notices, distinct failed rules, computed pass count). Use this in the engine
    /// so the report can showcase "X% passing" alongside the violation list.
    /// </summary>
    public static async Task<HtmlCsRunResult> RunWithStats(IPage page, string cacheDir)
    {
        var output = new HtmlCsRunResult();
        try {
            output.Issues = await Run(page, cacheDir);
            output.ErrorCount = output.Issues.Count(i => i.Severity == "serious");
            output.WarningCount = output.Issues.Count(i => i.Severity == "moderate");
            output.NoticeCount = output.Issues.Count(i => i.Severity == "minor");
            output.DistinctFailedRules = output.Issues
                .Select(i => i.RuleId).Where(r => !string.IsNullOrEmpty(r))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        } catch { /* leave defaults */ }
        return output;
    }

    /// <summary>
    /// Draws colored outlines on elements that HTMLCS flagged,
    /// with severity-based colors and a summary banner.
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
                serious:  '#d32f2f',
                moderate: '#f9a825',
                minor:    '#1565c0'
            };

            violations.forEach(v => {
                if (!v.Selector) return;
                try {
                    const el = document.querySelector(v.Selector);
                    if (el) {
                        const color = colorMap[v.Severity] || '#f9a825';
                        el.style.outline = '3px dashed ' + color;
                        el.style.outlineOffset = '2px';
                        el.setAttribute('data-a11y-overlay', 'htmlcs');
                    }
                } catch(e) { /* selector may be invalid */ }
            });

            const banner = document.createElement('div');
            banner.id = 'a11y-htmlcs-overlay-banner';
            banner.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:999999;' +
                'background:#2d2d44;color:#fff;padding:8px 16px;font:14px/1.4 sans-serif;' +
                'display:flex;gap:16px;align-items:center;';

            const counts = { serious: 0, moderate: 0, minor: 0 };
            violations.forEach(v => { counts[v.Severity] = (counts[v.Severity] || 0) + 1; });

            banner.innerHTML = '<strong>HTML_CodeSniffer</strong> ' +
                Object.entries(counts).filter(([,c]) => c > 0)
                    .map(([sev, c]) => '<span style=""color:' + (colorMap[sev] || '#fff') + '"">' + c + ' ' + sev + '</span>')
                    .join(' | ');

            document.body.prepend(banner);
        }", violationsJson);
    }

    /// <summary>
    /// Removes the HTMLCS overlay outlines and summary banner from the page.
    /// </summary>
    public static async Task RemoveOverlay(IPage page)
    {
        await page.EvaluateAsync(@"() => {
            document.querySelectorAll('[data-a11y-overlay=""htmlcs""]').forEach(el => {
                el.style.outline = '';
                el.style.outlineOffset = '';
                el.removeAttribute('data-a11y-overlay');
            });
            const banner = document.getElementById('a11y-htmlcs-overlay-banner');
            if (banner) banner.remove();
        }");
    }

    /// <summary>
    /// Parses WCAG success criteria from HTMLCS code strings.
    /// e.g., "WCAG2AA.Principle1.Guideline1_1.1_1_1.H37" → "1.1.1"
    /// </summary>
    private static string ParseWcagFromCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;

        // Pattern: after "Guideline\d_\d." there is the SC number like "1_1_1" or "2_4_1"
        Match m = Regex.Match(code, @"Guideline\d+_\d+\.(\d+)_(\d+)_(\d+)");
        if (m.Success) {
            return m.Groups[1].Value + "." + m.Groups[2].Value + "." + m.Groups[3].Value;
        }

        return string.Empty;
    }
}
