namespace FreeManager.Client;

#region ProjectTemplates - Starter Templates
// ============================================================================
// PROJECT TEMPLATES - STARTER
// Working example with Items list using Settings table storage.
// Part of: ProjectTemplates.App (partial)
// ============================================================================

public static partial class ProjectTemplates
{
    // ============================================================
    // STARTER TEMPLATES (Working example with Settings storage)
    // ============================================================

    private static string GetStarterDataObjects(string name) => $@"using System.Text.Json.Serialization;

namespace FreeManager;

#region {name} DataObjects
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// This template provides a working Items list stored in the Settings table.
// No database migration required!
// ============================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            public const string GetItems = ""api/Data/{name}_GetItems"";
            public const string SaveItem = ""api/Data/{name}_SaveItem"";
            public const string DeleteItem = ""api/Data/{name}_DeleteItem"";
        }}
    }}

    /// <summary>
    /// {name} item - stored as JSON in Settings table.
    /// </summary>
    public class {name}Item
    {{
        public Guid Id {{ get; set; }} = Guid.NewGuid();
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }} = false;
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        public DateTime? CompletedAt {{ get; set; }}
    }}

    /// <summary>
    /// Request to save an item.
    /// </summary>
    public class {name}SaveRequest
    {{
        public Guid? Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }} = false;
    }}
}}

#endregion
";

    private static string GetStarterDataAccess(string name) => $@"using System.Text.Json;

namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// Business logic using Settings table for JSON storage.
// ============================================================================

public partial interface IDataAccess
{{
    Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    private const string {name}SettingsKey = ""{name}_Items"";

    public async Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser)
    {{
        var items = await {name}_LoadItems(CurrentUser.TenantId);
        return items.OrderByDescending(x => x.CreatedAt).ToList();
    }}

    public async Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser)
    {{
        List<DataObjects.{name}Item> items = await {name}_LoadItems(CurrentUser.TenantId);
        DataObjects.{name}Item item;

        if (request.Id.HasValue && request.Id != Guid.Empty) {{
            item = items.FirstOrDefault(x => x.Id == request.Id.Value) ?? new DataObjects.{name}Item();
            item.Name = request.Name;
            item.Description = request.Description;

            if (request.IsComplete && !item.IsComplete) {{
                item.CompletedAt = DateTime.UtcNow;
            }} else if (!request.IsComplete) {{
                item.CompletedAt = null;
            }}
            item.IsComplete = request.IsComplete;

            if (!items.Any(x => x.Id == item.Id)) {{
                items.Add(item);
            }}
        }} else {{
            item = new DataObjects.{name}Item {{
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsComplete = request.IsComplete,
                CreatedAt = DateTime.UtcNow
            }};
            items.Add(item);
        }}

        await {name}_SaveItems(items, CurrentUser.TenantId);
        return item;
    }}

    public async Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        DataObjects.BooleanResponse output = new();
        List<DataObjects.{name}Item> items = await {name}_LoadItems(CurrentUser.TenantId);

        int removed = items.RemoveAll(x => x.Id == itemId);
        if (removed > 0) {{
            await {name}_SaveItems(items, CurrentUser.TenantId);
            output.Result = true;
        }} else {{
            output.Messages.Add(""Item not found"");
        }}

        return output;
    }}

    private async Task<List<DataObjects.{name}Item>> {name}_LoadItems(Guid tenantId)
    {{
        DataObjects.Setting? setting = await GetSetting({name}SettingsKey, tenantId);
        if (setting == null || string.IsNullOrEmpty(setting.Value)) {{
            return new List<DataObjects.{name}Item>();
        }}
        return JsonSerializer.Deserialize<List<DataObjects.{name}Item>>(setting.Value) ?? new();
    }}

    private async Task {name}_SaveItems(List<DataObjects.{name}Item> items, Guid tenantId)
    {{
        string json = JsonSerializer.Serialize(items);
        await SaveSetting({name}SettingsKey, json, tenantId);
    }}
}}

