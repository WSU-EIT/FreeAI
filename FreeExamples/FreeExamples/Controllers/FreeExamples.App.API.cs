using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FreeExamples.Server.Services;
using System.Text.Json;

namespace FreeExamples.Server.Controllers;

// ---- API.SampleData ----

/// <summary>
/// Three-endpoint CRUD pattern for SampleData: GetMany, SaveMany, DeleteMany.
/// Plus a dashboard aggregate endpoint shared across demo pages.
/// </summary>
public partial class DataController
{
    /// <summary>
    /// DeleteMany: POST a list of IDs to delete. Empty/null → error.
    /// To delete all: GetSampleItems([]) first, then pass IDs.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/DeleteSampleItems")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteSampleItems(List<Guid>? ids)
    {
        var result = da.DeleteSampleItems(ids);

        // Broadcast each deleted ID via SignalR.
        if (result.Result && ids != null) {
            foreach (var id in ids) {
                await da.SignalRUpdate(new DataObjects.SignalRUpdate {
                    TenantId = CurrentUser.TenantId,
                    ItemId = id,
                    UserId = CurrentUser.UserId,
                    UpdateType = DataObjects.SignalRUpdateType.SampleItemDeleted,
                    Message = "deleted",
                });
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Server-side CSV export for download demo.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GenerateSampleCsvExport")]
    public ActionResult<DataObjects.SampleFileResponse> GenerateSampleCsvExport()
    {
        return Ok(da.GenerateSampleCsvExport());
    }

    /// <summary>
    /// Server-side text file generation for download demo.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GenerateSampleTextFile")]
    public ActionResult<DataObjects.SampleFileResponse> GenerateSampleTextFile()
    {
        return Ok(da.GenerateSampleTextFile());
    }

    /// <summary>
    /// Dashboard aggregate data — status counts, category breakdowns, timeline.
    /// Shared across dashboard, charts, and reporting demo pages.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetSampleDashboard")]
    public ActionResult<DataObjects.SampleDashboard> GetSampleDashboard()
    {
        return Ok(da.GetSampleDashboard());
    }

    /// <summary>
    /// Network graph nodes and edges from sample data for vis.js demo.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetSampleGraphData")]
    public ActionResult<DataObjects.SampleGraphData> GetSampleGraphData()
    {
        return Ok(da.GetSampleGraphData());
    }
    /// <summary>
    /// GetMany: POST null/empty list → all items, or list of IDs → matching items.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetSampleItems")]
    public ActionResult<List<DataObjects.SampleItem>> GetSampleItems(List<Guid>? ids)
    {
        return Ok(da.GetSampleItems(ids));
    }

    /// <summary>
    /// Filtered, sorted, paginated list of sample items.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetSampleItemsFiltered")]
    public ActionResult<DataObjects.FilterSampleItems> GetSampleItemsFiltered(DataObjects.FilterSampleItems filter)
    {
        return Ok(da.GetSampleItemsFiltered(filter));
    }

    /// <summary>
    /// SaveMany: POST a list of items. If SampleItemId exists → update, otherwise → insert.
    /// Caller may provide their own ID or leave it empty for auto-generation.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/SaveSampleItems")]
    public async Task<ActionResult<List<DataObjects.SampleItem>>> SaveSampleItems(List<DataObjects.SampleItem> items)
    {
        var saved = da.SaveSampleItems(items, CurrentUser);

        // Broadcast each saved item via SignalR so other clients update in real time.
        foreach (var item in saved) {
            await da.SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = CurrentUser.TenantId,
                ItemId = item.SampleItemId,
                UserId = CurrentUser.UserId,
                UpdateType = DataObjects.SignalRUpdateType.SampleItemSaved,
                Message = "saved",
                Object = item,
            });
        }

        return Ok(saved);
    }
}

// ---- API.JsonStore ----

/// <summary>
/// Three-endpoint CRUD for all JSON store entity types.
/// Each entity gets GetMany, SaveMany, DeleteMany — thin wrappers over generic CRUD.
/// </summary>
public partial class DataController
{
    [HttpPost, Authorize, Route("~/api/Data/DeleteBoardConfigs")]
    public ActionResult<DataObjects.BooleanResponse> DeleteBoardConfigs([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.BoardConfig>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteBudgetRequests")]
    public ActionResult<DataObjects.BooleanResponse> DeleteBudgetRequests([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.BudgetRequest>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteEquipment")]
    public ActionResult<DataObjects.BooleanResponse> DeleteEquipment([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Equipment>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteEvaluations")]
    public ActionResult<DataObjects.BooleanResponse> DeleteEvaluations([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Evaluation>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteOnboarding")]
    public ActionResult<DataObjects.BooleanResponse> DeleteOnboarding([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Onboarding>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteProjects")]
    public ActionResult<DataObjects.BooleanResponse> DeleteProjects([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Project>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteSavedViews")]
    public ActionResult<DataObjects.BooleanResponse> DeleteSavedViews([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.SavedView>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteSprints")]
    public ActionResult<DataObjects.BooleanResponse> DeleteSprints([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Sprint>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteTickets")]
    public ActionResult<DataObjects.BooleanResponse> DeleteTickets([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.Ticket>(ids));

    [HttpPost, Authorize, Route("~/api/Data/DeleteWorkOrders")]
    public ActionResult<DataObjects.BooleanResponse> DeleteWorkOrders([FromBody] List<Guid>? ids) => Ok(da.DeleteJsonRecords<DataObjects.WorkOrder>(ids));

    // ── Board Configs ──
    [HttpPost, Authorize, Route("~/api/Data/GetBoardConfigs")]
    public ActionResult<List<DataObjects.BoardConfig>> GetBoardConfigs([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.BoardConfig>(ids));

    // ── Budget Requests ──
    [HttpPost, Authorize, Route("~/api/Data/GetBudgetRequests")]
    public ActionResult<List<DataObjects.BudgetRequest>> GetBudgetRequests([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.BudgetRequest>(ids));

    // ── Equipment ──
    [HttpPost, Authorize, Route("~/api/Data/GetEquipment")]
    public ActionResult<List<DataObjects.Equipment>> GetEquipment([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Equipment>(ids));

    // ── Evaluations ──
    [HttpPost, Authorize, Route("~/api/Data/GetEvaluations")]
    public ActionResult<List<DataObjects.Evaluation>> GetEvaluations([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Evaluation>(ids));

    // ── Onboarding ──
    [HttpPost, Authorize, Route("~/api/Data/GetOnboarding")]
    public ActionResult<List<DataObjects.Onboarding>> GetOnboarding([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Onboarding>(ids));
    // ── Projects ──
    [HttpPost, Authorize, Route("~/api/Data/GetProjects")]
    public ActionResult<List<DataObjects.Project>> GetProjects([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Project>(ids));

    // ── Saved Views ──
    [HttpPost, Authorize, Route("~/api/Data/GetSavedViews")]
    public ActionResult<List<DataObjects.SavedView>> GetSavedViews([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.SavedView>(ids));

    // ── Sprints ──
    [HttpPost, Authorize, Route("~/api/Data/GetSprints")]
    public ActionResult<List<DataObjects.Sprint>> GetSprints([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Sprint>(ids));

    // ── Tickets ──
    [HttpPost, Authorize, Route("~/api/Data/GetTickets")]
    public ActionResult<List<DataObjects.Ticket>> GetTickets([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.Ticket>(ids));

    // ── Work Orders ──
    [HttpPost, Authorize, Route("~/api/Data/GetWorkOrders")]
    public ActionResult<List<DataObjects.WorkOrder>> GetWorkOrders([FromBody] List<Guid>? ids) => Ok(da.GetJsonRecords<DataObjects.WorkOrder>(ids));

    [HttpPost, Authorize, Route("~/api/Data/SaveBoardConfigs")]
    public ActionResult<List<DataObjects.BoardConfig>> SaveBoardConfigs([FromBody] List<DataObjects.BoardConfig> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveBudgetRequests")]
    public ActionResult<List<DataObjects.BudgetRequest>> SaveBudgetRequests([FromBody] List<DataObjects.BudgetRequest> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveEquipment")]
    public ActionResult<List<DataObjects.Equipment>> SaveEquipment([FromBody] List<DataObjects.Equipment> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveEvaluations")]
    public ActionResult<List<DataObjects.Evaluation>> SaveEvaluations([FromBody] List<DataObjects.Evaluation> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveOnboarding")]
    public ActionResult<List<DataObjects.Onboarding>> SaveOnboarding([FromBody] List<DataObjects.Onboarding> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveProjects")]
    public ActionResult<List<DataObjects.Project>> SaveProjects([FromBody] List<DataObjects.Project> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveSavedViews")]
    public ActionResult<List<DataObjects.SavedView>> SaveSavedViews([FromBody] List<DataObjects.SavedView> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveSprints")]
    public ActionResult<List<DataObjects.Sprint>> SaveSprints([FromBody] List<DataObjects.Sprint> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveTickets")]
    public ActionResult<List<DataObjects.Ticket>> SaveTickets([FromBody] List<DataObjects.Ticket> items) => Ok(da.SaveJsonRecords(items, CurrentUser));

    [HttpPost, Authorize, Route("~/api/Data/SaveWorkOrders")]
    public ActionResult<List<DataObjects.WorkOrder>> SaveWorkOrders([FromBody] List<DataObjects.WorkOrder> items) => Ok(da.SaveJsonRecords(items, CurrentUser));
}

// ---- API.ApiKeyDemo ----

/// <summary>
/// API endpoints for the API Key Middleware demo.
/// Pattern from: FreeGLBA GlbaController (external API key auth) + DataController (internal user auth).
/// 
/// Two categories of endpoints:
/// 1. Internal (user auth via [Authorize]) — manage keys, view logs
/// 2. External (API key auth via middleware) — the protected endpoint
/// </summary>
public partial class DataController
{
    /// <summary>
    /// Generate a new API key. Returns the key info AND the plaintext key (shown once).
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GenerateApiKey")]
    public ActionResult<object> GenerateApiKey(
        [FromBody] DataObjects.ApiKeyTestRequest request,
        [FromServices] ApiKeyDemoService keyService){
        var name = !string.IsNullOrWhiteSpace(request.Message) ? request.Message : "Demo Key";
        var (keyInfo, plaintextKey) = keyService.GenerateKey(name);

        return Ok(new {
            keyInfo,
            plaintextKey,
        });
    }

    /// <summary>
    /// Get the middleware request log.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetApiKeyLogs")]
    public ActionResult<List<DataObjects.ApiKeyRequestLog>> GetApiKeyLogs(
        [FromServices] ApiKeyDemoService keyService){
        return Ok(keyService.GetLogs());
    }
    // ================================================================
    // INTERNAL ENDPOINTS — user-authenticated, for managing demo keys
    // ================================================================

    /// <summary>
    /// GetMany for API keys. Returns all registered keys.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetApiKeys")]
    public ActionResult<List<DataObjects.ApiKeyInfo>> GetApiKeys(
        [FromServices] ApiKeyDemoService keyService){
        return Ok(keyService.GetKeys());
    }

    /// <summary>
    /// Revoke (deactivate) an API key.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/RevokeApiKey")]
    public ActionResult<DataObjects.BooleanResponse> RevokeApiKey(
        [FromBody] Guid apiKeyId,
        [FromServices] ApiKeyDemoService keyService){
        var result = keyService.RevokeKey(apiKeyId);
        return Ok(new DataObjects.BooleanResponse { Result = result });
    }
}

// ================================================================
// EXTERNAL CONTROLLER — protected by ApiKeyDemoMiddleware
// Separate controller so it uses a different route prefix.
// Pattern from: FreeGLBA GlbaController
// ================================================================

[ApiController]
[Route("api/demo/external")]
public class ExternalDemoController : ControllerBase
{
    /// <summary>
    /// Simple GET endpoint that just returns status (also protected by middleware).
    /// </summary>
    [HttpGet("ping")]
    public ActionResult<DataObjects.ApiKeyTestResponse> Ping(
        [FromServices] ApiKeyDemoService keyService){
        var keyInfo = HttpContext.Items["ApiKeyInfo"] as DataObjects.ApiKeyInfo;

        keyService.LogRequest(
            HttpContext.Request.Path.Value ?? "",
            HttpContext.Request.Method,
            keyInfo?.Name,
            200,
            "ping");

        return Ok(new DataObjects.ApiKeyTestResponse {
            Success = true,
            Message = "pong",
            AuthenticatedAs = keyInfo?.Name ?? "Unknown",
            Timestamp = DateTime.UtcNow,
        });
    }
    /// <summary>
    /// A protected endpoint that requires a valid API key via Bearer token.
    /// The middleware validates the key before this code runs.
    /// </summary>
    [HttpPost("data")]
    public ActionResult<DataObjects.ApiKeyTestResponse> PostData(
        [FromBody] DataObjects.ApiKeyTestRequest request,
        [FromServices] ApiKeyDemoService keyService){
        // Get the validated key info set by the middleware
        var keyInfo = HttpContext.Items["ApiKeyInfo"] as DataObjects.ApiKeyInfo;
        if (keyInfo == null) {
            return Unauthorized(new { error = "invalid_api_key", message = "API key validation failed" });
        }

        // Log the successful request
        keyService.LogRequest(
            HttpContext.Request.Path.Value ?? "",
            HttpContext.Request.Method,
            keyInfo.Name,
            200,
            request.Message);

        return Ok(new DataObjects.ApiKeyTestResponse {
            Success = true,
            Message = $"Received: {request.Message ?? "(empty)"}",
            AuthenticatedAs = keyInfo.Name,
            Timestamp = DateTime.UtcNow,
        });
    }
}

// ---- API.CodePlayground ----

/// <summary>
/// API endpoints for the Code Playground demo.
/// Three-endpoint pattern: GetMany, SaveMany, DeleteMany for CodeSnippet.
/// Plus an "execute" endpoint for the API Notebook tab.
/// </summary>
public partial class DataController
{
    private static readonly JsonSerializerOptions _prettyJson = new() { WriteIndented = true };

    /// <summary>
    /// Delete a snippet by ID.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/DeleteCodeSnippet")]
    public ActionResult<DataObjects.BooleanResponse> DeleteCodeSnippet(
        [FromBody] Guid id,
        [FromServices] CodeSnippetService snippetService){
        return Ok(new DataObjects.BooleanResponse { Result = snippetService.DeleteSnippet(id) });
    }

    /// <summary>
    /// Execute a request against a selected internal API endpoint.
    /// Used by the API Notebook tab to POST JSON and get the response.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/ExecuteCodePlayground")]
    public ActionResult<DataObjects.CodePlaygroundResponse> ExecuteCodePlayground(
        [FromBody] DataObjects.CodePlaygroundRequest request){
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = new DataObjects.CodePlaygroundResponse();

        try {
            object? result = request.Endpoint switch {
                "GetSampleItems" => da.GetSampleItems(
                    string.IsNullOrWhiteSpace(request.Body) ? null :
                    JsonSerializer.Deserialize<List<Guid>>(request.Body)),

                "GetSampleDashboard" => da.GetSampleDashboard(),

                "GetSampleItemsFiltered" => da.GetSampleItemsFiltered(
                    JsonSerializer.Deserialize<DataObjects.FilterSampleItems>(request.Body)
                    ?? new DataObjects.FilterSampleItems()),

                "GetSampleGraphData" => da.GetSampleGraphData(),

                _ => null,
            };

            if (result != null) {
                response.Success = true;
                response.StatusCode = 200;
                response.ResponseBody = JsonSerializer.Serialize(result, _prettyJson);
            } else {
                response.Success = false;
                response.StatusCode = 404;
                response.ResponseBody = "{ \"error\": \"Unknown endpoint: " + request.Endpoint + "\" }";
            }
        } catch (Exception ex) {
            response.Success = false;
            response.StatusCode = 500;
            response.ResponseBody = "{ \"error\": \"" + ex.Message.Replace("\"", "\\\"") + "\" }";
        }

        sw.Stop();
        response.DurationMs = sw.ElapsedMilliseconds;
        return Ok(response);
    }

    /// <summary>
    /// GetMany: null/empty → all snippets, list of IDs → filtered.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetCodeSnippets")]
    public ActionResult<List<DataObjects.CodeSnippet>> GetCodeSnippets(
        List<Guid>? ids,
        [FromServices] CodeSnippetService snippetService){
        return Ok(snippetService.GetSnippets(ids));
    }

    /// <summary>
    /// Save a single snippet (insert or update).
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/SaveCodeSnippet")]
    public ActionResult<DataObjects.CodeSnippet> SaveCodeSnippet(
        [FromBody] DataObjects.CodeSnippet snippet,
        [FromServices] CodeSnippetService snippetService){
        return Ok(snippetService.SaveSnippet(snippet));
    }
}

// ---- API.CommentThread ----

/// <summary>
/// API endpoints for the Comment Thread demo.
/// </summary>
public partial class DataController
{
    /// <summary>
    /// Delete a comment.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/DeleteComment")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteComment(
        [FromBody] Guid commentId,
        [FromServices] CommentService commentService){
        var result = commentService.DeleteComment(commentId);

        if (result) {
            await da.SignalRUpdate(new DataObjects.SignalRUpdate {
                TenantId = CurrentUser.TenantId,
                ItemId = commentId,
                UserId = CurrentUser.UserId,
                UpdateType = DataObjects.SignalRUpdateType.CommentDeleted,
                Message = "comment deleted",
            });
        }

        return Ok(new DataObjects.BooleanResponse { Result = result });
    }
    /// <summary>
    /// Get comments for a SampleItem.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetComments")]
    public ActionResult<List<DataObjects.SampleComment>> GetComments(
        [FromBody] Guid sampleItemId,
        [FromServices] CommentService commentService){
        return Ok(commentService.GetComments(sampleItemId));
    }

    /// <summary>
    /// Save a comment (insert or update).
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/SaveComment")]
    public async Task<ActionResult<DataObjects.SampleComment>> SaveComment(
        [FromBody] DataObjects.SampleComment comment,
        [FromServices] CommentService commentService){
        var saved = commentService.SaveComment(comment);

        await da.SignalRUpdate(new DataObjects.SignalRUpdate {
            TenantId = CurrentUser.TenantId,
            ItemId = saved.SampleItemId,
            UserId = CurrentUser.UserId,
            UpdateType = DataObjects.SignalRUpdateType.CommentSaved,
            Message = "comment",
            Object = saved,
        });

        return Ok(saved);
    }
}

// ---- API.GitBrowser ----

/// <summary>
/// API endpoints for browsing a git repository. Uses the GitBrowserService
/// to clone public repos and serve their directory/file contents.
/// </summary>
public partial class DataController
{
    /// <summary>
    /// Get a single file's content from a git repo.
    /// POST a GitFileRequest with RepoUrl and FilePath.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetGitFileContent")]
    public async Task<ActionResult<DataObjects.GitFileContent>> GetGitFileContent(
        [FromBody] DataObjects.GitFileRequest request,
        [FromServices] GitBrowserService gitService){
        if (string.IsNullOrWhiteSpace(request.RepoUrl) || string.IsNullOrWhiteSpace(request.FilePath))
            return Ok(new DataObjects.GitFileContent { Content = "No file specified." });

        var content = await gitService.GetFileAsync(request.RepoUrl, request.FilePath);
        return Ok(content);
    }
    /// <summary>
    /// GetMany for git repo entries: POST a GitBrowseRequest with RepoUrl and optional Path.
    /// Returns list of GitRepoEntry (folders first, then files).
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("~/api/Data/GetGitRepoContents")]
    public async Task<ActionResult<List<DataObjects.GitRepoEntry>>> GetGitRepoContents(
        [FromBody] DataObjects.GitBrowseRequest request,
        [FromServices] GitBrowserService gitService){
        if (string.IsNullOrWhiteSpace(request.RepoUrl))
            return Ok(new List<DataObjects.GitRepoEntry>());

        var entries = await gitService.BrowseAsync(request.RepoUrl, request.Path);
        return Ok(entries);
    }
}
