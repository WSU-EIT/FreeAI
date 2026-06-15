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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The web server for the showcase. It does the usual FreeCRM host duties (auth, plugins, serves the WASM client) **plus** the wiring two of the library's features need: it maps **three** SignalR hubs (`/freeblazorexampleHub`, `/agentHub`, `/presentationHub`) and registers all seven FreeBlazorExtended feature services in DI.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample/Program.cs) (hubs + feature-service registration) · [Controllers/DataController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample/Controllers/DataController.cs).

**Why does this exist?** So the showcase can run the *real* server-side parts of the library — the AgentHub (Feature 105) and PresentationHub (Feature 102) — not just the in-browser components.

**What does it beat?** It's the one place that shows **how to wire the library's SignalR-backed features** end-to-end (hubs mapped + services registered), which the feature READMEs reference as the canonical example.

**Terminology:** **Hub mapping** — exposing a SignalR endpoint (`MapHub`) clients connect to.

**The hard part, drawn:**
```
  FreeBlazorExample host
        ├─ MapHub: /freeblazorexampleHub · /agentHub (Feature 105) · /presentationHub (Feature 102)
        ├─ register 7 feature services (AgentMonitoring, RealtimeSync, Calendar, Form, UserPrefs, Tree, Notification)
        └─ load plugins · serve the WASM showcase client
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
