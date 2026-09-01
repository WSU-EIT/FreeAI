# FreeGLBA

GLBA (Gramm-Leach-Bliley Act) compliance data-access tracking system built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). Tracks who accessed protected financial data — when, by whom, and for what purpose — and surfaces it in an access dashboard. Ships a NuGet client library (`FreeGLBA.NugetClient`) so any other application can emit GLBA access events with a single method call.

## What it does

- **Access event logging** — record who accessed which financial record, when, and why (single or bulk events)
- **Data-ownership tracking** — every source system records a **data owner** (the point of contact for the
  data itself, not the requester); each access event stores an immutable **snapshot of the owner at the time
  of access**, the source system holds the **live current owner**, and a `DataOwnerships` history table
  answers "who owned this data at any time T". The UI flags events whose data has since changed hands.
- **Access dashboard** — access statistics, a 30-day volume trend chart, recent events, top accessors,
  breakdowns by access type and data category, and source-system status — refreshed **live over
  SignalR** as events arrive
- **Anomaly detection** — a "Needs Attention" card flags per-user volume spikes, large bulk exports
  (50+ subjects), first-time accessors, after-hours access (judged in the configurable institution
  timezone), and source systems with no data owner, each with a deep link to the relevant filtered
  view (`GET api/glba/stats/insights`)
- **Webhook alerts** — large bulk accesses and (optionally) after-hours events POST immediately to a
  configured webhook as JSON with a `text` field (Slack/Teams-compatible); configured on the new
  **GLBA Settings** page (`/GlbaSettings`, admin) with a send-test button. Delivery is best-effort
  and can never break or slow the ingest path.
- **Tamper-evident audit trail** — every ingested event joins a per-source-system SHA-256 hash chain
  (`RowHash`, `PrevRowHash`, `ChainSequence`). One click on Source Systems verifies a chain and
  reports modified rows, broken links, and deletions. Editing a stored event breaks its hash *by
  design* — audit records are meant to be immutable, and verification shows exactly what changed.
- **Compliance report exports** — one click on the Compliance Reports page generates a PDF summary
  (QuestPDF) or a CSV of every event in the period, with data-owner snapshots included
- **Subject access-history export** — one click on a data subject's detail panel produces a
  DSAR/audit-style PDF of every recorded access to that person's data, including who owned the
  data at the time of each access
- **REST API** — integrate any system via HTTP, with API-key authentication per source system
- **API Explorer** — an in-app page (`/ApiExplorer`) with clickable sample requests: paste a source-system
  API key, send real events, and watch them appear on the dashboard
- **NuGet client** — `dotnet add package FreeGLBA.Client` gives any .NET app a typed `GlbaClient`
- **Bulk insert** — `POST api/Data/SaveAccessEvents` writes up to 1,000 events in one round trip

> **Implementation status.** The ingestion path (client → API key → validate → dedupe → store → statistics
> → SignalR push) and compliance-report generation (QuestPDF summary + full CSV detail export, downloaded
> from the Compliance Reports page) are complete and working. See [`Docs/002_roadmap.md`](Docs/002_roadmap.md)
> for what remains.

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
| [`FreeGLBA.Tests`](FreeGLBA.Tests/GlbaCoreTests.cs) | xUnit suite (21 tests) over the real data layer — no server needed |
| [`FreeGLBA.Showcase`](FreeGLBA.Showcase/README.md) | Playwright demo/verification runner (seeds data, screenshots, validates downloads) |
| [`FreeGLBA.TestClient`](FreeGLBA.TestClient/README.md) | Integration test client (project reference) |
| [`FreeGLBA.TestClientWithNugetPackage`](FreeGLBA.TestClientWithNugetPackage/README.md) | Integration test client (NuGet package) |

> **New here?** [`Docs/HANDOFF.md`](Docs/HANDOFF.md) is the engineering handoff for the
> September 2026 overhaul — what was built and why, how each piece works, every bug found,
> deployment caveats, and the prioritized remaining work.

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
  repeat) → data-owner snapshot capture → storage → source-system and data-subject statistics →
  SignalR publish
- Real-time updates: the dashboard and the Access Events, Accessors, Data Subjects, and Source
  Systems pages refresh in place when events arrive (batches are coalesced into one refresh)
- Data ownership: owner fields + full ownership history per source system; owner-at-time-of-access
  snapshot on every event; "ownership has changed hands" indicator on event detail views
- Access Events supports creating single events and generating bulk test data (see below)
- API Explorer page for interactive, conference-demo-style API calls against the running server

- Compliance reports generate real content: a **PDF summary** (statistics, access-type and category
  breakdowns, top accessors, source systems with current data owners, 16 CFR 314.4(c)(8) citation)
  and a **CSV detail export** of every event in the period, including the data-owner snapshot columns
- All `api/Data/*` GLBA endpoints now require a signed-in, enabled user; configuration and
  destructive operations require Admin (they previously had no auth checks at all)

**Not implemented yet**

- Anomaly detection, retention automation, tamper-evidence, SIEM export (see the roadmap)

**Known gaps**

