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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A workspace for authoring and testing plugins for the WSU-EIT plugin system. `FreePluginsV1/` is the full reference solution — a FreeCRM host, the Roslyn file-plugin runtime, the compiled NuGet plugin SDK, and ~7 example plugins. `BlazorApp1/` is a throwaway app used as a test target.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| The full plugin solution | Host + both plugin systems + examples | [FreePluginsV1/](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/FreePluginsV1) |
| Throwaway test target | A sample app to run plugins against | [BlazorApp1/](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/BlazorApp1) |

**Why does this exist?** To be the reference *and* sandbox for the plugin ecosystem — where you learn, write, and test both styles of plugin.

**What does it beat?** It demonstrates **two plugin authoring models in one place** (see the V1 briefing) rather than just one.

**Terminology:** **Plugin** — drop-in code that extends the app without rebuilding it.

**The hard part, drawn:**
```
  FreePlugins workspace
        ├─ FreePluginsV1/  (host + Roslyn file plugins + compiled NuGet plugins + examples)
        └─ BlazorApp1/     (a target app to test plugins against)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
