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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A **runtime C# compiler**. At startup it scans `PluginFiles/` for `.cs` and `.plugin` files, compiles each in-memory with Roslyn, and exposes `ExecuteDynamicCSharpCode<T>()` to invoke plugin methods on demand. It supports external DLLs via `.assemblies` sidecars, Blazor UI plugins (`.razor` files sent to the browser for in-browser compilation), and AES-encrypts plugin code marked `ContainsSensitiveData = true` before it leaves the server.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Compile plugin C# at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.Plugins/Plugins.cs) |
| `Basic.Reference.Assemblies.Net100` | .NET 10 reference assemblies for the compile | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.Plugins/Plugins.cs) |

**Why does this exist?**
So one deployment can be extended per-customer — including custom Smartsheet reporting widgets once that integration lands — without forking or redeploying the core app.

**What does it accomplish that other tools don't?**
- **Real compilation at runtime** for five plugin types (`Auth`, `BackgroundProcess`, `UserUpdate`, `Example`, `Blazor`).
- Blazor UI plugins compiled **in the browser**; sensitive code AES-protected; external DLLs via sidecars.

**Terminology & "can I see it?"**
- **Roslyn** — the official C# compiler exposed as a library.
- **Sidecar** — a `.assemblies` file listing extra DLLs a plugin needs.

**The hard part, drawn** — source files become live plugins:

```
  startup ─▶ Plugins.Load("PluginFiles/") ─▶ Roslyn compile .cs/.plugin (+ .assemblies sidecars)
        Blazor (.razor) plugins ─▶ sent to the browser for in-browser compile
        ContainsSensitiveData ─▶ AES-encrypt before sending  ;  ExecuteDynamicCSharpCode<T>() runs a method
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
