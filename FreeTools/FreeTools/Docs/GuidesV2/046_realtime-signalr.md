# 046 — Pushing State Live Over SignalR

> **Document ID:** 046  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Show how SignalR pushes server-driven updates into the state container and how to broadcast tenant-safe events.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|----------------|
| 1 | [Why Live State Matters](#why-live-state) | Plain-language overview of SignalR, the state container, and tenants |
| 2 | [The Push Pipeline End to End](#push-pipeline) | The five hops a change makes from server code to a browser |
| 3 | [Wiring SignalR Into the State Container](#wire-state-container) | How the hub message turns into a model event components can hear |
| 4 | [Broadcasting Tenant-Safe Events](#tenant-safe-events) | How groups and the `TenantId` keep one customer's data off another's screen |
| 5 | [Reconnection, Ordering, and Backpressure](#reliability) | What the client does on a dropped connection and why polling still backs it up |
| 6 | [Testing and Common Pitfalls](#testing-pitfalls) | How to exercise broadcasts and the traps that leak data or spin the UI |
| 7 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-live-state"></a>
## 1. Why Live State Matters

**Why it matters:** When two people work in the same app at the same time, edits one person makes should appear on the other person's screen without anyone hitting refresh. If they don't, people overwrite each other, act on stale numbers, and lose trust in the tool. This doc explains the machinery FreeCRM uses to make the screen update itself.

A few terms, defined once and used throughout:

- **SignalR** — a Microsoft library for *real-time* messaging. "Real-time" here just means the server can push a message to a browser the moment something happens, instead of the browser having to ask "anything new?" over and over. It keeps a long-lived connection open (a WebSocket when the browser supports one) so messages travel in milliseconds.
- **Hub** — the server-side class that SignalR connections attach to. It is the switchboard: browsers connect to it, and the server calls methods on it to fan messages back out. FreeCRM's hub is named `crmHub`.
- **The state container** — the single in-memory object on the client (`BlazorDataModel`, injected everywhere as `Model`) that holds the current user, tenant, lists of records, and so on. Components read from it and *subscribe* to it so they redraw when it changes. Doc [014](014_state-container.md) covers it in full; here we only care that SignalR feeds new data into it.
- **Tenant** — one isolated customer/organization inside a multi-tenant app. Every record carries a `TenantId`, and a user from one tenant must never see another tenant's data. SignalR has to honor that boundary too, which is the whole point of Section 4.

The alternative to push is **polling** — the browser asking the server "anything new?" on a timer. Polling is simple but wasteful: most requests come back empty, and you still feel a delay equal to the polling interval. FreeCRM uses push for instant updates *and* keeps a light poll for version/heartbeat checks, so the two approaches complement each other rather than compete (see Section 5).

The single message type that travels across this whole system is `DataObjects.SignalRUpdate`. Everything below is really just "how does one of these get created, routed to the right browsers, and turned into a screen update":

```csharp
public partial class SignalRUpdate
{
    public Guid? TenantId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public string UpdateType { get; set; } = "Unknown";
    public string Message { get; set; } = String.Empty;
    public object? Object { get; set; }
    public string? ObjectAsString { get; set; }
}
```

Read that as: *who* it concerns (`TenantId`, `UserId`), *what* it concerns (`ItemId`, `UpdateType` like `"Invoice"` or `"User"`, plus a free-text `Message` like `"saved"` or `"deleted"`), and *the payload itself* (`Object`, serialized into `ObjectAsString` for transport). The allowed `UpdateType` values are constants on `DataObjects.SignalRUpdateType` — `Appointment`, `Invoice`, `User`, `Tenant`, `Setting`, `Undelete`, and so on — so both server and client agree on the vocabulary.

---

<a id="push-pipeline"></a>
## 2. The Push Pipeline End to End

**Why it matters:** Debugging "the screen didn't update" is much easier when you know the exact path a change travels. There are five hops, and a failure is always at one of them.

Here is the full journey, server to browser:

1. **A data operation finishes and asks for a broadcast.** Inside the data layer, after a save or delete succeeds, the code constructs a `SignalRUpdate` and calls `SignalRUpdate(...)`. For example, when a tenant is saved (`DataAccess.Tenants.cs`):

   ```csharp
   await SignalRUpdate(new DataObjects.SignalRUpdate {
       TenantId = TenantId,
       ItemId = TenantId,
       UpdateType = DataObjects.SignalRUpdateType.Tenant,
       Message = "saved",
       // ...
   });
   ```

2. **The data layer posts the update to the app's own API.** `DataAccess.SignalR.cs` doesn't talk to the hub directly. It serializes the update and HTTP-POSTs it to the application's `api/Data/SignalRUpdate` endpoint. This indirection lets background work and out-of-process jobs trigger broadcasts by just calling an HTTP endpoint:

   ```csharp
   string updateData = SerializeObject(update);
   await client.PostAsync(baseURL + "api/Data/SignalRUpdate/",
       new StringContent(updateData, System.Text.Encoding.UTF8, "application/json"));
   ```

   Note two helpful touches it adds before sending: if `Object` is set but `ObjectAsString` is empty, it serializes the object for you; and if there's a `UserId` but no `UserDisplayName`, it fills in the display name so the receiving UI can say "saved by Jane" without another lookup.

3. **The controller hands the update to the hub.** The endpoint in `DataController.Utilities.cs` holds an injected `IHubContext<crmHub, IsrHub>` (the server-side handle for talking *into* the hub). It first gives app-specific code a chance to handle the message, then routes it:

   ```csharp
   public async Task SignalRUpdate(DataObjects.SignalRUpdate update)
   {
       if (_signalR != null) {
           var processedInApp = await SignalRUpdateApp(update);
           if (!processedInApp) {
               if (update.TenantId.HasValue) {
                   // Tenant-specific: send only to that tenant's group.
                   await _signalR.Clients.Group(update.TenantId.Value.ToString()).SignalRUpdate(update);
               } else {
                   // Non-tenant update: send to everyone connected.
                   await _signalR.Clients.All.SignalRUpdate(update);
               }
           }
       }
   }
   ```

   This is the tenant boundary in action — covered in detail in Section 4.

4. **SignalR pushes the message to the matching browsers.** Each connected browser registered a handler for the `"SignalRUpdate"` method. SignalR invokes it over the open connection. No request from the browser is involved — this is the "push."

5. **The browser turns the message into a state change.** The client's handler (in `MainLayout.razor`) runs `ProcessSignalRUpdate`, which inspects `UpdateType` and `Message`, reloads or patches the relevant slice of `Model`, and finally re-notifies any component that subscribed. Section 3 covers this hop.

A useful mental model: the hub is a **strongly typed** switchboard. The interface `IsrHub` declares exactly one outbound method, so the compiler guarantees server and client speak the same shape:

```csharp
public partial interface IsrHub
{
    Task SignalRUpdate(DataObjects.SignalRUpdate update);
}
```

---

<a id="wire-state-container"></a>
## 3. Wiring SignalR Into the State Container

**Why it matters:** A message arriving in the browser does nothing on its own. It only matters if it lands in the state container and the right components hear about it. This is where SignalR and the live UI actually meet.

**Setting up the connection (once, in `MainLayout`).** When the user is logged in and the model is loaded, `MainLayout.razor` builds a single hub connection for the whole app, registers the inbound handler, starts it, and announces which tenant this browser belongs to:

```csharp
hubConnection = new HubConnectionBuilder()
    .WithUrl(Model.ApplicationUrl + "crmHub")
    .WithStatefulReconnect()
    .WithAutomaticReconnect()
    .Build();

hubConnection.On<DataObjects.SignalRUpdate>("SignalRUpdate", async (update) => {
    await ProcessSignalRUpdate(update);
});

await hubConnection.StartAsync();

await hubConnection.InvokeAsync("JoinTenantId", Model.TenantId);
```

`WithUrl(... + "crmHub")` matches the server's mapped endpoint. `On<...>("SignalRUpdate", ...)` says "when a `SignalRUpdate` message arrives, run `ProcessSignalRUpdate`." `InvokeAsync("JoinTenantId", Model.TenantId)` calls a method on the server hub to join this connection to the tenant's group — that's the subscription side of tenant isolation.

**Translating the message into model changes.** `ProcessSignalRUpdate` is a big, deliberate `switch` on `update.UpdateType`. Each case does the smallest thing that keeps the model correct — sometimes a targeted patch, sometimes a reload. Two real examples:

```csharp
// A reference list changed: just reload that list into the model.
case DataObjects.SignalRUpdateType.Location:
    await Helpers.LoadLocations();
    break;

// A user record changed: deserialize the payload and patch it in place.
case DataObjects.SignalRUpdateType.User:
    var user = Helpers.DeserializeObject<DataObjects.User>(update.ObjectAsString);
    if (user != null) {
        var existingUser = Model.Users.FirstOrDefault(x => x.UserId == user.UserId);
        // ...replace or add, and if it's the current user, refresh Model.User...
    }
    break;
```

Notice the payload arrives as `update.ObjectAsString` (a JSON string) and is turned back into a typed object with `Helpers.DeserializeObject<T>(...)`. That mirrors the server serializing it on the way out.

**Two ways components find out.** After handling the message, `ProcessSignalRUpdate` ends with:

```csharp
// Also trigger the update in the model.
Model.SignalRUpdate(update);
```

This is the bridge into the state container's event system. `Model` exposes two relevant events:

- `OnChange` — fired whenever any model property changes (via the model's private `NotifyDataChanged`). Components subscribe to it to call `StateHasChanged` and redraw. The reloads/patches above trip this automatically.
- `OnSignalRUpdate` — fired specifically by `Model.SignalRUpdate(update)`, carrying the raw update. Components subscribe when they care about the *event itself*, not just the new data — for example, to show a toast like "this record was updated by someone else."

A component opts in by subscribing in `OnInitialized` and unsubscribing in `Dispose` (failing to unsubscribe is the classic memory leak — see [015](015_listening-for-change.md)):

```csharp
protected override void OnInitialized()
{
    Model.OnChange += OnDataModelUpdated;
    Model.OnSignalRUpdate += SignalRUpdate;
}
```

And it reacts only to updates it cares about, ignoring its own edits so it doesn't double-notify the person who made the change:

```csharp
protected void SignalRUpdate(DataObjects.SignalRUpdate update)
{
    if (Model.View == "viewinvoice"
        && update.UpdateType == DataObjects.SignalRUpdateType.Invoice
        && update.ItemId == _invoice.InvoiceId
        && update.UserId != Model.User.UserId) {
        switch (update.Message.ToLower()) {
            case "deleted":
                Back();
                Model.Message_RecordDeleted("", update.UserDisplayName);
                break;
            case "saved":
                Model.Message_RecordUpdated("", update.UserDisplayName);
                break;
        }
    }
}
```

That `update.UserId != Model.User.UserId` guard is the small detail that makes live updates feel right: you don't get told "Jane updated this" when *you* are Jane.

---

<a id="tenant-safe-events"></a>
## 4. Broadcasting Tenant-Safe Events

**Why it matters:** In a multi-tenant system, a leaked broadcast is a data breach. If a "customer saved" message from Tenant A reached a browser logged into Tenant B, that's another company's data on the wrong screen. SignalR isolation is therefore a correctness *and* a security feature.

The mechanism is SignalR **groups** — named buckets of connections. FreeCRM uses one group per tenant, named with the tenant's GUID.

**Joining a group (the hub side).** When a browser calls `JoinTenantId`, the hub first removes the connection from any tenant groups it had joined before (so a user switching tenants doesn't keep receiving the old one), then adds it to the requested tenant's group:

```csharp
public async Task JoinTenantId(string TenantId)
{
    if (!tenants.Contains(TenantId)) {
        tenants.Add(TenantId);
    }

    // Before adding a user to a Tenant group remove them from any groups they were in before.
    if (tenants != null && tenants.Count() > 0) {
        foreach (var tenant in tenants) {
            try {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenant);
            } catch { }
        }
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, TenantId);
}
```

`Context.ConnectionId` is SignalR's unique ID for this one browser connection; groups are just collections of those IDs.

**Targeting a group (the broadcast side).** When the server sends, the `TenantId` on the update decides the audience. This is the same branch you saw in the pipeline, and it's the whole rule:

```csharp
if (update.TenantId.HasValue) {
    // Tenant-specific: only this tenant's group receives it.
    await Clients.Group(update.TenantId.Value.ToString()).SignalRUpdate(update);
} else {
    // Intentionally global (e.g. an app-wide setting change).
    await Clients.All.SignalRUpdate(update);
}
```

So the safety contract is simple: **if a message concerns one tenant, it must carry that tenant's `TenantId`.** Forget it, and `Clients.All` sends it to everyone — the leak. Set it correctly, and `Clients.Group(...)` confines it to exactly the connections that joined that tenant.

**Defense in depth on the client.** The browser doesn't blindly trust the routing. `ProcessSignalRUpdate` re-checks the tenant before acting:

```csharp
if (update != null && (update.TenantId == null || update.TenantId == Model.TenantId)) {
    // ...handle it...
}
```

A message is only processed if it's explicitly global (`TenantId == null`) or matches the browser's own tenant. Belt and suspenders.

**The hub requires authentication.** The hub class is marked `[Authorize]`, so an unauthenticated connection can't attach to it at all. That, plus group targeting, plus the client-side tenant check, gives three independent layers protecting the boundary.

**When global is correct.** Some updates *should* reach everyone — for example an application-settings change handled under `SignalRUpdateType.Setting` with message `"applicationsettingsupdate"`, which can flip app-wide URL/behavior flags. For those, leaving `TenantId` null and using `Clients.All` is the intended design, not a mistake. The rule is "scope to the tenant *unless the change genuinely affects all tenants*."

---

<a id="reliability"></a>
## 5. Reconnection, Ordering, and Backpressure

**Why it matters:** Real networks drop. Laptops sleep, Wi-Fi hiccups, phones change towers. If a dropped connection meant lost updates and a frozen UI, live state would be worse than no live state. FreeCRM leans on SignalR's built-in resilience and backstops it with a poll.

**Automatic reconnect.** The client connection is built with two reliability options:

```csharp
.WithStatefulReconnect()
.WithAutomaticReconnect()
```

- `WithAutomaticReconnect()` tells the client to transparently retry a dropped connection on a backoff schedule, instead of giving up. The user usually never notices.
- `WithStatefulReconnect()` enables SignalR's *stateful reconnect*: on a brief disconnect, the server buffers messages and the client resumes the same logical connection, so messages sent during the blip aren't simply lost. The server must opt in too — and it does, in `Program.cs`:

  ```csharp
  app.MapHub<crmHub>("/crmHub", signalRConnctionOptions => {
      signalRConnctionOptions.AllowStatefulReconnects = true;
  });
  ```

**Re-joining the group after a reconnect.** Group membership lives on the server side of a connection. A genuinely *new* connection (not a stateful resume) starts in no groups, so the app must call `JoinTenantId` again. In practice the connection is only built when `hubConfigured` is false, and that flag is reset during re-validation (`hubConfigured = false` in `ValidateLogin`), so a fresh login/connection re-runs the join. Keep this in mind if you add your own groups: re-establish them whenever a new connection is created, never assume membership survived.

**Ordering.** SignalR delivers messages from a single connection in the order they were sent. FreeCRM does *not* add sequence numbers or reordering logic — it doesn't need to, because each `SignalRUpdate` is self-describing (`UpdateType`, `ItemId`, `Message`) and handlers are written to be **idempotent**: applying the same "Location changed, reload locations" twice produces the same result. Design new handlers the same way — reload-or-replace by ID rather than "increment" or "append blindly" — and out-of-order or duplicate delivery stops mattering.

**Backpressure and the polling backstop.** "Backpressure" means what happens when updates arrive faster than the UI can handle, or when the connection is down entirely. Two things keep this safe:

- Handlers do the *minimum* work per message — a targeted patch or a single list reload — so a burst doesn't pile up expensive renders.
- Independently of SignalR, `MainLayout` runs a timer that calls `CheckForUpdates` every 10 seconds (every ~2 seconds while the server appears offline). That poll fetches version info, refreshes the auth token, flips `Model.AppOnline`, and triggers a reload when the server has been updated. So even if a push is missed entirely, the app still notices the server is back and recovers — push for speed, poll for guaranteed eventual consistency.

The takeaway: you get instant updates on the happy path and self-healing on the unhappy path, without writing reconnection logic yourself.

---

<a id="testing-pitfalls"></a>
## 6. Testing and Common Pitfalls

**Why it matters:** SignalR bugs are easy to miss in single-user testing and embarrassing in production — you only see them when two people (or two tenants) are online at once. Test like that on purpose.

**How to exercise broadcasts.**

- **Two-browser test.** Open the app in two browsers (or one normal, one private/incognito) logged in as different users in the *same* tenant. Edit a record in one; confirm the other reacts (toast, list refresh) and that the editor itself does *not* get the "someone else changed this" toast.
- **Cross-tenant test.** Log the two browsers into *different* tenants and repeat. The second browser must see **nothing**. If it reacts, a `TenantId` is missing on the broadcast or the group join failed — this is the most important test in this doc.
- **Trigger a broadcast directly.** Because the data layer just POSTs to `api/Data/SignalRUpdate`, you can fire a test update by POSTing a `SignalRUpdate` JSON body to that endpoint with the right `TenantId` and `UpdateType`, then watch connected browsers respond.
- **Reconnect test.** Kill network/Wi-Fi briefly, then restore it. Confirm the connection comes back (`IsSignalRConnected` reflects `HubConnectionState.Connected`) and that updates flow again.

**Common pitfalls.**

- **Missing `TenantId` → cross-tenant leak.** The single most dangerous mistake. No `TenantId` means `Clients.All`, which means everyone. Always set it for tenant-scoped changes.
- **Forgetting to unsubscribe → memory leak and ghost handlers.** If a component does `Model.OnSignalRUpdate += ...` but never `-= ...` in `Dispose`, the disposed component keeps receiving updates and is never garbage-collected. Always pair subscribe/unsubscribe. (See [015](015_listening-for-change.md).)
- **Acting on your own update.** Without the `update.UserId != Model.User.UserId` guard, the person who made a change gets told someone changed it. Always exclude your own `UserId` for "someone else did X" UX.
- **Heavy work inside a handler.** Handlers run on every matching message. Expensive calls (large reloads, synchronous loops) cause jank under load. Keep handlers to a targeted patch or a single scoped reload.
- **Assuming group membership survives a new connection.** A brand-new connection joins no groups. Re-call `JoinTenantId` (and any custom group joins) when the connection is rebuilt.
- **Forgetting the payload is a string.** `Object` travels as `ObjectAsString` (JSON). On the client you must `DeserializeObject<T>` it; reading `update.Object` after transport won't give you your typed object back.
- **Trusting routing alone.** Keep the client-side tenant check (`update.TenantId == null || update.TenantId == Model.TenantId`) in place as defense in depth, even though the server already targets the group.

If a live update "doesn't work," walk the five hops from Section 2 in order — broadcast created? POSTed to the API? routed by the controller? received by the browser? handled into the model? The break is always at exactly one hop.

---

<a id="related-docs"></a>
## 7. Related Docs

- [014 — The Living State Container](014_state-container.md) — the container it updates
- [015 — Listening for Change Without Leaking](015_listening-for-change.md) — how components react
- [045 — Work That Outlives a Click](045_background-service.md) — the sibling background service
- [085 — When Things Go Sideways](085_diagnostics-playbook.md) — diagnosing live-update failures
- [086 — Keeping It Fast at Scale](086_performance-at-scale.md) — keeping broadcasts and handlers cheap under load

---
*GuidesV2 046 · drafted from source on 2026-06-05.*
