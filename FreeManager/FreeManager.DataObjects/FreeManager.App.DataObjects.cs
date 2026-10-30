namespace FreeManager;

// ---- DataObjects.Projects ----

#region FreeManager Platform - Project, File & Build DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │            FreeManager.App.DataObjects.cs: Projects             │
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
        public int BuildCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Number of entities from Entity Wizard</summary>
        public int EntityCount { get; set; }

        public int FileCount { get; set; }
        public Guid Id { get; set; }
        public List<string> IncludedModules { get; set; } = new();
        public FMBuildInfo? LastBuild { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>Number of relationships from Entity Wizard</summary>
        public int RelationshipCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create a new project.
    /// </summary>
    public class FMCreateProjectRequest
    {
        /// <summary>Project description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Human-friendly display name.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional modules to include (e.g., ["Tags", "Appointments"]).</summary>
        public List<string> IncludedModules { get; set; } = new();

        /// <summary>Project name - must be valid C# identifier.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Project template to use.</summary>
        public FMProjectTemplate Template { get; set; } = FMProjectTemplate.Starter;
    }

    /// <summary>
    /// Request to update project metadata.
    /// </summary>
    public class FMUpdateProjectRequest
    {
        public string Description { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Guid Id { get; set; }
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
        public int CurrentVersion { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// File content for editing.
    /// </summary>
    public class FMAppFileContent
    {
        public string Content { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
    }

    /// <summary>
    /// Request to save file content. Uses optimistic concurrency.
    /// </summary>
    public class FMSaveFileRequest
    {
        /// <summary>Optional commit message.</summary>
        public string Comment { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        /// <summary>Expected version - save fails if file was modified.</summary>
        public int ExpectedVersion { get; set; }
        public Guid FileId { get; set; }
    }

    /// <summary>
    /// Response from saving a file.
    /// </summary>
    public class FMSaveFileResponse
    {
        public string Message { get; set; } = string.Empty;
        public int NewVersion { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Request to create a new file.
    /// </summary>
    public class FMCreateFileRequest
    {
        public string Content { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
    }

    /// <summary>
    /// Version history entry.
    /// </summary>
    public class FMFileVersionInfo
    {
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public int Version { get; set; }
    }

    // ============================================================
    // BUILD DTOs
    // ============================================================

    /// <summary>
    /// Build information for list views.
    /// </summary>
    public class FMBuildInfo
    {
        public long? ArtifactSizeBytes { get; set; }
        public int BuildNumber { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public DateTime? StartedAt { get; set; }
        public string Status { get; set; } = string.Empty;
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

// ---- DataObjects.Templates ----

#region FreeManager Platform - Project & Page Templates
// ┌─────────────────────────────────────────────────────────────────┐
// │            FreeManager.App.DataObjects.cs: Templates            │
// │               Project & Page Pattern Templates                   │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROJECT TEMPLATES (enum + info)                                 │
// │   FMProjectTemplate      → Empty, Skeleton, Starter, FullCrud   │
// │   FMProjectTemplateInfo  → Display info with file counts        │
// │   FMProjectTemplates.All → All templates with descriptions      │
// ├─────────────────────────────────────────────────────────────────┤
// │ PAGE PATTERN TEMPLATES                                          │
// │   FMPagePattern          → List, Details, EditForm, Dashboard   │
// │                           → MasterDetail, Wizard, KanbanBoard   │
// │   FMPagePatternInfo      → Pattern info with component list     │
// ├─────────────────────────────────────────────────────────────────┤
// │ PAGE PATTERNS CATALOG                                           │
// │   FMPagePatterns.All     → Complete pattern library             │
// │   FMPagePatterns.Get()   → Get pattern by enum                  │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // PROJECT TEMPLATES
    // ============================================================

    /// <summary>
    /// Available project templates (starter scaffolding).
    /// </summary>
    public enum FMProjectTemplate
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
    /// Project template information for UI display.
    /// </summary>
    public class FMProjectTemplateInfo
    {
        public string Description { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public string Icon { get; set; } = string.Empty;
        public List<string> IncludedFiles { get; set; } = new();
        public bool IsRecommended { get; set; }
        public string Name { get; set; } = string.Empty;
        public FMProjectTemplate Template { get; set; }
    }

    // ============================================================
    // PAGE PATTERN TEMPLATES
    // ============================================================

    /// <summary>
    /// Available page pattern templates for the wizard.
    /// Based on focus group feedback and common UX patterns.
    /// </summary>
    public enum FMPagePattern
    {
        /// <summary>Standard CRUD list + edit form (default Entity Wizard output).</summary>
        CrudListEdit = 0,

        /// <summary>Multi-step wizard for setup/onboarding flows.</summary>
        QuickStartWizard = 1,

        /// <summary>Email-style split panel: list on left, detail on right.</summary>
        SplitPanelMasterDetail = 2,

        /// <summary>Drag-and-drop Kanban board for task/status tracking.</summary>
        KanbanBoard = 3,

        /// <summary>Chronological activity feed / audit log timeline.</summary>
        TimelineActivityFeed = 4,

        /// <summary>Multi-section settings hub with sidebar navigation.</summary>
        SettingsHub = 5,

        /// <summary>Multi-step data import wizard with mapping and validation.</summary>
        DataImportWizard = 6,

        /// <summary>Side-by-side comparison view for versions/diffs.</summary>
        ComparisonView = 7,

        /// <summary>Customizable dashboard with drag-drop widgets.</summary>
        CustomizableDashboard = 8,

        /// <summary>Multi-step approval workflow with status tracking.</summary>
        ApprovalWorkflow = 9,

        /// <summary>Interactive API documentation with try-it-now.</summary>
        ApiDocumentation = 10
    }

    /// <summary>
    /// Detailed information about a page pattern template.
    /// </summary>
    public class FMPagePatternInfo
    {
        public string Complexity { get; set; } = "Medium";
        public string Description { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// True if this pattern has a working code generator.
        /// False patterns show a "Coming Soon" warning.
        /// </summary>
        public bool IsImplemented { get; set; } = false;
        public List<string> KeyFeatures { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public FMPagePattern Pattern { get; set; }
        public int Priority { get; set; } = 5;
        public bool RequiresEntity { get; set; } = true;
        public string UseCase { get; set; } = string.Empty;
    }

    /// <summary>
    /// Static helper to get page pattern metadata.
    /// </summary>
    public static class FMPagePatterns
    {
        /// <summary>All available page patterns with metadata.</summary>
        public static readonly List<FMPagePatternInfo> All = new()
        {
            new() {
                Pattern = FMPagePattern.CrudListEdit,
                Name = "CRUD List + Edit",
                Description = "Standard paginated list with create/edit/delete modal",
                Icon = "fa-table-list",
                UseCase = "Basic data management (contacts, products, orders)",
                Complexity = "Low",
                KeyFeatures = new() { "Paginated list", "Search & filter", "Edit modal", "Soft delete" },
                GeneratedFiles = new() { "ListPage.razor", "EditModal.razor" },
                RequiresEntity = true,
                Priority = 1,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.QuickStartWizard,
                Name = "Quick Start Wizard",
                Description = "Multi-step wizard for guided setup or onboarding",
                Icon = "fa-wand-magic-sparkles",
                UseCase = "Onboarding, initial setup, multi-step forms",
                Complexity = "Medium",
                KeyFeatures = new() { "Progress stepper", "Step validation", "Skip option", "State persistence" },
                GeneratedFiles = new() { "Wizard.razor", "Step1.razor", "Step2.razor", "Step3.razor", "Complete.razor" },
                RequiresEntity = false,
                Priority = 2,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.SplitPanelMasterDetail,
                Name = "Split-Panel Master-Detail",
                Description = "Email-style interface: list on left, detail on right",
                Icon = "fa-columns",
                UseCase = "Email inbox, ticket systems, document browsers",
                Complexity = "Medium",
                KeyFeatures = new() { "Resizable panels", "Selection highlighting", "Keyboard navigation", "Detail tabs" },
                GeneratedFiles = new() { "SplitView.razor", "ListPanel.razor", "DetailPanel.razor", "ListItem.razor" },
                RequiresEntity = true,
                Priority = 2,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.KanbanBoard,
                Name = "Kanban Board",
                Description = "Drag-and-drop columns for visual workflow management",
                Icon = "fa-grip-vertical",
                UseCase = "Task boards, project management, status tracking",
                Complexity = "High",
                KeyFeatures = new() { "Drag-and-drop", "WIP limits", "Quick-edit cards", "Swimlanes" },
                GeneratedFiles = new() { "KanbanBoard.razor", "KanbanColumn.razor", "KanbanCard.razor", "CardEditor.razor" },
                RequiresEntity = true,
                Priority = 3,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.TimelineActivityFeed,
                Name = "Timeline / Activity Feed",
                Description = "Chronological event history grouped by date",
                Icon = "fa-clock-rotate-left",
                UseCase = "Audit logs, activity streams, changelogs",
                Complexity = "Low",
                KeyFeatures = new() { "Date grouping", "Event icons", "Entity links", "Infinite scroll", "Filters" },
                GeneratedFiles = new() { "Timeline.App.razor", "TimelineDay.App.razor", "TimelineEvent.App.razor", "TimelineFilters.App.razor" },
                RequiresEntity = true,
                Priority = 1,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.SettingsHub,
                Name = "Settings Hub",
                Description = "Multi-section settings page with sidebar navigation",
                Icon = "fa-sliders",
                UseCase = "Tenant configuration, user preferences, admin settings",
                Complexity = "Medium",
                KeyFeatures = new() { "Collapsible sidebar", "Section highlighting", "Role-based visibility", "Unsaved warning" },
                GeneratedFiles = new() { "SettingsHub.razor", "SettingsSidebar.razor", "ProfileSettings.razor", "NotificationSettings.razor" },
                RequiresEntity = false,
                Priority = 1,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.DataImportWizard,
                Name = "Data Import Wizard",
                Description = "Multi-step file upload with column mapping and validation",
                Icon = "fa-file-import",
                UseCase = "CSV import, data migration, bulk upload",
                Complexity = "High",
                KeyFeatures = new() { "Drag-drop upload", "Column mapping", "Validation preview", "Progress tracking", "Rollback" },
                GeneratedFiles = new() { "ImportWizard.App.razor", "UploadStep.App.razor", "MappingStep.App.razor", "ValidateStep.App.razor", "ProgressStep.App.razor" },
                RequiresEntity = true,
                Priority = 3,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.ComparisonView,
                Name = "Comparison View",
                Description = "Side-by-side comparison for versions or diffs",
                Icon = "fa-code-compare",
                UseCase = "Version history, merge conflicts, before/after",
                Complexity = "Medium",
                KeyFeatures = new() { "Side-by-side view", "Change highlighting", "Version selector", "Restore option" },
                GeneratedFiles = new() { "{Entity}CompareView.App.razor", "{Entity}VersionSelector.App.razor", "{Entity}SideBySide.App.razor", "{Entity}FieldDiff.App.razor" },
                RequiresEntity = true,
                Priority = 4,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.CustomizableDashboard,
                Name = "Customizable Dashboard",
                Description = "User-configurable dashboard with drag-drop widgets",
                Icon = "fa-grip",
                UseCase = "Home page, analytics dashboard, reporting",
                Complexity = "High",
                KeyFeatures = new() { "Drag-drop placement", "Resizable widgets", "Widget library", "Layout persistence" },
                GeneratedFiles = new() { "Dashboard.razor", "DashboardGrid.razor", "WidgetContainer.razor", "WidgetPicker.razor", "StatsWidget.razor", "ChartWidget.razor" },
                RequiresEntity = false,
                Priority = 3,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.ApprovalWorkflow,
                Name = "Approval Workflow",
                Description = "Multi-step approval process with status tracking",
                Icon = "fa-stamp",
                UseCase = "Purchase requests, leave approval, document review",
                Complexity = "Medium",
                KeyFeatures = new() { "Workflow progress", "Role-based actions", "Approval history", "Email notifications" },
                GeneratedFiles = new() { "ApprovalDetail.razor", "WorkflowProgress.razor", "ApprovalHistory.razor", "ApprovalActions.razor", "ApprovalList.razor" },
                RequiresEntity = true,
                Priority = 3,
                IsImplemented = true
            },
            new() {
                Pattern = FMPagePattern.ApiDocumentation,
                Name = "API Documentation",
                Description = "Interactive API documentation with try-it-now",
                Icon = "fa-plug",
                UseCase = "Developer portal, API reference, endpoint explorer",
                Complexity = "Medium",
                KeyFeatures = new() { "Endpoint tree", "Request/response schema", "Try-it form", "Code generation" },
                GeneratedFiles = new() { "ApiExplorer.razor", "EndpointTree.razor", "EndpointDetail.razor", "TryItPanel.razor", "ResponseViewer.razor" },
                RequiresEntity = false,
                Priority = 4,
                IsImplemented = true
            }
        };

        /// <summary>Get patterns by complexity level.</summary>
        public static List<FMPagePatternInfo> GetByComplexity(string complexity)
            => All.Where(p => p.Complexity == complexity).ToList();

        /// <summary>Get patterns sorted by priority (recommended first).</summary>
        public static List<FMPagePatternInfo> GetByPriority()
            => All.OrderBy(p => p.Priority).ThenBy(p => p.Name).ToList();

        /// <summary>Get patterns that require an entity definition.</summary>
        public static List<FMPagePatternInfo> GetEntityPatterns()
            => All.Where(p => p.RequiresEntity).ToList();

        /// <summary>Get only implemented patterns.</summary>
        public static List<FMPagePatternInfo> GetImplemented()
            => All.Where(p => p.IsImplemented).ToList();
    }
}

#endregion

// ---- DataObjects.ApplicationTemplates ----

#region FreeManager Platform - Application Templates
// ┌─────────────────────────────────────────────────────────────────┐
// │      FreeManager.App.DataObjects.cs: ApplicationTemplates       │
// │           Pre-built Application Templates                        │
// ├─────────────────────────────────────────────────────────────────┤
// │ APPLICATION TEMPLATES                                           │
// │   ApplicationTemplate      → Complete app starter with entities │
// │   ApplicationTemplates.All → FreeBase, FreeTracker, FreeAudit   │
// ├─────────────────────────────────────────────────────────────────┤
// │ TEMPLATE PROGRESSION                                            │
// │   FreeBase    → Collection + Categories (foundation)            │
// │   FreeTracker → + Assignment + Checkout + Status                │
// │   FreeAudit   → + Logging + External API + Reports              │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // APPLICATION TEMPLATE
    // ============================================================

    /// <summary>
    /// A complete application template with multiple entities, relationships, and features.
    /// </summary>
    public class ApplicationTemplate
    {
        public DashboardConfig? Dashboard { get; set; }
        public EntityWizardOptions DefaultOptions { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Beginner";
        public List<EntityDefinition> Entities { get; set; } = new();

        // Advanced generation configuration (for FreeAudit and similar)
        public ExternalApiConfig? ExternalApi { get; set; }
        public List<string> Features { get; set; } = new();

        // Helper to check if template has a feature
        public bool HasFeature(string feature) =>
            Features.Any(f => f.Equals(feature, StringComparison.OrdinalIgnoreCase));
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = "primary";
        public string Id { get; set; } = string.Empty;
        public bool IsImplemented { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public List<RelationshipDefinition> Relationships { get; set; } = new();
        public string ShortDescription { get; set; } = string.Empty;
        public List<string> UseCases { get; set; } = new();
    }

    // ============================================================
    // EXTERNAL API CONFIGURATION
    // ============================================================

    /// <summary>
    /// Configuration for generating external API endpoints (e.g., for FreeAudit).
    /// </summary>
    public class ExternalApiConfig
    {
        /// <summary>Enable batch insert endpoint.</summary>
        public bool AllowBatch { get; set; } = true;

        /// <summary>Entity that stores API keys (typically "SourceSystem").</summary>
        public string ApiKeyEntity { get; set; } = "SourceSystem";

        /// <summary>Property on ApiKeyEntity that stores the hashed key.</summary>
        public string ApiKeyProperty { get; set; } = "ApiKey";

        /// <summary>Authentication type: "ApiKey", "Bearer", or "None".</summary>
        public string AuthType { get; set; } = "ApiKey";

        /// <summary>Maximum events per batch request.</summary>
        public int BatchLimit { get; set; } = 1000;

        /// <summary>Controller class name (e.g., "Glba" generates GlbaController).</summary>
        public string ControllerName { get; set; } = "External";

        /// <summary>Property used for deduplication (e.g., "SourceEventId").</summary>
        public string? DedupeProperty { get; set; }
        /// <summary>Name of the entity that receives external events.</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Route prefix for the external API (e.g., "api/glba").</summary>
        public string RoutePrefix { get; set; } = "api/external";

        /// <summary>Entity to update stats on each event (e.g., "DataSubject").</summary>
        public string? StatsEntity { get; set; }

        /// <summary>Property on StatsEntity to match (e.g., "ExternalId" matched to event's SubjectId).</summary>
        public string? StatsMatchProperty { get; set; }
    }

    // ============================================================
    // DASHBOARD CONFIGURATION
    // ============================================================

    /// <summary>
    /// Configuration for generating a dashboard page.
    /// </summary>
    public class DashboardConfig
    {
        /// <summary>Icon for nav menu (FontAwesome class).</summary>
        public string Icon { get; set; } = "fa-chart-line";
        /// <summary>Page name (e.g., "GlbaDashboard").</summary>
        public string PageName { get; set; } = "Dashboard";

        /// <summary>Page title shown in UI.</summary>
        public string PageTitle { get; set; } = "Dashboard";

        /// <summary>Properties to show in recent feed columns.</summary>
        public List<string> RecentFeedColumns { get; set; } = new();

        /// <summary>Entity for recent feed table.</summary>
        public string RecentFeedEntity { get; set; } = string.Empty;

        /// <summary>Number of items in recent feed.</summary>
        public int RecentFeedLimit { get; set; } = 20;

        /// <summary>Date property on StatsEntity for time-based grouping.</summary>
        public string StatsDateProperty { get; set; } = "CreatedAt";

        /// <summary>Entity to count for stats cards.</summary>
        public string StatsEntity { get; set; } = string.Empty;

        /// <summary>Time periods for stats cards.</summary>
        public List<string> StatsPeriods { get; set; } = new() { "Today", "ThisWeek", "ThisMonth" };

        /// <summary>Property that indicates active status.</summary>
        public string StatusActiveProperty { get; set; } = "IsActive";

        /// <summary>Property that shows count/total.</summary>
        public string? StatusCountProperty { get; set; }

        /// <summary>Entity for status grid (e.g., source systems).</summary>
        public string? StatusGridEntity { get; set; }

        /// <summary>Property that shows last activity timestamp.</summary>
        public string? StatusLastActivityProperty { get; set; }
    }

    // ============================================================
    // APPLICATION TEMPLATES CATALOG
    // ============================================================

    /// <summary>
    /// Catalog of pre-built application templates.
    /// </summary>
    public static partial class ApplicationTemplates
    {
        /// <summary>All available application templates.</summary>
        public static List<ApplicationTemplate> All => new()
        {
            FreeBase,
            FreeTracker,
            FreeAudit
        };

        /// <summary>Get template by ID.</summary>
        public static ApplicationTemplate? GetById(string id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Converts an ApplicationTemplate to EntityWizardState for code generation.
        /// </summary>
        public static EntityWizardState ToWizardState(
            ApplicationTemplate template,
            string projectName,
            string? displayName = null,
            Dictionary<string, string>? entityRenames = null){
            var state = new EntityWizardState
            {
                Project = new FMCreateProjectRequest
                {
                    Name = projectName,
                    DisplayName = displayName ?? projectName,
                    Template = FMProjectTemplate.FullCrud
                },
                Options = new EntityWizardOptions
                {
                    GenerateListPage = template.DefaultOptions.GenerateListPage,
                    GenerateEditPage = template.DefaultOptions.GenerateEditPage,
                    IncludeAuditFields = template.DefaultOptions.IncludeAuditFields,
                    IncludeSoftDelete = template.DefaultOptions.IncludeSoftDelete,
                    IncludeTenantId = template.DefaultOptions.IncludeTenantId,
                    GenerateSignalRUpdates = template.DefaultOptions.GenerateSignalRUpdates
                },
                Entities = new List<EntityDefinition>(),
                Relationships = new List<RelationshipDefinition>()
            };

            // Copy entities, applying renames if specified
            foreach (var entity in template.Entities)
            {
                var newName = entity.Name;
                if (entityRenames != null && entityRenames.TryGetValue(entity.Name, out var rename) && !string.IsNullOrWhiteSpace(rename))
                {
                    newName = rename;
                }

                var newEntity = new EntityDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = newName,
                    PluralName = newName + "s",
                    TableName = newName + "s",
                    PrimaryKeyName = newName + "Id",
                    PrimaryKeyType = entity.PrimaryKeyType,
                    Description = entity.Description,
                    SortOrder = entity.SortOrder,
                    Properties = entity.Properties.Select(p => new PropertyDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = p.Name,
                        Type = p.Type,
                        IsRequired = p.IsRequired,
                        IsNullable = p.IsNullable,
                        DefaultValue = p.DefaultValue,
                        MaxLength = p.MaxLength,
                        SortOrder = p.SortOrder,
                        Description = p.Description,
                        IsFilterable = p.IsFilterable
                    }).ToList()
                };

                state.Entities.Add(newEntity);
            }

            // Copy relationships, updating entity names if renamed
            foreach (var rel in template.Relationships)
            {
                var sourceEntityName = rel.SourceEntityName;
                var targetEntityName = rel.TargetEntityName;

                if (entityRenames != null)
                {
                    if (entityRenames.TryGetValue(rel.SourceEntityName, out var sourceRename) && !string.IsNullOrWhiteSpace(sourceRename))
                        sourceEntityName = sourceRename;
                    if (entityRenames.TryGetValue(rel.TargetEntityName, out var targetRename) && !string.IsNullOrWhiteSpace(targetRename))
                        targetEntityName = targetRename;
                }

                state.Relationships.Add(new RelationshipDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = $"{sourceEntityName}_{targetEntityName}",
                    Type = rel.Type,
                    SourceEntityName = sourceEntityName,
                    TargetEntityName = targetEntityName,
                    SourceNavigationProperty = rel.SourceNavigationProperty,
                    TargetNavigationProperty = rel.TargetNavigationProperty,
                    ForeignKeyProperty = rel.ForeignKeyProperty,
                    IsRequired = rel.IsRequired,
                    OnDelete = rel.OnDelete,
                    SortOrder = rel.SortOrder
                });
            }

            // Copy advanced generation configs (for FreeAudit-style templates)
            if (template.ExternalApi != null)
            {
                state.ExternalApi = new ExternalApiConfig
                {
                    EntityName = template.ExternalApi.EntityName,
                    RoutePrefix = template.ExternalApi.RoutePrefix,
                    ControllerName = template.ExternalApi.ControllerName,
                    AuthType = template.ExternalApi.AuthType,
                    ApiKeyEntity = template.ExternalApi.ApiKeyEntity,
                    ApiKeyProperty = template.ExternalApi.ApiKeyProperty,
                    AllowBatch = template.ExternalApi.AllowBatch,
                    BatchLimit = template.ExternalApi.BatchLimit,
                    DedupeProperty = template.ExternalApi.DedupeProperty,
                    StatsEntity = template.ExternalApi.StatsEntity,
                    StatsMatchProperty = template.ExternalApi.StatsMatchProperty
                };
            }

            if (template.Dashboard != null)
            {
                state.Dashboard = new DashboardConfig
                {
                    PageName = template.Dashboard.PageName,
                    PageTitle = template.Dashboard.PageTitle,
                    Icon = template.Dashboard.Icon,
                    StatsEntity = template.Dashboard.StatsEntity,
                    StatsDateProperty = template.Dashboard.StatsDateProperty,
                    StatsPeriods = new List<string>(template.Dashboard.StatsPeriods),
                    RecentFeedEntity = template.Dashboard.RecentFeedEntity,
                    RecentFeedLimit = template.Dashboard.RecentFeedLimit,
                    RecentFeedColumns = new List<string>(template.Dashboard.RecentFeedColumns),
                    StatusGridEntity = template.Dashboard.StatusGridEntity,
                    StatusActiveProperty = template.Dashboard.StatusActiveProperty,
                    StatusLastActivityProperty = template.Dashboard.StatusLastActivityProperty,
                    StatusCountProperty = template.Dashboard.StatusCountProperty
                };
            }

            return state;
        }
    }
}

#endregion

// ---- DataObjects.ApplicationTemplates.FreeAudit ----

// FreeAudit catalog entry: Logging + External API + Reports. Part of DataObjects.ApplicationTemplates (see the ApplicationTemplates section of FreeManager.App.DataObjects.cs).

public partial class DataObjects
{
    public static partial class ApplicationTemplates
    {
        // ============================================================
        // FREEAUDIT - Logging + External API + Reports
        // ============================================================

        public static ApplicationTemplate FreeAudit => new()
        {
            Id = "freeaudit",
            Name = "FreeAudit",
            Description = "Full GLBA compliance tracking with external API, event logging, and reports. Built for audit trails and regulatory compliance.",
            ShortDescription = "+ Compliance + Logging + Reports",
            Icon = "fa-shield-halved",
            IconColor = "danger",
            Difficulty = "Advanced",
            UseCases = new() { "GLBA compliance", "Audit trails", "Access logging", "Regulatory reporting" },
            Features = new()
            {
                "External API endpoint",
                "API key authentication",
                "Access event logging",
                "Compliance dashboard",
                "Report generation",
                "Subject lookup"
            },

            // External API configuration for receiving events from source systems
            ExternalApi = new ExternalApiConfig
            {
                EntityName = "AccessEvent",
                RoutePrefix = "api/glba",
                ControllerName = "Glba",
                AuthType = "ApiKey",
                ApiKeyEntity = "SourceSystem",
                ApiKeyProperty = "ApiKey",
                AllowBatch = true,
                BatchLimit = 1000,
                DedupeProperty = "SourceEventId",
                StatsEntity = "DataSubject",
                StatsMatchProperty = "ExternalId"
            },

            // Dashboard configuration for GLBA compliance view
            Dashboard = new DashboardConfig
            {
                PageName = "GlbaDashboard",
                PageTitle = "GLBA Compliance Dashboard",
                Icon = "fa-shield-halved",
                StatsEntity = "AccessEvent",
                StatsDateProperty = "AccessedAt",
                StatsPeriods = new() { "Today", "ThisWeek", "ThisMonth" },
                RecentFeedEntity = "AccessEvent",
                RecentFeedLimit = 20,
                RecentFeedColumns = new() { "AccessedAt", "UserId", "SubjectId", "DataCategory", "AccessType" },
                StatusGridEntity = "SourceSystem",
                StatusActiveProperty = "IsActive",
                StatusLastActivityProperty = "LastEventReceivedAt",
                StatusCountProperty = "EventCount"
            },
            Entities = new()
            {
                // SourceSystem
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000001"),
                    Name = "SourceSystem",
                    PluralName = "SourceSystems",
                    PrimaryKeyType = "Guid",
                    Description = "Systems that send events to FreeAudit",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DisplayName", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ApiKey", Type = "string", IsRequired = true, MaxLength = 500, SortOrder = 3, Description = "Hashed API key" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ContactEmail", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "LastEventReceivedAt", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "EventCount", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 7 }
                    }
                },
                // AccessEvent
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000002"),
                    Name = "AccessEvent",
                    PluralName = "AccessEvents",
                    PrimaryKeyType = "Guid",
                    Description = "Individual access log entries",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SourceSystemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SourceEventId", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 2, Description = "Dedupe key from source" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReceivedAt", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserName", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserEmail", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 7 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserDepartment", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 8 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 9, Description = "Student/employee ID" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectType", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 10 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DataCategory", Type = "string", IsRequired = false, MaxLength = 100, SortOrder = 11 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AccessType", Type = "string", IsRequired = true, MaxLength = 50, SortOrder = 12 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Purpose", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 13 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IpAddress", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 14 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AdditionalData", Type = "string", IsRequired = false, SortOrder = 15, Description = "JSON for flexible extras" }
                    }
                },
                // DataSubject
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000003"),
                    Name = "DataSubject",
                    PluralName = "DataSubjects",
                    PrimaryKeyType = "Guid",
                    Description = "Aggregated stats per subject (student/employee)",
                    SortOrder = 3,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ExternalId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectType", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "FirstAccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "LastAccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "TotalAccessCount", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueAccessorCount", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 6 }
                    }
                },
                // ComplianceReport
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000004"),
                    Name = "ComplianceReport",
                    PluralName = "ComplianceReports",
                    PrimaryKeyType = "Guid",
                    Description = "Generated compliance reports",
                    SortOrder = 4,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReportType", Type = "string", IsRequired = true, MaxLength = 50, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "GeneratedAt", Type = "DateTime", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "GeneratedBy", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "PeriodStart", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "PeriodEnd", Type = "DateTime", IsRequired = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "TotalEvents", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueUsers", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 7 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueSubjects", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 8 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReportData", Type = "string", IsRequired = false, SortOrder = 9, Description = "JSON report content" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "FileUrl", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 10 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "SourceSystem_Events", Type = RelationshipType.OneToMany, SourceEntityName = "SourceSystem", TargetEntityName = "AccessEvent", ForeignKeyProperty = "SourceSystemId", IsRequired = true }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = false, // Audit logs shouldn't be soft-deleted
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };
    }
}

// ---- DataObjects.ApplicationTemplates.FreeBase ----

// FreeBase catalog entry: collection of things. Part of DataObjects.ApplicationTemplates (see the ApplicationTemplates section of FreeManager.App.DataObjects.cs).

public partial class DataObjects
{
    public static partial class ApplicationTemplates
    {
        // ============================================================
        // FREEBASE - Collection of Things
        // ============================================================

        public static ApplicationTemplate FreeBase => new()
        {
            Id = "freebase",
            Name = "FreeBase",
            Description = "Simple collection management with items and categories. Perfect for catalogs, inventories, and contact lists.",
            ShortDescription = "Collection of things",
            Icon = "fa-box",
            IconColor = "primary",
            Difficulty = "Beginner",
            UseCases = new() { "Product catalog", "Contact list", "Inventory", "Asset registry" },
            Features = new() { "Items + Categories", "Basic CRUD operations", "List + Edit pages" },
            Entities = new()
            {
                // Category entity
                new EntityDefinition
                {
                    Id = Guid.Parse("10000001-0001-0001-0001-000000000001"),
                    Name = "Category",
                    PluralName = "Categories",
                    TableName = "Categories",
                    PrimaryKeyName = "CategoryId",
                    PrimaryKeyType = "Guid",
                    Description = "Organize items into groups",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 4 }
                    }
                },
                // Item entity
                new EntityDefinition
                {
                    Id = Guid.Parse("10000001-0001-0001-0001-000000000002"),
                    Name = "Item",
                    PluralName = "Items",
                    TableName = "Items",
                    PrimaryKeyName = "ItemId",
                    PrimaryKeyType = "Guid",
                    Description = "Main entity for your collection",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CategoryId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 1, Description = "FK to Category" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 4 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Category_Items",
                    Type = RelationshipType.OneToMany,
                    SourceEntityName = "Category",
                    TargetEntityName = "Item",
                    SourceNavigationProperty = "Items",
                    TargetNavigationProperty = "Category",
                    ForeignKeyProperty = "CategoryId",
                    IsRequired = false,
                    OnDelete = RelationshipDeleteBehavior.SetNull
                }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = true,
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };
    }
}

// ---- DataObjects.ApplicationTemplates.FreeTracker ----

// FreeTracker catalog entry: Assignment + Checkout + Status. Part of DataObjects.ApplicationTemplates (see the ApplicationTemplates section of FreeManager.App.DataObjects.cs).

public partial class DataObjects
{
    public static partial class ApplicationTemplates
    {
        // ============================================================
        // FREETRACKER - Assignment + Checkout + Status
        // ============================================================

        public static ApplicationTemplate FreeTracker => new()
        {
            Id = "freetracker",
            Name = "FreeTracker",
            Description = "Track assignments, checkouts, and status workflows. Great for helpdesk, equipment loans, and task management.",
            ShortDescription = "+ Assignment + Checkout + Status",
            Icon = "fa-clipboard-list",
            IconColor = "warning",
            Difficulty = "Intermediate",
            UseCases = new() { "Helpdesk tickets", "Equipment loans", "Task management", "Library checkouts" },
            Features = new()
            {
                "Everything in FreeBase",
                "Assign items to users",
                "Checkout/return tracking",
                "Status workflow",
                "Due date notifications"
            },
            Entities = new()
            {
                // Category (inherited from FreeBase)
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000001"),
                    Name = "Category",
                    PluralName = "Categories",
                    PrimaryKeyType = "Guid",
                    Description = "Organize items into groups",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 3 }
                    }
                },
                // Status lookup
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000002"),
                    Name = "Status",
                    PluralName = "Statuses",
                    PrimaryKeyType = "Guid",
                    Description = "Workflow states for items",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 100, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsDefault", Type = "bool", IsRequired = false, DefaultValue = "false", SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsCompleted", Type = "bool", IsRequired = false, DefaultValue = "false", SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Color", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 5 }
                    }
                },
                // Item (extended from FreeBase)
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000003"),
                    Name = "Item",
                    PluralName = "Items",
                    PrimaryKeyType = "Guid",
                    Description = "Main entity with status tracking",
                    SortOrder = 3,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CategoryId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "StatusId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 5 }
                    }
                },
                // Assignment
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000004"),
                    Name = "Assignment",
                    PluralName = "Assignments",
                    PrimaryKeyType = "Guid",
                    Description = "Track who is responsible for items",
                    SortOrder = 4,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ItemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedToUserId", Type = "Guid", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedByUserId", Type = "Guid", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedDate", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DueDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CompletedDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Notes", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 7 }
                    }
                },
                // CheckoutRecord
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000005"),
                    Name = "CheckoutRecord",
                    PluralName = "CheckoutRecords",
                    PrimaryKeyType = "Guid",
                    Description = "Track borrowing/checkout history",
                    SortOrder = 5,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ItemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutToUserId", Type = "Guid", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutByUserId", Type = "Guid", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutDate", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DueDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReturnedDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Notes", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 7 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Category_Items", Type = RelationshipType.OneToMany, SourceEntityName = "Category", TargetEntityName = "Item", ForeignKeyProperty = "CategoryId", IsRequired = false },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Status_Items", Type = RelationshipType.OneToMany, SourceEntityName = "Status", TargetEntityName = "Item", ForeignKeyProperty = "StatusId", IsRequired = false },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Item_Assignments", Type = RelationshipType.OneToMany, SourceEntityName = "Item", TargetEntityName = "Assignment", ForeignKeyProperty = "ItemId", IsRequired = true },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Item_Checkouts", Type = RelationshipType.OneToMany, SourceEntityName = "Item", TargetEntityName = "CheckoutRecord", ForeignKeyProperty = "ItemId", IsRequired = true }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = true,
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };
    }
}

