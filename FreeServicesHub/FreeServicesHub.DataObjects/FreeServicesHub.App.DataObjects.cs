namespace FreeServicesHub;

// ---- FreeServicesHub.App.Config.cs ----
// Interface — what consumers see
public partial interface IConfigurationHelper
{
    public int AgentHeartbeatIntervalSeconds { get; }
    public int AgentStaleThresholdSeconds { get; }
    public string CiCdAdminToken { get; }
    public int CiCdMaxAgentRegistrations { get; }
    public int CpuErrorThreshold { get; }
    public int CpuWarningThreshold { get; }
    public int DiskErrorThreshold { get; }
    public int DiskWarningThreshold { get; }
    public int HeartbeatRetentionHours { get; }
    public int MemoryErrorThreshold { get; }
    public int MemoryWarningThreshold { get; }
    public int RegistrationKeyExpiryHours { get; }
}

// Implementation — reads from loader
public partial class ConfigurationHelper : IConfigurationHelper
{
    public int AgentHeartbeatIntervalSeconds { get { return _loader.AgentHeartbeatIntervalSeconds; } }
    public int AgentStaleThresholdSeconds { get { return _loader.AgentStaleThresholdSeconds; } }
    public string CiCdAdminToken { get { return _loader.CiCdAdminToken; } }
    public int CiCdMaxAgentRegistrations { get { return _loader.CiCdMaxAgentRegistrations; } }
    public int CpuErrorThreshold { get { return _loader.CpuErrorThreshold; } }
    public int CpuWarningThreshold { get { return _loader.CpuWarningThreshold; } }
    public int DiskErrorThreshold { get { return _loader.DiskErrorThreshold; } }
    public int DiskWarningThreshold { get { return _loader.DiskWarningThreshold; } }
    public int HeartbeatRetentionHours { get { return _loader.HeartbeatRetentionHours; } }
    public int MemoryErrorThreshold { get { return _loader.MemoryErrorThreshold; } }
    public int MemoryWarningThreshold { get { return _loader.MemoryWarningThreshold; } }
    public int RegistrationKeyExpiryHours { get { return _loader.RegistrationKeyExpiryHours; } }
}

// Loader — populated during startup
public partial class ConfigurationHelperLoader
{
    public int AgentHeartbeatIntervalSeconds { get; set; } = 30;
    public int AgentStaleThresholdSeconds { get; set; } = 120;
    public string CiCdAdminToken { get; set; } = string.Empty;
    public int CiCdMaxAgentRegistrations { get; set; } = 10;
    public int CpuErrorThreshold { get; set; } = 90;
    public int CpuWarningThreshold { get; set; } = 70;
    public int DiskErrorThreshold { get; set; } = 90;
    public int DiskWarningThreshold { get; set; } = 50;
    public int HeartbeatRetentionHours { get; set; } = 24;
    public int MemoryErrorThreshold { get; set; } = 90;
    public int MemoryWarningThreshold { get; set; } = 70;
    public int RegistrationKeyExpiryHours { get; set; } = 24;
}

// ---- FreeServicesHub.App.DataObjects.AgentSettings.cs ----
public partial class DataObjects
{
    /// <summary>
    /// Windows Service metadata and editable agent settings.
    /// Sent with every heartbeat and on-demand via SignalR.
    /// </summary>
    public class AgentServiceInfo
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;

