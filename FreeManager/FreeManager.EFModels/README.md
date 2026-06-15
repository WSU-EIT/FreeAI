# FreeManager.EFModels

Entity Framework Core models and `DbContext` for the FreeManager platform.

Owns the database schema and all migrations. The `EFDataModel` partial class registers `DbSet<T>` properties for every entity. Four database backends are supported: SQL Server, MySQL, PostgreSQL, and SQLite (InMemory for testing). App Builder-specific entities (`FMProject`, `FMBuild`, `FMAppFile`, `FMAppFileVersion`) extend the context through additional partial files.

## Key Public Classes

| Class | Description |
|-------|-------------|
| `EFDataModel` | Partial `DbContext`; registers all `DbSet<T>` properties and configures relationships in `OnModelCreating` |
| `User` | Application user: credentials, tenant, group memberships, auth tokens |
| `Tenant` | Multi-tenant isolation record |
| `Department` / `DepartmentGroup` | Organizational hierarchy |
| `Setting` | Key/value settings per tenant (also used as JSON blob storage for Starter-template projects) |
| `FMProject` | App Builder saved project record |
| `FMAppFile` / `FMAppFileVersion` | Generated source-file storage with version history |
| `FMBuild` | Build/export record for a project |

## Entity Sets

| Entity | Table | Notes |
|--------|-------|-------|
| `User` | Users | |
| `UserGroup` / `UserInGroup` | UserGroups / UserInGroups | M:M bridge |
| `Tenant` | Tenants | |
| `Department` / `DepartmentGroup` | Departments / DepartmentGroups | |
| `Setting` | Settings | Also stores JSON blobs |
| `FileStorage` | FileStorages | Binary file content |
| `Tag` / `TagItem` | Tags / TagItems | Optional module |
| `UDFLabel` | UDFLabels | User-defined field labels |
| `PluginCache` | PluginCaches | Compiled plugin bytecode cache |
| `FMProject` | FMProjects | App Builder projects |
| `FMAppFile` / `FMAppFileVersion` | FMAppFiles / FMAppFileVersions | Generated files |
| `FMBuild` | FMBuilds | Export/build records |

## Project References and NuGet Packages

| Package | Version |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | 10.0.1 |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.1 |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.1 |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.1 |
| `MySql.EntityFrameworkCore` | 10.0.0-preview |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.1 |

## Running Migrations

```bash
# SQL Server
dotnet ef migrations add <Name> --startup-project ../FreeManager
dotnet ef database update --startup-project ../FreeManager

# SQLite (for local development)
dotnet ef migrations add <Name> --startup-project ../FreeManager
```

Set `"DatabaseType"` in `appsettings.json` to `SqlServer`, `MySQL`, `PostgreSQL`, `SQLite`, or `InMemory`.

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library *defines the database*: the `EFDataModel` DbContext, the core FreeCRM tables, **plus the App Builder tables** — `FMProject` (a saved project), `FMAppFile`/`FMAppFileVersion` (generated source files with version history), and `FMBuild` (an export/build record). One model targets five database engines.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| EF Core DbContext | The schema (core + App Builder tables) | [EFModels/EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.EFModels/EFModels/EFDataModel.cs) |
| 5 provider packages | One model, five engines | [the EFModels project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeManager/FreeManager.EFModels) |

**Why does this exist?**
One schema that runs on whatever database is in place — and that stores the App Builder's projects and generated files alongside the platform tables.

**What does it accomplish that other tools don't?**
- **Versioned storage of generated files** (`FMAppFileVersion`) — the output of code generation is tracked, not just dumped to disk.
- **Five engines from one model**, InMemory included for tests.

**Terminology & "can I see it?"**
- **DbContext** — the C# object representing the database and its tables.
- **`FM*` tables** — the App Builder's own storage (Projects, Files, Builds).

**The hard part, drawn** — platform tables + the App Builder's own store:

```
  EFDataModel (DbContext)
       └ core ▶ User · Tenant · Department · Setting · FileStorage · PluginCache
       └ App Builder ▶ FMProject · FMAppFile / FMAppFileVersion (gen'd files + history) · FMBuild
       └ providers ▶ SQL Server · MySQL · PostgreSQL · SQLite · InMemory
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
