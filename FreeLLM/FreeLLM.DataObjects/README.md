# FreeLLM.DataObjects

Shared DTO and contract library for FreeLLM.

Referenced by both the ASP.NET Core server and the Blazor WebAssembly client. Defines all data-transfer objects, enums, response envelopes, typed API endpoint constants, application-wide settings models, and the `BlazorDataModel` client-state class. No external service dependencies — no EF Core, no HTTP, no auth.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `DataObjects.EndpointFileGpt` | `DataObjects.App.FreeLLM.cs` | Constants for `api/Data/GetFiles`, `GetFileMetadata`, `GetFileContents` |
| `DataObjects.FileItem` | `DataObjects.App.FreeLLM.cs` | `FileName` + `FullPath`; returned by `GetFiles` |
| `DataObjects.FileContentItem` | `DataObjects.App.FreeLLM.cs` | `FileName`, `FullPath`, `Content`; returned by `GetFileContents` |
| `DataObjects.FileMetadataItem` | `DataObjects.App.FreeLLM.cs` | `FileName`, `FullPath`, `LineCount`, `CharCount`; returned by `GetFileMetadata` |
| `DataObjects.FilePathRequest` | `DataObjects.App.FreeLLM.cs` | Request body for `GetFiles`: `{ Path }` |
| `DataObjects.FileContentRequest` | `DataObjects.App.FreeLLM.cs` | Request body for `GetFileContents`/`GetFileMetadata`: `{ FilePaths }` |
| `DataObjects.ApplicationSettings` | `DataObjects.cs` | Global app settings (URL, maintenance mode, tenant config, mail server) |
| `DataObjects.ActiveUser` | `DataObjects.cs` | Logged-in user identity with preferences and last-access timestamp |
| `GlobalSettings` | `GlobalSettings.cs` + `GlobalSettings.App.cs` | Singleton runtime configuration loaded from appsettings |

## Project references and notable packages

**Project references:** `FreeLLM.Plugins`

| Package | Version | Use |
|---------|---------|-----|
| `System.Runtime.Caching` | 9.0.9 | `MemoryCache` used in shared caching helpers |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net9.0 |
| Nullable | enabled |

Part of the **FreeLLM** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
