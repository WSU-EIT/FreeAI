using Microsoft.AspNetCore.Components;

namespace FreeServicesHub.Client;

// ---- FreeServicesHub.App.Client.AgentStatus.cs ----
// Agent status badge colours and the CPU/memory/disk warning and error thresholds,
// shared by the Agent Dashboard and Agent Detail so the thresholds cannot drift.

public static class AppAgentStatus
{
    public const double CpuWarn = 70, CpuError = 90;
    public const double MemoryWarn = 75, MemoryError = 90;
    public const double DiskWarn = 80, DiskError = 95;

    /// <summary>The Bootstrap progress-bar colour for a metric value.</summary>
    public static string GetMetricClass(double value, double warn, double error) =>
        value >= error ? "bg-danger" :
        value >= warn  ? "bg-warning text-dark" :
                         "bg-success";

    /// <summary>The Bootstrap text colour for a metric value.</summary>
    public static string GetMetricTextClass(double value, double warn, double error) =>
        value >= error ? "text-danger" :
        value >= warn  ? "text-warning" :
                         "text-success";

    /// <summary>The Bootstrap badge colour for an agent status.</summary>
    public static string GetStatusBadgeClass(string status) => status switch {
        DataObjects.AgentStatuses.Online  => "bg-success",
        DataObjects.AgentStatuses.Warning => "bg-warning text-dark",
        DataObjects.AgentStatuses.Error   => "bg-danger",
        DataObjects.AgentStatuses.Offline => "bg-secondary",
        DataObjects.AgentStatuses.Stale   => "bg-secondary",
        _ => "bg-secondary"
    };

    /// <summary>
    /// The worst of the agent's reported status and its latest CPU, memory and disk readings:
    /// "offline", "error", "warning" or "online". With no heartbeat it returns the agent's own status.
    /// </summary>
    public static string GetWorstStatus(DataObjects.Agent agent, DataObjects.AgentHeartbeat? hb, List<DataObjects.DiskMetric>? disks)
    {
        if (hb == null) {
            return agent.Status;
        }

        bool hasError = hb.CpuPercent >= CpuError || hb.MemoryPercent >= MemoryError;
        bool hasWarning = hb.CpuPercent >= CpuWarn || hb.MemoryPercent >= MemoryWarn;

        if (disks != null) {
            foreach (var disk in disks) {
                if (disk.Percent >= DiskError) hasError = true;
                else if (disk.Percent >= DiskWarn) hasWarning = true;
            }
        }

        if (agent.Status == DataObjects.AgentStatuses.Offline || agent.Status == DataObjects.AgentStatuses.Stale) return "offline";
        if (agent.Status == DataObjects.AgentStatuses.Error || hasError) return "error";
        if (agent.Status == DataObjects.AgentStatuses.Warning || hasWarning) return "warning";
        return "online";
    }
}

// ---- FreeServicesHub.App.Client.Grid.cs ----
// Sort-header and pagination helpers shared by the Agent Management and Agent Settings tables.
// The pages keep their own sort/page state and call these for the markup and the page window.

public static class AppGrid
{
    /// <summary>The aria-sort value for a column header.</summary>
    public static string GetAriaSortValue(string sortColumn, bool sortAscending, string column) =>
        sortColumn != column ? "none" : sortAscending ? "ascending" : "descending";

    /// <summary>The sort arrow shown next to a column header.</summary>
    public static MarkupString SortIndicator(string sortColumn, bool sortAscending, string column)
    {
        if (sortColumn != column) return new MarkupString("<span class=\"text-muted opacity-50\" aria-hidden=\"true\">⇅</span>");
        return sortAscending
            ? new MarkupString("<span aria-hidden=\"true\">▲</span>")
            : new MarkupString("<span aria-hidden=\"true\">▼</span>");
    }

    /// <summary>The window of up to seven page numbers around the current page.</summary>
    public static IEnumerable<int> VisiblePageNumbers(int currentPage, int totalPages)
    {
        const int maxVisible = 7;
        int start = Math.Max(1, currentPage - maxVisible / 2);
        int end = Math.Min(totalPages, start + maxVisible - 1);
        start = Math.Max(1, end - maxVisible + 1);
        return Enumerable.Range(start, end - start + 1);
    }

    /// <summary>Keeps a requested page number inside 1..totalPages.</summary>
    public static int ClampPage(int page, int totalPages) => Math.Clamp(page, 1, totalPages);
}

// ---- FreeServicesHub.App.DataModel.cs ----
// FreeServicesHub additions to the client data model. DataModel.App.cs stays stock FreeCRM.

public partial class BlazorDataModel
{
    private List<DataObjects.HubJob> _ActiveJobs = new List<DataObjects.HubJob>();
    private List<DataObjects.Agent> _AgentStatuses = new List<DataObjects.Agent>();

