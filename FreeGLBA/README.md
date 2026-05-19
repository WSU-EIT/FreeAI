# FreeGLBA

GLBA (Gramm-Leach-Bliley Act) compliance data-access tracking system built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). Tracks who accessed protected financial data — when, by whom, and for what purpose — and provides audit-ready reports and a real-time access dashboard. Ships a NuGet client library (`FreeGLBA.NugetClient`) so any other application can emit GLBA access events with a single method call.

## What it does

- **Access event logging** — record who accessed which financial record, when, and why (single or bulk events)
- **Real-time dashboard** — live access statistics and pattern monitoring via SignalR
- **Compliance reports** — exportable audit reports for GLBA review
- **REST API** — integrate any system via HTTP
- **NuGet client** — `dotnet add package FreeGLBA.Client` gives any .NET app a typed `GlbaClient`

## Projects

| Project | Description |
|---------|-------------|
| [`FreeGLBA`](FreeGLBA/README.md) | ASP.NET Core host; REST API, auth, SignalR, background service |
| [`FreeGLBA.Client`](FreeGLBA.Client/README.md) | Blazor WASM client; dashboard, log viewer, settings |
| [`FreeGLBA.DataAccess`](FreeGLBA.DataAccess/README.md) | Business logic; EF Core, access-event repositories, auth helpers |
| [`FreeGLBA.DataObjects`](FreeGLBA.DataObjects/README.md) | Shared DTOs; `GlbaEventRequest`, endpoint constants |
| [`FreeGLBA.EFModels`](FreeGLBA.EFModels/README.md) | EF Core DbContext; access-event log and core framework tables |
| [`FreeGLBA.NugetClient`](FreeGLBA.NugetClient/README.md) | NuGet package; `GlbaClient` for external system integration |
| [`FreeGLBA.NugetClientPublisher`](FreeGLBA.NugetClientPublisher/README.md) | Tool for publishing the NuGet package |
| [`FreeGLBA.Plugins`](FreeGLBA.Plugins/README.md) | Roslyn dynamic C# plugin runtime |
| [`FreeGLBA.TestClient`](FreeGLBA.TestClient/README.md) | Integration test client (project reference) |
| [`FreeGLBA.TestClientWithNugetPackage`](FreeGLBA.TestClientWithNugetPackage/README.md) | Integration test client (NuGet package) |

## Quick start

```bash
cd FreeGLBA/FreeGLBA
dotnet run
```

Boots with `DatabaseType=InMemory` — no database setup required. Navigate to `http://localhost:5001`.

### Client library

```bash
dotnet add package FreeGLBA.Client
```

```csharp
var client = new GlbaClient("https://your-server.com", "your-api-key");
await client.LogAccessAsync(new GlbaEventRequest {
    UserId    = "jsmith",
    SubjectId = "S12345678",
    AccessType = "View",
    Purpose   = "Enrollment verification"
});
```

## Current state

- Boots and serves all pages in InMemory mode
- Full multi-page crawl captured: **903 screenshots across 50 pages** in `Docs/showcase/runs/latest/`
- Login with `admin`/`admin` requires seeded data — public pages are accessible without authentication
- Documentation complete: see [`Docs/`](Docs/)

## Build details

| Property | Value |
|---|---|
| Target framework | net10.0 |
| Database backends | SQL Server, PostgreSQL, SQLite, InMemory |
| Auth providers | Cookie, OpenID Connect, Microsoft, Google, Facebook, Apple |
| Real-time | SignalR (local or Azure SignalR Service) |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT