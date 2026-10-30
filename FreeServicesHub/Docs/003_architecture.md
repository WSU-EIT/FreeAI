# FreeServicesHub -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | Role |
|---------|------|
| `FreeServicesHub` (web) | Blazor/SignalR hub server; registration API, telemetry ingestion, web UI |
| `FreeServicesHub.Client` | Blazor WASM; live dashboard, agent management |
| `FreeServicesHub.Agent` | Windows Worker Service; heartbeat loop, SignalR client, job executor |
| `FreeServicesHub.Agent.Installer` | Agent Windows Service installer |
| `FreeServicesHub.AppHost` | .NET Aspire dev orchestrator -- starts hub + agent together |
| `FreeServicesHub.DataAccess` | Business logic; EF Core, JWT auth, telemetry storage |
| `FreeServicesHub.DataObjects` | Shared DTOs; `SystemSnapshot`, `AgentOptions`, job models |
| `FreeServicesHub.EFModels` | EF Core DbContext; agents, heartbeats, jobs |
| `FreeServicesHub.Plugins` | Roslyn dynamic plugin runtime |
| `CRM.*` | FreeCRM scaffold re-used as the hub''s base application layer |
| `FreeServicesHub.Tests.Integration` | Integration test suite |

## Heartbeat data flow

```
Agent (Windows Service)
  -> every 30s: collect SystemSnapshot (CPU/RAM/disk)
  -> if SignalR connected: hub.SendHeartbeat(snapshot)
  -> if disconnected:      POST /api/Data/SaveHeartbeat + local buffer
  -> after heartbeat:      POST /api/agent/jobs (poll for queued jobs)

Hub
  -> receives heartbeat via SignalR or REST
  -> stores to DB (EF Core)
  -> pushes SignalRUpdate to browser clients
  -> browser dashboard re-renders live
```

## SignalR message types

| Message | Direction | Purpose |
|---------|-----------|---------|
| `SendHeartbeat` | Agent -> Hub | CPU/RAM/disk snapshot |
| `SignalRUpdate` | Hub -> Browser | Trigger dashboard refresh |
| `AgentSettingsReport` | Hub -> Agent | Current settings |
| `AgentSettingsUpdated` | Hub -> Agent | Push new settings |

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*