- On MySQL and InMemory only, `(SourceSystemId, SourceEventId)` deduplication remains a check-then-insert
  (no filtered unique index support), so two concurrent retries can race past it. SQL Server,
  PostgreSQL, and SQLite enforce a filtered **unique** index and report the loser as a 409 duplicate.
- The GLBA tables carry no `TenantId`, so reads are not tenant-filtered. Single-tenant deployments only.
- In bulk ingest paths, `UniqueAccessorCount` is recomputed from direct-match events only (multi-subject
  JSON events are excluded from the grouped query), so it is a floor that never regresses; the
  single-event path computes it exactly.
- Dashboard period tiles and insights use UTC day boundaries; an institution-timezone setting (and
  after-hours detection built on it) is on the roadmap.

**Tests**

`FreeGLBA.Tests` is an xUnit suite (21 tests) that runs the real `DataAccess` over the EF InMemory
provider — no server, no API key, CI-friendly:

```bash
dotnet test FreeGLBA.Tests
```

It covers the ownership lifecycle (snapshot, history, change detection), deduplication, exact
subject statistics on insert and delete, the source-system delete guard, PDF/CSV report generation,
the subject access-history export, the insights endpoint, GLBA settings sanitization, and the
tamper-evident hash chain — including a direct-database tamper simulation and deletion-gap
detection. The console apps in `FreeGLBA.TestClient*` remain as live end-to-end
clients against a running server.

**Upgrading an existing (non-InMemory) database**

The schema is created by `EnsureCreated()` on first run, so *fresh* databases pick up the new columns
automatically. Existing SQL databases need these additions (SQL Server shown; adjust types for
PostgreSQL/MySQL/SQLite, where GUIDs are stored as strings):

```sql
ALTER TABLE SourceSystems ADD DataOwnerName nvarchar(200) NOT NULL DEFAULT '',
    DataOwnerEmail nvarchar(200) NOT NULL DEFAULT '', DataOwnerDepartment nvarchar(200) NOT NULL DEFAULT '',
    DataOwnerPhone nvarchar(50) NOT NULL DEFAULT '', DataOwnerAssignedAt datetime2 NULL;
ALTER TABLE AccessEvents ADD DataOwnerName nvarchar(200) NOT NULL DEFAULT '',
    DataOwnerEmail nvarchar(200) NOT NULL DEFAULT '', DataOwnerDepartment nvarchar(200) NOT NULL DEFAULT '',
    ChainSequence bigint NOT NULL DEFAULT 0,
    PrevRowHash nvarchar(100) NOT NULL DEFAULT '', RowHash nvarchar(100) NOT NULL DEFAULT '';
-- Existing rows keep ChainSequence 0 = "recorded before integrity chaining" (reported by
-- verification as unhashed, not as an error).
CREATE TABLE DataOwnerships (
    DataOwnershipId uniqueidentifier NOT NULL PRIMARY KEY,
    SourceSystemId uniqueidentifier NOT NULL,
    OwnerName nvarchar(200) NOT NULL DEFAULT '', OwnerEmail nvarchar(200) NOT NULL DEFAULT '',
    OwnerDepartment nvarchar(200) NOT NULL DEFAULT '', OwnerPhone nvarchar(50) NOT NULL DEFAULT '',
    AssignedAt datetime2 NOT NULL, EndedAt datetime2 NULL,
    AssignedBy nvarchar(200) NOT NULL DEFAULT '', Notes nvarchar(max) NOT NULL DEFAULT '',
    CONSTRAINT FK_DataOwnerships_SourceSystems FOREIGN KEY (SourceSystemId)
        REFERENCES SourceSystems (SourceSystemId) ON DELETE CASCADE);
CREATE INDEX IX_DataOwnerships_SourceSystemId ON DataOwnerships (SourceSystemId);
CREATE INDEX IX_AccessEvents_AccessedAt ON AccessEvents (AccessedAt);
CREATE INDEX IX_AccessEvents_UserId ON AccessEvents (UserId);
CREATE INDEX IX_AccessEvents_SubjectId ON AccessEvents (SubjectId);
-- On SQL Server / PostgreSQL / SQLite use the filtered UNIQUE variant (matches what
-- EnsureCreated builds); on MySQL keep it non-unique:
CREATE UNIQUE INDEX IX_AccessEvents_SourceSystemId_SourceEventId
    ON AccessEvents (SourceSystemId, SourceEventId) WHERE SourceEventId <> '';
```

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
| Real-time | SignalR (local or Azure SignalR Service); GLBA events publish `GlbaAccessEvent` / `GlbaSourceSystem` updates and the UI refreshes live |

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
The GLBA Safeguards Rule (16 CFR 314.4(c)(8)) requires institutions to *monitor and log the activity of authorized users* on customer financial information — the who/what/when — and auditors also expect the business purpose and a responsible point of contact. Instead of every application inventing its own audit log, FreeGLBA gives one central, queryable record and a drop-in client so any app complies with a single method call — and it tracks **who owns the data** (at the time of each access, and now), not just who requested it.

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