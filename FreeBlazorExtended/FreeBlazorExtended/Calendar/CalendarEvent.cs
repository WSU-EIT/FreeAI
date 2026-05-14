/*
    Purpose: Calendar model bundle for event scheduling, recurrence, and lifecycle state.
    Contains: Event status and recurrence enums plus the persisted event shape used by the calendar feature.
    Used by: Calendar services and UI surfaces that schedule, query, and render events.
*/
namespace FreeBlazorExtended.Calendar;

public enum EventStatus
{
    Tentative,
    Confirmed,
    Cancelled
}

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class CalendarEvent
{
    /// <summary>Unique identifier for this calendar event. Generated on creation.</summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits visibility to events belonging to this tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Short display title shown on the calendar tile and in list views.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description; shown in the event detail panel or tooltip.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Event start time in UTC. When <see cref="IsAllDay"/> is <c>true</c>, the calendar
    /// renders only the date portion and ignores the time component.
    /// </summary>
    public DateTime StartUtc { get; set; }

    /// <summary>
    /// Event end time in UTC. Must be greater than <see cref="StartUtc"/>.
    /// Ignored for visual purposes when <see cref="IsAllDay"/> is <c>true</c>.
    /// </summary>
    public DateTime EndUtc { get; set; }

    /// <summary>
    /// When <c>true</c>, the event spans the full day and the time portion of
    /// <see cref="StartUtc"/> / <see cref="EndUtc"/> is not displayed. The calendar
    /// renders all-day events in a dedicated banner row above timed slots.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// Optional reference to a <see cref="ScheduleResource"/> (room, equipment, etc.)
    /// that this event reserves. Used by the scheduling service to detect conflicts.
    /// <c>null</c> means no specific resource is required.
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Display name of the person or team who owns or created this event.
    /// Shown in the event detail panel; not a foreign key — stored as a snapshot string.
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Optional CSS color string (e.g. <c>"#3b82f6"</c> or <c>"var(--bs-warning)"</c>)
    /// applied to the event tile on the calendar. Falls back to the calendar's default
    /// category color when <c>null</c>.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// JSON-serialized <see cref="RecurrenceRule"/> describing how this event repeats.
    /// <c>null</c> means the event is a one-time occurrence. When set, the service expands
    /// occurrences on demand — only this root event row is persisted.
    /// </summary>
    public string? RecurrenceRuleJson { get; set; }

    /// <summary>
    /// When this event is a modified instance of a recurring series (i.e.
    /// <see cref="IsException"/> is <c>true</c>), this points to the root event's
    /// <see cref="EventId"/>. <c>null</c> for standalone or root recurring events.
    /// </summary>
    public Guid? ParentEventId { get; set; }

    /// <summary>
    /// <c>true</c> when this event record overrides one specific occurrence of a
    /// recurring series (e.g., a rescheduled or cancelled instance). The parent series
    /// is identified by <see cref="ParentEventId"/>. The expanded occurrence for this
    /// date is suppressed in favour of this record.
    /// </summary>
    public bool IsException { get; set; }

    /// <summary>
    /// Lifecycle state of the event. Defaults to <see cref="EventStatus.Confirmed"/>.
    /// <c>Tentative</c> events are shown with a visual indicator; <c>Cancelled</c> events
    /// are greyed out and excluded from resource-conflict checks.
    /// </summary>
    public EventStatus Status { get; set; } = EventStatus.Confirmed;

    /// <summary>UTC timestamp of when this record was first created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;

    /// <summary>Display name of the user who created this record.</summary>
    public string? AddedBy { get; set; }

    /// <summary>UTC timestamp of the most recent update to this record.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>Display name of the user who last modified this record.</summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Soft-delete flag. When <c>true</c>, the event is excluded from all calendar
    /// queries and the UI without removing the database row.
    /// </summary>
    public bool Deleted { get; set; }
}

public class RecurrenceRule
{
    /// <summary>
    /// How often the event repeats — Daily, Weekly, Monthly, or Yearly.
    /// Combined with <see cref="Interval"/> to determine the gap between occurrences
    /// (e.g., <c>Weekly</c> + <c>Interval = 2</c> = every other week).
    /// </summary>
    public RecurrenceFrequency Frequency { get; set; }

