# FreeBlazorExample

ASP.NET Core host for the FreeBlazorExtended showcase application.

Wires authentication (local, Google, Facebook, Microsoft, Apple, OpenID Connect), maps SignalR hubs (`/freeblazorexampleHub`, `/agentHub`, `/presentationHub`), registers all FreeBlazorExtended feature services (`AgentMonitoringService`, `RealtimeSyncService`, `CalendarEventService`, `FormService`, `UserPreferencesService`, `TreeService`, `NotificationService`), loads Roslyn plugins from `PluginFiles/`, and serves the Blazor WebAssembly client. Background service handles periodic record cleanup and plugin-defined background tasks.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `Program` | `Program.cs` + `Program.App.cs` | Host builder; wires DI, auth, SignalR hubs, plugin loader, EF connection |
| `DataController` | `Controllers/DataController.cs` | Base controller with auth helpers and policy constants |
| `DataController` (partial) | `Controllers/DataController.App.cs` | App-specific API endpoints extension point |
| `ConfigurationHelper` | `Classes/ConfigurationHelper.cs` + `.App.cs` | Typed appsettings loader; auth provider toggles, base path, analytics |

## @page routes

Routes are defined in `FreeBlazorExample.Client`. Showcase pages live under `/showcase/*`. Standard framework pages (`/Settings/*`, `/Login`, `/Profile`, etc.) are shared with the base template.

## Project references and notable packages

**Project references:** `FreeBlazorExample.DataAccess`, `FreeBlazorExample.Client`, `FreeBlazorExample.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 10.0.3 | Serves the WASM bundle |
| `Microsoft.Azure.SignalR` | 1.33.0 | Azure SignalR fallback |
| `AspNet.Security.OAuth.Apple` | 10.0.0 | Apple Sign-In |
| `Microsoft.AspNetCore.Authentication.Google/Facebook/MicrosoftAccount/OpenIdConnect` | 10.0.3 | OAuth providers |
| `Serilog.Extensions.Logging.File` | 3.0.0 | File-based structured logging |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | net10.0 |
| Nullable | enabled |
| User secrets | `c3a4acfd-bf26-4267-98c7-6746a2b80f10` |

Part of the **FreeBlazorExtended** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
