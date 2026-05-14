/*
    Purpose: Orchestrating service for the GenericGitRepoBrowser component.
    Responsibilities:
      - Accepts a raw URL + credentials, calls GitPlatformDetector, selects the right adapter.
      - Delegates GetBranches / GetTree / GetFileContent to the adapter.
      - Enforces a configurable maxFileSizeBytes guard before fetching file content.
    DI lifetime: Scoped (registered in Program.cs with AddScoped<GitRepoBrowserService>).
*/
namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class GitRepoBrowserService
{
    private readonly HttpClient _http;
    private GitRepoContext?     _currentContext;
    private IGitRepoAdapter?    _currentAdapter;

    // One adapter instance per platform — adapters are stateless, HttpClient is shared.
    private readonly GitHubAdapter       _github;
    private readonly GitLabAdapter       _gitlab;
    private readonly BitbucketAdapter    _bitbucket;
    private readonly GiteaAdapter        _gitea;
    private readonly AzureDevOpsAdapter  _azureDevOps;

    public GitRepoBrowserService(HttpClient http)
    {
        _http         = http;
        _github       = new GitHubAdapter(http);
        _gitlab       = new GitLabAdapter(http);
        _bitbucket    = new BitbucketAdapter(http);
        _gitea        = new GiteaAdapter(http);
        _azureDevOps  = new AzureDevOpsAdapter(http);
    }

    /// <summary>The resolved context for the currently connected repository (null before first Resolve).</summary>
    public GitRepoContext? CurrentContext => _currentContext;

    /// <summary>
    /// Parses <paramref name="repoUrl"/>, selects the matching adapter, and caches the context.
    /// Returns the resolved <see cref="GitRepoContext"/> (no network calls made here).
    /// </summary>
    public GitRepoContext Resolve(
        string repoUrl,
        GitCredentials credentials,
        GitPlatform? platformOverride = null)
    {
        var (platform, owner, repo, baseApiUrl) = GitPlatformDetector.Detect(repoUrl, platformOverride);
        _currentContext = new GitRepoContext(repoUrl, owner, repo, platform, credentials, baseApiUrl);
        _currentAdapter = SelectAdapter(platform);
        return _currentContext;
    }

    /// <summary>Returns all branches for the current repository.</summary>
    public Task<List<GitBranch>> GetBranchesAsync(CancellationToken ct = default) =>
        RequireAdapter().GetBranchesAsync(RequireContext(), ct);

    /// <summary>
    /// Lists the immediate children of <paramref name="path"/> on <paramref name="branch"/>.
    /// Pass an empty string for the repository root.
    /// </summary>
    public Task<List<GitTreeNode>> GetTreeAsync(
        string path, string branch, CancellationToken ct = default) =>
        RequireAdapter().GetTreeAsync(RequireContext(), path, branch, ct);

    /// <summary>
    /// Fetches and decodes a single file.
    /// Throws <see cref="GitRepoException"/> with <see cref="GitErrorCode.FileTooLarge"/>
    /// if the file exceeds <paramref name="maxFileSizeBytes"/>.
    /// </summary>
    public async Task<GitFileContent> GetFileContentAsync(
        string path,
        string branch,
        long maxFileSizeBytes = 512 * 1024,
        CancellationToken ct = default)
    {
        var content = await RequireAdapter().GetFileContentAsync(RequireContext(), path, branch, ct);

        // Secondary guard — adapter may not have had Content-Length up front
        if (content.Size.HasValue && content.Size.Value > maxFileSizeBytes)
            throw new GitRepoException(GitErrorCode.FileTooLarge,
                $"File '{path}' ({content.Size.Value / 1024} KB) exceeds the {maxFileSizeBytes / 1024} KB limit.");

        return content;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private IGitRepoAdapter SelectAdapter(GitPlatform platform) => platform switch
    {
        GitPlatform.GitHub or GitPlatform.GitHubEnterprise => _github,
        GitPlatform.GitLab    => _gitlab,
        GitPlatform.Bitbucket => _bitbucket,
        GitPlatform.Gitea     => _gitea,
        GitPlatform.AzureDevOps => _azureDevOps,
        _ => throw new InvalidOperationException($"No adapter is registered for platform '{platform}'.")
    };

    private GitRepoContext RequireContext() =>
        _currentContext ?? throw new InvalidOperationException(
            "Call Resolve() before making API requests.");

    private IGitRepoAdapter RequireAdapter() =>
        _currentAdapter ?? throw new InvalidOperationException(
            "Call Resolve() before making API requests.");
}
