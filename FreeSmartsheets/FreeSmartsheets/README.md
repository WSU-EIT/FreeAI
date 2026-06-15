# FreeSmartsheets

Smartsheet workspace inventory viewer. A Blazor WebAssembly application that connects to a Smartsheet account via API key and displays workspace contents, sheet listings, and access information for the organization's Smartsheet environment.

Built on the FreeCRM platform, this application provides an authenticated, multi-tenant-capable web interface for reviewing the Smartsheet workspace structure and who has access to what across the organization's Smartsheet environment.

## Project Structure

| Project | Description |
|---------|-------------|
| [`FreeSmartsheets`](FreeSmartsheets/README.md) | Main server application (ASP.NET Core, Blazor WASM host) |
| [`FreeSmartsheets.Client`](FreeSmartsheets.Client/README.md) | Blazor WebAssembly UI components |
| [`FreeSmartsheets.DataAccess`](FreeSmartsheets.DataAccess/README.md) | Business logic and data access layer |
| [`FreeSmartsheets.DataObjects`](FreeSmartsheets.DataObjects/README.md) | Shared DTOs and configuration objects |
| [`FreeSmartsheets.EFModels`](FreeSmartsheets.EFModels/README.md) | Entity Framework Core database models |
| [`FreeSmartsheets.Plugins`](FreeSmartsheets.Plugins/README.md) | Dynamic C# plugin system (Roslyn) |
| [`Docs`](Docs/README.md) | Architecture, style guides, and developer documentation |

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server, PostgreSQL, SQLite, or MySQL
- Smartsheet API key (from your Smartsheet account settings)

### Configuration

1. Set the Smartsheet API key in `FreeSmartsheets/appsettings.json`
2. Configure a database connection string in `FreeSmartsheets/appsettings.json`
3. Run the application:

```bash
cd FreeSmartsheets
dotnet run
```

Navigate to `https://localhost:5001`

## Technology Stack

- **.NET 10** — latest .NET runtime
- **Blazor WebAssembly** — interactive browser-side UI
- **ASP.NET Core** — server host and REST API
- **Entity Framework Core** — database access (SQL Server, PostgreSQL, SQLite, MySQL, InMemory)
- **SignalR** — real-time notifications
- **Roslyn** — server-side and client-side dynamic C# plugin compilation

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the solution folder for FreeSmartsheets: the FreeCRM platform (multi-tenant Blazor app — auth, users, tenants, plugins, real-time updates) plus the *intended* Smartsheet workspace-inventory viewer. You set a Smartsheet API key in `appsettings.json` and point it at your account.

> **Honest status:** the Smartsheet inventory flow is *designed* in [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md) but **not yet implemented** — the data layer has no `GetWorkspaces`/Smartsheet-SDK code yet. See the [top-level README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/README.md) briefing for the full picture.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WASM + ASP.NET Core | Platform UI + host | [FreeSmartsheets/Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Program.cs) |
| EF Core (5 providers) | Data layer | [EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.EFModels/EFModels/EFDataModel.cs) |
| Smartsheet flow *(planned)* | Inventory design, not yet coded | [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md) |

**Why does this exist?**
To give an org a single read-only inventory and access map of its Smartsheet account, built on a platform that already handles SSO and multi-tenancy.

**What does it accomplish that other tools don't?**
- *(Goal)* One cross-account "who-has-access-to-what" view.
- Honest, inspectable status — the scaffold is real; the Smartsheet calls are pending.

**Terminology & "can I see it?"**
- **Workspace / sheet** — Smartsheet's data containers. **API key** — read access token. *See:* [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md).

**The hard part, drawn** — platform now, Smartsheet next:

```
  TODAY:   Browser (Blazor WASM) ─REST─▶ DataController ─▶ IDataAccess ─ EF Core ─▶ 5 DBs
                                              (auth · multi-tenant · plugins · SignalR)
  PLANNED: DataAccess.GetWorkspaces() ─▶ SmartsheetClient.ListWorkspaces() ─▶ inventory UI
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
