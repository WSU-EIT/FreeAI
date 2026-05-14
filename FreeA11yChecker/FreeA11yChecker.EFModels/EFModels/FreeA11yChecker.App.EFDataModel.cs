using Microsoft.EntityFrameworkCore;

namespace FreeA11yChecker.EFModels.EFModels;

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
}
