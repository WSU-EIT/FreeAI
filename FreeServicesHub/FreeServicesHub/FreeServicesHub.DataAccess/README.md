# FreeServicesHub.DataAccess

The server-side data access and business logic layer for FreeServicesHub. A large partial class (`DataAccess`) spanning 39 source files provides every database operation, authentication integration, JWT handling, plugin execution, file storage, PDF generation, migrations, and utility functions consumed by the web host and controllers.

## What it does

`DataAccess` is injected as `IDataAccess` (transient). Each request gets its own instance opened against a configurable database (see below). Key functional areas:

- **Agent management** (`DataAccess.App.cs`, `FreeServicesHub.App.DataAccess.Agents.cs`) — register agents, query agent list, update agent status, track last heartbeat.
- **Heartbeat storage** (`FreeServicesHub.App.DataAccess.Heartbeats.cs`) — persist `AgentHeartbeat` records with CPU, RAM, disk metrics, and service info JSON.
- **Registration keys** (`FreeServicesHub.App.DataAccess.Registration.cs`) — validate one-time registration keys (SHA-256 hashed), issue API client tokens.
- **Job dispatch** (`FreeServicesHub.App.DataAccess.Jobs.cs`) — create, assign, and complete `HubJob` records polled by agents.
- **Authentication** (`DataAccess.Authenticate.cs`, `DataAccess.JWT.cs`) — local accounts, Active Directory/LDAP, Microsoft Graph, OAuth, JWT generation and validation.
- **Users, groups, tenants, departments** — full CRUD with soft-delete, UDF labels, and role-based access.
- **Plugins** (`DataAccess.Plugins.cs`) — store and retrieve plugin definitions; execute plugin C# code via Roslyn via `IPlugins.ExecuteDynamicCSharpCode<T>`.
- **File storage** (`DataAccess.FileStorage.cs`) — binary file BLOB storage with metadata.
- **PDF generation** (`QuestPDF`) — reports and exports.
- **Data migrations** (`DataMigrations.*.cs`) — SQL scripts applied at startup for SQL Server, SQLite, MySQL, and PostgreSQL.

Supported database back-ends: SQL Server, SQLite, MySQL, PostgreSQL, InMemory (for tests and dev).

## Key public classes

| Class | Purpose |
|---|---|
| `DataAccess` | Main partial class (implements `IDataAccess`, `IDisposable`); all DB operations |
| `IDataAccess` | Public interface exposed to DI consumers |
| `GraphAPI` | Microsoft Graph API integration for Azure AD/Entra user lookups |
| `RandomPasswordGenerator` | Cryptographically random password generation |
| `Utilities` | Internal helpers (string formatting, null-coalescing, etc.) |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — class library |
| Target framework | net10.0 |
| Database support | SQL Server · SQLite · MySQL · PostgreSQL · InMemory |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub.DataObjects` | Shared DTOs |
| `FreeServicesHub.EFModels` | EF Core entity model and `DbContext` |
| `FreeServicesHub.Plugins` | Roslyn plugin execution engine |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.Graph` | Azure AD / Entra ID user lookups |
| `Novell.Directory.Ldap.NETStandard` | LDAP/Active Directory authentication |
| `JWTHelpers` | JWT encode/decode utilities |
| `QuestPDF` | PDF report generation |
| `CsvHelper` | CSV import/export |
| `Brad.Wickett_Sql2LINQ` | Dynamic LINQ query builder |
| `Azure.Identity` | Azure managed identity credential support |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The server-side data layer. Beyond the usual FreeCRM duties (EF Core over 5 engines, auth, JWT, PDF), it owns the **fleet-monitor data**: registering agents and tracking their status/last-heartbeat, persisting each `AgentHeartbeat` (CPU/RAM/disk + service-info JSON), validating one-time registration keys (SHA-256 hashed) and issuing API tokens, and dispatching `HubJob` records that agents poll for.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Heartbeat storage | Persist each agent snapshot | [FreeServicesHub.App.DataAccess.Heartbeats.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataAccess/FreeServicesHub.App.DataAccess.Heartbeats.cs) |
| Agent registration + tokens | Validate keys, issue Bearer tokens | [FreeServicesHub.App.DataAccess.Registration.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataAccess/FreeServicesHub.App.DataAccess.Registration.cs) |
| Agent management | List/status/last-heartbeat | [FreeServicesHub.App.DataAccess.Agents.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataAccess/FreeServicesHub.App.DataAccess.Agents.cs) |
| EF Core core | All other DB I/O across 5 engines | [DataAccess.App.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataAccess/DataAccess.App.cs) |

**Why does this exist?**
To keep all the monitoring logic — *what a valid registration is, how heartbeats are stored, how jobs are dispatched* — in one server-only layer, so the hub controllers and the dashboard stay thin.

**What does it accomplish that other tools don't?**
- **Hashed one-time registration keys** (SHA-256) — the key isn't stored in the clear, and a token replaces it after enrollment.
- **Time-series heartbeat storage** ready for charting, plus a job queue for two-way agent control.
- One code path across **five** database engines.

**Terminology & "can I see it?"**
- **`AgentHeartbeat`** — one stored snapshot row (CPU/RAM/disk).
- **Registration key** — a one-time secret an agent uses to enroll, stored only as a hash.

**The hard part, drawn** — enroll once, then stream heartbeats:

```
  agent ─▶ Registration: validate one-time key (compare SHA-256) ─▶ issue ApiClientToken
  agent ─▶ Heartbeats:   save AgentHeartbeat (CPU/RAM/disk + service JSON) · update Agent.LastHeartbeat
  hub   ─▶ Jobs:         queue HubJob ─▶ agent polls ─▶ status: queued → assigned → complete
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
