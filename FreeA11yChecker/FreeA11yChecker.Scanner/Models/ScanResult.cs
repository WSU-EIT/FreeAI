namespace FreeA11yChecker.Scanner.Models;

/// <summary>
/// Top-level result for a full scan run across all configured sites.
/// Returned by ScannerEngine.ScanAll().
/// </summary>
public class RunScanResult
{
    /// <summary>
    /// Results for each site that was scanned, keyed by base URL.
    /// </summary>
    public Dictionary<string, SiteScanResult> Sites { get; set; } = new Dictionary<string, SiteScanResult>();

    /// <summary>
    /// Total scan duration across all sites in milliseconds.
    /// </summary>
    public int TotalDurationMs { get; set; }

    /// <summary>
    /// Timestamp when the scan run started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the scan run completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Result for scanning a single site (all its pages).
/// Returned by ScannerEngine.ScanSite().
/// </summary>
public class SiteScanResult
{
    /// <summary>
    /// Base URL of the site that was scanned.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Results for each page that was scanned within this site.
    /// </summary>
    public List<PageScanResult> Pages { get; set; } = new List<PageScanResult>();

    /// <summary>
    /// Whether authentication was attempted for this site.
    /// </summary>
    public bool AuthAttempted { get; set; }

    /// <summary>
    /// Whether authentication succeeded (false if not attempted).
    /// </summary>
    public bool AuthSucceeded { get; set; }

    /// <summary>
    /// Total scan duration for this site in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Cross-page consistency findings as JSON (array of {checkType, severity, message, affectedPages}).
    /// Populated after all pages in the site have been scanned.
    /// </summary>
    public string CrossPageConsistencyJson { get; set; } = string.Empty;
}

/// <summary>
/// Result for scanning a single page. Contains all violations from all tools,
/// screenshot paths, HTML metadata, image catalog, and the merged summary.
/// Returned by ScannerEngine.ScanPage().
/// </summary>
public class PageScanResult
{
    /// <summary>
    /// Full URL that was scanned.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code from the page navigation.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Page title from the document.
    /// </summary>
    public string PageTitle { get; set; } = string.Empty;

    /// <summary>
    /// Size of the HTML content in bytes.
    /// </summary>
    public long HtmlSizeBytes { get; set; }

    /// <summary>
    /// Duration of the page scan in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Violations found by axe-core.
    /// </summary>
    public List<A11yIssue> AxeIssues { get; set; } = new List<A11yIssue>();

    /// <summary>
    /// Violations found by the HTML regex checker.
    /// </summary>
    public List<A11yIssue> HtmlCheckIssues { get; set; } = new List<A11yIssue>();

    /// <summary>
    /// Violations found by HTML_CodeSniffer.
    /// </summary>
    public List<A11yIssue> HtmlCsIssues { get; set; } = new List<A11yIssue>();

    /// <summary>
    /// Violations found by IBM Equal Access.
    /// </summary>
    public List<A11yIssue> IbmIssues { get; set; } = new List<A11yIssue>();

    /// <summary>
    /// Merged summary with cross-tool consensus scoring.
    /// </summary>
    public A11yPageSummary Summary { get; set; } = new A11yPageSummary();

    /// <summary>
    /// Screenshot files captured during the scan.
    /// </summary>
    public List<ScreenshotInfo> Screenshots { get; set; } = new List<ScreenshotInfo>();

    /// <summary>
    /// All images found on the page with their alt text status.
    /// </summary>
    public List<ImageInfo> Images { get; set; } = new List<ImageInfo>();

    /// <summary>
    /// SSL certificate information for the page, if available.
    /// </summary>
    public CertInfo? Certificate { get; set; }

    /// <summary>
    /// Error message if the page scan failed. Empty on success.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Output directory where results and screenshots were saved.
    /// </summary>
    public string OutputDir { get; set; } = string.Empty;

