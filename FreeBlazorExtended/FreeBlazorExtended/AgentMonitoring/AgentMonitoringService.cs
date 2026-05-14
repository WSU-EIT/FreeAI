/*
    Role: AgentMonitoring feature orchestration service.
    Purpose: Owns the in-memory monitoring state, alert evaluation, registration flows, and remote-command surface for managed agents.
    Contains: The lightweight monitoring API plus the richer management API layered on top of the same feature state.
    Host expectations: Can run in in-memory showcase mode, but command dispatch becomes real only when the host wires the related SignalR dispatcher and endpoints.
*/
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FreeBlazorExtended.AgentMonitoring;

public class AgentMonitoringService
{
    private static readonly ConcurrentDictionary<Guid, Agent> _agents = new();
    private static readonly ConcurrentDictionary<Guid, List<AgentHeartbeat>> _heartbeats = new();
    private static readonly ConcurrentDictionary<Guid, AlertRule> _alertRules = new();

    // Optional SignalR hub context. When the host has wired up AgentHub via
    // app.MapHub<AgentHub>("/agentHub") and added DI for IHubContext<AgentHub>,
    // command-issuing methods will push the command over the wire to the
    // connected agent. When null (e.g. unit tests, or a deployment where the
    // hub isn't mapped), commands fall back to in-memory simulation.
    private readonly IAgentCommandDispatcher? _commandDispatcher;
    private readonly ILogger<AgentMonitoringService>? _logger;

    public AgentMonitoringService()
    {
    }

    public AgentMonitoringService(IAgentCommandDispatcher commandDispatcher, ILogger<AgentMonitoringService> logger)
    {
        _commandDispatcher = commandDispatcher;
        _logger = logger;
    }

    public Task<Agent> RegisterAgent(Guid tenantId, string name, string machineName, string? description = null, string? userId = null)
    {
        var agent = new Agent
        {
            AgentId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            MachineName = machineName,
            Description = description,
            Status = AgentStatus.Online,
            RegisteredUtc = DateTime.UtcNow,
            AddedBy = userId
        };

        _agents[agent.AgentId] = agent;
        _heartbeats[agent.AgentId] = new();

        return Task.FromResult(agent);
    }

    public Task<List<Agent>> GetAgents(Guid tenantId)
    {
        var agents = _agents.Values
            .Where(a => a.TenantId == tenantId && !a.Deleted)
            .ToList();
        return Task.FromResult(agents);
    }

