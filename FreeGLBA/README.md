# FreeGLBA

GLBA (Gramm-Leach-Bliley Act) compliance data-access tracking system built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). Tracks who accessed protected financial data — when, by whom, and for what purpose — and surfaces it in an access dashboard. Ships a NuGet client library (`FreeGLBA.NugetClient`) so any other application can emit GLBA access events with a single method call.

## What it does

- **Access event logging** — record who accessed which financial record, when, and why (single or bulk events)
- **Access dashboard** — access statistics, recent events, top accessors, and source-system status
- **REST API** — integrate any system via HTTP, with API-key authentication per source system
- **NuGet client** — `dotnet add package FreeGLBA.Client` gives any .NET app a typed `GlbaClient`
- **Bulk insert** — `POST api/Data/SaveAccessEvents` writes up to 1,000 events in one round trip

> **Implementation status.** The ingestion path (client → API key → validate → dedupe → store → statistics)
> is complete and working. Two features are **scaffolded but not implemented**: compliance-report
> *generation* (the `ComplianceReports` table stores report metadata, but nothing populates `ReportData`
> or `FileUrl` — there is no PDF/CSV export yet), and *real-time* dashboard push (SignalR is wired into
> the host, but no GLBA event publishes to it, so the dashboard loads on navigation rather than updating
> live). See [`Docs/002_roadmap.md`](Docs/002_roadmap.md).

## Projects

| Project | Description |
|---------|-------------|
| [`FreeGLBA`](FreeGLBA/README.md) | ASP.NET Core host; REST API, auth, SignalR, background service |
| [`FreeGLBA.Client`](FreeGLBA.Client/README.md) | Blazor WASM client; dashboard, log viewer, settings |
| [`FreeGLBA.DataAccess`](FreeGLBA.DataAccess/README.md) | Business logic; EF Core, access-event repositories, auth helpers |
| [`FreeGLBA.DataObjects`](FreeGLBA.DataObjects/README.md) | Shared DTOs; `GlbaEventRequest`, endpoint constants |
| [`FreeGLBA.EFModels`](FreeGLBA.EFModels/README.md) | EF Core DbContext; access-event log and core framework tables |
| [`FreeGLBA.NugetClient`](FreeGLBA.NugetClient/README.md) | NuGet package; `GlbaClient` for external system integration |
| [`FreeGLBA.NugetClientPublisher`](FreeGLBA.NugetClientPublisher/README.md) | Tool for publishing the NuGet package |
| [`FreeGLBA.Plugins`](FreeGLBA.Plugins/README.md) | Roslyn dynamic C# plugin runtime |
| [`FreeGLBA.TestClient`](FreeGLBA.TestClient/README.md) | Integration test client (project reference) |
| [`FreeGLBA.TestClientWithNugetPackage`](FreeGLBA.TestClientWithNugetPackage/README.md) | Integration test client (NuGet package) |

## Quick start

```bash
dotnet run --project FreeGLBA/FreeGLBA/FreeGLBA.csproj --launch-profile https
```

Boots with `DatabaseType=InMemory` — no database setup required. Navigate to
**https://localhost:7271** and sign in with **`admin` / `admin`** (seeded automatically on first run).

Use `--launch-profile http` for **http://localhost:5201** instead.

> **`LocalModeUrl` must match the port you browse on.** The app builds its own self-referencing URLs
> (login redirects, SignalR callbacks) from the `LocalModeUrl` setting. If it is empty or points at a
> different port, the Login link resolves to an unusable URL and lands on `about:blank`, and
> `[Authorize]` endpoints fail. `appsettings.Development.json` ships set to the `https` profile:
>
> ```json
> "LocalModeUrl": "https://localhost:7271/"
> ```
>
> **Keep the trailing slash** — client navigation concatenates paths onto this value without inserting
> a separator, so `…:7271` + `Login` would produce `…:7271Login`.

### Client library

```bash
dotnet add package FreeGLBA.Client
```

```csharp
var client = new GlbaClient("https://your-server.com", "your-api-key");
await client.LogAccessAsync(new GlbaEventRequest {
    UserId    = "jsmith",
    SubjectId = "S12345678",
    AccessType = "View",
    Purpose   = "Enrollment verification"
});
```

## Current state

**Working**

- Boots and serves all pages in InMemory mode; `admin`/`admin` login works out of the box
  (`SeedTestData()` runs on first database initialisation and creates the admin user and tenants)
- Event ingestion end to end: API-key auth → validation → `SourceEventId` de-duplication (409 on
  repeat) → storage → source-system and data-subject statistics
- Dashboard, Access Events, Accessors, Data Subjects, and Source Systems pages all read live data
- Access Events supports creating single events and generating bulk test data (see below)

**Not implemented yet**

- Compliance report generation — CRUD over report metadata only; no PDF or CSV output
- Real-time dashboard push — no GLBA SignalR publisher; pages load on navigation
- Anomaly detection, retention automation, RBAC for audit-log access (see the roadmap)

**Known gaps**

- `AccessEvents` has no index on `AccessedAt`, `UserId`, or `SubjectId`, and no unique constraint on
  `(SourceSystemId, SourceEventId)`. The de-duplication check is therefore a table scan, and two
  concurrent retries of the same event can both insert.
