using Microsoft.EntityFrameworkCore;

namespace FreeManager.EFModels.EFModels;

// ============================================================================
// FREEMANAGER EFDATAMODEL EXTENSION
// ============================================================================
// This partial class extends the base EFDataModel with FreeManager-specific
// DbSets and entity configurations for the application builder platform.
//
// Related entity files:
// - FreeManager.App.FMProject.cs
// - FreeManager.App.FMAppFile.cs
// - FreeManager.App.FMAppFileVersion.cs
// - FreeManager.App.FMBuild.cs
// ============================================================================

/// <summary>
/// FreeManager DbContext extension - adds DbSets and configuration for FM entities.
/// This is a partial class extending EFDataModel without modifying the original file.
/// </summary>
public partial class EFDataModel
{
    // ============================================================
    // FREEMANAGER DbSets
    // ============================================================

    public virtual DbSet<FMProject> FMProjects { get; set; } = null!;
    public virtual DbSet<FMAppFile> FMAppFiles { get; set; } = null!;
    public virtual DbSet<FMAppFileVersion> FMAppFileVersions { get; set; } = null!;
    public virtual DbSet<FMBuild> FMBuilds { get; set; } = null!;

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
