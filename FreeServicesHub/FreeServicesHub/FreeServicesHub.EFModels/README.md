# FreeServicesHub.EFModels

The Entity Framework Core model library for FreeServicesHub. Owns the `EFDataModel` `DbContext`, all entity classes, and the EF migrations. Supports SQL Server, SQLite, MySQL, PostgreSQL, and InMemory back-ends.

## What it contains

### DbContext — `EFDataModel`

`EFDataModel` declares `DbSet<T>` properties for every entity and configures column types, max lengths, and relationships in `OnModelCreating`.

### Entity tables

| Entity | Description |
|---|---|
| `Agent` | Registered agent: hostname, OS, architecture, agent version, status, last heartbeat |
| `AgentHeartbeat` | Time-series heartbeat row: CPU %, RAM GB, disk metrics JSON, service info JSON |
| `ApiClientToken` | Bearer tokens issued to agents after registration |
| `RegistrationKey` | One-time keys (SHA-256 hashed) that agents use to self-register |
| `HubJob` | Jobs dispatched to agents: type, payload, status, result, timestamps |
| `CiCdTokenUsage` | Audit log for CI/CD pipeline token consumption |
| `Tenant` | Multi-tenant isolation root |
| `User` | User account with roles, lockout, and soft-delete |
| `UserGroup` / `UserInGroup` | Group membership |
| `Department` / `DepartmentGroup` | Org-chart department hierarchy |
| `Setting` | Key-value settings with typed storage (text, encrypted, JSON, etc.) |
| `Tag` / `TagItem` | Tagging system for any entity |
| `FileStorage` | Binary file blobs with metadata |
| `PluginCache` | Compiled plugin cache for Roslyn-evaluated plugins |
| `UDFLabel` | User-defined field labels |

### Migrations

`Migrations/20260416220434_initial` — initial schema for all tables above, applied at startup by `DataAccess.Migrations.cs`.

## Key public classes

| Class | Purpose |
|---|---|
| `EFDataModel` | `DbContext`; all `DbSet<T>` properties and `OnModelCreating` configuration |
| `Agent` | EF entity for registered agents |
| `AgentHeartbeat` | EF entity for heartbeat time-series data |
| `RegistrationKey` | EF entity for hashed one-time registration keys |
| `HubJob` | EF entity for agent job dispatch records |
| `EFModelOverrides` | Partial class extension point for app-specific EF configuration |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — class library |
| Target framework | net10.0 |
| EF tooling | `Microsoft.EntityFrameworkCore.Tools` (private) |

## Project references

None (no project references — referenced by DataAccess).

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite provider |
| `MySql.EntityFrameworkCore` | MySQL provider |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory provider for tests and dev |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library *defines the database*. Alongside core FreeCRM tables it adds the **fleet-monitor tables**: `Agent` (each registered machine), `AgentHeartbeat` (a time-series row per heartbeat), `RegistrationKey` (one-time, SHA-256-hashed enrollment keys), `ApiClientToken` (Bearer tokens issued to agents), `HubJob` (dispatched jobs), and `CiCdTokenUsage` (pipeline-token audit). One model targets five engines, with an initial migration applied at startup.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core DbContext | The schema (core + agent tables) | [FreeServicesHub.App.EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.EFModels/EFModels/FreeServicesHub.App.EFDataModel.cs) |
| Agent entity | The registered machine record | [FreeServicesHub.App.Agent.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.EFModels/EFModels/FreeServicesHub.App.Agent.cs) |
| Heartbeat entity (time-series) | One row per heartbeat | [FreeServicesHub.App.AgentHeartbeat.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.EFModels/EFModels/FreeServicesHub.App.AgentHeartbeat.cs) |

**Why does this exist?**
One schema that stores the fleet's enrollment, heartbeats, and jobs on whatever database the institution already runs.

**What does it accomplish that other tools don't?**
- The heartbeat history is **first-class time-series data** (`AgentHeartbeat` rows) — so the dashboard charts are just queries.
- Security-relevant tables are explicit: **hashed registration keys** and **issued tokens** are their own entities.

**Terminology & "can I see it?"**
- **Time-series** — many rows over time for the same agent (the basis of the trend charts).
- **Entity** — a C# class mapped to one table.

**The hard part, drawn** — the monitoring schema:

```
  EFDataModel (DbContext)
       └ fleet ▶ Agent (machine) ─1:N─ AgentHeartbeat (CPU/RAM/disk over time)
                · RegistrationKey (hashed) · ApiClientToken (issued) · HubJob · CiCdTokenUsage
       └ core  ▶ User · Tenant · Department · Setting · PluginCache
       └ providers ▶ SQL Server · SQLite · MySQL · PostgreSQL · InMemory
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