// ---- DataObjects.EntityWizard ----

#region FreeManager Platform - Entity Wizard DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │          FreeManager.App.DataObjects.cs: EntityWizard           │
// │              Entity Builder Wizard DTOs (20+ classes)            │
// ├─────────────────────────────────────────────────────────────────┤
// │ WIZARD STATE                                                    │
// │   EntityWizardState     → Complete wizard state container       │
// │   EntityWizardOptions   → Generation options (tests, API, etc)  │
// │   GeneratedFileInfo     → Info about generated output files     │
// ├─────────────────────────────────────────────────────────────────┤
// │ SETUP WIZARD BUILDER                                            │
// │   SetupWizardDefinition      → Multi-step wizard definition     │
// │   SetupWizardStepDefinition  → Individual step with fields      │
// ├─────────────────────────────────────────────────────────────────┤
// │ ENTITY DEFINITION                                               │
// │   EntityDefinition      → Entity to scaffold (name, pk, props)  │
// │   PropertyDefinition    → Property with type, validation, UI    │
// │   EnumTypeDefinition    → Custom enum with values               │
// │   EnumValue             → Single enum value                     │
// ├─────────────────────────────────────────────────────────────────┤
// │ RELATIONSHIPS                                                   │
// │   RelationshipDefinition → FK between entities (1:N, N:N)       │
// │   ManyToManyConfig       → Join table configuration             │
// ├─────────────────────────────────────────────────────────────────┤
// │ TEMPLATES                                                       │
// │   SavedEntityTemplate    → Reusable entity starter              │
// │   SavedWizardStateTemplate → Complete project template          │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // ENTITY WIZARD STATE
    // ============================================================

    /// <summary>
    /// Complete state for the entity builder wizard.
    /// Supports single entity (backward compatible) or multiple entities with relationships.
    /// </summary>
    public class EntityWizardState
    {
        public DateTime? CompletedAt { get; set; }
        public int CurrentStep { get; set; }

        /// <summary>Dashboard configuration (generates dashboard page with stats)</summary>
        public DashboardConfig? Dashboard { get; set; }

        /// <summary>All entities in this project (multi-entity support)</summary>
        public List<EntityDefinition> Entities { get; set; } = new();

        /// <summary>Backward compatible single entity access</summary>
        public EntityDefinition Entity
        {
            get => Entities.FirstOrDefault() ?? new();
            set { if (Entities.Count == 0) Entities.Add(value); else Entities[0] = value; }
        }

        // Advanced generation configs (from ApplicationTemplate, for FreeAudit-style apps)
        /// <summary>External API configuration (generates controller, middleware, DTOs)</summary>
        public ExternalApiConfig? ExternalApi { get; set; }
        public List<GeneratedFileInfo> GeneratedFiles { get; set; } = new();

        public EntityWizardOptions Options { get; set; } = new();
        public FMCreateProjectRequest Project { get; set; } = new();

        /// <summary>Relationships between entities</summary>
        public List<RelationshipDefinition> Relationships { get; set; } = new();

        /// <summary>Currently selected entity index for editing (null = overview)</summary>
        public int? SelectedEntityIndex { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }

    // ============================================================
    // SETUP WIZARD BUILDER TYPES
    // ============================================================

    /// <summary>
    /// Definition for a setup wizard (multi-step onboarding/setup flow).
    /// </summary>
    public class SetupWizardDefinition
    {
        public bool AllowSkip { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-wand-magic-sparkles";
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public bool PersistStateAcrossSessions { get; set; } = true;
        public List<SetupWizardStepDefinition> Steps { get; set; } = new();
        public bool TrackCompletion { get; set; } = true;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A step within a setup wizard.
    /// </summary>
    public class SetupWizardStepDefinition
    {
        public string Description { get; set; } = string.Empty;
        public List<PropertyDefinition> Fields { get; set; } = new();
        public string Icon { get; set; } = "fa-circle-check";
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsOptional { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public Guid StepId => Id;
    }

    // ============================================================
    // ENTITY DEFINITION
    // ============================================================

    /// <summary>
    /// Definition of an entity to scaffold.
    /// </summary>
    public class EntityDefinition
    {
        /// <summary>Description for this entity</summary>
        public string? Description { get; set; }

        /// <summary>All enums defined for this entity</summary>
        public List<EnumDefinition> Enums { get; set; } = new();
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>True if this is a join table for M:M</summary>
        public bool IsJoinTable { get; set; }

        /// <summary>Singular name (e.g., "Task")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Plural name (e.g., "Tasks")</summary>
        public string PluralName { get; set; } = string.Empty;

        /// <summary>Primary key property name (e.g., "TaskId")</summary>
        public string PrimaryKeyName { get; set; } = string.Empty;

        /// <summary>Primary key type: Guid, int, long</summary>
        public string PrimaryKeyType { get; set; } = "Guid";

        /// <summary>All properties on the entity</summary>
        public List<PropertyDefinition> Properties { get; set; } = new();

        /// <summary>Display order in entity list</summary>
        public int SortOrder { get; set; }

        /// <summary>Database table name (defaults to plural)</summary>
        public string TableName { get; set; } = string.Empty;
    }

    // ============================================================
    // ENTITY RELATIONSHIPS
    // ============================================================

    /// <summary>Types of entity relationships.</summary>
    public enum RelationshipType
    {
        /// <summary>One-to-Many: Source has collection of Target.</summary>
        OneToMany = 0,
        /// <summary>Many-to-Many via join entity.</summary>
        ManyToMany = 1,
        /// <summary>Self-Reference (hierarchies).</summary>
        SelfReference = 2,
        /// <summary>One-to-One.</summary>
        OneToOne = 3
    }

    /// <summary>Delete behavior for relationships.</summary>
    public enum RelationshipDeleteBehavior
    {
        Cascade = 0,
        Restrict = 1,
        SetNull = 2,
        NoAction = 3
    }

    /// <summary>Defines a relationship between two entities.</summary>
    public class RelationshipDefinition
    {
        public string ForeignKeyProperty { get; set; } = string.Empty;
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsRequired { get; set; } = true;
        public string? JoinEntityName { get; set; }
        public string Name { get; set; } = string.Empty;
        public RelationshipDeleteBehavior OnDelete { get; set; } = RelationshipDeleteBehavior.Cascade;
        public int SortOrder { get; set; }
        public string SourceEntityName { get; set; } = string.Empty;
        public string SourceNavigationProperty { get; set; } = string.Empty;
        public string TargetEntityName { get; set; } = string.Empty;
        public string TargetNavigationProperty { get; set; } = string.Empty;
        public RelationshipType Type { get; set; }
    }

    // ============================================================
    // PROPERTY DEFINITION
    // ============================================================

    /// <summary>How a property should be filtered in search interfaces.</summary>
    public enum PropertyFilterType
    {
        /// <summary>No filter generated.</summary>
        None = 0,
        /// <summary>Text contains search (default for strings).</summary>
        TextContains = 1,
        /// <summary>Exact text match.</summary>
        TextExact = 2,
        /// <summary>Date range picker (default for DateTime).</summary>
        DateRange = 3,
        /// <summary>Yes/No/All dropdown (default for bool).</summary>
        BooleanTriState = 4,
        /// <summary>Multi-select checkboxes (default for enum).</summary>
        EnumMultiSelect = 5,
        /// <summary>Single-select dropdown.</summary>
        EnumSingleSelect = 6,
        /// <summary>Numeric range (min/max).</summary>
        NumericRange = 7
    }

    /// <summary>
    /// Definition of a property on an entity.
    /// </summary>
    public class PropertyDefinition
    {
        /// <summary>Default value expression</summary>
        public string? DefaultValue { get; set; }

        /// <summary>Description/comment</summary>
        public string? Description { get; set; }

        /// <summary>If type is enum, this is the enum name</summary>
        public string? EnumName { get; set; }

        /// <summary>How to filter this property</summary>
        public PropertyFilterType? FilterType { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Should this appear in filters?</summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>True if property can be null</summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>True if this is the primary key</summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>True if property is required</summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>True if this is a system field</summary>
        public bool IsSystemField { get; set; }

        /// <summary>Max length for strings</summary>
        public int? MaxLength { get; set; }

        /// <summary>Property name (e.g., "Name", "Status")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Display order</summary>
        public int SortOrder { get; set; }

        /// <summary>C# type: string, int, bool, DateTime, Guid, decimal, or enum:{EnumName}</summary>
        public string Type { get; set; } = "string";
    }

    /// <summary>
    /// Definition of an enum for the entity.
    /// </summary>
    public class EnumDefinition
    {
        public string? DefaultValue { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
    }

    // ============================================================
    // WIZARD OPTIONS
    // ============================================================

    /// <summary>
    /// Options for code generation.
    /// </summary>
    public class EntityWizardOptions
    {
        /// <summary>Default page size for list pagination</summary>
        public int DefaultPageSize { get; set; } = 25;

        /// <summary>Generate an edit/create form page</summary>
        public bool GenerateEditPage { get; set; } = true;

        /// <summary>Generate a paginated list page</summary>
        public bool GenerateListPage { get; set; } = true;

        /// <summary>Generate SignalR update calls</summary>
        public bool GenerateSignalRUpdates { get; set; } = true;

        /// <summary>Include audit fields (CreatedAt, UpdatedAt, etc.)</summary>
        public bool IncludeAuditFields { get; set; } = true;

        /// <summary>Include soft delete (Deleted, DeletedAt fields)</summary>
        public bool IncludeSoftDelete { get; set; } = true;

        /// <summary>Include TenantId for multi-tenant support</summary>
        public bool IncludeTenantId { get; set; } = true;

        /// <summary>When true: no Save/Delete endpoints, no Edit page.</summary>
        public bool IsReadOnly { get; set; } = false;
        /// <summary>Selected page pattern template</summary>
        public FMPagePattern SelectedPagePattern { get; set; } = FMPagePattern.CrudListEdit;
    }

    /// <summary>
    /// Information about a generated file.
    /// </summary>
    public class GeneratedFileInfo
    {
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;

        /// <summary>True if content has been manually edited</summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>Monaco editor language for syntax highlighting</summary>
        public string Language => FileType switch {
            "EFModel" or "DataObjects" or "DataAccess" or "Controller" => "csharp",
            "RazorPage" or "RazorComponent" => "razor",
            "Stylesheet" => "css",
            _ => "plaintext"
        };

        /// <summary>Original content before any manual edits</summary>
        public string OriginalContent { get; set; } = string.Empty;
    }

    // ============================================================
    // WIZARD UI HELPER TYPES
    // ============================================================

    /// <summary>Step information for wizard stepper display.</summary>
    public class WizardStepInfo
    {
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SelectedValue { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public int StepNumber { get; set; }
    }

    /// <summary>Selection item for wizard summary display.</summary>
    public class WizardSelectionItem
    {
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Saved entity template for reuse.</summary>
    public class SavedEntityTemplate
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public EntityDefinition Entity { get; set; } = new();
        public string Icon { get; set; } = "fa-file-code";
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsBuiltIn { get; set; }
        public string Name { get; set; } = string.Empty;
        public EntityWizardOptions Options { get; set; } = new();
        public Guid TemplateId => Id;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ============================================================
    // DASHBOARD & STATS TYPES
    // ============================================================

    /// <summary>Entity list item for dashboard display.</summary>
    public class EntityListItem
    {
        public DateTime CreatedAt { get; set; }
        public Guid EntityId { get => Id; set => Id = value; }
        public int EnumCount { get; set; }
        public int FileCount { get; set; }
        public bool HasAuditFields { get; set; }
        public bool HasSoftDelete { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsReadOnly { get; set; }
        public DateTime? LastGeneratedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int PropertyCount { get; set; }
        public string Status { get; set; } = "Draft";
    }

    /// <summary>Entity builder statistics for dashboard.</summary>
    public class EntityBuilderStats
    {
        public int EntitiesThisWeek { get; set; }
        public DateTime? LastGeneratedAt { get; set; }
        public List<RecentEntityActivity> RecentActivity { get; set; } = new();
        public int TotalEntities { get; set; }
        public int TotalEnums { get; set; }
        public int TotalFilesGenerated { get; set; }
        public int TotalProperties { get; set; }
    }

    /// <summary>Activity item for dashboard.</summary>
    public class RecentEntityActivity
    {
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-circle";
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    // ============================================================
    // IMPORT TYPES
    // ============================================================

    /// <summary>Source type for entity import.</summary>
    public enum ImportSourceType
    {
        CSharpClass,
        JsonSchema,
        SqlTable,
        TypeScript
    }

    /// <summary>Result of an import operation.</summary>
    public class ImportResult
    {
        public int ConfidenceScore { get; set; }
        public EntityDefinition Entity { get; set; } = new();
        public string? EntityName { get; set; }
        public List<EnumDefinition> Enums { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public HashSet<string> ImportedFieldNames { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public List<PropertyDefinition> Properties { get; set; } = new();
        public bool Success { get; set; }
        public List<ImportWarning> Warnings { get; set; } = new();
    }

    /// <summary>Warning from import parsing.</summary>
    public class ImportWarning
    {
        public string? FieldName { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string Severity { get; set; } = "Warning";
        public string? Suggestion { get; set; }
    }
}

#endregion

// ---- DataObjects.EntityWizardPersistence ----

#region FreeManager Platform - Persistence DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │           FreeManager.App.DataObjects.cs: Persistence           │
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

// ---- DataObjects.Gallery ----

#region FreeManager Platform - Template Gallery
// ┌─────────────────────────────────────────────────────────────────┐
// │             FreeManager.App.DataObjects.cs: Gallery             │
// │              Template Gallery & Built-in Templates               │
// ├─────────────────────────────────────────────────────────────────┤
// │ BUILT-IN ENTITY TEMPLATES                                       │
// │   BuiltInEntityTemplates.All → Ready-to-use entity starters:    │
// │     • Basic CRUD        → Simple name/description entity        │
// │     • Task/Ticket       → Status, priority, assignment          │
// │     • Customer/Contact  → Person with email, phone, address     │
// │     • Product/Inventory → SKU, price, quantity                  │
// │     • Document/Content  → Title, body, versioning               │
// ├─────────────────────────────────────────────────────────────────┤
// │ GALLERY TEMPLATE INFO                                           │
// │   GalleryTemplateInfo    → Template card display info           │
// │   GalleryTemplateType    → Entity, Project, or Page template    │
// ├─────────────────────────────────────────────────────────────────┤
// │ GALLERY CATALOG                                                 │
// │   GalleryTemplates.All   → Unified list of all templates        │
// │   GalleryTemplates.ByType(type) → Filter by template type       │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // BUILT-IN ENTITY TEMPLATES
    // ============================================================

    /// <summary>
    /// Built-in entity templates for quick start.
    /// </summary>
    public static class BuiltInEntityTemplates
    {
        public static readonly List<SavedEntityTemplate> All = new()
        {
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000001"),
                Name = "Basic CRUD",
                Description = "Simple entity with name, description, and standard fields",
                Icon = "fa-table",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "Item",
                    PluralName = "Items",
                    PrimaryKeyName = "ItemId",
                    PrimaryKeyType = "Guid"
                }
            },
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000002"),
                Name = "Task/Ticket",
                Description = "Task tracking with status, priority, and assignment",
                Icon = "fa-list-check",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "Task",
                    PluralName = "Tasks",
                    PrimaryKeyName = "TaskId",
                    PrimaryKeyType = "Guid"
                }
            },
            new SavedEntityTemplate
            {
                Id = Guid.Parse("00000001-0000-0000-0000-000000000003"),
                Name = "Audit Log",
                Description = "Read-only event log with user, action, and timestamp",
                Icon = "fa-clock-rotate-left",
                IsBuiltIn = true,
                Entity = new EntityDefinition
                {
                    Name = "AuditEntry",
                    PluralName = "AuditEntries",
                    PrimaryKeyName = "AuditEntryId",
                    PrimaryKeyType = "Guid"
                },
                Options = new EntityWizardOptions { IsReadOnly = true }
            }
        };
    }

    // ============================================================
    // TEMPLATE GALLERY - Unified Template Browsing
    // ============================================================

    /// <summary>
    /// Category of template for the gallery.
    /// </summary>
    public enum GalleryTemplateCategory
    {
        Entity,
        PagePattern,
        PropertySnippet,
        Wizard
    }

    /// <summary>
    /// Unified template info for the Template Gallery page.
    /// </summary>
    public class GalleryTemplate
    {
        public GalleryTemplateCategory Category { get; set; }
        public string CategoryDisplay => Category switch {
            GalleryTemplateCategory.Entity => "Entity Template",
            GalleryTemplateCategory.PagePattern => "Page Pattern",
            GalleryTemplateCategory.PropertySnippet => "Property Snippet",
            GalleryTemplateCategory.Wizard => "Wizard Template",
            _ => "Template"
        };

        /// <summary>Complexity: Low, Medium, High</summary>
        public string Complexity { get; set; } = "Medium";
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;

        /// <summary>Required inputs</summary>
        public List<GalleryTemplateInput> Inputs { get; set; } = new();

        /// <summary>Is this template implemented?</summary>
        public bool IsImplemented { get; set; } = true;
        public string Name { get; set; } = string.Empty;

        /// <summary>What this template generates</summary>
        public List<string> Outputs { get; set; } = new();

        /// <summary>Preview code snippet</summary>
        public string PreviewCode { get; set; } = string.Empty;

        /// <summary>Preview language</summary>
        public string PreviewLanguage { get; set; } = "csharp";

        /// <summary>When to use this template</summary>
        public string UseCase { get; set; } = string.Empty;
    }

    /// <summary>
    /// An input field required by a template.
    /// </summary>
    public class GalleryTemplateInput
    {
        public string Description { get; set; } = string.Empty;
        public string? Example { get; set; }
        public bool IsRequired { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
    }

    /// <summary>
    /// Static catalog of all templates for the gallery.
    /// </summary>
    public static class GalleryTemplates
    {
        /// <summary>All available templates.</summary>
        public static List<GalleryTemplate> All => GetAllTemplates();

        private static List<GalleryTemplate> GetAllTemplates()
        {
            var templates = new List<GalleryTemplate>();
            templates.AddRange(GetEntityTemplates());
            templates.AddRange(GetPagePatternTemplates());
            templates.AddRange(GetPropertySnippetTemplates());
            return templates;
        }

        /// <summary>Get templates by category.</summary>
        public static List<GalleryTemplate> GetByCategory(GalleryTemplateCategory category)
            => All.Where(t => t.Category == category).ToList();

        /// <summary>Get a specific template by ID.</summary>
        public static GalleryTemplate? GetById(string id)
            => All.FirstOrDefault(t => t.Id == id);

        private static List<GalleryTemplate> GetEntityTemplates()
        {
            return new List<GalleryTemplate>
            {
                new GalleryTemplate
                {
                    Id = "entity-efmodel",
                    Name = "EF Model",
                    Description = "Entity Framework model class with data annotations",
                    Icon = "fa-database",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate a C# class for database mapping",
                    Outputs = new() { "{EntityName}Item.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-dataobjects",
                    Name = "DataObjects (DTOs)",
                    Description = "Data transfer objects and filter classes",
                    Icon = "fa-box",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate DTOs for API communication",
                    Outputs = new() { "DataObjects.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-dataaccess",
                    Name = "DataAccess (CRUD)",
                    Description = "Data access layer with Get, Save, Delete methods",
                    Icon = "fa-server",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate repository methods for database operations",
                    Outputs = new() { "DataAccess.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-controller",
                    Name = "API Controller",
                    Description = "REST API endpoints with authentication",
                    Icon = "fa-plug",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Low",
                    UseCase = "Generate web API endpoints",
                    Outputs = new() { "DataController.App.{ProjectName}.cs" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-listpage",
                    Name = "List Page (Razor)",
                    Description = "Paginated list page with search and filters",
                    Icon = "fa-table-list",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate a Blazor page for entity records",
                    Outputs = new() { "{PluralName}Page.App.razor" },
                    IsImplemented = true
                },
                new GalleryTemplate
                {
                    Id = "entity-editpage",
                    Name = "Edit Modal (Razor)",
                    Description = "Create/edit form with validation",
                    Icon = "fa-pen-to-square",
                    Category = GalleryTemplateCategory.Entity,
                    Complexity = "Medium",
                    UseCase = "Generate a modal form for editing records",
                    Outputs = new() { "Edit{EntityName}.App.razor" },
                    IsImplemented = true
                }
            };
        }

        private static List<GalleryTemplate> GetPagePatternTemplates()
        {
            return FMPagePatterns.All.Select(p => new GalleryTemplate
            {
                Id = $"page-{p.Pattern.ToString().ToLower()}",
                Name = p.Name,
                Description = p.Description,
                Icon = p.Icon,
                Category = GalleryTemplateCategory.PagePattern,
                Complexity = p.Complexity,
                UseCase = p.UseCase,
                Outputs = p.GeneratedFiles,
                IsImplemented = p.IsImplemented
            }).ToList();
        }

        private static List<GalleryTemplate> GetPropertySnippetTemplates()
        {
            return PropertySnippets.All.Select(s => new GalleryTemplate
            {
                Id = $"snippet-{s.Id}",
                Name = s.Name,
                Description = s.Description,
                Icon = s.Icon,
                Category = GalleryTemplateCategory.PropertySnippet,
                Complexity = "Low",
                UseCase = $"Quick-add: {string.Join(", ", s.Properties.Select(p => p.Name))}",
                Outputs = s.Properties.Select(p => $"{p.Name} ({p.Type})").ToList(),
                IsImplemented = true
            }).ToList();
        }
    }
}

#endregion

// ---- DataObjects.SmartHelpers ----

#region FreeManager Platform - Smart Helpers & Standard Fields
// ┌─────────────────────────────────────────────────────────────────┐
// │          FreeManager.App.DataObjects.cs: SmartHelpers           │
// │          Property Types, Snippets & Smart Detection              │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROPERTY TYPES                                                  │
// │   PropertyTypeInfo      → Type display info (icon, tile, etc)   │
// │   PropertyTypes         → Static: string, int, bool, DateTime.. │
// │   PropertyTypes.All     → List of all types with metadata       │
// ├─────────────────────────────────────────────────────────────────┤
// │ STANDARD FIELDS (Reusable property groups)                      │
// │   StandardFields.Audit    → CreatedAt, UpdatedAt, ModifiedBy    │
// │   StandardFields.SoftDelete → IsDeleted, DeletedAt, DeletedBy   │
// │   StandardFields.Ownership  → OwnerId, OwnerName, shared flags  │
// │   StandardFields.Common     → Name, Description, Notes, Tags    │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROPERTY SNIPPETS (Quick-add patterns)                          │
// │   PropertySnippets.All   → Common property patterns (Status,    │
// │                           Priority, Email, Phone, Currency...)  │
// ├─────────────────────────────────────────────────────────────────┤
// │ SMART NAME DETECTION                                            │
// │   SmartNameDetection     → Auto-suggest type from property name │
// │                           (Email→string, IsActive→bool, etc)    │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // PROPERTY TYPE DEFINITIONS
    // ============================================================

    /// <summary>
    /// Property type display information.
    /// </summary>
    public class PropertyTypeInfo
    {
        /// <summary>Common default value for this type</summary>
        public string? CommonDefault { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        /// <summary>Background color class for tile</summary>
        public string TileColorClass { get; set; } = "bg-secondary";

        /// <summary>Short label for visual tile picker</summary>
        public string TileLabel { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Supported property types for the wizard.
    /// </summary>
    public static class PropertyTypes
    {
        public const string String = "string";
        public const string Int = "int";
        public const string Long = "long";
        public const string Decimal = "decimal";
        public const string Bool = "bool";
        public const string DateTime = "DateTime";
        public const string DateOnly = "DateOnly";
        public const string Guid = "Guid";
        public const string Enum = "enum";

        /// <summary>All available types for dropdown and visual picker</summary>
        public static readonly List<PropertyTypeInfo> All = new()
        {
            new() { Type = String, DisplayName = "Text (string)", Icon = "fa-font", TileLabel = "Abc", TileColorClass = "bg-success", CommonDefault = "string.Empty" },
            new() { Type = Int, DisplayName = "Integer (int)", Icon = "fa-hashtag", TileLabel = "123", TileColorClass = "bg-info", CommonDefault = "0" },
            new() { Type = Long, DisplayName = "Long Integer (long)", Icon = "fa-hashtag", TileLabel = "123L", TileColorClass = "bg-info", CommonDefault = "0L" },
            new() { Type = Decimal, DisplayName = "Decimal", Icon = "fa-dollar-sign", TileLabel = "$", TileColorClass = "bg-warning", CommonDefault = "0m" },
            new() { Type = Bool, DisplayName = "Yes/No (bool)", Icon = "fa-toggle-on", TileLabel = "✓/✗", TileColorClass = "bg-purple", CommonDefault = "false" },
            new() { Type = DateTime, DisplayName = "Date & Time", Icon = "fa-calendar", TileLabel = "📅", TileColorClass = "bg-primary", CommonDefault = "DateTime.UtcNow" },
            new() { Type = DateOnly, DisplayName = "Date Only", Icon = "fa-calendar-day", TileLabel = "📆", TileColorClass = "bg-primary", CommonDefault = "DateOnly.FromDateTime(DateTime.Today)" },
            new() { Type = Guid, DisplayName = "Unique ID (Guid)", Icon = "fa-fingerprint", TileLabel = "🔑", TileColorClass = "bg-secondary", CommonDefault = "Guid.NewGuid()" },
            new() { Type = Enum, DisplayName = "Enum (choice list)", Icon = "fa-list", TileLabel = "📋", TileColorClass = "bg-danger", CommonDefault = null }
        };

        /// <summary>Get PropertyTypeInfo by type string</summary>
        public static PropertyTypeInfo? Get(string type) => All.FirstOrDefault(t => t.Type == type);
    }

    // ============================================================
    // STANDARD FIELDS (Reusable property groups)
    // ============================================================

    /// <summary>
    /// Standard field groups for common patterns.
    /// Matches FreeManager/FreeCRM naming conventions.
    /// </summary>
    public static class StandardFields
    {
        /// <summary>Audit fields: Added, AddedBy, LastModified, LastModifiedBy (FreeManager convention)</summary>
        public static readonly List<PropertyDefinition> Audit = new()
        {
            new() { Name = "Added", Type = "DateTime", IsRequired = true, DefaultValue = "DateTime.UtcNow", IsSystemField = true, Description = "When created" },
            new() { Name = "AddedBy", Type = "string", IsRequired = false, IsNullable = true, MaxLength = 100, IsSystemField = true, Description = "Who created" },
            new() { Name = "LastModified", Type = "DateTime", IsRequired = true, DefaultValue = "DateTime.UtcNow", IsSystemField = true, Description = "When last modified" },
            new() { Name = "LastModifiedBy", Type = "string", IsRequired = false, IsNullable = true, MaxLength = 100, IsSystemField = true, Description = "Who modified" }
        };

        /// <summary>Soft delete fields: Deleted, DeletedAt</summary>
        public static readonly List<PropertyDefinition> SoftDelete = new()
        {
            new() { Name = "Deleted", Type = "bool", IsRequired = true, DefaultValue = "false", IsSystemField = true, Description = "Soft delete flag" },
            new() { Name = "DeletedAt", Type = "DateTime", IsRequired = false, IsNullable = true, IsSystemField = true, Description = "When deleted" }
        };

        /// <summary>Tenant isolation field</summary>
        public static readonly List<PropertyDefinition> Tenant = new()
        {
            new() { Name = "TenantId", Type = "Guid", IsRequired = true, IsSystemField = true, Description = "Tenant ID" }
        };
    }

    // ============================================================
    // PROPERTY SNIPPETS (Reusable field groups)
    // ============================================================

    /// <summary>
    /// A reusable group of properties that can be inserted at once.
    /// </summary>
    public class PropertySnippet
    {
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public List<PropertyDefinition> Properties { get; set; } = new();
    }

    /// <summary>
    /// Standard property snippets for quick add.
    /// </summary>
    public static class PropertySnippets
    {
        public static readonly PropertySnippet BasicEntity = new()
        {
            Id = "basic-entity",
            Name = "Basic Entity",
            Description = "Name and Description fields",
            Icon = "fa-file-lines",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, Description = "Display name" },
                new() { Name = "Description", Type = "string", IsRequired = false, IsNullable = true, Description = "Optional description" }
            }
        };

        public static readonly PropertySnippet AuditFields = new()
        {
            Id = "audit-fields",
            Name = "Audit Fields",
            Description = "CreatedAt, UpdatedAt, LastModifiedBy",
            Icon = "fa-clock",
            Category = "System",
            Properties = StandardFields.Audit
        };

        public static readonly PropertySnippet SoftDeleteFields = new()
        {
            Id = "soft-delete",
            Name = "Soft Delete",
            Description = "Deleted flag and DeletedAt timestamp",
            Icon = "fa-trash-can",
            Category = "System",
            Properties = StandardFields.SoftDelete
        };

        public static readonly PropertySnippet TenantFields = new()
        {
            Id = "tenant",
            Name = "Tenant ID",
            Description = "Multi-tenant isolation field",
            Icon = "fa-building",
            Category = "System",
            Properties = StandardFields.Tenant
        };

        public static readonly PropertySnippet StatusFields = new()
        {
            Id = "status",
            Name = "Status & Active",
            Description = "IsActive flag and Status enum",
            Icon = "fa-toggle-on",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "IsActive", Type = "bool", IsRequired = true, DefaultValue = "true", Description = "Is active" },
                new() { Name = "Status", Type = "string", IsRequired = false, MaxLength = 50, Description = "Current status" }
            }
        };

        public static readonly PropertySnippet ContactInfo = new()
        {
            Id = "contact-info",
            Name = "Contact Info",
            Description = "Email, Phone, Website fields",
            Icon = "fa-address-card",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Email", Type = "string", IsRequired = false, MaxLength = 255, Description = "Email" },
                new() { Name = "Phone", Type = "string", IsRequired = false, MaxLength = 20, Description = "Phone" },
                new() { Name = "Website", Type = "string", IsRequired = false, MaxLength = 500, Description = "Website" }
            }
        };

        public static readonly PropertySnippet AddressFields = new()
        {
            Id = "address",
            Name = "Address Block",
            Description = "Street, City, State, Zip, Country",
            Icon = "fa-location-dot",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Street", Type = "string", IsRequired = false, MaxLength = 200, Description = "Street" },
                new() { Name = "City", Type = "string", IsRequired = false, MaxLength = 100, Description = "City" },
                new() { Name = "State", Type = "string", IsRequired = false, MaxLength = 50, Description = "State" },
                new() { Name = "ZipCode", Type = "string", IsRequired = false, MaxLength = 20, Description = "Zip" },
                new() { Name = "Country", Type = "string", IsRequired = false, MaxLength = 100, Description = "Country" }
            }
        };

        public static readonly PropertySnippet NotesFields = new()
        {
            Id = "notes",
            Name = "Notes & Comments",
            Description = "Notes and InternalNotes fields",
            Icon = "fa-sticky-note",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Notes", Type = "string", IsRequired = false, IsNullable = true, Description = "Public notes" },
                new() { Name = "InternalNotes", Type = "string", IsRequired = false, IsNullable = true, Description = "Internal notes" }
            }
        };

        /// <summary>All built-in snippets</summary>
        public static readonly List<PropertySnippet> All = new()
        {
            BasicEntity, AuditFields, SoftDeleteFields, TenantFields,
            StatusFields, ContactInfo, AddressFields, NotesFields
        };

        /// <summary>Get snippets by category</summary>
        public static List<PropertySnippet> GetByCategory(string category)
            => All.Where(s => s.Category == category).ToList();
    }

    // ============================================================
    // SMART NAME DETECTION
    // ============================================================

    /// <summary>
    /// A suggestion for property type based on the property name.
    /// </summary>
    public class SmartNameSuggestion
    {
        public float Confidence { get; set; } = 0.5f;
        public string Reason { get; set; } = string.Empty;
        public string? SuggestedDefault { get; set; }
        public string SuggestedType { get; set; } = "string";
        public int? SuggestMaxLength { get; set; }
        public bool SuggestRequired { get; set; } = true;
        public bool SuggestSystemField { get; set; }
    }

    /// <summary>
    /// Smart name pattern detection for property type suggestions.
    /// </summary>
    public static class SmartNameDetection
    {
        /// <summary>
        /// Analyze a property name and suggest type and defaults.
        /// </summary>
        public static SmartNameSuggestion Analyze(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return new SmartNameSuggestion();

            var name = propertyName.Trim();
            var nameLower = name.ToLowerInvariant();

            // DateTime patterns
            if (nameLower.EndsWith("at") || nameLower.EndsWith("date") ||
                nameLower.EndsWith("time") || nameLower.EndsWith("on"))
            {
                var isCreated = nameLower.Contains("created") || nameLower.Contains("added");
                return new SmartNameSuggestion
                {
                    SuggestedType = "DateTime",
                    SuggestedDefault = isCreated ? "DateTime.UtcNow" : null,
                    SuggestSystemField = nameLower.Contains("created") || nameLower.Contains("updated") || nameLower.Contains("deleted"),
                    SuggestRequired = isCreated,
                    Reason = $"'{name}' looks like a timestamp",
                    Confidence = 0.9f
                };
            }

            // Boolean patterns
            if (nameLower.StartsWith("is") || nameLower.StartsWith("has") ||
                nameLower.StartsWith("can") || nameLower.StartsWith("should") ||
                nameLower.StartsWith("enable") || nameLower.StartsWith("allow") ||
                nameLower.EndsWith("enabled") || nameLower.EndsWith("active") ||
                nameLower.EndsWith("deleted") || nameLower.EndsWith("visible"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "bool",
                    SuggestedDefault = nameLower.Contains("active") || nameLower.Contains("enabled") ? "true" : "false",
                    SuggestSystemField = nameLower.Contains("deleted"),
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a boolean flag",
                    Confidence = 0.95f
                };
            }

            // Guid patterns
            if ((nameLower.EndsWith("id") && nameLower != "id" && nameLower.Length > 2) ||
                nameLower.EndsWith("guid") || nameLower.EndsWith("key"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "Guid",
                    SuggestSystemField = nameLower.Contains("tenant"),
                    SuggestRequired = nameLower.Contains("tenant"),
                    Reason = $"'{name}' looks like a unique identifier",
                    Confidence = 0.85f
                };
            }

            // Integer patterns
            if (nameLower.EndsWith("count") || nameLower.EndsWith("number") ||
                nameLower.EndsWith("quantity") || nameLower.EndsWith("index") ||
                nameLower.EndsWith("order") || nameLower.EndsWith("rank") ||
                nameLower.EndsWith("level") || nameLower.EndsWith("age") ||
                nameLower.Contains("sortorder") || nameLower.Contains("displayorder"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "int",
                    SuggestedDefault = "0",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a numeric count",
                    Confidence = 0.8f
                };
            }

            // Decimal patterns
            if (nameLower.EndsWith("amount") || nameLower.EndsWith("price") ||
                nameLower.EndsWith("cost") || nameLower.EndsWith("total") ||
                nameLower.EndsWith("balance") || nameLower.EndsWith("rate") ||
                nameLower.EndsWith("percentage") || nameLower.EndsWith("fee"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "decimal",
                    SuggestedDefault = "0m",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a monetary value",
                    Confidence = 0.85f
                };
            }

            // Enum patterns
            if (nameLower.EndsWith("status") || nameLower.EndsWith("state") ||
                nameLower.EndsWith("type") || nameLower.EndsWith("category") ||
                nameLower.EndsWith("priority") || nameLower.EndsWith("role") ||
                nameLower.EndsWith("kind"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "enum",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a choice list (enum)",
                    Confidence = 0.75f
                };
            }

            // Email pattern
            if (nameLower.Contains("email"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 255,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like an email",
                    Confidence = 0.9f
                };
            }

            // URL patterns
            if (nameLower.Contains("url") || nameLower.Contains("website") || nameLower.Contains("link"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 2000,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like a URL",
                    Confidence = 0.85f
                };
            }

            // Phone pattern
            if (nameLower.Contains("phone") || nameLower.Contains("fax") || nameLower.Contains("mobile"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 20,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like a phone number",
                    Confidence = 0.85f
                };
            }

            // Name/Title patterns
            if (nameLower == "name" || nameLower == "title" || nameLower.EndsWith("name") || nameLower.EndsWith("title"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 200,
                    SuggestRequired = nameLower == "name" || nameLower == "title",
                    Reason = $"'{name}' looks like a name field",
                    Confidence = 0.8f
                };
            }

            // Description/Notes
            if (nameLower.Contains("description") || nameLower.Contains("notes") ||
                nameLower.Contains("comment") || nameLower.Contains("content") ||
                nameLower.Contains("body") || nameLower.Contains("text"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = null,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like text content",
                    Confidence = 0.7f
                };
            }

            // Default
            return new SmartNameSuggestion
            {
                SuggestedType = "string",
                SuggestMaxLength = 200,
                SuggestRequired = false,
                Reason = "Default to text field",
                Confidence = 0.3f
            };
        }
    }
}

#endregion

// ---- Endpoints ----

#region FreeManager Platform - API Endpoint Constants
// ┌─────────────────────────────────────────────────────────────────┐
// │            FreeManager.App.DataObjects.cs: Endpoints            │
// │                    API Endpoint Constants                        │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROJECT ENDPOINTS (6)                                           │
// │   GetProjects, GetProject, CreateProject                        │
// │   UpdateProject, DeleteProject, ExportProject                   │
// ├─────────────────────────────────────────────────────────────────┤
// │ FILE ENDPOINTS (7)                                              │
// │   GetAppFiles, GetAppFile, SaveAppFile, CreateAppFile           │
// │   DeleteAppFile, GetFileVersions, GetFileVersion                │
// ├─────────────────────────────────────────────────────────────────┤
// │ BUILD ENDPOINTS (4)                                             │
// │   StartBuild, GetBuilds, GetBuild, DownloadArtifact             │
// ├─────────────────────────────────────────────────────────────────┤
// │ ENTITY WIZARD ENDPOINTS (6)                                     │
// │   GenerateEntityCode, GetSavedProjects, GetSavedProject         │
// │   SaveWizardProject, DeleteSavedProject, DownloadProjectZip     │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    public static partial class Endpoints
    {
        /// <summary>
        /// FreeManager API endpoints for project, file, and build management.
        /// </summary>
        public static class FreeManager
        {
            // ============================================================
            // PROJECT ENDPOINTS
            // ============================================================

            /// <summary>Get all projects for tenant</summary>
            public const string GetProjects = "api/Data/FM_GetProjects";

            /// <summary>Get single project by ID</summary>
            public const string GetProject = "api/Data/FM_GetProject";

            /// <summary>Create new project</summary>
            public const string CreateProject = "api/Data/FM_CreateProject";

            /// <summary>Update project metadata</summary>
            public const string UpdateProject = "api/Data/FM_UpdateProject";

            /// <summary>Soft-delete project</summary>
            public const string DeleteProject = "api/Data/FM_DeleteProject";

            /// <summary>Export project as ZIP</summary>
            public const string ExportProject = "api/Data/FM_ExportProject";

            // ============================================================
            // FILE ENDPOINTS
            // ============================================================

            /// <summary>Get all files for project</summary>
            public const string GetAppFiles = "api/Data/FM_GetAppFiles";

            /// <summary>Get single file content</summary>
            public const string GetAppFile = "api/Data/FM_GetAppFile";

            /// <summary>Save file content (creates new version)</summary>
            public const string SaveAppFile = "api/Data/FM_SaveAppFile";

            /// <summary>Create new file in project</summary>
            public const string CreateAppFile = "api/Data/FM_CreateAppFile";

            /// <summary>Soft-delete file</summary>
            public const string DeleteAppFile = "api/Data/FM_DeleteAppFile";

            /// <summary>Get version history for file</summary>
            public const string GetFileVersions = "api/Data/FM_GetFileVersions";

            /// <summary>Get specific version content</summary>
            public const string GetFileVersion = "api/Data/FM_GetFileVersion";

            // ============================================================
            // BUILD ENDPOINTS (future use)
            // ============================================================

            /// <summary>Start new build</summary>
            public const string StartBuild = "api/Data/FM_StartBuild";

            /// <summary>Get all builds for project</summary>
            public const string GetBuilds = "api/Data/FM_GetBuilds";

            /// <summary>Get build details</summary>
            public const string GetBuild = "api/Data/FM_GetBuild";

            /// <summary>Download build artifact</summary>
            public const string DownloadArtifact = "api/Data/FM_DownloadArtifact";

            // ============================================================
            // ENTITY WIZARD ENDPOINTS
            // ============================================================

            /// <summary>Generate entity code from wizard state</summary>
            public const string GenerateEntityCode = "api/Data/FM_GenerateEntityCode";

            /// <summary>Get saved wizard projects (paginated)</summary>
            public const string GetSavedProjects = "api/Data/FM_GetSavedProjects";

            /// <summary>Get single wizard project with state JSON</summary>
            public const string GetSavedProject = "api/Data/FM_GetSavedProject";

            /// <summary>Save wizard project state</summary>
            public const string SaveWizardProject = "api/Data/FM_SaveWizardProject";

            /// <summary>Delete wizard project</summary>
            public const string DeleteSavedProject = "api/Data/FM_DeleteSavedProject";

            /// <summary>Download project as ZIP with generated files</summary>
            public const string DownloadProjectZip = "api/Data/FM_DownloadProjectZip";
        }
    }
}

#endregion
