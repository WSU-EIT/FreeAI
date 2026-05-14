namespace FreePlugins.Abstractions;

/// <summary>
/// Attribute to define plugin metadata on a compiled plugin class.
/// This is an alternative to implementing Properties() for compiled plugins.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PluginAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for this plugin.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of this plugin.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The plugin type (General, Auth, BackgroundProcess, UserUpdate).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The version of this plugin.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// The author of this plugin.
    /// </summary>
    public string Author { get; init; } = String.Empty;

    /// <summary>
    /// A description of this plugin.
    /// </summary>
    public string Description { get; init; } = String.Empty;

    /// <summary>
    /// The sort order for this plugin.
    /// </summary>
    public int SortOrder { get; init; } = 0;

    /// <summary>
    /// Whether this plugin contains sensitive data.
    /// </summary>
    public bool ContainsSensitiveData { get; init; } = false;

    /// <summary>
    /// Whether this plugin is enabled by default.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Converts the attribute to a PluginMetadata object.
    /// </summary>
    public PluginMetadata ToMetadata(Type pluginType)
    {
        PluginMetadata output = new PluginMetadata {
            Id = Guid.Parse(Id),
            Name = Name,
            Type = Type,
            Version = Version,
            Author = Author,
            Description = Description,
            SortOrder = SortOrder,
            ContainsSensitiveData = ContainsSensitiveData,
            Enabled = Enabled,
            ClassName = pluginType.Name,
            Namespace = pluginType.Namespace ?? String.Empty,
            IsCompiled = true,
            CompiledTypeName = pluginType.AssemblyQualifiedName,
            Invoker = Type.ToLower() switch {
                "auth" => "Login",
                "userupdate" => "UpdateUser",
                _ => "Execute"
            }
        };

        return output;
    }
}

/// <summary>
/// Well-known plugin type constants.
/// </summary>
public static class PluginTypes
{
    public const string General = "General";
    public const string Auth = "Auth";
    public const string BackgroundProcess = "BackgroundProcess";
    public const string UserUpdate = "UserUpdate";
}
