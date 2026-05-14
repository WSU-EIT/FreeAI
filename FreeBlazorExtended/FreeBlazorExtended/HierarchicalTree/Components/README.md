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