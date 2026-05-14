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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
