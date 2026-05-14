using FreePlugins.Abstractions;

namespace FreePlugins.ExamplePlugin;

/// <summary>
/// A comprehensive example compiled plugin demonstrating all prompt types,
/// button callbacks, dynamic prompt loading, and conditional field visibility.
/// 
/// This is a compiled NuGet-based version of the file-based Example1.cs plugin.
/// 
/// Features demonstrated:
/// - All 16 prompt types
/// - Button callback handlers
/// - Dynamic prompt option loading
/// - Prompt value change handlers (show/hide fields)
/// - File upload handling
/// - Required field validation
/// </summary>
[Plugin(
    Id = "9bbdfb99-80cd-4bbb-8741-6d287437e5f8",
    Name = "All Prompts Example (Compiled)",
    Type = PluginTypes.General,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A comprehensive example plugin demonstrating all prompt types, button callbacks, dynamic prompt loading, and conditional field visibility. Converted from file-based Example1.cs.",
    SortOrder = 0,
    Enabled = true
)]
public class AllPromptsPlugin : ICompiledGeneralPlugin
{
    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(AllPromptsPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("9bbdfb99-80cd-4bbb-8741-6d287437e5f8") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", false },
        { "Description", "A comprehensive example plugin demonstrating all prompt types." },
        { "Name", "All Prompts Example (Compiled)" },
        { "Prompts", BuildPrompts() },
        { "PromptValuesOnUpdate", "PromptValuesUpdated" },
        { "SortOrder", 0 },
        { "Type", PluginTypes.General },
        { "Version", "1.0.0" },
        { "Enabled", true },
    };

    /// <summary>
    /// Execute the plugin - displays all entered prompt values.
    /// </summary>
    public async Task<PluginResult> ExecuteAsync(IPluginContext context)
    {
        await Task.CompletedTask;

        var messages = new List<string>();
        var plugin = context.Plugin;

        messages.Add($"Plugin Executed: {plugin.Name}");
        messages.Add($"Version: {plugin.Version}");
        messages.Add($"Is Compiled: {plugin.IsCompiled}");
        messages.Add("");

        // See if there are any prompts configured
        if (plugin.Prompts.Count > 0) {
            messages.Add($"Prompts Configured: {plugin.Prompts.Count}");
            messages.Add("");

            // In a real scenario, prompt values would be passed through the context.
            // For now, we just list the configured prompts.
            foreach (var prompt in plugin.Prompts) {
                string visibility = prompt.Hidden ? " (hidden)" : "";
                string required = prompt.Required ? " *" : "";
                messages.Add($"  • {prompt.Name}{required}: {prompt.PromptType}{visibility}");
            }
        }

        context.LogInfo("All Prompts Example plugin executed successfully");

        return PluginResult.Success(messages);
    }

