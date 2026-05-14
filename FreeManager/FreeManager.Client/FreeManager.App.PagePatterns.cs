namespace FreeManager.Client;

#region PagePatterns - Coordinator
// ============================================================================
// PAGE PATTERN TEMPLATES - Router for page pattern code generation.
// ============================================================================

public static partial class PagePatterns
{
    public static List<(string FileName, string FileType, string Content)> GetPatternFiles(
        DataObjects.FMPagePattern pattern, string projectName, DataObjects.EntityDefinition? entity = null)
    {
        return pattern switch
        {
            DataObjects.FMPagePattern.QuickStartWizard => GetQuickStartWizardFiles(projectName, entity),
            DataObjects.FMPagePattern.SplitPanelMasterDetail => GetSplitPanelMasterDetailFiles(projectName, entity),
            DataObjects.FMPagePattern.KanbanBoard => GetKanbanBoardFiles(projectName, entity),
            DataObjects.FMPagePattern.TimelineActivityFeed => GetTimelineActivityFeedFiles(projectName, entity),
            DataObjects.FMPagePattern.SettingsHub => GetSettingsHubFiles(projectName, entity),
            DataObjects.FMPagePattern.DataImportWizard => GetDataImportWizardFiles(projectName, entity),
            DataObjects.FMPagePattern.ComparisonView => GetComparisonViewFiles(projectName, entity),
            DataObjects.FMPagePattern.CustomizableDashboard => GetCustomizableDashboardFiles(projectName, entity),
            DataObjects.FMPagePattern.ApprovalWorkflow => GetApprovalWorkflowFiles(projectName, entity),
            DataObjects.FMPagePattern.ApiDocumentation => GetApiDocumentationFiles(projectName, entity),
            _ => new()
        };
    }
}
#endregion
