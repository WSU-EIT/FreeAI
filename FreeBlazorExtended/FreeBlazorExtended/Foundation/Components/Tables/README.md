# Foundation Tables

Composable table primitives for selection, expansion, pagination, visibility, and batch actions.

## Files in this folder

| File | Purpose |
|---|---|
| `BulkActions.razor` | Shows a selected-row toolbar and executes shared actions across multiple records. |
| `ColumnSelector.razor` | Toggles visible columns for configurable data-table layouts. |
| `DataTable.razor` | Renders a generic table with search, headers, rows, and optional action content. |
| `ExpandableRows.razor` | Adds expandable detail rows to tabular record views. |
| `TablePagination.razor` | Handles page navigation, counts, and page-size changes for table views. |

## Usage notes

- These components are intended to compose together through shared options and caller-managed data.
- `DataTableOptions.cs` in the Foundation root holds the common configuration model for this area.

---

### 🧭 Briefing — **In one line:** five composable table parts — `DataTable` (generic table with search), `TablePagination`, `ColumnSelector` (toggle columns), `ExpandableRows`, and `BulkActions` (act on selected rows) — wired together through shared `DataTableOptions` and caller-owned data. **Why:** so any list view gets selection/expansion/paging without rebuilding a grid. See the parent [Foundation Components README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Components/README.md).