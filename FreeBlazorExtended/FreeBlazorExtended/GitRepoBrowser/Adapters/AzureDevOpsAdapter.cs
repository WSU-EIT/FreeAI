/*
    Purpose: Azure DevOps (dev.azure.com) adapter using the Azure DevOps REST API 7.1.
    Auth:    Personal Access Token (PAT) passed as Basic auth — username is ignored,
             the PAT is used as the password per Microsoft's documented pattern.
    URLs:    https://dev.azure.com/{organization}/{project}/_git/{repo}
    Notes:
      - Tree listing uses the Items API with recursionLevel=oneLevel.
      - File content uses the Items API with format=text (raw download).
      - Branch listing uses the Refs API filtered to heads/.
      - CORS: Azure DevOps APIs allow browser CORS from any origin with a valid PAT.
*/
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FreeBlazorExtended.GitRepoBrowser;

public sealed class AzureDevOpsAdapter : IGitRepoAdapter
{
    private readonly HttpClient _http;

    public GitPlatform Platform => GitPlatform.AzureDevOps;

    public AzureDevOpsAdapter(HttpClient http) => _http = http;

    // -------------------------------------------------------------------------
    // IGitRepoAdapter implementation
    // -------------------------------------------------------------------------

    public async Task<List<GitBranch>> GetBranchesAsync(GitRepoContext ctx, CancellationToken ct = default)
    {
        // GET https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}/refs?filter=heads/&api-version=7.1
        var url = $"{BuildRepoBase(ctx)}/refs?filter=heads/&api-version=7.1";
        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Azure DevOps", ct);
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitBranch>();
        if (doc.RootElement.TryGetProperty("value", out var values))
        {
            // Azure DevOps doesn't directly expose the "default" branch in the refs API.
            // We'll mark "main" or "master" as default if present, else the first branch.
            string? defaultName = null;

            foreach (var b in values.EnumerateArray())
            {
                // name is "refs/heads/main" — strip the prefix
                var fullName = b.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var shortName = fullName.StartsWith("refs/heads/", StringComparison.Ordinal)
                    ? fullName["refs/heads/".Length..] : fullName;
                var sha = b.TryGetProperty("objectId", out var oid) ? oid.GetString() : null;

                defaultName ??= shortName; // first branch as fallback default
                result.Add(new GitBranch(shortName, sha, IsDefault: false));
            }

            // Mark the default
            if (result.Count > 0)
            {
                var preferred = result.FirstOrDefault(b => b.Name == "main")
                             ?? result.FirstOrDefault(b => b.Name == "master")
                             ?? result[0];
                result = result.Select(b => b with { IsDefault = b.Name == preferred.Name }).ToList();
            }
        }
        return result;
    }

    public async Task<List<GitTreeNode>> GetTreeAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        // GET .../items?scopePath=/{path}&recursionLevel=oneLevel&versionDescriptor.version={branch}&api-version=7.1
        var scopePath = string.IsNullOrEmpty(path?.Trim('/')) ? "/" : $"/{path.Trim('/')}";
        var url = $"{BuildRepoBase(ctx)}/items" +
                  $"?scopePath={Uri.EscapeDataString(scopePath)}" +
                  $"&recursionLevel=oneLevel" +
                  $"&versionDescriptor.version={Uri.EscapeDataString(branch)}" +
                  $"&versionDescriptor.versionType=branch" +
                  $"&api-version=7.1";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Azure DevOps", ct);
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var result = new List<GitTreeNode>();
        if (doc.RootElement.TryGetProperty("value", out var values))
        {
            foreach (var item in values.EnumerateArray())
            {
                var itemPath = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                var isFolder = item.TryGetProperty("isFolder", out var f) && f.GetBoolean();
                var contentUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;

                // Skip the root entry itself (same path as scopePath)
                if (itemPath.TrimEnd('/') == scopePath.TrimEnd('/'))
                    continue;

                var name = itemPath.Split('/').Last(s => !string.IsNullOrEmpty(s));

                result.Add(new GitTreeNode
                {
                    Name = name,
                    Path = itemPath.TrimStart('/'),
                    Type = isFolder ? GitNodeType.Directory : GitNodeType.File,
                    DownloadUrl = isFolder ? null : contentUrl,
                });
            }
        }

