namespace FreeManager.Client;

#region ProjectTemplates - Skeleton Templates
// ============================================================================
// PROJECT TEMPLATES - SKELETON
// Basic structure with placeholder comments.
// Part of: ProjectTemplates.App (partial)
// ============================================================================

public static partial class ProjectTemplates
{
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
}

#endregion
