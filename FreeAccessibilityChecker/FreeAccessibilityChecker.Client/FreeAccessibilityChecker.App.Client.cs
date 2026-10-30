using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace FreeAccessibilityChecker.Client;

// ---- FreeAccessibilityChecker.App.Client.Badges.cs ----
/// <summary>
/// The severity, run-status and run-border markup the scan pages share. Each method returns the same
/// Bootstrap HTML the pages used to build for themselves.
/// </summary>
public static class ScanBadges
{
    /// <summary>The badge for a violation severity. extraClass adds a class to the badge, e.g. "me-1".</summary>
    public static string SeverityBadge(string severity, string? extraClass = null)
    {
        string extra = String.IsNullOrEmpty(extraClass) ? "" : " " + extraClass;
        return severity.ToLower() switch {
            "critical" => "<span class=\"badge bg-danger" + extra + "\">Critical</span>",
            "serious" => "<span class=\"badge bg-warning text-dark" + extra + "\">Serious</span>",
            "moderate" => "<span class=\"badge bg-info text-dark" + extra + "\">Moderate</span>",
            "minor" => "<span class=\"badge bg-secondary" + extra + "\">Minor</span>",
            _ => "<span class=\"badge bg-secondary" + extra + "\">" + severity + "</span>",
        };
    }

    /// <summary>The badge for a scan run's status (Complete, Running, Queued, Failed).</summary>
    public static string RunStatusBadge(string? status)
    {
        return status switch {
            "Complete" or "Completed" => "<span class=\"badge bg-success\">Complete</span>",
            "Running" => "<span class=\"badge bg-primary\">Running</span>",
            "Queued" => "<span class=\"badge bg-info text-dark\">Queued</span>",
            "Failed" => "<span class=\"badge bg-danger\">Failed</span>",
            _ => "<span class=\"badge bg-secondary\">" + (status ?? "Unknown") + "</span>",
        };
    }

    /// <summary>The left border class for a scan run's card, by status and violation counts.</summary>
    public static string RunBorderClass(DataObjects.ScanRun run)
    {
        return run.Status switch {
            "Complete" or "Completed" when run.CriticalCount > 0 => "border-start border-danger border-3",
            "Complete" or "Completed" when run.TotalViolations > 0 => "border-start border-warning border-3",
            "Complete" or "Completed" => "border-start border-success border-3",
            "Running" => "border-start border-primary border-3",
            "Queued" => "border-start border-info border-3",
            "Failed" => "border-start border-danger border-3",
            _ => "",
        };
    }
}

// ---- FreeAccessibilityChecker.App.Client.PageBase.cs ----
/// <summary>
/// Base class for the scan and site UI components that follow the data model and SignalR updates:
/// it subscribes in OnInitialized, renders again when the data model changes while this view is showing,
/// and offers RequestThrottledRender so a burst of SignalR messages causes one render per RenderThrottleMs.
/// </summary>
public abstract class FreeAccessibilityCheckerPageBase : ComponentBase, IDisposable
{
    protected const int RenderThrottleMs = 300;

    private System.Threading.Timer? _renderTimer;
    private bool _renderPending = false;

    [Inject] protected BlazorDataModel Model { get; set; } = default!;

    /// <summary>The view name this component sets on Model.View; changes and updates for other views are ignored.</summary>
    protected abstract string PageName { get; }

    public virtual void Dispose()
    {
        _renderTimer?.Dispose();
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;
    }

    protected override void OnInitialized()
    {
        Model.OnChange += OnDataModelUpdated;
        Model.OnSignalRUpdate += SignalRUpdate;
        Model.View = PageName;
    }

    /// <summary>Runs after each throttled render, e.g. to scroll a log to its end.</summary>
    protected virtual Task AfterThrottledRender() => Task.CompletedTask;

    protected virtual void OnDataModelUpdated()
    {
        if (Model.View == PageName) {
            StateHasChanged();
        }
    }

