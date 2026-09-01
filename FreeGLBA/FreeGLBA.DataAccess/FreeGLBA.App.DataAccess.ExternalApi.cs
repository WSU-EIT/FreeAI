using Microsoft.EntityFrameworkCore;

namespace FreeGLBA;

// ============================================================================
// GLBA EXTERNAL API DATA ACCESS
// ============================================================================

/// <summary>Glba External API interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Get access events for a specific subject by external ID.</summary>
    Task<List<DataObjects.AccessEvent>> GetAccessEventsBySubjectAsync(string subjectId, int limit = 100);

    /// <summary>Get accessor (user) statistics with filtering and pagination.</summary>
    Task<DataObjects.AccessorFilterResult> GetAccessorsAsync(DataObjects.AccessorFilter filter);

    /// <summary>Get dashboard trend data (events per day) and items needing attention.</summary>
    Task<DataObjects.GlbaInsights> GetGlbaInsightsAsync(int windowDays = 30);

    /// <summary>Get dashboard statistics.</summary>
    Task<DataObjects.GlbaStats> GetGlbaStatsAsync();

    /// <summary>Get recent events for dashboard feed.</summary>
    Task<List<DataObjects.AccessEvent>> GetRecentAccessEventsAsync(int limit = 50);

    /// <summary>Get top accessors for dashboard display.</summary>
    Task<List<DataObjects.AccessorSummary>> GetTopAccessorsAsync(int limit = 10);

    /// <summary>Process a batch of events from external source.</summary>
    Task<DataObjects.GlbaBatchResponse> ProcessGlbaBatchAsync(List<DataObjects.GlbaEventRequest> requests, Guid sourceSystemId);
    /// <summary>Process a single event from external source.</summary>
    Task<DataObjects.GlbaEventResponse> ProcessGlbaEventAsync(DataObjects.GlbaEventRequest request, Guid sourceSystemId, bool suppressSignalR = false);
}

public partial class DataAccess
{
    #region Glba External API

    /// <summary>
    /// Publishes a GLBA SignalR update without letting a relay failure break the
    /// write path (the relay is an HTTP post to the app's own URL). Real-time
    /// refresh is best-effort; the data is already stored.
    /// </summary>
    private async Task NotifyGlbaChangeAsync(string updateType, Guid? itemId, string message)
    {
        try {
            await SignalRUpdate(new DataObjects.SignalRUpdate {
                UpdateType = updateType,
                ItemId = itemId,
                Message = message,
            });
        } catch { }
    }

    /// <summary>Process a single event from external source.</summary>
    public async Task<DataObjects.GlbaEventResponse> ProcessGlbaEventAsync(
        DataObjects.GlbaEventRequest request, Guid sourceSystemId, bool suppressSignalR = false){
        var response = new DataObjects.GlbaEventResponse
        {
            ReceivedAt = DateTime.UtcNow
        };

        // Validation - UserId and AccessType are required
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            response.Status = "error";
            response.Message = "Missing required field: UserId";
            return response;
        }

        if (string.IsNullOrWhiteSpace(request.AccessType))
        {
            response.Status = "error";
            response.Message = "Missing required field: AccessType";
            return response;
        }

        // Validation - SubjectId is optional for general audit logging
        // If no SubjectId provided, use "SYSTEM" as a placeholder
        var hasSubject = !string.IsNullOrWhiteSpace(request.SubjectId);
        var hasBulkSubjects = request.SubjectIds?.Any(s => !string.IsNullOrWhiteSpace(s)) == true;
        
        if (!hasSubject && !hasBulkSubjects)
        {
            // General audit log - no specific data subject
            request.SubjectId = "SYSTEM";
        }

        // Deduplication check
        if (!string.IsNullOrEmpty(request.SourceEventId))
        {
            var exists = await data.AccessEvents.AnyAsync(x =>
                x.SourceSystemId == sourceSystemId &&
                x.SourceEventId == request.SourceEventId);

            if (exists)
            {
                response.Status = "duplicate";
                response.Message = "Event with this SourceEventId already exists";
                return response;
            }
        }

