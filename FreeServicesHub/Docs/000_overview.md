# FreeServicesHub -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeServicesHub is a hub-and-agent system for real-time Windows server fleet monitoring. A Blazor/SignalR web application (the hub) receives heartbeats from one or more Windows agent services, displaying live CPU, RAM, disk, and uptime telemetry. Agents register with the hub via JWT, maintain a persistent SignalR connection, and can be managed (settings updated, jobs dispatched) from the web UI.

The solution also includes an .NET Aspire AppHost for local development that starts both the hub and an agent together, wiring their ports and environment variables automatically.

## Why it exists

WSU-EIT needed a lightweight way to monitor Windows servers running FreeCRM-based applications -- CPU spikes, disk fill-up, service status -- without deploying a heavyweight monitoring stack. FreeServicesHub provides that visibility with a single `dotnet run` on the AppHost.

## Who it is for

- IT operations staff who need a live dashboard of server health
- Developers who want to see how SignalR hub-and-agent patterns work in .NET 10
- WSU-EIT teams running FreeCRM applications on Windows servers

## Quick start

```bash
cd FreeServicesHub/FreeServicesHub.AppHost
dotnet run
```

The Aspire dashboard shows both processes. Navigate to `http://localhost:5105` for the hub UI.

Or run hub and agent separately:

```bash
# Hub
cd FreeServicesHub/FreeServicesHub/FreeServicesHub
dotnet run

# Agent (in a separate terminal)
cd FreeServicesHub/FreeServicesHub.Agent
dotnet run
```

## Related projects

- [FreeBlazorExtended](../FreeBlazorExtended/README.md) -- also has an agent component for IIS/Windows Service control

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*