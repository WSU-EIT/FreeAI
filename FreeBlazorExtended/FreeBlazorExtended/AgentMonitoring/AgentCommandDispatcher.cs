/*
    Role: AgentMonitoring transport bridge.
    Purpose: Defines the command-dispatch abstraction and the SignalR-backed implementation used by the monitoring service.
    Used by: AgentMonitoringService when a host wires real-time command delivery instead of falling back to local simulation.
*/
using Microsoft.AspNetCore.SignalR;

namespace FreeBlazorExtended.AgentMonitoring;

public interface IAgentCommandDispatcher
{
    Task<bool> TryDispatchServiceCommand(Guid agentId, Guid commandId, string commandType, string serviceName);
    Task<bool> TryDispatchAppPoolCommand(Guid agentId, Guid commandId, string commandType, string appPoolName);
}

public sealed class SignalRAgentCommandDispatcher : IAgentCommandDispatcher
{
    private readonly IHubContext<AgentHub> _agentHubContext;

    public SignalRAgentCommandDispatcher(IHubContext<AgentHub> agentHubContext)
    {
        _agentHubContext = agentHubContext;
    }

    public async Task<bool> TryDispatchServiceCommand(Guid agentId, Guid commandId, string commandType, string serviceName)
    {
        string? connectionId = AgentHub.GetConnectionForAgent(agentId);
        if (connectionId == null) {
            return false;
        }

        await _agentHubContext.Clients.Client(connectionId).SendAsync(
            "ExecuteServiceCommand", commandId, commandType, serviceName);

        return true;
    }

    public async Task<bool> TryDispatchAppPoolCommand(Guid agentId, Guid commandId, string commandType, string appPoolName)
    {
        string? connectionId = AgentHub.GetConnectionForAgent(agentId);
        if (connectionId == null) {
            return false;
        }

        await _agentHubContext.Clients.Client(connectionId).SendAsync(
            "ExecuteAppPoolCommand", commandId, commandType, appPoolName);

        return true;
    }
}