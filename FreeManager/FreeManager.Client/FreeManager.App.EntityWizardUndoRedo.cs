// EntityWizardUndoRedo.App.cs - Undo/Redo state management for Entity Wizard
// Provides history tracking with snapshot-based state restoration

using System.Text.Json;

namespace FreeManager.Client;

/// <summary>
/// Manages undo/redo history for the Entity Wizard.
/// Uses JSON serialization for state snapshots.
/// </summary>
public class EntityWizardUndoRedo
{
    private readonly List<UndoRedoEntry> _history = new();
    private int _currentIndex = -1;
    private const int MaxHistorySize = 50;

    /// <summary>
    /// True if there are actions that can be undone.
    /// </summary>
    public bool CanUndo => _currentIndex > 0;

    /// <summary>
    /// True if there are actions that can be redone.
    /// </summary>
    public bool CanRedo => _currentIndex < _history.Count - 1;

    /// <summary>
    /// Current history position (for display).
    /// </summary>
    public int HistoryPosition => _currentIndex + 1;

    /// <summary>
    /// Total history count (for display).
    /// </summary>
    public int HistoryCount => _history.Count;

    /// <summary>
    /// Description of the last action (for tooltip).
    /// </summary>
    public string? LastActionDescription => _currentIndex >= 0 ? _history[_currentIndex].Description : null;

    /// <summary>
    /// Record a state change that can be undone.
    /// </summary>
    public void RecordChange(DataObjects.EntityWizardState state, string description)
    {
        // Remove any redo history
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        // Serialize current state
        var snapshot = SerializeState(state);

        // Don't record if identical to last entry
        if (_history.Count > 0 && _history[^1].StateSnapshot == snapshot)
        {
            return;
        }

        // Add new entry
        _history.Add(new UndoRedoEntry
        {
            Description = description,
            Timestamp = DateTime.UtcNow,
            StateSnapshot = snapshot
        });

        // Trim history if too large
        while (_history.Count > MaxHistorySize)
        {
            _history.RemoveAt(0);
        }

        _currentIndex = _history.Count - 1;
    }

    /// <summary>
    /// Undo the last change and return the restored state.
    /// </summary>
    public DataObjects.EntityWizardState? Undo()
    {
        if (!CanUndo) return null;

        _currentIndex--;
        return DeserializeState(_history[_currentIndex].StateSnapshot);
    }

    /// <summary>
    /// Redo the previously undone change and return the restored state.
    /// </summary>
    public DataObjects.EntityWizardState? Redo()
    {
        if (!CanRedo) return null;

        _currentIndex++;
        return DeserializeState(_history[_currentIndex].StateSnapshot);
    }

    /// <summary>
    /// Get list of recent actions (for history panel).
    /// </summary>
    public List<UndoRedoEntry> GetRecentHistory(int count = 10)
    {
        return _history
            .Skip(Math.Max(0, _history.Count - count))
            .Reverse()
            .ToList();
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
    }

    private static string SerializeState(DataObjects.EntityWizardState state)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };
        return JsonSerializer.Serialize(state, options);
    }

    private static DataObjects.EntityWizardState? DeserializeState(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DataObjects.EntityWizardState>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Single entry in the undo/redo history.
/// </summary>
public class UndoRedoEntry
{
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string StateSnapshot { get; set; } = string.Empty;
}
