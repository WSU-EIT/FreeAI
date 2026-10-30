using System.Net.NetworkInformation;

namespace FreeGLBA.Client;

// ---- Helpers ----

public static partial class Helpers
{
    
    public static List<DataObjects.MenuItem> FreeGLBAMenuItemsApp {
        get {
            // Add any app-specific top-level menu items here.
            var output = new List<DataObjects.MenuItem>();

            // GLBA Dashboard - visible to all logged in users
            output.Add(new DataObjects.MenuItem {
                Title = "GLBA Dashboard",
                Icon = "fa-solid fa-chart-line",
                PageNames = new List<string> { "glbadashboard" },
                SortOrder = 100,
                url = Helpers.BuildUrl("GlbaDashboard"),
                AppAdminOnly = false,
            });

            // Source Systems - Admin only
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Source Systems",
                    Icon = "fa-solid fa-server",
                    PageNames = new List<string> { "sourcesystems" },
                    SortOrder = 200,
                    url = Helpers.BuildUrl("SourceSystems"),
                    AppAdminOnly = false,
                });
            }

            // Access Events - visible to all logged in users
            output.Add(new DataObjects.MenuItem {
                Title = "Access Events",
                Icon = "fa-solid fa-list-check",
                PageNames = new List<string> { "accessevents" },
                SortOrder = 300,
                url = Helpers.BuildUrl("AccessEvents"),
                AppAdminOnly = false,
            });

            // Accessors (Users who accessed data) - visible to all logged in users
            output.Add(new DataObjects.MenuItem {
                Title = "Accessors",
                Icon = "fa-solid fa-user-shield",
                PageNames = new List<string> { "accessors" },
                SortOrder = 350,
                url = Helpers.BuildUrl("Accessors"),
                AppAdminOnly = false,
            });

            // Data Subjects - visible to all logged in users
            output.Add(new DataObjects.MenuItem {
                Title = "Data Subjects",
                Icon = "fa-solid fa-users",
                PageNames = new List<string> { "datasubjects" },
                SortOrder = 400,
                url = Helpers.BuildUrl("DataSubjects"),
                AppAdminOnly = false,
            });

            // Compliance Reports - Admin only
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Compliance Reports",
                    Icon = "fa-solid fa-file-contract",
                    PageNames = new List<string> { "compliancereports" },
                    SortOrder = 500,
                    url = Helpers.BuildUrl("ComplianceReports"),
                    AppAdminOnly = false,
                });
            }

            // API Explorer - Admin only (works with source-system API keys)
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "API Explorer",
                    Icon = "fa-solid fa-flask",
                    PageNames = new List<string> { "apiexplorer" },
                    SortOrder = 600,
                    url = Helpers.BuildUrl("ApiExplorer"),
                    AppAdminOnly = false,
                });
            }

            // GLBA Settings (alerts, timezone) - Admin only
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "GLBA Settings",
                    Icon = "fa-solid fa-sliders",
                    PageNames = new List<string> { "glbasettings" },
                    SortOrder = 650,
                    url = Helpers.BuildUrl("GlbaSettings"),
                    AppAdminOnly = false,
                });
            }

            return output;
        }
    }

    /// <summary>
    /// FreeGLBA's SignalR updates. Called as the first line of ProcessSignalRUpdateApp in Helpers.App.cs.
    /// Returns true when the update was a FreeGLBA update and has been handled.
    /// </summary>
    public static async Task<bool> FreeGLBAProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        await Task.CompletedTask;

        if (update == null || (update.TenantId != null && update.TenantId != Model.TenantId)) {
            return false;
        }

        switch (update.UpdateType) {
            case DataObjects.SignalRUpdateType.GlbaAccessEvent:
            case DataObjects.SignalRUpdateType.GlbaSourceSystem:
                // New access events or source-system changes. Pages subscribed to
                // Model.OnGlbaUpdate (dashboard, events, subjects, accessors,
                // source systems) reload their data.
                Model.NotifyGlbaUpdate(update);
                return true;
        }

        return false;
    }
}

// ---- DataModel ----

// FreeGLBA additions to the client data model.

