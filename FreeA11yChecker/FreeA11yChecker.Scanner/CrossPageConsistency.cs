using System.Text.Json;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Compares structural elements (navigation, headings, landmarks) across all pages
/// in a site scan to detect inconsistencies.
/// WCAG 3.2.3 (AA) — Consistent Navigation, 3.2.4 (AA) — Consistent Identification.
/// </summary>
public static class CrossPageConsistency
{
    /// <summary>
    /// Result of cross-page consistency comparison.
    /// </summary>
    public class ConsistencyResult
    {
        public List<ConsistencyFinding> Findings { get; set; } = new();
    }

    public class ConsistencyFinding
    {
        public string CheckType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public List<string> AffectedPages { get; set; } = new();
    }

    /// <summary>
    /// Compares the pages in a site result for structural consistency.
    /// Should be called after all pages have been scanned.
    /// </summary>
    public static string Compare(List<Models.PageScanResult> pages)
    {
        if (pages == null || pages.Count < 2) {
            return "[]";
        }

        var findings = new List<object>();

        try {
            // 1. Compare navigation structures — nav links should appear in the same order.
            CompareNavigationStructures(pages, findings);

            // 2. Compare landmark presence — all pages should have the same set of landmarks.
            CompareLandmarks(pages, findings);

            // 3. Compare heading hierarchy — each page should start with a single h1.
            CompareHeadingHierarchy(pages, findings);

            // 4. Check for pages missing lang attribute.
            CheckLanguageConsistency(pages, findings);
        } catch {
            // Cross-page analysis is best-effort.
        }

        return JsonSerializer.Serialize(findings);
    }

    private static void CompareNavigationStructures(List<Models.PageScanResult> pages, List<object> findings)
    {
        // Extract nav landmark labels from each page.
        var pageNavs = new Dictionary<string, List<string>>();

        foreach (var page in pages) {
            try {
                if (string.IsNullOrEmpty(page.LandmarksJson) || page.LandmarksJson == "[]") continue;
                using var doc = JsonDocument.Parse(page.LandmarksJson);
                var navLabels = new List<string>();
                foreach (var el in doc.RootElement.EnumerateArray()) {
                    string role = el.GetProperty("role").GetString() ?? "";
                    if (role == "navigation") {
                        navLabels.Add(el.GetProperty("label").GetString() ?? "(unlabeled)");
                    }
                }
                if (navLabels.Count > 0) {
                    pageNavs[page.Url] = navLabels;
                }
            } catch { }
        }

        if (pageNavs.Count < 2) return;

        // Find the most common nav structure (the "reference").
        var navStructures = pageNavs.Values.Select(n => string.Join("|", n)).ToList();
        string? mostCommon = navStructures.GroupBy(s => s).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        if (mostCommon == null) return;

        // Flag pages that deviate.
        var deviating = pageNavs.Where(kv => string.Join("|", kv.Value) != mostCommon).Select(kv => kv.Key).ToList();
        if (deviating.Count > 0) {
            findings.Add(new {
                checkType = "navigation-consistency",
                severity = "moderate",
                message = deviating.Count + " page(s) have a different navigation structure than the majority",
                affectedPages = deviating
            });
        }
    }

    private static void CompareLandmarks(List<Models.PageScanResult> pages, List<object> findings)
    {
        // Each page should have at least: main, banner/header, contentinfo/footer.
        var requiredRoles = new[] { "main" };

        foreach (var page in pages) {
            try {
                if (string.IsNullOrEmpty(page.LandmarksJson) || page.LandmarksJson == "[]") {
                    findings.Add(new {
                        checkType = "landmark-presence",
                        severity = "serious",
                        message = "Page has no landmark regions",
                        affectedPages = new List<string> { page.Url }
                    });
                    continue;
                }

                using var doc = JsonDocument.Parse(page.LandmarksJson);
                var roles = new HashSet<string>();
                foreach (var el in doc.RootElement.EnumerateArray()) {
                    roles.Add(el.GetProperty("role").GetString() ?? "");
                }

                foreach (string required in requiredRoles) {
                    if (!roles.Contains(required)) {
                        findings.Add(new {
                            checkType = "landmark-presence",
                            severity = "serious",
                            message = "Page is missing required '" + required + "' landmark",
                            affectedPages = new List<string> { page.Url }
                        });
                    }
                }
            } catch { }
        }
    }

    private static void CompareHeadingHierarchy(List<Models.PageScanResult> pages, List<object> findings)
    {
        foreach (var page in pages) {
            try {
                if (string.IsNullOrEmpty(page.HeadingsJson) || page.HeadingsJson == "[]") {
                    findings.Add(new {
                        checkType = "heading-hierarchy",
                        severity = "moderate",
                        message = "Page has no headings",
                        affectedPages = new List<string> { page.Url }
                    });
                    continue;
                }

                using var doc = JsonDocument.Parse(page.HeadingsJson);
                var headings = doc.RootElement.EnumerateArray().ToList();
                if (headings.Count == 0) continue;

                // Check that first heading is h1.
                int firstLevel = headings[0].GetProperty("level").GetInt32();
                if (firstLevel != 1) {
                    findings.Add(new {
                        checkType = "heading-hierarchy",
                        severity = "moderate",
                        message = "First heading is h" + firstLevel + " instead of h1",
                        affectedPages = new List<string> { page.Url }
                    });
                }

                // Count h1s.
                int h1Count = headings.Count(h => h.GetProperty("level").GetInt32() == 1);
                if (h1Count > 1) {
                    findings.Add(new {
                        checkType = "heading-hierarchy",
                        severity = "moderate",
                        message = "Page has " + h1Count + " h1 elements (expected exactly 1)",
                        affectedPages = new List<string> { page.Url }
                    });
                }

                // Check for level skipping (e.g., h1 → h3 skipping h2).
                for (int i = 1; i < headings.Count; i++) {
                    int prev = headings[i - 1].GetProperty("level").GetInt32();
                    int curr = headings[i].GetProperty("level").GetInt32();
                    if (curr > prev + 1) {
                        findings.Add(new {
                            checkType = "heading-hierarchy",
                            severity = "minor",
                            message = "Heading level skips from h" + prev + " to h" + curr,
                            affectedPages = new List<string> { page.Url }
                        });
                        break; // One finding per page is enough.
                    }
                }
            } catch { }
        }
    }

    private static void CheckLanguageConsistency(List<Models.PageScanResult> pages, List<object> findings)
    {
        var missingLang = pages.Where(p => string.IsNullOrWhiteSpace(p.Language)).Select(p => p.Url).ToList();
        if (missingLang.Count > 0) {
            findings.Add(new {
                checkType = "language-consistency",
                severity = "serious",
                message = missingLang.Count + " page(s) missing html lang attribute",
                affectedPages = missingLang
            });
        }

        // Check that all pages use the same language.
        var langs = pages.Where(p => !string.IsNullOrWhiteSpace(p.Language))
            .Select(p => p.Language!.Split('-')[0].ToLowerInvariant())
            .Distinct().ToList();
        if (langs.Count > 1) {
            findings.Add(new {
                checkType = "language-consistency",
                severity = "moderate",
                message = "Mixed languages detected across pages: " + string.Join(", ", langs),
                affectedPages = pages.Select(p => p.Url).ToList()
            });
        }
    }
}
