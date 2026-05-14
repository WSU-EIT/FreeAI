/*
    Purpose: Common contract that every platform adapter must implement.
    Used by: GitRepoBrowserService (to dispatch to the correct adapter at runtime).
*/
namespace FreeBlazorExtended.GitRepoBrowser;

public interface IGitRepoAdapter
{
    GitPlatform Platform { get; }

    /// <summary>Returns all branches for the repository described by <paramref name="ctx"/>.</summary>
    Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default);

    /// <summary>
    /// Lists the immediate children of <paramref name="path"/> on <paramref name="branch"/>.
    /// Pass an empty string for the repository root.
    /// </summary>
    Task<List<GitTreeNode>> GetTreeAsync(GitRepoContext ctx, string path, string branch, CancellationToken ct = default);

    /// <summary>Downloads and returns the content of a single file.</summary>
    Task<GitFileContent> GetFileContentAsync(GitRepoContext ctx, string path, string branch, CancellationToken ct = default);
}
