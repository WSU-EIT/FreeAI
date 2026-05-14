# Feature 107 — HierarchicalTree

> Adjacency-list tree with safe move/reorder operations and a drop-in Razor component — for category trees, org charts, folder structures.

## What this feature does
Stores `TreeNode` POCOs with `ParentNodeId` + `SortOrder` and answers structural queries: `GetDescendants` (BFS), `GetNodePath` (root-to-node), sibling reorder, and `MoveTreeNode` with cycle/self-move detection via `IsDescendant`. Two Razor components render the tree recursively and let the user drag/click to move nodes. Drop in for any "things that contain things" UI without inventing the move-validation logic from scratch.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `TreeService.cs` | CRUD + move/reorder with cycle detection, BFS descendants, path lookup | 147 |
| `TreeNode.cs` | `TreeNode` POCO with `ParentNodeId`, `SortOrder` | 30 |
| `Components/HierarchicalTree.razor` | Top-level tree renderer | n/a |
| `Components/TreeNodeComponent.razor` | Recursive node renderer with expand/collapse | n/a |

## Dependencies
- **NuGet packages:** none beyond the BCL
- **Cross-feature dependencies:** none; uses `Foundation/Helpers.cs` and `Foundation/DataObjects.cs`
- **SignalR:** not used
- **EF Core:** not used (in-memory `ConcurrentDictionary<Guid, TreeNode>`)

## DI registration
Add this to your `Program.cs`:
```csharp
builder.Services.AddScoped<FreeBlazorExtended.HierarchicalTree.TreeService>();
```
(For Blazor WASM client also add a Singleton variant — see `FreeBlazorExample/FreeBlazorExample.Client/Program.cs` line 33 for the pattern.)

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/HierarchicalTree/` folder into your project (includes `Components/` subfolder).
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present.
3. Add the DI registration above to server `Program.cs` (line 34 in the example) and the Singleton variant to WASM client `Program.cs` (line 33).
4. Either copy a feature-local `_Imports.razor` or add `@using FreeBlazorExtended.HierarchicalTree` to the target project's existing `_Imports.razor` so the components resolve their `@inject`.
5. EF Core migration not applicable today.

## Showcase
The interactive demo lives at `/showcase/feature107-hierarchical-tree` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature107_HierarchicalTree.razor`

## Status
- Implementation: **REAL** (in-memory)
- Persistence: in-memory only — needs EF migration before production use
- Known gaps: no drag-and-drop polyfill (move is click-to-target today); no transactional batch reorder API.

## Effort to integrate
**S** — two C# files, two Razor components, one DI line, no external services.
