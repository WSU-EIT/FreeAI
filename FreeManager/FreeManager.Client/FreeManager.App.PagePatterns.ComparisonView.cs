namespace FreeManager.Client;

#region PagePatterns - ComparisonView
// ============================================================================
// PAGE PATTERN: COMPARISON VIEW
// Side-by-side comparison for versions or diffs
//
// Generated Files:
// CompareView.razor, VersionSelector.razor, SideBySide.razor, FieldDiff.razor
// ============================================================================

public static partial class PagePatterns
{
    /// <summary>
    /// Generate ComparisonView files for the given entity/project.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetComparisonViewFiles(
        string projectName,
        DataObjects.EntityDefinition? entity = null)
    {
        List<(string, string, string)> files = new();
        string name = entity?.Name ?? projectName;

        files.Add(($"{projectName}.App.{name}CompareView.razor", "RazorComponent", GetComparisonView_CompareView(name, entity)));
        files.Add(($"{projectName}.App.{name}VersionSelector.razor", "RazorComponent", GetComparisonView_VersionSelector(name, entity)));
        files.Add(($"{projectName}.App.{name}SideBySide.razor", "RazorComponent", GetComparisonView_SideBySide(name, entity)));
        files.Add(($"{projectName}.App.{name}FieldDiff.razor", "RazorComponent", GetComparisonView_FieldDiff(name, entity)));

        return files;
    }

    // ============================================================
    // CompareView.razor - Main comparison container
    // ============================================================
    private static string GetComparisonView_CompareView(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}CompareView.App.razor *@
@* Main comparison container for version history and diffs *@
@page ""/{name}/compare""
@page ""/{{TenantCode}}/{name}/compare""
@page ""/{name}/compare/{{ItemId:guid}}""
@page ""/{{TenantCode}}/{name}/compare/{{ItemId:guid}}""
@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http
@inject NavigationManager Nav

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <div class=""@Model.StickyMenuClass"">
            <div class=""d-flex justify-content-between align-items-center"">
                <h1 class=""page-title mb-0"">
                    <i class=""fa-solid fa-code-compare me-2""></i>{name} Version Comparison
                </h1>
                <div class=""d-flex gap-2"">
                    <button class=""btn btn-outline-secondary"" @onclick=""GoBack"">
                        <i class=""fa-solid fa-arrow-left me-1""></i>Back
                    </button>
                    @if (_canRestore && _selectedRightVersion != null) {{
                        <button class=""btn btn-warning"" @onclick=""RestoreVersion"">
                            <i class=""fa-solid fa-clock-rotate-left me-1""></i>Restore This Version
                        </button>
                    }}
                </div>
            </div>
        </div>

        @if (_loading) {{
            <div class=""text-center py-5"">
                <i class=""fa-solid fa-spinner fa-spin fa-2x""></i>
                <p class=""mt-2 text-muted"">Loading version history...</p>
            </div>
        }} else if (_versions.Count == 0) {{
            <div class=""text-center py-5 text-muted"">
                <i class=""fa-solid fa-clock-rotate-left fa-4x mb-3 opacity-50""></i>
                <h4>No version history available</h4>
                <p>Changes to this item will be tracked here.</p>
            </div>
        }} else {{
            <div class=""card shadow-sm mb-3"">
                <div class=""card-body"">
                    <{name}_App_VersionSelector
                        Versions=""_versions""
                        SelectedLeftVersion=""_selectedLeftVersion""
                        SelectedRightVersion=""_selectedRightVersion""
                        OnLeftVersionChanged=""OnLeftVersionChanged""
                        OnRightVersionChanged=""OnRightVersionChanged"" />
                </div>
            </div>

            @if (_selectedLeftVersion != null && _selectedRightVersion != null) {{
                <{name}_App_SideBySide
                    LeftVersion=""_selectedLeftVersion""
                    RightVersion=""_selectedRightVersion""
                    Differences=""_differences"" />
            }} else {{
                <div class=""alert alert-info"">
                    <i class=""fa-solid fa-info-circle me-2""></i>
                    Select two versions above to compare them.
                </div>
            }}
        }}
    </div>

    @if (_showRestoreConfirm) {{
        <div class=""modal fade show d-block"" tabindex=""-1"" style=""background: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header"">
                        <h5 class=""modal-title"">
                            <i class=""fa-solid fa-clock-rotate-left me-2""></i>Restore Version
                        </h5>
                        <button type=""button"" class=""btn-close"" @onclick=""CancelRestore""></button>
                    </div>
                    <div class=""modal-body"">
                        <p>Are you sure you want to restore this version?</p>
                        <p class=""text-muted small"">
                            <strong>Version:</strong> @_selectedRightVersion?.VersionNumber<br />
                            <strong>Created:</strong> @_selectedRightVersion?.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")<br />
                            <strong>By:</strong> @_selectedRightVersion?.CreatedBy
                        </p>
                        <div class=""alert alert-warning mb-0"">
                            <i class=""fa-solid fa-exclamation-triangle me-2""></i>
                            This will create a new version with the restored data.
                        </div>
                    </div>
                    <div class=""modal-footer"">
                        <button type=""button"" class=""btn btn-secondary"" @onclick=""CancelRestore"">Cancel</button>
                        <button type=""button"" class=""btn btn-warning"" @onclick=""ConfirmRestore"">
                            <i class=""fa-solid fa-clock-rotate-left me-1""></i>Restore
                        </button>
                    </div>
                </div>
            </div>
        </div>
    }}
}}

