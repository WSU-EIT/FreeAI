namespace FreeA11yChecker;

public partial class DataObjects
{
    public class ScanRun : ActionResponseObject
    {
        public Guid ScanRunId { get; set; }
        public Guid SiteId { get; set; }
        public Guid TenantId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Status { get; set; }
        public int TotalPages { get; set; }
        public int PagesScanned { get; set; }
        public int TotalViolations { get; set; }
        public int CriticalCount { get; set; }
        public int SeriousCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }
        public string? TriggeredBy { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public List<PageScanResult> PageResults { get; set; } = new();
    }

    public class PageScanResult : ActionResponseObject
    {
        public Guid PageScanResultId { get; set; }
        public Guid ScanRunId { get; set; }
        public Guid SitePageId { get; set; }
        public string Url { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? PageTitle { get; set; }
        public long HtmlSizeBytes { get; set; }
        public string? ScreenshotPath { get; set; }
        public int AxeViolationCount { get; set; }
        public int HtmlCheckViolationCount { get; set; }
        public int OverlayViolationCount { get; set; }
        public int TotalViolations { get; set; }
        public int CriticalCount { get; set; }
        public int SeriousCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }
        public int ScanDurationMs { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputDir { get; set; }
        public string? Language { get; set; }
        public string? ResponseHeaders { get; set; }
        public int AxePassCount { get; set; }
        public int AxeIncompleteCount { get; set; }
        public int AxeInapplicableCount { get; set; }
        public int IbmPassCount { get; set; }
        public int IbmPotentialCount { get; set; }
        public int IbmManualCount { get; set; }
        public int HtmlCheckPassCount { get; set; }
        public int HtmlCheckTotalChecks { get; set; }
        public string? HeadingsJson { get; set; }
        public string? LandmarksJson { get; set; }
        public string? MediaJson { get; set; }
        public string? AriaLiveRegionsJson { get; set; }
        public string? TargetSizeJson { get; set; }
        public string? PerformanceJson { get; set; }
        public string? AccessibilityTreeJson { get; set; }
        public string? KeyboardNavJson { get; set; }
        public string? FocusTrapsJson { get; set; }
        public string? TextSpacingJson { get; set; }
        public string? ReadingLevelJson { get; set; }
        public string? AutocompleteJson { get; set; }
        public string? FixedElementsJson { get; set; }
        public string? MobileViewportsJson { get; set; }
        public List<A11yViolation> Violations { get; set; } = new();
        public List<ScanScreenshot> Screenshots { get; set; } = new();
        public List<ScanImage> Images { get; set; } = new();
        public List<ScanRankedRule> RankedRules { get; set; } = new();
        public ScanCertificate? Certificate { get; set; }
    }

