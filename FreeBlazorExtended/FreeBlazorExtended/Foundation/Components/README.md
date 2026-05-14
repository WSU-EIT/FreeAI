# Foundation Components

Reusable UI primitives shared across FreeBlazorExtended feature folders and showcase pages.

## What this folder provides

- `Cards/` for compact summary and KPI surfaces.
- `Feedback/` for loading, empty, success, and error states.
- `Forms/` for field grouping, actions, and validation helpers.
- `Layout/` for page and multi-column scaffolding.
- `Tables/` for reusable table behaviors and controls.

## Categories in this folder

| Category | Count | Purpose |
|---|---:|---|
| `Cards` | 4 | Present metrics, status, progress, and feature summaries. |
| `Feedback` | 4 | Render loading, empty, success, and error states. |
| `Forms` | 3 | Compose common form sections, action bars, and validation messaging. |
| `Layout` | 4 | Provide reusable page, grid, and sidebar layouts. |
| `Tables` | 5 | Add selection, expansion, pagination, and column controls for table views. |

## Notes

- All components in this subtree use the `FreeBlazorExtended.Components` namespace.
- These primitives are intended to stay generic so feature folders can compose them without bringing in feature-specific dependencies.