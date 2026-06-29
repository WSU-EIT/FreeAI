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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** This solution shows **two ways to write a plugin for the same host**: (1) **file-based** — drop a `.cs`/`.plugin` file into the `Plugins/` folder; Roslyn compiles it at startup; it implements a `Properties()` method describing itself. (2) **compiled** — a real NuGet package that implements interfaces from `FreePlugins.Abstractions`, carries a `[Plugin]` attribute, and is registered in `Program.cs`. An integration bridge makes both kinds appear *identical* to the host. There are four plugin types — `General`, `Auth`, `BackgroundProcess`, `UserUpdate`.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Plugin contracts (the SDK) | Interfaces + metadata for compiled plugins | [FreePlugins.Abstractions/IPluginInterfaces.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions/IPluginInterfaces.cs) |
| Roslyn file-plugin runtime | Compile `.cs`/`.plugin` files at startup | [FreePlugins.Plugins/Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Plugins/Plugins.cs) |
| The integration bridge | Make compiled plugins join the file pipeline | [FreePlugins.Abstractions.Integration/](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions.Integration) |
| The web host | Loads both, runs them | [FreePlugins/](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/FreePluginsV1/FreePlugins) |

**Why does this exist?** Two authoring models serve two needs: **file-drop** plugins are fast for ad-hoc/per-deployment tweaks; **compiled NuGet** plugins are versioned, testable, and distributable. This shows how to support both at once.

**What does it accomplish that other tools don't?**
- **Two plugin systems side-by-side**, unified by a bridge so the host doesn't care which kind a plugin is.
- A rich, declarative **prompt system** — 16 input types (text, date, file, multiselect…) a plugin can request before it runs.
- Compiled plugins reference **only the SDK** (`FreePlugins.Abstractions`), not the host — so anyone can publish one.

**Terminology & "can I see it?"**
- **File-based plugin** — a source file compiled at startup by Roslyn.
- **Compiled plugin** — a pre-built NuGet package implementing the SDK interfaces.
- **`[Plugin]` attribute** — declares a compiled plugin's metadata.
- **Prompt** — an input the plugin asks the user for before executing.

**The hard part, drawn** — two authoring paths, one unified pipeline:

```
  AUTHOR a plugin two ways:
    A) drop Foo.cs in Plugins/  ──Roslyn compiles at startup──┐
    B) NuGet pkg : implements FreePlugins.Abstractions ───────┤
                   + [Plugin] attribute + AddPlugin<>()        │
                                                               ▼
                              Abstractions.Integration bridge (one registry)
                                                               ▼
                    HOST sees ALL plugins identically ─▶ run General/Auth/BackgroundProcess/UserUpdate
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
