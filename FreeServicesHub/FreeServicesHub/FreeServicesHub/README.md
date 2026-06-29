# FreeServicesHub

The central ASP.NET Core 10 / Blazor web application that acts as the hub for all registered agents. Hosts the Blazor WebAssembly UI, a SignalR hub for real-time agent communication, a REST API consumed by agents, multi-provider authentication, a plugin system with live Roslyn compilation, and an optional background task processor.

## What it does

`Program.cs` wires together:

- **Blazor WebAssembly** (`AddInteractiveWebAssemblyRenderMode`) served from the `FreeServicesHub.Client` project, plus server-side Razor pages.
- **SignalR hub** (`freeserviceshubHub`) mapped at `/freeserviceshubHub`. Agents connect with Bearer tokens, join the `Agents` group, send heartbeats, and receive settings-push messages. Supports optional Azure SignalR Service via `AzureSignalRurl` in appsettings.
- **REST controllers** — agent registration (`/api/Data/RegisterAgent`), heartbeat save (`/api/Data/SaveHeartbeat`), job dispatch (`/api/agent/jobs`), setup, and authorization endpoints.
- **Multi-provider authentication** — Microsoft, Google, Facebook, Apple OAuth; OpenID Connect (WSU login.wsu.edu); local accounts with JWT; LDAP. Providers are configured at startup via `CustomAuthenticationProviders`.
- **Plugin system** — on startup, `.plugin` and `.assemblies` files are loaded from `PluginFiles/`. Each plugin is compiled at runtime via Roslyn (`Microsoft.CodeAnalysis.CSharp`); plugins implement `IPlugin`, `IPluginAuth`, or `IPluginBackground`.
- **Background processor** (`BackgroundProcessor`) — optional `BackgroundService` that polls for queued `backgroundprocess` plugins and executes them on a timer. Enabled/disabled per machine via `BackgroundService:LoadBalancingFilter`.
- **Health endpoint** — `GET /health` returns `{ status, timestamp }` for CI/CD readiness checks.
- **Authorization policies** — `AppAdmin`, `Admin`, `ManageFiles`, `PreventPasswordChange`, plus any app-defined policies.

## Key public classes

| Class | Purpose |
|---|---|
| `Program` (partial) | Application entry point; DI wiring, middleware pipeline, SignalR, plugin loading |
| `freeserviceshubHub` | SignalR hub; handles heartbeats, agent settings request/push, group management |
| `BackgroundProcessor` | `BackgroundService`; runs plugin-defined background tasks on a configurable timer |
| `DataAccess` | Partial class spanning 39 files; all database operations via EF Core |
| `CustomAuthentication` | Multi-provider auth abstraction injected as `ICustomAuthentication` |
| `IPlugin` | Interface for executable plugins (`Execute` method) |
| `IPluginAuth` | Interface for authentication plugins (`Login`/`Logout` methods) |
| `RouteHelper` | Utility for resolving tenant-aware routes |

## Blazor pages (server-side)

These Razor pages handle authentication redirects:

| File | Route |
|---|---|
| `CustomAuthenticationLogin.cshtml` | `/CustomAuthenticationLogin` |
| `PluginAuthenticationLogin.cshtml` | `/PluginAuthenticationLogin` |

The main SPA routes are served from `FreeServicesHub.Client` (see that project's README).

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | net10.0 |
| User secrets ID | `42bdcaca-d07e-4dbe-933d-413ef924fa39` |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub.Client` | Blazor WebAssembly frontend |
| `FreeServicesHub.DataAccess` | All database and business logic |
| `FreeServicesHub.Plugins` | Plugin loading and Roslyn execution engine |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Authentication.MicrosoftAccount` | Microsoft OAuth |
| `Microsoft.AspNetCore.Authentication.Google` | Google OAuth |
| `Microsoft.AspNetCore.Authentication.Facebook` | Facebook OAuth |
| `AspNet.Security.OAuth.Apple` | Apple Sign-In |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | OpenID Connect (WSU SSO) |
| `Microsoft.Azure.SignalR` | Optional Azure SignalR Service backplane |
| `Serilog.Extensions.Logging.File` | File logging for the background service |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the **hub** — the central server every agent reports to. It serves the Blazor dashboard, runs the SignalR hub agents connect to, and exposes the REST API for agent registration, heartbeat saving, and job dispatch. It authenticates agents with Bearer tokens (and users with OAuth/OIDC/local + LDAP), loads Roslyn plugins, runs an optional background processor, and includes a server-side agent monitor and a `/health` endpoint.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| SignalR hub | Receives heartbeats, pushes settings | [Hubs/signalrHub.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/Hubs/signalrHub.cs) |
| Agent REST API | Register / save-heartbeat / jobs | [Controllers/FreeServicesHub.App.API.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/Controllers/FreeServicesHub.App.API.cs) |
| Server-side agent monitor | Tracks which agents are reporting | [FreeServicesHub.App.AgentMonitorService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/FreeServicesHub.App.AgentMonitorService.cs) |
| Host wiring | DI, auth, SignalR, plugin loading | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/Program.cs) |

**Why does this exist?**
A single server that securely accepts live telemetry from many agents, stores it, serves the real-time dashboard, and exposes the management API — the hub everything points at.

**What does it accomplish that other tools don't?**
- **Token-gated agent endpoints** — agents authenticate with a Bearer JWT issued at registration.
- **Both transports** — SignalR for live heartbeats, plus HTTP endpoints so a disconnected agent can still deliver buffered snapshots.
- **CI-friendly** — a `/health` endpoint returns `{ status, timestamp }` for readiness checks.

**Terminology & "can I see it?"**
- **Hub** (`freeserviceshubHub`) — the SignalR endpoint agents connect to.
- **`Agents` group** — the SignalR group every connected agent joins, so the hub can broadcast to all of them.

**The hard part, drawn** — the receive-and-serve side:

```
  Agents ──SignalR SendHeartbeat──▶ signalrHub ─┐
  Agents ──HTTP /SaveHeartbeat (fallback)──────▶ ├─▶ DataAccess.SaveHeartbeat ─▶ EF Core
                                                 │        │
  AgentMonitorService watches for silent agents ─┘        ▼
  Browser dashboard ◀── SignalR live updates ──── /AgentDashboard reflects the whole fleet
  /RegisterAgent (issue Bearer JWT)   ·   /api/agent/jobs (queue Ping / CollectStats)   ·   /health
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
