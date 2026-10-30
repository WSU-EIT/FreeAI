using System.Text;

namespace FreeManager;

// ---- WizardTemplates ----
// FreeManager.App.WizardTemplates.cs - Code generation templates for Setup Wizards
// Generates all files needed for a multi-step onboarding/setup wizard

#region WizardTemplates - Setup Wizard Template Coordinator
// ============================================================================
// FREEMANAGER SETUP WIZARD TEMPLATES
// This is the coordinator file for setup wizard template generation.
// Template generators are split into partial files:
//
// - WizardTemplates.Services section    → State DTO, Service, Validator
// - WizardTemplates.Components section  → Container, Step, Complete components
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

// ---- WizardTemplates.Components ----

#region WizardTemplates - Component Templates
// ============================================================================
// WIZARD TEMPLATES - BLAZOR COMPONENTS
// Container, Step, and Complete component generators.
// Part of: WizardTemplates.App (partial)
// ============================================================================

public static partial class WizardTemplates
{
    /// <summary>
    /// Generate the Complete component.
    /// </summary>
    public static string GenerateCompleteComponent(DataObjects.SetupWizardDefinition wizard)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;

        sb.AppendLine($"@* {name}Complete.razor - Wizard completion screen *@");
        sb.AppendLine();
        sb.AppendLine("<div class=\"wizard-complete text-center py-5\">");
        sb.AppendLine("    <div class=\"mb-4\"><i class=\"fa-solid fa-circle-check fa-5x text-success\"></i></div>");
        sb.AppendLine("    <h3 class=\"mb-3\">Setup Complete!</h3>");
        sb.AppendLine($"    <p class=\"text-muted mb-4\">{wizard.Description}</p>");
        sb.AppendLine();
        sb.AppendLine("    @if (State.SkippedSteps.Any()) {");
        sb.AppendLine("        <div class=\"alert alert-warning mb-4\">");
        sb.AppendLine("            <i class=\"fa-solid fa-exclamation-triangle me-2\"></i>You skipped @State.SkippedSteps.Count step(s).");
        sb.AppendLine("        </div>");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    <div class=\"d-flex justify-content-center gap-2\">");
        sb.AppendLine("        <button class=\"btn btn-outline-secondary\" @onclick=\"OnStartOver\"><i class=\"fa-solid fa-rotate-left me-1\"></i>Start Over</button>");
        sb.AppendLine("        <a href=\"/\" class=\"btn btn-primary\"><i class=\"fa-solid fa-home me-1\"></i>Go to Dashboard</a>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("@code {");
        sb.AppendLine($"    [Parameter] public DataObjects.{name}State State {{ get; set; }} = new();");
        sb.AppendLine("    [Parameter] public EventCallback OnStartOver { get; set; }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    /// <summary>
    /// Generate the main Container component with stepper.
    /// </summary>
    public static string GenerateContainer(DataObjects.SetupWizardDefinition wizard)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;
        var stepCount = wizard.Steps.Count;

        sb.AppendLine($"@* {name}Container.razor - Main wizard container with stepper *@");
        sb.AppendLine($"@* Auto-generated by FreeManager Setup Wizard Builder *@");
        sb.AppendLine();
        sb.AppendLine("@inject BlazorDataModel Model");
        sb.AppendLine();
        sb.AppendLine("<div class=\"wizard-container\">");
        sb.AppendLine("    <div class=\"wizard-stepper d-flex justify-content-between mb-4\">");

        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            sb.AppendLine($"        <div class=\"step @(CurrentStep == {step.Order} ? \"active\" : \"\") @(CurrentStep > {step.Order} ? \"completed\" : \"\")\">");
            sb.AppendLine($"            <div class=\"step-circle\">{step.Order + 1}</div>");
            sb.AppendLine($"            <div class=\"step-label\">{step.ShortName ?? step.Name}</div>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <div class=\"wizard-content\">");
        sb.AppendLine("        @switch (CurrentStep) {");

        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            sb.AppendLine($"            case {step.Order}:");
            sb.AppendLine($"                <{name}Step{step.Order + 1} State=\"State\" OnNext=\"NextStep\" OnBack=\"PrevStep\"");
            if (step.IsOptional && wizard.AllowSkip)
            {
                sb.AppendLine($"                    OnSkip=\"SkipStep\" AllowSkip=\"true\" />");
            }
            else
            {
                sb.AppendLine($"                    />");
            }
            sb.AppendLine("                break;");
        }

        sb.AppendLine($"            case {stepCount}:");
        sb.AppendLine($"                <{name}Complete State=\"State\" OnStartOver=\"StartOver\" />");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("@code {");
        sb.AppendLine($"    [Parameter] public DataObjects.{name}State State {{ get; set; }} = new();");
        sb.AppendLine("    [Parameter] public EventCallback OnCompleted { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    private int CurrentStep { get; set; } = 0;");
        sb.AppendLine();
        sb.AppendLine("    private void NextStep() {");
        sb.AppendLine($"        if (CurrentStep < {stepCount}) {{");
        sb.AppendLine("            CurrentStep++;");
        sb.AppendLine("            State.CurrentStep = CurrentStep;");
        sb.AppendLine($"            if (CurrentStep == {stepCount}) {{");
        sb.AppendLine("                State.IsCompleted = true;");
        sb.AppendLine("                State.CompletedAt = DateTime.UtcNow;");
        sb.AppendLine("                OnCompleted.InvokeAsync();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private void PrevStep() {");
        sb.AppendLine("        if (CurrentStep > 0) { CurrentStep--; State.CurrentStep = CurrentStep; }");
        sb.AppendLine("    }");

        if (wizard.AllowSkip)
        {
            sb.AppendLine();
            sb.AppendLine("    private void SkipStep() { State.SkippedSteps.Add(CurrentStep); NextStep(); }");
        }

        sb.AppendLine();
        sb.AppendLine("    private void StartOver() {");
        sb.AppendLine("        CurrentStep = 0;");
        sb.AppendLine("        State.CurrentStep = 0;");
        sb.AppendLine("        State.IsCompleted = false;");
        sb.AppendLine("        State.CompletedAt = null;");
        sb.AppendLine("        State.SkippedSteps.Clear();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generate a step component.
    /// </summary>
    public static string GenerateStepComponent(DataObjects.SetupWizardDefinition wizard, DataObjects.SetupWizardStepDefinition step)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;
        var stepNum = step.Order + 1;

        sb.AppendLine($"@* {name}Step{stepNum}.razor - {step.Name} *@");
        sb.AppendLine();
        sb.AppendLine("<div class=\"wizard-step\">");
        sb.AppendLine($"    <h4 class=\"mb-3\"><i class=\"fa-solid {step.Icon} me-2\"></i>{step.Name}</h4>");

        if (!string.IsNullOrWhiteSpace(step.Description))
        {
            sb.AppendLine($"    <p class=\"text-muted mb-4\">{step.Description}</p>");
        }

        sb.AppendLine("    <div class=\"step-fields\">");

        foreach (var field in step.Fields)
        {
            sb.AppendLine($"        <div class=\"mb-3\">");
            sb.AppendLine($"            <label class=\"form-label\">{ToDisplayName(field.Name)}{(field.IsRequired ? " <span class=\"text-danger\">*</span>" : "")}</label>");

            switch (field.Type.ToLower())
            {
                case "bool":
                    sb.AppendLine($"            <div class=\"form-check\"><input type=\"checkbox\" class=\"form-check-input\" @bind=\"State.Step{stepNum}.{field.Name}\" /></div>");
                    break;
                case "datetime":
                    sb.AppendLine($"            <input type=\"datetime-local\" class=\"form-control\" @bind=\"State.Step{stepNum}.{field.Name}\" />");
                    break;
                case "int":
                    sb.AppendLine($"            <input type=\"number\" class=\"form-control\" @bind=\"State.Step{stepNum}.{field.Name}\" />");
                    break;
                default:
                    sb.AppendLine($"            <input type=\"text\" class=\"form-control\" @bind=\"State.Step{stepNum}.{field.Name}\" />");
                    break;
            }

            sb.AppendLine($"        </div>");
        }

        if (!step.Fields.Any())
        {
            sb.AppendLine("        <p class=\"text-muted\">Configure fields for this step in the wizard builder.</p>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine();
        sb.AppendLine("    <div class=\"d-flex justify-content-between mt-4\">");
        sb.AppendLine("        <button class=\"btn btn-secondary\" @onclick=\"OnBack\" disabled=\"@(StepNumber == 1)\"><i class=\"fa-solid fa-arrow-left me-1\"></i>Back</button>");
        sb.AppendLine("        <div>");

        if (step.IsOptional && wizard.AllowSkip)
        {
            sb.AppendLine("            @if (AllowSkip) { <button class=\"btn btn-outline-secondary me-2\" @onclick=\"OnSkip\">Skip</button> }");
        }

        sb.AppendLine("            <button class=\"btn btn-primary\" @onclick=\"OnNext\" disabled=\"@(!IsValid)\">Next<i class=\"fa-solid fa-arrow-right ms-1\"></i></button>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("@code {");
        sb.AppendLine($"    [Parameter] public DataObjects.{name}State State {{ get; set; }} = new();");
        sb.AppendLine("    [Parameter] public EventCallback OnNext { get; set; }");
        sb.AppendLine("    [Parameter] public EventCallback OnBack { get; set; }");

        if (step.IsOptional && wizard.AllowSkip)
        {
            sb.AppendLine("    [Parameter] public EventCallback OnSkip { get; set; }");
            sb.AppendLine("    [Parameter] public bool AllowSkip { get; set; }");
        }

        sb.AppendLine();
        sb.AppendLine($"    private int StepNumber => {stepNum};");
        sb.AppendLine("    private bool IsValid => true; // Add validation logic");
        sb.AppendLine("}");

        return sb.ToString();
    }
}

#endregion

// ---- WizardTemplates.Services ----

#region WizardTemplates - Service/State Templates
// ============================================================================
// WIZARD TEMPLATES - STATE & SERVICE
// State DTO and Service class generators.
// Part of: WizardTemplates.App (partial)
// ============================================================================

public static partial class WizardTemplates
{
    /// <summary>
    /// Generate the Service for state persistence.
    /// </summary>
    public static string GenerateService(DataObjects.SetupWizardDefinition wizard)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;

        sb.AppendLine("// Auto-generated by FreeManager Setup Wizard Builder");
        sb.AppendLine();
        sb.AppendLine("namespace Services;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Service for managing {name} wizard state.</summary>");
        sb.AppendLine($"public class {name}Service");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IHttpClientFactory _httpClientFactory;");
        sb.AppendLine();
        sb.AppendLine($"    public {name}Service(IHttpClientFactory httpClientFactory)");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClientFactory = httpClientFactory;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<DataObjects.{name}State> GetStateAsync(Guid userId)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: Load from database or cache");
        sb.AppendLine($"        return new DataObjects.{name}State {{ UserId = userId }};");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task SaveStateAsync(DataObjects.{name}State state)");
        sb.AppendLine("    {");
        sb.AppendLine("        // TODO: Persist to database");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (wizard.TrackCompletion)
        {
            sb.AppendLine($"    public async Task MarkCompletedAsync(Guid userId)");
            sb.AppendLine("    {");
            sb.AppendLine("        // TODO: Record completion in database");
            sb.AppendLine("        await Task.CompletedTask;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
    /// <summary>
    /// Generate the State DTO containing all wizard data.
    /// </summary>
    public static string GenerateStateDto(DataObjects.SetupWizardDefinition wizard)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;

        sb.AppendLine("// Auto-generated by FreeManager Setup Wizard Builder");
        sb.AppendLine("// Do not modify manually - regenerate instead");
        sb.AppendLine();
        sb.AppendLine("namespace DataObjects;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// State object for {name} wizard.");
        sb.AppendLine($"/// {wizard.Description}");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class {name}State");
        sb.AppendLine("{");
        sb.AppendLine("    public Guid UserId { get; set; }");
        sb.AppendLine("    public int CurrentStep { get; set; }");
        sb.AppendLine("    public bool IsCompleted { get; set; }");
        sb.AppendLine("    public DateTime StartedAt { get; set; } = DateTime.UtcNow;");
        sb.AppendLine("    public DateTime? CompletedAt { get; set; }");
        sb.AppendLine("    public List<int> SkippedSteps { get; set; } = new();");
        sb.AppendLine();

        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            var stepClassName = $"Step{step.Order + 1}Data";
            sb.AppendLine($"    public {stepClassName} {stepClassName.Replace("Data", "")} {{ get; set; }} = new();");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            var stepClassName = $"Step{step.Order + 1}Data";
            sb.AppendLine($"/// <summary>Data for step {step.Order + 1}: {step.Name}</summary>");
            sb.AppendLine($"public class {stepClassName}");
            sb.AppendLine("{");

            foreach (var field in step.Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.Description))
                {
                    sb.AppendLine($"    /// <summary>{field.Description}</summary>");
                }
                var nullableMarker = !field.IsRequired && field.Type != "string" ? "?" : "";
                var defaultValue = GetDefaultValue(field);
                sb.AppendLine($"    public {field.Type}{nullableMarker} {field.Name} {{ get; set; }}{defaultValue}");
            }

            if (!step.Fields.Any())
            {
                sb.AppendLine("    // Add properties for this step");
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate the Validator using FluentValidation.
    /// </summary>
    public static string GenerateValidator(DataObjects.SetupWizardDefinition wizard)
    {
        var sb = new StringBuilder();
        var name = wizard.Name;

        sb.AppendLine("// Auto-generated by FreeManager Setup Wizard Builder");
        sb.AppendLine();
        sb.AppendLine("using FluentValidation;");
        sb.AppendLine();
        sb.AppendLine("namespace Validators;");
        sb.AppendLine();
        sb.AppendLine($"public class {name}StateValidator : AbstractValidator<DataObjects.{name}State>");
        sb.AppendLine("{");
        sb.AppendLine($"    public {name}StateValidator()");
        sb.AppendLine("    {");

        foreach (var step in wizard.Steps.OrderBy(s => s.Order))
        {
            var stepNum = step.Order + 1;
            foreach (var field in step.Fields.Where(f => f.IsRequired))
            {
                if (field.Type.ToLower() == "string")
                {
                    sb.AppendLine($"        RuleFor(x => x.Step{stepNum}.{field.Name})");
                    sb.AppendLine($"            .NotEmpty().WithMessage(\"{ToDisplayName(field.Name)} is required\");");
                }
                else
                {
                    sb.AppendLine($"        RuleFor(x => x.Step{stepNum}.{field.Name})");
                    sb.AppendLine($"            .NotNull().WithMessage(\"{ToDisplayName(field.Name)} is required\");");
                }
            }
        }

        if (!wizard.Steps.SelectMany(s => s.Fields).Any(f => f.IsRequired))
        {
            sb.AppendLine("        // Add validation rules for required fields");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static string GetDefaultValue(DataObjects.PropertyDefinition field)
    {
        if (!string.IsNullOrWhiteSpace(field.DefaultValue))
        {
            return $" = {field.DefaultValue};";
        }

        return field.Type switch
        {
            "string" => " = string.Empty;",
            "bool" => " = false;",
            "int" => " = 0;",
            _ => ""
        };
    }

    private static string ToDisplayName(string name)
    {
        var result = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(c);
        }
        return result.ToString();
    }
}

#endregion
