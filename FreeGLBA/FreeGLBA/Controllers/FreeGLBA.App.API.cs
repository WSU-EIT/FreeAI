using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreeGLBA.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

// ---- API ----

namespace FreeGLBA.Server.Controllers
{
    // ============================================================================
    // FREEGLBA PROJECT API ENDPOINTS
    // All endpoints require a signed-in, enabled user; configuration and
    // destructive operations additionally require Admin. External systems never
    // call these - they use the API-key-authenticated /api/glba endpoints.
    // ============================================================================

    public partial class DataController
    {
        // SourceSystem API Endpoints
        #region SourceSystem

        [HttpPost("api/Data/GetSourceSystems")]
        public async Task<ActionResult<DataObjects.SourceSystemFilterResult>> GetSourceSystems([FromBody] DataObjects.SourceSystemFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetSourceSystemsAsync(filter));
        }

        [HttpPost("api/Data/GetSourceSystem")]
        public async Task<ActionResult<DataObjects.SourceSystem?>> GetSourceSystem([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var item = await da.GetSourceSystemAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("api/Data/GetSourceSystemLookups")]
        public async Task<ActionResult<List<DataObjects.SourceSystemLookup>>> GetSourceSystemLookups()
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetSourceSystemLookupsAsync());
        }

        [HttpPost("api/Data/SaveSourceSystem")]
        public async Task<ActionResult<DataObjects.SourceSystem?>> SaveSourceSystem([FromBody] DataObjects.SourceSystem item)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            var savedBy = !string.IsNullOrWhiteSpace(CurrentUser.DisplayName)
                ? CurrentUser.DisplayName
                : CurrentUser.Username;
            var result = await da.SaveSourceSystemAsync(item, savedBy);
            if (result == null) return BadRequest();
            return Ok(result);
        }

        [HttpPost("api/Data/DeleteSourceSystem")]
        public async Task<ActionResult<bool>> DeleteSourceSystem([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.DeleteSourceSystemAsync(id));
        }

        /// <summary>Get the data-ownership history for a source system (current owner first).</summary>
        [HttpPost("api/Data/GetDataOwnershipHistory")]
        public async Task<ActionResult<List<DataObjects.DataOwnership>>> GetDataOwnershipHistory([FromBody] Guid sourceSystemId)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetDataOwnershipHistoryAsync(sourceSystemId));
        }

        #endregion


        // AccessEvent API Endpoints
        #region AccessEvent

        [HttpPost("api/Data/GetAccessEvents")]
        public async Task<ActionResult<DataObjects.AccessEventFilterResult>> GetAccessEvents([FromBody] DataObjects.AccessEventFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetAccessEventsAsync(filter));
        }

        [HttpPost("api/Data/GetAccessEvent")]
        public async Task<ActionResult<DataObjects.AccessEvent?>> GetAccessEvent([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var item = await da.GetAccessEventAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("api/Data/GetAccessEventLookups")]
        public async Task<ActionResult<List<DataObjects.AccessEventLookup>>> GetAccessEventLookups()
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetAccessEventLookupsAsync());
        }

        [HttpPost("api/Data/SaveAccessEvent")]
        public async Task<ActionResult<DataObjects.AccessEvent?>> SaveAccessEvent([FromBody] DataObjects.AccessEvent item)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var result = await da.SaveAccessEventAsync(item);
            if (result == null) return BadRequest();
            return Ok(result);
        }

        /// <summary>
        /// Saves many AccessEvents in a single request. Maximum 1000 per call.
        /// </summary>
        [HttpPost("api/Data/SaveAccessEvents")]
        public async Task<ActionResult<DataObjects.AccessEventBulkResult>> SaveAccessEvents([FromBody] List<DataObjects.AccessEvent> items)
        {
            if (!CurrentUser.Enabled) return Unauthorized();

            if (items == null || items.Count == 0) {
                return Ok(new DataObjects.AccessEventBulkResult { Success = true });
            }

            if (items.Count > 1000) {
                return BadRequest(new DataObjects.AccessEventBulkResult {
                    Success = false,
                    Message = "Maximum 1000 events per request."
                });
            }

            return Ok(await da.SaveAccessEventsAsync(items));
        }

        [HttpPost("api/Data/DeleteAccessEvent")]
        public async Task<ActionResult<bool>> DeleteAccessEvent([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.DeleteAccessEventAsync(id));
        }

        #endregion


        // DataSubject API Endpoints
        #region DataSubject

        [HttpPost("api/Data/GetDataSubjects")]
        public async Task<ActionResult<DataObjects.DataSubjectFilterResult>> GetDataSubjects([FromBody] DataObjects.DataSubjectFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetDataSubjectsAsync(filter));
        }

        [HttpPost("api/Data/GetDataSubject")]
        public async Task<ActionResult<DataObjects.DataSubject?>> GetDataSubject([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var item = await da.GetDataSubjectAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("api/Data/GetDataSubjectLookups")]
        public async Task<ActionResult<List<DataObjects.DataSubjectLookup>>> GetDataSubjectLookups()
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetDataSubjectLookupsAsync());
        }

        [HttpPost("api/Data/SaveDataSubject")]
        public async Task<ActionResult<DataObjects.DataSubject?>> SaveDataSubject([FromBody] DataObjects.DataSubject item)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var result = await da.SaveDataSubjectAsync(item);
            if (result == null) return BadRequest();
            return Ok(result);
        }

        [HttpPost("api/Data/DeleteDataSubject")]
        public async Task<ActionResult<bool>> DeleteDataSubject([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.DeleteDataSubjectAsync(id));
        }

        /// <summary>Generate a PDF of one data subject's complete access history (DSAR/audit-style export).</summary>
        [HttpPost("api/Data/GenerateSubjectAccessHistoryPdf")]
        public async Task<ActionResult<DataObjects.ComplianceReportExport?>> GenerateSubjectAccessHistoryPdf([FromBody] string subjectExternalId)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var generatedBy = !string.IsNullOrWhiteSpace(CurrentUser.DisplayName) ? CurrentUser.DisplayName : CurrentUser.Username;
            var result = await da.GenerateSubjectAccessHistoryPdfAsync(subjectExternalId, generatedBy);
            if (result == null) return NotFound();
            return Ok(result);
        }

        #endregion


        // ComplianceReport API Endpoints
        #region ComplianceReport

        [HttpPost("api/Data/GetComplianceReports")]
        public async Task<ActionResult<DataObjects.ComplianceReportFilterResult>> GetComplianceReports([FromBody] DataObjects.ComplianceReportFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetComplianceReportsAsync(filter));
        }

        [HttpPost("api/Data/GetComplianceReport")]
        public async Task<ActionResult<DataObjects.ComplianceReport?>> GetComplianceReport([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var item = await da.GetComplianceReportAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("api/Data/GetComplianceReportLookups")]
        public async Task<ActionResult<List<DataObjects.ComplianceReportLookup>>> GetComplianceReportLookups()
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetComplianceReportLookupsAsync());
        }

        [HttpPost("api/Data/SaveComplianceReport")]
        public async Task<ActionResult<DataObjects.ComplianceReport?>> SaveComplianceReport([FromBody] DataObjects.ComplianceReport item)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var result = await da.SaveComplianceReportAsync(item);
            if (result == null) return BadRequest();
            return Ok(result);
        }

        [HttpPost("api/Data/DeleteComplianceReport")]
        public async Task<ActionResult<bool>> DeleteComplianceReport([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.DeleteComplianceReportAsync(id));
        }

        /// <summary>Generate a CSV export of every access event in the report's period.</summary>
        [HttpPost("api/Data/GenerateComplianceReportCsv")]
        public async Task<ActionResult<DataObjects.ComplianceReportExport?>> GenerateComplianceReportCsv([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var generatedBy = !string.IsNullOrWhiteSpace(CurrentUser.DisplayName) ? CurrentUser.DisplayName : CurrentUser.Username;
            var result = await da.GenerateComplianceReportCsvAsync(id, generatedBy);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>Generate a PDF summary report for the report's period.</summary>
        [HttpPost("api/Data/GenerateComplianceReportPdf")]
        public async Task<ActionResult<DataObjects.ComplianceReportExport?>> GenerateComplianceReportPdf([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var generatedBy = !string.IsNullOrWhiteSpace(CurrentUser.DisplayName) ? CurrentUser.DisplayName : CurrentUser.Username;
            var result = await da.GenerateComplianceReportPdfAsync(id, generatedBy);
            if (result == null) return NotFound();
            return Ok(result);
        }

        #endregion

        #region GLBA Settings and Integrity

        /// <summary>Get the app-wide GLBA settings (alerts, thresholds, institution timezone).</summary>
        [HttpGet("api/Data/GetGlbaSettings")]
        public async Task<ActionResult<DataObjects.GlbaSettings>> GetGlbaSettings()
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.GetGlbaSettingsAsync());
        }

        /// <summary>Save the app-wide GLBA settings.</summary>
        [HttpPost("api/Data/SaveGlbaSettings")]
        public async Task<ActionResult<DataObjects.GlbaSettings>> SaveGlbaSettings([FromBody] DataObjects.GlbaSettings settings)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.SaveGlbaSettingsAsync(settings, CurrentUser));
        }

        /// <summary>Send a test alert to the configured webhook.</summary>
        [HttpPost("api/Data/SendTestGlbaAlert")]
        public async Task<ActionResult<bool>> SendTestGlbaAlert()
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.SendTestGlbaAlertAsync());
        }

        /// <summary>Verify a source system's tamper-evident event hash chain.</summary>
        [HttpPost("api/Data/VerifyAccessEventChain")]
        public async Task<ActionResult<DataObjects.ChainVerificationResult>> VerifyAccessEventChain([FromBody] Guid sourceSystemId)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.VerifyAccessEventChainAsync(sourceSystemId));
        }

        #endregion

        #region Accessor Endpoints

        /// <summary>Get filtered list of accessors (users who have accessed data).</summary>
        [HttpPost("api/Data/GetAccessors")]
        public async Task<ActionResult<DataObjects.AccessorFilterResult>> GetAccessors([FromBody] DataObjects.AccessorFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetAccessorsAsync(filter));
        }

        /// <summary>Get top accessors for dashboard.</summary>
        [HttpGet("api/Data/GetTopAccessors")]
        public async Task<ActionResult<List<DataObjects.AccessorSummary>>> GetTopAccessors([FromQuery] int limit = 10)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetTopAccessorsAsync(limit));
        }

        #endregion

        // ============================================================================
        // API REQUEST LOGGING ENDPOINTS
        // ============================================================================

        #region API Request Logging

        /// <summary>Get dashboard statistics for API logs.</summary>
        [HttpPost("api/Data/GetApiLogDashboardStats")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<DataObjects.ApiLogDashboardStats>> GetApiLogDashboardStats([FromBody] DataObjects.ApiLogDashboardRequest request)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var from = request.From ?? DateTime.UtcNow.AddHours(-24);
            var to = request.To ?? DateTime.UtcNow;
            return Ok(await da.GetApiLogDashboardStatsAsync(from, to));
        }

        /// <summary>Get paginated/filtered list of API request logs.</summary>
        [HttpPost("api/Data/GetApiLogs")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<DataObjects.ApiLogFilterResult>> GetApiLogs([FromBody] DataObjects.ApiLogFilter filter)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            return Ok(await da.GetApiLogsAsync(filter));
        }

        /// <summary>Get a single API request log by ID.</summary>
        [HttpPost("api/Data/GetApiLog")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<DataObjects.ApiRequestLog?>> GetApiLog([FromBody] Guid id)
        {
            if (!CurrentUser.Enabled) return Unauthorized();
            var item = await da.GetApiLogAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        #endregion

        #region Body Logging Configuration

        /// <summary>Get all body logging configurations.</summary>
        [HttpGet("api/Data/GetBodyLoggingConfigs")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<List<DataObjects.BodyLoggingConfig>>> GetBodyLoggingConfigs()
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.GetBodyLoggingConfigsAsync());
        }

        /// <summary>Enable body logging for a source system.</summary>
        [HttpPost("api/Data/EnableBodyLogging")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<DataObjects.BodyLoggingConfig>> EnableBodyLogging([FromBody] DataObjects.EnableBodyLoggingRequest request)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            var result = await da.EnableBodyLoggingAsync(
                request.SourceSystemId,
                request.EnabledByUserId,
                request.EnabledByUserName,
                request.DurationHours,
                request.Reason);
            return Ok(result);
        }

        /// <summary>Disable body logging for a source system.</summary>
        [HttpPost("api/Data/DisableBodyLogging")]
        [SkipApiLogging(Reason = "Prevents infinite loop")]
        public async Task<ActionResult<bool>> DisableBodyLogging([FromBody] Guid configId)
        {
            if (!CurrentUser.Enabled || !CurrentUser.Admin) return Unauthorized();
            return Ok(await da.DisableBodyLoggingAsync(configId));
        }

        #endregion

    }
}

