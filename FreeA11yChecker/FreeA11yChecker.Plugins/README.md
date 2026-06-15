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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is a **runtime C# compiler**. At startup it reads `.cs` and `.plugin` files from a `PluginFiles/` folder, compiles them in memory with **Roslyn** using the running app's own assemblies as references, and exposes the resulting types as services — *no rebuild of the app required*. It also compiles arbitrary C# strings on demand (for the in-browser plugin editor) and caches compiled output, AES-256 encrypted, so unchanged plugins don't recompile on the next start.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Compile plugin C# at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Plugins/Plugins.cs) |
| `Basic.Reference.Assemblies.Net100` | .NET 10 reference assemblies for the compile | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Plugins/Plugins.cs) |
| AES-256 compile cache | Skip recompiling unchanged plugins | [Encryption.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Plugins/Encryption.cs) |

**Why does this exist?**
So one deployment can be extended per-customer — custom auth, post-scan webhooks, UI widgets — *without forking or redeploying the core app*.

**What does it accomplish that other tools don't?**
- **Real compilation at runtime**, not just config toggles — plugins are first-class C# with full access to the app's types.
- `.plugin` text files can reference **external DLLs** via an `.assemblies` sidecar, so plugins can pull in code that isn't in the solution.
- Compiled assemblies are **cached encrypted** on disk for fast warm starts.

**Terminology & "can I see it?"**
- **Roslyn** — the official C# compiler, exposed as a library you can call from your own code.
- **Plugin type** — the category that decides *when* a plugin runs: `Auth`, `BackgroundProcess`, `UserUpdate`, `Example`, or a Blazor component.
- **Sidecar** — a companion file (`.assemblies`) that lists extra DLLs a plugin needs.

**The hard part, drawn** — source files become live services at startup:

```
  startup ──▶ Plugins.Load("PluginFiles/")
                  │ read .cs / .plugin  (+ optional .assemblies sidecar)
                  ▼  Roslyn compile   (references = the running app's own assemblies)
        in-memory Assembly ──▶ extract types, sort by PluginType
                  │                   (Auth · BackgroundProcess · UserUpdate · Example · Blazor)
                  ▼
        injected into DataAccess + offered as DI services   ──cache──▶ AES-256 on disk
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
