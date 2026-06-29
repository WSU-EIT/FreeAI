# FreeAI.DataObjects

Class library — shared data transfer objects.

This is the FreeCRM scaffold DTO layer, renamed to the FreeAI namespace. The single `DataObjects` partial class (spread across several files) defines all request/response shapes shared between the Blazor WASM client and the ASP.NET Core server. `DataObjects.App.cs` adds a placeholder `MyCustomUserProperty` on `User` and a stub `YourClass` — no app-specific DTOs have been added yet. `GlobalSettings` holds static startup-state flags (`StartupRun`, `StartupError`, `PluginsSavedToCache`, `RunningSince`). `Caching.cs` provides a `System.Runtime.Caching`-backed in-process object cache helper used by the server.

## Key public classes

| Class | Notes |
|---|---|
| `DataObjects` | Root partial class containing all DTOs (see below for members) |
| `DataObjects.User` | Full user record including 10 UDF fields, auth token, group membership |
| `DataObjects.Tenant` | Tenant record with nested `TenantSettings` (themes, auth, LDAP, JWT keys) |
| `DataObjects.BlazorDataModelLoader` | Full payload sent to the WASM client on boot |
| `DataObjects.Filter` / `FilterUsers` / `FilterFileStorage` | Paginated filter request/response objects |
| `DataObjects.BooleanResponse` | Standard success/messages wrapper returned by most write operations |
| `GlobalSettings` | Static startup state (error codes, run flags, plugin cache flag) |
| `Caching` | In-process object cache (wraps `System.Runtime.Caching.MemoryCache`) |

## Project references

| Reference | Role |
|---|---|
| `FreeAI.Plugins` | `Plugin`, `PluginPrompt`, and related types referenced in `BlazorDataModelLoader` |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `System.Runtime.Caching` | In-process memory cache used by `Caching.cs` |

## Build info

| Field | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net9.0` |
| Output type | Library |

Part of the ChatWithAI solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A pure "data shapes" library — every request/response object shared between the WASM browser client and the server. No database code, no logic. The standout is `BlazorDataModelLoader`: the full payload the server sends the browser on boot (current user, tenant, settings). `Caching.cs` wraps an in-process memory cache. App-specific DTOs are still stubs.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Plain C# DTOs (.NET 9) | The shared shapes | [DataObjects.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.DataObjects/DataObjects.cs) |
| `System.Runtime.Caching` | In-process object cache | [Caching.cs](https://github.com/WSU-EIT/FreeAI/blob/main/ChatWithAI/FreeAI.DataObjects/Caching.cs) |

**Why does this exist?**
When server and browser compile against the *same* shapes, a contract-breaking change fails at **build time**, not in production.

**What does it accomplish that other tools don't?**
- One shared model used by C# on the server **and** in the browser — no hand-written TypeScript types to drift.
- A single boot payload (`BlazorDataModelLoader`) hydrates the whole client in one round-trip.

**Terminology & "can I see it?"**
- **DTO** — a plain shape with fields and no behavior.
- **Boot payload** — everything the client needs at startup, sent once.

**The hard part, drawn** — one vocabulary, both sides of the wire:

```
  Server ─┐                                ┌─ Browser (WASM)
          ├──  DataObjects (shared C#)  ────┤
  DB ◀────┘  User · Tenant · Filter         └─▶ UI binds the SAME types
             BooleanResponse · BlazorDataModelLoader (one-shot boot payload)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