    /// <summary>
    /// Internal paths discovered on this page (same-host links).
    /// </summary>
    public List<string> DiscoveredPaths { get; set; } = new List<string>();

    /// <summary>
    /// External origin URLs discovered on this page (different-host links).
    /// </summary>
    public List<string> DiscoveredExternalUrls { get; set; } = new List<string>();

    /// <summary>
    /// HTTP response headers captured from the page navigation.
    /// </summary>
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();

    /// <summary>
    /// HTML lang attribute value (e.g., "en", "en-US"). Empty if not set.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Structured heading tree as JSON (array of {level, text, id}).
    /// </summary>
    public string HeadingsJson { get; set; } = string.Empty;

    /// <summary>
    /// Structured landmark inventory as JSON (array of {tag, role, label}).
    /// </summary>
    public string LandmarksJson { get; set; } = string.Empty;

    /// <summary>
    /// Media element inventory as JSON (array of {tag, src, hasControls, hasAutoplay, tracks[]}).
    /// </summary>
    public string MediaJson { get; set; } = string.Empty;

    /// <summary>
    /// ARIA live region inventory as JSON (array of {tag, role, ariaLive, text}).
    /// </summary>
    public string AriaLiveRegionsJson { get; set; } = string.Empty;

    /// <summary>
    /// Target size violations as JSON (array of {selector, tag, width, height, text}).
    /// Elements with interactive roles smaller than 24×24px (WCAG 2.2 AA).
    /// </summary>
    public string TargetSizeJson { get; set; } = string.Empty;

    /// <summary>
    /// Performance timing data as JSON ({ttfb, domContentLoaded, loadComplete, firstPaint, firstContentfulPaint}).
    /// </summary>
    public string PerformanceJson { get; set; } = string.Empty;

    /// <summary>
    /// Playwright accessibility tree snapshot as JSON.
    /// </summary>
    public string AccessibilityTreeJson { get; set; } = string.Empty;

    /// <summary>
    /// axe-core pass count — rules that passed successfully.
    /// </summary>
    public int AxePassCount { get; set; }

    /// <summary>
    /// axe-core incomplete count — rules that could not be determined automatically.
    /// </summary>
    public int AxeIncompleteCount { get; set; }

    /// <summary>
    /// axe-core inapplicable count — rules that do not apply to this page.
    /// </summary>
    public int AxeInapplicableCount { get; set; }

    /// <summary>
    /// IBM Equal Access pass count — rules that PASS on this page.
    /// </summary>
    public int IbmPassCount { get; set; }

    /// <summary>
    /// IBM Equal Access POTENTIAL count — rules that need human review.
    /// </summary>
    public int IbmPotentialCount { get; set; }

    /// <summary>
    /// IBM Equal Access MANUAL count — rules that require manual verification.
    /// </summary>
    public int IbmManualCount { get; set; }

    /// <summary>
    /// HtmlChecker pass count — checks that did not flag any issue on this page.
    /// </summary>
    public int HtmlCheckPassCount { get; set; }

    /// <summary>
    /// HtmlChecker total checks performed (pass + fail).
    /// </summary>
    public int HtmlCheckTotalChecks { get; set; }

    /// <summary>
    /// HTML CodeSniffer (HTMLCS) pass count — known WCAG2AA rules that did NOT
    /// produce a message on this page. Computed as TotalChecks - distinct-failed-rules.
    /// </summary>
    public int HtmlCsPassCount { get; set; }

    /// <summary>HTMLCS total rule count for the WCAG 2.0 AA standard (constant ~84).</summary>
    public int HtmlCsTotalChecks { get; set; }

    /// <summary>HTMLCS message-type-1 count (errors).</summary>
    public int HtmlCsErrorCount { get; set; }

    /// <summary>HTMLCS message-type-2 count (warnings).</summary>
    public int HtmlCsWarningCount { get; set; }

