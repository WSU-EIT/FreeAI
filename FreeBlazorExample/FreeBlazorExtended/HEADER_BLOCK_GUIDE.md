# FreeBlazorExtended Header Block Guide

This library should not use one header shape for every file. The header should tell a future maintainer what they need to know before they read the body, and that depends on the file's role.

## Decision process

Use this sequence when deciding how much header to write:

1. Identify the file kind.
   - Core infrastructure or orchestration
   - Stateful feature service or rich interactive component
   - Model bundle or configuration file
   - Thin wrapper, helper, interface, or small presentational component
2. Ask what a maintainer cannot infer quickly from the filename alone.
   - Shared ownership of state
   - Host expectations or DI registration
   - External dependencies or JS interop
   - Cross-feature reuse
   - Why this file exists instead of pushing the logic somewhere else
3. Match the header depth to that complexity.

## Tier 1: Core infrastructure and orchestration files

Use this for helpers, shared data contracts, central services, and files that own important state or cross-cutting behavior.

Include:

- `Role`: where the file sits in the library architecture.
- `Purpose`: why the file exists.
- `Contains` or `Owns`: the main types, state, or responsibilities in the file.
- `Used by` or `Host expectations`: who depends on it, or what the host must provide.

Typical examples:

- `Foundation/Helpers.cs`
- `Foundation/DataObjects.cs`
- `Foundation/DataTableOptions.cs`
- `DynamicForms/FormService.cs`
- `AgentMonitoring/AgentMonitoringService.cs`

## Tier 2: Stateful feature services and rich components

Use this for files that are not cross-cutting infrastructure, but still have enough behavior that a one-line purpose statement is not sufficient.

Include:

- `Purpose`
- `Key behaviors`
- `Dependencies` or `Interop` when applicable

Typical examples:

- `Calendar/CalendarEventService.cs`
- `MultiViewSync/RealtimeSyncService.cs`
- `Foundation/Components/Tables/DataTable.razor`
- `CommandPalette/CommandPalette.razor`
- `NetworkChart/NetworkChart.razor`

## Tier 3: Model bundles and configuration files

Use this for files that primarily define related enums, DTOs, or option objects.

Include:

- `Purpose`
- `Contains`
- Optional `Used by` when the consuming layer is not obvious

Typical examples:

- `Calendar/CalendarEvent.cs`
- `DynamicForms/FormDefinition.cs`
- `HierarchicalTree/TreeNode.cs`
- `MultiViewSync/PresentationSession.cs`
- `UserPreferences/UserPreferences.cs`

## Tier 4: Thin wrappers, interfaces, and small presentational components

Keep these short. If the implementation is obvious once opened, the header should not be longer than the file's own conceptual weight.

Include:

- `Purpose`
- One short sentence explaining where it fits when that is not obvious from the filename alone

Typical examples:

- `Foundation/Components/Forms/ValidationMessage.razor`
- `DynamicForms/Components/ConditionalField.razor`
- `Foundation/Components/Layout/TwoColumnLayout.razor`
- `UserPreferences/IUserPreferencesStore.cs`

## Style rules

- Use file-level block comments for file intent.
- Use XML documentation on types and members for API-level detail.
- Do not restate the class name in prose unless needed for clarity.
- Do not describe syntax-level trivia such as "contains properties" unless you also explain why those properties matter.
- If a file is reused across multiple features, say that explicitly.
- If a file depends on host wiring, JS interop, or DI, say that explicitly.
- If a file is intentionally small, keep the header small.

## Quick rubric

| File shape | Header depth |
|---|---|
| Central shared helper or orchestration service | 4 lines with architecture context |
| Feature service or rich UI component | 3 lines with behavior and dependencies |
| Model bundle or option file | 2-3 lines naming what it contains |
| Thin interface or wrapper | 1-2 lines |