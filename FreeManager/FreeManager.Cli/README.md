# FreeManager.Cli

`FreeManager.exe` — command-line project generator for FreeCRM / FreeManager applications.

Produces scaffolded `.App.` partial-class files that drop directly into the layered FreeCRM project structure. Supports both a rich interactive console menu (powered by Spectre.Console) and a fully non-interactive command-line mode.

## Commands

```
FreeManager.exe                                   Interactive menu
FreeManager.exe new <name> [options]              Create a simple project
FreeManager.exe app <name> [options]              Create a full application
FreeManager.exe list                              List all templates
FreeManager.exe help                              Show usage
```

### `new` — Simple project templates

Creates partial `.App.` files for one named project module.

| Option | Alias | Description |
|--------|-------|-------------|
| `--template` | `-t` | `Empty`, `Skeleton`, `Starter` *(default)*, `FullCrud` |
| `--output` | `-o` | Output directory (default: `./<name>`) |

| Template | Files | Description |
|----------|-------|-------------|
| `Empty` | 0 | No starter files |
| `Skeleton` | 4 | DataObjects, DataAccess, Controller, GlobalSettings stubs |
| `Starter` | 6 | Working items list (JSON in Settings table, no migration needed) |
| `FullCrud` | 8 | Full EF Core CRUD — run `dotnet ef migrations add` after export |

```
FreeManager.exe new Tasks
FreeManager.exe new Inventory -t FullCrud -o C:\Projects\MyApp
```

### `app` — Application templates

Generates a complete application with multiple entities across all project layers.

| Option | Alias | Description |
|--------|-------|-------------|
| `--template` | `-t` | `FreeBase`, `FreeTracker`, `FreeAudit` *(default)* |
| `--output` | `-o` | Output directory (default: `./<name>`) |

| Template | Difficulty | Entities | Description |
|----------|-----------|----------|-------------|
| `FreeBase` | Beginner | 2 | Items + Categories — simple collection foundation |
| `FreeTracker` | Intermediate | 5 | + Assignment, Checkout, Status tracking |
| `FreeAudit` | Advanced | 4 | + External API endpoint, API-key auth, access-event logging, compliance reports |

```
FreeManager.exe app FreeGLBA --template FreeAudit
FreeManager.exe app Assets -t FreeTracker -o C:\Projects\GLBA
```

Output folder structure:

```
<name>/                    Controllers (.App.DataController.cs)
<name>.Client/             Pages/ and Shared/AppComponents/ (.razor)
<name>.DataAccess/         (.App.DataAccess.cs)
<name>.DataObjects/        (.App.DataObjects.cs, GlobalSettings.App.cs)
<name>.EFModels/EFModels/  EF entity files
README.txt
```

## Key Public Classes

| Class | Description |
|-------|-------------|
| `Program` | Entry point; wires `System.CommandLine` verbs and dispatches to `MenuService` |
| `MenuService` | Interactive Spectre.Console menu; wizards for project and application creation |
| `CliProjectTemplates` | Template definitions and code-string generators for all four project templates |
| `CliTemplateInfo` | Metadata for a simple project template |
| `CliAppTemplateInfo` | Metadata for an application template |
| `GeneratedFile` | File name, type, and content for one generated output file |
| `CliProjectTemplate` | Enum: `Empty`, `Skeleton`, `Starter`, `FullCrud` |
| `CliApplicationTemplate` | Enum: `FreeBase`, `FreeTracker`, `FreeAudit` |

## Project References and NuGet Packages

**References:** `FreeManager.DataObjects`, `FreeManager.Client`

| Package | Version |
|---------|---------|
| `System.CommandLine` | 2.0.0-beta4.22272.1 |
| `Spectre.Console` | 0.50.0 |

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Assembly name | `FreeManager` (produces `FreeManager.exe`) |
| Target framework | `net10.0` |
| Output type | `Exe` |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
`FreeManager.exe` is the command-line side of the code generator. `new <name>` produces a single module from a template (`Empty` / `Skeleton` / `Starter` / `FullCrud`); `app <name>` generates a whole multi-entity application (`FreeBase` / `FreeTracker` / `FreeAudit`). Run it with no arguments for a rich interactive menu (Spectre.Console); pass verbs for fully scripted, non-interactive generation. The output mirrors the FreeCRM folder layout exactly.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| `System.CommandLine` verbs | `new` / `app` / `list` / `help` parsing | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Cli/Program.cs) |
| Spectre.Console menu | Interactive wizards when run with no args | [MenuService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Cli/MenuService.cs) |
| Template generators | The actual file-producing templates | [CliProjectTemplates.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.Cli/CliProjectTemplates.cs) |

**Why does this exist?**
So project/app generation can be **scripted** — in CI, in a setup script, or at a developer's terminal — without opening the web App Builder.

**What does it accomplish that other tools don't?**
- The same generation idea as the web App Builder, but **automatable**: one command scaffolds an entire layered app.
- **Progressive templates** from a 2-entity starter up to a 4-entity audit app with API-key auth and compliance reports.

**Terminology & "can I see it?"**
- **Verb** — a CLI sub-command (`new`, `app`, `list`).
- **Template** — a named recipe deciding how many files/entities to emit.

**The hard part, drawn** — a command becomes a layered project:

```
  FreeManager.exe new Tasks -t FullCrud        FreeManager.exe app Assets -t FreeTracker
          │ System.CommandLine verbs  (or the Spectre.Console interactive menu)
          ▼ CliProjectTemplates / application templates
   generated .App. files ─▶ <name>/  <name>.Client/  <name>.DataAccess/  <name>.DataObjects/  <name>.EFModels/
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
