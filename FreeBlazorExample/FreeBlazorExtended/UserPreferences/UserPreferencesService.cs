/*
    Role: UserPreferences feature service.
    Purpose: Owns per-user preference retrieval, default creation, JSON-backed entity preferences, and recent-item history.
    Host expectations: Delegates persistence to IUserPreferencesStore so hosts can swap between in-memory and durable storage through DI.
*/
using System.Text.Json;

namespace FreeBlazorExtended.UserPreferences;

/// <summary>
/// Per-(tenant, user) preferences service. Delegates persistence to an
/// <see cref="IUserPreferencesStore"/> — the default in-memory store is
/// used when no other store is registered (WASM client, tests). The
/// FreeBlazorExample server registers an EF Core-backed store so prefs
/// survive restarts and roam across devices.
///
/// Public method signatures are preserved from the original in-memory
/// implementation; existing callers (e.g. Feature104 showcase page) need
/// no changes.
/// </summary>
public partial class UserPreferencesService
{
    private readonly IUserPreferencesStore _store;

    /// <summary>True when running against the in-memory fallback (WASM, tests).</summary>
    public bool UseInMemory { get; }

    /// <summary>
    /// Parameterless ctor — used by WASM Singleton registration and any host
    /// that has not registered a custom <see cref="IUserPreferencesStore"/>.
    /// Defaults to the in-memory store.
    /// </summary>
    public UserPreferencesService()
        : this(new InMemoryUserPreferencesStore())
    {
    }

    /// <summary>
    /// DI ctor — server hosts inject a real <see cref="IUserPreferencesStore"/>
    /// (e.g. EFUserPreferencesStore in FreeBlazorExample.DataAccess).
    /// </summary>
    public UserPreferencesService(IUserPreferencesStore store)
    {
        _store = store ?? new InMemoryUserPreferencesStore();
        UseInMemory = _store is InMemoryUserPreferencesStore;
    }

    public async Task<UserPreferences> GetUserPreferences(Guid TenantId, Guid UserId)
    {
        var prefs = await _store.GetAsync(TenantId, UserId);

        if (prefs != null) {
            return prefs;
        }

        // Create default preferences if not found
        var newPrefs = new UserPreferences
        {
            TenantId = TenantId,
            UserId = UserId
        };

        await _store.SaveAsync(newPrefs);
        return newPrefs;
    }

    public async Task<UserPreferences> SaveUserPreferences(UserPreferences prefs, string? UserId = null)
    {
        if (prefs.UserPreferencesId == Guid.Empty)
            prefs.UserPreferencesId = Guid.NewGuid();

        prefs.LastModified = DateTime.UtcNow;
        prefs.LastModifiedBy = UserId;

        await _store.SaveAsync(prefs);
        return prefs;
    }

    public async Task<T?> GetPerEntityPreference<T>(Guid TenantId, Guid UserId, string EntityKey) where T : class
    {
        var prefs = await _store.GetAsync(TenantId, UserId);

        if (prefs == null)
            return null;

        try {
            var perEntity = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(prefs.PerEntityJson) ?? new();

            if (perEntity.TryGetValue(EntityKey, out var element)) {
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
        }
        catch (Exception) {

            // JSON parse failed; treat as malformed data and fall through to default.

        }

        return null;
    }

    public async Task SetPerEntityPreference(Guid TenantId, Guid UserId, string EntityKey, object value, string? ModifiedBy = null)
    {
        var prefs = await GetUserPreferences(TenantId, UserId);

        try {
            var perEntity = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(prefs.PerEntityJson) ?? new();
            perEntity[EntityKey] = JsonSerializer.SerializeToElement(value);
            prefs.PerEntityJson = JsonSerializer.Serialize(perEntity);
        }
        catch (Exception) {

            // JSON parse failed; treat as malformed data and fall through to default.

        }

        await SaveUserPreferences(prefs, ModifiedBy);
    }

    public async Task AddRecentItem(Guid TenantId, Guid UserId, string ItemId, string? ModifiedBy = null)
    {
        var prefs = await GetUserPreferences(TenantId, UserId);

        try {
            var recent = JsonSerializer.Deserialize<List<string>>(prefs.RecentItemsJson) ?? new();

            // Remove if already exists, then add to front
            recent.Remove(ItemId);
            recent.Insert(0, ItemId);

            // Keep only last 20 items
            if (recent.Count > 20)
                recent = recent.Take(20).ToList();

            prefs.RecentItemsJson = JsonSerializer.Serialize(recent);
        }
        catch (Exception) {

            // JSON parse failed; treat as malformed data and fall through to default.

        }

        await SaveUserPreferences(prefs, ModifiedBy);
    }

    public async Task<List<string>> GetRecentItems(Guid TenantId, Guid UserId)
    {
        var prefs = await _store.GetAsync(TenantId, UserId);

        if (prefs == null)
            return new List<string>();

        try {
            var recent = JsonSerializer.Deserialize<List<string>>(prefs.RecentItemsJson) ?? new();
            return recent;
        }
        catch {
            return new List<string>();
        }
    }

    public async Task ResetUserPreferences(Guid TenantId, Guid UserId, string? ModifiedBy = null)
    {
        // Delete the existing row (if any), then write a fresh defaults row.
        await _store.DeleteAsync(TenantId, UserId);

        var prefs = new UserPreferences
        {
            TenantId = TenantId,
            UserId = UserId,
            LastModifiedBy = ModifiedBy
        };

        await SaveUserPreferences(prefs, ModifiedBy);
    }

    public void ClearAllPreferences()
    {
        // Only meaningful for the in-memory store. For the EF-backed store this
        // is a no-op; production callers should not be wiping every tenant's prefs
        // anyway. Test harnesses that need a clean slate should use the
        // InMemoryUserPreferencesStore static helper directly.
        InMemoryUserPreferencesStore.ClearAll();
    }
}