public partial class BlazorDataModel
{
    /// <summary>
    /// Raised when a GLBA SignalR update arrives (new access events, source-system
    /// changes). Pages subscribe to refresh their data in real time and must
    /// unsubscribe in Dispose.
    /// </summary>
    public event Action<DataObjects.SignalRUpdate>? OnGlbaUpdate;

    /// <summary>Notify subscribed pages that GLBA data changed.</summary>
    public void NotifyGlbaUpdate(DataObjects.SignalRUpdate update)
    {
        OnGlbaUpdate?.Invoke(update);
    }
}

// ---- Client.Grid ----

// ============================================================================
// GRID HELPERS
// Sorting, paging, relative-time and link helpers shared by the list pages
// (Access Events, Accessors, Compliance Reports, Data Subjects, Source Systems).
// Each page keeps its own filter state and Sort() rules and calls these.
// ============================================================================

public static class AppGrid
{
    /// <summary>aria-sort value for a sortable column header.</summary>
    public static string AriaSort(string sortColumn, bool sortDescending, string column) =>
        sortColumn == column ? (sortDescending ? "descending" : "ascending") : "none";

    /// <summary>CSS class for the active sort column header (pages differ: "sort-active" or "text-primary").</summary>
    public static string SortClass(string sortColumn, string column, string activeClass = "sort-active") =>
        sortColumn == column ? activeClass : "";

    /// <summary>Font Awesome sort icon classes for a column header; inactiveClasses is what an unsorted column gets.</summary>
    public static string SortIcon(string sortColumn, bool sortDescending, string column, string inactiveClasses = "fa-sort text-muted") {
        if (sortColumn != column) return inactiveClasses;
        return sortDescending ? "fa-sort-down" : "fa-sort-up";
    }

    /// <summary>True for the keys that activate a clickable sort header (Enter or Space).</summary>
    public static bool IsSortKey(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e) => e.Key == "Enter" || e.Key == " ";

    /// <summary>
    /// Page numbers to show in a pager. slide = true shows up to 5 numbers centered on the current page and
    /// slides the window at the ends; false shows current +/- 2 clipped to the page range.
    /// </summary>
    public static IEnumerable<int> PageNumbers(int page, int totalPages, bool slide) {
        if (slide) {
            int start = Math.Max(1, page - 2);
            int end = Math.Min(totalPages, start + 4);
            start = Math.Max(1, end - 4);
            for (int i = start; i <= end; i++)
                yield return i;
        } else {
            var start = Math.Max(1, page - 2);
            var end = Math.Min(totalPages, page + 2);
            for (int i = start; i <= end; i++)
                yield return i;
        }
    }

    /// <summary>Short relative time: "Just now", "5m ago", "3h ago", "2d ago", then "MMM dd, yyyy".</summary>
    public static string RelativeTime(DateTime dt) {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dt.ToString("MMM dd, yyyy");
    }

    /// <summary>Link to a data subject's page; "#" when there is no subject id.</summary>
    public static string SubjectUrl(string? subjectId) {
        if (string.IsNullOrEmpty(subjectId)) return "#";
        var encoded = Uri.EscapeDataString(subjectId);
        return Helpers.BuildUrl($"DataSubjects/{encoded}");
    }
}

// ---- Client.TestData ----

// ============================================================================
// TEST DATA
// Sample values used by the Access Event test-data generators (the edit form's
// "generate" buttons and the bulk test-event generator on the Access Events page).
// ============================================================================

public static class AppTestData
{
    public static readonly string[] FirstNames = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "David", "Elizabeth", "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen" };
    public static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin" };
    public static readonly string[] Departments = { "Financial Aid", "Bursar", "Registrar", "Admissions", "Student Accounts", "Academic Advising", "IT Services", "Business Office", "Enrollment Services", "Records" };
    public static readonly string[] DataCategories = { "Financial Aid", "Financial Records", "Tax Info", "Payment History", "Account Balance", "Loan Info", "Scholarship Data", "FAFSA Data", "Direct Deposit", "1098-T" };
    public static readonly string[] SubjectTypes = { "Student", "Student", "Student", "Student", "Employee", "Applicant" }; // Weighted toward Student
    public static readonly string[] Purposes = { "Enrollment verification", "Aid disbursement review", "Account inquiry", "Payment processing", "Audit request", "Student inquiry", "Compliance check", "Report generation", "Data verification", "Annual review" };
}
