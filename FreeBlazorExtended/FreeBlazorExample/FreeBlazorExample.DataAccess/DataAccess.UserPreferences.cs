using FreeBlazorExtended.UserPreferences;

namespace FreeBlazorExample;

public partial class DataAccess
{
    /// <summary>
    /// Loads the FreeBlazorExtended Feature 104 UserPreferences row for the
    /// given (tenant, user). Returns null if no row exists yet.
    /// AsNoTracking — callers mutate and re-save via <see cref="SaveUserPreferencesRow"/>.
    /// </summary>
    public async Task<UserPreferences?> GetUserPreferencesRow(Guid TenantId, Guid UserId)
    {
        return await data.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == TenantId && p.UserId == UserId);
    }

    /// <summary>
    /// Upserts a UserPreferences row. Look-up is by (TenantId, UserId)
    /// because UserPreferencesId is generated client-side and may differ
    /// between the in-memory copy and the persisted one.
    /// </summary>
    public async Task<UserPreferences> SaveUserPreferencesRow(UserPreferences prefs)
    {
        var existing = await data.UserPreferences
            .FirstOrDefaultAsync(p => p.TenantId == prefs.TenantId && p.UserId == prefs.UserId);

        if (existing == null) {
            if (prefs.UserPreferencesId == Guid.Empty) {
                prefs.UserPreferencesId = Guid.NewGuid();
            }
            if (prefs.Added == default) {
                prefs.Added = DateTime.UtcNow;
            }
            data.UserPreferences.Add(prefs);
        } else {
            existing.Theme = prefs.Theme;
            existing.CultureCode = prefs.CultureCode;
            existing.Zoom = prefs.Zoom;
            existing.Density = prefs.Density;
            existing.LayoutPanelLeftWidth = prefs.LayoutPanelLeftWidth;
            existing.LayoutPanelRightWidth = prefs.LayoutPanelRightWidth;
            existing.SidebarCollapsed = prefs.SidebarCollapsed;
            existing.PerEntityJson = prefs.PerEntityJson;
            existing.RecentItemsJson = prefs.RecentItemsJson;
            existing.LastModified = prefs.LastModified == default ? DateTime.UtcNow : prefs.LastModified;
            existing.LastModifiedBy = prefs.LastModifiedBy;
            existing.Deleted = prefs.Deleted;
            // Reflect the canonical PK back to the caller's instance.
            prefs.UserPreferencesId = existing.UserPreferencesId;
        }

        await data.SaveChangesAsync();
        return prefs;
    }

    /// <summary>
    /// Hard-deletes the UserPreferences row for the given (tenant, user).
    /// Used by <see cref="UserPreferencesService.ResetUserPreferences"/> so a
    /// reset really clears state rather than leaving stale audit fields.
    /// </summary>
    public async Task DeleteUserPreferencesRow(Guid TenantId, Guid UserId)
    {
        var existing = await data.UserPreferences
            .FirstOrDefaultAsync(p => p.TenantId == TenantId && p.UserId == UserId);

        if (existing != null) {
            data.UserPreferences.Remove(existing);
            await data.SaveChangesAsync();
        }
    }
}
