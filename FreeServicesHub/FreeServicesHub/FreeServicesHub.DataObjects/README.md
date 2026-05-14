# FreeServicesHub.DataObjects

Shared data transfer objects (DTOs), enums, and contracts referenced by both the server (`FreeServicesHub`, `FreeServicesHub.DataAccess`) and the WebAssembly client (`FreeServicesHub.Client`). Contains no database or UI logic — only plain C# partial classes.

## What it contains

`DataObjects` is a single partial class spread across multiple files:

| File | Content |
|---|---|
| `DataObjects.cs` | Core types: `BooleanResponse`, `ActiveUser`, `ApplicationSettings`, `SettingType`, `DeletePreference`, `UserLookupType` enums |
| `FreeServicesHub.App.DataObjects.Agents.cs` | `Agent`, `AgentHeartbeat`, `DiskMetric`, `AgentServiceInfo`, `AgentSettingsUpdate`, `GetAgentsRequest` |
| `FreeServicesHub.App.DataObjects.ApiKeys.cs` | `ApiKey`, `ApiClientToken` |
| `FreeServicesHub.App.DataObjects.Jobs.cs` | `HubJob`, `HubJobStatus` |
| `FreeServicesHub.App.DataObjects.BackgroundServiceLogs.cs` | Background task log entries |
| `DataObjects.SignalR.cs` | `SignalRUpdate`, `SignalRUpdateType` enum (agent settings report, settings updated, etc.) |
| `DataObjects.Departments.cs` | `Department`, `DepartmentGroup` |
| `DataObjects.Tags.cs` | `Tag`, `TagItem` |
| `DataObjects.UserGroups.cs` | `UserGroup`, `UserInGroup` |
| `DataObjects.Services.cs` | `ServiceStatus`, `ServiceRecord` |
| `ConfigurationHelper.cs` | `ConfigurationHelperLoader`, `ConfigurationHelperConnectionStrings` |
| `Caching.cs` | In-memory cache helper |
| `GlobalSettings.cs` | Application-wide global settings |

The `SensitiveAttribute` marks properties that should be excluded from logs and diagnostics.

## Key public classes

| Class | Purpose |
|---|---|
| `DataObjects` (partial) | Root container for all shared types |
| `Agent` | Registered agent record: hostname, OS, status, last heartbeat |
| `AgentHeartbeat` | Time-series snapshot: CPU %, RAM GB, disk metrics JSON, service info JSON |
| `SignalRUpdate` | Message envelope for real-time hub-to-client and hub-to-agent messages |
| `HubJob` | Queued job dispatched to an agent (type, payload, status, result) |
| `BooleanResponse` | Standard success/failure response with messages |
| `ConfigurationHelper` | Configuration values surfaced from server to client |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` — class library |
| Target framework | net10.0 |
| Referenced by | Server, WebAssembly client, integration tests |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub.Plugins` | `Plugin` type used in data objects |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `System.Runtime.Caching` | In-memory object cache used by `Caching.cs` |

Part of the **FreeServicesHub** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
