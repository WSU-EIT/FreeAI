namespace FreeManager.Client;

// ---- ProjectTemplates ----

#region ProjectTemplates - Project Template Coordinator
// ============================================================================
// FREEMANAGER PROJECT TEMPLATES
// This is the coordinator file for project template generation.
// Template generators are split into partial files:
//
// - Skeleton section               → Basic structure with placeholders
// - Starter section                 → Working example using Settings storage
// - FullCrud section                → Full CRUD with EF entities
//
// This file contains the template router and metadata.
// ============================================================================

/// <summary>
/// Project templates for FreeManager.
/// Generates starter files based on selected template type.
/// </summary>
public static partial class ProjectTemplates
{
    /// <summary>
    /// Gets the files to create for a given template.
    /// Routes to appropriate template generator in partial files.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetTemplateFiles(
        DataObjects.FMProjectTemplate template,
        string projectName){
        List<(string, string, string)> files = new();

        switch (template)
        {
            case DataObjects.FMProjectTemplate.Empty:
                // No files
                break;

            case DataObjects.FMProjectTemplate.Skeleton:
                files.Add(($"DataObjects.App.{projectName}.cs", "DataObjects", GetSkeletonDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", "DataAccess", GetSkeletonDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", "Controller", GetSkeletonController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", "GlobalSettings", GetSkeletonGlobalSettings(projectName)));
                break;

            case DataObjects.FMProjectTemplate.Starter:
                files.Add(($"DataObjects.App.{projectName}.cs", "DataObjects", GetStarterDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", "DataAccess", GetStarterDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", "Controller", GetStarterController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", "GlobalSettings", GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.{projectName}.razor", "RazorComponent", GetStarterComponent(projectName)));
                files.Add(($"{projectName}.App.{projectName}Page.razor", "RazorPage", GetStarterPage(projectName)));
                break;

            case DataObjects.FMProjectTemplate.FullCrud:
                files.Add(($"DataObjects.App.{projectName}.cs", "DataObjects", GetFullCrudDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", "DataAccess", GetFullCrudDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", "Controller", GetFullCrudController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", "GlobalSettings", GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.{projectName}.razor", "RazorComponent", GetStarterComponent(projectName)));
                files.Add(($"{projectName}.App.{projectName}Page.razor", "RazorPage", GetStarterPage(projectName)));
                files.Add(($"{projectName}Item.cs", "EFModel", GetFullCrudEntity(projectName)));
                files.Add(($"EFDataModel.App.{projectName}.cs", "EFDataModel", GetFullCrudDbContext(projectName)));
                break;
        }

        return files;
    }
    /// <summary>
    /// Gets information about all available templates.
    /// </summary>
    public static List<DataObjects.FMProjectTemplateInfo> GetTemplates()
    {
        return new List<DataObjects.FMProjectTemplateInfo>
        {
            new DataObjects.FMProjectTemplateInfo {
                Template = DataObjects.FMProjectTemplate.Empty,
                Name = "Empty Project",
                Description = "No starter files. You create everything from scratch.",
                Icon = "fa-solid fa-file",
                FileCount = 0,
                IncludedFiles = new List<string>(),
                IsRecommended = false
            },
            new DataObjects.FMProjectTemplateInfo {
                Template = DataObjects.FMProjectTemplate.Skeleton,
                Name = "Skeleton Project",
                Description = "Basic structure with placeholder comments. Shows where to add code.",
                Icon = "fa-solid fa-bone",
                FileCount = 4,
                IncludedFiles = new List<string> {
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs"
                },
                IsRecommended = false
            },
            new DataObjects.FMProjectTemplateInfo {
                Template = DataObjects.FMProjectTemplate.Starter,
                Name = "Starter Project",
                Description = "Working example with Items list. Has UI, API, and data layer. No database migration needed.",
                Icon = "fa-solid fa-star",
                FileCount = 6,
                IncludedFiles = new List<string> {
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs",
                    "{Name}.App.{Name}.razor",
                    "{Name}.App.{Name}Page.razor"
                },
                IsRecommended = true
            },
            new DataObjects.FMProjectTemplateInfo {
                Template = DataObjects.FMProjectTemplate.FullCrud,
                Name = "Full CRUD Project",
                Description = "Complete CRUD with EF Entity, edit form, validation. Requires database migration after export.",
                Icon = "fa-solid fa-database",
                FileCount = 8,
                IncludedFiles = new List<string> {
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs",
                    "{Name}.App.{Name}.razor",
                    "{Name}.App.{Name}Page.razor",
                    "{Name}Item.cs",
                    "EFDataModel.App.{Name}.cs"
                },
                IsRecommended = false
            }
        };
    }
}

#endregion

// ---- ProjectTemplates.Skeleton ----

#region ProjectTemplates - Skeleton Templates
// ============================================================================
// PROJECT TEMPLATES - SKELETON
// Basic structure with placeholder comments.
// Part of: ProjectTemplates.App (partial)
// ============================================================================

public static partial class ProjectTemplates
{
    private static string GetSkeletonController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your API endpoints here.
// ============================================================================

public partial class DataController
{{
    // Add your endpoints here
    // [HttpGet]
    // [Authorize]
    // [Route(""~/api/Data/{name}_GetItems"")]
    // public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems() {{ }}
}}

#endregion
";

    private static string GetSkeletonDataAccess(string name) => $@"namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your business logic methods here.
// ============================================================================

public partial interface IDataAccess
{{
    // Define your method signatures here
    // Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    // Implement your methods here
}}

#endregion
";
    // ============================================================
    // SKELETON TEMPLATES
    // ============================================================

    private static string GetSkeletonDataObjects(string name) => $@"namespace FreeManager;

#region {name} DataObjects
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your DTOs and models here.
// ============================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            // Define your API endpoints here
            // public const string GetItems = ""api/Data/{name}_GetItems"";
        }}
    }}

    // Add your DTOs here
    // public class {name}Item {{ }}
}}

