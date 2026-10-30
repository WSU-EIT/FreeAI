using Microsoft.Extensions.Configuration.Json;

namespace FreeLLM;

// FreeLLM startup code, called from one line in the hook in Program.App.cs.
// Program.cs and the appsettings files stay stock FreeCRM so a framework upgrade can overwrite them.

public partial class Program
{
    /// <summary>
    /// Loads FreeLLM's own settings files (appsettings.FreeLLM.App.json and its environment variant), so the stock
    /// appsettings files stay untouched. They go right after the stock appsettings files: they override those, while user
    /// secrets, environment variables and the command line still override them. Then it pins the web host to loopback.
    /// Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeLLMAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeLLM.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeLLM.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        // SECURITY: the file reader (AppAdmin endpoints that read arbitrary server paths) is kept off the network only
        // by this binding. Do not remove it or change the address. (Development: appsettings.FreeLLM.App.Development.json
        // sets a Kestrel endpoint that must stay on localhost as well.)
        // always run on 55000
        builder.WebHost.UseUrls("http://127.0.0.1:55000");

        return builder;
    }
}
