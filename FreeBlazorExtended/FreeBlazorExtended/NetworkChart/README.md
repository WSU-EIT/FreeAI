# NetworkChart

> Network/graph visualization (nodes + edges) backed by vis-network.

## What this component does
Renders an interactive force-directed graph using vis.js. Supports custom physics solvers (repulsion, barnesHut), per-node icons (FontAwesome), edge labels, click callbacks for both nodes and relationships, and seedable layout for consistent rendering.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `NetworkChart.razor` | The component (with `NetworkNode`, `NetworkRelationship` data classes) | 226 |
| `NetworkChart.razor.js` | JS interop wrapping vis.js | 200+ |

## Dependencies
- **NuGet packages:** none
- **JS libraries (must load before component):** [vis-network](https://visjs.github.io/vis-network/), jQuery
- **Cross-feature dependencies:** none
- **SignalR:** not used
- **EF Core:** not used

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/NetworkChart/` folder.
2. Add `@using FreeBlazorExtended.NetworkChart` to your `_Imports.razor`.
3. Ensure vis-network is loaded in your host page.

## Usage
```razor
<NetworkChart Nodes="_nodes" Relationships="_edges"
              OnElementSelected="@(new Action<string>(id => _selectedNodeId = id))" />

@code {
    private List<NetworkChart.NetworkNode> _nodes = new() {
        new() { id = "1", label = "Alice" },
        new() { id = "2", label = "Bob" },
    };
    private List<NetworkChart.NetworkRelationship> _edges = new() {
        new() { from = "1", to = "2", label = "knows" },
    };
}
```

## Status
- Implementation: **REAL** — direct port from FreeExamples
- Known gaps: none

## Effort to integrate
**M** — requires loading vis.js + jQuery in the host page.