        // ── Runtime info ──
        public string AgentVersion { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public int HeartbeatIntervalSeconds { get; set; } = 30;

        // ── Editable agent settings ──
        public string HubUrl { get; set; } = string.Empty;
        public DateTime? LastBootTime { get; set; }
        public string LogOnAccount { get; set; } = string.Empty;   // e.g. LocalSystem, NT AUTHORITY\NETWORK SERVICE
        public int ProcessId { get; set; }
        public string ServiceDescription { get; set; } = string.Empty;

        // ── Windows Service read-only metadata ──
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceStatus { get; set; } = string.Empty; // Running, Stopped, StartPending, etc.
        public string StartupType { get; set; } = string.Empty;   // Automatic, Manual, Disabled
        public DateTime TimestampUtc { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    /// <summary>
    /// Payload sent from the hub to an agent to update its editable settings.
    /// </summary>
    public class AgentSettingsUpdate
    {
        public Guid AgentId { get; set; }
        public string? AgentName { get; set; }
        public int? HeartbeatIntervalSeconds { get; set; }
        public string? HubUrl { get; set; }
        public string? StartupType { get; set; } // Automatic, Manual, Disabled
    }
}

// ---- FreeServicesHub.App.DataObjects.Agents.cs ----
public partial class DataObjects
{
    public class GetAgentsRequest
    {
        public List<Guid>? Ids { get; set; }
    }

    public class GetHubJobsRequest
    {
        public List<Guid>? Ids { get; set; }
    }

    // Agent - a registered service agent instance
    public class Agent : ActionResponseObject
    {
        public DateTime Added { get; set; }
        public string AddedBy { get; set; } = string.Empty;
        public Guid AgentId { get; set; }
        public string AgentVersion { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DotNetVersion { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public DateTime? LastHeartbeat { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public DateTime? RegisteredAt { get; set; }
        public string RegisteredBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Online, Warning, Error, Offline
        public Guid TenantId { get; set; }
    }

    // AgentHeartbeat - time-series snapshot from an agent
    public class AgentHeartbeat
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty; // Denormalized for display
        public double CpuPercent { get; set; }
        public string CustomDataJson { get; set; } = string.Empty;  // Extensible JSON block
        public string DiskMetricsJson { get; set; } = string.Empty; // JSON array of {Drive, UsedGB, TotalGB, Percent}
        public Guid HeartbeatId { get; set; }
        public double MemoryPercent { get; set; }
        public double MemoryTotalGB { get; set; }
        public double MemoryUsedGB { get; set; }
        public string ServiceInfoJson { get; set; } = string.Empty; // JSON of AgentServiceInfo
        public DateTime Timestamp { get; set; }
    }

    // DiskMetric - individual disk stats (deserialized from DiskMetricsJson)
    public class DiskMetric
    {
        public string Drive { get; set; } = string.Empty;
        public double Percent { get; set; }
        public double TotalGB { get; set; }
        public double UsedGB { get; set; }
    }

    // AgentStatus constants
    public static class AgentStatuses
    {
        public const string Online = "Online";
        public const string Warning = "Warning";
        public const string Error = "Error";
        public const string Offline = "Offline";
        public const string Stale = "Stale";
    }

    // Extend the BlazorDataModelLoader with agent summary data for dashboard hydration
    public partial class BlazorDataModelLoader
    {
        public List<Agent> AgentStatuses { get; set; } = new List<Agent>();
    }
}

// ---- FreeServicesHub.App.DataObjects.ApiKeys.cs ----
public partial class DataObjects
{
    // RegistrationKey - one-time use key generated by CI/CD pipeline
    public class RegistrationKey : ActionResponseObject
    {
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string KeyHash { get; set; } = string.Empty;      // SHA-256 hash, never store plaintext
        public string KeyPrefix { get; set; } = string.Empty;     // First 8 chars for identification
        public string NewKeyPlaintext { get; set; } = string.Empty; // Transient - only populated on generation, never stored
        public Guid RegistrationKeyId { get; set; }
        public Guid TenantId { get; set; }
        public bool Used { get; set; }
        public DateTime? UsedAt { get; set; }
        public Guid? UsedByAgentId { get; set; }
    }

    // ApiClientToken - long-lived revocable token issued to agent during registration
    public class ApiClientToken : ActionResponseObject
    {
        public bool Active { get; set; } = true;
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;      // Denormalized for display
        public Guid ApiClientTokenId { get; set; }
        public DateTime Created { get; set; }
        public string NewTokenPlaintext { get; set; } = string.Empty; // Transient - only on generation
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string TokenHash { get; set; } = string.Empty;     // SHA-256 hash
        public string TokenPrefix { get; set; } = string.Empty;    // First 8 chars for identification
    }

    // AgentRegistrationRequest - what the agent sends to register
    public class AgentRegistrationRequest
    {
        public string AgentVersion { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string RegistrationKey { get; set; } = string.Empty; // Plaintext key
    }

    // AgentRegistrationResponse - what the hub returns after registration
    public class AgentRegistrationResponse : ActionResponseObject
    {
        public Guid AgentId { get; set; }
        public string ApiClientToken { get; set; } = string.Empty; // Plaintext token, shown once
        public string HubUrl { get; set; } = string.Empty;
    }

    // CiCdRegistrationResponse - returned by the CI/CD pipeline registration endpoint
    public class CiCdRegistrationResponse
    {
        public string AgentSlug { get; set; } = string.Empty;       // Friendly name / primary key for this agent
        public DateTime ExpiresAt { get; set; }                      // When the key expires
        public string Message { get; set; } = string.Empty;
        public string RegistrationKey { get; set; } = string.Empty;  // Plaintext key to inject into agent appsettings.json
    }
}

// ---- FreeServicesHub.App.DataObjects.BackgroundServiceLogs.cs ----
public partial class DataObjects
{
    /// <summary>
    /// Represents a single log entry produced by a hub-side background service.
    /// Transmitted via SignalR for real-time display on the Background Services page.
    /// </summary>
    public class BackgroundServiceLogEntry
    {
        public Guid LogId { get; set; }
        public string LogLevel { get; set; } = string.Empty; // Information, Warning, Error, etc.
        public string Message { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Describes a registered hub-side background service and its current state.
    /// </summary>
    public class BackgroundServiceInfo
    {
        public string Description { get; set; } = string.Empty;
        public DateTime? LastActivity { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = "Running"; // Running, Stopped, Error
    }
}

// ---- FreeServicesHub.App.DataObjects.Jobs.cs ----
public partial class DataObjects
{
    public class HubJob : ActionResponseObject
    {
        public Guid? AgentId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Guid HubJobId { get; set; }
        public string JobType { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public int MaxRetries { get; set; }
        public string Payload { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Result { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime? StartedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
    }

    public static class HubJobStatuses
    {
        public const string Queued = "Queued";
        public const string Assigned = "Assigned";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }
}

// ---- FreeServicesHub.App.DataObjects.SignalR.cs ----
// FreeServicesHub SignalR update types. DataObjects.App.cs stays stock FreeCRM.

public partial class DataObjects
{
    public partial class SignalRUpdateType
    {
        public const string AgentHeartbeat = "AgentHeartbeat";
        public const string AgentConnected = "AgentConnected";
        public const string AgentDisconnected = "AgentDisconnected";
        public const string AgentStatusChanged = "AgentStatusChanged";
        public const string AgentShutdown = "AgentShutdown";
        public const string RegistrationKeyGenerated = "RegistrationKeyGenerated";
        public const string BackgroundServiceLog = "BackgroundServiceLog";
        public const string AgentSettingsUpdated = "AgentSettingsUpdated";
        public const string AgentSettingsReport = "AgentSettingsReport";
        public const string JobUpdated = "JobUpdated";
        public const string JobCompleted = "JobCompleted";
    }
}
