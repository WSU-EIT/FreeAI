using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class ViolationSuppression
{
    [Key]
    public Guid ViolationSuppressionId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required, MaxLength(200)]
    public string RuleId { get; set; } = null!;

    [MaxLength(1000)]
    public string? SelectorPattern { get; set; }

    public Guid? SiteId { get; set; }

    [Required]
    public string Reason { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    public DateTime Added { get; set; }

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    public DateTime LastModified { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }
}