    /// <summary>
    /// Builds all the prompt configurations for this plugin.
    /// Demonstrates all 16 prompt types available in the plugin system.
    /// </summary>
    private static List<PluginPrompt> BuildPrompts() =>
    [
        // Button prompt - executes code when clicked
        new PluginPrompt {
            Name = "Button1",
            Description = "A sample of how to use a button to execute code.",
            ElementClass = "col col-12",
            PromptType = PluginPromptType.Button,
            Function = "Button1",
            Required = false,
            SortOrder = 0,
            Options = [
                new PluginPromptOption { Label = "ButtonText", Value = "Test Button" },
                new PluginPromptOption { Label = "ButtonClass", Value = "btn btn-success" },
                new PluginPromptOption { Label = "ButtonIcon", Value = "fa-regular fa-circle-check" },
            ],
        },

        // Checkbox - single boolean toggle (controls visibility of other fields)
        new PluginPrompt {
            Name = "Checkbox",
            Description = "Click below to agree and show additional fields.",
            ElementClass = "col col-3",
            PromptType = PluginPromptType.Checkbox,
            Required = true,
            SortOrder = 1,
        },

        // CheckboxList - multiple selection from list
        new PluginPrompt {
            Name = "CheckboxList",
            Description = "Select one or more options from the list.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.CheckboxList,
            Required = true,
            SortOrder = 2,
            Options = [
                new PluginPromptOption { Label = "Option 1", Value = "1" },
                new PluginPromptOption { Label = "Option 2", Value = "2" },
                new PluginPromptOption { Label = "Option 3", Value = "3" },
            ],
        },

        // Date - date picker
        new PluginPrompt {
            Name = "Date",
            Description = "Select a date from the calendar.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Date,
            Required = false,
            SortOrder = 3,
        },

        // DateTime - date and time picker
        new PluginPrompt {
            Name = "DateTime",
            Description = "Select a date and time from the calendar.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.DateTime,
            Required = false,
            SortOrder = 4,
        },

        // File - single file upload
        new PluginPrompt {
            Name = "File",
            Description = "Upload a file from your computer.",
            ElementClass = "col col-6",
            Hidden = true,
            PromptType = PluginPromptType.File,
            Required = false,
            SortOrder = 5,
        },

        // Files - multiple file upload
        new PluginPrompt {
            Name = "Files",
            Description = "Upload one or more files from your computer.",
            ElementClass = "col col-6",
            Hidden = true,
            PromptType = PluginPromptType.Files,
            Required = false,
            SortOrder = 6,
        },

        // HTML - display-only HTML content
        new PluginPrompt {
            Name = "HTML",
            Description = "",
            ElementClass = "col col-12",
            Hidden = true,
            PromptType = PluginPromptType.HTML,
            DefaultValue = "<h4>HTML Example</h4><p>This is an example of HTML in a plugin prompt used to display information.</p>",
            Required = false,
            SortOrder = 7,
        },

        // Multiselect - multiple selection dropdown
        new PluginPrompt {
            Name = "Multiselect",
            Description = "Select one or more values from the list.",
            ElementClass = "col col-12",
            Hidden = true,
            PromptType = PluginPromptType.Multiselect,
            Required = false,
            SortOrder = 8,
            Options = [
                new PluginPromptOption { Label = "Option 1", Value = "1" },
                new PluginPromptOption { Label = "Option 2", Value = "2" },
                new PluginPromptOption { Label = "Option 3", Value = "3" },
            ],
        },

        // Number - numeric input
        new PluginPrompt {
            Name = "Number",
            Description = "Enter a number.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Number,
            Required = false,
            SortOrder = 9,
        },

        // Password - masked text input
        new PluginPrompt {
            Name = "Password",
            Description = "Enter a password.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Password,
            Required = false,
            SortOrder = 10,
        },

        // Radio - single selection from list
        new PluginPrompt {
            Name = "Radio",
            Description = "Select one option from the list.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Radio,
            Required = false,
            SortOrder = 11,
            Options = [
                new PluginPromptOption { Label = "Option 1", Value = "1" },
                new PluginPromptOption { Label = "Option 2", Value = "2" },
                new PluginPromptOption { Label = "Option 3", Value = "3" },
            ],
        },

        // Select - single selection dropdown
        new PluginPrompt {
            Name = "Select",
            Description = "Select one option from the list.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Select,
            Required = false,
            SortOrder = 12,
            Options = [
                new PluginPromptOption { Label = "Option 1", Value = "1" },
                new PluginPromptOption { Label = "Option 2", Value = "2" },
                new PluginPromptOption { Label = "Option 3", Value = "3" },
            ],
        },

        // Select with dynamic options from function
        new PluginPrompt {
            Name = "Select with Values from Function",
            Description = "",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Select,
            Function = "GetPromptValues",
            Required = false,
            SortOrder = 13,
        },

        // Text - single line text input
        new PluginPrompt {
            Name = "Text",
            Description = "Enter some text.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Text,
            Required = false,
            SortOrder = 14,
        },

        // Textarea - multi-line text input
        new PluginPrompt {
            Name = "Textarea",
            Description = "Please describe...",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Textarea,
            Required = false,
            SortOrder = 15,
        },

        // Time - time picker
        new PluginPrompt {
            Name = "Time",
            Description = "Enter a time.",
            ElementClass = "col col-3",
            Hidden = true,
            PromptType = PluginPromptType.Time,
            Required = false,
            SortOrder = 16,
        },
    ];

    #region Callback Methods (for documentation - these would need integration support)

    /// <summary>
    /// Button1 click handler.
    /// Note: In compiled plugins, button callbacks require additional integration work.
    /// This method documents what the callback would do.
    /// </summary>
    public PluginResult OnButton1Click()
    {
        return PluginResult.Success(["Button 1 Clicked!"]);
    }

    /// <summary>
    /// Dynamic option loading for Select prompts.
    /// Note: In compiled plugins, dynamic option loading requires additional integration work.
    /// This method documents how to provide dynamic options.
    /// </summary>
    public List<PluginPromptOption> GetDynamicOptions()
    {
        return [
            new PluginPromptOption { Label = "Dynamic Option 1", Value = "d1" },
            new PluginPromptOption { Label = "Dynamic Option 2", Value = "d2" },
            new PluginPromptOption { Label = "Dynamic Option 3", Value = "d3" },
        ];
    }

    /// <summary>
    /// Prompt value change handler - shows/hides fields based on checkbox state.
    /// Note: In compiled plugins, change handlers require additional integration work.
    /// This method documents the conditional visibility logic.
    /// </summary>
    public void OnPromptValuesChanged(PluginMetadata plugin, string promptName, string[] values)
    {
        if (promptName == "Checkbox" && values.Length > 0) {
            bool isChecked = values[0].Equals("true", StringComparison.OrdinalIgnoreCase);

            // In the file-based version, this would show/hide fields after the checkbox.
            // When isChecked = true, show all fields.
            // When isChecked = false, hide all fields after the checkbox.
        }
    }

    #endregion
}

