# FreeManager.DataObjects

Shared DTO and contract library for the FreeManager platform.

Contains all data transfer objects, endpoint URL constants, configuration helpers, entity wizard models, App Builder project/file models, application template definitions, and global settings. Because this project targets `net10.0` (not Blazor-specific), it can be referenced by both the server-side `DataAccess` and the WASM `FreeManager.Client` without circular dependencies.

## Key Public Classes

| Class | Description |
|-------|-------------|
| `DataObjects` | Root partial class; contains `User`, `Tenant`, `Department`, `Tag`, `FileStorage`, `Setting`, `BooleanResponse`, `AuthenticationProviders`, and other core DTOs |
| `DataObjects.Endpoints` | Nested static class with `const string` API route constants for every endpoint, including `FreeManager.*` App Builder routes |
| `GlobalSettings` | Partial static class for per-app configuration constants |
| `ConfigurationHelper` | Loads `appsettings.json` sections into strongly typed helpers |
| `ApplicationTemplates` | Built-in definitions for `FreeBase`, `FreeTracker`, and `FreeAudit`; converts to `EntityWizardState` for code generation |
| `EntityWizardState` | Root model for the Entity Wizard: project info, entity definitions, relationships, options, generated files |
| `EntityDefinition` | Single entity: name, plural, table, PK type, properties, enums |
| `PropertyDefinition` | One property on an entity: type, required, nullable, max length, enum name, system field flag |

## Feature Files

| File | Contents |
|------|----------|
| `DataObjects.cs` | Core DTOs: User, Tenant, BooleanResponse, ActionResponse, etc. |
| `DataObjects.Departments.cs` | Department / DepartmentGroup DTOs |
| `DataObjects.Tags.cs` | Tag and TagItem DTOs |
| `DataObjects.Services.cs` | Service/plugin request DTOs |
| `FreeManager.App.DataObjects.cs` | App Builder DTOs: FMProject, FMBuild, FMAppFile |
| `FreeManager.App.DataObjects.EntityWizard.cs` | Entity Wizard state models |
| `FreeManager.App.DataObjects.Projects.cs` | Saved project and wizard project DTOs |
| `FreeManager.App.DataObjects.ApplicationTemplates.cs` | FreeBase / FreeTracker / FreeAudit template definitions |
| `FreeManager.App.DataObjects.Gallery.cs` | Template gallery item models |
| `FreeManager.App.DataObjects.Persistence.cs` | Save/load request/response models |
| `FreeManager.App.DataObjects.SmartHelpers.cs` | Smart-suggestion helper models |
| `GlobalSettings.App.cs` | App-specific global settings extensions |
| `Caching.cs` | In-memory cache wrapper (`System.Runtime.Caching`) |

## Project References and NuGet Packages

**References:** `FreeManager.Plugins`

| Package | Version |
|---------|---------|
| `System.Runtime.Caching` | 10.0.1 |

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Library |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A shared "data shapes" library — and crucially, the home of the **code-generation model**. Alongside the usual DTOs, it defines `EntityWizardState`, `EntityDefinition`, and `PropertyDefinition` (the structured description of what to generate) plus the built-in `ApplicationTemplates` (FreeBase / FreeTracker / FreeAudit). Because it targets plain `net10.0`, both the server and the browser client reference it.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Code-generation model | The structured "what to generate" | [FreeManager.App.DataObjects.EntityWizard.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.DataObjects/FreeManager.App.DataObjects.EntityWizard.cs) |
| Core shared DTOs + endpoint constants | The client/server contract | [DataObjects.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeManager/FreeManager.DataObjects/DataObjects.cs) |

**Why does this exist?**
So the wizard UI, the generator, and the persistence layer all speak the *same* model — `EntityWizardState` is defined once and shared, so a change to the model can't silently break one side.

**What does it accomplish that other tools don't?**
- The generator's input (`EntityWizardState`) and the app templates are **shared, typed models** — not loose JSON — so the whole pipeline is compile-checked.

**Terminology & "can I see it?"**
- **EntityWizardState** — the full description of a project: entities, properties, relationships, options.
- **Application template** — a predefined `EntityWizardState` (FreeBase/Tracker/Audit) you can start from.

**The hard part, drawn** — one model drives the whole pipeline:

```
  Browser wizard ─┐                                          ┌─ Generator (EntityTemplates)
                  ├─ EntityWizardState (shared, typed model) ─┤
  Persistence ◀───┘  EntityDefinition · PropertyDefinition    └─▶ emits the .App. source files
                     ApplicationTemplates (FreeBase/Tracker/Audit)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
