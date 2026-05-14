using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

/// <summary>
/// Build job for a FreeManager project.
/// Tracks the full lifecycle: Queued -> Running -> Succeeded/Failed
/// </summary>
[Table("FMBuilds")]
public class FMBuild
{
    [Key]
    public Guid FMBuildId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FMProjectId { get; set; }

    /// <summary>
    /// Sequential build number for this project (1, 2, 3, etc.)
    /// </summary>
    public int BuildNumber { get; set; }

    /// <summary>
    /// Build status: Queued, Running, Succeeded, Failed
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Path to the ZIP artifact on the server.
    /// </summary>
    [MaxLength(1000)]
    public string ArtifactPath { get; set; } = string.Empty;

    /// <summary>
    /// Size of the artifact in bytes.
    /// </summary>
    public long? ArtifactSizeBytes { get; set; }

    /// <summary>
    /// Full build log output.
    /// </summary>
    public string LogOutput { get; set; } = string.Empty;

    /// <summary>
    /// Error message if build failed.
    /// </summary>
    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;

    public Guid? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("FMProjectId")]
    public virtual FMProject? Project { get; set; }
}
