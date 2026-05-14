# FreeBlazorExample.Plugins

Roslyn-based dynamic C# plugin runtime for FreeBlazorExample.

Defines `IPlugin`, `IPlugins`, and the `Plugins` loader class. At startup the host scans `PluginFiles/` for `.cs` and `.plugin` source files, compiles them in-process with Roslyn, and registers them as DI-injectable services. Supports four built-in plugin types: `Auth`, `BackgroundProcess`, `Example`, and `UserUpdate`. The `ExecuteDynamicCSharpCode<T>()` method accepts runtime-injected assembly references and `using` statements from appsettings.

## Key classes

| Class / Interface | Purpose |
|---|---|
| `IPlugins` | Interface: `AllPlugins`, `Load(path)`, `ExecuteDynamicCSharpCode<T>()`, `ServerReferences`, `UsingStatements` |
| `Plugins` | Implements `IPlugins`; file scanning, Roslyn compilation, assembly caching |
| `Plugin` | Metadata record for one loaded plugin |
| `Encryption` | AES encrypt/decrypt helpers used by `DataAccess` |

## Notable NuGet packages

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.CodeAnalysis.CSharp` | 5.0.0 | Roslyn C# compiler |
| `Basic.Reference.Assemblies.Net100` | 1.8.4 | .NET 10 BCL reference assemblies for Roslyn |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net10.0 |
| Nullable | enabled |

Part of the **FreeBlazorExtended** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