#endregion
";

    private static string GetSkeletonGlobalSettings(string name) => $@"namespace FreeManager;

#region {name} Settings
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your app configuration here.
// ============================================================================

public static partial class GlobalSettings
{{
    public static class {name}
    {{
        public static string AppName {{ get; set; }} = ""{name}"";
        public static string Version {{ get; set; }} = ""1.0.0"";
    }}
}}

#endregion
";
}

#endregion

// ---- ProjectTemplates.Starter ----

#region ProjectTemplates - Starter Templates
// ============================================================================
// PROJECT TEMPLATES - STARTER
// Working example with Items list using Settings table storage.
// Part of: ProjectTemplates.App (partial)
// ============================================================================

public static partial class ProjectTemplates
{
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

// ---- ProjectTemplates.FullCrud ----

#region ProjectTemplates - Full CRUD Templates
// ============================================================================
// PROJECT TEMPLATES - FULL CRUD
// Complete CRUD with EF Entity requiring database migration.
// Part of: ProjectTemplates.App (partial)
// ============================================================================

public static partial class ProjectTemplates
{
    private static string GetFullCrudController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// REST API endpoints with full CRUD operations.
// ============================================================================

public partial class DataController
{{
    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItems}}"")]
    public async Task<ActionResult<List<DataObjects.{name}ItemInfo>>> {name}_GetItems()
    {{
        return await da.{name}_GetItems(CurrentUser);
    }}

    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItem}}"")]
    public async Task<ActionResult<DataObjects.{name}ItemInfo?>> {name}_GetItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_GetItem(itemId, CurrentUser);
    }}

    [HttpPost]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.SaveItem}}"")]
    public async Task<ActionResult<DataObjects.{name}ItemInfo?>> {name}_SaveItem([FromBody] DataObjects.{name}SaveRequest request)
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

    private static string GetFullCrudDataAccess(string name) => $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// Business logic with EF Core database operations.
// ============================================================================

