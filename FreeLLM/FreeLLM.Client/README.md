# FreeLLM.Client

Blazor WebAssembly client for FreeLLM.

Contains the entire in-browser UI: the `Index.App.razor` component (the core LLM prompt-curation workflow), all settings pages, auth pages, shared components (Monaco editor wrapper, PDF viewer, sortable file list, upload dialog, HTML editor, plugin prompt runner), and the `BlazorDataModel` service that syncs app state across components via SignalR.

## @page routes

| Route | Component |
|-------|-----------|
| `/`, `/{TenantCode}` | Index — hosts `Index.App.razor` (file explorer + prompt builder) |
| `/About`, `/{TenantCode}/About` | About page |
| `/Login`, `/{TenantCode}/Login` | Login |
| `/Logout`, `/{TenantCode}/Logout` | Logout |
| `/ProcessLogin`, `/{TenantCode}/ProcessLogin` | OAuth callback processor |
| `/Profile`, `/{TenantCode}/Profile` | User profile |
| `/ChangePassword`, `/{TenantCode}/ChangePassword` | Password change |
| `/Plugins`, `/{TenantCode}/Plugins` | Plugin testing sandbox |
| `/Settings`, `/{TenantCode}/Settings` | Settings hub |
| `/Settings/Users`, `/Settings/UserGroups` | User and group management |
| `/Settings/EditUser/{userid}`, `/Settings/AddUser` | Edit/add user |
| `/Settings/Departments`, `/Settings/DepartmentGroups` | Department management |
| `/Settings/Tenants`, `/Settings/EditTenant/{tenantid}` | Tenant management |
| `/Settings/AppSettings` | Application-wide settings |
| `/Settings/Files`, `/Settings/Language`, `/Settings/UDF` | File storage, language, user-defined fields |
| `/Settings/DeletedRecords` | Soft-deleted record recovery |
| `/Setup` | First-run setup wizard |
| `/Authorization/AccessDenied`, `/Authorization/InvalidUser` | Auth error pages |

## Key classes and components

| Class / Component | File | Purpose |
|---|---|---|
| `Index.App.razor` | `Shared/AppComponents/Index.App.razor` | Full LLM prompt-curation UI: file listing, filtering, chunking, clipboard export |
| `FileItem` (inner class) | `Index.App.razor` | Holds `FileName`, `FullPath`, `IsSelected`, `LineCount`, `CharCount` per listed file |
| `SplitSummaryIntoChunks()` | `Index.App.razor` | Greedy block-aware line-count balancer; emits `=== Chunk X of N ===` headers |
| `CheckDefaults()` | `Index.App.razor` | Pre-selects `Program.cs`, `App.razor`, `MainLayout.razor`, `DataController.cs`, etc. by path suffix |
| `UpdateSummary()` | `Index.App.razor` | Debounced (300 ms) orchestrator; fetches metadata and file contents then rebuilds output |
| `Settings.App.razor` | `Shared/AppComponents/Settings.App.razor` | App-specific settings panel |
| `MonacoEditor.razor` | `Shared/MonacoEditor.razor` | Wraps `BlazorMonaco` for code display |
| `RenderFiles.razor` | `Shared/RenderFiles.razor` | Renders file lists with preview |

## Project references and notable packages

**Project references:** `FreeLLM.DataObjects`

| Package | Version | Use |
|---------|---------|-----|
| `FreeBlazor` | 1.0.60 | Base WSU-EIT Blazor component library |
| `MudBlazor` | 8.12.0 | Material Design components |
| `Radzen.Blazor` | 7.3.5 | Additional UI components |
| `BlazorMonaco` | 3.3.0 | Monaco code editor |
| `BlazorSortableList` | 2.1.0 | Drag-to-sort list component |
| `Blazored.LocalStorage` | 4.5.0 | Browser local storage access |
| `Humanizer` | 2.14.1 | Human-readable number/date formatting |
| `CsvHelper` | 33.1.0 | CSV export |
| `HtmlAgilityPack` | 1.12.3 | HTML parsing |
| `Microsoft.CodeAnalysis.CSharp` | 4.14.0 | Roslyn; client-side syntax analysis |
| `Microsoft.AspNetCore.SignalR.Client` | 9.0.9 | Real-time hub connection |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | net9.0 |
| Nullable | enabled |

Part of the **FreeLLM** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The browser UI, all in C#/WebAssembly. The heart is `Index.App.razor`: a file explorer with filters, the prompt builder, the balanced chunker, and clipboard export. Around it are shared components (a Monaco code editor, a sortable file list) and the usual settings/auth pages. As you tick files and type instructions, a **debounced** rebuild (300 ms) keeps the assembled prompt up to date without lag.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| The workflow component | File listing → filter → chunk → copy | [Index.App.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Shared/AppComponents/Index.App.razor) |
| Debounced orchestrator | Rebuild the package smoothly | [Index.App.razor › UpdateSummary()](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Shared/AppComponents/Index.App.razor) |
| Client DI / startup | MudBlazor, Radzen, Monaco, SignalR | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Program.cs) |

**Why does this exist?**
So the entire prompt-curation experience runs client-side (cheap to host, snappy) and only calls the server to read files.

**What does it accomplish that other tools don't?**
- **Debounced rebuilds** keep a large prompt responsive as you edit.
- **Drag-to-sort** file ordering and an in-browser **Monaco** editor for previewing code.

**Terminology & "can I see it?"**
- **Debounce** — wait until you stop changing things, then do the expensive rebuild once.
- **Monaco** — the same editor engine that powers VS Code, embedded in the page.

**The hard part, drawn** — the live build loop:

```
  You ─▶ Index.App.razor: pick folder → filter → tick files
            │ UpdateSummary()  [debounced 300 ms]
            ▼  fetch metadata/contents → rebuild the package
        SplitSummaryIntoChunks() ──▶ === Chunk X of N === ──▶ clipboard
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
