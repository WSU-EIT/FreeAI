# FreePlugins -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure (FreePluginsV1)

| Project | Role |
|---------|------|
| `FreePlugins` | ASP.NET Core host; loads plugins at startup, REST API, SignalR |
| `FreePlugins.Client` | Blazor WASM; plugin list, test UI, standard admin pages |
| `FreePlugins.DataAccess` | Business logic; EF Core, plugin execution bridge |
| `FreePlugins.DataObjects` | Shared DTOs |
| `FreePlugins.EFModels` | EF Core DbContext; PluginCaches, core tables |
| `FreePlugins.Plugins` | Roslyn compiler runtime; `IPlugin`, `Plugins` loader class |
| `FreePlugins.Abstractions` | NuGet package; public `IPlugin` contract for external authors |
| `FreePlugins.Abstractions.Integration` | NuGet package; host-side DI bridge |

## Plugin lifecycle

```
App startup
  -> Plugins.Load(pluginFiles/, serverReferences)
       -> foreach .cs / .plugin file:
            -> Roslyn CSharpCompilation.Create(...)
            -> Assembly.Load(emitResult.GetAssemblyImage())
            -> cast to IPlugin, call Properties()
            -> if ContainsSensitiveData: AES-encrypt before browser delivery
  -> inject compiled plugins into DataAccess DI
  -> make each plugin available as its own DI service
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*