// ---- API.ExternalApi ----

namespace FreeGLBA.Controllers
{
    // ============================================================================
    // GLBA EXTERNAL API CONTROLLER
    // Receives events from external source systems via API key authentication.
    // Generated by FreeManager for template: FreeAudit
    // ============================================================================

    /// <summary>
    /// GLBA Compliance API - Log and query access to protected financial information
    /// </summary>
    [ApiController]
    [Route("api/glba")]
    [ApiRequestLogging]
    [Produces("application/json")]
    public class GlbaController : ControllerBase
    {
        private readonly IDataAccess _da;

        public GlbaController(IDataAccess da)
        {
            _da = da;
        }

        /// <summary>
        /// Get a single access event by ID
        /// </summary>
        /// <remarks>
        /// Retrieves full details of a specific access event.
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <param name="eventId">The unique identifier of the event</param>
        /// <response code="200">Event retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        /// <response code="404">Event not found</response>
        [HttpGet("events/{eventId:guid}")]
        [Authorize]
        [Tags("Internal - Queries")]
        [ProducesResponseType(typeof(DataObjects.AccessEvent), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DataObjects.AccessEvent>> GetEvent(Guid eventId)
        {
            var evt = await _da.GetAccessEventAsync(eventId);
            if (evt == null) return NotFound();
            return Ok(evt);
        }

        /// <summary>
        /// Get recent access events
        /// </summary>
        /// <remarks>
        /// Returns the most recent access events for the dashboard feed.
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <param name="limit">Maximum events to return (default 50, max 100)</param>
        /// <response code="200">Events retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("events/recent")]
        [Authorize]
        [Tags("Internal - Dashboard")]
        [ProducesResponseType(typeof(List<DataObjects.AccessEvent>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<DataObjects.AccessEvent>>> GetRecentEvents(
            [FromQuery] int limit = 50){
            var events = await _da.GetRecentAccessEventsAsync(Math.Min(limit, 100));
            return Ok(events);
        }

        /// <summary>
        /// Get status of all source systems
        /// </summary>
        /// <remarks>
        /// Returns information about all registered source systems including:
        /// - Name and display name
        /// - Active status
        /// - Last event received timestamp
        /// - Total event count
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <response code="200">Source systems retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("sources/status")]
        [Authorize]
        [Tags("Internal - Dashboard")]
        [ProducesResponseType(typeof(List<DataObjects.SourceSystem>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<DataObjects.SourceSystem>>> GetSourceStatus()
        {
            var sources = await _da.GetSourceSystemsAsync();
            return Ok(sources);
        }

        // ============================================================
        // INTERNAL ENDPOINTS (User Auth)
        // ============================================================

        /// <summary>
        /// Get dashboard summary statistics
        /// </summary>
        /// <remarks>
        /// Returns aggregate statistics for the GLBA dashboard including:
        /// - Event counts (today, this week, this month)
        /// - Subject counts (unique individuals whose data was accessed)
        /// - Breakdowns by category and access type
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <response code="200">Statistics retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("stats/summary")]
        [Authorize]
        [Tags("Internal - Dashboard")]
        [ProducesResponseType(typeof(DataObjects.GlbaStats), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<DataObjects.GlbaStats>> GetStats()
        {
            var stats = await _da.GetGlbaStatsAsync();
            return Ok(stats);
        }

        /// <summary>
        /// Get dashboard trend data and items needing attention
        /// </summary>
        /// <remarks>
        /// Returns event counts per day for the trend chart plus insight items:
        /// unusual per-user volume spikes, large bulk exports, first-time accessors,
        /// and source systems with no data owner recorded.
        ///
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <param name="days">Trend window in days (7–90, default 30)</param>
        /// <response code="200">Insights retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("stats/insights")]
        [Authorize]
        [Tags("Internal - Dashboard")]
        [ProducesResponseType(typeof(DataObjects.GlbaInsights), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<DataObjects.GlbaInsights>> GetInsights([FromQuery] int days = 30)
        {
            var insights = await _da.GetGlbaInsightsAsync(days);
            return Ok(insights);
        }

        /// <summary>
        /// Get access events for a specific data subject
        /// </summary>
        /// <remarks>
        /// Returns all access events where the specified subject's data was accessed.
        /// Useful for auditing who has accessed a particular individual's information.
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <param name="subjectId">The external ID of the data subject (e.g., student ID)</param>
        /// <param name="limit">Maximum events to return (default 100, max 500)</param>
        /// <response code="200">Events retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("subjects/{subjectId}/events")]
        [Authorize]
        [Tags("Internal - Queries")]
        [ProducesResponseType(typeof(List<DataObjects.AccessEvent>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<DataObjects.AccessEvent>>> GetSubjectEvents(
            string subjectId, [FromQuery] int limit = 100){
            var events = await _da.GetAccessEventsBySubjectAsync(subjectId, Math.Min(limit, 500));
            return Ok(events);
        }

        /// <summary>
        /// Get top data accessors
        /// </summary>
        /// <remarks>
        /// Returns the users who have accessed the most protected data, including:
        /// - Total access count
        /// - Unique subjects accessed
        /// - Export vs view breakdown
        /// 
        /// Useful for compliance monitoring and identifying unusual access patterns.
        /// 
        /// **Authentication:** User JWT (not API key)
        /// </remarks>
        /// <param name="limit">Maximum accessors to return (default 10, max 50)</param>
        /// <response code="200">Accessors retrieved successfully</response>
        /// <response code="401">Not authenticated</response>
        [HttpGet("accessors/top")]
        [Authorize]
        [Tags("Internal - Dashboard")]
        [ProducesResponseType(typeof(List<DataObjects.AccessorSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<DataObjects.AccessorSummary>>> GetTopAccessors(
            [FromQuery] int limit = 10){
            var accessors = await _da.GetTopAccessorsAsync(Math.Min(limit, 50));
            return Ok(accessors);
        }

        /// <summary>
        /// Log multiple access events in a single request
        /// </summary>
        /// <remarks>
        /// Submit up to 1000 events in a single batch request. Useful for:
        /// - Migrating historical data
        /// - Batch processing from external systems
        /// - High-volume event logging
        /// 
        /// **Authentication:** API Key (Bearer token in Authorization header)
        /// 
        /// Each event is processed independently - partial success is possible.
        /// </remarks>
        /// <param name="events">List of access events (max 1000)</param>
        /// <returns>Batch processing results with accepted/rejected counts</returns>
        /// <response code="200">Batch processed (check accepted/rejected counts)</response>
        /// <response code="400">Batch too large (over 1000 events)</response>
        /// <response code="401">Invalid or missing API key</response>
        [HttpPost("events/batch")]
        [Tags("External - Event Logging")]
        [ProducesResponseType(typeof(DataObjects.GlbaBatchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<DataObjects.GlbaBatchResponse>> PostBatch(
            [FromBody] List<DataObjects.GlbaEventRequest> events){
            var source = HttpContext.Items["SourceSystem"] as DataObjects.SourceSystem;
            if (source == null)
            {
                return Unauthorized(new { error = "invalid_api_key", message = "API key is invalid or inactive" });
            }

            if (events.Count > 1000)
            {
                return BadRequest(new { error = "batch_too_large", message = "Maximum 1000 events per batch" });
            }

            var result = await _da.ProcessGlbaBatchAsync(events, source.SourceSystemId);
            return Ok(result);
        }

        // ============================================================
        // EXTERNAL ENDPOINTS (API Key Auth via middleware)
        // ============================================================

        /// <summary>
        /// Log a single access event
        /// </summary>
        /// <remarks>
        /// Records a single instance of data access for GLBA compliance tracking.
        /// 
        /// **Authentication:** API Key (Bearer token in Authorization header)
        /// 
        /// **Example request:**
        /// ```json
        /// {
        ///   "userId": "jsmith",
        ///   "userName": "John Smith",
        ///   "subjectId": "STU-12345",
        ///   "accessType": "View",
        ///   "purpose": "Reviewing financial aid application"
        /// }
        /// ```
        /// </remarks>
        /// <param name="request">The access event details</param>
        /// <returns>Event confirmation with assigned ID</returns>
        /// <response code="201">Event recorded successfully</response>
        /// <response code="400">Validation error - check required fields</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="409">Duplicate event (same SourceEventId already exists)</response>
        [HttpPost("events")]
        [Tags("External - Event Logging")]
        [ProducesResponseType(typeof(DataObjects.GlbaEventResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(DataObjects.GlbaEventResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DataObjects.GlbaEventResponse>> PostEvent(
            [FromBody] DataObjects.GlbaEventRequest request){
            // Get source system from middleware (set by ApiKeyMiddleware)
            var source = HttpContext.Items["SourceSystem"] as DataObjects.SourceSystem;
            if (source == null)
            {
                return Unauthorized(new { error = "invalid_api_key", message = "API key is invalid or inactive" });
            }

            var result = await _da.ProcessGlbaEventAsync(request, source.SourceSystemId);

            return result.Status switch
            {
                "accepted" => Created($"/api/glba/events/{result.EventId}", result),
                "duplicate" => Conflict(result),
                _ => BadRequest(result)
            };
        }
    }
}

// ---- ApiRequestLoggingAttribute ----

namespace FreeGLBA.Controllers
{
    // ============================================================================
    // API REQUEST LOGGING - Action Filter Attribute
    // Logs API requests/responses for compliance and debugging
    // ============================================================================

    /// <summary>
    /// Attribute to enable API request logging on a controller or action.
    /// Captures request/response details, timing, and error information.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiRequestLoggingAttribute : ActionFilterAttribute
    {
        private const string StopwatchKey = "ApiLogging_Stopwatch";
        private const string RequestDataKey = "ApiLogging_RequestData";

        private static string CaptureHeaders(
            Microsoft.AspNetCore.Http.IHeaderDictionary headers,
            List<string> sensitiveHeaders){
            var filteredHeaders = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                var headerName = header.Key;
                var headerValue = header.Value.ToString();

                // Check if sensitive (case-insensitive)
                var isSensitive = sensitiveHeaders
                    .Any(s => s.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                filteredHeaders[headerName] = isSensitive
                    ? "[REDACTED]"
                    : TruncateString(headerValue, 500);
            }

            return JsonSerializer.Serialize(filteredHeaders);
        }

        private static string GetClientIpAddress(Microsoft.AspNetCore.Http.HttpContext context)
        {
            // Check for forwarded IP first (behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if there are multiple
                var firstIp = forwardedFor.Split(',')[0].Trim();
                return TruncateString(firstIp, 50);
            }

            // Fall back to direct connection IP
            return TruncateString(context.Connection.RemoteIpAddress?.ToString() ?? string.Empty, 50);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Get stopwatch and stop
            if (context.HttpContext.Items[StopwatchKey] is not Stopwatch stopwatch)
            {
                base.OnActionExecuted(context);
                return;
            }
            stopwatch.Stop();

            // Get captured request data
            if (context.HttpContext.Items[RequestDataKey] is not RequestCaptureData requestData)
            {
                base.OnActionExecuted(context);
                return;
            }

            // Build complete log entry
            var logEntry = new EFModels.EFModels.ApiRequestLogItem
            {
                ApiRequestLogId = Guid.NewGuid(),
                SourceSystemId = requestData.SourceSystemId,
                SourceSystemName = requestData.SourceSystemName,
                UserId = requestData.UserId,
                UserName = requestData.UserName,
                HttpMethod = requestData.HttpMethod,
                RequestPath = requestData.RequestPath,
                QueryString = requestData.QueryString,
                RequestHeaders = requestData.RequestHeaders,
                RequestedAt = requestData.RequestedAt,
                RespondedAt = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IpAddress = requestData.IpAddress,
                UserAgent = requestData.UserAgent,
                ForwardedFor = requestData.ForwardedFor,
                StatusCode = context.HttpContext.Response.StatusCode,
                IsSuccess = context.HttpContext.Response.StatusCode < 400,
                CorrelationId = context.HttpContext.TraceIdentifier,
                AuthType = requestData.AuthType,
            };

            // Handle exceptions
            if (context.Exception != null)
            {
                logEntry.ErrorMessage = TruncateString(context.Exception.Message, 1000);
                logEntry.ExceptionType = context.Exception.GetType().Name;
                logEntry.IsSuccess = false;
            }

            // Fire and forget - don't block the response
            _ = SaveLogAsync(context.HttpContext.RequestServices, logEntry);

            base.OnActionExecuted(context);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Check for skip attribute
            var skipAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<SkipApiLoggingAttribute>()
                .FirstOrDefault();
        
            if (skipAttribute != null)
            {
                base.OnActionExecuting(context);
                return;
            }

            // Start stopwatch
            var stopwatch = Stopwatch.StartNew();
            context.HttpContext.Items[StopwatchKey] = stopwatch;

            // Get options from DI
            var options = context.HttpContext.RequestServices
                .GetService<IOptions<DataObjects.ApiLoggingOptions>>()?.Value
                ?? new DataObjects.ApiLoggingOptions();

            // Capture request data
            var requestData = new RequestCaptureData
            {
                RequestedAt = DateTime.UtcNow,
                HttpMethod = context.HttpContext.Request.Method,
                RequestPath = context.HttpContext.Request.Path.ToString(),
                QueryString = context.HttpContext.Request.QueryString.ToString(),
                IpAddress = GetClientIpAddress(context.HttpContext),
                UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
                ForwardedFor = context.HttpContext.Request.Headers["X-Forwarded-For"].ToString(),
                RequestHeaders = CaptureHeaders(context.HttpContext.Request.Headers, options.SensitiveHeaders),
            };

            // Get source system from middleware if available
            if (context.HttpContext.Items["SourceSystem"] is DataObjects.SourceSystem source)
            {
                requestData.SourceSystemId = source.SourceSystemId;
                requestData.SourceSystemName = source.DisplayName;
                requestData.AuthType = "ApiKey";
            }
            else if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                requestData.UserId = context.HttpContext.User.FindFirst("sub")?.Value ?? string.Empty;
                requestData.UserName = context.HttpContext.User.Identity?.Name ?? string.Empty;
                requestData.AuthType = "JWT";
            }

            // Store for OnActionExecuted
            context.HttpContext.Items[RequestDataKey] = requestData;

            base.OnActionExecuting(context);
        }

        private static async Task SaveLogAsync(IServiceProvider serviceProvider, EFModels.EFModels.ApiRequestLogItem log)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dataAccess = scope.ServiceProvider.GetRequiredService<IDataAccess>();
                await dataAccess.CreateApiLogAsync(log);
            }
            catch (Exception ex)
            {
                // Fallback to Serilog - never throw from logging
                Log.Error(ex, "Failed to save API request log: {Path}", log.RequestPath);
            }
        }

        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        /// <summary>
        /// Internal class to hold captured request data between OnActionExecuting and OnActionExecuted.
        /// </summary>
        private class RequestCaptureData
        {
            public string AuthType { get; set; } = string.Empty;
            public string ForwardedFor { get; set; } = string.Empty;
            public string HttpMethod { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public string QueryString { get; set; } = string.Empty;
            public DateTime RequestedAt { get; set; }
            public string RequestHeaders { get; set; } = string.Empty;
            public string RequestPath { get; set; } = string.Empty;
            public Guid SourceSystemId { get; set; }
            public string SourceSystemName { get; set; } = string.Empty;
            public string UserAgent { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
        }
    }

    // ----------------------------------------------------------------------------
    // Skip Marker Attribute
    // Marks actions/controllers that should NOT be logged
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Marker attribute to skip API request logging on specific actions.
    /// Use on endpoints that would create recursive logging (e.g., log viewing endpoints)
    /// or endpoints that should not be logged for other reasons.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipApiLoggingAttribute : Attribute
    {
        /// <summary>
        /// Optional reason why logging is skipped for this endpoint.
        /// </summary>
        public string? Reason { get; set; }

        public SkipApiLoggingAttribute() { }

        public SkipApiLoggingAttribute(string reason)
        {
            Reason = reason;
        }
    }
}
