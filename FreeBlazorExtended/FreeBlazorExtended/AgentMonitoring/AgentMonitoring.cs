/*
    Role: AgentMonitoring feature model bundle.
    Purpose: Defines the Windows-host monitoring, inventory, alerting, and command contracts used across the feature.
    Contains: Enums, response types, and the agent-management models shared by the service layer, hubs, and showcase UI.
    Scope note: Windows-only for now; a future Linux port would need parallel runtime and model decisions where behavior diverges.
*/
namespace FreeBlazorExtended.AgentMonitoring;

public partial class AgentMonitoring
{
    // -------------------------------------------------------------------------
    // Enums
    // -------------------------------------------------------------------------
    public enum AgentStatus
    {
        Online,
        Warning,
        Error,
        Offline,
        Stale,
    }

    public enum ServiceStartupType
    {
        Automatic,
        AutomaticDelayed,
        Manual,
        Disabled,
    }

    public enum ServiceState
    {
        Running,
        Stopped,
        StartPending,
        StopPending,
        Paused,
        ContinuePending,
        PausePending,
        Unknown,
    }

    public enum AppPoolState
    {
        Started,
        Stopped,
        Starting,
        Stopping,
        Unknown,
    }

    public enum AgentCommandType
    {
        StartService,
        StopService,
        RestartService,
        UninstallService,
        InstallService,
        RecycleAppPool,
        StartAppPool,
        StopAppPool,
        StartIisSite,
        StopIisSite,
        RunScript,
        DeployArtifact,
        UpdateAgent,
        Reboot,
        CollectInventory,
    }

    public enum AgentCommandStatus
    {
        Queued,
        Sent,
        Running,
        Completed,
        Failed,
        TimedOut,
        Cancelled,
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical,
    }

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------
    public static class AgentStatuses
    {
        public const string Online = "Online";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Offline = "Offline";
        public const string Stale = "Stale";
    }

    // -------------------------------------------------------------------------
    // Common pattern - matches DataObjects.BooleanResponse from FreeCRM/FreeServicesHub
    // -------------------------------------------------------------------------
    public partial class BooleanResponse
    {
        public bool Result { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }

    public partial class ActionResponseObject
    {
        public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
    }

