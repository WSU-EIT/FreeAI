using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanScreenshot
{
    [Key]
    public Guid ScanScreenshotId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(1000)]
    public string Path { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Label { get; set; } = null!;

    public long SizeBytes { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public byte[]? Data { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
