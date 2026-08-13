# Diagram coverage

Three editing modes. The mode depends on how the diagram behaves, not on how popular it is.

## Canvas (drag nodes, drag to connect)

| Type | Mermaid keyword | Notes |
| --- | --- | --- |
| Flowchart | `flowchart` | 15 shapes, 8 link styles, nested subgraphs, per-subgraph direction, style and classDef, click links |
| State | `stateDiagram-v2` | start and end markers, choice, fork, composite states |
| Class | `classDiagram` | members per class, six relationship kinds, namespaces |
| ER | `erDiagram` | attributes with keys, six cardinality forms |
| Mindmap | `mindmap` | six node shapes, tree derived from connections |
| Requirement | `requirementDiagram` | all six requirement kinds plus elements, seven relationship kinds |
| C4 context | `C4Context` | person, system, container, component, database, queue, boundaries |
| Architecture | `architecture-beta` | services, groups, junctions, port sides taken from node positions |
| Block | `block-beta` | blocks, spaces, column count from layout |

## Rows (a table, because these are data not drawings)

Sequence, Gantt, Pie, XY chart, Quadrant, Sankey, Radar, Timeline, User journey, Kanban, Git graph, Treemap, Packet.

## Source with live render

ZenUML, Venn, Ishikawa, Railroad, Cynefin, TreeView, and a catch-all entry for pasting any Mermaid source.

## Known gaps

- Swimlanes have no dedicated editor yet
- Sequence fragments (alt, loop, par, critical) are not in the row editor; add them in source mode
- Subgraph membership is decided by position on the canvas, not by an explicit parent field
- Node positions are the editor's own layout. Mermaid runs its own layout engine, so the rendered output will not match canvas positions exactly
