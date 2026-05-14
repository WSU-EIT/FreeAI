# FreeBlazorExample.Client

Blazor WebAssembly client for the FreeBlazorExtended showcase.

Contains all in-browser UI: standard framework pages (auth, settings, profile), the `/showcase` section that demonstrates every `FreeBlazorExtended` component and feature service, and the `ShowcaseLayout` used by all demo pages. References `FreeBlazorExtended.csproj` directly so components are available without a NuGet publish step.

## @page routes

**Standard pages:**

| Route | Page |
|-------|------|
| `/`, `/{TenantCode}` | Home/Index |
| `/About`, `/{TenantCode}/About` | About |
| `/Profile`, `/{TenantCode}/Profile` | User profile |
| `/Settings`, `/{TenantCode}/Settings` | Settings hub |
| `/Settings/Users`, `/Settings/Tenants`, `/Settings/Departments` | Admin management |

**Showcase pages:**

| Route | Feature |
|-------|---------|
| `/showcase` | Showcase index |
| `/showcase/catalog` | Component catalog |
| `/showcase/feature101-dynamic-forms` | Feature 101: JSON-schema dynamic forms |
| `/showcase/feature102-multi-view-sync` | Feature 102: MultiViewSync real-time SignalR |
| `/showcase/feature103-calendar` | Feature 103: Event calendar |
| `/showcase/feature104-user-preferences` | Feature 104: Per-user preference store |
| `/showcase/feature105-agent-monitoring` | Feature 105: Windows agent management |
| `/showcase/feature107-hierarchical-tree` | Feature 107: Drag-to-reparent tree |
| `/showcase/carousel/v1-v3` | Carousel variants |
| `/showcase/commandpalette/v1-v3` | Command palette (Ctrl+K) variants |
| `/showcase/kanbanboard/v1-v3` | Kanban board variants |
| `/showcase/signature/v1-v3` | Signature pad variants |
| `/showcase/timeline/v1-v3`, `/showcase/timer/v1-v3` | Timeline/Timer variants |
| `/showcase/networkchart/v1-v3` | vis.js network chart variants |
| `/showcase/imagegallery/v1-v3` | Image gallery/lightbox variants |
| `/showcase/github-repo`, `/showcase/generic-git` | Git repository browsers |
| `/showcase/smartsheet` | Smartsheet viewer |
| `/showcase/autocomplete`, `/showcase/multiselect`, `/showcase/stringlist` | FreeBlazor component demos |
| `/showcase/datetimepicker`, `/showcase/autogrowtext`, `/showcase/togglepassword` | Input component demos |
| `/showcase/deleteconfirmation`, `/showcase/confirmationcode`, `/showcase/getinput` | Dialog/interaction demos |
| `/showcase/pagedrecordset` | Paged data table demo |

## Key classes and components

| Class / Component | Purpose |
|---|---|
| `ShowcaseLayout.razor` | Layout shell for all `/showcase/*` pages (sidebar nav, header) |
| `Feature105_AgentMonitoring.razor` | Live agent dashboard: heartbeat display, service/app-pool control UI |
| `Feature101_DynamicForms.razor` | Dynamic form builder and renderer demo |
| `Feature102_MultiViewSync.razor` | Master/slave synchronized view demo |
| `Feature107_HierarchicalTree.razor` | Tree component with drag-to-reparent demo |

## Project references and notable packages

**Project references:** `FreeBlazorExample.DataObjects`, `FreeBlazorExtended`

| Package | Version | Use |
|---------|---------|-----|
| `FreeBlazor` | 1.0.62 | Base WSU-EIT Blazor component library |
| `MudBlazor` | 8.15.0 | Material Design components |
| `Radzen.Blazor` | 9.0.4 | Additional UI components |
| `BlazorMonaco` | 3.4.0 | Monaco code editor |
| `BlazorSortableList` | 2.1.2 | Drag-to-sort list component |
| `Blazored.LocalStorage` | 4.5.0 | Browser local storage |
| `Humanizer` | 3.0.1 | Human-readable formatting |
| `FluentValidation` | 12.1.1 | Validation for dynamic forms |
| `CsvHelper` | 33.1.0 | CSV export |
| `HtmlAgilityPack` | 1.12.4 | HTML parsing |
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.3 | SignalR hub connection |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | net10.0 |
| Nullable | enabled |

Part of the **FreeBlazorExtended** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
