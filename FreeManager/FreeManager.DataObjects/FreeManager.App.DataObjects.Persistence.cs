namespace FreeManager;

#region FreeManager Platform - Persistence DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │        DataObjects.App.FreeManager.Persistence.cs               │
// │             Save, Load & Export Wizard Projects                  │
// ├─────────────────────────────────────────────────────────────────┤
// │ SAVED PROJECT DTOs                                              │
// │   FMSavedProject         → Wizard project with state JSON       │
// │   FMSavedProjectFilter   → Pagination & filtering options       │
// │   FMSavedProjectFilterResult → Paginated result with totals     │
// ├─────────────────────────────────────────────────────────────────┤
// │ EXPORT DTOs                                                     │
// │   FMProjectZipResponse   → ZIP download with generated files    │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // SAVED PROJECT DTOs
    // ============================================================

    /// <summary>
    /// DTO for Entity Wizard saved project (for Dashboard display).
    /// </summary>
    public class FMSavedProject
    {
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int EntityCount { get; set; }

        /// <summary>Full wizard state JSON - only populated for edit</summary>
        public string? EntityWizardStateJson { get; set; }
        public Guid FMProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RelationshipCount { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Filter for querying saved projects.
    /// </summary>
    public class FMSavedProjectFilter
    {
        public bool IncludeDeleted { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int Skip => (Page - 1) * PageSize;
        public string SortColumn { get; set; } = "UpdatedAt";
        public bool SortDescending { get; set; } = true;
        public string? Status { get; set; }
    }

    /// <summary>
    /// Paginated result for saved projects.
    /// </summary>
    public class FMSavedProjectFilterResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<FMSavedProject> Records { get; set; } = new();
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public int TotalRecords { get; set; }
    }

    /// <summary>
    /// Request to save an Entity Wizard project.
    /// </summary>
    public class FMSaveWizardProjectRequest
    {
        /// <summary>Description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Display name</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Entity count (denormalized)</summary>
        public int EntityCount { get; set; }

        /// <summary>Full EntityWizardState serialized as JSON</summary>
        public string EntityWizardStateJson { get; set; } = string.Empty;
        /// <summary>Project ID - null for new, existing ID for update</summary>
        public Guid? FMProjectId { get; set; }

        /// <summary>Project name (C# identifier)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Relationship count (denormalized)</summary>
        public int RelationshipCount { get; set; }
    }

    // ============================================================
    // ZIP EXPORT DTOs
    // ============================================================

    /// <summary>
    /// Generated file for ZIP download.
    /// </summary>
    public class GeneratedFileForDownload
    {
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response containing ZIP file as base64.
    /// </summary>
    public class FMProjectZipResponse
    {
        public string Base64Content { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public int FileCount { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool Success { get; set; }
    }
}

#endregion
