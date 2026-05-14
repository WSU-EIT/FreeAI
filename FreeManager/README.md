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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
