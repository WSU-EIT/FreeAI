using System.Net.NetworkInformation;

namespace FreeA11yChecker.Client;

public static partial class Helpers
{
    public static Dictionary<string, List<string>> AppIcons {
        get {
            Dictionary<string, List<string>> icons = new Dictionary<string, List<string>> {
                { "fa:fa-solid fa-home", new List<string> { "IconName1", "IconName2" }},
                { "fa:fa-solid fa-globe", new List<string> { "Sites" }},
                { "fa:fa-solid fa-search", new List<string> { "Scans" }},
                { "fa:fa-solid fa-check-circle", new List<string> { "Compliance" }},
                { "fa:fa-solid fa-chart-line", new List<string> { "Trends" }},
                { "fa:fa-solid fa-file-pdf", new List<string> { "Audit" }},
                { "fa:fa-solid fa-desktop", new List<string> { "ScanMonitor" }},
                { "fa:fa-solid fa-bullseye", new List<string> { "Triage" }},
            };

            return icons;
        }
    }

    public static bool AppMethod()
    {
        return true;
    }

    // {{ModuleItemStart:Tags}}
    public static List<DataObjects.Tag> AvailableTagListApp(DataObjects.TagModule? Module, List<Guid> ExcludeTags)
    {
        var output = new List<DataObjects.Tag>();

        if (Module != null) {
            switch (Module) {
                //case DataObjects.TagModule.AppTagType:
                //    output = Model.Tags.Where(x => !ExcludeTags.Contains(x.TagId) && x.UseInAppTagType == true)
                //        .OrderBy(x => x.Name)
                //        .ToList();
                //    break;

                default:
                    break;
            }
        }

        return output;
    }
    // {{ModuleItemEnd:Tags}}

    private static List<string> GetDeletedRecordTypesApp()
    {
        var output = new List<string>();

        // Add any app-specific deleted record types here.

        return output;
    }

    /// <summary>
    /// Gets the deleted records for a specific app type.
    /// </summary>
    /// <param name="deletedRecords">The DeletedRecords object.</param>
    /// <param name="type">The item type.</param>
    /// <returns>A nullable list of DeletedRecordItem objects.</returns>
    public static List<DataObjects.DeletedRecordItem>? GetDeletedRecordsForAppType(DataObjects.DeletedRecords deletedRecords, string type)
    {
        List<DataObjects.DeletedRecordItem>? output = null;

        switch (StringLower(type)) {
            //case "this":
            //    output = deletedRecords.That;
            //    break;

            default:
                break;
        }

        return output;
    }

    /// <summary>
    /// Gets the language tag for deleted records based on the app type.
    /// </summary>
    /// <param name="type">The item type.</param>
    /// <returns>The language tag for the item type.</returns>
    public static string GetDeletedRecordsLanguageTagForAppType(string type)
    {
        string output = String.Empty;

        switch (StringLower(type)) {
            //case "this":
            //    output = "That";
            //    break;

            default:
                break;
        }

        return output;
    }

    public static List<DataObjects.MenuItem> MenuItemsApp {
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

    public static List<DataObjects.MenuItem> MenuItemsAdminApp {
        get {
            // Add any app-specific admin menu items here.
            var output = new List<DataObjects.MenuItem>();

            return output;
        }
    }

    public static async Task ProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        if (update != null && (update.TenantId == null || update.TenantId == Model.TenantId)) {
            Guid? itemId = update.ItemId;
            string message = update.Message.ToLower();
            Guid? userId = update.UserId;

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
                    break;

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
                    break;

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
                    break;

                case DataObjects.SignalRUpdateType.ScanLog:
                    // ScanLog entries are handled by individual pages (ScanDetail, SiteRuns)
                    // via Model.OnSignalRUpdate subscriptions. No central processing needed.
                    break;

                case DataObjects.SignalRUpdateType.ScanStarted:
                    // ScanStarted is informational; individual pages react via Model.OnSignalRUpdate.
                    break;

                default:
                    await Helpers.ConsoleLog("Unknown SignalR Update Type Received");
                    break;
            }
        }
    }

    public static async Task ProcessSignalRUpdateAppUndelete(DataObjects.SignalRUpdate update)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        switch (Helpers.StringLower(update.Message)) {
            case "this":
                // Add code to reload your app-specific data based on the undelete type.
                break;
        }
    }

    private async static Task ReloadModelApp(DataObjects.BlazorDataModelLoader? blazorDataModelLoader)
    {
        await Task.CompletedTask;

        // Load site list from the BlazorDataModelLoader into the Model.
        if (blazorDataModelLoader != null) {
            Model.SiteList = blazorDataModelLoader.SiteList;
        }
    }

    private static void UpdateModelDeletedRecordCountsForAppItems(DataObjects.DeletedRecords deletedRecords)
    {
        // Model.DeletedRecordCounts.MyValue = deletedRecords.MyValue.Count();
    }

}
