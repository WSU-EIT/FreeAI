/*
    Purpose: Shared in-memory notification queue for toast-style user feedback.
    Key behaviors: Creates notification records, raises add or remove events, and handles timed dismissal behavior.
    Used by: Host applications that want a lightweight, DI-friendly feedback service without external dependencies.

    DI registration (server or WASM):
        builder.Services.AddScoped<NotificationService>();

    Wire-up in a layout or top-level component:
        @inject NotificationService Notifications
        protected override void OnInitialized()
        {
            Notifications.OnNotificationAdded   += n => { ... StateHasChanged(); };
            Notifications.OnNotificationRemoved += id => { ... StateHasChanged(); };
        }
*/
namespace FreeBlazorExtended.Foundation.Services;

public partial class NotificationService
{
    /// <summary>Fired on the calling thread immediately after a notification is added to the queue.</summary>
    public event Action<NotificationMessage> OnNotificationAdded;

    /// <summary>Fired on the calling thread immediately after a notification is removed from the queue (by timeout or explicit dismissal).</summary>
    public event Action<string> OnNotificationRemoved;

    protected List<NotificationMessage> _notifications = new();
    protected string _notificationIdPrefix = Guid.NewGuid().ToString().Substring(0, 8);
    protected int _notificationCounter = 0;

    /// <summary>
    /// Enqueues a success-themed notification with a default 5-second auto-dismiss.
    /// </summary>
    /// <param name="message">The body text shown inside the toast. Required.</param>
    /// <param name="title">Header text. Defaults to <c>"Success"</c>.</param>
    /// <param name="durationMs">
    /// How long (milliseconds) before the toast auto-dismisses. Pass <c>0</c> to require
    /// the user to close it manually. Default: <c>5000</c>.
    /// </param>
    public void ShowSuccess(string message, string title = "Success", int durationMs = 5000)
    {
        Show(message, NotificationType.Success, title, durationMs);
    }

    /// <summary>
    /// Enqueues an error-themed notification that stays visible until manually dismissed
    /// (default <paramref name="durationMs"/> is <c>0</c>, meaning it never auto-dismisses).
    /// </summary>
    /// <param name="message">The body text. Required — should describe what went wrong.</param>
    /// <param name="title">Header text. Defaults to <c>"Error"</c>.</param>
    /// <param name="durationMs">
    /// Override the default sticky behaviour by passing a positive value in milliseconds.
    /// Default: <c>0</c> (stays until dismissed).
    /// </param>
    public void ShowError(string message, string title = "Error", int durationMs = 0)
    {
        Show(message, NotificationType.Error, title, durationMs);
    }

    /// <summary>
    /// Enqueues an info-themed notification with a default 5-second auto-dismiss.
    /// </summary>
    /// <param name="message">The body text. Required.</param>
    /// <param name="title">Header text. Defaults to <c>"Information"</c>.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds. Default: <c>5000</c>.</param>
    public void ShowInfo(string message, string title = "Information", int durationMs = 5000)
    {
        Show(message, NotificationType.Info, title, durationMs);
    }

    /// <summary>
    /// Enqueues a warning-themed notification with a default 5-second auto-dismiss.
    /// </summary>
    /// <param name="message">The body text. Required.</param>
    /// <param name="title">Header text. Defaults to <c>"Warning"</c>.</param>
    /// <param name="durationMs">Auto-dismiss delay in milliseconds. Default: <c>5000</c>.</param>
    public void ShowWarning(string message, string title = "Warning", int durationMs = 5000)
    {
        Show(message, NotificationType.Warning, title, durationMs);
    }

    /// <summary>
    /// Core enqueue method. All typed overloads delegate here.
    /// Call this directly when you need a notification type not covered by the typed helpers.
    /// </summary>
    /// <param name="message">Body text displayed inside the notification.</param>
    /// <param name="type">Visual theme and semantics of the notification.</param>
    /// <param name="title">Optional header. Displayed above the message when non-empty.</param>
    /// <param name="durationMs">
    /// Auto-dismiss delay in milliseconds. Pass <c>0</c> to require manual dismissal.
    /// </param>
    public void Show(string message, NotificationType type, string title = "", int durationMs = 5000)
    {
        var notification = new NotificationMessage
        {
            Id = $"{_notificationIdPrefix}_{_notificationCounter++}",
            Message = message,
            Title = title,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            DurationMs = durationMs
        };

        _notifications.Add(notification);
        OnNotificationAdded?.Invoke(notification);

        // Auto-remove after duration if specified
        if (durationMs > 0) {
            _ = Task.Delay(durationMs).ContinueWith(_ => Remove(notification.Id));
        }
    }

    /// <summary>
    /// Removes a single notification from the queue and fires <see cref="OnNotificationRemoved"/>.
    /// Called automatically after the auto-dismiss delay; also safe to call from a dismiss button.
    /// </summary>
    /// <param name="id">The <see cref="NotificationMessage.Id"/> of the notification to remove.</param>
    public void Remove(string id)
    {
        _notifications.RemoveAll(n => n.Id == id);
        OnNotificationRemoved?.Invoke(id);
    }

    /// <summary>Returns a snapshot of every currently-visible notification, in add order.</summary>
    public List<NotificationMessage> GetAll() => new(_notifications);

    /// <summary>
    /// Removes every active notification at once and fires <see cref="OnNotificationRemoved"/>
    /// for each one. Useful for a "clear all" button in a notification tray.
    /// </summary>
    public void ClearAll()
    {
        var ids = _notifications.Select(n => n.Id).ToList();
        _notifications.Clear();
        foreach (var id in ids) {
            OnNotificationRemoved?.Invoke(id);
        }
    }
}

/// <summary>
/// Immutable snapshot of a single notification item managed by <see cref="NotificationService"/>.
/// Created internally by the service; callers receive this via the <see cref="NotificationService.OnNotificationAdded"/> event.
/// </summary>
public class NotificationMessage
{
    /// <summary>Unique identifier assigned by the service. Use this when calling <see cref="NotificationService.Remove"/>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Short header displayed above the body text. May be empty.</summary>
    public string Title { get; set; } = "";

    /// <summary>The main body text of the notification.</summary>
    public string Message { get; set; } = "";

    /// <summary>Controls the visual theme (icon, colour) of the rendered notification.</summary>
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>UTC timestamp when the notification was enqueued. Useful for ordering or time-since display.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Auto-dismiss delay in milliseconds. <c>0</c> means the notification stays visible
    /// until <see cref="NotificationService.Remove"/> is called explicitly.
    /// </summary>
    public int DurationMs { get; set; } = 5000;
}

/// <summary>Visual and semantic classification for a <see cref="NotificationMessage"/>.</summary>
public enum NotificationType
{
    /// <summary>Neutral informational message (blue tones).</summary>
    Info,
    /// <summary>Confirms a successful operation (green tones).</summary>
    Success,
    /// <summary>Alerts the user to a potential issue that does not block progress (amber tones).</summary>
    Warning,
    /// <summary>Signals a failure or blocking problem that requires attention (red tones).</summary>
    Error
}
