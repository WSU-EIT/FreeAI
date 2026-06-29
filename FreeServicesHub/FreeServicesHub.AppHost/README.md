# FreeServicesHub.AppHost

The .NET Aspire orchestration host for local development of the FreeServicesHub solution. Starts the hub web server and one agent worker together, wires their ports and environment variables, and ensures the agent waits for the hub to be ready before connecting.

## What it does

`Program.cs` creates a `DistributedApplication` with two resources:

- **hub** (`FreeServicesHub`) — the Blazor/SignalR web server, pinned to HTTPS port 7271 and HTTP port 5111, with `DatabaseType=InMemory` so no database setup is required for development.
- **agent** (`FreeServicesHub.Agent`) — the Windows worker service, given the hub URL and a dev registration key via environment variables (`Agent__HubUrl`, `Agent__RegistrationKey`). The agent calls `.WaitFor(hub)` so it does not start until the hub is accepting connections.

Running `dotnet run` from this project launches both processes under the Aspire dashboard, which displays live logs and resource health for each process.

## SDK note

This project uses **both** `Sdk="Microsoft.NET.Sdk"` (outer) **and** `<Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0"/>` (inner). The Aspire SDK injects the orchestration infrastructure; the standard SDK provides the executable entry point.

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` + `Aspire.AppHost.Sdk 9.2.0` |
| Output type | Executable (orchestrator process) |
| Target framework | net10.0 |
| Role | Local-dev orchestrator — not deployed to production |

## Key public classes

| Class | Purpose |
|---|---|
| `Program` (top-level statements) | Builds the `DistributedApplication`, registers `hub` and `agent` resources, runs the Aspire host |

## Projects orchestrated

| Project | Port(s) | Notes |
|---|---|---|
| `FreeServicesHub` | HTTPS 7271, HTTP 5111 | Blazor + SignalR hub server |
| `FreeServicesHub.Agent` | N/A | Windows worker; waits for hub, then registers and connects |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub` | Hub web application |
| `FreeServicesHub.Agent` | Agent worker service |

## Notable NuGet packages

| Package | Version | Purpose |
|---|---|---|
| `Aspire.Hosting.AppHost` | 9.2.0 | Core Aspire orchestration primitives |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This is the **.NET Aspire orchestrator** for local development. With one `dotnet run` it starts *both* the hub web server and an agent worker together, wires their ports and environment variables, and makes the agent wait until the hub is ready before connecting — all shown live in the Aspire dashboard.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| .NET Aspire `DistributedApplication` | Declares & runs the hub + agent together | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.AppHost/Program.cs) |
| `.WaitFor(hub)` dependency | Agent starts only once the hub is up | [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.AppHost/Program.cs) |

**Why does this exist?**
A hub-and-agent system is two processes that must start in the right order with matching URLs/keys. Aspire makes "run the whole thing locally" a single command, with a dashboard showing both processes' logs and health.

**What does it accomplish that other tools don't?**
- **One-command multi-process dev**: no manually launching the hub, then the agent, then fixing ports.
- **Ordered startup** via `.WaitFor(hub)` so the agent doesn't fail trying to connect to a hub that isn't listening yet.
- Runs with `DatabaseType=InMemory` so there's **no database setup** for development.

**Terminology & "can I see it?"**
- **.NET Aspire** — a framework for wiring up and running multi-service .NET apps locally.
- **AppHost** — the orchestrator project (not deployed to production).

**The hard part, drawn** — one command boots the whole system in order:

```
  dotnet run (AppHost)
        ├─▶ start  hub  (FreeServicesHub)  HTTPS 7271 / HTTP 5111 · DatabaseType=InMemory
        └─▶ start  agent (FreeServicesHub.Agent)  ──.WaitFor(hub)──▶ given Agent__HubUrl + RegistrationKey
                        (agent connects only AFTER the hub is accepting connections)
        ▼  Aspire dashboard shows live logs + health for both processes
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
