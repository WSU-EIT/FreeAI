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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A pure "data shapes" library shared by the server and the browser. It holds all request/response DTOs, plus `ConfigurationHelper` (carries config into DI), `GlobalSettings` (app constants), `Caching` (a small in-memory cache), and `BlazorDataModelLoader` (the boot payload the server sends the client on first load).

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Shared C# DTOs (.NET 10) | The contract between client and server | [DataObjects.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataObjects/DataObjects.cs) |
| Config carrier | Connection strings, flags, modules into DI | [ConfigurationHelper.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataObjects/ConfigurationHelper.cs) |
| In-memory cache | Short-lived caching helper | [Caching.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeSmartsheets/FreeSmartsheets/FreeSmartsheets.DataObjects/Caching.cs) |

**Why does this exist?**
So the two halves of the app compile against the *same* shapes — a contract break fails at build time, not in production. (Smartsheet-specific DTOs would be added in `DataObjects.App.cs` when that integration is built.)

**What does it accomplish that other tools don't?**
- One model used by C# on the server **and** in the browser — no separate TypeScript types to drift.
- A single boot payload (`BlazorDataModelLoader`) hydrates the whole client in one round-trip.

**Terminology & "can I see it?"**
- **DTO** — a plain data shape with no behavior.
- **Boot payload** — everything the client needs at startup, sent once.

**The hard part, drawn** — one vocabulary, both sides:

```
  Server ─┐                                ┌─ Browser (WASM)
          ├─ DataObjects (shared C#) ───────┤
  DB ◀────┘  User · Tenant · Department · Tag └─▶ UI binds the SAME types
             BooleanResponse · BlazorDataModelLoader (one-shot boot payload)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT

Part of the [FreeSmartsheets](../../../README.md) solution.
