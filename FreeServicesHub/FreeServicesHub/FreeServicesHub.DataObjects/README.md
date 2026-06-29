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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A pure "data shapes" library shared by the server, the browser client, *and* (in spirit) the agent. It carries the fleet-monitor vocabulary: `Agent`, `AgentHeartbeat` (CPU/RAM/disk metrics), `DiskMetric`, `HubJob`, `ApiClientToken`, and the `SignalRUpdate` envelope used for hub↔client and hub↔agent messages. A `SensitiveAttribute` marks fields that must be kept out of logs.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Agent/heartbeat DTOs | The fleet data shapes | [FreeServicesHub.App.DataObjects.Agents.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataObjects/FreeServicesHub.App.DataObjects.Agents.cs) |
| Agent settings DTOs | Hub-pushed settings payloads | [FreeServicesHub.App.DataObjects.AgentSettings.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataObjects/FreeServicesHub.App.DataObjects.AgentSettings.cs) |

**Why does this exist?**
So the hub, the dashboard, and the tests all agree on exactly what an `Agent`, a `AgentHeartbeat`, and a `SignalRUpdate` look like — a mismatch is a build error, not a dropped heartbeat.

**What does it accomplish that other tools don't?**
- The whole telemetry contract is **shared, typed** — no loose JSON guessed at on either side.
- `SensitiveAttribute` keeps secrets out of logs by marking them on the shape itself.

**Terminology & "can I see it?"**
- **DTO** — a plain data shape with no behavior.
- **`SignalRUpdate`** — the message envelope for real-time pushes (settings report/updated, etc.).

**The hard part, drawn** — one telemetry contract, all participants:

```
  Agent ─┐                                              ┌─ Hub server
         ├── DataObjects: Agent · AgentHeartbeat ───────┤
  Dashboard ─┘  HubJob · ApiClientToken · SignalRUpdate └─ Tests
                  → everyone serializes the SAME shapes (no drift)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
