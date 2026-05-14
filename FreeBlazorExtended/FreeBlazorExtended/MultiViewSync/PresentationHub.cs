/*
    Purpose: SignalR hub for joining sessions and broadcasting MultiViewSync presentation updates.
    Host expectations: Requires SignalR endpoint mapping and a RealtimeSyncService registration in the consuming app.
*/
using Microsoft.AspNetCore.SignalR;

namespace FreeBlazorExtended.MultiViewSync;

/// <summary>
/// SignalR hub that drives Feature 102 MultiViewSync. One SignalR group per
/// <see cref="PresentationSession"/> (group name = SessionId.ToString()). Masters
/// (the presenter) push state changes; Slaves (audience views) receive them.
///
/// Server-side wiring (host Program.cs):
///   builder.Services.AddScoped&lt;RealtimeSyncService&gt;();
///   app.MapHub&lt;PresentationHub&gt;("/presentationHub");
///
/// Client-side wiring (consumer's responsibility — see README for an example):
///   var hub = new HubConnectionBuilder().WithUrl(nav.ToAbsoluteUri("/presentationHub")).Build();
///   hub.On&lt;Guid&gt;("ActiveItemChanged", id =&gt; ...);
///   await hub.StartAsync();
///   await hub.InvokeAsync("JoinSession", sessionId, "Slave");
/// </summary>
public class PresentationHub : Hub
{
    public const string MasterClient = "Master";
    public const string SlaveClient = "Slave";

    private readonly RealtimeSyncService _sync;

    // Tracks the (sessionId, clientType) bound to a connection so we can
    // tear down cleanly on disconnect, and so SetActiveItem can verify the
    // caller really is a Master.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ConnectionContext> _connectionContexts = new();

    public PresentationHub(RealtimeSyncService sync)
    {
        _sync = sync;
    }

    public override async Task OnConnectedAsync()
    {
        // The caller must follow up with JoinSession to actually be useful;
        // until then the connection is unscoped. Nothing to register yet.
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionContexts.TryRemove(Context.ConnectionId, out var ctx)) {
            await _sync.UnregisterClient(ctx.SessionId.ToString(), Context.ConnectionId);
            try {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ctx.SessionId.ToString());
            } catch { }

            // Notify remaining clients that the audience headcount changed.
            await Clients.Group(ctx.SessionId.ToString()).SendAsync("ClientCountChanged", _connectionContexts.Values.Count(c => c.SessionId == ctx.SessionId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a presentation session group. <paramref name="clientType"/> should be
    /// <c>"Master"</c> (the presenter) or <c>"Slave"</c> (an audience view).
    /// </summary>
    public async Task JoinSession(Guid sessionId, string clientType)
    {
        if (sessionId == Guid.Empty)
            throw new HubException("sessionId is required.");

        if (string.IsNullOrWhiteSpace(clientType))
            clientType = SlaveClient;

        if (!string.Equals(clientType, MasterClient, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(clientType, SlaveClient, StringComparison.OrdinalIgnoreCase)) {
            throw new HubException($"clientType must be '{MasterClient}' or '{SlaveClient}'.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        await _sync.RegisterClient(sessionId.ToString(), Context.ConnectionId);

        _connectionContexts[Context.ConnectionId] = new ConnectionContext(sessionId, clientType);

        // Push the current session state to the joiner so it can render immediately.
        var session = await _sync.GetPresentationSession(sessionId);
        if (session != null)
            await Clients.Caller.SendAsync("SessionUpdated", session);

        // Let everyone in the group know the headcount changed.
        var count = _connectionContexts.Values.Count(c => c.SessionId == sessionId);
        await Clients.Group(sessionId.ToString()).SendAsync("ClientCountChanged", count);
    }

    /// <summary>
    /// Leave a presentation session group. Call before disconnecting if you
    /// want a clean teardown without waiting for OnDisconnected.
    /// </summary>
    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());
        await _sync.UnregisterClient(sessionId.ToString(), Context.ConnectionId);

        _connectionContexts.TryRemove(Context.ConnectionId, out _);

        var count = _connectionContexts.Values.Count(c => c.SessionId == sessionId);
        await Clients.Group(sessionId.ToString()).SendAsync("ClientCountChanged", count);
    }

    /// <summary>
    /// Master-only. Mutates the session's active item and broadcasts
    /// <c>ActiveItemChanged</c> to every client in the session group.
    /// </summary>
    public async Task SetActiveItem(Guid sessionId, Guid itemId)
    {
        if (!_connectionContexts.TryGetValue(Context.ConnectionId, out var ctx) ||
            ctx.SessionId != sessionId) {
            throw new HubException("Caller has not joined this session. Call JoinSession first.");
        }

        if (!string.Equals(ctx.ClientType, MasterClient, StringComparison.OrdinalIgnoreCase)) {
            throw new HubException("Only Master clients can change the active item.");
        }

        await _sync.SetActiveItem(sessionId, itemId);
        await Clients.Group(sessionId.ToString()).SendAsync("ActiveItemChanged", itemId);
    }

    /// <summary>
    /// Re-pushes the current session state to every client in the group.
    /// Useful after a Master mutates state out-of-band (e.g. SetBlankScreen,
    /// AddSessionItem) and wants slaves to refresh.
    /// </summary>
    public async Task BroadcastSessionUpdate(Guid sessionId)
    {
        var session = await _sync.GetPresentationSession(sessionId);
        if (session == null)
            throw new HubException("Session not found.");

        await Clients.Group(sessionId.ToString()).SendAsync("SessionUpdated", session);
    }

    private sealed record ConnectionContext(Guid SessionId, string ClientType);
}
