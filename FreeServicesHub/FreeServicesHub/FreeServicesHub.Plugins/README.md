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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