@code {{
    [Parameter] public Guid? ItemId {{ get; set; }}

    private bool _loading = true;
    private bool _showRestoreConfirm = false;
    private bool _canRestore = true;
    private List<{name}Version> _versions = new();
    private {name}Version? _selectedLeftVersion;
    private {name}Version? _selectedRightVersion;
    private List<FieldDifference> _differences = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) await LoadVersions();
    }}

    private async Task LoadVersions()
    {{
        _loading = true;
        StateHasChanged();

        if (ItemId.HasValue)
        {{
            // Load version history for the item
            // _versions = await Helpers.GetOrPost<List<{name}Version>>(
            //     DataObjects.Endpoints.{name}.GetVersions, ItemId.Value) ?? new();

            // Mock data for template demonstration
            _versions = new List<{name}Version>
            {{
                new() {{ VersionNumber = 3, CreatedAt = DateTime.UtcNow.AddHours(-1), CreatedBy = ""Current User"", IsCurrent = true }},
                new() {{ VersionNumber = 2, CreatedAt = DateTime.UtcNow.AddDays(-1), CreatedBy = ""Other User"" }},
                new() {{ VersionNumber = 1, CreatedAt = DateTime.UtcNow.AddDays(-7), CreatedBy = ""Current User"" }}
            }};

            if (_versions.Count >= 2)
            {{
                _selectedLeftVersion = _versions[1];
                _selectedRightVersion = _versions[0];
                await ComputeDifferences();
            }}
        }}

        _loading = false;
        StateHasChanged();
    }}

    private async Task OnLeftVersionChanged({name}Version version)
    {{
        _selectedLeftVersion = version;
        await ComputeDifferences();
    }}

    private async Task OnRightVersionChanged({name}Version version)
    {{
        _selectedRightVersion = version;
        _canRestore = !version.IsCurrent;
        await ComputeDifferences();
    }}

    private Task ComputeDifferences()
    {{
        _differences.Clear();

        if (_selectedLeftVersion != null && _selectedRightVersion != null)
        {{
            // Compare the two versions and compute field differences
            // This would typically call an API or compare locally
            // Mock data for template demonstration
            _differences = new List<FieldDifference>
            {{
                new() {{ FieldName = ""Name"", OldValue = ""Old Name"", NewValue = ""Updated Name"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Description"", OldValue = ""Previous description"", NewValue = ""New description with more details"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Status"", OldValue = ""Draft"", NewValue = ""Published"", ChangeType = DiffChangeType.Modified }},
                new() {{ FieldName = ""Tags"", OldValue = null, NewValue = ""important, featured"", ChangeType = DiffChangeType.Added }},
                new() {{ FieldName = ""LegacyField"", OldValue = ""old value"", NewValue = null, ChangeType = DiffChangeType.Removed }}
            }};
        }}

        StateHasChanged();
        return Task.CompletedTask;
    }}

    private void GoBack() => Nav.NavigateTo($""/{name}"");

    private void RestoreVersion() => _showRestoreConfirm = true;

    private void CancelRestore() => _showRestoreConfirm = false;

    private async Task ConfirmRestore()
    {{
        if (_selectedRightVersion != null)
        {{
            // await Helpers.GetOrPost<bool>(
            //     DataObjects.Endpoints.{name}.RestoreVersion,
            //     new {{ ItemId = ItemId, VersionNumber = _selectedRightVersion.VersionNumber }});

            _showRestoreConfirm = false;
            await LoadVersions();
        }}
    }}

    // Version model
    public class {name}Version
    {{
        public int VersionNumber {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public string CreatedBy {{ get; set; }} = string.Empty;
        public bool IsCurrent {{ get; set; }}
        public Dictionary<string, object?> Data {{ get; set; }} = new();
    }}

    // Field difference model
    public class FieldDifference
    {{
        public string FieldName {{ get; set; }} = string.Empty;
        public object? OldValue {{ get; set; }}
        public object? NewValue {{ get; set; }}
        public DiffChangeType ChangeType {{ get; set; }}
    }}

    public enum DiffChangeType {{ Added, Removed, Modified, Unchanged }}
}}
";

    // ============================================================
    // VersionSelector.razor - Dropdown to select versions to compare
    // ============================================================
    private static string GetComparisonView_VersionSelector(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}VersionSelector.App.razor *@
