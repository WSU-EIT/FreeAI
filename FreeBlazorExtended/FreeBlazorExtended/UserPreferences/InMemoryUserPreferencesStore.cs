/*
    Purpose: Default in-memory persistence store for UserPreferences.
    Fits: Used by WASM, tests, and any host that has not registered a durable IUserPreferencesStore implementation.
*/
using System.Collections.Concurrent;

namespace FreeBlazorExtended.UserPreferences;

/// <summary>
/// Default <see cref="IUserPreferencesStore"/> backed by a process-local
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Used by the WASM client
/// (no SQL connection) and as a fallback when no EF store is registered.
/// State is lost on app restart — wire up an EF-backed store on the server.
/// </summary>
public class InMemoryUserPreferencesStore : IUserPreferencesStore
{
    private static readonly ConcurrentDictionary<(Guid TenantId, Guid UserId), UserPreferences> _preferences = new();

    public Task<UserPreferences?> GetAsync(Guid TenantId, Guid UserId)
    {
        _preferences.TryGetValue((TenantId, UserId), out var prefs);
        return Task.FromResult(prefs);
    }

    public Task SaveAsync(UserPreferences prefs)
    {
        _preferences[(prefs.TenantId, prefs.UserId)] = prefs;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid TenantId, Guid UserId)
    {
        _preferences.TryRemove((TenantId, UserId), out _);
        return Task.CompletedTask;
    }

    /// <summary>Test/teardown helper — clears every cached row across tenants.</summary>
    public static void ClearAll()
    {
        _preferences.Clear();
    }
}
