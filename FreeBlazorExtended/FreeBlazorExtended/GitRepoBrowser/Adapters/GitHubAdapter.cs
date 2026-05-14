/*
    Purpose: GitHub and GitHub Enterprise adapter using the Contents REST API.
    API version: 2022-11-28
    Notes:
      - All headers are set per-request (not on DefaultRequestHeaders) to be safe with shared HttpClient.
      - Files >500 KB trigger FileTooLarge before reading content.
      - Binary detection: scan first 8 KB for null bytes.
*/
using System.Net.Http.Headers;
using System.Text.Json;

namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class GitHubAdapter : IGitRepoAdapter
{
    private readonly HttpClient _http;

    public GitPlatform Platform => GitPlatform.GitHub;  // Also handles GitHubEnterprise

    public GitHubAdapter(HttpClient http) => _http = http;

    // -------------------------------------------------------------------------
    // Public interface
    // -------------------------------------------------------------------------

    public async Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default)
    {
        // Fetch the default branch name first
        using var repoResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}", ct);
        await EnsureSuccessAsync(repoResp, "GitHub", ct);
        using var repoDoc  = await JsonDocument.ParseAsync(await repoResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var defaultBranch  = repoDoc.RootElement.TryGetProperty("default_branch", out var db)
                             ? db.GetString() ?? "main" : "main";

        // Fetch branch list (first page — 100 items)
        using var listResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/branches?per_page=100", ct);
        await EnsureSuccessAsync(listResp, "GitHub", ct);
        using var listDoc  = await JsonDocument.ParseAsync(await listResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitBranch>();
        foreach (var b in listDoc.RootElement.EnumerateArray())
        {
            var name = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var sha  = b.TryGetProperty("commit", out var c) && c.TryGetProperty("sha", out var s)
                       ? s.GetString() : null;
            result.Add(new GitBranch(name, sha, name == defaultBranch));
        }
        return result;
    }

    public async Task<List<GitTreeNode>> GetTreeAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var safePath = path?.Trim('/') ?? "";
        var url = string.IsNullOrEmpty(safePath)
            ? $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/contents?ref={Uri.EscapeDataString(branch)}"
            : $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/contents/{safePath}?ref={Uri.EscapeDataString(branch)}";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "GitHub", ct);
        using var doc  = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitTreeNode>();
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
                result.Add(MapToNode(item));
        }
        return result;
    }

    public async Task<GitFileContent> GetFileContentAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var safePath = path.Trim('/');
        var url      = $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/contents/{safePath}?ref={Uri.EscapeDataString(branch)}";

        using var resp = await SendAsync(ctx, url, ct);

        // Check rate-limit header before raising the error
        if (resp.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
            int.TryParse(remaining.FirstOrDefault(), out var rem) && rem == 0)
            throw new GitRepoException(GitErrorCode.RateLimited,
                "GitHub API rate limit exceeded. Provide a Personal Access Token to increase your quota.");

        await EnsureSuccessAsync(resp, "GitHub", ct);
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var size = doc.RootElement.TryGetProperty("size", out var sz) ? (long?)sz.GetInt64() : null;
        if (size > 500 * 1024)
            throw new GitRepoException(GitErrorCode.FileTooLarge,
                $"File '{path}' is {size / 1024} KB which exceeds the 500 KB display limit.");

        var mimeHint = GetMimeHint(path);

        if (doc.RootElement.TryGetProperty("encoding", out var enc) && enc.GetString() == "base64" &&
            doc.RootElement.TryGetProperty("content", out var contentEl))
        {
            var raw     = contentEl.GetString() ?? string.Empty;
            var cleaned = raw.Replace("\n", "").Replace("\r", "");
            var bytes   = Convert.FromBase64String(cleaned);
            if (IsBinaryContent(bytes))
                return new GitFileContent(path, null, true, size, mimeHint);
            return new GitFileContent(path, System.Text.Encoding.UTF8.GetString(bytes), false, size, mimeHint);
        }

        return new GitFileContent(path, null, true, size, mimeHint);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private Task<HttpResponseMessage> SendAsync(GitRepoContext ctx, string url, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");
        req.Headers.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(ctx.Credentials.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.Credentials.Token);
        return _http.SendAsync(req, ct);
    }

    private static GitTreeNode MapToNode(JsonElement item)
    {
        var type = item.TryGetProperty("type", out var t) ? t.GetString() : "file";
        return new GitTreeNode
        {
            Name        = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            Path        = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "",
            Type        = type switch
                          {
                              "dir"       => GitNodeType.Directory,
                              "submodule" => GitNodeType.Submodule,
                              "symlink"   => GitNodeType.Symlink,
                              _           => GitNodeType.File
                          },
            Sha         = item.TryGetProperty("sha", out var sha) ? sha.GetString() : null,
            Size        = item.TryGetProperty("size", out var sz) ? (long?)sz.GetInt64() : null,
            DownloadUrl = item.TryGetProperty("download_url", out var dl) ? dl.GetString() : null
        };
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
            ".zip"  => "application/zip",
            _       => null
        };
    }
}
