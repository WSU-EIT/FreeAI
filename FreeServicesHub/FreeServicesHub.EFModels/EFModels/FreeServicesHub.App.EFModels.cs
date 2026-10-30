using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace FreeServicesHub.EFModels.EFModels;

// ---- FreeServicesHub.App.EFDataModel.cs ----
public partial class EFDataModel
{
    public virtual DbSet<Agent> Agents { get; set; }

    public virtual DbSet<RegistrationKey> RegistrationKeys { get; set; }

    public virtual DbSet<ApiClientToken> ApiClientTokens { get; set; }

    public virtual DbSet<AgentHeartbeat> AgentHeartbeats { get; set; }

    public virtual DbSet<HubJob> HubJobs { get; set; }

    public virtual DbSet<CiCdTokenUsage> CiCdTokenUsages { get; set; }

    // The OnConfiguring override is only commented out and used to build the migration scripts.
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // => optionsBuilder.UseSqlite("Data Source=C:\\Working\\FreeServicesHub.db");
    // => optionsBuilder.UseMySQL("Server=localhost;Database=FreeServicesHub;User=admin;Password=admin");
    // => optionsBuilder.UseNpgsql("Host=localhost;Database=FreeServicesHub;Username=postgres;Password=admin");
    //=> optionsBuilder.UseSqlServer("Data Source=(local);Initial Catalog=FreeServicesHub;Persist Security Info=True;User ID=sa;Password=saPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;");

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(e => e.AgentId).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Hostname).HasMaxLength(255);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.Architecture).HasMaxLength(50);
            entity.Property(e => e.AgentVersion).HasMaxLength(50);
            entity.Property(e => e.DotNetVersion).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.LastHeartbeat).HasColumnType("datetime");
            entity.Property(e => e.RegisteredAt).HasColumnType("datetime");
            entity.Property(e => e.RegisteredBy).HasMaxLength(255);
            entity.Property(e => e.Added).HasColumnType("datetime");
            entity.Property(e => e.AddedBy).HasMaxLength(100);
            entity.Property(e => e.LastModified).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<RegistrationKey>(entity =>
        {
            entity.Property(e => e.RegistrationKeyId).ValueGeneratedNever();
            entity.Property(e => e.KeyHash).HasMaxLength(100);
            entity.Property(e => e.KeyPrefix).HasMaxLength(20);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.UsedAt).HasColumnType("datetime");
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<ApiClientToken>(entity =>
        {
            entity.Property(e => e.ApiClientTokenId).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(100);
            entity.Property(e => e.TokenPrefix).HasMaxLength(20);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");
            entity.Property(e => e.RevokedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<AgentHeartbeat>(entity =>
        {
            entity.HasKey(e => e.HeartbeatId);

            entity.Property(e => e.HeartbeatId).ValueGeneratedNever();
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
        });

        modelBuilder.Entity<HubJob>(entity =>
        {
            entity.Property(e => e.HubJobId).ValueGeneratedNever();
            entity.Property(e => e.JobType).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.StartedAt).HasColumnType("datetime");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.LastModified).HasColumnType("datetime");
            entity.Property(e => e.DeletedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CiCdTokenUsage>(entity =>
        {
            entity.Property(e => e.CiCdTokenUsageId).ValueGeneratedNever();
            entity.Property(e => e.TokenHash).HasMaxLength(100);
            entity.Property(e => e.TokenPrefix).HasMaxLength(20);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Created).HasColumnType("datetime");
            entity.Property(e => e.InvalidatedAt).HasColumnType("datetime");
        });
    }
}

// ---- FreeServicesHub.App.Agent.cs ----
public partial class Agent
{
    public Guid AgentId { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Hostname { get; set; }

    public string? OperatingSystem { get; set; }

    public string? Architecture { get; set; }

    public string? AgentVersion { get; set; }

    public string? DotNetVersion { get; set; }

    public string? Status { get; set; }

    public DateTime? LastHeartbeat { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public string? RegisteredBy { get; set; }

    public DateTime Added { get; set; }

    public string? AddedBy { get; set; }

    public DateTime LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}

// ---- FreeServicesHub.App.AgentHeartbeat.cs ----
public partial class AgentHeartbeat
{
    public Guid HeartbeatId { get; set; }

    public Guid AgentId { get; set; }

    public DateTime Timestamp { get; set; }

    public double CpuPercent { get; set; }

    public double MemoryPercent { get; set; }

    public double MemoryUsedGB { get; set; }

    public double MemoryTotalGB { get; set; }

    public string? DiskMetricsJson { get; set; }

    public string? CustomDataJson { get; set; }
}

// ---- FreeServicesHub.App.ApiClientToken.cs ----
public partial class ApiClientToken
{
    public Guid ApiClientTokenId { get; set; }

    public Guid AgentId { get; set; }

    public Guid TenantId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string? TokenPrefix { get; set; }

    public bool Active { get; set; }

    public DateTime Created { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedBy { get; set; }
}

// ---- FreeServicesHub.App.CiCdTokenUsage.cs ----
public partial class CiCdTokenUsage
{
    public Guid CiCdTokenUsageId { get; set; }
    public Guid TenantId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string? TokenPrefix { get; set; }
    public int MaxUses { get; set; }
    public int UsesConsumed { get; set; }
    public bool Active { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime Created { get; set; }
    public DateTime? InvalidatedAt { get; set; }
}

// ---- FreeServicesHub.App.HubJob.cs ----
public partial class HubJob
{
    public Guid HubJobId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? AgentId { get; set; }

    public string JobType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Payload { get; set; }

    public string? Result { get; set; }

    public string? ErrorMessage { get; set; }

    public int Priority { get; set; }

    public int MaxRetries { get; set; }

    public int RetryCount { get; set; }

    public DateTime Created { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public bool Deleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}

// ---- FreeServicesHub.App.RegistrationKey.cs ----
public partial class RegistrationKey
{
    public Guid RegistrationKeyId { get; set; }

    public Guid TenantId { get; set; }

    public string KeyHash { get; set; } = null!;

    public string? KeyPrefix { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public Guid? UsedByAgentId { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime Created { get; set; }

    public string? CreatedBy { get; set; }
}
