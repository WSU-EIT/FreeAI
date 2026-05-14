# FreeBlazorExample.EFModels

Entity Framework Core models and DbContext for FreeBlazorExample.

Owns the database schema. `EFDataModel` is the `DbContext`; entity classes map to database tables. Supports SQL Server, SQLite, MySQL, and PostgreSQL. References `FreeBlazorExtended` to share the `UserPreferences` POCO between the EF model layer and the library's Feature 104 service without duplication.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `EFDataModel` | `EFModels/EFDataModel.cs` | DbContext; all entity DbSets, `OnModelCreating` constraints |
| `User` | `EFModels/User.cs` | Auth credentials, display name, email, tenant membership |
| `Tenant` | `EFModels/Tenant.cs` | Multi-tenant scope |
| `Department` / `DepartmentGroup` | `EFModels/` | Org structure entities |
| `UserGroup` / `UserInGroup` | `EFModels/` | Group membership |
| `Setting` | `EFModels/Setting.cs` | Typed key-value app-settings store |
| `FileStorage` | `EFModels/FileStorage.cs` | Binary file storage metadata |
| `PluginCache` | `EFModels/PluginCache.cs` | Compiled plugin assembly cache |

## Project references and notable packages

**Project references:** `FreeBlazorExtended`

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.EntityFrameworkCore` | 10.0.3 | ORM core |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.3 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.3 | SQLite provider |
| `MySql.EntityFrameworkCore` | 10.0.1 | MySQL provider |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 | PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.3 | In-memory provider for testing |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net10.0 |
| Nullable | enabled |

Part of the **FreeBlazorExtended** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
