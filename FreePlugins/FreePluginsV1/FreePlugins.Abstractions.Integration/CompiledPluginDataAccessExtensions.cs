using FreePlugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugins;

namespace FreePlugins.Integration;

/// <summary>
/// Extension methods for compiled plugin support in DataAccess.
/// This file adds compiled plugin execution without modifying existing DataAccess code.
/// 
/// Usage:
///   // Check if a plugin is compiled
///   if (CompiledPluginRegistry.IsCompiledPlugin(pluginId)) {
///       var result = await da.ExecuteCompiledPluginAsync(pluginId, iteration, serviceProvider);
///   }
/// </summary>
public static class CompiledPluginDataAccessExtensions
{
    /// <summary>
    /// Executes a compiled background process plugin.
    /// </summary>
    public static async Task<PluginExecuteResult> ExecuteCompiledBackgroundProcessAsync(
        this IDataAccess da,
        Guid pluginId,
        long iteration,
        IServiceProvider serviceProvider)
    {
        var output = new PluginExecuteResult
        {
            Messages = new List<string>(),
            Objects = new List<object>(),
            Result = false,
        };

        var entry = CompiledPluginRegistry.GetById(pluginId);
        if (entry == null)
        {
            output.Messages.Add($"Compiled plugin not found: {pluginId}");
            return output;
        }

        try
        {
            var plugin = serviceProvider.GetService(entry.PluginType);
            if (plugin is not IPluginBackgroundProcess bgPlugin)
            {
                output.Messages.Add($"Plugin {entry.Metadata.Name} does not implement IPluginBackgroundProcess");
                return output;
            }

            var logger = serviceProvider.GetService<ILogger<CompiledPluginExecutor>>();
            var context = new PluginContext(entry.Metadata, serviceProvider, logger);
            
            var result = await bgPlugin.ExecuteAsync(context, iteration);

            output.Result = result.Result;
            if (result.Messages != null)
            {
                output.Messages = result.Messages;
            }
            if (result.Objects != null)
            {
                output.Objects = result.Objects.ToList();
            }
        }
        catch (Exception ex)
        {
            output.Messages.Add($"Error executing compiled plugin: {ex.Message}");
        }

        return output;
    }

    /// <summary>
    /// Executes a compiled general plugin.
    /// </summary>
    public static async Task<PluginExecuteResult> ExecuteCompiledPluginAsync(
        this IDataAccess da,
        Guid pluginId,
        IServiceProvider serviceProvider)
    {
        var output = new PluginExecuteResult
        {
            Messages = new List<string>(),
            Objects = new List<object>(),
            Result = false,
        };

        var entry = CompiledPluginRegistry.GetById(pluginId);
        if (entry == null)
        {
            output.Messages.Add($"Compiled plugin not found: {pluginId}");
            return output;
        }

        try
        {
            var plugin = serviceProvider.GetService(entry.PluginType);
            if (plugin is not Abstractions.IPlugin generalPlugin)
            {
                output.Messages.Add($"Plugin {entry.Metadata.Name} does not implement IPlugin");
                return output;
            }

            var logger = serviceProvider.GetService<ILogger<CompiledPluginExecutor>>();
            var context = new PluginContext(entry.Metadata, serviceProvider, logger);
            
            var result = await generalPlugin.ExecuteAsync(context);

            output.Result = result.Result;
            if (result.Messages != null)
            {
                output.Messages = result.Messages;
            }
            if (result.Objects != null)
            {
                output.Objects = result.Objects.ToList();
            }
        }
        catch (Exception ex)
        {
            output.Messages.Add($"Error executing compiled plugin: {ex.Message}");
        }

        return output;
    }

    /// <summary>
    /// Gets all plugins including both file-based and compiled plugins.
    /// </summary>
    public static List<Plugin> GetAllPluginsIncludingCompiled(this IDataAccess da)
    {
        var plugins = da.GetPlugins().ToList();
        
        // Add compiled plugins that aren't already in the list
        foreach (var compiled in CompiledPluginRegistry.AllPlugins)
        {
            if (!plugins.Any(p => p.Id == compiled.Metadata.Id))
            {
                plugins.Add(CompiledPluginConverter.ToPlugin(compiled.Metadata));
            }
        }

        return plugins.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Gets all plugins without code, including compiled plugins.
    /// </summary>
    public static List<Plugin> GetAllPluginsWithoutCodeIncludingCompiled(this IDataAccess da)
    {
        // Get plugins and strip code manually
        var plugins = da.GetPlugins()
            .Select(p => new Plugin
            {
                Id = p.Id,
                Author = p.Author,
                ClassName = p.ClassName,
                Code = "", // Strip code
                ContainsSensitiveData = p.ContainsSensitiveData,
                Description = p.Description,
                LimitToTenants = p.LimitToTenants,
                Name = p.Name,
                Namespace = p.Namespace,
                Invoker = p.Invoker,
                Prompts = p.Prompts,
                PromptValues = p.PromptValues,
                Properties = p.Properties,
                SortOrder = p.SortOrder,
                Type = p.Type,
                Version = p.Version,
                AdditionalAssemblies = p.AdditionalAssemblies,
            })
            .ToList();
        
        // Add compiled plugins (they already have no code)
        foreach (var compiled in CompiledPluginRegistry.AllPlugins)
        {
            if (!plugins.Any(p => p.Id == compiled.Metadata.Id))
            {
                plugins.Add(CompiledPluginConverter.ToPlugin(compiled.Metadata));
            }
        }

        return plugins.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
    }
}

/// <summary>
/// Helper class to check if a plugin should use compiled execution.
/// </summary>
public static class CompiledPluginHelper
{
    /// <summary>
    /// Determines if a plugin request should be executed as a compiled plugin.
    /// </summary>
    public static bool ShouldUseCompiledExecution(PluginExecuteRequest request)
    {
        return CompiledPluginRegistry.IsCompiledPlugin(request.Plugin.Id);
    }

    /// <summary>
    /// Determines if a plugin request should be executed as a compiled plugin.
    /// </summary>
    public static bool ShouldUseCompiledExecution(Guid pluginId)
    {
        return CompiledPluginRegistry.IsCompiledPlugin(pluginId);
    }
}
