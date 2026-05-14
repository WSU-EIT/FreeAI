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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
