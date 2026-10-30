# FreeManager -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | Role |
|---------|------|
| `FreeManager` | ASP.NET Core host; controllers, SignalR, OAuth, plugin loader |
| `FreeManager.Client` | Blazor WASM; App Builder, Entity Wizard, template gallery |
| `FreeManager.DataAccess` | Business logic; EF Core, code-generation engine, auth helpers |
| `FreeManager.DataObjects` | Shared DTOs; entity wizard models, project/file models |
| `FreeManager.EFModels` | EF Core DbContext; saved projects, core framework tables |
| `FreeManager.Plugins` | Roslyn dynamic plugin runtime |
| `FreeManager.Cli` | `FreeManager.exe` console; template rendering, file generation |

## App Builder data flow

```
Browser (Entity Wizard)
  -> POST /api/Data/SaveProject     (persist entity model to DB)
  -> POST /api/Data/GenerateCode    (render templates to C# / Razor text)
  -> GET  /api/Data/PreviewCode     (syntax-highlighted display)
  -> GET  /api/Data/ExportProject   (zip download)
```

## CLI data flow

```
FreeManager.exe new Tasks
  -> template engine renders partial-class stubs
  -> writes files to --output directory
  -> prints file manifest to stdout
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*