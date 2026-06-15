# FreeA11yChecker.Client

Blazor WebAssembly client for the FreeA11yChecker accessibility auditing platform. Provides the full web UI: scan dashboards, per-page violation review, site management, manual check wizards, trend reports, compliance scorecards, and real-time scan progress via SignalR. Hosted by the `FreeA11yChecker` server project.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net10.0` |
| Output type | Blazor WebAssembly (client-side) |

## @page Routes

### Accessibility / Compliance

| Route | Component |
|---|---|
| `/Compliance` | `FreeA11yChecker.App.ScanDashboard.razor` — tenant-wide scan overview |
| `/Compliance/{SiteId}/{ScanRunId}` | `FreeA11yChecker.App.ScanDetail.razor` — per-run violation list |
| `/Compliance/Page/{PageResultId}` | `FreeA11yChecker.App.PageDetail.razor` — per-page screenshot and violations |
| `/Compliance/Rule/{RuleId}` | `FreeA11yChecker.App.RuleDetail.razor` — cross-site occurrences of one rule |
| `/Compliance/Rules` | `FreeA11yChecker.App.RuleHotlist.razor` — ranked rule violations across all sites |
| `/Compliance/Search` | `FreeA11yChecker.App.CrossSiteSearch.razor` |
| `/Compliance/Scorecard` | `FreeA11yChecker.App.SiteScorecard.razor` |
| `/Compliance/Status/{SiteId}` | `FreeA11yChecker.App.ComplianceStatus.razor` — WCAG criterion pass/fail matrix |
| `/Compliance/Tree` | `FreeA11yChecker.App.DomainTree.razor` |
| `/Reports/Audit/{SiteId}` | `FreeA11yChecker.App.AuditReport.razor` — downloadable PDF audit report |
| `/Reports/Trends` | `FreeA11yChecker.App.TrendReport.razor` — violation trend over time |

### Settings / Administration

| Route | Component |
|---|---|
| `/Settings` | `Settings.razor` |
| `/Settings/Sites` | `FreeA11yChecker.App.SiteList.razor` |
| `/Settings/EditSite/{SiteId}` | `FreeA11yChecker.App.EditSite.razor` |
| `/Settings/SiteRuns/{SiteId}` | `FreeA11yChecker.App.SiteRuns.razor` |
| `/Settings/ScanMonitor` | `FreeA11yChecker.App.ScanMonitor.razor` — live scan progress |
| `/Settings/ManualChecks/{SiteId}` | `FreeA11yChecker.App.ManualCheckWizard.razor` |
| `/Settings/Suppressions` | `FreeA11yChecker.App.Suppressions.razor` |
| `/Settings/Users` | `Users.razor` |
| `/Settings/EditUser/{userid}` | `EditUser.razor` |
| `/Settings/UserGroups` | `UserGroups.razor` |
| `/Settings/EditUserGroup/{groupid}` | `EditUserGroup.razor` |
| `/Settings/Departments` | `Departments.razor` |
| `/Settings/Tags` | `Tags.razor` |
| `/Settings/Tenants` | `Tenants.razor` |
| `/Settings/AppSettings` | `AppSettings.razor` |
| `/Settings/Files` | `Files.razor` |
| `/Settings/Language` | `Languages.razor` |
| `/Settings/UDF` | `UDF.razor` |
| `/Settings/DeletedRecords` | `DeletedRecords.razor` |
| `/Setup` | `Setup.razor` |

### Authentication / Utility

| Route | Component |
|---|---|
| `/Login` | `Login.razor` |
| `/Logout` | `Logout.razor` |
| `/ProcessLogin` | `ProcessLogin.razor` |
| `/ChangePassword` | `ChangePassword.razor` |
| `/PasswordChanged` | `PasswordChanged.razor` |
| `/Profile` | `Profile.razor` |
| `/Authorization/AccessDenied` | `AccessDenied.razor` |
| `/Authorization/InvalidUser` | `InvalidUser.razor` |
| `/Authorization/NoLocalAccount` | `NoLocalAccount.razor` |
| `/` | `Index.razor` |
| `/About` | `About.razor` |
| `/not-found` | `NotFound.razor` |

## Key Classes / Methods

| Class / File | Purpose |
|---|---|
| `BlazorDataModel` (`DataModel.cs`) | Singleton state bag shared across all pages; holds current user, tenant, site list, scan run cache, and SignalR connection |
| `Helpers.cs` | Large partial class; all `HttpClient`-based API calls to the server (`GetSites`, `SaveScanRun`, `GetViolations`, `GetPageScanResults`, etc.) and SignalR connection management |
| `WcagLevelHelper` (`WcagLevelHelper.cs`) | Maps violation rule IDs to WCAG 2.1/2.2 criterion numbers and levels for display |
| `Program` (`Program.cs`) | WASM entry point; registers `BlazorDataModel` singleton, MudBlazor, Radzen, Blazored.LocalStorage, and in-browser Roslyn compiler service |

## Project References

| Project | Role |
|---|---|
| `FreeA11yChecker.DataObjects` | Shared DTOs used for all API request/response models |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `MudBlazor` | Primary UI component library |
| `Radzen.Blazor` | Additional UI components (charts, data grids) |
| `Blazor.Bootstrap` | Bootstrap-based Blazor components |
| `BlazorMonaco` | Monaco editor for source code viewing |
| `Microsoft.AspNetCore.SignalR.Client` | Real-time scan progress updates |
| `Blazored.LocalStorage` | Client-side preference persistence |
| `CsvHelper` | CSV export of violation data |
| `HtmlAgilityPack` | Client-side HTML parsing |
| `Humanizer` | Human-readable durations and counts |
| `FluentValidation` | Form input validation |
| `Microsoft.CodeAnalysis.CSharp` | In-browser Roslyn compilation for plugin editing |
| `FreeBlazor` | WSU-EIT shared Blazor component library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The whole web UI is C# compiled to **WebAssembly** and run *inside your browser* — dashboards, per-page violation review, site management, manual-check wizards, PDF report links, and a live scan monitor. A single shared state object (`BlazorDataModel`) holds the current user, tenant, site list, and the SignalR connection; `Helpers.cs` makes all the API calls to the server. It even embeds a Roslyn C# compiler so plugin components can be edited and previewed in the browser.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WebAssembly (.NET 10) | C# UI running client-side | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/Program.cs) |
| Shared state bag | Current user, tenant, sites, SignalR conn | [DataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/DataModel.cs) |
| API + SignalR client | Every server call lives here | [Helpers.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/Helpers.cs) |
| The screens | Dashboards, rule hotlist, page detail | [Pages/App/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Client/Pages/App) |
| In-browser Roslyn compile | Edit/preview plugin components live | [DynamicBlazorSupport/CompilationService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/DynamicBlazorSupport/CompilationService.cs) |
| MudBlazor / Radzen | UI component & chart libraries | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Client/Program.cs) |

**Why does this exist?**
A rich web app that runs client-side is cheap to host (it ships as static files), scales trivially, and only talks to the server for data and real-time updates — a good fit for an auditing dashboard many teams hit at once.

**What does it accomplish that other tools don't?**
- A **live scan monitor**: SignalR pushes each page's result to the dashboard as the scan runs.
- A cross-site **"rule hotlist"** that ranks the most common violations across every site in the tenant.
- In-browser plugin editing — write a C# component and see it compiled by Roslyn *in the browser*, no rebuild.

**Terminology & "can I see it?"**
- **WebAssembly (WASM)** — a format that lets compiled C# run at near-native speed in the browser.
- **Hydration** — the moment the downloaded C# "wakes up" and the page becomes interactive.
- **State bag** — one shared object every page reads/writes so the UI stays consistent.

**The hard part, drawn** — a click becomes a server call and the screen updates:

```
  You ──click──▶  *.razor page  ──calls──▶  Helpers.cs  ──HTTP──▶  server API  ──▶  DB
                       ▲                        │
                       │ StateHasChanged()      └── SignalR subscribe ──▶ live scan ticks ──┐
                       │                                                                      │
                       └──────────────  BlazorDataModel  ◀───────────────────────────────────┘
                          (shared state: current user, tenant, site list, live connection)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
