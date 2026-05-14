namespace FreeManager;

#region FreeManager Platform - Project, File & Build DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │          DataObjects.App.FreeManager.Projects.cs                │
// │                 Project, File & Build DTOs                       │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROJECT DTOs                                                    │
// │   FMProjectInfo         → Project metadata & stats              │
// │   FMCreateProjectRequest → Create project with template         │
// │   FMUpdateProjectRequest → Update project name/desc             │
// ├─────────────────────────────────────────────────────────────────┤
// │ FILE DTOs                                                       │
// │   FMAppFileInfo         → File metadata in project              │
// │   FMAppFileContent      → Full file with content                │
// │   FMCreateFileRequest   → Create new file                       │
// │   FMSaveFileRequest     → Save file (new version)               │
// │   FMSaveFileResponse    → Response with concurrency token       │
// │   FMFileVersionInfo     → Version history entry                 │
// ├─────────────────────────────────────────────────────────────────┤
// │ BUILD DTOs                                                      │
// │   FMBuildInfo           → Build run info                        │
// │   FMBuildDetailInfo     → Full build with logs                  │
// ├─────────────────────────────────────────────────────────────────┤
// │ MODULE CONFIGURATION                                            │
// │   FMModules             → Available modules list                │
// │   FMFileTypes           → Supported file types                  │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // PROJECT DTOs
    // ============================================================

    /// <summary>
    /// Project information for list views and detail views.
    /// </summary>
    public class FMProjectInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> IncludedModules { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Number of entities from Entity Wizard</summary>
        public int EntityCount { get; set; }

        /// <summary>Number of relationships from Entity Wizard</summary>
        public int RelationshipCount { get; set; }

        public int FileCount { get; set; }
        public int BuildCount { get; set; }
        public FMBuildInfo? LastBuild { get; set; }
    }

    /// <summary>
    /// Request to create a new project.
    /// </summary>
    public class FMCreateProjectRequest
    {
        /// <summary>Project template to use.</summary>
        public FMProjectTemplate Template { get; set; } = FMProjectTemplate.Starter;

        /// <summary>Project name - must be valid C# identifier.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-friendly display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Project description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Optional modules to include (e.g., ["Tags", "Appointments"]).</summary>
        public List<string> IncludedModules { get; set; } = new();
    }

    /// <summary>
    /// Request to update project metadata.
    /// </summary>
    public class FMUpdateProjectRequest
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // ============================================================
    // FILE DTOs
    // ============================================================

    /// <summary>
    /// File metadata for list views.
    /// </summary>
    public class FMAppFileInfo
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public int CurrentVersion { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// File content for editing.
    /// </summary>
    public class FMAppFileContent
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to save file content. Uses optimistic concurrency.
    /// </summary>
    public class FMSaveFileRequest
    {
        public Guid FileId { get; set; }
        public string Content { get; set; } = string.Empty;

        /// <summary>Expected version - save fails if file was modified.</summary>
        public int ExpectedVersion { get; set; }

        /// <summary>Optional commit message.</summary>
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response from saving a file.
    /// </summary>
    public class FMSaveFileResponse
    {
        public bool Success { get; set; }
        public int NewVersion { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to create a new file.
    /// </summary>
    public class FMCreateFileRequest
    {
        public Guid ProjectId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Version history entry.
    /// </summary>
    public class FMFileVersionInfo
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    // ============================================================
    // BUILD DTOs
    // ============================================================

    /// <summary>
    /// Build information for list views.
    /// </summary>
    public class FMBuildInfo
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public int BuildNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? ArtifactSizeBytes { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed build information including log output.
    /// </summary>
    public class FMBuildDetailInfo : FMBuildInfo
    {
        public string LogOutput { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to start a new build.
    /// </summary>
    public class FMStartBuildRequest
    {
        public Guid ProjectId { get; set; }
    }

    // ============================================================
    // AVAILABLE MODULES
    // ============================================================

    /// <summary>
    /// Lists available FreeCRM modules for project configuration.
    /// </summary>
    public static class FMModules
    {
        /// <summary>Optional modules that can be included/excluded.</summary>
        public static readonly List<string> Optional = new()
        {
            "Appointments",
            "EmailTemplates",
            "Invoices",
            "Locations",
            "Payments",
            "Services",
            "Tags"
        };

        /// <summary>Required modules always included.</summary>
        public static readonly List<string> Required = new()
        {
            "Contacts",
            "Departments",
            "UserGroups"
        };
    }

    // ============================================================
    // FILE TYPE CONSTANTS
    // ============================================================

    /// <summary>
    /// File type classifications for .App. files.
    /// </summary>
    public static class FMFileTypes
    {
        public const string DataObjects = "DataObjects";
        public const string DataAccess = "DataAccess";
        public const string Controller = "Controller";
        public const string RazorComponent = "RazorComponent";
        public const string RazorPage = "RazorPage";
        public const string Stylesheet = "Stylesheet";
        public const string GlobalSettings = "GlobalSettings";
        public const string EFModel = "EFModel";
        public const string EFDataModel = "EFDataModel";
        public const string HelpersApp = "HelpersApp";
        public const string Utilities = "Utilities";
        public const string Middleware = "Middleware";
        public const string Snippet = "Snippet";
        public const string Other = "Other";
    }
}

#endregion
