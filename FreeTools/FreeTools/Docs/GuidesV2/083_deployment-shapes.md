# 083 — Shipping It to Production

> **Document ID:** 083  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Guide deploying the WebAssembly client and API across the supported tenancy topologies and their operational costs.
> **Audience:** Operators and maintainers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Deployment Shape Matters](#why-it-matters) | Plain-language overview; what "one app hosts both halves" means for you |
| 2 | [The Supported Tenancy Topologies](#topologies) | The single deployable artifact and the shared-vs-isolated shapes it supports |
| 3 | [Choosing the Right Shape](#choosing) | Mapping isolation, blast-radius, and cost needs to a topology |
| 4 | [Deploying the WASM Client and API](#procedure) | The real publish-and-IIS rollout, settings, and the background service |
| 5 | [Verifying a Healthy Deployment](#verification) | Post-deploy checks using the version metadata and startup error codes |
| 6 | [Operational Costs and Scaling](#costs) | Database, SignalR, and load-balancing cost drivers per shape |
| 7 | [Rollback and Recovery](#rollback) | Reverting a bad deploy when one binary serves everyone |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Deployment Shape Matters

**Why it matters:** The single biggest surprise for a first-time operator of this framework is that there is *not* a separate "frontend deployment" and "backend deployment." There is **one** thing you publish, and it serves both. Get that mental model right and the rest of this document is easy. Get it wrong and you will waste a day looking for an API server that does not exist.

Let me define the jargon up front, in plain language:

- **WASM (WebAssembly)** is a way to run real compiled code — here, C# — *inside the user's browser* instead of on your server. The framework's client project (`CRM.Client`) is built with the `Microsoft.NET.Sdk.BlazorWebAssembly` SDK, so the user's browser downloads the app once and then runs it locally. That is why the screens feel fast after the first load.
- **API (Application Programming Interface)** is the set of server endpoints the browser calls to read and write data. In this framework the API lives in the *same* server project (`CRM`, built with `Microsoft.NET.Sdk.Web`) that also hands the WASM client to the browser.
- **Tenancy** means how many separate customers (companies, teams, organizations) one running copy of the app serves. A **tenant** is one such customer with its own walled-off data.
- **Topology** is the *shape* of your deployment: how many copies of the server you run, how many databases you point them at, and how tenants map onto them.

The load-bearing fact: the server project references the client project directly and bootstraps it as an interactive WASM component. From `CRM/Program.cs`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
...
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
```

`AddInteractiveWebAssemblyRenderMode()` is the line that says "this page's interactivity runs as WebAssembly in the browser." `AddAdditionalAssemblies(typeof(Client._Imports).Assembly)` is the line that pulls the client project's pages into the same host. So when you "deploy the WASM client," you are really deploying *the server*, which carries the client inside it. There is no second box to stand up.

The practical consequence — and the reason topology matters — is that **every tenant is served by the same published binary**. Isolation between customers is a property of *data and configuration*, not of separate deployments. So your job at deploy time is less "wire up two tiers" and more "decide how many databases and how many server instances, then point the one artifact at them correctly."

---

<a id="topologies"></a>
## 2. The Supported Tenancy Topologies

Because there is exactly one deployable artifact, the "topologies" are really *configurations* of that artifact. There are three practical shapes. (Doc [082](082_tenancy-topology.md) covers the single-vs-many decision in depth; this section covers what each shape looks like *on a server*.)

### Shape A — One instance, many tenants (shared)
One published copy of `CRM` runs, points at one database, and serves all tenants. Tenants live side by side as rows in shared tables, keyed by a tenant id. The background service walks **every** enabled tenant on each pass — see `BackgroundProcessor.GetTasksToProcess()`:

```csharp
var tenants = await da.GetTenants();
...
foreach (var tenant in tenants.Where(x => x.Enabled == true)) {
    ...
    var appTasksForTenant = await da.ProcessBackgroundTasksApp(tenant.TenantId, _iterations);
    ...
}
```

This is the lowest-cost, lowest-isolation shape: one binary, one database, one set of logs. Tenants are distinguished at request time (the framework parses the request path into action/id segments via `RouteHelper`, and resolves a tenant by its code through `GetTenantIdFromCode`).

### Shape B — One instance, one tenant (dedicated)
The same single artifact, but pointed at a database that holds just one customer. You get strong data isolation (a customer's data is *physically* its own database/server) at the cost of running and patching one deployment per customer. Operationally identical to Shape A from the server's point of view — only the connection string and the set of tenants differ.

### Shape C — Many instances, shared data (scaled-out / load-balanced)
Several copies of the *same* published artifact run behind a load balancer, all pointing at the same database, to handle more traffic or to survive a node failure. Two framework features exist specifically for this shape:

1. **Background-service election.** You usually want the periodic background work to run on exactly one instance, not all of them. The `LoadBalancingFilter` setting gates this. From `Program.cs`:

   ```csharp
   var loadBalancingFilter = String.Empty + builder.Configuration.GetValue<string>("BackgroundService:LoadBalancingFilter");
   if (!String.IsNullOrWhiteSpace(loadBalancingFilter)) {
       var hostname = (String.Empty + System.Environment.MachineName).ToLower();
       backgroundServiceEnabled = hostname.Contains(loadBalancingFilter.ToLower());
   }
   ```

   Set `LoadBalancingFilter` to a substring that appears in only one machine's name, and only that node runs the background processor.

2. **Backplane for real-time updates.** The app pushes live updates over **SignalR** (a real-time messaging library) through a hub registered at `/crmHub`. With multiple instances you need a shared backplane so a message raised on node 1 reaches a user connected to node 2. The framework supports Azure SignalR Service via the `AzureSignalRurl` setting:

   ```csharp
   if (String.IsNullOrWhiteSpace(azureSignalRUrl)) {
       builder.Services.AddSignalR();                 // single-instance, in-process
   } else {
       builder.Services.AddSignalR().AddAzureSignalR("Endpoint=" + azureSignalRUrl);  // multi-instance
   }
   ```

   Leave `AzureSignalRurl` empty for a single instance; set it when you scale out.

The three shapes are not mutually exclusive — a real deployment is often "Shape A plus Shape C" (many shared tenants, scaled across nodes) or "Shape B per big customer."

---

<a id="choosing"></a>
## 3. Choosing the Right Shape

**Why it matters:** picking a shape is mostly picking how much isolation you are willing to pay for. Use these criteria, in order:

1. **Regulatory / contractual isolation.** If a customer's contract or your regulator demands that their data never share a database with anyone else, you need **Shape B (dedicated)** for that customer. There is no cheaper way to make that guarantee true; data isolation in Shape A is logical (a tenant id column), not physical.

2. **Blast radius tolerance.** "Blast radius" is how many customers a single bad event hurts. In Shape A, one bad migration or one runaway query can affect everyone on that database. In Shape B, the blast radius is one customer. If a single noisy or fragile customer would jeopardize others, isolate them.

3. **Traffic and availability.** If one server can comfortably carry your load and a few minutes of downtime during a deploy is acceptable, stay single-instance (Shape A or B). If you need to survive a node failure or exceed one machine's capacity, add **Shape C (scale-out)** — remembering that Shape C *requires* you to configure `LoadBalancingFilter` and, for live updates across nodes, `AzureSignalRurl`.

4. **Operational headcount.** Every dedicated deployment (Shape B) multiplies your patching, monitoring, and upgrade work by the number of tenants. Because one artifact serves everyone in Shape A, the shared shape is dramatically cheaper to *operate*, not just to host. Be honest about how many separate deployments your team can actually keep patched.

A reasonable default for most teams: **Shape A** (shared, single instance) until isolation or scale forces your hand, promoting individual large or regulated customers to **Shape B**, and adding **Shape C** only when uptime or capacity demands it. The same published binary serves all three, so you are never locked in — moving a tenant between shapes is a data-and-configuration move, not a re-platforming.

---

<a id="procedure"></a>
## 4. Deploying the WASM Client and API

**Why it matters:** because the client and API are one artifact, the rollout is a single publish followed by a single copy to your web server. Here is the real, ordered procedure for IIS (the hosting model the framework's `web.config` is written for).

### Step 1 — Set the configuration

Edit `CRM/appsettings.json` (or environment-specific overrides) before you publish. The settings that actually change deployment behavior:

- **`DatabaseType`** — one of `InMemory`, `MySQL`, `PostgreSQL`, `SQLite`, or `SQLServer`. This selects the EF Core provider at startup in `DataAccess.cs` (e.g. `optionsBuilder.UseSqlServer(...)`). **Do not ship `InMemory` to production** — its data evaporates on restart.
- **`ConnectionStrings:AppData`** — the database the chosen provider connects to. This is what makes a deployment Shape A/B (shared vs. dedicated DB).
- **`BasePath`** — set this only if the app is served from a sub-path rather than the site root; it feeds the `<base href>` the WASM client uses to resolve URLs (see `App.razor`).
- **`AzureSignalRurl`** — empty for single instance; set for Shape C.
- **`BackgroundService`** — `Enabled`, `LoadBalancingFilter`, `LogFilePath`, `ProcessingIntervalSeconds` (default 60), and `StartOnLoad`. For Shape C, set `LoadBalancingFilter` so only one node runs it.

### Step 2 — Publish the server project (it carries the client)

Publish `CRM` in Release. This compiles the WASM client, fingerprints the static assets, and emits a self-contained web app including `CRM.exe`, the `_framework` WASM payload, and the `web.config`:

```
dotnet publish CRM/CRM.csproj -c Release -o C:\Publish\CRM
```

You publish *only* the server project — its `.csproj` references `CRM.Client`, so the WASM bits come along automatically. There is no separate client publish step.

### Step 3 — Copy to the web server and let IIS host it

The published `web.config` already configures IIS to run the app **in-process** through the ASP.NET Core Module v2 and to serve `.wasm` correctly:

```xml
<aspNetCore processPath=".\CRM.exe" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
...
<staticContent>
  <remove fileExtension=".wasm" />
  <mimeMap fileExtension=".wasm" mimeType="application/wasm" />
</staticContent>
```

It also raises the upload limit (`maxAllowedContentLength="104857600"`, i.e. 100 MB) for file features. Point an IIS site/application at the published folder; the module launches `CRM.exe` for you.

### Step 4 — Keep the background service alive (if enabled)

The background service is a hosted timer inside the same process; if IIS lets the worker process idle out, the service stops. Per the project README, on IIS set the Application Pool **Start Mode** to `AlwaysRunning` and turn on **Preload Enabled** for the application (the Application Initialization IIS feature must be installed). For Shape C, also confirm `LoadBalancingFilter` so the service runs on exactly one node.

### Step 5 — First request triggers database setup

You do not run a separate migration step. On the first request the data layer connects and, for a real provider, applies the latest schema automatically. From `DataAccess.cs`:

```csharp
if (data.Database.CanConnect()) {
    _open = true;
    if (!_inMemoryDatabase && _useMigrations) {
        DatabaseApplyLatestMigrations();
    }
} else {
    // Try and create the database using the built-in EF command
    data.Database.EnsureCreated();
    ...
}
...
SeedTestData();   // ensures default/seed data exists and is current
```

So the deploy is: configure, publish, copy, ensure the pool stays warm — then hit the site once and let it bring the database up to date.

---

<a id="verification"></a>
## 5. Verifying a Healthy Deployment

**Why it matters:** a deploy that *starts* is not the same as a deploy that *works*. The framework gives you concrete, source-backed signals to check.

1. **The page renders and reports its build.** Every page emits server identity and version metadata in `<head>` (from `App.razor`):

   ```html
   <meta name="app:server" content="@System.Environment.MachineName" />
   <meta name="app:version" content="@data.Version" />
   <meta name="app:released" content="@data.Released" />
   ```

   View-source on the live site and confirm `app:version` matches the build you just shipped — this is your fastest "did the new bits actually deploy?" check. In Shape C, `app:server` (the machine name) confirms *which* node answered, which is exactly what `LoadBalancingFilter` keys off of.

2. **No startup error banner.** If the database is misconfigured, the data layer does not crash silently — it records a coded startup error instead. The two you will see most:
   - `MissingConnectionString` — `ConnectionStrings:AppData` or `DatabaseType` was blank.
   - `DatabaseOffline` — the connection string is present but the database could not be reached (the exception messages are captured in `GlobalSettings.StartupErrorMessages`).

   A clean deploy shows neither.

3. **The WASM payload loads.** In the browser dev-tools Network tab, confirm `_framework/*.wasm` files return `200` with `Content-Type: application/wasm`. A `404` or wrong MIME type here means IIS is not serving WASM — usually a missing/overwritten `web.config`.

4. **Real-time updates work.** Confirm the SignalR connection to `/crmHub` is established (dev-tools shows the WebSocket). In Shape C, change data as a user on one node and confirm a user on another node sees it — that proves your `AzureSignalRurl` backplane is wired up.

5. **The background service ran.** If `BackgroundService:Enabled` is true, check the console/log (or the file at `LogFilePath`) for `"Background Processor is starting."`. In Shape C, confirm that message appears on **only** the elected node.

---

<a id="costs"></a>
## 6. Operational Costs and Scaling

**Why it matters:** cost here is driven by *how many of each thing you run*, and the three shapes differ mostly in count. Use this to plan capacity and budget.

| Cost driver | Shape A (shared) | Shape B (dedicated) | Shape C (scaled-out) |
|---|---|---|---|
| Server instances to run & patch | 1 | 1 **per tenant** | N (all the same binary) |
| Databases to back up & migrate | 1 | 1 per tenant | 1 shared |
| SignalR | in-process (free) | in-process (free) | Azure SignalR Service (paid, `AzureSignalRurl`) |
| Background service runs | 1 process | 1 process per tenant | 1 elected node (`LoadBalancingFilter`) |
| Blast radius of an incident | all tenants | one tenant | all tenants on that DB |

Practical capacity notes grounded in the code:

- **Background work scales with tenant count, not request count.** Each pass loops over every enabled tenant and, periodically, purges soft-deleted records and stale cached plugin binaries (`DeleteAllPendingDeletedRecords`, `DeleteOldBlazorCachedPluginBinaries`). More tenants in one database means longer passes; tune `ProcessingIntervalSeconds` accordingly.
- **The database is the shared bottleneck.** In Shapes A and C everything funnels into one `AppData` database. EF Core is configured with `EnableRetryOnFailure()` for SQL Server, MySQL, and PostgreSQL, which buys resilience against transient blips but does not add capacity. Scale the database before adding more app nodes.
- **Scaling out is cheap on the app tier, not free on the real-time tier.** Adding app instances (Shape C) is just running the same binary more times — but cross-node live updates require the paid Azure SignalR backplane. Budget for it the moment you go past one instance.
- **Static assets are served by the same process.** The published app serves its fingerprinted WASM and `_content/*` assets via `MapStaticAssets()`. For heavy read traffic, fronting the site with a CDN/reverse proxy reduces load on the app tier without changing the deployment shape.

---

<a id="rollback"></a>
## 7. Rollback and Recovery

**Why it matters:** when one binary serves everyone (Shape A/C), a bad deploy is an *everyone* problem, so a fast, rehearsed rollback is your safety net. The single-artifact model that makes deploys simple also makes rollback simple — *as long as you account for the database*.

**Before you deploy:**

- **Keep the previous published folder.** Because a "deploy" is a folder of files plus `CRM.exe`, the cleanest rollback is swapping the folder back. Publish into versioned directories (e.g. `C:\Publish\CRM_2026-06-05`) and keep the last known-good one.
- **Back up the database first.** This is the non-negotiable step. The app applies the latest migrations on first request (`DatabaseApplyLatestMigrations()`), so a deploy can change schema. Reverting the *code* does not revert the *schema*. A database backup taken immediately before deploy is your real rollback point.

**To roll back:**

1. Repoint the IIS site (or load-balancer pool) back to the previous published folder, or restore that folder over the current one. Recycle the app pool so `CRM.exe` restarts on the old bits.
2. If the failed deploy applied a schema change that the old code cannot tolerate, restore the database from the pre-deploy backup. (If the new schema is backward-compatible — the common case for additive migrations — you can often skip this and just revert the code.)
3. In **Shape C**, roll all nodes back to the same version. Mixed versions behind one load balancer talking to one database is a recipe for confusing intermittent failures — keep the fleet uniform. The `app:version` meta tag (see §5) lets you confirm every node is on the same build.

**Recovering from a startup failure rather than a bad release:** if the site comes up showing a `DatabaseOffline` or `MissingConnectionString` startup error, the binary is fine — fix the configuration (connection string, `DatabaseType`, database availability) and recycle the pool. No rollback of code is needed; the data layer re-runs its startup sequence on the next request.

---

<a id="related-docs"></a>
## 8. Related Docs

- [082 — One Tenant or Many](082_tenancy-topology.md) — the topology you are shipping
- [002 — Getting .NET 10 and the Toolchain to Cooperate](002_toolchain-prereqs.md) — the runtimes involved
- [084 — Riding the Framework Forward](084_performing-upgrades.md) — and later, upgrades

---
*GuidesV2 083 · drafted from source (FreeCRM `CRM`/`CRM.Client`, `Program.cs`, `web.config`, `appsettings.json`, `BackgroundProcessor.cs`, `DataAccess.cs`) · 2026-06-05.*
