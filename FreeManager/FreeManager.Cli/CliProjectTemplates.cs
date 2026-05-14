namespace FreeManager.Cli;

#region CLI Project Templates
// ============================================================================
// FREEMANAGER CLI - PROJECT TEMPLATES
// Template generators for CLI project generation.
// Mirrors the templates from FreeManager.Client.ProjectTemplates.
// Also supports Application Templates (FreeBase, FreeTracker, FreeAudit).
/// ============================================================================

/// <summary>
/// Project template type for CLI (simple templates).
/// </summary>
public enum CliProjectTemplate
{
    /// <summary>No starter files - create everything from scratch.</summary>
    Empty = 0,

    /// <summary>Basic structure with placeholder comments.</summary>
    Skeleton = 1,

    /// <summary>Working example with Items list using Settings storage.</summary>
    Starter = 2,

    /// <summary>Complete CRUD with EF Entity (requires migration).</summary>
    FullCrud = 3
}

/// <summary>
/// Application template type for CLI (full applications with multiple entities).
/// </summary>
public enum CliApplicationTemplate
{
    /// <summary>Collection + Categories (foundation).</summary>
    FreeBase = 0,

    /// <summary>+ Assignment + Checkout + Status tracking.</summary>
    FreeTracker = 1,

    /// <summary>+ Logging + External API + Reports (GLBA compliance).</summary>
    FreeAudit = 2
}

/// <summary>
/// Template information for display.
/// </summary>
public class CliTemplateInfo
{
    public CliProjectTemplate Template { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public List<string> IncludedFiles { get; set; } = [];
    public bool IsRecommended { get; set; }
}

/// <summary>
/// Application template information for display.
/// </summary>
public class CliAppTemplateInfo
{
    public CliApplicationTemplate Template { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> Features { get; set; } = [];
    public List<string> UseCases { get; set; } = [];
    public int EntityCount { get; set; }
}

/// <summary>
/// Generated file information.
/// </summary>
public class GeneratedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Project templates for CLI generation.
/// </summary>
public static class CliProjectTemplates
{
    /// <summary>
    /// Gets information about all available templates.
    /// </summary>
    public static List<CliTemplateInfo> GetTemplates()
    {
        return
        [
            new CliTemplateInfo {
                Template = CliProjectTemplate.Empty,
                Name = "Empty Project",
                Description = "No starter files. You create everything from scratch.",
                FileCount = 0,
                IncludedFiles = [],
                IsRecommended = false
            },
            new CliTemplateInfo {
                Template = CliProjectTemplate.Skeleton,
                Name = "Skeleton Project",
                Description = "Basic structure with placeholder comments. Shows where to add code.",
                FileCount = 4,
                IncludedFiles = [
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs"
                ],
                IsRecommended = false
            },
            new CliTemplateInfo {
                Template = CliProjectTemplate.Starter,
                Name = "Starter Project",
                Description = "Working example with Items list. Has UI, API, and data layer. No database migration needed.",
                FileCount = 6,
                IncludedFiles = [
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs",
                    "{Name}.App.{Name}.razor",
                    "{Name}.App.{Name}Page.razor"
                ],
                IsRecommended = true
            },
            new CliTemplateInfo {
                Template = CliProjectTemplate.FullCrud,
                Name = "Full CRUD Project",
                Description = "Complete CRUD with EF Entity, edit form, validation. Requires database migration after export.",
                FileCount = 8,
                IncludedFiles = [
                    "DataObjects.App.{Name}.cs",
                    "DataAccess.App.{Name}.cs",
                    "DataController.App.{Name}.cs",
                    "GlobalSettings.App.{Name}.cs",
                    "{Name}.App.{Name}.razor",
                    "{Name}.App.{Name}Page.razor",
                    "{Name}Item.cs",
                    "EFDataModel.App.{Name}.cs"
                ],
                IsRecommended = false
            }
        ];
    }

