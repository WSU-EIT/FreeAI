# FreeServicesHub.Plugins

The Roslyn-based dynamic plugin runtime for FreeServicesHub. Loads `.plugin` definition files and `.assemblies` manifests from the `PluginFiles/` folder at startup, compiles plugin C# source code on demand via `Microsoft.CodeAnalysis.CSharp`, and provides an `IPlugins` interface that the web host and DataAccess inject to execute plugins.

## What it does

`Plugins.Load(path)` scans the plugin folder for `.plugin` files (JSON descriptors), reads their associated C# source, and registers them in the `AllPlugins` list. When a plugin is invoked, `ExecuteDynamicCSharpCode<T>` compiles the source string with Roslyn, loads server assembly references (injected by the hub's `Program.cs`), and invokes the specified method on the compiled type.

Three plugin contracts are defined in `FreeServicesHub` (server project's `PluginsInterfaces.cs`):

- `IPlugin` — general-purpose plugin with an `Execute` method
- `IPluginAuth` — authentication plugin with `Login` and `Logout` methods
- `IPluginBackground` — background-process plugin

`Encryption.cs` provides AES/RSA encryption helpers used by DataAccess for encrypted settings storage. `IEncryption` defines encrypt, decrypt, and byte-array conversion operations.

## Key public classes

| Class / Interface | Purpose |
|---|---|
| `IPlugins` | DI interface; exposes `AllPlugins`, `Load`, `ExecuteDynamicCSharpCode<T>`, `ServerReferences`, `UsingStatements` |
| `Plugins` (implementation) | Loads plugin descriptors and executes Roslyn-compiled code |
| `Plugin` | Plugin descriptor: name, type, source code, metadata |
| `IEncryption` | Encryption/decryption interface (AES/RSA, byte-array, object serialization) |
| `Encryption` (implementation) | Cryptographic helpers used by DataAccess |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — class library |
| Target framework | net10.0 |

## Project references

None (no project references — this is a leaf library).

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler for dynamic plugin execution |
| `Basic.Reference.Assemblies.Net100` | BCL reference assemblies required for Roslyn compilation targeting net10.0 |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A **runtime C# compiler**. `Plugins.Load(path)` scans `PluginFiles/` for `.plugin` descriptors + their C# source, registers them, and `ExecuteDynamicCSharpCode<T>` compiles a plugin's source with Roslyn (using server-injected references) and invokes the named method. It also provides the `Encryption` helper (AES/RSA) the data layer uses for encrypted settings.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Compile plugin C# at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Plugins/Plugins.cs) |
| AES / RSA encryption | Encrypted settings storage | [Encryption.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.Plugins/Encryption.cs) |

**Why does this exist?**
So custom behavior — a `IPluginBackground` job, a `IPluginAuth` login, a general `IPlugin` — can be added without recompiling or redeploying the hub.

**What does it accomplish that other tools don't?**
- **Real runtime compilation** behind three typed contracts (`IPlugin`, `IPluginAuth`, `IPluginBackground`).
- A shared **AES/RSA** helper reused by the data layer for at-rest encryption.

**Terminology & "can I see it?"**
- **Roslyn** — the official C# compiler exposed as a library.
- **Plugin contract** — the interface a plugin implements (`Execute`, `Login/Logout`, …).

**The hard part, drawn** — descriptor + source become a callable plugin:

```
  startup ─▶ Plugins.Load("PluginFiles/") ─▶ read .plugin descriptors + C# source ─▶ register
        run ─▶ ExecuteDynamicCSharpCode<T>(): Roslyn compile (server refs injected) ─▶ invoke method
        contracts: IPlugin · IPluginAuth · IPluginBackground
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
