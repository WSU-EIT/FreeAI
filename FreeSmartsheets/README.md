# FreeSmartsheets

A Blazor WebAssembly application for viewing a Smartsheet workspace inventory — showing what workspaces, sheets, and reports exist in an organization's Smartsheet account and who has access to what. Configure the Smartsheet API key in `appsettings.json` to connect to your organization's account.

## Project Structure

| Project | Description |
|---------|-------------|
| [`FreeSmartsheets`](FreeSmartsheets/FreeSmartsheets/README.md) | Main server application (ASP.NET Core, Blazor Server/WASM host) |
| [`FreeSmartsheets.Client`](FreeSmartsheets/FreeSmartsheets.Client/README.md) | Blazor WebAssembly UI components |
| [`FreeSmartsheets.DataAccess`](FreeSmartsheets/FreeSmartsheets.DataAccess/README.md) | Business logic and data access layer |
| [`FreeSmartsheets.DataObjects`](FreeSmartsheets/FreeSmartsheets.DataObjects/README.md) | DTOs and configuration objects |
| [`FreeSmartsheets.EFModels`](FreeSmartsheets/FreeSmartsheets.EFModels/README.md) | Entity Framework Core database models |
| [`FreeSmartsheets.Plugins`](FreeSmartsheets/FreeSmartsheets.Plugins/README.md) | Dynamic C# plugin system |

## Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server, PostgreSQL, SQLite, or MySQL
- Smartsheet API key

### Configuration

Set your Smartsheet API key in `FreeSmartsheets/appsettings.json` and configure a database connection string, then run:

```bash
cd FreeSmartsheets/FreeSmartsheets
dotnet run
```

Navigate to `https://localhost:5001`

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeSmartsheets is built on the FreeCRM platform — a multi-tenant Blazor web app with login, user/tenant admin, runtime plugins, and real-time updates. Its *intended* job is to connect to a Smartsheet account (API key in `appsettings.json`) and show an inventory of the workspaces, sheets, and reports in that account, plus who can access what.

> **Honest status (as of this commit):** the platform is fully in place, and the Smartsheet data flow is *designed and documented* in [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md) (`GetWorkspaces() → SmartsheetClient.WorkspaceResources.ListWorkspaces()`) — but those Smartsheet API calls are **not yet implemented in the code**. There is no `GetWorkspaces` method or Smartsheet SDK usage in the data layer yet. So today this is the scaffold + design; building the inventory viewer is the next step.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WASM + ASP.NET Core host | The platform UI and web server | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Program.cs) |
| EF Core (5 providers) | Data layer | [EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.EFModels/EFModels/EFDataModel.cs) |
| Roslyn plugins | Extend at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.Plugins/Plugins.cs) |
| SignalR (`/freesmartsheetsHub`) | Real-time updates | [Program.cs#L238](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets/Program.cs#L238) |
| Smartsheet integration *(planned)* | The inventory data flow — **design only, not yet coded** | [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md) |

**Why does this exist?**
Organizations on Smartsheet lose track of what workspaces and sheets exist and who has access. The goal is a single read-only **inventory + access map** across the whole account — governance and audit value Smartsheet's own UI doesn't surface in one place. Building it on FreeCRM means it inherits enterprise SSO and multi-tenancy for free.

**What does it accomplish that other tools don't?**
- *(Goal)* A cross-account "who can see what" map across all workspaces in one view.
- Built on a reusable, SSO-capable multi-tenant platform rather than a one-off script.
- *(Today)* Transparent status: the platform is done; the Smartsheet API calls are the remaining work — no overstating what's shipped.

**Terminology & "can I see it?"**
- **Workspace / sheet / report** — Smartsheet's containers for data.
- **API key** — the token that authorizes read access to a Smartsheet account.
- **Scaffold** — the shared FreeCRM platform this is built on.
- *See it:* the intended data flow is written out in [Docs/003_architecture.md](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/Docs/003_architecture.md).

**The hard part, drawn** — design vs. what's live today:

```
  [ DESIGN — documented in Docs/003_architecture.md, NOT yet implemented in code ]
     Browser ─▶ GET /api/Data/GetWorkspaces
                     ▼
            DataAccess.GetWorkspaces()
                     ▼  (Smartsheet C# SDK, using the configured API key)
            SmartsheetClient.WorkspaceResources.ListWorkspaces()
                     ▼
            workspaces · sheets · reports · who-has-access ──▶ inventory UI

  [ TODAY — the FreeCRM platform underneath is fully in place ]
     Browser (Blazor WASM) ─REST─▶ DataController ─▶ IDataAccess ─ EF Core ─▶ 5 DB engines
            └ auth (OIDC/OAuth) · multi-tenant · Roslyn plugins · SignalR (freesmartsheetsHub)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
