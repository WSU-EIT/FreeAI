# FreeAI.EFModels

Class library — Entity Framework Core models and DbContext.

This is the FreeCRM scaffold EF model layer, renamed to the FreeAI namespace with no app-specific tables added. `EFDataModel` is a `DbContext` targeting SQL Server, MySQL (MySql.EntityFrameworkCore), PostgreSQL (Npgsql), SQLite, or in-memory; the `OnConfiguring` override is intentionally commented out and only activated when generating migrations. `EFModelOverrides.cs` contains any additional Fluent API configuration. The schema covers the ten core FreeCRM tables listed below; no FreeAI-specific tables exist yet.

## DbSet members (tables)

| DbSet | Entity | Purpose |
|---|---|---|
| `Tenants` | `Tenant` | Multi-tenant root records |
| `Users` | `User` | User accounts with 10 UDF columns |
| `UserGroups` / `UserInGroups` | `UserGroup`, `UserInGroup` | Group membership |
| `Departments` / `DepartmentGroups` | `Department`, `DepartmentGroup` | Org structure |
| `FileStorages` | `FileStorage` | Binary file blobs |
| `Settings` | `Setting` | Key/value settings store (tenant- and user-scoped) |
| `UDFLabels` | `UDFLabel` | Custom column label overrides |
| `PluginCaches` | `PluginCache` | Serialised plugin definitions cached to DB |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `MySql.EntityFrameworkCore` | MySQL provider |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite provider |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory provider (testing / first-run) |
| `Google.Protobuf` | Required by the Npgsql provider |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net9.0` |
| Output type | Library |

Part of the ChatWithAI solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
