using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

// ---- EFDataModel ----

// ============================================================================
// FREEMANAGER EFDATAMODEL EXTENSION
// ============================================================================
// This partial class extends the base EFDataModel with FreeManager-specific
// DbSets and entity configurations for the application builder platform.
//
// Related entity files:
// - FMProject (section of this file, FreeManager.App.EFModels.cs)
// - FMAppFile (section of this file, FreeManager.App.EFModels.cs)
// - FMAppFileVersion (section of this file, FreeManager.App.EFModels.cs)
// - FMBuild (section of this file, FreeManager.App.EFModels.cs)
// ============================================================================

/// <summary>
/// FreeManager DbContext extension - adds DbSets and configuration for FM entities.
/// This is a partial class extending EFDataModel without modifying the original file.
/// </summary>
public partial class EFDataModel
{
    public virtual DbSet<FMAppFile> FMAppFiles { get; set; } = null!;
    public virtual DbSet<FMAppFileVersion> FMAppFileVersions { get; set; } = null!;
    public virtual DbSet<FMBuild> FMBuilds { get; set; } = null!;
    // ============================================================
    // FREEMANAGER DbSets
    // ============================================================

    public virtual DbSet<FMProject> FMProjects { get; set; } = null!;

    // ============================================================
    // ENTITY CONFIGURATION
    // ============================================================

    /// <summary>
    /// Configures FreeManager entity relationships and constraints.
    /// Called from OnModelCreating via the partial method hook.
    /// </summary>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // FMProject configuration
        modelBuilder.Entity<FMProject>(entity =>
        {
            entity.Property(e => e.FMProjectId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Tenant)
                .WithMany()
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FMProjects_Tenants");

            // Unique constraint: project name per tenant
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .IsUnique()
                .HasFilter("[Deleted] = 0")
                .HasDatabaseName("IX_FMProjects_TenantId_Name");
        });

        // FMAppFile configuration
        modelBuilder.Entity<FMAppFile>(entity =>
        {
            entity.Property(e => e.FMAppFileId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Project)
                .WithMany(p => p.AppFiles)
                .HasForeignKey(d => d.FMProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FMAppFiles_FMProjects");

            // Unique constraint: file path per project
            entity.HasIndex(e => new { e.FMProjectId, e.FilePath })
                .IsUnique()
                .HasFilter("[Deleted] = 0")
                .HasDatabaseName("IX_FMAppFiles_ProjectId_FilePath");
        });

        // FMAppFileVersion configuration
        modelBuilder.Entity<FMAppFileVersion>(entity =>
        {
            entity.Property(e => e.FMAppFileVersionId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.AppFile)
                .WithMany(p => p.Versions)
                .HasForeignKey(d => d.FMAppFileId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_FMAppFileVersions_FMAppFiles");

            // Index for efficient version lookups
            entity.HasIndex(e => new { e.FMAppFileId, e.Version })
                .IsUnique()
                .HasDatabaseName("IX_FMAppFileVersions_FileId_Version");
        });

        // FMBuild configuration
        modelBuilder.Entity<FMBuild>(entity =>
        {
            entity.Property(e => e.FMBuildId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.StartedAt).HasColumnType("datetime");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Project)
                .WithMany(p => p.Builds)
                .HasForeignKey(d => d.FMProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FMBuilds_FMProjects");

            // Index for efficient build history lookups
            entity.HasIndex(e => new { e.FMProjectId, e.BuildNumber })
                .IsUnique()
                .HasDatabaseName("IX_FMBuilds_ProjectId_BuildNumber");
        });
    }
}

// ---- FMProject ----

/// <summary>
/// FreeManager Project - represents a user's custom application project.
/// Part of the FreeManager platform for creating custom FreeCRM-based applications.
/// </summary>
[Table("FMProjects")]
public class FMProject
{
    public virtual ICollection<FMAppFile> AppFiles { get; set; } = new List<FMAppFile>();
    public virtual ICollection<FMBuild> Builds { get; set; } = new List<FMBuild>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public bool Deleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Project description.
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Human-friendly display name for the project.
    /// </summary>
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of entities defined in EntityWizardState (denormalized for display).
    /// </summary>
    public int EntityCount { get; set; } = 0;

    /// <summary>
    /// Serialized EntityWizardState JSON - stores entity definitions, relationships, and options.
    /// Used by the Entity Builder Wizard for save/load functionality.
    /// </summary>
    public string? EntityWizardStateJson { get; set; }
    [Key]
    public Guid FMProjectId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Comma-separated list of included modules (e.g., "Tags,Appointments").
    /// Modules not listed will be removed during build.
    /// </summary>
    [MaxLength(500)]
    public string IncludedModules { get; set; } = string.Empty;

    /// <summary>
    /// Project name - must be valid C# identifier (no spaces, starts with letter)
    /// Used for namespace and assembly naming.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of relationships defined in EntityWizardState (denormalized for display).
    /// </summary>
    public int RelationshipCount { get; set; } = 0;

    /// <summary>
    /// Project status: Draft, Active, Archived
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";

    // Navigation properties
    public virtual Tenant? Tenant { get; set; }

    public Guid TenantId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ---- FMAppFile ----

/// <summary>
/// Represents an .App. file belonging to a FreeManager project.
/// Files store metadata; actual content is in FMAppFileVersion.
/// </summary>
[Table("FMAppFiles")]
public class FMAppFile
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current version number (incremented on each save).
    /// </summary>
    public int CurrentVersion { get; set; } = 1;
    public bool Deleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

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
    [Key]
    public Guid FMAppFileId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FMProjectId { get; set; }

    // Navigation properties
    [ForeignKey("FMProjectId")]
    public virtual FMProject? Project { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<FMAppFileVersion> Versions { get; set; } = new List<FMAppFileVersion>();
}

// ---- FMAppFileVersion ----

/// <summary>
/// Version history for an FMAppFile - stores actual file content.
/// Each save creates a new version for full history tracking.
/// </summary>
[Table("FMAppFileVersions")]
public class FMAppFileVersion
{
    // Navigation properties
    [ForeignKey("FMAppFileId")]
    public virtual FMAppFile? AppFile { get; set; }

    /// <summary>
    /// Optional commit message describing the change.
    /// </summary>
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

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

    [Required]
    public Guid FMAppFileId { get; set; }
    [Key]
    public Guid FMAppFileVersionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Version number for this file (1, 2, 3, etc.)
    /// </summary>
    public int Version { get; set; }
}

// ---- FMBuild ----

/// <summary>
/// Build job for a FreeManager project.
/// Tracks the full lifecycle: Queued -> Running -> Succeeded/Failed
/// </summary>
[Table("FMBuilds")]
public class FMBuild
{
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
    /// Sequential build number for this project (1, 2, 3, etc.)
    /// </summary>
    public int BuildNumber { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Error message if build failed.
    /// </summary>
    [MaxLength(2000)]
    public string ErrorMessage { get; set; } = string.Empty;
    [Key]
    public Guid FMBuildId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid FMProjectId { get; set; }

    /// <summary>
    /// Full build log output.
    /// </summary>
    public string LogOutput { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("FMProjectId")]
    public virtual FMProject? Project { get; set; }
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Build status: Queued, Running, Succeeded, Failed
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";
}
