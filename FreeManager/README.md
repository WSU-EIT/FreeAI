# FreeManager

FreeManager is a multi-tenant application management and code-generation platform built on the FreeCRM framework with ASP.NET Core and Blazor WebAssembly (.NET 10). It provides two major capabilities:

1. **A running multi-tenant web application** — users, departments, tenants, tags, file storage, settings, UDFs, and a plugin system — that serves as the host for generated app modules.
2. **An in-browser and CLI code generator** — an Entity Wizard and App Builder that let developers define data models with relationships, preview the generated C# / Razor code, and export it into the layered FreeCRM project structure.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `FreeManager` | ASP.NET Core Web (host) | Server-side host: MVC controllers, SignalR hub, OAuth/OIDC authentication, background service, plugin loader |
| `FreeManager.Client` | Blazor WebAssembly | In-browser UI: App Builder, Entity Wizard, template gallery, project editor, settings pages |
| `FreeManager.DataAccess` | Class library | Business logic, EF Core repositories, JWT/AD/Graph auth helpers, migrations for SQL Server, MySQL, PostgreSQL, SQLite |
| `FreeManager.DataObjects` | Class library | Shared DTOs, endpoint constants, entity wizard models, project/file models — used by both server and WASM client |
| `FreeManager.EFModels` | Class library | EF Core `DbContext` and entity models; owns database schema and migrations |
| `FreeManager.Plugins` | Class library | Roslyn-based dynamic plugin runtime; defines `IPlugin` contracts and compiles/executes `.cs` / `.plugin` files at startup |
| `FreeManager.Cli` | Console executable | `FreeManager.exe` — CLI project generator for FreeCRM applications (see below) |

## FreeManager.Cli — Usage

Run without arguments for an interactive menu:

```
FreeManager.exe
```

### Commands

```
FreeManager.exe new <name> [--template|-t <type>] [--output|-o <dir>]
FreeManager.exe app <name> [--template|-t <type>] [--output|-o <dir>]
FreeManager.exe list
FreeManager.exe help
```

### `new` — Simple project templates

Creates a set of partial-class `.App.` files that drop into the FreeCRM layer structure.

| Template | Files | Description |
|----------|-------|-------------|
| `Empty` | 0 | No starter files; build from scratch |
| `Skeleton` | 4 | Placeholder stubs for DataObjects, DataAccess, Controller, GlobalSettings |
| `Starter` *(default)* | 6 | Working items list stored in the Settings table — no migration needed |
| `FullCrud` | 8 | Full CRUD backed by an EF Core entity; requires `dotnet ef migrations add` after export |

```
FreeManager.exe new Tasks
FreeManager.exe new Inventory -t FullCrud -o C:\Projects\MyApp
```

### `app` — Application templates

Generates a complete multi-file application from one of three progressive templates.

| Template | Difficulty | Entities | Description |
|----------|-----------|----------|-------------|
| `FreeBase` | Beginner | 2 | Items + Categories; foundation for any collection app |
| `FreeTracker` | Intermediate | 5 | + Assignment, Checkout, Status tracking |
| `FreeAudit` *(default)* | Advanced | 4 | + External API, API-key auth, access-event logging, compliance reports |

```
FreeManager.exe app FreeGLBA --template FreeAudit
FreeManager.exe app Assets -t FreeTracker -o C:\Projects\GLBA
```

Generated output mirrors the FreeCRM folder structure:

```
<name>/                    Controllers
<name>.Client/             Pages, Shared/AppComponents
<name>.DataAccess/
<name>.DataObjects/
<name>.EFModels/EFModels/
README.txt
```

## App Builder (Web UI)

The Blazor client includes an in-browser code-generation suite:

| Route | Description |
|-------|-------------|
| `/AppBuilder` | Project dashboard — list, create, open saved projects |
| `/AppBuilder/New` | New project wizard entry point |
| `/AppBuilder/EntityWizard` | 7-step entity wizard: Project → Entities → Edit Entity → Relationships → Options → Preview → Complete |
| `/AppBuilder/Edit/{ProjectId}` | Re-open and edit a previously saved project |
| `/AppBuilder/SetupWizardBuilder` | Quick-start wizard builder |
| `/AppBuilder/Dashboard` | Legacy redirect to `/AppBuilder` |
| `/Templates` | Template selection (FreeBase / FreeTracker / FreeAudit) |
| `/Templates/Setup` | Configure a selected template before scaffolding |
| `/fm/templates` | Template gallery browser |

