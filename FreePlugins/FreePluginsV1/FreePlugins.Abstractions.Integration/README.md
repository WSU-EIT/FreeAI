# FreePlugins.Abstractions.Integration

Class library that bridges the compiled NuGet-based plugin system (`FreePlugins.Abstractions`) with the existing Roslyn file-based plugin pipeline (`FreePlugins.Plugins` / `FreePlugins.DataAccess`). Add this project to the web host to enable both plugin systems side-by-side.

## What it does

The existing system loads `.cs` and `.plugin` files at startup, compiles them with Roslyn, and executes them dynamically. This integration layer lets compiled plugins (installed as NuGet packages) participate in the same pipeline without modifying any existing code.

| Component | Purpose |
|-----------|---------|
| `CompiledPluginRegistry` | Thread-safe static registry that holds all compiled plugins loaded at startup. Exposes `AllPlugins`, `GetById(Guid)`, `GetByType(string)`, `IsCompiledPlugin(Guid)` |
| `CompiledPluginHostExtensions.LoadCompiledPlugins<T>()` | `IHost` extension method — reads all `CompiledPluginRegistration` services and copies them into `CompiledPluginRegistry` |
| `CompiledPluginConverter.ToPlugin(PluginMetadata)` | Converts a `PluginMetadata` object into a `Plugins.Plugin` so compiled plugins appear identically to file-based ones in the existing data layer |
| `CompiledPluginDataAccessExtensions` | Extension methods on `IDataAccess`: `ExecuteCompiledBackgroundProcessAsync`, `ExecuteCompiledPluginAsync`, `GetAllPluginsIncludingCompiled`, `GetAllPluginsWithoutCodeIncludingCompiled` |
| `CompiledPluginHelper` | `ShouldUseCompiledExecution(PluginExecuteRequest)` / `ShouldUseCompiledExecution(Guid)` — determines dispatch path |

## Startup wiring (Program.cs)

```csharp
// 1. Register compiled plugins in DI
builder.Services.AddPlugin<MyPlugin>();
// or
builder.Services.AddPluginsFromAssembly(typeof(MyPlugin).Assembly);

var app = builder.Build();

// 2. Load them into the static registry
app.LoadCompiledPlugins();

app.Run();
```

## Execution flow

When a plugin request arrives the host checks `CompiledPluginHelper.ShouldUseCompiledExecution(pluginId)`. If true, it calls one of the `IDataAccess` extension methods which resolve the plugin type from DI, build the appropriate context, and dispatch to the correct interface method (`ExecuteAsync`, `LoginAsync`, `LogoutAsync`, or `UpdateUserAsync`).

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.Abstractions` | Compiled plugin contracts |
| `FreePlugins.Plugins` | Existing file-based plugin runtime and `Plugin` model |
| `FreePlugins.DataAccess` | `IDataAccess` interface extended by this project |

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.Hosting.Abstractions` | 10.0.0-preview.4 |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class library |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
