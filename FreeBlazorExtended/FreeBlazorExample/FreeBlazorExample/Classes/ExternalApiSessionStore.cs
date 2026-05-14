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
using System.Collections.Concurrent;

namespace FreeBlazorExample;

public sealed class ExternalApiSessionStore
{
    public const string CookieName = "_fbe_ext";
    private static readonly TimeSpan SlidingExpiry = TimeSpan.FromHours(2);

    private readonly ConcurrentDictionary<string, ExternalApiSession> _store = new();

    // -------------------------------------------------------------------------
    // CRUD
    // -------------------------------------------------------------------------

    /// <summary>Creates a new session and returns the opaque session key for the cookie.</summary>
    public string Create(Action<ExternalApiSession> configure)
    {
        var session = new ExternalApiSession { ExpiresAt = DateTime.UtcNow.Add(SlidingExpiry) };
        configure(session);

        var key = Guid.NewGuid().ToString("N");
        _store[key] = session;
        Cleanup();
        return key;
    }

    /// <summary>Returns the session if it exists and has not expired; null otherwise.</summary>
    public ExternalApiSession? Get(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (!_store.TryGetValue(key, out var session)) return null;
        if (session.ExpiresAt < DateTime.UtcNow) { _store.TryRemove(key, out _); return null; }

        // Slide expiry on access
        session.ExpiresAt = DateTime.UtcNow.Add(SlidingExpiry);
        return session;
    }

    /// <summary>Removes the session immediately (disconnect / logout).</summary>
    public void Remove(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _store.TryRemove(key, out _);
    }

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
}

/// <summary>
/// Holds credentials for a single external API session.
/// Fields are nullable — a session may have Smartsheet OR Git credentials OR both.
/// </summary>
public sealed class ExternalApiSession
{
    // Smartsheet ---
    public string? SmartsheetToken { get; set; }

    // Git (shared between GitHubRepoBrowser and GenericGitRepoBrowser) ---
    public string? GitRepoUrl      { get; set; }
    public string? GitToken        { get; set; }
    public string? GitUsername     { get; set; }
    public string? GitPlatform     { get; set; }  // enum name string
    public string? GitOwner        { get; set; }
    public string? GitRepo         { get; set; }
    public string? GitBaseApiUrl   { get; set; }

    public DateTime ExpiresAt { get; set; }
}
