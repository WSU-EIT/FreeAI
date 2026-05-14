using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeA11yChecker.Server.Controllers;

public partial class DataController
{
    [HttpPost]
    [AllowAnonymous]
    [Route("~/api/Data/GetPublicSites")]
    public async Task<ActionResult<List<DataObjects.Site>>> GetPublicSites([FromBody] Guid TenantId)
    {
        List<DataObjects.Site> output = await da.GetPublicSites(TenantId);
        return Ok(output);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("~/api/Data/GetPublicScanHistory")]
    public async Task<ActionResult<List<DataObjects.ScanRun>>> GetPublicScanHistory(DataObjects.ScanHistoryFilter filter)
    {
        List<DataObjects.ScanRun> output = await da.GetPublicScanHistory(filter.SiteId, filter.Count);
        return Ok(output);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("~/api/Data/GetPublicScanDetail")]
    public async Task<ActionResult<DataObjects.ScanRun?>> GetPublicScanDetail([FromBody] Guid ScanRunId)
    {
        DataObjects.ScanRun? output = await da.GetPublicScanDetail(ScanRunId);
        return Ok(output);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("~/api/Data/GetPublicPageDetail")]
    public async Task<ActionResult<DataObjects.PageScanResult?>> GetPublicPageDetail([FromBody] Guid PageScanResultId)
    {
        DataObjects.PageScanResult? output = await da.GetPublicPageDetail(PageScanResultId);
        return Ok(output);
    }
}
