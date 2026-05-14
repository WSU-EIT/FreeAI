# Timeline

> FedEx-style vertical timeline of events — dot, connector line, title, timestamp, actor, optional details. Mobile-friendly.

## What this component does
Renders a vertical column of status events. Each event has a colored state dot on the left, an optional connector line down to the next event, and content on the right (title, timestamp, actor, description, optional rich detail). Color-coded by state: Completed (green), InProgress (blue), Pending (gray), Error (red), Skipped (gray with strike-through). No horizontal space required, so it works well on mobile and inside narrow cards.

This is the "tracking page" / FedEx / UPS layout — read-only status visualization, not an interactive form.

## Files in this folder
| File | Purpose |
|---|---|
| `Timeline.razor` | The component, plus public nested `TimelineEvent` class and `TimelineEventState` enum |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 utility classes
- **Icons:** FontAwesome 6 (default per-state icons; caller may override per event)
- **JS:** none
- **Cross-feature dependencies:** none

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/Timeline/` folder into your project.
2. Add `@using FreeBlazorExtended.Timeline` to your `_Imports.razor`.
3. Ensure Bootstrap 5 and FontAwesome 6 are loaded by the host app.

## Usage
```razor
@using static FreeBlazorExtended.Timeline.Timeline

<Timeline Events="_events"
          ShowTimestamps="true"
          ShowConnectorLine="true"
          TimestampFormat="MMM d, h:mm tt"
          OnEventClick="HandleClick" />

@code {
    private List<TimelineEvent> _events = new() {
        new() { Title = "Order Placed",   Timestamp = DateTime.Now.AddMinutes(-45),
                State = TimelineEventState.Completed, Actor = "Customer" },
        new() { Title = "Preparing",      Timestamp = DateTime.Now.AddMinutes(-30),
                State = TimelineEventState.Completed, Actor = "Kitchen Staff" },
        new() { Title = "Out for Delivery", State = TimelineEventState.InProgress,
                Description = "Driver is en route." },
        new() { Title = "Delivered",      State = TimelineEventState.Pending },
    };

    private void HandleClick(TimelineEvent evt)
    {
        // optional — only fires if you wire it
    }
}
```

### Parameters
| Name | Type | Default | Notes |
|---|---|---|---|
| `Events` | `List<TimelineEvent>` | `new()` | Required content |
| `ShowTimestamps` | `bool` | `true` | Hide to use as a plain step list |
| `ShowConnectorLine` | `bool` | `true` | Vertical line between dots |
| `TimestampFormat` | `string` | `"MMM d, h:mm tt"` | Standard `DateTime.ToString` format |
| `OnEventClick` | `EventCallback<TimelineEvent>` | — | Rows only become clickable when wired |
| `Class` | `string` | `""` | Extra CSS on the wrapper |

### TimelineEvent fields
`Id`, `Title`, `Description`, `Timestamp`, `State`, `Icon` (optional FA class), `Actor`, `DetailHtml` (rendered as `MarkupString`).

### TimelineEventState
`Pending`, `InProgress`, `Completed`, `Error`, `Skipped`.

## Status
- Implementation: **REAL** — direct port from the FreeExamples PipelineTracker demo.
- Known gaps: none.

## Effort to integrate
**S** — single Razor file, no JS, no host-app deps.
