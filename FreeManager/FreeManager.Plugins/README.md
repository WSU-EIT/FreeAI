# FreeManager.Plugins

Roslyn-based dynamic plugin runtime for the FreeManager platform.

Defines the `IPlugin` contract and the `IPlugins` loader/executor interface. At startup, `FreeManager` (the web host) calls `Plugins.Load(path)` which scans the `Plugins/` folder for `.cs` and `.plugin` files, compiles them with the Microsoft C# compiler (`Microsoft.CodeAnalysis.CSharp`), and caches the resulting assemblies. Plugins can then be invoked by name anywhere in the `DataAccess` layer.

The runtime also exposes `ExecuteDynamicCSharpCode<T>` for on-demand code execution — used by the plugin auth handler, background process plugins, and user-update hooks.

## Key Public Classes

| Class | Description |
|-------|-------------|
| `IPlugins` | Service interface: `Load`, `ExecuteDynamicCSharpCode<T>`, `AllPlugins`, `ServerReferences`, `UsingStatements` |
| `Plugins` | Concrete implementation; scans folder, compiles `.cs`/`.plugin` files, caches assemblies |
| `Plugin` | Represents a single loaded plugin: name, type, compiled assembly, source path |
| `IPlugin` | Contract that plugin classes must implement |
| `Encryption` | Encryption helpers available to plugins |

## Plugin Types (built-in)

| Type | Description |
|------|-------------|
| `Auth` | Custom login / authentication logic |
| `BackgroundProcess` | Periodic task executed by the background service |
| `Example` | Demonstration stub |
| `UserUpdate` | Fires on user record create/update |
| `LoginWithPrompts` | Login flow with additional prompts |

## External DLL Plugins

A plugin can reference external assemblies by pairing a `.plugin` file with a `.assemblies` file. Each line in the `.assemblies` file is either a relative path to a DLL or a C# expression that evaluates to an assembly location at runtime:

```
.\HelloWorld\HelloWorld.dll
typeof(SomeNameSpace.SomeProperty).Assembly.Location
```

## Project References and NuGet Packages

| Package | Version |
|---------|---------|
| `Microsoft.CodeAnalysis.CSharp` | 5.0.0 |
| `Basic.Reference.Assemblies.Net100` | 1.8.4 |

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A **runtime C# compiler**. At startup the web host calls `Plugins.Load(path)`, which scans the `Plugins/` folder for `.cs`/`.plugin` files, compiles them with Roslyn, and caches the assemblies; the data layer can then invoke any plugin by name. `ExecuteDynamicCSharpCode<T>` runs code on demand (used by auth, background-process, and user-update plugins). External DLLs are referenced via a `.assemblies` sidecar.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Compile plugin C# at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Plugins/Plugins.cs) |
| `Basic.Reference.Assemblies.Net100` | .NET 10 reference assemblies for the compile | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Plugins/Plugins.cs) |

**Why does this exist?**
So one deployment can be extended without forking or redeploying — including the `LoginWithPrompts` flow and custom background jobs.

**What does it accomplish that other tools don't?**
- **Real compilation at runtime** for five plugin types (`Auth`, `BackgroundProcess`, `Example`, `UserUpdate`, `LoginWithPrompts`).
- External-DLL support via `.assemblies` sidecars (a path *or* a `typeof(...).Assembly.Location` expression).

**Terminology & "can I see it?"**
- **Roslyn** — the official C# compiler exposed as a library.
- **Sidecar** — a `.assemblies` file listing extra DLLs a plugin needs.

**The hard part, drawn** — source files become callable plugins:

```
  startup ─▶ Plugins.Load("Plugins/") ─▶ Roslyn compile .cs/.plugin (+ .assemblies sidecar) ─▶ cache assemblies
        types: Auth · BackgroundProcess · Example · UserUpdate · LoginWithPrompts
        ExecuteDynamicCSharpCode<T>() ─▶ run a plugin method on demand
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
