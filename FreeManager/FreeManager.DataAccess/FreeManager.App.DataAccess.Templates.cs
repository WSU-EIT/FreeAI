using System.Text.Json.Serialization;

namespace FreeManager;

#region FreeManager Platform - Project Template Generators
// ============================================================================
// FREEMANAGER PROJECT TEMPLATES
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
// Coordinator: DataAccess.App.FreeManager.cs
//
// Code generators that produce .App. files for new FreeManager projects.
// Called by FM_CreateProject when a template is selected.
//
// TEMPLATE TYPES:
// ┌────────────────────────────────────────────────────────────────────────┐
// │                                                                        │
// │  EMPTY        → No files generated                                     │
// │                                                                        │
// │  SKELETON     → Minimal scaffolds with placeholder comments            │
// │                 ├── DataObjects.App.{Name}.cs                          │
// │                 ├── DataAccess.App.{Name}.cs                           │
// │                 ├── DataController.App.{Name}.cs                       │
// │                 └── GlobalSettings.App.{Name}.cs                       │
// │                                                                        │
// │  STARTER      → Working example using Settings table (no migrations)   │
// │                 ├── All Skeleton files (with implementations)          │
// │                 ├── {Name}.App.razor          (card-based list view)   │
// │                 └── {Name}Page.App.razor      (routed page)            │
// │                                                                        │
// │  FULLCRUD     → Database-backed entity with EF Core                    │
// │                 ├── All Starter files                                  │
// │                 ├── {Name}Item.cs             (EF entity model)        │
// │                 └── EFDataModel.App.{Name}.cs (DbSet registration)     │
// │                                                                        │
// └────────────────────────────────────────────────────────────────────────┘
//
// GENERATED CODE FEATURES:
//   • Tenant isolation (TenantId filtering)
//   • Soft delete (Deleted, DeletedAt fields)
//   • Audit fields (CreatedAt, UpdatedAt, CreatedBy)
//   • REST API endpoints with [Authorize]
//   • Blazor UI with modal add/edit
//
// NOTE: This file is 900+ lines. Consider splitting into:
//   • Templates.Skeleton.cs
//   • Templates.Starter.cs
//   • Templates.FullCrud.cs
// ============================================================================

public partial class DataAccess
{
    // ============================================================
    // PROJECT TEMPLATE ROUTING
    // ============================================================

    /// <summary>
    /// Gets template files based on project template type.
    /// </summary>
    private static List<(string FileName, string FileType, string Content)> FM_GetProjectTemplateFiles(
        DataObjects.FMProjectTemplate template,
        string projectName)
    {
        List<(string, string, string)> files = new();

        switch (template)
        {
            case DataObjects.FMProjectTemplate.Empty:
                break;

            case DataObjects.FMProjectTemplate.Skeleton:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetSkeletonDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetSkeletonDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetSkeletonController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetSkeletonGlobalSettings(projectName)));
                break;

            case DataObjects.FMProjectTemplate.Starter:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetStarterDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetStarterDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetStarterController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.razor", DataObjects.FMFileTypes.RazorComponent, FM_GetStarterComponent(projectName)));
                files.Add(($"{projectName}Page.App.razor", DataObjects.FMFileTypes.RazorPage, FM_GetStarterPage(projectName)));
                break;

            case DataObjects.FMProjectTemplate.FullCrud:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetFullCrudDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetFullCrudDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetFullCrudController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.razor", DataObjects.FMFileTypes.RazorComponent, FM_GetStarterComponent(projectName)));
                files.Add(($"{projectName}Page.App.razor", DataObjects.FMFileTypes.RazorPage, FM_GetStarterPage(projectName)));
                files.Add(($"{projectName}Item.cs", DataObjects.FMFileTypes.EFModel, FM_GetFullCrudEntity(projectName)));
                files.Add(($"EFDataModel.App.{projectName}.cs", DataObjects.FMFileTypes.EFDataModel, FM_GetFullCrudDbContext(projectName)));
                break;
        }

        return files;
    }

    // ============================================================
    // SKELETON TEMPLATES
    // ============================================================

    private static string FM_GetSkeletonDataObjects(string name) => $@"namespace FreeManager;

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

    private static string FM_GetSkeletonDataAccess(string name) => $@"namespace FreeManager;

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

    private static string FM_GetSkeletonController(string name) => $@"using Microsoft.AspNetCore.Authorization;
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

    private static string FM_GetSkeletonGlobalSettings(string name) => $@"namespace FreeManager;

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

    // ============================================================
    // STARTER TEMPLATES (Working example with Settings storage)
    // ============================================================

    private static string FM_GetStarterDataObjects(string name) => $@"using System.Text.Json.Serialization;

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

    private static string FM_GetStarterDataAccess(string name) => $@"using System.Text.Json;

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
            // Update existing
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
            // Create new
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

    private static string FM_GetStarterController(string name) => $@"using Microsoft.AspNetCore.Authorization;
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

    private static string FM_GetStarterGlobalSettings(string name) => $@"namespace FreeManager;

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

    private static string FM_GetStarterComponent(string name) => $@"@implements IDisposable
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

    @* Add/Edit Modal *@
    @if (_showModal) {{
        <div class=""modal fade show d-block"" style=""background-color: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header"">
                        <h5 class=""modal-title"">@(_editingItem.Id == null ? ""Add"" : ""Edit"") Item</h5>
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
                            @if (_saving) {{
                                <i class=""fa-solid fa-spinner fa-spin me-1""></i>
                            }}
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
            _items = await Http.GetFromJsonAsync<List<DataObjects.{name}Item>>(
                DataObjects.Endpoints.{name}.GetItems) ?? new();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error loading items: {{ex.Message}}"");
        }}
        _loading = false;
        StateHasChanged();
    }}

    private void ShowAddModal()
    {{
        _editingItem = new DataObjects.{name}SaveRequest();
        _showModal = true;
    }}

    private void ShowEditModal(DataObjects.{name}Item item)
    {{
        _editingItem = new DataObjects.{name}SaveRequest {{
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsComplete = item.IsComplete
        }};
        _showModal = true;
    }}

    private void CloseModal()
    {{
        _showModal = false;
        _editingItem = new();
    }}

    private async Task SaveItem()
    {{
        if (string.IsNullOrWhiteSpace(_editingItem.Name)) return;

        _saving = true;
        StateHasChanged();

        try {{
            await Helpers.GetOrPost<DataObjects.{name}Item>(
                DataObjects.Endpoints.{name}.SaveItem, _editingItem);
            await LoadData();
            CloseModal();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error saving item: {{ex.Message}}"");
        }}

        _saving = false;
        StateHasChanged();
    }}

    private async Task DeleteItem(DataObjects.{name}Item item)
    {{
        try {{
            await Http.DeleteAsync($""{{DataObjects.Endpoints.{name}.DeleteItem}}?itemId={{item.Id}}"");
            await LoadData();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error deleting item: {{ex.Message}}"");
        }}
    }}
}}
";

    private static string FM_GetStarterPage(string name) => $@"@page ""/{name}""
