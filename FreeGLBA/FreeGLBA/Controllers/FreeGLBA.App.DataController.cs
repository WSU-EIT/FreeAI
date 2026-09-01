using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FreeGLBA.Controllers;

namespace FreeGLBA.Server.Controllers;

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
