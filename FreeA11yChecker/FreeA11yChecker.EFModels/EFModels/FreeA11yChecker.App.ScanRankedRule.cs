using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanRankedRule
{
    [Key]
    public Guid ScanRankedRuleId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(200)]
    public string CanonicalRuleId { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Severity { get; set; } = null!;

    public int Consensus { get; set; }

    public double Confidence { get; set; }

    [MaxLength(500)]
    public string? ToolsFound { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
