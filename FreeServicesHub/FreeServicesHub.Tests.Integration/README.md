# FreeServicesHub.Tests.Integration

xUnit integration tests that start the hub server in-process (using `WebApplicationFactory<Program>` with an InMemory database) and exercise the full HTTP and SignalR API end-to-end, including agent registration, heartbeat saves, job CRUD, SignalR messaging, authentication, multi-tenant isolation, and dashboard data flows.

## What the tests cover

| File | Test class | What it tests |
|---|---|---|
| `HeartbeatTests.cs` | `HeartbeatTests` | Register an agent, POST a heartbeat with a valid Bearer token, verify the heartbeat is persisted |
| `RegistrationTests.cs` | `RegistrationTests` | Agent self-registration with a seeded key; duplicate/expired key rejection |
| `AuthTests.cs` | `AuthTests` | Unauthenticated requests return 401; authenticated requests succeed |
| `SignalRTests.cs` | `SignalRTests` | Connect to `/freeserviceshubHub` with a token, join the `Agents` group, send a heartbeat via SignalR |
| `HeartbeatTests.cs` | `JobCrudTests` | Create, retrieve, update job status via the REST API |
| `JobCrudTests.cs` | `JobCrudTests` | Full job lifecycle: queue → assign → complete |
| `DashboardE2ETests.cs` | `DashboardE2ETests` | Fetch agent list and heartbeat history as a logged-in user |
| `ManagementE2ETests.cs` | `ManagementE2ETests` | Agent management CRUD (rename, delete, restore) |
| `TenantIsolationTests.cs` | `TenantIsolationTests` | Agents from tenant A cannot see data from tenant B |
| `HubFixture.cs` | `HubFixture` | Shared fixture: starts server, seeds the test registration key, exposes `HttpClient` and `Services` |

## Key public classes

| Class | Purpose |
|---|---|
| `HubFixture` | `IAsyncLifetime` xUnit fixture; starts hub in-process with `WebApplicationFactory`, seeds registration key |

## Build details

| Property | Value |
|---|---|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | net10.0 |
| Test runner | xUnit 2.9.3 / xunit.runner.visualstudio 3.1.0 |
| `IsTestProject` | `true` |

## Project references

| Project | Role |
|---|---|
| `FreeServicesHub` | Hub server under test |
| `FreeServicesHub.DataObjects` | Shared DTOs for request/response construction |

## Notable NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Mvc.Testing` | In-process `WebApplicationFactory` |
| `Microsoft.AspNetCore.SignalR.Client` | SignalR client for hub message tests |
| `xunit` | Test framework |
| `Microsoft.NET.Test.Sdk` | dotnet test runner support |

Part of the **FreeServicesHub** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
xUnit integration tests that boot the **whole hub in-process** (`WebApplicationFactory<Program>` with an InMemory database) and exercise the real HTTP and SignalR API end-to-end: register an agent, send a heartbeat with a Bearer token, verify it's stored; connect to the SignalR hub and send a heartbeat that way; run the full job lifecycle; and confirm tenants can't see each other's data.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| In-process hub (`WebApplicationFactory`) | Run the real server in a test | [HeartbeatTests.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Tests.Integration/HeartbeatTests.cs) |
| SignalR client tests | Exercise the live hub channel | [SignalRTests.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeServicesHub/FreeServicesHub.Tests.Integration/SignalRTests.cs) |

**Why does this exist?**
To prove the agent↔hub contract actually works through the real HTTP/SignalR stack — registration, auth, heartbeat persistence, jobs, tenant isolation — not just unit-tested in isolation.

**What does it accomplish that other tools don't?**
- **End-to-end** coverage of both transports (REST *and* SignalR) against the real server, with an InMemory DB so no setup is needed.
- Explicit **tenant-isolation** and **auth (401)** tests — the security-critical paths get their own checks.

**Terminology & "can I see it?"**
- **Integration test** — exercises multiple layers together (vs. one unit in isolation).
- **`WebApplicationFactory`** — spins the ASP.NET app up inside the test process.

**The hard part, drawn** — the real stack under test:

```
  xUnit test ─▶ HubFixture starts the hub in-process (WebApplicationFactory + InMemory DB, seeded key)
        ├─ HTTP: register agent ─▶ POST heartbeat (Bearer) ─▶ assert persisted
        ├─ SignalR: connect /freeserviceshubHub ─▶ join "Agents" ─▶ SendHeartbeat ─▶ assert
        └─ assert: unauth ⇒ 401  ·  tenant A can't see tenant B  ·  job queue→assign→complete
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
