using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FreePlugins.Abstractions;

/// <summary>
/// Default implementation of IPluginContext for compiled plugins.
/// </summary>
public class PluginContext : IPluginContext
{
    private readonly IServiceProvider _services;
    private readonly ILogger? _logger;
    private PluginMetadata _plugin;

    public PluginContext(PluginMetadata plugin, IServiceProvider services, ILogger? logger = null)
    {
        _plugin = plugin;
        _services = services;
        _logger = logger;
    }

    public PluginMetadata Plugin => _plugin;

    public IServiceProvider Services => _services;

    public T? GetService<T>() where T : class
    {
        return _services.GetService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        return _services.GetRequiredService<T>();
    }

    public void LogInfo(string message)
    {
        _logger?.LogInformation("[{PluginName}] {Message}", _plugin.Name, message);
        Console.WriteLine($"[{_plugin.Name}] INFO: {message}");
    }

    public void LogWarning(string message)
    {
        _logger?.LogWarning("[{PluginName}] {Message}", _plugin.Name, message);
        Console.WriteLine($"[{_plugin.Name}] WARN: {message}");
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null) {
            _logger?.LogError(exception, "[{PluginName}] {Message}", _plugin.Name, message);
            Console.WriteLine($"[{_plugin.Name}] ERROR: {message} - {exception.Message}");
        } else {
            _logger?.LogError("[{PluginName}] {Message}", _plugin.Name, message);
            Console.WriteLine($"[{_plugin.Name}] ERROR: {message}");
        }
    }
}

/// <summary>
/// Default implementation of IPluginAuthContext for compiled auth plugins.
/// </summary>
public class PluginAuthContext : PluginContext, IPluginAuthContext
{
    private string _url;
    private Guid _tenantId;
    private object _httpContext;

    public PluginAuthContext(
        PluginMetadata plugin,
        IServiceProvider services,
        string url,
        Guid tenantId,
        object httpContext,
        ILogger? logger = null)
        : base(plugin, services, logger)
    {
        _url = url;
        _tenantId = tenantId;
        _httpContext = httpContext;
    }

    public string Url => _url;
    public Guid TenantId => _tenantId;
    public object HttpContext => _httpContext;
}

/// <summary>
/// Default implementation of IPluginUserContext for compiled user update plugins.
/// </summary>
public class PluginUserContext : PluginContext, IPluginUserContext
{
    private object? _user;

    public PluginUserContext(
        PluginMetadata plugin,
        IServiceProvider services,
        object? user,
        ILogger? logger = null)
        : base(plugin, services, logger)
    {
        _user = user;
    }

    public object? User => _user;
}
