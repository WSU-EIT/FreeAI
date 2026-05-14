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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