    /// <summary>
    /// Multiplier applied to <see cref="Frequency"/>. Default <c>1</c> means every
    /// occurrence; <c>2</c> means every second occurrence (bi-weekly, bi-monthly, etc.).
    /// </summary>
    public int Interval { get; set; } = 1;

    /// <summary>
    /// For <c>Weekly</c> recurrence: the days on which the event fires,
    /// expressed as three-letter abbreviations (e.g. <c>["Mon","Wed","Fri"]</c>).
    /// Empty list means the event fires only on the day-of-week of the original start date.
    /// </summary>
    public List<string> ByDayOfWeek { get; set; } = new();

    /// <summary>
    /// For <c>Monthly</c> recurrence: the calendar days (1–31) on which the event fires.
    /// Empty list means the event fires on the same day-of-month as the original start date.
    /// </summary>
    public List<int> ByDayOfMonth { get; set; } = new();

    /// <summary>
    /// Hard end date for the recurrence. When set, no occurrences are generated after
    /// this date. Mutually exclusive with <see cref="Count"/> — set one or neither.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Maximum number of occurrences to generate. When set, expansion stops after this
    /// many instances. Mutually exclusive with <see cref="Until"/> — set one or neither.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// ISO 8601 date strings (e.g. <c>"2025-12-25"</c>) for specific occurrences that
    /// should be skipped during expansion. Used for holidays, overridden instances, etc.
    /// </summary>
    public List<string> Exceptions { get; set; } = new();
}

public class ScheduleResource
{
    /// <summary>Unique identifier for this bookable resource. Generated on creation.</summary>
    public Guid ResourceId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits resource visibility to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Human-readable label shown in the resource picker (e.g. "Conference Room A").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of resource. Drives icon selection and filtering in the UI.
    /// Common values: <c>"Room"</c>, <c>"Equipment"</c>, <c>"Person"</c>.
    /// </summary>
    public string Type { get; set; } = "Room";

    /// <summary>
    /// Maximum number of simultaneous bookings allowed for this resource.
    /// <c>null</c> means unlimited (e.g., a shared projector that multiple events may reference).
    /// The scheduling service enforces this limit during conflict checks.
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>UTC timestamp of when this resource record was created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who created this resource.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this resource.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from all queries when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}

public enum PetitionStatus
{
    Pending,
    Approved,
    Denied
}

public class SchedulingPetition
{
    /// <summary>Unique identifier for this petition request. Generated on creation.</summary>
    public Guid PetitionId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant scope; limits petition visibility to the owning tenant.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The <see cref="CalendarEvent.EventId"/> of the event the petitioner wants to schedule.
    /// The approval workflow resolves conflicts against this event before confirming it.
    /// </summary>
    public Guid RequestedEventId { get; set; }

    /// <summary>Display name or email of the person who submitted the booking request.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// The petitioner's written justification for why the booking should be approved.
    /// Shown to approvers in the review panel.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Current review state. Starts as <see cref="PetitionStatus.Pending"/> until an
    /// approver acts. Drives the badge colour and available actions in the dashboard.
    /// </summary>
    public PetitionStatus Status { get; set; } = PetitionStatus.Pending;

    /// <summary>Display name of the user who approved or denied the petition.</summary>
    public string? ApproverName { get; set; }

    /// <summary>UTC timestamp of when the approver acted on the petition.</summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Free-text note from the approver explaining the decision — especially useful
    /// when the petition is denied so the requester understands what to change.
    /// </summary>
    public string? ApprovalComments { get; set; }

    /// <summary>UTC timestamp of when this petition record was created.</summary>
    public DateTime Added { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who submitted this petition.</summary>
    public string? AddedBy { get; set; }
    /// <summary>UTC timestamp of the most recent update to this petition.</summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    /// <summary>Display name of the user who last modified this petition record.</summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>Soft-delete flag; excluded from all queries when <c>true</c>.</summary>
    public bool Deleted { get; set; }
}
