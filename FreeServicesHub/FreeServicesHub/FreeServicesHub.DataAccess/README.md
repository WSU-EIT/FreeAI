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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
