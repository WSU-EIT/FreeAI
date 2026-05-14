# FreeSmartsheets.DataObjects

Shared DTO (Data Transfer Object) and contract library for FreeSmartsheets. Referenced by both the server and the Blazor WebAssembly client; contains all data shapes exchanged over the API.

## What It Does

- Defines partial `DataObjects` class with all request/response objects, view models, and shared enumerations
- Provides `ConfigurationHelper` — loaded at startup to carry connection strings, feature flags, analytics codes, and enabled/disabled module lists into the DI container
- Provides `GlobalSettings` — static application-wide constants
- Provides `Caching` — lightweight in-memory cache wrappers backed by `System.Runtime.Caching.MemoryCache`
- Contains `DataObjects.App.cs` — extension point for app-specific DTOs and `SignalRUpdateType` constants specific to FreeSmartsheets

## Key Public Classes/Methods

| Class / Method | Description |
|----------------|-------------|
| `DataObjects` | Root partial class; contains all shared request/response DTOs (User, Tenant, Department, Tag, etc.) |
| `DataObjects.BooleanResponse` | Standard API response wrapper with `Result` bool and `Messages` list |
| `DataObjects.BlazorDataModelLoader` | Payload returned by the server to bootstrap the Blazor client state on first load |
| `ConfigurationHelper` | DI service carrying app configuration values into controllers and data access |
| `ConfigurationHelperLoader` | Initialization object populated in `Program.cs` before DI registration |
| `GlobalSettings` | App-wide constants (version numbers, default values) |
| `Caching` | Thread-safe in-memory cache with typed get/set helpers |

## Project References

| Reference | Role |
|-----------|------|
| `FreeSmartsheets.Plugins` | Referenced for `Plugin` and `PluginPrompt` types shared in DTOs |

## Notable NuGet Packages

| Package | Purpose |
|---------|---------|
| `System.Runtime.Caching` | In-memory caching backing `Caching` class |

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target Framework | `net10.0` |
| Output Type | Class Library |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
