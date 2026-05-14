using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class PageScanResult
{
    [Key]
    public Guid PageScanResultId { get; set; }

    [Required]
    public Guid ScanRunId { get; set; }

    [Required]
    public Guid SitePageId { get; set; }

    [Required, MaxLength(1000)]
    public string Url { get; set; } = null!;

    public int StatusCode { get; set; }

    [MaxLength(500)]
    public string? PageTitle { get; set; }

    public long HtmlSizeBytes { get; set; }

    [MaxLength(1000)]
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

    [MaxLength(1000)]
    public string? OutputDir { get; set; }

    [MaxLength(20)]
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

    public virtual ScanRun ScanRun { get; set; } = null!;

    public virtual ICollection<A11yViolation> A11yViolations { get; set; } = new List<A11yViolation>();

    public virtual ICollection<ScanScreenshot> ScanScreenshots { get; set; } = new List<ScanScreenshot>();

    public virtual ICollection<ScanImage> ScanImages { get; set; } = new List<ScanImage>();

    public virtual ICollection<ScanRankedRule> ScanRankedRules { get; set; } = new List<ScanRankedRule>();

    public virtual ICollection<ScanArtifact> ScanArtifacts { get; set; } = new List<ScanArtifact>();

    public virtual ScanCertificate? ScanCertificate { get; set; }
}
