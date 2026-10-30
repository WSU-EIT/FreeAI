using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FreeAccessibilityChecker.EFModels.EFModels;

// ---- FreeAccessibilityChecker.App.EFDataModel.cs ----
public partial class EFDataModel
{
    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<SitePage> SitePages { get; set; }

    public virtual DbSet<SiteCredential> SiteCredentials { get; set; }

    public virtual DbSet<ScanRun> ScanRuns { get; set; }

    public virtual DbSet<PageScanResult> PageScanResults { get; set; }

    public virtual DbSet<A11yViolation> A11yViolations { get; set; }

    public virtual DbSet<ManualCheckResult> ManualCheckResults { get; set; }

    public virtual DbSet<ScanScreenshot> ScanScreenshots { get; set; }

    public virtual DbSet<ScanImage> ScanImages { get; set; }

    public virtual DbSet<ScanCertificate> ScanCertificates { get; set; }

    public virtual DbSet<ScanRankedRule> ScanRankedRules { get; set; }

    public virtual DbSet<ScanArtifact> ScanArtifacts { get; set; }

    public virtual DbSet<ViolationSuppression> ViolationSuppressions { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Site>(entity =>
        {
            entity.Property(e => e.SiteId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.BaseUrl).HasMaxLength(500);
            entity.Property(e => e.ScanScheduleCron).HasMaxLength(100);
            entity.Property(e => e.LastScanStatus).HasMaxLength(50);
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.AddedBy).HasMaxLength(100);
            entity.Property(e => e.LastModified).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
            entity.Property(e => e.LastScanAt).HasColumnType("datetime");

            entity.HasIndex(e => e.TenantId, "IX_Sites_TenantId");
            entity.HasIndex(e => e.Enabled, "IX_Sites_Enabled");
            entity.HasIndex(e => e.PublicVisible, "IX_Sites_PublicVisible");
        });

        modelBuilder.Entity<SitePage>(entity =>
        {
            entity.Property(e => e.SitePageId).ValueGeneratedNever();
            entity.Property(e => e.Path).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(500);

            entity.HasIndex(e => e.SiteId, "IX_SitePages_SiteId");

            entity.HasOne(d => d.Site).WithMany(p => p.SitePages)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SitePages_Sites");
        });

        modelBuilder.Entity<SiteCredential>(entity =>
        {
            entity.Property(e => e.SiteCredentialId).ValueGeneratedNever();
            entity.Property(e => e.Label).HasMaxLength(200);
            entity.Property(e => e.Username).HasMaxLength(200);
            entity.Property(e => e.AuthType).HasMaxLength(50);
            entity.Property(e => e.TenantCode).HasMaxLength(100);
            entity.Property(e => e.LoginUrl).HasMaxLength(1000);
            entity.Property(e => e.UsernameSelector).HasMaxLength(500);
            entity.Property(e => e.PasswordSelector).HasMaxLength(500);
            entity.Property(e => e.SubmitSelector).HasMaxLength(500);

            entity.HasIndex(e => e.SiteId, "IX_SiteCredentials_SiteId");

            entity.HasOne(d => d.Site).WithMany(p => p.SiteCredentials)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SiteCredentials_Sites");
        });

