# FreeLLM -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeLLM` | `Microsoft.NET.Sdk.Web` | net10.0 | ASP.NET Core host; REST API, SignalR, auth, plugin loader |
| `FreeLLM.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net10.0 | WASM UI; `Index.App.razor` workflow, all pages |
| `FreeLLM.DataAccess` | `Microsoft.NET.Sdk` | net10.0 | Business logic; file enumeration, content serving, EF Core |
| `FreeLLM.DataObjects` | `Microsoft.NET.Sdk` | net10.0 | Shared DTOs; `EndpointFileGpt` route constants |
| `FreeLLM.EFModels` | `Microsoft.NET.Sdk` | net10.0 | EF Core DbContext; Users, Tenants, Settings, FileStorage |
| `FreeLLM.Plugins` | `Microsoft.NET.Sdk` | net10.0 | Roslyn dynamic C# plugin runtime |

## Key classes

| Class / Method | Location | Purpose |
|---|---|---|
| `DataController.GetFiles` | Server / DataController.App.FreeLLM.cs | Enumerates directory, strips hidden/bin/obj |
| `DataController.GetFileContents` | same | Returns full text for selected paths; 5-min cache |
| `DataController.GetFileMetadata` | same | Returns line/char counts; same cache |
| `Index.App.razor / SplitSummaryIntoChunks()` | Client | Greedy block-aware chunker |
| `Index.App.razor / CheckDefaults()` | Client | Pre-selects canonical file set |
| `Index.App.razor / UpdateSummary()` | Client | Debounced (300ms) full prompt rebuild |

## Data flow

```
Browser selects files
  -> POST /api/Data/GetFileMetadata (counts only, 5-min cache)
  -> POST /api/Data/GetFileContents (full text for selected files)
  -> Index.App.razor assembles prompt package
  -> SplitSummaryIntoChunks() divides into N balanced chunks
  -> navigator.clipboard.writeText() copies all chunks
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*