using System.Net.NetworkInformation;

namespace FreeManager.Client;

public static partial class Helpers
{
    public static Dictionary<string, List<string>> AppIcons {
        get {
            Dictionary<string, List<string>> icons = new Dictionary<string, List<string>> {
                { "fa:fa-solid fa-cubes", new List<string> { "Builder", "AppBuilder" }},
                { "fa:fa-solid fa-gauge-high", new List<string> { "Dashboard" }},
                { "fa:fa-solid fa-wand-magic-sparkles", new List<string> { "Wizard", "EntityWizard" }},
                { "fa:fa-solid fa-layer-group", new List<string> { "Templates", "TemplateGallery" }},
                { "fa:fa-solid fa-hat-wizard", new List<string> { "SetupWizard", "SetupWizardBuilder" }},
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
            var output = new List<DataObjects.MenuItem>();

            // New Template Selection - main entry point
            output.Add(new DataObjects.MenuItem {
                Title = "New Project",
                Icon = "Templates",
                PageNames = new List<string> { "templates", "templates/setup" },
                SortOrder = 100,
                url = BuildUrl("Templates"),
                AppAdminOnly = false,
            });

            // My Projects - view saved projects
            output.Add(new DataObjects.MenuItem {
                Title = "My Projects",
                Icon = "Builder",
                PageNames = new List<string> { "appbuilder", "appbuilder/new", "appbuilder/edit" },
                SortOrder = 200,
                url = BuildUrl("AppBuilder"),
                AppAdminOnly = false,
            });

            return output;
        }
    }

    public static List<DataObjects.MenuItem> MenuItemsAdminApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            // Archive section - old pages moved here
            if (Model.User.Admin) {
                // Entity Wizard (legacy - for custom entity building)
                output.Add(new DataObjects.MenuItem {
                    Title = "Entity Wizard (Archive)",
                    Icon = "Wizard",
                    PageNames = new List<string> { "appbuilder/entitywizard" },
                    SortOrder = 800,
                    url = BuildUrl("AppBuilder/EntityWizard"),
                    AppAdminOnly = false,
                });

                // Template Gallery (legacy - view all template types)
                output.Add(new DataObjects.MenuItem {
                    Title = "Template Gallery (Archive)",
                    Icon = "Templates",
                    PageNames = new List<string> { "fm/templates" },
                    SortOrder = 810,
                    url = BuildUrl("fm/templates"),
                    AppAdminOnly = false,
                });

                // Dashboard (legacy)
                output.Add(new DataObjects.MenuItem {
                    Title = "Dashboard (Archive)",
                    Icon = "Dashboard",
                    PageNames = new List<string> { "appbuilder/dashboard" },
                    SortOrder = 820,
                    url = BuildUrl("AppBuilder/Dashboard"),
                    AppAdminOnly = false,
                });

                // Setup Wizard Builder (legacy)
                output.Add(new DataObjects.MenuItem {
                    Title = "Setup Wizard (Archive)",
                    Icon = "SetupWizard",
                    PageNames = new List<string> { "appbuilder/setupwizardbuilder" },
                    SortOrder = 830,
                    url = BuildUrl("AppBuilder/SetupWizardBuilder"),
                    AppAdminOnly = false,
                });
            }

            return output;
        }
    }

    public static async Task ProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        // Process any SignalR updates specific to your app here. See the main ProcessSignalRUpdate method for an example in the MainLayout.razor page.

        if (update != null && (update.TenantId == null || update.TenantId == Model.TenantId)) {
            var itemId = update.ItemId;
            string message = update.Message.ToLower();
            var userId = update.UserId;

            switch (update.UpdateType) {
                default:
                    // Since this is called only from the default method in the main handler here,
                    // we can assume that the update type is not recognized by this app.
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
        // Called from the main ReloadModel method in Helpers to load app-specific data.
    }

    private static void UpdateModelDeletedRecordCountsForAppItems(DataObjects.DeletedRecords deletedRecords)
    {
        // Model.DeletedRecordCounts.MyValue = deletedRecords.MyValue.Count();
    }

}
