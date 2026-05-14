using System.Text.Json;
using Microsoft.Playwright;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Result wrapper for the IBM Equal Access runner: violations plus per-outcome
/// counts so the rest of the system can show "X passed / Y failed" health stats.
/// </summary>
public class IbmRunResult
{
    public List<A11yIssue> Issues { get; set; } = new();
    public int PassCount { get; set; }
    public int PotentialCount { get; set; }
    public int ManualCount { get; set; }
}

/// <summary>
/// Attempts to inject IBM Equal Access Accessibility Checker from CDN.
/// If the CDN is not available or injection fails, returns an empty list gracefully.
/// This runner is best-effort — the scanner should not fail if IBM EA is unavailable.
/// </summary>
public static class IbmEqualAccessRunner
{
    private const string IbmCdnUrl = "https://cdn.jsdelivr.net/npm/accessibility-checker-engine@latest/ace.js";

    /// <summary>
    /// Injects IBM Equal Access engine into the page and runs analysis.
    /// The entire method is wrapped in try/catch — returns an empty result on any failure.
    /// Returns violations (FAIL/POTENTIAL/MANUAL outcomes) plus separate counts so callers
    /// can surface "rules passed" health stats without re-running the engine.
    /// </summary>
    public static async Task<IbmRunResult> Run(IPage page, string cacheDir)
    {
        IbmRunResult result = new();

        try {
            // Try to inject from CDN. If CDN is down, this will throw and we return empty.
            await page.AddScriptTagAsync(new PageAddScriptTagOptions {
                Url = IbmCdnUrl,
            });

            // IBM Equal Access result.value is [outcome, level] — e.g. ['PASS', 'INFORMATION']
            // or ['FAIL', 'VIOLATION']. We return non-PASS outcomes as Issues and aggregate
            // counts for PASS / POTENTIAL / MANUAL so the rest of the pipeline can show
            // health stats without re-running the engine.
            string ibmScript = @"
                async () => {
                    try {
                        if (typeof ace === 'undefined' && typeof window.ace === 'undefined') return '{}';
                        const checker = new ace.Checker();
                        const report = await checker.check(document, ['WCAG_2_1']);
                        if (!report || !report.results) return '{}';

                        let passCount = 0, potentialCount = 0, manualCount = 0, recCount = 0, unknownCount = 0;
                        const issues = [];
                        for (const r of report.results) {
                            // IBM's result.value array is [level, status] — NOT [status, level]
                            // as we previously assumed. Reading the wrong index made every
                            // PASS slip through as a 'minor' issue, inflating counts to 1000+.
                            // Defensive read: check BOTH positions and any string field for the
                            // status token. Whichever position yields a known outcome wins.
                            const v0 = String((r.value && r.value[0]) || '').toUpperCase();
                            const v1 = String((r.value && r.value[1]) || '').toUpperCase();
                            const tokens = [v0, v1];

                            const isPass = tokens.includes('PASS');
                            const isPotential = tokens.includes('POTENTIAL');
                            const isManual = tokens.includes('MANUAL');
                            const isRec = tokens.includes('RECOMMENDATION');
                            const isFail = tokens.includes('FAIL');

                            if (isPass) { passCount++; continue; }
                            if (isPotential) { potentialCount++; continue; }
                            if (isManual) { manualCount++; continue; }
                            // RECOMMENDATION-without-FAIL = positive suggestion / best-practice
                            // note. NOT a real failure. Skip as noise.
                            if (isRec && !isFail) { recCount++; continue; }
                            // No FAIL token AND no other recognized outcome — IBM rule of
                            // unknown shape. Skip rather than pollute the violations list.
                            if (!isFail) { unknownCount++; continue; }

                            // Real failure. The non-FAIL token (if present) is the level
                            // = severity bucket: VIOLATION > RECOMMENDATION > INFORMATION.
                            const level = (v0 !== 'FAIL') ? v0 : v1;
                            issues.push({
                                ruleId: r.ruleId || '',
                                message: r.message || (r.messageArgs ? r.messageArgs.join(' ') : ''),
                                outcome: 'FAIL',
                                severity: level || 'INFORMATION',
                                path: (r.path && r.path.dom) || '',
                                snippet: r.snippet || '',
                                help: r.help || '',
                                category: r.category || ''
                            });
                        }
                        return JSON.stringify({ issues, passCount, potentialCount, manualCount, recCount, unknownCount });
                    } catch(e) {
                        return '{}';
                    }
                }
            ";

            string resultJson = await page.EvaluateAsync<string>(ibmScript) ?? "{}";

            using JsonDocument doc = JsonDocument.Parse(resultJson);
            JsonElement root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object) {
                if (root.TryGetProperty("passCount", out var pcEl) && pcEl.ValueKind == JsonValueKind.Number) {
                    result.PassCount = pcEl.GetInt32();
                }
                if (root.TryGetProperty("potentialCount", out var ptEl) && ptEl.ValueKind == JsonValueKind.Number) {
                    result.PotentialCount = ptEl.GetInt32();
                }
                if (root.TryGetProperty("manualCount", out var mcEl) && mcEl.ValueKind == JsonValueKind.Number) {
                    result.ManualCount = mcEl.GetInt32();
                }
                if (root.TryGetProperty("issues", out var issuesEl) && issuesEl.ValueKind == JsonValueKind.Array) {
                    foreach (JsonElement issueEl in issuesEl.EnumerateArray()) {
                        string ruleId = issueEl.TryGetProperty("ruleId", out JsonElement ruleEl)
                            ? ruleEl.GetString() ?? string.Empty : string.Empty;
                        string message = issueEl.TryGetProperty("message", out JsonElement msgEl)
                            ? msgEl.GetString() ?? string.Empty : string.Empty;
                        string ibmSeverity = issueEl.TryGetProperty("severity", out JsonElement sevEl)
                            ? sevEl.GetString() ?? "INFORMATION" : "INFORMATION";
                        string selector = issueEl.TryGetProperty("path", out JsonElement pathEl)
                            ? pathEl.GetString() ?? string.Empty : string.Empty;
                        string snippet = issueEl.TryGetProperty("snippet", out JsonElement snipEl)
                            ? snipEl.GetString() ?? string.Empty : string.Empty;
                        string helpUrl = issueEl.TryGetProperty("help", out JsonElement helpEl)
                            ? helpEl.GetString() ?? string.Empty : string.Empty;

                        string severity = ibmSeverity.ToUpperInvariant() switch {
                            "VIOLATION" => "serious",
                            "RECOMMENDATION" => "moderate",
                            "INFORMATION" => "minor",
                            _ => "minor",
                        };

                        result.Issues.Add(new A11yIssue {
                            Tool = "ibm",
                            RuleId = ruleId,
                            Severity = severity,
                            Message = message,
                            Selector = selector,
                            Snippet = snippet.Length > 500 ? snippet[..500] : snippet,
                            HelpUrl = helpUrl,
                        });
                    }
                }
            }
        } catch (Exception) {
            // IBM EA is optional. CDN may be down, engine may not load.
            // Return empty result — other engines will still provide data.
        }

