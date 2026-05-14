// FreeBlazorExtended -- AgentMonitoring/AgentHub.cs
//
// SignalR hub that bridges the FreeBlazorExtended.Agent Worker Service
// (running on a remote Windows host) and Feature 105's AgentMonitoringService.
//
// The hub:
//   - Validates a one-time registration key on first connect, returns an AgentId.
//   - Accepts heartbeat / inventory pushes (services, IIS pools/sites).
//   - Receives ReportCommandResult callbacks after the agent executes a command.
//   - Maintains a connection-id -> agent-id map so AgentMonitoringService can
//     route hub-issued commands to the correct connection (and so disconnect
//     cleanup can mark the agent offline).
//
// Server-side wiring (host Program.cs):
//   builder.Services.AddScoped<AgentMonitoringService>();
//   app.MapHub<FreeBlazorExtended.AgentMonitoring.AgentHub>("/agentHub");
//
// Worker-side wiring (FreeBlazorExtended.Agent/AgentWorkerService.cs):
//   var conn = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
//   var agentId = await conn.InvokeAsync<Guid>("RegisterAgent", regKey, info);
//   conn.On<Guid, string, string>("ExecuteServiceCommand", (cmdId, type, target) => ...);
//   conn.On<Guid, string, string>("ExecuteAppPoolCommand", (cmdId, type, target) => ...);

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FreeBlazorExtended.AgentMonitoring;

/// <summary>
/// Information sent by an agent at registration time. Lets the hub decide
/// whether to accept the connection and gives the dashboard a host snapshot
/// for display before the first heartbeat arrives.
/// </summary>
public sealed class AgentRegistrationInfo
{
    public string HostName { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new List<string>();
}

/// <summary>
/// SignalR hub that the FreeBlazorExtended.Agent worker connects to. All
/// methods on this class are invoked by the agent — server-to-agent commands
/// are dispatched from <see cref="AgentMonitoringService"/> via
/// <c>IHubContext&lt;AgentHub&gt;</c>.
/// </summary>
public class AgentHub : Hub
{
    // Connection -> agent map. Lets us mark agents Offline on disconnect and
    // lets the service look up which connection to send a command to.
    private static readonly ConcurrentDictionary<string, Guid> _connectionToAgent = new();
    private static readonly ConcurrentDictionary<Guid, string> _agentToConnection = new();

    private readonly AgentMonitoringService _service;
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(AgentMonitoringService service, ILogger<AgentHub> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns the SignalR connection-id for the given agent, or <c>null</c>
    /// if no agent worker is currently connected. Used by
    /// <see cref="AgentMonitoringService"/> to decide whether to push a command
    /// over the wire or fall back to the in-memory simulation.
    /// </summary>
    public static string? GetConnectionForAgent(Guid agentId)
    {
        return _agentToConnection.TryGetValue(agentId, out var conn) ? conn : null;
    }

    /// <summary>
    /// First call from a freshly-started agent. Validates the registration key
    /// against <see cref="AgentMonitoringService"/> and returns the assigned
    /// AgentId. The agent should persist the AgentId locally so subsequent
    /// reconnects can re-claim it.
    /// </summary>
    public async Task<Guid> RegisterAgent(string registrationKey, AgentRegistrationInfo info)
    {
        if (string.IsNullOrWhiteSpace(registrationKey)) {
            throw new HubException("registrationKey is required.");
        }
        if (info == null) {
            throw new HubException("AgentRegistrationInfo is required.");
        }

        Guid agentId = await _service.ConsumeRegistrationKey(registrationKey, info);
        if (agentId == Guid.Empty) {
            throw new HubException("Registration key is invalid, expired, revoked, or already consumed.");
        }

        _connectionToAgent[Context.ConnectionId] = agentId;
        _agentToConnection[agentId] = Context.ConnectionId;

        _logger.LogInformation("Agent {AgentId} ({Host}) registered on connection {Conn}",
            agentId, info.HostName, Context.ConnectionId);

        return agentId;
    }

    /// <summary>
    /// Reconnect path -- agent already has an AgentId from a prior registration.
    /// Just establishes the connection-id mapping so commands can route.
    /// </summary>
    public Task ResumeAgent(Guid agentId)
    {
        if (agentId == Guid.Empty) {
            throw new HubException("agentId is required.");
        }

        _connectionToAgent[Context.ConnectionId] = agentId;
        _agentToConnection[agentId] = Context.ConnectionId;
        _service.MarkAgentOnline(agentId);

        _logger.LogInformation("Agent {AgentId} resumed on connection {Conn}", agentId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pushes a heartbeat (CPU / memory / disk snapshot) up to the service.
    /// </summary>
    public async Task PushHeartbeat(Guid agentId, AgentMonitoring.AgentHeartbeat heartbeat)
    {
        if (heartbeat == null) {
            throw new HubException("heartbeat is required.");
        }
        await _service.RecordAgentHeartbeat(agentId, heartbeat);
    }

    /// <summary>
    /// Pushes the agent's installed-Windows-Service inventory.
    /// </summary>
    public Task PushServiceInventory(Guid agentId, List<AgentMonitoring.AgentService> services)
    {
        _service.UpdateServiceInventory(agentId, services ?? new List<AgentMonitoring.AgentService>());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pushes the agent's IIS app-pool / site inventory.
    /// </summary>
    public Task PushIisInventory(Guid agentId, List<AgentMonitoring.AgentIisAppPool> pools, List<AgentMonitoring.AgentIisSite> sites)
    {
        _service.UpdateIisInventory(agentId,
            pools ?? new List<AgentMonitoring.AgentIisAppPool>(),
            sites ?? new List<AgentMonitoring.AgentIisSite>());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Agent reports the outcome of a previously-issued command. The service
    /// updates the matching <see cref="AgentMonitoring.AgentCommand"/> row.
    /// </summary>
    public Task ReportCommandResult(Guid commandId, bool succeeded, string? errorMessage)
    {
        _service.RecordCommandResult(commandId, succeeded, errorMessage);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionToAgent.TryRemove(Context.ConnectionId, out Guid agentId)) {
            _agentToConnection.TryRemove(agentId, out _);
            _service.MarkAgentOffline(agentId);
            _logger.LogInformation("Agent {AgentId} disconnected (connection {Conn})", agentId, Context.ConnectionId);
        }
        return base.OnDisconnectedAsync(exception);
    }
}
