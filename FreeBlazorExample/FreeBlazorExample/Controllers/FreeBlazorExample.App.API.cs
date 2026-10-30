using FreeBlazorExtended.GitRepoBrowser;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Headers;

namespace FreeBlazorExample.Server.Controllers;

// ---- API.GitRepo ----

/*
    Purpose: Server-side proxy for Git repository API calls.
    Supports: GitHub, GitHub Enterprise, GitLab, Bitbucket, Gitea, Azure DevOps.
    Security: Credentials are accepted once via POST /connect, stored in FreeBlazorExampleExternalApiSessionStore,
              and never returned to the browser. All subsequent requests use an HttpOnly cookie.
    Adapters: Reuses FreeBlazorExtended.GitRepoBrowser adapters (GitHubAdapter, etc.) which are
              accessible because the server project references FreeBlazorExample.Client.csproj → FreeBlazorExtended.
*/

[ApiController]
[Route("api/Showcase/GitRepo")]
public sealed class GitRepoController : ControllerBase
{
    private static readonly System.Net.Http.HttpClient _http = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "FreeBlazorExtended-Showcase/1.0" } }
    };

    private readonly FreeBlazorExampleExternalApiSessionStore _store;

    public GitRepoController(FreeBlazorExampleExternalApiSessionStore store)
    {
        _store = store;
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

        _store.IssueCookie(Request, Response, sessionKey);

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
    // DELETE /api/Showcase/GitRepo/disconnect
    // -------------------------------------------------------------------------
    [HttpDelete("disconnect")]
    public IActionResult Disconnect()
    {
        _store.ClearCookie(Request, Response);
        return Ok(new { disconnected = true });
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
    // Private helpers
    // -------------------------------------------------------------------------

    private (GitRepoContext? ctx, IGitRepoAdapter? adapter) GetSessionContext()
    {
        var session = _store.GetSession(Request);
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

// ---- API.Smartsheet ----

/*
    Purpose: Server-side proxy for Smartsheet REST API v2.
    Security: The API token is accepted once via POST /connect, stored server-side in
              FreeBlazorExampleExternalApiSessionStore, and returned to the browser only as an HttpOnly
              session cookie. All subsequent data requests carry only that cookie.
    Pattern:  Browser → POST /api/Showcase/Smartsheet/connect (token in body, HTTPS)
              Browser → GET  /api/Showcase/Smartsheet/sheets  (cookie only)
              Tokens never appear in GET URLs or response bodies.
*/

[ApiController]
[Route("api/Showcase/Smartsheet")]
public sealed class SmartsheetController : ControllerBase
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.smartsheet.com/2.0/"),
        DefaultRequestHeaders = { { "User-Agent", "FreeBlazorExtended-Showcase/1.0" } }
    };

    private readonly FreeBlazorExampleExternalApiSessionStore _store;

    public SmartsheetController(FreeBlazorExampleExternalApiSessionStore store)
    {
        _store = store;
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl, string token)
    {
        var req = new HttpRequestMessage(method, relativeUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    // -------------------------------------------------------------------------
    // POST /api/Showcase/Smartsheet/connect
    // Body: { "token": "..." }
    // Sets HttpOnly session cookie. Returns connected user info (no token echoed).
    // -------------------------------------------------------------------------
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token))
            return BadRequest(new { error = "Token is required." });

        // Verify the token works and get the user's email
        string? email = null;
        using var verifyReq = BuildRequest(HttpMethod.Get, "users/me", req.Token);
        try
        {
            using var resp = await _http.SendAsync(verifyReq, HttpContext.RequestAborted);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return StatusCode((int)resp.StatusCode,
                    new { error = $"Smartsheet rejected the token ({(int)resp.StatusCode}). Check that it is valid and has not expired." });
            }

            using var doc = await JsonDocument.ParseAsync(
                await resp.Content.ReadAsStreamAsync(), cancellationToken: HttpContext.RequestAborted);
            email = doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = $"Could not reach Smartsheet API: {ex.Message}" });
        }

        // Store token server-side and issue session cookie
        var sessionKey = _store.Create(s => s.SmartsheetToken = req.Token);
        _store.IssueCookie(Request, Response, sessionKey);

        return Ok(new { connected = true, email });
    }

    // -------------------------------------------------------------------------
    // DELETE /api/Showcase/Smartsheet/disconnect
    // Clears the server session and the cookie.
    // -------------------------------------------------------------------------
    [HttpDelete("disconnect")]
    public IActionResult Disconnect()
    {
        _store.ClearCookie(Request, Response);
        return Ok(new { disconnected = true });
    }

    private static async Task<IActionResult> ForwardError(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        return new ObjectResult(new { error = body }) { StatusCode = (int)resp.StatusCode };
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private FreeBlazorExampleExternalApiSession? GetSession()
    {
        var session = _store.GetSession(Request);
        return session?.SmartsheetToken is null ? null : session;
    }

    // -------------------------------------------------------------------------
    // GET /api/Showcase/Smartsheet/sheets/{id}?page=1&pageSize=500
    // Returns one page of a sheet (columns + rows).
    // -------------------------------------------------------------------------
    [HttpGet("sheets/{id:long}")]
    public async Task<IActionResult> GetSheet(long id,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500){
        var session = GetSession();
        if (session is null) return Unauthorized(new { error = "No active Smartsheet session. Connect first." });

        var url = $"sheets/{id}?pageSize={pageSize}&page={page}";
        using var req = BuildRequest(HttpMethod.Get, url, session.SmartsheetToken!);
        using var resp = await _http.SendAsync(req, HttpContext.RequestAborted);
        if (!resp.IsSuccessStatusCode) return await ForwardError(resp);

        // Stream the JSON straight back — the component already knows the Smartsheet schema
        var json = await resp.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    // -------------------------------------------------------------------------
    // GET /api/Showcase/Smartsheet/sheets
    // Lists all sheets the token owner can see.
    // -------------------------------------------------------------------------
    [HttpGet("sheets")]
    public async Task<IActionResult> ListSheets()
    {
        var session = GetSession();
        if (session is null) return Unauthorized(new { error = "No active Smartsheet session. Connect first." });

        using var req = BuildRequest(HttpMethod.Get, "sheets?includeAll=true", session.SmartsheetToken!);
        using var resp = await _http.SendAsync(req, HttpContext.RequestAborted);
        if (!resp.IsSuccessStatusCode) return await ForwardError(resp);

        // Parse and return only the data array (sheet list)
        using var doc = await JsonDocument.ParseAsync(
            await resp.Content.ReadAsStreamAsync(), cancellationToken: HttpContext.RequestAborted);

        var sheets = new List<object>();
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            foreach (var s in data.EnumerateArray())
            {
                sheets.Add(new
                {
                    id          = s.TryGetProperty("id", out var id) ? id.GetInt64() : 0L,
                    name        = s.TryGetProperty("name", out var n) ? n.GetString() : "",
                    modifiedAt  = s.TryGetProperty("modifiedAt", out var m) ? m.GetString() : null,
                    permalink   = s.TryGetProperty("permalink", out var p) ? p.GetString() : null,
                    accessLevel = s.TryGetProperty("accessLevel", out var al) ? al.GetString() : null,
                });
            }
        }
        return Ok(sheets);
    }

    // ── Request/Response models ──────────────────────────────────────────────

    public record ConnectRequest(string Token);
}
