/*
    Purpose: All GitHub API model records for the GitHubRepoBrowser feature.
    Fits: Consumed by GitHubRepoService and the GitHubRepoBrowser component.
*/
using System.Text.Json.Serialization;

namespace FreeBlazorExtended.GitHubRepoBrowser;

public record GitHubRepo(
    [property: JsonPropertyName("full_name")]      string FullName,
    [property: JsonPropertyName("name")]           string Name,
    [property: JsonPropertyName("description")]    string? Description,
    [property: JsonPropertyName("default_branch")] string DefaultBranch,
    [property: JsonPropertyName("private")]        bool Private,
    [property: JsonPropertyName("stargazers_count")] int Stars,
    [property: JsonPropertyName("language")]       string? Language
);

public record GitHubBranchCommitRef(
    [property: JsonPropertyName("sha")] string Sha
);

public record GitHubBranch(
    [property: JsonPropertyName("name")]   string Name,
    [property: JsonPropertyName("commit")] GitHubBranchCommitRef Commit
);

/// <summary>
/// A single entry in a git tree (recursive listing).
/// IMPORTANT: symlinks appear as Type="blob" with Mode="120000" — NOT as Type="symlink".
/// </summary>
public record GitHubTreeItem(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("type")] string Type,      // "blob" | "tree" | "commit"
    [property: JsonPropertyName("sha")]  string? Sha,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("url")]  string? Url
);

public record GitHubTreeResponse(
    [property: JsonPropertyName("sha")]       string Sha,
    [property: JsonPropertyName("url")]       string Url,
    [property: JsonPropertyName("tree")]      IReadOnlyList<GitHubTreeItem> Tree,
    [property: JsonPropertyName("truncated")] bool Truncated
);

/// <summary>
/// Response from the /contents API.
/// Type here is "file" | "dir" | "symlink" | "submodule" — unlike tree items where symlinks are "blob".
/// </summary>
public record GitHubFileContent(
    [property: JsonPropertyName("name")]         string Name,
    [property: JsonPropertyName("path")]         string Path,
    [property: JsonPropertyName("sha")]          string Sha,
    [property: JsonPropertyName("size")]         long Size,
    [property: JsonPropertyName("type")]         string Type,
    [property: JsonPropertyName("content")]      string? Content,
    [property: JsonPropertyName("encoding")]     string? Encoding,
    [property: JsonPropertyName("target")]       string? Target,
    [property: JsonPropertyName("html_url")]     string? HtmlUrl,
    [property: JsonPropertyName("download_url")] string? DownloadUrl
);

public record GitHubCommitAuthor(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("date")] string Date
);

public record GitHubCommitDetail(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("author")]  GitHubCommitAuthor Author
);

public record GitHubCommit(
    [property: JsonPropertyName("sha")]    string Sha,
    [property: JsonPropertyName("commit")] GitHubCommitDetail Commit
);

public record GitHubBrowseError(
    GitHubErrorKind Kind,
    string Message,
    int? HttpStatus
);

public enum GitHubErrorKind
{
    None,
    Unauthorized,
    NotFound,
    RateLimited,
    NetworkError,
    FileTooLarge,
    BinaryFile,
    Truncated
}
