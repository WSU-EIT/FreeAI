# Feature 105 — AgentMonitoring

> ADO-runner-style remote-host management: register agents, collect heartbeats, control Windows services / IIS app-pools / IIS sites from a Blazor dashboard.

## What this feature does
Exposes two layers from a single service. (1) A lightweight monitoring API: `RegisterAgent`, `RecordHeartbeat` (CPU/mem/disk), and an alert-rule evaluator. (2) A full management API: managed agents, Windows services control (Start/Stop/Restart/Uninstall), IIS app-pool and site control (Recycle/Start/Stop), registration keys, command history, and a dashboard summary. `SeedSampleAgents` populates a 5-host demo dataset. Commands are dispatched to a connected `FreeBlazorExtended.Agent` worker over SignalR via `AgentHub`; if no worker is currently connected the service falls back to the in-memory simulation so the showcase keeps working.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `AgentMonitoringService.cs` | Monitoring + management service (largest in the suite) | 891 |
| `AgentMonitoring.cs` | `Agent`, `AgentHeartbeat`, `AgentService`, `AgentIisAppPool`, `AgentIisSite`, `AgentIisBinding`, `AgentRegistrationKey`, `AgentCommand`, `AgentDashboardSummary`, status/state enums, `BooleanResponse` | 416 |
| `AgentHub.cs` | SignalR hub that bridges remote `FreeBlazorExtended.Agent` workers to the service | 158 |

## Dependencies
- **NuGet packages:** `System.Text.Json` (BCL). For real remote execution: `Microsoft.AspNetCore.SignalR` plus the sibling `FreeBlazorExtended.Agent` Worker Service.
- **Cross-feature dependencies:** none; uses `Foundation/Helpers.cs` and `Foundation/DataObjects.cs`
- **SignalR:** required for real deployment (currently missing — see Known gaps)
- **EF Core:** not used (in-memory `ConcurrentDictionary` collections)

## DI registration
Add this to your `Program.cs`:
```csharp
builder.Services.AddScoped<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();

// Map the SignalR hub workers connect to.
app.MapHub<FreeBlazorExtended.AgentMonitoring.AgentHub>("/agentHub");
```
(For Blazor WASM client also add a Singleton variant — see `FreeBlazorExample/FreeBlazorExample.Client/Program.cs` line 32 for the pattern.)

## Deploying the FreeBlazorExtended.Agent worker
1. Build `FreeBlazorExtended.Agent.csproj` and copy the publish output to each Windows host you want to manage.
2. (One-time) Generate a registration key from the showcase dashboard: `/showcase/feature105-agent-monitoring` → "Generate Key". Each key is single-use; the hub binds the agent's first connection to it and refuses subsequent unbound use.
3. Edit `appsettings.json` next to the agent EXE:
   ```json
   {
     "Agent": {
       "HubUrl": "https://your-host:7271",
       "RegistrationKey": "PASTE-KEY-FROM-DASHBOARD",
       "ApiClientToken": "",
       "HeartbeatIntervalSeconds": 30,
       "AgentName": ""
     }
   }
   ```
   `AgentName` defaults to `Environment.MachineName` if blank. The agent appends `/agentHub` to `HubUrl` automatically.
4. Install as a Windows Service: `sc.exe create FreeBlazorExtendedAgent binPath= "C:\Program Files\FreeBlazorExtended.Agent\FreeBlazorExtended.Agent.exe"` then `sc.exe start FreeBlazorExtendedAgent`. Or run the EXE directly for debugging.
5. Verify on the dashboard: a new managed-agent row should appear with `Status = Online` within `HeartbeatIntervalSeconds`.

### What the worker does today
On startup the worker connects to `AgentHub`, calls `RegisterAgent(registrationKey, info)` to receive its `AgentId`, subscribes to `ExecuteServiceCommand` and `ExecuteAppPoolCommand` events, and starts pushing heartbeats. **Actual** Windows Service / IIS command execution is currently stubbed: the agent logs `"Would execute X"` and immediately reports success via `ReportCommandResult`. The real `ServiceController` / `Microsoft.Web.Administration` calls are wired in the same file (`StartWindowsService`, `StopWindowsService`, ...) and ready to swap in — flagged for follow-up.

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/AgentMonitoring/` folder into your project.
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present.
3. Add the DI registration above to server `Program.cs` (line 33 in the example) and the Singleton variant to WASM client `Program.cs` (line 32).
4. (Optional) Copy the sibling `FreeBlazorExtended.Agent/` Worker Service project (located at `AllOfDanielsProjects/FreeBlazorExtended.Agent/`, outside this folder) if you want the real-world host-side runner.
5. EF Core migration not applicable today.

## Showcase
The interactive demo lives at `/showcase/feature105-agent-monitoring` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature105_AgentMonitoring.razor`

## Status
- Implementation: **REAL** (in-memory) for the API surface; SignalR transport via `AgentHub` is wired and routes commands to the connected `FreeBlazorExtended.Agent` worker; the worker logs intent only — actual `ServiceController` / `Microsoft.Web.Administration` shell-out is the next step.
- Persistence: in-memory only — needs EF migration before production use
- Known gaps:
  - **Worker stubs Windows Service / IIS execution.** `AgentWorkerService.HandleServiceCommand` and `HandleAppPoolCommand` log `"Would execute X"` and immediately report success. Swap in the existing `StartWindowsService` / `StopWindowsService` / `RestartWindowsService` / `UninstallWindowsService` helpers (already implemented on the same class) and the equivalent `Microsoft.Web.Administration.ServerManager` calls to make commands actually do work.
  - No long-lived bearer token issued at registration: `AgentApiToken` exists as a model but the hub doesn't yet hand one back, so reconnection currently relies on the registration key being preserved.
  - Persistence is still in-memory; an EF migration is needed before production.

## Effort to integrate
**L** — service is large (712 LoC) and a production deployment requires the sibling Worker Service, a SignalR hub, auth via registration keys, and EF persistence for command history.