    /// <summary>HTMLCS message-type-3 count (notices — often informational, not failures).</summary>
    public int HtmlCsNoticeCount { get; set; }

    /// <summary>
    /// In-memory artifacts (JSON, HTML, markdown, logs) built during scanning.
    /// Each tuple contains (FileName, ContentType, Data).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public List<(string FileName, string ContentType, byte[] Data)> Artifacts { get; set; } = new();

    // ================================================================
    // Phase 3 — Expensive / Complex Checks
    // ================================================================

    /// <summary>
    /// Keyboard navigation focus order as JSON (array of {index, tag, role, selector, text, hasFocusIndicator}).
    /// </summary>
    public string KeyboardNavJson { get; set; } = string.Empty;

    /// <summary>
    /// Focus trap detection results as JSON (array of {selector, text, tabIndex, reason}).
    /// </summary>
    public string FocusTrapsJson { get; set; } = string.Empty;

    /// <summary>
    /// Text spacing test result as JSON ({clippedElements[], overflowElements[], screenshotLabel}).
    /// </summary>
    public string TextSpacingJson { get; set; } = string.Empty;

    /// <summary>
    /// Reading level analysis as JSON ({fleschKincaid, fleschEase, wordCount, sentenceCount, syllableCount, gradeLevel}).
    /// </summary>
    public string ReadingLevelJson { get; set; } = string.Empty;

    /// <summary>
    /// Autocomplete audit results as JSON (array of {selector, name, type, label, expectedAutocomplete, actualAutocomplete}).
    /// </summary>
    public string AutocompleteJson { get; set; } = string.Empty;

    /// <summary>
    /// Fixed/sticky element detection as JSON (array of {selector, position, tag, height, width, coversPercent}).
    /// </summary>
    public string FixedElementsJson { get; set; } = string.Empty;

    /// <summary>
    /// Mobile viewport scan results as JSON (array of {viewport, width, height, violationCount, critical, serious, targetSizeIssues}).
    /// </summary>
    public string MobileViewportsJson { get; set; } = string.Empty;
}

/// <summary>
/// A single accessibility violation found by one of the scanning tools.
/// This is the universal format — all tools (axe, htmlcheck, htmlcs, ibm) produce these.
/// </summary>
public class A11yIssue
{
    /// <summary>
    /// Which tool found this issue: "axe", "htmlcheck", "htmlcs", "ibm".
    /// </summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>
    /// Tool-specific rule ID (e.g., "image-alt" for axe, "img-alt" for htmlcheck).
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Canonical rule ID used for cross-tool consensus matching.
    /// Multiple tools may report the same underlying issue with different rule IDs;
    /// the canonical ID normalizes them.
    /// </summary>
    public string CanonicalRuleId { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: "critical", "serious", "moderate", "minor".
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the violation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// CSS selector identifying the affected element.
    /// </summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>
    /// HTML snippet of the affected element (truncated to 500 chars).
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// URL to documentation explaining how to fix this issue.
    /// </summary>
    public string HelpUrl { get; set; } = string.Empty;

    /// <summary>
    /// WCAG success criteria this violation relates to (e.g., "wcag2a, wcag21aa").
    /// </summary>
    public string WcagCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Foreground color for contrast violations (e.g., "#333333"). Populated from axe-core node.any[].data.
    /// </summary>
    public string? ContrastForeground { get; set; }

    /// <summary>
    /// Background color for contrast violations (e.g., "#ffffff"). Populated from axe-core node.any[].data.
    /// </summary>
    public string? ContrastBackground { get; set; }

    /// <summary>
    /// Actual contrast ratio (e.g., 3.2). Populated from axe-core node.any[].data.
    /// </summary>
    public double? ContrastRatio { get; set; }

    /// <summary>
    /// Required/expected contrast ratio (e.g., 4.5). Populated from axe-core node.any[].data.
    /// </summary>
    public double? ContrastExpected { get; set; }

    /// <summary>
    /// Font size in the contrast violation context. Populated from axe-core node.any[].data.
    /// </summary>
    public string? ContrastFontSize { get; set; }

    /// <summary>
    /// Font weight in the contrast violation context. Populated from axe-core node.any[].data.
    /// </summary>
    public string? ContrastFontWeight { get; set; }
}

/// <summary>
/// Merged accessibility summary for a page, produced by ConsensusScorer.
/// Contains all issues from all tools, ranked by cross-tool consensus.
/// </summary>
public class A11yPageSummary
{
    /// <summary>
    /// Total number of unique violations across all tools.
    /// </summary>
    public int TotalViolations { get; set; }

