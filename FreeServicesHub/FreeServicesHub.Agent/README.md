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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the **agent** — a .NET Worker Service that runs on each monitored machine (as a Windows Service, or a console app for debugging). On its timer it registers with the hub (once, caching a token), keeps a live SignalR connection open, and every ~30 seconds collects a `SystemSnapshot` (CPU %, RAM, disk) and sends it. It also polls for queued jobs and applies any settings the hub pushes down. If it can't talk to the hub, it buffers snapshots and keeps trying with exponential backoff.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| `BackgroundService` worker | The whole register → connect → heartbeat loop | [AgentWorkerService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent/AgentWorkerService.cs) |
| `Microsoft.AspNetCore.SignalR.Client` | Live connection to the hub | [AgentWorkerService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent/AgentWorkerService.cs) |
| Windows Services hosting | Run under the Windows Service Control Manager | [AgentWorkerService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent/AgentWorkerService.cs) |

**Why does this exist?**
So each machine reports its own health without anyone logging in — and keeps reporting reliably across network hiccups and restarts.

**What does it accomplish that other tools don't?**
- **Self-registration with token persistence** — enrolls once, then restarts skip re-registration.
- **Graceful degradation** — SignalR preferred; HTTP `SaveHeartbeat` fallback; up to 100 snapshots buffered locally so nothing is lost.
- **Remote-tunable** — accepts pushed settings (interval, name) and runs read-only jobs (`Ping`, `CollectStats`).
- **Standalone mode** — with no hub configured, it just logs snapshots locally.

**Terminology & "can I see it?"**
- **Worker Service** — a long-running background process (.NET's service host model).
- **`SystemSnapshot`** — the immutable record of one heartbeat (CPU/RAM/disk/uptime).
- **`.boot-status` file** — written after a successful connect so CI can detect a live agent.

**The hard part, drawn** — one heartbeat cycle with fallback:

```
  AgentWorkerService timer (every ~30s)
        │ first run? ─▶ POST /RegisterAgent ─▶ save Bearer token
        ▼
   ensure SignalR connected (else exponential backoff 2→4→8→…→30s)
        ▼
   collect SystemSnapshot (CPU% via Win32_Processor · RAM via GC · disks via DriveInfo)
        ▼
   SendHeartbeat(snapshot)  ──connected?──no──▶ POST /SaveHeartbeat  (buffer ≤100 if that fails too)
        ▼
   poll /api/agent/jobs  ·  apply any pushed AgentSettings
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
