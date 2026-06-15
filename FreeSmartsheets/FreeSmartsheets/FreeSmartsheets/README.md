# FreeSmartsheets

Main server application for FreeSmartsheets — a Smartsheet workspace inventory viewer. Hosts the ASP.NET Core web application, REST API, Blazor Server-side rendering, and SignalR hub.

## What It Does

This project is the HTTP entry point. On startup it:
- Loads plugins from the `PluginFiles/` directory using Roslyn runtime compilation
- Registers `IDataAccess` as a transient service bound to the configured database connection string
- Configures authentication (OpenID Connect, OAuth for Google/Microsoft/Apple/Facebook, and custom cookie-based auth)
- Starts the optional background processor if `BackgroundService.Enabled` is `true` in `appsettings.json`
- Mounts the Blazor WebAssembly client assembly alongside SSR Razor components
- Falls back to local SignalR unless `AzureSignalRurl` is configured

The Smartsheet API key is configured in `appsettings.json` and consumed by the data access layer to call the Smartsheet REST API to enumerate workspaces and access permissions.

## @page Routes (Blazor)

All routes support an optional `/{TenantCode}/` prefix. Standard platform routes are inherited from the client project:

| Route | Purpose |
|-------|---------|
| `/` | Home / welcome page |
| `/Login` | Login page |
| `/Profile` | User profile |
| `/Settings/Users` | User management |
| `/Settings/Departments` | Department management |
| `/Settings/Misc/Settings` | Application settings |
| `/Settings/Misc/Setup` | Initial setup wizard |

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `Program.Main` | Entry point; wires DI, middleware, SignalR, and Blazor rendering |
| `Program.AppModifyBuilderStart` | Hook to inject app-specific service registrations before core setup |
| `Program.AppModifyEnd` | Hook to modify the `WebApplication` pipeline after standard middleware |
| `DataController` (partial) | REST API controller; app-specific endpoint `YourEndpoint` stub in `DataController.App.cs` |
| `BackgroundProcessor` | Hosted service for periodic tasks (runs every 60 s by default) |
| `CustomAuthenticationHandler` | Cookie-based custom authentication handler |
| `signalrHub` | SignalR hub for real-time UI updates |

## Project References

| Reference | Role |
|-----------|------|
| `FreeSmartsheets.Client` | Blazor WebAssembly UI |
| `FreeSmartsheets.DataAccess` | All database and Smartsheets API business logic |
| `FreeSmartsheets.Plugins` | Runtime plugin loader |

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | OpenID Connect / Okta integration |
| `Microsoft.AspNetCore.Authentication.*` | OAuth providers (Google, Microsoft, Apple, Facebook) |
| `Microsoft.Azure.SignalR` | Azure SignalR Service support |
| `Serilog.Extensions.Logging.File` | File-based structured logging |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target Framework | `net10.0` |
| Output Type | Web executable |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the web server (the HTTP entry point). On startup it loads Roslyn plugins, registers the data-access service, wires authentication (OpenID Connect + Google/Microsoft/Apple/Facebook + custom cookie auth), optionally starts a background processor, serves the Blazor WASM client, and maps the SignalR hub. The Smartsheet API key is read from `appsettings.json` and *handed to the data layer* — which is where the Smartsheet calls will live once implemented.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| ASP.NET Core host + DI + auth | Boots the app, wires providers | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Program.cs) |
| SignalR hub | Real-time UI updates | [Program.cs#L238](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Program.cs#L238) |
| REST API controllers | What the browser calls | [Controllers/DataController.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Controllers/DataController.cs) |
| Plugin interfaces | Plugin contract the host loads | [PluginsInterfaces.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/PluginsInterfaces.cs) |

**Why does this exist?**
A single host that delivers the UI, the API, real-time updates, and plugin loading — and will surface the Smartsheet inventory once the data-layer calls are built.

**What does it accomplish that other tools don't?**
- Full enterprise auth stack (OIDC + four OAuth providers + custom cookie) out of the box.
- Optional **Azure SignalR** fallback for scale; local SignalR otherwise.
- The Smartsheet API key is centralized in config and injected into the data layer (one place to wire the integration).

**Terminology & "can I see it?"**
- **SignalR hub** (`/freesmartsheetsHub`) — pushes live updates to browsers.
- **Background processor** — periodic server-side worker (off unless enabled in config).

**The hard part, drawn** — the host's startup and request flow:

```
  startup ─▶ load Roslyn plugins · register IDataAccess · wire OIDC/OAuth · map freesmartsheetsHub
        │ (Smartsheet API key read from appsettings → passed to the data layer)
        ▼
  Browser (Blazor WASM) ─REST─▶ DataController ─▶ IDataAccess ─ EF Core ─▶ DB
        ▲────────────── freesmartsheetsHub (SignalR live updates) ─────────────┘
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
