using System.Text.RegularExpressions;
using Spectre.Console;

namespace FreeManager.Cli;

#region CLI Menu Service
// ============================================================================
// FREEMANAGER CLI - MENU SERVICE
// Interactive menu system using Spectre.Console for beautiful console output.
// Supports both simple Project Templates and full Application Templates.
// ============================================================================

/// <summary>
/// Interactive menu service for CLI.
/// </summary>
public static class MenuService
{
    /// <summary>
    /// Show the main menu and handle user selections.
    /// </summary>
    public static async Task ShowMainMenu()
    {
        while (true)
        {
            Console.Clear();
            ShowHeader();
            ShowMainMenuOptions();

            Console.Write("Select option: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine();

            switch (char.ToUpper(key.KeyChar))
            {
                case '1':
                    await CreateProjectWizard();
                    break;
                case '2':
                    await CreateApplicationWizard();
                    break;
                case '3':
                    ShowTemplateList();
                    break;
                case '4':
                    ShowHelp();
                    break;
                case '0':
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid option. Please try again.");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                    break;
            }
        }
    }

    /// <summary>
    /// Show the application header with box drawing.
    /// </summary>
    private static void ShowHeader()
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           FreeManager - FreeCRM Project Generator            ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    /// <summary>
    /// Show main menu options.
    /// </summary>
    private static void ShowMainMenuOptions()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                          MAIN MENU                            ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  1. Create Simple Project (Empty, Skeleton, Starter, FullCrud)");
        Console.WriteLine("  2. Create Application (FreeBase, FreeTracker, FreeAudit)");
        Console.WriteLine("  3. List Available Templates");
        Console.WriteLine("  4. Help / Command Line Usage");
        Console.WriteLine();
        Console.WriteLine("  0. Exit");
        Console.WriteLine();
    }

