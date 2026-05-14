/*
    Purpose: MultiViewSync model bundle for synchronized presentation sessions.
    Contains: Session, item, and message contracts that describe shared presentation state across clients.
    Used by: RealtimeSyncService, PresentationHub, and the feature UI surfaces.
*/
namespace FreeBlazorExtended.MultiViewSync;

public class PresentationSession
{
    /// <summary>Unique identifier for this session. Used as the SignalR group name so all connected clients share the same broadcast group.</summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits session visibility and client join access to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Human-readable session title shown in the session picker and the presenter dashboard header.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ordered list of content items available in this session. The presenter advances
    /// through these items; audience screens display whichever item is currently active.
    /// </summary>
    public List<SessionItem> Items { get; set; } = new();

    /// <summary>
    /// The <see cref="SessionItem.ItemId"/> of the item currently shown to audience screens.
    /// <c>null</c> means no item is active (session is at the start or end state).
    /// Updated by the presenter via <c>RealtimeSyncService.SetActiveItemAsync</c>, which
    /// broadcasts the change to all connected audience clients.
    /// </summary>
    public Guid? ActiveItemId { get; set; }

    /// <summary>
    /// <c>true</c> when the presenter has activated the "blank screen" mode.
    /// Audience screens render a solid black overlay that hides all content without
    /// changing the active item — useful between segments or during Q&amp;A.
    /// </summary>
    public bool IsBlanked { get; set; }

    /// <summary>
    /// <c>true</c> when the presenter has hidden all text overlays on audience screens
    /// while still showing the background image or content. Used for media-heavy slides
    /// where the presenter wants to speak over visuals without text distraction.
    /// </summary>
    public bool IsTextHidden { get; set; }

    /// <summary>
    /// <c>true</c> when the session is actively broadcasting. Audience join links are
    /// only active when <c>IsLive = true</c>. The presenter toggles this to start/end
    /// the session; toggling to <c>false</c> clears all connected audience clients.
    /// </summary>
    public bool IsLive { get; set; }

    /// <summary>UTC timestamp of when this session was first created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who created this session.</summary>
    public string? CreatedBy { get; set; }
    /// <summary>UTC timestamp of when this record was inserted (mirrors <see cref="CreatedAt"/> for audit-trail consistency).</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who inserted this record.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update to the session record.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this session record.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from session lists and join lookups when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public class SessionItem
{
    /// <summary>Unique identifier for this content item within the session. Referenced by <see cref="PresentationSession.ActiveItemId"/>.</summary>
    public Guid ItemId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display order of this item in the presenter's slide list. Lower values appear first.
    /// The presenter reorders items by dragging; the service updates these values on save.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Content type that tells audience screens which renderer to use.
    /// Accepted values: <c>"text"</c>, <c>"image"</c>, <c>"html"</c>, <c>"countdown"</c>, <c>"message"</c>.
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// For text/html items: the primary content or HTML string.
    /// For image items: the image URL or base-64 data URI.
    /// For countdown items: the ISO 8601 target datetime string.
    /// For message items: the message text to flash across screens.
    /// </summary>
    public string TitleOrPayload { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON blob carrying additional structured content specific to the item type
    /// (e.g. animation settings for countdown, layout options for HTML slides).
    /// <c>null</c> for simple text and image items.
    /// </summary>
    public string? ContentJson { get; set; }

    /// <summary>
    /// Optional JSON blob carrying display style overrides for this item
    /// (e.g. background color, font size, transition effect).
    /// Applied by the audience screen renderer on top of the session's default theme.
    /// </summary>
    public string? StyleJson { get; set; }
}

public class PresentationMessage
{
    /// <summary>
    /// Which connected client types should receive this message.
    /// Accepted values: <c>"tablet"</c> (presenter tablet only), <c>"screen"</c> (audience screens only),
    /// <c>"all"</c> (every connected client). Routed by <c>PresentationHub</c> at dispatch time.
    /// </summary>
    public string Target { get; set; } = "all";

    /// <summary>The notification text displayed as an overlay banner on the targeted screens.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Visual severity of the banner. Drives Bootstrap colour class selection on the audience screen.
    /// Accepted values: <c>"info"</c>, <c>"warning"</c>, <c>"error"</c>.
    /// </summary>
    public string Style { get; set; } = "info";

    /// <summary>
    /// How long the message banner remains visible on audience screens before automatically dismissing.
    /// Default is 5 seconds. Set to <c>0</c> for a sticky banner that requires manual dismissal.
    /// </summary>
    public int DurationMs { get; set; } = 5000;

    /// <summary>
    /// <c>true</c> while the message is currently displayed on targeted screens.
    /// Set to <c>false</c> by the hub when <see cref="DurationMs"/> elapses or the presenter
    /// manually clears the message.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
