namespace FreeServicesHub;

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
