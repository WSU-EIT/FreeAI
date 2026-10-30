using Microsoft.Extensions.Configuration.Json;
using System.Collections.Concurrent;

namespace FreeBlazorExample;

// ---- Program ----

// FreeBlazorExample startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads FreeBlazorExample's own settings files (appsettings.FreeBlazorExample.App.json and its environment variant),
    /// so the stock appsettings files stay untouched. They go right after the stock appsettings files: they override those,
    /// while user secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeBlazorExampleAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeBlazorExample.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeBlazorExample.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }

    /// <summary>
    /// Registers the FreeBlazorExtended showcase services. Called from AppModifyBuilderEnd.
    /// </summary>
    public static WebApplicationBuilder FreeBlazorExampleAppModifyBuilderEnd(WebApplicationBuilder builder)
    {
        // FreeBlazorExtended showcase services (Features 100-114)
        // Registered server-side too so prerender / InteractiveAuto can resolve them
        builder.Services.AddScoped<FreeBlazorExtended.Foundation.Services.NotificationService>();
        builder.Services.AddScoped<FreeBlazorExtended.DynamicForms.FormService>();
        // Feature 104 — wire EF Core-backed store so prefs persist to SQL on the server.
        // (WASM client falls back to in-memory via the parameterless ctor.)
        builder.Services.AddScoped<FreeBlazorExtended.UserPreferences.IUserPreferencesStore, FreeBlazorExampleEFUserPreferencesStore>();
        builder.Services.AddScoped<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
        builder.Services.AddScoped<FreeBlazorExtended.Calendar.CalendarEventService>();
        builder.Services.AddScoped<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();
        builder.Services.AddScoped<FreeBlazorExtended.AgentMonitoring.IAgentCommandDispatcher, FreeBlazorExtended.AgentMonitoring.SignalRAgentCommandDispatcher>();
        builder.Services.AddScoped<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();
        builder.Services.AddScoped<FreeBlazorExtended.HierarchicalTree.TreeService>();

        // External API session store — holds credentials server-side, keyed by HttpOnly cookie
        builder.Services.AddSingleton<FreeBlazorExampleExternalApiSessionStore>();

        return builder;
    }

    /// <summary>
    /// Maps the FreeBlazorExtended SignalR hubs. Called from AppModifyEnd (endpoint order does not matter).
    /// </summary>
    public static WebApplication FreeBlazorExampleAppModifyEnd(WebApplication app)
    {
        app.MapHub<FreeBlazorExtended.MultiViewSync.PresentationHub>("/presentationHub");

        app.MapHub<FreeBlazorExtended.AgentMonitoring.AgentHub>("/agentHub");

        return app;
    }
}

// ---- Services.ExternalApiSession ----

/*
    Role: Server-side session store for external API credentials.
    Purpose: Holds API tokens (Smartsheet, Git platforms) in server memory, keyed by
             a random session ID that is set as an HttpOnly cookie on the client.
             Credentials never leave the server after the initial /connect call.
    Security:
      - Cookie is HttpOnly + SameSite=Strict (JS cannot read it)
      - Tokens are never returned to the browser
      - Sessions expire after a sliding 2-hour window
      - Each connect call issues a new session key (no reuse)
    Lifetime: Singleton — one store shared across all server requests.
*/

public sealed class FreeBlazorExampleExternalApiSessionStore
{
    public const string CookieName = "_fbe_ext";
    public const string CookiePath = "/api/Showcase";
    private static readonly TimeSpan SlidingExpiry = TimeSpan.FromHours(2);

    private readonly ConcurrentDictionary<string, FreeBlazorExampleExternalApiSession> _store = new();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Prunes all expired sessions. Called automatically on Create.</summary>
    private void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _store)
            if (kv.Value.ExpiresAt < now)
                _store.TryRemove(kv.Key, out _);
    }

    // -------------------------------------------------------------------------
    // CRUD
    // -------------------------------------------------------------------------

    /// <summary>Creates a new session and returns the opaque session key for the cookie.</summary>
    public string Create(Action<FreeBlazorExampleExternalApiSession> configure)
    {
        var session = new FreeBlazorExampleExternalApiSession { ExpiresAt = DateTime.UtcNow.Add(SlidingExpiry) };
        configure(session);

        var key = Guid.NewGuid().ToString("N");
        _store[key] = session;
        Cleanup();
        return key;
    }

    /// <summary>Returns the session if it exists and has not expired; null otherwise.</summary>
    public FreeBlazorExampleExternalApiSession? Get(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (!_store.TryGetValue(key, out var session)) return null;
        if (session.ExpiresAt < DateTime.UtcNow) { _store.TryRemove(key, out _); return null; }

        // Slide expiry on access
        session.ExpiresAt = DateTime.UtcNow.Add(SlidingExpiry);
        return session;
    }

    /// <summary>Issues the HttpOnly session cookie for a key returned by <see cref="Create"/>.</summary>
    public void IssueCookie(HttpRequest request, HttpResponse response, string sessionKey)
    {
        response.Cookies.Append(CookieName, sessionKey, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure   = request.IsHttps,
            MaxAge   = TimeSpan.FromHours(2),
            Path     = CookiePath
        });
    }

    /// <summary>Removes the caller's session and deletes its cookie (disconnect).</summary>
    public void ClearCookie(HttpRequest request, HttpResponse response)
    {
        var key = request.Cookies[CookieName];
        Remove(key);
        response.Cookies.Delete(CookieName, new CookieOptions { Path = CookiePath });
    }

    /// <summary>Looks up the session named by the request's cookie; null when absent or expired.</summary>
    public FreeBlazorExampleExternalApiSession? GetSession(HttpRequest request)
    {
        return Get(request.Cookies[CookieName]);
    }

    /// <summary>Removes the session immediately (disconnect / logout).</summary>
    public void Remove(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _store.TryRemove(key, out _);
    }
}

/// <summary>
/// Holds credentials for a single external API session.
/// Fields are nullable — a session may have Smartsheet OR Git credentials OR both.
/// </summary>
public sealed class FreeBlazorExampleExternalApiSession
{
    public DateTime ExpiresAt { get; set; }
    public string? GitBaseApiUrl   { get; set; }
    public string? GitOwner        { get; set; }
    public string? GitPlatform     { get; set; }  // enum name string
    public string? GitRepo         { get; set; }

    // Git (shared between GitHubRepoBrowser and GenericGitRepoBrowser) ---
    public string? GitRepoUrl      { get; set; }
    public string? GitToken        { get; set; }
    public string? GitUsername     { get; set; }
    // Smartsheet ---
    public string? SmartsheetToken { get; set; }
}
