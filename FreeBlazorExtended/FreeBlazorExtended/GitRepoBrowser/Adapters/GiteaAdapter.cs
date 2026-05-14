/*
    Purpose: Gitea (and Gogs-compatible) adapter for self-hosted instances.
    BaseApiUrl contract: already includes /api/v1 (set by GitPlatformDetector).
    Auth: "Authorization: token {PAT}" for personal access tokens;
          Basic base64(username:password) for username+password auth.
    Notes:
      - Contents endpoint returns array for directories (same shape as GitHub).
      - Raw file endpoint: GET /repos/{owner}/{repo}/raw/{filepath}?ref={branch}
*/
using System.Net.Http.Headers;
using System.Text.Json;

namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class GiteaAdapter : IGitRepoAdapter
{
    private readonly HttpClient _http;

    public GitPlatform Platform => GitPlatform.Gitea;

    public GiteaAdapter(HttpClient http) => _http = http;

    // -------------------------------------------------------------------------
    // Public interface
    // -------------------------------------------------------------------------

    public async Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default)
    {
        // Fetch default branch
        using var repoResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}", ct);
        await EnsureSuccessAsync(repoResp, "Gitea", ct);
        using var repoDoc  = await JsonDocument.ParseAsync(await repoResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var defaultBranch  = repoDoc.RootElement.TryGetProperty("default_branch", out var db)
                             ? db.GetString() ?? "main" : "main";

        // Fetch branch list
        using var listResp = await SendAsync(ctx,
            $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/branches?limit=50", ct);
        await EnsureSuccessAsync(listResp, "Gitea", ct);
        using var listDoc  = await JsonDocument.ParseAsync(await listResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitBranch>();
        foreach (var b in listDoc.RootElement.EnumerateArray())
        {
            var name = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var sha  = b.TryGetProperty("commit", out var c) && c.TryGetProperty("id", out var id)
                       ? id.GetString() : null;
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
        await EnsureSuccessAsync(resp, "Gitea", ct);
        using var doc  = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitTreeNode>();
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var t) ? t.GetString() : "file";
                result.Add(new GitTreeNode
                {
                    Name        = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Path        = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "",
                    Type        = type switch
                                  {
                                      "dir"       => GitNodeType.Directory,
                                      "symlink"   => GitNodeType.Symlink,
                                      "submodule" => GitNodeType.Submodule,
                                      _           => GitNodeType.File
                                  },
                    Sha         = item.TryGetProperty("sha", out var sha) ? sha.GetString() : null,
                    Size        = item.TryGetProperty("size", out var sz) ? (long?)sz.GetInt64() : null,
                    DownloadUrl = item.TryGetProperty("download_url", out var dl) ? dl.GetString() : null
                });
            }
        }
        return result;
    }

    public async Task<GitFileContent> GetFileContentAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        // Gitea raw file: /api/v1/repos/{owner}/{repo}/raw/{filepath}?ref={branch}
        var safePath = path.TrimStart('/');
        var url      = $"{ctx.BaseApiUrl}/repos/{ctx.Owner}/{ctx.Repo}/raw/{safePath}?ref={Uri.EscapeDataString(branch)}";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Gitea", ct);

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

    private Task<HttpResponseMessage> SendAsync(GitRepoContext ctx, string url, CancellationToken ct)
    {
        var req   = new HttpRequestMessage(HttpMethod.Get, url);
        var creds = ctx.Credentials;

        if (creds.Method == GitAuthMethod.UsernameAndPassword &&
            !string.IsNullOrWhiteSpace(creds.Username) &&
            !string.IsNullOrWhiteSpace(creds.Token))
        {
            var encoded = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{creds.Username}:{creds.Token}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }
        else if (!string.IsNullOrWhiteSpace(creds.Token))
        {
            // Gitea PAT format: "token {PAT}"
            req.Headers.TryAddWithoutValidation("Authorization", $"token {creds.Token}");
        }

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
