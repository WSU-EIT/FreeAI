using System.Text.RegularExpressions;
using FreeA11yChecker.Scanner.Models;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Result wrapper for HtmlChecker: violations plus rule-level pass/fail counts so the
/// rest of the system can show "X passed / Y failed" health stats.
/// </summary>
public class HtmlCheckResult
{
    public List<A11yIssue> Issues { get; set; } = new();
    public int TotalChecks { get; set; }
    public int PassCount { get; set; }
}

/// <summary>
/// Runs 20 regex-based accessibility rules against raw HTML.
/// Fast, no JS injection needed. Catches obvious structural issues
/// that don't require a live DOM.
/// </summary>
public static class HtmlChecker
{
    /// <summary>
    /// Total number of distinct rules this checker runs. Used to compute pass rate
    /// alongside the issue list returned from <see cref="CheckWithStats"/>.
    /// </summary>
    public const int TotalRules = 20;

    /// <summary>
    /// Runs all rules and returns issues plus pass-rate stats. Pass count is
    /// (TotalRules - distinct rule IDs that produced at least one issue) — a rule
    /// that fired multiple times still counts as one failed rule.
    /// </summary>
    public static HtmlCheckResult CheckWithStats(string html)
    {
        var issues = Check(html);
        int failedRules = issues.Select(i => i.RuleId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return new HtmlCheckResult {
            Issues = issues,
            TotalChecks = TotalRules,
            PassCount = Math.Max(0, TotalRules - failedRules),
        };
    }

    /// <summary>
    /// Runs all 20 HTML checks against the provided HTML string
    /// and returns a list of accessibility issues found.
    /// </summary>
    public static List<A11yIssue> Check(string html)
    {
        List<A11yIssue> issues = new();

        if (string.IsNullOrWhiteSpace(html)) { return issues; }

        // 1. img-alt — Images missing alt attribute.
        foreach (Match m in Regex.Matches(html, @"<img\b(?![^>]*\balt\b)[^>]*>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("img-alt", "serious", "Image missing alt attribute", m.Value));

        // 2. heading-order — Heading levels are skipped (e.g. h2 to h4).
        MatchCollection headings = Regex.Matches(html, @"<(h[1-6])\b", RegexOptions.IgnoreCase);
        int lastLevel = 0;
        foreach (Match h in headings) {
            int level = int.Parse(h.Groups[1].Value.Substring(1));
            if (lastLevel > 0 && level > lastLevel + 1)
                issues.Add(CreateIssue("heading-order", "moderate", $"Heading level skipped: h{lastLevel} to h{level}"));
            lastLevel = level;
        }

        // 3. heading-empty — Heading elements with no text content.
        foreach (Match m in Regex.Matches(html, @"<h[1-6]\b[^>]*>\s*</h[1-6]>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("heading-empty", "moderate", "Heading element is empty", m.Value));

        // 4. html-lang — Missing lang attribute on <html>.
        if (!Regex.IsMatch(html, @"<html\b[^>]*\blang\s*=", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("html-lang", "serious", "Missing lang attribute on <html>"));

        // 5. title-missing — Missing or empty <title> element.
        if (!Regex.IsMatch(html, @"<title\b[^>]*>.+?</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)) {
            if (Regex.IsMatch(html, @"<title\b[^>]*>\s*</title>", RegexOptions.IgnoreCase))
                issues.Add(CreateIssue("title-missing", "serious", "Page has an empty <title> element", "<title></title>"));
            else if (!Regex.IsMatch(html, @"<title\b", RegexOptions.IgnoreCase))
                issues.Add(CreateIssue("title-missing", "serious", "Page is missing a <title> element"));
        }

        // 6. label-missing — Form inputs that may lack an associated label.
        // An input is considered labeled if ANY of these are true:
        //   a) it has aria-label or aria-labelledby
        //   b) it has a title attribute
        //   c) its id is referenced by some <label for="that-id"> elsewhere in the document
        //   d) it is wrapped in a <label>...<input/>...</label> (implicit label)
        foreach (Match m in Regex.Matches(html, @"<input\b(?![^>]*type\s*=\s*""(?:hidden|submit|button|image)"")[^>]*>", RegexOptions.IgnoreCase)) {
            string tag = m.Value;
            if (Regex.IsMatch(tag, @"\baria-label\b|\baria-labelledby\b|\btitle\s*=", RegexOptions.IgnoreCase)) continue;

            // explicit label association via id
            Match idMatch = Regex.Match(tag, @"\bid\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
            if (idMatch.Success) {
                string id = idMatch.Groups[1].Value;
                string forPattern = @"<label\b[^>]*\bfor\s*=\s*""" + Regex.Escape(id) + @"""";
                if (Regex.IsMatch(html, forPattern, RegexOptions.IgnoreCase)) continue;
            }

            // implicit wrapping label: nearest preceding unclosed <label> on the same line/block
            string before = html[..m.Index];
            int lastLabelOpen = before.LastIndexOf("<label", StringComparison.OrdinalIgnoreCase);
            int lastLabelClose = before.LastIndexOf("</label>", StringComparison.OrdinalIgnoreCase);
            if (lastLabelOpen > lastLabelClose) continue;

            issues.Add(CreateIssue("label-missing", "moderate", "Form input may be missing a label", tag));
        }

        // 7. link-empty — Links with no text content.
        foreach (Match m in Regex.Matches(html, @"<a\b[^>]*>\s*</a>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("link-empty", "serious", "Link has no text content", m.Value));

        // 8. button-empty — Buttons with no accessible name.
        // Empty content is fine if the button has aria-label, aria-labelledby, or title — these
        // provide the accessible name even when the visible content is icon-only or empty.
        foreach (Match m in Regex.Matches(html, @"<button\b([^>]*)>\s*</button>", RegexOptions.IgnoreCase)) {
            string attrs = m.Groups[1].Value;
            if (Regex.IsMatch(attrs, @"\baria-label\s*=\s*""[^""]+""|\baria-labelledby\s*=|\btitle\s*=\s*""[^""]+""", RegexOptions.IgnoreCase))
                continue;
            issues.Add(CreateIssue("button-empty", "serious", "Button has no text content or accessible name", m.Value));
        }

        // 9. skip-link — No skip-to-content link found.
        if (!Regex.IsMatch(html, @"<a\b[^>]*href\s*=\s*""#[^""]*""[^>]*>.*?(skip|main\s+content)", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            issues.Add(CreateIssue("skip-link", "moderate", "No skip-to-content link found"));

        // 10. landmark-main — No <main> landmark found.
        if (!Regex.IsMatch(html, @"<main\b|role\s*=\s*""main""", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("landmark-main", "moderate", "No <main> landmark found"));

        // 11. landmark-nav — No <nav> landmark found.
        if (!Regex.IsMatch(html, @"<nav\b|role\s*=\s*""navigation""", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("landmark-nav", "moderate", "No <nav> landmark found"));

        // 12. div-button — Div or span with click handler acting as button without role.
        foreach (Match m in Regex.Matches(html, @"<(?:div|span)\b[^>]*\bonclick\b[^>]*>", RegexOptions.IgnoreCase)) {
            if (!Regex.IsMatch(m.Value, @"role\s*=\s*""button""", RegexOptions.IgnoreCase))
                issues.Add(CreateIssue("div-button", "serious", "Interactive div/span is missing role=\"button\"", m.Value));
        }

        // 13. tabindex — Positive tabindex values disrupt natural tab order.
        foreach (Match m in Regex.Matches(html, @"tabindex\s*=\s*""([1-9]\d*)""", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("tabindex", "moderate", "Positive tabindex value disrupts natural tab order", m.Value));

        // 14. meta-refresh — Meta refresh tags can disorient users.
        foreach (Match m in Regex.Matches(html, @"<meta\b[^>]*http-equiv\s*=\s*""refresh""[^>]*>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("meta-refresh", "serious", "Meta refresh can disorient users", m.Value));

        // 15. table-header — Data tables missing <th> header cells.
        foreach (Match m in Regex.Matches(html, @"<table\b[^>]*>[\s\S]*?</table>", RegexOptions.IgnoreCase)) {
            if (!Regex.IsMatch(m.Value, @"<th\b", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(m.Value, @"<td\b", RegexOptions.IgnoreCase))
                issues.Add(CreateIssue("table-header", "moderate", "Data table is missing header cells (<th>)", Truncate(m.Value)));
        }

        // 16. link-suspicious — Links with non-descriptive text.
        foreach (Match m in Regex.Matches(html, @"<a\b[^>]*>(click\s+here|here|more|read\s+more|learn\s+more|link)</a>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("link-suspicious", "moderate", "Suspicious link text (not descriptive)", m.Value));

        // 17. link-pdf — Links to PDF documents.
        foreach (Match m in Regex.Matches(html, @"<a\b[^>]*href\s*=\s*""[^""]*\.pdf""[^>]*>", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("link-pdf", "moderate", "Link points to a PDF — verify accessibility of linked document", m.Value));

        // 18. img-input-alt — Image inputs missing alt attribute.
        foreach (Match m in Regex.Matches(html, @"<input\b[^>]*type\s*=\s*""image""[^>]*>", RegexOptions.IgnoreCase)) {
            if (!Regex.IsMatch(m.Value, @"\balt\s*=", RegexOptions.IgnoreCase))
                issues.Add(CreateIssue("img-input-alt", "serious", "Image input is missing alt attribute", m.Value));
        }

        // 19. fieldset-missing — Groups of related radios/checkboxes need a <fieldset>.
        // A standalone toggle/switch with its own label does NOT need a fieldset — fieldsets are
        // for *grouping* related controls (e.g. radio button list for "Color: red/green/blue").
        // Only flag when 2+ inputs share the same `name` attribute and none are wrapped in a fieldset.
        var radioCheckboxes = Regex.Matches(html, @"<input\b[^>]*type\s*=\s*""(?:radio|checkbox)""[^>]*>", RegexOptions.IgnoreCase);
        var groupsByName = new Dictionary<string, List<Match>>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in radioCheckboxes) {
            Match nameMatch = Regex.Match(m.Value, @"\bname\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
            if (!nameMatch.Success) continue;
            string name = nameMatch.Groups[1].Value;
            if (!groupsByName.ContainsKey(name)) groupsByName[name] = new List<Match>();
            groupsByName[name].Add(m);
        }
        foreach (var group in groupsByName.Values.Where(g => g.Count >= 2)) {
            foreach (Match m in group) {
                string beforeMatch = html[..m.Index];
                int fieldsetOpens = Regex.Matches(beforeMatch, @"<fieldset\b", RegexOptions.IgnoreCase).Count;
                int fieldsetCloses = Regex.Matches(beforeMatch, @"</fieldset>", RegexOptions.IgnoreCase).Count;
                if (fieldsetOpens <= fieldsetCloses) {
                    issues.Add(CreateIssue("fieldset-missing", "moderate", "Radio/checkbox group is not wrapped in a fieldset", m.Value));
                    break; // one finding per group, not per input
                }
            }
        }

        // 20. text-justified — Inline styles using text-align: justify.
        foreach (Match m in Regex.Matches(html, @"style\s*=\s*""[^""]*text-align\s*:\s*justify[^""]*""", RegexOptions.IgnoreCase))
            issues.Add(CreateIssue("text-justified", "moderate", "Inline style uses text-align: justify which can reduce readability", m.Value));

        return issues;
    }

    /// <summary>
    /// Creates a standardized htmlcheck issue with snippet truncation.
    /// </summary>
    private static A11yIssue CreateIssue(string ruleId, string severity, string message, string snippet = "")
    {
        return new A11yIssue {
            Tool = "htmlcheck",
            RuleId = ruleId,
            Severity = severity,
            Message = message,
            Snippet = snippet.Length > 500 ? snippet[..500] : snippet,
        };
    }

    /// <summary>
    /// Truncates a string to a maximum length for use in snippets.
    /// </summary>
    private static string Truncate(string value, int maxLength = 500)
    {
        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