        modelBuilder.Entity<ScanRun>(entity =>
        {
            entity.Property(e => e.ScanRunId).ValueGeneratedNever();
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TriggeredBy).HasMaxLength(100);
            entity.Property(e => e.StartedAt).HasColumnType("datetime");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");

            entity.HasIndex(e => e.SiteId, "IX_ScanRuns_SiteId");
            entity.HasIndex(e => e.TenantId, "IX_ScanRuns_TenantId");
            entity.HasIndex(e => e.StartedAt, "IX_ScanRuns_StartedAt");

            entity.HasOne(d => d.Site).WithMany(p => p.ScanRuns)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScanRuns_Sites");
        });

        modelBuilder.Entity<PageScanResult>(entity =>
        {
            entity.Property(e => e.PageScanResultId).ValueGeneratedNever();
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.PageTitle).HasMaxLength(500);
            entity.Property(e => e.ScreenshotPath).HasMaxLength(1000);
            entity.Property(e => e.OutputDir).HasMaxLength(1000);
            entity.Property(e => e.Language).HasMaxLength(20);

            entity.HasIndex(e => e.ScanRunId, "IX_PageScanResults_ScanRunId");
            entity.HasIndex(e => e.SitePageId, "IX_PageScanResults_SitePageId");

            entity.HasOne(d => d.ScanRun).WithMany(p => p.PageScanResults)
                .HasForeignKey(d => d.ScanRunId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PageScanResults_ScanRuns");
        });

        modelBuilder.Entity<A11yViolation>(entity =>
        {
            entity.HasKey(e => e.A11yViolationId);

            entity.Property(e => e.A11yViolationId).ValueGeneratedNever();
            entity.Property(e => e.Tool).HasMaxLength(50);
            entity.Property(e => e.RuleId).HasMaxLength(200);
            entity.Property(e => e.CanonicalRuleId).HasMaxLength(200);
            entity.Property(e => e.Severity).HasMaxLength(50);
            entity.Property(e => e.HelpUrl).HasMaxLength(1000);
            entity.Property(e => e.WcagCriteria).HasMaxLength(50);
            entity.Property(e => e.ContrastForeground).HasMaxLength(50);
            entity.Property(e => e.ContrastBackground).HasMaxLength(50);
            entity.Property(e => e.ContrastFontSize).HasMaxLength(50);
            entity.Property(e => e.ContrastFontWeight).HasMaxLength(50);

            entity.HasIndex(e => e.PageScanResultId, "IX_A11yViolations_PageScanResultId");
            entity.HasIndex(e => e.Severity, "IX_A11yViolations_Severity");
            entity.HasIndex(e => e.CanonicalRuleId, "IX_A11yViolations_CanonicalRuleId");

            entity.HasOne(d => d.PageScanResult).WithMany(p => p.A11yViolations)
                .HasForeignKey(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_A11yViolations_PageScanResults");
        });

        modelBuilder.Entity<ManualCheckResult>(entity =>
        {
            entity.Property(e => e.ManualCheckResultId).ValueGeneratedNever();
            entity.Property(e => e.WcagCriterion).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TestedBy).HasMaxLength(200);
            entity.Property(e => e.TestedAt).HasColumnType("datetime");

            entity.HasIndex(e => e.SiteId, "IX_ManualCheckResults_SiteId");
            entity.HasIndex(e => e.WcagCriterion, "IX_ManualCheckResults_WcagCriterion");

            entity.HasOne(d => d.Site).WithMany(p => p.ManualCheckResults)
                .HasForeignKey(d => d.SiteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ManualCheckResults_Sites");
        });

        modelBuilder.Entity<ScanScreenshot>(entity =>
        {
            entity.Property(e => e.ScanScreenshotId).ValueGeneratedNever();
            entity.Property(e => e.Path).HasMaxLength(1000);
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.ContentType).HasMaxLength(100);

            entity.HasIndex(e => e.PageScanResultId, "IX_ScanScreenshots_PageScanResultId");

            entity.HasOne(d => d.PageScanResult).WithMany(p => p.ScanScreenshots)
                .HasForeignKey(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScanScreenshots_PageScanResults");
        });

        modelBuilder.Entity<ScanArtifact>(entity =>
        {
            entity.Property(e => e.ScanArtifactId).ValueGeneratedNever();
            entity.Property(e => e.FileName).HasMaxLength(200);
            entity.Property(e => e.ContentType).HasMaxLength(100);

            entity.HasIndex(e => e.PageScanResultId, "IX_ScanArtifacts_PageScanResultId");

            entity.HasOne(d => d.PageScanResult).WithMany(p => p.ScanArtifacts)
                .HasForeignKey(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScanArtifacts_PageScanResults");
        });

        modelBuilder.Entity<ScanImage>(entity =>
        {
            entity.Property(e => e.ScanImageId).ValueGeneratedNever();
            entity.Property(e => e.Url).HasMaxLength(2000);

            entity.HasIndex(e => e.PageScanResultId, "IX_ScanImages_PageScanResultId");

            entity.HasOne(d => d.PageScanResult).WithMany(p => p.ScanImages)
                .HasForeignKey(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScanImages_PageScanResults");
        });

        modelBuilder.Entity<ScanCertificate>(entity =>
        {
            entity.Property(e => e.ScanCertificateId).ValueGeneratedNever();
            entity.Property(e => e.Subject).HasMaxLength(500);
            entity.Property(e => e.Issuer).HasMaxLength(500);
            entity.Property(e => e.Expiry).HasColumnType("datetime");

            entity.HasIndex(e => e.PageScanResultId, "IX_ScanCertificates_PageScanResultId").IsUnique();

            entity.HasOne(d => d.PageScanResult).WithOne(p => p.ScanCertificate)
                .HasForeignKey<ScanCertificate>(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScanCertificates_PageScanResults");
        });

        modelBuilder.Entity<ScanRankedRule>(entity =>
        {
            entity.Property(e => e.ScanRankedRuleId).ValueGeneratedNever();
            entity.Property(e => e.CanonicalRuleId).HasMaxLength(200);
            entity.Property(e => e.Severity).HasMaxLength(50);
            entity.Property(e => e.ToolsFound).HasMaxLength(500);

            entity.HasIndex(e => e.PageScanResultId, "IX_ScanRankedRules_PageScanResultId");

            entity.HasOne(d => d.PageScanResult).WithMany(p => p.ScanRankedRules)
                .HasForeignKey(d => d.PageScanResultId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScanRankedRules_PageScanResults");
        });

        modelBuilder.Entity<ViolationSuppression>(entity =>
        {
            entity.Property(e => e.ViolationSuppressionId).ValueGeneratedNever();
            entity.Property(e => e.RuleId).HasMaxLength(200);
            entity.Property(e => e.SelectorPattern).HasMaxLength(1000);
            entity.Property(e => e.AddedBy).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.LastModified).HasColumnType("datetime");

            entity.HasIndex(e => e.TenantId, "IX_ViolationSuppressions_TenantId");
            entity.HasIndex(e => e.RuleId, "IX_ViolationSuppressions_RuleId");
            entity.HasIndex(e => e.Enabled, "IX_ViolationSuppressions_Enabled");
        });
    }

    // FreeAccessibilityChecker: FreeCRM's EFModelOverrides.cs declares its ConfigureConventions override in namespace
    // FreeAccessibilityChecker.EFModels, but the real context lives in FreeAccessibilityChecker.EFModels.EFModels, so that override lands on
    // a second, unused EFDataModel type and never runs (still true upstream; report it). The same override lives here,
    // on the real context, so EFModelOverrides.cs can stay stock. It only changes anything for MySQL, PostgreSQL and SQLite.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // MySQL, PostgreSQL, and SQLite store uniqueidentifier fields (GUID) as a string in the EFCore provider.
        // So, if this instance is running one of those providers then override the converter for all Guid
        // values to use strings.
        var providerName = this.Database.ProviderName;
        if (!String.IsNullOrEmpty(providerName)) {
            switch (providerName.ToUpper()) {
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLSERVER":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.INMEMORY":
                    break;

                case "MYSQL.ENTITYFRAMEWORKCORE":
                case "NPGSQL.ENTITYFRAMEWORKCORE.POSTGRESQL":
                case "MICROSOFT.ENTITYFRAMEWORKCORE.SQLITE":
                    configurationBuilder
                        .Properties<Guid>()
                        .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.GuidToStringConverter>();
                    break;
            }
        }
    }
}

// Design-time DbContext factory for `dotnet ef migrations add` / `dotnet ef database update`.
// At runtime the app constructs EFDataModel via DI with options from appsettings.json — this
// factory is ONLY consulted by EF tooling. Connection string mirrors appsettings.json's
// "AppData" SQL Server trusted-connection entry.
public class EFDataModelDesignTimeFactory : IDesignTimeDbContextFactory<EFDataModel>
{
    public EFDataModel CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<EFDataModel> options = new DbContextOptionsBuilder<EFDataModel>();
        options.UseSqlServer(
            "Data Source=(local);Initial Catalog=FreeAccessibilityChecker;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;");
        return new EFDataModel(options.Options);
    }
}