    public Task<Agent?> GetAgent(Guid agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent?.Deleted == false ? agent : null);
    }

    public async Task<AgentHeartbeat> RecordHeartbeat(
        Guid agentId,
        double cpuPercent,
        double memoryPercent,
        double memoryUsedMb,
        Dictionary<string, (double usedGb, double totalGb)>? diskMetrics = null,
        Dictionary<string, object>? customMetrics = null)
    {
        var agent = await GetAgent(agentId);
        if (agent == null)
            throw new InvalidOperationException("Agent not found");

        var heartbeat = new AgentHeartbeat
        {
            HeartbeatId = Guid.NewGuid(),
            AgentId = agentId,
            AgentNameSnapshot = agent.Name,
            TimestampUtc = DateTime.UtcNow,
            CpuPercent = (decimal)cpuPercent,
            MemoryPercent = (decimal)memoryPercent,
            MemoryUsedMB = (decimal)memoryUsedMb,
            DiskMetricsJson = diskMetrics != null ? JsonSerializer.Serialize(diskMetrics) : null,
            CustomMetricsJson = customMetrics != null ? JsonSerializer.Serialize(customMetrics) : null,
            Healthy = cpuPercent < 80 && memoryPercent < 80
        };

        // Store heartbeat
        if (!_heartbeats.ContainsKey(agentId))
            _heartbeats[agentId] = new();

        _heartbeats[agentId].Add(heartbeat);

        // Keep only last 1000 heartbeats per agent
        if (_heartbeats[agentId].Count > 1000)
            _heartbeats[agentId] = _heartbeats[agentId].TakeLast(1000).ToList();

        // Update agent status
        agent.LastHeartbeatUtc = heartbeat.TimestampUtc;
        agent.Status = heartbeat.Healthy ? AgentStatus.Online : AgentStatus.Warning;

        // Check alert rules
        await CheckAlertRules(agent, heartbeat);

        return heartbeat;
    }

    public Task<List<AgentHeartbeat>> GetHeartbeats(Guid agentId, DateTime? since = null)
    {
        if (!_heartbeats.TryGetValue(agentId, out var beats))
            return Task.FromResult(new List<AgentHeartbeat>());

        var result = beats.AsEnumerable();
        if (since.HasValue)
            result = result.Where(b => b.TimestampUtc >= since.Value);

        return Task.FromResult(result.OrderByDescending(b => b.TimestampUtc).ToList());
    }

    public Task<AgentHeartbeat?> GetLatestHeartbeat(Guid agentId)
    {
        if (!_heartbeats.TryGetValue(agentId, out var beats))
            return Task.FromResult((AgentHeartbeat?)null);

        return Task.FromResult(beats.OrderByDescending(b => b.TimestampUtc).FirstOrDefault());
    }

    public Task<AlertRule> CreateAlertRule(
        Guid tenantId,
        string name,
        string description,
        string metricName,
        string condition,
        double threshold,
        string? userId = null)
    {
        var rule = new AlertRule
        {
            AlertRuleId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            MetricName = metricName,
            Condition = condition, // "gt", "lt", "eq"
            Threshold = threshold,
            IsActive = true,
            AddedBy = userId
        };

        _alertRules[rule.AlertRuleId] = rule;
        return Task.FromResult(rule);
    }

    public Task<List<AlertRule>> GetAlertRules(Guid tenantId)
    {
        var rules = _alertRules.Values
            .Where(r => r.TenantId == tenantId && !r.Deleted)
            .ToList();
        return Task.FromResult(rules);
    }

    private async Task CheckAlertRules(Agent agent, AgentHeartbeat heartbeat)
    {
        var rules = await GetAlertRules(agent.TenantId);

        foreach (var rule in rules.Where(r => r.IsActive))
        {
            double? metricValue = rule.MetricName switch
            {
                "cpu" => (double)heartbeat.CpuPercent,
                "memory" => (double)heartbeat.MemoryPercent,
                "disk" => TryGetDiskMetric(heartbeat.DiskMetricsJson),
                _ => null
            };

            if (metricValue.HasValue && EvaluateCondition(metricValue.Value, rule.Condition, rule.Threshold))
            {
                // Alert triggered - in a real implementation, notify via email, webhook, etc.
                agent.Status = AgentStatus.Error;
            }
        }
    }

    private double? TryGetDiskMetric(string? diskMetricsJson)
    {
        if (string.IsNullOrEmpty(diskMetricsJson))
            return null;

        try
        {
            var metrics = JsonSerializer.Deserialize<Dictionary<string, (double usedGb, double totalGb)>>(diskMetricsJson);
            if (metrics?.Any() == true)
            {
                var first = metrics.First().Value;
                return (first.usedGb / first.totalGb) * 100;
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"AgentMonitoringService: failed to parse disk metrics JSON: {ex.Message}");
        }

        return null;
    }

    private bool EvaluateCondition(double value, string condition, double threshold)
    {
        return condition switch
        {
            "gt" => value > threshold,
            "lt" => value < threshold,
            "gte" => value >= threshold,
            "lte" => value <= threshold,
            "eq" => Math.Abs(value - threshold) < 0.01,
            _ => false
        };
    }

    public void ClearAll()
    {
        _agents.Clear();
        _heartbeats.Clear();
        _alertRules.Clear();
        _managedAgents.Clear();
        _agentServices.Clear();
        _agentAppPools.Clear();
        _agentIisSites.Clear();
        _registrationKeys.Clear();
        _commands.Clear();
        _seeded = false;
    }

    // =========================================================================
    // Comprehensive Agent Management API
    //
    // Adds the full ADO-runner-style management surface on top of the simple
    // monitoring API above. Uses the strongly-typed DataObjects in
    // FreeBlazorExtended.Models.AgentMonitoring (matches the FreeServicesHub
    // partial-class convention).
    //
    // In a production deployment, the FreeBlazorExtended.Agent process running
    // as a Windows Service on each host would handle the actual execution of
    // these commands using ServiceController, Microsoft.Web.Administration, etc.
    // For this in-process showcase, the service simulates remote execution by
    // mutating the in-memory state and returning success/failure responses
    // immediately.
    // =========================================================================

    private static readonly ConcurrentDictionary<Guid, AgentMonitoring.Agent> _managedAgents = new();
    private static readonly ConcurrentDictionary<Guid, List<AgentMonitoring.AgentService>> _agentServices = new();
    private static readonly ConcurrentDictionary<Guid, List<AgentMonitoring.AgentIisAppPool>> _agentAppPools = new();
    private static readonly ConcurrentDictionary<Guid, List<AgentMonitoring.AgentIisSite>> _agentIisSites = new();
    private static readonly ConcurrentDictionary<Guid, AgentMonitoring.AgentRegistrationKey> _registrationKeys = new();
    private static readonly ConcurrentDictionary<Guid, AgentMonitoring.AgentCommand> _commands = new();
    private static bool _seeded;
    private static readonly object _seedLock = new object();

    // -------------------------------------------------------------------------
    // Agents - CRUD
    // -------------------------------------------------------------------------

    public Task<List<AgentMonitoring.Agent>> GetManagedAgents(Guid tenantId, DataObjects.User? CurrentUser = null)
    {
        EnsureSeeded(tenantId);

        var output = _managedAgents.Values
            .Where(x => x.TenantId == tenantId && !x.Deleted)
            .OrderBy(x => x.Name)
            .ToList();

        return Task.FromResult(output);
    }

    public Task<AgentMonitoring.Agent?> GetManagedAgent(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        if (_managedAgents.TryGetValue(AgentId, out var agent) && !agent.Deleted) {
            return Task.FromResult<AgentMonitoring.Agent?>(agent);
        }
        return Task.FromResult<AgentMonitoring.Agent?>(null);
    }

    public Task<AgentMonitoring.Agent> SaveManagedAgent(AgentMonitoring.Agent agent, DataObjects.User? CurrentUser = null)
    {
        if (agent.AgentId == Guid.Empty) {
            agent.AgentId = Guid.NewGuid();
            agent.Added = DateTime.UtcNow;
            agent.AddedBy = CurrentUserIdString(CurrentUser);
            agent.RegisteredAt = DateTime.UtcNow;
            agent.RegisteredBy = CurrentUserIdString(CurrentUser);
        }

        agent.LastModified = DateTime.UtcNow;
        agent.LastModifiedBy = CurrentUserIdString(CurrentUser);
        agent.ActionResponse.Result = true;

        _managedAgents[agent.AgentId] = agent;
        return Task.FromResult(agent);
    }

    public Task<AgentMonitoring.BooleanResponse> DeleteManagedAgent(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        AgentMonitoring.BooleanResponse output = new AgentMonitoring.BooleanResponse();

        if (_managedAgents.TryGetValue(AgentId, out var agent)) {
            agent.Deleted = true;
            agent.DeletedAt = DateTime.UtcNow;
            agent.LastModified = DateTime.UtcNow;
            agent.LastModifiedBy = CurrentUserIdString(CurrentUser);
            output.Result = true;
        } else {
            output.Messages.Add("Error Deleting Agent " + AgentId.ToString() + " - Record No Longer Exists");
        }

        return Task.FromResult(output);
    }

    // -------------------------------------------------------------------------
    // Windows Services on an Agent
    //
    // Linux equivalents to research for future port:
    //   1. systemd unit (.service files; systemctl start/stop/restart/disable)
    //   2. OpenRC service (rc-service)
    //   3. Supervisor (supervisord) for non-init managed processes
    // -------------------------------------------------------------------------

    public Task<List<AgentMonitoring.AgentService>> GetAgentServices(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        if (_agentServices.TryGetValue(AgentId, out var list)) {
            return Task.FromResult(list.OrderBy(x => x.DisplayName).ToList());
        }
        return Task.FromResult(new List<AgentMonitoring.AgentService>());
    }

    public Task<AgentMonitoring.AgentCommand> StartService(Guid AgentId, string ServiceName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteServiceCommand(AgentId, ServiceName, AgentMonitoring.AgentCommandType.StartService, CurrentUser);
    }

    public Task<AgentMonitoring.AgentCommand> StopService(Guid AgentId, string ServiceName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteServiceCommand(AgentId, ServiceName, AgentMonitoring.AgentCommandType.StopService, CurrentUser);
    }

    public Task<AgentMonitoring.AgentCommand> RestartService(Guid AgentId, string ServiceName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteServiceCommand(AgentId, ServiceName, AgentMonitoring.AgentCommandType.RestartService, CurrentUser);
    }

    public Task<AgentMonitoring.AgentCommand> UninstallService(Guid AgentId, string ServiceName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteServiceCommand(AgentId, ServiceName, AgentMonitoring.AgentCommandType.UninstallService, CurrentUser);
    }

    private async Task<AgentMonitoring.AgentCommand> ExecuteServiceCommand(Guid AgentId, string ServiceName, AgentMonitoring.AgentCommandType CommandType, DataObjects.User? CurrentUser)
    {
        AgentMonitoring.AgentCommand cmd = QueueCommand(AgentId, CommandType.ToString(), ServiceName, "", CurrentUser);

        // Wire path: if a worker is connected for this agent, push the command
        // over SignalR. The worker will invoke ReportCommandResult when finished
        // and that updates this same row to Completed/Failed.
        if (_commandDispatcher != null) {
            try {
                if (await _commandDispatcher.TryDispatchServiceCommand(AgentId, cmd.CommandId, CommandType.ToString(), ServiceName)) {
                    cmd.Status = AgentMonitoring.AgentCommandStatus.Sent.ToString();
                    cmd.SentAt = DateTime.UtcNow;
                    _logger?.LogInformation("Dispatched {Cmd} on '{Svc}' to agent {AgentId} via hub",
                        CommandType, ServiceName, AgentId);
                    return cmd;
                }
            } catch (Exception ex) {
                cmd.Status = AgentMonitoring.AgentCommandStatus.Failed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ResponseMessage = "Hub dispatch failed: " + ex.Message;
                cmd.ActionResponse.Messages.Add(cmd.ResponseMessage);
                _logger?.LogError(ex, "Hub dispatch failed for command {Cmd}", cmd.CommandId);
                return cmd;
            }
        }

        // Fallback path: no worker connected -- simulate the result in-memory so
        // the showcase dashboard still demonstrates the workflow end-to-end.
        _logger?.LogInformation("No agent worker connected for {AgentId}; simulating {Cmd} on '{Svc}'",
            AgentId, CommandType, ServiceName);

        if (_agentServices.TryGetValue(AgentId, out var list)) {
            var svc = list.FirstOrDefault(x => x.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
            if (svc != null) {
                switch (CommandType) {
                    case AgentMonitoring.AgentCommandType.StartService:
                        svc.State = AgentMonitoring.ServiceState.Running.ToString();
                        cmd.ResponseMessage = "Service started.";
                        break;

                    case AgentMonitoring.AgentCommandType.StopService:
                        svc.State = AgentMonitoring.ServiceState.Stopped.ToString();
                        svc.ProcessId = 0;
                        cmd.ResponseMessage = "Service stopped.";
                        break;

                    case AgentMonitoring.AgentCommandType.RestartService:
                        svc.State = AgentMonitoring.ServiceState.Running.ToString();
                        cmd.ResponseMessage = "Service restarted.";
                        break;

                    case AgentMonitoring.AgentCommandType.UninstallService:
                        list.Remove(svc);
                        cmd.ResponseMessage = "Service uninstalled.";
                        break;
                }

                cmd.Status = AgentMonitoring.AgentCommandStatus.Completed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ActionResponse.Result = true;
            } else {
                cmd.Status = AgentMonitoring.AgentCommandStatus.Failed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ResponseMessage = "Service not found on agent.";
                cmd.ActionResponse.Messages.Add(cmd.ResponseMessage);
            }
        } else {
            cmd.Status = AgentMonitoring.AgentCommandStatus.Failed.ToString();
            cmd.CompletedAt = DateTime.UtcNow;
            cmd.ResponseMessage = "Agent has no service inventory.";
            cmd.ActionResponse.Messages.Add(cmd.ResponseMessage);
        }

        return cmd;
    }

    // -------------------------------------------------------------------------
    // IIS Application Pools and Sites on an Agent
    //
    // Linux equivalents to research for future port:
    //   1. nginx server { } / upstream { } blocks
    //   2. Apache <VirtualHost> + mod_proxy
    //   3. Caddy site definitions (Caddyfile)
    // -------------------------------------------------------------------------

    public Task<List<AgentMonitoring.AgentIisAppPool>> GetAgentAppPools(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        if (_agentAppPools.TryGetValue(AgentId, out var list)) {
            return Task.FromResult(list.OrderBy(x => x.Name).ToList());
        }
        return Task.FromResult(new List<AgentMonitoring.AgentIisAppPool>());
    }

    public Task<List<AgentMonitoring.AgentIisSite>> GetAgentIisSites(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        if (_agentIisSites.TryGetValue(AgentId, out var list)) {
            return Task.FromResult(list.OrderBy(x => x.Name).ToList());
        }
        return Task.FromResult(new List<AgentMonitoring.AgentIisSite>());
    }

    public Task<AgentMonitoring.AgentCommand> RecycleAppPool(Guid AgentId, string AppPoolName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteAppPoolCommand(AgentId, AppPoolName, AgentMonitoring.AgentCommandType.RecycleAppPool, CurrentUser);
    }

    public Task<AgentMonitoring.AgentCommand> StartAppPool(Guid AgentId, string AppPoolName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteAppPoolCommand(AgentId, AppPoolName, AgentMonitoring.AgentCommandType.StartAppPool, CurrentUser);
    }

    public Task<AgentMonitoring.AgentCommand> StopAppPool(Guid AgentId, string AppPoolName, DataObjects.User? CurrentUser = null)
    {
        return ExecuteAppPoolCommand(AgentId, AppPoolName, AgentMonitoring.AgentCommandType.StopAppPool, CurrentUser);
    }

    private async Task<AgentMonitoring.AgentCommand> ExecuteAppPoolCommand(Guid AgentId, string AppPoolName, AgentMonitoring.AgentCommandType CommandType, DataObjects.User? CurrentUser)
    {
        AgentMonitoring.AgentCommand cmd = QueueCommand(AgentId, CommandType.ToString(), AppPoolName, "", CurrentUser);

        // Wire path: hand off to the connected worker over SignalR.
        if (_commandDispatcher != null) {
            try {
                if (await _commandDispatcher.TryDispatchAppPoolCommand(AgentId, cmd.CommandId, CommandType.ToString(), AppPoolName)) {
                    cmd.Status = AgentMonitoring.AgentCommandStatus.Sent.ToString();
                    cmd.SentAt = DateTime.UtcNow;
                    _logger?.LogInformation("Dispatched {Cmd} on '{Pool}' to agent {AgentId} via hub",
                        CommandType, AppPoolName, AgentId);
                    return cmd;
                }
            } catch (Exception ex) {
                cmd.Status = AgentMonitoring.AgentCommandStatus.Failed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ResponseMessage = "Hub dispatch failed: " + ex.Message;
                cmd.ActionResponse.Messages.Add(cmd.ResponseMessage);
                return cmd;
            }
        }

        // Fallback: no agent connected. Simulate the result locally.
        _logger?.LogInformation("No agent worker connected for {AgentId}; simulating {Cmd} on '{Pool}'",
            AgentId, CommandType, AppPoolName);

        if (_agentAppPools.TryGetValue(AgentId, out var list)) {
            var pool = list.FirstOrDefault(x => x.Name.Equals(AppPoolName, StringComparison.OrdinalIgnoreCase));
            if (pool != null) {
                switch (CommandType) {
                    case AgentMonitoring.AgentCommandType.StartAppPool:
                        pool.State = AgentMonitoring.AppPoolState.Started.ToString();
                        cmd.ResponseMessage = "Application pool started.";
                        break;

                    case AgentMonitoring.AgentCommandType.StopAppPool:
                        pool.State = AgentMonitoring.AppPoolState.Stopped.ToString();
                        cmd.ResponseMessage = "Application pool stopped.";
                        break;

                    case AgentMonitoring.AgentCommandType.RecycleAppPool:
                        pool.State = AgentMonitoring.AppPoolState.Started.ToString();
                        cmd.ResponseMessage = "Application pool recycled.";
                        break;
                }

                cmd.Status = AgentMonitoring.AgentCommandStatus.Completed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ActionResponse.Result = true;
            } else {
                cmd.Status = AgentMonitoring.AgentCommandStatus.Failed.ToString();
                cmd.CompletedAt = DateTime.UtcNow;
                cmd.ResponseMessage = "Application pool not found on agent.";
                cmd.ActionResponse.Messages.Add(cmd.ResponseMessage);
            }
        }

        return cmd;
    }

    // -------------------------------------------------------------------------
    // Registration Keys
    // -------------------------------------------------------------------------

    public Task<AgentMonitoring.AgentRegistrationKey> GenerateRegistrationKey(Guid TenantId, string Description, int ExpiresInHours = 24, DataObjects.User? CurrentUser = null)
    {
        AgentMonitoring.AgentRegistrationKey key = new AgentMonitoring.AgentRegistrationKey {
            RegistrationKeyId = Guid.NewGuid(),
            TenantId = TenantId,
            Key = Guid.NewGuid().ToString("N").ToUpperInvariant(),
            Description = Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentUserIdString(CurrentUser),
            ExpiresAt = DateTime.UtcNow.AddHours(ExpiresInHours),
        };
        key.ActionResponse.Result = true;

        _registrationKeys[key.RegistrationKeyId] = key;
        return Task.FromResult(key);
    }

    public Task<List<AgentMonitoring.AgentRegistrationKey>> GetRegistrationKeys(Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        var output = _registrationKeys.Values
            .Where(x => x.TenantId == TenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        return Task.FromResult(output);
    }

    public Task<AgentMonitoring.BooleanResponse> RevokeRegistrationKey(Guid RegistrationKeyId, DataObjects.User? CurrentUser = null)
    {
        AgentMonitoring.BooleanResponse output = new AgentMonitoring.BooleanResponse();

        if (_registrationKeys.TryGetValue(RegistrationKeyId, out var key)) {
            key.Revoked = true;
            output.Result = true;
        } else {
            output.Messages.Add("Registration key not found.");
        }

        return Task.FromResult(output);
    }

    // -------------------------------------------------------------------------
    // Commands - history of all hub-issued commands
    // -------------------------------------------------------------------------

    public Task<List<AgentMonitoring.AgentCommand>> GetCommands(Guid AgentId, DataObjects.User? CurrentUser = null)
    {
        var output = _commands.Values
            .Where(x => x.AgentId == AgentId)
            .OrderByDescending(x => x.QueuedAt)
            .ToList();
        return Task.FromResult(output);
    }

    public Task<AgentMonitoring.AgentCommand?> GetCommand(Guid CommandId, DataObjects.User? CurrentUser = null)
    {
        _commands.TryGetValue(CommandId, out var cmd);
        return Task.FromResult(cmd);
    }

    private AgentMonitoring.AgentCommand QueueCommand(Guid AgentId, string CommandType, string TargetName, string ArgumentsJson, DataObjects.User? CurrentUser)
    {
        AgentMonitoring.AgentCommand cmd = new AgentMonitoring.AgentCommand {
            CommandId = Guid.NewGuid(),
            AgentId = AgentId,
            TenantId = _managedAgents.TryGetValue(AgentId, out var a) ? a.TenantId : Guid.Empty,
            CommandType = CommandType,
            TargetName = TargetName,
            ArgumentsJson = ArgumentsJson,
            Status = AgentMonitoring.AgentCommandStatus.Queued.ToString(),
            QueuedAt = DateTime.UtcNow,
            SentAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
            IssuedBy = CurrentUserIdString(CurrentUser),
        };
        _commands[cmd.CommandId] = cmd;
        return cmd;
    }

    // -------------------------------------------------------------------------
    // Dashboard summary
    // -------------------------------------------------------------------------

    public Task<AgentMonitoring.AgentDashboardSummary> GetDashboardSummary(Guid TenantId, DataObjects.User? CurrentUser = null)
    {
        EnsureSeeded(TenantId);

        AgentMonitoring.AgentDashboardSummary output = new AgentMonitoring.AgentDashboardSummary();

        var agents = _managedAgents.Values.Where(x => x.TenantId == TenantId && !x.Deleted).ToList();
        output.TotalAgents = agents.Count;
        output.OnlineCount = agents.Count(x => x.Status == AgentMonitoring.AgentStatuses.Online);
        output.WarningCount = agents.Count(x => x.Status == AgentMonitoring.AgentStatuses.Warning);
        output.ErrorCount = agents.Count(x => x.Status == AgentMonitoring.AgentStatuses.Error);
        output.OfflineCount = agents.Count(x => x.Status == AgentMonitoring.AgentStatuses.Offline);
        output.StaleCount = agents.Count(x => x.Status == AgentMonitoring.AgentStatuses.Stale);

        var tenantCommands = _commands.Values.Where(x => x.TenantId == TenantId).ToList();
        output.CommandsQueued = tenantCommands.Count(x => x.Status == AgentMonitoring.AgentCommandStatus.Queued.ToString());
        output.CommandsRunning = tenantCommands.Count(x => x.Status == AgentMonitoring.AgentCommandStatus.Running.ToString() || x.Status == AgentMonitoring.AgentCommandStatus.Sent.ToString());
        output.CommandsCompletedToday = tenantCommands.Count(x => x.Status == AgentMonitoring.AgentCommandStatus.Completed.ToString() && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == DateTime.UtcNow.Date);

        return Task.FromResult(output);
    }

    // -------------------------------------------------------------------------
    // AgentHub-facing API
    //
    // Methods called from AgentHub when an agent worker pushes data up the
    // wire. Kept on this service (not on the hub) so business logic stays
    // testable without spinning up a SignalR pipeline.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates a registration key and consumes it -- on success returns the
    /// AgentId associated with the freshly-created (or matched) managed agent.
    /// Returns <see cref="Guid.Empty"/> when the key is invalid, expired,
    /// revoked, or already consumed.
    /// </summary>
    public Task<Guid> ConsumeRegistrationKey(string registrationKey, AgentRegistrationInfo info)
    {
        if (string.IsNullOrWhiteSpace(registrationKey)) {
            return Task.FromResult(Guid.Empty);
        }

        var match = _registrationKeys.Values.FirstOrDefault(k =>
            string.Equals(k.Key, registrationKey, StringComparison.OrdinalIgnoreCase));

        if (match == null) {
            return Task.FromResult(Guid.Empty);
        }
        if (match.Revoked) {
            return Task.FromResult(Guid.Empty);
        }
        if (match.ExpiresAt.HasValue && match.ExpiresAt.Value < DateTime.UtcNow) {
            return Task.FromResult(Guid.Empty);
        }

        // If the key has already been consumed, just hand back the same AgentId
        // so reconnects keep working without re-issuing a fresh key.
        if (match.ConsumedByAgentId.HasValue && match.ConsumedByAgentId.Value != Guid.Empty) {
            if (_managedAgents.TryGetValue(match.ConsumedByAgentId.Value, out var existing)) {
                existing.Status = AgentMonitoring.AgentStatuses.Online;
                existing.LastHeartbeat = DateTime.UtcNow;
                existing.AgentVersion = info.AgentVersion;
                existing.OperatingSystem = info.OsVersion;
                existing.Hostname = info.HostName;
                existing.LastModified = DateTime.UtcNow;
                return Task.FromResult(existing.AgentId);
            }
        }

        // First-time consumption -- create a managed agent row.
        DateTime now = DateTime.UtcNow;
        AgentMonitoring.Agent agent = new AgentMonitoring.Agent {
            AgentId = Guid.NewGuid(),
            TenantId = match.TenantId,
            Name = !string.IsNullOrWhiteSpace(info.HostName) ? info.HostName : "Agent-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            Hostname = info.HostName,
            OperatingSystem = info.OsVersion,
            AgentVersion = info.AgentVersion,
            Status = AgentMonitoring.AgentStatuses.Online,
            RegisteredAt = now,
            RegisteredBy = "AgentHub",
            LastHeartbeat = now,
            Added = now,
            AddedBy = "AgentHub",
            LastModified = now,
            LastModifiedBy = "AgentHub",
            ManagementEnabled = true,
        };
        _managedAgents[agent.AgentId] = agent;

        match.ConsumedAt = now;
        match.ConsumedByAgentId = agent.AgentId;

        return Task.FromResult(agent.AgentId);
    }

    /// <summary>Marks the given managed agent as Online (called on connect / resume).</summary>
    public void MarkAgentOnline(Guid agentId)
    {
        if (_managedAgents.TryGetValue(agentId, out var agent)) {
            agent.Status = AgentMonitoring.AgentStatuses.Online;
            agent.LastHeartbeat = DateTime.UtcNow;
            agent.LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>Marks the given managed agent as Offline (called on disconnect).</summary>
    public void MarkAgentOffline(Guid agentId)
    {
        if (_managedAgents.TryGetValue(agentId, out var agent)) {
            agent.Status = AgentMonitoring.AgentStatuses.Offline;
            agent.LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Stores a heartbeat that arrived from the worker over SignalR. Mirrors
    /// the older <see cref="RecordHeartbeat"/> shape but takes the strongly-typed
    /// <see cref="AgentMonitoring.AgentHeartbeat"/> the worker ships.
    /// </summary>
    public Task RecordAgentHeartbeat(Guid agentId, AgentMonitoring.AgentHeartbeat heartbeat)
    {
        if (heartbeat == null) {
            return Task.CompletedTask;
        }
        heartbeat.AgentId = agentId;
        heartbeat.HeartbeatId = heartbeat.HeartbeatId == Guid.Empty ? Guid.NewGuid() : heartbeat.HeartbeatId;
        heartbeat.Timestamp = heartbeat.Timestamp == default ? DateTime.UtcNow : heartbeat.Timestamp;

        if (_managedAgents.TryGetValue(agentId, out var agent)) {
            agent.LastHeartbeat = heartbeat.Timestamp;
            agent.Status = heartbeat.Healthy ? AgentMonitoring.AgentStatuses.Online : AgentMonitoring.AgentStatuses.Warning;
            agent.LastModified = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Replaces the agent's installed-Windows-Service inventory with whatever
    /// the worker just reported.
    /// </summary>
    public void UpdateServiceInventory(Guid agentId, List<AgentMonitoring.AgentService> services)
    {
        DateTime now = DateTime.UtcNow;
        foreach (var s in services) {
            s.AgentId = agentId;
            s.LastSeenUtc = now;
            if (s.AgentServiceId == Guid.Empty) {
                s.AgentServiceId = Guid.NewGuid();
            }
        }
        _agentServices[agentId] = services;
    }

    /// <summary>
    /// Replaces the agent's IIS app-pool / site inventory with whatever the
    /// worker just reported.
    /// </summary>
    public void UpdateIisInventory(Guid agentId, List<AgentMonitoring.AgentIisAppPool> pools, List<AgentMonitoring.AgentIisSite> sites)
    {
        DateTime now = DateTime.UtcNow;
        foreach (var p in pools) {
            p.AgentId = agentId;
            p.LastSeenUtc = now;
            if (p.AppPoolId == Guid.Empty) {
                p.AppPoolId = Guid.NewGuid();
            }
        }
        foreach (var s in sites) {
            s.AgentId = agentId;
            s.LastSeenUtc = now;
            if (s.IisSiteId == Guid.Empty) {
                s.IisSiteId = Guid.NewGuid();
            }
        }
        _agentAppPools[agentId] = pools;
        _agentIisSites[agentId] = sites;
    }

    /// <summary>
    /// Updates the matching <see cref="AgentMonitoring.AgentCommand"/> row
    /// when a worker reports the outcome of an executed command.
    /// </summary>
    public void RecordCommandResult(Guid commandId, bool succeeded, string? errorMessage)
    {
        if (!_commands.TryGetValue(commandId, out var cmd)) {
            return;
        }
        cmd.CompletedAt = DateTime.UtcNow;
        cmd.Status = succeeded
            ? AgentMonitoring.AgentCommandStatus.Completed.ToString()
            : AgentMonitoring.AgentCommandStatus.Failed.ToString();
        cmd.ResponseMessage = errorMessage ?? (succeeded ? "Command completed." : "Command failed.");
        cmd.ActionResponse.Result = succeeded;
        if (!succeeded && !string.IsNullOrEmpty(errorMessage)) {
            cmd.ActionResponse.Messages.Add(errorMessage);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string CurrentUserIdString(DataObjects.User? CurrentUser)
    {
        if (CurrentUser == null || CurrentUser.UserId == Guid.Empty) {
            return "System";
        }
        return CurrentUser.UserId.ToString();
    }

    private void EnsureSeeded(Guid TenantId)
    {
        if (_seeded) {
            return;
        }

        lock (_seedLock) {
            if (_seeded) {
                return;
            }
            SeedSampleAgents(TenantId);
            _seeded = true;
        }
    }

    private void SeedSampleAgents(Guid TenantId)
    {
        var samples = new[] {
            new {
                Name = "Build Agent 01",
                Hostname = "WS-BUILD-01",
                Os = "Windows Server 2022 Standard (10.0.20348)",
                Status = AgentMonitoring.AgentStatuses.Online,
                Services = new[] { ("FreeBlazorAgent", "FreeBlazor Build Agent", AgentMonitoring.ServiceState.Running),
                                   ("W3SVC", "World Wide Web Publishing Service", AgentMonitoring.ServiceState.Running),
                                   ("MSSQLSERVER", "SQL Server (MSSQLSERVER)", AgentMonitoring.ServiceState.Running) },
                Pools = new[] { ("DefaultAppPool", AgentMonitoring.AppPoolState.Started, "v4.0"),
                                ("FreeBlazor.Web", AgentMonitoring.AppPoolState.Started, "No Managed Code") },
            },
            new {
                Name = "Build Agent 02",
                Hostname = "WS-BUILD-02",
                Os = "Windows Server 2022 Datacenter (10.0.20348)",
                Status = AgentMonitoring.AgentStatuses.Warning,
                Services = new[] { ("FreeBlazorAgent", "FreeBlazor Build Agent", AgentMonitoring.ServiceState.Running),
                                   ("W3SVC", "World Wide Web Publishing Service", AgentMonitoring.ServiceState.Running),
                                   ("Spooler", "Print Spooler", AgentMonitoring.ServiceState.Stopped) },
                Pools = new[] { ("DefaultAppPool", AgentMonitoring.AppPoolState.Started, "v4.0"),
                                ("LegacyApi.AppPool", AgentMonitoring.AppPoolState.Stopped, "v4.0") },
            },
            new {
                Name = "Web Front-End",
                Hostname = "WS-WEB-PROD-01",
                Os = "Windows Server 2019 Standard (10.0.17763)",
                Status = AgentMonitoring.AgentStatuses.Online,
                Services = new[] { ("FreeBlazorAgent", "FreeBlazor Build Agent", AgentMonitoring.ServiceState.Running),
                                   ("W3SVC", "World Wide Web Publishing Service", AgentMonitoring.ServiceState.Running),
                                   ("WAS", "Windows Process Activation Service", AgentMonitoring.ServiceState.Running) },
                Pools = new[] { ("DefaultAppPool", AgentMonitoring.AppPoolState.Started, "v4.0"),
                                ("FreeBlazor.Web", AgentMonitoring.AppPoolState.Started, "No Managed Code"),
                                ("Marketing.Site", AgentMonitoring.AppPoolState.Started, "No Managed Code") },
            },
            new {
                Name = "Database Server",
                Hostname = "WS-DB-PROD-01",
                Os = "Windows Server 2022 Standard (10.0.20348)",
                Status = AgentMonitoring.AgentStatuses.Online,
                Services = new[] { ("FreeBlazorAgent", "FreeBlazor Build Agent", AgentMonitoring.ServiceState.Running),
                                   ("MSSQLSERVER", "SQL Server (MSSQLSERVER)", AgentMonitoring.ServiceState.Running),
                                   ("SQLSERVERAGENT", "SQL Server Agent (MSSQLSERVER)", AgentMonitoring.ServiceState.Running),
                                   ("MSSQLServerOLAPService", "SQL Server Analysis Services (MSSQLSERVER)", AgentMonitoring.ServiceState.Stopped) },
                Pools = Array.Empty<(string, AgentMonitoring.AppPoolState, string)>(),
            },
            new {
                Name = "Reporting Worker",
                Hostname = "WS-WORKER-03",
                Os = "Windows Server 2019 Standard (10.0.17763)",
                Status = AgentMonitoring.AgentStatuses.Offline,
                Services = new[] { ("FreeBlazorAgent", "FreeBlazor Build Agent", AgentMonitoring.ServiceState.Stopped) },
                Pools = Array.Empty<(string, AgentMonitoring.AppPoolState, string)>(),
            },
        };

        int idx = 0;
        DateTime now = DateTime.UtcNow;
        foreach (var sample in samples) {
            var agent = new AgentMonitoring.Agent {
                AgentId = Guid.NewGuid(),
                TenantId = TenantId,
                Name = sample.Name,
                Hostname = sample.Hostname,
                Description = "Sample agent for showcase purposes.",
                OperatingSystem = sample.Os,
                Architecture = "x64",
                AgentVersion = "1.0.0",
                DotNetVersion = ".NET 10.0",
                IpAddressInternal = $"10.0.1.{10 + idx}",
                Status = sample.Status,
                LastHeartbeat = sample.Status == AgentMonitoring.AgentStatuses.Offline ? now.AddHours(-3) : now.AddSeconds(-12 - idx),
                RegisteredAt = now.AddDays(-30 - idx),
                RegisteredBy = "System",
                Uptime = TimeSpan.FromHours(72 + (idx * 24)),
                Added = now.AddDays(-30 - idx),
                AddedBy = "System",
                LastModified = now,
                LastModifiedBy = "System",
                ManagementEnabled = true,
            };
            _managedAgents[agent.AgentId] = agent;

            // Services
            var svcList = new List<AgentMonitoring.AgentService>();
            foreach (var (sn, dn, state) in sample.Services) {
                svcList.Add(new AgentMonitoring.AgentService {
                    AgentServiceId = Guid.NewGuid(),
                    AgentId = agent.AgentId,
                    ServiceName = sn,
                    DisplayName = dn,
                    Description = "Sample service entry for showcase.",
                    State = state.ToString(),
                    StartupType = AgentMonitoring.ServiceStartupType.Automatic.ToString(),
                    LogOnAccount = "NT AUTHORITY\\NetworkService",
                    ProcessId = state == AgentMonitoring.ServiceState.Running ? 1000 + idx * 100 : 0,
                    BinaryPath = $"\"C:\\Program Files\\{sn}\\{sn}.exe\"",
                    LastSeenUtc = now,
                });
            }
            _agentServices[agent.AgentId] = svcList;

            // App pools
            var poolList = new List<AgentMonitoring.AgentIisAppPool>();
            var siteList = new List<AgentMonitoring.AgentIisSite>();
            int siteIdx = 0;
            foreach (var (poolName, state, runtime) in sample.Pools) {
                Guid poolId = Guid.NewGuid();
                poolList.Add(new AgentMonitoring.AgentIisAppPool {
                    AppPoolId = poolId,
                    AgentId = agent.AgentId,
                    Name = poolName,
                    State = state.ToString(),
                    ManagedRuntimeVersion = runtime,
                    ManagedPipelineMode = "Integrated",
                    IdentityType = "ApplicationPoolIdentity",
                    IdentityUsername = $"IIS APPPOOL\\{poolName}",
                    AutoStart = true,
                    Enable32BitAppOnWin64 = false,
                    IdleTimeoutMinutes = 20,
                    RecyclingTimeMinutes = 1740,
                    LastSeenUtc = now,
                });

                siteList.Add(new AgentMonitoring.AgentIisSite {
                    IisSiteId = Guid.NewGuid(),
                    AgentId = agent.AgentId,
                    Name = poolName == "DefaultAppPool" ? "Default Web Site" : poolName,
                    State = state == AgentMonitoring.AppPoolState.Started ? "Started" : "Stopped",
                    PhysicalPath = $"C:\\inetpub\\wwwroot\\{poolName}",
                    AppPoolName = poolName,
                    Bindings = new List<AgentMonitoring.AgentIisBinding> {
                        new AgentMonitoring.AgentIisBinding {
                            Protocol = "https",
                            HostName = $"{poolName.ToLowerInvariant().Replace(".", "-")}.{sample.Hostname.ToLowerInvariant()}.local",
                            Port = 443,
                            IpAddress = "*",
                            RequireSsl = true,
                        },
                    },
                    LastSeenUtc = now,
                });
                siteIdx++;
            }
            _agentAppPools[agent.AgentId] = poolList;
            _agentIisSites[agent.AgentId] = siteList;

            idx++;
        }
    }
}

// =============================================================================
// Lightweight DataObjects.User shim
//
// Lets the management API match the FreeCRM/FreeServicesHub method signature
// pattern (..., DataObjects.User? CurrentUser = null) without forcing a hard
// dependency on the full FreeBlazorExample CRM.DataObjects assembly. When this
// service is consumed inside an app that already has a richer DataObjects
// namespace, this type is just used for the interface contract.
// =============================================================================
public static class DataObjects
{
    public class User
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Admin { get; set; }
    }
}

public class Agent
{
    public Guid AgentId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AgentStatus Status { get; set; } = AgentStatus.Online;
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;
    public string? TagsJson { get; set; } // JSON array of tags like ["env:prod", "role:worker"]

    // Audit
    public DateTime Added { get; set; } = DateTime.UtcNow;
    public string? AddedBy { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }
    public bool Deleted { get; set; }
}

// AgentStatus and AgentHeartbeat moved to FreeBlazorExtended.Models

public class AlertRule
{
    public Guid AlertRuleId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty; // cpu, memory, disk, custom
    public string Condition { get; set; } = "gt"; // gt, lt, gte, lte, eq
    public double Threshold { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime Added { get; set; } = DateTime.UtcNow;
    public string? AddedBy { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public string? LastModifiedBy { get; set; }
    public bool Deleted { get; set; }
}
