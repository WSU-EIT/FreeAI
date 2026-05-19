# FreeAI

Monorepo consolidating WSU-EIT's AI-adjacent and framework tooling projects under one roof. Each subfolder is a self-contained .NET 10 / Blazor WebAssembly solution built on the FreeCRM scaffold with its own database, auth, plugin system, and documentation set.

## Project areas

| Folder | What it is | Pages crawled |
|--------|-----------|--------------|
| [ChatWithAI](./ChatWithAI) | FreeCRM scaffold + Azure OpenAI token-budgeted chat console | 38 |
| [FreeA11yChecker](./FreeA11yChecker) | WCAG 2.1/2.2 AA accessibility auditing platform — 4-engine scanner, Blazor UI, CLI crawl tool | 75 |
| [FreeBlazorExtended](./FreeBlazorExtended) | Razor class library of reusable Blazor components + Windows agent for remote service/IIS control | 110 |
| [FreeGLBA](./FreeGLBA) | GLBA compliance data-access tracking system + NuGet client library for external integration | 50 |
| [FreeLLM](./FreeLLM) | Source-file curator that assembles token-balanced LLM prompt packages from a local directory | 40 |
| [FreeManager](./FreeManager) | Code-generation platform: Entity Wizard, App Builder, and CLI scaffolder for FreeCRM projects | 54 |
| [FreePlugins](./FreePlugins) | Plugin authoring workspace demonstrating the WSU-EIT Roslyn runtime-compilation plugin system | 33 |
| [FreeServicesHub](./FreeServicesHub) | Hub-and-agent fleet monitor: SignalR hub receives live CPU/RAM/disk heartbeats from Windows agents | 3 |
| [FreeSmartsheets](./FreeSmartsheets) | Smartsheet API inventory viewer — workspaces, sheets, reports, and sharing across an org account | 1 |

## Current state

All 9 project areas are on **.NET 10**, running with an **InMemory database** by default (no setup required), and have been fully documented and accessibility-audited:

- **Documentation** — each area has `Docs/000_overview.md` through `004_showcase.md` written from the actual source code
- **Showcase** — `Docs/showcase/runs/latest/` contains a full multi-page crawl with per-page screenshots: login flow, page load, 5 a11y overlays, 7 CVD simulations, screen reader view, reduced motion, forced colors
- **Total screenshots across all 9 apps: ~7,240** across ~424 pages

## Quick start — any project

```powershell
cd <ProjectFolder>\<ServerProject>
dotnet run
```

Each server project has an `appsettings.Development.json` that sets `DatabaseType: InMemory` so no database configuration is needed.

## Re-run accessibility showcase

```powershell
cd FreeAI
.\run-a11y-showcase.ps1                              # all 9 apps
.\run-a11y-showcase.ps1 -Targets ChatWithAI,FreeLLM  # specific apps
.\run-a11y-showcase.ps1 -SkipBuild                   # skip scanner rebuild
```

Results land in each project's `Docs/showcase/runs/latest/`. Git history tracks every run as a diff.

## Tech stack

| Technology | Usage |
|---|---|
| .NET 10 / ASP.NET Core | Server host for all 9 applications |
| Blazor WebAssembly | Client UI across all projects |
| Entity Framework Core | Data access (SQL Server, PostgreSQL, MySQL, SQLite, InMemory) |
| SignalR | Real-time updates in every application |
| Roslyn | Dynamic C# plugin compilation at runtime |
| Playwright | Headless browser automation for accessibility scanning |
| PowerShell | Showcase orchestration (`run-a11y-showcase.ps1`) |

## Documentation standard

Every project area follows the five-document standard defined in [DOCS_STANDARD.md](./DOCS_STANDARD.md):

| File | Purpose |
|------|---------|
| `000_overview.md` | What it is, why it exists, quick start |
| `001_features.md` | Implemented feature inventory |
| `002_roadmap.md` | Planned future work |
| `003_architecture.md` | Project structure, data flow, key classes |
| `004_showcase.md` | Screenshot index, how to re-run the capture |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT