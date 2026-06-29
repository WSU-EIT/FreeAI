# FreeLLM

Blazor WebAssembly + ASP.NET Core application for curating source files into structured LLM prompt packages.

FreeLLM guides engineers through selecting files from a local directory, applying filters and exclusions, computing line/character counts, assembling modification instructions, and exporting a clean, chunk-balanced prompt ready to paste into any LLM (ChatGPT, Azure OpenAI, etc.).

## Projects in this solution

| Project | SDK | Framework | Role |
|---------|-----|-----------|------|
| `FreeLLM` | `Microsoft.NET.Sdk.Web` | net9.0 | ASP.NET Core host; controllers, SignalR hub, auth, plugin loader |
| `FreeLLM.Client` | `Microsoft.NET.Sdk.BlazorWebAssembly` | net9.0 | WASM client; Index page workflow, all UI components |
| `FreeLLM.DataAccess` | `Microsoft.NET.Sdk` | net9.0 | Server-side business logic, EF Core repositories, auth helpers |
| `FreeLLM.DataObjects` | `Microsoft.NET.Sdk` | net9.0 | Shared DTOs and endpoint constants used by both server and client |
| `FreeLLM.EFModels` | `Microsoft.NET.Sdk` | net9.0 | EF Core DbContext and entity classes; supports SQL Server, SQLite, MySQL, PostgreSQL |
| `FreeLLM.Plugins` | `Microsoft.NET.Sdk` | net9.0 | Roslyn-based dynamic C# plugin runtime and `IPlugin` contracts |

## What the application does

The **Index page** (`/`) is the core workflow. On load it embeds `Index.App.razor` which:

1. Accepts a local directory path and calls `POST api/Data/GetFiles` to enumerate files, excluding `bin/`, `obj/`, and hidden directories.
2. Displays the file list with checkboxes and real-time file-count badges (colored by line-count thresholds: green < 1000 lines, blue < 2000, amber < 3000, red ≥ 3000).
3. Filters by extension (`.cs`, `.razor`, `.cshtml`, `.md` on by default; `.js`, `.css`, `.ts`, `.json`, `.html`, `.xml`, `.scss` toggleable), wildcard pattern, plain-text path search, and ignored-folder exclusions (`fontawesome`, `bootstrap` by default).
4. Calls `POST api/Data/GetFileMetadata` for unselected files (line/char counts only) and `POST api/Data/GetFileContents` for selected files (full content), both cached server-side for 5 minutes via `IMemoryCache`.
5. Assembles a structured "Project Update" string with: selected-file manifest, top modification instructions, editable before-text (8-step AI prompt scaffold), full file contents with size annotations, common modification phrases, editable after-text, and optional bottom instructions.
6. Splits the output into N balanced chunks (greedy line-count packing, configurable via a number input), each prefixed with a `=== Chunk X of N ===` header.
7. Copies all chunks to the clipboard via `navigator.clipboard.writeText`.

`CheckDefaults()` pre-selects canonical entry-point files (`Program.cs`, `App.razor`, `MainLayout.razor`, `DataAccess.cs`, `DataObjects.cs`, `DataController.cs`, `EFDataModel.cs`).

## @page routes (FreeLLM.Client)

| Route | Component |
|-------|-----------|
| `/`, `/{TenantCode}` | Index (hosts `Index.App.razor`) |
| `/About`, `/{TenantCode}/About` | About |
| `/Login`, `/{TenantCode}/Login` | Login |
| `/Logout`, `/{TenantCode}/Logout` | Logout |
| `/Profile`, `/{TenantCode}/Profile` | User profile |
| `/ChangePassword`, `/{TenantCode}/ChangePassword` | Change password |
| `/Plugins`, `/{TenantCode}/Plugins` | Plugin testing |
| `/Settings`, `/{TenantCode}/Settings` | Settings hub |
| `/Settings/Users`, `/Settings/UserGroups` | User management |
| `/Settings/Departments`, `/Settings/Tenants` | Org management |
| `/Settings/AppSettings`, `/Settings/Files` | App configuration |
| `/Setup` | First-run setup |
| `/Authorization/AccessDenied`, `/Authorization/InvalidUser` | Auth error pages |

## Key public classes and methods

| Class / Method | Location | Purpose |
|---|---|---|
| `DataController.GetFiles` | `FreeLLM/Controllers/DataController.App.FreeLLM.cs` | Enumerates directory files, strips hidden/bin/obj paths |
| `DataController.GetFileContents` | same | Returns full text for a list of paths; 5-min `IMemoryCache` |
| `DataController.GetFileMetadata` | same | Returns line/char counts for a list of paths; same cache key |
| `Index.App.razor — SplitSummaryIntoChunks()` | `FreeLLM.Client/Shared/AppComponents/Index.App.razor` | Greedy block-aware chunker; emits chunk headers |
| `Index.App.razor — CheckDefaults()` | same | Pre-selects canonical file set by suffix matching |
| `Index.App.razor — UpdateSummary()` | same | Debounced (300 ms) full rebuild of the prompt package |
| `DataObjects.EndpointFileGpt` | `FreeLLM.DataObjects/DataObjects.App.FreeLLM.cs` | Typed constants for the three API routes |
| `EFDataModel` | `FreeLLM.EFModels/EFModels/EFDataModel.cs` | DbContext for Users, Tenants, Settings, Departments, FileStorage, PluginCaches |

