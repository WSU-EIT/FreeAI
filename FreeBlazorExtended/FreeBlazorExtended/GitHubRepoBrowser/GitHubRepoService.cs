/*
    Purpose: HTTP service for calling the GitHub REST API v3 from Blazor WASM.
    Fits: Injected into GitHubRepoBrowser component. Uses fully-qualified URLs so
          the injected HttpClient's BaseAddress is irrelevant.
    Notes: Does NOT dispose the injected HttpClient — DI owns its lifetime.
*/
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace FreeBlazorExtended.GitHubRepoBrowser;

public sealed class GitHubRepoService : IDisposable
{
    private readonly HttpClient _http;
    private const string ApiBase = "https://api.github.com";

    public GitHubRepoService(HttpClient http)
    {
        _http = http;
    }

    // ── Request factory ────────────────────────────────────────────────────────

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string? pat)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("Accept", "application/vnd.github+json");
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(pat))
            req.Headers.Add("Authorization", $"Bearer {pat}");
        return req;
    }

    // ── Path encoding ──────────────────────────────────────────────────────────

    private static string EncodePath(string path)
    {
        return string.Join("/", path.Split('/').Select(Uri.EscapeDataString));
    }

    // ── Error mapping ──────────────────────────────────────────────────────────

    private static GitHubErrorKind MapStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => GitHubErrorKind.Unauthorized,
        HttpStatusCode.Forbidden    => GitHubErrorKind.Unauthorized,
        HttpStatusCode.NotFound     => GitHubErrorKind.NotFound,
        (HttpStatusCode)429         => GitHubErrorKind.RateLimited,
        _                           => GitHubErrorKind.NetworkError
    };

    private static (T?, GitHubBrowseError) HttpError<T>(HttpResponseMessage response) where T : class =>
        (null, new GitHubBrowseError(
            MapStatus(response.StatusCode),
            $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
            (int)response.StatusCode));

    private static (IReadOnlyList<T>, GitHubBrowseError) HttpListError<T>(HttpResponseMessage response) =>
        (Array.Empty<T>(), new GitHubBrowseError(
            MapStatus(response.StatusCode),
            $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
            (int)response.StatusCode));

    // ── Public API ─────────────────────────────────────────────────────────────

    public async Task<(GitHubRepo? Value, GitHubBrowseError? Error)> GetRepoAsync(
        string owner, string repo, string? pat, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}";
            using var req = BuildRequest(HttpMethod.Get, url, pat);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return HttpError<GitHubRepo>(res);
            var data = await res.Content.ReadFromJsonAsync<GitHubRepo>(ct);
            return (data, null);
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) { return (null, new GitHubBrowseError(GitHubErrorKind.NetworkError, ex.Message, null)); }
    }

    public async Task<(IReadOnlyList<GitHubBranch> Value, GitHubBrowseError? Error)> ListBranchesAsync(
        string owner, string repo, string? pat, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/branches?per_page=100";
            using var req = BuildRequest(HttpMethod.Get, url, pat);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return HttpListError<GitHubBranch>(res);
            var data = await res.Content.ReadFromJsonAsync<IReadOnlyList<GitHubBranch>>(ct);
            return (data ?? Array.Empty<GitHubBranch>(), null);
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) { return (Array.Empty<GitHubBranch>(), new GitHubBrowseError(GitHubErrorKind.NetworkError, ex.Message, null)); }
    }

    public async Task<(GitHubTreeResponse? Value, GitHubBrowseError? Error)> GetTreeAsync(
        string owner, string repo, string branchOrSha, string? pat, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/git/trees/{Uri.EscapeDataString(branchOrSha)}?recursive=1";
            using var req = BuildRequest(HttpMethod.Get, url, pat);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return HttpError<GitHubTreeResponse>(res);
            var data = await res.Content.ReadFromJsonAsync<GitHubTreeResponse>(ct);
            return (data, null);
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) { return (null, new GitHubBrowseError(GitHubErrorKind.NetworkError, ex.Message, null)); }
    }

    public async Task<(GitHubFileContent? Value, GitHubBrowseError? Error)> GetFileContentAsync(
        string owner, string repo, string path, string branch, string? pat,
        long? knownSize, CancellationToken ct = default)
    {
        if (knownSize > 1_000_000)
            return (null, new GitHubBrowseError(GitHubErrorKind.FileTooLarge,
                $"File is too large to display ({knownSize:N0} bytes).", null));

        try
        {
            var url = $"{ApiBase}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{EncodePath(path)}?ref={Uri.EscapeDataString(branch)}";
            using var req = BuildRequest(HttpMethod.Get, url, pat);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return HttpError<GitHubFileContent>(res);
            var data = await res.Content.ReadFromJsonAsync<GitHubFileContent>(ct);
            return (data, null);
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) { return (null, new GitHubBrowseError(GitHubErrorKind.NetworkError, ex.Message, null)); }
    }

    public async Task<(IReadOnlyList<GitHubCommit> Value, GitHubBrowseError? Error)> ListCommitsAsync(
        string owner, string repo, string branch, int page, string? pat, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/commits?sha={Uri.EscapeDataString(branch)}&per_page=30&page={page}";
            using var req = BuildRequest(HttpMethod.Get, url, pat);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return HttpListError<GitHubCommit>(res);
            var data = await res.Content.ReadFromJsonAsync<IReadOnlyList<GitHubCommit>>(ct);
            return (data ?? Array.Empty<GitHubCommit>(), null);
        }
        catch (TaskCanceledException) { throw; }
        catch (Exception ex) { return (Array.Empty<GitHubCommit>(), new GitHubBrowseError(GitHubErrorKind.NetworkError, ex.Message, null)); }
    }

    // ── Static utilities ───────────────────────────────────────────────────────

    /// <summary>
    /// Decodes a base64 GitHub content blob (which contains embedded newlines) to a UTF-8 string.
    /// Returns null if the content cannot be decoded (e.g. truly binary with invalid UTF-8).
    /// </summary>
    public static string? DecodeContent(string base64Content)
    {
        try
        {
            var clean = base64Content.Replace("\n", "").Replace("\r", "");
            var bytes = Convert.FromBase64String(clean);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Returns a Prism/highlight.js CSS language class for a given file path.</summary>
    public static string GetLanguageClass(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cs"                       => "language-csharp",
            ".razor" or ".cshtml"       => "language-razor",
            ".html" or ".htm"           => "language-html",
            ".css"                      => "language-css",
            ".scss"                     => "language-scss",
            ".less"                     => "language-less",
            ".js" or ".jsx"             => "language-javascript",
            ".ts" or ".tsx"             => "language-typescript",
            ".json"                     => "language-json",
            ".xml" or ".csproj"
                or ".vbproj" or ".sln"  => "language-xml",
            ".md" or ".mdx"             => "language-markdown",
            ".yaml" or ".yml"           => "language-yaml",
            ".py"                       => "language-python",
            ".sql"                      => "language-sql",
            ".sh" or ".bash"            => "language-bash",
            ".ps1"                      => "language-powershell",
            ".fs" or ".fsx"             => "language-fsharp",
            ".go"                       => "language-go",
            ".rs"                       => "language-rust",
            ".java"                     => "language-java",
            ".rb"                       => "language-ruby",
            ".php"                      => "language-php",
            ".cpp" or ".cc" or ".cxx"   => "language-cpp",
            ".c" or ".h"                => "language-c",
            _                           => "language-plaintext"
        };
    }

    // ── IDisposable ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        // Do NOT dispose the injected HttpClient — DI owns its lifetime.
    }
}