    protected void RequestThrottledRender()
    {
        if (_renderPending) return;
        _renderPending = true;
        _renderTimer?.Dispose();
        _renderTimer = new System.Threading.Timer(_ => {
            _renderPending = false;
            InvokeAsync(async () => {
                StateHasChanged();
                await AfterThrottledRender();
            });
        }, null, RenderThrottleMs, System.Threading.Timeout.Infinite);
    }

    protected abstract void SignalRUpdate(DataObjects.SignalRUpdate update);
}

// ---- FreeAccessibilityChecker.App.Client.SiteTree.cs ----
/// <summary>A node in the Sites tree: a site, or a domain group that has no site of its own.</summary>
public class FreeAccessibilityCheckerSiteTreeNode
{
    public List<FreeAccessibilityCheckerSiteTreeNode> Children { get; set; } = new();
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public DataObjects.Site? Site { get; set; }
}

/// <summary>
/// Builds the Sites tree from the flat list of sites, and counts sites within a branch.
/// The Sites page (FreeAccessibilityChecker.App.UI.Sites) owns the tree's open/closed state.
/// </summary>
public static class FreeAccessibilityCheckerSiteTree
{
    /// <summary>
    /// Builds a hierarchical tree from the flat list of sites.
    /// Sites are grouped by domain, then nested by URL path segments.
    /// A site B is a child of site A if B's URL starts with A's URL.
    /// Subdomain sites (e.g. edit.em.wsu.edu) are children of their parent domain site (em.wsu.edu).
    /// </summary>
    public static List<FreeAccessibilityCheckerSiteTreeNode> Build(List<DataObjects.Site> filtered)
    {
        // Parse each site URL into parts for comparison.
        var parsed = filtered.Select(s => {
            try {
                var uri = new Uri(s.BaseUrl.TrimEnd('/'));
                return new { Site = s, Uri = uri, NormalizedUrl = uri.ToString().TrimEnd('/').ToLowerInvariant() };
            } catch {
                return new { Site = s, Uri = (Uri?)null, NormalizedUrl = s.BaseUrl.ToLowerInvariant() };
            }
        }).Where(x => x.Uri != null).ToList();

        // Sort by URL length so parents come before children.
        parsed.Sort((a, b) => a.NormalizedUrl!.Length.CompareTo(b.NormalizedUrl!.Length));

        // Build a flat list of tree nodes, assigning each to its closest parent.
        var nodeMap = new Dictionary<string, FreeAccessibilityCheckerSiteTreeNode>(); // key = normalized URL
        var roots = new List<FreeAccessibilityCheckerSiteTreeNode>();

        foreach (var item in parsed) {
            var node = new FreeAccessibilityCheckerSiteTreeNode {
                Key = "site-" + item.Site.SiteId,
                Label = item.Site.Name,
                Site = item.Site,
            };

            // Find the closest ancestor.
            FreeAccessibilityCheckerSiteTreeNode? parent = null;

            // 1. Check for path-based parent: same host, shorter path.
            string host = item.Uri!.Host.ToLowerInvariant();
            string path = item.Uri.AbsolutePath.TrimEnd('/');
            if (!String.IsNullOrEmpty(path) && path != "/") {
                // Walk up path segments to find a parent.
                string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                for (int i = segments.Length - 1; i >= 0; i--) {
                    string parentPath = i == 0 ? "" : "/" + String.Join("/", segments.Take(i));
                    string parentUrl = (item.Uri.Scheme + "://" + host + parentPath).ToLowerInvariant();
                    if (nodeMap.TryGetValue(parentUrl, out var pathParent)) {
                        parent = pathParent;
                        break;
                    }
                }
            }

            // 2. If no path parent, check for subdomain parent.
            if (parent == null) {
                string[] hostParts = host.Split('.');
                for (int i = 1; i < hostParts.Length - 1; i++) {
                    string parentHost = String.Join(".", hostParts.Skip(i));
                    // Try with scheme.
                    string parentUrl = (item.Uri.Scheme + "://" + parentHost).ToLowerInvariant();
                    if (nodeMap.TryGetValue(parentUrl, out var subParent)) {
                        parent = subParent;
                        break;
                    }
                    // Also try https specifically.
                    parentUrl = ("https://" + parentHost).ToLowerInvariant();
                    if (nodeMap.TryGetValue(parentUrl, out subParent)) {
                        parent = subParent;
                        break;
                    }
                }
            }

            if (parent != null) {
                parent.Children.Add(node);
            } else {
                roots.Add(node);
            }

            nodeMap[item.NormalizedUrl!] = node;
        }

        // Post-process: group orphan root nodes by base domain.
        // When multiple sites share a base domain (e.g. app.smartsheet.com,
        // community.smartsheet.com) but the bare domain isn't a site, create
        // a virtual grouping node so they appear nested together.
        roots = GroupRootsByBaseDomain(roots);

        return roots;
    }

