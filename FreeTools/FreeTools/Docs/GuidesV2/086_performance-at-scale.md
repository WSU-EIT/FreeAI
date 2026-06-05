# 086 — Keeping It Fast at Scale

> **Document ID:** 086  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Cover tuning the cached lists, event traffic, and queries for responsiveness under multi-tenant load.
> **Audience:** Operators and maintainers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Performance at Scale Matters](#why-it-matters) | Plain-language overview of cache, events, queries, and multi-tenant load |
| 2 | [Reading the Signals: Metrics and Thresholds](#signals) | The poll cadence, cache hits, and SignalR traffic that reveal slowdowns |
| 3 | [Tuning the Cached Lists](#cached-lists) | The two cache layers, the `ObjectsAreEqual` guard, and tenant-keyed expiry |
| 4 | [Taming Event Traffic](#event-traffic) | How tenant-scoped SignalR groups and targeted reloads keep traffic small |
| 5 | [Optimizing Queries Under Load](#queries) | Server-side `IQueryable` filtering, `TenantId` scoping, and paging |
| 6 | [Verifying Improvements](#verification) | Confirming a change actually helped, not just felt faster |
| 7 | [Rollback and Safe Recovery](#rollback) | Reverting tuning safely when results regress |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Performance at Scale Matters

When the app has ten users and one customer account, almost any code is fast enough. The interesting question is what happens when it has *thousands* of users spread across *hundreds* of customer accounts, all clicking at once. That is what "at scale" means here, and a few plain-language terms unlock the whole document:

- **Tenant** — one isolated customer account. FreeCRM is **multi-tenant**, meaning a single running server hosts many separate tenants at the same time, each seeing only its own data. Almost everything that follows hinges on keeping one tenant's load from spilling onto another's.
- **Cache** — a saved copy of data kept close to where it is needed, so the app does not have to re-fetch or re-compute it. A cache trades a little memory and a little staleness for a lot of speed.
- **Cached list** — one specific kind of cache: the in-memory collections the Blazor client holds, such as `Departments`, `Locations`, `Services`, and `Tags`. Pages read these instantly instead of calling the server.
- **Event traffic** — the stream of "something changed" messages pushed from the server to connected browsers over **SignalR** (a .NET library for real-time server-to-client messaging). When one admin edits a department, every other admin in that tenant needs to hear about it.
- **Query** — a request to the database for a set of records, for example "the next 25 users for this tenant, sorted by last login."

Why it matters: the three subsystems above are exactly the three that get *more expensive as the system grows*. A cache that is sized or scoped wrong leaks one tenant's data or wastes memory. Event traffic that is broadcast too widely turns one edit into a storm of messages to every browser on the server. A query that scans every row instead of one tenant's rows gets slower for everyone as the table grows. Get these three right and the app stays responsive; get them wrong and it degrades quietly until users complain. The good news is that FreeCRM already has deliberate patterns for all three — this doc explains them so you can tune within the grain of the design rather than against it.

---

<a id="signals"></a>
## 2. Reading the Signals: Metrics and Thresholds

You cannot tune what you cannot see. Before changing anything, learn what "normal" looks like, because the app has a steady built-in rhythm that you can measure against.

**The background poll (the heartbeat you can count).** The client's `MainLayout.razor` starts two repeating timers in `OnAfterRenderAsync`. One fires every **500 milliseconds** to run lightweight UI watchers, and one fires every **10 seconds** to call `CheckForUpdates`:

```csharp
updateTimer = new System.Threading.Timer(async (object? stateInfo) => {
    await CheckForUpdates();
}, new System.Threading.AutoResetEvent(false), 0, 10000);
```

That 10-second poll hits `api/Data/GetVersionInfo`. Every 90th iteration (about every **15 minutes**) it instead calls `GetVersionInfoWithToken` to refresh the login token. This matters for scale math: every connected browser makes roughly **6 version-check requests per minute**. With 1,000 active browsers that is ~6,000 requests/minute of pure background chatter before anyone clicks anything. If the app detects it has gone offline, the same timer tightens to a 2-second interval (`updateTimer?.Change(0, 2000)`) to reconnect quickly — useful for recovery, but a signal worth watching, because a flapping connection multiplies that background load fivefold.

**Cache effectiveness.** The single most important performance signal is how often the app serves data from a cache instead of the database. On the server, the `CacheStore` (built on `System.Runtime.Caching.MemoryCache`) holds things like per-tenant settings. A method such as `GetSettings` checks the cache first and only touches the database on a miss:

```csharp
var cached = CacheStore.GetCachedItem<DataObjects.TenantSettings>(TenantId, "Settings");
if (cached != null) {
    output = cached;
} else {
    // ...read from the database, then SetCacheItem(...)
}
```

If you instrument these read paths, a healthy system shows mostly hits. A sudden rise in *misses* (every request reaching the database) is the clearest early warning that a tuning change, a cache-clear, or a too-short expiry is hurting you.

**Event volume.** Watch how many SignalR messages flow per edit. The healthy pattern is *one* message per change, delivered only to the affected tenant (Section 4). If a single save produces a burst of messages, or messages reach tenants that did not change, that is the signal to investigate.

**Suggested thresholds to alert on:** background request rate climbing far above the ~6/minute/client baseline (points to reconnect flapping or a stuck timer); cache-hit ratio dropping noticeably below its normal band; any tenant-scoped query whose typical response time creeps past a fraction of a second. None of these are hard-coded limits in the product — they are the moving parts you should put a number on for *your* deployment so a regression announces itself instead of hiding.

---

<a id="cached-lists"></a>
## 3. Tuning the Cached Lists

The app caches in **two distinct layers**, and tuning each is a different decision. Confusing them is the most common mistake, so define them clearly first.

**Layer 1 — the client-side cached lists (per browser, in `BlazorDataModel`).** Each user's browser holds the reference lists it needs as plain properties on the shared state container described in [014](014_state-container.md). They are filled by small loaders in `Helpers.cs` that call the server once and stash the result:

```csharp
public async static Task LoadDepartments()
{
    var items = await GetOrPost<List<DataObjects.Department>>("api/Data/GetDepartments");
    Model.Departments = items != null && items.Any() ? items : new List<DataObjects.Department>();
}
```

The tuning lever here is **assignment, not size**. Every cached-list property runs an equality guard before it notifies the UI. Assigning a value only fires a re-render when the data actually changed, because the setter compares the old and new values by serializing both to JSON:

```csharp
public List<DataObjects.Department> Departments {
    get { return _Departments; }
    set {
        if (!ObjectsAreEqual(_Departments, value)) {
            _Departments = value;
            _ModelUpdated = DateTime.UtcNow;
            NotifyDataChanged();
        }
    }
}
```

Why it matters at scale: `ObjectsAreEqual` serializes both the old and new collection with `System.Text.Json` and compares the strings. That guard is what prevents a flood of needless redraws, but on a very large list it is itself work. The practical tuning rule is to keep these client lists scoped to genuinely small reference data (departments, locations, services, tags) and to **never** stuff large, paged record sets (like every user) into them — those belong to a query (Section 5), not a cached list. The lists are reloaded surgically when their data changes (Section 4), so they stay both small and fresh.

**Layer 2 — the server-side `MemoryCache` (shared, tenant-keyed).** On the server, `CacheStore` keeps frequently read data in process memory. Two design facts drive its tuning. First, **every entry is keyed by tenant** — the key is the logical name plus the tenant's id, so one tenant can never read another's cached value:

```csharp
string key = cacheKey + "_" + TenantId.ToString();
```

Second, entries **expire on a timer**. `SetCacheItem` defaults to a one-hour absolute expiration unless you pass your own:

```csharp
var policy = new CacheItemPolicy { AbsoluteExpiration = absoluteExpiration ?? DateTimeOffset.Now.AddHours(1.0) };
```

The tuning decisions for Layer 2 are therefore: **what to cache** (data that is read far more often than it is written, like tenant settings), **how long to hold it** (longer expiry means fewer database trips but staler data — raise it for rarely-changing config, keep it short for anything time-sensitive), and **when to clear it**. For clearing, prefer the *narrowest* tool: `CacheStore.SetCacheItem(tenantId, key, null)` evicts a single entry, `ClearAllUserItems()` removes only user-token entries, and `ClearAll()` wipes everything. Reach for `ClearAll()` only in emergencies — on a busy multi-tenant box it forces *every* tenant to rebuild its cache from the database at once, briefly turning a cache benefit into a database stampede.

A clean tenant boundary in the cache is what lets a heavy tenant's churn stay contained: their entries expiring or being cleared never disturbs a quiet tenant's hits.

---

<a id="event-traffic"></a>
## 4. Taming Event Traffic

Real-time updates are wonderful for users and dangerous for servers, because the naive version — "tell everyone about every change" — does not survive contact with hundreds of tenants. FreeCRM's design avoids that in two ways, and tuning event traffic mostly means *preserving* these two properties rather than adding new machinery.

**First lever: scope every message to one tenant's group.** A **SignalR group** is a named set of connections the server can address as a unit. When a browser connects, it joins the group named for its tenant via `JoinTenantId`, and the server sends a change only to that group:

```csharp
public async Task SignalRUpdate(DataObjects.SignalRUpdate update)
{
    if (update.TenantId.HasValue) {
        await Clients.Group(update.TenantId.Value.ToString()).SignalRUpdate(update);
    } else {
        // This is a non-tenant-specific update.
        await Clients.All.SignalRUpdate(update);
    }
}
```

Why it matters: an edit in tenant A reaches only tenant A's connections. The fan-out is bounded by *one tenant's* user count, not the whole server's. The single most damaging change you can make to event performance is to send a tenant-specific update without a `TenantId` — that drops into the `Clients.All` branch and broadcasts to every connected browser on the box. Treat `Clients.All` as reserved for genuinely global notices (a new app version, maintenance mode), never for per-tenant data changes.

**Second lever: send a small "what changed," not the whole dataset.** A `SignalRUpdate` is a thin envelope — a `TenantId`, an `UpdateType` string (one of the constants in `SignalRUpdateType`, like `"Department"` or `"Location"`), an optional `ItemId`, and an optional serialized object. The client's `ProcessSignalRUpdate` switches on `UpdateType` and reloads only the one affected list:

```csharp
case DataObjects.SignalRUpdateType.Department:
    await Helpers.LoadDepartments();
    break;
// ...
case DataObjects.SignalRUpdateType.Location:
    await Helpers.LoadLocations();
    break;
```

Why it matters: a department change triggers exactly one small `GetDepartments` reload, not a full model refresh. The combination is what keeps traffic flat as you grow — narrow audience (one tenant's group) times narrow payload (one list reloaded) equals predictable, small load per edit. Note also `LastAccessTime`, a deliberately high-frequency update type used for presence; because it only nudges an entry in the `ActiveUsers` list rather than reloading data, it stays cheap even though it fires often.

**Backpressure and recovery are handled by the transport, not by you.** The client builds its connection with stateful, automatic reconnect:

```csharp
hubConnection = new HubConnectionBuilder()
    .WithUrl(Model.ApplicationUrl + "crmHub")
    .WithStatefulReconnect()
    .WithAutomaticReconnect()
    .Build();
```

That means a brief network blip recovers without a full reload, and the 10-second poll from Section 2 is the safety net that catches anything missed while disconnected. The practical tuning guidance: keep payloads small (push an id and let the client reload, rather than pushing large objects through the hub), and resist the temptation to fire an update on trivial changes — every avoided message is fan-out you did not pay for.

---

<a id="queries"></a>
## 5. Optimizing Queries Under Load

Caches and events handle small reference data and change notifications. Large record sets — users, files, appointments — are served by **queries** against the database, and queries are where a growing tenant most directly meets the database's limits. The data-access layer described in [023](023_partial-data-access.md) already follows three habits that keep queries fast; tuning is mostly a matter of not breaking them.

**Habit 1 — scope every query to the tenant in the database, not in memory.** The filtered list methods build an `IQueryable` (a query description that has *not yet run*; Entity Framework turns it into SQL only when you finally enumerate it) and immediately constrain it to one tenant:

```csharp
recs = data.Users
    .Include(x => x.Department)
    .Where(x => x.TenantId == output.TenantId && x.Deleted != true
        && x.Username != null && x.Username.ToLower() != "admin");
```

Why it matters: because the `TenantId` filter is part of the `IQueryable`, it becomes a `WHERE` clause in the SQL, and the database returns only that tenant's rows. The anti-pattern to avoid is fetching everything and filtering in C# with `.ToList()` first — that pulls every tenant's rows across the wire before discarding them, and it gets linearly slower as other tenants grow. Keep filtering inside the `IQueryable`. This `TenantId` column is also the first thing to confirm is indexed in the database, since nearly every list query leads with it.

**Habit 2 — page on the server, never in the browser.** After all filters and sorting are applied, the method counts the matches and then asks the database for only one page:

```csharp
int TotalRecords = recs.Count();
output.RecordCount = TotalRecords;
// ...
if (output.Page > 1) {
    recs = recs.Skip((output.Page - 1) * output.RecordsPerPage).Take(output.RecordsPerPage);
} else {
    recs = recs.Take(output.RecordsPerPage);
}
```

`Skip` and `Take` become SQL paging, so a tenant with 50,000 users still transfers only one page (the default `RecordsPerPage` falls back to **25** when unset). The `RecordCount` total drives the page navigator without loading every row. Why it matters: page size is your most direct query-cost lever. Raising it to show "more per page" multiplies the rows materialized, the JSON serialized, and the bytes sent on *every* request — a small convenience that becomes a large, constant tax under load. Keep pages modest.

**Habit 3 — only join and search what you need.** The query pulls related data deliberately with `.Include(x => x.Department)` rather than lazily, and keyword search is built conditionally — extra columns (such as user-defined fields) are folded into the `WHERE` only when that tenant has actually enabled and indexed them for search:

```csharp
bool includeUdf01 = showUDF && UDFLabelIncludedInSearch("Users", "UDF01", udfLabels);
```

Why it matters: every column you search with `.Contains(keyword)` is a `LIKE '%...%'` the database cannot satisfy from an ordinary index, so widening the search set widens the scan. Letting tenants opt specific fields into search keeps the common case narrow. The tuning summary for queries: lead with the indexed `TenantId`, page small on the server, include exactly the related data the screen shows, and keep wildcard text search to the fewest columns that satisfy the use case.

---

<a id="verification"></a>
## 6. Verifying Improvements

A tuning change that *feels* faster on your laptop with one tenant can easily be slower in production with five hundred. Verification is the step that separates a real gain from a comfortable story, and it has to reproduce the conditions that made the system slow in the first place: concurrency and tenant breadth.

**Measure before you touch anything.** Capture a baseline of the signals from Section 2 — background request rate per client, cache-hit behavior on the hot read paths, SignalR messages per edit, and the response time of the specific query you intend to tune. Without a before-number, you cannot prove an after-improvement; you can only assert one.

**Reproduce multi-tenant load, not single-user load.** The behaviors that bite at scale are invisible with one user. Exercise several tenants at once so you can confirm the boundaries actually hold: a heavy tenant's query load should not slow a quiet tenant's pages, a change in tenant A should produce SignalR messages only to tenant A's group, and clearing or expiring one tenant's cache entries should not force others to rebuild. If you tuned a query, run it against a tenant whose table is large enough that a missing index or an in-memory filter would show — a few dozen rows will hide both.

**Confirm the change did the thing you intended, not just something.** If you raised a cache expiry, verify the hit ratio rose *and* that no screen now shows stale data past an acceptable window. If you narrowed a SignalR broadcast, verify the affected tenant still receives its updates while others no longer do. If you reduced a page size, verify response time and payload dropped while the page navigator still reports the correct total from `RecordCount`. A change that improves one metric while quietly breaking correctness is a regression wearing a success costume.

**Re-run the original complaint.** The final check is the human one: reproduce the exact slow path a user reported, under realistic load, and confirm it is now within the threshold you set. If you cannot reproduce the original slowness at all, you have not yet recreated production conditions — fix the test before trusting the result.

---

<a id="rollback"></a>
## 7. Rollback and Safe Recovery

Tuning is reversible by design, and the cheapest insurance is to change one thing at a time so that when a number goes the wrong way you know exactly what to undo. Plan the retreat before you advance.

**Cache changes are the easiest to reverse — and the easiest to reverse badly.** If a longer expiry caused stale data, set the value back and evict the affected entries so the new (correct) setting takes effect immediately. Reach for the **narrowest** eviction that fixes it: clear the single key with `CacheStore.SetCacheItem(tenantId, key, null)`, or at most the scoped helper `ClearAllUserItems()`. Avoid `CacheStore.ClearAll()` as a recovery reflex — on a loaded server it makes every tenant rebuild its cache from the database simultaneously, converting a small staleness problem into a brief database stampede. Recover gently.

**Event-traffic changes revert to the safe default: tenant scope.** If a change caused updates to reach the wrong audience, the correct state to return to is a populated `TenantId` on every per-tenant `SignalRUpdate`, so messages flow through `Clients.Group(...)` and not `Clients.All`. Because the client already runs `WithAutomaticReconnect` plus the 10-second poll as a backstop, browsers re-sync on their own once the server resumes sending correct messages — you rarely need users to reload.

**Query changes revert in code, but watch the database side.** Reverting a query edit (a wider page size, a different filter, an added search column) is a normal code change. The subtlety is the index that may have accompanied it: dropping or adding a database index is a separate operation from deploying code, so coordinate the two. Rolling code back to a state that assumed an index, on a database where the index is now gone, can be slower than either version alone.

**Keep changes small, observable, and one at a time.** The reason to tune in small, isolated steps is precisely so rollback is a one-line decision rather than an investigation. Pair every change with the metric that proves it worked (Section 6); if that metric regresses, you already know what to revert and you have the before-number to confirm the revert landed. Recovery is not a separate skill from tuning — it is the same discipline run in reverse.

---

<a id="related-docs"></a>
## 8. Related Docs

- [014 — The Living State Container](014_state-container.md) — tuning the cached lists
- [046 — Pushing State Live Over SignalR](046_realtime-signalr.md) — taming event traffic
- [023 — Inside the Partial Data-Access Layer](023_partial-data-access.md) — query performance

---
*GuidesV2 086 · drafted from source (CRM.Client/DataModel.cs, Layout/MainLayout.razor, Hubs/signalrHub.cs, DataAccess.SignalR.cs, DataObjects.SignalR.cs, Caching.cs, DataAccess.Settings.cs, DataAccess.Users.cs).*
