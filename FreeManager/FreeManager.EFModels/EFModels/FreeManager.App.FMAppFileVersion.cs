using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

/// <summary>
/// Version history for an FMAppFile - stores actual file content.
/// Each save creates a new version for full history tracking.
/// </summary>
[Table("FMAppFileVersions")]
public class FMAppFileVersion
{
    [Key]
    public Guid FMAppFileVersionId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FMAppFileId { get; set; }

    /// <summary>
    /// Version number for this file (1, 2, 3, etc.)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The actual file content (C# code, Razor markup, CSS, etc.)
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 hash of content for quick change detection.
    /// </summary>
    [MaxLength(64)]
    public string ContentHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Optional commit message describing the change.
    /// </summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("FMAppFileId")]
    public virtual FMAppFile? AppFile { get; set; }
}
