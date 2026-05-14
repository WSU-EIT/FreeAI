namespace FreePlugins.Abstractions;

/// <summary>
/// Fluent builder for creating plugin prompts more easily.
/// </summary>
public class PluginPromptBuilder
{
    private readonly PluginPrompt _prompt = new();

    private PluginPromptBuilder() { }

    /// <summary>
    /// Creates a new prompt builder.
    /// </summary>
    public static PluginPromptBuilder Create(string name) => new PluginPromptBuilder().WithName(name);

    /// <summary>
    /// Creates a button prompt.
    /// </summary>
    public static PluginPromptBuilder Button(string name, string text, string cssClass = "btn btn-primary", string? icon = null)
    {
        var builder = Create(name).OfType(PluginPromptType.Button);
        var options = new List<PluginPromptOption> {
            new() { Label = "ButtonText", Value = text },
            new() { Label = "ButtonClass", Value = cssClass },
        };
        if (!String.IsNullOrEmpty(icon)) {
            options.Add(new() { Label = "ButtonIcon", Value = icon });
        }
        builder._prompt.Options = options;
        return builder;
    }

    /// <summary>
    /// Creates a checkbox prompt.
    /// </summary>
    public static PluginPromptBuilder Checkbox(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Checkbox).WithDescription(description);

    /// <summary>
    /// Creates a checkbox list prompt.
    /// </summary>
    public static PluginPromptBuilder CheckboxList(string name, string description = "")
        => Create(name).OfType(PluginPromptType.CheckboxList).WithDescription(description);

    /// <summary>
    /// Creates a date prompt.
    /// </summary>
    public static PluginPromptBuilder Date(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Date).WithDescription(description);

    /// <summary>
    /// Creates a datetime prompt.
    /// </summary>
    public static PluginPromptBuilder DateTime(string name, string description = "")
        => Create(name).OfType(PluginPromptType.DateTime).WithDescription(description);

    /// <summary>
    /// Creates a file upload prompt.
    /// </summary>
    public static PluginPromptBuilder File(string name, string description = "")
        => Create(name).OfType(PluginPromptType.File).WithDescription(description);

    /// <summary>
    /// Creates a multi-file upload prompt.
    /// </summary>
    public static PluginPromptBuilder Files(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Files).WithDescription(description);

    /// <summary>
    /// Creates an HTML display prompt.
    /// </summary>
    public static PluginPromptBuilder Html(string name, string htmlContent)
        => Create(name).OfType(PluginPromptType.HTML).WithDefaultValue(htmlContent);

    /// <summary>
    /// Creates a multiselect prompt.
    /// </summary>
    public static PluginPromptBuilder Multiselect(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Multiselect).WithDescription(description);

    /// <summary>
    /// Creates a number prompt.
    /// </summary>
    public static PluginPromptBuilder Number(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Number).WithDescription(description);

    /// <summary>
    /// Creates a password prompt.
    /// </summary>
    public static PluginPromptBuilder Password(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Password).WithDescription(description);

    /// <summary>
    /// Creates a radio button prompt.
    /// </summary>
    public static PluginPromptBuilder Radio(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Radio).WithDescription(description);

    /// <summary>
    /// Creates a select dropdown prompt.
    /// </summary>
    public static PluginPromptBuilder Select(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Select).WithDescription(description);

    /// <summary>
    /// Creates a text input prompt.
    /// </summary>
    public static PluginPromptBuilder Text(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Text).WithDescription(description);

    /// <summary>
    /// Creates a textarea prompt.
    /// </summary>
    public static PluginPromptBuilder Textarea(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Textarea).WithDescription(description);

    /// <summary>
    /// Creates a time prompt.
    /// </summary>
    public static PluginPromptBuilder Time(string name, string description = "")
        => Create(name).OfType(PluginPromptType.Time).WithDescription(description);

    // Fluent configuration methods

    /// <summary>
    /// Sets the prompt name.
    /// </summary>
    public PluginPromptBuilder WithName(string name)
    {
        _prompt.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the prompt type.
    /// </summary>
    public PluginPromptBuilder OfType(PluginPromptType type)
    {
        _prompt.PromptType = type;
        return this;
    }

    /// <summary>
    /// Sets the prompt description.
    /// </summary>
    public PluginPromptBuilder WithDescription(string description)
    {
        _prompt.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the prompt label (alias for WithDescription).
    /// </summary>
    public PluginPromptBuilder WithPrompt(string prompt)
    {
        _prompt.Prompt = prompt;
        return this;
    }

    /// <summary>
    /// Sets the default value.
    /// </summary>
    public PluginPromptBuilder WithDefaultValue(string defaultValue)
    {
        _prompt.DefaultValue = defaultValue;
        return this;
    }

    /// <summary>
    /// Marks the prompt as required.
    /// </summary>
    public PluginPromptBuilder Required(bool required = true)
    {
        _prompt.Required = required;
        return this;
    }

    /// <summary>
    /// Sets the sort order.
    /// </summary>
    public PluginPromptBuilder WithSortOrder(int sortOrder)
    {
        _prompt.SortOrder = sortOrder;
        return this;
    }

    /// <summary>
    /// Marks the prompt as initially hidden.
    /// </summary>
    public PluginPromptBuilder Hidden(bool hidden = true)
    {
        _prompt.Hidden = hidden;
        return this;
    }

    /// <summary>
    /// Sets the CSS class for the element.
    /// </summary>
    public PluginPromptBuilder WithClass(string elementClass)
    {
        _prompt.ElementClass = elementClass;
        return this;
    }

    /// <summary>
    /// Sets the function name for callbacks or dynamic loading.
    /// </summary>
    public PluginPromptBuilder WithFunction(string function)
    {
        _prompt.Function = function;
        return this;
    }

    /// <summary>
    /// Adds options for select/checkbox/radio prompts.
    /// </summary>
    public PluginPromptBuilder WithOptions(params (string Label, string Value)[] options)
    {
        _prompt.Options = options.Select(o => new PluginPromptOption { Label = o.Label, Value = o.Value }).ToList();
        return this;
    }

    /// <summary>
    /// Adds options for select/checkbox/radio prompts.
    /// </summary>
    public PluginPromptBuilder WithOptions(IEnumerable<PluginPromptOption> options)
    {
        _prompt.Options = options.ToList();
        return this;
    }

    /// <summary>
    /// Adds options using simple string values (label = value).
    /// </summary>
    public PluginPromptBuilder WithOptions(params string[] values)
    {
        _prompt.Options = values.Select(v => new PluginPromptOption { Label = v, Value = v }).ToList();
        return this;
    }

    /// <summary>
    /// Builds the prompt.
    /// </summary>
    public PluginPrompt Build() => _prompt;

    /// <summary>
    /// Implicit conversion to PluginPrompt.
    /// </summary>
    public static implicit operator PluginPrompt(PluginPromptBuilder builder) => builder.Build();
}

/// <summary>
/// Extension methods for building prompt lists.
/// </summary>
public static class PluginPromptExtensions
{
    /// <summary>
    /// Creates a list of prompts from builders.
    /// </summary>
    public static List<PluginPrompt> ToPromptList(this IEnumerable<PluginPromptBuilder> builders)
        => builders.Select(b => b.Build()).ToList();
}
