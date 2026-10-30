namespace FreePlugins.Client;

// FreePlugins additions to the client Helpers. Each is called from one line in Helpers.App.cs.

public static partial class Helpers
{
    /// <summary>
    /// Icons for the FreePlugins menu items. Called from AppIcons in Helpers.App.cs.
    /// </summary>
    public static Dictionary<string, List<string>> FreePluginsAppIcons {
        get {
            return new Dictionary<string, List<string>> {
                { "fa:fa-solid fa-home", new List<string> { "IconName1", "IconName2" }},
                { "fa:fa-solid fa-puzzle-piece", new List<string> { "Plugins", "Plugin" }},
                { "fa:fa-solid fa-code", new List<string> { "Code", "Monaco" }},
                { "fa:fa-solid fa-vial", new List<string> { "Test", "Testing" }},
                { "fa:fa-solid fa-sort", new List<string> { "Sort", "Sorting" }},
            };
        }
    }

    /// <summary>
    /// The FreePlugins items in the Admin dropdown (admins only). Called from MenuItemsAdminApp in Helpers.App.cs.
    /// The routes are the real page routes; PageNames match each page's _pageName so the active item is highlighted.
    /// </summary>
    public static List<DataObjects.MenuItem> FreePluginsMenuItemsAdminApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            if (!Model.User.Admin) {
                return output;
            }

            output.Add(new DataObjects.MenuItem {
                Title = "Example Plugins",
                Icon = "Plugins",
                PageNames = new List<string> { "plugins" },
                url = Helpers.BuildUrl("Plugins"),
                SortOrder = 0,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Monaco Editor",
                Icon = "Code",
                PageNames = new List<string> { "monaco" },
                url = Helpers.BuildUrl("Monaco"),
                SortOrder = 100,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Double Click Test",
                Icon = "Test",
                PageNames = new List<string> { "doubleclick" },
                url = Helpers.BuildUrl("DoubleClick"),
                SortOrder = 100,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Sort Test",
                Icon = "Sort",
                PageNames = new List<string> { "sorttest" },
                url = Helpers.BuildUrl("SortTest"),
                SortOrder = 100,
            });

            output.Add(new DataObjects.MenuItem {
                Title = "Timer Test",
                Icon = "Test",
                PageNames = new List<string> { "timer" },
                url = Helpers.BuildUrl("TimerTest"),
                SortOrder = 100,
            });

            return output;
        }
    }
}
