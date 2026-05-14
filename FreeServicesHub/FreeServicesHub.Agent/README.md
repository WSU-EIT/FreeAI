# FreeServicesHub.Agent

A .NET 10 Worker Service that registers with a FreeServicesHub server, maintains a persistent SignalR connection, and sends periodic heartbeats carrying live system telemetry. Runs as a Windows Service (`sc.exe`) or as a console process for debugging.

## What the background work actually does

`AgentWorkerService` (a `BackgroundService`) performs the following work on every heartbeat cycle (default 30 seconds):

1. **Registration** — On first start, POSTs to `/api/Data/RegisterAgent` with the machine hostname, OS details, and a pre-shared registration key. The hub returns a `ApiClientToken` (Bearer JWT) which is persisted back to `appsettings.json` so subsequent restarts skip re-registration.
2. **SignalR connection** — Connects to `/freeserviceshubHub` using the Bearer token, joins the `Agents` group, and subscribes to `SignalRUpdate` messages. Reconnects with exponential backoff (2 s → 4 s → 8 s → capped at 30 s) on disconnection.
3. **Heartbeat** — Collects a `SystemSnapshot` (CPU % via PowerShell `Win32_Processor.LoadPercentage`, RAM via GC memory info, fixed-drive sizes via `DriveInfo`) and invokes `SendHeartbeat` on the hub. If SignalR is disconnected, falls back to HTTP POST to `/api/Data/SaveHeartbeat` and buffers up to 100 snapshots locally.
4. **Job polling** — After each heartbeat, POSTs to `/api/agent/jobs` to retrieve queued jobs. Supports two read-only job types: `CollectStats` (returns a full snapshot JSON) and `Ping` (returns `pong` with timestamp).
5. **Settings sync** — Listens for `AgentSettingsReport`/`AgentSettingsUpdated` messages from the hub; applies updated `HubUrl`, `HeartbeatIntervalSeconds`, or `AgentName` and persists changes to `appsettings.json`.
6. **Standalone mode** — If no hub credentials are configured, runs indefinitely collecting snapshots and printing them to the console/log file.

A `.boot-status` file is written to the exe directory after successful registration and SignalR connect, letting CI/CD pipelines detect a live agent.

## Key public classes

| Class | Purpose |
|---|---|
| `AgentWorkerService` | Main `BackgroundService`; registration, SignalR, heartbeat loop, job execution |
| `AgentOptions` | Configuration bound from `appsettings.json "Agent"` section |
| `SystemSnapshot` | Immutable record of CPU, RAM, disk, and uptime for one heartbeat |
| `DriveSnapshot` | Per-drive free/used/total GB |
| `WindowsServiceSnapshot` | Windows SCM metadata (status, startup type, log-on account) |
| `ExponentialBackoffRetryPolicy` | `IRetryPolicy` with 2-4-8-16-30s caps for SignalR reconnects |
| `AgentSettingsUpdatePayload` | Deserialization target for hub-pushed settings updates |
| `Program` (static class) | Exposes `IHostApplicationLifetime` for the SignalR shutdown handler |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk.Worker` — produces a Windows service host executable |
| Target framework | net10.0 |
| Service name | `FreeServicesHubAgent` (registered with Windows SCM) |
| Log file | `agent.log` in the exe directory |

## Project references

None (standalone agent — deliberately has no reference to server assemblies).

## Notable NuGet packages

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.SignalR.Client` | 10.0.0-preview.3 | SignalR hub connection |
| `Microsoft.Extensions.Hosting.WindowsServices` | 10.0.0-preview.3 | Windows Service lifetime integration |
| `System.ServiceProcess.ServiceController` | 10.0.0-preview.3 | Query Windows SCM for service metadata |

Part of the **FreeServicesHub** solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
