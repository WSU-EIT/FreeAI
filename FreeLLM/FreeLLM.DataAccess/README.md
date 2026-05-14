# FreeLLM.DataAccess

Server-side data access and business logic library for FreeLLM.

Implements `IDataAccess` which is injected into the ASP.NET Core controllers. Contains EF Core repository methods for all entities (Users, Tenants, Departments, Settings, FileStorage, PluginCaches), authentication helpers (local accounts, Active Directory/LDAP, Microsoft Graph), background service task processing, PDF generation via QuestPDF, CSV import/export, and JWT token issuance.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `DataAccess` | `DataAccess.cs` | Main repository; implements `IDataAccess`; receives connection string, database type, and `IServiceProvider` via DI |
| `DataAccess` (partial) | `DataAccess.App.cs` | App-specific business logic extension point (`ProcessBackgroundTasksApp`) |
| `CustomAuthentication` | `CustomAuthentication.cs` | Implements `ICustomAuthentication`; wraps local, LDAP, and Graph auth flows |
| `BackgroundService` | `BackgroundService.cs` | Hosted background worker; periodic task runner driven by appsettings interval |

## Project references and notable packages

**Project references:** `FreeLLM.DataObjects`, `FreeLLM.EFModels`, `FreeLLM.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `Azure.Identity` | 1.16.0 | Azure-managed identity credential |
| `Microsoft.Graph` | 5.92.0 | Entra ID / Graph API user lookups |
| `Novell.Directory.Ldap.NETStandard` | 4.0.0 | LDAP/AD authentication |
| `QuestPDF` | 2025.7.1 | PDF document generation |
| `Brad.Wickett_Sql2LINQ` | 3.0.1 | Dynamic LINQ query builder |
| `JWTHelpers` | 1.0.1 | JWT encode/decode utilities |
| `CsvHelper` | 33.1.0 | CSV import/export |
| `NuGet.Protocol` | 6.14.0 | NuGet API access (plugin package resolution) |
| `Microsoft.CodeAnalysis.CSharp` | 4.14.0 | Roslyn; server-side dynamic code analysis |

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
