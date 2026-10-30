using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeLLM.Server.Controllers;

// FreeLLM prompt packager file endpoints. The file reading lives in FreeLLM.App.DataAccess.cs.

public partial class DataController
{
    #region File Operations

    // POST: api/Data/GetFileContents
    // Retrieves full file contents for a given list of files.
    [HttpPost($"~/{DataObjects.Endpoints.FreeLLM.GetFileContents}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public async Task<IActionResult> FreeLLMGetFileContents([FromBody] DataObjects.FreeLLMFileContentRequest request)
    {
        return Ok(await da.FreeLLMGetFileContents(request.FilePaths));
    }

    // POST: api/Data/GetFileMetadata
    // Retrieves only metadata (line count and character count) for a given list of files.
    [HttpPost($"~/{DataObjects.Endpoints.FreeLLM.GetFileMetadata}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public async Task<IActionResult> FreeLLMGetFileMetadata([FromBody] DataObjects.FreeLLMFileContentRequest request)
    {
        return Ok(await da.FreeLLMGetFileMetadata(request.FilePaths));
    }

    // POST: api/Data/GetFiles
    // Retrieves a list of files (excluding bin/obj folders and hidden folders).
    [HttpPost($"~/{DataObjects.Endpoints.FreeLLM.GetFiles}")]
    [Authorize(Policy = Policies.AppAdmin)]
    public IActionResult FreeLLMGetFiles([FromBody] DataObjects.FreeLLMFilePathRequest request)
    {
        var files = da.FreeLLMGetFiles(request.Path);
        if (files == null) {
            return BadRequest("Directory does not exist");
        }

        return Ok(files);
    }
    #endregion File Operations
}
