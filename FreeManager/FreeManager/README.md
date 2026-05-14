# FreeManager

ASP.NET Core web host for the FreeManager application platform.

This project is the server-side entry point. It hosts the Blazor WebAssembly client, exposes REST API controllers and a SignalR hub, configures the authentication pipeline, loads plugins from the `Plugins/` folder at startup, and runs an optional background service.

## Key Public Classes

| Class | Description |
|-------|-------------|
| `Program` | Startup: configures DI, auth, SignalR, plugin loader, background service, and Radzen/Blazor WASM hosting |
| `DataController` | Partial `ApiController` — base class and per-feature partials covering auth, users, departments, tenants, tags, files, encryption, UDFs, plugins, Ajax, settings |
| `SetupController` | First-run setup endpoints |
| `AuthorizationController` | Custom authorization helpers |
| `BackgroundProcessor` | `IHostedService` — runs periodic tasks; supports load-balancing filter via `MachineName` |
| `CustomAuthenticationHandler` | ASP.NET Core authentication handler for the custom cookie/token scheme |
| `freemanagerHub` (SignalR) | Real-time hub; supports Azure SignalR or local SignalR |
| `PluginsInterfaces` | Bridge between the server DI and the `Plugins.IPlugins` runtime |

## Blazor / Razor Pages

| Page | Route |
|------|-------|
| `CustomAuthentication.cshtml` | `/CustomAuthentication` |
| `CustomAuthenticationLogout.cshtml` | `/CustomAuthenticationLogout` |
| `PluginAuthentication.cshtml` | `/PluginAuthentication` |
| `PluginAuthenticationLogout.cshtml` | `/PluginAuthenticationLogout` |
| `Error.razor` (component) | `/Error` |

The Blazor WebAssembly client (`FreeManager.Client`) is served from this project and registered with `AddInteractiveWebAssemblyRenderMode`.

## Plugin System

Place `.cs` or `.plugin` files in the `Plugins/` folder. Supported built-in plugin types:

- `Auth` — custom authentication logic
- `BackgroundProcess` — tasks run by the background service
- `Example` — demonstration stubs
- `UserUpdate` — user-record hooks on login/update

Plugins with external DLL dependencies use a paired `.assemblies` file. See `Plugins/HelloWorld.*` for an example.

## Project References and NuGet Packages

**References:** `FreeManager.DataAccess`, `FreeManager.Plugins`, `FreeManager.Client`

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 10.0.1 |
| `Microsoft.AspNetCore.Authentication.OpenIdConnect` | 10.0.1 |
| `Microsoft.AspNetCore.Authentication.MicrosoftAccount` | 10.0.1 |
| `Microsoft.AspNetCore.Authentication.Google` | 10.0.1 |
| `Microsoft.AspNetCore.Authentication.Facebook` | 10.0.1 |
| `AspNet.Security.OAuth.Apple` | 10.0.0 |
| `Microsoft.Azure.SignalR` | 1.32.0 |
| `Serilog.Extensions.Logging.File` | 3.0.0 |

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Output type | Executable (web host) |
| IIS config | `web.config` included |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
