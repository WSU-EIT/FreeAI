# FreeBlazorExtended Audit
*Generated: 2026-04-30*

## Summary
- Total `FreeBlazorExtended.*` namespaces declared: **9** (excluding generated `obj/` files and `.razor` `@namespace` directives)
  - `FreeBlazorExtended` (Helpers, DataObjects)
  - `FreeBlazorExtended.Foundation`
  - `FreeBlazorExtended.Foundation.Services`
  - `FreeBlazorExtended.DynamicForms`
  - `FreeBlazorExtended.MultiViewSync`
  - `FreeBlazorExtended.Calendar`
  - `FreeBlazorExtended.UserPreferences`
  - `FreeBlazorExtended.AgentMonitoring`
  - `FreeBlazorExtended.HierarchicalTree`
- Real implementations: **6 / 6** features (all have working in-memory logic, none throw `NotImplementedException`)
- Stub implementations: **0 / 6** (caveat: every service uses `ConcurrentDictionary` in-memory storage and no EF/database persistence — see "Stub features" section)
- Owning project(s): **`FreeBlazorExtended.csproj`** at `c:\Users\pepkad\source\repos\DanielPepka\AllOfDanielsProjects\FreeBlazorExtended\FreeBlazorExtended.csproj` (a separate Razor class library that the FreeBlazorExample.Client project references via `ProjectReference`). The code does **not** live inside `FreeBlazorExample.Client` or `FreeBlazorExample` (server) projects.

> **Architectural note.** The orchestrator's brief mentions auditing the FreeBlazorExtended namespace situation "in the FreeBlazorExample project". In reality the Showcase pages live in `FreeBlazorExample.Client` but the FreeBlazorExtended types (services + models + Razor components) live in a *sibling* class library `FreeBlazorExtended\FreeBlazorExtended.csproj`. FreeBlazorExample.Client adds a `<ProjectReference Include="..\..\FreeBlazorExtended\FreeBlazorExtended.csproj" />` (line 37 of `CRM.Client.csproj`). All DI registrations live in the two FreeBlazorExample `Program.cs` files.

## Per-feature breakdown

