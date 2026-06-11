# 085 — When Things Go Sideways

> **Document ID:** 085  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** A symptom-to-fix troubleshooting playbook spanning state, tenancy, real-time, and data-layer failures.
> **Audience:** Operators and maintainers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | Purpose, the four subsystems, and the plain-language terms you need |
| 2 | [Triage: Symptom to Suspect](#triage) | A quick routing table from "what the user saw" to "which subsystem to open" |
| 3 | [State & Session Failures](#state-failures) | Stale screens, blank screens, and changes that don't appear — the `BlazorDataModel` |
| 4 | [Tenancy & Isolation Failures](#tenancy-failures) | Wrong-tenant data, "Access Denied," and the `TenantId` filter chain |
| 5 | [Real-Time & Connection Failures](#realtime-failures) | The SignalR hub, dropped sockets, and updates that never arrive |
| 6 | [Data-Layer Failures](#data-failures) | Database offline, migrations, and the `ActionResponse` error envelope |
| 7 | [Verify the Fix & Roll Back](#verify-rollback) | Proving recovery and undoing a change without making it worse |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

When a user says "it's broken," they almost never know *what* is broken — they only know what they saw. Your job as an operator is to turn a vague symptom into a confident guess about *which part of the system* is misbehaving, and then to confirm it. This doc is the map from symptom to suspect.

Almost every failure in FreeCRM falls into one of four subsystems. Learn these four names and you have a mental filing cabinet for every bug report:

- **State** — the in-memory "current picture" the browser holds. In this codebase that picture is a single C# object, `BlazorDataModel` (a `partial class`, so its code is spread across several files like `DataModel.cs`). Think of it as the app's short-term memory: who is logged in, which tenant they're in, the lists of users, services, appointments, and so on. When state goes wrong, the screen shows the *wrong* thing or a *stale* thing, even though the database is fine.

- **Tenancy** — the rule that keeps each customer's data walled off from every other customer's. A **tenant** is one customer organization. Every record carries a `TenantId` (a GUID — a long unique ID like `a1b2...`), and the system filters every query by it. When tenancy goes wrong, a user either sees another tenant's data (a leak — serious) or sees nothing because the scope is missing (an over-block).

- **Real-time** — the live push channel that lets one user's change appear on everyone else's screen without a refresh. It runs on **SignalR**, Microsoft's WebSocket library. A **WebSocket** is a connection that stays open so the server can push messages to the browser at any time. When real-time goes wrong, edits are saved correctly but other people don't *see* them until they reload.

- **Data layer** — the code that actually talks to the database, built on **Entity Framework (EF)**, Microsoft's object-to-database mapper. It lives in the `DataAccess` class. When the data layer goes wrong, saves fail, pages error out, or the whole app refuses to start because it can't reach the database.

**Why the split matters:** the same visible symptom — "my change didn't show up" — has a completely different cause and fix in each subsystem. If it's a *state* problem, the change is in the database but the browser didn't refresh its picture. If it's a *real-time* problem, the change saved but the push never fired. If it's a *data-layer* problem, the change never saved at all. Guessing wrong wastes time and can make things worse. The triage table in the next section exists to stop you guessing.

**How to read this doc:** Start at the triage table (Section 2) to find your suspect subsystem, jump to that section (3–6) for the symptom→cause→fix detail, then use Section 7 to confirm the fix actually worked and to roll back safely if it didn't. Each subsystem section is grounded in the real source files, named so you can open them yourself.

---

<a id="triage"></a>
## 2. Triage: Symptom to Suspect

The goal of triage is to spend two minutes narrowing four suspects down to one *before* you start digging. Read the symptom the user reported, then follow the row.

| What the user saw | Most likely subsystem | Go to |
|---|---|---|
| "My edit saved but I still see the old value" / screen looks frozen | **State** — the model didn't re-notify the UI | [§3](#state-failures) |
| "Someone else's change shows on my screen but mine doesn't, until I refresh" | **Real-time** — the SignalR push didn't reach you | [§5](#realtime-failures) |
| "I can see another company's customers / records" | **Tenancy** — a `TenantId` filter is missing | [§4](#tenancy-failures) |
| "Access Denied" or a forced logout on a page they should reach | **Tenancy / auth** — token, fingerprint, or tenant mismatch | [§4](#tenancy-failures) |
| "Save failed" / a red error message with details | **Data layer** — the save returned `Result = false` | [§6](#data-failures) |
| "The whole site is down / a setup screen appears" | **Data layer** — database offline or not configured | [§6](#data-failures) |
| "It works for me but not my coworker" | **State or real-time**, scoped to one browser session | [§3](#state-failures) → [§5](#realtime-failures) |

**The single best first question to ask:** *did the change reach the database?* You can check this directly in the database (or via an admin list page that re-queries). The answer splits the whole tree:

- **Data has the new value, but the screen doesn't** → it's a *delivery* problem, not a *save* problem. Suspect **state** (your own screen is stale) or **real-time** (someone else's screen is stale). State problems affect the person who made the change; real-time problems affect *everyone else*.
- **Data does NOT have the new value** → it's a *save* problem. Suspect the **data layer**. Look at the `ActionResponse` that came back from the save call (see [§6](#data-failures)) — it carries the reason.
- **Data has values that belong to the wrong tenant** → it's a **tenancy** problem. Stop and treat it as a potential leak (see [§4](#tenancy-failures)).

Triage is about elimination, not proof. Once you have one suspect, jump to its section and confirm.

---

<a id="state-failures"></a>
## 3. State & Session Failures

**Why this matters:** state failures are the most common and the most *deceptive*, because nothing is actually broken in the database — the browser is just showing an out-of-date picture. Users feel like they "lost work" when in fact the work is safely saved; the UI just never refreshed. Knowing this lets you reassure the user and look in the right place.

**The one object to understand.** All shared screen state lives in `BlazorDataModel`, a partial class whose main body is in `FreeCRM/CRM.Client/DataModel.cs`. It is held as a single instance (commonly referenced as `Model`) and every page reads from it. The model does not magically update the screen — it *raises an event* when a property changes, and pages re-render in response. That event mechanism is the heart of almost every state bug.

Here is the real pattern, repeated for nearly every property in `DataModel.cs`:

```csharp
public bool ViewIsEditPage {
    get { return _ViewIsEditPage; }
    set {
        if (value != _ViewIsEditPage) {
            _ViewIsEditPage = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

And the notifier itself:

```csharp
public event Action? OnChange;
private void NotifyDataChanged() => OnChange?.Invoke();
```

Two facts fall out of this that explain most state bugs:

1. **The setter only fires `NotifyDataChanged()` if the value actually changed** (`if (value != _ViewIsEditPage)`). If code mutates a list or object *in place* — for example adding to `Model.Users` without reassigning the property — the equality check may not trip and the UI is never told to redraw. The change is in memory but invisible.
2. **A page only updates if it subscribed to `OnChange`.** A page that forgot to subscribe (or that disposed its subscription) shows a frozen snapshot.

### Symptom → Cause → Fix

**Symptom: "I edited a record, it saved, but my own screen still shows the old value."**
- *Cause:* the model property was mutated without a change being detected, so `OnChange` never fired; or the page isn't subscribed.
- *Fix (operator):* have the user navigate away and back, or do a full browser refresh. Both rebuild the model from the server.
- *Fix (maintainer):* the durable recovery is `Helpers.ReloadModel()` in `FreeCRM/CRM.Client/Helpers.cs`. It re-fetches the whole picture from the server:

```csharp
blazorDataModelLoader = await GetOrPost<DataObjects.BlazorDataModelLoader>(
    "api/Data/GetBlazorDataModel/" + Model.User.UserId.ToString());
```

  `BlazorDataModelLoader` is the server-built bundle of everything a freshly-loaded page needs; calling `ReloadModel()` replaces the stale in-memory picture with a correct one from the database.

**Symptom: "The screen is completely blank or stuck on a spinner."**
- *Cause:* state never finished loading (`Model.Loaded` stayed false), often because an upstream call failed. Note in `MainLayout.razor` that the SignalR hub, version checks, and most logic only run `if (Model.Loaded)`.
- *Fix:* check the browser console and network tab for the failed startup call (commonly `api/Data/GetBlazorDataModel` or `api/Data/GetVersionInfo`). A blank screen is usually a *data-layer* or *auth* failure wearing a state costume — follow it to [§6](#data-failures).

**Symptom: "It logged me out / I lost my session."**
- *Cause:* the auth token the browser holds expired or stopped matching. The app proactively refreshes it: `MainLayout.razor` periodically calls `api/Data/GetVersionInfoWithToken` and runs `Helpers.UpdateUserToken(info.Token)` roughly every 15 minutes. If those calls are failing (server unreachable, clock skew, fingerprint mismatch), the session silently dies.
- *Fix:* confirm the version/token endpoint is reachable and returning. If the token itself is being rejected, that is an auth/tenancy concern — see [§4](#tenancy-failures).

**Rule of thumb:** for any "stale screen" report where the data is correct in the database, the safe, universal recovery is a full reload, which runs `ReloadModel()`. It is never destructive — it only *reads*.

---

<a id="tenancy-failures"></a>
## 4. Tenancy & Isolation Failures

**Why this matters most of all:** a tenancy failure is the one bug category that can become a *security incident*. If a user sees another customer's data, that is a cross-tenant leak — treat it as urgent, not cosmetic. The opposite failure (a user blocked from their own data) is annoying but safe. Knowing which way the failure leans tells you how fast to move.

**The model.** A **tenant** is one customer org, identified by a `TenantId` GUID. Isolation is enforced in three independent layers, and a bug in any one of them produces a different symptom.

**Layer 1 — server resolves *which* tenant the request belongs to.** Every API request flows through the `DataController` constructor in `FreeCRM/CRM/Controllers/DataController.cs`. It figures out the tenant and the current user from the request itself:

```csharp
// See if a TenantId is included in the header or querystring.
string tenantId = HeaderValue("TenantId");
if (String.IsNullOrEmpty(tenantId)) {
    tenantId = QueryStringValue("TenantId");
}
```

and, for an authenticated user, from the signed login token's claims:

```csharp
string tenantIdString = context?.User?.FindFirstValue(ClaimTypes.GroupSid) ?? string.Empty;
TenantId = Guid.Empty;
Guid.TryParse(tenantIdString, out TenantId);
CurrentUser = da.GetUserFromToken(TenantId, token, _fingerprint).Result;
```

So the server's idea of "your tenant" comes from the header/querystring **or** the `GroupSid` claim baked into your token. If those disagree or are wrong, everything downstream is scoped to the wrong tenant.

**Layer 2 — the data layer filters every query by `TenantId`.** This is the actual wall. Open almost any method in `FreeCRM/CRM.DataAccess/` and you'll see the filter spelled out on every read, for example in `DataAccess.Users.cs`:

```csharp
recs = await data.Users
    .Where(x => x.TenantId == TenantId && x.Deleted != true)
    .ToListAsync();
```

There is no global, automatic tenant filter — isolation is achieved by *every query individually* including `x.TenantId == TenantId`. That design is why a single forgotten `.Where(... TenantId ...)` in one new method is the classic source of a leak.

**Layer 3 — the client double-checks incoming real-time updates.** Even pushed updates are re-filtered on arrival, in `ProcessSignalRUpdate` (`MainLayout.razor`):

```csharp
if (update != null && (update.TenantId == null || update.TenantId == Model.TenantId)) {
    ...
}
```

A non-tenant update (`TenantId == null`, e.g. an app-wide announcement) is allowed through; a tenant-specific one is honored only if it matches *your* tenant. This is a backstop, not the primary wall.

### Symptom → Cause → Fix

**Symptom: "I can see another company's records." (LEAK — urgent)**
- *Cause:* a data-access query is missing its `x.TenantId == TenantId` filter, or a method received the wrong `TenantId`.
- *Fix (maintainer):* identify the exact list/screen, find the `DataAccess` method behind it, and confirm the `.Where(...)` includes the tenant filter. Compare against a sibling method in the same file that *does* filter correctly. Until patched, consider disabling the offending screen.

**Symptom: "Access Denied on a page I should be able to reach."**
- *Cause:* the controller couldn't establish a valid `CurrentUser` for the resolved tenant, so the endpoint returns its access-denied marker. The marker is literally `_returnCodeAccessDenied = "{{AccessDenied}}"`, returned via `Unauthorized(...)` when checks like `CurrentUser.Admin || CurrentUser.UserId == id` fail.
- *Cause (deeper):* the token's **fingerprint** (a value tying the token to one browser/session) didn't match. In `GetUserFromToken` (`DataAccess.Users.cs`) a mismatch returns an empty, unauthenticated user:

```csharp
if (!String.IsNullOrWhiteSpace(fingerprint) || !String.IsNullOrWhiteSpace(tokenFingerprint)) {
    // Make sure the fingerprint matches
    if (fingerprint != tokenFingerprint) {
        return output;   // empty user → not authenticated
    }
}
```

- *Fix:* have the user log out and back in to mint a fresh token with the current fingerprint. If it persists across a clean login, check that the `TenantId` claim in the token matches the tenant they're trying to use.

**Symptom: "After switching tenants the page shows the old tenant's data."**
- *Cause:* `Model.TenantId` changed but a page didn't react to the tenant-change events. The model exposes `OnTenantChanging` / `OnTenantChanged` and the `NotifyTenantChanging()` / `NotifyTenantChanged()` methods (in `DataModel.cs`) for exactly this; a page that doesn't reload its data on those events keeps the prior tenant's lists.
- *Fix:* a full reload (which runs `ReloadModel()` for the now-current tenant) clears it. For real-time membership, see the `JoinTenantId` note in [§5](#realtime-failures) — switching tenants must also re-join the new SignalR group.

---

<a id="realtime-failures"></a>
## 5. Real-Time & Connection Failures

**Why this matters:** real-time failures are sneaky because *nothing is actually lost*. Saves succeed, the database is correct — the only problem is that *other people's screens* don't update live. Users interpret this as data loss ("my coworker's edit disappeared") when it's really a delivery gap. Spotting it saves you from chasing a phantom data bug.

**The model.** Live updates ride on **SignalR**. The server side is a **hub** — a class clients connect to and call methods on. It is `crmHub` in `FreeCRM/CRM/Hubs/signalrHub.cs`:

```csharp
[Authorize]
public partial class crmHub : Hub<IsrHub>
{
    public async Task JoinTenantId(string TenantId)
    {
        ...
        await Groups.AddToGroupAsync(Context.ConnectionId, TenantId);
    }
}
```

Two things to notice. First, `[Authorize]` means **only authenticated users can connect** — an unauthenticated or expired session gets *no* live updates. Second, clients are sorted into **groups** named by `TenantId`; a message sent to group `X` reaches only the connections that joined `X`. That group membership is how real-time respects tenancy.

**The client side** lives in `FreeCRM/CRM.Client/Layout/MainLayout.razor`. Once the user is logged in and the model is loaded, it builds the connection:

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

`WithAutomaticReconnect()` means dropped sockets try to heal themselves; `WithStatefulReconnect()` lets a brief drop resume without losing messages. The crucial last line — `JoinTenantId` — is what puts this browser in its tenant's group. **If a session connects but never joins (or joins the wrong tenant), it stays connected yet silently receives nothing.**

**The full path of one live update**, end to end:
1. Some server code calls `DataAccess.SignalRUpdate(update)` (`DataAccess.SignalR.cs`), which POSTs the update to `api/Data/SignalRUpdate`.
2. That endpoint (`DataController.Utilities.cs`) fans it out through the hub, respecting tenancy:

```csharp
if (update.TenantId.HasValue) {
    // tenant-specific: only that tenant's group
    await _signalR.Clients.Group(update.TenantId.Value.ToString()).SignalRUpdate(update);
} else {
    // app-wide
    await _signalR.Clients.All.SignalRUpdate(update);
}
```

3. Each subscribed browser's `On("SignalRUpdate", ...)` handler runs `ProcessSignalRUpdate`, which re-checks the tenant ([§4](#tenancy-failures)) and then updates `Model` so the screen redraws.

Break any link and live updates stop while saves keep working.

### Symptom → Cause → Fix

**Symptom: "My edits save, but my coworker doesn't see them until they refresh."**
- *Cause:* the coworker's browser isn't receiving pushes — usually a dropped/unstarted connection, or it never joined the tenant group.
- *Fix:* have the coworker refresh. A refresh re-runs the whole `MainLayout` setup: rebuild connection → `StartAsync()` → `JoinTenantId`. You can confirm the connection's health with MainLayout's own helper:

```csharp
public bool IsSignalRConnected =>
    hubConnection?.State == HubConnectionState.Connected;
```

  If `State` is `Reconnecting` or `Disconnected`, that browser is the problem, not the server.

**Symptom: "Nobody gets live updates at all."**
- *Cause:* the broadcast path is broken — the `api/Data/SignalRUpdate` POST is failing, or `_signalR` is unavailable on the server.
- *Fix:* check server logs around the POST to `api/Data/SignalRUpdate`. Confirm clients can reach `…/crmHub` (the WebSocket endpoint) — a proxy or load balancer that doesn't forward WebSocket upgrades is a classic culprit. Also confirm users are actually authenticated, since `[Authorize]` blocks anonymous connections.

**Symptom: "After switching tenants I get updates for the old tenant (or none)."**
- *Cause:* the connection didn't re-`JoinTenantId` for the new tenant. The hub's `JoinTenantId` removes the user from prior groups before adding the new one, so it's safe to re-call — but it *must* be called again on a tenant change.
- *Fix:* a full refresh re-joins correctly. For maintainers, ensure any in-app tenant switch re-invokes `JoinTenantId` with the new `Model.TenantId`.

**Symptom: "Updates lag by many seconds, then arrive in a burst."**
- *Cause:* the socket dropped and `WithAutomaticReconnect()` is mid-retry; messages queue until it reconnects.
- *Fix:* usually self-healing. Persistent lag points to flaky networking or a proxy timing out idle WebSocket connections — raise the idle timeout on the proxy.

---

<a id="data-failures"></a>
## 6. Data-Layer Failures

**Why this matters:** when the data layer fails, work is *genuinely* not saved — this is the one category where the user's fear of "I lost my changes" is real. It also includes the most dramatic failure of all: the database being unreachable, which can take the whole app offline. Recognizing data-layer signatures lets you separate a real data loss from a cosmetic display glitch.

**The model.** All database access goes through the `DataAccess` class (`FreeCRM/CRM.DataAccess/`), built on **Entity Framework** over a context called `EFDataModel`. The constructor (`DataAccess.cs`) picks a database provider from configuration and wires in automatic retry for transient blips:

```csharp
switch (_databaseType.ToLower()) {
    case "inmemory":   optionsBuilder.UseInMemoryDatabase("InMemory"); ... break;
    case "mysql":      optionsBuilder.UseMySQL(_connectionString, options => options.EnableRetryOnFailure()); break;
    case "postgresql": optionsBuilder.UseNpgsql(_connectionString, options => options.EnableRetryOnFailure()); break;
    case "sqlite":     optionsBuilder.UseSqlite(_connectionString); break;
    case "sqlserver":  optionsBuilder.UseSqlServer(_connectionString, options => options.EnableRetryOnFailure()); break;
}
```

`EnableRetryOnFailure()` means a momentary network hiccup to MySQL/Postgres/SQL Server is retried automatically — so a *transient* error often self-heals, and a *persistent* one is a real outage worth investigating.

**How errors come back.** Data-layer methods almost never throw to the caller. Instead they return objects carrying an `ActionResponse` envelope. The base type is `ActionResponseObject` (`DataObjects.cs`):

```csharp
public partial class ActionResponseObject
{
    public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
}

public partial class BooleanResponse
{
    public List<string> Messages { get; set; } = new List<string>();
    public bool Result { get; set; }
}
```

So **the first thing to read on any failed operation is `result.ActionResponse.Result` and `result.ActionResponse.Messages`.** `Result == false` means it failed; `Messages` carries the human-readable reason. This is your single most useful diagnostic for "save failed" reports.

### Symptom → Cause → Fix

**Symptom: "Save failed" with a red message.**
- *Cause:* the operation returned `ActionResponse.Result == false`. The reason is sitting in `ActionResponse.Messages`.
- *Fix:* read the message. Validation failures (missing required field, duplicate) are user-fixable; a database error message points to a connectivity or schema problem (next two rows).

**Symptom: "The whole site shows a setup/error screen and won't start."**
- *Cause:* the data layer couldn't open the database at startup. The constructor sets global flags rather than crashing silently:

```csharp
} catch (Exception ex) {
    GlobalSettings.StartupError = true;
    GlobalSettings.StartupErrorCode = "DatabaseOffline";
    GlobalSettings.StartupErrorMessages.Add(ex.Message);
    ...
}
```

- *Fix:* the `StartupErrorCode` tells you which problem it is:
  - `"DatabaseOffline"` — the connection string is set but the database can't be reached. Check the database server is up, credentials are valid, and the network/firewall allows the connection. The captured `ex.Message` (and any `InnerException`) is in `StartupErrorMessages`.
  - `"MissingConnectionString"` — no connection string or database type is configured at all. The app falls back to an InMemory database and shows the configuration screen. Supply the connection string and database type, then restart.

**Symptom: "A page errors only after a deployment."**
- *Cause:* the database schema is behind the code — a **migration** (a versioned schema change) hasn't been applied.
- *Fix:* the app applies migrations itself on startup when enabled — `DatabaseApplyLatestMigrations()` runs after a successful connect (skipped for the InMemory provider). If a deploy left the schema stale, restart the app so startup re-runs migrations, and check startup logs for migration errors. **Caution:** migrations change schema; have a database backup before forcing one (see [§7](#verify-rollback)).

**Symptom: "Intermittent save failures that come and go."**
- *Cause:* transient database connectivity. For MySQL/Postgres/SQL Server, `EnableRetryOnFailure()` already retries these, so frequent breakthroughs mean the underlying connection is genuinely unstable (resource limits, failover, network).
- *Fix:* investigate the database server's health and the network path; this is infrastructure, not application code.

**A note on caching.** Some reads are cached briefly via `CacheStore` (`DataObjects/Caching.cs`) — for example `GetUserFromToken` caches its result for about 5 seconds to avoid duplicate lookups. If a user reports that a change "took a few seconds to take effect server-side," a short cache window is the benign explanation; user-related caches can be cleared with `CacheStore.ClearAllUserItems()`.

---

<a id="verify-rollback"></a>
## 7. Verify the Fix & Roll Back

**Why this matters:** a fix you can't *prove* is just a hope, and a rollback you do carelessly can turn one outage into two. This section is about closing the loop with evidence and about undoing safely.

### Verify the fix

Verify in the same subsystem terms you used to triage:

- **State fix:** confirm the screen now reflects the database *without* a forced reload after the next genuine change. If it only looks right immediately after a refresh, you fixed the symptom (stale picture) but maybe not the cause (a setter that didn't notify). The durable check: make a change in one place and watch it appear where it should, driven by `OnChange`/`ReloadModel()` rather than your manual refresh.

- **Tenancy fix:** prove isolation *both ways*. Log in as tenant A and confirm you see only A's records; log in as tenant B and confirm B's records and *no* A records. For a leak fix, this two-sided check is mandatory — fixing one direction doesn't prove the wall.

- **Real-time fix:** open two browser sessions (ideally two users in the same tenant). Change something in one and confirm it appears live in the other with no refresh. Confirm `IsSignalRConnected` is true and that a tenant switch re-joins via `JoinTenantId`.

- **Data-layer fix:** confirm `ActionResponse.Result == true` on the operation that previously failed, *and* confirm the value is actually present in the database. For a startup/database-offline fix, confirm `GlobalSettings.StartupError` is no longer set after a restart.

A fix isn't verified until you've reproduced the *original symptom path* and seen it succeed — not just confirmed the app is "up."

### Roll back safely

Roll back along the axis that changed:

- **Configuration changes** (connection string, database type, tenant settings) are the safest to revert: restore the previous value and restart. Because the `DataAccess` constructor reads configuration at startup, a restart re-reads it cleanly.

- **Code/deployment changes** roll back by redeploying the previous build. Because data-layer errors surface as `ActionResponse` envelopes and startup errors as `GlobalSettings` flags (rather than corrupting data), reverting code generally returns you to the prior behavior without data cleanup.

- **Schema migrations are the dangerous one.** A migration can drop or alter columns, so rolling code back *without* accounting for the schema can leave new code expecting an old schema or vice versa. **Never** roll back a migration against production without a current backup. The order that minimizes risk: take a backup first; prefer rolling *forward* with a corrective migration over reverting one; only revert schema as a last resort with the backup in hand.

- **State and real-time** rarely need a "rollback" — they hold no durable data. If a client is in a bad state, a full browser refresh (which rebuilds the model and reconnects the hub) is the reset.

**Golden rule:** the further down the stack a change is (state → real-time → tenancy → data/schema), the more caution its rollback needs, because the blast radius grows from "one browser" to "the whole database."

---

<a id="related-docs"></a>
## 8. Related Docs

- [014 — The Living State Container](014_state-container.md) — state-related failures
- [046 — Pushing State Live Over SignalR](046_realtime-signalr.md) — real-time failures
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — data-layer failures

---
*GuidesV2 085 · drafted from source on 2026-06-05 · grounded in FreeCRM DataModel, DataController, signalrHub, and DataAccess.*
