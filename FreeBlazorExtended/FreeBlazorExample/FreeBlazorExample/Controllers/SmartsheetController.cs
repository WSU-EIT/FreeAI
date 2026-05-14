/*
    Purpose: Server-side proxy for Smartsheet REST API v2.
    Security: The API token is accepted once via POST /connect, stored server-side in
              ExternalApiSessionStore, and returned to the browser only as an HttpOnly
              session cookie. All subsequent data requests carry only that cookie.
    Pattern:  Browser → POST /api/Showcase/Smartsheet/connect (token in body, HTTPS)
              Browser → GET  /api/Showcase/Smartsheet/sheets  (cookie only)
              Tokens never appear in GET URLs or response bodies.
*/
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FreeBlazorExample.Server.Controllers;

[ApiController]
[Route("api/Showcase/Smartsheet")]
public sealed class SmartsheetController : ControllerBase
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.smartsheet.com/2.0/"),
        DefaultRequestHeaders = { { "User-Agent", "FreeBlazorExtended-Showcase/1.0" } }
    };

    private readonly ExternalApiSessionStore _store;

    public SmartsheetController(ExternalApiSessionStore store)
    {
        _store = store;
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
        Response.Cookies.Append(ExternalApiSessionStore.CookieName, sessionKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure   = Request.IsHttps,
            MaxAge   = TimeSpan.FromHours(2),
            Path     = "/api/Showcase"
        });

        return Ok(new { connected = true, email });
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

    // -------------------------------------------------------------------------
    // GET /api/Showcase/Smartsheet/sheets/{id}?page=1&pageSize=500
    // Returns one page of a sheet (columns + rows).
    // -------------------------------------------------------------------------
    [HttpGet("sheets/{id:long}")]
    public async Task<IActionResult> GetSheet(long id,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 500)
    {
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
    // DELETE /api/Showcase/Smartsheet/disconnect
    // Clears the server session and the cookie.
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

    private ExternalApiSession? GetSession()
    {
        var key = Request.Cookies[ExternalApiSessionStore.CookieName];
        var session = _store.Get(key);
        return session?.SmartsheetToken is null ? null : session;
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl, string token)
    {
        var req = new HttpRequestMessage(method, relativeUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static async Task<IActionResult> ForwardError(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        return new ObjectResult(new { error = body }) { StatusCode = (int)resp.StatusCode };
    }

    // ── Request/Response models ──────────────────────────────────────────────

    public record ConnectRequest(string Token);
}
