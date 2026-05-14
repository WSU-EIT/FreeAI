/*
    Purpose: UserPreferences model bundle for persisted UI, culture, and layout settings.
    Contains: Theme and density enums plus the per-user preference shape stored by the feature service.
    Used by: UserPreferencesService and any host screen that reads or saves per-user settings.
*/
namespace FreeBlazorExtended.UserPreferences;

public enum ThemeMode
{
    Auto,
    Light,
    Dark
}

public enum UIDensity
{
    Compact,
    Comfortable,
    Spacious
}

public class UserPreferences
{
    /// <summary>Unique identifier for this preference record. Generated on creation.</summary>
    public Guid UserPreferencesId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant that owns these preferences; used to scope reads and writes in multi-tenant hosts.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The user whose preferences this record describes. Paired with <see cref="TenantId"/> as the logical key.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Colour-scheme preference. <see cref="ThemeMode.Auto"/> respects the OS/browser
    /// prefers-color-scheme media query. The host layout applies this value via a CSS class
    /// on the root element.
    /// </summary>
    public ThemeMode Theme { get; set; } = ThemeMode.Auto;

    /// <summary>
    /// BCP-47 culture tag (e.g. <c>"en-US"</c>, <c>"fr-FR"</c>) used to format dates,
    /// numbers, and currency throughout the UI. Passed to <c>CultureInfo.GetCultureInfo</c>
    /// by the host during render.
    /// </summary>
    public string CultureCode { get; set; } = "en-US";

    /// <summary>
    /// Browser zoom level as a percentage (50–200). Applied as a CSS <c>zoom</c> or
    /// <c>font-size</c> scale on the root layout so the user can scale the entire UI
    /// without using browser zoom (which can break fixed layouts).
    /// </summary>
    public int Zoom { get; set; } = 100;

    /// <summary>
    /// Controls the vertical rhythm and padding density of UI elements. Compact reduces
    /// row height and padding for power users; Spacious increases breathing room for
    /// accessibility or touch targets.
    /// </summary>
    public UIDensity Density { get; set; } = UIDensity.Comfortable;

    /// <summary>
    /// Persisted pixel width of the left sidebar/panel in layouts that support resizable panels.
    /// Restored on next load so the user's preferred panel proportions are remembered.
    /// </summary>
    public decimal LayoutPanelLeftWidth { get; set; } = 300;

    /// <summary>
    /// Persisted pixel width of the right sidebar/panel in layouts that support resizable panels.
    /// Restored on next load alongside <see cref="LayoutPanelLeftWidth"/>.
    /// </summary>
    public decimal LayoutPanelRightWidth { get; set; } = 300;

    /// <summary>
    /// <c>true</c> when the user has collapsed the main navigation sidebar.
    /// The layout reads this on initial render to avoid the sidebar flashing open
    /// before the preference is applied.
    /// </summary>
    public bool SidebarCollapsed { get; set; }

    /// <summary>
    /// JSON dictionary of entity-scoped preferences keyed by <c>"entityType:entityId"</c>.
    /// Stores arbitrary per-record UI state (column widths, last-selected tab, etc.) without
    /// requiring a separate database row per entity.
    /// Format: <c>{ "project:abc123": { "activeTab": "files" } }</c>
    /// </summary>
    public string PerEntityJson { get; set; } = "{}";

    /// <summary>
    /// JSON array of recently accessed item IDs in visit order (newest first).
    /// Used to populate "Recent" navigation lists. The service caps this list at a
    /// configured maximum (default 20) and trims the oldest entries automatically.
    /// Format: <c>["guid1","guid2",...]</c>
    /// </summary>
    public string RecentItemsJson { get; set; } = "[]";

    /// <summary>UTC timestamp of when this preference record was first created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user or process that created this record.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update to any preference in this record.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user or process that last updated this record.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from preference lookups when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}
