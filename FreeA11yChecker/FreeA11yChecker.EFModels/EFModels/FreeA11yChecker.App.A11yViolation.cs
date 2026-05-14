using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class A11yViolation
{
    [Key]
    public Guid A11yViolationId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(50)]
    public string Tool { get; set; } = null!;

    [Required, MaxLength(200)]
    public string RuleId { get; set; } = null!;

    [MaxLength(200)]
    public string? CanonicalRuleId { get; set; }

    [Required, MaxLength(50)]
    public string Severity { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public string? Selector { get; set; }

    public string? HtmlSnippet { get; set; }

    [MaxLength(1000)]
    public string? HelpUrl { get; set; }

    [MaxLength(50)]
    public string? WcagCriteria { get; set; }

    [MaxLength(50)]
    public string? ContrastForeground { get; set; }

    [MaxLength(50)]
    public string? ContrastBackground { get; set; }

    public double? ContrastRatio { get; set; }

    public double? ContrastExpected { get; set; }

    [MaxLength(50)]
    public string? ContrastFontSize { get; set; }

    [MaxLength(50)]
    public string? ContrastFontWeight { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
