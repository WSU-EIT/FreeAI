using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

/// <summary>
/// FreeManager Project - represents a user's custom application project.
/// Part of the FreeManager platform for creating custom FreeCRM-based applications.
/// </summary>
[Table("FMProjects")]
public class FMProject
{
    [Key]
    public Guid FMProjectId { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    /// <summary>
    /// Project name - must be valid C# identifier (no spaces, starts with letter)
    /// Used for namespace and assembly naming.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-friendly display name for the project.
    /// </summary>
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Project description.
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of included modules (e.g., "Tags,Appointments").
    /// Modules not listed will be removed during build.
    /// </summary>
    [MaxLength(500)]
    public string IncludedModules { get; set; } = string.Empty;

    /// <summary>
    /// Project status: Draft, Active, Archived
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Serialized EntityWizardState JSON - stores entity definitions, relationships, and options.
    /// Used by the Entity Builder Wizard for save/load functionality.
    /// </summary>
    public string? EntityWizardStateJson { get; set; }

    /// <summary>
    /// Number of entities defined in EntityWizardState (denormalized for display).
    /// </summary>
    public int EntityCount { get; set; } = 0;

    /// <summary>
    /// Number of relationships defined in EntityWizardState (denormalized for display).
    /// </summary>
    public int RelationshipCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public bool Deleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<FMAppFile> AppFiles { get; set; } = new List<FMAppFile>();
    public virtual ICollection<FMBuild> Builds { get; set; } = new List<FMBuild>();
}