#endregion
";

    private static string GetStarterController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// REST API endpoints for {name} items.
// ============================================================================

public partial class DataController
{{
    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItems}}"")]
    public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems()
    {{
        return await da.{name}_GetItems(CurrentUser);
    }}

    [HttpPost]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.SaveItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_SaveItem([FromBody] DataObjects.{name}SaveRequest request)
    {{
        return await da.{name}_SaveItem(request, CurrentUser);
    }}

    [HttpDelete]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.DeleteItem}}"")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> {name}_DeleteItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_DeleteItem(itemId, CurrentUser);
    }}
}}

#endregion
";

    private static string GetStarterGlobalSettings(string name) => $@"namespace FreeManager;

#region {name} Settings
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// App configuration and constants.
// ============================================================================

public static partial class GlobalSettings
{{
    public static class {name}
    {{
        public static string AppName {{ get; set; }} = ""{name}"";
        public static string Version {{ get; set; }} = ""1.0.0"";
        public static string Description {{ get; set; }} = ""A {name} application built with FreeManager"";
    }}
}}

#endregion
";

    private static string GetStarterComponent(string name) => $@"@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@* ============================================================================
   {name} Component - STARTER TEMPLATE
   Card-based list view with add/edit/delete functionality.
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <h1 class=""page-title"">
            <i class=""fa-solid fa-list-check me-2""></i>
            {name} Items
        </h1>

        <div class=""mb-3"">
            <button class=""btn btn-success"" @onclick=""ShowAddModal"">
                <i class=""fa-solid fa-plus me-1""></i>
                Add Item
            </button>
        </div>

        @if (_loading) {{
            <LoadingMessage />
        }} else {{
            @if (_items.Count == 0) {{
                <div class=""alert alert-info"">
                    <i class=""fa-solid fa-info-circle me-2""></i>
                    No items yet. Click ""Add Item"" to create your first one!
                </div>
            }} else {{
                <div class=""row"">
                    @foreach (var item in _items) {{
                        <div class=""col-md-4 mb-3"">
                            <div class=""card @(item.IsComplete ? ""border-success"" : """")"">
                                <div class=""card-body"">
                                    <h5 class=""card-title"">
                                        @if (item.IsComplete) {{
                                            <i class=""fa-solid fa-check-circle text-success me-1""></i>
                                        }}
                                        @item.Name
                                    </h5>
                                    <p class=""card-text text-muted"">@item.Description</p>
                                    <div class=""d-flex justify-content-between align-items-center"">
                                        <small class=""text-muted"">@item.CreatedAt.ToString(""MMM dd, yyyy"")</small>
                                        <div class=""btn-group btn-group-sm"">
                                            <button class=""btn btn-outline-primary"" @onclick=""() => ShowEditModal(item)"">
                                                <i class=""fa-solid fa-edit""></i>
                                            </button>
                                            <button class=""btn btn-outline-danger"" @onclick=""() => DeleteItem(item)"">
                                                <i class=""fa-solid fa-trash""></i>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    }}
                </div>
            }}
        }}
    </div>

    @if (_showModal) {{
        <div class=""modal fade show d-block"" style=""background-color: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header"">
                        <h5 class=""modal-title"">@(_editingItem.Id == Guid.Empty ? ""Add"" : ""Edit"") Item</h5>
                        <button type=""button"" class=""btn-close"" @onclick=""CloseModal""></button>
                    </div>
                    <div class=""modal-body"">
                        <div class=""mb-3"">
                            <label class=""form-label"">Name</label>
                            <input type=""text"" class=""form-control"" @bind=""_editingItem.Name"" />
                        </div>
                        <div class=""mb-3"">
                            <label class=""form-label"">Description</label>
                            <textarea class=""form-control"" rows=""3"" @bind=""_editingItem.Description""></textarea>
                        </div>
                        <div class=""form-check"">
                            <input type=""checkbox"" class=""form-check-input"" id=""isComplete"" @bind=""_editingItem.IsComplete"" />
                            <label class=""form-check-label"" for=""isComplete"">Mark as complete</label>
                        </div>
                    </div>
                    <div class=""modal-footer"">
                        <button class=""btn btn-secondary"" @onclick=""CloseModal"">Cancel</button>
                        <button class=""btn btn-primary"" @onclick=""SaveItem"" disabled=""@_saving"">
                            @if (_saving) {{ <i class=""fa-solid fa-spinner fa-spin me-1""></i> }}
                            Save
                        </button>
                    </div>
                </div>
            </div>
        </div>
    }}
}}

@code {{
    private bool _loading = true;
    private bool _loadedData = false;
    private bool _saving = false;
    private bool _showModal = false;
    private List<DataObjects.{name}Item> _items = new();
    private DataObjects.{name}SaveRequest _editingItem = new();

    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}

    protected override void OnInitialized() {{ Model.OnChange += StateHasChanged; }}

    protected override async Task OnAfterRenderAsync(bool firstRender) {{
        if (Model.Loaded && Model.LoggedIn && !_loadedData) {{
            _loadedData = true;
            await LoadData();
        }}
    }}

    private async Task LoadData() {{
        try {{
            _items = await Http.GetFromJsonAsync<List<DataObjects.{name}Item>>(
                DataObjects.Endpoints.{name}.GetItems) ?? new();
        }} catch {{ }}
        _loading = false;
        StateHasChanged();
    }}

    private void ShowAddModal() {{ _editingItem = new(); _showModal = true; }}
    private void ShowEditModal(DataObjects.{name}Item item) {{
        _editingItem = new() {{ Id = item.Id, Name = item.Name, Description = item.Description, IsComplete = item.IsComplete }};
        _showModal = true;
    }}
    private void CloseModal() {{ _showModal = false; _editingItem = new(); }}

    private async Task SaveItem() {{
        if (string.IsNullOrWhiteSpace(_editingItem.Name)) return;
        _saving = true;
        StateHasChanged();
        try {{
            await Helpers.GetOrPost<DataObjects.{name}Item>(DataObjects.Endpoints.{name}.SaveItem, _editingItem);
            await LoadData();
            CloseModal();
        }} catch {{ }}
        _saving = false;
        StateHasChanged();
    }}

    private async Task DeleteItem(DataObjects.{name}Item item) {{
        try {{
            await Http.DeleteAsync($""{{DataObjects.Endpoints.{name}.DeleteItem}}?itemId={{item.Id}}"");
            await LoadData();
        }} catch {{ }}
    }}
}}
";

    private static string GetStarterPage(string name) => $@"@page ""/{name}""
@page ""/{{TenantCode}}/{name}""
@inject BlazorDataModel Model
@implements IDisposable

@* ============================================================================
   {name} Page - STARTER TEMPLATE
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <{name}_App_{name} />
}}

@code {{
    [Parameter] public string? TenantCode {{ get; set; }}
    protected bool _loadedData = false;
    protected string _pageName = ""{name.ToLower()}"";

    public void Dispose() {{ Model.OnChange -= OnDataModelUpdated; }}

    protected override async Task OnAfterRenderAsync(bool firstRender) {{
        if (firstRender) {{ Model.TenantCodeFromUrl = TenantCode; }}
        if (Model.Loaded) {{
            if (Model.LoggedIn) {{
                if (!_loadedData) {{ _loadedData = true; await Helpers.ValidateUrl(TenantCode, true); }}
            }} else {{ Helpers.NavigateToLogin(); }}
        }}
    }}

    protected void OnDataModelUpdated() {{ if (Model.View == _pageName) StateHasChanged(); }}

    protected override void OnInitialized() {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}
}}
";
}

#endregion
