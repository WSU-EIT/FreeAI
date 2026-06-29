# FreePlugins.Client

Blazor WebAssembly client application. This project runs entirely in the browser and communicates with the `FreePlugins` ASP.NET Core host via the `DataController` REST API and a SignalR hub.

## What it does

- Hosts all Blazor UI pages and components for the FreePlugins application
- Provides a `DataModel` client for calling the server API
- Includes `Helpers` utilities used across client-side pages
- Consumes `FreePlugins.DataObjects` for shared DTOs (no server-side dependencies)

## Key files

| File | Purpose |
|------|---------|
| `DataModel.cs` / `DataModel.App.cs` | Typed HTTP client for the server REST API |
| `Helpers.cs` / `Helpers.App.cs` | Client-side utility methods |
| `Program.cs` | WASM entry point — DI bootstrap, service registration |

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.DataObjects` | Shared DTOs (users, tenants, settings, plugin objects, etc.) |

## Notable NuGet packages

| Package | Purpose |
|---------|---------|
| `FreeBlazor` | WSU-EIT shared Blazor component library |
| `Blazor.Bootstrap` | Bootstrap-based Blazor components |
| `MudBlazor` | Material Design Blazor component library |
| `Radzen.Blazor` | Additional Blazor UI components |
| `BlazorMonaco` | Monaco code editor component |
| `BlazorSortableList` | Drag-and-drop sortable lists |
| `Blazored.LocalStorage` | Browser local storage access |
| `Microsoft.AspNetCore.SignalR.Client` | SignalR real-time communication |
| `CsvHelper` | CSV import/export |
| `HtmlAgilityPack` | HTML parsing |
| `Humanizer` | Human-readable strings |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The browser UI (Blazor WebAssembly). It hosts all pages and components, calls the server through a typed `DataModel` HTTP client, and renders the plugin management/testing UI — including the prompt inputs a plugin declares.

**What tech & where?** [DataModel.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Client/DataModel.cs) (API client) · [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreePlugins/FreePluginsV1/FreePlugins.Client/Program.cs).

**Why does this exist?** A client-rendered UI for browsing, configuring, and running plugins.

**What does it beat?** It renders the **16 prompt types** a plugin can request, so plugin authors get a real input UI for free.

**Terminology:** **Prompt UI** — the inputs the client renders from a plugin's declared prompts.

**The hard part, drawn:**
```
  You ─▶ plugin page ─▶ DataModel ─HTTP─▶ server ─▶ run plugin ─▶ result/prompt UI re-renders
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
