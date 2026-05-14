# FreePlugins.UIExamplePlugin

Example compiled plugin project demonstrating the `ICompiledGeneralPlugin` interface pattern for UI-oriented plugins. The plugin implementation file (`UIExamplePlugin.cs`) is currently a placeholder — it reserves the project structure and NuGet package shape for a UI-focused plugin.

## Purpose

This project serves as a skeleton for plugins that need to render or interact with Blazor UI elements. It references `FreePlugins.Abstractions` and is wired into the main host as a compiled plugin project reference, making it discoverable by `AddPluginsFromAssembly`.

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.Abstractions` | Plugin contracts and DI helpers |

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
