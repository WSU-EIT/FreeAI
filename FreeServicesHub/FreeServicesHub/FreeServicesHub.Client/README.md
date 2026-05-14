# FreeServicesHub.Client

The Blazor WebAssembly frontend for FreeServicesHub. Runs entirely in the browser and communicates with the server via REST and SignalR. Hosts the agent dashboard, management screens, settings pages, and shared UI components including a Monaco code editor, Highcharts charts, a PDF viewer, and a dynamic Blazor plugin renderer.

## @page routes

| Route | Page |
|---|---|
| `/` and `/{TenantCode}` | Index / dashboard |
| `/About` and `/{TenantCode}/About` | About |
| `/AgentDashboard` | Real-time agent health dashboard |
| `/AgentDetail/{agentid}` | Detailed view for a single agent |
| `/AgentManagement` | Agent list management |
| `/AgentSettings` | Agent configuration settings |
| `/Settings/ApiKeys` | API key manager |
| `/BackgroundServices` | Background service log viewer |
| `/Login` | Login page |
| `/Logout` | Logout handler |
| `/ProcessLogin` | OAuth callback processor |
| `/ChangePassword` | Change password form |
| `/PasswordChanged` | Post-change confirmation |
| `/Profile` | User profile editor |
| `/Settings` | Settings home |
| `/Settings/AppSettings` | Application-wide settings |
| `/Settings/Users` | User list |
| `/Settings/EditUser/{userid}` | Edit individual user |
| `/Settings/UserGroups` | User group list |
| `/Settings/EditUserGroup/{groupid}` | Edit user group |
| `/Settings/Tenants` | Tenant list |
| `/Settings/EditTenant/{tenantid}` | Edit tenant |
| `/Settings/Departments` | Department list |
| `/Settings/DepartmentGroups` | Department group list |
| `/Settings/Tags` | Tag list |
| `/Settings/EditTag/{id}` | Edit tag |
| `/Settings/Language` | Language settings |
| `/Settings/Files` | File storage manager |
| `/Settings/UDF` | User-defined field labels |
| `/Settings/DeletedRecords` | Soft-deleted record viewer |
| `/Setup` | First-run setup wizard |
| `/not-found` | 404 page |
| `/Authorization/AccessDenied` | Access denied |
| `/Authorization/InvalidUser` | Invalid user |
| `/Authorization/NoLocalAccount` | No local account found |
| `/InvalidTenantCode` | Invalid tenant code error |
| `/MissingTenantCode` | Missing tenant code error |
| `/DatabaseOffline` | Database unavailable error |
| `/ServerUpdated` | Server restart notice |

## Key public classes

| Class | Purpose |
|---|---|
| `BlazorDataModel` | Singleton client state model injected into all pages |
| `Program` | WebAssembly host builder; registers MudBlazor, Radzen, Blazored.LocalStorage, SignalR client, Roslyn compiler service |

## Shared components

`/Shared/` contains reusable Blazor components including `ModalMessage`, `ToastMessage`, `NavigationMenu`, `HighchartsChart`, `MonacoEditor`, `PDF_Viewer`, `BlazorPlugin` (dynamic plugin renderer), `UploadFile`, `TagSelector`, `Icon`, `Tooltip`, and more.

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | net10.0 |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub.DataObjects` | Shared data transfer objects |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `MudBlazor` | Material Design component library |
| `Radzen.Blazor` | Radzen UI components (grids, charts, forms) |
| `Blazor.Bootstrap` | Bootstrap-themed Blazor components |
| `BlazorMonaco` | Monaco editor integration |
| `Blazored.LocalStorage` | Browser local storage access |
| `Microsoft.AspNetCore.SignalR.Client` | Real-time SignalR connection to hub |
| `Microsoft.CodeAnalysis.CSharp` | In-browser Roslyn C# compiler for plugin rendering |
| `CsvHelper` | CSV export/import |
| `Humanizer` | Human-readable time/number formatting |
| `HtmlAgilityPack` | HTML parsing utilities |
| `FluentValidation` | Client-side validation |

Part of the **FreeServicesHub** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
