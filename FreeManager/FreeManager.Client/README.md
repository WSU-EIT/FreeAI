# FreeManager.Client

Blazor WebAssembly client for the FreeManager application platform.

This project is the in-browser UI. It contains all pages, components, the App Builder / Entity Wizard code-generation suite, shared UI components (Monaco editor, file upload, tag selector, dialogs), and the client-side data model (`BlazorDataModel`).

## Blazor Pages and Routes

| Route | Page file | Description |
|-------|-----------|-------------|
| `/` and `/{TenantCode}` | `Index.razor` | Home / dashboard |
| `/Login` | `Authorization/Login.razor` | Login page |
| `/Logout` | `Authorization/Logout.razor` | Logout |
| `/AppBuilder` | `FreeManager.App.Builder.razor` | App Builder project dashboard |
| `/AppBuilder/New` | `FreeManager.App.BuilderNew.razor` | New project entry |
| `/AppBuilder/Edit/{ProjectId}` | `FreeManager.App.BuilderEdit.razor` | Edit saved project |
| `/AppBuilder/EntityWizard` | `FreeManager.App.EntityWizard.razor` | 7-step entity & code-gen wizard |
| `/AppBuilder/Dashboard` | `FreeManager.App.EntityDashboard.razor` | Legacy redirect to `/AppBuilder` |
| `/AppBuilder/SetupWizardBuilder` | `FreeManager.App.SetupWizardBuilder.razor` | Setup wizard builder |
| `/Templates` | `FreeManager.App.Templates.razor` | Template selection (FreeBase / FreeTracker / FreeAudit) |
| `/Templates/Setup` | `FreeManager.App.TemplateSetup.razor` | Configure template before scaffolding |
| `/fm/templates` | `FreeManager.App.TemplateGallery.razor` | Full template gallery browser |
| `/Settings` | `Settings/Misc/Settings.razor` | Application settings |
| `/Users` | `Settings/Users/Users.razor` | User management |
| `/Departments` | `Settings/Departments/Departments.razor` | Department management |
| `/Tenants` | `Settings/Tenants/Tenants.razor` | Tenant management |
| `/Tags` | `Settings/Tags/Tags.razor` | Tag management |
| `/Files` | `Settings/Files/Files.razor` | File storage |
| `/ChangePassword` | `ChangePassword.razor` | Password change |
| `/Profile` | `Profile.razor` | User profile |
| `/About` | `About.razor` | About page |

## Key Public Classes

| Class | Description |
|-------|-------------|
| `BlazorDataModel` | Singleton client state model — authentication, tenant, navigation, messages |
| `Helpers` | Static HTTP helper methods (`GetOrPost`, `NavigateToLogin`, `BuildUrl`, `ConsoleLog`) |
| `EntityTemplates` | Code generator — `GenerateAllFiles(EntityWizardState)` produces all `.App.` partial-class files |
| `ProjectTemplates` | Project-level template generators used by the App Builder |
| `PagePatterns` | Reusable Blazor page pattern generators (Kanban, Timeline, Dashboard, SplitPanel, etc.) |
| `WizardTemplates` | Component and service code templates for the wizard output |
| `EntityWizardUndoRedo` | Undo/redo state stack for the Entity Wizard |
| `EntityWizardImportParser` | Parses imported entity definitions (CSV / JSON) into wizard state |

## Project References and NuGet Packages

**References:** `FreeManager.DataObjects`

| Package | Version |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.1 |
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.1 |
| `MudBlazor` | 8.15.0 |
| `Radzen.Blazor` | 8.4.0 |
| `Blazor.Bootstrap` | 3.5.0 |
| `BlazorMonaco` | 3.4.0 (Monaco code editor) |
| `BlazorSortableList` | 2.1.0 |
| `Blazored.LocalStorage` | 4.5.0 |
| `FreeBlazor` | 1.0.62 |
| `CsvHelper` | 33.1.0 |
| `Humanizer` | 3.0.1 |
| `HtmlAgilityPack` | 1.12.4 |

## Build Details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net10.0` |
| Output type | Library (served by `FreeManager` host) |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
