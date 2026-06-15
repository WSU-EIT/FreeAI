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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A pure "data shapes" library shared by server and browser. Beyond the usual settings/user DTOs, it holds the **FreeLLM-specific file contract**: the request/response shapes for the three file endpoints (`FileItem`, `FileContentItem`, `FileMetadataItem`) and the typed route-name constants (`EndpointFileGpt`). No EF Core, no HTTP, no auth — just shapes.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| FreeLLM file DTOs + endpoint constants | The client/server file contract | [DataObjects.App.FreeLLM.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeLLM/FreeLLM.DataObjects/DataObjects.App.FreeLLM.cs) |
| `System.Runtime.Caching` | Shared cache helper | [the DataObjects project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeLLM/FreeLLM.DataObjects) |

**Why does this exist?**
So the curation UI and the file API agree on *exactly* what a file request and response look like — a mismatch becomes a compile error, not a runtime bug.

**What does it accomplish that other tools don't?**
- The three endpoints' request/response shapes **and their route names** are defined once and shared, so client and server can't drift apart.

**Terminology & "can I see it?"**
- **Contract** — the agreed shapes the two halves exchange.
- **Endpoint constant** — a typed name for an API route, so nobody hard-codes a string.

**The hard part, drawn** — one file contract, both sides:

```
  Server (file API) ─┐                                   ┌─ Browser (curation UI)
                     ├── DataObjects.App.FreeLLM (shared) ┤
  disk ◀─────────────┘  FileItem · FileContentItem        └─▶ UI binds the SAME types
                        FileMetadataItem · EndpointFileGpt (the 3 route names)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
