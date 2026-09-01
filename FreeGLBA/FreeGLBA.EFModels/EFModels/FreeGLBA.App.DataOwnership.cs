using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeGLBA.EFModels.EFModels;

/// <summary>
/// DataOwnership entity - stored in [DataOwnerships] table.
/// One row per ownership period for a source system's data. The row with
/// EndedAt == null is the current owner. Together with the owner snapshot
/// stored on each AccessEvent, this answers both "who owned the data at the
/// time of a given access" and "who owns it now".
/// </summary>
[Table("DataOwnerships")]
[Index(nameof(SourceSystemId))]
public partial class DataOwnershipItem
{
    /// <summary>
    /// When this owner became responsible for the data.
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// Who recorded this ownership assignment (application user name).
    /// </summary>
    [MaxLength(200)]
    public string AssignedBy { get; set; } = string.Empty;

    [Key]
    public Guid DataOwnershipId { get; set; }

    /// <summary>
    /// When this ownership period ended. Null while this owner is current.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    [MaxLength(200)]
    public string OwnerDepartment { get; set; } = string.Empty;

    [MaxLength(200)]
    public string OwnerEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string OwnerName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string OwnerPhone { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("SourceSystemId")]
    public virtual SourceSystemItem SourceSystem { get; set; } = null!;

    public Guid SourceSystemId { get; set; } = Guid.Empty;
}
