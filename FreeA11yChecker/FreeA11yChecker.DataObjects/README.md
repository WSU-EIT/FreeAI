# FreeA11yChecker.DataObjects

Shared DTO library for FreeA11yChecker. Contains every request/response model, enum, and configuration record used by both the Blazor Server host and the Blazor WebAssembly client. Has no EF Core dependency and no business logic — it is a pure data-shape layer.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Class library |

## What Is in Here

### Core shared objects (`DataObjects.cs`)

`DataObjects` is a single large partial class. Key records include:

| Class | Purpose |
|---|---|
| `ActionResponseObject` | Base class for all response DTOs; carries a `BooleanResponse ActionResponse` |
| `BooleanResponse` | Standard success/error envelope with `Result`, `Message`, and `ErrorList` |
| `ApplicationSettings` | Global app configuration (URL, mail server, encryption key, tenant code) |
| `Authenticate` | Login request: username, password, tenant code/ID |
| `User` | Full user profile including roles, preferences, photo, department, and token |
| `Tenant` | Tenant record with settings, enabled flags, and custom fields |
| `SignalRUpdate` | SignalR message envelope: tenant-scoped, typed by `Module` and `Action` |

### App-specific scan objects (`FreeA11yChecker.App.DataObjects.Scans.cs`)

| Class | Purpose |
|---|---|
| `ScanRun` | Represents one scan execution: status, page count, violation counts by severity, triggered-by label |
| `PageScanResult` | Per-page result: URL, HTTP status, HTML size, violation counts per engine, JSON blobs for keyboard nav, text spacing, reading level, fixed elements, mobile viewports, focus traps, accessibility tree |
| `A11yViolation` | Individual violation: canonical rule ID, WCAG criterion, impact, engine source, affected HTML element, help text |
| `ScanScreenshot` | Screenshot record with file name, label (e.g., `"fullpage"`, `"cvd-deuteranopia"`), and binary data |
| `ScanRankedRule` | Rule ranked by consensus score: tool count, severity, WCAG level, element count |
| `ViolationSuppression` | Per-tenant rule suppression with justification and expiry |
| `CrossSiteViolation` | Aggregated violation count for a rule across all sites in a tenant |
| `ManualCheckResult` | Result of a manually completed WCAG criterion check |

### App-specific site objects (`FreeA11yChecker.App.DataObjects.Sites.cs`)

| Class | Purpose |
|---|---|
| `Site` | Audit target: base URL, cron schedule, concurrency, last scan summary |
| `SitePage` | Individual URL within a site configured for scanning |
| `SiteCredential` | Authentication credential set for a site (username/password or token) |

### Configuration and caching

| Class | Purpose |
|---|---|
| `ConfigurationHelper` / `ConfigurationHelperLoader` | Carries runtime configuration values (analytics code, base path, connection strings, enabled/disabled modules) as a DI-injectable object |
| `Caching` (`Caching.cs`) | In-process `MemoryCache` wrapper with typed get/set helpers |
| `GlobalSettings` | Static application-wide constants |

## Project References

| Project | Role |
|---|---|
| `FreeA11yChecker.Plugins` | Plugin type definitions referenced in some DTO methods |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `System.Runtime.Caching` | `MemoryCache` backing for `Caching.cs` |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A pure "data shapes" library — every request/response object, enum, and config record shared by both the server and the browser client. No database code, no logic: it's just the *vocabulary* both sides agree on (a `ScanRun`, an `A11yViolation`, a `SignalRUpdate`, a `User`).

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Plain C# records/classes (.NET 10) | The shared data shapes | [the DataObjects project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.DataObjects) |
| Scan-specific DTOs | `ScanRun`, `A11yViolation`, `ScanScreenshot` | [App.DataObjects.Scans](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.DataObjects) |
| `System.Runtime.Caching` | In-process `MemoryCache` helper | [the DataObjects project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.DataObjects) |

**Why does this exist?**
When the server and the browser client both compile against the *same* shapes, an API change that breaks the contract fails at **build time**, not in production.

**What does it accomplish that other tools don't?**
- One shared model used by C# on the server **and** C# in the browser — there are no hand-written TypeScript types to drift out of sync.
- A standard success/error envelope (`BooleanResponse`) every endpoint returns, so error handling is uniform.

**Terminology & "can I see it?"**
- **DTO** (Data Transfer Object) — a plain shape with no behavior, just fields.
- **Contract** — the agreed set of shapes the two halves of the app exchange.

**The hard part, drawn** — one vocabulary, both sides of the wire:

```
   Server  ─┐                                   ┌─  Browser (WASM client)
            ├──  DataObjects (shared C#)  ───────┤
   EF / DB ◀┘   ScanRun · A11yViolation          └─▶  UI binds to the SAME types
                SignalRUpdate · User · BooleanResponse
        a breaking change here = a compile error on BOTH sides (not a runtime surprise)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
