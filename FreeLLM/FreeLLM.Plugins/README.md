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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
