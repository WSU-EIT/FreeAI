namespace FreeManager.Client;

#region ProjectTemplates - Project Template Coordinator
// ============================================================================
// FREEMANAGER PROJECT TEMPLATES
// This is the coordinator file for project template generation.
// Template generators are split into partial files:
//
// - ProjectTemplates.App.Skeleton.cs  → Basic structure with placeholders
// - ProjectTemplates.App.Starter.cs   → Working example using Settings storage
// - ProjectTemplates.App.FullCrud.cs  → Full CRUD with EF entities
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

    /// <summary>
    /// Gets the files to create for a given template.
    /// Routes to appropriate template generator in partial files.
    /// </summary>
    public static List<(string FileName, string FileType, string Content)> GetTemplateFiles(
        DataObjects.FMProjectTemplate template,
        string projectName)
    {
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
}

#endregion
