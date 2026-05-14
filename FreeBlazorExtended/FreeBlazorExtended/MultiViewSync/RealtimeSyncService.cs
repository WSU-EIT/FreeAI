/*
    Purpose: In-memory synchronization service for presentation sessions and connected clients.
    Key behaviors: Tracks sessions, active items, connection groups, and live presentation state for synchronized views.
    Dependencies: Uses in-memory state and serialized payloads; real-time host transport is supplied by the surrounding app and hub wiring.
*/
using System.Collections.Concurrent;
using System.Text.Json;

namespace FreeBlazorExtended.MultiViewSync;

public partial class RealtimeSyncService
{
    private static readonly ConcurrentDictionary<Guid, PresentationSession> _sessions = new();
    private static readonly ConcurrentDictionary<string, List<string>> _clientConnections = new(); // SessionId -> list of connectionIds

    public Task<List<PresentationSession>> GetPresentationSessions(Guid TenantId)
    {
        var sessions = _sessions.Values
            .Where(s => s.TenantId == TenantId && !s.Deleted)
            .ToList();
        return Task.FromResult(sessions);
    }

    public Task<PresentationSession?> GetPresentationSession(Guid SessionId)
    {
        _sessions.TryGetValue(SessionId, out var session);
        return Task.FromResult(session?.Deleted == false ? session : null);
    }

    public Task<PresentationSession> CreateSession(Guid TenantId, string name, string? UserId = null)
    {
        var session = new PresentationSession
        {
            SessionId = Guid.NewGuid(),
            TenantId = TenantId,
            Name = name,
            CreatedBy = UserId,
            CreatedAt = DateTime.UtcNow
        };

        _sessions[session.SessionId] = session;
        _clientConnections[session.SessionId.ToString()] = new();

        return Task.FromResult(session);
    }

    public Task<PresentationSession> SaveSession(PresentationSession session, string? UserId = null)
    {
        if (session.SessionId == Guid.Empty)
            session.SessionId = Guid.NewGuid();

        session.LastModified = DateTime.UtcNow;
        session.LastModifiedBy = UserId;

        if (!_sessions.ContainsKey(session.SessionId))
            session.CreatedAt = DateTime.UtcNow;

        _sessions[session.SessionId] = session;
        return Task.FromResult(session);
    }

    public async Task SetActiveItem(Guid SessionId, Guid? ItemId, string? UserId = null)
    {
        if (!_sessions.TryGetValue(SessionId, out var session))
            return;

        session.ActiveItemId = ItemId;
        session.LastModified = DateTime.UtcNow;
        session.LastModifiedBy = UserId;

        await SaveSession(session, UserId);
    }

    public async Task SetBlankScreen(Guid SessionId, bool IsBlanked, string? UserId = null)
    {
        if (!_sessions.TryGetValue(SessionId, out var session))
            return;

        session.IsBlanked = IsBlanked;
        session.LastModified = DateTime.UtcNow;
        session.LastModifiedBy = UserId;

        await SaveSession(session, UserId);
    }

    public async Task SetTextHidden(Guid SessionId, bool IsTextHidden, string? UserId = null)
    {
        if (!_sessions.TryGetValue(SessionId, out var session))
            return;

        session.IsTextHidden = IsTextHidden;
        session.LastModified = DateTime.UtcNow;
        session.LastModifiedBy = UserId;

        await SaveSession(session, UserId);
    }

    public async Task<PresentationSession> AddSessionItem(Guid SessionId, string ItemType, string TitleOrPayload, string? UserId = null)
    {
        if (!_sessions.TryGetValue(SessionId, out var session))
            throw new InvalidOperationException("Session not found");

        if (session.Items == null)
            session.Items = new();

        var item = new SessionItem
        {
            ItemId = Guid.NewGuid(),
            Order = session.Items.Count,
            Type = ItemType,
            TitleOrPayload = TitleOrPayload
        };

        session.Items.Add(item);

        return await SaveSession(session, UserId);
    }

    public async Task<PresentationSession> RemoveSessionItem(Guid SessionId, Guid ItemId, string? UserId = null)
    {
        if (!_sessions.TryGetValue(SessionId, out var session))
            throw new InvalidOperationException("Session not found");

        if (session.Items != null) {
            var item = session.Items.FirstOrDefault(i => i.ItemId == ItemId);
            if (item != null)
                session.Items.Remove(item);
        }

        return await SaveSession(session, UserId);
    }

    public Task RegisterClient(string SessionId, string ConnectionId)
    {
        if (!_clientConnections.ContainsKey(SessionId))
            _clientConnections[SessionId] = new();

        var connections = _clientConnections[SessionId];
        if (!connections.Contains(ConnectionId))
            connections.Add(ConnectionId);

        return Task.CompletedTask;
    }

    public Task UnregisterClient(string SessionId, string ConnectionId)
    {
        if (_clientConnections.TryGetValue(SessionId, out var connections))
            connections.Remove(ConnectionId);

        return Task.CompletedTask;
    }

    public Task<int> GetActiveClientCount(string SessionId)
    {
        if (_clientConnections.TryGetValue(SessionId, out var connections))
            return Task.FromResult(connections.Count);

        return Task.FromResult(0);
    }

    public Task DeleteSession(Guid SessionId)
    {
        if (_sessions.TryGetValue(SessionId, out var session)) {
            session.Deleted = true;
            session.LastModified = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public void ClearAllSessions()
    {
        _sessions.Clear();
        _clientConnections.Clear();
    }
}

// SessionItem moved to FreeBlazorExtended.Models
