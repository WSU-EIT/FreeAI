using Microsoft.AspNetCore.SignalR;

namespace FreeServicesHub.Server.Hubs;

// FreeServicesHub agent protocol on the stock hub. signalrHub.cs stays stock FreeCRM so a framework upgrade can
// overwrite it; [Authorize] and the Hub<IsrHub> base come from that stock part. No constructor here: services are
// resolved per call (hub method parameter injection), so a constructor FreeCRM adds later cannot clash with ours.
// The agent (FreeServicesHub.Agent/AgentWorkerService.cs) calls JoinGroup, ReportAgentSettings and SendHeartbeat.

public partial class freeserviceshubHub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        // Auto-join agents to their individual group so the hub can target them
        var agentIdClaim = Context.User?.FindFirst("AgentId")?.Value;
        if (Guid.TryParse(agentIdClaim, out var agentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Agent_" + agentId.ToString());
            await Groups.AddToGroupAsync(Context.ConnectionId, "Agents");
        }
    }

    /// <summary>
    /// Agent → Hub: agent reports its current service settings.
    /// Broadcast to the agent's tenant group so dashboard viewers see it.
    /// </summary>
    public async Task ReportAgentSettings(DataObjects.AgentServiceInfo serviceInfo)
    {
        var agentIdClaim = Context.User?.FindFirst("AgentId")?.Value;
        var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(agentIdClaim, out var agentId))
        {
            serviceInfo.AgentId = agentId;
        }

        // Send to the tenant's group (clients join via JoinTenantId)
        string targetGroup = tenantIdClaim ?? "";

        await Clients.Group(targetGroup).SignalRUpdate(new DataObjects.SignalRUpdate
        {
            TenantId = Guid.TryParse(tenantIdClaim, out var tid) ? tid : null,
            UpdateType = DataObjects.SignalRUpdateType.AgentSettingsReport,
            Message = "SettingsReport",
            ItemId = serviceInfo.AgentId,
            Object = serviceInfo,
        });
    }

    /// <summary>
    /// Hub → Agent: request current service settings from a specific agent.
    /// The agent listens for "RequestSettings" and responds with ReportAgentSettings.
    /// </summary>
    public async Task RequestAgentSettings(Guid agentId)
    {
        await Clients.Group("Agent_" + agentId.ToString()).SignalRUpdate(new DataObjects.SignalRUpdate
        {
            UpdateType = DataObjects.SignalRUpdateType.AgentSettingsReport,
            Message = "RequestSettings",
            ItemId = agentId,
        });
    }

    /// <summary>
    /// Agent → Hub: saves a heartbeat. Clients send one argument (the heartbeat); SignalR supplies the
    /// IDataAccess parameter from the services for this call.
    /// </summary>
    public async Task SendHeartbeat(DataObjects.AgentHeartbeat heartbeat, IDataAccess da)
    {
        // Extract AgentId from the authenticated connection's claims
        var agentIdClaim = Context.User?.FindFirst("AgentId")?.Value;
        if (Guid.TryParse(agentIdClaim, out var agentId))
        {
            heartbeat.AgentId = agentId;
        }

        await da.SaveHeartbeat(heartbeat);
    }

    /// <summary>
    /// Hub → Agent: push updated settings to a specific agent.
    /// The agent listens for "UpdateSettings" and applies the changes.
    /// </summary>
    public async Task UpdateAgentSettings(DataObjects.AgentSettingsUpdate settings)
    {
        await Clients.Group("Agent_" + settings.AgentId.ToString()).SignalRUpdate(new DataObjects.SignalRUpdate
        {
            UpdateType = DataObjects.SignalRUpdateType.AgentSettingsUpdated,
            Message = "UpdateSettings",
            ItemId = settings.AgentId,
            Object = settings,
        });
    }
}
