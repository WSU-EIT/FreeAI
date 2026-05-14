# FreeBlazorExtended Structure - Verification & Proposal
*Generated: 2026-04-30*

## Scope note
The audit at `FreeBlazorExample/FreeBlazorExtended-audit.md` already established
that the FreeBlazorExtended class library lives at the **solution root**
(`AllOfDanielsProjects/FreeBlazorExtended/`), not under `FreeBlazorExample/`,
and is referenced by `FreeBlazorExample.Client.csproj` via a `ProjectReference`.
This document verifies the current per-feature folder layout against the ideal
cherry-pickable target.

## Current actual tree

Top level of `FreeBlazorExtended/`:

```
FreeBlazorExtended/
  .gitignore
  FreeBlazorExtended.csproj
  README.md                 <-- top-level index already exists
  _Imports.razor            <-- @using all 6 feature namespaces + Foundation

  AgentMonitoring/          (Feature 105)
    AgentMonitoring.cs              (416 lines - all DTOs/enums)
    AgentMonitoringService.cs       (712 lines - service)

  Calendar/                 (Feature 103)
    CalendarEvent.cs                (85 lines - models + enums)
    CalendarEventService.cs         (143 lines - service)

  DynamicForms/             (Feature 101)
    FormDefinition.cs               (69 lines - models)
    FormService.cs                  (202 lines - service)
    Components/
      ConditionalField.razor
      DynamicFormRenderer.razor
      FormBuilder.razor

  Foundation/               (shared cross-feature code)
    DataObjects.cs                  (namespace: FreeBlazorExtended)
    DataTableOptions.cs             (namespace: FreeBlazorExtended.Foundation)
    Helpers.cs                      (namespace: FreeBlazorExtended)
    Services/
      NotificationService.cs        (namespace: FreeBlazorExtended.Foundation.Services)
    Components/
      Cards/        (4 razor: MetricCard, FeatureCard, StatusCard, ProgressCard)
      Tables/       (5 razor: DataTable, TablePagination, BulkActions, ExpandableRows, ColumnSelector)
      Forms/        (3 razor: FormSection, FormActions, ValidationMessage)
      Layout/       (4 razor: TwoColumnLayout, ThreeColumnLayout, ResponsiveGrid, PageContainer)
      Feedback/     (4 razor: LoadingSpinner, EmptyState, ErrorMessage, SuccessMessage)

  HierarchicalTree/         (Feature 107)
    TreeNode.cs                     (30 lines - model)
    TreeService.cs                  (147 lines - service)
    Components/
      HierarchicalTree.razor
      TreeNodeComponent.razor

  MultiViewSync/            (Feature 102)
    PresentationSession.cs          (37 lines - models)
    RealtimeSyncService.cs          (134 lines - service; SignalR Hub MISSING)

  UserPreferences/          (Feature 104)
    UserPreferences.cs              (37 lines - POCO)
    UserPreferencesService.cs       (106 lines - service)

  bin/, obj/                (build artifacts, ignored)
```

## Per-feature verification

Cross-feature dependency check methodology: Grep for
`^using FreeBlazorExtended` across all `.cs` files under `FreeBlazorExtended/`
returned **zero matches**. The only `@using FreeBlazorExtended.*` references
appear in (a) the project-level `_Imports.razor` (which globally imports all
six feature namespaces plus Foundation - this is by design for the showcase
host but is irrelevant to per-feature cherry-picking) and (b) razor components
self-importing their own feature namespace. No service `.cs` file imports
another feature's namespace.

### Feature 101 - DynamicForms
- Folder exists: **yes** (`FreeBlazorExtended/DynamicForms/`)
- Expected files present: **yes** - `FormService.cs`, `FormDefinition.cs`,
  `Components/FormBuilder.razor`, `Components/DynamicFormRenderer.razor`,
  `Components/ConditionalField.razor` all match the audit's "Files to copy" list
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes**
- Caveats: Razor components rely on `_Imports.razor` for `@inject` glue. When
  cherry-picked into `wicketbr/FreeBlazor`, either copy a feature-local
  `_Imports.razor` or add the namespace to the target project's existing one.

### Feature 102 - MultiViewSync
- Folder exists: **yes** (`FreeBlazorExtended/MultiViewSync/`)
- Expected files present: **yes** for what's in the repo today
  (`RealtimeSyncService.cs`, `PresentationSession.cs`)
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes-with-caveats**
- Caveats:
  - **SignalR `Hub<>` class is MISSING** (audit line 52). `RealtimeSyncService`
    tracks connection IDs but no hub pushes events. A `PresentationHub` and
    client-side `HubConnection` wiring must be authored before this feature is
    truly real-time.
  - No `Hubs/` subfolder under `MultiViewSync/` yet. When the hub is added,
    place it at `FreeBlazorExtended/MultiViewSync/Hubs/PresentationHub.cs` to
    keep it self-contained.

