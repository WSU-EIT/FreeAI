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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The server-side data layer — the standard FreeCRM responsibilities (EF Core, auth, JWT, Graph/AD, PDF) **plus the plugin execution dispatcher**: `ExecutePlugin` looks up the plugin's code, assembles the right arguments for its type (`General`/`Auth`/`BackgroundProcess`/`UserUpdate`), loads needed assemblies, and calls into the Roslyn runtime.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Plugin execution dispatch | Build args per type, run the plugin | [DataAccess.Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.DataAccess/DataAccess.Plugins.cs) |
| EF Core data access | All DB I/O across 4 engines | [DataAccess.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.DataAccess/DataAccess.cs) |

**Why does this exist?** To keep data + the plugin-execution glue in one server-only layer.

**What does it accomplish that other tools don't?** It's the single place that knows **how to call each plugin type** correctly (different argument patterns), and where the integration bridge extends `IDataAccess` to also run compiled plugins.

**Terminology & "can I see it?"** **Dispatch** — picking and calling the right method for a plugin's type.

**The hard part, drawn:**
```
  ExecutePlugin(request, user)
        │ resolve code from cache · pick args by type (General/Auth/Background/UserUpdate) · load assemblies
        ▼ IPlugins.ExecuteDynamicCSharpCode<T>() ─▶ PluginExecuteResult
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
