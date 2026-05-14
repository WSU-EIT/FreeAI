using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanRun
{
    [Key]
    public Guid ScanRunId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = null!;

    public int TotalPages { get; set; }

    public int PagesScanned { get; set; }

    public int TotalViolations { get; set; }

    public int CriticalCount { get; set; }

    public int SeriousCount { get; set; }

    public int ModerateCount { get; set; }

    public int MinorCount { get; set; }

    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    public virtual Site Site { get; set; } = null!;

    public virtual ICollection<PageScanResult> PageScanResults { get; set; } = new List<PageScanResult>();
}