    public static void BuildNodeMap(List<FreeAccessibilityCheckerSiteTreeNode> nodes, Dictionary<Guid, FreeAccessibilityCheckerSiteTreeNode> nodeMap)
    {
        foreach (var node in nodes) {
            if (node.Site != null) {
                nodeMap[node.Site.SiteId] = node;
            }
            BuildNodeMap(node.Children, nodeMap);
        }
    }

    /// <summary>
    /// Groups root-level tree nodes by their registrable base domain.
    /// If multiple roots share the same base domain, a virtual parent node is
    /// created so they appear nested (e.g. app.smartsheet.com and
    /// community.smartsheet.com → grouped under "smartsheet.com").
    /// </summary>
    private static List<FreeAccessibilityCheckerSiteTreeNode> GroupRootsByBaseDomain(List<FreeAccessibilityCheckerSiteTreeNode> roots)
    {
        var grouped = new Dictionary<string, List<FreeAccessibilityCheckerSiteTreeNode>>(StringComparer.OrdinalIgnoreCase);
        var ungrouped = new List<FreeAccessibilityCheckerSiteTreeNode>();

        foreach (var root in roots) {
            string baseDomain = "";
            if (root.Site != null) {
                try {
                    var host = new Uri(root.Site.BaseUrl).Host.ToLowerInvariant();
                    if (host.StartsWith("www.")) host = host.Substring(4);
                    baseDomain = GetBaseDomain(host);
                } catch { }
            }

            if (string.IsNullOrEmpty(baseDomain)) {
                ungrouped.Add(root);
            } else {
                if (!grouped.ContainsKey(baseDomain))
                    grouped[baseDomain] = new List<FreeAccessibilityCheckerSiteTreeNode>();
                grouped[baseDomain].Add(root);
            }
        }

        var result = new List<FreeAccessibilityCheckerSiteTreeNode>(ungrouped);
        foreach (var kvp in grouped) {
            if (kvp.Value.Count == 1) {
                // Only one site for this domain — keep at root level.
                result.Add(kvp.Value[0]);
            } else {
                // Multiple sites share the domain — create a virtual parent.
                var virtualNode = new FreeAccessibilityCheckerSiteTreeNode {
                    Key = "domain-" + kvp.Key,
                    Label = kvp.Key,
                    Site = null,
                    Children = kvp.Value.OrderBy(x => x.Label).ToList()
                };
                result.Add(virtualNode);
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts the registrable base domain from a hostname.
    /// e.g. "app.smartsheet.com" → "smartsheet.com",
    ///      "em.wsu.edu" → "wsu.edu",
    ///      "foo.co.uk" → "foo.co.uk".
    /// </summary>
    private static string GetBaseDomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length <= 2) return host;

        // Common multi-part TLD suffixes where the registrable domain
        // includes one extra label (e.g. example.co.uk).
        var multiPartTlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "co.uk", "org.uk", "ac.uk", "gov.uk",
            "com.au", "org.au", "edu.au",
            "co.nz", "org.nz",
            "co.jp", "or.jp", "ne.jp",
            "com.br", "org.br",
            "co.in", "org.in",
            "co.za",
        };

        string lastTwo = parts[^2] + "." + parts[^1];
        if (multiPartTlds.Contains(lastTwo) && parts.Length > 3) {
            return parts[^3] + "." + lastTwo;
        }

        return parts[^2] + "." + parts[^1];
    }

