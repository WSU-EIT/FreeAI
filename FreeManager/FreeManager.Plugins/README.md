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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