@* Dropdown selectors for choosing versions to compare *@

<div class=""row g-3 align-items-end"">
    <div class=""col-md-5"">
        <label class=""form-label fw-bold"">
            <i class=""fa-solid fa-arrow-left me-1 text-danger""></i>Left (Older)
        </label>
        <select class=""form-select"" value=""@SelectedLeftVersion?.VersionNumber"" @onchange=""OnLeftChanged"">
            <option value="""">Select a version...</option>
            @foreach (var version in Versions.Where(v => v != SelectedRightVersion))
            {{
                <option value=""@version.VersionNumber"">
                    Version @version.VersionNumber - @version.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")
                    @if (version.IsCurrent) {{ <text> (Current)</text> }}
                </option>
            }}
        </select>
    </div>

    <div class=""col-md-2 text-center"">
        <button class=""btn btn-outline-secondary"" @onclick=""SwapVersions"" title=""Swap versions"">
            <i class=""fa-solid fa-arrows-left-right""></i>
        </button>
    </div>

    <div class=""col-md-5"">
        <label class=""form-label fw-bold"">
            <i class=""fa-solid fa-arrow-right me-1 text-success""></i>Right (Newer)
        </label>
        <select class=""form-select"" value=""@SelectedRightVersion?.VersionNumber"" @onchange=""OnRightChanged"">
            <option value="""">Select a version...</option>
            @foreach (var version in Versions.Where(v => v != SelectedLeftVersion))
            {{
                <option value=""@version.VersionNumber"">
                    Version @version.VersionNumber - @version.CreatedAt.ToString(""MMM dd, yyyy HH:mm"")
                    @if (version.IsCurrent) {{ <text> (Current)</text> }}
                </option>
            }}
        </select>
    </div>
</div>

@if (SelectedLeftVersion != null && SelectedRightVersion != null)
{{
    <div class=""row mt-3"">
        <div class=""col-md-5"">
            <div class=""card bg-light"">
                <div class=""card-body py-2"">
                    <small class=""text-muted"">
                        <i class=""fa-solid fa-user me-1""></i>@SelectedLeftVersion.CreatedBy
                        <br />
                        <i class=""fa-solid fa-clock me-1""></i>@SelectedLeftVersion.CreatedAt.ToString(""MMM dd, yyyy HH:mm:ss"")
                    </small>
                </div>
            </div>
        </div>
        <div class=""col-md-2""></div>
        <div class=""col-md-5"">
            <div class=""card bg-light"">
                <div class=""card-body py-2"">
                    <small class=""text-muted"">
                        <i class=""fa-solid fa-user me-1""></i>@SelectedRightVersion.CreatedBy
                        <br />
                        <i class=""fa-solid fa-clock me-1""></i>@SelectedRightVersion.CreatedAt.ToString(""MMM dd, yyyy HH:mm:ss"")
                    </small>
                </div>
            </div>
        </div>
    </div>
}}

