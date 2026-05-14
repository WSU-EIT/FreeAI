/*
    Role: Typed HttpClient wrapper for the Smartsheet REST API v2.
    Purpose: Provides list-sheets, get-sheet, and paginated all-rows
             operations. Auth token is passed per-call and never cached.
    Registration: builder.Services.AddHttpClient<SmartsheetService>(c =>
                      c.BaseAddress = new Uri("https://api.smartsheet.com/2.0/"));
*/
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace FreeBlazorExtended.Smartsheet;

public sealed class SmartsheetService(HttpClient httpClient)
{
    private readonly HttpClient _http = httpClient;
    // BaseAddress is set to https://api.smartsheet.com/2.0/ by the host's DI registration.

    // -----------------------------------------------------------------
    // Per-call auth — token never stored as a field, never in the URL.
    // HttpRequestMessage must be constructed fresh per SendAsync call.
    // -----------------------------------------------------------------
    private static HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl, string apiToken)
    {
        var req = new HttpRequestMessage(method, relativeUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        return req;
    }

    /// <summary>
    /// Returns all sheets visible to the token owner.
    /// Uses includeAll=true which bypasses paging for reasonable account sizes.
    /// </summary>
    public async Task<List<SmartsheetSheet>> ListAllSheetsAsync(
        string apiToken,
        CancellationToken ct = default)
    {
        using var req = BuildRequest(HttpMethod.Get, "sheets?includeAll=true", apiToken);
        using var response = await _http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SmartsheetApiResponse<SmartsheetSheet>>(cancellationToken: ct);
        return result?.Data ?? [];
    }

    /// <summary>
    /// Gets a single page of sheet rows plus columns and sheet metadata.
    /// </summary>
    public async Task<SmartsheetSheet> GetSheetAsync(
        string apiToken,
        long sheetId,
        int pageSize = 500,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = $"sheets/{sheetId}?pageSize={pageSize}&page={page}";
        using var req = BuildRequest(HttpMethod.Get, url, apiToken);
        using var response = await _http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();
        var sheet = await response.Content.ReadFromJsonAsync<SmartsheetSheet>(cancellationToken: ct);
        return sheet ?? throw new InvalidOperationException("Empty response from Smartsheet API.");
    }

    /// <summary>
    /// Fetches all row pages (500 rows each) and merges them into one sheet object.
    /// Calls progress.Report(pageNumber) after each page so the UI can update.
    /// </summary>
    public async Task<SmartsheetSheet> GetSheetAllRowsAsync(
        string apiToken,
        long sheetId,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        // Page 1 — also carries columns, sheet metadata, and total pages
        var firstPage = await GetSheetAsync(apiToken, sheetId, pageSize: 500, page: 1, ct);
        progress?.Report(1);

        int totalPages = firstPage.TotalPages ?? 1;
        if (totalPages <= 1)
            return firstPage;

        var allRows = new List<SmartsheetRow>(firstPage.Rows);

        for (int page = 2; page <= totalPages; page++)
        {
            ct.ThrowIfCancellationRequested();
            var nextPage = await GetSheetAsync(apiToken, sheetId, pageSize: 500, page: page, ct);
            allRows.AddRange(nextPage.Rows);
            progress?.Report(page);
        }

        firstPage.Rows = allRows;
        return firstPage;
    }
}
