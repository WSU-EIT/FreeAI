/*
    Role: Core Foundation configuration and query-state contracts.
    Purpose: Defines the option, column, filter, and sort models used by the shared table stack.
    Contains: DataTable feature toggles, column metadata, and caller-managed filter and sort descriptors.
    Used by: Foundation table primitives and any host feature that composes table behavior around shared options.
*/
namespace FreeBlazorExtended.Foundation;

/// <summary>
/// Feature-toggle and visual-configuration object for Foundation table components.
/// Construct one instance per table and pass it via the component's <c>Options</c> parameter.
/// All members have safe defaults so you only need to set the flags that differ from the default.
/// </summary>
public class DataTableOptions
{
    /// <summary>
    /// Allows the user to sort rows by clicking column headers.
    /// Default: <c>true</c>. Set to <c>false</c> for read-only display tables
    /// where ordering is controlled entirely by the caller.
    /// </summary>
    public bool EnableSorting { get; set; } = true;

    /// <summary>
    /// Shows a per-column search input so the user can narrow rows by value.
    /// Default: <c>true</c>. Disable for very large server-paginated datasets
    /// where client-side filtering is impractical.
    /// </summary>
    public bool EnableFiltering { get; set; } = true;

    /// <summary>
    /// Shows page-navigation controls below the table.
    /// Default: <c>true</c>. Set to <c>false</c> when the caller manages paging
    /// externally (e.g., server-side pagination via a custom toolbar).
    /// </summary>
    public bool EnablePagination { get; set; } = true;

    /// <summary>
    /// Number of rows displayed per page when <see cref="EnablePagination"/> is <c>true</c>.
    /// Default: <c>25</c>. Common values: 10, 25, 50, 100.
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Renders a leading checkbox column so the user can select individual rows.
    /// Default: <c>false</c>. Enable when the table drives a bulk-action workflow.
    /// Wire the component's <c>OnSelectionChanged</c> callback to react to changes.
    /// </summary>
    public bool EnableSelection { get; set; } = false;

    /// <summary>
    /// Allows rows to expand in place to reveal additional detail content.
    /// Default: <c>false</c>. Enable together with an <c>ExpansionContent</c>
    /// render fragment to show nested or supplemental row data on demand.
    /// </summary>
    public bool EnableExpansion { get; set; } = false;

    /// <summary>
    /// Shows a bulk-action toolbar when one or more rows are selected.
    /// Default: <c>false</c>. Pair with <see cref="EnableSelection"/> and supply
    /// <c>BulkActionsContent</c> to expose contextual operations (delete, export, etc.).
    /// </summary>
    public bool EnableBulkActions { get; set; } = false;

    /// <summary>
    /// Renders a column-visibility picker so the user can show/hide individual columns.
    /// Default: <c>false</c>. Useful for wide tables with many optional fields.
    /// </summary>
    public bool EnableColumnSelector { get; set; } = false;

    /// <summary>
    /// Shows an export button (CSV/Excel) in the table toolbar.
    /// Default: <c>false</c>. The host must wire an <c>OnExport</c> callback;
    /// this flag only controls whether the button is visible.
    /// </summary>
    public bool EnableExport { get; set; } = false;

    /// <summary>
    /// Renders animated skeleton rows while data is loading.
    /// Default: <c>true</c>. Set to <c>false</c> if you prefer a spinner overlay.
    /// </summary>
    public bool ShowSkeletonLoading { get; set; } = true;

    /// <summary>
    /// Applies alternating row background shading (Bootstrap <c>table-striped</c>).
    /// Default: <c>true</c>. Set to <c>false</c> for a flat, uniform look.
    /// </summary>
    public bool Striped { get; set; } = true;

    /// <summary>
    /// Highlights the row under the cursor on hover (Bootstrap <c>table-hover</c>).
    /// Default: <c>true</c>. Disable for purely informational tables.
    /// </summary>
    public bool Hover { get; set; } = true;

    /// <summary>
    /// Draws borders on all cells (Bootstrap <c>table-bordered</c>).
    /// Default: <c>false</c>. Enable for dense data grids where cell boundaries
    /// improve readability.
    /// </summary>
    public bool Bordered { get; set; } = false;

    /// <summary>
    /// Reduces vertical cell padding for a denser layout (Bootstrap <c>table-sm</c>).
    /// Default: <c>false</c>. Useful when screen real estate is limited.
    /// </summary>
    public bool Compact { get; set; } = false;

    /// <summary>
    /// The <see cref="DataTableColumn.Key"/> of the column to sort by on first render.
    /// Default: empty string (no initial sort). Must match one of the column keys
    /// passed in the column definition list.
    /// </summary>
    public string DefaultSortColumn { get; set; } = "";

    /// <summary>
    /// Initial sort direction when <see cref="DefaultSortColumn"/> is set.
    /// Accepted values: <c>"asc"</c> (default) or <c>"desc"</c>.
    /// </summary>
    public string DefaultSortDirection { get; set; } = "asc";
}

