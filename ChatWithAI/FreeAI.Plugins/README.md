# FreeAI.Plugins

Class library — Roslyn-based dynamic plugin runtime.

`Plugins.cs` implements `IPlugins` and `Plugins`, which load `.cs` and `.plugin` source files from a folder at startup, compile each one in-process via Roslyn (`Microsoft.CodeAnalysis.CSharp`), call its `Properties()` method to extract metadata (id, name, author, type, version, prompts, tenant restrictions), and store the resulting `Plugin` objects in memory. `ExecuteDynamicCSharpCode<T>` is the generic method that compiles and invokes any C# snippet at runtime, resolving references from `Basic.Reference.Assemblies.Net90` plus a server-supplied list of additional assembly paths. Plugins that declare `ContainsSensitiveData = true` have their source code AES-encrypted before being sent to the browser. `Encryption.cs` provides the AES helper used to encrypt/decrypt plugin code. The `Plugin`, `PluginPrompt`, `PluginPromptValue`, `PluginPromptOption`, and `PluginExecuteResult` model classes are defined here and referenced by both the server and client via `FreeAI.DataObjects`.

## Key public classes

| Class | Purpose |
|---|---|
| `IPlugins` | Service interface injected into the ASP.NET Core DI container |
| `Plugins` | Concrete implementation; loads, compiles, and executes plugins |
| `Plugin` | Plugin descriptor (id, code, prompts, type, version, tenant restrictions) |
| `PluginPrompt` / `PluginPromptOption` | UI prompt definitions collected before plugin execution |
| `PluginPromptValue` | Holds user-supplied values for a prompt |
| `PluginExecuteResult` | Return type for plugin execution (result flag, messages, objects) |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler used to compile and execute plugin source at runtime |
| `Basic.Reference.Assemblies.Net90` | Pre-packaged .NET 9 reference assemblies for the Roslyn compilation context |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net9.0` |
| Output type | Library |

Part of the ChatWithAI solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
