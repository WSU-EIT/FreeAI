# FreeBlazorExtended -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeBlazorExtended` | `Microsoft.NET.Sdk.Razor` | net10.0 | Razor class library; 12 components + 6 services + SignalR hubs |
| `FreeBlazorExtended.Agent` | `Microsoft.NET.Sdk.Worker` | net10.0-windows | Windows Service; heartbeats + remote service/IIS control |
| `FreeBlazorExample` | `Microsoft.NET.Sdk.Web` | net10.0 | ASP.NET Core showcase host |
| `FreeBlazorExample.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net10.0 | WASM client; standard pages + /showcase/* demo pages |
| `FreeBlazorExample.DataAccess` | `Microsoft.NET.Sdk` | net10.0 | Business logic and EF Core |
| `FreeBlazorExample.DataObjects` | `Microsoft.NET.Sdk` | net10.0 | Shared DTOs |
| `FreeBlazorExample.EFModels` | `Microsoft.NET.Sdk` | net10.0 | EF Core DbContext |
| `FreeBlazorExample.Plugins` | `Microsoft.NET.Sdk` | net10.0 | Roslyn dynamic plugin runtime |
| `FreeBlazorExample.ShowcaseTool` | `Microsoft.NET.Sdk` | net10.0 | Console; Playwright screenshots + Magick.NET GIF generation |

## Component consumption pattern

```
Downstream Blazor app
  -> references FreeBlazorExtended (Razor class library)
  -> @using FreeBlazorExtended.Components
  -> <FBEDataTable ...> <FBEModal ...> etc.
  -> SignalR services registered in DI at startup
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*