# FreePlugins.DataAccess

Server-side data access layer. Contains all business logic, EF Core repositories, authentication helpers, external service integrations, and the plugin execution engine. Consumed exclusively by the `FreePlugins` web host.

## What it does

The `DataAccess` partial class is split into focused files:

| File | Responsibility |
|------|---------------|
| `DataAccess.cs` | Core constructor, DI wiring, `IDataAccess` interface |
| `DataAccess.Plugins.cs` | `ExecutePlugin`, `GetPlugins`, `GetPluginsWithoutCode`, `SavePluginsToCache`. Dispatches dynamic Roslyn plugins via `IPlugins.ExecuteDynamicCSharpCode` |
| `DataAccess.Authenticate.cs` | Login, logout, token validation, session management |
| `DataAccess.Users.cs` | User CRUD, password management, `UserUpdate` plugin invocation |
| `DataAccess.Tenants.cs` | Multi-tenant management |
| `DataAccess.ActiveDirectory.cs` | LDAP / Active Directory queries |
| `DataAccess.JWT.cs` | JWT token generation and validation |
| `DataAccess.FileStorage.cs` | Blob/file storage operations |
| `DataAccess.Encryption.cs` | AES encryption helpers |
| `DataAccess.Migrations.cs` | EF migration runner |
| `DataAccess.Settings.cs` | Application settings CRUD |
| `DataAccess.Departments.cs` | Department management |
| `DataAccess.Tags.cs` | Tagging system |
| `DataAccess.UserGroups.cs` | User group management |
| `DataAccess.SignalR.cs` | SignalR message broadcasting |
| `DataAccess.Ajax.cs` | AJAX helper responses |
| `DataAccess.Language.cs` | Localization |
| `DataAccess.UDFLabels.cs` | User-defined field labels |
| `DataAccess.Utilities.cs` | Shared utilities (serialization, duplication, etc.) |
| `GraphAPI.cs` | Microsoft Graph API calls |
| `RandomPasswordGenerator.cs` | Secure random password generation |
| `DataMigrations.*.cs` | Per-database migration scripts (MySQL, PostgreSQL, SQLite, SQLServer) |

## Plugin execution

`ExecutePlugin(PluginExecuteRequest request, DataObjects.User? CurrentUser)` resolves the plugin code from the in-memory cache, builds the argument array appropriate for the plugin type, loads the required assemblies, and calls `IPlugins.ExecuteDynamicCSharpCode`. The method handles `General`, `Auth`, `BackgroundProcess`, and `UserUpdate` argument patterns.

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.DataObjects` | Shared DTOs |
| `FreePlugins.EFModels` | EF Core DbContext and entities |
| `FreePlugins.Plugins` | `IPlugins` interface and Roslyn executor |

## Notable NuGet packages

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore` (via EFModels) | ORM |
| `Novell.Directory.Ldap.NETStandard` | LDAP / Active Directory |
| `Microsoft.Graph` | Microsoft Graph API |
| `Azure.Identity` | Azure credential management |
| `JWTHelpers` | JWT helpers |
| `QuestPDF` | PDF generation |
| `CsvHelper` | CSV import/export |
| `Brad.Wickett_Sql2LINQ` | SQL-to-LINQ helpers |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Blazor WASM server-side support |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class library |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
