namespace FreeAccessibilityChecker;

// ---- FreeAccessibilityChecker.App.Config.cs ----
public partial interface IConfigurationHelper
{
    string ScanEncryptionKey { get; }
    bool ScanHeadless { get; }
    int ScanMaxConcurrency { get; }
    int ScanSettleDelayMs { get; }
    int ScanTimeoutMs { get; }
    string ScanUserAgent { get; }
    string ScanWcagLevel { get; }
}

public partial class ConfigurationHelper : IConfigurationHelper
{
    public string ScanEncryptionKey {
        get {
            return _loader.ScanEncryptionKey;
        }
    }

    public bool ScanHeadless {
        get {
            return _loader.ScanHeadless;
        }
    }

    public int ScanMaxConcurrency {
        get {
            return _loader.ScanMaxConcurrency;
        }
    }
    public int ScanSettleDelayMs {
        get {
            return _loader.ScanSettleDelayMs;
        }
    }

    public int ScanTimeoutMs {
        get {
            return _loader.ScanTimeoutMs;
        }
    }

    public string ScanUserAgent {
        get {
            return _loader.ScanUserAgent;
        }
    }

    public string ScanWcagLevel {
        get {
            return _loader.ScanWcagLevel;
        }
    }
}

public partial class ConfigurationHelperLoader
{
    public string ScanEncryptionKey { get; set; } = "";
    public bool ScanHeadless { get; set; } = true;
    public int ScanMaxConcurrency { get; set; } = 5;
    public int ScanSettleDelayMs { get; set; } = 5000;
    public int ScanTimeoutMs { get; set; } = 30000;
    public string ScanUserAgent { get; set; } = "";
    public string ScanWcagLevel { get; set; } = "wcag21aa";
}

// ---- FreeAccessibilityChecker.App.DataObjects.Scans.cs ----
public partial class DataObjects
{
    public class ScanRun : ActionResponseObject
    {
        public DateTime? CompletedAt { get; set; }
        public int CriticalCount { get; set; }
        public int MinorCount { get; set; }
        public int ModerateCount { get; set; }
        public List<PageScanResult> PageResults { get; set; } = new();
        public int PagesScanned { get; set; }
        public Guid ScanRunId { get; set; }
        public int SeriousCount { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public string? Status { get; set; }
        public Guid TenantId { get; set; }
        public int TotalPages { get; set; }
        public int TotalViolations { get; set; }
        public string? TriggeredBy { get; set; }
    }

    public class PageScanResult : ActionResponseObject
    {
        public string? AccessibilityTreeJson { get; set; }
        public string? AriaLiveRegionsJson { get; set; }
        public string? AutocompleteJson { get; set; }
        public int AxeInapplicableCount { get; set; }
        public int AxeIncompleteCount { get; set; }
        public int AxePassCount { get; set; }
        public int AxeViolationCount { get; set; }
        public ScanCertificate? Certificate { get; set; }
        public int CriticalCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FixedElementsJson { get; set; }
        public string? FocusTrapsJson { get; set; }
        public string? HeadingsJson { get; set; }
        public int HtmlCheckPassCount { get; set; }
        public int HtmlCheckTotalChecks { get; set; }
        public int HtmlCheckViolationCount { get; set; }
        public long HtmlSizeBytes { get; set; }
        public int IbmManualCount { get; set; }
        public int IbmPassCount { get; set; }
        public int IbmPotentialCount { get; set; }
        public List<ScanImage> Images { get; set; } = new();
        public string? KeyboardNavJson { get; set; }
        public string? LandmarksJson { get; set; }
        public string? Language { get; set; }
        public string? MediaJson { get; set; }
        public int MinorCount { get; set; }
        public string? MobileViewportsJson { get; set; }
        public int ModerateCount { get; set; }
        public string? OutputDir { get; set; }
        public int OverlayViolationCount { get; set; }
        public Guid PageScanResultId { get; set; }
        public string? PageTitle { get; set; }
        public string? PerformanceJson { get; set; }
        public List<ScanRankedRule> RankedRules { get; set; } = new();
        public string? ReadingLevelJson { get; set; }
        public string? ResponseHeaders { get; set; }
        public int ScanDurationMs { get; set; }
        public Guid ScanRunId { get; set; }
        public string? ScreenshotPath { get; set; }
        public List<ScanScreenshot> Screenshots { get; set; } = new();
        public int SeriousCount { get; set; }
        public Guid SitePageId { get; set; }
        public int StatusCode { get; set; }
        public string? TargetSizeJson { get; set; }
        public string? TextSpacingJson { get; set; }
        public int TotalViolations { get; set; }
        public string Url { get; set; } = string.Empty;
        public List<A11yViolation> Violations { get; set; } = new();
    }

