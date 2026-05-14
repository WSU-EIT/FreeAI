/*
    Purpose: HierarchicalTree model bundle for persisted nodes and computed hierarchy views.
    Contains: The editable tree-node contract plus the computed shape used for rendering and traversal.
    Used by: TreeService and the hierarchical tree UI components.
*/
namespace FreeBlazorExtended.HierarchicalTree;

public class TreeNode
{
    /// <summary>Unique identifier for this node. Generated on creation; used as the foreign key in parent/child relationships.</summary>
    public Guid TreeNodeId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits tree visibility and mutations to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Points to the <see cref="TreeNodeId"/> of this node's parent.
    /// <c>null</c> means the node is a root of the tree — the UI renders root nodes
    /// at the top level with no indentation.
    /// </summary>
    public Guid? ParentNodeId { get; set; }

    /// <summary>Display name rendered on the tree node row in the UI.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional longer description shown in the node's tooltip or detail panel.
    /// Not displayed on the tree row itself to avoid clutter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional CSS color string (e.g. <c>"#3b82f6"</c>) applied to the node's icon or
    /// indicator dot. <c>null</c> falls back to the tree's default colour.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional FontAwesome icon class (e.g. <c>"fa-folder"</c>) displayed to the left
    /// of the node name. <c>null</c> shows the default tree chevron icon.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Controls display order among siblings sharing the same <see cref="ParentNodeId"/>.
    /// Lower values appear first. The tree editor updates these values during drag-and-drop reordering.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// <c>true</c> when this node's children are visible in the tree view.
    /// Toggled by clicking the expand/collapse chevron. Defaults to <c>true</c> so the
    /// tree opens fully expanded on first render.
    /// </summary>
    public bool IsExpanded { get; set; } = true;

    /// <summary>
    /// Semantic category for this node used by the host to drive icon selection, permission
    /// checks, or contextual menus. Common values: <c>"default"</c>, <c>"folder"</c>,
    /// <c>"item"</c>. The tree itself treats all types equally.
    /// </summary>
    public string NodeType { get; set; } = "default";

    /// <summary>
    /// Arbitrary JSON blob for host-specific data that doesn't fit the standard fields
    /// (e.g. external IDs, custom attributes). Parsed by the consuming application; ignored by the tree.
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>UTC timestamp of when this node was created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who created this node.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update to this node.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this node.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from all tree queries and render passes when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public class TreeNodeComputed
{
    /// <summary>The underlying persisted node data that this computed view wraps.</summary>
    public TreeNode Node { get; set; } = new();

    /// <summary>
    /// Zero-based nesting level of this node within the tree hierarchy.
    /// Root nodes have <c>Depth = 0</c>; their direct children have <c>Depth = 1</c>, and so on.
    /// Used by <c>TreeNodeComponent</c> to calculate left-margin indentation (<c>Depth × 20 px</c>).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Slash-delimited display path from root to this node (e.g. <c>"Org / Division / Team"</c>).
    /// Computed by <c>TreeService</c> during hierarchy resolution; useful for breadcrumbs
    /// and search result context.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Total number of nodes below this one at all depths. Shown as a badge on the node
    /// row when non-zero; also used by the editor to warn before deleting a parent.
    /// </summary>
    public int DescendantCount { get; set; }

    /// <summary>
    /// IDs of the immediate children of this node (one level only).
    /// Used for efficient child-lookup without re-traversing the full hierarchy.
    /// </summary>
    public List<Guid> ChildNodeIds { get; set; } = new();

    /// <summary>
    /// Fully resolved child nodes (recursive). Populated by <c>TreeService.BuildHierarchy</c>
    /// for the root-level render pass; <c>TreeNodeComponent</c> recurses into this list.
    /// </summary>
    public List<TreeNodeComputed> Children { get; set; } = new();
}
