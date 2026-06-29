# Foundation

Shared infrastructure layer for FreeBlazorExtended. This folder holds the cross-cutting models, helpers, services, and reusable UI primitives that the feature folders build on.

## What this folder provides

- Shared DTOs and response types used across the library.
- Common table configuration and metadata models.
- Small helper methods reused by services and components.
- Reusable UI primitives grouped under Cards, Feedback, Forms, Layout, and Tables.
- Shared runtime services that support host applications.

## Files in this folder

| File | Purpose |
|---|---|
| `DataObjects.cs` | Shared response and user DTOs used across FreeBlazorExtended features. |
| `DataTableOptions.cs` | Shared option and metadata types for Foundation data-table components. |
| `Helpers.cs` | Utility helpers reused across FreeBlazorExtended services and components. |

## Subfolders

| Folder | Purpose |
|---|---|
| `Components/` | Shared UI primitives grouped by concern. |
| `Services/` | Shared runtime services such as notifications. |

## Dependencies

- Bootstrap classes for layout, spacing, and status styling.
- Font Awesome icons for visual state indicators in several components.
- Consumed by most top-level feature folders in FreeBlazorExtended.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** Foundation is the **shared base layer** every other component builds on: common DTOs and response types (`BooleanResponse`, user models), table option/metadata types, small reusable helpers, plus a set of UI primitives (Cards, Feedback, Forms, Layout, Tables) and shared services (notifications, etc.).

**What tech & where?** [DataObjects.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/DataObjects.cs) (shared DTOs) · [Helpers.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Helpers.cs) (utilities) · [Components/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Components) · [Services/](https://github.com/WSU-EIT/FreeAI/tree/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Services).

**Why does this exist?** So the ~20 feature components don't each redefine the same response envelope, helpers, and basic UI parts — they share one foundation. (This is why cherry-picking any component says "also copy `Foundation/Helpers.cs` + `DataObjects.cs`".)

**What does it beat?** It's the **single source of shared truth** — change a response shape or a helper once, and every feature gets it.

**Terminology:** **Primitive** — a small building-block component (a card, a spinner) that bigger features compose.

**The hard part, drawn:**
```
  Foundation (DataObjects · Helpers · Components/{Cards,Tables,Forms,Layout,Feedback} · Services)
        ▲ every feature folder depends on this
  KanbanBoard · Calendar · DynamicForms · … ──┘  (copy Foundation when you cherry-pick one)
```