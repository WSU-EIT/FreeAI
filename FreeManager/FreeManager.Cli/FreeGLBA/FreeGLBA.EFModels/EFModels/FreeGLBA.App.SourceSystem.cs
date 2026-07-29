using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeGLBA.EFModels.EFModels;

/// <summary>
/// SourceSystem entity - stored in [SourceSystems] table.
/// </summary>
[Table("SourceSystems")]
public partial class SourceSystemItem
{
    // Navigation properties
    public virtual ICollection<AccessEventItem> AccessEvents { get; set; } = new List<AccessEventItem>();

    [MaxLength(500)]
    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public long EventCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? LastEventReceivedAt { get; set; }
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
