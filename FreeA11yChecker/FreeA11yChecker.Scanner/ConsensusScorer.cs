namespace FreeA11yChecker.Scanner;

using FreeA11yChecker.Scanner.Models;

/// <summary>
/// Merges accessibility results from all scanning tools, normalizes rule IDs
/// to canonical form, and ranks violations by cross-tool consensus.
/// </summary>
public static class ConsensusScorer
{
    /// <summary>
    /// Maps tool-specific rule IDs to canonical (axe-core) IDs for cross-tool matching.
    /// </summary>
    private static readonly Dictionary<string, string> RuleNormalization = new(StringComparer.OrdinalIgnoreCase)
    {
        // htmlcheck -> canonical
        ["img-alt"] = "image-alt",
        ["html-lang"] = "html-has-lang",
        ["html-lang-valid"] = "html-lang-valid",
        ["label-missing"] = "label",
        ["link-empty"] = "link-name",
        ["button-empty"] = "button-name",
        ["skip-link-missing"] = "skip-link",
        ["landmark-main"] = "landmark-one-main",
        ["heading-missing-h1"] = "page-has-heading-one",
        ["table-header-missing"] = "td-has-header",
        // htmlcs -> canonical
        ["WCAG2AA.Principle1.Guideline1_1.1_1_1.H37"] = "image-alt",
        ["WCAG2AA.Principle1.Guideline1_3.1_3_1.H44"] = "label",
        ["WCAG2AA.Principle3.Guideline3_1.3_1_1.H57.2"] = "html-has-lang",
        ["WCAG2AA.Principle2.Guideline2_4.2_4_1.H64.1"] = "frame-title",
        ["WCAG2AA.Principle2.Guideline2_4.2_4_2.H25.2"] = "document-title",
        ["WCAG2AA.Principle1.Guideline1_3.1_3_1.H49"] = "heading-order",
        ["WCAG2AA.Principle2.Guideline2_4.2_4_6.G130"] = "heading-order",
        ["WCAG2AA.Principle4.Guideline4_1.4_1_2.H91"] = "link-name",
        // ibm -> canonical
        ["img_alt_missing"] = "image-alt",
        ["input_label_missing"] = "label",
        ["html_lang_missing"] = "html-has-lang",
        ["html_lang_invalid"] = "html-lang-valid",
        ["heading_markup_misuse"] = "heading-order",
        ["frame_title_missing"] = "frame-title",
        ["aria_role_invalid"] = "aria-roles",
        ["aria_attr_invalid"] = "aria-valid-attr",
        ["a_text_purpose"] = "link-name",
        ["button_label_missing"] = "button-name",
    };

    /// <summary>
    /// Rules that specific tools CANNOT check. Used to calculate the "capable" denominator
    /// for consensus scoring.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> ToolCannotCheck = new()
    {
        ["htmlcheck"] = new HashSet<string>
        {
            "color-contrast", "color-contrast-enhanced", "aria-allowed-attr",
            "aria-hidden-body", "aria-required-attr", "aria-roles", "aria-valid-attr",
            "aria-valid-attr-value", "focus-order-semantics", "target-size"
        },
        ["htmlcs"] = new HashSet<string>
        {
            "color-contrast", "skip-link", "target-size", "landmark-one-main",
            "landmark-nav"
        },
        ["ibm"] = new HashSet<string>
        {
            "skip-link", "meta-refresh", "target-size"
        }
    };

    /// <summary>
    /// Merge results from all four tools, compute consensus ranking, and return a summary.
    /// </summary>
    public static A11yPageSummary Merge(
        List<A11yIssue> axeIssues,
        List<A11yIssue> htmlcheckIssues,
        List<A11yIssue> htmlcsIssues,
        List<A11yIssue> ibmIssues)
    {
        var summary = new A11yPageSummary();
        var allIssues = new List<A11yIssue>();
        var toolSets = new (string Name, List<A11yIssue> Issues)[]
        {
            ("axe", axeIssues), ("htmlcheck", htmlcheckIssues),
            ("htmlcs", htmlcsIssues), ("ibm", ibmIssues)
        };

        var completedTools = new List<string>();

        foreach (var (name, issues) in toolSets)
        {
            if (issues.Count > 0)
                completedTools.Add(name);

            allIssues.AddRange(issues);
        }

        // Severity totals across all tools (not de-duplicated)
        summary.TotalViolations = allIssues.Count;
        summary.CriticalCount = allIssues.Count(i => i.Severity == "critical");
        summary.SeriousCount = allIssues.Count(i => i.Severity == "serious");
        summary.ModerateCount = allIssues.Count(i => i.Severity == "moderate");
        summary.MinorCount = allIssues.Count(i => i.Severity == "minor");

        // Per-tool counts
        summary.AxeCount = axeIssues.Count;
        summary.HtmlCheckCount = htmlcheckIssues.Count;
        summary.HtmlCsCount = htmlcsIssues.Count;
        summary.IbmCount = ibmIssues.Count;

        // Normalize canonical rule IDs
        foreach (var issue in allIssues)
        {
            if (string.IsNullOrEmpty(issue.CanonicalRuleId))
                issue.CanonicalRuleId = NormalizeRuleId(issue.RuleId);
        }

        // Group by canonical rule and compute consensus
        var ruleGroups = allIssues.GroupBy(i => i.CanonicalRuleId).ToList();
        var ranked = new List<A11yRankedRule>();

        foreach (var group in ruleGroups)
        {
            var ruleId = group.Key;
            var toolsFound = group.Select(i => i.Tool).Distinct().ToList();

            // Determine which completed tools can check this rule
            var capableTools = completedTools
                .Where(t => !(ToolCannotCheck.TryGetValue(t, out var cantCheck) && cantCheck.Contains(ruleId)))
                .ToList();

            var capableCount = Math.Max(1, capableTools.Count);
            var score = (double)toolsFound.Count / capableCount;

            var representative = group.First();
            ranked.Add(new A11yRankedRule
            {
                CanonicalRuleId = ruleId,
                Severity = representative.Severity,
                Consensus = toolsFound.Count,
                Confidence = score,
                ToolsFound = toolsFound,
                Instances = group.ToList()
            });
        }

        // Sort: confidence desc, severity rank asc, instance count desc
        summary.RankedRules = ranked
            .OrderByDescending(r => r.Confidence)
            .ThenBy(r => SeverityRank(r.Severity))
            .ThenByDescending(r => r.Instances.Count)
            .ToList();

        return summary;
    }

    private static string NormalizeRuleId(string ruleId)
        => RuleNormalization.TryGetValue(ruleId, out var canonical) ? canonical : ruleId;

    private static int SeverityRank(string severity) => severity switch
    {
        "critical" => 0,
        "serious" => 1,
        "moderate" => 2,
        "minor" => 3,
        _ => 4
    };
}
