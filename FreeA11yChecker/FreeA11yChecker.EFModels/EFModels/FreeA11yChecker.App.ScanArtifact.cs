using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanArtifact
{
    [Key]
    public Guid ScanArtifactId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = null!;

    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public byte[]? Data { get; set; }

    public long SizeBytes { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
