# FreePlugins.Plugins

Class library that implements the Roslyn-based dynamic plugin runtime. It defines the `IPlugins` / `Plugins` types, the `Plugin` data model, and all supporting prompt/result types. The `DataAccess` project uses this library to compile and execute file-based plugin code at runtime.

## What it does

`Plugins.Load(string path)` scans a folder for `.cs` and `.plugin` files, compiles each one into an in-memory assembly using the Roslyn `CSharpCompilation` API, calls the `Properties()` method on the compiled type to read metadata (Id, Name, Type, Version, etc.), and returns a list of `Plugin` objects. Plugins marked with `ContainsSensitiveData = true` have their source code AES-encrypted before being stored in the cache.

`IPlugins.ExecuteDynamicCSharpCode<T>` takes a code string, an argument array, an optional list of additional assembly paths, and the namespace/class/method to invoke. It compiles the code against `Basic.Reference.Assemblies.Net100` plus any server assemblies and user-specified additional assemblies, then invokes the named method via reflection. Async methods (tasks) are polled to completion.

## Key types in `Plugins.cs`

| Type | Purpose |
|------|---------|
| `IPlugins` | Interface: `Load`, `ExecuteDynamicCSharpCode<T>`, `AllPlugins`, `AllPluginsForCache`, `PluginFolder`, `ServerReferences`, `UsingStatements` |
| `Plugins` | Concrete implementation |
| `Plugin` | Plugin descriptor: `Id`, `Name`, `Type`, `Version`, `Author`, `Code` (possibly encrypted), `ClassName`, `Namespace`, `Invoker`, `Prompts`, `PromptValues`, `Properties`, `LimitToTenants`, `ContainsSensitiveData`, `AdditionalAssemblies`, `SortOrder` |
| `PluginExecuteRequest` | Request wrapper: `Plugin`, `Objects` |
| `PluginExecuteResult` | Result wrapper: `bool Result`, `List<string> Messages`, `List<object> Objects` |
| `PluginPrompt` | Prompt definition: `Name`, `Type` (enum), `Description`, `DefaultValue`, `Required`, `Hidden`, `Options`, `Function`, `ElementClass` |
| `PluginPromptOption` | `Label` / `Value` pair |
| `PluginPromptValue` | Collected value: `Name`, `Values` (string[]) |
| `PluginPromptType` | Enum with 16 types: Button, Checkbox, CheckboxList, Date, DateTime, File, Files, HTML, Multiselect, Number, Password, Radio, Select, Text, Textarea, Time |

## `.assemblies` sidecar files

A plugin file may have a matching `.assemblies` file listing extra assemblies to load. Entries can be a path to a DLL (relative, starting with `.\`) or a `typeof(...)` expression that is itself dynamically compiled and evaluated to resolve the file path at runtime.

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `Microsoft.CodeAnalysis.CSharp` | `5.0.0` |
| `Basic.Reference.Assemblies.Net100` | `1.8.4` |

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
