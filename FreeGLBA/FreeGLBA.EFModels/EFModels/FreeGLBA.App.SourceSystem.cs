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

    public virtual ICollection<DataOwnershipItem> OwnershipHistory { get; set; } = new List<DataOwnershipItem>();

    [MaxLength(500)]
    public string ApiKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// When the current data owner was assigned. Null when no owner has been recorded.
    /// </summary>
    public DateTime? DataOwnerAssignedAt { get; set; }

    /// <summary>
    /// Department of the current data owner (e.g., "Financial Aid Office").
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerDepartment { get; set; } = string.Empty;

    /// <summary>
    /// Email of the current data owner (point of contact for the data this system holds).
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Name of the current data owner (point of contact for the data this system holds).
    /// This is the "live" owner; each AccessEvent also stores a snapshot of the owner
    /// at the time of access, and DataOwnerships stores the full ownership history.
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the current data owner.
    /// </summary>
    [MaxLength(50)]
    public string DataOwnerPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastEventReceivedAt { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Key]
    public Guid SourceSystemId { get; set; }
}