        // Handle bulk subjects - calculate count and serialize IDs
        var subjectIdList = request.SubjectIds?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        hasBulkSubjects = subjectIdList?.Count > 0;
        var subjectCount = hasBulkSubjects ? subjectIdList!.Count : (hasSubject ? 1 : 0);
        var subjectIdsJson = hasBulkSubjects ? System.Text.Json.JsonSerializer.Serialize(subjectIdList) : string.Empty;
        var primarySubjectId = hasBulkSubjects 
            ? (subjectIdList!.Count > 1 ? "BULK" : subjectIdList[0])
            : request.SubjectId;

        // Ensure dates are UTC
        var accessedAtUtc = request.AccessedAt.Kind == DateTimeKind.Utc 
            ? request.AccessedAt 
            : DateTime.SpecifyKind(request.AccessedAt, DateTimeKind.Utc);
        var agreementAtUtc = request.AgreementAcknowledgedAt.HasValue
            ? (request.AgreementAcknowledgedAt.Value.Kind == DateTimeKind.Utc 
                ? request.AgreementAcknowledgedAt.Value 
                : DateTime.SpecifyKind(request.AgreementAcknowledgedAt.Value, DateTimeKind.Utc))
            : accessedAtUtc;

        // Snapshot the data owner at the time of access. Caller-supplied values win;
        // otherwise capture the source system's current data owner. This preserves
        // "who owned the data then" even after ownership changes hands.
        var sourceSystem = await data.SourceSystems.FindAsync(sourceSystemId);
        var ownerName = (request.DataOwnerName ?? string.Empty).Trim();
        var ownerEmail = (request.DataOwnerEmail ?? string.Empty).Trim();
        var ownerDepartment = (request.DataOwnerDepartment ?? string.Empty).Trim();
        if (ownerName == string.Empty && ownerEmail == string.Empty && ownerDepartment == string.Empty && sourceSystem != null) {
            ownerName = sourceSystem.DataOwnerName;
            ownerEmail = sourceSystem.DataOwnerEmail;
            ownerDepartment = sourceSystem.DataOwnerDepartment;
        }

        // Create event record - ensure all strings are never null
        var evt = new EFModels.EFModels.AccessEventItem
        {
            AccessEventId = Guid.NewGuid(),
            SourceSystemId = sourceSystemId,
            ReceivedAt = DateTime.UtcNow,
            SourceEventId = (request.SourceEventId ?? string.Empty).Trim(),
            AccessedAt = accessedAtUtc,
            UserId = (request.UserId ?? string.Empty).Trim(),
            UserName = (request.UserName ?? string.Empty).Trim(),
            UserEmail = (request.UserEmail ?? string.Empty).Trim(),
            UserDepartment = (request.UserDepartment ?? string.Empty).Trim(),
            SubjectId = (primarySubjectId ?? string.Empty).Trim(),
            SubjectType = (request.SubjectType ?? string.Empty).Trim(),
            SubjectIds = subjectIdsJson ?? "[]",
            SubjectCount = subjectCount,
            DataCategory = (request.DataCategory ?? string.Empty).Trim(),
            AccessType = (request.AccessType ?? string.Empty).Trim(),
            Purpose = (request.Purpose ?? string.Empty).Trim(),
            IpAddress = (request.IpAddress ?? string.Empty).Trim(),
            AdditionalData = string.IsNullOrWhiteSpace(request.AdditionalData) ? "{}" : request.AdditionalData.Trim(),
            AgreementText = (request.AgreementText ?? string.Empty).Trim(),
            AgreementAcknowledgedAt = agreementAtUtc,
            DataOwnerName = ownerName,
            DataOwnerEmail = ownerEmail,
            DataOwnerDepartment = ownerDepartment,
        };

        data.AccessEvents.Add(evt);

        // Update LastEventReceivedAt on source system (works with all providers including InMemory)
        if (sourceSystem != null) {
            sourceSystem.LastEventReceivedAt = DateTime.UtcNow;
        }

