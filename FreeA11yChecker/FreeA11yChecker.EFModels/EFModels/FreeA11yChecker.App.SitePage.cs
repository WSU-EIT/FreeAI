using System;
using System.ComponentModel.DataAnnotations;

namespace FreeA11yChecker.EFModels.EFModels;

public partial class SitePage
{
    [Key]
    public Guid SitePageId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required, MaxLength(500)]
    public string Path { get; set; } = null!;

    [MaxLength(500)]
    public string? Title { get; set; }

    public bool RequiresAuth { get; set; }

    public bool Enabled { get; set; } = true;

    public bool IncludeInScan { get; set; } = true;

    public int SortOrder { get; set; }

    public virtual Site Site { get; set; } = null!;
}
