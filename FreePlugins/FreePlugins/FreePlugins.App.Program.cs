using FreePlugins.Abstractions;
using Microsoft.Extensions.Configuration.Json;

namespace FreePlugins;

// FreePlugins startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads FreePlugins' own settings files (appsettings.FreePlugins.App.json and its environment variant), so the stock
    /// appsettings files stay untouched. They go right after the stock appsettings files: they override those, while user
    /// secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreePluginsAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreePlugins.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreePlugins.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }

    /// <summary>
    /// Registers the compiled example plugins (strongly-typed plugins from project references that need no dynamic
    /// compilation) and the compiled plugin executor. Called from AppModifyBuilderEnd, just before Build().
    /// </summary>
    public static WebApplicationBuilder FreePluginsAppModifyBuilderEnd(WebApplicationBuilder builder)
    {
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.SamplePlugin.SampleBackgroundPlugin).Assembly);
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.ExamplePlugin.AllPromptsPlugin).Assembly);
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.AuthExamplePlugin.CustomAuthPlugin).Assembly);
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.UserUpdateExamplePlugin.UserSyncPlugin).Assembly);
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.TenantRestrictedPlugin.TenantSpecificPlugin).Assembly);
        builder.Services.AddPluginsFromAssembly(typeof(FreePlugins.DataAccessExamplePlugin.ContextInfoPlugin).Assembly);

        builder.Services.AddTransient<CompiledPluginExecutor>();

        return builder;
    }
}
