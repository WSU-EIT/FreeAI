/*
    Role: Data models for the Smartsheet API (v2) integration.
    Purpose: Typed DTOs that map to Smartsheet REST API responses.
    Notes:  JsonElement? on SmartsheetCell.Value handles the mixed
            bool/number/string/null values Smartsheet can return.
            ObjectValue (CONTACT_LIST, MULTI_PICKLIST) is intentionally
            omitted in MVP — DisplayValue is sufficient.
*/
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeBlazorExtended.Smartsheet;

// Generic paged response for /sheets and /workspaces list endpoints
public class SmartsheetApiResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public List<T> Data { get; set; } = [];
}

// Sheet — same class used for list items (thin) and GET /sheets/{id} (rich, has Columns/Rows)
public class SmartsheetSheet
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Permalink { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Populated only on GET /sheets/{id}:
    public List<SmartsheetColumn> Columns { get; set; } = [];
    public List<SmartsheetRow> Rows { get; set; } = [];

    // Pagination fields returned in the sheet-detail response envelope:
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public int? TotalPages { get; set; }
    public int? TotalRowCount { get; set; }
}

public class SmartsheetColumn
{
    public long Id { get; set; }
    public int Index { get; set; }
    public string Title { get; set; } = "";

    /// <summary>
    /// Smartsheet column type: "TEXT_NUMBER", "DATE", "DATETIME",
    /// "CHECKBOX", "PICKLIST", "CONTACT_LIST", "MULTI_PICKLIST", etc.
    /// </summary>
    public string? Type { get; set; }

    public bool Primary { get; set; }
    public List<string>? Options { get; set; }
    public bool Hidden { get; set; }
}

public class SmartsheetRow
{
    public long Id { get; set; }
    public int RowNumber { get; set; }
    public long? ParentId { get; set; }
    public bool Expanded { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<SmartsheetCell> Cells { get; set; } = [];
}

public class SmartsheetCell
{
    public long ColumnId { get; set; }

    /// <summary>
    /// JsonElement? handles the mixed bool/number/string/null Smartsheet can return.
    /// </summary>
    [JsonPropertyName("value")]
    public JsonElement? Value { get; set; }

    public string? DisplayValue { get; set; }
    public string? Formula { get; set; }
    public string? Format { get; set; }
    // ObjectValue intentionally omitted in MVP — CONTACT_LIST uses DisplayValue
}

// Phase 2 stubs:

public class SmartsheetWorkspace
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public List<SmartsheetSheet>? Sheets { get; set; }
}

public class SmartsheetFolder
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public List<SmartsheetSheet>? Sheets { get; set; }
    public List<SmartsheetFolder>? Folders { get; set; }
}
