# FreeLLM -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeLLM` | `Microsoft.NET.Sdk.Web` | net10.0 | ASP.NET Core host; REST API, SignalR, auth, plugin loader |
| `FreeLLM.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net10.0 | WASM UI; `FreeLLM.App.UI.PromptPackager.razor` workflow, all pages |
| `FreeLLM.DataAccess` | `Microsoft.NET.Sdk` | net10.0 | Business logic; file enumeration, content serving, EF Core |
| `FreeLLM.DataObjects` | `Microsoft.NET.Sdk` | net10.0 | Shared DTOs; `Endpoints.FreeLLM` route constants |
| `FreeLLM.EFModels` | `Microsoft.NET.Sdk` | net10.0 | EF Core DbContext; Users, Tenants, Settings, FileStorage |
| `FreeLLM.Plugins` | `Microsoft.NET.Sdk` | net10.0 | Roslyn dynamic C# plugin runtime |

## Key classes

| Class / Method | Location | Purpose |
|---|---|---|
| `DataController.FreeLLMGetFiles` | Server / FreeLLM.App.API.cs | Enumerates directory, strips hidden/bin/obj |
| `DataController.FreeLLMGetFileContents` | same | Returns full text for selected paths; 5-min cache |
| `DataController.FreeLLMGetFileMetadata` | same | Returns line/char counts; same cache |
| `Helpers.FreeLLMSplitIntoChunks()` | Client / FreeLLM.App.Client.cs | Greedy block-aware chunker |
| `FreeLLMPromptPackagerState.CheckDefaults()` | Client / FreeLLM.App.Client.cs | Pre-selects canonical file set |
| `FreeLLMPromptPackagerState.UpdateSummary()` | Client / FreeLLM.App.Client.cs | Debounced (300ms) full prompt rebuild |

## Data flow

```
Browser selects files
  -> POST /api/Data/GetFileMetadata (counts only, 5-min cache)
  -> POST /api/Data/GetFileContents (full text for selected files)
  -> FreeLLM.App.UI.PromptPackager.razor assembles prompt package
  -> FreeLLMSplitIntoChunks() divides into N balanced chunks
  -> navigator.clipboard.writeText() copies all chunks
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*