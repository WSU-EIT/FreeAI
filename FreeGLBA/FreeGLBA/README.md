# FreeGLBA

Main server application for the FreeGLBA GLBA Compliance Data Access Tracking System. Hosts the ASP.NET Core web application, REST API, Blazor Server rendering, SignalR hub, and background service.

## What It Does

This project is the entry point and HTTP host. On startup it:
- Loads plugins from the `PluginFiles/` directory using Roslyn runtime compilation
- Registers `IDataAccess` as a transient service bound to the configured database connection string and type
- Configures authentication (OpenID Connect, OAuth for Google/Microsoft/Apple/Facebook, and custom cookie-based auth)
- Starts the optional background processor (`BackgroundProcessor`) if enabled in `appsettings.json`
- Mounts the Blazor WebAssembly client assembly alongside SSR Razor components
- Exposes OpenAPI documentation via Scalar at `/scalar/glba-api` (development only)
- Applies API key middleware (`ApiKeyMiddleware`) that validates `Authorization: Bearer` tokens for external GLBA event endpoints
- Configures Azure SignalR or local SignalR depending on whether `AzureSignalRurl` is set

## @page Routes (Blazor)

GLBA-specific pages (all support optional `/{TenantCode}/` prefix):

| Route | Purpose |
|-------|---------|
| `/GlbaDashboard` | GLBA compliance dashboard — stats, recent events, source systems, top accessors |
| `/AccessEvents` | Paginated access event log with detail drill-down (`/AccessEvents/{guid}`) |
| `/Accessors` | Top data accessors report |
| `/DataSubjects` | Data subjects registry with per-subject event history |
| `/SourceSystems` | Registered source system management |
| `/ComplianceReports` | Audit-ready compliance report generation |
| `/ApiLogs` | API request log viewer |
| `/ApiLogs/Dashboard` | API log dashboard |
| `/ApiLogs/Settings` | Body-logging configuration |
| `/ApiLogs/{guid}` | Single API request log detail |

Standard platform pages (login, settings, users, departments, etc.) are inherited from the client project.

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `Program.Main` | Entry point; wires DI, middleware, SignalR, and Blazor rendering |
| `GlbaController.PostEvent` | `POST /api/glba/events` — logs a single GLBA access event (API key auth) |
| `GlbaController.PostBatch` | `POST /api/glba/events/batch` — batch event logging (up to 1,000 events) |
| `GlbaController.GetStats` | `GET /api/glba/stats/summary` — dashboard aggregate statistics (user JWT auth) |
| `GlbaController.GetRecentEvents` | `GET /api/glba/events/recent` — recent access event feed |
| `ApiKeyMiddleware.InvokeAsync` | Validates Bearer token against registered source systems before POST event endpoints |
| `BackgroundProcessor` | Hosted service for periodic tasks; supports load-balancing filter via `MachineName` |
| `CustomAuthenticationHandler` | Cookie-based custom authentication handler for non-OAuth login flows |

## Project References

| Reference | Role |
|-----------|------|
| `FreeGLBA.Client` | Blazor WebAssembly UI — served as interactive client-side assembly |
| `FreeGLBA.DataAccess` | All database and business logic |
| `FreeGLBA.Plugins` | Runtime plugin loader (Roslyn-based) |

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | WSU OpenID Connect / Okta integration |
| `Microsoft.AspNetCore.Authentication.*` | OAuth providers (Google, Microsoft, Apple, Facebook) |
| `Microsoft.Azure.SignalR` | Azure SignalR Service (falls back to local when unconfigured) |
| `Scalar.AspNetCore` | OpenAPI documentation UI at `/scalar/glba-api` |
| `Serilog.Extensions.Logging.File` | File-based structured logging |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target Framework | `net10.0` |
| Output Type | Web executable |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The web server. It serves the Blazor dashboard, but its compliance-critical job is the **GLBA ingestion API**: a middleware checks every incoming event's `Authorization: Bearer` API key against the registered source systems, then the controller hands the event to the data layer to validate, de-duplicate, and store. It also runs SignalR (live dashboard), loads Roslyn plugins, and exposes OpenAPI docs (Scalar) in development.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| API-key middleware | Authenticate each source system | [FreeGLBA.App.ApiKeyMiddleware.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA/Controllers/FreeGLBA.App.ApiKeyMiddleware.cs) |
| GLBA event endpoints | `POST events` / `events/batch` / `stats` | [FreeGLBA.App.GlbaController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA/Controllers/FreeGLBA.App.GlbaController.cs) |
| Host wiring | DI, auth, SignalR, OpenAPI (Scalar) | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeGLBA/FreeGLBA/Program.cs) |

**Why does this exist?**
A single host that securely *receives* events from many external systems, serves the live dashboard, and exposes the reports — the compliance hub everything else points at.

**What does it accomplish that other tools don't?**
- **Per-source-system API keys** validated in middleware *before* the controller runs — unregistered callers never touch the data layer.
- **Batch ingestion** (up to 1,000 events) and **two auth modes**: API key for ingestion, user JWT for the dashboard.
- Built-in **Scalar OpenAPI** docs at `/scalar/glba-api` for integrators.

**Terminology & "can I see it?"**
- **Middleware** — code that runs on every request before the controller (here, the API-key check).
- **Bearer token** — the API key sent in the `Authorization` header.

**The hard part, drawn** — the guarded ingestion path:

```
  POST /api/glba/events   (Authorization: Bearer {api-key})
        │
        ▼  ApiKeyMiddleware  ── not a registered Source System? ──▶ 401 Unauthorized
        │  registered ✓ (attaches SourceSystem to the request)
        ▼  GlbaController.PostEvent ──▶ DataAccess.ProcessGlbaEventAsync ──▶ EF Core
        └──────────────────────────────────────────────────────────▶ SignalR → live dashboard
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeGLBA](../README.md) solution.
