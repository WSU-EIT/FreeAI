using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeGLBA.EFModels.EFModels;

/// <summary>
/// AccessEvent entity - stored in [AccessEvents] table.
/// </summary>
[Table("AccessEvents")]
[Index(nameof(AccessedAt))]
[Index(nameof(UserId))]
[Index(nameof(SubjectId))]
[Index(nameof(SourceSystemId), nameof(SourceEventId))]
public partial class AccessEventItem
{
    public DateTime AccessedAt { get; set; }
    [Key]
    public Guid AccessEventId { get; set; }

    [MaxLength(50)]
    public string AccessType { get; set; } = string.Empty;

    public string AdditionalData { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the user acknowledged the privacy agreement (if different from AccessedAt).
    /// </summary>
    public DateTime? AgreementAcknowledgedAt { get; set; }

    /// <summary>
    /// Copy of the privacy notice/agreement text the user acknowledged when accessing data.
    /// Captures what disclosure was shown at time of access for GLBA compliance.
    /// </summary>
    public string AgreementText { get; set; } = string.Empty;

    /// <summary>
    /// Position of this event in its source system's tamper-evident hash chain.
    /// 0 for events recorded before integrity chaining existed.
    /// </summary>
    public long ChainSequence { get; set; }

    [MaxLength(100)]
    public string DataCategory { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot: department of the data owner at the time of access.
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerDepartment { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot: email of the data owner at the time of access.
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot: name of the data owner (point of contact for the data this event
    /// is about) at the time of access. Captured at ingest — either supplied by the
    /// source system on the event, or copied from the source system's current data
    /// owner. Immutable record of "who owned the data then"; the source system holds
    /// the live "who owns it now".
    /// </summary>
    [MaxLength(200)]
    public string DataOwnerName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// RowHash of the previous event in this source system's chain
    /// (empty for the first chained event).
    /// </summary>
    [MaxLength(100)]
    public string PrevRowHash { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// SHA-256 over this event's immutable audit fields plus PrevRowHash and
    /// ChainSequence, computed once at ingest. Any later modification of the row,
    /// a broken link, or a sequence gap (deletion) is detectable by verification.
    /// </summary>
    [MaxLength(100)]
    public string RowHash { get; set; } = string.Empty;

    [MaxLength(200)]
    public string SourceEventId { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("SourceSystemId")]
    public virtual SourceSystemItem SourceSystem { get; set; } = null!;

    public Guid SourceSystemId { get; set; } = Guid.Empty;

    /// <summary>
    /// Count of subjects accessed. For single access = 1, for bulk exports = count of SubjectIds.
    /// Useful for quick reporting without parsing SubjectIds JSON.
    /// </summary>
    public int SubjectCount { get; set; } = 1;

    /// <summary>
    /// Primary subject ID for single-subject access. For bulk access, this may contain
    /// "BULK" or the first subject ID as a reference.
    /// </summary>
    [MaxLength(200)]
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of all subject IDs when accessing multiple subjects (e.g., CSV export).
    /// For bulk exports from systems like Touchpoints, this captures all affected individuals.
    /// </summary>
    public string SubjectIds { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SubjectType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserDepartment { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;
}