@code {{
    [Parameter] public List<{name}_App_CompareView.{name}Version> Versions {{ get; set; }} = new();
    [Parameter] public {name}_App_CompareView.{name}Version? SelectedLeftVersion {{ get; set; }}
    [Parameter] public {name}_App_CompareView.{name}Version? SelectedRightVersion {{ get; set; }}
    [Parameter] public EventCallback<{name}_App_CompareView.{name}Version> OnLeftVersionChanged {{ get; set; }}
    [Parameter] public EventCallback<{name}_App_CompareView.{name}Version> OnRightVersionChanged {{ get; set; }}

    private async Task OnLeftChanged(ChangeEventArgs e)
    {{
        if (int.TryParse(e.Value?.ToString(), out var versionNumber))
        {{
            var version = Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version != null)
            {{
                await OnLeftVersionChanged.InvokeAsync(version);
            }}
        }}
    }}

    private async Task OnRightChanged(ChangeEventArgs e)
    {{
        if (int.TryParse(e.Value?.ToString(), out var versionNumber))
        {{
            var version = Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version != null)
            {{
                await OnRightVersionChanged.InvokeAsync(version);
            }}
        }}
    }}

    private async Task SwapVersions()
    {{
        var temp = SelectedLeftVersion;
        if (SelectedRightVersion != null)
            await OnLeftVersionChanged.InvokeAsync(SelectedRightVersion);
        if (temp != null)
            await OnRightVersionChanged.InvokeAsync(temp);
    }}
}}
";

    // ============================================================
    // SideBySide.razor - Two-column layout for old vs new
    // ============================================================
    private static string GetComparisonView_SideBySide(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}SideBySide.App.razor *@
@* Side-by-side comparison layout for version differences *@

<div class=""card shadow-sm"">
    <div class=""card-header"">
        <div class=""row"">
            <div class=""col-6"">
                <h6 class=""mb-0"">
                    <i class=""fa-solid fa-arrow-left me-1 text-danger""></i>
                    Version @LeftVersion?.VersionNumber
                    <span class=""text-muted small"">(@LeftVersion?.CreatedAt.ToString(""MMM dd, yyyy""))</span>
                </h6>
            </div>
            <div class=""col-6"">
                <h6 class=""mb-0"">
                    <i class=""fa-solid fa-arrow-right me-1 text-success""></i>
                    Version @RightVersion?.VersionNumber
                    @if (RightVersion?.IsCurrent == true) {{ <span class=""badge bg-primary ms-1"">Current</span> }}
                    <span class=""text-muted small"">(@RightVersion?.CreatedAt.ToString(""MMM dd, yyyy""))</span>
                </h6>
            </div>
        </div>
    </div>
    <div class=""card-body p-0"">
        @if (Differences.Count == 0)
        {{
            <div class=""text-center py-4 text-muted"">
                <i class=""fa-solid fa-equals fa-2x mb-2""></i>
                <p>No differences found between these versions.</p>
            </div>
        }}
        else
        {{
            <div class=""comparison-summary p-3 bg-light border-bottom"">
                <span class=""badge bg-success me-2"">
                    <i class=""fa-solid fa-plus me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Added) Added
                </span>
                <span class=""badge bg-danger me-2"">
                    <i class=""fa-solid fa-minus me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Removed) Removed
                </span>
                <span class=""badge bg-warning text-dark"">
                    <i class=""fa-solid fa-pen me-1""></i>@Differences.Count(d => d.ChangeType == {name}_App_CompareView.DiffChangeType.Modified) Modified
                </span>
            </div>

            <div class=""table-responsive"">
                <table class=""table table-hover align-middle mb-0"">
                    <thead class=""table-light"">
                        <tr>
                            <th style=""width: 15%;"">Field</th>
                            <th style=""width: 5%;"">Change</th>
                            <th style=""width: 40%;"">Old Value</th>
                            <th style=""width: 40%;"">New Value</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var diff in Differences)
                        {{
                            <{name}_App_FieldDiff Difference=""diff"" />
                        }}
                    </tbody>
                </table>
            </div>
        }}
    </div>