@page ""/{{TenantCode}}/{name}""
@inject BlazorDataModel Model
@implements IDisposable

@* ============================================================================
   {name} Page - STARTER TEMPLATE
   Routed page that hosts the {name} component.
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <{name}_App />
}}

@code {{
    [Parameter] public string? TenantCode {{ get; set; }}

    protected bool _loadedData = false;
    protected string _pageName = ""{name.ToLower()}"";

    public void Dispose()
    {{
        Model.OnChange -= OnDataModelUpdated;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) {{
            Model.TenantCodeFromUrl = TenantCode;
        }}

        if (Model.Loaded) {{
            if (Model.LoggedIn) {{
                if (!_loadedData) {{
                    _loadedData = true;
                    await Helpers.ValidateUrl(TenantCode, true);
                }}
            }} else {{
                Helpers.NavigateToLogin();
            }}
        }}
    }}

    protected void OnDataModelUpdated()
    {{
        if (Model.View == _pageName) {{
            StateHasChanged();
        }}
    }}

    protected override void OnInitialized()
    {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}
}}
";

    // ============================================================
    // FULL CRUD TEMPLATES (EF Entity based)
    // ============================================================

    private static string FM_GetFullCrudDataObjects(string name) => $@"namespace FreeManager;

#region {name} DataObjects
// ===========================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// DTOs for database-backed CRUD operations.
// ===========================================================================

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
    public class {name}Item
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

    private static string FM_GetFullCrudDataAccess(string name) => $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// Business logic with EF Core database operations.
// ============================================================================

public partial interface IDataAccess
{{
    Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    public async Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        List<DataObjects.{name}Item> output = await data.{name}Items
            .Where(x => x.TenantId == tenantId && !x.Deleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DataObjects.{name}Item {{
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsComplete = x.IsComplete,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CompletedAt = x.CompletedAt
            }})
            .ToListAsync();

        return output;
    }}

    public async Task<DataObjects.{name}Item?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        EFModels.EFModels.{name}Item? entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity == null) return null;

        return new DataObjects.{name}Item {{
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        }};
    }}

    public async Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;
        EFModels.EFModels.{name}Item entity;

        if (request.Id.HasValue && request.Id != Guid.Empty) {{
            // Update existing
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
            // Create new
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

        return new DataObjects.{name}Item {{
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
        var output = new DataObjects.BooleanResponse();
        var tenantId = CurrentUser.TenantId;

        var entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity == null) {{
            output.Messages.Add(""Item not found"");
            return output;
        }}

        entity.Deleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await data.SaveChangesAsync();

        output.Result = true;
        return output;
    }}
}}

#endregion
";

    private static string FM_GetFullCrudController(string name) => $@"using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems()
    {{
        return await da.{name}_GetItems(CurrentUser);
    }}

    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_GetItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_GetItem(itemId, CurrentUser);
    }}

    [HttpPost]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.SaveItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_SaveItem([FromBody] DataObjects.{name}Item item)
    {{
        return await da.{name}_SaveItem(item, CurrentUser);
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

    private static string FM_GetFullCrudEntity(string name) => $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

#region {name} Entity
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// Entity Framework model with tenant isolation and soft delete.
// ============================================================================

[Table(""{name}Items"")]
public class {name}Item
{{
    [Key]
    public Guid Id {{ get; set; }} = Guid.NewGuid();

    [Required]
    public Guid TenantId {{ get; set; }}

    [MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;

    public string Description {{ get; set; }} = string.Empty;

    public bool IsComplete {{ get; set; }} = false;

    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime UpdatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? CompletedAt {{ get; set; }}

    public Guid? CreatedBy {{ get; set; }}

    public bool Deleted {{ get; set; }} = false;
    public DateTime? DeletedAt {{ get; set; }}

    // Navigation
    public virtual Tenant? Tenant {{ get; set; }}
}}

#endregion
";

    private static string FM_GetFullCrudDbContext(string name) => $@"using Microsoft.EntityFrameworkCore;

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
}

#endregion