    public class A11yViolation : ActionResponseObject
    {
        public Guid A11yViolationId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string Tool { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string? CanonicalRuleId { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Selector { get; set; }
        public string? HtmlSnippet { get; set; }
        public string? HelpUrl { get; set; }
        public string? WcagCriteria { get; set; }
        public string? ContrastForeground { get; set; }
        public string? ContrastBackground { get; set; }
        public double? ContrastRatio { get; set; }
        public double? ContrastExpected { get; set; }
        public string? ContrastFontSize { get; set; }
        public string? ContrastFontWeight { get; set; }
        public int AffectedPageCount { get; set; }
        // Display-only fields (computed at read time from ViolationSuppressions table).
        public bool IsSuppressed { get; set; }
        public string? SuppressionReason { get; set; }
        public string? OriginalSeverity { get; set; }
    }

    public class ScanScreenshot
    {
        public Guid ScanScreenshotId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? ContentType { get; set; }
        public byte[]? Data { get; set; }
    }

    public class ScanArtifact
    {
        public Guid ScanArtifactId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public byte[]? Data { get; set; }
        public long SizeBytes { get; set; }
    }

    public class ScanImage
    {
        public Guid ScanImageId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool HasAlt { get; set; }
    }

    public class ScanCertificate
    {
        public Guid ScanCertificateId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string? Subject { get; set; }
        public string? Issuer { get; set; }
        public DateTime? Expiry { get; set; }
        public string? SubjectAlternativeNames { get; set; }
    }

    public class ScanRankedRule
    {
        public Guid ScanRankedRuleId { get; set; }
        public Guid PageScanResultId { get; set; }
        public string CanonicalRuleId { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int Consensus { get; set; }
        public double Confidence { get; set; }
        public string? ToolsFound { get; set; }
    }

    public class ManualCheckResult : ActionResponseObject
    {
        public Guid ManualCheckResultId { get; set; }
        public Guid SiteId { get; set; }
        public string WcagCriterion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string TestedBy { get; set; } = string.Empty;
        public DateTime? TestedAt { get; set; }
    }

    public class ScanProgress
    {
        public Guid ScanRunId { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string CurrentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Running totals — updated after each page completes.
        public int PagesScanned { get; set; }
        public int TotalViolations { get; set; }
        public int CriticalCount { get; set; }
        public int SeriousCount { get; set; }
        public int ModerateCount { get; set; }
        public int MinorCount { get; set; }

        // The page result that just completed (null for pre-scan progress).
        public PageScanResult? CompletedPageResult { get; set; }
    }

    public class ScanLogEntry
    {
        public Guid ScanRunId { get; set; }
        public Guid SiteId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "info";   // info, success, warning, error, detail
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }          // nav, axe, htmlcheck, quickpeek, discovery, auth, result, system
    }

    public class AuditReport
    {
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public string WcagLevel { get; set; } = "wcag22aa";
        public DateTime? LastScanAt { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int TotalCriteria { get; set; }
        public int EvaluatedCount { get; set; }
        public int SupportsCount { get; set; }
        public int PartialCount { get; set; }
        public int DoesNotSupportCount { get; set; }
        public int NotEvaluatedCount { get; set; }
        public List<WcagCriterionStatus> Criteria { get; set; } = new();
    }

    public class WcagCriterionStatus
    {
        public string Criterion { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Principle { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string ConformanceLevel { get; set; } = "Not Evaluated";
        public string Source { get; set; } = string.Empty;
        public int AutomatedViolationCount { get; set; }
        public string ManualStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool CanBeAutomated { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TestInstructions { get; set; } = string.Empty;
    }

    public class WcagCriterionDefinition
    {
        public string Criterion { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Principle { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public bool CanBeAutomated { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TestInstructions { get; set; } = string.Empty;
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
        public Guid SiteId { get; set; }
        public int Count { get; set; }
    }

    public class ViolationsByRuleFilter
    {
        public Guid ScanRunId { get; set; }
        public string? CanonicalRuleId { get; set; }
    }

    public class AllSiteScanHistoryFilter
    {
        public int CountPerSite { get; set; } = 30;
    }

    public class CrossSiteViolationFilter
    {
        public List<Guid>? SiteIds { get; set; }
        public string? UrlPattern { get; set; }
        public string? Severity { get; set; }
        public string? WcagLevel { get; set; }
        public string? CanonicalRuleId { get; set; }
        public bool LatestScanOnly { get; set; } = true;
    }

    public class CrossSiteViolation
    {
        public Guid A11yViolationId { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public string SiteBaseUrl { get; set; } = string.Empty;
        public Guid ScanRunId { get; set; }
        public DateTime ScanDate { get; set; }
        public Guid PageScanResultId { get; set; }
        public string PageUrl { get; set; } = string.Empty;
        public string? PageTitle { get; set; }
        public string Tool { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string? CanonicalRuleId { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Selector { get; set; }
        public string? HelpUrl { get; set; }
        public string? WcagCriteria { get; set; }
        public bool IsSuppressed { get; set; }
        public string? SuppressionReason { get; set; }
        public string? OriginalSeverity { get; set; }
    }

    public class ViolationSuppression : ActionResponseObject
    {
        public Guid ViolationSuppressionId { get; set; }
        public Guid TenantId { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public string? SelectorPattern { get; set; }
        public Guid? SiteId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
