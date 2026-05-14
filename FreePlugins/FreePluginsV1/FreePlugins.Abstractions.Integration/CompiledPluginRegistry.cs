using FreePlugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FreePlugins.Integration;

/// <summary>
/// Static registry for compiled plugins. This is a standalone system that works alongside
/// the existing file-based plugin system without modifying it.
/// 
/// Usage in Program.cs:
///   // Register compiled plugins
///   builder.Services.AddPlugin&lt;MyPlugin&gt;();
///   // OR auto-discover from assembly
///   builder.Services.AddPluginsFromAssembly(typeof(MyPlugin).Assembly);
///   
///   var app = builder.Build();
///   
///   // Load compiled plugins into the registry
///   app.LoadCompiledPlugins();
/// </summary>
public static class CompiledPluginRegistry
{
    private static readonly List<CompiledPluginEntry> _plugins = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Gets all registered compiled plugins.
    /// </summary>
    public static IReadOnlyList<CompiledPluginEntry> AllPlugins
    {
        get
        {
            lock (_lock)
            {
                return _plugins.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Registers a compiled plugin.
    /// </summary>
    internal static void Register(CompiledPluginEntry entry)
    {
        lock (_lock)
        {
            if (!_plugins.Any(p => p.Metadata.Id == entry.Metadata.Id))
            {
                _plugins.Add(entry);
            }
        }
    }

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    public static CompiledPluginEntry? GetById(Guid id)
    {
        lock (_lock)
        {
            return _plugins.FirstOrDefault(p => p.Metadata.Id == id);
        }
    }

    /// <summary>
    /// Gets plugins by type.
    /// </summary>
    public static IReadOnlyList<CompiledPluginEntry> GetByType(string type)
    {
        lock (_lock)
        {
            return _plugins.Where(p => p.Metadata.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if a plugin ID is a compiled plugin.
    /// </summary>
    public static bool IsCompiledPlugin(Guid id)
    {
        lock (_lock)
        {
            return _plugins.Any(p => p.Metadata.Id == id);
        }
    }

    /// <summary>
    /// Clears all registered plugins (mainly for testing).
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _plugins.Clear();
        }
    }
}

/// <summary>
/// Represents a registered compiled plugin.
/// </summary>
public record CompiledPluginEntry(Type PluginType, PluginMetadata Metadata);

/// <summary>
/// Extension methods for loading compiled plugins at application startup.
/// </summary>
public static class CompiledPluginHostExtensions
{
    /// <summary>
    /// Loads compiled plugins into the CompiledPluginRegistry after the application has been built.
    /// Call this after builder.Build() and before app.Run().
    /// </summary>
    public static T LoadCompiledPlugins<T>(this T host) where T : IHost
    {
        using var scope = host.Services.CreateScope();
        var registrations = scope.ServiceProvider.GetServices<CompiledPluginRegistration>();
        
        int count = 0;
        foreach (var registration in registrations)
        {
            CompiledPluginRegistry.Register(new CompiledPluginEntry(registration.PluginType, registration.Metadata));
            count++;
        }

        if (count > 0)
        {
            Console.WriteLine($"[CompiledPlugins] Loaded {count} compiled plugin(s) from NuGet packages.");
        }

        return host;
    }
}