    public static int CountDescendants(FreeAccessibilityCheckerSiteTreeNode node)
    {
        int count = 0;
        foreach (var child in node.Children) {
            if (child.Site != null) count++;
            count += CountDescendants(child);
        }
        return count;
    }

    public static int CountEnabledDescendants(FreeAccessibilityCheckerSiteTreeNode node)
    {
        int count = 0;
        foreach (var child in node.Children) {
            if (child.Site != null && child.Site.Enabled) count++;
            count += CountEnabledDescendants(child);
        }
        return count;
    }

    public static void CollectAllNodeKeys(List<FreeAccessibilityCheckerSiteTreeNode> nodes, HashSet<string> keys)
    {
        foreach (var node in nodes) {
            if (node.Children.Any()) {
                keys.Add(node.Key);
                CollectAllNodeKeys(node.Children, keys);
            }
        }
    }
}

// ---- FreeAccessibilityChecker.App.Client.Wcag.cs ----
/// <summary>
/// Shared utility for matching WCAG conformance levels against violation criteria.
/// Handles both axe-style tags ("wcag2a", "wcag111") and criterion numbers ("1.1.1").
/// </summary>
public static class WcagLevelHelper
{
    /// <summary>Criterion number to lower-case level ("a" or "aa"), built from the shared WCAG criteria table.</summary>
    private static readonly Dictionary<string, string> CriterionLevels =
        DataObjects.WcagCriteria.All.ToDictionary(x => x.Criterion, x => x.Level.ToLower());

    private static readonly Regex CriterionRegex = new(@"(\d+)\.?(\d+)\.?(\d+)", RegexOptions.Compiled);

    public static bool MatchesWcagLevel(string? wcagCriteria, string levelFilter)
    {
        if (String.IsNullOrWhiteSpace(wcagCriteria)) return true; // Unclassified — show.
        var allowed = levelFilter switch {
            "a" => new HashSet<string> { "a" },
            "aa" => new HashSet<string> { "a", "aa" },
            "aaa" => new HashSet<string> { "a", "aa", "aaa" },
            _ => null
        };
        if (allowed == null) return true; // "all"

        var tags = wcagCriteria.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // Fail-open: only hide a violation if we definitively classified at least one of its
        // tags as belonging to a higher WCAG level than the user requested. Tags whose level
        // we cannot determine (e.g. WCAG 2.2 criteria missing from our local dictionary) do
        // not contribute to a "hide" decision — better to show too much than to silently
        // hide critical violations the user is hunting for.
        bool sawClassifiedTag = false;
        foreach (var tag in tags) {
            string lower = tag.ToLower();
            if (lower.StartsWith("wcag")) {
                string suffix = lower.Substring(4).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
                if (suffix == "a" || suffix == "aa" || suffix == "aaa") {
                    sawClassifiedTag = true;
                    if (allowed.Contains(suffix)) return true;
                    continue;
                }
            }
            string? normalized = NormalizeCriterion(tag);
            if (normalized != null && CriterionLevels.TryGetValue(normalized, out string? level)) {
                sawClassifiedTag = true;
                if (allowed.Contains(level)) return true;
            }
            // Tag was non-numeric or numeric-but-unknown-to-our-dictionary — don't count
            // it as a definitive classification, leaving sawClassifiedTag false on this tag.
        }
        return !sawClassifiedTag;
    }

    private static string? NormalizeCriterion(string raw)
    {
        string cleaned = raw.Trim().ToLower()
            .Replace("wcag", "").Replace("sc", "").Replace("-", ".").Replace("_", ".").Trim('.');
        var match = CriterionRegex.Match(cleaned);
        if (match.Success) {
            return match.Groups[1].Value + "." + match.Groups[2].Value + "." + match.Groups[3].Value;
        }
        return null;
    }
}

// ---- FreeAccessibilityChecker.App.DataModel.cs ----
// FreeAccessibilityChecker data-model properties (shared state for the dashboard, scan and site pages).