    /// <summary>
    /// Gets information about all available application templates.
    /// </summary>
    public static List<CliAppTemplateInfo> GetApplicationTemplates()
    {
        return
        [
            new CliAppTemplateInfo {
                Template = CliApplicationTemplate.FreeBase,
                Id = "freebase",
                Name = "FreeBase",
                Description = "Foundation template with Items and Categories. Great starting point for any collection-based app.",
                Difficulty = "Beginner",
                Features = ["Item management", "Category organization", "Basic CRUD"],
                UseCases = ["Inventory basics", "Simple collections", "Categorized lists"],
                EntityCount = 2
            },
            new CliAppTemplateInfo {
                Template = CliApplicationTemplate.FreeTracker,
                Id = "freetracker",
                Name = "FreeTracker",
                Description = "Asset tracking with assignments, checkouts, and status tracking. Built for tracking who has what.",
                Difficulty = "Intermediate",
                Features = ["Item tracking", "Assignment management", "Checkout records", "Status workflow"],
                UseCases = ["Equipment checkout", "Asset assignment", "Resource tracking"],
                EntityCount = 5
            },
            new CliAppTemplateInfo {
                Template = CliApplicationTemplate.FreeAudit,
                Id = "freeaudit",
                Name = "FreeAudit",
                Description = "Full GLBA compliance tracking with external API, event logging, and reports. Built for audit trails and regulatory compliance.",
                Difficulty = "Advanced",
                Features = ["External API endpoint", "API key authentication", "Access event logging", "Compliance dashboard", "Report generation"],
                UseCases = ["GLBA compliance", "Audit trails", "Access logging", "Regulatory reporting"],
                EntityCount = 4
            }
        ];
    }

    /// <summary>
    /// Gets the files to create for a given template.
    /// </summary>
    public static List<GeneratedFile> GetTemplateFiles(CliProjectTemplate template, string projectName)
    {
        List<GeneratedFile> files = [];

        switch (template)
        {
            case CliProjectTemplate.Empty:
                // No files
                break;

            case CliProjectTemplate.Skeleton:
                files.Add(new GeneratedFile { FileName = $"DataObjects.App.{projectName}.cs", FileType = "DataObjects", Content = GetSkeletonDataObjects(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataAccess.App.{projectName}.cs", FileType = "DataAccess", Content = GetSkeletonDataAccess(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataController.App.{projectName}.cs", FileType = "Controller", Content = GetSkeletonController(projectName) });
                files.Add(new GeneratedFile { FileName = $"GlobalSettings.App.{projectName}.cs", FileType = "GlobalSettings", Content = GetSkeletonGlobalSettings(projectName) });
                break;

            case CliProjectTemplate.Starter:
                files.Add(new GeneratedFile { FileName = $"DataObjects.App.{projectName}.cs", FileType = "DataObjects", Content = GetStarterDataObjects(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataAccess.App.{projectName}.cs", FileType = "DataAccess", Content = GetStarterDataAccess(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataController.App.{projectName}.cs", FileType = "Controller", Content = GetStarterController(projectName) });
                files.Add(new GeneratedFile { FileName = $"GlobalSettings.App.{projectName}.cs", FileType = "GlobalSettings", Content = GetStarterGlobalSettings(projectName) });
                files.Add(new GeneratedFile { FileName = $"{projectName}.App.{projectName}.razor", FileType = "RazorComponent", Content = GetStarterComponent(projectName) });
                files.Add(new GeneratedFile { FileName = $"{projectName}.App.{projectName}Page.razor", FileType = "RazorPage", Content = GetStarterPage(projectName) });
                break;

            case CliProjectTemplate.FullCrud:
                files.Add(new GeneratedFile { FileName = $"DataObjects.App.{projectName}.cs", FileType = "DataObjects", Content = GetFullCrudDataObjects(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataAccess.App.{projectName}.cs", FileType = "DataAccess", Content = GetFullCrudDataAccess(projectName) });
                files.Add(new GeneratedFile { FileName = $"DataController.App.{projectName}.cs", FileType = "Controller", Content = GetFullCrudController(projectName) });
                files.Add(new GeneratedFile { FileName = $"GlobalSettings.App.{projectName}.cs", FileType = "GlobalSettings", Content = GetStarterGlobalSettings(projectName) });
                files.Add(new GeneratedFile { FileName = $"{projectName}.App.{projectName}.razor", FileType = "RazorComponent", Content = GetStarterComponent(projectName) });
                files.Add(new GeneratedFile { FileName = $"{projectName}.App.{projectName}Page.razor", FileType = "RazorPage", Content = GetStarterPage(projectName) });
                files.Add(new GeneratedFile { FileName = $"{projectName}Item.cs", FileType = "EFModel", Content = GetFullCrudEntity(projectName) });
                files.Add(new GeneratedFile { FileName = $"EFDataModel.App.{projectName}.cs", FileType = "EFDataModel", Content = GetFullCrudDbContext(projectName) });
                break;
        }

        return files;
    }

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

    // ============================================================
    // STARTER TEMPLATES
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

    // ============================================================
    // FULL CRUD TEMPLATES
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
}

#endregion