</div>

<style>
    .diff-added {{
        background-color: rgba(25, 135, 84, 0.1);
    }}
    .diff-removed {{
        background-color: rgba(220, 53, 69, 0.1);
    }}
    .diff-modified {{
        background-color: rgba(255, 193, 7, 0.1);
    }}
    .diff-value-old {{
        text-decoration: line-through;
        color: #dc3545;
    }}
    .diff-value-new {{
        color: #198754;
        font-weight: 500;
    }}
    .diff-value-null {{
        font-style: italic;
        color: #6c757d;
    }}
</style>

@code {{
    [Parameter] public {name}_App_CompareView.{name}Version? LeftVersion {{ get; set; }}
    [Parameter] public {name}_App_CompareView.{name}Version? RightVersion {{ get; set; }}
    [Parameter] public List<{name}_App_CompareView.FieldDifference> Differences {{ get; set; }} = new();
}}
";

    // ============================================================
    // FieldDiff.razor - Field-by-field difference display
    // ============================================================
    private static string GetComparisonView_FieldDiff(string name, DataObjects.EntityDefinition? entity) => $@"@* {name}FieldDiff.App.razor *@
@* Individual field difference row with change highlighting *@

<tr class=""@GetRowClass()"">
    <td>
        <strong>@Difference.FieldName</strong>
    </td>
    <td class=""text-center"">
        @switch (Difference.ChangeType)
        {{
            case {name}_App_CompareView.DiffChangeType.Added:
                <span class=""badge bg-success"" title=""Added"">
                    <i class=""fa-solid fa-plus""></i>
                </span>
                break;
            case {name}_App_CompareView.DiffChangeType.Removed:
                <span class=""badge bg-danger"" title=""Removed"">
                    <i class=""fa-solid fa-minus""></i>
                </span>
                break;
            case {name}_App_CompareView.DiffChangeType.Modified:
                <span class=""badge bg-warning text-dark"" title=""Modified"">
                    <i class=""fa-solid fa-pen""></i>
                </span>
                break;
            default:
                <span class=""badge bg-secondary"" title=""Unchanged"">
                    <i class=""fa-solid fa-equals""></i>
                </span>
                break;
        }}
    </td>
    <td>
        @if (Difference.OldValue == null)
        {{
            <span class=""diff-value-null"">(empty)</span>
        }}
        else
        {{
            <span class=""@(Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Removed || Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Modified ? ""diff-value-old"" : """")"">
                @FormatValue(Difference.OldValue)
            </span>
        }}
    </td>
    <td>
        @if (Difference.NewValue == null)
        {{
            <span class=""diff-value-null"">(empty)</span>
        }}
        else
        {{
            <span class=""@(Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Added || Difference.ChangeType == {name}_App_CompareView.DiffChangeType.Modified ? ""diff-value-new"" : """")"">
                @FormatValue(Difference.NewValue)
            </span>
        }}
    </td>
</tr>

@code {{
    [Parameter] public {name}_App_CompareView.FieldDifference Difference {{ get; set; }} = new();

    private string GetRowClass()
    {{
        return Difference.ChangeType switch
        {{
            {name}_App_CompareView.DiffChangeType.Added => ""diff-added"",
            {name}_App_CompareView.DiffChangeType.Removed => ""diff-removed"",
            {name}_App_CompareView.DiffChangeType.Modified => ""diff-modified"",
            _ => """"
        }};
    }}

    private string FormatValue(object? value)
    {{
        if (value == null) return ""(empty)"";

        return value switch
        {{
            DateTime dt => dt.ToString(""MMM dd, yyyy HH:mm""),
            bool b => b ? ""Yes"" : ""No"",
            IEnumerable<string> list => string.Join("", "", list),
            _ => value.ToString() ?? """"
        }};
    }}
}}
";
}

#endregion
