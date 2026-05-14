using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FreePlugins.Abstractions;

/// <summary>
/// Executes compiled plugins using dependency injection.
/// </summary>
public class CompiledPluginExecutor
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CompiledPluginExecutor>? _logger;

    public CompiledPluginExecutor(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetService<ILogger<CompiledPluginExecutor>>();
    }

    /// <summary>
    /// Executes a compiled background process plugin.
    /// </summary>
    public async Task<PluginResult> ExecuteBackgroundProcess(
        Type pluginType,
        PluginMetadata metadata,
        long iteration)
    {
        try {
            object? plugin = _services.GetService(pluginType);
            if (plugin is not IPluginBackgroundProcess bgPlugin) {
                return PluginResult.Failure($"Plugin {metadata.Name} does not implement IPluginBackgroundProcess");
            }

            PluginContext context = new PluginContext(metadata, _services, _logger);
            return await bgPlugin.ExecuteAsync(context, iteration);
        } catch (Exception ex) {
            _logger?.LogError(ex, "Error executing background process plugin {PluginName}", metadata.Name);
            return PluginResult.Failure($"Error executing plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a compiled general plugin.
    /// </summary>
    public async Task<PluginResult> Execute(Type pluginType, PluginMetadata metadata)
    {
        try {
            object? plugin = _services.GetService(pluginType);
            if (plugin is not IPlugin generalPlugin) {
                return PluginResult.Failure($"Plugin {metadata.Name} does not implement IPlugin");
            }

            PluginContext context = new PluginContext(metadata, _services, _logger);
            return await generalPlugin.ExecuteAsync(context);
        } catch (Exception ex) {
            _logger?.LogError(ex, "Error executing plugin {PluginName}", metadata.Name);
            return PluginResult.Failure($"Error executing plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a compiled auth plugin login.
    /// </summary>
    public async Task<PluginResult> ExecuteLogin(
        Type pluginType,
        PluginMetadata metadata,
        string url,
        Guid tenantId,
        object httpContext)
    {
        try {
            object? plugin = _services.GetService(pluginType);
            if (plugin is not IPluginAuth authPlugin) {
                return PluginResult.Failure($"Plugin {metadata.Name} does not implement IPluginAuth");
            }

            PluginAuthContext context = new PluginAuthContext(metadata, _services, url, tenantId, httpContext, _logger);
            return await authPlugin.LoginAsync(context);
        } catch (Exception ex) {
            _logger?.LogError(ex, "Error executing auth plugin login {PluginName}", metadata.Name);
            return PluginResult.Failure($"Error executing plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a compiled auth plugin logout.
    /// </summary>
    public async Task<PluginResult> ExecuteLogout(
        Type pluginType,
        PluginMetadata metadata,
        string url,
        Guid tenantId,
        object httpContext)
    {
        try {
            object? plugin = _services.GetService(pluginType);
            if (plugin is not IPluginAuth authPlugin) {
                return PluginResult.Failure($"Plugin {metadata.Name} does not implement IPluginAuth");
            }

            PluginAuthContext context = new PluginAuthContext(metadata, _services, url, tenantId, httpContext, _logger);
            return await authPlugin.LogoutAsync(context);
        } catch (Exception ex) {
            _logger?.LogError(ex, "Error executing auth plugin logout {PluginName}", metadata.Name);
            return PluginResult.Failure($"Error executing plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a compiled user update plugin.
    /// </summary>
    public async Task<PluginResult> ExecuteUserUpdate(
        Type pluginType,
        PluginMetadata metadata,
        object? user)
    {
        try {
            object? plugin = _services.GetService(pluginType);
            if (plugin is not IPluginUserUpdate userPlugin) {
                return PluginResult.Failure($"Plugin {metadata.Name} does not implement IPluginUserUpdate");
            }

            PluginUserContext context = new PluginUserContext(metadata, _services, user, _logger);
            return await userPlugin.UpdateUserAsync(context);
        } catch (Exception ex) {
            _logger?.LogError(ex, "Error executing user update plugin {PluginName}", metadata.Name);
            return PluginResult.Failure($"Error executing plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a plugin type is a compiled plugin by examining if it's registered.
    /// </summary>
    public bool IsCompiledPlugin(Guid pluginId)
    {
        IEnumerable<CompiledPluginRegistration> registrations = _services.GetServices<CompiledPluginRegistration>();
        return registrations.Any(r => r.Metadata.Id == pluginId);
    }

    /// <summary>
    /// Gets the registration for a compiled plugin by ID.
    /// </summary>
    public CompiledPluginRegistration? GetRegistration(Guid pluginId)
    {
        IEnumerable<CompiledPluginRegistration> registrations = _services.GetServices<CompiledPluginRegistration>();
        return registrations.FirstOrDefault(r => r.Metadata.Id == pluginId);
    }
}
