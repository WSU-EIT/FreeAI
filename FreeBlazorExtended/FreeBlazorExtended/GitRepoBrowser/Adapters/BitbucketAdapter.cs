/*
    Purpose: Bitbucket Cloud adapter using the Bitbucket REST API 2.0.
    BaseApiUrl = "https://api.bitbucket.org" (no version prefix — adapter adds /2.0/).
    Auth: Basic (username:app-password encoded) or Bearer token.
    Notes:
      - Paginated responses use the "values" array.
      - Tree items have type "commit_file" | "commit_directory".
      - File raw endpoint returns bytes directly.
*/
using System.Net.Http.Headers;
using System.Text.Json;

namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class BitbucketAdapter : IGitRepoAdapter
{
    private readonly HttpClient _http;

    public GitPlatform Platform => GitPlatform.Bitbucket;

    public BitbucketAdapter(HttpClient http) => _http = http;

    // -------------------------------------------------------------------------
    // Public interface
    // -------------------------------------------------------------------------

    public async Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default)
    {
        // Fetch main branch name from repo metadata
        using var repoResp = await SendAsync(ctx, $"{ctx.BaseApiUrl}/2.0/repositories/{ctx.Owner}/{ctx.Repo}", ct);
        await EnsureSuccessAsync(repoResp, "Bitbucket", ct);
        using var repoDoc  = await JsonDocument.ParseAsync(await repoResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var defaultBranch  = repoDoc.RootElement.TryGetProperty("mainbranch", out var mb)
                             && mb.TryGetProperty("name", out var mbn)
                             ? mbn.GetString() ?? "main" : "main";

        // Fetch branch list (first page — 100 items)
        using var listResp = await SendAsync(ctx,
            $"{ctx.BaseApiUrl}/2.0/repositories/{ctx.Owner}/{ctx.Repo}/refs/branches?pagelen=100", ct);
        await EnsureSuccessAsync(listResp, "Bitbucket", ct);
        using var listDoc  = await JsonDocument.ParseAsync(await listResp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitBranch>();
        if (listDoc.RootElement.TryGetProperty("values", out var values))
        {
            foreach (var b in values.EnumerateArray())
            {
                var name = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var sha  = b.TryGetProperty("target", out var target) && target.TryGetProperty("hash", out var hash)
                           ? hash.GetString() : null;
                result.Add(new GitBranch(name, sha, name == defaultBranch));
            }
        }
        return result;
    }

    public async Task<List<GitTreeNode>> GetTreeAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var safePath = path?.Trim('/') ?? "";
        // Bitbucket src endpoint: branch in path, trailing slash requests directory listing
        var url = string.IsNullOrEmpty(safePath)
            ? $"{ctx.BaseApiUrl}/2.0/repositories/{ctx.Owner}/{ctx.Repo}/src/{Uri.EscapeDataString(branch)}/?pagelen=100"
            : $"{ctx.BaseApiUrl}/2.0/repositories/{ctx.Owner}/{ctx.Repo}/src/{Uri.EscapeDataString(branch)}/{safePath}/?pagelen=100";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Bitbucket", ct);
        using var doc  = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitTreeNode>();
        if (doc.RootElement.TryGetProperty("values", out var values))
        {
            foreach (var item in values.EnumerateArray())
            {
                var type     = item.TryGetProperty("type", out var t) ? t.GetString() : "commit_file";
                var itemPath = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                result.Add(new GitTreeNode
                {
                    Name = System.IO.Path.GetFileName(itemPath) ?? itemPath,
                    Path = itemPath,
                    Type = type == "commit_directory" ? GitNodeType.Directory : GitNodeType.File,
                    Size = item.TryGetProperty("size", out var sz) ? (long?)sz.GetInt64() : null
                });
            }
        }
        return result;
    }

    public async Task<GitFileContent> GetFileContentAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        var safePath = path.TrimStart('/');
        var url      = $"{ctx.BaseApiUrl}/2.0/repositories/{ctx.Owner}/{ctx.Repo}/src/{Uri.EscapeDataString(branch)}/{safePath}";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Bitbucket", ct);

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
        var req = new HttpRequestMessage(HttpMethod.Get, url);
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
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.Token);
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