### Feature 103 - Calendar
- Folder exists: **yes** (`FreeBlazorExtended/Calendar/`)
- Expected files present: **yes** - `CalendarEventService.cs`, `CalendarEvent.cs`
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes**
- Caveats: No razor components yet (audit doesn't list any). The showcase page
  in FreeBlazorExample.Client renders the calendar inline.

### Feature 104 - UserPreferences
- Folder exists: **yes** (`FreeBlazorExtended/UserPreferences/`)
- Expected files present: **yes** - `UserPreferencesService.cs`,
  `UserPreferences.cs`
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes**
- Caveats: none

### Feature 105 - AgentMonitoring
- Folder exists: **yes** (`FreeBlazorExtended/AgentMonitoring/`)
- Expected files present: **yes** - `AgentMonitoringService.cs` (712 lines),
  `AgentMonitoring.cs` (416 lines, all DTOs)
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes-with-caveats**
- Caveats:
  - The companion `FreeBlazorExtended.Agent` Worker Service project lives
    *outside* this folder at `AllOfDanielsProjects/FreeBlazorExtended.Agent/`.
    A consumer who wants the matching real-world endpoint must copy that
    sibling project too.
  - Same as 102: no SignalR hub for pushing commands to remote agents.

### Feature 107 - HierarchicalTree
- Folder exists: **yes** (`FreeBlazorExtended/HierarchicalTree/`)
- Expected files present: **yes** - `TreeService.cs`, `TreeNode.cs`,
  `Components/HierarchicalTree.razor`, `Components/TreeNodeComponent.razor`
- Cross-feature imports detected: **none**
- Has README: **no**
- Cherry-pick-ready: **yes**
- Caveats: same `_Imports.razor` glue note as Feature 101.

## Proposed adjustments

The current layout is **already very close to the ideal cherry-pickable
target**. The audit's claim is confirmed: per-feature folders exist, services
and models live alongside their components, and there are zero cross-feature
`using` statements at the C# level. The only proposals are minor:

1. **Add per-feature `README.md`** files (deferred to Phase 5.3). Each should
   cover purpose, public API, DI registration line, and any caveats (e.g.
   missing SignalR hub for 102, missing remote-agent transport for 105).
2. **Reserve `MultiViewSync/Hubs/`** subfolder for the future
   `PresentationHub.cs` (Feature 102 SignalR gap from audit). Folder need not
   be created until the hub is authored, but the README should call out the
   intended location so cherry-pickers know where to look.
3. **Document Foundation coupling in the top-level README.** Every feature
   transitively pulls in `FreeBlazorExtended/Foundation/Helpers.cs` and
   `Foundation/DataObjects.cs` (root namespace `FreeBlazorExtended`), because
   service files use `BooleanResponse` and other Foundation DTOs implicitly via
   the global namespace. Cherry-pickers must copy `Foundation/Helpers.cs`,
   `Foundation/DataObjects.cs`, and `Foundation/Services/NotificationService.cs`
   alongside any feature folder. The current top-level `README.md` lists the
   Foundation folder but does not call out this hard dependency for cherry-pick
   consumers - one paragraph would close the gap.
4. **No file moves needed.** `Helpers.cs`, `DataObjects.cs`, and
   `NotificationService.cs` are correctly under `Foundation/`. No feature
   currently has a helper that should migrate out of Foundation, and no
   Foundation file is exclusively used by one feature.
5. **Optional polish:** rename folders to align with showcase numbering (e.g.
   `101_DynamicForms/`, `102_MultiViewSync/`) for visual grouping with the
   showcase pages. **Recommend against** - it would churn `_Imports.razor`,
   every showcase-page `@inject` line, and every DI registration in both
   `Program.cs` files for purely cosmetic gain. Keep current names.

## Migration checklist for Phase 5.2

Since the audit confirmed the structure is already in place, **5.2 (file
migration) is effectively a no-op**. Outstanding work that touches structure:

- [ ] Write per-feature `README.md` files (Phase 5.3) - 6 files, ~50 lines each
- [ ] Author `MultiViewSync/Hubs/PresentationHub.cs` + client `HubConnection`
      wiring to close the Feature 102 SignalR gap (separate phase, not 5.2)
- [ ] Update top-level `FreeBlazorExtended/README.md` with a "Cherry-picking"
      section that explicitly lists the Foundation files every feature pulls in
- [ ] (Optional) `FreeBlazorExtended.Agent/` README explaining its relationship
      to Feature 105

No code edits, no folder renames, no file moves are required for 5.2.

## Sign-off
- Estimated remaining effort: **S** (small) - structure is correct; only doc
  writing and one missing SignalR hub remain.
- Blockers: **none** for structure verification. The Feature 102 SignalR-hub
  gap is a feature-completeness blocker, not a structure blocker.
- Verification mode: **read-only**. No code edits made during this audit.
