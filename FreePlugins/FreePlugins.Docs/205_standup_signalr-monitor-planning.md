# 205 — Standup: SignalR Connection Monitor Plugin Planning

> **Document ID:** 205  
> **Category:** Meeting  
> **Purpose:** Team standup to define MVP features for a SignalR connection monitoring plugin  
> **Attendees:** [Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]  
> **Date:** 2025-01-XX  
> **Predicted Outcome:** Minimal feature set defined, ready for implementation  
> **Actual Outcome:** *(Pending)*  
> **Resolution:** *(Pending)*

---

## TL;DR

**What:** Plugin that tracks SignalR hub connections and provides a monitoring dashboard.  
**Challenge:** Plugins run in BackgroundProcess context, not in the Hub lifecycle.  
**Approach:** Hook into Hub events via static tracking + expose via API endpoint.

---

## Standup Context

Following the success of ApplicationLifecycleLogger (docs 200-204), we're planning our second cross-compatible plugin. This one is more complex — it needs to interact with SignalR's connection lifecycle.

---

## Discussion

**[Architect]:** Alright, second plugin. The ask is: track SignalR connections, show active hubs, maybe log connection history. What's the landscape look like?

**[Backend]:** I checked the codebase. Here's what we have:

```
FreePluginsV1/FreePlugins/Hubs/signalrHub.cs
├── freepluginsHub : Hub<IsrHub>
├── JoinTenantId(string TenantId) — adds user to tenant group
└── SignalRUpdate(SignalRUpdate update) — broadcasts to group or all
```

The hub is `[Authorize]` so only authenticated users connect. It tracks tenants via groups.

**[JrDev]:** Wait, how do plugins interact with the Hub? The BackgroundProcessor runs on a timer, not on connection events.

**[Backend]:** Good catch. That's the core challenge. We have three options:

### Option A: Hub Event Hooks (Recommended)

Override `OnConnectedAsync()` and `OnDisconnectedAsync()` in the Hub to call into a static tracker:

```csharp
public partial class freepluginsHub : Hub<IsrHub>
{
    public override async Task OnConnectedAsync()
    {
        SignalRConnectionTracker.TrackConnection(Context);
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        SignalRConnectionTracker.TrackDisconnection(Context);
        await base.OnDisconnectedAsync(exception);
    }
}
```

**Pros:** Real-time tracking, accurate counts  
**Cons:** Requires modifying the Hub (framework change)

### Option B: IHubContext Polling

Use `IHubContext<freepluginsHub>` in BackgroundProcessor to query state:

```csharp
// In BackgroundProcessor
var hubContext = _serviceProvider.GetService<IHubContext<freepluginsHub>>();
// But... IHubContext doesn't expose connection list!
```

**Pros:** No framework changes  
**Cons:** `IHubContext` doesn't expose active connections — dead end

### Option C: Middleware Tracking

Add middleware that intercepts SignalR negotiation:

```csharp
app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/signalrhub")) {
        // Track
    }
    await next();
});
```

**Pros:** No Hub changes  
**Cons:** Only sees HTTP negotiate, not WebSocket lifecycle

**[Architect]:** Option A is the only one that actually works. But that means we need a framework change — this isn't a pure drop-in plugin.

**[Sanity]:** Mid-check. Are we okay with that? The first plugin (ApplicationLifecycleLogger) was drop-in. This one requires modifying the Hub.

**[Architect]:** Fair point. Let's split this into two parts:

1. **Framework Extension** (modify Hub) — one-time change to all suites
2. **Plugin** (BackgroundProcess) — reads from tracker, logs history, exposes API

**[Quality]:** That's a bigger scope. What's the MVP?

---

## Feature Brainstorm

| # | Feature | Complexity | Value | MVP? |
|---|---------|------------|-------|------|
| 1 | Track active connection count | Low | High | ✅ |
| 2 | Track connections by tenant | Medium | High | ✅ |
| 3 | Track connection/disconnection events | Medium | Medium | ✅ |
| 4 | Log events to file (like Lifecycle) | Low | Medium | ✅ |
| 5 | Expose API endpoint for dashboard | Medium | High | ⚠️ v1.1 |
| 6 | Blazor dashboard page | High | High | ❌ v2 |
| 7 | Track user identity per connection | Medium | Medium | ⚠️ v1.1 |
| 8 | Connection duration tracking | Medium | Low | ❌ v2 |
| 9 | Real-time connection alerts | High | Low | ❌ v2 |
| 10 | Historical analytics | High | Medium | ❌ v2 |

**[Backend]:** For MVP, I'd say: track connections, log to file, basic count. Same pattern as ApplicationLifecycleLogger.

**[Frontend]:** The API endpoint is important. Without it, there's no way to see the data except reading the log file.

**[Architect]:** Okay, let's do a v1.0 and v1.1:

---

## Final Scope

