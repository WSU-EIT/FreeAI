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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
