using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ManualCheckResult
{
    [Key]
    public Guid ManualCheckResultId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required, MaxLength(20)]
    public string WcagCriterion { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? TestedBy { get; set; }

    public DateTime? TestedAt { get; set; }

    public virtual Site Site { get; set; } = null!;
}
