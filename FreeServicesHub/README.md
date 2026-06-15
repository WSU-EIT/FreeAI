# FreeServicesHub

Hub-and-agent example demonstrating SignalR, Blazor, and Windows services interacting in a complex multi-process environment. Includes the hub web app, the agent service, an installer, an Aspire AppHost, and integration tests.

## Projects in this solution

- `CRM`
- `CRM.Client`
- `CRM.DataAccess`
- `CRM.DataObjects`
- `CRM.EFModels`
- `CRM.Plugins`
- `Docs`
- `FreeServicesHub`
- `FreeServicesHub.Agent`
- `FreeServicesHub.Agent.Installer`
- `FreeServicesHub.AppHost`
- `FreeServicesHub.Client`
- `FreeServicesHub.DataAccess`
- `FreeServicesHub.DataObjects`
- `FreeServicesHub.EFModels`
- `FreeServicesHub.Plugins`
- `FreeServicesHub.TestMe`
- `FreeServicesHub.Tests.Integration`

See each project README.md for its specific role.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
FreeServicesHub is a **fleet monitor**. A small **agent** (a Windows Service) runs on each machine you want to watch; it registers with the central **hub** (a Blazor/SignalR web app), then every ~30 seconds collects a snapshot of that machine's health — CPU %, RAM, disk free/used — and pushes it over a live SignalR connection. The hub stores each heartbeat and shows the whole fleet's health on a real-time dashboard. If an agent's live connection drops, it falls back to plain HTTP and buffers snapshots locally until it reconnects.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Agent (Windows Service worker) | Collects & sends heartbeats | [AgentWorkerService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Agent/AgentWorkerService.cs) |
| SignalR hub | Receives heartbeats live | [Hubs/signalrHub.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/Hubs/signalrHub.cs) |
| Registration / heartbeat / jobs API | Agent ↔ hub over HTTP | [FreeServicesHub.App.API.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub/Controllers/FreeServicesHub.App.API.cs) |
| Heartbeat storage | Save each snapshot | [FreeServicesHub.App.DataAccess.Heartbeats.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub/FreeServicesHub.DataAccess/FreeServicesHub.App.DataAccess.Heartbeats.cs) |
| Aspire AppHost | Run hub + agent together for dev | [AppHost/Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.AppHost/Program.cs) |

**Why does this exist?**
To know, at a glance and in real time, whether a fleet of Windows machines/services is healthy — without logging into each box. It's also a worked example of the genuinely hard parts of distributed systems: service registration, token auth, live push, reconnection, and offline buffering.

**What does it accomplish that other tools don't?**
- **Live push, not polling** — SignalR streams each heartbeat the moment it's taken.
- **Resilient by design** — exponential-backoff reconnect, HTTP fallback when SignalR is down, and a local buffer (up to 100 snapshots) so nothing is lost across a blip.
- **Self-registering agents** — an agent enrolls with a pre-shared key and gets a token; restarts skip re-registration.
- **Two-way** — the hub can push settings changes (heartbeat interval, name) down to agents and queue read-only jobs (`Ping`, `CollectStats`).

**Terminology & "can I see it?"**
- **Agent / Hub** — the small service on each machine / the central server it reports to.
- **Heartbeat** — one periodic health snapshot (CPU/RAM/disk).
- **Backoff** — waiting progressively longer between reconnect attempts.
- *See it:* the `/AgentDashboard` page (the live fleet view).

**The hard part, drawn** — the resilient heartbeat loop:

```
  ┌──────────── monitored Windows machine ────────────┐
  │ FreeServicesHub.Agent (Windows Service)            │
  │  1. register  (POST /RegisterAgent) ──▶ Bearer token (saved to appsettings)
  │  2. connect SignalR /freeserviceshubHub (Bearer) → join "Agents"
  │  3. every ~30s: collect SystemSnapshot (CPU% · RAM · disks)
  └───────────────┬────────────────────────────────────┘
                  │ SendHeartbeat(snapshot)       ┌──────────── HUB (Blazor + SignalR) ────────────┐
                  ├───────────────────────────────▶ signalrHub → DataAccess.SaveHeartbeat → EF Core │
                  │ SignalR down?                  │        │                                       │
                  └─ fallback POST /SaveHeartbeat  │        ▼ live /AgentDashboard updates per box  │
                     (buffer up to 100 locally) ──▶└────────────────────────────────────────────────┘
                  ◀── hub pushes settings (interval / name) + queues Ping / CollectStats jobs
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
