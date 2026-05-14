# FreeAI.Client

Blazor WebAssembly client application.

This is the FreeCRM scaffold client, renamed to the FreeAI namespace with no app-specific pages added. It delivers the full multi-tenant Blazor WASM UI: login/logout/OAuth flows, user profile and password management, settings administration for tenants, users, user groups, departments, department groups, files, languages, UDF labels, and application settings. The `Index.App.razor` home page shows a welcome message with an optional per-tenant logo and has a stub `LoadData()` for app-specific content. `BlazorDataModel.App.cs` adds a single placeholder `MyValues` property and an empty deleted-records check.

## @page routes

All routes are available with and without a `/{TenantCode}` prefix.

| Route | Page |
|---|---|
| `/` | Home (Index) |
| `/Login`, `/Logout`, `/ProcessLogin` | Authentication flow |
| `/Profile`, `/ChangePassword`, `/PasswordChanged` | User self-service |
| `/Settings` | Settings hub |
| `/Settings/Users`, `/Settings/EditUser/{userid}`, `/Settings/AddUser` | User management |
| `/Settings/UserGroups`, `/Settings/EditUserGroup/{groupid}`, `/Settings/AddUserGroup` | User group management |
| `/Settings/Tenants`, `/Settings/EditTenant/{tenantid}`, `/Settings/AddTenant` | Tenant management |
| `/Settings/Departments`, `/Settings/EditDepartment/{departmentid}` | Department management |
| `/Settings/DepartmentGroups`, `/Settings/EditDepartmentGroup/{departmentgroupid}` | Department group management |
| `/Settings/Files`, `/Settings/AppSettings`, `/Settings/Language`, `/Settings/UDF` | Misc admin settings |
| `/Settings/DeletedRecords` | Soft-delete recovery |
| `/Plugins` | Plugin testing page |
| `/Setup` | First-run database setup |
| `/DatabaseOffline` | Error page when DB is unreachable |
| `/About` | Application about page |
| `/Authorization/AccessDenied`, `/Authorization/InvalidUser`, `/Authorization/NoLocalAccount` | Auth error pages |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `FreeBlazor` | WSU-EIT shared Blazor component library |
| `MudBlazor` | Material Design component framework |
| `Blazor.Bootstrap` | Bootstrap 5 Blazor components |
| `Radzen.Blazor` | Additional data-grid and chart components |
| `Blazored.LocalStorage` | Browser localStorage interop |
| `BlazorMonaco` | Monaco (VS Code) in-browser code editor |
| `BlazorSortableList` | Drag-and-drop sortable lists |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn (used by plugin testing page) |
| `CsvHelper` | CSV import/export |
| `HtmlAgilityPack` | HTML parsing |
| `Humanizer` | Human-friendly string formatting |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.DataObjects` | Shared DTOs consumed by both client and server |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net9.0` |
| Output type | WebAssembly app (hosted by FreeAI server) |

Part of the ChatWithAI solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
