using Microsoft.EntityFrameworkCore;

namespace FreeGLBA;

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
