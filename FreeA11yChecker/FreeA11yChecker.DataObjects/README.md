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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
