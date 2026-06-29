# FreeAI.Client

Blazor WebAssembly client application.

This is the FreeCRM scaffold client, renamed to the FreeAI namespace with no app-specific pages added. It delivers the full multi-tenant Blazor WASM UI: login/logout/OAuth flows, user profile and password management, settings administration for tenants, users, user groups, departments, department groups, files, languages, UDF labels, and application settings. The `Index.App.razor` home page shows a welcome message with an optional per-tenant logo and has a stub `LoadData()` for app-specific content. `BlazorDataModel.App.cs` adds a single placeholder `MyValues` property and an empty deleted-records check.

## @page routes

All routes are available with and without a `/{TenantCode}` prefix.

| Route | Page |
|---|---|
| `/` | Home (Index) |
| `/Login`, `/Logout`, `/ProcessLogin` | Authentication flow |
| `/Profile`, `/ChangePassword`, `/PasswordChanged` | User self-service |
| `/Settings` | Settings hub |
| `/Settings/Users`, `/Settings/EditUser/{userid}`, `/Settings/AddUser` | User management |
| `/Settings/UserGroups`, `/Settings/EditUserGroup/{groupid}`, `/Settings/AddUserGroup` | User group management |
| `/Settings/Tenants`, `/Settings/EditTenant/{tenantid}`, `/Settings/AddTenant` | Tenant management |
| `/Settings/Departments`, `/Settings/EditDepartment/{departmentid}` | Department management |
| `/Settings/DepartmentGroups`, `/Settings/EditDepartmentGroup/{departmentgroupid}` | Department group management |
| `/Settings/Files`, `/Settings/AppSettings`, `/Settings/Language`, `/Settings/UDF` | Misc admin settings |
| `/Settings/DeletedRecords` | Soft-delete recovery |
| `/Plugins` | Plugin testing page |
| `/Setup` | First-run database setup |
| `/DatabaseOffline` | Error page when DB is unreachable |
| `/About` | Application about page |
| `/Authorization/AccessDenied`, `/Authorization/InvalidUser`, `/Authorization/NoLocalAccount` | Auth error pages |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `FreeBlazor` | WSU-EIT shared Blazor component library |
| `MudBlazor` | Material Design component framework |
| `Blazor.Bootstrap` | Bootstrap 5 Blazor components |
| `Radzen.Blazor` | Additional data-grid and chart components |
| `Blazored.LocalStorage` | Browser localStorage interop |
| `BlazorMonaco` | Monaco (VS Code) in-browser code editor |
| `BlazorSortableList` | Drag-and-drop sortable lists |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn (used by plugin testing page) |
| `CsvHelper` | CSV import/export |
| `HtmlAgilityPack` | HTML parsing |
| `Humanizer` | Human-friendly string formatting |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.DataObjects` | Shared DTOs consumed by both client and server |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly` |
| Target framework | `net9.0` |
| Output type | WebAssembly app (hosted by FreeAI server) |

Part of the ChatWithAI solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
The whole admin/user interface as C# compiled to **WebAssembly** and run in the browser: login, profile, password management, and settings administration for tenants, users, groups, departments, files, languages, and custom fields. It's the FreeCRM scaffold UI renamed to `FreeAI`; the home page's `LoadData()` is an empty stub for future app content.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Blazor WebAssembly (.NET 9) | C# UI running client-side | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.Client/Program.cs) |
| API client + helpers | Every server call lives here | [Helpers.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.Client/Helpers.cs) |
| MudBlazor / Radzen / BlazorMonaco | UI components + in-browser code editor | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.Client/Program.cs) |

**Why does this exist?**
A complete multi-tenant admin UI you get *for free*, client-rendered so it ships as static files (cheap to host, scales trivially) and only calls the server for data.

**What does it accomplish that other tools don't?**
- Every route works **with or without** a `/{TenantCode}` prefix — one UI cleanly serves many tenants.
- A built-in **plugin testing page** and an in-browser **Monaco** (VS Code) editor.

**Terminology & "can I see it?"**
- **WebAssembly (WASM)** — lets compiled C# run at near-native speed in the browser.
- **Tenant prefix** — the `/{TenantCode}` URL segment that scopes the UI to one organization.

**The hard part, drawn** — a click becomes a tenant-scoped server call:

```
  You ─click─▶ *.razor page ─▶ Helpers.cs ─HTTP─▶ server API ─▶ DB
                   └──── routes resolve with OR without /{TenantCode} prefix ────┘
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
