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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
