using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace FreePlugins.Abstractions;

/// <summary>
/// Extension methods for registering compiled plugins with the DI container.
/// </summary>
public static class PluginServiceCollectionExtensions
{
    /// <summary>
    /// Registers a compiled plugin with the service collection.
    /// </summary>
    /// <typeparam name="TPlugin">The plugin type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for the plugin.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPlugin<TPlugin>(
        this IServiceCollection services,
        Action<PluginMetadata>? configure = null)
        where TPlugin : class, IPluginBase
    {
        Type pluginType = typeof(TPlugin);
        PluginMetadata metadata = GetPluginMetadata(pluginType);

        configure?.Invoke(metadata);

        // Register the plugin type
        services.AddTransient(pluginType);

        // Register the metadata
        services.AddSingleton(new CompiledPluginRegistration(pluginType, metadata));

        return services;
    }

    /// <summary>
    /// Scans an assembly for compiled plugins and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPluginsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        IEnumerable<Type> pluginTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i == typeof(IPluginBase)))
            .Where(t => t.GetCustomAttribute<PluginAttribute>() != null);

        foreach (Type pluginType in pluginTypes) {
            PluginMetadata metadata = GetPluginMetadata(pluginType);

            // Register the plugin type
            services.AddTransient(pluginType);

            // Register the metadata
            services.AddSingleton(new CompiledPluginRegistration(pluginType, metadata));
        }

        return services;
    }

    /// <summary>
    /// Scans the calling assembly for compiled plugins and registers them.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddPluginsFromCallingAssembly(this IServiceCollection services)
    {
        return services.AddPluginsFromAssembly(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Gets all registered compiled plugin registrations.
    /// </summary>
    /// <param name="services">The service provider.</param>
    public static IEnumerable<CompiledPluginRegistration> GetCompiledPlugins(this IServiceProvider services)
    {
        return services.GetServices<CompiledPluginRegistration>();
    }

    private static PluginMetadata GetPluginMetadata(Type pluginType)
    {
        // See if the plugin has metadata from an attribute
        PluginAttribute? attribute = pluginType.GetCustomAttribute<PluginAttribute>();
        if (attribute != null) {
            return attribute.ToMetadata(pluginType);
        }

        // Fall back to creating an instance and calling Properties().
        // This requires a parameterless constructor.
        if (pluginType.GetConstructor(Type.EmptyTypes) != null) {
            object? instance = Activator.CreateInstance(pluginType);
            if (instance is IPluginBase pluginBase) {
                Dictionary<string, object> properties = pluginBase.Properties();
                return CreateMetadataFromProperties(pluginType, properties);
            }
        }

        throw new InvalidOperationException(
            $"Plugin type {pluginType.Name} must have a [Plugin] attribute or a parameterless constructor.");
    }

    private static PluginMetadata CreateMetadataFromProperties(Type pluginType, Dictionary<string, object> properties)
    {
        T? GetProperty<T>(string key)
        {
            if (properties.TryGetValue(key, out object? value)) {
                try { return (T)value; } catch { }
            }
            return default;
        }

        string pluginTypeString = GetProperty<string>("Type") ?? "General";

        PluginMetadata output = new PluginMetadata {
            Id = GetProperty<Guid>("Id"),
            Name = GetProperty<string>("Name") ?? pluginType.Name,
            Type = pluginTypeString,
            Version = GetProperty<string>("Version") ?? "1.0.0",
            Author = GetProperty<string>("Author") ?? String.Empty,
            Description = GetProperty<string>("Description") ?? String.Empty,
            SortOrder = GetProperty<int>("SortOrder"),
            ContainsSensitiveData = GetProperty<bool>("ContainsSensitiveData"),
            Enabled = properties.ContainsKey("Enabled") ? GetProperty<bool>("Enabled") : true,
            ClassName = pluginType.Name,
            Namespace = pluginType.Namespace ?? String.Empty,
            IsCompiled = true,
            CompiledTypeName = pluginType.AssemblyQualifiedName,
            Invoker = pluginTypeString.ToLower() switch {
                "auth" => "Login",
                "userupdate" => "UpdateUser",
                _ => "Execute"
            },
            Properties = properties
        };

        return output;
    }
}

/// <summary>
/// Represents a registered compiled plugin.
/// </summary>
public record CompiledPluginRegistration(Type PluginType, PluginMetadata Metadata);