## Build

| Property | Value |
|----------|-------|
| Target framework | `net10.0` |
| Database backends | SQL Server, MySQL, PostgreSQL, SQLite, InMemory |
| Auth providers | Cookie, OpenID Connect, Microsoft, Google, Facebook, Apple |
| Real-time | SignalR (local) or Azure SignalR |

```bash
dotnet build FreeManager.slnx
dotnet run --project FreeManager
```

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeManager is two things in one: a running multi-tenant web app (the usual FreeCRM platform), **and a code generator for building more FreeCRM apps.** You describe your data model — entities, properties, relationships — either in a 7-step in-browser **Entity Wizard** or via the **`FreeManager.exe` CLI**, and it emits ready-to-drop C#/Razor source files (`.App.` partial classes) laid out in the exact FreeCRM project structure (Controllers, DataAccess, DataObjects, EFModels, Client pages). You can preview the generated code, save the project, and export it.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| The code generator | Emits the C#/Razor for one entity across all layers | [FreeManager.App.EntityTemplates.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Client/FreeManager.App.EntityTemplates.cs) |
| 7-step Entity Wizard (UI) | Define the model in the browser | [Pages/FreeManager.App.EntityWizard.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Client/Pages/FreeManager.App.EntityWizard.razor) |
| Wizard data model | Entities, properties, relationships | [FreeManager.App.DataObjects.EntityWizard.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.DataObjects/FreeManager.App.DataObjects.EntityWizard.cs) |
| CLI generator (`new` / `app`) | Same generation from the command line | [CliProjectTemplates.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Cli/CliProjectTemplates.cs) |
| Saved-project storage | Persist/reopen App Builder projects | [EFDataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.EFModels/EFModels/EFDataModel.cs) |

**Why does this exist?**
Every FreeCRM app repeats the same layered boilerplate (DTO + EF entity + data access + controller + Razor page, per feature). FreeManager automates that from a model definition, so a new app or module starts as working, consistent, layered code instead of hand-copied scaffolding.

**What does it accomplish that other tools don't?**
- **Generates the whole layered stack at once** — DTO, EF model, data access, controller, and UI page — matching FreeCRM conventions exactly, so the output drops in without rework.
- **Two front-ends, one generator**: an in-browser wizard (with preview, undo/redo, CSV/JSON import) *and* a scriptable CLI for automation/CI.
- **Progressive app templates** (FreeBase → FreeTracker → FreeAudit) scaffold whole multi-entity apps, not just single files.
- It's self-hosting: FreeManager can scaffold apps like FreeGLBA (the example in its own docs).

**Terminology & "can I see it?"**
- **Entity** — one data type/table (e.g. "Asset") with properties.
- **`.App.` partial class** — a generated file that extends a FreeCRM base class without editing the base.
- **Scaffold** — generate the starter code for something.
- **Entity Wizard** — the 7 steps: Project → Entities → Edit → Relationships → Options → Preview → Complete.
- *See it:* open `/AppBuilder/EntityWizard` in the running app.

**The hard part, drawn** — one model definition fans out into a full layered app:

```
  you ─▶ define entities / properties / relationships
          │   (7-step Entity Wizard in the browser  OR  FreeManager.exe new/app on the CLI)
          ▼
   EntityWizardState   (the model)
          │  EntityTemplates.GenerateAllFiles(state)
          ▼
  ┌──────────── ONE entity fans out into the full FreeCRM stack ────────────┐
  │  .App.DataObjects.cs   →  the DTO                                         │
  │  .App.EFModel.cs       →  the EF Core entity + DbSet                      │
  │  .App.DataAccess.cs    →  CRUD repository methods                         │
  │  .App.Controller.cs    →  REST endpoints                                  │
  │  .App.*.razor          →  the Blazor page (list / edit / dashboard / …)   │
  └───────────────────────────────┬──────────────────────────────────────────┘
          │ preview in-browser (Monaco) · save project (FMProject) · export to disk
          ▼
   a working, layered FreeCRM module — drops in, ready to build
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
