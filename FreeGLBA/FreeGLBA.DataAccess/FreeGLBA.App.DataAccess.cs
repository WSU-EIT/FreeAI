using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace FreeGLBA;

// ---- DataAccess.SourceSystems ----

// ============================================================================
// SOURCESYSTEM - DATA ACCESS
// SourceSystem CRUD data access methods and their IDataAccess members.
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.SourceSystemFilterResult> GetSourceSystemsAsync(DataObjects.SourceSystemFilter filter);
    Task<DataObjects.SourceSystem?> GetSourceSystemAsync(Guid id);
    Task<List<DataObjects.SourceSystemLookup>> GetSourceSystemLookupsAsync();
    /// <summary>
    /// Creates or updates a SourceSystem. When the data-owner fields change, the open
    /// DataOwnership history row is closed and a new one is written, stamped with savedBy.
    /// </summary>
    Task<DataObjects.SourceSystem?> SaveSourceSystemAsync(DataObjects.SourceSystem dto, string? savedBy = null);
    Task<bool> DeleteSourceSystemAsync(Guid id);
    /// <summary>
    /// Gets the data-ownership history for a source system, current owner first.
    /// The row with EndedAt == null is the current owner.
    /// </summary>
    Task<List<DataObjects.DataOwnership>> GetDataOwnershipHistoryAsync(Guid sourceSystemId);
}

public partial class DataAccess
{
    #region SourceSystem

    public async Task<DataObjects.SourceSystemFilterResult> GetSourceSystemsAsync(DataObjects.SourceSystemFilter filter)
    {
        var query = data.SourceSystems
            .AsQueryable();

        if (filter.IsActiveFilter.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActiveFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.Name.Contains(filter.Search) || x.DisplayName.Contains(filter.Search) || x.ContactEmail.Contains(filter.Search)
                || x.DataOwnerName.Contains(filter.Search) || x.DataOwnerEmail.Contains(filter.Search) || x.DataOwnerDepartment.Contains(filter.Search));
        }

