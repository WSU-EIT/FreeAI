using FreePlugins.Abstractions;

namespace FreePlugins.DataAccessExamplePlugin;

/// <summary>
/// An example compiled plugin demonstrating how to access plugin metadata
/// and context information.
/// 
/// This is a compiled NuGet-based version of the file-based Example3.cs plugin.
/// 
/// Features demonstrated:
/// - Accessing plugin metadata (name, version, author)
/// - Using IPluginContext for services and logging
/// - ContainsSensitiveData flag for security marking
/// - SortOrder for execution ordering
/// - Comprehensive plugin information display
/// </summary>
[Plugin(
    Id = "4dd6cae9-b9a7-4048-8f7c-f338151d46ac",
    Name = "Context Info Example (Compiled)",
    Type = PluginTypes.General,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A compiled example plugin demonstrating access to plugin metadata and context information. Converted from file-based Example3.cs.",
    SortOrder = 3,
    Enabled = true
)]
public class ContextInfoPlugin : ICompiledGeneralPlugin
{
    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(ContextInfoPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("4dd6cae9-b9a7-4048-8f7c-f338151d46ac") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", true },
        { "Description", "Demonstrates accessing plugin metadata and context." },
        { "Name", "Context Info Example (Compiled)" },
        { "SortOrder", 3 },
        { "Type", PluginTypes.General },
        { "Version", "1.0.0" },
        { "Enabled", true },
    };

    /// <summary>
    /// Execute the plugin and display context information.
    /// </summary>
    /// <remarks>
    /// This plugin demonstrates all the information available through
    /// the IPluginContext interface.
    /// </remarks>
    public async Task<PluginResult> ExecuteAsync(IPluginContext context)
    {
        // Simulate async operation
        await Task.Delay(10);

        var messages = new List<string>();
        var plugin = context.Plugin;

        // Log start
        context.LogInfo($"Context Info plugin executing: {plugin.Name}");

        // First, display plugin information
        messages.Add("═══════════════════════════════════════");
        messages.Add("         PLUGIN INFORMATION            ");
        messages.Add("═══════════════════════════════════════");
        messages.Add("");
        messages.Add($"Plugin Name: {plugin.Name}");
        messages.Add($"Plugin Author: {plugin.Author}");
        messages.Add($"Plugin Version: {plugin.Version}");
        messages.Add($"Plugin Type: {plugin.Type}");
        messages.Add($"Plugin ID: {plugin.Id}");
        messages.Add($"Sort Order: {plugin.SortOrder}");
        messages.Add($"Is Compiled: {plugin.IsCompiled}");
        messages.Add($"Is Enabled: {plugin.Enabled}");
        messages.Add($"Contains Sensitive Data: {plugin.ContainsSensitiveData}");
        messages.Add("");

        // See if there is a description
        if (!String.IsNullOrWhiteSpace(plugin.Description)) {
            messages.Add($"Description: {plugin.Description}");
            messages.Add("");
        }

        // Now, display prompts configuration
        messages.Add("═══════════════════════════════════════");
        messages.Add("         PROMPTS CONFIGURATION         ");
        messages.Add("═══════════════════════════════════════");
        messages.Add("");
        messages.Add($"Prompts Configured: {plugin.Prompts.Count}");
        if (plugin.Prompts.Count > 0) {
            foreach (var prompt in plugin.Prompts) {
                messages.Add($"  • {prompt.Name}: {prompt.PromptType}");
            }
        }
        messages.Add("");

        // Next, display tenant restrictions
        messages.Add("═══════════════════════════════════════");
        messages.Add("         TENANT RESTRICTIONS           ");
        messages.Add("═══════════════════════════════════════");
        messages.Add("");
        if (plugin.LimitToTenants.Count > 0) {
            messages.Add("Restricted to tenants:");
            foreach (var tenantId in plugin.LimitToTenants) {
                messages.Add($"  • {tenantId}");
            }
        } else {
            messages.Add("Available to all tenants (no restrictions)");
        }
        messages.Add("");

        // Then, display context services
        messages.Add("═══════════════════════════════════════");
        messages.Add("         CONTEXT SERVICES              ");
        messages.Add("═══════════════════════════════════════");
        messages.Add("");
        messages.Add($"Services Available: {(context.Services != null ? "Yes" : "No")}");
        messages.Add("");
        messages.Add("Available through context:");
        messages.Add("  • context.Plugin - Plugin metadata");
        messages.Add("  • context.Services - IServiceProvider");
        messages.Add("  • context.GetService<T>() - Get optional service");
        messages.Add("  • context.GetRequiredService<T>() - Get required service");
        messages.Add("  • context.LogInfo() - Log information");
        messages.Add("  • context.LogWarning() - Log warnings");
        messages.Add("  • context.LogError() - Log errors");
        messages.Add("");

        // Finally, display custom properties
        messages.Add("═══════════════════════════════════════");
        messages.Add("         CUSTOM PROPERTIES             ");
        messages.Add("═══════════════════════════════════════");
        messages.Add("");
        if (plugin.Properties.Count > 0) {
            messages.Add($"Custom Properties: {plugin.Properties.Count}");
            foreach (var prop in plugin.Properties) {
                var valueStr = prop.Value?.ToString() ?? "null";
                if (valueStr.Length > 50) {
                    valueStr = valueStr[..50] + "...";
                }
                messages.Add($"  • {prop.Key}: {valueStr}");
            }
        } else {
            messages.Add("No custom properties defined");
        }
        messages.Add("");

        var output = new object[] {
            "Data object returned from plugin",
            plugin.Id,
            plugin.Version
        };

        // Log completion
        context.LogInfo("Context Info plugin completed successfully");

        return PluginResult.Success(messages, output);
    }
}