### Feature 101 — DynamicForms
- **Namespace:** `FreeBlazorExtended.DynamicForms`
- **Owning project:** `FreeBlazorExtended` (Razor class library)
- **Service file:** `FreeBlazorExtended/DynamicForms/FormService.cs` — **REAL**, 202 lines. In-memory `ConcurrentDictionary<Guid, FormDefinition>` with full CRUD, JSON-driven validation rules (min/max length, regex), conditional-visibility evaluator, soft-delete via `Deleted` flag.
- **Models / supporting files:**
  - `FreeBlazorExtended/DynamicForms/FormDefinition.cs` (69 lines — `FormDefinition`, `FormField`, `FormSubmission`)
  - `FreeBlazorExtended/DynamicForms/Components/FormBuilder.razor`
  - `FreeBlazorExtended/DynamicForms/Components/DynamicFormRenderer.razor`
  - `FreeBlazorExtended/DynamicForms/Components/ConditionalField.razor`
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:29` — `builder.Services.AddScoped<FreeBlazorExtended.DynamicForms.FormService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:28` — `builder.Services.AddSingleton<FreeBlazorExtended.DynamicForms.FormService>();`
- **SignalR hub:** none
- **EF Core entities:** none — purely in-memory; no `DbSet<FormDefinition>` exists in `FreeBlazorExample.EFModels/EFModels/EFDataModel.cs`
- **Files to copy if cherry-picking:**
  - `DynamicForms/FormService.cs`
  - `DynamicForms/FormDefinition.cs`
  - `DynamicForms/Components/*.razor` (3 files)
  - DI registration line in target `Program.cs`

### Feature 102 — MultiViewSync
- **Namespace:** `FreeBlazorExtended.MultiViewSync`
- **Owning project:** `FreeBlazorExtended`
- **Service file:** `FreeBlazorExtended/MultiViewSync/RealtimeSyncService.cs` — **REAL**, 134 lines. Manages `PresentationSession` records (active item, blanked screen, hidden text, ordered items list) plus a `_clientConnections` dictionary that tracks SignalR connection IDs per session. **Caveat:** the service exposes `RegisterClient` / `UnregisterClient` / `GetActiveClientCount` but there is no actual `Hub<>` class in either `FreeBlazorExtended` or `FreeBlazorExample` for "MultiViewSync" — only the existing app-wide `crmHub` exists. The showcase page is single-process and does **not** call `HubConnectionBuilder`.
- **Models / supporting files:**
  - `FreeBlazorExtended/MultiViewSync/PresentationSession.cs` (37 lines — `PresentationSession`, `SessionItem`)
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:32` — `builder.Services.AddScoped<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:31` — `builder.Services.AddSingleton<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();`
- **SignalR hub:** **MISSING**. The only hub in the solution is `FreeBlazorExample/FreeBlazorExample/Hubs/signalrHub.cs` (`crmHub : Hub<IsrHub>`, 39 lines, mapped at `/crmHub` in `Program.cs:248`). It is the generic CRM tenant-broadcast hub, not feature-102 specific. A `PresentationHub` would need to be authored.
- **EF Core entities:** none
- **Files to copy if cherry-picking:**
  - `MultiViewSync/RealtimeSyncService.cs`
  - `MultiViewSync/PresentationSession.cs`
  - **Plus to make it actually realtime**: a new SignalR hub class + client wiring (currently absent)

### Feature 103 — Calendar
- **Namespace:** `FreeBlazorExtended.Calendar`
- **Owning project:** `FreeBlazorExtended`
- **Service file:** `FreeBlazorExtended/Calendar/CalendarEventService.cs` — **REAL**, 143 lines. Has range queries, resource-conflict detection, recurrence rule expansion (daily/weekly/monthly/yearly with `Until` and `Count` limits), soft-delete.
- **Models / supporting files:**
  - `FreeBlazorExtended/Calendar/CalendarEvent.cs` (85 lines — `CalendarEvent`, `RecurrenceRule`, `RecurrenceFrequency`, `EventStatus`)
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:31` — `builder.Services.AddScoped<FreeBlazorExtended.Calendar.CalendarEventService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:30` — `builder.Services.AddSingleton<FreeBlazorExtended.Calendar.CalendarEventService>();`
- **SignalR hub:** none
- **EF Core entities:** none
- **Files to copy if cherry-picking:**
  - `Calendar/CalendarEventService.cs`
  - `Calendar/CalendarEvent.cs`
  - DI line

### Feature 104 — UserPreferences
- **Namespace:** `FreeBlazorExtended.UserPreferences`
- **Owning project:** `FreeBlazorExtended`
- **Service file:** `FreeBlazorExtended/UserPreferences/UserPreferencesService.cs` — **REAL**, 106 lines. Per-(tenant, user) preferences with JSON-blob "per-entity preference" map (typed via generic `GetPerEntityPreference<T>`) and a recent-items list capped at 20.
- **Models / supporting files:**
  - `FreeBlazorExtended/UserPreferences/UserPreferences.cs` (37 lines — `UserPreferences` POCO with `PerEntityJson`, `RecentItemsJson`, theme/density fields)
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:30` — `builder.Services.AddScoped<FreeBlazorExtended.UserPreferences.UserPreferencesService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:29` — `builder.Services.AddSingleton<FreeBlazorExtended.UserPreferences.UserPreferencesService>();`
- **SignalR hub:** none
- **EF Core entities:** none
- **Files to copy if cherry-picking:**
  - `UserPreferences/UserPreferencesService.cs`
  - `UserPreferences/UserPreferences.cs`
  - DI line

### Feature 105 — AgentMonitoring
- **Namespace:** `FreeBlazorExtended.AgentMonitoring`
- **Owning project:** `FreeBlazorExtended`
- **Service file:** `FreeBlazorExtended/AgentMonitoring/AgentMonitoringService.cs` — **REAL**, 712 lines (largest of the six). Two layers:
  1. *Lightweight monitoring API* — `RegisterAgent`, `RecordHeartbeat` (CPU/mem/disk), alert-rule evaluator.
  2. *Full ADO-runner-style management API* — managed agents, Windows services control (Start/Stop/Restart/Uninstall), IIS app-pool/site control (Recycle/Start/Stop), registration keys, command history, dashboard summary, plus a 5-host seeded sample dataset (`SeedSampleAgents`).
  Service simulates remote execution by mutating in-memory state. Companion class `FreeBlazorExtended.Agent` (separate Worker Service project at `FreeBlazorExtended.Agent/`) is the would-be real-world execution endpoint but is not wired up to this in-process service.
- **Models / supporting files:**
  - `FreeBlazorExtended/AgentMonitoring/AgentMonitoring.cs` (416 lines — `Agent`, `AgentHeartbeat`, `AgentService`, `AgentIisAppPool`, `AgentIisSite`, `AgentIisBinding`, `AgentRegistrationKey`, `AgentCommand`, `AgentDashboardSummary`, `AgentStatuses`, `AgentCommandStatus`, `AgentCommandType`, `ServiceState`, `ServiceStartupType`, `AppPoolState`, `BooleanResponse`)
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:33` — `builder.Services.AddScoped<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:32` — `builder.Services.AddSingleton<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();`
- **SignalR hub:** none in this project. (A real deployment would presumably push commands via a hub to the FreeBlazorExtended.Agent worker; that wiring is absent.)
- **EF Core entities:** none
- **Files to copy if cherry-picking:**
  - `AgentMonitoring/AgentMonitoringService.cs`
  - `AgentMonitoring/AgentMonitoring.cs`
  - DI line
  - Optional: the entire `FreeBlazorExtended.Agent` project if you want the matching Windows-service runner

### Feature 107 — HierarchicalTree
- **Namespace:** `FreeBlazorExtended.HierarchicalTree`
- **Owning project:** `FreeBlazorExtended`
- **Service file:** `FreeBlazorExtended/HierarchicalTree/TreeService.cs` — **REAL**, 147 lines. Adjacency-list tree with `MoveTreeNode` (cycle/self-move detection via `IsDescendant`), sibling reorder, BFS `GetDescendants`, `GetNodePath`.
- **Models / supporting files:**
  - `FreeBlazorExtended/HierarchicalTree/TreeNode.cs` (30 lines — `TreeNode` POCO with `ParentNodeId`, `SortOrder`)
  - `FreeBlazorExtended/HierarchicalTree/Components/TreeNodeComponent.razor`
  - `FreeBlazorExtended/HierarchicalTree/Components/HierarchicalTree.razor`
- **DI registration:**
  - `FreeBlazorExample/FreeBlazorExample/Program.cs:34` — `builder.Services.AddScoped<FreeBlazorExtended.HierarchicalTree.TreeService>();`
  - `FreeBlazorExample/FreeBlazorExample.Client/Program.cs:33` — `builder.Services.AddSingleton<FreeBlazorExtended.HierarchicalTree.TreeService>();`
- **SignalR hub:** none
- **EF Core entities:** none
- **Files to copy if cherry-picking:**
  - `HierarchicalTree/TreeService.cs`
  - `HierarchicalTree/TreeNode.cs`
  - `HierarchicalTree/Components/*.razor` (2 files)
  - DI line

## Cherry-pick cheat sheet

| Feature | Files | Dependencies | Effort (S/M/L) |
|---|---|---|---|
| 101 Dynamic Forms | 2 cs + 3 razor + DI | None beyond `System.Text.Json`, `System.Text.RegularExpressions` | S |
| 102 MultiViewSync | 2 cs + DI; **plus a new Hub** | `Microsoft.AspNetCore.SignalR` server-side; client `HubConnection` wiring; auth | M |
| 103 Calendar | 2 cs + DI | `System.Text.Json` | S |
| 104 UserPreferences | 2 cs + DI | `System.Text.Json` | S |
| 105 AgentMonitoring | 2 cs + DI; optional `FreeBlazorExtended.Agent` worker | `System.Text.Json`. Real production wiring would need a SignalR hub + the worker service + auth/registration-key validation | L |
| 107 HierarchicalTree | 2 cs + 2 razor + DI | None | S |

## Stub features (need real implementation before merge)

None of the six services are stubs in the strict sense (no `NotImplementedException`, no empty methods). However, **all six rely on `ConcurrentDictionary` in-memory storage** with no persistence layer, which means before merging into the base FreeBlazor library a deployment-ready implementation would need:

- **All six** — EF Core entities + `DbSet<>` additions in `FreeBlazorExample.EFModels/EFModels/EFDataModel.cs`, repository pattern, migration. Currently `EFDataModel.cs` only contains the original CRM tables (Departments, Settings, Tags, Tenants, UDFLabels, Users, UserGroups, UserInGroup, FileStorage stub, PluginCache).
- **Feature 102 (MultiViewSync)** — additionally requires an actual SignalR `Hub<>` (e.g. `PresentationHub`) plus client `HubConnection` wiring; today the service tracks connection IDs but nothing pushes events.
- **Feature 105 (AgentMonitoring)** — additionally requires the `FreeBlazorExtended.Agent` Worker Service to be wired up over a hub/transport; today `ExecuteServiceCommand` / `ExecuteAppPoolCommand` mutate local in-memory state and return success without actually contacting any remote machine.

## Orphaned references (namespace used but no file declares it)

None detected. Every `FreeBlazorExtended.*` reference in `FreeBlazorExample` resolves to a declared namespace in the `FreeBlazorExtended` class library. Notable supporting namespaces (also resolved):

- `FreeBlazorExtended.Foundation.Services` → `FreeBlazorExtended/Foundation/Services/NotificationService.cs` (registered in both Program.cs files at lines 28 server / 27 client — note: this is a 7th DI registration not enumerated in the 6-feature task).
- `FreeBlazorExtended.Foundation` → `FreeBlazorExtended/Foundation/DataTableOptions.cs` plus the table/layout/forms/feedback/cards Razor components under `Foundation/Components/`.
- `FreeBlazorExtended` (root) → `FreeBlazorExtended/Foundation/Helpers.cs`, `FreeBlazorExtended/Foundation/DataObjects.cs`.
- A separate `FreeBlazorExtended.Agent` namespace lives in the standalone `FreeBlazorExtended.Agent` project (Worker Service host) — referenced conceptually by Feature 105 documentation but not by the FreeBlazorExample solution graph.