        // Apply advanced filters
        if (filter.LastActivityAfter.HasValue)
            query = query.Where(x => x.LastEventReceivedAt >= filter.LastActivityAfter.Value);
        if (filter.LastActivityBefore.HasValue)
            query = query.Where(x => x.LastEventReceivedAt <= filter.LastActivityBefore.Value);

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "Name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "DisplayName" => filter.SortDescending ? query.OrderByDescending(x => x.DisplayName) : query.OrderBy(x => x.DisplayName),
            "ContactEmail" => filter.SortDescending ? query.OrderByDescending(x => x.ContactEmail) : query.OrderBy(x => x.ContactEmail),
            "DataOwnerName" => filter.SortDescending ? query.OrderByDescending(x => x.DataOwnerName) : query.OrderBy(x => x.DataOwnerName),
            "IsActive" => filter.SortDescending ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            "LastEventReceivedAt" => filter.SortDescending ? query.OrderByDescending(x => x.LastEventReceivedAt) : query.OrderBy(x => x.LastEventReceivedAt),
            _ => query.OrderByDescending(x => x.SourceSystemId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        // Get event counts via query (computed, not stored)
        var sourceSystemIds = items.Select(x => x.SourceSystemId).ToList();
        var eventCounts = await data.AccessEvents
            .Where(x => sourceSystemIds.Contains(x.SourceSystemId))
            .GroupBy(x => x.SourceSystemId)
            .Select(g => new { SourceSystemId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.SourceSystemId, x => x.Count);

        // Apply event count filters after getting counts (post-filter since it's computed)
        var records = items.Select(x => new DataObjects.SourceSystem
        {
            SourceSystemId = x.SourceSystemId,
            Name = x.Name,
            DisplayName = x.DisplayName,
            ApiKey = x.ApiKey,
            ContactEmail = x.ContactEmail,
            DataOwnerName = x.DataOwnerName,
            DataOwnerEmail = x.DataOwnerEmail,
            DataOwnerDepartment = x.DataOwnerDepartment,
            DataOwnerPhone = x.DataOwnerPhone,
            DataOwnerAssignedAt = x.DataOwnerAssignedAt,
            IsActive = x.IsActive,
            LastEventReceivedAt = x.LastEventReceivedAt,
            EventCount = eventCounts.GetValueOrDefault(x.SourceSystemId, 0),
        }).ToList();

        // Apply event count filters
        if (filter.MinEventCount.HasValue)
            records = records.Where(x => x.EventCount >= filter.MinEventCount.Value).ToList();
        if (filter.MaxEventCount.HasValue)
            records = records.Where(x => x.EventCount <= filter.MaxEventCount.Value).ToList();

        return new DataObjects.SourceSystemFilterResult
        {
            Records = records,
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.SourceSystem?> GetSourceSystemAsync(Guid id)
    {
        var item = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.SourceSystemId == id);
        if (item == null) return null;

        // Get event count via query
        var eventCount = await data.AccessEvents.CountAsync(x => x.SourceSystemId == id);

        return new DataObjects.SourceSystem
        {
            SourceSystemId = item.SourceSystemId,
            Name = item.Name,
            DisplayName = item.DisplayName,
            ApiKey = item.ApiKey,
            ContactEmail = item.ContactEmail,
            DataOwnerName = item.DataOwnerName,
            DataOwnerEmail = item.DataOwnerEmail,
            DataOwnerDepartment = item.DataOwnerDepartment,
            DataOwnerPhone = item.DataOwnerPhone,
            DataOwnerAssignedAt = item.DataOwnerAssignedAt,
            IsActive = item.IsActive,
            LastEventReceivedAt = item.LastEventReceivedAt,
            EventCount = eventCount,
        };
    }

    public async Task<DataObjects.SourceSystem?> SaveSourceSystemAsync(DataObjects.SourceSystem dto, string? savedBy = null)
    {
        EFModels.EFModels.SourceSystemItem item;
        var isNew = dto.SourceSystemId == default;
        string? newPlaintextKey = null;

        if (isNew) {
            item = new EFModels.EFModels.SourceSystemItem();
            item.SourceSystemId = Guid.NewGuid();
            data.SourceSystems.Add(item);
        } else {
            item = await data.SourceSystems.FindAsync(dto.SourceSystemId);
            if (item == null) return null;
        }

        // Only update user-editable fields
        item.Name = dto.Name;
        item.DisplayName = dto.DisplayName;
        item.ContactEmail = dto.ContactEmail;
        item.IsActive = dto.IsActive;

        // Data owner (point of contact for the data this system holds).
        // When the owner changes, close the open ownership-history row and start a new
        // one, so "who owned the data at time T" can always be answered.
        var newOwnerName = (dto.DataOwnerName ?? string.Empty).Trim();
        var newOwnerEmail = (dto.DataOwnerEmail ?? string.Empty).Trim();
        var newOwnerDepartment = (dto.DataOwnerDepartment ?? string.Empty).Trim();
        var newOwnerPhone = (dto.DataOwnerPhone ?? string.Empty).Trim();

        var ownerChanged =
            !string.Equals(item.DataOwnerName, newOwnerName, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(item.DataOwnerEmail, newOwnerEmail, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(item.DataOwnerDepartment, newOwnerDepartment, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(item.DataOwnerPhone, newOwnerPhone, StringComparison.OrdinalIgnoreCase);

        if (ownerChanged) {
            var now = DateTime.UtcNow;
            var hasNewOwner = newOwnerName != string.Empty || newOwnerEmail != string.Empty
                || newOwnerDepartment != string.Empty || newOwnerPhone != string.Empty;

            if (!isNew) {
                var openHistory = await data.DataOwnerships
                    .Where(x => x.SourceSystemId == item.SourceSystemId && x.EndedAt == null)
                    .ToListAsync();
                foreach (var history in openHistory) {
                    history.EndedAt = now;
                }
            }

            if (hasNewOwner) {
                data.DataOwnerships.Add(new EFModels.EFModels.DataOwnershipItem {
                    DataOwnershipId = Guid.NewGuid(),
                    SourceSystemId = item.SourceSystemId,
                    OwnerName = newOwnerName,
                    OwnerEmail = newOwnerEmail,
                    OwnerDepartment = newOwnerDepartment,
                    OwnerPhone = newOwnerPhone,
                    AssignedAt = now,
                    AssignedBy = (savedBy ?? string.Empty).Trim(),
                });
            }

            item.DataOwnerName = newOwnerName;
            item.DataOwnerEmail = newOwnerEmail;
            item.DataOwnerDepartment = newOwnerDepartment;
            item.DataOwnerPhone = newOwnerPhone;
            item.DataOwnerAssignedAt = hasNewOwner ? now : null;
        }

        // Generate API key if new or if regeneration requested
        if (isNew || dto.ApiKey == "REGENERATE") {
            newPlaintextKey = GenerateApiKey();
            item.ApiKey = HashApiKey(newPlaintextKey); // Store the hash, not the plaintext
        }

        await data.SaveChangesAsync();

        // Get event count via query for return value
        var eventCount = await data.AccessEvents.CountAsync(x => x.SourceSystemId == item.SourceSystemId);

        // Return the updated DTO with the generated values
        dto.SourceSystemId = item.SourceSystemId;
        dto.ApiKey = item.ApiKey; // This is the hash (for display masking)
        dto.NewApiKey = newPlaintextKey; // This is the plaintext key (show ONCE to user)
        dto.EventCount = eventCount;
        dto.LastEventReceivedAt = item.LastEventReceivedAt;
        dto.DataOwnerName = item.DataOwnerName;
        dto.DataOwnerEmail = item.DataOwnerEmail;
        dto.DataOwnerDepartment = item.DataOwnerDepartment;
        dto.DataOwnerPhone = item.DataOwnerPhone;
        dto.DataOwnerAssignedAt = item.DataOwnerAssignedAt;

        await NotifyGlbaChangeAsync(DataObjects.SignalRUpdateType.GlbaSourceSystem, item.SourceSystemId, isNew ? "New" : "Updated");

        return dto;
    }

    /// <summary>
    /// Gets the data-ownership history for a source system, current owner first,
    /// then previous owners in reverse chronological order.
    /// </summary>
    public async Task<List<DataObjects.DataOwnership>> GetDataOwnershipHistoryAsync(Guid sourceSystemId)
    {
        return await data.DataOwnerships
            .AsNoTracking()
            .Where(x => x.SourceSystemId == sourceSystemId)
            .OrderByDescending(x => x.EndedAt == null)
            .ThenByDescending(x => x.AssignedAt)
            .Select(x => new DataObjects.DataOwnership {
                DataOwnershipId = x.DataOwnershipId,
                SourceSystemId = x.SourceSystemId,
                OwnerName = x.OwnerName,
                OwnerEmail = x.OwnerEmail,
                OwnerDepartment = x.OwnerDepartment,
                OwnerPhone = x.OwnerPhone,
                AssignedAt = x.AssignedAt,
                EndedAt = x.EndedAt,
                AssignedBy = x.AssignedBy,
                Notes = x.Notes,
            })
            .ToListAsync();
    }

    /// <summary>
    /// Generates a secure random API key for source system authentication.
    /// </summary>
    private string GenerateApiKey()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[64]; // 64 bytes = 512 bits = equivalent to 4 GUIDs
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public async Task<bool> DeleteSourceSystemAsync(Guid id)
    {
        var item = await data.SourceSystems.FindAsync(id);
        if (item == null) return false;

        // Access events are the audit trail and must never be deleted as a side effect.
        // The AccessEvents FK cascades, so deleting a source system with events would
        // silently destroy them - refuse instead. Deactivate the system to retire it.
        var hasEvents = await data.AccessEvents.AnyAsync(x => x.SourceSystemId == id);
        if (hasEvents) return false;

        var ownershipHistory = await data.DataOwnerships.Where(x => x.SourceSystemId == id).ToListAsync();
        if (ownershipHistory.Count > 0) {
            data.DataOwnerships.RemoveRange(ownershipHistory);
        }

        data.SourceSystems.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.SourceSystemLookup>> GetSourceSystemLookupsAsync()
    {
        return await data.SourceSystems
            .Select(x => new DataObjects.SourceSystemLookup
            {
                SourceSystemId = x.SourceSystemId,
                DisplayName = x.Name
            })
            .ToListAsync();
    }

    #endregion
}

// ---- DataAccess.AccessEvents ----

// ============================================================================
// ACCESSEVENT - DATA ACCESS
// AccessEvent CRUD data access methods and their IDataAccess members.
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.AccessEventFilterResult> GetAccessEventsAsync(DataObjects.AccessEventFilter filter);
    Task<DataObjects.AccessEvent?> GetAccessEventAsync(Guid id);
    Task<List<DataObjects.AccessEventLookup>> GetAccessEventLookupsAsync();
    Task<DataObjects.AccessEvent?> SaveAccessEventAsync(DataObjects.AccessEvent dto);
    /// <summary>
    /// Saves many new AccessEvents in a single round trip, then updates
    /// SourceSystem and DataSubject statistics in aggregate.
    /// </summary>
    Task<DataObjects.AccessEventBulkResult> SaveAccessEventsAsync(List<DataObjects.AccessEvent> items);
    Task<bool> DeleteAccessEventAsync(Guid id);
}

public partial class DataAccess
{
    #region AccessEvent

    public async Task<DataObjects.AccessEventFilterResult> GetAccessEventsAsync(DataObjects.AccessEventFilter filter)
    {
        var query = data.AccessEvents
            .AsNoTracking()
            .AsQueryable();

        if (filter.SourceSystemIdFilter != default)
            query = query.Where(x => x.SourceSystemId == filter.SourceSystemIdFilter);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.SourceEventId.Contains(filter.Search) || 
                                     x.UserId.Contains(filter.Search) || 
                                     x.UserName.Contains(filter.Search) ||
                                     x.SubjectId.Contains(filter.Search) ||
                                     x.Purpose.Contains(filter.Search));
        }

        // Apply advanced filters
        if (filter.AccessedAfter.HasValue)
            query = query.Where(x => x.AccessedAt >= filter.AccessedAfter.Value);
        if (filter.AccessedBefore.HasValue)
            query = query.Where(x => x.AccessedAt <= filter.AccessedBefore.Value);
        if (!string.IsNullOrWhiteSpace(filter.UserIdFilter))
            query = query.Where(x => x.UserId.Contains(filter.UserIdFilter));
        if (!string.IsNullOrWhiteSpace(filter.SubjectIdFilter))
            query = query.Where(x => x.SubjectId.Contains(filter.SubjectIdFilter));
        if (!string.IsNullOrWhiteSpace(filter.AccessTypeFilter))
            query = query.Where(x => x.AccessType == filter.AccessTypeFilter);
        if (!string.IsNullOrWhiteSpace(filter.DataCategoryFilter))
            query = query.Where(x => x.DataCategory == filter.DataCategoryFilter);
        if (!string.IsNullOrWhiteSpace(filter.DepartmentFilter))
            query = query.Where(x => x.UserDepartment == filter.DepartmentFilter);

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "SourceSystemName" => filter.SortDescending ? query.OrderByDescending(x => x.SourceSystem.Name) : query.OrderBy(x => x.SourceSystem.Name),
            "SourceEventId" => filter.SortDescending ? query.OrderByDescending(x => x.SourceEventId) : query.OrderBy(x => x.SourceEventId),
            "AccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.AccessedAt) : query.OrderBy(x => x.AccessedAt),
            "ReceivedAt" => filter.SortDescending ? query.OrderByDescending(x => x.ReceivedAt) : query.OrderBy(x => x.ReceivedAt),
            "UserId" => filter.SortDescending ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
            "SubjectId" => filter.SortDescending ? query.OrderByDescending(x => x.SubjectId) : query.OrderBy(x => x.SubjectId),
            "AccessType" => filter.SortDescending ? query.OrderByDescending(x => x.AccessType) : query.OrderBy(x => x.AccessType),
            _ => query.OrderByDescending(x => x.AccessedAt)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        // Get source system names and current owners separately to avoid join issues
        var sourceSystemIds = items.Select(x => x.SourceSystemId).Distinct().ToList();
        var sourceSystems = await data.SourceSystems
            .Where(x => sourceSystemIds.Contains(x.SourceSystemId))
            .ToDictionaryAsync(x => x.SourceSystemId);

        return new DataObjects.AccessEventFilterResult
        {
            Records = items.Select(x => {
                sourceSystems.TryGetValue(x.SourceSystemId, out var source);
                return new DataObjects.AccessEvent
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
                    AgreementText = x.AgreementText,
                    AgreementAcknowledgedAt = x.AgreementAcknowledgedAt,
                    DataOwnerName = x.DataOwnerName,
                    DataOwnerEmail = x.DataOwnerEmail,
                    DataOwnerDepartment = x.DataOwnerDepartment,
                    SourceSystemName = source?.Name ?? string.Empty,
                    CurrentDataOwnerName = source?.DataOwnerName ?? string.Empty,
                    CurrentDataOwnerEmail = source?.DataOwnerEmail ?? string.Empty,
                    CurrentDataOwnerDepartment = source?.DataOwnerDepartment ?? string.Empty,
                };
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.AccessEvent?> GetAccessEventAsync(Guid id)
    {
        var item = await data.AccessEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccessEventId == id);
        if (item == null) return null;

        // Get source system name and current owner separately
        EFModels.EFModels.SourceSystemItem? sourceSystem = null;
        if (item.SourceSystemId != default) {
            sourceSystem = await data.SourceSystems.FirstOrDefaultAsync(x => x.SourceSystemId == item.SourceSystemId);
        }

        return new DataObjects.AccessEvent
        {
            AccessEventId = item.AccessEventId,
            SourceSystemId = item.SourceSystemId,
            SourceEventId = item.SourceEventId,
            AccessedAt = item.AccessedAt,
            ReceivedAt = item.ReceivedAt,
            UserId = item.UserId,
            UserName = item.UserName,
            UserEmail = item.UserEmail,
            UserDepartment = item.UserDepartment,
            SubjectId = item.SubjectId,
            SubjectType = item.SubjectType,
            SubjectIds = item.SubjectIds,
            SubjectCount = item.SubjectCount,
            DataCategory = item.DataCategory,
            AccessType = item.AccessType,
            Purpose = item.Purpose,
            IpAddress = item.IpAddress,
            AdditionalData = item.AdditionalData,
            AgreementText = item.AgreementText,
            AgreementAcknowledgedAt = item.AgreementAcknowledgedAt,
            DataOwnerName = item.DataOwnerName,
            DataOwnerEmail = item.DataOwnerEmail,
            DataOwnerDepartment = item.DataOwnerDepartment,
            SourceSystemName = sourceSystem?.Name ?? string.Empty,
            CurrentDataOwnerName = sourceSystem?.DataOwnerName ?? string.Empty,
            CurrentDataOwnerEmail = sourceSystem?.DataOwnerEmail ?? string.Empty,
            CurrentDataOwnerDepartment = sourceSystem?.DataOwnerDepartment ?? string.Empty,
        };
    }

    public async Task<DataObjects.AccessEvent?> SaveAccessEventAsync(DataObjects.AccessEvent dto)
    {
        EFModels.EFModels.AccessEventItem item;
        var isNew = dto.AccessEventId == default;

        if (isNew) {
            item = new EFModels.EFModels.AccessEventItem();
            item.AccessEventId = Guid.NewGuid();
            item.ReceivedAt = DateTime.UtcNow;
            data.AccessEvents.Add(item);
        } else {
            item = await data.AccessEvents.FindAsync(dto.AccessEventId);
            if (item == null) return null;
        }

        item.SourceSystemId = dto.SourceSystemId;
        item.SourceEventId = dto.SourceEventId ?? string.Empty;
        // Ensure AccessedAt is stored as UTC
        item.AccessedAt = dto.AccessedAt.Kind == DateTimeKind.Utc 
            ? dto.AccessedAt 
            : DateTime.SpecifyKind(dto.AccessedAt, DateTimeKind.Utc);
        item.UserId = dto.UserId ?? string.Empty;
        item.UserName = dto.UserName ?? string.Empty;
        item.UserEmail = dto.UserEmail ?? string.Empty;
        item.UserDepartment = dto.UserDepartment ?? string.Empty;
        item.SubjectId = dto.SubjectId ?? string.Empty;
        item.SubjectType = dto.SubjectType ?? string.Empty;
        item.SubjectIds = dto.SubjectIds ?? string.Empty;
        item.SubjectCount = dto.SubjectCount > 0 ? dto.SubjectCount : 1;
        item.DataCategory = dto.DataCategory ?? string.Empty;
        item.AccessType = dto.AccessType ?? string.Empty;
        item.Purpose = dto.Purpose ?? string.Empty;
        item.IpAddress = dto.IpAddress ?? string.Empty;
        item.AdditionalData = dto.AdditionalData ?? string.Empty;
        item.AgreementText = dto.AgreementText ?? string.Empty;
        // Ensure AgreementAcknowledgedAt is stored as UTC
        item.AgreementAcknowledgedAt = dto.AgreementAcknowledgedAt.HasValue
            ? (dto.AgreementAcknowledgedAt.Value.Kind == DateTimeKind.Utc
                ? dto.AgreementAcknowledgedAt.Value
                : DateTime.SpecifyKind(dto.AgreementAcknowledgedAt.Value, DateTimeKind.Utc))
            : null;

        // Snapshot the data owner at the time of access. Caller-supplied values win;
        // otherwise capture the source system's current data owner on new events.
        item.DataOwnerName = (dto.DataOwnerName ?? string.Empty).Trim();
        item.DataOwnerEmail = (dto.DataOwnerEmail ?? string.Empty).Trim();
        item.DataOwnerDepartment = (dto.DataOwnerDepartment ?? string.Empty).Trim();

        var ownerSnapshotEmpty = item.DataOwnerName == string.Empty
            && item.DataOwnerEmail == string.Empty
            && item.DataOwnerDepartment == string.Empty;

        if (isNew && ownerSnapshotEmpty && dto.SourceSystemId != Guid.Empty) {
            var snapshotSource = await data.SourceSystems.FindAsync(dto.SourceSystemId);
            if (snapshotSource != null) {
                item.DataOwnerName = snapshotSource.DataOwnerName;
                item.DataOwnerEmail = snapshotSource.DataOwnerEmail;
                item.DataOwnerDepartment = snapshotSource.DataOwnerDepartment;
            }
        }

        if (isNew) {
            // New audit records join the tamper-evident chain. Edits of existing
            // records deliberately do NOT rehash - verification flags them.
            await _chainLock.WaitAsync();
            try {
                await AssignChainPositionAsync(item);
                await data.SaveChangesAsync();
            } finally {
                _chainLock.Release();
            }
        } else {
            await data.SaveChangesAsync();
        }

        // Update LastEventReceivedAt on source system (works with all providers including InMemory)
        string alertSourceName = string.Empty;
        if (isNew && dto.SourceSystemId != Guid.Empty) {
            var sourceSystem = await data.SourceSystems.FindAsync(dto.SourceSystemId);
            if (sourceSystem != null) {
                sourceSystem.LastEventReceivedAt = DateTime.UtcNow;
                alertSourceName = sourceSystem.Name;
                await data.SaveChangesAsync();
            }
        }

        if (isNew) {
            // Webhook alerts (large bulk access, after-hours) - best-effort, detached.
            await QueueEventAlertsAsync(new[] { (item, alertSourceName) });
        }

        // Update DataSubject stats - handle both single and bulk access
        if (isNew) {
            // Parse SubjectIds if it's a bulk access
            if (!string.IsNullOrEmpty(dto.SubjectIds)) {
                try {
                    var subjectIdList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dto.SubjectIds);
                    if (subjectIdList?.Count > 0) {
                        await UpdateDataSubjectStatsAsync(subjectIdList, dto.SubjectType);
                    }
                } catch {
                    // If JSON parsing fails, fall back to single subject
                    if (!string.IsNullOrEmpty(dto.SubjectId)) {
                        await UpdateDataSubjectStatsAsync(dto.SubjectId, dto.SubjectType);
                    }
                }
            } else if (!string.IsNullOrEmpty(dto.SubjectId) && dto.SubjectId != "BULK") {
                await UpdateDataSubjectStatsAsync(dto.SubjectId, dto.SubjectType);
            }
        }
        
        if (isNew) {
            await NotifyGlbaChangeAsync(DataObjects.SignalRUpdateType.GlbaAccessEvent, item.AccessEventId, "New");
        }

        // Return updated DTO
        dto.AccessEventId = item.AccessEventId;
        dto.ReceivedAt = item.ReceivedAt;
        dto.SubjectCount = item.SubjectCount;
        dto.DataOwnerName = item.DataOwnerName;
        dto.DataOwnerEmail = item.DataOwnerEmail;
        dto.DataOwnerDepartment = item.DataOwnerDepartment;
        return dto;
    }

    /// <summary>
    /// Saves many new AccessEvents in a single round trip, then updates
    /// SourceSystem and DataSubject statistics in aggregate.
    /// Intended for seeding/demo data and high-volume internal inserts.
    /// </summary>
    public async Task<DataObjects.AccessEventBulkResult> SaveAccessEventsAsync(List<DataObjects.AccessEvent> items)
    {
        var output = new DataObjects.AccessEventBulkResult();

        if (items == null || items.Count == 0) {
            output.Success = true;
            return output;
        }

        try {
            var now = DateTime.UtcNow;

            // Tracks how many times each subject was touched, its type, and the
            // earliest/latest access times, so subject statistics can be applied
            // in one pass at the end.
            var subjectHits = new Dictionary<string, int>();
            var subjectTypes = new Dictionary<string, string>();
            var subjectFirstAccess = new Dictionary<string, DateTime>();
            var subjectLastAccess = new Dictionary<string, DateTime>();

            void NoteSubject(string? subjectId, string? subjectType, DateTime accessedAt)
            {
                if (string.IsNullOrWhiteSpace(subjectId) || subjectId == "BULK" || subjectId == "SYSTEM") return;
                subjectHits[subjectId] = subjectHits.TryGetValue(subjectId, out var c) ? c + 1 : 1;
                if (!string.IsNullOrWhiteSpace(subjectType) && !subjectTypes.ContainsKey(subjectId)) {
                    subjectTypes[subjectId] = subjectType;
                }
                if (!subjectFirstAccess.TryGetValue(subjectId, out var first) || accessedAt < first) {
                    subjectFirstAccess[subjectId] = accessedAt;
                }
                if (!subjectLastAccess.TryGetValue(subjectId, out var last) || accessedAt > last) {
                    subjectLastAccess[subjectId] = accessedAt;
                }
            }

            var records = new List<EFModels.EFModels.AccessEventItem>(items.Count);
            var sourceSystemIds = new HashSet<Guid>();

            // Prefetch the source systems for the batch so each event can snapshot
            // the data owner at the time of access.
            var batchSourceIds = items.Select(x => x.SourceSystemId).Where(x => x != Guid.Empty).Distinct().ToList();
            var batchSources = batchSourceIds.Count > 0
                ? await data.SourceSystems.Where(x => batchSourceIds.Contains(x.SourceSystemId)).ToDictionaryAsync(x => x.SourceSystemId)
                : new Dictionary<Guid, EFModels.EFModels.SourceSystemItem>();

            foreach (var dto in items) {
                var accessedAt = dto.AccessedAt == default
                    ? now
                    : (dto.AccessedAt.Kind == DateTimeKind.Utc
                        ? dto.AccessedAt
                        : DateTime.SpecifyKind(dto.AccessedAt, DateTimeKind.Utc));

                // Snapshot the data owner at the time of access. Caller-supplied
                // values win; otherwise capture the source system's current owner.
                var ownerName = (dto.DataOwnerName ?? string.Empty).Trim();
                var ownerEmail = (dto.DataOwnerEmail ?? string.Empty).Trim();
                var ownerDepartment = (dto.DataOwnerDepartment ?? string.Empty).Trim();
                if (ownerName == string.Empty && ownerEmail == string.Empty && ownerDepartment == string.Empty
                    && batchSources.TryGetValue(dto.SourceSystemId, out var ownerSource)) {
                    ownerName = ownerSource.DataOwnerName;
                    ownerEmail = ownerSource.DataOwnerEmail;
                    ownerDepartment = ownerSource.DataOwnerDepartment;
                }

                var record = new EFModels.EFModels.AccessEventItem {
                    AccessEventId = Guid.NewGuid(),
                    SourceSystemId = dto.SourceSystemId,
                    SourceEventId = dto.SourceEventId ?? string.Empty,
                    AccessedAt = accessedAt,
                    ReceivedAt = now,
                    UserId = dto.UserId ?? string.Empty,
                    UserName = dto.UserName ?? string.Empty,
                    UserEmail = dto.UserEmail ?? string.Empty,
                    UserDepartment = dto.UserDepartment ?? string.Empty,
                    SubjectId = dto.SubjectId ?? string.Empty,
                    SubjectType = dto.SubjectType ?? string.Empty,
                    SubjectIds = dto.SubjectIds ?? string.Empty,
                    SubjectCount = dto.SubjectCount > 0 ? dto.SubjectCount : 1,
                    DataCategory = dto.DataCategory ?? string.Empty,
                    AccessType = dto.AccessType ?? string.Empty,
                    Purpose = dto.Purpose ?? string.Empty,
                    IpAddress = dto.IpAddress ?? string.Empty,
                    AdditionalData = string.IsNullOrWhiteSpace(dto.AdditionalData) ? "{}" : dto.AdditionalData,
                    AgreementText = dto.AgreementText ?? string.Empty,
                    AgreementAcknowledgedAt = dto.AgreementAcknowledgedAt.HasValue
                        ? (dto.AgreementAcknowledgedAt.Value.Kind == DateTimeKind.Utc
                            ? dto.AgreementAcknowledgedAt.Value
                            : DateTime.SpecifyKind(dto.AgreementAcknowledgedAt.Value, DateTimeKind.Utc))
                        : null,
                    DataOwnerName = ownerName,
                    DataOwnerEmail = ownerEmail,
                    DataOwnerDepartment = ownerDepartment,
                };

                records.Add(record);

                if (dto.SourceSystemId != Guid.Empty) {
                    sourceSystemIds.Add(dto.SourceSystemId);
                }

                // Record subject hits for bulk (JSON list) or single-subject events.
                if (!string.IsNullOrEmpty(dto.SubjectIds)) {
                    try {
                        var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(dto.SubjectIds);
                        if (ids != null && ids.Count > 0) {
                            foreach (var id in ids) {
                                NoteSubject(id, dto.SubjectType, accessedAt);
                            }
                        } else {
                            NoteSubject(dto.SubjectId, dto.SubjectType, accessedAt);
                        }
                    } catch {
                        NoteSubject(dto.SubjectId, dto.SubjectType, accessedAt);
                    }
                } else {
                    NoteSubject(dto.SubjectId, dto.SubjectType, accessedAt);
                }
            }

            // Single insert for all events, chained under one lock so the whole
            // batch gets consecutive tamper-evident sequence numbers per source.
            await _chainLock.WaitAsync();
            try {
                var chainTails = new Dictionary<Guid, EFModels.EFModels.AccessEventItem>();
                foreach (var record in records) {
                    chainTails.TryGetValue(record.SourceSystemId, out var pendingTail);
                    await AssignChainPositionAsync(record, pendingTail);
                    chainTails[record.SourceSystemId] = record;
                }

                data.AccessEvents.AddRange(records);
                await data.SaveChangesAsync();
            } finally {
                _chainLock.Release();
            }
            output.Saved = records.Count;

            // Webhook alerts (large bulk access, after-hours) - best-effort, detached.
            await QueueEventAlertsAsync(records.Select(r =>
                (r, batchSources.TryGetValue(r.SourceSystemId, out var alertSource) ? alertSource.Name : string.Empty)));

            // Update LastEventReceivedAt once per source system.
            if (sourceSystemIds.Count > 0) {
                var sources = await data.SourceSystems
                    .Where(x => sourceSystemIds.Contains(x.SourceSystemId))
                    .ToListAsync();
                foreach (var source in sources) {
                    source.LastEventReceivedAt = now;
                }
            }

            // Apply subject statistics in one pass rather than per event.
            if (subjectHits.Count > 0) {
                var ids = subjectHits.Keys.ToList();
                var existing = await data.DataSubjects
                    .Where(x => ids.Contains(x.ExternalId))
                    .ToDictionaryAsync(x => x.ExternalId);

                // Recalculate unique accessor counts for the affected subjects in one
                // grouped query over direct-match events. Multi-subject (JSON) events
                // are not matched here, so the value is treated as a floor and never
                // allowed to regress below the previously stored count.
                var accessorCounts = await data.AccessEvents
                    .Where(x => ids.Contains(x.SubjectId))
                    .GroupBy(x => x.SubjectId)
                    .Select(g => new { SubjectId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                    .ToDictionaryAsync(x => x.SubjectId, x => x.Count);

                foreach (var kvp in subjectHits) {
                    var externalId = kvp.Key;
                    var hits = kvp.Value;
                    subjectTypes.TryGetValue(externalId, out var type);
                    var firstAccess = subjectFirstAccess.GetValueOrDefault(externalId, now);
                    var lastAccess = subjectLastAccess.GetValueOrDefault(externalId, now);
                    var uniqueAccessors = Math.Max(1, accessorCounts.GetValueOrDefault(externalId, 0));

                    if (existing.TryGetValue(externalId, out var subject)) {
                        if (firstAccess < subject.FirstAccessedAt) {
                            subject.FirstAccessedAt = firstAccess;
                        }
                        if (lastAccess > subject.LastAccessedAt) {
                            subject.LastAccessedAt = lastAccess;
                        }
                        subject.TotalAccessCount += hits;
                        subject.UniqueAccessorCount = Math.Max(subject.UniqueAccessorCount, uniqueAccessors);
                        if (!string.IsNullOrEmpty(type) && string.IsNullOrEmpty(subject.SubjectType)) {
                            subject.SubjectType = type;
                        }
                    } else {
                        data.DataSubjects.Add(new EFModels.EFModels.DataSubjectItem {
                            DataSubjectId = Guid.NewGuid(),
                            ExternalId = externalId,
                            SubjectType = string.IsNullOrEmpty(type) ? "Student" : type,
                            FirstAccessedAt = firstAccess,
                            LastAccessedAt = lastAccess,
                            TotalAccessCount = hits,
                            UniqueAccessorCount = uniqueAccessors,
                        });
                    }
                }

                output.SubjectsAffected = subjectHits.Count;
            }

            await data.SaveChangesAsync();
            output.Success = true;

            if (output.Saved > 0) {
                await NotifyGlbaChangeAsync(DataObjects.SignalRUpdateType.GlbaAccessEvent, null, "Batch:" + output.Saved.ToString());
            }
        } catch (Exception ex) {
            output.Success = false;
            output.Message = ex.Message;
            if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message)) {
                output.Message += " " + ex.InnerException.Message;
            }
        }

        return output;
    }

    public async Task<bool> DeleteAccessEventAsync(Guid id)
    {
        var item = await data.AccessEvents.FindAsync(id);
        if (item == null) return false;
        
        var subjectId = item.SubjectId;
        
        data.AccessEvents.Remove(item);
        await data.SaveChangesAsync();
        
        // Update DataSubject stats
        if (!string.IsNullOrEmpty(subjectId))
        {
            await UpdateDataSubjectStatsAsync(subjectId);
        }
        
        return true;
    }

    public async Task<List<DataObjects.AccessEventLookup>> GetAccessEventLookupsAsync()
    {
        return await data.AccessEvents
            .Select(x => new DataObjects.AccessEventLookup
            {
                AccessEventId = x.AccessEventId,
                DisplayName = x.UserName
            })
            .ToListAsync();
    }

    #endregion
}

// ---- DataAccess.DataSubjects ----

// ============================================================================
// DATASUBJECT - DATA ACCESS
// DataSubject CRUD data access methods and their IDataAccess members.
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.DataSubjectFilterResult> GetDataSubjectsAsync(DataObjects.DataSubjectFilter filter);
    Task<DataObjects.DataSubject?> GetDataSubjectAsync(Guid id);
    Task<List<DataObjects.DataSubjectLookup>> GetDataSubjectLookupsAsync();
    Task<DataObjects.DataSubject?> SaveDataSubjectAsync(DataObjects.DataSubject dto);
    Task<bool> DeleteDataSubjectAsync(Guid id);
}

public partial class DataAccess
{
    #region DataSubject

    public async Task<DataObjects.DataSubjectFilterResult> GetDataSubjectsAsync(DataObjects.DataSubjectFilter filter)
    {
        var query = data.DataSubjects
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.ExternalId.Contains(filter.Search) || x.SubjectType.Contains(filter.Search));
        }

        // Apply advanced filters
        if (!string.IsNullOrWhiteSpace(filter.SubjectTypeFilter))
            query = query.Where(x => x.SubjectType == filter.SubjectTypeFilter);
        if (filter.MinTotalAccesses.HasValue)
            query = query.Where(x => x.TotalAccessCount >= filter.MinTotalAccesses.Value);
        if (filter.MaxTotalAccesses.HasValue)
            query = query.Where(x => x.TotalAccessCount <= filter.MaxTotalAccesses.Value);
        if (filter.MinUniqueAccessors.HasValue)
            query = query.Where(x => x.UniqueAccessorCount >= filter.MinUniqueAccessors.Value);
        if (filter.MaxUniqueAccessors.HasValue)
            query = query.Where(x => x.UniqueAccessorCount <= filter.MaxUniqueAccessors.Value);
        if (filter.LastAccessAfter.HasValue)
            query = query.Where(x => x.LastAccessedAt >= filter.LastAccessAfter.Value);
        if (filter.LastAccessBefore.HasValue)
            query = query.Where(x => x.LastAccessedAt <= filter.LastAccessBefore.Value);
        if (filter.FirstAccessAfter.HasValue)
            query = query.Where(x => x.FirstAccessedAt >= filter.FirstAccessAfter.Value);
        if (filter.FirstAccessBefore.HasValue)
            query = query.Where(x => x.FirstAccessedAt <= filter.FirstAccessBefore.Value);

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "ExternalId" => filter.SortDescending ? query.OrderByDescending(x => x.ExternalId) : query.OrderBy(x => x.ExternalId),
            "SubjectType" => filter.SortDescending ? query.OrderByDescending(x => x.SubjectType) : query.OrderBy(x => x.SubjectType),
            "FirstAccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.FirstAccessedAt) : query.OrderBy(x => x.FirstAccessedAt),
            "LastAccessedAt" => filter.SortDescending ? query.OrderByDescending(x => x.LastAccessedAt) : query.OrderBy(x => x.LastAccessedAt),
            "TotalAccessCount" => filter.SortDescending ? query.OrderByDescending(x => x.TotalAccessCount) : query.OrderBy(x => x.TotalAccessCount),
            "UniqueAccessorCount" => filter.SortDescending ? query.OrderByDescending(x => x.UniqueAccessorCount) : query.OrderBy(x => x.UniqueAccessorCount),
            _ => query.OrderByDescending(x => x.DataSubjectId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.DataSubjectFilterResult
        {
            Records = items.Select(x => new DataObjects.DataSubject
            {
                DataSubjectId = x.DataSubjectId,
                ExternalId = x.ExternalId,
                SubjectType = x.SubjectType,
                FirstAccessedAt = x.FirstAccessedAt,
                LastAccessedAt = x.LastAccessedAt,
                TotalAccessCount = x.TotalAccessCount,
                UniqueAccessorCount = x.UniqueAccessorCount,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.DataSubject?> GetDataSubjectAsync(Guid id)
    {
        var item = await data.DataSubjects
            .FirstOrDefaultAsync(x => x.DataSubjectId == id);
        if (item == null) return null;

        return new DataObjects.DataSubject
        {
            DataSubjectId = item.DataSubjectId,
            ExternalId = item.ExternalId,
            SubjectType = item.SubjectType,
            FirstAccessedAt = item.FirstAccessedAt,
            LastAccessedAt = item.LastAccessedAt,
            TotalAccessCount = item.TotalAccessCount,
            UniqueAccessorCount = item.UniqueAccessorCount,
        };
    }

    public async Task<DataObjects.DataSubject?> SaveDataSubjectAsync(DataObjects.DataSubject dto)
    {
        EFModels.EFModels.DataSubjectItem item;
        var isNew = dto.DataSubjectId == default;

        if (isNew) {
            item = new EFModels.EFModels.DataSubjectItem();
            item.DataSubjectId = Guid.NewGuid();
            item.FirstAccessedAt = DateTime.UtcNow;
            item.LastAccessedAt = DateTime.UtcNow;
            item.TotalAccessCount = 0;
            item.UniqueAccessorCount = 0;
            data.DataSubjects.Add(item);
        } else {
            item = await data.DataSubjects.FindAsync(dto.DataSubjectId);
            if (item == null) return null;
        }

        // Only update user-editable fields
        item.ExternalId = dto.ExternalId ?? string.Empty;
        item.SubjectType = dto.SubjectType ?? string.Empty;
        // Don't update statistics - those are managed by the system

        await data.SaveChangesAsync();
        
        // Return updated DTO with all values
        dto.DataSubjectId = item.DataSubjectId;
        dto.FirstAccessedAt = item.FirstAccessedAt;
        dto.LastAccessedAt = item.LastAccessedAt;
        dto.TotalAccessCount = item.TotalAccessCount;
        dto.UniqueAccessorCount = item.UniqueAccessorCount;
        return dto;
    }

    public async Task<bool> DeleteDataSubjectAsync(Guid id)
    {
        var item = await data.DataSubjects.FindAsync(id);
        if (item == null) return false;
        data.DataSubjects.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.DataSubjectLookup>> GetDataSubjectLookupsAsync()
    {
        return await data.DataSubjects
            .Select(x => new DataObjects.DataSubjectLookup
            {
                DataSubjectId = x.DataSubjectId,
                DisplayName = x.ExternalId
            })
            .ToListAsync();
    }

    #endregion
}

// ---- DataAccess.ComplianceReports ----

// ============================================================================
// COMPLIANCEREPORT - DATA ACCESS
// ComplianceReport CRUD data access methods and their IDataAccess members.
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.ComplianceReportFilterResult> GetComplianceReportsAsync(DataObjects.ComplianceReportFilter filter);
    Task<DataObjects.ComplianceReport?> GetComplianceReportAsync(Guid id);
    Task<List<DataObjects.ComplianceReportLookup>> GetComplianceReportLookupsAsync();
    Task<DataObjects.ComplianceReport?> SaveComplianceReportAsync(DataObjects.ComplianceReport dto);
    Task<bool> DeleteComplianceReportAsync(Guid id);
}

public partial class DataAccess
{
    #region ComplianceReport

    public async Task<DataObjects.ComplianceReportFilterResult> GetComplianceReportsAsync(DataObjects.ComplianceReportFilter filter)
    {
        var query = data.ComplianceReports
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(x => x.ReportType.Contains(filter.Search) || x.GeneratedBy.Contains(filter.Search) || x.ReportData.Contains(filter.Search));
        }

        // Apply advanced filters
        if (!string.IsNullOrWhiteSpace(filter.ReportTypeFilter))
            query = query.Where(x => x.ReportType == filter.ReportTypeFilter);
        if (filter.GeneratedAfter.HasValue)
            query = query.Where(x => x.GeneratedAt >= filter.GeneratedAfter.Value);
        if (filter.GeneratedBefore.HasValue)
            query = query.Where(x => x.GeneratedAt <= filter.GeneratedBefore.Value);
        if (filter.MinTotalEvents.HasValue)
            query = query.Where(x => x.TotalEvents >= filter.MinTotalEvents.Value);
        if (filter.MaxTotalEvents.HasValue)
            query = query.Where(x => x.TotalEvents <= filter.MaxTotalEvents.Value);
        if (filter.MinUniqueUsers.HasValue)
            query = query.Where(x => x.UniqueUsers >= filter.MinUniqueUsers.Value);
        if (filter.MaxUniqueUsers.HasValue)
            query = query.Where(x => x.UniqueUsers <= filter.MaxUniqueUsers.Value);
        if (filter.MinUniqueSubjects.HasValue)
            query = query.Where(x => x.UniqueSubjects >= filter.MinUniqueSubjects.Value);
        if (filter.MaxUniqueSubjects.HasValue)
            query = query.Where(x => x.UniqueSubjects <= filter.MaxUniqueSubjects.Value);

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "ReportType" => filter.SortDescending ? query.OrderByDescending(x => x.ReportType) : query.OrderBy(x => x.ReportType),
            "GeneratedAt" => filter.SortDescending ? query.OrderByDescending(x => x.GeneratedAt) : query.OrderBy(x => x.GeneratedAt),
            "GeneratedBy" => filter.SortDescending ? query.OrderByDescending(x => x.GeneratedBy) : query.OrderBy(x => x.GeneratedBy),
            "PeriodStart" => filter.SortDescending ? query.OrderByDescending(x => x.PeriodStart) : query.OrderBy(x => x.PeriodStart),
            "PeriodEnd" => filter.SortDescending ? query.OrderByDescending(x => x.PeriodEnd) : query.OrderBy(x => x.PeriodEnd),
            "TotalEvents" => filter.SortDescending ? query.OrderByDescending(x => x.TotalEvents) : query.OrderBy(x => x.TotalEvents),
            "UniqueUsers" => filter.SortDescending ? query.OrderByDescending(x => x.UniqueUsers) : query.OrderBy(x => x.UniqueUsers),
            "UniqueSubjects" => filter.SortDescending ? query.OrderByDescending(x => x.UniqueSubjects) : query.OrderBy(x => x.UniqueSubjects),
            _ => query.OrderByDescending(x => x.ComplianceReportId)
        };

        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();

        return new DataObjects.ComplianceReportFilterResult
        {
            Records = items.Select(x => new DataObjects.ComplianceReport
            {
                ComplianceReportId = x.ComplianceReportId,
                ReportType = x.ReportType,
                GeneratedAt = x.GeneratedAt,
                GeneratedBy = x.GeneratedBy,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd,
                TotalEvents = x.TotalEvents,
                UniqueUsers = x.UniqueUsers,
                UniqueSubjects = x.UniqueSubjects,
                ReportData = x.ReportData,
                FileUrl = x.FileUrl,
            }).ToList(),
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.ComplianceReport?> GetComplianceReportAsync(Guid id)
    {
        var item = await data.ComplianceReports
            .FirstOrDefaultAsync(x => x.ComplianceReportId == id);
        if (item == null) return null;

        return new DataObjects.ComplianceReport
        {
            ComplianceReportId = item.ComplianceReportId,
            ReportType = item.ReportType,
            GeneratedAt = item.GeneratedAt,
            GeneratedBy = item.GeneratedBy,
            PeriodStart = item.PeriodStart,
            PeriodEnd = item.PeriodEnd,
            TotalEvents = item.TotalEvents,
            UniqueUsers = item.UniqueUsers,
            UniqueSubjects = item.UniqueSubjects,
            ReportData = item.ReportData,
            FileUrl = item.FileUrl,
        };
    }

    public async Task<DataObjects.ComplianceReport?> SaveComplianceReportAsync(DataObjects.ComplianceReport dto)
    {
        EFModels.EFModels.ComplianceReportItem item;
        var isNew = dto.ComplianceReportId == default;

        // Ensure period dates are treated as UTC (end of day for PeriodEnd)
        var periodStartUtc = dto.PeriodStart.Kind == DateTimeKind.Utc 
            ? dto.PeriodStart.Date 
            : DateTime.SpecifyKind(dto.PeriodStart.Date, DateTimeKind.Utc);
        var periodEndUtc = dto.PeriodEnd.Kind == DateTimeKind.Utc 
            ? dto.PeriodEnd.Date.AddDays(1).AddTicks(-1) 
            : DateTime.SpecifyKind(dto.PeriodEnd.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        if (isNew) {
            item = new EFModels.EFModels.ComplianceReportItem();
            item.ComplianceReportId = Guid.NewGuid();
            item.GeneratedAt = DateTime.UtcNow;
            item.GeneratedBy = "System"; // TODO: Get from CurrentUser when available
            
            // Calculate statistics for the report period (using UTC dates)
            var stats = await CalculateReportStatisticsAsync(periodStartUtc, periodEndUtc);
            item.TotalEvents = stats.TotalEvents;
            item.UniqueUsers = stats.UniqueUsers;
            item.UniqueSubjects = stats.UniqueSubjects;
            
            data.ComplianceReports.Add(item);
        } else {
            item = await data.ComplianceReports.FindAsync(dto.ComplianceReportId);
            if (item == null) return null;
        }

        // User-editable fields - store dates as UTC
        item.ReportType = dto.ReportType ?? string.Empty;
        item.PeriodStart = periodStartUtc;
        item.PeriodEnd = DateTime.SpecifyKind(dto.PeriodEnd.Date, DateTimeKind.Utc); // Store just the date part
        item.ReportData = dto.ReportData ?? string.Empty;
        item.FileUrl = dto.FileUrl ?? string.Empty;
        // Don't update GeneratedAt, GeneratedBy, or statistics for existing records

        await data.SaveChangesAsync();
        
        // Return updated DTO
        dto.ComplianceReportId = item.ComplianceReportId;
        dto.GeneratedAt = item.GeneratedAt;
        dto.GeneratedBy = item.GeneratedBy;
        dto.TotalEvents = item.TotalEvents;
        dto.UniqueUsers = item.UniqueUsers;
        dto.UniqueSubjects = item.UniqueSubjects;
        dto.PeriodStart = item.PeriodStart;
        dto.PeriodEnd = item.PeriodEnd;
        return dto;
    }

    /// <summary>
    /// Calculate report statistics for a given period.
    /// Period dates should already be in UTC.
    /// </summary>
    private async Task<(int TotalEvents, int UniqueUsers, int UniqueSubjects)> CalculateReportStatisticsAsync(
        DateTime periodStartUtc, DateTime periodEndUtc){
        var events = await data.AccessEvents
            .Where(x => x.AccessedAt >= periodStartUtc && x.AccessedAt <= periodEndUtc)
            .ToListAsync();

        return (
            TotalEvents: events.Count,
            UniqueUsers: events.Select(x => x.UserId).Distinct().Count(),
            UniqueSubjects: events.Select(x => x.SubjectId).Distinct().Count()
        );
    }

    public async Task<bool> DeleteComplianceReportAsync(Guid id)
    {
        var item = await data.ComplianceReports.FindAsync(id);
        if (item == null) return false;
        data.ComplianceReports.Remove(item);
        await data.SaveChangesAsync();
        return true;
    }

    public async Task<List<DataObjects.ComplianceReportLookup>> GetComplianceReportLookupsAsync()
    {
        return await data.ComplianceReports
            .Select(x => new DataObjects.ComplianceReportLookup
            {
                ComplianceReportId = x.ComplianceReportId,
                DisplayName = x.ReportType
            })
            .ToListAsync();
    }

    #endregion
}

// ---- DataAccess.ComplianceReports.Generation ----

// ============================================================================
// GLBA COMPLIANCE REPORT GENERATION
// Produces the actual report content (CSV detail export and PDF summary) for
// a ComplianceReport record, and stores the summary JSON in ReportData.
// ============================================================================

public partial interface IDataAccess
{
    /// <summary>Generates a CSV export of every access event in the report's period.</summary>
    Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportCsvAsync(Guid reportId, string? generatedBy = null);

    /// <summary>Generates a PDF summary report for the report's period.</summary>
    Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportPdfAsync(Guid reportId, string? generatedBy = null);

    /// <summary>
    /// Generates a PDF of one data subject's complete access history (a
    /// DSAR/audit-style export): every recorded access to that person's data,
    /// including the data-owner snapshot for each event.
    /// </summary>
    Task<DataObjects.ComplianceReportExport?> GenerateSubjectAccessHistoryPdfAsync(string subjectExternalId, string? generatedBy = null);
}

public partial class DataAccess
{
    #region Compliance Report Generation

    private sealed class ReportSourceData
    {
        public EFModels.EFModels.ComplianceReportItem Report = null!;
        public List<EFModels.EFModels.AccessEventItem> Events = new();
        public Dictionary<Guid, EFModels.EFModels.SourceSystemItem> Sources = new();
        public DateTime PeriodStartUtc;
        public DateTime PeriodEndUtc;
    }

    private async Task<ReportSourceData?> LoadReportSourceDataAsync(Guid reportId)
    {
        var report = await data.ComplianceReports.FindAsync(reportId);
        if (report == null) return null;

        var periodStartUtc = DateTime.SpecifyKind(report.PeriodStart.Date, DateTimeKind.Utc);
        var periodEndUtc = DateTime.SpecifyKind(report.PeriodEnd.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.AccessedAt >= periodStartUtc && x.AccessedAt <= periodEndUtc)
            .OrderBy(x => x.AccessedAt)
            .ToListAsync();

        var sourceIds = events.Select(x => x.SourceSystemId).Distinct().ToList();
        var sources = await data.SourceSystems
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.SourceSystemId))
            .ToDictionaryAsync(x => x.SourceSystemId);

        return new ReportSourceData {
            Report = report,
            Events = events,
            Sources = sources,
            PeriodStartUtc = periodStartUtc,
            PeriodEndUtc = periodEndUtc,
        };
    }

    /// <summary>
    /// Refreshes the stored statistics and summary JSON on the report record so
    /// the ComplianceReports table reflects what was actually generated.
    /// </summary>
    private async Task UpdateReportRecordAsync(ReportSourceData src, string? generatedBy)
    {
        var report = src.Report;
        report.GeneratedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(generatedBy)) {
            report.GeneratedBy = generatedBy;
        }
        report.TotalEvents = src.Events.Count;
        report.UniqueUsers = src.Events.Select(x => x.UserId).Distinct().Count();
        report.UniqueSubjects = src.Events.Where(x => x.SubjectId != "SYSTEM" && x.SubjectId != "BULK")
            .Select(x => x.SubjectId).Distinct().Count();

        var summary = new {
            generatedAt = report.GeneratedAt,
            periodStart = src.PeriodStartUtc,
            periodEnd = src.PeriodEndUtc,
            totalEvents = report.TotalEvents,
            uniqueUsers = report.UniqueUsers,
            uniqueSubjects = report.UniqueSubjects,
            byAccessType = src.Events.GroupBy(x => x.AccessType)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => string.IsNullOrEmpty(g.Key) ? "(none)" : g.Key, g => g.Count()),
            byDataCategory = src.Events.Where(x => !string.IsNullOrEmpty(x.DataCategory))
                .GroupBy(x => x.DataCategory)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count()),
        };
        report.ReportData = SerializeObject(summary);

        await data.SaveChangesAsync();
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportCsvAsync(Guid reportId, string? generatedBy = null)
    {
        var src = await LoadReportSourceDataAsync(reportId);
        if (src == null) return null;

        static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains('"') || value.Contains(',') || value.Contains('\n')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("AccessedAt (UTC),ReceivedAt (UTC),SourceSystem,SourceEventId,UserId,UserName,UserEmail,UserDepartment,SubjectId,SubjectCount,SubjectType,DataCategory,AccessType,Purpose,IpAddress,DataOwnerName,DataOwnerEmail,DataOwnerDepartment");

        foreach (var evt in src.Events) {
            src.Sources.TryGetValue(evt.SourceSystemId, out var source);
            csv.Append(evt.AccessedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            csv.Append(evt.ReceivedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(',');
            csv.Append(Escape(source?.Name)).Append(',');
            csv.Append(Escape(evt.SourceEventId)).Append(',');
            csv.Append(Escape(evt.UserId)).Append(',');
            csv.Append(Escape(evt.UserName)).Append(',');
            csv.Append(Escape(evt.UserEmail)).Append(',');
            csv.Append(Escape(evt.UserDepartment)).Append(',');
            csv.Append(Escape(evt.SubjectId)).Append(',');
            csv.Append(evt.SubjectCount).Append(',');
            csv.Append(Escape(evt.SubjectType)).Append(',');
            csv.Append(Escape(evt.DataCategory)).Append(',');
            csv.Append(Escape(evt.AccessType)).Append(',');
            csv.Append(Escape(evt.Purpose)).Append(',');
            csv.Append(Escape(evt.IpAddress)).Append(',');
            csv.Append(Escape(evt.DataOwnerName)).Append(',');
            csv.Append(Escape(evt.DataOwnerEmail)).Append(',');
            csv.AppendLine(Escape(evt.DataOwnerDepartment));
        }

        await UpdateReportRecordAsync(src, generatedBy);

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-report-{src.Report.PeriodStart:yyyyMMdd}-{src.Report.PeriodEnd:yyyyMMdd}.csv",
            ContentType = "text/csv",
            Bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString()),
        };
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateComplianceReportPdfAsync(Guid reportId, string? generatedBy = null)
    {
        var src = await LoadReportSourceDataAsync(reportId);
        if (src == null) return null;

        var report = src.Report;
        var events = src.Events;

        var byAccessType = events.GroupBy(x => string.IsNullOrEmpty(x.AccessType) ? "(none)" : x.AccessType)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count).ToList();
        var byCategory = events.Where(x => !string.IsNullOrEmpty(x.DataCategory))
            .GroupBy(x => x.DataCategory)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count).ToList();
        var topAccessors = events.GroupBy(x => x.UserId)
            .Select(g => new {
                UserId = g.Key,
                UserName = g.OrderByDescending(x => x.AccessedAt).First().UserName,
                Department = g.OrderByDescending(x => x.AccessedAt).First().UserDepartment,
                Total = g.Count(),
                Subjects = g.Select(x => x.SubjectId).Distinct().Count(),
                Exports = g.Count(x => x.AccessType == "Export" || x.AccessType == "Download"),
            })
            .OrderByDescending(x => x.Total).Take(10).ToList();
        var bySource = events.GroupBy(x => x.SourceSystemId)
            .Select(g => new { SourceSystemId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();
        var totalEvents = events.Count;
        var uniqueUsers = events.Select(x => x.UserId).Distinct().Count();
        var uniqueSubjects = events.Where(x => x.SubjectId != "SYSTEM" && x.SubjectId != "BULK")
            .Select(x => x.SubjectId).Distinct().Count();
        var exportEvents = events.Count(x => x.AccessType == "Export" || x.AccessType == "Download");

        static IContainer HeaderCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(4).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.SemiBold().FontSize(9));

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(9));

        var reportType = string.IsNullOrEmpty(report.ReportType) ? "Access Summary" : report.ReportType;
        var generatedAt = DateTime.UtcNow;

        var pdfBytes = Document.Create(document => {
            document.Page(page => {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Lato));

                page.Header().Column(col => {
                    col.Item().Text("GLBA Compliance Report").FontSize(20).SemiBold();
                    col.Item().Text($"{reportType} · {report.PeriodStart:MMMM d, yyyy} – {report.PeriodEnd:MMMM d, yyyy}")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(2).Text($"Generated {generatedAt:MMMM d, yyyy HH:mm} UTC" +
                        (string.IsNullOrWhiteSpace(generatedBy) ? "" : $" by {generatedBy}"))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col => {
                    col.Spacing(14);

                    // Summary statistics
                    col.Item().Row(row => {
                        void Stat(string value, string label)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c => {
                                c.Item().AlignCenter().Text(value).FontSize(16).SemiBold();
                                c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                        Stat(totalEvents.ToString("N0"), "Access events");
                        Stat(uniqueUsers.ToString("N0"), "Unique accessors");
                        Stat(uniqueSubjects.ToString("N0"), "Unique data subjects");
                        Stat(exportEvents.ToString("N0"), "Exports / downloads");
                    });

                    // Breakdown tables side by side
                    col.Item().Row(row => {
                        row.Spacing(16);
                        row.RelativeItem().Column(c => {
                            c.Item().Text("By Access Type").FontSize(11).SemiBold();
                            c.Item().PaddingTop(4).Table(t => {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(h => {
                                    h.Cell().Element(HeaderCell).Text("Type");
                                    h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                });
                                foreach (var itm in byAccessType) {
                                    t.Cell().Element(BodyCell).Text(itm.Label);
                                    t.Cell().Element(BodyCell).AlignRight().Text(itm.Count.ToString("N0"));
                                }
                            });
                        });
                        row.RelativeItem().Column(c => {
                            c.Item().Text("By Data Category").FontSize(11).SemiBold();
                            c.Item().PaddingTop(4).Table(t => {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(h => {
                                    h.Cell().Element(HeaderCell).Text("Category");
                                    h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                });
                                foreach (var itm in byCategory) {
                                    t.Cell().Element(BodyCell).Text(itm.Label);
                                    t.Cell().Element(BodyCell).AlignRight().Text(itm.Count.ToString("N0"));
                                }
                            });
                        });
                    });

                    // Top accessors
                    col.Item().Column(c => {
                        c.Item().Text("Top Accessors").FontSize(11).SemiBold();
                        c.Item().PaddingTop(4).Table(t => {
                            t.ColumnsDefinition(cd => {
                                cd.RelativeColumn(2); cd.RelativeColumn(3); cd.RelativeColumn(3);
                                cd.RelativeColumn(1); cd.RelativeColumn(1); cd.RelativeColumn(1);
                            });
                            t.Header(h => {
                                h.Cell().Element(HeaderCell).Text("User ID");
                                h.Cell().Element(HeaderCell).Text("Name");
                                h.Cell().Element(HeaderCell).Text("Department");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Subjects");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Exports");
                            });
                            foreach (var accessor in topAccessors) {
                                t.Cell().Element(BodyCell).Text(accessor.UserId);
                                t.Cell().Element(BodyCell).Text(accessor.UserName);
                                t.Cell().Element(BodyCell).Text(accessor.Department);
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Total.ToString("N0"));
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Subjects.ToString("N0"));
                                t.Cell().Element(BodyCell).AlignRight().Text(accessor.Exports.ToString("N0"));
                            }
                        });
                    });

                    // Source systems and data owners
                    col.Item().Column(c => {
                        c.Item().Text("Source Systems and Data Owners").FontSize(11).SemiBold();
                        c.Item().PaddingTop(4).Table(t => {
                            t.ColumnsDefinition(cd => {
                                cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(3); cd.RelativeColumn(1);
                            });
                            t.Header(h => {
                                h.Cell().Element(HeaderCell).Text("System");
                                h.Cell().Element(HeaderCell).Text("Current Data Owner");
                                h.Cell().Element(HeaderCell).Text("Owner Department");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Events");
                            });
                            foreach (var srcRow in bySource) {
                                src.Sources.TryGetValue(srcRow.SourceSystemId, out var system);
                                t.Cell().Element(BodyCell).Text(system?.Name ?? "(deleted system)");
                                t.Cell().Element(BodyCell).Text(string.IsNullOrEmpty(system?.DataOwnerName) ? "Not recorded" : system!.DataOwnerName);
                                t.Cell().Element(BodyCell).Text(system?.DataOwnerDepartment ?? "");
                                t.Cell().Element(BodyCell).AlignRight().Text(srcRow.Count.ToString("N0"));
                            }
                        });
                    });

                    col.Item().PaddingTop(4).Text(
                        "Access events record who accessed protected financial information, when, how, and for what stated purpose, " +
                        "with a snapshot of the responsible data owner at the time of access. Generated by FreeGLBA in support of the " +
                        "FTC Safeguards Rule requirement to monitor and log the activity of authorized users (16 CFR 314.4(c)(8)).")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(t => {
                    t.Span("FreeGLBA Compliance Report · Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();

        await UpdateReportRecordAsync(src, generatedBy);

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-report-{report.PeriodStart:yyyyMMdd}-{report.PeriodEnd:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            Bytes = pdfBytes,
        };
    }

    public async Task<DataObjects.ComplianceReportExport?> GenerateSubjectAccessHistoryPdfAsync(string subjectExternalId, string? generatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(subjectExternalId)) return null;
        subjectExternalId = subjectExternalId.Trim();

        var subject = await data.DataSubjects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalId == subjectExternalId);
        if (subject == null) return null;

        // Every event that touched this subject: direct matches plus membership
        // in multi-subject (JSON list) events. Capped to keep the PDF bounded.
        const int maxEvents = 2000;
        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectExternalId || (x.SubjectCount > 1 && x.SubjectIds.Contains(subjectExternalId)))
            .OrderByDescending(x => x.AccessedAt)
            .Take(maxEvents)
            .ToListAsync();

        var sourceIds = events.Select(x => x.SourceSystemId).Distinct().ToList();
        var sources = await data.SourceSystems
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.SourceSystemId))
            .ToDictionaryAsync(x => x.SourceSystemId);

        var generatedAt = DateTime.UtcNow;
        var uniqueUsers = events.Select(x => x.UserId).Distinct().Count();
        var exportCount = events.Count(x => x.AccessType == "Export" || x.AccessType == "Download");

        static IContainer HeaderCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Darken1)
            .PaddingVertical(4).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.SemiBold().FontSize(8));

        static IContainer BodyCell(IContainer c) => c
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(8));

        var pdfBytes = Document.Create(document => {
            document.Page(page => {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Lato));

                page.Header().Column(col => {
                    col.Item().Text("Data Subject Access History").FontSize(18).SemiBold();
                    col.Item().Text($"Subject {subject.ExternalId} ({(string.IsNullOrEmpty(subject.SubjectType) ? "unknown type" : subject.SubjectType)})")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(2).Text($"Generated {generatedAt:MMMM d, yyyy HH:mm} UTC" +
                        (string.IsNullOrWhiteSpace(generatedBy) ? "" : $" by {generatedBy}"))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col => {
                    col.Spacing(12);

                    col.Item().Row(row => {
                        void Stat(string value, string label)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c => {
                                c.Item().AlignCenter().Text(value).FontSize(14).SemiBold();
                                c.Item().AlignCenter().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                        Stat(events.Count.ToString("N0"), "Access events");
                        Stat(uniqueUsers.ToString("N0"), "Unique accessors");
                        Stat(exportCount.ToString("N0"), "Exports / downloads");
                        Stat(events.Count > 0 ? events.Min(x => x.AccessedAt).ToString("MMM d, yyyy") : "-", "Earliest access shown");
                    });

                    if (events.Count >= maxEvents) {
                        col.Item().Text($"Showing the most recent {maxEvents:N0} events; older history exists in the system.")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().Table(t => {
                        t.ColumnsDefinition(cd => {
                            cd.ConstantColumn(78); // when
                            cd.RelativeColumn(2);  // who
                            cd.RelativeColumn(2);  // dept
                            cd.ConstantColumn(48); // type
                            cd.RelativeColumn(2);  // category
                            cd.RelativeColumn(3);  // purpose
                            cd.RelativeColumn(2);  // source
                            cd.RelativeColumn(2);  // owner at time
                        });
                        t.Header(h => {
                            h.Cell().Element(HeaderCell).Text("When (UTC)");
                            h.Cell().Element(HeaderCell).Text("Accessed By");
                            h.Cell().Element(HeaderCell).Text("Department");
                            h.Cell().Element(HeaderCell).Text("Type");
                            h.Cell().Element(HeaderCell).Text("Category");
                            h.Cell().Element(HeaderCell).Text("Purpose");
                            h.Cell().Element(HeaderCell).Text("Source System");
                            h.Cell().Element(HeaderCell).Text("Data Owner (then)");
                        });
                        foreach (var evt in events) {
                            sources.TryGetValue(evt.SourceSystemId, out var source);
                            t.Cell().Element(BodyCell).Text(evt.AccessedAt.ToString("yyyy-MM-dd HH:mm"));
                            t.Cell().Element(BodyCell).Text(string.IsNullOrEmpty(evt.UserName) ? evt.UserId : $"{evt.UserName} ({evt.UserId})");
                            t.Cell().Element(BodyCell).Text(evt.UserDepartment);
                            t.Cell().Element(BodyCell).Text(evt.AccessType);
                            t.Cell().Element(BodyCell).Text(evt.DataCategory);
                            t.Cell().Element(BodyCell).Text(evt.Purpose);
                            t.Cell().Element(BodyCell).Text(source?.Name ?? "");
                            t.Cell().Element(BodyCell).Text(evt.DataOwnerName);
                        }
                    });

                    col.Item().PaddingTop(4).Text(
                        "Complete recorded history of access to this individual's protected financial information, " +
                        "including the responsible data owner at the time of each access. Generated by FreeGLBA in " +
                        "support of the FTC Safeguards Rule (16 CFR 314.4(c)(8)).")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1)).Text(t => {
                    t.Span($"Subject {subject.ExternalId} · Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf();

        return new DataObjects.ComplianceReportExport {
            FileName = $"glba-subject-{SafeFileName(subject.ExternalId)}-{generatedAt:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            Bytes = pdfBytes,
        };
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    #endregion
}

// ---- DataAccess.Dashboard ----

// ============================================================================
// GLBA DASHBOARD DATA ACCESS
// Dashboard statistics, trend insights, the recent-events feed and events by subject.
// ============================================================================

/// <summary>Glba dashboard interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Get dashboard statistics.</summary>
    Task<DataObjects.GlbaStats> GetGlbaStatsAsync();

    /// <summary>Get dashboard trend data (events per day) and items needing attention.</summary>
    Task<DataObjects.GlbaInsights> GetGlbaInsightsAsync(int windowDays = 30);

    /// <summary>Get recent events for dashboard feed.</summary>
    Task<List<DataObjects.AccessEvent>> GetRecentAccessEventsAsync(int limit = 50);

    /// <summary>Get access events for a specific subject by external ID.</summary>
    Task<List<DataObjects.AccessEvent>> GetAccessEventsBySubjectAsync(string subjectId, int limit = 100);
}

public partial class DataAccess
{
    #region Glba Dashboard

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

    #endregion
}

// ---- DataAccess.Accessors ----

// ============================================================================
// GLBA ACCESSORS DATA ACCESS
// Accessor (user) statistics with filtering and pagination, and the top-accessors list.
// ============================================================================

/// <summary>Glba accessors interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Get accessor (user) statistics with filtering and pagination.</summary>
    Task<DataObjects.AccessorFilterResult> GetAccessorsAsync(DataObjects.AccessorFilter filter);

    /// <summary>Get top accessors for dashboard display.</summary>
    Task<List<DataObjects.AccessorSummary>> GetTopAccessorsAsync(int limit = 10);
}

public partial class DataAccess
{
    #region Glba Accessors

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

// ---- DataAccess.ExternalApi ----

// ============================================================================
// GLBA EXTERNAL API DATA ACCESS
// Event ingest from external source systems (single and batch), SignalR notification and DataSubject statistics.
// ============================================================================

/// <summary>Glba External API interface extensions.</summary>
public partial interface IDataAccess
{
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

    #endregion
}

// ---- DataAccess.ApiKey ----

// ============================================================================
// API KEY AUTHENTICATION DATA ACCESS
// ============================================================================

/// <summary>API Key authentication interface extensions.</summary>
public partial interface IDataAccess
{
    /// <summary>Generates a new API key for a source system.</summary>
    Task<string> GenerateApiKeyAsync(Guid sourcesystemId);

    /// <summary>Gets all source systems for the dashboard.</summary>
    Task<List<DataObjects.SourceSystem>> GetSourceSystemsAsync();
    /// <summary>Validates an API key and returns the associated source system.</summary>
    Task<DataObjects.SourceSystem?> ValidateApiKeyAsync(string apiKey);
}

public partial class DataAccess
{
    #region API Key Authentication

    /// <summary>
    /// Validates an API key and returns the associated source system.
    /// Returns null if key is invalid or source is inactive.
    /// </summary>
    public async Task<DataObjects.SourceSystem?> ValidateApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return null;

        // Hash the provided key
        var hash = HashApiKey(apiKey);

        // Look up by hash
        var record = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.ApiKey == hash && x.IsActive);

        if (record == null)
            return null;

        // Return as DataObject with inline mapping
        // Note: EventCount not needed for API key validation - caller just needs source system identity
        return new DataObjects.SourceSystem
        {
            SourceSystemId = record.SourceSystemId,
            Name = record.Name,
            DisplayName = record.DisplayName,
            ApiKey = record.ApiKey,
            ContactEmail = record.ContactEmail,
            DataOwnerName = record.DataOwnerName,
            DataOwnerEmail = record.DataOwnerEmail,
            DataOwnerDepartment = record.DataOwnerDepartment,
            DataOwnerPhone = record.DataOwnerPhone,
            DataOwnerAssignedAt = record.DataOwnerAssignedAt,
            IsActive = record.IsActive,
            LastEventReceivedAt = record.LastEventReceivedAt,
            // EventCount intentionally omitted - not needed for auth, compute separately if needed
        };
    }

    /// <summary>
    /// Generates a new API key for a source system.
    /// Returns the plaintext key (show to user ONCE, then store only the hash).
    /// </summary>
    public async Task<string> GenerateApiKeyAsync(Guid sourcesystemId)
    {
        var record = await data.SourceSystems
            .FirstOrDefaultAsync(x => x.SourceSystemId == sourcesystemId);

        if (record == null)
            throw new InvalidOperationException("SourceSystem not found");

        // Generate random 32-byte key and encode as Base64
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var plaintextKey = Convert.ToBase64String(bytes);

        // Store only the hash
        record.ApiKey = HashApiKey(plaintextKey);
        await data.SaveChangesAsync();

        // Return plaintext key (caller shows to user ONCE)
        return plaintextKey;
    }

    /// <summary>
    /// Gets all source systems for the dashboard with event counts.
    /// </summary>
    public async Task<List<DataObjects.SourceSystem>> GetSourceSystemsAsync()
    {
        var sources = await data.SourceSystems
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        // Get event counts in one query
        var sourceIds = sources.Select(x => x.SourceSystemId).ToList();
        var eventCounts = await data.AccessEvents
            .Where(x => sourceIds.Contains(x.SourceSystemId))
            .GroupBy(x => x.SourceSystemId)
            .Select(g => new { SourceSystemId = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.SourceSystemId, x => x.Count);

        return sources.Select(x => new DataObjects.SourceSystem
        {
            SourceSystemId = x.SourceSystemId,
            Name = x.Name,
            DisplayName = x.DisplayName,
            ApiKey = x.ApiKey,
            ContactEmail = x.ContactEmail,
            DataOwnerName = x.DataOwnerName,
            DataOwnerEmail = x.DataOwnerEmail,
            DataOwnerDepartment = x.DataOwnerDepartment,
            DataOwnerPhone = x.DataOwnerPhone,
            DataOwnerAssignedAt = x.DataOwnerAssignedAt,
            IsActive = x.IsActive,
            LastEventReceivedAt = x.LastEventReceivedAt,
            EventCount = eventCounts.GetValueOrDefault(x.SourceSystemId, 0),
        }).ToList();
    }

    /// <summary>
    /// Hashes an API key using SHA256.
    /// </summary>
    private static string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

// ---- DataAccess.ApiLogging ----

// ============================================================================
// API REQUEST LOGGING - Data Access Methods
// CRUD operations for API request logs and body logging configuration
// ============================================================================

public partial interface IDataAccess
{
    Task<Guid> CreateApiLogAsync(EFModels.EFModels.ApiRequestLogItem log);
    Task<DataObjects.ApiRequestLog?> GetApiLogAsync(Guid id);
    Task<DataObjects.ApiLogDashboardStats> GetApiLogDashboardStatsAsync(DateTime from, DateTime to);
    Task<DataObjects.ApiLogFilterResult> GetApiLogsAsync(DataObjects.ApiLogFilter filter);
    Task<EFModels.EFModels.BodyLoggingConfigItem?> GetBodyLoggingConfigAsync(Guid sourceSystemId);
    Task<List<DataObjects.BodyLoggingConfig>> GetBodyLoggingConfigsAsync();
    Task<DataObjects.BodyLoggingConfig> EnableBodyLoggingAsync(Guid sourceSystemId, Guid enabledByUserId, string enabledByUserName, int durationHours, string reason);
    Task<bool> DisableBodyLoggingAsync(Guid configId);
}

public partial class DataAccess
{
    #region API Request Logging

    /// <summary>
    /// Create a new API request log entry.
    /// </summary>
    public async Task<Guid> CreateApiLogAsync(EFModels.EFModels.ApiRequestLogItem log)
    {
        if (log.ApiRequestLogId == Guid.Empty)
        {
            log.ApiRequestLogId = Guid.NewGuid();
        }

        data.ApiRequestLogs.Add(log);
        await data.SaveChangesAsync();

        return log.ApiRequestLogId;
    }

    /// <summary>
    /// Get paginated/filtered list of API request logs.
    /// </summary>
    public async Task<DataObjects.ApiLogFilterResult> GetApiLogsAsync(DataObjects.ApiLogFilter filter)
    {
        var query = data.ApiRequestLogs
            .AsNoTracking()
            .AsQueryable();

        // Apply time range filter
        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.RequestedAt >= filter.FromDate.Value);
        }
        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.RequestedAt <= filter.ToDate.Value);
        }

        // Apply source system filter
        if (filter.SourceSystemId.HasValue)
        {
            query = query.Where(x => x.SourceSystemId == filter.SourceSystemId.Value);
        }

        // Apply status filters
        if (filter.ErrorsOnly == true)
        {
            query = query.Where(x => !x.IsSuccess);
        }
        else if (filter.SuccessOnly == true)
        {
            query = query.Where(x => x.IsSuccess);
        }

        if (filter.StatusCodes?.Count > 0)
        {
            query = query.Where(x => filter.StatusCodes.Contains(x.StatusCode));
        }

        // Apply duration filters
        if (filter.MinDurationMs.HasValue)
        {
            query = query.Where(x => x.DurationMs >= filter.MinDurationMs.Value);
        }
        if (filter.MaxDurationMs.HasValue)
        {
            query = query.Where(x => x.DurationMs <= filter.MaxDurationMs.Value);
        }

        // Apply correlation filter
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        }

        // Apply search term
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(x => 
                x.RequestPath.ToLower().Contains(term) ||
                x.ErrorMessage.ToLower().Contains(term) ||
                x.SourceSystemName.ToLower().Contains(term));
        }

        // Get total count before pagination
        var totalRecords = await query.CountAsync();

        // Apply sorting
        query = filter.SortColumn switch
        {
            "RequestedAt" => filter.SortDescending ? query.OrderByDescending(x => x.RequestedAt) : query.OrderBy(x => x.RequestedAt),
            "DurationMs" => filter.SortDescending ? query.OrderByDescending(x => x.DurationMs) : query.OrderBy(x => x.DurationMs),
            "StatusCode" => filter.SortDescending ? query.OrderByDescending(x => x.StatusCode) : query.OrderBy(x => x.StatusCode),
            "RequestPath" => filter.SortDescending ? query.OrderByDescending(x => x.RequestPath) : query.OrderBy(x => x.RequestPath),
            "SourceSystemName" => filter.SortDescending ? query.OrderByDescending(x => x.SourceSystemName) : query.OrderBy(x => x.SourceSystemName),
            _ => query.OrderByDescending(x => x.RequestedAt)
        };

        // Apply pagination
        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(x => new DataObjects.ApiRequestLogListItem
            {
                ApiRequestLogId = x.ApiRequestLogId,
                SourceSystemName = x.SourceSystemName,
                HttpMethod = x.HttpMethod,
                RequestPath = x.RequestPath,
                RequestedAt = x.RequestedAt,
                DurationMs = x.DurationMs,
                StatusCode = x.StatusCode,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync();

        return new DataObjects.ApiLogFilterResult
        {
            Records = items,
            TotalRecords = totalRecords,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// Get a single API request log by ID.
    /// </summary>
    public async Task<DataObjects.ApiRequestLog?> GetApiLogAsync(Guid id)
    {
        var item = await data.ApiRequestLogs
            .AsNoTracking()
            .Where(x => x.ApiRequestLogId == id)
            .FirstOrDefaultAsync();

        if (item == null) return null;

        return new DataObjects.ApiRequestLog
        {
            ApiRequestLogId = item.ApiRequestLogId,
            SourceSystemId = item.SourceSystemId,
            SourceSystemName = item.SourceSystemName,
            UserId = item.UserId,
            UserName = item.UserName,
            TenantId = item.TenantId,
            HttpMethod = item.HttpMethod,
            RequestPath = item.RequestPath,
            QueryString = item.QueryString,
            RequestHeaders = item.RequestHeaders,
            RequestBody = item.RequestBody,
            RequestBodySize = item.RequestBodySize,
            RequestedAt = item.RequestedAt,
            RespondedAt = item.RespondedAt,
            DurationMs = item.DurationMs,
            IpAddress = item.IpAddress,
            UserAgent = item.UserAgent,
            ForwardedFor = item.ForwardedFor,
            StatusCode = item.StatusCode,
            IsSuccess = item.IsSuccess,
            ResponseBody = item.ResponseBody,
            ResponseBodySize = item.ResponseBodySize,
            ErrorMessage = item.ErrorMessage,
            ExceptionType = item.ExceptionType,
            CorrelationId = item.CorrelationId,
            AuthType = item.AuthType,
            RelatedEntityId = item.RelatedEntityId,
            RelatedEntityType = item.RelatedEntityType,
            BodyLoggingEnabled = item.BodyLoggingEnabled
        };
    }

    /// <summary>
    /// Get dashboard statistics for API logs.
    /// </summary>
    public async Task<DataObjects.ApiLogDashboardStats> GetApiLogDashboardStatsAsync(DateTime from, DateTime to)
    {
        var query = data.ApiRequestLogs
            .AsNoTracking()
            .Where(x => x.RequestedAt >= from && x.RequestedAt <= to);

        // Get basic stats
        var totalRequests = await query.CountAsync();
        var totalErrors = await query.CountAsync(x => !x.IsSuccess);
        var avgDurationMs = totalRequests > 0 
            ? await query.AverageAsync(x => (double)x.DurationMs) 
            : 0;

        // Get total log count (all time)
        var totalLogCount = await data.ApiRequestLogs.LongCountAsync();

        // Get logs older than 7 years
        var sevenYearsAgo = DateTime.UtcNow.AddYears(-7);
        var logsOlderThan7Years = await data.ApiRequestLogs
            .LongCountAsync(x => x.RequestedAt < sevenYearsAgo);

        // Get breakdown by source system
        var bySourceSystem = await query
            .GroupBy(x => x.SourceSystemName)
            .Select(g => new DataObjects.SourceSystemStats
            {
                SourceSystemName = g.Key ?? "(Unknown)",
                Count = g.Count(),
                Percentage = 0 // Calculated below
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        // Calculate percentages
        foreach (var item in bySourceSystem)
        {
            item.Percentage = totalRequests > 0 
                ? Math.Round((double)item.Count / totalRequests * 100, 1) 
                : 0;
        }

        // Get breakdown by status code
        var byStatusCode = await query
            .GroupBy(x => x.StatusCode)
            .Select(g => new DataObjects.StatusCodeStats
            {
                StatusCode = g.Key,
                Category = GetStatusCategory(g.Key),
                Count = g.Count(),
                Percentage = 0 // Calculated below
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        // Calculate percentages and set categories
        foreach (var item in byStatusCode)
        {
            item.Category = GetStatusCategory(item.StatusCode);
            item.Percentage = totalRequests > 0 
                ? Math.Round((double)item.Count / totalRequests * 100, 1) 
                : 0;
        }

        // Get recent errors
        var recentErrors = await query
            .Where(x => !x.IsSuccess)
            .OrderByDescending(x => x.RequestedAt)
            .Take(10)
            .Select(x => new DataObjects.ApiRequestLogListItem
            {
                ApiRequestLogId = x.ApiRequestLogId,
                SourceSystemName = x.SourceSystemName,
                HttpMethod = x.HttpMethod,
                RequestPath = x.RequestPath,
                RequestedAt = x.RequestedAt,
                DurationMs = x.DurationMs,
                StatusCode = x.StatusCode,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync();

        // Get requests over time (hourly buckets for last 24h, daily for longer ranges)
        var requestsOverTime = new List<DataObjects.TimeSeriesPoint>();
        var timeSpan = to - from;
        
        if (timeSpan.TotalHours <= 24)
        {
            // Hourly buckets
            for (var hour = from; hour < to; hour = hour.AddHours(1))
            {
                var hourEnd = hour.AddHours(1);
                var count = await query.CountAsync(x => x.RequestedAt >= hour && x.RequestedAt < hourEnd);
                requestsOverTime.Add(new DataObjects.TimeSeriesPoint
                {
                    Timestamp = hour,
                    Count = count
                });
            }
        }
        else
        {
            // Daily buckets
            for (var day = from.Date; day < to.Date; day = day.AddDays(1))
            {
                var dayEnd = day.AddDays(1);
                var count = await query.CountAsync(x => x.RequestedAt >= day && x.RequestedAt < dayEnd);
                requestsOverTime.Add(new DataObjects.TimeSeriesPoint
                {
                    Timestamp = day,
                    Count = count
                });
            }
        }

        return new DataObjects.ApiLogDashboardStats
        {
            TotalRequests = totalRequests,
            TotalErrors = totalErrors,
            ErrorRate = totalRequests > 0 ? Math.Round((double)totalErrors / totalRequests * 100, 2) : 0,
            AvgDurationMs = Math.Round(avgDurationMs, 1),
            TotalLogCount = totalLogCount,
            LogsOlderThan7Years = logsOlderThan7Years,
            BySourceSystem = bySourceSystem,
            ByStatusCode = byStatusCode,
            RequestsOverTime = requestsOverTime,
            RecentErrors = recentErrors
        };
    }

    private static string GetStatusCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx Success",
            >= 300 and < 400 => "3xx Redirect",
            >= 400 and < 500 => "4xx Client Error",
            >= 500 => "5xx Server Error",
            _ => "Other"
        };
    }

    #endregion

    #region Body Logging Configuration

    /// <summary>
    /// Get active body logging configuration for a source system.
    /// </summary>
    public async Task<EFModels.EFModels.BodyLoggingConfigItem?> GetBodyLoggingConfigAsync(Guid sourceSystemId)
    {
        // First check for expired configs and auto-disable them
        var expiredConfigs = await data.BodyLoggingConfigs
            .Where(c => c.SourceSystemId == sourceSystemId && c.IsActive && c.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var config in expiredConfigs)
        {
            config.IsActive = false;
            config.DisabledAt = DateTime.UtcNow;
        }
        
        if (expiredConfigs.Any())
        {
            await data.SaveChangesAsync();
        }

        return await data.BodyLoggingConfigs
            .Where(c => c.SourceSystemId == sourceSystemId && c.IsActive)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get all body logging configurations (for settings page).
    /// </summary>
    public async Task<List<DataObjects.BodyLoggingConfig>> GetBodyLoggingConfigsAsync()
    {
        // Auto-disable any expired configs first
        var expiredConfigs = await data.BodyLoggingConfigs
            .Where(c => c.IsActive && c.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var config in expiredConfigs)
        {
            config.IsActive = false;
            config.DisabledAt = DateTime.UtcNow;
        }
        
        if (expiredConfigs.Any())
        {
            await data.SaveChangesAsync();
        }

        var items = await data.BodyLoggingConfigs
            .AsNoTracking()
            .OrderByDescending(c => c.EnabledAt)
            .Take(100)
            .ToListAsync();

        return items.Select(c => new DataObjects.BodyLoggingConfig
        {
            BodyLoggingConfigId = c.BodyLoggingConfigId,
            SourceSystemId = c.SourceSystemId,
            EnabledByUserId = c.EnabledByUserId,
            EnabledByUserName = c.EnabledByUserName,
            EnabledAt = c.EnabledAt,
            ExpiresAt = c.ExpiresAt,
            DisabledAt = c.DisabledAt,
            IsActive = c.IsActive,
            Reason = c.Reason
        }).ToList();
    }

    /// <summary>
    /// Enable body logging for a source system.
    /// </summary>
    public async Task<DataObjects.BodyLoggingConfig> EnableBodyLoggingAsync(
        Guid sourceSystemId, 
        Guid enabledByUserId, 
        string enabledByUserName, 
        int durationHours, 
        string reason){
        // Disable any existing active configs for this source system
        var existingConfigs = await data.BodyLoggingConfigs
            .Where(c => c.SourceSystemId == sourceSystemId && c.IsActive)
            .ToListAsync();
        
        foreach (var config in existingConfigs)
        {
            config.IsActive = false;
            config.DisabledAt = DateTime.UtcNow;
        }

        // Create new config
        var now = DateTime.UtcNow;
        var newConfig = new EFModels.EFModels.BodyLoggingConfigItem
        {
            BodyLoggingConfigId = Guid.NewGuid(),
            SourceSystemId = sourceSystemId,
            EnabledByUserId = enabledByUserId,
            EnabledByUserName = enabledByUserName,
            EnabledAt = now,
            ExpiresAt = now.AddHours(durationHours),
            IsActive = true,
            Reason = reason
        };

        data.BodyLoggingConfigs.Add(newConfig);
        await data.SaveChangesAsync();

        return new DataObjects.BodyLoggingConfig
        {
            BodyLoggingConfigId = newConfig.BodyLoggingConfigId,
            SourceSystemId = newConfig.SourceSystemId,
            EnabledByUserId = newConfig.EnabledByUserId,
            EnabledByUserName = newConfig.EnabledByUserName,
            EnabledAt = newConfig.EnabledAt,
            ExpiresAt = newConfig.ExpiresAt,
            DisabledAt = newConfig.DisabledAt,
            IsActive = newConfig.IsActive,
            Reason = newConfig.Reason
        };
    }

    /// <summary>
    /// Disable body logging for a source system.
    /// </summary>
    public async Task<bool> DisableBodyLoggingAsync(Guid configId)
    {
        var config = await data.BodyLoggingConfigs
            .Where(c => c.BodyLoggingConfigId == configId)
            .FirstOrDefaultAsync();
        
        if (config == null) return false;

        config.IsActive = false;
        config.DisabledAt = DateTime.UtcNow;
        await data.SaveChangesAsync();

        return true;
    }

    #endregion

    // NOTE: Database cleanup (log retention) is handled externally
    // via scheduled SQL jobs managed by your DBA team.
    // See doc 123 for recommended SQL scripts.
}

// ---- DataAccess.Alerts ----

// ============================================================================
// GLBA SETTINGS AND WEBHOOK ALERTS
// App-wide GLBA configuration (stored in the framework Settings table) and
// immediate webhook alert delivery for events that trip a rule at ingest:
// large bulk exports and after-hours access. Delivery is best-effort and can
// never break the ingest path.
// ============================================================================

public partial interface IDataAccess
{
    /// <summary>Gets the app-wide GLBA settings (alerts, thresholds, institution timezone).</summary>
    Task<DataObjects.GlbaSettings> GetGlbaSettingsAsync();

    /// <summary>Saves the app-wide GLBA settings.</summary>
    Task<DataObjects.GlbaSettings> SaveGlbaSettingsAsync(DataObjects.GlbaSettings settings, DataObjects.User? currentUser = null);

    /// <summary>Sends a test alert to the configured webhook. Returns false when disabled, unconfigured, or delivery fails.</summary>
    Task<bool> SendTestGlbaAlertAsync();
}

public partial class DataAccess
{
    #region GLBA Settings and Alerts

    private const string GlbaSettingsName = "GlbaSettings";

    private static readonly HttpClient _alertHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public Task<DataObjects.GlbaSettings> GetGlbaSettingsAsync()
    {
        var settings = GetSetting<DataObjects.GlbaSettings>(GlbaSettingsName, DataObjects.SettingType.Object)
            ?? new DataObjects.GlbaSettings();
        return Task.FromResult(settings);
    }

    public async Task<DataObjects.GlbaSettings> SaveGlbaSettingsAsync(DataObjects.GlbaSettings settings, DataObjects.User? currentUser = null)
    {
        // Keep stored values sane.
        settings.WebhookUrl = (settings.WebhookUrl ?? string.Empty).Trim();
        settings.InstitutionTimeZone = (settings.InstitutionTimeZone ?? string.Empty).Trim();
        settings.BulkExportAlertThreshold = Math.Max(2, settings.BulkExportAlertThreshold);
        settings.BusinessHoursStart = Math.Clamp(settings.BusinessHoursStart, 0, 23);
        settings.BusinessHoursEnd = Math.Clamp(settings.BusinessHoursEnd, 1, 24);
        if (settings.BusinessHoursEnd <= settings.BusinessHoursStart) {
            settings.BusinessHoursEnd = Math.Min(24, settings.BusinessHoursStart + 12);
        }

        SaveSetting(GlbaSettingsName, DataObjects.SettingType.Object, settings,
            Description: "App-wide GLBA settings (alerts, thresholds, institution timezone)",
            CurrentUser: currentUser);

        return await GetGlbaSettingsAsync();
    }

    /// <summary>
    /// Resolves the institution timezone; falls back to UTC when unset or invalid.
    /// </summary>
    private static TimeZoneInfo GetInstitutionTimeZone(DataObjects.GlbaSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.InstitutionTimeZone)) {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById(settings.InstitutionTimeZone);
            } catch { }
        }
        return TimeZoneInfo.Utc;
    }

    /// <summary>
    /// True when a UTC timestamp falls outside the configured business hours
    /// (or on a weekend, when weekends count) in the institution timezone.
    /// </summary>
    private static bool IsAfterHours(DateTime accessedAtUtc, DataObjects.GlbaSettings settings)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            accessedAtUtc.Kind == DateTimeKind.Utc ? accessedAtUtc : DateTime.SpecifyKind(accessedAtUtc, DateTimeKind.Utc),
            GetInstitutionTimeZone(settings));

        if (settings.WeekendsAreAfterHours &&
            (local.DayOfWeek == DayOfWeek.Saturday || local.DayOfWeek == DayOfWeek.Sunday)) {
            return true;
        }

        return local.Hour < settings.BusinessHoursStart || local.Hour >= settings.BusinessHoursEnd;
    }

    /// <summary>
    /// POSTs an alert to a webhook as {"text": ..., "severity": ...} (the "text"
    /// field renders in Slack and Teams incoming webhooks). Static and context-free
    /// so it can safely outlive the request that queued it.
    /// </summary>
    private static async Task<bool> PostGlbaWebhookAsync(string webhookUrl, string severity, string text)
    {
        try {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { text, severity, source = "FreeGLBA" });
            var response = await _alertHttpClient.PostAsync(webhookUrl,
                new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        } catch {
            return false;
        }
    }

    public async Task<bool> SendTestGlbaAlertAsync()
    {
        var settings = await GetGlbaSettingsAsync();
        if (!settings.AlertsEnabled || string.IsNullOrWhiteSpace(settings.WebhookUrl)) return false;
        return await PostGlbaWebhookAsync(settings.WebhookUrl, "info",
            $"FreeGLBA test alert — webhook delivery is working. Sent {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.");
    }

    /// <summary>
    /// Builds the alert messages one event trips (large bulk access, after-hours
    /// access), or none.
    /// </summary>
    private static List<(string Severity, string Text)> BuildEventAlerts(
        DataObjects.GlbaSettings settings, EFModels.EFModels.AccessEventItem evt, string sourceSystemName)
    {
        var messages = new List<(string, string)>();
        var who = evt.UserId + (string.IsNullOrEmpty(evt.UserName) ? "" : $" ({evt.UserName})");

        if (evt.SubjectCount >= settings.BulkExportAlertThreshold) {
            messages.Add(("critical",
                $"Large bulk access: {who} performed a {evt.AccessType} touching {evt.SubjectCount:N0} data subjects " +
                $"via {sourceSystemName} at {evt.AccessedAt:yyyy-MM-dd HH:mm} UTC. " +
                $"Purpose: {(string.IsNullOrEmpty(evt.Purpose) ? "(none recorded)" : evt.Purpose)}"));
        }

        if (settings.AlertOnAfterHours && IsAfterHours(evt.AccessedAt, settings)) {
            messages.Add(("warning",
                $"After-hours access: {who} performed a {evt.AccessType} on subject {evt.SubjectId} " +
                $"via {sourceSystemName} at {evt.AccessedAt:yyyy-MM-dd HH:mm} UTC " +
                $"(outside {settings.BusinessHoursStart:00}:00–{settings.BusinessHoursEnd:00}:00 institution time)."));
        }

        return messages;
    }

    /// <summary>
    /// Evaluates just-ingested events against the alert rules and fires the
    /// webhook for any that trip, detached from the request so delivery can
    /// never break or slow the audit write path. Settings are read here (while
    /// the context is alive); the HTTP sends run on a background task.
    /// </summary>
    private async Task QueueEventAlertsAsync(IEnumerable<(EFModels.EFModels.AccessEventItem Event, string SourceSystemName)> events)
    {
        try {
            var settings = await GetGlbaSettingsAsync();
            if (!settings.AlertsEnabled || string.IsNullOrWhiteSpace(settings.WebhookUrl)) return;

            var messages = events
                .SelectMany(x => BuildEventAlerts(settings, x.Event, x.SourceSystemName))
                .ToList();
            if (messages.Count == 0) return;

            var webhookUrl = settings.WebhookUrl;
            _ = Task.Run(async () => {
                foreach (var (severity, text) in messages) {
                    await PostGlbaWebhookAsync(webhookUrl, severity, text);
                }
            });
        } catch { }
    }

    #endregion
}

// ---- DataAccess.Integrity ----

// ============================================================================
// TAMPER-EVIDENT EVENT HASH CHAIN
// Every ingested event is stamped with a per-source-system chain position
// (ChainSequence), the previous event's hash (PrevRowHash), and a SHA-256 over
// its own immutable audit fields (RowHash). Verification then detects three
// kinds of tampering: modified rows (content mismatch), broken links, and
// deleted rows (sequence gaps). Legitimate UI edits of an audit record break
// its hash BY DESIGN - an audit trail is supposed to be immutable, and
// verification surfaces exactly which records changed after the fact.
//
// Sequence assignment is serialized with an in-process lock; when multiple app
// instances ingest concurrently, chains can fork (documented limitation).
// ============================================================================

public partial interface IDataAccess
{
    /// <summary>
    /// Verifies a source system's tamper-evident hash chain: recomputes every
    /// chained event's hash, checks the links between events, and checks the
    /// sequence for gaps left by deletions.
    /// </summary>
    Task<DataObjects.ChainVerificationResult> VerifyAccessEventChainAsync(Guid sourceSystemId);
}

public partial class DataAccess
{
    #region Event Hash Chain

    /// <summary>Serializes chain-position assignment within this process.</summary>
    private static readonly SemaphoreSlim _chainLock = new(1, 1);

    /// <summary>
    /// Canonical representation of the fields protected by the hash. Anything
    /// listed here becomes tamper-evident; timestamps use ticks so formatting
    /// can never shift the hash.
    /// </summary>
    private static string CanonicalEventString(EFModels.EFModels.AccessEventItem evt)
    {
        return string.Join('|',
            evt.AccessEventId.ToString("N"),
            evt.SourceSystemId.ToString("N"),
            evt.SourceEventId,
            evt.AccessedAt.Ticks,
            evt.ReceivedAt.Ticks,
            evt.UserId,
            evt.UserName,
            evt.SubjectId,
            evt.SubjectIds,
            evt.SubjectCount,
            evt.SubjectType,
            evt.AccessType,
            evt.DataCategory,
            evt.Purpose,
            evt.DataOwnerName,
            evt.PrevRowHash,
            evt.ChainSequence);
    }

    private static string ComputeEventRowHash(EFModels.EFModels.AccessEventItem evt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalEventString(evt)));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Stamps ChainSequence, PrevRowHash, and RowHash onto a new event. The
    /// caller must hold <see cref="_chainLock"/> and save before releasing it.
    /// Pass the previous event of the same batch via <paramref name="pendingTail"/>
    /// when several events are chained before a single save.
    /// </summary>
    private async Task AssignChainPositionAsync(EFModels.EFModels.AccessEventItem evt,
        EFModels.EFModels.AccessEventItem? pendingTail = null)
    {
        if (pendingTail != null && pendingTail.SourceSystemId == evt.SourceSystemId) {
            evt.ChainSequence = pendingTail.ChainSequence + 1;
            evt.PrevRowHash = pendingTail.RowHash;
        } else {
            var tail = await data.AccessEvents
                .AsNoTracking()
                .Where(x => x.SourceSystemId == evt.SourceSystemId && x.ChainSequence > 0)
                .OrderByDescending(x => x.ChainSequence)
                .FirstOrDefaultAsync();
            evt.ChainSequence = (tail?.ChainSequence ?? 0) + 1;
            evt.PrevRowHash = tail?.RowHash ?? string.Empty;
        }

        evt.RowHash = ComputeEventRowHash(evt);
    }

    public async Task<DataObjects.ChainVerificationResult> VerifyAccessEventChainAsync(Guid sourceSystemId)
    {
        var output = new DataObjects.ChainVerificationResult {
            SourceSystemId = sourceSystemId,
            VerifiedAt = DateTime.UtcNow,
        };

        var source = await data.SourceSystems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceSystemId == sourceSystemId);
        output.SourceSystemName = source?.Name ?? string.Empty;

        output.UnhashedEvents = await data.AccessEvents
            .CountAsync(x => x.SourceSystemId == sourceSystemId && x.ChainSequence == 0);

        var events = await data.AccessEvents
            .AsNoTracking()
            .Where(x => x.SourceSystemId == sourceSystemId && x.ChainSequence > 0)
            .OrderBy(x => x.ChainSequence)
            .ToListAsync();
        output.EventsChecked = events.Count;

        EFModels.EFModels.AccessEventItem? previous = null;
        foreach (var evt in events) {
            // A modified row no longer matches the hash computed at ingest.
            if (ComputeEventRowHash(evt) != evt.RowHash) {
                output.Issues.Add(new DataObjects.ChainVerificationIssue {
                    AccessEventId = evt.AccessEventId,
                    ChainSequence = evt.ChainSequence,
                    IssueType = "ContentMismatch",
                    Detail = $"Event #{evt.ChainSequence} ({evt.AccessedAt:yyyy-MM-dd HH:mm} UTC, user {evt.UserId}) no longer matches the hash recorded at ingest - its content was modified after the fact.",
                });
            }

            if (previous != null) {
                // A deleted row leaves a hole in the sequence.
                if (evt.ChainSequence != previous.ChainSequence + 1) {
                    output.Issues.Add(new DataObjects.ChainVerificationIssue {
                        AccessEventId = evt.AccessEventId,
                        ChainSequence = evt.ChainSequence,
                        IssueType = "SequenceGap",
                        Detail = $"Sequence jumps from #{previous.ChainSequence} to #{evt.ChainSequence} - {evt.ChainSequence - previous.ChainSequence - 1} event(s) were deleted from the chain.",
                    });
                } else if (evt.PrevRowHash != previous.RowHash) {
                    output.Issues.Add(new DataObjects.ChainVerificationIssue {
                        AccessEventId = evt.AccessEventId,
                        ChainSequence = evt.ChainSequence,
                        IssueType = "BrokenLink",
                        Detail = $"Event #{evt.ChainSequence} does not link to the recorded hash of event #{previous.ChainSequence}.",
                    });
                }
            } else if (evt.ChainSequence != 1) {
                output.Issues.Add(new DataObjects.ChainVerificationIssue {
                    AccessEventId = evt.AccessEventId,
                    ChainSequence = evt.ChainSequence,
                    IssueType = "SequenceGap",
                    Detail = $"Chain starts at #{evt.ChainSequence} - the first {evt.ChainSequence - 1} event(s) were deleted.",
                });
            }

            previous = evt;
        }

        return output;
    }

    #endregion
}
