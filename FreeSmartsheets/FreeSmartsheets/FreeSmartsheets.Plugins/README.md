# FreeSmartsheets.Plugins

Dynamic C# plugin runtime for FreeSmartsheets. Provides the plugin contract interfaces, the Roslyn-based plugin loader/executor, and the `Plugin` object model shared between the server and client.

## What It Does

- Implements `IPlugins` / `Plugins` — loaded at server startup to scan the `PluginFiles/` directory for `.cs` and `.plugin` files
- Uses Roslyn (`Microsoft.CodeAnalysis.CSharp`) to compile each plugin file into an in-memory assembly at startup and cache it
- Executes compiled plugins via `ExecuteDynamicCSharpCode<T>` — invokes a named method on the compiled class and returns a typed result
- Supports external assembly references via `.assemblies` sidecar files (paths or `typeof()` expressions)
- Supports Blazor UI plugins (`.razor` files in `PluginFiles/BlazorComponents/`) — these are sent to the client for in-browser compilation
- Encrypts plugin code marked with `ContainsSensitiveData = true` before the plugin list is sent to the browser
- Defines built-in plugin types: `Auth`, `BackgroundProcess`, `UserUpdate`, `Example`, and `Blazor`

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `IPlugins` | Interface for DI / mocking — all public methods declared here |
| `Plugins.Load` | Scans a folder, compiles `.cs`/`.plugin` files, and returns the loaded plugin list |
| `Plugins.ExecuteDynamicCSharpCode<T>` | Roslyn-compiles a code block and invokes a named method, returning typed `T` |
| `Plugins.AllPlugins` | Returns the full list of loaded `Plugin` objects |
| `Plugins.AllPluginsForCache` | Returns the list with sensitive code encrypted, safe to send to the database |
| `Plugin` | Plugin descriptor: `Id`, `Name`, `Type`, `Code`, `Prompts`, `AdditionalAssemblies` |
| `PluginPrompt` / `PluginPromptValue` | Defines runtime prompt inputs collected from users before plugin execution |

## Project References

None — this project has no local project references.

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler for runtime plugin compilation |
| `Basic.Reference.Assemblies.Net100` | .NET 10 BCL reference assemblies required by the Roslyn compiler |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
