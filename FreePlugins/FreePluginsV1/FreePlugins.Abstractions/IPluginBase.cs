namespace FreePlugins.Abstractions;

/// <summary>
/// Base plugin interface that all plugins should implement.
/// This defines the Properties method that returns a dictionary of properties.
/// </summary>
public interface IPluginBase
{
    /// <summary>
    /// Returns the properties for this plugin including Id, Name, Type, Version, Author, etc.
    /// </summary>
    Dictionary<string, object> Properties();
}

/// <summary>
/// Result type returned from plugin execution methods.
/// </summary>
/// <param name="Result">Whether the plugin executed successfully.</param>
/// <param name="Messages">Optional messages from the plugin execution.</param>
/// <param name="Objects">Optional objects returned from the plugin execution.</param>
public record PluginResult(bool Result, List<string>? Messages, IEnumerable<object>? Objects)
{
    /// <summary>
    /// Creates a successful result with no messages or objects.
    /// </summary>
    public static PluginResult Success() => new(true, null, null);

    /// <summary>
    /// Creates a successful result with messages.
    /// </summary>
    public static PluginResult Success(List<string> messages) => new(true, messages, null);

    /// <summary>
    /// Creates a successful result with messages and objects.
    /// </summary>
    public static PluginResult Success(List<string>? messages, IEnumerable<object>? objects) => new(true, messages, objects);

    /// <summary>
    /// Creates a failed result with no messages.
    /// </summary>
    public static PluginResult Failure() => new(false, null, null);

    /// <summary>
    /// Creates a failed result with messages.
    /// </summary>
    public static PluginResult Failure(List<string> messages) => new(false, messages, null);

    /// <summary>
    /// Creates a failed result with a single message.
    /// </summary>
    public static PluginResult Failure(string message) => new(false, [message], null);

    /// <summary>
    /// Converts to the tuple format used by existing plugin interfaces.
    /// </summary>
    public (bool Result, List<string>? Messages, IEnumerable<object>? Objects) ToTuple() => (Result, Messages, Objects);
}
