using FreePlugins.Abstractions;
using Plugins;

namespace FreePlugins.Integration;

/// <summary>
/// Converts compiled plugins to the existing Plugin format for compatibility with the existing system.
/// </summary>
public static class CompiledPluginConverter
{
    /// <summary>
    /// Converts a PluginMetadata to a Plugin object for compatibility with existing code.
    /// </summary>
    public static Plugin ToPlugin(PluginMetadata metadata)
    {
        return new Plugin
        {
            Id = metadata.Id,
            Author = metadata.Author,
            ClassName = metadata.ClassName,
            Code = "", // Compiled plugins don't have source code
            ContainsSensitiveData = metadata.ContainsSensitiveData,
            Description = metadata.Description,
            LimitToTenants = metadata.LimitToTenants,
            Name = metadata.Name,
            Namespace = metadata.Namespace,
            Invoker = metadata.Invoker,
            Prompts = metadata.Prompts.Select(p => new Plugins.PluginPrompt
            {
                DefaultValue = p.DefaultValue,
                Name = p.Name,
                Options = p.Options?.Select(o => new Plugins.PluginPromptOption
                {
                    Label = o.Label,
                    Value = o.Value
                }).ToList(),
                Required = p.Required,
                Type = (Plugins.PluginPromptType)(int)p.PromptType,
            }).ToList(),
            PromptValues = new List<Plugins.PluginPromptValue>(),
            Properties = metadata.Properties,
            SortOrder = metadata.SortOrder,
            Type = metadata.Type,
            Version = metadata.Version,
            AdditionalAssemblies = new List<string>(),
        };
    }

    /// <summary>
    /// Gets all compiled plugins as Plugin objects.
    /// </summary>
    public static List<Plugin> GetAllAsPlugins()
    {
        return CompiledPluginRegistry.AllPlugins
            .Select(e => ToPlugin(e.Metadata))
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToList();
    }
}