        return result;
    }

    /// <summary>
    /// Draws colored outlines on elements that IBM EA flagged,
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
                        el.style.outline = '3px dotted ' + color;
                        el.style.outlineOffset = '2px';
                        el.setAttribute('data-a11y-overlay', 'ibm');
                    }
                } catch(e) { /* selector may be invalid */ }
            });

            const banner = document.createElement('div');
            banner.id = 'a11y-ibm-overlay-banner';
            banner.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:999999;' +
                'background:#1a2744;color:#fff;padding:8px 16px;font:14px/1.4 sans-serif;' +
                'display:flex;gap:16px;align-items:center;';

            const counts = { serious: 0, moderate: 0, minor: 0 };
            violations.forEach(v => { counts[v.Severity] = (counts[v.Severity] || 0) + 1; });

            banner.innerHTML = '<strong>IBM Equal Access</strong> ' +
                Object.entries(counts).filter(([,c]) => c > 0)
                    .map(([sev, c]) => '<span style=""color:' + (colorMap[sev] || '#fff') + '"">' + c + ' ' + sev + '</span>')
                    .join(' | ');

            document.body.prepend(banner);
        }", violationsJson);
    }

    /// <summary>
    /// Removes the IBM EA overlay outlines and summary banner from the page.
    /// </summary>
    public static async Task RemoveOverlay(IPage page)
    {
        await page.EvaluateAsync(@"() => {
            document.querySelectorAll('[data-a11y-overlay=""ibm""]').forEach(el => {
                el.style.outline = '';
                el.style.outlineOffset = '';
                el.removeAttribute('data-a11y-overlay');
            });
            const banner = document.getElementById('a11y-ibm-overlay-banner');
            if (banner) banner.remove();
        }");
    }
}
