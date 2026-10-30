using Microsoft.Extensions.Configuration.Json;

namespace FreeSmartsheets;

public partial class Program
{
    /// <summary>
    /// Loads FreeSmartsheets' own settings files (appsettings.FreeSmartsheets.App.json and its environment variant), so the
    /// stock appsettings files stay untouched. They go right after the stock appsettings files: they override those, while
    /// user secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeSmartsheetsAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeSmartsheets.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeSmartsheets.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }
}