- The GLBA tables carry no `TenantId`, so reads are not tenant-filtered. Single-tenant deployments only.
- `DataSubject.UniqueAccessorCount` is set to `1` on creation and never recalculated.
- The "test suites" in `FreeGLBA.TestClient` are console applications, not a unit-test framework, and
  require a running server plus a manually provisioned API key.

## Generating test data

The Access Events page can populate the system without an external source system:

1. Create a source system under **Source Systems** (its **Generate Test Data** button fills the form).
2. Go to **Access Events** → **Generate Test Events**.
3. Choose a count (10–500), a time window (today / 7 / 30 / 90 days), how many distinct data subjects
   to draw from, and whether to include multi-subject bulk exports.

Events are generated client-side and posted in chunks of 100 to `api/Data/SaveAccessEvents`. Because
the generator draws from a fixed pool of subjects and a small pool of staff accessors, the Accessors
and Data Subjects pages show realistic repeat-access counts rather than everything having a count of 1.
Spreading events across days populates the Today / This Week / This Month dashboard tiles.

**Single event:** **New Access Event** opens the full editor, which has its own **Generate Test Data**
dropdown (single new/existing subject, or bulk across 2–10 subjects).

> In-memory mode discards all data on restart — generate demo data *after* starting the app.

## Build details

| Property | Value |
|---|---|
| Target framework | net10.0 |
| Database backends | SQL Server, PostgreSQL, SQLite, InMemory |
| Auth providers | Cookie, OpenID Connect, Microsoft, Google, Facebook, Apple |
| Real-time | SignalR available (local or Azure SignalR Service); no GLBA publisher wired up yet |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeGLBA is a compliance recorder for the **Gramm-Leach-Bliley Act** — the US law requiring institutions to track and protect access to people's financial data. Any application that opens a protected financial record calls a one-line method from FreeGLBA's NuGet client (`GlbaClient.LogAccessAsync`) to report "*user X viewed subject Y's data at time T for purpose P.*" Those events POST to the FreeGLBA server (authenticated by an API key tied to that source system), get de-duplicated and stored, and surface in a queryable dashboard with per-user and per-subject access history. Exportable report *generation* is on the roadmap; today the reports section stores report metadata only.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| NuGet client (`GlbaClient`) | One call to log an access event | [GlbaClient.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.NugetClient/GlbaClient.cs) |
| Ingestion API (`POST /api/glba/events`) | Receives events from any system | [FreeGLBA.App.GlbaController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA/Controllers/FreeGLBA.App.GlbaController.cs) |
| API-key gate | Validates the source system's Bearer token | [FreeGLBA.App.ApiKeyMiddleware.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA/Controllers/FreeGLBA.App.ApiKeyMiddleware.cs) |
| Event processing | Validate · dedupe · store · update stats | [FreeGLBA.App.DataAccess.ExternalApi.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.ExternalApi.cs) |
| Audit tables | `AccessEvent`, `SourceSystem` | [FreeGLBA.App.AccessEvent.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA.EFModels/EFModels/FreeGLBA.App.AccessEvent.cs) |

**Why does this exist?**
GLBA (and similar rules) require institutions to *prove* who accessed protected financial data and why — for audits and breach response. Instead of every application inventing its own audit log, FreeGLBA gives one central, queryable record and a drop-in client so any app complies with a single method call.

**What does it accomplish that other tools don't?**
- **One-line compliance for any app**: `dotnet add package FreeGLBA.Client`, then `client.LogAccessAsync(...)` — no bespoke audit code per system.
- **Central, deduplicated record** across many source systems, queryable by user, by subject, and by date.
- **Built for integration**: typed client with automatic retry, batch logging (up to 1,000 events), one API key per source system, and a plain REST API for non-.NET callers.

**Terminology & "can I see it?"**
- **GLBA** — Gramm-Leach-Bliley Act; mandates safeguarding consumer financial data.
- **Access event** — one record: who accessed whose data, when, how (view/export), and why.
- **Source system** — an external app registered with an API key that sends events.
- **Subject** — the person whose data was accessed (e.g. a student).
- **Dedup (`SourceEventId`)** — the client stamps each event so re-sends don't double-count.
- *See it:* the dashboard at `/GlbaDashboard`; 850 screenshots across 50 pages in `Docs/showcase/runs/latest/`.

**The hard part, drawn** — an access in some other app becomes an auditable, deduplicated record:

```
  External app (any .NET) ──▶ GlbaClient.LogAccessAsync({ User, Subject, AccessType, Purpose })
          │  POST /api/glba/events     Authorization: Bearer {api-key}
          ▼
  ┌──────────────────────── FreeGLBA server ─────────────────────────┐
  │ ApiKeyMiddleware: is this a registered Source System?  ─no─▶ 401  │
  │        │ yes                                                      │
  │        ▼ GlbaController.PostEvent                                 │
  │ DataAccess.ProcessGlbaEventAsync:                                 │
  │   • validate  · dedupe by SourceEventId  · write AccessEvent      │
  │   • bump that Source System's event statistics                    │
  └───────────────────────────────┬──────────────────────────────────┘
          │ stored (EF Core)                   │ SignalR "NewEvent"
          ▼                                     ▼
   compliance reports (PDF / CSV)        live dashboard (/GlbaDashboard)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT