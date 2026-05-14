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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