    /// <summary>
    /// Interactive wizard to create a new simple project.
    /// </summary>
    private static async Task CreateProjectWizard()
    {
        Console.Clear();
        ShowHeader();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("              CREATE SIMPLE PROJECT FROM TEMPLATE              ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Step 1: Select Template
        var templates = CliProjectTemplates.GetTemplates();

        Console.WriteLine("  STEP 1: Choose a template");
        Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
        for (int i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            var recommended = t.IsRecommended ? " [RECOMMENDED]" : "";
            Console.WriteLine($"  │  {i + 1}. {t.Name}{recommended}");
            Console.WriteLine($"  │     {t.Description}");
            Console.WriteLine($"  │     Files: {t.FileCount}");
            if (i < templates.Count - 1) Console.WriteLine("  │");
        }
        Console.WriteLine("  └────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.Write("  Select template (1-4): ");
        if (!int.TryParse(Console.ReadLine(), out int templateChoice) || templateChoice < 1 || templateChoice > templates.Count)
        {
            ShowError("Invalid template selection.");
            return;
        }

        var selectedTemplate = templates[templateChoice - 1];
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Selected: {selectedTemplate.Name}");
        Console.ResetColor();
        Console.WriteLine();

        // Step 2: Enter Project Name
        Console.WriteLine("  STEP 2: Enter project name");
        Console.WriteLine("          (Must be valid C# identifier: start with letter, letters/numbers only)");
        Console.Write("  Project name: ");
        var projectName = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(projectName) || !Regex.IsMatch(projectName, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            ShowError("Invalid project name. Must start with a letter and contain only letters and numbers.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Project name: {projectName}");
        Console.ResetColor();
        Console.WriteLine();

        // Step 3: Output Directory
        var defaultOutput = Path.Combine(Environment.CurrentDirectory, projectName);
        Console.WriteLine("  STEP 3: Output directory");
        Console.WriteLine($"          Default: {defaultOutput}");
        Console.Write("  Output directory (press Enter for default): ");
        var outputInput = Console.ReadLine()?.Trim();
        var outputPath = string.IsNullOrWhiteSpace(outputInput) ? defaultOutput : outputInput;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Output: {outputPath}");
        Console.ResetColor();
        Console.WriteLine();

        // Preview
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                         PREVIEW                               ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        var files = CliProjectTemplates.GetTemplateFiles(selectedTemplate.Template, projectName);
        Console.WriteLine($"  Template: {selectedTemplate.Name}");
        Console.WriteLine($"  Project:  {projectName}");
        Console.WriteLine($"  Output:   {outputPath}");
        Console.WriteLine($"  Files to create: {files.Count}");
        Console.WriteLine();

        if (files.Count > 0)
        {
            Console.WriteLine("  ┌─────────────────────────────────────────────┬──────────────┐");
            Console.WriteLine("  │ File Name                                   │ Type         │");
            Console.WriteLine("  ├─────────────────────────────────────────────┼──────────────┤");
            foreach (var file in files)
            {
                var fileName = file.FileName.Length > 43 ? file.FileName[..40] + "..." : file.FileName;
                var fileType = file.FileType.Length > 12 ? file.FileType[..12] : file.FileType;
                Console.WriteLine($"  │ {fileName.PadRight(43)} │ {fileType.PadRight(12)} │");
            }
            Console.WriteLine("  └─────────────────────────────────────────────┴──────────────┘");
        }

        Console.WriteLine();
        Console.Write("  Create this project? (Y/N): ");
        var confirm = Console.ReadKey();
        Console.WriteLine();

        if (char.ToUpper(confirm.KeyChar) != 'Y')
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Project creation cancelled.");
            Console.ResetColor();
            WaitForKey();
            return;
        }

        await GenerateProject(selectedTemplate.Template, projectName, outputPath);
        WaitForKey();
    }

    /// <summary>
    /// Interactive wizard to create an application from Application Templates.
    /// </summary>
    private static async Task CreateApplicationWizard()
    {
        Console.Clear();
        ShowHeader();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("           CREATE APPLICATION FROM TEMPLATE                    ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Step 1: Select Application Template
        var appTemplates = CliProjectTemplates.GetApplicationTemplates();

        Console.WriteLine("  STEP 1: Choose an application template");
        Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
        for (int i = 0; i < appTemplates.Count; i++)
        {
            var t = appTemplates[i];
            var color = t.Template == CliApplicationTemplate.FreeAudit ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.Write("  │  ");
            Console.ForegroundColor = color;
            Console.Write($"{i + 1}. {t.Name}");
            Console.ResetColor();
            Console.WriteLine($" [{t.Difficulty}]");
            Console.WriteLine($"  │     {t.Description}");
            Console.WriteLine($"  │     Entities: {t.EntityCount}");
            Console.Write("  │     Features: ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(string.Join(", ", t.Features.Take(3)) + (t.Features.Count > 3 ? "..." : ""));
            Console.ResetColor();
            if (i < appTemplates.Count - 1) Console.WriteLine("  │");
        }
        Console.WriteLine("  └────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        Console.Write("  Select template (1-3): ");
        if (!int.TryParse(Console.ReadLine(), out int templateChoice) || templateChoice < 1 || templateChoice > appTemplates.Count)
        {
            ShowError("Invalid template selection.");
            return;
        }

        var selectedAppTemplate = appTemplates[templateChoice - 1];
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Selected: {selectedAppTemplate.Name}");
        Console.ResetColor();
        Console.WriteLine();

        // Step 2: Enter Application Name
        Console.WriteLine("  STEP 2: Enter application name");
        Console.WriteLine("          Example: FreeGLBA, AssetTracker, InventoryApp");
        Console.Write("  Application name: ");
        var projectName = Console.ReadLine()?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(projectName) || !Regex.IsMatch(projectName, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            ShowError("Invalid name. Must start with a letter and contain only letters and numbers.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Application name: {projectName}");
        Console.ResetColor();
        Console.WriteLine();

        // Step 3: Output Directory
        var defaultOutput = Path.Combine(Environment.CurrentDirectory, projectName);
        Console.WriteLine("  STEP 3: Output directory");
        Console.WriteLine($"          Default: {defaultOutput}");
        Console.Write("  Output directory (press Enter for default): ");
        var outputInput = Console.ReadLine()?.Trim();
        var outputPath = string.IsNullOrWhiteSpace(outputInput) ? defaultOutput : outputInput;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Output: {outputPath}");
        Console.ResetColor();
        Console.WriteLine();

        // Preview
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                         PREVIEW                               ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  Template:    {selectedAppTemplate.Name}");
        Console.WriteLine($"  Application: {projectName}");
        Console.WriteLine($"  Output:      {outputPath}");
        Console.WriteLine($"  Difficulty:  {selectedAppTemplate.Difficulty}");
        Console.WriteLine($"  Entities:    {selectedAppTemplate.EntityCount}");
        Console.WriteLine();
        Console.WriteLine("  This will generate a complete application with:");
        foreach (var feature in selectedAppTemplate.Features)
        {
            Console.WriteLine($"    • {feature}");
        }

        Console.WriteLine();
        Console.Write("  Create this application? (Y/N): ");
        var confirm = Console.ReadKey();
        Console.WriteLine();

        if (char.ToUpper(confirm.KeyChar) != 'Y')
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Application creation cancelled.");
            Console.ResetColor();
            WaitForKey();
            return;
        }

        await GenerateApplication(selectedAppTemplate.Template, projectName, outputPath);
        WaitForKey();
    }

    /// <summary>
    /// Generate a project with the specified template.
    /// </summary>
    public static async Task GenerateProject(CliProjectTemplate template, string projectName, string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                    GENERATING PROJECT                         ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        try
        {
            // Create output directory
            Directory.CreateDirectory(outputPath);

            var files = CliProjectTemplates.GetTemplateFiles(template, projectName);
            int current = 0;

            foreach (var file in files)
            {
                current++;
                Console.Write($"  [{current}/{files.Count}] Creating {file.FileName}... ");
                var filePath = Path.Combine(outputPath, file.FileName);
                await File.WriteAllTextAsync(filePath, file.Content);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                    PROJECT CREATED SUCCESSFULLY               ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine($"  Location: {outputPath}");
            Console.WriteLine($"  Files created: {files.Count}");
            Console.WriteLine();

            ShowNextSteps(template, projectName);
        }
        catch (Exception ex)
        {
            ShowError($"Failed to create project: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate an application from an Application Template.
    /// </summary>
    public static async Task GenerateApplication(CliApplicationTemplate template, string projectName, string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                   GENERATING APPLICATION                      ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");

        // Get the actual ApplicationTemplate from DataObjects
        var templateId = template switch
        {
            CliApplicationTemplate.FreeBase => "freebase",
            CliApplicationTemplate.FreeTracker => "freetracker",
            CliApplicationTemplate.FreeAudit => "freeaudit",
            _ => "freebase"
        };

        var appTemplate = FreeManager.DataObjects.ApplicationTemplates.GetById(templateId);
        if (appTemplate == null)
        {
            ShowError($"Template '{templateId}' not found.");
            return;
        }

        try
        {
            // Convert to EntityWizardState for code generation
            Console.Write("  Preparing template... ");
            var wizardState = FreeManager.DataObjects.ApplicationTemplates.ToWizardState(appTemplate, projectName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();

            // Generate files using EntityTemplates
            Console.Write("  Generating code... ");
            var files = EntityTemplates.GenerateAllFiles(wizardState);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ ({files.Count} files)");
            Console.ResetColor();

            // Create base output directory
            Directory.CreateDirectory(outputPath);

            Console.WriteLine();
            Console.WriteLine("  Writing files to folder structure:");
            Console.WriteLine($"    {projectName}/");
            Console.WriteLine($"    {projectName}.Client/");
            Console.WriteLine($"    {projectName}.DataAccess/");
            Console.WriteLine($"    {projectName}.DataObjects/");
            Console.WriteLine($"    {projectName}.EFModels/");
            Console.WriteLine();

            int current = 0;
            foreach (var file in files)
            {
                current++;
                
                // Get the proper export path based on file type
                var relativePath = GetExportPath(file.FileType, file.FileName, projectName);
                var fullPath = Path.Combine(outputPath, relativePath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var displayPath = relativePath.Length > 55 ? "..." + relativePath[^52..] : relativePath;
                Console.Write($"    [{current,2}/{files.Count}] {displayPath}... ");
                await File.WriteAllTextAsync(fullPath, file.Content);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓");
                Console.ResetColor();
            }

            // Write README.txt
            var readmePath = Path.Combine(outputPath, "README.txt");
            await File.WriteAllTextAsync(readmePath, GetExportReadme(projectName, projectName, files.Count));
            Console.WriteLine($"    [  ] README.txt... ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                 APPLICATION CREATED SUCCESSFULLY              ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine($"  Location: {outputPath}");
            Console.WriteLine($"  Files created: {files.Count + 1}"); // +1 for README

            // Show folder structure
            Console.WriteLine();
            Console.WriteLine("  FOLDER STRUCTURE:");
            ShowFolderStructure(outputPath, projectName);

            Console.WriteLine();
            ShowApplicationNextSteps(template, projectName);
        }
        catch (Exception ex)
        {
            ShowError($"Failed to create application: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps file type to export path in project folder structure.
    /// Matches the web export structure exactly.
    /// </summary>
    private static string GetExportPath(string fileType, string fileName, string projectName)
    {
        // Ensure file has .App. in the name for proper FreeCRM convention
        string baseName = fileName;
        if (!baseName.Contains(".App.") && 
            !fileType.Equals(FreeManager.DataObjects.FMFileTypes.EFModel) && 
            !fileType.Equals(FreeManager.DataObjects.FMFileTypes.EFDataModel) &&
            !fileType.Equals("Stylesheet"))
        {
            int extIndex = baseName.LastIndexOf('.');
            if (extIndex > 0)
            {
                baseName = baseName[..extIndex] + ".App" + baseName[extIndex..];
            }
        }

        return fileType switch
        {
            FreeManager.DataObjects.FMFileTypes.DataObjects => $"{projectName}.DataObjects/{baseName}",
            FreeManager.DataObjects.FMFileTypes.DataAccess => $"{projectName}.DataAccess/{baseName}",
            FreeManager.DataObjects.FMFileTypes.Controller => $"{projectName}/Controllers/{baseName}",
            FreeManager.DataObjects.FMFileTypes.RazorComponent => $"{projectName}.Client/Shared/AppComponents/{baseName}",
            FreeManager.DataObjects.FMFileTypes.RazorPage => $"{projectName}.Client/Pages/{baseName}",
            FreeManager.DataObjects.FMFileTypes.Stylesheet => $"{projectName}.Client/wwwroot/css/{baseName}",
            FreeManager.DataObjects.FMFileTypes.GlobalSettings => $"{projectName}.DataObjects/{baseName}",
            FreeManager.DataObjects.FMFileTypes.HelpersApp => $"{projectName}.Client/{baseName}",
            FreeManager.DataObjects.FMFileTypes.EFModel => $"{projectName}.EFModels/EFModels/{baseName}",
            FreeManager.DataObjects.FMFileTypes.EFDataModel => $"{projectName}.EFModels/EFModels/{baseName}",
            FreeManager.DataObjects.FMFileTypes.Utilities => $"{projectName}.DataAccess/{baseName}",
            _ => $"{projectName}/{baseName}"
        };
    }

    /// <summary>
    /// Generates README content for the export.
    /// </summary>
    private static string GetExportReadme(string projectName, string displayName, int fileCount) => $@"
================================================================================
{displayName} ({projectName})
Exported from FreeManager CLI
================================================================================

This folder contains {fileCount} .App. extension file(s) for your project.

INSTALLATION:
1. Clone or download the latest FreeCRM from GitHub
2. Fork it to create your {projectName} project (or use FreeTools.ForkCRM)
3. Copy these folders on top of your {projectName} folder
4. The .App. files will merge with the existing FreeCRM codebase

FOLDER STRUCTURE:
  {projectName}/                    - Server project (Controllers)
  {projectName}.Client/             - Blazor WebAssembly client (Pages, Components)
  {projectName}.DataAccess/         - Data access layer
  {projectName}.DataObjects/        - DTOs and data objects
  {projectName}.EFModels/           - Entity Framework models

AFTER COPYING:
1. Open the solution in Visual Studio
2. Build the solution to verify no errors
3. Run EF migrations:
   dotnet ef migrations add {projectName}_Initial --startup-project ../{projectName}
   dotnet ef database update --startup-project ../{projectName}
4. Run the application

Generated by FreeManager CLI
";

    /// <summary>
    /// Shows the folder structure created.
    /// </summary>
    private static void ShowFolderStructure(string outputPath, string projectName)
    {
        var folders = new[]
        {
            $"{projectName}",
            $"{projectName}.Client",
            $"{projectName}.DataAccess",
            $"{projectName}.DataObjects",
            $"{projectName}.EFModels"
        };

        Console.WriteLine("  ┌────────────────────────────────────────────────────┐");
        foreach (var folder in folders)
        {
            var fullPath = Path.Combine(outputPath, folder);
            if (Directory.Exists(fullPath))
            {
                var fileCount = Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories).Length;
                Console.WriteLine($"  │  {folder.PadRight(35)} ({fileCount,3} files) │");
            }
        }
        Console.WriteLine($"  │  README.txt                                        │");
        Console.WriteLine("  └────────────────────────────────────────────────────┘");
    }

    /// <summary>
    /// Show list of available templates.
    /// </summary>
    private static void ShowTemplateList()
    {
        Console.Clear();
        ShowHeader();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                   AVAILABLE TEMPLATES                         ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Simple Project Templates
        Console.WriteLine("  SIMPLE PROJECT TEMPLATES (for 'new' command):");
        Console.WriteLine("  ┌──────────────┬───────┬─────────────────────────────────────────┐");
        Console.WriteLine("  │ Template     │ Files │ Description                             │");
        Console.WriteLine("  ├──────────────┼───────┼─────────────────────────────────────────┤");

        var templates = CliProjectTemplates.GetTemplates();
        foreach (var t in templates)
        {
            var name = t.IsRecommended ? $"{t.Name} *" : t.Name;
            var desc = t.Description.Length > 39 ? t.Description[..36] + "..." : t.Description;
            Console.WriteLine($"  │ {name.PadRight(12)} │ {t.FileCount,5} │ {desc.PadRight(39)} │");
        }
        Console.WriteLine("  └──────────────┴───────┴─────────────────────────────────────────┘");
        Console.WriteLine("  (* = Recommended)");
        Console.WriteLine();

        // Application Templates
        Console.WriteLine("  APPLICATION TEMPLATES (for 'app' command):");
        Console.WriteLine("  ┌──────────────┬──────────────┬──────────┬──────────────────────┐");
        Console.WriteLine("  │ Template     │ Difficulty   │ Entities │ Description          │");
        Console.WriteLine("  ├──────────────┼──────────────┼──────────┼──────────────────────┤");

        var appTemplates = CliProjectTemplates.GetApplicationTemplates();
        foreach (var t in appTemplates)
        {
            var desc = t.Description.Length > 20 ? t.Description[..17] + "..." : t.Description;
            Console.Write("  │ ");
            Console.ForegroundColor = t.Template == CliApplicationTemplate.FreeAudit ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.Write(t.Name.PadRight(12));
            Console.ResetColor();
            Console.WriteLine($" │ {t.Difficulty.PadRight(12)} │ {t.EntityCount,8} │ {desc.PadRight(20)} │");
        }
        Console.WriteLine("  └──────────────┴──────────────┴──────────┴──────────────────────┘");

        WaitForKey();
    }

    /// <summary>
    /// Show help information.
    /// </summary>
    private static void ShowHelp()
    {
        Console.Clear();
        ShowHeader();

        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                    HELP & COMMAND LINE USAGE                  ");
        Console.ResetColor();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine("  INTERACTIVE MODE:");
        Console.WriteLine("    Run without arguments to use the interactive menu:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    FreeManager.exe");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine("  COMMAND LINE MODE:");
        Console.WriteLine();

        Console.WriteLine("    Create a simple project:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    FreeManager.exe new <name> [--template <type>] [--output <dir>]");
        Console.ResetColor();
        Console.WriteLine("      Templates: Empty, Skeleton, Starter (default), FullCrud");
        Console.WriteLine();

        Console.WriteLine("    Create an application from template:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    FreeManager.exe app <name> [--template <type>] [--output <dir>]");
        Console.ResetColor();
        Console.WriteLine("      Templates: FreeBase, FreeTracker, FreeAudit (default)");
        Console.WriteLine();

        Console.WriteLine("    Other commands:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    FreeManager.exe list                  ");
        Console.ResetColor();
        Console.WriteLine("      List all available templates");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("    FreeManager.exe help                  ");
        Console.ResetColor();
        Console.WriteLine("      Show this help");
        Console.WriteLine();

        Console.WriteLine("  EXAMPLES:");
        Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  │  # Create a Starter project named 'Tasks'                 │");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  │  FreeManager.exe new Tasks                                │");
        Console.ResetColor();
        Console.WriteLine("  │                                                            │");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  │  # Create a FullCrud project with custom output           │");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  │  FreeManager.exe new Inventory -t FullCrud -o C:\\Projects │");
        Console.ResetColor();
        Console.WriteLine("  │                                                            │");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  │  # Create a FreeAudit application named 'FreeGLBA'        │");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  │  FreeManager.exe app FreeGLBA --template FreeAudit        │");
        Console.ResetColor();
        Console.WriteLine("  │                                                            │");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  │  # Create FreeTracker app in specific folder              │");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  │  FreeManager.exe app Assets -t FreeTracker -o C:\\MyApp    │");
        Console.ResetColor();
        Console.WriteLine("  └────────────────────────────────────────────────────────────┘");

        WaitForKey();
    }

    /// <summary>
    /// Show next steps after project creation.
    /// </summary>
    private static void ShowNextSteps(CliProjectTemplate template, string projectName)
    {
        Console.WriteLine("  NEXT STEPS:");
        Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
        Console.WriteLine("  │  1. Copy the generated files to your FreeCRM project:     │");
        Console.WriteLine("  │     • DataObjects.App.*.cs  → FreeManager.DataObjects     │");
        Console.WriteLine("  │     • DataAccess.App.*.cs   → FreeManager.DataAccess      │");
        Console.WriteLine("  │     • DataController.App.*  → FreeManager/Controllers     │");
        Console.WriteLine("  │     • *.razor               → FreeManager.Client/Pages    │");

        if (template == CliProjectTemplate.FullCrud)
        {
            Console.WriteLine("  │                                                            │");
            Console.WriteLine("  │  2. Run EF migrations:                                     │");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  │     dotnet ef migrations add {projectName}_Initial \\       │");
            Console.WriteLine("  │       --startup-project ../FreeManager                     │");
            Console.WriteLine("  │     dotnet ef database update \\                            │");
            Console.WriteLine("  │       --startup-project ../FreeManager                     │");
            Console.ResetColor();
        }

        Console.WriteLine("  │                                                            │");
        Console.WriteLine("  │  " + (template == CliProjectTemplate.FullCrud ? "3" : "2") + ". Build and run your application                        │");
        Console.WriteLine("  └────────────────────────────────────────────────────────────┘");
    }

    /// <summary>
    /// Show next steps after application creation.
    /// </summary>
    private static void ShowApplicationNextSteps(CliApplicationTemplate template, string projectName)
    {
        Console.WriteLine("  NEXT STEPS:");
        Console.WriteLine("  ┌────────────────────────────────────────────────────────────┐");
        Console.WriteLine("  │  1. Copy the generated files to your FreeCRM project:     │");
        Console.WriteLine("  │     • *.App.*.cs (EFModel)    → FreeManager.EFModels      │");
        Console.WriteLine("  │     • *.App.DataObjects.cs    → FreeManager.DataObjects   │");
        Console.WriteLine("  │     • *.App.DataAccess.cs     → FreeManager.DataAccess    │");
        Console.WriteLine("  │     • *.App.*Controller.cs    → FreeManager/Controllers   │");
        Console.WriteLine("  │     • *.razor                 → FreeManager.Client/Pages  │");
        Console.WriteLine("  │                                                            │");
        Console.WriteLine("  │  2. Run EF migrations:                                     │");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  │     dotnet ef migrations add {projectName}_Initial \\       │");
        Console.WriteLine("  │       --startup-project ../FreeManager                     │");
        Console.WriteLine("  │     dotnet ef database update \\                            │");
        Console.WriteLine("  │       --startup-project ../FreeManager                     │");
        Console.ResetColor();

        if (template == CliApplicationTemplate.FreeAudit)
        {
            Console.WriteLine("  │                                                            │");
            Console.WriteLine("  │  3. Configure external API (FreeAudit specific):          │");
            Console.WriteLine("  │     • Add SourceSystem records with hashed API keys       │");
            Console.WriteLine("  │     • External systems POST to /api/glba/events           │");
            Console.WriteLine("  │     • Review the GlbaController for customization         │");
        }

        Console.WriteLine("  │                                                            │");
        Console.WriteLine($"  │  {(template == CliApplicationTemplate.FreeAudit ? "4" : "3")}. Build and run your application                        │");
        Console.WriteLine("  └────────────────────────────────────────────────────────────┘");
    }

    /// <summary>
    /// Show error message.
    /// </summary>
    private static void ShowError(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ ERROR: {message}");
        Console.ResetColor();
        WaitForKey();
    }

    /// <summary>
    /// Wait for user to press a key.
    /// </summary>
    private static void WaitForKey()
    {
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }
}

#endregion