// ---- FreeAccessibilityChecker.App.A11yViolation.cs ----
public partial class A11yViolation
{
    [Key]
    public Guid A11yViolationId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(50)]
    public string Tool { get; set; } = null!;

    [Required, MaxLength(200)]
    public string RuleId { get; set; } = null!;

    [MaxLength(200)]
    public string? CanonicalRuleId { get; set; }

    [Required, MaxLength(50)]
    public string Severity { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public string? Selector { get; set; }

    public string? HtmlSnippet { get; set; }

    [MaxLength(1000)]
    public string? HelpUrl { get; set; }

    [MaxLength(50)]
    public string? WcagCriteria { get; set; }

    [MaxLength(50)]
    public string? ContrastForeground { get; set; }

    [MaxLength(50)]
    public string? ContrastBackground { get; set; }

    public double? ContrastRatio { get; set; }

    public double? ContrastExpected { get; set; }

    [MaxLength(50)]
    public string? ContrastFontSize { get; set; }

    [MaxLength(50)]
    public string? ContrastFontWeight { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ManualCheckResult.cs ----
public partial class ManualCheckResult
{
    [Key]
    public Guid ManualCheckResultId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required, MaxLength(20)]
    public string WcagCriterion { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? TestedBy { get; set; }

    public DateTime? TestedAt { get; set; }

    public virtual Site Site { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.PageScanResult.cs ----
public partial class PageScanResult
{
    [Key]
    public Guid PageScanResultId { get; set; }

    [Required]
    public Guid ScanRunId { get; set; }

    [Required]
    public Guid SitePageId { get; set; }

    [Required, MaxLength(1000)]
    public string Url { get; set; } = null!;

    public int StatusCode { get; set; }

    [MaxLength(500)]
    public string? PageTitle { get; set; }

    public long HtmlSizeBytes { get; set; }

    [MaxLength(1000)]
    public string? ScreenshotPath { get; set; }

    public int AxeViolationCount { get; set; }

    public int HtmlCheckViolationCount { get; set; }

    public int OverlayViolationCount { get; set; }

    public int TotalViolations { get; set; }

    public int CriticalCount { get; set; }

    public int SeriousCount { get; set; }

    public int ModerateCount { get; set; }

    public int MinorCount { get; set; }

    public int ScanDurationMs { get; set; }

    public string? ErrorMessage { get; set; }

    [MaxLength(1000)]
    public string? OutputDir { get; set; }

    [MaxLength(20)]
    public string? Language { get; set; }

    public string? ResponseHeaders { get; set; }

    public int AxePassCount { get; set; }

    public int AxeIncompleteCount { get; set; }

    public int AxeInapplicableCount { get; set; }

    public int IbmPassCount { get; set; }

    public int IbmPotentialCount { get; set; }

    public int IbmManualCount { get; set; }

    public int HtmlCheckPassCount { get; set; }

    public int HtmlCheckTotalChecks { get; set; }

    public string? HeadingsJson { get; set; }

    public string? LandmarksJson { get; set; }

    public string? MediaJson { get; set; }

    public string? AriaLiveRegionsJson { get; set; }

    public string? TargetSizeJson { get; set; }

    public string? PerformanceJson { get; set; }

    public string? AccessibilityTreeJson { get; set; }

    public string? KeyboardNavJson { get; set; }

    public string? FocusTrapsJson { get; set; }

    public string? TextSpacingJson { get; set; }

    public string? ReadingLevelJson { get; set; }

    public string? AutocompleteJson { get; set; }

    public string? FixedElementsJson { get; set; }

    public string? MobileViewportsJson { get; set; }

    public virtual ScanRun ScanRun { get; set; } = null!;

    public virtual ICollection<A11yViolation> A11yViolations { get; set; } = new List<A11yViolation>();

    public virtual ICollection<ScanScreenshot> ScanScreenshots { get; set; } = new List<ScanScreenshot>();

    public virtual ICollection<ScanImage> ScanImages { get; set; } = new List<ScanImage>();

    public virtual ICollection<ScanRankedRule> ScanRankedRules { get; set; } = new List<ScanRankedRule>();

    public virtual ICollection<ScanArtifact> ScanArtifacts { get; set; } = new List<ScanArtifact>();

    public virtual ScanCertificate? ScanCertificate { get; set; }
}

// ---- FreeAccessibilityChecker.App.ScanArtifact.cs ----
public partial class ScanArtifact
{
    [Key]
    public Guid ScanArtifactId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = null!;

    [MaxLength(500)]
    public string Path { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public byte[]? Data { get; set; }

    public long SizeBytes { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ScanCertificate.cs ----
public partial class ScanCertificate
{
    [Key]
    public Guid ScanCertificateId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [MaxLength(500)]
    public string? Subject { get; set; }

    [MaxLength(500)]
    public string? Issuer { get; set; }

    public DateTime? Expiry { get; set; }

    public string? SubjectAlternativeNames { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ScanImage.cs ----
public partial class ScanImage
{
    [Key]
    public Guid ScanImageId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(2000)]
    public string Url { get; set; } = null!;

    public string? AltText { get; set; }

    public bool HasAlt { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ScanRankedRule.cs ----
public partial class ScanRankedRule
{
    [Key]
    public Guid ScanRankedRuleId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(200)]
    public string CanonicalRuleId { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Severity { get; set; } = null!;

    public int Consensus { get; set; }

    public double Confidence { get; set; }

    [MaxLength(500)]
    public string? ToolsFound { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ScanRun.cs ----
public partial class ScanRun
{
    [Key]
    public Guid ScanRunId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = null!;

    public int TotalPages { get; set; }

    public int PagesScanned { get; set; }

    public int TotalViolations { get; set; }

    public int CriticalCount { get; set; }

    public int SeriousCount { get; set; }

    public int ModerateCount { get; set; }

    public int MinorCount { get; set; }

    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    public virtual Site Site { get; set; } = null!;

    public virtual ICollection<PageScanResult> PageScanResults { get; set; } = new List<PageScanResult>();
}

// ---- FreeAccessibilityChecker.App.ScanScreenshot.cs ----
public partial class ScanScreenshot
{
    [Key]
    public Guid ScanScreenshotId { get; set; }

    [Required]
    public Guid PageScanResultId { get; set; }

    [Required, MaxLength(1000)]
    public string Path { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Label { get; set; } = null!;

    public long SizeBytes { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public byte[]? Data { get; set; }

    public virtual PageScanResult PageScanResult { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.Site.cs ----
public partial class Site
{
    [Key]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(500)]
    public string BaseUrl { get; set; } = null!;

    public bool IsFreeCRMApp { get; set; }

    [MaxLength(100)]
    public string? ScanScheduleCron { get; set; }

    public int MaxConcurrency { get; set; } = 5;

    public bool Enabled { get; set; } = true;

    public bool PublicVisible { get; set; } = true;

    public Guid? LastScanRunId { get; set; }

    public DateTime? LastScanAt { get; set; }

    [MaxLength(50)]
    public string? LastScanStatus { get; set; }

    public int LastViolationCount { get; set; }

    public int LastCriticalCount { get; set; }

    public DateTime Added { get; set; }

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    public DateTime LastModified { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<SitePage> SitePages { get; set; } = new List<SitePage>();

    public virtual ICollection<SiteCredential> SiteCredentials { get; set; } = new List<SiteCredential>();

    public virtual ICollection<ScanRun> ScanRuns { get; set; } = new List<ScanRun>();

    public virtual ICollection<ManualCheckResult> ManualCheckResults { get; set; } = new List<ManualCheckResult>();
}

// ---- FreeAccessibilityChecker.App.SiteCredential.cs ----
public partial class SiteCredential
{
    [Key]
    public Guid SiteCredentialId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [MaxLength(200)]
    public string? Label { get; set; }

    [Required, MaxLength(200)]
    public string Username { get; set; } = null!;

    [Required]
    public string PasswordEncrypted { get; set; } = null!;

    [Required, MaxLength(50)]
    public string AuthType { get; set; } = null!;

    [MaxLength(100)]
    public string? TenantCode { get; set; }

    [MaxLength(1000)]
    public string? LoginUrl { get; set; }

    [MaxLength(500)]
    public string? UsernameSelector { get; set; }

    [MaxLength(500)]
    public string? PasswordSelector { get; set; }

    [MaxLength(500)]
    public string? SubmitSelector { get; set; }

    public virtual Site Site { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.SitePage.cs ----
public partial class SitePage
{
    [Key]
    public Guid SitePageId { get; set; }

    [Required]
    public Guid SiteId { get; set; }

    [Required, MaxLength(500)]
    public string Path { get; set; } = null!;

    [MaxLength(500)]
    public string? Title { get; set; }

    public bool RequiresAuth { get; set; }

    public bool Enabled { get; set; } = true;

    public bool IncludeInScan { get; set; } = true;

    public int SortOrder { get; set; }

    public virtual Site Site { get; set; } = null!;
}

// ---- FreeAccessibilityChecker.App.ViolationSuppression.cs ----
public partial class ViolationSuppression
{
    [Key]
    public Guid ViolationSuppressionId { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    [Required, MaxLength(200)]
    public string RuleId { get; set; } = null!;

    [MaxLength(1000)]
    public string? SelectorPattern { get; set; }

    public Guid? SiteId { get; set; }

    [Required]
    public string Reason { get; set; } = null!;

    public bool Enabled { get; set; } = true;

    public DateTime Added { get; set; }

    [MaxLength(100)]
    public string? AddedBy { get; set; }

    public DateTime LastModified { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }
}