    /// <summary>
    /// Count by severity: critical, serious, moderate, minor.
    /// </summary>
    public int CriticalCount { get; set; }
    public int SeriousCount { get; set; }
    public int ModerateCount { get; set; }
    public int MinorCount { get; set; }

    /// <summary>
    /// Count by tool.
    /// </summary>
    public int AxeCount { get; set; }
    public int HtmlCheckCount { get; set; }
    public int HtmlCsCount { get; set; }
    public int IbmCount { get; set; }

    /// <summary>
    /// Rules ranked by cross-tool consensus. Rules found by multiple tools
    /// are ranked higher than rules found by only one tool.
    /// </summary>
    public List<A11yRankedRule> RankedRules { get; set; } = new List<A11yRankedRule>();
}

/// <summary>
/// A canonical rule ranked by how many tools agree on it.
/// Rules with higher consensus are more likely to be real issues.
/// </summary>
public class A11yRankedRule
{
    /// <summary>
    /// Canonical rule ID shared across tools.
    /// </summary>
    public string CanonicalRuleId { get; set; } = string.Empty;

    /// <summary>
    /// Highest severity reported by any tool for this rule.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Number of tools that found this rule (1-4).
    /// </summary>
    public int Consensus { get; set; }

    /// <summary>
    /// Confidence score based on consensus and severity (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Names of the tools that found this rule.
    /// </summary>
    public List<string> ToolsFound { get; set; } = new List<string>();

    /// <summary>
    /// All individual issue instances across all tools for this canonical rule.
    /// </summary>
    public List<A11yIssue> Instances { get; set; } = new List<A11yIssue>();
}

/// <summary>
/// Progress callback data passed to the OnProgress delegate during scanning.
/// Both the web app (SignalR) and console (stdout) use this to report status.
/// </summary>
public class ScanProgress
{
    /// <summary>
    /// Current page index (1-based) within the current site.
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total number of pages to scan in the current site.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Name or URL of the site currently being scanned.
    /// </summary>
    public string CurrentSite { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable status message (e.g., "Scanning /about...", "Running axe-core...").
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Metadata about a screenshot captured during the scan.
/// </summary>
public class ScreenshotInfo
{
    /// <summary>
    /// File path to the screenshot image.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive label for the screenshot (e.g., "page-loaded", "axe-overlay", "cvd-protanopia").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// In-memory screenshot binary data. When populated, no disk file is needed.
    /// </summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// MIME content type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// Information about an image found on the scanned page.
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// Source URL of the image.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Alt text value. Empty string if alt="" (decorative), null if no alt attribute.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Whether the image has an alt attribute at all.
    /// </summary>
    public bool HasAlt { get; set; }
}

/// <summary>
/// SSL certificate information extracted from the scanned page's connection.
/// </summary>
public class CertInfo
{
    /// <summary>
    /// Certificate subject (e.g., "CN=example.com").
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Certificate issuer (e.g., "CN=Let's Encrypt Authority X3").
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Certificate expiration date.
    /// </summary>
    public DateTime Expiry { get; set; }

    /// <summary>
    /// Subject Alternative Names (SANs) listed on the certificate.
    /// </summary>
    public List<string> SubjectAlternativeNames { get; set; } = new List<string>();
}
