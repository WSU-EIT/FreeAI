using Microsoft.Extensions.Configuration.Json;

namespace ChatWithAI;

// ChatWithAI startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads ChatWithAI's own settings files (appsettings.ChatWithAI.App.json and its environment variant), so the stock
    /// appsettings files stay untouched. They go right after the stock appsettings files: they override those, while user
    /// secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder ChatWithAIAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.ChatWithAI.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.ChatWithAI.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }
}
