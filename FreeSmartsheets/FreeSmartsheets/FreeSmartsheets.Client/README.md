# FreeSmartsheets.Client

Blazor WebAssembly client project for FreeSmartsheets. Contains all interactive UI components, page routes, shared components, and the client-side data model. Runs in the browser via WebAssembly and communicates with the server via the `DataController` REST API and SignalR.

## What It Does

- Provides the browser-side Blazor WebAssembly application loaded by the server host
- Maintains `BlazorDataModel` — a singleton client-side state object that all pages inject and react to via `OnChange` events
- Provides the `Helpers` static class with API call helpers (`GetOrPost<T>`), URL builders, and SignalR update processors
- Includes a `DynamicBlazorSupport` subsystem (`CompilationService`, `VirtualProjectFileSystem`) for in-browser Roslyn compilation of Blazor plugin components — enables plugin UI to be compiled and rendered at runtime in the browser
- Renders all settings pages, authorization pages, and platform UI via shared components in `Shared/`
- Supports optional precompilation of all Blazor plugin components on page load (controlled by `BlazorDataModel.PrecompileBlazorPlugins`)

## @page Routes (Blazor)

All routes support an optional `/{TenantCode}/` prefix.

| Route | Purpose |
|-------|---------|
| `/` | Home page |
| `/Login` | Login |
| `/Logout` | Logout |
| `/Profile` | User profile and password change |
| `/Settings/Users` | User management |
| `/Settings/UserGroups` | User group management |
| `/Settings/Departments` | Department management |
| `/Settings/Files` | File storage management |
| `/Settings/Tags` | Tag management |
| `/Settings/Tenants` | Tenant management |
| `/Settings/Misc/Settings` | Application settings |
| `/Settings/Misc/Setup` | Initial setup wizard |

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `BlazorDataModel` | Client-side singleton state; holds current user, tenant, settings, and triggers `OnChange` on mutations |
| `BlazorDataModel.PrecompileBlazorPlugins` | When `true`, all plugin Blazor components are compiled on page load |
| `Helpers.GetOrPost<T>` | Unified typed HTTP call helper to the server REST API |
| `Helpers.BuildUrl` | Constructs tenant-aware URLs from page names |
| `Helpers.MenuItemsApp` | Hook for app-specific navigation menu entries |
| `CompilationService` | In-browser Roslyn compiler for dynamic Blazor plugin components |
| `VirtualProjectFileSystem` | In-memory virtual filesystem backing the browser-side Blazor compiler |

## Project References

| Reference | Role |
|-----------|------|
| `FreeSmartsheets.DataObjects` | Shared DTOs used by both client and server |

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Components.WebAssembly` | Blazor WASM runtime |
| `Microsoft.AspNetCore.SignalR.Client` | SignalR client for real-time updates |
| `MudBlazor` | Material Design component library |
| `Radzen.Blazor` | Radzen UI component library |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn compiler for in-browser dynamic component compilation |
| `FluentValidation` | Model validation |
| `BlazorMonaco` | Monaco editor component for code editing |
| `Blazor.Bootstrap` | Bootstrap-based Blazor components |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target Framework | `net10.0` |
| Output Type | WebAssembly browser application |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