```
┌─────────────────────────────────────────────────────────────┐
│        SignalRConnectionMonitor v1.0 — MVP SCOPE            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✅ IN SCOPE (v1.0)                                         │
│  ─────────────────                                          │
│  • Static SignalRConnectionTracker class                    │
│  • Track active connection count                            │
│  • Track connections by TenantId                            │
│  • Log CONNECT/DISCONNECT events to SignalRConnections.log  │
│  • ON/OFF toggle via Enabled property                       │
│  • CSV format matching ApplicationLifecycleLogger           │
│                                                             │
│  ⚠️ FRAMEWORK CHANGE REQUIRED                               │
│  ───────────────────────────                                │
│  • Add OnConnectedAsync override to Hub                     │
│  • Add OnDisconnectedAsync override to Hub                  │
│  • One-time change to each suite's signalrHub.cs            │
│                                                             │
│  📋 v1.1 (After MVP)                                        │
│  ──────────────────                                         │
│  • API endpoint: GET /api/SignalR/Connections               │
│  • Include user identity in tracking                        │
│  • Connection duration                                      │
│                                                             │
│  ❌ OUT OF SCOPE (v2+)                                      │
│  ──────────────────────                                     │
│  • Blazor dashboard page                                    │
│  • Real-time alerts                                         │
│  • Historical analytics                                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component 1: SignalRConnectionTracker (Static Class)

```csharp
// New file: FreePlugins/Classes/SignalRConnectionTracker.cs
public static class SignalRConnectionTracker
{
    private static readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private static readonly List<ConnectionEvent> _eventLog = new();
    private static readonly object _logLock = new();
    
    public static int ActiveConnections => _connections.Count;
    public static IReadOnlyDictionary<string, ConnectionInfo> Connections => _connections;
    
    public static void OnConnected(string connectionId, string? tenantId, string? userId)
    {
        _connections[connectionId] = new ConnectionInfo {
            ConnectionId = connectionId,
            TenantId = tenantId,
            UserId = userId,
            ConnectedAt = DateTime.UtcNow
        };
        
        LogEvent("CONNECT", connectionId, tenantId, userId);
    }
    
    public static void OnDisconnected(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info)) {
            LogEvent("DISCONNECT", connectionId, info.TenantId, info.UserId);
        }
    }
    
    // ...
}
```

### Component 2: Hub Modification (Framework Change)

```csharp
// Modify: FreePlugins/Hubs/signalrHub.cs
public partial class freepluginsHub : Hub<IsrHub>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        SignalRConnectionTracker.OnConnected(Context.ConnectionId, null, userId);
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        SignalRConnectionTracker.OnDisconnected(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
    
    // Update JoinTenantId to track tenant
    public async Task JoinTenantId(string TenantId)
    {
        SignalRConnectionTracker.UpdateTenant(Context.ConnectionId, TenantId);
        // ... existing code ...
    }
}
```

### Component 3: Plugin (BackgroundProcess)

```csharp
// New file: FreePlugins/Plugins/SignalRConnectionMonitor.cs
public class SignalRConnectionMonitor : IPluginBackgroundProcess
{
    public Dictionary<string, object> Properties() => new() {
        { "Id", new Guid("...") },
        { "Name", "SignalR Connection Monitor" },
        { "Type", "BackgroundProcess" },
        { "Enabled", true },
    };
    
    public async Task<...> Execute(DataAccess da, Plugin plugin, long iteration)
    {
        // On each iteration, flush pending events to log file
        var events = SignalRConnectionTracker.GetAndClearPendingEvents();
        foreach (var evt in events) {
            LogEvent(evt);
        }
        
        // Optionally log summary every N iterations
        if (iteration % 60 == 0) { // Every ~60 minutes
            LogSummary();
        }
        
        return (true, null, null);
    }
}
```

---

## Files to Create/Modify

| File | Type | Location |
|------|------|----------|
| `SignalRConnectionTracker.cs` | NEW | `FreePlugins/Classes/` |
| `SignalRConnectionMonitor.cs` | NEW | `FreePlugins/Plugins/` |
| `signalrHub.cs` | MODIFY | `FreePlugins/Hubs/` |

---

## Portability Considerations

Unlike ApplicationLifecycleLogger, this plugin requires:

1. **Framework changes** to each suite's Hub
2. **New static class** that must be copied with the plugin

**Portability Package:**
```
SignalR-Monitor-Plugin/
├── SignalRConnectionTracker.cs    → Copy to {Suite}/Classes/
├── SignalRConnectionMonitor.cs    → Copy to {Suite}/Plugins/
└── README.md                      → Hub modification instructions
```

---

## Open Questions

1. **Should the tracker persist across app restarts?** (Currently: No, in-memory only)
2. **How many events to keep in memory before forcing flush?** (Proposed: 1000)
3. **Should we integrate with existing SignalRUpdate broadcasting?** (Proposed: No, separate concern)

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Create SignalRConnectionTracker.cs | [Backend] | P1 |
| Create SignalRConnectionMonitor.cs plugin | [Backend] | P1 |
| Modify signalrHub.cs | [Backend] | P1 |
| Test in FreePluginsV1 | [Quality] | P1 |
| Document Hub modification for portability | [Quality] | P2 |

---

**[Architect]:** Okay, scope locked for MVP. This is more complex than ApplicationLifecycleLogger because of the framework change. Backend, start with the tracker class, then the plugin, then the hub mods.

**[Backend]:** Got it.

**[Sanity]:** Final check — are we documenting that this ISN'T a pure drop-in plugin?

**[Quality]:** Yes. The portability section is clear. This requires Hub modifications. We'll create a "Portability Package" with instructions.

---

*Created: 2025-01-XX*  
*Maintained by: [Quality]*
