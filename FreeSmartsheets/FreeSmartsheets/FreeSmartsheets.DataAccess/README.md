# FreeSmartsheets.DataAccess

Server-side data access layer for FreeSmartsheets. Contains all business logic, Entity Framework Core repository methods, authentication helpers, Microsoft Graph integration, JWT utilities, and the Smartsheets-specific data methods consumed by the web host controllers.

## What It Does

- Implements the `IDataAccess` interface with partial classes organized by domain (Users, Departments, Tags, Tenants, Plugins, Authentication, etc.)
- Issues queries against EF Core (`EFDataModel`) for the configured database backend (SQL Server, PostgreSQL, SQLite, MySQL, or InMemory)
- Handles multi-tenant data isolation; all queries are scoped by `TenantId`
- Provides JWT creation and validation via `DataAccess.JWT.cs`
- Integrates with Microsoft Entra ID / Active Directory via `DataAccess.ActiveDirectory.cs` and `GraphAPI.cs`
- Supports database migrations for all four supported engines (`DataMigrations.SQLServer.cs`, `.PostgreSQL.cs`, `.SQLite.cs`, `.MySQL.cs`)
- Exposes `ProcessBackgroundTasksApp` — the hook called by the background service for Smartsheets-specific periodic work
- Provides `Utilities.App.cs` for any additional app-specific utility methods

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `DataAccess` | Main partial class implementing `IDataAccess`; constructed with connection string and database type |
| `DataAccess.Authenticate` | Validates credentials, creates JWT sessions, and handles OAuth callbacks |
| `DataAccess.Users` | CRUD for users, password management, and user group membership |
| `DataAccess.Tenants` | Multi-tenant provisioning and per-tenant settings |
| `DataAccess.Plugins` | Loads, caches, and executes plugins; stores plugin cache in the `PluginCache` table |
| `DataAccess.App.ProcessBackgroundTasksApp` | Hook for Smartsheets-specific background tasks |
| `GraphClient` | Microsoft Graph API client for Entra ID / Active Directory lookups |
| `RandomPasswordGenerator` | Cryptographically random password generation |

## Project References

| Reference | Role |
|-----------|------|
| `FreeSmartsheets.DataObjects` | Shared DTOs and contracts |
| `FreeSmartsheets.EFModels` | EF Core `DbContext` and entity classes |
| `FreeSmartsheets.Plugins` | Plugin runtime for executing dynamic C# code |

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Graph` | Microsoft Entra ID / Active Directory integration |
| `Azure.Identity` | Azure credential provider for Graph API |
| `Novell.Directory.Ldap.NETStandard` | LDAP / Active Directory queries |
| `JWTHelpers` | JWT creation and validation |
| `QuestPDF` | PDF report generation |
| `Brad.Wickett_Sql2LINQ` | Dynamic LINQ query builder |
| `CsvHelper` | CSV import/export |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The server-only data layer. It implements `IDataAccess` as partial classes by domain (Users, Tenants, Tags, Auth, Plugins…), runs EF Core against the configured database, scopes every query by `TenantId`, and provides JWT, Graph/AD, and migration support. This is **where the Smartsheet methods will live** — the design (`GetWorkspaces()` calling the Smartsheet SDK) is documented in [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md), but **no Smartsheet code exists here yet**; today it's the standard FreeCRM data layer plus a `ProcessBackgroundTasksApp` hook for future Smartsheet work.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core data access | All database I/O, tenant-scoped | [DataAccess.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataAccess/DataAccess.cs) |
| Authentication + JWT | Login, sessions, OAuth callbacks | [DataAccess.Authenticate.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataAccess/DataAccess.Authenticate.cs) |
| Microsoft Graph / AD | Directory lookups | [GraphAPI.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataAccess/GraphAPI.cs) |
| Smartsheet methods *(planned)* | Inventory queries — **not yet implemented** | [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md) |

**Why does this exist?**
To keep all data access, business logic, and (future) Smartsheet integration in one server-only layer so the UI and DTOs stay thin and database-agnostic.

**What does it accomplish that other tools don't?**
- One code path across **five** database engines, with tenant isolation throughout.
- A clear, single home for the Smartsheet integration (the `ProcessBackgroundTasksApp` hook + a future `GetWorkspaces`), so the design has an obvious place to land.

**Terminology & "can I see it?"**
- **Partial class** — one class (`DataAccess`) split across many files by topic.
- **Tenant isolation** — every query carries a `TenantId` so orgs can't see each other's data.

**The hard part, drawn** — today's layer and where Smartsheet plugs in:

```
  Controllers ─▶ IDataAccess (DataAccess.*) ─ EF Core ─▶ SQL Server | PostgreSQL | MySQL | SQLite | InMemory
                       ├─ Authenticate (local/OAuth) · JWT · Graph/AD
                       └─ ProcessBackgroundTasksApp hook  ◀── Smartsheet GetWorkspaces() goes here (planned)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
