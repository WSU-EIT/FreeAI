using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class Site
{
    [Key]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(500)]
    public string BaseUrl { get; set; } = null!;

    public bool IsFreeCRMApp { get; set; }

    [MaxLength(100)]
    public string? ScanScheduleCron { get; set; }

    public int MaxConcurrency { get; set; } = 5;

    public bool Enabled { get; set; } = true;

    public bool PublicVisible { get; set; } = true;

    public Guid? LastScanRunId { get; set; }

    public DateTime? LastScanAt { get; set; }

    [MaxLength(50)]
    public string? LastScanStatus { get; set; }

    public int LastViolationCount { get; set; }

    public int LastCriticalCount { get; set; }

    public DateTime Added { get; set; }

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    public DateTime LastModified { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<SitePage> SitePages { get; set; } = new List<SitePage>();

    public virtual ICollection<SiteCredential> SiteCredentials { get; set; } = new List<SiteCredential>();

    public virtual ICollection<ScanRun> ScanRuns { get; set; } = new List<ScanRun>();

    public virtual ICollection<ManualCheckResult> ManualCheckResults { get; set; } = new List<ManualCheckResult>();
}
