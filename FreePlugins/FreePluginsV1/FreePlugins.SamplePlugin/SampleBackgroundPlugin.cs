using FreePlugins.Abstractions;

namespace FreePlugins.SamplePlugin;

/// <summary>
/// A sample compiled background process plugin demonstrating the NuGet-based plugin architecture.
/// 
/// This plugin can be:
/// 1. Published as a NuGet package
/// 2. Referenced by the main application
/// 3. Registered via builder.Services.AddPlugin&lt;SampleBackgroundPlugin&gt;()
/// 4. Auto-discovered via builder.Services.AddPluginsFromAssembly()
/// </summary>
[Plugin(
    Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    Name = "Sample Background Plugin",
    Type = PluginTypes.BackgroundProcess,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A sample compiled plugin that demonstrates the NuGet-based plugin architecture.",
    SortOrder = 100,
    Enabled = true
)]
public class SampleBackgroundPlugin : ICompiledBackgroundProcessPlugin
{
    private static bool _firstRunLogged = false;

    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(SampleBackgroundPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// Note: When using [Plugin] attribute, this method is not called.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
        { "Name", "Sample Background Plugin" },
        { "Type", PluginTypes.BackgroundProcess },
        { "Version", "1.0.0" },
        { "Author", "WSU EIT" },
        { "Description", "A sample compiled plugin that demonstrates the NuGet-based plugin architecture." },
        { "SortOrder", 100 },
        { "Enabled", true },
    };

    /// <summary>
    /// Execute the background process.
    /// </summary>
    public async Task<PluginResult> ExecuteAsync(IPluginContext context, long iteration)
    {
        await Task.CompletedTask;

        // Only log on first run
        if (_firstRunLogged) {
            return PluginResult.Success();
        }

        _firstRunLogged = true;

        // See if the plugin is enabled
        if (!context.Plugin.Enabled) {
            return PluginResult.Success(["Sample plugin is disabled"]);
        }

        context.LogInfo($"Sample Background Plugin executed! Iteration: {iteration}");
        context.LogInfo($"Plugin Version: {context.Plugin.Version}");
        context.LogInfo($"Plugin is compiled: {context.Plugin.IsCompiled}");

        return PluginResult.Success([$"Sample plugin ran successfully on iteration {iteration}"]);
    }
}
