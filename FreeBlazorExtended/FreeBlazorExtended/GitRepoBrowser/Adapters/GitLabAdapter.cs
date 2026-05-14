/*
    Purpose: GitLab adapter (cloud and self-hosted) using the GitLab REST API v4.
    BaseApiUrl contract: already includes /api/v4 (set by GitPlatformDetector).
    Auth: PRIVATE-TOKEN header when a token is provided; unauthenticated otherwise.
    Notes:
      - {encoded_path} = Uri.EscapeDataString("owner/repo")  →  "owner%2Frepo"
      - File raw endpoint returns raw bytes; size checked via Content-Length before reading.
*/
using System.Text.Json;

namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class GitLabAdapter : IGitRepoAdapter
{
    private readonly HttpClient _http;

    public GitPlatform Platform => GitPlatform.GitLab;

    public GitLabAdapter(HttpClient http) => _http = http;

    // -------------------------------------------------------------------------
    // Public interface
    // -------------------------------------------------------------------------

    public async Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default)
    {
        var encoded = EncodedPath(ctx);

        // Fetch default branch from project info
        using var repoResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/projects/{encoded}", ct);
        await EnsureSuccessAsync(repoResp, "GitLab", ct);
        using var repoDoc  = await JsonDocument.ParseAsync(await repoResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var defaultBranch  = repoDoc.RootElement.TryGetProperty("default_branch", out var db)
                             ? db.GetString() ?? "main" : "main";

        // Fetch branch list
        using var listResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/projects/{encoded}/repository/branches?per_page=100", ct);
        await EnsureSuccessAsync(listResp, "GitLab", ct);
        using var listDoc  = await JsonDocument.ParseAsync(await listResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitBranch>();
        foreach (var b in listDoc.RootElement.EnumerateArray())
        {
            var name      = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var sha       = b.TryGetProperty("commit", out var c) && c.TryGetProperty("id", out var id)
                            ? id.GetString() : null;
            var isDefault = b.TryGetProperty("default", out var def) ? def.GetBoolean() : name == defaultBranch;
            result.Add(new GitBranch(name, sha, isDefault));
        }
        return result;
    }

    public async Task<List<GitTreeNode>> GetTreeAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var encoded  = EncodedPath(ctx);
        var safePath = path?.Trim('/') ?? "";
        var url      = $"{ctx.BaseApiUrl}/projects/{encoded}/repository/tree" +
                       $"?path={Uri.EscapeDataString(safePath)}&ref={Uri.EscapeDataString(branch)}&per_page=100";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "GitLab", ct);
        using var doc  = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitTreeNode>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var type = item.TryGetProperty("type", out var t) ? t.GetString() : "blob";
            result.Add(new GitTreeNode
            {
                Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Path = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "",
                Type = type == "tree" ? GitNodeType.Directory : GitNodeType.File,
                Sha  = item.TryGetProperty("id", out var sha) ? sha.GetString() : null
            });
        }
        return result;
    }

    public async Task<GitFileContent> GetFileContentAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var encoded     = EncodedPath(ctx);
        var encodedFile = Uri.EscapeDataString(path.TrimStart('/'));
        var url         = $"{ctx.BaseApiUrl}/projects/{encoded}/repository/files/{encodedFile}/raw?ref={Uri.EscapeDataString(branch)}";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "GitLab", ct);

        long? size = resp.Content.Headers.ContentLength;
        if (size > 500 * 1024)
            throw new GitRepoException(GitErrorCode.FileTooLarge,
                $"File '{path}' is {size / 1024} KB which exceeds the 500 KB display limit.");

        var bytes    = await resp.Content.ReadAsByteArrayAsync(ct);
        var mimeHint = GetMimeHint(path);

        if (IsBinaryContent(bytes))
            return new GitFileContent(path, null, true, size ?? bytes.Length, mimeHint);

        return new GitFileContent(path, System.Text.Encoding.UTF8.GetString(bytes), false, size ?? bytes.Length, null);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string EncodedPath(GitRepoContext ctx) =>
        Uri.EscapeDataString($"{ctx.Owner}/{ctx.Repo}");

    private Task<HttpResponseMessage> SendAsync(GitRepoContext ctx, string url, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrWhiteSpace(ctx.Credentials.Token))
            req.Headers.TryAddWithoutValidation("PRIVATE-TOKEN", ctx.Credentials.Token);
        return _http.SendAsync(req, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, string platform, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;
        var status = (int)resp.StatusCode;
        var body   = await resp.Content.ReadAsStringAsync(ct);
        var code   = status switch
        {
            401 or 403 => GitErrorCode.Unauthorized,
            404        => GitErrorCode.NotFound,
            429        => GitErrorCode.RateLimited,
            _          => GitErrorCode.NetworkError
        };
        throw new GitRepoException(code, $"{platform} API returned HTTP {status}.", status, body);
    }

    private static bool IsBinaryContent(byte[] bytes)
    {
        var check = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < check; i++)
            if (bytes[i] == 0) return true;
        return false;
    }

    private static string? GetMimeHint(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png"  => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif"  => "image/gif",
            ".webp" => "image/webp",
            ".pdf"  => "application/pdf",
            _       => null
        };
    }
}
