# FreePlugins -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreePlugins is a plugin authoring and testing workspace that demonstrates the WSU-EIT Roslyn-based dynamic plugin system. It contains two sub-projects:

- **FreePluginsV1** -- The full solution: a FreeCRM-based web host, Blazor WASM client, data layer, and the complete Roslyn plugin runtime. Includes example plugins and a published NuGet plugin SDK (`FreePlugins.Abstractions`) for external consumers.
- **BlazorApp1** -- A throwaway Blazor Server app used as a scan/test target by analysis tools.

The plugin system allows `.cs` and `.plugin` files to be dropped into a running application at startup, compiled on-the-fly by Roslyn, and executed without restarting or recompiling the host.

## Why it exists

FreeCRM applications need a safe way to extend behavior without modifying core source files. FreePlugins codifies the plugin contract (`IPlugin`) and runtime (`FreePlugins.Plugins`) and provides example implementations so developers know exactly how to write a plugin.

## Who it is for

- WSU-EIT developers writing plugins for any FreeCRM-based application
- External teams who want to consume the `FreePlugins.Abstractions` NuGet package
- Engineers who want to see how Roslyn runtime compilation works in practice

## Quick start

```bash
cd FreePlugins/FreePluginsV1/FreePlugins
dotnet run
```

Navigate to `http://localhost:5104`.

## Related projects

- [ChatWithAI](../ChatWithAI/README.md) -- uses the same plugin pattern
- [FreeManager](../FreeManager/README.md) -- can generate plugin scaffolding
- [FreeA11yChecker](../FreeA11yChecker/README.md) -- uses Roslyn plugins for custom auth and background processes

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*