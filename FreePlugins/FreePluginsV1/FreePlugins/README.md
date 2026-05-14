# FreePlugins

ASP.NET Core / Blazor Server host application. This is the main web project that wires together the data layer, the Blazor WebAssembly client, the Roslyn-based file plugin runtime, and the compiled NuGet plugin integration layer.

## What it does

- Hosts the Blazor WebAssembly client via `Microsoft.AspNetCore.Components.WebAssembly.Server`
- Exposes a REST API (`DataController`, `AuthorizationController`, `SetupController`) consumed by the WASM client
- Runs a background processor (`BackgroundProcessor`) that fires `BackgroundProcess` plugins on a configurable interval
- Loads file-based plugins from the `Plugins/` folder at startup (`.cs` and `.plugin` files compiled by Roslyn at runtime)
- Registers and loads compiled NuGet plugins via `FreePlugins.Abstractions.Integration`
- Handles custom authentication via `CustomAuthenticationHandler` / `CustomAuthIdentity`

## Bundled file-based plugins

The `Plugins/` folder ships with the following example `.cs` plugins:

| File | Type | Description |
|------|------|-------------|
| `Example1.cs` | General | Comprehensive prompt-type showcase |
| `Example2.cs` | General | Tenant-restricted plugin |
| `Example3.cs` | General | Plugin context/metadata display |
| `ExampleBackgroundProcess.cs` | BackgroundProcess | Periodic logging example |
| `LoginWithPrompts.cs` | Auth | Username/password login flow |
| `UserUpdate.cs` | UserUpdate | User sync stub |

A `HelloWorld.plugin` / `HelloWorld.dll` pair demonstrates external assembly loading via a `.assemblies` sidecar file.

## Key classes

| Class | Purpose |
|-------|---------|
| `BackgroundProcessor` | `IHostedService` that executes `BackgroundProcess` plugins on a timer |
| `CustomAuthenticationHandler` | Pluggable authentication handler that defers to auth plugins |
| `CustomAuthIdentity` | `ClaimsIdentity` built by the custom auth system |
| `RouteHelper` | Maps controller routes |
| `DataController` | Split-file partial controller covering plugins, users, tenants, settings, auth, etc. |

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.DataAccess` | Business logic and data persistence |
| `FreePlugins.Plugins` | File-based plugin runtime |
| `FreePlugins.Client` | Blazor WASM client |
| `FreePlugins.Abstractions` | Compiled plugin contracts |
| `FreePlugins.Abstractions.Integration` | Compiled plugin bridge |
| All `*.ExamplePlugin` / `*.SamplePlugin` projects | Compiled plugins registered at startup |

## Notable NuGet packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Authentication.Google/Facebook/MicrosoftAccount/Apple/OpenIdConnect` | OAuth providers |
| `Microsoft.Azure.SignalR` | Azure-backed SignalR hub |
| `Serilog.Extensions.Logging.File` | File-based structured logging |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | Blazor WASM hosting |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |
| User Secrets ID | `e534f0ca-6a41-412e-bea9-d68886a17773` |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
