# FreeLLM.EFModels

Entity Framework Core models and DbContext for FreeLLM.

Owns the database schema. `EFDataModel` is the `DbContext`; entity classes map one-to-one to database tables. Supports SQL Server, SQLite, MySQL (via `MySql.EntityFrameworkCore`), and PostgreSQL (via `Npgsql`). The InMemory provider is available for testing. No project references; all other projects reference this one.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `EFDataModel` | `EFModels/EFDataModel.cs` | DbContext; registers all entity DbSets and configures model constraints |
| `User` | `EFModels/User.cs` | User record (auth credentials, display name, email, tenant membership) |
| `Tenant` | `EFModels/Tenant.cs` | Multi-tenant scope entity |
| `Department` | `EFModels/Department.cs` | Department with Active Directory name mappings |
| `DepartmentGroup` | `EFModels/DepartmentGroup.cs` | Groups of departments |
| `UserGroup` | `EFModels/UserGroup.cs` | Named user group |
| `UserInGroup` | `EFModels/UserInGroup.cs` | User-to-group membership join |
| `Setting` | `EFModels/Setting.cs` | Typed key-value app settings store |
| `FileStorage` | `EFModels/FileStorage.cs` | Binary file storage metadata |
| `PluginCache` | `EFModels/PluginCache.cs` | Cached compiled plugin assemblies |

## Notable NuGet packages

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.EntityFrameworkCore` | 9.0.9 | ORM core |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.9 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` | 9.0.9 | SQLite provider |
| `MySql.EntityFrameworkCore` | 9.0.6 | MySQL provider |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | PostgreSQL provider |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.9 | In-memory provider for testing |
| `Google.Protobuf` | 3.32.1 | Protobuf serialization support |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net9.0 |
| Nullable | enabled |

Part of the **FreeLLM** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
