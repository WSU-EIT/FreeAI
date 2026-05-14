/*
    Purpose: All domain models, enums, and exceptions for the GenericGitRepoBrowser feature.
    Covers: Platform identifiers, auth methods, tree nodes, file content, and a typed exception.
    Used by: IGitRepoAdapter implementations, GitRepoBrowserService, GenericGitRepoBrowser component.
*/
namespace FreeBlazorExtended.GitRepoBrowser;

public enum GitPlatform { Unknown, GitHub, GitHubEnterprise, GitLab, Bitbucket, Gitea, AzureDevOps }
public enum GitNodeType { File, Directory, Submodule, Symlink }
public enum GitAuthMethod { None, PersonalAccessToken, UsernameAndPassword, OAuthBearer }
public enum GitErrorCode { None, Unauthorized, NotFound, RateLimited, NetworkError, ParseError, FileTooLarge, CorsBlocked }

/// <summary>Holds authentication credentials — never embedded in API URLs.</summary>
public record GitCredentials(string? Username, string? Token, GitAuthMethod Method);

/// <summary>
/// Resolved context for a single repository session.
/// BaseApiUrl contains no credentials; it is safe to log or display.
/// </summary>
public record GitRepoContext(
    string RepoUrl,
    string Owner,
    string Repo,
    GitPlatform Platform,
    GitCredentials Credentials,
    string BaseApiUrl);

/// <summary>A branch reference returned by the platform API.</summary>
public record GitBranch(string Name, string? CommitSha, bool IsDefault);

/// <summary>
/// A node in the repository file tree.
/// Mutable by design so the UI can toggle IsExpanded and lazily populate Children.
/// </summary>
public class GitTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public GitNodeType Type { get; set; }
    public string? Sha { get; set; }
    public long? Size { get; set; }
    public string? DownloadUrl { get; set; }

    /// <summary>Whether the directory node is currently expanded in the UI.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>Null until the directory has been lazy-loaded; empty list means loaded but empty.</summary>
    public List<GitTreeNode>? Children { get; set; }
}

/// <summary>The decoded content of a file retrieved from a repository.</summary>
public record GitFileContent(
    string Path,
    string? RawText,
    bool IsBinary,
    long? Size,
    string? MimeTypeHint);

/// <summary>Typed exception thrown by adapters and the browser service.</summary>
public class GitRepoException : Exception
{
    public GitErrorCode ErrorCode { get; }
    public int? HttpStatus { get; }
    public string? PlatformMessage { get; }

    public GitRepoException(
        GitErrorCode errorCode,
        string message,
        int? httpStatus = null,
        string? platformMsg = null,
        Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        HttpStatus = httpStatus;
        PlatformMessage = platformMsg;
    }
}
