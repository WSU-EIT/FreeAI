# FreeGLBA

GLBA (Gramm-Leach-Bliley Act) compliance data-access tracking system built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). Tracks who accessed protected financial data — when, by whom, and for what purpose — and provides audit-ready reports and a real-time access dashboard. Ships a NuGet client library (`FreeGLBA.NugetClient`) so any other application can emit GLBA access events with a single method call.

## What it does

- **Access event logging** — record who accessed which financial record, when, and why (single or bulk events)
- **Real-time dashboard** — live access statistics and pattern monitoring via SignalR
- **Compliance reports** — exportable audit reports for GLBA review
- **REST API** — integrate any system via HTTP
- **NuGet client** — `dotnet add package FreeGLBA.Client` gives any .NET app a typed `GlbaClient`

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
cd FreeGLBA/FreeGLBA
dotnet run
```

Boots with `DatabaseType=InMemory` — no database setup required. Navigate to `http://localhost:5001`.

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

- Boots and serves all pages in InMemory mode
- Full multi-page crawl captured: **903 screenshots across 50 pages** in `Docs/showcase/runs/latest/`
- Login with `admin`/`admin` requires seeded data — public pages are accessible without authentication
- Documentation complete: see [`Docs/`](Docs/)

## Build details

| Property | Value |
|---|---|
| Target framework | net10.0 |
| Database backends | SQL Server, PostgreSQL, SQLite, InMemory |
| Auth providers | Cookie, OpenID Connect, Microsoft, Google, Facebook, Apple |
| Real-time | SignalR (local or Azure SignalR Service) |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeGLBA is a compliance recorder for the **Gramm-Leach-Bliley Act** — the US law requiring institutions to track and protect access to people's financial data. Any application that opens a protected financial record calls a one-line method from FreeGLBA's NuGet client (`GlbaClient.LogAccessAsync`) to report "*user X viewed subject Y's data at time T for purpose P.*" Those events POST to the FreeGLBA server (authenticated by an API key tied to that source system), get de-duplicated and stored, and surface in a real-time dashboard and exportable, audit-ready compliance reports.

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
- **Central, deduplicated record** across many source systems, with a live dashboard and audit-ready PDF/CSV reports.
- **Built for integration**: typed client with automatic retry, batch logging (up to 1,000 events), one API key per source system, and a plain REST API for non-.NET callers.

**Terminology & "can I see it?"**
- **GLBA** — Gramm-Leach-Bliley Act; mandates safeguarding consumer financial data.
- **Access event** — one record: who accessed whose data, when, how (view/export), and why.
- **Source system** — an external app registered with an API key that sends events.
- **Subject** — the person whose data was accessed (e.g. a student).
- **Dedup (`SourceEventId`)** — the client stamps each event so re-sends don't double-count.
- *See it:* the dashboard at `/GlbaDashboard`; 903 screenshots in `Docs/showcase/runs/latest/`.

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