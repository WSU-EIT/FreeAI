using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class SiteCredential
{
    [Key]
    public Guid SiteCredentialId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    [Required, MaxLength(200)]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordEncrypted { get; set; } = null!;

    [Required, MaxLength(50)]
    public string AuthType { get; set; } = null!;

    [MaxLength(100)]
    public string? TenantCode { get; set; }

    [MaxLength(1000)]
    public string? LoginUrl { get; set; }

    [MaxLength(500)]
    public string? UsernameSelector { get; set; }

    [MaxLength(500)]
    public string? PasswordSelector { get; set; }

    [MaxLength(500)]
    public string? SubmitSelector { get; set; }

    public virtual Site Site { get; set; } = null!;
}
