using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

/// <summary>
/// Represents an .App. file belonging to a FreeManager project.
/// Files store metadata; actual content is in FMAppFileVersion.
/// </summary>
[Table("FMAppFiles")]
public class FMAppFile
{
    [Key]
    public Guid FMAppFileId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FMProjectId { get; set; }

    /// <summary>
    /// Relative path like "DataObjects.App.MyProject.cs" or "Index.App.razor"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File type classification: DataObjects, DataAccess, Controller, RazorComponent, Stylesheet, GlobalSettings, Other
    /// </summary>
    [MaxLength(50)]
    public string FileType { get; set; } = "Other";

    /// <summary>
    /// Current version number (incremented on each save).
    /// </summary>
    public int CurrentVersion { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool Deleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    [ForeignKey("FMProjectId")]
    public virtual FMProject? Project { get; set; }

    public virtual ICollection<FMAppFileVersion> Versions { get; set; } = new List<FMAppFileVersion>();
}
