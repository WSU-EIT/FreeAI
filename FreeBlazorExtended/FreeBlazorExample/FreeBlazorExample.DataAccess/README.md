# FreeBlazorExample.DataAccess

Server-side data access and business logic library for FreeBlazorExample.

Implements `IDataAccess` for EF Core repositories (Users, Tenants, Departments, Settings, FileStorage, Tags), authentication flows (local, LDAP/AD, Microsoft Graph), PDF document generation, CSV import/export, JWT issuance, and the background service task dispatcher (`ProcessBackgroundTasksApp`). Injected into the ASP.NET Core controllers by DI.

## Key classes

| Class | Purpose |
|-------|---------|
| `DataAccess` | Main repository; implements `IDataAccess` |
| `DataAccess` (partial — App) | App-specific business logic extension point |
| `CustomAuthentication` | Wraps local, LDAP, and Graph authentication flows |
| `BackgroundService` | Periodic task runner; handles soft-delete cleanup and plugin background tasks |

## Project references and notable packages

**Project references:** `FreeBlazorExample.DataObjects`, `FreeBlazorExample.EFModels`, `FreeBlazorExample.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `Azure.Identity` | 1.17.1 | Azure managed-identity credential |
| `Microsoft.Graph` | 5.102.0 | Entra ID / Graph API user lookups |
| `Novell.Directory.Ldap.NETStandard` | 4.0.0 | LDAP/AD authentication |
| `QuestPDF` | 2026.2.0 | PDF document generation |
| `Brad.Wickett_Sql2LINQ` | 3.0.1 | Dynamic LINQ query builder |
| `JWTHelpers` | 1.0.1 | JWT encode/decode |
| `CsvHelper` | 33.1.0 | CSV import/export |

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