        // Sort: directories first, then files, both alphabetical
        return result
            .OrderBy(n => n.Type == GitNodeType.File ? 1 : 0)
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<GitFileContent> GetFileContentAsync(
        GitRepoContext ctx, string path, string branch, CancellationToken ct = default)
    {
        // GET .../items?path=/{path}&versionDescriptor.version={branch}&$format=octetStream&api-version=7.1
        var safePath = $"/{path.TrimStart('/')}";
        var url = $"{BuildRepoBase(ctx)}/items" +
                  $"?path={Uri.EscapeDataString(safePath)}" +
                  $"&versionDescriptor.version={Uri.EscapeDataString(branch)}" +
                  $"&versionDescriptor.versionType=branch" +
                  $"&$format=octetStream" +
                  $"&api-version=7.1";

        using var resp = await SendAsync(ctx, url, ct);
        await EnsureSuccessAsync(resp, "Azure DevOps", ct);

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

        // Guard oversized files
        if (bytes.Length > 512 * 1024)
            throw new GitRepoException(GitErrorCode.FileTooLarge,
                $"File '{path}' ({bytes.Length / 1024} KB) exceeds the 512 KB display limit.");

        bool isBinary = IsBinary(bytes);
        string? rawText = isBinary ? null : Encoding.UTF8.GetString(bytes);
        var mimeHint = GetMimeHint(path);

        return new GitFileContent(path, rawText, isBinary, bytes.Length, mimeHint);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the per-repo API base from the context.
    /// Azure DevOps URL format: https://dev.azure.com/{org}/{project}/_git/{repo}
    /// API base:                https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}
    /// </summary>
    private static string BuildRepoBase(GitRepoContext ctx)
    {
        // ctx.Owner holds org, ctx.Repo holds repo; project may be encoded in the URL path.
        // GitPlatformDetector sets BaseApiUrl to "https://dev.azure.com".
        // We expect the RepoUrl to be https://dev.azure.com/{org}/{project}/_git/{repo}.
        // Parse org, project, repo from the URL segments.
        if (Uri.TryCreate(ctx.RepoUrl, UriKind.Absolute, out var uri))
        {
            var segs = uri.AbsolutePath.Trim('/').Split('/');
            // Expected: segs[0]=org, segs[1]=project, segs[2]="_git", segs[3]=repo
            if (segs.Length >= 4 && segs[2].Equals("_git", StringComparison.OrdinalIgnoreCase))
            {
                var org     = segs[0];
                var project = segs[1];
                var repo    = segs[3];
                return $"https://dev.azure.com/{org}/{project}/_apis/git/repositories/{repo}";
            }
        }
        // Fallback using Owner/Repo from context (org/repo, no project)
        return $"https://dev.azure.com/{ctx.Owner}/{ctx.Repo}/_apis/git/repositories/{ctx.Repo}";
    }

    private async Task<HttpResponseMessage> SendAsync(GitRepoContext ctx, string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Azure DevOps uses Basic auth: username can be anything, password = PAT
        if (!string.IsNullOrEmpty(ctx.Credentials.Token))
        {
            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{ctx.Credentials.Token}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
        }

        return await _http.SendAsync(req, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp, string platform, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode) return;

        var code = resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? GitErrorCode.Unauthorized
            : resp.StatusCode == System.Net.HttpStatusCode.NotFound
                ? GitErrorCode.NotFound
                : GitErrorCode.NetworkError;

        var body = await resp.Content.ReadAsStringAsync(ct);
        throw new GitRepoException(code, $"{platform} API error {(int)resp.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
    }

    private static bool IsBinary(byte[] bytes)
    {
        var scanLen = Math.Min(bytes.Length, 8192);
        for (var i = 0; i < scanLen; i++)
            if (bytes[i] == 0) return true;
        return false;
    }

    private static string? GetMimeHint(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cs" or ".razor" or ".csx" => "text/csharp",
            ".js" or ".ts" or ".jsx" or ".tsx" => "text/javascript",
            ".json" => "application/json",
            ".xml" or ".csproj" or ".props" or ".targets" => "text/xml",
            ".md" or ".markdown" => "text/markdown",
            ".html" or ".htm" => "text/html",
            ".css" or ".scss" or ".less" => "text/css",
            ".py" => "text/python",
            ".sh" or ".bash" => "text/shell",
            ".yml" or ".yaml" => "text/yaml",
            _ => "text/plain"
        };
    }
}