    public class A11yViolation : ActionResponseObject
    {
        public Guid A11yViolationId { get; set; }
        public int AffectedPageCount { get; set; }
        public string? CanonicalRuleId { get; set; }
        public string? ContrastBackground { get; set; }
        public double? ContrastExpected { get; set; }
        public string? ContrastFontSize { get; set; }
        public string? ContrastFontWeight { get; set; }
        public string? ContrastForeground { get; set; }
        public double? ContrastRatio { get; set; }
        public string? HelpUrl { get; set; }
        public string? HtmlSnippet { get; set; }
        // Display-only fields (computed at read time from ViolationSuppressions table).
        public bool IsSuppressed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OriginalSeverity { get; set; }
        public Guid PageScanResultId { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public string? Selector { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? SuppressionReason { get; set; }
        public string Tool { get; set; } = string.Empty;
        public string? WcagCriteria { get; set; }
    }

    public class ScanScreenshot
    {
        public string? ContentType { get; set; }
        public byte[]? Data { get; set; }
        public string Label { get; set; } = string.Empty;
        public Guid PageScanResultId { get; set; }
        public string Path { get; set; } = string.Empty;
        public Guid ScanScreenshotId { get; set; }
        public long SizeBytes { get; set; }
    }

    public class ScanArtifact
    {
        public string? ContentType { get; set; }
        public byte[]? Data { get; set; }
        public string FileName { get; set; } = string.Empty;
        public Guid PageScanResultId { get; set; }
        public string Path { get; set; } = string.Empty;
        public Guid ScanArtifactId { get; set; }
        public long SizeBytes { get; set; }
    }

    public class ScanImage
    {
        public string? AltText { get; set; }
        public bool HasAlt { get; set; }
        public Guid PageScanResultId { get; set; }
        public Guid ScanImageId { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class ScanCertificate
    {
        public DateTime? Expiry { get; set; }
        public string? Issuer { get; set; }
        public Guid PageScanResultId { get; set; }
        public Guid ScanCertificateId { get; set; }
        public string? Subject { get; set; }
        public string? SubjectAlternativeNames { get; set; }
    }

    public class ScanRankedRule
    {
        public string CanonicalRuleId { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int Consensus { get; set; }
        public Guid PageScanResultId { get; set; }
        public Guid ScanRankedRuleId { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? ToolsFound { get; set; }
    }

    public class ManualCheckResult : ActionResponseObject
    {
        public Guid ManualCheckResultId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? TestedAt { get; set; }
        public string TestedBy { get; set; } = string.Empty;
        public string WcagCriterion { get; set; } = string.Empty;
    }

    public class ScanProgress
    {
        // The page result that just completed (null for pre-scan progress).
        public PageScanResult? CompletedPageResult { get; set; }
        public int CriticalCount { get; set; }
        public int CurrentPage { get; set; }
        public string CurrentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int MinorCount { get; set; }
        public int ModerateCount { get; set; }

        // Running totals — updated after each page completes.
        public int PagesScanned { get; set; }
        public Guid ScanRunId { get; set; }
        public int SeriousCount { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public int TotalPages { get; set; }
        public int TotalViolations { get; set; }
    }

    public class ScanLogEntry
    {
        public string? Category { get; set; }          // nav, axe, htmlcheck, quickpeek, discovery, auth, result, system
        public string Level { get; set; } = "info";   // info, success, warning, error, detail
        public string Message { get; set; } = string.Empty;
        public Guid ScanRunId { get; set; }
        public Guid SiteId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ScanRunFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid? SiteId { get; set; }
    }

    public class PageScanResultFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid? ScanRunId { get; set; }
    }

    public class ViolationFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid? PageScanResultId { get; set; }
    }

    public class ScanHistoryFilter
    {
        public int Count { get; set; }
        public Guid SiteId { get; set; }
    }

    public class ViolationsByRuleFilter
    {
        public string? CanonicalRuleId { get; set; }
        public Guid ScanRunId { get; set; }
    }

    public class AllSiteScanHistoryFilter
    {
        public int CountPerSite { get; set; } = 30;
    }

    public class CrossSiteViolationFilter
    {
        public string? CanonicalRuleId { get; set; }
        public bool LatestScanOnly { get; set; } = true;
        public string? Severity { get; set; }
        public List<Guid>? SiteIds { get; set; }
        public string? UrlPattern { get; set; }
        public string? WcagLevel { get; set; }
    }

    public class CrossSiteViolation
    {
        public Guid A11yViolationId { get; set; }
        public string? CanonicalRuleId { get; set; }
        public string? HelpUrl { get; set; }
        public bool IsSuppressed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OriginalSeverity { get; set; }
        public Guid PageScanResultId { get; set; }
        public string? PageTitle { get; set; }
        public string PageUrl { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public DateTime ScanDate { get; set; }
        public Guid ScanRunId { get; set; }
        public string? Selector { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string SiteBaseUrl { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string? SuppressionReason { get; set; }
        public string Tool { get; set; } = string.Empty;
        public string? WcagCriteria { get; set; }
    }

    public class ViolationSuppression : ActionResponseObject
    {
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string? SelectorPattern { get; set; }
        public Guid? SiteId { get; set; }
        public Guid TenantId { get; set; }
        public Guid ViolationSuppressionId { get; set; }
    }
}

// ---- FreeAccessibilityChecker.App.DataObjects.Sites.cs ----
public partial class DataObjects
{
    public partial class BlazorDataModelLoader
    {
        /// <summary>
        /// Site summaries loaded during GetBlazorDataModelApp for the current tenant.
        /// </summary>
        public List<DataObjects.Site> SiteList { get; set; } = new List<DataObjects.Site>();
    }

    public class Site : ActionResponseObject
    {
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public List<SiteCredential> Credentials { get; set; } = new();
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool Enabled { get; set; }
        public bool IsFreeCRMApp { get; set; }
        public int LastCriticalCount { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastScanAt { get; set; }
        public Guid? LastScanRunId { get; set; }
        public string? LastScanStatus { get; set; }
        public int LastViolationCount { get; set; }
        public int MaxConcurrency { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SitePage> Pages { get; set; } = new();
        public bool PublicVisible { get; set; }
        public string ScanScheduleCron { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public Guid TenantId { get; set; }
    }

    public class SitePage : ActionResponseObject
    {
        public bool Enabled { get; set; }
        public bool IncludeInScan { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool RequiresAuth { get; set; }
        public Guid SiteId { get; set; }
        public Guid SitePageId { get; set; }
        public int SortOrder { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class SiteCredential : ActionResponseObject
    {
        public string AuthType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string PasswordEncrypted { get; set; } = string.Empty;
        public string PasswordSelector { get; set; } = string.Empty;
        public Guid SiteCredentialId { get; set; }
        public Guid SiteId { get; set; }
        public string SubmitSelector { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = string.Empty;
    }

    public class SiteChildFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid SiteId { get; set; }
    }

    public class DiscoverLinksFilter
    {
        public List<string> AdditionalUrls { get; set; } = new();
        public Guid SiteId { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class DiscoveredLink
    {
        public bool IsInternal { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class DiscoverLinksResult
    {
        public string? ErrorMessage { get; set; }
        public List<DiscoveredLink> ExternalLinks { get; set; } = new();
        public List<DiscoveredLink> InternalLinks { get; set; } = new();
    }
}

// ---- FreeAccessibilityChecker.App.DataObjects.SignalR.cs ----
// FreeAccessibilityChecker SignalR update types, sent by the scanner and handled in FreeAccessibilityCheckerProcessSignalRUpdateApp.

public partial class DataObjects
{
    public partial class SignalRUpdateType
    {
        public const string ScanQueued = "ScanQueued";
        public const string ScanStarted = "ScanStarted";
        public const string ScanProgress = "ScanProgress";
        public const string ScanComplete = "ScanComplete";
        public const string ScanFailed = "ScanFailed";
        public const string ScanLog = "ScanLog";
    }
}

// ---- FreeAccessibilityChecker.App.DataObjects.Wcag.cs ----
public partial class DataObjects
{
    public class AuditReport
    {
        public List<WcagCriterionStatus> Criteria { get; set; } = new();
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int DoesNotSupportCount { get; set; }
        public int EvaluatedCount { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? LastScanAt { get; set; }
        public int NotEvaluatedCount { get; set; }
        public int PartialCount { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public int SupportsCount { get; set; }
        public int TotalCriteria { get; set; }
        public string WcagLevel { get; set; } = "wcag22aa";
    }

    public class WcagCriterionStatus
    {
        public int AutomatedViolationCount { get; set; }
        public bool CanBeAutomated { get; set; }
        public string ConformanceLevel { get; set; } = "Not Evaluated";
        public string Criterion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string ManualStatus { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Principle { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string TestInstructions { get; set; } = string.Empty;
    }

    public class WcagCriterionDefinition
    {
        public bool CanBeAutomated { get; set; }
        public string Criterion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Principle { get; set; } = string.Empty;
        public string TestInstructions { get; set; } = string.Empty;
    }

    /// <summary>
    /// The WCAG 2.x success-criteria table (A and AA, through 2.2) the audit report, compliance status,
    /// manual-check wizard and site editor all read. One list, so the criteria are defined once.
    /// </summary>
    public static class WcagCriteria
    {
        public static readonly IReadOnlyList<WcagCriterionDefinition> All = new List<WcagCriterionDefinition> {
            // Perceivable
            new() { Criterion = "1.1.1", Name = "Non-text Content", Principle = "Perceivable", Level = "A", CanBeAutomated = true, Description = "All non-text content has a text alternative that serves the equivalent purpose." },
            new() { Criterion = "1.2.1", Name = "Audio-only and Video-only", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "Alternatives are provided for time-based media (audio-only or video-only content)." },
            new() { Criterion = "1.2.2", Name = "Captions (Prerecorded)", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "Captions are provided for all prerecorded audio content in synchronized media." },
            new() { Criterion = "1.2.3", Name = "Audio Description or Media Alternative", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "An alternative or audio description is provided for prerecorded video content." },
            new() { Criterion = "1.2.5", Name = "Audio Description (Prerecorded)", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Audio description is provided for all prerecorded video content." },
            new() { Criterion = "1.3.1", Name = "Info and Relationships", Principle = "Perceivable", Level = "A", CanBeAutomated = true, Description = "Information, structure, and relationships conveyed through presentation can be programmatically determined." },
            new() { Criterion = "1.3.2", Name = "Meaningful Sequence", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "The correct reading sequence can be programmatically determined when sequence affects meaning." },
            new() { Criterion = "1.3.3", Name = "Sensory Characteristics", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "Instructions do not rely solely on sensory characteristics like shape, color, size, or location." },
            new() { Criterion = "1.3.4", Name = "Orientation", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Content does not restrict its view and operation to a single display orientation unless essential." },
            new() { Criterion = "1.3.5", Name = "Identify Input Purpose", Principle = "Perceivable", Level = "AA", CanBeAutomated = true, Description = "The purpose of input fields collecting user information can be programmatically determined." },
            new() { Criterion = "1.4.1", Name = "Use of Color", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "Color is not used as the only visual means of conveying information." },
            new() { Criterion = "1.4.2", Name = "Audio Control", Principle = "Perceivable", Level = "A", CanBeAutomated = false, Description = "If audio plays automatically for more than 3 seconds, there is a mechanism to pause or control volume." },
            new() { Criterion = "1.4.3", Name = "Contrast (Minimum)", Principle = "Perceivable", Level = "AA", CanBeAutomated = true, Description = "Text and images of text have a contrast ratio of at least 4.5:1 (3:1 for large text)." },
            new() { Criterion = "1.4.4", Name = "Resize Text", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Text can be resized up to 200% without loss of content or functionality." },
            new() { Criterion = "1.4.5", Name = "Images of Text", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Text is used to convey information rather than images of text, unless customizable or essential." },
            new() { Criterion = "1.4.10", Name = "Reflow", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Content can be presented without loss of information at 320 CSS px wide without horizontal scrolling." },
            new() { Criterion = "1.4.11", Name = "Non-text Contrast", Principle = "Perceivable", Level = "AA", CanBeAutomated = true, Description = "Visual presentation of UI components and graphical objects have a contrast ratio of at least 3:1." },
            new() { Criterion = "1.4.12", Name = "Text Spacing", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "No loss of content or functionality when overriding text spacing properties." },
            new() { Criterion = "1.4.13", Name = "Content on Hover or Focus", Principle = "Perceivable", Level = "AA", CanBeAutomated = false, Description = "Additional content triggered by hover or focus is dismissible, hoverable, and persistent." },
            // Operable
            new() { Criterion = "2.1.1", Name = "Keyboard", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "All functionality is operable through a keyboard interface without requiring specific timings." },
            new() { Criterion = "2.1.2", Name = "No Keyboard Trap", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Keyboard focus can always be moved away from a component using standard navigation." },
            new() { Criterion = "2.1.4", Name = "Character Key Shortcuts", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "If character key shortcuts exist, they can be turned off, remapped, or are only active on focus." },
            new() { Criterion = "2.2.1", Name = "Timing Adjustable", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Users can turn off, adjust, or extend time limits set by the content." },
            new() { Criterion = "2.2.2", Name = "Pause, Stop, Hide", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Moving, blinking, scrolling, or auto-updating information can be paused, stopped, or hidden." },
            new() { Criterion = "2.3.1", Name = "Three Flashes or Below Threshold", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Content does not contain anything that flashes more than three times per second." },
            new() { Criterion = "2.4.1", Name = "Bypass Blocks", Principle = "Operable", Level = "A", CanBeAutomated = true, Description = "A mechanism is available to bypass blocks of content that are repeated on multiple pages." },
            new() { Criterion = "2.4.2", Name = "Page Titled", Principle = "Operable", Level = "A", CanBeAutomated = true, Description = "Web pages have titles that describe their topic or purpose." },
            new() { Criterion = "2.4.3", Name = "Focus Order", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Focusable components receive focus in an order that preserves meaning and operability." },
            new() { Criterion = "2.4.4", Name = "Link Purpose (In Context)", Principle = "Operable", Level = "A", CanBeAutomated = true, Description = "The purpose of each link can be determined from the link text alone or together with context." },
            new() { Criterion = "2.4.5", Name = "Multiple Ways", Principle = "Operable", Level = "AA", CanBeAutomated = false, Description = "More than one way is available to locate a web page within a set of web pages." },
            new() { Criterion = "2.4.6", Name = "Headings and Labels", Principle = "Operable", Level = "AA", CanBeAutomated = true, Description = "Headings and labels describe topic or purpose." },
            new() { Criterion = "2.4.7", Name = "Focus Visible", Principle = "Operable", Level = "AA", CanBeAutomated = false, Description = "Any keyboard operable user interface has a mode where the keyboard focus indicator is visible." },
            new() { Criterion = "2.4.11", Name = "Focus Not Obscured (Minimum)", Principle = "Operable", Level = "AA", CanBeAutomated = false, Description = "When a UI component receives keyboard focus, it is not entirely hidden by author-created content." },
            new() { Criterion = "2.5.1", Name = "Pointer Gestures", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Functionality using multipoint or path-based gestures can also be operated with a single pointer." },
            new() { Criterion = "2.5.2", Name = "Pointer Cancellation", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Functionality operated by a single pointer can be cancelled, and the up-event completes the function." },
            new() { Criterion = "2.5.3", Name = "Label in Name", Principle = "Operable", Level = "A", CanBeAutomated = true, Description = "For UI components with labels that include text or images of text, the accessible name contains the visible text." },
            new() { Criterion = "2.5.4", Name = "Motion Actuation", Principle = "Operable", Level = "A", CanBeAutomated = false, Description = "Functionality triggered by device motion can also be operated by UI components and motion can be disabled." },
            new() { Criterion = "2.5.7", Name = "Dragging Movements", Principle = "Operable", Level = "AA", CanBeAutomated = false, Description = "Functionality that uses dragging can also be achieved with a single pointer without dragging." },
            new() { Criterion = "2.5.8", Name = "Target Size (Minimum)", Principle = "Operable", Level = "AA", CanBeAutomated = true, Description = "The size of the target for pointer inputs is at least 24 by 24 CSS pixels, with some exceptions." },
            // Understandable
            new() { Criterion = "3.1.1", Name = "Language of Page", Principle = "Understandable", Level = "A", CanBeAutomated = true, Description = "The default human language of each web page can be programmatically determined." },
            new() { Criterion = "3.1.2", Name = "Language of Parts", Principle = "Understandable", Level = "AA", CanBeAutomated = true, Description = "The human language of each passage or phrase can be programmatically determined." },
            new() { Criterion = "3.2.1", Name = "On Focus", Principle = "Understandable", Level = "A", CanBeAutomated = false, Description = "Receiving focus on a component does not initiate a change of context." },
            new() { Criterion = "3.2.2", Name = "On Input", Principle = "Understandable", Level = "A", CanBeAutomated = false, Description = "Changing the setting of a UI component does not automatically cause a change of context." },
            new() { Criterion = "3.2.6", Name = "Consistent Help", Principle = "Understandable", Level = "A", CanBeAutomated = false, Description = "Help mechanisms occur in the same relative order on each page within a set of web pages." },
            new() { Criterion = "3.3.1", Name = "Error Identification", Principle = "Understandable", Level = "A", CanBeAutomated = false, Description = "If an input error is automatically detected, the item in error is identified and described to the user in text." },
            new() { Criterion = "3.3.2", Name = "Labels or Instructions", Principle = "Understandable", Level = "A", CanBeAutomated = true, Description = "Labels or instructions are provided when content requires user input." },
            new() { Criterion = "3.3.3", Name = "Error Suggestion", Principle = "Understandable", Level = "AA", CanBeAutomated = false, Description = "If an input error is detected and suggestions are known, they are provided to the user." },
            new() { Criterion = "3.3.4", Name = "Error Prevention (Legal, Financial, Data)", Principle = "Understandable", Level = "AA", CanBeAutomated = false, Description = "For pages with legal commitments or financial transactions, submissions are reversible, checked, or confirmed." },
            new() { Criterion = "3.3.7", Name = "Redundant Entry", Principle = "Understandable", Level = "A", CanBeAutomated = false, Description = "Information previously entered by the user is auto-populated or available for selection." },
            // Robust
            new() { Criterion = "4.1.2", Name = "Name, Role, Value", Principle = "Robust", Level = "A", CanBeAutomated = true, Description = "For all UI components, the name, role, and state can be programmatically determined and set." },
            new() { Criterion = "4.1.3", Name = "Status Messages", Principle = "Robust", Level = "AA", CanBeAutomated = false, Description = "Status messages can be programmatically determined through role or properties without receiving focus." },
        };

        /// <summary>The criterion's name, or the number itself when the criterion is not in the table.</summary>
        public static string NameOf(string criterion)
        {
            foreach (var item in All) {
                if (item.Criterion == criterion) return item.Name;
            }
            return criterion;
        }
    }
}
