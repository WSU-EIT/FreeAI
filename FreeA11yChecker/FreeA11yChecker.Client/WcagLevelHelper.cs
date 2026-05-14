using System.Text.RegularExpressions;

namespace FreeA11yChecker.Client;

/// <summary>
/// Shared utility for matching WCAG conformance levels against violation criteria.
/// Handles both axe-style tags ("wcag2a", "wcag111") and criterion numbers ("1.1.1").
/// </summary>
public static class WcagLevelHelper
{
    private static readonly Dictionary<string, string> CriterionLevels = new() {
        // Level A
        ["1.1.1"] = "a", ["1.2.1"] = "a", ["1.2.2"] = "a", ["1.2.3"] = "a",
        ["1.3.1"] = "a", ["1.3.2"] = "a", ["1.3.3"] = "a",
        ["1.4.1"] = "a", ["1.4.2"] = "a",
        ["2.1.1"] = "a", ["2.1.2"] = "a", ["2.1.4"] = "a",
        ["2.2.1"] = "a", ["2.2.2"] = "a",
        ["2.3.1"] = "a",
        ["2.4.1"] = "a", ["2.4.2"] = "a", ["2.4.3"] = "a", ["2.4.4"] = "a",
        ["2.5.1"] = "a", ["2.5.2"] = "a", ["2.5.3"] = "a", ["2.5.4"] = "a",
        ["3.1.1"] = "a",
        ["3.2.1"] = "a", ["3.2.2"] = "a", ["3.2.6"] = "a",
        ["3.3.1"] = "a", ["3.3.2"] = "a", ["3.3.7"] = "a",
        ["4.1.2"] = "a",
        // Level AA
        ["1.2.5"] = "aa", ["1.3.4"] = "aa", ["1.3.5"] = "aa",
        ["1.4.3"] = "aa", ["1.4.4"] = "aa", ["1.4.5"] = "aa",
        ["1.4.10"] = "aa", ["1.4.11"] = "aa", ["1.4.12"] = "aa", ["1.4.13"] = "aa",
        ["2.4.5"] = "aa", ["2.4.6"] = "aa", ["2.4.7"] = "aa", ["2.4.11"] = "aa",
        ["2.5.7"] = "aa", ["2.5.8"] = "aa",
        ["3.1.2"] = "aa",
        ["3.3.3"] = "aa", ["3.3.4"] = "aa",
        ["4.1.3"] = "aa",
    };

    private static readonly Regex CriterionRegex = new(@"(\d+)\.?(\d+)\.?(\d+)", RegexOptions.Compiled);

    public static bool MatchesWcagLevel(string? wcagCriteria, string levelFilter)
    {
        if (String.IsNullOrWhiteSpace(wcagCriteria)) return true; // Unclassified — show.
        var allowed = levelFilter switch {
            "a" => new HashSet<string> { "a" },
            "aa" => new HashSet<string> { "a", "aa" },
            "aaa" => new HashSet<string> { "a", "aa", "aaa" },
            _ => null
        };
        if (allowed == null) return true; // "all"

        var tags = wcagCriteria.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Fail-open: only hide a violation if we definitively classified at least one of its
        // tags as belonging to a higher WCAG level than the user requested. Tags whose level
        // we cannot determine (e.g. WCAG 2.2 criteria missing from our local dictionary) do
        // not contribute to a "hide" decision — better to show too much than to silently
        // hide critical violations the user is hunting for.
        bool sawClassifiedTag = false;
        foreach (var tag in tags) {
            string lower = tag.ToLower();
            if (lower.StartsWith("wcag")) {
                string suffix = lower.Substring(4).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                if (suffix == "a" || suffix == "aa" || suffix == "aaa") {
                    sawClassifiedTag = true;
                    if (allowed.Contains(suffix)) return true;
                    continue;
                }
            }
            string? normalized = NormalizeCriterion(tag);
            if (normalized != null && CriterionLevels.TryGetValue(normalized, out string? level)) {
                sawClassifiedTag = true;
                if (allowed.Contains(level)) return true;
            }
            // Tag was non-numeric or numeric-but-unknown-to-our-dictionary — don't count
            // it as a definitive classification, leaving sawClassifiedTag false on this tag.
        }
        return !sawClassifiedTag;
    }

    private static string? NormalizeCriterion(string raw)
    {
        string cleaned = raw.Trim().ToLower()
            .Replace("wcag", "").Replace("sc", "").Replace("-", ".").Replace("_", ".").Trim('.');
        var match = CriterionRegex.Match(cleaned);
        if (match.Success) {
            return match.Groups[1].Value + "." + match.Groups[2].Value + "." + match.Groups[3].Value;
        }
        return null;
    }
}
