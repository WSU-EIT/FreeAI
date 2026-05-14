/*
    Purpose: In-memory calendar event service for scheduling and reservation workflows.
    Key behaviors: Stores events, queries by range or resource, and supports recurrence-aware scheduling operations.
    Dependencies: Uses in-memory concurrent state only; production hosts can replace persistence behind the same service boundary.
*/
using System.Collections.Concurrent;
using System.Text.Json;

namespace FreeBlazorExtended.Calendar;

public partial class CalendarEventService
{
    private static readonly ConcurrentDictionary<Guid, CalendarEvent> _events = new();

    public Task<List<CalendarEvent>> GetCalendarEvents(Guid TenantId)
    {
        var events = _events.Values
            .Where(e => e.TenantId == TenantId && !e.Deleted)
            .ToList();
        return Task.FromResult(events);
    }

    public Task<List<CalendarEvent>> GetEventsInRange(Guid TenantId, DateTime StartUtc, DateTime EndUtc)
    {
        var events = _events.Values
            .Where(e => e.TenantId == TenantId && !e.Deleted &&
                        e.StartUtc < EndUtc && e.EndUtc > StartUtc)
            .ToList();
        return Task.FromResult(events);
    }

    public Task<List<CalendarEvent>> GetResourceEvents(Guid TenantId, Guid ResourceId, DateTime StartUtc, DateTime EndUtc)
    {
        var events = _events.Values
            .Where(e => e.TenantId == TenantId && !e.Deleted &&
                        e.ResourceId == ResourceId &&
                        e.StartUtc < EndUtc && e.EndUtc > StartUtc)
            .OrderBy(e => e.StartUtc)
            .ToList();
        return Task.FromResult(events);
    }

    public Task<CalendarEvent?> GetCalendarEvent(Guid EventId)
    {
        _events.TryGetValue(EventId, out var evt);
        return Task.FromResult(evt?.Deleted == false ? evt : null);
    }

    public Task<CalendarEvent> SaveCalendarEvent(CalendarEvent evt, string? UserId = null)
    {
        if (evt.EventId == Guid.Empty)
            evt.EventId = Guid.NewGuid();

        evt.LastModified = DateTime.UtcNow;
        evt.LastModifiedBy = UserId;

        if (!_events.ContainsKey(evt.EventId)) {
            evt.Added = DateTime.UtcNow;
            evt.AddedBy = UserId;
        }

        _events[evt.EventId] = evt;
        return Task.FromResult(evt);
    }

    public async Task<(bool hasConflict, List<CalendarEvent> conflicts)> CheckConflicts(
        Guid TenantId,
        Guid? ResourceId,
        DateTime StartUtc,
        DateTime EndUtc,
        Guid? excludeEventId = null)
    {
        if (!ResourceId.HasValue)
            return (false, new());

        var resourceEvents = await GetResourceEvents(TenantId, ResourceId.Value, StartUtc, EndUtc);

        var conflicts = resourceEvents
            .Where(e => (excludeEventId == null || e.EventId != excludeEventId) &&
                        e.Status != EventStatus.Cancelled &&
                        !(e.EndUtc <= StartUtc || e.StartUtc >= EndUtc))
            .ToList();

        return (conflicts.Count > 0, conflicts);
    }

    public async Task<List<CalendarEvent>> ExpandRecurringEvent(
        Guid EventId,
        DateTime throughUtc)
    {
        var evt = await GetCalendarEvent(EventId);
        if (evt == null || string.IsNullOrEmpty(evt.RecurrenceRuleJson))
            return new() { evt };

        var expanded = new List<CalendarEvent> { evt };

        try {
            var rule = JsonSerializer.Deserialize<RecurrenceRule>(evt.RecurrenceRuleJson);
            if (rule != null) {
                var instances = GenerateRecurrenceInstances(evt, rule, throughUtc);
                expanded.AddRange(instances);
            }
        }
        catch (Exception) {

            // JSON parse failed; treat as malformed data and fall through to default.

        }

        return expanded;
    }

    private List<CalendarEvent> GenerateRecurrenceInstances(
        CalendarEvent baseEvent,
        RecurrenceRule rule,
        DateTime throughUtc)
    {
        var instances = new List<CalendarEvent>();
        var current = baseEvent.StartUtc;
        var duration = baseEvent.EndUtc - baseEvent.StartUtc;
        var untilDate = rule.Until ?? throughUtc;

        while (current <= untilDate && (rule.Count == null || instances.Count < rule.Count)) {
            var instance = new CalendarEvent
            {
                EventId = Guid.NewGuid(),
                TenantId = baseEvent.TenantId,
                Title = baseEvent.Title,
                StartUtc = current,
                EndUtc = current.Add(duration),
                IsAllDay = baseEvent.IsAllDay,
                ResourceId = baseEvent.ResourceId,
                OwnerName = baseEvent.OwnerName,
                Color = baseEvent.Color,
                ParentEventId = baseEvent.EventId,
                Status = baseEvent.Status
            };

            instances.Add(instance);
            current = IncrementByRule(current, rule);
        }

        return instances;
    }

    private DateTime IncrementByRule(DateTime current, RecurrenceRule rule)
    {
        return rule.Frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(rule.Interval),
            RecurrenceFrequency.Weekly => current.AddDays(rule.Interval * 7),
            RecurrenceFrequency.Monthly => current.AddMonths(rule.Interval),
            RecurrenceFrequency.Yearly => current.AddYears(rule.Interval),
            _ => current.AddDays(1)
        };
    }

    public Task<DataObjects.BooleanResponse> DeleteCalendarEvent(Guid EventId, DataObjects.User? CurrentUser = null)
    {
        var output = new DataObjects.BooleanResponse();

        if (_events.TryGetValue(EventId, out var evt)) {
            evt.Deleted = true;
            evt.LastModified = DateTime.UtcNow;
            output.Result = true;
        } else {
            output.Messages.Add("Calendar event not found.");
        }

        return Task.FromResult(output);
    }

    public void ClearAllEvents()
    {
        _events.Clear();
    }
}