    // -------------------------------------------------------------------------
    // Agent - one row per registered Windows-server agent process
    // -------------------------------------------------------------------------
    public partial class Agent : ActionResponseObject
    {
        public Guid AgentId { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string AgentVersion { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public string? IpAddressInternal { get; set; }
        public string? IpAddressExternal { get; set; }
        public string Status { get; set; } = AgentStatuses.Offline;
        public DateTime? LastHeartbeat { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public string RegisteredBy { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public string TagsJson { get; set; } = string.Empty;
        public bool ManagementEnabled { get; set; } = true;

        // Audit fields - matches FreeCRM convention exactly
        public DateTime Added { get; set; }
        public string AddedBy { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentHeartbeat - time-series snapshot from an agent
    // -------------------------------------------------------------------------
    public partial class AgentHeartbeat
    {
        public Guid HeartbeatId { get; set; }
        public Guid AgentId { get; set; }
        public Guid TenantId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double CpuPercent { get; set; }
        public double MemoryPercent { get; set; }
        public double MemoryUsedGB { get; set; }
        public double MemoryTotalGB { get; set; }
        public string DiskMetricsJson { get; set; } = string.Empty;
        public string CustomDataJson { get; set; } = string.Empty;
        public string ServiceInfoJson { get; set; } = string.Empty;
        public bool Healthy { get; set; } = true;
    }

    public partial class DiskMetric
    {
        public string Drive { get; set; } = string.Empty;
        public string DriveFormat { get; set; } = string.Empty;
        public double UsedGB { get; set; }
        public double TotalGB { get; set; }
        public double Percent { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentService - a Windows Service installed on an agent's host
    //
    // Linux equivalents to research for future port:
    //   1. systemd unit (.service files; systemctl)
    //   2. OpenRC service (init.d-style on older distros)
    //   3. Supervisor (supervisord for non-init use cases)
    // -------------------------------------------------------------------------
    public partial class AgentService
    {
        public Guid AgentServiceId { get; set; }
        public Guid AgentId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string State { get; set; } = ServiceState.Unknown.ToString();
        public string StartupType { get; set; } = ServiceStartupType.Manual.ToString();
        public string LogOnAccount { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string BinaryPath { get; set; } = string.Empty;
        public DateTime? LastSeenUtc { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentIisAppPool - an IIS application pool on an agent's host
    //
    // Linux equivalents to research for future port:
    //   1. nginx upstream block (process group via PHP-FPM, uwsgi, etc.)
    //   2. systemd socket-activated service (per-pool)
    //   3. Kestrel + reverse proxy (no app-pool concept; per-process)
    // -------------------------------------------------------------------------
    public partial class AgentIisAppPool
    {
        public Guid AppPoolId { get; set; }
        public Guid AgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = AppPoolState.Unknown.ToString();
        public string ManagedRuntimeVersion { get; set; } = string.Empty;
        public string ManagedPipelineMode { get; set; } = string.Empty;
        public string IdentityType { get; set; } = string.Empty;
        public string IdentityUsername { get; set; } = string.Empty;
        public bool AutoStart { get; set; }
        public bool Enable32BitAppOnWin64 { get; set; }
        public int IdleTimeoutMinutes { get; set; }
        public int RecyclingTimeMinutes { get; set; }
        public DateTime? LastSeenUtc { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentIisSite - an IIS site bound to an app pool
    //
    // Linux equivalents to research for future port:
    //   1. nginx server { } block
    //   2. Apache <VirtualHost>
    //   3. Caddy site definition (Caddyfile)
    // -------------------------------------------------------------------------
    public partial class AgentIisSite
    {
        public Guid IisSiteId { get; set; }
        public Guid AgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PhysicalPath { get; set; } = string.Empty;
        public string AppPoolName { get; set; } = string.Empty;
        public List<AgentIisBinding> Bindings { get; set; } = new List<AgentIisBinding>();
        public DateTime? LastSeenUtc { get; set; }
    }

    public partial class AgentIisBinding
    {
        public string Protocol { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public bool RequireSsl { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentRegistrationKey - one-time registration key for new agents
    // -------------------------------------------------------------------------
    public partial class AgentRegistrationKey : ActionResponseObject
    {
        public Guid RegistrationKeyId { get; set; }
        public Guid TenantId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public Guid? ConsumedByAgentId { get; set; }
        public bool Revoked { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentApiToken - long-lived bearer token an agent uses after registration
    // -------------------------------------------------------------------------
    public partial class AgentApiToken
    {
        public Guid AgentApiTokenId { get; set; }
        public Guid AgentId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    // -------------------------------------------------------------------------
    // AgentCommand - hub-issued command queued for an agent to execute
    // -------------------------------------------------------------------------
    public partial class AgentCommand : ActionResponseObject
    {
        public Guid CommandId { get; set; }
        public Guid AgentId { get; set; }
        public Guid TenantId { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string ArgumentsJson { get; set; } = string.Empty;
        public string Status { get; set; } = AgentCommandStatus.Queued.ToString();
        public DateTime QueuedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public string ResponseMessage { get; set; } = string.Empty;
        public string ResponseDetailsJson { get; set; } = string.Empty;
    }

    // -------------------------------------------------------------------------
    // AlertRule - threshold rule for a metric on one or all agents
    // -------------------------------------------------------------------------
    public partial class AlertRule : ActionResponseObject
    {
        public Guid AlertRuleId { get; set; }
        public Guid TenantId { get; set; }
        public Guid? AgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public string Operator { get; set; } = ">";
        public double Threshold { get; set; }
        public string Severity { get; set; } = AlertSeverity.Warning.ToString();
        public bool Enabled { get; set; } = true;
        public string NotificationChannelsJson { get; set; } = string.Empty;
        public DateTime Added { get; set; }
        public string AddedBy { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public bool Deleted { get; set; }
    }

    // -------------------------------------------------------------------------
    // MonitoringAlert - a fired alert (time-series row)
    // -------------------------------------------------------------------------
    public partial class MonitoringAlert
    {
        public Guid AlertId { get; set; }
        public Guid AlertRuleId { get; set; }
        public Guid AgentId { get; set; }
        public Guid TenantId { get; set; }
        public string Severity { get; set; } = AlertSeverity.Info.ToString();
        public string Message { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double MetricValue { get; set; }
        public double Threshold { get; set; }
        public DateTime FiredAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    // -------------------------------------------------------------------------
    // Convenience aggregates for dashboard hydration
    // -------------------------------------------------------------------------
    public partial class AgentDashboardSummary
    {
        public int TotalAgents { get; set; }
        public int OnlineCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int OfflineCount { get; set; }
        public int StaleCount { get; set; }
        public int CommandsQueued { get; set; }
        public int CommandsRunning { get; set; }
        public int CommandsCompletedToday { get; set; }
        public int ActiveAlerts { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}

// =============================================================================
// Backward-compatible top-level type aliases (existing showcase code uses these)
// =============================================================================

public enum AgentStatus
{
    Online,
    Warning,
    Error,
    Offline,
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical,
}

public class MonitoringAgent
{
    /// <summary>Unique identifier for this registered agent. Generated on creation.</summary>
    public Guid AgentId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits monitoring visibility to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Friendly display name for this agent, shown in the dashboard agent list and alert messages.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The Windows <c>Environment.MachineName</c> of the host where the agent process runs.
    /// Used to correlate heartbeats and command results to the correct physical machine.
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>Optional notes about this agent's role or location (e.g. "Production web server — rack 3").</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current availability state of the agent, derived from heartbeat recency and self-reported health.
    /// <c>Online</c> = recently seen and healthy; <c>Warning</c> = metric threshold breached;
    /// <c>Error</c> = agent reported a critical condition; <c>Offline</c> = no recent heartbeat.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Online;

    /// <summary>
    /// UTC timestamp of the most recent heartbeat received from this agent.
    /// <c>null</c> when the agent has never sent a heartbeat since registration.
    /// The monitoring service compares this to <c>DateTime.UtcNow</c> to determine staleness.
    /// </summary>
    public DateTime? LastHeartbeatUtc { get; set; }

    /// <summary>UTC timestamp of when this agent first registered with the monitoring hub.</summary>
    public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of when this agent record was first created in the service.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user or process that registered this agent.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update to this agent record.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this agent record.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from all monitoring queries and dashboard views when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public class AgentHeartbeat
{
    /// <summary>Unique identifier for this heartbeat snapshot. Generated on creation.</summary>
    public Guid HeartbeatId { get; set; } = Guid.NewGuid();

    /// <summary>The agent that sent this heartbeat. Foreign key to <see cref="MonitoringAgent.AgentId"/>.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Tenant scope for the owning agent.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Snapshot of the agent's display name at the time the heartbeat was recorded.
    /// Stored here so historical heartbeat records remain readable even if the agent is renamed.
    /// </summary>
    public string AgentNameSnapshot { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when this heartbeat record was stored by the server.</summary>
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp reported by the agent itself at the moment the heartbeat was sent.</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>CPU utilization on the agent host as a percentage (0–100). Triggers <c>Warning</c> alerts when sustained above the configured threshold.</summary>
    public decimal CpuPercent { get; set; }

    /// <summary>Memory utilization as a percentage of total physical RAM. Used in dashboard sparklines and alert rules.</summary>
    public decimal MemoryPercent { get; set; }

    /// <summary>Megabytes of physical RAM currently in use on the agent host.</summary>
    public decimal MemoryMbUsed { get; set; }

    /// <summary>Alias for <see cref="MemoryMbUsed"/>; kept for backward compatibility with earlier showcase code.</summary>
    public decimal MemoryUsedMB { get; set; }

    /// <summary>Megabytes of physical RAM not currently in use on the agent host.</summary>
    public decimal MemoryMbAvailable { get; set; }

    /// <summary>Percentage of the primary disk volume that is currently used. Triggers storage alerts when above the configured threshold.</summary>
    public decimal DiskPercentUsed { get; set; }

    /// <summary>
    /// JSON array of per-drive metrics (see <c>AgentMonitoring.DiskMetric</c> for the shape).
    /// <c>null</c> when the agent is running an older version that does not report per-drive data.
    /// </summary>
    public string? DiskMetricsJson { get; set; }

    /// <summary>
    /// <c>true</c> when all self-checks on the agent pass (disk readable, services reachable, etc.).
    /// <c>false</c> triggers an immediate <c>Error</c> status on the parent agent regardless of metric thresholds.
    /// </summary>
    public bool Healthy { get; set; } = true;

    /// <summary>
    /// Optional JSON blob carrying host-specific metrics beyond the standard fields
    /// (e.g. GPU temperature, custom application counters). Displayed in the "Custom Metrics"
    /// section of the agent detail panel.
    /// </summary>
    public string? CustomMetricsJson { get; set; }

    /// <summary>UTC timestamp of when this heartbeat record was inserted into storage.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Soft-delete flag; excluded from time-series queries when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public class MonitoringAlert
{
    /// <summary>Unique identifier for this fired alert instance. Generated on creation.</summary>
    public Guid AlertId { get; set; } = Guid.NewGuid();

    /// <summary>The agent whose metric triggered this alert. Foreign key to <see cref="MonitoringAgent.AgentId"/>.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Tenant scope for the owning agent.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Visual and triage priority of this alert. <c>Info</c> is informational only;
    /// <c>Warning</c> means a threshold was breached but the agent is still functional;
    /// <c>Critical</c> requires immediate attention.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Human-readable description of what triggered this alert (e.g. "CPU at 95% for 5 minutes").
    /// Displayed in the alerts panel and any notification channels.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The name of the metric that breached the threshold (e.g. <c>"CpuPercent"</c>, <c>"DiskPercentUsed"</c>).
    /// Used to group related alerts in the dashboard and link back to the heartbeat sparkline.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>The actual value the metric had at the moment the alert fired.</summary>
    public decimal MetricValue { get; set; }

    /// <summary>The configured threshold that <see cref="MetricValue"/> crossed to trigger this alert.</summary>
    public decimal ThresholdValue { get; set; }

    /// <summary>UTC timestamp of when the alert condition was first detected and the alert was created.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// <c>true</c> when the alert has been acknowledged and the metric has returned below the threshold.
    /// Resolved alerts are moved to the alert history panel and excluded from the active-alerts badge count.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// UTC timestamp of when the alert was resolved — either automatically when the metric recovered,
    /// or manually by an operator. <c>null</c> while the alert is still active.
    /// </summary>
    public DateTime? ResolvedUtc { get; set; }

    /// <summary>UTC timestamp of when this alert record was written to storage.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user or system process that recorded this alert.</summary>
    public string? AddedBy { get; set; }
}