public partial interface IDataAccess
{{
    Task<List<DataObjects.{name}ItemInfo>> {name}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{name}ItemInfo?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser);
    Task<DataObjects.{name}ItemInfo?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    public async Task<List<DataObjects.{name}ItemInfo>> {name}_GetItems(DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        return await data.{name}Items
            .Where(x => x.TenantId == tenantId && !x.Deleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DataObjects.{name}ItemInfo {{
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsComplete = x.IsComplete,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CompletedAt = x.CompletedAt
            }})
            .ToListAsync();
    }}

    public async Task<DataObjects.{name}ItemInfo?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        EFModels.EFModels.{name}Item? entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity == null) return null;

        return new DataObjects.{name}ItemInfo {{
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        }};
    }}

    public async Task<DataObjects.{name}ItemInfo?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;
        EFModels.EFModels.{name}Item entity;

        if (request.Id.HasValue && request.Id != Guid.Empty) {{
            entity = await data.{name}Items
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == tenantId && !x.Deleted)
                ?? new EFModels.EFModels.{name}Item {{ TenantId = tenantId }};

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            if (request.IsComplete && !entity.IsComplete) {{
                entity.CompletedAt = DateTime.UtcNow;
            }} else if (!request.IsComplete) {{
                entity.CompletedAt = null;
            }}
            entity.IsComplete = request.IsComplete;

            if (entity.Id == Guid.Empty) {{
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = CurrentUser.UserId;
                data.{name}Items.Add(entity);
            }}
        }} else {{
            entity = new EFModels.EFModels.{name}Item {{
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                IsComplete = request.IsComplete,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUser.UserId
            }};
            data.{name}Items.Add(entity);
        }}

        await data.SaveChangesAsync();

        return new DataObjects.{name}ItemInfo {{
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        }};
    }}

    public async Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        DataObjects.BooleanResponse output = new();
        Guid tenantId = CurrentUser.TenantId;

        EFModels.EFModels.{name}Item? entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity != null) {{
            entity.Deleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await data.SaveChangesAsync();
            output.Result = true;
        }} else {{
            output.Messages.Add(""Item not found"");
        }}

        return output;
    }}
}}

#endregion
";
    // ============================================================
    // FULL CRUD TEMPLATES (EF Entity based)
    // ============================================================

    private static string GetFullCrudDataObjects(string name) => $@"namespace FreeManager;

#region {name} DataObjects
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// DTOs for database-backed CRUD operations.
// ============================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            public const string GetItems = ""api/Data/{name}_GetItems"";
            public const string GetItem = ""api/Data/{name}_GetItem"";
            public const string SaveItem = ""api/Data/{name}_SaveItem"";
            public const string DeleteItem = ""api/Data/{name}_DeleteItem"";
        }}
    }}

    /// <summary>
    /// {name} item DTO for API responses.
    /// </summary>
    public class {name}ItemInfo
    {{
        public Guid Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public DateTime UpdatedAt {{ get; set; }}
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
        public bool IsComplete {{ get; set; }}
    }}
}}

#endregion
";

    private static string GetFullCrudDbContext(string name) => $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager.EFModels.EFModels;

#region {name} DbContext Extension
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// DbSet registration for EF Core.
// ============================================================================

public partial class EFDataModel
{{
    public virtual DbSet<{name}Item> {name}Items {{ get; set; }} = null!;
}}

#endregion
";

    private static string GetFullCrudEntity(string name) => $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

#region {name} Entity
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// EF Core entity for database storage.
//
// AFTER EXPORT, run these commands:
// 1. dotnet ef migrations add {name}_Initial --startup-project ../FreeManager
// 2. dotnet ef database update --startup-project ../FreeManager
// ============================================================================

[Table(""{name}Items"")]
public class {name}Item
{{
    [Key]
    public Guid Id {{ get; set; }} = Guid.NewGuid();

    public Guid TenantId {{ get; set; }}

    [Required]
    [MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;

    [MaxLength(1000)]
    public string Description {{ get; set; }} = string.Empty;

    public bool IsComplete {{ get; set; }} = false;

    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime UpdatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? CompletedAt {{ get; set; }}
    public Guid? CreatedBy {{ get; set; }}

    public bool Deleted {{ get; set; }} = false;
    public DateTime? DeletedAt {{ get; set; }}

    public virtual Tenant? Tenant {{ get; set; }}
}}

#endregion
";
}

#endregion

// ---- FileTemplates ----

/// <summary>
/// Default file templates for FreeManager projects.
/// Provides comprehensive starter code for each file type.
/// </summary>
public static class FMFileTemplates
{
    private static string GetControllerTemplate(string projectName, string projectNameUpper)
    {
        return $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {projectName} API Endpoints
// ============================================================================
// {projectNameUpper} PROJECT EXTENSION
// Add your API endpoints here. All endpoints should use [Authorize].
// ============================================================================

public partial class DataController
{{
    /// <summary>
    /// Get all items for the current tenant.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route(""~/api/Data/{projectName}_GetItems"")]
    public async Task<ActionResult<List<DataObjects.{projectName}Item>>> {projectName}_GetItems()
    {{
        return await da.{projectName}_GetItems(CurrentUser);
    }}

    /// <summary>
    /// Create a new item.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route(""~/api/Data/{projectName}_CreateItem"")]
    public async Task<ActionResult<DataObjects.{projectName}ItemResponse>> {projectName}_CreateItem(
        [FromBody] DataObjects.{projectName}CreateRequest request)
    {{
        return await da.{projectName}_CreateItem(request, CurrentUser);
    }}

    // TIP: HTTP Methods
    // [HttpGet]    - Read operations
    // [HttpPost]   - Create operations
    // [HttpPut]    - Update operations
    // [HttpDelete] - Delete operations
}}

#endregion
";
    }

