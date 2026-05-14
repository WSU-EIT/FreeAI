# FreeManager.DataAccess

Server-side data access and business logic layer for the FreeManager platform.

Implements `IDataAccess` through a large set of partial classes organized by feature. All database access uses Entity Framework Core. Also includes Graph API integration (Azure AD / Microsoft Graph), LDAP/Active Directory helpers, JWT token management, Roslyn-based dynamic C# execution for plugins, QuestPDF document generation, and database migration scripts for all four supported backends.

## Key Public Classes

| Class | Description |
|-------|-------------|
| `DataAccess` | Main partial class implementing `IDataAccess`; initializes EF Core `DbContext` for SQL Server, MySQL, PostgreSQL, SQLite, or InMemory |
| `DataAccess` (App partials) | FreeManager-specific builds, projects, files, entity wizard persistence, template management |
| `GraphAPI` | Microsoft Graph API client for Azure AD user lookup and management |
| `RandomPasswordGenerator` | Cryptographically random password generation |
| `Utilities` | Shared utility methods (string helpers, validation, etc.) |
| `DataMigrations.*` | Per-backend migration scripts: `SQLServer`, `MySQL`, `PostgreSQL`, `SQLite` |

## Feature Partials

| File | Responsibility |
|------|---------------|
| `DataAccess.Users.cs` | User CRUD, authentication, token generation |
| `DataAccess.Authenticate.cs` | Login, token validation, fingerprint handling |
| `DataAccess.JWT.cs` | JWT encode/decode helpers |
| `DataAccess.Tenants.cs` | Multi-tenant management |
| `DataAccess.Departments.cs` | Department and department-group management |
| `DataAccess.Tags.cs` | Tagging system |
| `DataAccess.FileStorage.cs` | Binary file upload/download/delete |
| `DataAccess.Plugins.cs` | Plugin caching, dynamic code compilation and execution |
| `DataAccess.Encryption.cs` | AES encryption helpers |
| `DataAccess.SignalR.cs` | Real-time notification dispatching |
| `DataAccess.CSharpCode.cs` | Roslyn C# compilation and execution |
| `FreeManager.App.DataAccess.Builds.cs` | Build/export management for App Builder projects |
| `FreeManager.App.DataAccess.Projects.cs` | Saved project persistence |
| `FreeManager.App.DataAccess.Files.cs` | App file version management |
| `FreeManager.App.DataAccess.EntityWizardPersistence.cs` | Save/load Entity Wizard state |
| `FreeManager.App.DataAccess.Templates.cs` | Application template resolution |

## Project References and NuGet Packages

**References:** `FreeManager.DataObjects`, `FreeManager.EFModels`, `FreeManager.Plugins`

| Package | Version |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | (via EFModels) |
| `Microsoft.Graph` | 5.98.0 |
| `Azure.Identity` | 1.17.1 |
| `Novell.Directory.Ldap.NETStandard` | 4.0.0 |
| `JWTHelpers` | 1.0.1 |
| `QuestPDF` | 2025.12.0 |
| `CsvHelper` | 33.1.0 |
| `Brad.Wickett_Sql2LINQ` | 3.0.1 |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 10.0.1 |

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
