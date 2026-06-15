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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A **placeholder** compiled-plugin project: it reserves the project + NuGet package shape for a UI-oriented plugin (`ICompiledGeneralPlugin`), referencing the Abstractions SDK so it's discoverable by `AddPluginsFromAssembly` — but `UIExamplePlugin.cs` is currently an empty stub.

> **Honest status:** this is intentionally a *skeleton* — the implementation file is a placeholder, not a working UI plugin yet.

**What tech & where?** [the UIExamplePlugin project](https://github.com/WSU-EIT/FreeAI/tree/main/FreePlugins/FreePluginsV1/FreePlugins.UIExamplePlugin) (references `FreePlugins.Abstractions`).

**Why does this exist?** To pre-stage the structure for a future UI-rendering plugin, so the package/registration plumbing is already in place.

**What does it beat?** Nothing yet — it's a starting skeleton; the value is the ready-made project shape.

**Terminology:** **Skeleton/placeholder** — a stubbed project reserving structure for later work.

**The hard part, drawn:**
```
  UIExamplePlugin (skeleton) ─references─▶ FreePlugins.Abstractions
        UIExamplePlugin.cs = empty stub ─▶ fill in ICompiledGeneralPlugin to make a real UI plugin
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
