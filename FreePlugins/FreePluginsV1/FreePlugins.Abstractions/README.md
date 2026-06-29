# FreePlugins.Abstractions

NuGet-publishable class library containing all contracts, interfaces, and DI helpers needed to author compiled FreePlugins plugins. Plugin authors reference only this package — they do not need a dependency on the host application.

## Interfaces

### Core plugin interfaces

| Interface | Extends | Purpose |
|-----------|---------|---------|
| `IPluginBase` | — | Base for all plugins. Requires `Dictionary<string, object> Properties()` |
| `IPlugin` | `IPluginBase` | General plugin. `Task<PluginResult> ExecuteAsync(IPluginContext context)` |
| `IPluginAuth` | `IPluginBase` | Auth plugin. `Task<PluginResult> LoginAsync(IPluginAuthContext)` and `LogoutAsync(IPluginAuthContext)` |
| `IPluginBackgroundProcess` | `IPluginBase` | Background process. `Task<PluginResult> ExecuteAsync(IPluginContext context, long iteration)` |
| `IPluginUserUpdate` | `IPluginBase` | User sync plugin. `Task<PluginResult> UpdateUserAsync(IPluginUserContext context)` |

### Compiled plugin marker interfaces

| Interface | Extends | Purpose |
|-----------|---------|---------|
| `ICompiledPlugin` | `IPluginBase` | Marker for NuGet-based plugins; requires `static abstract Type PluginType { get; }` |
| `ICompiledGeneralPlugin` | `ICompiledPlugin`, `IPlugin` | Compiled general plugin |
| `ICompiledAuthPlugin` | `ICompiledPlugin`, `IPluginAuth` | Compiled auth plugin |
| `ICompiledBackgroundProcessPlugin` | `ICompiledPlugin`, `IPluginBackgroundProcess` | Compiled background plugin |
| `ICompiledUserUpdatePlugin` | `ICompiledPlugin`, `IPluginUserUpdate` | Compiled user update plugin |

### Context interfaces

| Interface | Extends | Key members |
|-----------|---------|-------------|
| `IPluginContext` | — | `Plugin` (metadata), `Services`, `GetService<T>()`, `GetRequiredService<T>()`, `LogInfo/Warning/Error()` |
| `IPluginAuthContext` | `IPluginContext` | Adds `Url`, `TenantId` (Guid), `HttpContext` |
| `IPluginUserContext` | `IPluginContext` | Adds `User` (object?) |

## Key types

| Type | Purpose |
|------|---------|
| `PluginResult` | Record returned by all plugin methods. `bool Result`, `List<string>? Messages`, `IEnumerable<object>? Objects`. Static factory methods: `Success()`, `Success(messages)`, `Success(messages, objects)`, `Failure()`, `Failure(message)` |
| `PluginMetadata` | Runtime metadata for a plugin: `Id`, `Name`, `Type`, `Version`, `Author`, `Description`, `Enabled`, `IsCompiled`, `LimitToTenants`, `Prompts`, `Properties` |
| `PluginAttribute` | Class attribute that provides metadata declaratively. Maps to `PluginMetadata` via `ToMetadata(Type)` |
| `PluginTypes` | Constants: `General`, `Auth`, `BackgroundProcess`, `UserUpdate` |
| `PluginPromptBuilder` | Fluent builder for `PluginPrompt` objects. Static factories for all 16 prompt types |
| `PluginPrompt` | Input prompt configuration: `Name`, `PromptType`, `Description`, `Required`, `Hidden`, `Options`, `Function`, `SortOrder` |
| `PluginPromptType` | Enum: `Button`, `Checkbox`, `CheckboxList`, `Date`, `DateTime`, `File`, `Files`, `HTML`, `Multiselect`, `Number`, `Password`, `Radio`, `Select`, `Text`, `Textarea`, `Time` |
| `CompiledPluginExecutor` | DI-aware executor — dispatches to `ExecuteAsync`, `ExecuteLogin`, `ExecuteLogout`, `ExecuteUserUpdate`, `ExecuteBackgroundProcess` |
| `PluginContext` / `PluginAuthContext` / `PluginUserContext` | Concrete implementations of the context interfaces |
| `CompiledPluginRegistration` | Record `(Type PluginType, PluginMetadata Metadata)` stored in DI |

## DI registration helpers (`PluginServiceCollectionExtensions`)

```csharp
// Register a single plugin
services.AddPlugin<MyPlugin>();

// Scan an assembly and register all [Plugin]-decorated types
services.AddPluginsFromAssembly(assembly);

// Scan the calling assembly
services.AddPluginsFromCallingAssembly();

// Retrieve all registrations from the provider
services.GetCompiledPlugins();
```

## NuGet package details

| Field | Value |
|-------|-------|
| Package ID | `FreePlugins.Abstractions` |
| Version | `1.0.0` |
| Authors | WSU EIT |
| License | MIT |
| Target framework | `net10.0` |

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.0-preview.4 |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.0-preview.4 |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class library |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** This is the **SDK** a plugin author references — and *only* this; no dependency on the host app. It defines the interfaces a plugin implements (`IPlugin`, `IPluginAuth`, `IPluginBackgroundProcess`, `IPluginUserUpdate`), the `PluginResult` they return, the `[Plugin]` attribute that declares metadata, a context object plugins use to log and resolve services, 16 typed input prompts with a fluent builder, and DI helpers to register/discover plugins.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Plugin interfaces | The contracts a plugin implements | [IPluginInterfaces.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions/IPluginInterfaces.cs) · [IPluginBase.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions/IPluginBase.cs) |
| Plugin context | Logging + service resolution for plugins | [IPluginContext.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions/IPluginContext.cs) |
| DI registration helpers | `AddPlugin<T>()` / `AddPluginsFromAssembly()` | [the Abstractions project](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/FreePluginsV1/FreePlugins.Abstractions) |

**Why does this exist?** So anyone can build a distributable plugin against a **stable, host-free contract** — reference the NuGet package, implement an interface, publish.

**What does it accomplish that other tools don't?**
- A **clean contract with no host dependency** — plugins are decoupled from the app's internals.
- **16 typed prompt types** via a fluent builder — rich input UIs declared in code.
- **Auto-discovery**: `AddPluginsFromAssembly` registers every `[Plugin]`-decorated type.

**Terminology & "can I see it?"**
- **`PluginResult`** — the standard return: success flag + messages + objects.
- **Marker interface** (`ICompiledPlugin`) — flags a NuGet plugin and pins its `PluginType`.
- **Prompt builder** — fluent helpers to declare the inputs a plugin needs.

**The hard part, drawn** — author once, the host discovers it:

```
  your NuGet plugin ──references──▶ FreePlugins.Abstractions (this SDK)
        implements IPlugin/IPluginAuth/… + [Plugin] attribute + declares Prompts (16 types)
        ▼
  host: AddPlugin<T>() / AddPluginsFromAssembly() ─▶ registered ─▶ runs returning PluginResult
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