/// <summary>
/// Describes a single column in a Foundation data table. Build a list of these
/// and pass them to the table component's column-definition parameter.
/// </summary>
public class DataTableColumn
{
    /// <summary>
    /// Unique identifier for the column. Must match the property name on the row model
    /// when using automatic reflection-based rendering, or the key used in a custom
    /// template render fragment.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// The text displayed in the column header cell. Should be concise and human-readable.
    /// </summary>
    public string Header { get; set; } = "";

    /// <summary>
    /// Controls whether this column is rendered. Default: <c>true</c>.
    /// Toggle at runtime to let users show/hide columns via <see cref="DataTableOptions.EnableColumnSelector"/>.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether clicking the column header cycles through sort directions.
    /// Default: <c>true</c>. Set to <c>false</c> for computed or multi-source columns
    /// where server-side sorting is not supported.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Whether a filter input is rendered for this column.
    /// Default: <c>true</c>. Disable for ID columns or columns that should never be filtered.
    /// </summary>
    public bool Filterable { get; set; } = true;

    /// <summary>
    /// CSS width for the column (e.g. <c>"120px"</c>, <c>"15%"</c>, <c>"auto"</c>).
    /// Default: <c>"auto"</c> — the browser distributes remaining width evenly.
    /// </summary>
    public string Width { get; set; } = "auto";

    /// <summary>
    /// Horizontal text alignment within cells. Accepted values: <c>"left"</c> (default),
    /// <c>"center"</c>, <c>"right"</c>.
    /// </summary>
    public string Align { get; set; } = "left";

    /// <summary>
    /// Optional .NET format string applied when the cell value is rendered as a string
    /// (e.g. <c>"N2"</c> for two decimal places, <c>"yyyy-MM-dd"</c> for dates).
    /// Ignored when a custom cell template is used.
    /// </summary>
    public string Format { get; set; } = "";

    /// <summary>
    /// Additional CSS class(es) applied to every <c>&lt;td&gt;</c> in this column.
    /// Useful for alignment helpers (<c>"text-end"</c>) or width constraints.
    /// </summary>
    public string CssClass { get; set; } = "";
}

/// <summary>The direction rows are ordered in a sorted column.</summary>
public enum SortDirection
{
    /// <summary>Smallest to largest, A–Z, oldest to newest.</summary>
    Ascending,
    /// <summary>Largest to smallest, Z–A, newest to oldest.</summary>
    Descending,
    /// <summary>No sort applied; rows appear in their original source order.</summary>
    None
}

/// <summary>
/// Comparison operator applied when a <see cref="DataTableFilter"/> is evaluated
/// against a cell value.
/// </summary>
public enum FilterCondition
{
    /// <summary>Cell value must equal the filter value exactly.</summary>
    Equals,
    /// <summary>Cell value must not equal the filter value.</summary>
    NotEquals,
    /// <summary>Cell value must contain the filter string (default for text search).</summary>
    Contains,
    /// <summary>Cell value must begin with the filter string.</summary>
    StartsWith,
    /// <summary>Cell value must end with the filter string.</summary>
    EndsWith,
    /// <summary>Cell value must be strictly greater than the filter value.</summary>
    GreaterThan,
    /// <summary>Cell value must be greater than or equal to the filter value.</summary>
    GreaterThanOrEqual,
    /// <summary>Cell value must be strictly less than the filter value.</summary>
    LessThan,
    /// <summary>Cell value must be less than or equal to the filter value.</summary>
    LessThanOrEqual,
    /// <summary>Cell value must fall between the two bounds in the filter's value collection.</summary>
    Between,
    /// <summary>Cell value must appear in the filter's value collection.</summary>
    In
}

/// <summary>
/// Describes an active filter applied to one column of a Foundation data table.
/// Build a list of these and pass them to the table component's filter parameter.
/// </summary>
public class DataTableFilter
{
    /// <summary>
    /// The <see cref="DataTableColumn.Key"/> of the column this filter targets.
    /// Must match a key in the table's column definition list.
    /// </summary>
    public string Column { get; set; } = "";

    /// <summary>
    /// How the filter value is compared against the cell value.
    /// Default: <see cref="FilterCondition.Contains"/>.
    /// Choose <see cref="FilterCondition.Equals"/> for exact matching (e.g., status dropdowns).
    /// </summary>
    public FilterCondition Condition { get; set; } = FilterCondition.Contains;

    /// <summary>
    /// The value to compare against. The type should match the column's data type
    /// (e.g., <see cref="string"/> for text columns, <see cref="DateTime"/> for date columns).
    /// For <see cref="FilterCondition.Between"/>, pass a two-element array or tuple.
    /// For <see cref="FilterCondition.In"/>, pass an <see cref="IEnumerable{T}"/>.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// When <c>false</c> (default), string comparisons ignore case.
    /// Set to <c>true</c> for case-sensitive filtering (e.g., reference codes).
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Describes the current sort state applied to a Foundation data table.
/// Pass a single instance to the table's sort parameter to establish initial ordering.
/// </summary>
public class DataTableSort
{
    /// <summary>
    /// The <see cref="DataTableColumn.Key"/> of the column to sort by.
    /// Must match a key in the table's column definition list.
    /// </summary>
    public string Column { get; set; } = "";

    /// <summary>
    /// The direction to sort in.
    /// Default: <see cref="SortDirection.Ascending"/>.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}
