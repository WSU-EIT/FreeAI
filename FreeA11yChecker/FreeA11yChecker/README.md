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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the web server. It serves the Blazor WebAssembly UI to the browser, exposes the REST API the UI calls, runs the SignalR hub that pushes live scan progress, and hosts a background service that wakes every few minutes to run any scans that are queued or due on a cron schedule. When you click "scan now" in the UI, it flips a semaphore that wakes that background loop *immediately* instead of waiting for the next tick.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| ASP.NET Core host + DI wiring | Boots the whole app, registers services | [FreeA11yChecker.App.Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker/FreeA11yChecker.App.Program.cs) |
| REST API (sites / scans / trigger) | What the browser UI calls | [Controllers/FreeA11yChecker.App.API.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker/Controllers/FreeA11yChecker.App.API.cs) |
| Background scan scheduler | Runs queued / cron-due scans unattended | [Classes/BackgroundProcessor.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker/Classes/BackgroundProcessor.cs) |
| Pluggable authentication | Local + Azure AD/OIDC + Google/Facebook/MS/Apple | [Classes/CustomAuthenticationHandler.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker/Classes/CustomAuthenticationHandler.cs) |
| PDF audit export endpoints | Download a per-site WCAG report | [Controllers/FreeA11yChecker.App.API.AuditExport.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker/Controllers/FreeA11yChecker.App.API.AuditExport.cs) |

**Why does this exist?**
A single host ties the UI, the API, real-time updates, and scheduling together — so a scan can be triggered from a browser, run unattended overnight, and report its progress live to whoever is watching.

**What does it accomplish that other tools don't?**
- **Load-balancing filter** — only the server whose hostname matches the configured filter runs scheduled scans, so a multi-server deployment never double-runs the same scan.
- **Instant manual trigger** — a `WakeSignal` semaphore wakes the scheduler the moment you click "scan now" rather than waiting up to 5 minutes.
- **Auth your way** — local accounts or any major OAuth/OIDC provider, switchable by config.

**Terminology & "can I see it?"**
- **Background service** — code that runs on a timer with no user present (here, the scan scheduler).
- **SignalR hub** — the server endpoint that pushes live updates to connected browsers.
- **Semaphore (`WakeSignal`)** — a one-slot "doorbell" the UI rings to wake the scheduler early.

**The hard part, drawn** — one host, four jobs, and the instant-trigger loop:

```
  Browser (Blazor WASM UI) ──REST──▶ App.API / DataController ──▶ DataAccess ──▶ EF Core DB
        ▲    ▲                                                          │
        │    └──────────── SignalR hub  (live scan progress) ◀──────────┘
        │                                                      ▲ OnPageComplete
  "scan now"                                                   │
        ▼                                                       │
   TriggerScan ── sets WakeSignal ──▶ Background scan loop ─────┘
                  (fires every ~5 min OR the instant it's woken)
                         └──▶ ScannerEngine.ScanAll  →  Playwright + the 4 engines
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
