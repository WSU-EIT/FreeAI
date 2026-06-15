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

## 🧭 Plain-English Briefing — The Boss Questions

*Every project's README has its own version of this section. This is the **suite-level** one — how the whole thing fits together.*

**How does this work?**
FreeAI is a monorepo of self-contained .NET 10 / Blazor apps that nearly all stand on **one shared foundation: the FreeCRM scaffold** — a multi-tenant Blazor WebAssembly skeleton with authentication, a **Roslyn runtime plugin system**, **Entity Framework Core across 5 databases**, and **SignalR**. Each project specializes that foundation for a purpose, and a few are the *tools that build and extend it*. New apps are created by *forking* the scaffold (clone → strip modules → rename) and adding only their `.App.` customization layer.

**What technology does it use — and where exactly?** *(each links to that project's own boss briefing)*

| Project | What it adds to the scaffold | Marquee tech |
|---|---|---|
| [ChatWithAI](./ChatWithAI) | A token-budgeted Azure OpenAI chat | SharpToken + raw REST |
| [FreeA11yChecker](./FreeA11yChecker) | 4-engine accessibility auditing | Playwright + consensus scoring |
| [FreeBlazorExtended](./FreeBlazorExtended) | ~20 reusable Blazor components + a Windows agent | Razor class library |
| [FreeGLBA](./FreeGLBA) | GLBA access-event tracking + a NuGet client | API-key middleware + `GlbaClient` |
| [FreeLLM](./FreeLLM) | Source-file → token-balanced LLM prompt packager | greedy chunking |
| [FreeManager](./FreeManager) | A code generator (Entity Wizard + CLI) | Roslyn-driven scaffolding |
| [FreePlugins](./FreePlugins) | The plugin SDK (file-based **and** compiled NuGet plugins) | Roslyn + integration bridge |
| [FreeServices](./FreeServices) | Cross-platform service toolkit (Win/Linux/macOS) | one binary, three service managers |
| [FreeServicesHub](./FreeServicesHub) | Hub-and-agent fleet monitor | SignalR heartbeats |
| [FreeSmartsheets](./FreeSmartsheets) | Smartsheet inventory viewer *(scaffold ready; integration pending)* | Smartsheet API *(planned)* |
| [FreeTools](./FreeTools) | CLI suite that analyzes & documents Blazor apps | Aspire pipeline + Playwright |

Shared pillars live in every project's `*.Plugins` (Roslyn), `*.EFModels` (EF Core), and `*.DataObjects` (shared DTOs) folders.

**Why does this exist?**
To consolidate WSU-EIT's AI-adjacent and framework tooling under one roof, all sharing a proven, multi-tenant foundation — so a new product starts at "platform done" instead of "platform from scratch," and so the tooling to build/audit/extend those apps lives alongside them.

**What does it accomplish that other tools don't?**
- **Eleven production apps from one forkable scaffold** — consistent auth, plugins, multi-database support, and real-time updates everywhere, for free.
- **A complete toolchain in the same repo**: a code generator (FreeManager), a plugin SDK (FreePlugins), an analysis/documentation pipeline (FreeTools), a component library (FreeBlazorExtended), and a cross-platform service deployer (FreeServices).
- **Free and open** (MIT), documented, and accessibility-audited end-to-end.

**Terminology & "can I see it?"**
- **Scaffold** — the shared FreeCRM skeleton each app is built on.
- **Fork** — making a renamed, module-trimmed copy of the scaffold (see FreeTools' `ForkCRM`).
- **`.App.` layer** — an app's own code, kept in separately-named files so the framework can be upgraded cleanly.
- **Plugin** — drop-in code (Roslyn-compiled at runtime) that extends an app without a rebuild.
- *See it:* each project's `Docs/showcase/` holds screenshot evidence; each README's briefing has its own diagram.

**The hard part, drawn** — one foundation, eleven specializations:

```
                ┌──────────────── the shared FreeCRM scaffold ─────────────────┐
                │  Blazor WASM · multi-tenant auth · EF Core (5 DBs)           │
                │  Roslyn plugin runtime · SignalR · background service        │
                └───────────────────────────┬──────────────────────────────────┘
      fork (clone→strip→rename) + add an .App. layer for each purpose:
                                            │
  ┌──────────────────────┬──────────────────┼───────────────────┬─────────────────────────┐
  ▼ compliance / audit    ▼ AI / LLM          ▼ build & tooling    ▼ reuse & extend          ▼ services
  FreeA11yChecker         ChatWithAI          FreeManager (codegen) FreeBlazorExtended (lib)  FreeServicesHub (fleet)
  FreeGLBA                FreeLLM             FreeTools (analyze)   FreePlugins (plugin SDK)  FreeServices (x-platform)
  FreeSmartsheets
```

---

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT