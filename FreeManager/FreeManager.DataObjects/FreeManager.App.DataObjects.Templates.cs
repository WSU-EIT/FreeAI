namespace FreeManager;

#region FreeManager Platform - Project & Page Templates
// ┌─────────────────────────────────────────────────────────────────┐
// │         DataObjects.App.FreeManager.Templates.cs                │
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
        public FMProjectTemplate Template { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public List<string> IncludedFiles { get; set; } = new();
        public bool IsRecommended { get; set; }
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
        public FMPagePattern Pattern { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string UseCase { get; set; } = string.Empty;
        public string Complexity { get; set; } = "Medium";
        public List<string> KeyFeatures { get; set; } = new();
        public List<string> GeneratedFiles { get; set; } = new();
        public bool RequiresEntity { get; set; } = true;
        public int Priority { get; set; } = 5;

        /// <summary>
        /// True if this pattern has a working code generator.
        /// False patterns show a "Coming Soon" warning.
        /// </summary>
        public bool IsImplemented { get; set; } = false;
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

        /// <summary>Get patterns that require an entity definition.</summary>
        public static List<FMPagePatternInfo> GetEntityPatterns()
            => All.Where(p => p.RequiresEntity).ToList();

        /// <summary>Get patterns sorted by priority (recommended first).</summary>
        public static List<FMPagePatternInfo> GetByPriority()
            => All.OrderBy(p => p.Priority).ThenBy(p => p.Name).ToList();

        /// <summary>Get only implemented patterns.</summary>
        public static List<FMPagePatternInfo> GetImplemented()
            => All.Where(p => p.IsImplemented).ToList();
    }
}

#endregion
