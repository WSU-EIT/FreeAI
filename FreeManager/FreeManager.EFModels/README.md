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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