public partial class BlazorDataModel
{
    private Dictionary<Guid, DataObjects.ScanProgress> _ScanStatuses = new Dictionary<Guid, DataObjects.ScanProgress>();
    private List<DataObjects.Site> _SiteList = new List<DataObjects.Site>();

    /// <summary>
    /// Tracks active scan progress by SiteId. Updated by FreeAccessibilityCheckerProcessSignalRUpdateApp (in this file, Helpers section)
    /// and read by the TriggerScan and ScanDashboard pages.
    /// </summary>
    public Dictionary<Guid, DataObjects.ScanProgress> ScanStatuses {
        get {
            return _ScanStatuses;
        }

        set {
            if (!ObjectsAreEqual(_ScanStatuses, value)) {
                _ScanStatuses = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// The list of configured sites for the current tenant.
    /// Loaded by GetBlazorDataModelApp and used by the dashboard and scan pages.
    /// </summary>
    public List<DataObjects.Site> SiteList {
        get {
            return _SiteList;
        }

        set {
            if (!ObjectsAreEqual(_SiteList, value)) {
                _SiteList = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }
}

// ---- FreeAccessibilityChecker.App.Helpers.cs ----
// FreeAccessibilityChecker client code for the Helpers hooks, called from one line in each hook in Helpers.App.cs.

public static partial class Helpers
{
    /// <summary>
    /// FreeAccessibilityChecker's icons. Added to the stock icons in AppIcons.
    /// </summary>
    public static Dictionary<string, List<string>> FreeAccessibilityCheckerAppIcons {
        get {
            return new Dictionary<string, List<string>> {
                { "fa:fa-solid fa-globe", new List<string> { "Sites" }},
                { "fa:fa-solid fa-search", new List<string> { "Scans" }},
                { "fa:fa-solid fa-check-circle", new List<string> { "Compliance" }},
                { "fa:fa-solid fa-chart-line", new List<string> { "Trends" }},
                { "fa:fa-solid fa-file-pdf", new List<string> { "Audit" }},
                { "fa:fa-solid fa-desktop", new List<string> { "ScanMonitor" }},
                { "fa:fa-solid fa-bullseye", new List<string> { "Triage" }},
            };
        }
    }

    /// <summary>
    /// FreeAccessibilityChecker's top-level menu items (Sites, Compliance, Triage, Monitor, Trends). Added in MenuItemsApp.
    /// </summary>
    public static List<DataObjects.MenuItem> FreeAccessibilityCheckerMenuItemsApp {
        get {
            List<DataObjects.MenuItem> output = new List<DataObjects.MenuItem>();

            // Sites management — admin only.
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Sites",
                    Icon = "Sites",
                    PageNames = new List<string> { "sites", "editsite", "siteruns", "manualcheckwizard", "suppressions" },
                    SortOrder = 1000,
                    url = Helpers.BuildUrl("Settings/Sites"),
                    AppAdminOnly = true,
                });
            }

            // Compliance dashboard — visible to all users.
            // Sub-pages (status, scan detail, page detail, audit report) highlight this nav item.
            output.Add(new DataObjects.MenuItem {
                Title = "Compliance",
                Icon = "Compliance",
                PageNames = new List<string> { "compliance", "compliancestatus", "scandetail", "pagedetail", "auditreport" },
                SortOrder = 1010,
                url = Helpers.BuildUrl("Compliance"),
                AppAdminOnly = false,
            });

            // Triage — cross-site violation views. Admin only (uses cross-tenant queries).
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Triage",
                    Icon = "Triage",
                    PageNames = new List<string> { "sitescorecard", "crosssitesearch", "domaintree", "rulehotlist", "ruledetail" },
                    SortOrder = 1012,
                    url = Helpers.BuildUrl("Compliance/Scorecard"),
                    AppAdminOnly = true,
                });
            }

            // Scan Monitor — admin only.
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Monitor",
                    Icon = "ScanMonitor",
                    PageNames = new List<string> { "scanmonitor" },
                    SortOrder = 1015,
                    url = Helpers.BuildUrl("Settings/ScanMonitor"),
                    AppAdminOnly = true,
                });
            }

            // Trends reports — admin only.
            if (Model.User.Admin) {
                output.Add(new DataObjects.MenuItem {
                    Title = "Trends",
                    Icon = "Trends",
                    PageNames = new List<string> { "trendreport" },
                    SortOrder = 1020,
                    url = Helpers.BuildUrl("Reports/Trends"),
                    AppAdminOnly = true,
                });
            }

            return output;
        }
    }

    /// <summary>
    /// FreeAccessibilityChecker's SignalR updates (the scan lifecycle). Called as the first line of ProcessSignalRUpdateApp in
    /// Helpers.App.cs. Returns true when the update was a FreeAccessibilityChecker scan update for this tenant and has been handled;
    /// anything else (another tenant, or an unknown type) returns false and falls through to the stock handler.
    /// </summary>
    public static async Task<bool> FreeAccessibilityCheckerProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        if (update == null || (update.TenantId != null && update.TenantId != Model.TenantId)) {
            return false;
        }

        Guid? itemId = update.ItemId;
        string message = update.Message.ToLower();

        switch (update.UpdateType) {
            case DataObjects.SignalRUpdateType.ScanProgress:
                // Deserialize the ScanProgress payload and update the model's active scan statuses.
                if (!String.IsNullOrWhiteSpace(update.ObjectAsString)) {
                    try {
                        DataObjects.ScanProgress? progress = System.Text.Json.JsonSerializer.Deserialize<DataObjects.ScanProgress>(update.ObjectAsString);
                        if (progress != null) {
                            Model.ScanStatuses[progress.SiteId] = progress;
                        }
                    } catch {
                        await Helpers.ConsoleLog("Error deserializing ScanProgress");
                    }
                }
                return true;

            case DataObjects.SignalRUpdateType.ScanComplete:
                // Remove from active scans and refresh dashboard data.
                if (itemId.HasValue) {
                    // Find the SiteId associated with this scan run and remove from statuses.
                    List<Guid> keysToRemove = new List<Guid>();
                    foreach (KeyValuePair<Guid, DataObjects.ScanProgress> kvp in Model.ScanStatuses) {
                        if (kvp.Value.ScanRunId == itemId.Value) {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    foreach (Guid key in keysToRemove) {
                        Model.ScanStatuses.Remove(key);
                    }
                }
                await Helpers.ConsoleLog("Scan completed: " + message);
                return true;

            case DataObjects.SignalRUpdateType.ScanFailed:
                // Remove from active scans and show error notification.
                if (itemId.HasValue) {
                    List<Guid> failedKeys = new List<Guid>();
                    foreach (KeyValuePair<Guid, DataObjects.ScanProgress> kvp in Model.ScanStatuses) {
                        if (kvp.Value.ScanRunId == itemId.Value) {
                            failedKeys.Add(kvp.Key);
                        }
                    }
                    foreach (Guid key in failedKeys) {
                        Model.ScanStatuses.Remove(key);
                    }
                }
                await Helpers.ConsoleLog("Scan failed: " + message);
                return true;

            case DataObjects.SignalRUpdateType.ScanLog:
                // ScanLog entries are handled by individual pages (ScanDetail, SiteRuns)
                // via Model.OnSignalRUpdate subscriptions. No central processing needed.
                return true;

            case DataObjects.SignalRUpdateType.ScanStarted:
                // ScanStarted is informational; individual pages react via Model.OnSignalRUpdate.
                return true;
        }

        return false;
    }

    /// <summary>
    /// Copies the site list from the BlazorDataModelLoader into the Model. Called from ReloadModelApp.
    /// </summary>
    private static async Task FreeAccessibilityCheckerReloadModelApp(DataObjects.BlazorDataModelLoader? blazorDataModelLoader)
    {
        await Task.CompletedTask;

        // Load site list from the BlazorDataModelLoader into the Model.
        if (blazorDataModelLoader != null) {
            Model.SiteList = blazorDataModelLoader.SiteList;
        }
    }
}
