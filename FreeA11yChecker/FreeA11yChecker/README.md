# FreeA11yChecker

Blazor Server host for the FreeA11yChecker accessibility auditing platform. Serves the Blazor WebAssembly client, exposes the REST API, runs the SignalR hub for real-time scan progress, hosts the scheduled scan background service, and delivers PDF audit reports.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Output type | Web application (Blazor Server host) |
| User Secrets ID | `0cbc4331-d04d-4e10-97b7-24a8798f626c` |

## What It Does

- Bootstraps the full Blazor interactive WebAssembly render mode and serves the client app.
- Registers `ScannerBackgroundService` (a `BackgroundService`) which polls every 5 minutes for queued or cron-scheduled scan runs and dispatches them through `ScannerEngine`. A static `SemaphoreSlim` (`WakeSignal`) lets a manual trigger wake the loop immediately.
- Loads plugins from `PluginFiles/` at startup using Roslyn compilation. Plugins are injected into the `DataAccess` DI object and available as their own DI service.
- Configures authentication via `CustomAuthenticationProviders` supporting local accounts, Azure AD / OpenID Connect, Google, Facebook, Microsoft Account, and Apple OAuth.
- Maps the `freeallycheckerHub` SignalR hub at `/freeallycheckerHub` with stateful reconnects. Clients join tenant-scoped groups and receive `SignalRUpdate` messages for live scan progress.
- Configures QuestPDF (Community license) for PDF report generation.
- When `AzureSignalRurl` is set in config, upgrades to Azure SignalR Service; otherwise falls back to local SignalR.
- Load-balancing filter: restricts background service activation to the machine whose hostname matches `BackgroundService:LoadBalancingFilter`, preventing duplicate scans in multi-server deployments.

## Key Classes / Methods

| Class / File | Purpose |
|---|---|
| `Program` (`Program.cs`) | Entry point; registers all services, configures the HTTP pipeline, maps hub and routes |
| `ScannerBackgroundService` (`FreeA11yChecker.App.ScannerService.cs`) | Polls for pending (`Status = "Queued"`) and cron-scheduled scan runs; wakes immediately via `WakeSignal` semaphore |
| `freeallycheckerHub` (`Hubs/signalrHub.cs`) | SignalR hub; routes `SignalRUpdate` messages to tenant-scoped groups; `JoinTenantId` moves a connection between groups |
| `DataController` (`Controllers/DataController.*.cs`) | Partial-class REST API covering sites, pages, scan runs, violations, users, tenants, settings, files, tags, and departments |
| `FreeA11yChecker.App.API.cs` | App-specific REST endpoints: `GetSites`, `SaveSites`, `DeleteSites`, `GetSitePages`, `DiscoverLinks`, `TriggerScan` |
| `FreeA11yChecker.App.API.AuditExport.cs` | PDF audit report generation and CSV export endpoints |
| `FreeA11yChecker.App.API.Public.cs` | Unauthenticated endpoints for public compliance status data |
| `PluginsInterfaces.cs` | Defines `IPlugin` and the plugin type taxonomy (`Auth`, `BackgroundProcess`, `UserUpdate`, `Example`) |

## Project References

| Project | Role |
|---|---|
| `FreeA11yChecker.DataAccess` | All database I/O, PDF generation, auth, MS Graph |
| `FreeA11yChecker.Plugins` | Roslyn dynamic C# compilation host |
| `FreeA11yChecker.Client` | Blazor WASM client app (served by this host) |
| `FreeA11yChecker.Scanner` | Core Playwright scanning engine |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Serves the WASM client |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | Azure AD / OIDC auth |
| `Microsoft.AspNetCore.Authentication.{Google,Facebook,MicrosoftAccount}` | OAuth providers |
| `AspNet.Security.OAuth.Apple` | Apple Sign In |
| `Microsoft.Azure.SignalR` | Azure SignalR Service backplane (optional) |
| `Microsoft.Playwright` | Passed to ScannerEngine for browser automation |
| `NCrontab` | Cron expression parsing for scheduled scans |
| `QuestPDF` | PDF report generation |
| `Serilog.Extensions.Logging.File` | Background service log file output |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
