using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanImage
{
    [Key]
    public Guid ScanImageId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(2000)]
    public string Url { get; set; } = null!;

    public string? AltText { get; set; }

    public bool HasAlt { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
