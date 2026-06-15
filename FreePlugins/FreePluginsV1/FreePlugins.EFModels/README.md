# FreePlugins.EFModels

Entity Framework Core class library that owns the database schema, DbContext, and migrations for the FreePlugins application. Supports four database backends: SQL Server, PostgreSQL, MySQL, and SQLite (including in-memory).

## EF entities

| Entity | Table | Description |
|--------|-------|-------------|
| `User` | Users | Application user account |
| `Tenant` | Tenants | Multi-tenant organization record |
| `UserGroup` | UserGroups | Named group of users |
| `UserInGroup` | UserInGroups | User-to-group membership |
| `Department` | Departments | Organizational department |
| `DepartmentGroup` | DepartmentGroups | Department-to-group mapping |
| `Setting` | Settings | Key-value application settings |
| `Tag` | Tags | Tagging taxonomy entry |
| `TagItem` | TagItems | Associates a tag with a domain object |
| `UDFLabel` | UDFLabels | User-defined field label |
| `PluginCache` | PluginCaches | Persisted plugin code cache (Id, Version, Name, Type, Code, Properties, etc.) |
| `FileStorage` | FileStorage | Stored file blobs |
| `EmailTemplate` | EmailTemplates | Email template records |

`EFDataModel` is the `DbContext`. `EFModelOverrides.cs` contains `OnModelCreating` fluent configurations.

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | `10.0.1` |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.1` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.0` |
| `MySql.EntityFrameworkCore` | `10.0.0-preview` |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.1` |
| `Microsoft.EntityFrameworkCore.InMemory` | `10.0.1` |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.1` (build-time only) |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class library |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** Defines the database: the `EFDataModel` DbContext and entities across four engines. Notable for plugins: the **`PluginCache`** table persists compiled plugin code + metadata so the host doesn't recompile unchanged plugins on every restart.

**What tech & where?** [EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.EFModels/EFModels/EFDataModel.cs) · `EFModelOverrides.cs`.

**Why does this exist?** One schema (incl. the plugin cache) that runs on whatever database is available.

**What does it beat?** The **`PluginCache` entity** makes plugin loading fast and persistent — compiled code survives restarts.

**Terminology:** **Plugin cache** — the stored, compiled plugin code so it isn't rebuilt each launch.

**The hard part, drawn:**
```
  EFDataModel ─DbSet─▶ User · Tenant · Setting · FileStorage · EmailTemplate · PluginCache (compiled plugin code)
        providers ▶ SQL Server · PostgreSQL · MySQL · SQLite (+ InMemory)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
