/*
    Purpose: Persistence abstraction for the UserPreferences feature.
    Fits: Lets the service keep one API while hosts choose in-memory or durable storage behind DI.
*/
namespace FreeBlazorExtended.UserPreferences;

/// <summary>
/// Persistence abstraction for <see cref="UserPreferencesService"/>.
/// The service ships with an in-memory default; server hosts that have
/// EF Core (and a connection string) register a SQL-backed implementation
/// via DI to swap persistence without touching the service or callers.
/// </summary>
public interface IUserPreferencesStore
{
    Task<UserPreferences?> GetAsync(Guid TenantId, Guid UserId);
    Task SaveAsync(UserPreferences prefs);
    Task DeleteAsync(Guid TenantId, Guid UserId);
}
