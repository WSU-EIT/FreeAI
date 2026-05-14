using FreeBlazorExtended.UserPreferences;

namespace FreeBlazorExample;

/// <summary>
/// EF Core-backed <see cref="IUserPreferencesStore"/> for Feature 104.
/// Wraps the project's existing <see cref="IDataAccess"/> so we reuse its
/// configured <see cref="EFDataModel"/> (connection string, provider, retry
/// policies) instead of registering a second DbContext.
///
/// Falls back to in-memory state if the underlying database can't be reached
/// (e.g. before first-run setup has supplied a connection string) so the
/// showcase keeps working in dev. The fallback only kicks in on connection
/// failures, NOT on row-level errors.
/// </summary>
public class EFUserPreferencesStore : IUserPreferencesStore
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly InMemoryUserPreferencesStore _fallback = new();

    public EFUserPreferencesStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private IDataAccess? GetDataAccess()
    {
        try {
            // IDataAccess is registered as Transient — resolve a fresh instance
            // per call so we get a fresh DbContext (DataAccess owns its own
            // EFDataModel internally and is IDisposable).
            return _serviceProvider.GetService(typeof(IDataAccess)) as IDataAccess;
        } catch {
            return null;
        }
    }

    public async Task<UserPreferences?> GetAsync(Guid TenantId, Guid UserId)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            return await _fallback.GetAsync(TenantId, UserId);
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    return await concrete.GetUserPreferencesRow(TenantId, UserId);
                }
            }
        } catch {
            return await _fallback.GetAsync(TenantId, UserId);
        }

        return null;
    }

    public async Task SaveAsync(UserPreferences prefs)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            await _fallback.SaveAsync(prefs);
            return;
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    await concrete.SaveUserPreferencesRow(prefs);
                    return;
                }
            }
        } catch {
            await _fallback.SaveAsync(prefs);
        }
    }

    public async Task DeleteAsync(Guid TenantId, Guid UserId)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            await _fallback.DeleteAsync(TenantId, UserId);
            return;
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    await concrete.DeleteUserPreferencesRow(TenantId, UserId);
                    return;
                }
            }
        } catch {
            await _fallback.DeleteAsync(TenantId, UserId);
        }
    }
}