    private static string GetDataAccessTemplate(string projectName, string projectNameUpper)
    {
        return $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager;

#region {projectName} DataAccess Methods
// ============================================================================
// {projectNameUpper} PROJECT EXTENSION
// Add your business logic methods here.
// All methods should accept CurrentUser for tenant isolation.
// ============================================================================

/// <summary>
/// Interface extension - define your method signatures here.
/// </summary>
public partial interface IDataAccess
{{
    Task<List<DataObjects.{projectName}Item>> {projectName}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{projectName}ItemResponse> {projectName}_CreateItem(
        DataObjects.{projectName}CreateRequest request,
        DataObjects.User CurrentUser);
}}

/// <summary>
/// Implementation - add your business logic here.
/// </summary>
public partial class DataAccess
{{
    public async Task<List<DataObjects.{projectName}Item>> {projectName}_GetItems(DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        // TODO: Replace with your actual entity query
        // var items = await data.YourEntitySet
        //     .Where(x => x.TenantId == tenantId && !x.Deleted)
        //     .OrderByDescending(x => x.CreatedAt)
        //     .ToListAsync();

        List<DataObjects.{projectName}Item> output = new();
        await Task.CompletedTask; // Placeholder for async
        return output;
    }}

    public async Task<DataObjects.{projectName}ItemResponse> {projectName}_CreateItem(
        DataObjects.{projectName}CreateRequest request,
        DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;
        Guid userId = CurrentUser.UserId;

        // TODO: Add validation
        // TODO: Create entity and save to database

        DataObjects.{projectName}ItemResponse output = new() {{
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        }};

        await Task.CompletedTask; // Placeholder for async
        return output;
    }}
}}

