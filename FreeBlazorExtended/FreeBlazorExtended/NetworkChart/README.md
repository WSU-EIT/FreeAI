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

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** An interactive force-directed graph (nodes + edges) rendered with vis.js: physics-driven layout, per-node FontAwesome icons, edge labels, and click callbacks for both nodes and relationships.

**What tech & where?** [NetworkChart.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/NetworkChart/NetworkChart.razor) (C# component + data classes) · [NetworkChart.razor.js](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/NetworkChart/NetworkChart.razor.js) (vis.js interop).

**Why does this exist?** To visualize relationships (org charts, dependency graphs, networks) without building a graph engine.

**What does it beat?** A **seedable layout** gives consistent, repeatable rendering, and both node *and* edge clicks raise callbacks. (Requires vis-network + jQuery loaded in the host page.)

**Terminology:** **Force-directed graph** — a layout where nodes repel and edges pull, so clusters arrange themselves.

**The hard part, drawn:**
```
  Nodes + Relationships ─▶ vis.js force/physics layout ─▶ render ─▶ click node/edge ─▶ OnElementSelected(id)
```
