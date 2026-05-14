# FreeBlazorExample.DataObjects

Shared DTO and contract library for FreeBlazorExample.

Referenced by both the ASP.NET Core server and the Blazor WebAssembly client. Defines data-transfer objects, typed API endpoint constants, response envelopes, the `BlazorDataModel` client-state class, and application-wide settings models. No EF Core, HTTP, or auth dependencies.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `DataObjects` (partial) | `DataObjects.cs` | Core DTOs: `ActiveUser`, `ApplicationSettings`, `Tenant`, `UserGroup`, `BooleanResponse` |
| `DataObjects` (partial) | `DataObjects.App.cs` | App-specific DTO extensions |
| `GlobalSettings` | `GlobalSettings.cs` + `GlobalSettings.App.cs` | Singleton runtime configuration from appsettings |
| `DataObjects.Endpoints` | various | Typed API route constants for all controllers |

## Project references and notable packages

**Project references:** `FreeBlazorExample.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `System.Runtime.Caching` | 10.0.3 | `MemoryCache` in shared caching helpers |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net10.0 |
| Nullable | enabled |

Part of the **FreeBlazorExtended** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