        // The tamper-evident chain position must be assigned and persisted under
        // the chain lock so concurrent ingests get consecutive sequence numbers.
        await _chainLock.WaitAsync();
        try {
            await AssignChainPositionAsync(evt);
            await data.SaveChangesAsync();
        } catch (DbUpdateException) {
            // The filtered unique index on (SourceSystemId, SourceEventId) tripped:
            // a concurrent retry of the same event won the race. Detach the failed
            // row (so batch processing on this context keeps working) and report it
            // as the duplicate it is rather than an error.
            data.Entry(evt).State = EntityState.Detached;

            var isDuplicate = !string.IsNullOrEmpty(evt.SourceEventId) && await data.AccessEvents.AnyAsync(x =>
                x.SourceSystemId == sourceSystemId && x.SourceEventId == evt.SourceEventId);
            if (isDuplicate) {
                response.Status = "duplicate";
                response.Message = "Event with this SourceEventId already exists";
                return response;
            }

            throw;
        } finally {
            _chainLock.Release();
        }

        // Webhook alerts (large bulk access, after-hours) - best-effort, detached.
        await QueueEventAlertsAsync(new[] { (evt, sourceSystem?.Name ?? string.Empty) });

        // Update DataSubject stats - handle bulk or single
        // Skip for SYSTEM subjects (general audit logs without a specific data subject)
        if (hasBulkSubjects) {
            await UpdateDataSubjectStatsAsync(subjectIdList!, request.SubjectType, accessedAtUtc);
        } else if (hasSubject && request.SubjectId != "SYSTEM") {
            await UpdateDataSubjectStatsAsync(request.SubjectId, request.SubjectType, accessedAtUtc);
        }

        if (!suppressSignalR) {
            await NotifyGlbaChangeAsync(DataObjects.SignalRUpdateType.GlbaAccessEvent, evt.AccessEventId, "New");
        }

