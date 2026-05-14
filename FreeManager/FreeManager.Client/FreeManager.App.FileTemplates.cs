namespace FreeManager.Client;

/// <summary>
/// Default file templates for FreeManager projects.
/// Provides comprehensive starter code for each file type.
/// </summary>
public static class FMFileTemplates
{
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
}
