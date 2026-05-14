# FreeA11yChecker.Plugins

Roslyn-based dynamic C# compilation host for FreeA11yChecker. Loads `.cs` and `.plugin` files from a configured `PluginFiles/` directory at startup, compiles them in-process using Roslyn, and makes the resulting types available to the application as injectable services. Also compiles and executes arbitrary C# strings at runtime for the in-UI plugin editor.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Class library |

## What It Does

- `Plugins.Load(path)` scans `PluginFiles/` for `.cs` files and `.plugin` files. `.cs` files are compiled with Roslyn using the server's loaded assemblies as references. `.plugin` files are plain-text C# with an optional `.assemblies` sidecar listing external DLL paths to include.
- Plugin types are categorized by their declared `PluginType` property: `Auth`, `BackgroundProcess`, `UserUpdate`, `Example`, or Blazor components.
- `Plugins.ExecuteDynamicCSharpCode<T>(code, objects, additionalAssemblies, namespace, classname, invokerFunction)` compiles a C# string on demand and invokes a named method, returning a typed result. Used by the plugin editor in the web UI.
- Compiled plugins are cached (AES-256 encrypted on disk via `Encryption.cs`) so repeated startups don't recompile unchanged files.
- `ServerReferences` and `UsingStatements` are injected at startup by the web host so compiled plugin code has access to all application types and standard namespaces.

## Key Classes / Methods

| Class / Interface | Purpose |
|---|---|
| `IPlugins` | Service interface; exposes `AllPlugins`, `Load(path)`, `ExecuteDynamicCSharpCode<T>`, `ServerReferences`, `UsingStatements`, `PluginFolder` |
| `Plugins` | Concrete implementation of `IPlugins`; handles file discovery, Roslyn compilation, cache encryption, and dynamic invocation |
| `Plugin` | Represents one loaded plugin: `PluginType`, `Name`, `Code`, compiled `Assembly`, and extracted `Type` objects |
| `Encryption.cs` | AES-256 encryption/decryption helpers used to cache compiled plugin assemblies on disk |

## Plugin Types

| Type | Trigger |
|---|---|
| `Auth` | Custom authentication strategy; invoked by `DataAccess.Authenticate` as a fallback |
| `BackgroundProcess` | Called on every background service tick; used for post-scan webhooks, custom alerting |
| `UserUpdate` | Fires when `SaveUser` is called; used for custom profile sync or notifications |
| `Example` | General-purpose extensibility hook |
| Blazor component | `.blazor`/`.razor` files compiled as Blazor components and rendered dynamically in the UI |

## Project References

None — this library has no project references.

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler |
| `Basic.Reference.Assemblies.Net100` | .NET 10 BCL reference assemblies for Roslyn compilation |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
