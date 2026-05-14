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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
