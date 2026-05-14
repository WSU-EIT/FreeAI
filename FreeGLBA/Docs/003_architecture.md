# FreeGLBA -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | Role |
|---------|------|
| `FreeGLBA` | ASP.NET Core host; REST API, auth, SignalR, background service |
| `FreeGLBA.Client` | Blazor WASM UI; dashboards, log viewer, settings |
| `FreeGLBA.DataAccess` | Business logic; EF Core repositories, auth helpers |
| `FreeGLBA.DataObjects` | Shared DTOs, endpoint constants, `GlbaEventRequest` model |
| `FreeGLBA.EFModels` | EF Core DbContext; access-event and core framework tables |
| `FreeGLBA.NugetClient` | NuGet package; `GlbaClient` for external system integration |
| `FreeGLBA.NugetClientPublisher` | Tool for publishing the NuGet package |
| `FreeGLBA.Plugins` | Roslyn dynamic plugin runtime |
| `FreeGLBA.TestClient` | Integration test client (project reference) |
| `FreeGLBA.TestClientWithNugetPackage` | Integration test client (NuGet) |

## Data flow

```
External System  --NuGet client-->  REST API (FreeGLBA)
                                      |-> DataAccess
                                      |     |-> EF Core (access-event log)
                                      |-> SignalR hub (real-time dashboard push)
Browser (WASM)   --HTTP/SignalR-->  FreeGLBA
                                      |-> Audit reports, dashboards
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*