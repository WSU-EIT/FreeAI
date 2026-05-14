namespace FreePlugins.Abstractions;

/// <summary>
/// Interface for Background Process plugins that run on a scheduled interval.
/// </summary>
public interface IPluginBackgroundProcess : IPluginBase
{
    /// <summary>
    /// Execute the background process.
    /// </summary>
    /// <param name="context">The plugin execution context containing services and plugin metadata.</param>
    /// <param name="iteration">The iteration number (1 on first run, increments each cycle).</param>
    /// <returns>A task containing the plugin result.</returns>
    Task<PluginResult> ExecuteAsync(IPluginContext context, long iteration);
}

/// <summary>
/// Interface for a basic plugin that just uses an Execute method.
/// </summary>
public interface IPlugin : IPluginBase
{
    /// <summary>
    /// Execute the plugin.
    /// </summary>
    /// <param name="context">The plugin execution context.</param>
    /// <returns>A task containing the plugin result.</returns>
    Task<PluginResult> ExecuteAsync(IPluginContext context);
}

/// <summary>
/// Interface for Auth plugins that implement Login and Logout methods.
/// </summary>
public interface IPluginAuth : IPluginBase
{
    /// <summary>
    /// Handle user login.
    /// </summary>
    /// <param name="context">The auth plugin context with HTTP context and tenant info.</param>
    Task<PluginResult> LoginAsync(IPluginAuthContext context);

    /// <summary>
    /// Handle user logout.
    /// </summary>
    /// <param name="context">The auth plugin context with HTTP context and tenant info.</param>
    Task<PluginResult> LogoutAsync(IPluginAuthContext context);
}

/// <summary>
/// Interface for plugins that handle user updates.
/// </summary>
public interface IPluginUserUpdate : IPluginBase
{
    /// <summary>
    /// Handle user update.
    /// </summary>
    /// <param name="context">The user plugin context with user data.</param>
    Task<PluginResult> UpdateUserAsync(IPluginUserContext context);
}
