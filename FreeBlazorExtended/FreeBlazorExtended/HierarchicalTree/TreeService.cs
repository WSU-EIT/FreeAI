/*
    Purpose: In-memory tree management service for hierarchical editing and traversal.
    Key behaviors: Stores nodes, resolves roots and children, computes hierarchy views, and enforces tree-safe mutations.
    Dependencies: Uses in-memory concurrent state so the feature can run standalone in the showcase host.
*/
using System.Collections.Concurrent;

namespace FreeBlazorExtended.HierarchicalTree;

public partial class TreeService
{
    private static readonly ConcurrentDictionary<Guid, TreeNode> _nodes = new();

    public Task<List<TreeNode>> GetTreeNodes(Guid TenantId)
    {
        var nodes = _nodes.Values
            .Where(n => n.TenantId == TenantId && !n.Deleted)
            .ToList();
        return Task.FromResult(nodes);
    }

    public Task<TreeNode?> GetTreeNode(Guid NodeId)
    {
        _nodes.TryGetValue(NodeId, out var node);
        return Task.FromResult(node?.Deleted == false ? node : null);
    }

    public Task<List<TreeNode>> GetTreeNodeChildren(Guid ParentNodeId)
    {
        var children = _nodes.Values
            .Where(n => n.ParentNodeId == ParentNodeId && !n.Deleted)
            .OrderBy(n => n.SortOrder)
            .ToList();
        return Task.FromResult(children);
    }

    public Task<List<TreeNode>> GetRootNodes(Guid TenantId)
    {
        var roots = _nodes.Values
            .Where(n => n.TenantId == TenantId && n.ParentNodeId == null && !n.Deleted)
            .OrderBy(n => n.SortOrder)
            .ToList();
        return Task.FromResult(roots);
    }

    public Task<TreeNode> SaveTreeNode(TreeNode node, string? UserId = null)
    {
        if (node.TreeNodeId == Guid.Empty)
            node.TreeNodeId = Guid.NewGuid();

        node.LastModified = DateTime.UtcNow;
        node.LastModifiedBy = UserId;

        if (!_nodes.ContainsKey(node.TreeNodeId)) {
            node.Added = DateTime.UtcNow;
            node.AddedBy = UserId;
        }

        _nodes[node.TreeNodeId] = node;
        return Task.FromResult(node);
    }

    public async Task<(bool success, string? error)> MoveTreeNode(
        Guid NodeId,
        Guid? newParentId,
        int newSortOrder,
        string? UserId = null)
    {
        // Check if node exists
        if (!_nodes.TryGetValue(NodeId, out var node))
            return (false, "Node not found");

        // Prevent moving a node to itself
        if (newParentId == NodeId)
            return (false, "Cannot move a node into itself");

        // Check for cycles
        if (newParentId.HasValue && IsDescendant(NodeId, newParentId.Value))
            return (false, "Cannot move a node into its own descendant (would create a cycle)");

        // Update the node
        node.ParentNodeId = newParentId;
        node.SortOrder = newSortOrder;
        node.LastModified = DateTime.UtcNow;
        node.LastModifiedBy = UserId;

        await SaveTreeNode(node, UserId);

        // Reorder siblings
        await ReorderSiblings(newParentId, UserId);

        return (true, null);
    }

    private async Task ReorderSiblings(Guid? ParentId, string? UserId = null)
    {
        var siblings = _nodes.Values
            .Where(n => n.ParentNodeId == ParentId && !n.Deleted)
            .OrderBy(n => n.SortOrder)
            .ToList();

        for (int i = 0; i < siblings.Count; i++) {
            siblings[i].SortOrder = i;
            await SaveTreeNode(siblings[i], UserId);
        }
    }

    private bool IsDescendant(Guid possibleAncestor, Guid possibleDescendant)
    {
        var current = possibleDescendant;
        var visited = new HashSet<Guid>();

        while (current != Guid.Empty && !visited.Contains(current)) {
            if (current == possibleAncestor)
                return true;

            visited.Add(current);

            if (!_nodes.TryGetValue(current, out var node) || node.ParentNodeId == null)
                break;

            current = node.ParentNodeId.Value;
        }

        return false;
    }

    public async Task<List<TreeNode>> GetDescendants(Guid NodeId)
    {
        var descendants = new List<TreeNode>();
        var queue = new Queue<Guid>();
        queue.Enqueue(NodeId);
        var visited = new HashSet<Guid>();

        while (queue.Count > 0) {
            var currentId = queue.Dequeue();
            if (visited.Contains(currentId))
                continue;

            visited.Add(currentId);

            var children = await GetTreeNodeChildren(currentId);
            descendants.AddRange(children);

            foreach (var child in children)
                queue.Enqueue(child.TreeNodeId);
        }

        return descendants;
    }

    public async Task<List<string>> GetNodePath(Guid NodeId)
    {
        var path = new List<string>();
        var current = NodeId;
        var visited = new HashSet<Guid>();

        while (current != Guid.Empty && !visited.Contains(current)) {
            visited.Add(current);

            if (!_nodes.TryGetValue(current, out var node))
                break;

            path.Insert(0, node.Name);

            if (node.ParentNodeId == null)
                break;

            current = node.ParentNodeId.Value;
        }

        return path;
    }

    public Task<DataObjects.BooleanResponse> DeleteTreeNode(Guid NodeId, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        if (_nodes.TryGetValue(NodeId, out var node)) {
            node.Deleted = true;
            node.LastModified = DateTime.UtcNow;
            output.Result = true;
        } else {
            output.Messages.Add("Tree node not found.");
        }

        return Task.FromResult(output);
    }

    public void ClearAllNodes()
    {
        _nodes.Clear();
    }
}
