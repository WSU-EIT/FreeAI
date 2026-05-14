# FreeSmartsheets -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | Role |
|---------|------|
| `FreeSmartsheets` (server) | ASP.NET Core host; Smartsheet API proxy, REST controllers, auth |
| `FreeSmartsheets.Client` | Blazor WASM UI; workspace tree, sheet/report listings |
| `FreeSmartsheets.DataAccess` | Business logic; Smartsheet SDK calls, EF Core, auth helpers |
| `FreeSmartsheets.DataObjects` | Shared DTOs; workspace/sheet/report view models |
| `FreeSmartsheets.EFModels` | EF Core DbContext; core FreeCRM tables |
| `FreeSmartsheets.Plugins` | Roslyn dynamic plugin runtime |

## Data flow

```
Browser (WASM)
  -> GET /api/Data/GetWorkspaces
       -> DataAccess.GetWorkspaces()
            -> SmartsheetClient.WorkspaceResources.ListWorkspaces()
            -> returns workspace list with sheets + reports
  -> render inventory tree in Blazor UI
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*