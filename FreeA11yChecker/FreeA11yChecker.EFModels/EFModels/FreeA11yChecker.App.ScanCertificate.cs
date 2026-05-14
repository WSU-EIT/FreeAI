using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ScanCertificate
{
    [Key]
    public Guid ScanCertificateId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [MaxLength(500)]
    public string? Subject { get; set; }

    [MaxLength(500)]
    public string? Issuer { get; set; }

    public DateTime? Expiry { get; set; }

    public string? SubjectAlternativeNames { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}
