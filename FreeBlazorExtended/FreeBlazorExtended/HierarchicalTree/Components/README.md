# HierarchicalTree Components

UI components that make up the Feature 107 hierarchical tree editing surface.

## Files in this folder

| File | Purpose |
|---|---|
| `HierarchicalTree.razor` | Main tree surface with toolbar actions and root-level rendering. |
| `TreeNodeComponent.razor` | Recursive node renderer for selection, expansion, and nested children. |

## Usage notes

- These components depend on the models in `TreeNode.cs` and the service logic in `TreeService.cs`.
- Keep feature-specific tree behaviors in the parent folder so these components stay reusable.

---

### 🧭 Plain-English Briefing — The Boss Questions

**In one line:** the two Razor pieces of Feature 107 — `HierarchicalTree` (the tree surface + toolbar) and `TreeNodeComponent` (the recursive node renderer with expand/collapse).

**Why it exists:** to render and edit a tree (drag/click to reparent). **See the full feature briefing** (including the cycle-safe move logic) in the parent [HierarchicalTree README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/HierarchicalTree/README.md).