    /// <summary>
    /// Active jobs for the dashboard. Updated by SignalR and manual load.
    /// </summary>
    public List<DataObjects.HubJob> ActiveJobs {
        get {
            return _ActiveJobs;
        }

        set {
            if (!ObjectsAreEqual(_ActiveJobs, value)) {
                _ActiveJobs = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// Current agent statuses for the dashboard. Updated by SignalR and initial model load.
    /// </summary>
    public List<DataObjects.Agent> AgentStatuses {
        get {
            return _AgentStatuses;
        }

        set {
            if (!ObjectsAreEqual(_AgentStatuses, value)) {
                _AgentStatuses = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }
}

// ---- FreeServicesHub.App.Helpers.cs ----
// FreeServicesHub client helpers, called from one line in each hook in Helpers.App.cs.
// Helpers.App.cs stays stock FreeCRM apart from those lines so a framework upgrade can overwrite it.

public static partial class Helpers
{
    /// <summary>
    /// FreeServicesHub icons. Merged into AppIcons in Helpers.App.cs.
    /// </summary>
    public static Dictionary<string, List<string>> FreeServicesHubAppIcons {
        get {
            return new Dictionary<string, List<string>> {
                { "fa:fa-solid fa-server", new List<string> { "AgentDashboard", "Agents" }},
                { "fa:fa-solid fa-heartbeat", new List<string> { "AgentHeartbeat" }},
                { "fa:fa-solid fa-key", new List<string> { "AgentApiKey", "RegistrationKey" }},
                { "fa:fa-solid fa-gears", new List<string> { "BackgroundServices" }},
                { "fa:fa-solid fa-sliders", new List<string> { "AgentSettings" }},
                { "fa:fa-solid fa-users-cog", new List<string> { "AgentManagement" }},
            };
        }
    }

    /// <summary>
    /// FreeServicesHub admin menu items. Added in MenuItemsAdminApp in Helpers.App.cs.
    /// </summary>
    public static List<DataObjects.MenuItem> FreeServicesHubMenuItemsAdminApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            output.Add(new DataObjects.MenuItem {
                Title = "Agent Management",
                Icon = "AgentManagement",
                PageNames = new List<string> { "agentmanagement" },
                SortOrder = 10,
                url = Helpers.BuildUrl("AgentManagement"),
                AppAdminOnly = true,
            });

            return output;
        }
    }

    /// <summary>
    /// FreeServicesHub top-level menu items. Added in MenuItemsApp in Helpers.App.cs.
    /// </summary>
    public static List<DataObjects.MenuItem> FreeServicesHubMenuItemsApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            output.Add(new DataObjects.MenuItem {
                Title = "Agent Dashboard",
                Icon = "AgentDashboard",
                PageNames = new List<string> { "agentdashboard" },
                SortOrder = 100,
                url = Helpers.BuildUrl("AgentDashboard"),
                AppAdminOnly = false,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Background Services",
                Icon = "BackgroundServices",
                PageNames = new List<string> { "backgroundservices" },
                SortOrder = 110,
                url = Helpers.BuildUrl("BackgroundServices"),
                AppAdminOnly = false,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Agent Settings",
                Icon = "AgentSettings",
                PageNames = new List<string> { "agentsettings" },
                SortOrder = 120,
                url = Helpers.BuildUrl("AgentSettings"),
                AppAdminOnly = false,
            });

            return output;
        }
    }

    /// <summary>
    /// FreeServicesHub's SignalR updates. Called as the first line of ProcessSignalRUpdateApp in Helpers.App.cs.
    /// Returns true when the update is a FreeServicesHub update for this tenant (or for all tenants) and has been
    /// handled, including the types the pages handle themselves. Anything else falls through to the stock code.
    /// </summary>
    public static async Task<bool> FreeServicesHubProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        await Task.CompletedTask;

        if (update == null || (update.TenantId != null && update.TenantId != Model.TenantId)) {
            return false;
        }

        switch (update.UpdateType) {
            case DataObjects.SignalRUpdateType.AgentHeartbeat:
                // Full agent list refresh from monitor service heartbeat
                if (update.Object is List<DataObjects.Agent> allAgents) {
                    Model.AgentStatuses = allAgents;
                }
                return true;

            case DataObjects.SignalRUpdateType.AgentConnected:
            case DataObjects.SignalRUpdateType.AgentDisconnected:
            case DataObjects.SignalRUpdateType.AgentStatusChanged:
                // Partial update — merge changed agents into the current list
                if (update.Object is List<DataObjects.Agent> changedAgents) {
                    List<DataObjects.Agent> current = new(Model.AgentStatuses);
                    foreach (DataObjects.Agent changed in changedAgents) {
                        int idx = current.FindIndex(a => a.AgentId == changed.AgentId);
                        if (idx >= 0) {
                            current[idx] = changed;
                        } else {
                            current.Add(changed);
                        }
                    }
                    Model.AgentStatuses = current;
                }
                return true;

            case DataObjects.SignalRUpdateType.BackgroundServiceLog:
                // Handled by the BackgroundServices page's own SignalRUpdate handler.
                return true;

            case DataObjects.SignalRUpdateType.AgentSettingsReport:
            case DataObjects.SignalRUpdateType.AgentSettingsUpdated:
                // Handled by the AgentSettings page's own SignalRUpdate handler.
                return true;

            case DataObjects.SignalRUpdateType.JobUpdated:
            case DataObjects.SignalRUpdateType.JobCompleted:
                // Handled by the AgentDashboard page's own SignalRUpdate handler.
                return true;
        }

        return false;
    }

    /// <summary>
    /// Loads agent summary data from the initial model hydration. Called from ReloadModelApp in Helpers.App.cs.
    /// </summary>
    public static async Task FreeServicesHubReloadModelApp(DataObjects.BlazorDataModelLoader? blazorDataModelLoader)
    {
        await Task.CompletedTask;

        if (blazorDataModelLoader != null) {
            Model.AgentStatuses = blazorDataModelLoader.AgentStatuses ?? new List<DataObjects.Agent>();
        }
    }
}
