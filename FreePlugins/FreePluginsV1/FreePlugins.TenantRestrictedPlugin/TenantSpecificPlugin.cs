using FreePlugins.Abstractions;

namespace FreePlugins.TenantRestrictedPlugin;

/// <summary>
/// An example compiled plugin demonstrating tenant restriction
/// in multi-tenant applications.
/// 
/// This is a compiled NuGet-based version of the file-based Example2.cs plugin.
/// 
/// Features demonstrated:
/// - LimitToTenants property for tenant restriction
/// - Simple ICompiledGeneralPlugin implementation
/// - SortOrder for plugin execution ordering
/// - Multi-tenant plugin visibility control
/// 
/// Use cases:
/// - Tenant-specific features or customizations
/// - Beta features for select tenants
/// - Licensed plugins for paying customers
/// - Region-specific functionality
/// </summary>
[Plugin(
    Id = "8507d6b9-deb4-45d6-bd6c-a8267c4a1693",
    Name = "Tenant-Restricted Example (Compiled)",
    Type = PluginTypes.General,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A compiled example plugin demonstrating tenant restriction. Only visible to specific tenants. Converted from file-based Example2.cs.",
    SortOrder = 1,
    Enabled = true
)]
public class TenantSpecificPlugin : ICompiledGeneralPlugin
{
    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(TenantSpecificPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("8507d6b9-deb4-45d6-bd6c-a8267c4a1693") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", false },
        { "Description", "A tenant-restricted plugin example." },
        { "Name", "Tenant-Restricted Example (Compiled)" },
        { "SortOrder", 1 },
        { "Type", PluginTypes.General },
        { "Version", "1.0.0" },
        { "Enabled", true },
        // This restricts the plugin to specific tenants only.
        // Users in other tenants will not see or be able to execute this plugin.
        { "LimitToTenants", new List<Guid> {
            Guid.Parse("00000000-0000-0000-0000-000000000002")
        }},
    };

    /// <summary>
    /// Execute the tenant-restricted plugin.
    /// </summary>
    /// <remarks>
    /// This plugin will only be visible and executable for tenants
    /// listed in the LimitToTenants property.
    /// 
    /// The tenant restriction is enforced by the plugin system
    /// before this method is called.
    /// </remarks>
    /// <param name="Context">The plugin execution context.</param>
    public async Task<PluginResult> ExecuteAsync(IPluginContext Context)
    {
        await Task.CompletedTask;

        List<string> messages = new();
        PluginMetadata plugin = Context.Plugin;

        Context.LogInfo($"Tenant-restricted plugin executing: {plugin.Name}");

        messages.Add($"Plugin: {plugin.Name}");
        messages.Add($"Version: {plugin.Version}");
        messages.Add($"Is Compiled: {plugin.IsCompiled}");
        messages.Add(String.Empty);
        messages.Add("This plugin is tenant-restricted.");
        messages.Add("It is only visible to tenants listed in LimitToTenants.");
        messages.Add(String.Empty);
        messages.Add("Restricted to tenant(s):");

        foreach (Guid tenantId in plugin.LimitToTenants) {
            messages.Add($"  • {tenantId}");
        }

        object[] output = ["Data from tenant-restricted plugin"];

        Context.LogInfo("Tenant-restricted plugin completed");

        return PluginResult.Success(messages, output);
    }
}

/// <summary>
/// Another example showing multiple tenant restrictions.
/// </summary>
[Plugin(
    Id = "8507d6b9-deb4-45d6-bd6c-a8267c4a1694",
    Name = "Multi-Tenant Restricted Example",
    Type = PluginTypes.General,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "An example showing restriction to multiple specific tenants.",
    SortOrder = 2,
    Enabled = true
)]
public class MultiTenantPlugin : ICompiledGeneralPlugin
{
    public static Type PluginType => typeof(MultiTenantPlugin);

    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("8507d6b9-deb4-45d6-bd6c-a8267c4a1694") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", false },
        { "Description", "Restricted to multiple tenants." },
        { "Name", "Multi-Tenant Restricted Example" },
        { "SortOrder", 2 },
        { "Type", PluginTypes.General },
        { "Version", "1.0.0" },
        { "Enabled", true },
        // Restricted to multiple specific tenants
        { "LimitToTenants", new List<Guid> {
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
        }},
    };

    /// <summary>
    /// Execute the multi-tenant plugin.
    /// </summary>
    /// <param name="Context">The plugin execution context.</param>
    public async Task<PluginResult> ExecuteAsync(IPluginContext Context)
    {
        await Task.CompletedTask;

        List<string> messages = new() {
            $"Plugin: {Context.Plugin.Name}",
            "This plugin is available to 3 specific tenants.",
            String.Empty,
            "Tenant IDs:"
        };

        foreach (Guid tenantId in Context.Plugin.LimitToTenants) {
            messages.Add($"  • {tenantId}");
        }

        Context.LogInfo("Multi-tenant plugin executed");

        return PluginResult.Success(messages);
    }
}
