# KanbanBoard

> Trello / Jira / Azure Boards style drag-and-drop board. HTML5-native drag-and-drop (no JS interop), built-in search, WIP limits, optional custom card template, fully **controlled** (caller owns the cards list).

## What this component does
Renders a horizontal row of columns; each column shows its cards as a stack. The user drags a card between columns (or to a specific slot within a column) and the component raises `OnCardMoved` with `{ CardId, FromColumnId, ToColumnId, NewSortOrder }`. The caller decides what to do with that event — persist to a database, broadcast over SignalR to other clients, optimistically update local state — and re-renders by passing a new `Cards` list back as a parameter.

## Refactor notes — what was removed from the demo
The original FreeExamples page wired its own SignalR `HubConnection` (subscribed to `BlazorDataModel.OnSignalRUpdate`) so a drag in one tab would push the move to other tabs. **All SignalR coupling has been removed from this component.** The component never opens a connection, never listens for updates, and never mutates `Cards` mid-drag. If you want real-time multi-client sync, you wire SignalR in the **caller** and update the `Cards` parameter when a remote move arrives.

## Controlled-component pattern
React-style. The component is a pure projection of `Cards` + `Columns`:
- The component **does not** mutate `Cards` on drop. It only fires `OnCardMoved`.
- The caller mutates its own list (changes `card.ColumnId`, re-numbers `SortOrder`) and re-renders.
- This means `Cards` is always the single source of truth — no internal copy can drift out of sync.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `KanbanBoard.razor` | Markup, drag handlers, public nested types | ~270 |
| `README.md` | This file | <90 |

## Dependencies
- **NuGet packages:** none beyond the BCL
- **CSS:** Bootstrap 5 utility classes
- **Icons:** FontAwesome 6 (used by the default card template only — supply your own `CardTemplate` to drop the dependency)
- **JS:** none. HTML5 drag-and-drop attributes (`draggable`, `ondragstart`, `ondragover`, `ondrop`) are handled natively by the browser and surfaced by Blazor as C# callbacks.
- **Cross-feature dependencies:** none

## Cherry-pick instructions
1. Copy `FreeBlazorExtended/KanbanBoard/` into your project.
2. Add `@using FreeBlazorExtended.KanbanBoard` to your `_Imports.razor`.
3. Make sure Bootstrap 5 is referenced from your host page; add FontAwesome 6 only if you use the default card template.
4. No DI, no services, no migrations.

## Usage
```razor
@using FreeBlazorExtended.KanbanBoard

<KanbanBoard Columns="_columns"
             Cards="_cards"
             OnCardMoved="HandleMoved"
             OnCardClick="HandleClick" />

@code {
    private List<KanbanBoard.KanbanColumn> _columns = new() {
        new() { Id = "todo",    Title = "To Do",       Color = "secondary", SortOrder = 0 },
        new() { Id = "doing",   Title = "In Progress", Color = "primary",   SortOrder = 1, CardLimit = 3 },
        new() { Id = "done",    Title = "Done",        Color = "success",   SortOrder = 2 },
    };

    private List<KanbanBoard.KanbanCard> _cards = new() {
        new() { Id = "1", ColumnId = "todo",  Title = "Write spec",  Priority = "high",   SortOrder = 0 },
        new() { Id = "2", ColumnId = "doing", Title = "Refactor",    Priority = "medium", SortOrder = 0, Assignee = "Dan" },
    };

    private async Task HandleMoved(KanbanBoard.KanbanCardMovedEventArgs e) {
        var card = _cards.First(c => c.Id == e.CardId);
        card.ColumnId   = e.ToColumnId;
        card.SortOrder  = e.NewSortOrder;
        // Renumber siblings, persist to DB, broadcast over SignalR — your call.
        StateHasChanged();
    }

    private void HandleClick(KanbanBoard.KanbanCard c) { /* open detail modal, etc. */ }
}
```

## Public API
| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Columns` | `List<KanbanColumn>` | `new()` | Required. Sorted by `SortOrder`. |
| `Cards` | `List<KanbanCard>` | `new()` | Required. **Source of truth** — caller mutates. |
| `OnCardMoved` | `EventCallback<KanbanCardMovedEventArgs>` | — | Fires after every drop. Component does **not** mutate. |
| `OnCardClick` | `EventCallback<KanbanCard>` | — | Fires on card click (also fires after a drop — guard if needed). |
| `EnableSearch` | `bool` | `true` | Toggles the built-in search bar. |
| `SearchPlaceholder` | `string` | `"Search cards…"` | Placeholder text for the search input. |
| `CardTemplate` | `RenderFragment<KanbanCard>?` | `null` | Custom card body. Fallback default shows title, description (clamped), priority badge, labels, assignee, due date. |
| `ReadOnly` | `bool` | `false` | Disables `draggable` and ignores drop events. |

`KanbanColumn`, `KanbanCard`, `KanbanCardMovedEventArgs` are public nested types — see source for fields.

## Status
- Implementation: **REAL**
- Persistence: caller-owned (component is stateless beyond UI drag/search)
- Known gaps: HTML5 drag-and-drop has uneven mobile-browser support; for touch devices a polyfill or pointer-event fallback would be needed (not included). The `OnCardClick` callback also fires on the click that ends a drag — callers that need to disambiguate can track a flag set in `OnCardMoved`.

## Effort to integrate
**L** — one Razor file, three public nested types, one `_Imports` line. No JS, no services, no migrations. The "L" reflects the SignalR refactor and the controlled-component contract: the caller must implement the `Cards` mutation logic in `OnCardMoved` themselves; nothing happens automatically.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A Trello/Jira-style drag-and-drop board. When you drop a card, the component doesn't move it — it *announces* the move via `OnCardMoved`. Your page decides what to do (save, sync, re-order) and hands back an updated `Cards` list to redraw. This "the board reports, you decide" design is a **controlled component**. Uses HTML5-native drag-and-drop (no JavaScript), with built-in search and WIP limits.

**What tech & where?** One file — [KanbanBoard.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/KanbanBoard/KanbanBoard.razor) (HTML5 `draggable` attributes surfaced as C# callbacks; zero NuGet packages).

**Why does this exist?** Teams wanted a board inside their own Blazor apps without a heavy third-party widget or writing JavaScript.

**What does it beat?** Commercial board widgets own their data and bury you in sync callbacks. This one is deliberately **stateless about your data** — your `Cards` list is the single source of truth, so it can never silently drift out of sync with your database or other users' screens.

**Terminology:** **Controlled component** — it renders what you give it and raises events; it never mutates your data. **`OnCardMoved`** — the event carrying `{ CardId, From, To, NewSortOrder }`.

**The hard part, drawn:**
```
  you drag a card ─▶ HTML5 drop event (C# callback) ─▶ KanbanBoard fires OnCardMoved {Card, From, To, Sort}
        │                                                          │ board does NOT move the card
        │ 5. board re-renders from the NEW list                    ▼
        └──────── YOUR page: update card.Column · renumber · persist · (optional) SignalR broadcast
                            then pass the new Cards list back ─────┘   (Cards is the single source of truth)
```
