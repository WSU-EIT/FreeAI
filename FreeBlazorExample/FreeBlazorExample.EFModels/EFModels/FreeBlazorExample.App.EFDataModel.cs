using Microsoft.EntityFrameworkCore;

namespace FreeBlazorExample.EFModels.EFModels;

// FreeBlazorExample entity configuration. EFDataModel.cs stays stock FreeCRM: its OnModelCreating calls
// OnModelCreatingPartial, which is implemented here.

public partial class EFDataModel
{
    // FreeBlazorExtended Feature 104 — UserPreferences
    // The POCO lives in FreeBlazorExtended/UserPreferences/UserPreferences.cs
    // (so the showcase library has zero EF dependency). Persistence is wired
    // here on the server only; the WASM client falls back to the in-memory
    // store registered in FreeBlazorExtended.UserPreferences.UserPreferencesService.
    public virtual DbSet<global::FreeBlazorExtended.UserPreferences.UserPreferences> UserPreferences { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<global::FreeBlazorExtended.UserPreferences.UserPreferences>(entity =>
        {
            entity.ToTable("UserPreferences");
            entity.HasKey(e => e.UserPreferencesId);
            entity.Property(e => e.UserPreferencesId).ValueGeneratedNever();
            entity.HasIndex(e => new { e.TenantId, e.UserId }, "IX_UserPreferences_TenantId_UserId").IsUnique();
            entity.Property(e => e.CultureCode).HasMaxLength(20);
            entity.Property(e => e.PerEntityJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.RecentItemsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.AddedBy).HasMaxLength(100);
            entity.Property(e => e.LastModified).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            // Theme + Density stored as int (enum). LayoutPanel widths are decimal — default precision OK.
            entity.Property(e => e.Theme).HasConversion<int>();
            entity.Property(e => e.Density).HasConversion<int>();
        });
    }
}
