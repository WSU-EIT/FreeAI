# FreePlugins

Plugin authoring and testing workspace for the WSU-EIT FreePlugins ecosystem. This directory contains two sub-projects:

| Project | Description |
|---------|-------------|
| `FreePluginsV1/` | The full FreePlugins v1 solution — FreeCRM-based host application, Blazor WASM client, data layer, Roslyn plugin runtime, compiled NuGet plugin SDK, and all example plugins |
| `BlazorApp1/` | Throwaway Blazor Server template application used as a scan/test target by analysis tools |

## FreePluginsV1 solution

The `FreePluginsV1/` sub-directory holds the complete solution. See `FreePluginsV1/README.md` for a full project-by-project breakdown.

Quick reference:

- **Plugin contracts**: `FreePlugins.Abstractions`
- **Integration bridge**: `FreePlugins.Abstractions.Integration`
- **File-based plugin runtime**: `FreePlugins.Plugins`
- **Web host**: `FreePlugins`
- **Blazor WASM client**: `FreePlugins.Client`
- **Data layer**: `FreePlugins.DataAccess`, `FreePlugins.EFModels`, `FreePlugins.DataObjects`

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
