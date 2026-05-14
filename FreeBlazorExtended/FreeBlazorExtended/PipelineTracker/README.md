# PipelineTracker

> Horizontal stage tracker — the Dominos / FedEx / Azure DevOps pattern. Read-only status visualization with circles + connector lines, completed / in-progress / pending / error / skipped states, and an optional click-to-inspect detail panel.

## What this component does
Renders a row of stage circles connected by lines. Each circle shows the stage's icon and state-dependent color (green check = completed, blue = in progress, gray = pending, red X = error, gray strike-through = skipped). Optionally raises `OnStageClick` so the caller can navigate, and optionally shows a built-in details card below the row when `ShowDetailsPanel="true"`.

This is a **status display**, not a wizard. For an interactive form stepper, use `Wizard/`. For a vertical list-style timeline, use `Timeline/`.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `PipelineTracker.razor` | Markup + parameters + nested `PipelineStage` / `PipelineStageState` | ~180 |
| `README.md` | This file | <90 |

## Dependencies
- **NuGet packages:** none beyond the BCL
- **CSS:** Bootstrap 5 utility classes (`d-flex`, `rounded-circle`, `bg-success`, etc.)
- **Icons:** FontAwesome 6 (`fa-solid fa-check`, etc.) — supply your own per-stage `Icon` class
- **JS:** none
- **Cross-feature dependencies:** none

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/PipelineTracker/` folder into your project.
2. Add `@using FreeBlazorExtended.PipelineTracker` to your `_Imports.razor` (or use the fully-qualified component tag).
3. Make sure Bootstrap 5 and FontAwesome 6 are referenced from your host page.
4. No DI, no services, no migrations.

## Usage
```razor
@using FreeBlazorExtended.PipelineTracker

<PipelineTracker Stages="_stages"
                 CurrentStageIndex="_current"
                 ShowDetailsPanel="true"
                 @bind-SelectedStageIndex="_selected"
                 OnStageClick="HandleClick" />

@code {
    private int _current = 2;
    private int? _selected = 2;

    private List<PipelineTracker.PipelineStage> _stages = new() {
        new() { Id = "1", Name = "Order Placed",      Icon = "fa-solid fa-receipt",          CompletedAt = DateTime.Now.AddMinutes(-45), CompletedBy = "Customer" },
        new() { Id = "2", Name = "Preparing",         Icon = "fa-solid fa-utensils",         CompletedAt = DateTime.Now.AddMinutes(-30), CompletedBy = "Kitchen" },
        new() { Id = "3", Name = "Quality Check",     Icon = "fa-solid fa-clipboard-check" },
        new() { Id = "4", Name = "Out for Delivery",  Icon = "fa-solid fa-truck" },
        new() { Id = "5", Name = "Delivered",         Icon = "fa-solid fa-house-circle-check" },
    };

    private void HandleClick(int idx) {
        // optional — navigate or load details for stages[idx]
    }
}
```

To force an `Error` or `Skipped` state on a specific stage, set `stage.State = PipelineStageState.Error` (or `Skipped`) before passing the list in. Otherwise state is inferred from `CurrentStageIndex`: indices below it are `Completed`, the index itself is `InProgress`, indices above it are `Pending`. Use `CurrentStageIndex = -1` for "nothing started" or `CurrentStageIndex = Stages.Count` for "all complete".

## Public API
| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Stages` | `List<PipelineStage>` | `new()` | Required |
| `CurrentStageIndex` | `int` | `0` | -1 = none started, N = all complete |
| `OnStageClick` | `EventCallback<int>` | — | Fires on every circle click |
| `ShowDetailsPanel` | `bool` | `false` | Renders a details card below the row |
| `SelectedStageIndex` | `int?` | `null` | Drives the details card |
| `SelectedStageIndexChanged` | `EventCallback<int?>` | — | Enables `@bind-SelectedStageIndex` |

`PipelineStage` (public nested) carries `Id`, `Name`, `Description`, `Icon`, `State`, `CompletedAt`, `CompletedBy`, `Notes`.
`PipelineStageState` (public nested enum): `Pending`, `InProgress`, `Completed`, `Error`, `Skipped`.

## Status
- Implementation: **REAL**
- Persistence: caller-managed (component is stateless beyond UI)
- Known gaps: no built-in wrapping for very long stage lists on narrow viewports beyond Bootstrap's `flex-wrap`; no animation for state transitions beyond a CSS `transition: all 0.3s` on the circle.

## Effort to integrate
**M** — one Razor file, two public nested types, one `_Imports` line. No services, no JS, no migrations. Caller owns the state machine that drives `CurrentStageIndex`.
