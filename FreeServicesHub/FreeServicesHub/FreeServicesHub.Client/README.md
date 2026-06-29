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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The browser dashboard, in C#/WebAssembly. Its headline screens are the **fleet views**: `/AgentDashboard` (real-time health of every agent), `/AgentDetail/{id}`, `/AgentManagement`, and `/AgentSettings`. It keeps a SignalR connection open so heartbeats update the dashboard live, and uses Highcharts for the CPU/RAM/disk graphs. Around the fleet views are the usual platform admin pages.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| API + SignalR helpers | Calls + live agent updates | [Helpers.App.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Client/Helpers.App.cs) |
| Blazor WASM dashboard | The agent screens & charts | [the Client project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Client) |
| Highcharts / MudBlazor / Radzen | Live charts & data grids | [the Client project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Client) |

**Why does this exist?**
So an operator watches the whole fleet's health on one live screen — and manages agents and API keys — without touching the database or any single machine.

**What does it accomplish that other tools don't?**
- A **live** `/AgentDashboard`: SignalR pushes each heartbeat as it arrives, so health is current, not last-refresh.
- Per-agent **drill-down** with charted CPU/RAM/disk trends, plus API-key and agent-settings management in the same UI.

**Terminology & "can I see it?"**
- **SignalR client** — the browser side of the live connection to the hub.
- **Highcharts** — the JS charting library used for the telemetry graphs.

**The hard part, drawn** — heartbeats become a live dashboard:

```
  hub ──SignalR push──▶ FreeServicesHub.Client
        │ /AgentDashboard updates each agent's tile in real time (Highcharts CPU/RAM/disk)
        │ /AgentDetail drills into one machine ; /AgentManagement + /Settings/ApiKeys manage them
        └ Http GET via Helpers.App ─▶ agent list · history · settings
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
