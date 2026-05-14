namespace FreePlugins.Abstractions;

/// <summary>
/// Marker interface for compiled plugins that can be discovered via assembly scanning.
/// All compiled plugins should implement this interface in addition to their specific plugin interface.
/// </summary>
public interface ICompiledPlugin : IPluginBase
{
    /// <summary>
    /// Gets the type of the compiled plugin instance.
    /// Used for creating instances via DI.
    /// </summary>
    static abstract Type PluginType { get; }
}

/// <summary>
/// Interface for compiled background process plugins.
/// </summary>
public interface ICompiledBackgroundProcessPlugin : ICompiledPlugin, IPluginBackgroundProcess
{
}

/// <summary>
/// Interface for compiled general plugins.
/// </summary>
public interface ICompiledGeneralPlugin : ICompiledPlugin, IPlugin
{
}

/// <summary>
/// Interface for compiled auth plugins.
/// </summary>
public interface ICompiledAuthPlugin : ICompiledPlugin, IPluginAuth
{
}

/// <summary>
/// Interface for compiled user update plugins.
/// </summary>
public interface ICompiledUserUpdatePlugin : ICompiledPlugin, IPluginUserUpdate
{
}
