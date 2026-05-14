using System.CommandLine;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace FreeManager.Cli;

#region FreeManager CLI Entry Point
// ============================================================================
// FREEMANAGER CLI - MAIN ENTRY POINT
// Command line project generator for FreeCRM applications.
//
// Usage:
//   FreeManager.exe                              - Interactive menu
//   FreeManager.exe new <name> [options]         - Create simple project
//   FreeManager.exe app <name> [options]         - Create application from template
//   FreeManager.exe list                         - List templates
//   FreeManager.exe help                         - Show help
// ============================================================================

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        // If no arguments, show interactive menu
        if (args.Length == 0)
        {
            await MenuService.ShowMainMenu();
            return 0;
        }

        // Set up command line parsing
        var rootCommand = new RootCommand("FreeManager CLI - FreeCRM Project Generator");

        // 'new' command - simple project templates
        var newCommand = new Command("new", "Create a new project from a simple template (Empty, Skeleton, Starter, FullCrud)");
        var nameArgument = new Argument<string>("name", "Project name (must be valid C# identifier)");
        var templateOption = new Option<string>(
            aliases: ["--template", "-t"],
            description: "Template type: Empty, Skeleton, Starter (default), FullCrud",
            getDefaultValue: () => "Starter");
        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output directory (default: ./<name>)",
            getDefaultValue: () => "");

        newCommand.AddArgument(nameArgument);
        newCommand.AddOption(templateOption);
        newCommand.AddOption(outputOption);

        newCommand.SetHandler(async (name, template, output) =>
        {
            await CreateProject(name, template, output);
        }, nameArgument, templateOption, outputOption);

        rootCommand.AddCommand(newCommand);

        // 'app' command - application templates (FreeBase, FreeTracker, FreeAudit)
        var appCommand = new Command("app", "Create an application from a full template (FreeBase, FreeTracker, FreeAudit)");
        var appNameArgument = new Argument<string>("name", "Application name (e.g., FreeGLBA)");
        var appTemplateOption = new Option<string>(
            aliases: ["--template", "-t"],
            description: "Application template: FreeBase, FreeTracker, FreeAudit (default)",
            getDefaultValue: () => "FreeAudit");
        var appOutputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output directory (default: ./<name>)",
            getDefaultValue: () => "");

        appCommand.AddArgument(appNameArgument);
        appCommand.AddOption(appTemplateOption);
        appCommand.AddOption(appOutputOption);

        appCommand.SetHandler(async (name, template, output) =>
        {
            await CreateApplication(name, template, output);
        }, appNameArgument, appTemplateOption, appOutputOption);

        rootCommand.AddCommand(appCommand);

        // 'list' command
        var listCommand = new Command("list", "List all available templates");
        listCommand.SetHandler(() =>
        {
            ListTemplates();
        });
        rootCommand.AddCommand(listCommand);

        // 'help' command (alias for --help)
        var helpCommand = new Command("help", "Show help information");
        helpCommand.SetHandler(() =>
        {
            ShowCliHelp();
        });
        rootCommand.AddCommand(helpCommand);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Create a project from command line arguments.
    /// </summary>
    private static async Task CreateProject(string name, string templateName, string output)
    {
        // Validate name
        if (!Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Project name must start with a letter and contain only letters and numbers.");
            Console.ResetColor();
            return;
        }

        // Parse template
        if (!Enum.TryParse<CliProjectTemplate>(templateName, true, out var template))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Unknown template '{templateName}'.");
            Console.WriteLine("Valid options: Empty, Skeleton, Starter, FullCrud");
            Console.ResetColor();
            return;
        }

        // Determine output path
        var outputPath = string.IsNullOrEmpty(output)
            ? Path.Combine(Environment.CurrentDirectory, name)
            : output;

        await MenuService.GenerateProject(template, name, outputPath);
    }

    /// <summary>
    /// Create an application from command line arguments.
    /// </summary>
    private static async Task CreateApplication(string name, string templateName, string output)
    {
        // Validate name
        if (!Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Application name must start with a letter and contain only letters and numbers.");
            Console.ResetColor();
            return;
        }

        // Parse template
        if (!Enum.TryParse<CliApplicationTemplate>(templateName, true, out var template))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Unknown template '{templateName}'.");
            Console.WriteLine("Valid options: FreeBase, FreeTracker, FreeAudit");
            Console.ResetColor();
            return;
        }

        // Determine output path
        var outputPath = string.IsNullOrEmpty(output)
            ? Path.Combine(Environment.CurrentDirectory, name)
            : output;

        await MenuService.GenerateApplication(template, name, outputPath);
    }

    /// <summary>
    /// List available templates (command line output).
    /// </summary>
    private static void ListTemplates()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("FreeManager CLI - Available Templates");
        Console.WriteLine("══════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        // Simple Project Templates
        Console.WriteLine("SIMPLE PROJECT TEMPLATES (use with 'new' command):");
        Console.WriteLine("┌──────────────┬───────┬─────────────────────────────────────────────────┐");
        Console.WriteLine("│ Template     │ Files │ Description                                     │");
        Console.WriteLine("├──────────────┼───────┼─────────────────────────────────────────────────┤");

        var templates = CliProjectTemplates.GetTemplates();
        foreach (var t in templates)
        {
            var name = t.IsRecommended ? $"{t.Name} *" : t.Name;
            Console.WriteLine($"│ {name.PadRight(12)} │ {t.FileCount,5} │ {t.Description.PadRight(47)} │");
        }
        Console.WriteLine("└──────────────┴───────┴─────────────────────────────────────────────────┘");
        Console.WriteLine("(* = Recommended)");
        Console.WriteLine();

        // Application Templates
        Console.WriteLine("APPLICATION TEMPLATES (use with 'app' command):");
        Console.WriteLine("┌──────────────┬──────────────┬──────────┬─────────────────────────────────┐");
        Console.WriteLine("│ Template     │ Difficulty   │ Entities │ Description                     │");
        Console.WriteLine("├──────────────┼──────────────┼──────────┼─────────────────────────────────┤");

        var appTemplates = CliProjectTemplates.GetApplicationTemplates();
        foreach (var t in appTemplates)
        {
            var desc = t.Description.Length > 31 ? t.Description[..28] + "..." : t.Description;
            Console.WriteLine($"│ {t.Name.PadRight(12)} │ {t.Difficulty.PadRight(12)} │ {t.EntityCount,8} │ {desc.PadRight(31)} │");
        }
        Console.WriteLine("└──────────────┴──────────────┴──────────┴─────────────────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("Examples:");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  FreeManager.exe new Tasks                    # Starter project");
        Console.WriteLine("  FreeManager.exe new Inventory -t FullCrud    # FullCrud project");
        Console.WriteLine("  FreeManager.exe app FreeGLBA -t FreeAudit    # FreeAudit application");
        Console.ResetColor();
    }

    /// <summary>
    /// Show CLI help (command line output).
    /// </summary>
    private static void ShowCliHelp()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("FreeManager CLI - FreeCRM Project Generator");
        Console.WriteLine("════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine("USAGE:");
        Console.WriteLine("  FreeManager.exe                              Interactive menu");
        Console.WriteLine("  FreeManager.exe new <name> [options]         Create simple project");
        Console.WriteLine("  FreeManager.exe app <name> [options]         Create application");
        Console.WriteLine("  FreeManager.exe list                         List templates");
        Console.WriteLine("  FreeManager.exe help                         Show this help");
        Console.WriteLine();

        Console.WriteLine("OPTIONS FOR 'new' COMMAND:");
        Console.WriteLine("  --template, -t   Template: Empty, Skeleton, Starter (default), FullCrud");
        Console.WriteLine("  --output, -o     Output directory (default: ./<name>)");
        Console.WriteLine();

        Console.WriteLine("OPTIONS FOR 'app' COMMAND:");
        Console.WriteLine("  --template, -t   Template: FreeBase, FreeTracker, FreeAudit (default)");
        Console.WriteLine("  --output, -o     Output directory (default: ./<name>)");
        Console.WriteLine();

        Console.WriteLine("SIMPLE PROJECT TEMPLATES:");
        Console.WriteLine("  Empty        No starter files, build from scratch");
        Console.WriteLine("  Skeleton     Basic structure with placeholder comments");
        Console.WriteLine("  Starter      Working example with Items list (recommended)");
        Console.WriteLine("  FullCrud     Complete CRUD with EF Entity (requires migration)");
        Console.WriteLine();

        Console.WriteLine("APPLICATION TEMPLATES:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  FreeBase     ");
        Console.ResetColor();
        Console.WriteLine("Collection + Categories (Beginner)");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  FreeTracker  ");
        Console.ResetColor();
        Console.WriteLine("+ Assignment + Checkout + Status (Intermediate)");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  FreeAudit    ");
        Console.ResetColor();
        Console.WriteLine("+ Logging + External API + Reports (Advanced)");
        Console.WriteLine();

        Console.WriteLine("EXAMPLES:");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  # Create a Starter project named 'Tasks'");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  FreeManager.exe new Tasks");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  # Create a FullCrud project with custom output");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  FreeManager.exe new Inventory -t FullCrud -o C:\\Projects\\MyApp");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  # Create a FreeAudit application named 'FreeGLBA'");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  FreeManager.exe app FreeGLBA --template FreeAudit");
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  # Create FreeGLBA in a specific folder");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  FreeManager.exe app FreeGLBA -t FreeAudit -o C:\\Projects\\GLBA");
        Console.ResetColor();
    }
}

#endregion
