# FreePlugins.DataObjects

Shared DTO (Data Transfer Object) class library. Both the server-side `FreePlugins.DataAccess` and the browser-side `FreePlugins.Client` (Blazor WASM) reference this project, so all types here must be serializable and must not take a dependency on server-only packages.

## What it contains

| File | Contents |
|------|----------|
| `DataObjects.cs` | Core application DTOs: users, tenants, authentication responses, settings |
| `DataObjects.ActiveDirectory.cs` | Active Directory / LDAP data objects |
| `DataObjects.Ajax.cs` | `BooleanResponse`, `StringResponse`, and other lightweight API response types |
| `DataObjects.App.cs` | Application-specific extra DTOs |
| `DataObjects.Departments.cs` | Department-related DTOs |
| `DataObjects.Services.cs` | Service-layer DTOs |
| `DataObjects.SignalR.cs` | SignalR message payloads |
| `DataObjects.Tags.cs` | Tag and tag-item DTOs |
| `DataObjects.UDFLabels.cs` | User-defined field label DTOs |
| `DataObjects.UserGroups.cs` | User group DTOs |
| `Caching.cs` | In-process cache wrapper |
| `ConfigurationHelper.cs` / `ConfigurationHelper.App.cs` | `IConfiguration` access helpers |
| `GlobalSettings.cs` / `GlobalSettings.App.cs` | Application-global settings models |

## Project references

| Reference | Role |
|-----------|------|
| `FreePlugins.Plugins` | `Plugin`, `PluginPrompt`, `PluginExecuteResult`, and related types re-exported to the client |

## Notable NuGet packages

| Package | Version |
|---------|---------|
| `System.Runtime.Caching` | `10.0.1` |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | Class library |
| Target framework | `net10.0` |
| Nullable | enabled |
| Implicit usings | enabled |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
