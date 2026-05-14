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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
