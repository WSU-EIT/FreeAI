// WizardTemplates.App.cs - Code generation templates for Setup Wizards
// Generates all files needed for a multi-step onboarding/setup wizard

namespace FreeManager;

#region WizardTemplates - Setup Wizard Template Coordinator
// ============================================================================
// FREEMANAGER SETUP WIZARD TEMPLATES
// This is the coordinator file for setup wizard template generation.
// Template generators are split into partial files:
//
// - WizardTemplates.App.Services.cs    → State DTO, Service, Validator
// - WizardTemplates.App.Components.cs  → Container, Step, Complete components
//
// This file contains the orchestrator.
// ============================================================================

/// <summary>
/// Code generation templates for Setup Wizards.
/// Generates State DTO, Service, Container, Step components, and Validator.
/// </summary>
public static partial class WizardTemplates
{
    /// <summary>
    /// Generate all files for a setup wizard.
    /// </summary>
    public static List<DataObjects.GeneratedFileInfo> GenerateAllFiles(DataObjects.SetupWizardDefinition wizard)
    {
        var files = new List<DataObjects.GeneratedFileInfo>();
        var name = wizard.Name;

        // 1. State DTO
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{name}State.cs",
            FileType = "State DTO",
            Content = GenerateStateDto(wizard)
        });

        // 2. Service
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{name}Service.cs",
            FileType = "Service",
            Content = GenerateService(wizard)
        });

        // 3. Container Component
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{name}Container.razor",
            FileType = "Blazor Component",
            Content = GenerateContainer(wizard)
        });

        // 4. Step Components
        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            files.Add(new DataObjects.GeneratedFileInfo
            {
                FileName = $"{name}Step{step.Order + 1}.razor",
                FileType = "Blazor Component",
                Content = GenerateStepComponent(wizard, step)
            });
        }

        // 5. Complete Component
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{name}Complete.razor",
            FileType = "Blazor Component",
            Content = GenerateCompleteComponent(wizard)
        });

        // 6. Validator
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{name}Validator.cs",
            FileType = "Validator",
            Content = GenerateValidator(wizard)
        });

        return files;
    }
}

#endregion