#endregion
";
    }

    private static string GetDataObjectsTemplate(string projectName, string projectNameUpper)
    {
        return $@"namespace FreeManager;

#region {projectName} Custom DataObjects
// ============================================================================
// {projectNameUpper} PROJECT EXTENSION
// Add your custom DTOs here. These extend the base FreeCRM DataObjects.
// ============================================================================

public partial class DataObjects
{{
    // --------------------------------------------------------
    // EXAMPLE: Create a new DTO
    // --------------------------------------------------------

    /// <summary>
    /// Example entity for your custom data.
    /// </summary>
    public class {projectName}Item
    {{
        public Guid Id {{ get; set; }} = Guid.NewGuid();
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        public bool IsActive {{ get; set; }} = true;
    }}

    /// <summary>
    /// Request to create a new item.
    /// </summary>
    public class {projectName}CreateRequest
    {{
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
    }}

    /// <summary>
    /// Response with item details.
    /// </summary>
    public class {projectName}ItemResponse
    {{
        public Guid Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public DateTime CreatedAt {{ get; set; }}
    }}

    // --------------------------------------------------------
    // TIP: Extend existing FreeCRM types like this:
    // --------------------------------------------------------

    // public partial class User
    // {{
    //     public string? CustomField {{ get; set; }}
    // }}
}}

#endregion
";
    }
    public static string GetDefaultContent(string fileType, string fileName, string projectName)
    {
        string projectNameUpper = projectName.ToUpper();

        return fileType switch {
            "DataObjects" => GetDataObjectsTemplate(projectName, projectNameUpper),
            "DataAccess" => GetDataAccessTemplate(projectName, projectNameUpper),
            "Controller" => GetControllerTemplate(projectName, projectNameUpper),
            "RazorComponent" => GetRazorComponentTemplate(projectName, fileName),
            "EFModel" => GetEFModelTemplate(projectName, projectNameUpper),
            "GlobalSettings" => GetGlobalSettingsTemplate(projectName, projectNameUpper),
            "Stylesheet" => $"/* {fileName} - {projectName} Styles */\n\n/* Add your custom styles here */\n",
            _ => $"// {fileName}\n// {projectName} Project\n\n"
        };
    }

    private static string GetEFModelTemplate(string projectName, string projectNameUpper)
    {
        return $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

#region {projectName} Entity
// ============================================================================
// {projectNameUpper} PROJECT EXTENSION
// Add your EF Core entity here. Remember to:
// 1. Add DbSet in EFDataModel.App.{projectName}.cs
// 2. Run: dotnet ef migrations add {projectName}_AddEntity
// 3. Run: dotnet ef database update
// ============================================================================

/// <summary>
/// {projectName} entity - stores your custom data.
/// </summary>
[Table(""{projectName}Items"")]
public class {projectName}Item
{{
    [Key]
    public Guid Id {{ get; set; }} = Guid.NewGuid();

    /// <summary>
    /// Tenant isolation - always filter by this in queries.
    /// </summary>
    public Guid TenantId {{ get; set; }}

    [Required]
    [MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;

    [MaxLength(1000)]
    public string Description {{ get; set; }} = string.Empty;

    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime UpdatedAt {{ get; set; }} = DateTime.UtcNow;
    public Guid? CreatedBy {{ get; set; }}

    /// <summary>
    /// Soft delete support - never hard delete.
    /// </summary>
    public bool Deleted {{ get; set; }} = false;
    public DateTime? DeletedAt {{ get; set; }}

    // Navigation properties
    public virtual Tenant? Tenant {{ get; set; }}
}}

#endregion

// ============================================================================
// NEXT STEP: Create EFDataModel.App.{projectName}.cs with this content:
// ============================================================================
//
// using Microsoft.EntityFrameworkCore;
//
// namespace FreeManager.EFModels.EFModels;
//
// public partial class EFDataModel
// {{
//     public virtual DbSet<{projectName}Item> {projectName}Items {{ get; set; }} = null!;
// }}
";
    }

    private static string GetGlobalSettingsTemplate(string projectName, string projectNameUpper)
    {
        return $@"namespace FreeManager;

#region {projectName} Global Settings
// ============================================================================
// {projectNameUpper} PROJECT EXTENSION
// Add your app-wide settings and constants here.
// ============================================================================

public static partial class GlobalSettings
{{
    /// <summary>
    /// {projectName} application settings.
    /// </summary>
    public static class {projectName}
    {{
        /// <summary>
        /// Application display name.
        /// </summary>
        public static string AppName {{ get; set; }} = ""{projectName}"";

        /// <summary>
        /// Current version.
        /// </summary>
        public static string Version {{ get; set; }} = ""1.0.0"";

        /// <summary>
        /// Feature flags.
        /// </summary>
        public static bool EnableFeatureX {{ get; set; }} = true;

        // Add more settings as needed
    }}
}}

#endregion
";
    }

    private static string GetRazorComponentTemplate(string projectName, string fileName)
    {
        return $@"@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@* ============================================================================
   {fileName}
   {projectName} Custom Component
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <h1 class=""page-title"">
            <i class=""fa-solid fa-cube me-2""></i>
            {projectName} Component
        </h1>

        @if (_loading) {{
            <LoadingMessage />
        }} else {{
            <div class=""card"">
                <div class=""card-body"">
                    <p>Items loaded: @_items.Count</p>

                    @foreach (var item in _items) {{
                        <div class=""mb-2"">
                            <strong>@item.Name</strong>
                        </div>
                    }}

                    @if (_items.Count == 0) {{
                        <p class=""text-muted"">No items yet.</p>
                    }}
                </div>
            </div>
        }}
    </div>
}}

@code {{
    private bool _loading = true;
    private bool _loadedData = false;
    private List<DataObjects.{projectName}Item> _items = new();

    public void Dispose()
    {{
        Model.OnChange -= StateHasChanged;
    }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (Model.Loaded && Model.LoggedIn && !_loadedData) {{
            _loadedData = true;
            await LoadData();
        }}
    }}

    private async Task LoadData()
    {{
        try {{
            // TODO: Call your API endpoint
            // _items = await Http.GetFromJsonAsync<List<DataObjects.{projectName}Item>>(
            //     ""api/Data/{projectName}_GetItems"") ?? new();

            _items = new(); // Placeholder
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error: {{ex.Message}}"");
        }}

        _loading = false;
        StateHasChanged();
    }}
}}
";
    }
}
