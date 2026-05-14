# Feature 102 — MultiViewSync

> Drive multiple "audience view" browsers from one "presenter" — active item, blank-screen toggle, hidden-text reveal — like ProPresenter for Blazor.

## What this feature does
Holds `PresentationSession` records (active item, blanked-screen flag, hidden-text flag, ordered `SessionItem` list) plus a per-session map of connected SignalR client IDs. The presenter ("Master") mutates the session via `RealtimeSyncService` and pushes changes through `PresentationHub`; audience clients ("Slaves") connect to the hub, join the session group, and re-render on every `ActiveItemChanged` / `SessionUpdated` push.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `RealtimeSyncService.cs` | Session CRUD, item navigation, blank/hide toggles, client-connection tracking | 134 |
| `PresentationSession.cs` | `PresentationSession`, `SessionItem` POCOs | 37 |
| `PresentationHub.cs` | SignalR hub: Join/Leave/SetActiveItem/BroadcastSessionUpdate, group-per-session, Master/Slave gating | 115 |

## Dependencies
- **NuGet packages:** `System.Text.Json`. To make it actually realtime: `Microsoft.AspNetCore.SignalR` (server) and `Microsoft.AspNetCore.SignalR.Client` (WASM client).
- **Cross-feature dependencies:** none; uses `Foundation/Helpers.cs` and `Foundation/DataObjects.cs`
- **SignalR:** required (currently missing — see Known gaps)
- **EF Core:** not used (in-memory `ConcurrentDictionary<Guid, PresentationSession>`)

## DI registration
Add this to your server `Program.cs`:
```csharp
builder.Services.AddScoped<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();
// ...
app.MapHub<FreeBlazorExtended.MultiViewSync.PresentationHub>("/presentationHub");
```
(For Blazor WASM client also add a Singleton variant of `RealtimeSyncService` — see `FreeBlazorExample/FreeBlazorExample.Client/Program.cs` line 31 for the pattern.)

## Client usage example
The hub provides the methods; the consumer's component owns the `HubConnection`:

```csharp
@implements IAsyncDisposable
@inject NavigationManager Nav

@code {
    private HubConnection? hub;
    private Guid sessionId;
    private Guid? activeItemId;

    protected override async Task OnInitializedAsync()
    {
        hub = new HubConnectionBuilder()
            .WithUrl(Nav.ToAbsoluteUri("/presentationHub"))
            .WithAutomaticReconnect()
            .Build();

        // Slave: re-render whenever the master changes the active item.
        hub.On<Guid>("ActiveItemChanged", id => {
            activeItemId = id;
            InvokeAsync(StateHasChanged);
        });

        // Slave: re-render on full session-state push (e.g. after BroadcastSessionUpdate).
        hub.On<PresentationSession>("SessionUpdated", session => {
            activeItemId = session.ActiveItemId;
            InvokeAsync(StateHasChanged);
        });

        hub.On<int>("ClientCountChanged", count => { /* optional UI */ });

        await hub.StartAsync();
        await hub.InvokeAsync("JoinSession", sessionId, "Slave");
    }

    // Master-side: presenter pushes a new active item.
    private Task SelectItem(Guid itemId) =>
        hub!.InvokeAsync("SetActiveItem", sessionId, itemId);

    public async ValueTask DisposeAsync()
    {
        if (hub is not null) {
            try { await hub.InvokeAsync("LeaveSession", sessionId); } catch { }
            await hub.DisposeAsync();
        }
    }
}
```

> The hub provides the four methods (`JoinSession`, `LeaveSession`, `SetActiveItem`, `BroadcastSessionUpdate`) and emits three events (`ActiveItemChanged`, `SessionUpdated`, `ClientCountChanged`). Wiring the `HubConnection`, calling those methods, and reacting to events is the consumer code's responsibility — the hub itself does not know about Razor components.

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/MultiViewSync/` folder into your project.
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present.
3. Add the DI registration and `MapHub` to server `Program.cs` (see line 32 and line 252 in the example).
4. Wire `HubConnectionBuilder` on the WASM client (see the example above).
5. EF Core migration not applicable today.

## Showcase
The interactive demo lives at `/showcase/feature102-multi-view-sync` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature102_MultiViewSync.razor`

## Status
- Implementation: **REAL** (in-memory) for session state; **REAL** for cross-process push (`PresentationHub` mapped at `/presentationHub`)
- Persistence: in-memory only — needs EF migration before production use
- Known gaps:
  - The showcase page is single-process and does not call `HubConnectionBuilder`, so the demo only proves the in-memory state machine works. Wiring the showcase to the hub is left to the consumer.
  - No authorization on `JoinSession` — anyone who can reach `/presentationHub` and knows a session ID can subscribe. Add an `[Authorize]` attribute and tenant-aware checks before exposing publicly.

## Effort to integrate
**S** — both the service and the hub now ship with the feature. A real-world deployment still needs client `HubConnection` wiring, EF persistence, and auth on the audience-join path.
