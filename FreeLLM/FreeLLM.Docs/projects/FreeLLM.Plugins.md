# FreeLLM.Plugins

Roslyn-based dynamic C# plugin runtime for FreeLLM.

Defines the `IPlugin` interface contract and the `Plugins` class which loads `.cs` and `.plugin` source files from the `Plugins/` folder at application startup, compiles them in-process with the Roslyn CSharp compiler, and exposes `ExecuteDynamicCSharpCode<T>()` for calling plugin methods at runtime. Server assembly references and `using` statements are injected at compile time from `Program.cs` configuration. The `Encryption` helper in this project is also used by `DataAccess` for credential management.

## Key classes

| Class / Interface | File | Purpose |
|---|---|---|
| `IPlugins` | `Plugins.cs` | Interface: `AllPlugins`, `Load(path)`, `ExecuteDynamicCSharpCode<T>()`, `ServerReferences`, `UsingStatements` |
| `Plugins` | `Plugins.cs` | Implements `IPlugins`; loads `.cs`/`.plugin` files, compiles via Roslyn, caches assembly bytes |
| `Plugin` | `Plugins.cs` | Metadata record for one loaded plugin (name, type, compiled assembly) |
| `Encryption` | `Encryption.cs` | AES-based encrypt/decrypt helpers consumed by `DataAccess` |

## Notable NuGet packages

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.CodeAnalysis.CSharp` | 4.14.0 | Roslyn C# compiler for dynamic plugin compilation |
| `Basic.Reference.Assemblies.Net90` | 1.8.3 | .NET 9 BCL reference assemblies required by Roslyn at runtime |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net9.0 |
| Nullable | enabled |

Part of the **FreeLLM** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A **runtime C# compiler**. At startup it loads `.cs` and `.plugin` source files from the `Plugins/` folder, compiles each in-process with Roslyn, and exposes `ExecuteDynamicCSharpCode<T>()` to call plugin methods on demand. The server injects its own assembly references and `using` statements at compile time so plugin code can see the app's types. The `Encryption` helper here is also shared with `DataAccess`.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Roslyn (`Microsoft.CodeAnalysis.CSharp`) | Compile plugin C# at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Plugins/Plugins.cs) |
| `Basic.Reference.Assemblies.Net90` | .NET 9 reference assemblies for the compile | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Plugins/Plugins.cs) |
| AES encryption (shared) | Credential/code protection | [Encryption.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Plugins/Encryption.cs) |

**Why does this exist?**
So one deployment can be extended without forking or redeploying the core app.

**What does it accomplish that other tools don't?**
- **Real compilation at runtime**, with the server injecting references so plugins have full access to app types.
- A shared **AES helper** reused by the data layer for credential handling.

**Terminology & "can I see it?"**
- **Roslyn** — the official C# compiler exposed as a library.
- **Reference assemblies** — the metadata Roslyn needs to compile against .NET.

**The hard part, drawn** — source files become callable code:

```
  startup ─▶ Plugins.Load("Plugins/") ─▶ Roslyn compile each .cs/.plugin ─▶ cache assembly bytes
        (server injects references + usings so plugins see app types)
        ExecuteDynamicCSharpCode<T>() ─▶ run a plugin method on demand
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
