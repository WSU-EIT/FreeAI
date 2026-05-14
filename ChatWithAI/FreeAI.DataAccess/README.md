# FreeAI.DataAccess

Class library — server-side data access layer.

This is the FreeCRM scaffold data access layer, renamed to the FreeAI namespace with placeholder app-specific extension points that contain no real logic yet. The `DataAccess` partial class connects to SQL Server, MySQL, PostgreSQL, SQLite, or in-memory via Entity Framework Core, runs custom SQL migration scripts on startup, seeds default data, and exposes a large `IDataAccess` interface split across partial files covering authentication (local, LDAP, Active Directory, OAuth), user/tenant/department/group management, file storage, encryption, language/UDF labels, SignalR updates, plugin cache persistence, JWT helpers, PDF generation (QuestPDF), CSV import/export, NuGet package queries, and Microsoft Graph API calls. `DataAccess.App.cs` provides the app-specific hook methods (`DataAccessAppInit`, `GetBlazorDataModelApp`, `DeleteTenantApp`, etc.) but all are stubs. `Utilities.App.cs` and `GraphAPI.App.cs` follow the same pattern.

## Key public classes

| Class | File | Purpose |
|---|---|---|
| `DataAccess` | `DataAccess.cs` | Main partial class; EF Core setup, DB selection, startup migration |
| `IDataAccess` | `DataAccess.cs` + partials | Full service interface consumed by controllers |
| `GraphAPI` | `GraphAPI.cs` | Microsoft Graph calls (mail, user lookup) |
| `RandomPasswordGenerator` | `RandomPasswordGenerator.cs` | Cryptographically-random password creation |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.DataObjects` | Shared DTOs |
| `FreeAI.EFModels` | EF Core DbContext and entity models |
| `FreeAI.Plugins` | Plugin runtime (for executing dynamic code inside data access) |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.Graph` | Microsoft Graph API client |
| `Novell.Directory.Ldap.NETStandard` | LDAP / Active Directory lookups |
| `QuestPDF` | PDF generation |
| `JWTHelpers` | JWT token creation and validation |
| `Brad.Wickett_Sql2LINQ` | SQL-to-LINQ query helper |
| `Azure.Identity` | Azure managed identity and credential support |
| `NuGet.Protocol` | NuGet feed queries |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn (used in dynamic code execution helpers) |

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
