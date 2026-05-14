namespace FreePlugins.Abstractions;

/// <summary>
/// Metadata describing a plugin.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// The unique Guid Id for this plugin.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the Author of this plugin.
    /// </summary>
    public string Author { get; set; } = String.Empty;

    /// <summary>
    /// The name of the class that contains the plugin code.
    /// </summary>
    public string ClassName { get; set; } = String.Empty;

    /// <summary>
    /// Flag that indicates if this plugin contains sensitive data.
    /// </summary>
    public bool ContainsSensitiveData { get; set; }

    /// <summary>
    /// A description of this plugin.
    /// </summary>
    public string Description { get; set; } = String.Empty;

    /// <summary>
    /// An option to limit this plugin to specific tenants.
    /// If empty, the plugin will be available to all tenants.
    /// </summary>
    public List<Guid> LimitToTenants { get; set; } = [];

    /// <summary>
    /// The name of this plugin.
    /// </summary>
    public string Name { get; set; } = String.Empty;

    /// <summary>
    /// The namespace in which the plugin resides.
    /// </summary>
    public string Namespace { get; set; } = String.Empty;

    /// <summary>
    /// The main invoker function for this plugin (defaults to "Execute").
    /// </summary>
    public string Invoker { get; set; } = "Execute";

    /// <summary>
    /// An optional collection of Prompts that can be used to collect data for this plugin.
    /// </summary>
    public List<PluginPrompt> Prompts { get; set; } = [];

    /// <summary>
    /// The collection of Properties for this plugin.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = [];

    /// <summary>
    /// The sort order for the plugin.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The type of plugin (General, Auth, BackgroundProcess, UserUpdate).
    /// </summary>
    public string Type { get; set; } = String.Empty;

    /// <summary>
    /// The version of the plugin.
    /// </summary>
    public string Version { get; set; } = String.Empty;

    /// <summary>
    /// Whether this plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Indicates this is a compiled plugin (from NuGet) vs a file-based plugin.
    /// </summary>
    public bool IsCompiled { get; set; }

    /// <summary>
    /// The assembly-qualified type name for compiled plugins.
    /// </summary>
    public string? CompiledTypeName { get; set; }
}

/// <summary>
/// Plugin prompt type enumeration.
/// </summary>
public enum PluginPromptType
{
    Button,
    Checkbox,
    CheckboxList,
    Date,
    DateTime,
    File,
    Files,
    HTML,
    Multiselect,
    Number,
    Password,
    Radio,
    Select,
    Text,
    Textarea,
    Time,
}

/// <summary>
/// Defines a prompt for collecting user input for a plugin.
/// </summary>
public class PluginPrompt
{
    /// <summary>
    /// The default value for this prompt.
    /// </summary>
    public string DefaultValue { get; set; } = String.Empty;

    /// <summary>
    /// A description of this prompt. This will be shown above the prompt
    /// when using the built-in PluginPrompts Blazor component.
    /// </summary>
    public string Description { get; set; } = String.Empty;

    /// <summary>
    /// A class to add to this individual prompt element.
    /// </summary>
    public string ElementClass { get; set; } = String.Empty;

    /// <summary>
    /// An optional function to call for this prompt (e.g., button click handler or option loader).
    /// </summary>
    public string Function { get; set; } = String.Empty;

    /// <summary>
    /// Indicates if this prompt element should be initially hidden.
    /// Can be updated by using the PromptValuesOnUpdate property of the plugin
    /// to call a function that will be used to update the plugin and/or prompts.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// The unique name/key for this prompt.
    /// </summary>
    public string Name { get; set; } = String.Empty;

    /// <summary>
    /// Optional list of options for select/checkbox/radio prompts.
    /// </summary>
    public List<PluginPromptOption> Options { get; set; } = [];

    /// <summary>
    /// The display label for this prompt.
    /// </summary>
    public string Prompt { get; set; } = String.Empty;

    /// <summary>
    /// The type of prompt.
    /// </summary>
    public PluginPromptType PromptType { get; set; } = PluginPromptType.Text;

    /// <summary>
    /// Whether this prompt is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Sort order for the prompt.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// An option for select/checkbox/radio prompts.
/// </summary>
public class PluginPromptOption
{
    /// <summary>
    /// The display label for this option.
    /// </summary>
    public string Label { get; set; } = String.Empty;

    /// <summary>
    /// The value for this option.
    /// </summary>
    public string Value { get; set; } = String.Empty;
}

/// <summary>
/// Stores the value for a plugin prompt.
/// </summary>
public class PluginPromptValue
{
    /// <summary>
    /// The name of the prompt this value is for.
    /// </summary>
    public string Name { get; set; } = String.Empty;

    /// <summary>
    /// The value entered for this prompt.
    /// </summary>
    public string Value { get; set; } = String.Empty;
}
