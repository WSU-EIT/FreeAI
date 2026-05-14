/*
    Purpose: Server-side proxy for Git repository API calls.
    Supports: GitHub, GitHub Enterprise, GitLab, Bitbucket, Gitea, Azure DevOps.
    Security: Credentials are accepted once via POST /connect, stored in ExternalApiSessionStore,
              and never returned to the browser. All subsequent requests use an HttpOnly cookie.
    Adapters: Reuses FreeBlazorExtended.GitRepoBrowser adapters (GitHubAdapter, etc.) which are
              accessible because the server project references FreeBlazorExample.Client.csproj → FreeBlazorExtended.
*/
using FreeBlazorExtended.GitRepoBrowser;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FreeBlazorExample.Server.Controllers;

[ApiController]
[Route("api/Showcase/GitRepo")]
public sealed class GitRepoController : ControllerBase
{
    private static readonly System.Net.Http.HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "FreeBlazorExtended-Showcase/1.0" } }
    };

    private readonly ExternalApiSessionStore _store;

    public GitRepoController(ExternalApiSessionStore store)
    {
        _store = store;
    }

    // -------------------------------------------------------------------------
    // POST /api/Showcase/GitRepo/connect
    // Body: { "repoUrl": "...", "token": "...", "username": "...", "platformOverride": "..." }
    // Detects platform, verifies connectivity (if possible), stores context in session.
    // Returns: { connected, owner, repo, platform, defaultBranch }
    // -------------------------------------------------------------------------
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] GitConnectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RepoUrl))
            return BadRequest(new { error = "RepoUrl is required." });

        GitPlatform? platformOverride = null;
        if (!string.IsNullOrWhiteSpace(req.PlatformOverride) &&
            Enum.TryParse<GitPlatform>(req.PlatformOverride, out var po) &&
            po != GitPlatform.Unknown)
            platformOverride = po;

        (GitPlatform platform, string owner, string repo, string baseApiUrl) detected;
        try
        {
            detected = GitPlatformDetector.Detect(req.RepoUrl.Trim(), platformOverride);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        var authMethod = !string.IsNullOrWhiteSpace(req.Username) && !string.IsNullOrWhiteSpace(req.Token)
                         ? GitAuthMethod.UsernameAndPassword
                         : !string.IsNullOrWhiteSpace(req.Token)
                             ? GitAuthMethod.PersonalAccessToken
                             : GitAuthMethod.None;

        var credentials = new GitCredentials(req.Username, req.Token, authMethod);
        var ctx         = new GitRepoContext(req.RepoUrl, detected.owner, detected.repo,
                              detected.platform, credentials, detected.baseApiUrl);

        // Quick connectivity check — try listing branches
        var adapter = BuildAdapter(detected.platform);
        string? defaultBranch = null;
        try
        {
            var branches = await adapter.GetBranchesAsync(ctx, HttpContext.RequestAborted);
            defaultBranch = branches.FirstOrDefault(b => b.IsDefault)?.Name
                         ?? branches.FirstOrDefault()?.Name;
        }
        catch (GitRepoException ex) when (ex.ErrorCode == GitErrorCode.Unauthorized)
        {
            return StatusCode(401, new { error = "Invalid or insufficient credentials for this repository." });
        }
        catch (GitRepoException ex) when (ex.ErrorCode == GitErrorCode.NotFound)
        {
            return NotFound(new { error = "Repository not found. Check the URL and that your token has access." });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = $"Could not reach repository: {ex.Message}" });
        }

        // Store in session
        var sessionKey = _store.Create(s =>
        {
            s.GitRepoUrl    = req.RepoUrl;
            s.GitToken      = req.Token;
            s.GitUsername   = req.Username;
            s.GitPlatform   = detected.platform.ToString();
            s.GitOwner      = detected.owner;
            s.GitRepo       = detected.repo;
            s.GitBaseApiUrl = detected.baseApiUrl;
        });

        Response.Cookies.Append(ExternalApiSessionStore.CookieName, sessionKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure   = Request.IsHttps,
            MaxAge   = TimeSpan.FromHours(2),
            Path     = "/api/Showcase"
        });

        return Ok(new
        {
            connected     = true,
            owner         = detected.owner,
            repo          = detected.repo,
            platform      = detected.platform.ToString(),
            defaultBranch
        });
    }

    // -------------------------------------------------------------------------
    // GET /api/Showcase/GitRepo/branches
    // -------------------------------------------------------------------------
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var (ctx, adapter) = GetSessionContext();
        if (ctx is null) return Unauthorized(new { error = "No active Git session. Connect first." });

        try
        {
            var branches = await adapter!.GetBranchesAsync(ctx, HttpContext.RequestAborted);
            return Ok(branches.Select(b => new { b.Name, b.CommitSha, b.IsDefault }));
        }
        catch (GitRepoException ex) { return StatusCode(MapErrorCode(ex.ErrorCode), new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(502, new { error = ex.Message }); }
    }

    // -------------------------------------------------------------------------
    // GET /api/Showcase/GitRepo/tree?path=&branch=
    // -------------------------------------------------------------------------
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree([FromQuery] string path = "", [FromQuery] string branch = "")
    {
        var (ctx, adapter) = GetSessionContext();
        if (ctx is null) return Unauthorized(new { error = "No active Git session. Connect first." });

        try
        {
            var nodes = await adapter!.GetTreeAsync(ctx, path, branch, HttpContext.RequestAborted);
            return Ok(nodes.Select(n => new
            {
                n.Name,
                n.Path,
                type        = n.Type.ToString(),
                n.Sha,
                n.Size,
                n.DownloadUrl
            }));
        }
        catch (GitRepoException ex) { return StatusCode(MapErrorCode(ex.ErrorCode), new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(502, new { error = ex.Message }); }
    }

    // -------------------------------------------------------------------------
    // GET /api/Showcase/GitRepo/file?path=&branch=
    // -------------------------------------------------------------------------
    [HttpGet("file")]
    public async Task<IActionResult> GetFile([FromQuery] string path = "", [FromQuery] string branch = "")
    {
        var (ctx, adapter) = GetSessionContext();
        if (ctx is null) return Unauthorized(new { error = "No active Git session. Connect first." });

        try
        {
            var content = await adapter!.GetFileContentAsync(ctx, path, branch, HttpContext.RequestAborted);
            return Ok(new
            {
                content.Path,
                content.RawText,
                content.IsBinary,
                content.Size,
                content.MimeTypeHint
            });
        }
        catch (GitRepoException ex) when (ex.ErrorCode == GitErrorCode.FileTooLarge)
        {
            return StatusCode(413, new { error = ex.Message });
        }
        catch (GitRepoException ex) { return StatusCode(MapErrorCode(ex.ErrorCode), new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(502, new { error = ex.Message }); }
    }

    // -------------------------------------------------------------------------
    // DELETE /api/Showcase/GitRepo/disconnect
    // -------------------------------------------------------------------------
    [HttpDelete("disconnect")]
    public IActionResult Disconnect()
    {
        var key = Request.Cookies[ExternalApiSessionStore.CookieName];
        _store.Remove(key);
        Response.Cookies.Delete(ExternalApiSessionStore.CookieName,
            new CookieOptions { Path = "/api/Showcase" });
        return Ok(new { disconnected = true });
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private (GitRepoContext? ctx, IGitRepoAdapter? adapter) GetSessionContext()
    {
        var key     = Request.Cookies[ExternalApiSessionStore.CookieName];
        var session = _store.Get(key);
        if (session?.GitPlatform is null) return (null, null);

        if (!Enum.TryParse<GitPlatform>(session.GitPlatform, out var platform))
            return (null, null);

        var authMethod = !string.IsNullOrWhiteSpace(session.GitUsername) && !string.IsNullOrWhiteSpace(session.GitToken)
                         ? GitAuthMethod.UsernameAndPassword
                         : !string.IsNullOrWhiteSpace(session.GitToken)
                             ? GitAuthMethod.PersonalAccessToken
                             : GitAuthMethod.None;

        var creds = new GitCredentials(session.GitUsername, session.GitToken, authMethod);
        var ctx   = new GitRepoContext(session.GitRepoUrl!, session.GitOwner!, session.GitRepo!,
                        platform, creds, session.GitBaseApiUrl!);

        return (ctx, BuildAdapter(platform));
    }

    private static IGitRepoAdapter BuildAdapter(GitPlatform platform) => platform switch
    {
        GitPlatform.GitHub or GitPlatform.GitHubEnterprise => new GitHubAdapter(_http),
        GitPlatform.GitLab    => new GitLabAdapter(_http),
        GitPlatform.Bitbucket => new BitbucketAdapter(_http),
        GitPlatform.Gitea     => new GiteaAdapter(_http),
        GitPlatform.AzureDevOps => new AzureDevOpsAdapter(_http),
        _ => throw new InvalidOperationException($"No adapter for platform '{platform}'.")
    };

    private static int MapErrorCode(GitErrorCode code) => code switch
    {
        GitErrorCode.Unauthorized => 401,
        GitErrorCode.NotFound     => 404,
        GitErrorCode.RateLimited  => 429,
        GitErrorCode.FileTooLarge => 413,
        _                         => 502
    };

    // ── Request models ───────────────────────────────────────────────────────

    public record GitConnectRequest(
        string  RepoUrl,
        string? Token            = null,
        string? Username         = null,
        string? PlatformOverride = null);
}
