# FreePlugins

Plugin authoring and testing solution for the FreeCRM/FreePlugins ecosystem. Contains the v1 plugin SDK (abstractions plus example plugins) and a full reference copy of the FreeCRM application the plugins target.

## Solution Layout

| Project | Purpose |
|---------|---------|
| `FreePlugins` | ASP.NET Core / Blazor Server host — the main web application |
| `FreePlugins.Client` | Blazor WebAssembly client (UI pages and components) |
| `FreePlugins.DataAccess` | Server-side data access layer (EF Core, business logic, integrations) |
| `FreePlugins.DataObjects` | Shared DTOs used by both server and WASM client |
| `FreePlugins.EFModels` | Entity Framework Core models, DbContext, and migrations |
| `FreePlugins.Plugins` | Roslyn-based dynamic plugin runtime (file-based `.cs`/`.plugin` plugins) |
| `FreePlugins.Abstractions` | NuGet-first plugin contracts and DI helpers |
| `FreePlugins.Abstractions.Integration` | Bridges compiled plugins into the existing file-based plugin pipeline |
| `FreePlugins.SamplePlugin` | Sample compiled `BackgroundProcess` plugin |
| `FreePlugins.ExamplePlugin` | Example `General` plugin showing all 16 prompt types |
| `FreePlugins.AuthExamplePlugin` | Example `Auth` plugin with Login/Logout and username/password prompts |
| `FreePlugins.DataAccessExamplePlugin` | Example `General` plugin that dumps plugin context and metadata |
| `FreePlugins.TenantRestrictedPlugin` | Example `General` plugins demonstrating `LimitToTenants` |
| `FreePlugins.UIExamplePlugin` | Example UI plugin placeholder |
| `FreePlugins.UserUpdateExamplePlugin` | Example `UserUpdate` plugin for syncing users from external systems |

## Plugin Types

The FreePlugins system supports four plugin types, each with a corresponding invoker method:

| Type | Invoker | Interface |
|------|---------|-----------|
| `General` | `Execute` | `IPlugin` / `ICompiledGeneralPlugin` |
| `Auth` | `Login` / `Logout` | `IPluginAuth` / `ICompiledAuthPlugin` |
| `BackgroundProcess` | `Execute(iteration)` | `IPluginBackgroundProcess` / `ICompiledBackgroundProcessPlugin` |
| `UserUpdate` | `UpdateUser` | `IPluginUserUpdate` / `ICompiledUserUpdatePlugin` |

## File-Based vs. Compiled Plugins

**File-based plugins** (`.cs` or `.plugin` files in the `Plugins/` folder) are loaded and compiled at startup using Roslyn. They implement `Properties()` to return a `Dictionary<string, object>` describing the plugin.

**Compiled plugins** (NuGet packages) implement interfaces from `FreePlugins.Abstractions`, use the `[Plugin]` attribute for metadata, and are registered in `Program.cs`:

```csharp
// Register individual plugin
builder.Services.AddPlugin<MyPlugin>();

// Or auto-discover from assembly
builder.Services.AddPluginsFromAssembly(typeof(MyPlugin).Assembly);

var app = builder.Build();
app.LoadCompiledPlugins();
```

## Build

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` / `Microsoft.NET.Sdk.Web` |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