## Notable NuGet packages

| Package | Project | Use |
|---------|---------|-----|
| `FreeBlazor` 1.0.60 | Client | Base Blazor component library from WSU-EIT |
| `MudBlazor` 8.12 | Client | Material UI components |
| `Radzen.Blazor` 7.3 | Client | Additional UI components |
| `BlazorMonaco` 3.3 | Client | Monaco editor component |
| `BlazorSortableList` 2.1 | Client | Drag-to-sort lists |
| `Humanizer` 2.14 | Client | Human-readable string formatting |
| `Microsoft.CodeAnalysis.CSharp` 4.14 | Client + DataAccess | Roslyn; client-side analysis and plugin compilation |
| `Microsoft.Azure.SignalR` 1.32 | Server | Azure SignalR fallback (local SignalR used when not configured) |
| `QuestPDF` 2025.7 | DataAccess | PDF generation |
| `Microsoft.Graph` 5.92 | DataAccess | Microsoft Graph API integration |
| `Novell.Directory.Ldap.NETStandard` 4.0 | DataAccess | LDAP/Active Directory authentication |
| `Basic.Reference.Assemblies.Net90` 1.8.3 | Plugins | Reference assemblies for Roslyn dynamic compilation |
| EF Core providers 9.0 | EFModels | SQL Server, SQLite, MySQL, PostgreSQL support |

## Build details

| Field | Value |
|-------|-------|
| Target framework | net9.0 |
| Auth options | Local accounts, Google, Facebook, Microsoft, Apple, OpenID Connect |
| SignalR | Local or Azure SignalR (configurable via `AzureSignalRurl` in appsettings) |
| Nullable | enabled |
| User secrets | `ffba3dfe-46f7-436d-8b09-e77396f7920b` (server project) |

Part of the **FreeLLM** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeLLM is a "prompt packager." You point it at a folder on your machine; it lists the files (skipping `bin/`, `obj/`, hidden), lets you filter and tick the ones you want, counts the lines and characters, then assembles a single structured prompt — a manifest of selected files, an 8-step instruction scaffold, the full file contents, and your edit instructions. Because LLMs have a size limit, it then splits that big prompt into **N balanced chunks** (a greedy line-count packer), each labeled `=== Chunk X of N ===`, and copies them to your clipboard ready to paste into ChatGPT / Azure OpenAI / etc. File contents are fetched through three small server endpoints that cache results for 5 minutes.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WASM workflow component | The entire curation UI | [Index.App.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Shared/AppComponents/Index.App.razor) |
| Greedy chunk balancer | Split the prompt into even pieces | [Index.App.razor › SplitSummaryIntoChunks()](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Shared/AppComponents/Index.App.razor) |
| Three file endpoints + 5-min cache | Enumerate / count / read local files | [DataController.App.FreeLLM.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM/Controllers/DataController.App.FreeLLM.cs) |
| Endpoint constants + file DTOs | The shared client/server contract | [DataObjects.App.FreeLLM.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.DataObjects/DataObjects.App.FreeLLM.cs) |
| Roslyn plugin runtime | Extend at runtime | [Plugins.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Plugins/Plugins.cs) |

**Why does this exist?**
Pasting a codebase into an LLM by hand is tedious and error-prone: you forget files, blow past the size limit, and lose the structure the model needs. FreeLLM makes a clean, consistent, *size-safe* prompt package in a few clicks.

**What does it accomplish that other tools don't?**
- **Balanced chunking** so a large codebase fits an LLM's window in even pieces, not one giant blob that gets truncated.
- **Live size feedback** — per-file line counts color-coded by threshold (green < 1000, blue < 2000, amber < 3000, red ≥ 3000) so you see the cost before you build.
- **A consistent prompt scaffold** (manifest + 8-step instructions + contents) — every prompt has the structure models respond best to.
- **Smart defaults** — it pre-ticks canonical entry-point files (`Program.cs`, `App.razor`, `DataAccess.cs`…).

**Terminology & "can I see it?"**
- **Prompt package** — the assembled text you paste into an LLM.
- **Chunk** — one size-bounded slice of that package.
- **Greedy packing** — fill each chunk up to the line budget before starting the next.
- **Debounce** — wait 300 ms after your last change before rebuilding, so typing stays smooth.
- *See it:* the whole workflow is one component — [Index.App.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.Client/Shared/AppComponents/Index.App.razor).

**The hard part, drawn** — a folder becomes paste-ready, size-safe chunks:

```
  you ─▶ enter a folder path
            │ POST GetFiles   (skip bin/obj/hidden)
            ▼
   file list + checkboxes + line/char badges  (green<1k · blue<2k · amber<3k · red≥3k)
            │ tick files → POST GetFileMetadata (counts) / GetFileContents (text)  [5-min cache]
            ▼
  ┌──────────────── assemble the prompt package ────────────────┐
  │ file manifest + 8-step instruction scaffold + full contents │
  │ + your before/after edit instructions                       │
  └───────────────────────────┬─────────────────────────────────┘
            │ SplitSummaryIntoChunks(): greedy line-count packing into N pieces
            ▼
   === Chunk 1 of N ===  …  === Chunk N of N ===  ──▶ copied to clipboard
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