        response.EventId = evt.AccessEventId;
        response.Status = "accepted";
        response.SubjectCount = subjectCount;
        return response;
    }

    /// <summary>Process a batch of events from external source.</summary>
    public async Task<DataObjects.GlbaBatchResponse> ProcessGlbaBatchAsync(
        List<DataObjects.GlbaEventRequest> requests, Guid sourceSystemId){
        var response = new DataObjects.GlbaBatchResponse();

        for (int i = 0; i < requests.Count; i++)
        {
            try
            {
                // Suppress per-event SignalR publishes; one aggregate update goes out below.
                var result = await ProcessGlbaEventAsync(requests[i], sourceSystemId, suppressSignalR: true);
                switch (result.Status)
                {
                    case "accepted": response.Accepted++; break;
                    case "duplicate": response.Duplicate++; break;
                    default:
                        response.Rejected++;
                        response.Errors.Add(new DataObjects.GlbaBatchError { Index = i, Error = result.Message ?? "Unknown error" });
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Rejected++;
                response.Errors.Add(new DataObjects.GlbaBatchError { Index = i, Error = ex.Message });
            }
        }

        if (response.Accepted > 0) {
            await NotifyGlbaChangeAsync(DataObjects.SignalRUpdateType.GlbaAccessEvent, null, "Batch:" + response.Accepted.ToString());
        }

        return response;
    }

    /// <summary>Get dashboard statistics.</summary>
    public async Task<DataObjects.GlbaStats> GetGlbaStatsAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        // Event counts
        var eventsToday = await data.AccessEvents.CountAsync(x => x.AccessedAt >= todayStart);
        var eventsThisWeek = await data.AccessEvents.CountAsync(x => x.AccessedAt >= weekStart);
        var eventsThisMonth = await data.AccessEvents.CountAsync(x => x.AccessedAt >= monthStart);

        // Subject counts - subjects accessed in each period
        var subjectsToday = await data.DataSubjects.CountAsync(x => x.LastAccessedAt >= todayStart);
        var subjectsThisWeek = await data.DataSubjects.CountAsync(x => x.LastAccessedAt >= weekStart);
        var subjectsThisMonth = await data.DataSubjects.CountAsync(x => x.LastAccessedAt >= monthStart);
        var totalSubjects = await data.DataSubjects.CountAsync();

        // Total distinct users who have ever accessed data
        var totalAccessors = await data.AccessEvents
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();

        // Breakdowns by access type and data category (all time)
        var byAccessType = await data.AccessEvents
            .GroupBy(x => x.AccessType)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToListAsync();
        var byCategory = await data.AccessEvents
            .Where(x => x.DataCategory != "")
            .GroupBy(x => x.DataCategory)
            .Select(g => new { g.Key, Count = g.LongCount() })
            .ToListAsync();

        return new DataObjects.GlbaStats
        {
            Today = eventsToday,
            ThisWeek = eventsThisWeek,
            ThisMonth = eventsThisMonth,
            TotalSubjects = totalSubjects,
            SubjectsToday = subjectsToday,
            SubjectsThisWeek = subjectsThisWeek,
            SubjectsThisMonth = subjectsThisMonth,
            TotalAccessors = totalAccessors,
            ByAccessType = byAccessType
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .OrderByDescending(x => x.Count)
                .ToDictionary(x => x.Key, x => x.Count),
            ByCategory = byCategory
                .OrderByDescending(x => x.Count)
                .ToDictionary(x => x.Key, x => x.Count),
        };
    }

    /// <summary>
    /// Get dashboard trend data (events per day, zero-filled) and items needing
    /// attention: volume spikes, large bulk exports, first-time accessors, and
    /// source systems with no data owner recorded.
    /// </summary>
    public async Task<DataObjects.GlbaInsights> GetGlbaInsightsAsync(int windowDays = 30)
    {
        if (windowDays < 7) windowDays = 7;
        if (windowDays > 90) windowDays = 90;

        var output = new DataObjects.GlbaInsights { WindowDays = windowDays };
        var todayStart = DateTime.UtcNow.Date;
        var windowStart = todayStart.AddDays(-(windowDays - 1));
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // Events per day, zero-filled across the window.
        var perDay = await data.AccessEvents
            .Where(x => x.AccessedAt >= windowStart)
            .GroupBy(x => x.AccessedAt.Date)
            .Select(g => new {
                Date = g.Key,
                Events = g.Count(),
                Exports = g.Count(x => x.AccessType == "Export" || x.AccessType == "Download"),
            })
            .ToListAsync();
        var perDayLookup = perDay.ToDictionary(x => x.Date);
        for (var day = windowStart; day <= todayStart; day = day.AddDays(1)) {
            perDayLookup.TryGetValue(day, out var counts);
            output.EventsPerDay.Add(new DataObjects.GlbaDailyCount {
                Date = day,
                Events = counts?.Events ?? 0,
                Exports = counts?.Exports ?? 0,
            });
        }

        // Volume spikes: users whose count today is at least 10 and more than
        // three times their average daily volume over the rest of the window.
        var todayByUser = await data.AccessEvents
            .Where(x => x.AccessedAt >= todayStart)
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .Where(x => x.Count >= 10)
            .ToListAsync();
        if (todayByUser.Count > 0) {
            var spikeCandidates = todayByUser.Select(x => x.UserId).ToList();
            var priorByUser = await data.AccessEvents
                .Where(x => x.AccessedAt >= windowStart && x.AccessedAt < todayStart && spikeCandidates.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            foreach (var user in todayByUser.OrderByDescending(x => x.Count)) {
                var priorDays = Math.Max(1, windowDays - 1);
                var dailyAverage = priorByUser.GetValueOrDefault(user.UserId, 0) / (double)priorDays;
                if (user.Count > dailyAverage * 3) {
                    output.Items.Add(new DataObjects.GlbaInsight {
                        Severity = "warning",
                        Title = $"Unusual volume: {user.UserId}",
                        Detail = dailyAverage > 0
                            ? $"{user.Count} accesses today vs. a {dailyAverage:0.#}/day average over the prior {priorDays} days."
                            : $"{user.Count} accesses today from a user with no activity in the prior {priorDays} days.",
                        LinkPage = $"AccessEvents?userId={Uri.EscapeDataString(user.UserId)}",
                        LinkText = "Review this user's events",
                    });
                }
            }
        }

        // Large bulk exports in the last 7 days.
        var largeBulk = await data.AccessEvents
            .Where(x => x.AccessedAt >= sevenDaysAgo && x.SubjectCount >= 50)
            .GroupBy(x => 1)
            .Select(g => new { Count = g.Count(), MaxSubjects = g.Max(x => x.SubjectCount) })
            .FirstOrDefaultAsync();
        if (largeBulk != null && largeBulk.Count > 0) {
            output.Items.Add(new DataObjects.GlbaInsight {
                Severity = "critical",
                Title = $"{largeBulk.Count} large bulk export{(largeBulk.Count == 1 ? "" : "s")} this week",
                Detail = $"Events touching 50+ data subjects in one operation (largest: {largeBulk.MaxSubjects:N0} subjects). Confirm each had a legitimate business purpose.",
                LinkPage = "AccessEvents",
                LinkText = "Review access events",
            });
        }

        // First-time accessors in the last 7 days.
        var newAccessors = await data.AccessEvents
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, First = g.Min(x => x.AccessedAt) })
            .Where(x => x.First >= sevenDaysAgo)
            .OrderByDescending(x => x.First)
            .ToListAsync();
        if (newAccessors.Count > 0) {
            var names = string.Join(", ", newAccessors.Take(5).Select(x => x.UserId));
            if (newAccessors.Count > 5) names += ", …";
            output.Items.Add(new DataObjects.GlbaInsight {
                Severity = "info",
                Title = $"{newAccessors.Count} first-time accessor{(newAccessors.Count == 1 ? "" : "s")} this week",
                Detail = $"Users accessing protected data for the first time: {names}. Expected for new hires; worth a look otherwise.",
                LinkPage = "Accessors",
                LinkText = "Open accessors",
            });
        }

        // After-hours access in the last 7 days, judged in the institution timezone.
        var glbaSettings = await GetGlbaSettingsAsync();
        var recentAccessTimes = await data.AccessEvents
            .Where(x => x.AccessedAt >= sevenDaysAgo)
            .Select(x => x.AccessedAt)
            .ToListAsync();
        var afterHoursCount = recentAccessTimes.Count(t => IsAfterHours(t, glbaSettings));
        if (afterHoursCount > 0) {
            var tzLabel = string.IsNullOrEmpty(glbaSettings.InstitutionTimeZone) ? "UTC" : glbaSettings.InstitutionTimeZone;
            var tzHint = string.IsNullOrEmpty(glbaSettings.InstitutionTimeZone)
                ? " Set the institution time zone in GLBA Settings for accurate detection."
                : "";
            output.Items.Add(new DataObjects.GlbaInsight {
                Severity = "info",
                Title = $"{afterHoursCount} after-hours access event{(afterHoursCount == 1 ? "" : "s")} this week",
                Detail = $"Access outside {glbaSettings.BusinessHoursStart:00}:00–{glbaSettings.BusinessHoursEnd:00}:00 {tzLabel}" +
                    (glbaSettings.WeekendsAreAfterHours ? " or on weekends." : ".") + tzHint,
                LinkPage = "AccessEvents",
                LinkText = "Review events",
            });
        }

        // Source systems with no data owner recorded.
        var unowned = await data.SourceSystems
            .Where(x => x.IsActive && x.DataOwnerName == "" && x.DataOwnerEmail == "")
            .Select(x => x.Name)
            .ToListAsync();
        if (unowned.Count > 0) {
            output.Items.Add(new DataObjects.GlbaInsight {
                Severity = "warning",
                Title = $"{unowned.Count} source system{(unowned.Count == 1 ? " has" : "s have")} no data owner",
                Detail = $"Without an owner there is no point of contact for the data, and events from {string.Join(", ", unowned.Take(5))} get no ownership snapshot.",
                LinkPage = "SourceSystems",
                LinkText = "Assign owners",
            });
        }

        return output;
    }

    /// <summary>Get recent events for dashboard feed.</summary>
    public async Task<List<DataObjects.AccessEvent>> GetRecentAccessEventsAsync(int limit = 50)
    {
        return await data.AccessEvents
            .OrderByDescending(x => x.AccessedAt)
            .Take(limit)
            .Select(x => new DataObjects.AccessEvent
            {
                AccessEventId = x.AccessEventId,
                SourceSystemId = x.SourceSystemId,
                SourceEventId = x.SourceEventId,
                AccessedAt = x.AccessedAt,
                ReceivedAt = x.ReceivedAt,
                UserId = x.UserId,
                UserName = x.UserName,
                UserEmail = x.UserEmail,
                UserDepartment = x.UserDepartment,
                SubjectId = x.SubjectId,
                SubjectType = x.SubjectType,
                SubjectIds = x.SubjectIds,
                SubjectCount = x.SubjectCount,
                DataCategory = x.DataCategory,
                AccessType = x.AccessType,
                Purpose = x.Purpose,
                IpAddress = x.IpAddress,
                AdditionalData = x.AdditionalData,
                DataOwnerName = x.DataOwnerName,
                DataOwnerEmail = x.DataOwnerEmail,
                DataOwnerDepartment = x.DataOwnerDepartment,
            })
            .ToListAsync();
    }

    /// <summary>Get access events for a specific subject by external ID.</summary>
    public async Task<List<DataObjects.AccessEvent>> GetAccessEventsBySubjectAsync(string subjectId, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(subjectId)) return new List<DataObjects.AccessEvent>();

        // Search both direct SubjectId match AND in SubjectIds JSON array (for bulk events)
        return await data.AccessEvents
            .Where(x => x.SubjectId == subjectId || x.SubjectIds.Contains(subjectId))
            .OrderByDescending(x => x.AccessedAt)
            .Take(limit)
            .Select(x => new DataObjects.AccessEvent
            {
                AccessEventId = x.AccessEventId,
                SourceSystemId = x.SourceSystemId,
                SourceEventId = x.SourceEventId,
                AccessedAt = x.AccessedAt,
                ReceivedAt = x.ReceivedAt,
                UserId = x.UserId,
                UserName = x.UserName,
                UserEmail = x.UserEmail,
                UserDepartment = x.UserDepartment,
                SubjectId = x.SubjectId,
                SubjectType = x.SubjectType,
                SubjectIds = x.SubjectIds,
                SubjectCount = x.SubjectCount,
                DataCategory = x.DataCategory,
                AccessType = x.AccessType,
                Purpose = x.Purpose,
                IpAddress = x.IpAddress,
                AdditionalData = x.AdditionalData,
                DataOwnerName = x.DataOwnerName,
                DataOwnerEmail = x.DataOwnerEmail,
                DataOwnerDepartment = x.DataOwnerDepartment,
            })
            .ToListAsync();
    }

    /// <summary>
    /// Update or create DataSubject stats for a single subject by recomputing them
    /// from the stored events (direct matches plus multi-subject JSON events), so the
    /// counts stay exact on insert and on delete.
    /// </summary>
    private async Task UpdateDataSubjectStatsAsync(string subjectId, string? subjectType = null, DateTime? accessedAt = null)
    {
        if (string.IsNullOrWhiteSpace(subjectId)) return;

        var stats = await data.AccessEvents
            .Where(x => x.SubjectId == subjectId || (x.SubjectCount > 1 && x.SubjectIds.Contains(subjectId)))
            .GroupBy(x => 1)
            .Select(g => new {
                Total = g.LongCount(),
                Unique = g.Select(x => x.UserId).Distinct().Count(),
                First = g.Min(x => x.AccessedAt),
                Last = g.Max(x => x.AccessedAt),
            })
            .FirstOrDefaultAsync();

        var subject = await data.DataSubjects
            .FirstOrDefaultAsync(x => x.ExternalId == subjectId);

        if (subject == null) {
            var now = accessedAt ?? DateTime.UtcNow;
            data.DataSubjects.Add(new EFModels.EFModels.DataSubjectItem
            {
                DataSubjectId = Guid.NewGuid(),
                ExternalId = subjectId,
                SubjectType = subjectType ?? "Student",
                FirstAccessedAt = stats?.First ?? now,
                LastAccessedAt = stats?.Last ?? now,
                TotalAccessCount = stats?.Total ?? 1,
                UniqueAccessorCount = Math.Max(1, stats?.Unique ?? 1),
            });
        } else {
            subject.FirstAccessedAt = stats?.First ?? subject.FirstAccessedAt;
            subject.LastAccessedAt = stats?.Last ?? subject.LastAccessedAt;
            subject.TotalAccessCount = stats?.Total ?? 0;
            subject.UniqueAccessorCount = stats?.Unique ?? 0;
            // Update SubjectType if provided and currently empty
            if (!string.IsNullOrEmpty(subjectType) && string.IsNullOrEmpty(subject.SubjectType)) {
                subject.SubjectType = subjectType;
            }
        }

        await data.SaveChangesAsync();
    }

    /// <summary>
    /// Update or create DataSubject stats for multiple subjects (bulk access).
    /// Unique accessor counts come from one grouped query over direct-match events;
    /// multi-subject JSON events are not matched there, so the value is treated as a
    /// floor and never regresses below the previously stored count.
    /// </summary>
    private async Task UpdateDataSubjectStatsAsync(IEnumerable<string> subjectIds, string? subjectType = null, DateTime? accessedAt = null)
    {
        if (subjectIds == null) return;

        var distinctIds = subjectIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        if (distinctIds.Count == 0) return;

        var eventTime = accessedAt ?? DateTime.UtcNow;

        // Get existing subjects
        var existingSubjects = await data.DataSubjects
            .Where(x => distinctIds.Contains(x.ExternalId))
            .ToDictionaryAsync(x => x.ExternalId);

        var accessorCounts = await data.AccessEvents
            .Where(x => distinctIds.Contains(x.SubjectId))
            .GroupBy(x => x.SubjectId)
            .Select(g => new { SubjectId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

        foreach (var subjectId in distinctIds) {
            var uniqueAccessors = Math.Max(1, accessorCounts.GetValueOrDefault(subjectId, 0));

            if (existingSubjects.TryGetValue(subjectId, out var subject)) {
                if (eventTime < subject.FirstAccessedAt) {
                    subject.FirstAccessedAt = eventTime;
                }
                if (eventTime > subject.LastAccessedAt) {
                    subject.LastAccessedAt = eventTime;
                }
                subject.TotalAccessCount++;
                subject.UniqueAccessorCount = Math.Max(subject.UniqueAccessorCount, uniqueAccessors);
                if (!string.IsNullOrEmpty(subjectType) && string.IsNullOrEmpty(subject.SubjectType)) {
                    subject.SubjectType = subjectType;
                }
            } else {
                var newSubject = new EFModels.EFModels.DataSubjectItem
                {
                    DataSubjectId = Guid.NewGuid(),
                    ExternalId = subjectId,
                    SubjectType = subjectType ?? "Student",
                    FirstAccessedAt = eventTime,
                    LastAccessedAt = eventTime,
                    TotalAccessCount = 1,
                    UniqueAccessorCount = uniqueAccessors,
                };
                data.DataSubjects.Add(newSubject);
            }
        }

        await data.SaveChangesAsync();
    }

    /// <summary>Get accessor (user) statistics with filtering and pagination.</summary>
    public async Task<DataObjects.AccessorFilterResult> GetAccessorsAsync(DataObjects.AccessorFilter filter)
    {
        // Group access events by UserId to get accessor stats
        var query = data.AccessEvents
            .AsNoTracking()
            .GroupBy(x => x.UserId)
            .Select(g => new DataObjects.AccessorSummary
            {
                UserId = g.Key,
                UserName = g.OrderByDescending(x => x.AccessedAt).Select(x => x.UserName).FirstOrDefault(),
                UserEmail = g.OrderByDescending(x => x.AccessedAt).Select(x => x.UserEmail).FirstOrDefault(),
                UserDepartment = g.OrderByDescending(x => x.AccessedAt).Select(x => x.UserDepartment).FirstOrDefault(),
                TotalAccesses = g.Count(),
                UniqueSubjectsAccessed = g.Select(x => x.SubjectId).Distinct().Count(),
                ExportCount = g.Count(x => x.AccessType == "Export" || x.AccessType == "Download"),
                ViewCount = g.Count(x => x.AccessType == "View" || x.AccessType == "Query"),
                FirstAccessAt = g.Min(x => x.AccessedAt),
                LastAccessAt = g.Max(x => x.AccessedAt)
            });

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(x => x.UserId.ToLower().Contains(search) ||
                                     (x.UserName != null && x.UserName.ToLower().Contains(search)) ||
                                     (x.UserEmail != null && x.UserEmail.ToLower().Contains(search)) ||
                                     (x.UserDepartment != null && x.UserDepartment.ToLower().Contains(search)));
        }

        // Apply department filter
        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            query = query.Where(x => x.UserDepartment == filter.Department);
        }

        // Apply advanced filters
        if (filter.MinTotalAccesses.HasValue)
            query = query.Where(x => x.TotalAccesses >= filter.MinTotalAccesses.Value);
        if (filter.MaxTotalAccesses.HasValue)
            query = query.Where(x => x.TotalAccesses <= filter.MaxTotalAccesses.Value);
        if (filter.MinUniqueSubjects.HasValue)
            query = query.Where(x => x.UniqueSubjectsAccessed >= filter.MinUniqueSubjects.Value);
        if (filter.MaxUniqueSubjects.HasValue)
            query = query.Where(x => x.UniqueSubjectsAccessed <= filter.MaxUniqueSubjects.Value);
        if (filter.MinExportCount.HasValue)
            query = query.Where(x => x.ExportCount >= filter.MinExportCount.Value);
        if (filter.MaxExportCount.HasValue)
            query = query.Where(x => x.ExportCount <= filter.MaxExportCount.Value);
        if (filter.MinViewCount.HasValue)
            query = query.Where(x => x.ViewCount >= filter.MinViewCount.Value);
        if (filter.MaxViewCount.HasValue)
            query = query.Where(x => x.ViewCount <= filter.MaxViewCount.Value);
        if (filter.LastAccessAfter.HasValue)
            query = query.Where(x => x.LastAccessAt >= filter.LastAccessAfter.Value);
        if (filter.LastAccessBefore.HasValue)
            query = query.Where(x => x.LastAccessAt <= filter.LastAccessBefore.Value);

        var total = await query.CountAsync();

        // Apply sorting
        query = filter.SortColumn switch
        {
            "UserId" => filter.SortDescending ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
            "UserName" => filter.SortDescending ? query.OrderByDescending(x => x.UserName) : query.OrderBy(x => x.UserName),
            "UserDepartment" => filter.SortDescending ? query.OrderByDescending(x => x.UserDepartment) : query.OrderBy(x => x.UserDepartment),
            "TotalAccesses" => filter.SortDescending ? query.OrderByDescending(x => x.TotalAccesses) : query.OrderBy(x => x.TotalAccesses),
            "UniqueSubjectsAccessed" => filter.SortDescending ? query.OrderByDescending(x => x.UniqueSubjectsAccessed) : query.OrderBy(x => x.UniqueSubjectsAccessed),
            "ExportCount" => filter.SortDescending ? query.OrderByDescending(x => x.ExportCount) : query.OrderBy(x => x.ExportCount),
            "ViewCount" => filter.SortDescending ? query.OrderByDescending(x => x.ViewCount) : query.OrderBy(x => x.ViewCount),
            "LastAccessAt" => filter.SortDescending ? query.OrderByDescending(x => x.LastAccessAt) : query.OrderBy(x => x.LastAccessAt),
            _ => query.OrderByDescending(x => x.TotalAccesses) // Default: most active first
        };

        // Apply pagination
        var records = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new DataObjects.AccessorFilterResult
        {
            Records = records,
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <summary>Get top accessors for dashboard display.</summary>
    public async Task<List<DataObjects.AccessorSummary>> GetTopAccessorsAsync(int limit = 10)
    {
        return await data.AccessEvents
            .AsNoTracking()
            .GroupBy(x => x.UserId)
            .Select(g => new DataObjects.AccessorSummary
            {
                UserId = g.Key,
                UserName = g.OrderByDescending(x => x.AccessedAt).Select(x => x.UserName).FirstOrDefault(),
                UserDepartment = g.OrderByDescending(x => x.AccessedAt).Select(x => x.UserDepartment).FirstOrDefault(),
                TotalAccesses = g.Count(),
                UniqueSubjectsAccessed = g.Select(x => x.SubjectId).Distinct().Count(),
                ExportCount = g.Count(x => x.AccessType == "Export" || x.AccessType == "Download"),
                LastAccessAt = g.Max(x => x.AccessedAt)
            })
            .OrderByDescending(x => x.TotalAccesses)
            .Take(limit)
            .ToListAsync();
    }

    #endregion
}
