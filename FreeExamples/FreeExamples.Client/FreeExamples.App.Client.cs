using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Components;

namespace FreeExamples.Client;

// ---- Helpers ----

public static partial class Helpers
{
    private static DataObjects.MenuItem FreeExamplesMakeSubItem(string title, string pageName, string route, string? icon = null)
    {
        return new DataObjects.MenuItem {
            Title = title,
            Icon = icon ?? "",
            PageNames = new List<string> { pageName },
            url = Helpers.BuildUrl(route),
            AppAdminOnly = false,
        };
    }

    public static List<DataObjects.MenuItem> FreeExamplesMenuItemsApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            // One top-level entry per example category; the pages come from FreeExamplesExampleCatalog.
            foreach (var category in FreeExamplesExampleCatalog.Categories) {
                var url = Helpers.BuildUrl(category.LandingRoute);

                // Stock NavigationMenu renders MenuItem.Children as a dropdown, sorted by SortOrder then Title.
                // The first entry "{Title} (Default)" links to the category's own url; SortOrder keeps the catalog order.
                var children = new List<DataObjects.MenuItem> {
                    new DataObjects.MenuItem {
                        Title = category.Title + " (Default)",
                        Icon = category.Icon,
                        url = url,
                        AppAdminOnly = false,
                    },
                };
                children.AddRange(FreeExamplesExampleCatalog.DropdownPagesOf(category)
                    .Select(page => FreeExamplesMakeSubItem(page.MenuTitle ?? page.Title, page.PageName, page.Route, page.MenuIcon)));
                for (int i = 0; i < children.Count; i++) {
                    children[i].SortOrder = i;
                }

                output.Add(new DataObjects.MenuItem {
                    Title = category.Title,
                    Icon = category.Icon,
                    PageNames = FreeExamplesExampleCatalog.PageNamesOf(category),
                    SortOrder = category.SortOrder,
                    url = url,
                    AppAdminOnly = false,
                    Children = children,
                });
            }

            return output;
        }
    }

    /// <summary>
    /// FreeExamples' menu icons, keyed by the Font Awesome class and listing the page names that use it.
    /// Returned by Helpers.AppIcons in Helpers.App.cs.
    /// </summary>
    public static Dictionary<string, List<string>> FreeExamplesAppIcons {
        get {
            return new Dictionary<string, List<string>> {
                // Dashboard & Data
                { "fa:fa-solid fa-table-columns",      new List<string> { "ExamplesDashboard", "DashboardData", "BoardViews" }},
                { "fa:fa-solid fa-list",                new List<string> { "SampleItems", "EditSampleItem" }},
                { "fa:fa-solid fa-grip",                new List<string> { "SampleItemsV1" }},
                { "fa:fa-solid fa-columns",             new List<string> { "SampleItemsV2" }},
                { "fa:fa-solid fa-layer-group",         new List<string> { "SampleItemsV3", "ItemCards", "BacklogV3" }},
                { "fa:fa-solid fa-timeline",            new List<string> { "SampleItemsV4" }},
                { "fa:fa-solid fa-chart-bar",           new List<string> { "SampleItemsV5" }},
                { "fa:fa-solid fa-magnifying-glass",    new List<string> { "SearchAutocomplete" }},
                { "fa:fa-solid fa-table-cells",         new List<string> { "ComparisonTable", "StatusBoard" }},

                // Files & Media
                { "fa:fa-solid fa-file-arrow-up",       new List<string> { "FileDemo", "FilesMedia" }},
                { "fa:fa-solid fa-camera",              new List<string> { "FileDemoV1" }},
                { "fa:fa-solid fa-folder-open",         new List<string> { "FileDemoV2" }},
                { "fa:fa-solid fa-file-lines",          new List<string> { "FileDemoV3", "TicketsV1" }},
                { "fa:fa-solid fa-file-import",         new List<string> { "FileDemoV4" }},
                { "fa:fa-solid fa-paperclip",           new List<string> { "FileDemoV5" }},
                { "fa:fa-solid fa-shield-halved",       new List<string> { "FileDemoV6" }},
                { "fa:fa-solid fa-images",              new List<string> { "ImageGallery" }},
                { "fa:fa-solid fa-film",                new List<string> { "Carousel" }},
                { "fa:fa-solid fa-signature",           new List<string> { "SignatureDemo" }},
                { "fa:fa-solid fa-file-signature",      new List<string> { "SignatureV1" }},
                { "fa:fa-solid fa-file-contract",       new List<string> { "SignatureV2" }},
                { "fa:fa-solid fa-cloud-arrow-up",      new List<string> { "SignatureV3" }},
                { "fa:fa-solid fa-scale-balanced",      new List<string> { "SignatureV4" }},
                { "fa:fa-solid fa-stamp",               new List<string> { "SignatureV5" }},

                // UI Components
                { "fa:fa-solid fa-palette",             new List<string> { "BootstrapShowcase", "UIComponents" }},
                { "fa:fa-solid fa-envelope-open-text",  new List<string> { "BootstrapV1" }},
                { "fa:fa-solid fa-gear",                new List<string> { "BootstrapV2" }},
                { "fa:fa-solid fa-triangle-exclamation", new List<string> { "BootstrapV3" }},
                { "fa:fa-solid fa-id-card",             new List<string> { "BootstrapV4" }},
                { "fa:fa-solid fa-tag",                 new List<string> { "BootstrapV5" }},
                { "fa:fa-solid fa-filter",              new List<string> { "BootstrapV6" }},
                { "fa:fa-solid fa-gears",               new List<string> { "BootstrapV7" }},
                { "fa:fa-solid fa-bars-staggered",      new List<string> { "BootstrapV8" }},
                { "fa:fa-solid fa-pen-to-square",       new List<string> { "BootstrapV9" }},
                { "fa:fa-solid fa-window-restore",      new List<string> { "BootstrapV10" }},
                { "fa:fa-solid fa-table",               new List<string> { "BootstrapV11" }},
                { "fa:fa-solid fa-file-pen",            new List<string> { "BootstrapV12" }},
                { "fa:fa-solid fa-th-large",            new List<string> { "KanbanBoard" }},
                { "fa:fa-solid fa-bars-progress",       new List<string> { "PipelineTracker" }},
                { "fa:fa-solid fa-hat-wizard",          new List<string> { "WizardDemo" }},
                { "fa:fa-solid fa-terminal",            new List<string> { "CommandPalette" }},
                { "fa:fa-solid fa-comments",            new List<string> { "CommentThread" }},
                { "fa:fa-solid fa-message",             new List<string> { "ChatView" }},

                // Charts & Viz
                { "fa:fa-solid fa-chart-pie",           new List<string> { "ChartsDashboard", "ChartsViz" }},
                { "fa:fa-solid fa-chart-line",          new List<string> { "ChartsV1" }},
                { "fa:fa-solid fa-chart-area",          new List<string> { "ChartsV2" }},
                { "fa:fa-solid fa-heartbeat",           new List<string> { "ChartsV3" }},
                { "fa:fa-solid fa-users",               new List<string> { "ChartsV4" }},
                { "fa:fa-solid fa-globe",               new List<string> { "ChartsV5" }},
                { "fa:fa-solid fa-diagram-project",     new List<string> { "NetworkGraph", "Projects", "ProjectManagement" }},
                { "fa:fa-solid fa-sitemap",             new List<string> { "NetworkGraphV1" }},
                { "fa:fa-solid fa-share-nodes",         new List<string> { "NetworkGraphV2" }},

                // Code & Real-Time
                { "fa:fa-solid fa-code",                new List<string> { "CodeEditor", "CodeRealTime" }},
                { "fa:fa-solid fa-database",            new List<string> { "CodeEditorV1" }},
                { "fa:fa-solid fa-vial",                new List<string> { "CodeEditorV2" }},
                { "fa:fa-solid fa-file-code",           new List<string> { "CodeEditorV3" }},
                { "fa:fa-solid fa-code-compare",        new List<string> { "CodeEditorV4", "ProjectsV4" }},
                { "fa:fa-solid fa-wand-magic-sparkles", new List<string> { "CodeEditorV5" }},
                { "fa:fa-solid fa-laptop-code",         new List<string> { "CodePlayground" }},
                { "fa:fa-solid fa-tower-broadcast",     new List<string> { "SignalRDemo" }},
                { "fa:fa-solid fa-bell",                new List<string> { "SignalRV1" }},
                { "fa:fa-solid fa-user-group",          new List<string> { "SignalRV2" }},
                { "fa:fa-solid fa-square-poll-vertical", new List<string> { "SignalRV3" }},
                { "fa:fa-solid fa-rss",                 new List<string> { "SignalRV4" }},
                { "fa:fa-solid fa-trophy",              new List<string> { "SignalRV5" }},
                { "fa:fa-solid fa-stopwatch",           new List<string> { "TimerDemo" }},
                { "fa:fa-solid fa-tomato",              new List<string> { "TimerV1" }},
                { "fa:fa-solid fa-hourglass-half",      new List<string> { "TimerV2" }},
                { "fa:fa-solid fa-rotate",              new List<string> { "TimerV3" }},
                { "fa:fa-solid fa-clock",               new List<string> { "TimerV4" }},
                { "fa:fa-solid fa-calendar-day",        new List<string> { "TimerV5" }},
                { "fa:fa-solid fa-code-branch",         new List<string> { "GitBrowser" }},
                { "fa:fa-solid fa-key",                 new List<string> { "ApiKeyDemo" }},

                // Project Management
                { "fa:fa-solid fa-folder-tree",         new List<string> { "ProjectsV1" }},
                { "fa:fa-solid fa-pen-ruler",           new List<string> { "ProjectsV2" }},
                { "fa:fa-solid fa-rectangle-list",      new List<string> { "ProjectsV3" }},
                { "fa:fa-solid fa-ticket",              new List<string> { "Tickets" }},
                { "fa:fa-solid fa-clipboard-check",     new List<string> { "TicketsV2" }},
                { "fa:fa-solid fa-bolt",                new List<string> { "TicketsV3" }},
                { "fa:fa-solid fa-list-check",          new List<string> { "TicketsV4" }},
                { "fa:fa-solid fa-grip-vertical",       new List<string> { "BoardViewsV1" }},
                { "fa:fa-solid fa-person-running",      new List<string> { "BoardViewsV2" }},
                { "fa:fa-solid fa-water",               new List<string> { "BoardViewsV3" }},
                { "fa:fa-solid fa-sliders",             new List<string> { "BoardViewsV4" }},
                { "fa:fa-solid fa-flag-checkered",      new List<string> { "SprintPlanning" }},
                { "fa:fa-solid fa-arrows-left-right",   new List<string> { "SprintPlanningV1" }},
                { "fa:fa-solid fa-fire",                new List<string> { "SprintPlanningV2" }},
                { "fa:fa-solid fa-rotate-left",         new List<string> { "SprintPlanningV3" }},
                { "fa:fa-solid fa-gauge-high",          new List<string> { "SprintPlanningV4" }},
                { "fa:fa-solid fa-inbox",               new List<string> { "Backlog" }},
                { "fa:fa-solid fa-broom",               new List<string> { "BacklogV1" }},
                { "fa:fa-solid fa-object-group",        new List<string> { "BacklogV2" }},
                { "fa:fa-solid fa-bookmark",            new List<string> { "BacklogV4" }},

                // Domain Workflows
                { "fa:fa-solid fa-wrench",              new List<string> { "WorkOrders", "DomainWorkflows" }},
                { "fa:fa-solid fa-paper-plane",         new List<string> { "WorkOrdersV1" }},
                { "fa:fa-solid fa-truck-fast",          new List<string> { "WorkOrdersV2" }},
                { "fa:fa-solid fa-screwdriver-wrench",  new List<string> { "WorkOrdersV3" }},
                { "fa:fa-solid fa-chart-simple",        new List<string> { "WorkOrdersV4" }},
                { "fa:fa-solid fa-money-check-dollar",  new List<string> { "BudgetRequests" }},
                { "fa:fa-solid fa-calculator",          new List<string> { "BudgetRequestsV1" }},
                { "fa:fa-solid fa-check-double",        new List<string> { "BudgetRequestsV2" }},
                { "fa:fa-solid fa-piggy-bank",          new List<string> { "BudgetRequestsV3" }},
                { "fa:fa-solid fa-receipt",             new List<string> { "BudgetRequestsV4" }},
                { "fa:fa-solid fa-laptop",              new List<string> { "EquipmentCheckout" }},
                { "fa:fa-solid fa-hand-holding",        new List<string> { "EquipmentCheckoutV1" }},
                { "fa:fa-solid fa-cart-shopping",       new List<string> { "EquipmentCheckoutV2" }},
                { "fa:fa-solid fa-exclamation-triangle", new List<string> { "EquipmentCheckoutV3" }},
                { "fa:fa-solid fa-circle-info",         new List<string> { "EquipmentCheckoutV4" }},
                { "fa:fa-solid fa-star-half-stroke",    new List<string> { "CourseEvaluations" }},
                { "fa:fa-solid fa-pen",                 new List<string> { "CourseEvaluationsV1" }},
                { "fa:fa-solid fa-square-poll-horizontal", new List<string> { "CourseEvaluationsV2" }},
                { "fa:fa-solid fa-puzzle-piece",        new List<string> { "CourseEvaluationsV3" }},
                { "fa:fa-solid fa-building-columns",    new List<string> { "CourseEvaluationsV4" }},
                { "fa:fa-solid fa-user-plus",           new List<string> { "EmployeeOnboarding" }},
                { "fa:fa-solid fa-clipboard-list",      new List<string> { "EmployeeOnboardingV1" }},
                { "fa:fa-solid fa-tasks",               new List<string> { "EmployeeOnboardingV2" }},
                { "fa:fa-solid fa-id-badge",            new List<string> { "EmployeeOnboardingV3" }},
                { "fa:fa-solid fa-chart-gantt",         new List<string> { "EmployeeOnboardingV4" }},
            };
        }
    }

    /// <summary>
    /// FreeExamples' SignalR updates. Called as the first line of ProcessSignalRUpdateApp in Helpers.App.cs.
    /// Returns true when the update was a FreeExamples update and has been handled.
    /// </summary>
    public static async Task<bool> FreeExamplesProcessSignalRUpdateApp(DataObjects.SignalRUpdate update)
    {
        await Task.CompletedTask;

        if (update == null || (update.TenantId != null && update.TenantId != Model.TenantId)) {
            return false;
        }

        switch (update.UpdateType) {
            case DataObjects.SignalRUpdateType.SampleItemSaved:
            case DataObjects.SignalRUpdateType.SampleItemDeleted:
                // Let page-level SignalR subscribers handle the update.
                // The framework calls Model.SignalRUpdate(update) after this method.
                return true;
        }

        return false;
    }
}

// ---- Client.ExampleCatalog ----

/// <summary>One example page: where it sits in the examples tree and what the menu and the Prev/Next bar call it.</summary>
/// <param name="MenuTitle">Menu text when it differs from <paramref name="Title"/> (for example "Sig V1: ...").</param>
/// <param name="MenuIcon">Icon for the menu entry, when it has one.</param>
public record FreeExamplesExamplePageInfo(string Category, string PageName, string Title, string Route, string? ParentTitle = null, string? ParentRoute = null, string? MenuTitle = null, string? MenuIcon = null);

/// <summary>An example category: one top-level menu entry with a dropdown of its pages.</summary>
/// <param name="LandingRoute">Where the top-level menu entry links to.</param>
/// <param name="ExtraPageNames">Page names that highlight the category but are not listed as example pages.</param>
/// <param name="LandingInDropdown">True when the page at the landing route is also listed in the dropdown; false when the dropdown leaves it out.</param>
public record FreeExamplesExampleCategory(string Title, string Icon, int SortOrder, string LandingRoute, string[] ExtraPageNames, bool LandingInDropdown = false);

/// <summary>
/// The one list of example pages. The Prev/Next bar (FreeExamples_App_UI_ExampleNav), the menu
/// (Helpers.MyMenuItemsApp) and the Dashboard variant lists all read it, so adding an example page is one row here.
/// </summary>
public static class FreeExamplesExampleCatalog
{
    public static readonly List<FreeExamplesExampleCategory> Categories = new() {
        new("Dashboard & Data", "DashboardData", 100, "Examples/Dashboard", ["examplesdashboard", "editsampleitem"]),
        new("Files & Media", "FilesMedia", 200, "Examples/FileDemo", []),
        new("UI Components", "UIComponents", 300, "Examples/BootstrapShowcase", []),
        new("Charts & Viz", "ChartsViz", 400, "Examples/ChartsDashboard", []),
        new("Code & Real-Time", "CodeRealTime", 500, "Examples/CodeEditor", []),
        new("Project Management", "ProjectManagement", 600, "Examples/Projects", [], true),
        new("Domain Workflows", "DomainWorkflows", 700, "Examples/WorkOrders", [], true),
    };

    public static readonly List<FreeExamplesExamplePageInfo> Pages = new() {
        // Dashboard & Data
        new("Dashboard & Data", "sampleitems", "Sample Items", "Examples/SampleItems", MenuIcon: "Files"),
        new("Dashboard & Data", "sampleitemsv1", "V1: Card View", "Examples/SampleItemsV1", "Sample Items", "Examples/SampleItems"),
        new("Dashboard & Data", "sampleitemsv2", "V2: Split Panel", "Examples/SampleItemsV2", "Sample Items", "Examples/SampleItems"),
        new("Dashboard & Data", "sampleitemsv3", "V3: Grouped View", "Examples/SampleItemsV3", "Sample Items", "Examples/SampleItems"),
        new("Dashboard & Data", "sampleitemsv4", "V4: Timeline", "Examples/SampleItemsV4", "Sample Items", "Examples/SampleItems"),
        new("Dashboard & Data", "sampleitemsv5", "V5: Stats Dashboard", "Examples/SampleItemsV5", "Sample Items", "Examples/SampleItems"),
        new("Dashboard & Data", "searchautocomplete", "Search & Autocomplete", "Examples/SearchAutocomplete"),
        new("Dashboard & Data", "comparisontable", "Comparison Table", "Examples/ComparisonTable"),
        new("Dashboard & Data", "itemcards", "Item Cards", "Examples/ItemCards"),

        // Files & Media
        new("Files & Media", "filedemo", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov1", "V1: Profile Photo", "Examples/FileDemoV1", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov2", "V2: Document Library", "Examples/FileDemoV2", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov3", "V3: Resume Upload", "Examples/FileDemoV3", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov4", "V4: Bulk Import", "Examples/FileDemoV4", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov5", "V5: Case Attachments", "Examples/FileDemoV5", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "filedemov6", "V6: Upload Policy", "Examples/FileDemoV6", "File Demo", "Examples/FileDemo"),
        new("Files & Media", "imagegallery", "Image Gallery", "Examples/ImageGallery"),
        new("Files & Media", "carousel", "Carousel", "Examples/Carousel"),
        new("Files & Media", "signaturedemo", "Signature Demo", "Examples/SignatureDemo"),
        new("Files & Media", "signaturev1", "V1: Job Application", "Examples/SignatureV1", "Signature Demo", "Examples/SignatureDemo", MenuTitle: "Sig V1: Job Application"),
        new("Files & Media", "signaturev2", "V2: Doc Acknowledgment", "Examples/SignatureV2", "Signature Demo", "Examples/SignatureDemo", MenuTitle: "Sig V2: Doc Acknowledgment"),
        new("Files & Media", "signaturev3", "V3: Upload & Sign", "Examples/SignatureV3", "Signature Demo", "Examples/SignatureDemo", MenuTitle: "Sig V3: Upload & Sign"),
        new("Files & Media", "signaturev4", "V4: GLBA Consent", "Examples/SignatureV4", "Signature Demo", "Examples/SignatureDemo", MenuTitle: "Sig V4: GLBA Consent"),
        new("Files & Media", "signaturev5", "V5: Contract Signing", "Examples/SignatureV5", "Signature Demo", "Examples/SignatureDemo", MenuTitle: "Sig V5: Contract Signing"),

        // UI Components
        new("UI Components", "bootstrapshowcase", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv1", "V1: Email Builder", "Examples/BootstrapV1", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv2", "V2: Settings Page", "Examples/BootstrapV2", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv3", "V3: Error Pages", "Examples/BootstrapV3", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv4", "V4: User Profile", "Examples/BootstrapV4", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv5", "V5: Pricing Page", "Examples/BootstrapV5", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv6", "V6: Filter Panel", "Examples/BootstrapV6", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv7", "V7: Settings Admin", "Examples/BootstrapV7", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv8", "V8: Offcanvas Sidebar", "Examples/BootstrapV8", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv9", "V9: Master-Detail", "Examples/BootstrapV9", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv10", "V10: Modals & Toasts", "Examples/BootstrapV10", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv11", "V11: Data Table", "Examples/BootstrapV11", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "bootstrapv12", "V12: Request Form", "Examples/BootstrapV12", "Bootstrap Showcase", "Examples/BootstrapShowcase"),
        new("UI Components", "kanbanboard", "Kanban Board", "Examples/KanbanBoard"),
        new("UI Components", "statusboard", "Status Board", "Examples/StatusBoard"),
        new("UI Components", "pipelinetracker", "Pipeline Tracker", "Examples/PipelineTracker"),
        new("UI Components", "wizarddemo", "Wizard Demo", "Examples/WizardDemo"),
        new("UI Components", "commandpalette", "Command Palette", "Examples/CommandPalette"),
        new("UI Components", "commentthread", "Comment Thread", "Examples/CommentThread"),
        new("UI Components", "chatview", "Chat View", "Examples/ChatView"),

        // Charts & Viz
        new("Charts & Viz", "chartsdashboard", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "chartsv1", "V1: Sales Analytics", "Examples/ChartsV1", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "chartsv2", "V2: Enrollment Stats", "Examples/ChartsV2", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "chartsv3", "V3: System Health", "Examples/ChartsV3", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "chartsv4", "V4: HR Analytics", "Examples/ChartsV4", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "chartsv5", "V5: Web Analytics", "Examples/ChartsV5", "Charts Dashboard", "Examples/ChartsDashboard"),
        new("Charts & Viz", "networkgraph", "Network Graph", "Examples/NetworkGraph"),
        new("Charts & Viz", "networkgraphv1", "V1: Org Chart", "Examples/NetworkGraphV1", "Network Graph", "Examples/NetworkGraph", MenuTitle: "Net V1: Org Chart"),
        new("Charts & Viz", "networkgraphv2", "V2: Dependency Map", "Examples/NetworkGraphV2", "Network Graph", "Examples/NetworkGraph", MenuTitle: "Net V2: Dependency Map"),

        // Code & Real-Time
        new("Code & Real-Time", "codeeditor", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeeditorv1", "V1: SQL Query Builder", "Examples/CodeEditorV1", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeeditorv2", "V2: API Tester", "Examples/CodeEditorV2", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeeditorv3", "V3: Config Editor", "Examples/CodeEditorV3", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeeditorv4", "V4: Diff Viewer", "Examples/CodeEditorV4", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeeditorv5", "V5: Template Engine", "Examples/CodeEditorV5", "Code Editor", "Examples/CodeEditor"),
        new("Code & Real-Time", "codeplayground", "Code Playground", "Examples/CodePlayground"),
        new("Code & Real-Time", "signalrdemo", "SignalR Demo", "Examples/SignalRDemo"),
        new("Code & Real-Time", "signalrv1", "V1: Live Notifications", "Examples/SignalRV1", "SignalR Demo", "Examples/SignalRDemo", MenuTitle: "SR V1: Live Notifications"),
        new("Code & Real-Time", "signalrv2", "V2: Online Users", "Examples/SignalRV2", "SignalR Demo", "Examples/SignalRDemo", MenuTitle: "SR V2: Online Users"),
        new("Code & Real-Time", "signalrv3", "V3: Live Poll", "Examples/SignalRV3", "SignalR Demo", "Examples/SignalRDemo", MenuTitle: "SR V3: Live Poll"),
        new("Code & Real-Time", "signalrv4", "V4: Activity Feed", "Examples/SignalRV4", "SignalR Demo", "Examples/SignalRDemo", MenuTitle: "SR V4: Activity Feed"),
        new("Code & Real-Time", "signalrv5", "V5: Live Scoreboard", "Examples/SignalRV5", "SignalR Demo", "Examples/SignalRDemo", MenuTitle: "SR V5: Live Scoreboard"),
        new("Code & Real-Time", "timerdemo", "Timer Demo", "Examples/TimerDemo"),
        new("Code & Real-Time", "timerv1", "V1: Pomodoro", "Examples/TimerV1", "Timer Demo", "Examples/TimerDemo", MenuTitle: "Timer V1: Pomodoro"),
        new("Code & Real-Time", "timerv2", "V2: Session Timeout", "Examples/TimerV2", "Timer Demo", "Examples/TimerDemo", MenuTitle: "Timer V2: Session Timeout"),
        new("Code & Real-Time", "timerv3", "V3: Auto-Refresh", "Examples/TimerV3", "Timer Demo", "Examples/TimerDemo", MenuTitle: "Timer V3: Auto-Refresh"),
        new("Code & Real-Time", "timerv4", "V4: Quiz Timer", "Examples/TimerV4", "Timer Demo", "Examples/TimerDemo", MenuTitle: "Timer V4: Quiz Timer"),
        new("Code & Real-Time", "timerv5", "V5: Event Countdown", "Examples/TimerV5", "Timer Demo", "Examples/TimerDemo", MenuTitle: "Timer V5: Event Countdown"),
        new("Code & Real-Time", "gitbrowser", "Git Browser", "Examples/GitBrowser"),
        new("Code & Real-Time", "apikeydemo", "API Key Demo", "Examples/ApiKeyDemo"),

        // Project Management — Projects & Hierarchy
        new("Project Management", "projects", "Projects", "Examples/Projects"),
        new("Project Management", "projectsv1", "V1: Project Tree", "Examples/ProjectsV1", "Projects", "Examples/Projects"),
        new("Project Management", "projectsv2", "V2: Project Form", "Examples/ProjectsV2", "Projects", "Examples/Projects"),
        new("Project Management", "projectsv3", "V3: Project Cards", "Examples/ProjectsV3", "Projects", "Examples/Projects"),
        new("Project Management", "projectsv4", "V4: Project Comparison", "Examples/ProjectsV4", "Projects", "Examples/Projects"),

        // Project Management — Tickets
        new("Project Management", "tickets", "Tickets", "Examples/Tickets"),
        new("Project Management", "ticketsv1", "V1: Ticket Form", "Examples/TicketsV1", "Tickets", "Examples/Tickets"),
        new("Project Management", "ticketsv2", "V2: Ticket Detail", "Examples/TicketsV2", "Tickets", "Examples/Tickets"),
        new("Project Management", "ticketsv3", "V3: Quick Create", "Examples/TicketsV3", "Tickets", "Examples/Tickets"),
        new("Project Management", "ticketsv4", "V4: Bulk Edit", "Examples/TicketsV4", "Tickets", "Examples/Tickets"),

        // Project Management — Board Views
        new("Project Management", "boardviews", "Board Views", "Examples/BoardViews"),
        new("Project Management", "boardviewsv1", "V1: Kanban Board", "Examples/BoardViewsV1", "Board Views", "Examples/BoardViews"),
        new("Project Management", "boardviewsv2", "V2: Sprint Board", "Examples/BoardViewsV2", "Board Views", "Examples/BoardViews"),
        new("Project Management", "boardviewsv3", "V3: Swimlane Board", "Examples/BoardViewsV3", "Board Views", "Examples/BoardViews"),
        new("Project Management", "boardviewsv4", "V4: Board Settings", "Examples/BoardViewsV4", "Board Views", "Examples/BoardViews"),

        // Project Management — Sprint Planning
        new("Project Management", "sprintplanning", "Sprint Planning", "Examples/SprintPlanning"),
        new("Project Management", "sprintplanningv1", "V1: Planning View", "Examples/SprintPlanningV1", "Sprint Planning", "Examples/SprintPlanning"),
        new("Project Management", "sprintplanningv2", "V2: Active Sprint", "Examples/SprintPlanningV2", "Sprint Planning", "Examples/SprintPlanning"),
        new("Project Management", "sprintplanningv3", "V3: Retrospective", "Examples/SprintPlanningV3", "Sprint Planning", "Examples/SprintPlanning"),
        new("Project Management", "sprintplanningv4", "V4: Velocity Report", "Examples/SprintPlanningV4", "Sprint Planning", "Examples/SprintPlanning"),

        // Project Management — Backlog & Grooming
        new("Project Management", "backlog", "Backlog", "Examples/Backlog"),
        new("Project Management", "backlogv1", "V1: Grooming View", "Examples/BacklogV1", "Backlog", "Examples/Backlog"),
        new("Project Management", "backlogv2", "V2: Bulk Operations", "Examples/BacklogV2", "Backlog", "Examples/Backlog"),
        new("Project Management", "backlogv3", "V3: Grouped Backlog", "Examples/BacklogV3", "Backlog", "Examples/Backlog"),
        new("Project Management", "backlogv4", "V4: Saved Views", "Examples/BacklogV4", "Backlog", "Examples/Backlog"),

        // Domain Workflows — Work Orders
        new("Domain Workflows", "workorders", "Work Orders", "Examples/WorkOrders"),
        new("Domain Workflows", "workordersv1", "V1: Submit Request", "Examples/WorkOrdersV1", "Work Orders", "Examples/WorkOrders"),
        new("Domain Workflows", "workordersv2", "V2: Dispatch Board", "Examples/WorkOrdersV2", "Work Orders", "Examples/WorkOrders"),
        new("Domain Workflows", "workordersv3", "V3: Technician View", "Examples/WorkOrdersV3", "Work Orders", "Examples/WorkOrders"),
        new("Domain Workflows", "workordersv4", "V4: Facilities Dashboard", "Examples/WorkOrdersV4", "Work Orders", "Examples/WorkOrders"),

        // Domain Workflows — Budget & Approvals
        new("Domain Workflows", "budgetrequests", "Budget Requests", "Examples/BudgetRequests"),
        new("Domain Workflows", "budgetrequestsv1", "V1: Request Builder", "Examples/BudgetRequestsV1", "Budget Requests", "Examples/BudgetRequests"),
        new("Domain Workflows", "budgetrequestsv2", "V2: Approval Queue", "Examples/BudgetRequestsV2", "Budget Requests", "Examples/BudgetRequests"),
        new("Domain Workflows", "budgetrequestsv3", "V3: Budget Overview", "Examples/BudgetRequestsV3", "Budget Requests", "Examples/BudgetRequests"),
        new("Domain Workflows", "budgetrequestsv4", "V4: Request Detail", "Examples/BudgetRequestsV4", "Budget Requests", "Examples/BudgetRequests"),

        // Domain Workflows — Equipment Checkout
        new("Domain Workflows", "equipmentcheckout", "Equipment Checkout", "Examples/EquipmentCheckout"),
        new("Domain Workflows", "equipmentcheckoutv1", "V1: Checkout Form", "Examples/EquipmentCheckoutV1", "Equipment Checkout", "Examples/EquipmentCheckout"),
        new("Domain Workflows", "equipmentcheckoutv2", "V2: My Checkouts", "Examples/EquipmentCheckoutV2", "Equipment Checkout", "Examples/EquipmentCheckout"),
        new("Domain Workflows", "equipmentcheckoutv3", "V3: Overdue Report", "Examples/EquipmentCheckoutV3", "Equipment Checkout", "Examples/EquipmentCheckout"),
        new("Domain Workflows", "equipmentcheckoutv4", "V4: Asset Detail", "Examples/EquipmentCheckoutV4", "Equipment Checkout", "Examples/EquipmentCheckout"),

        // Domain Workflows — Course Evaluations
        new("Domain Workflows", "courseevaluations", "Course Evaluations", "Examples/CourseEvaluations"),
        new("Domain Workflows", "courseevaluationsv1", "V1: Take Evaluation", "Examples/CourseEvaluationsV1", "Course Evaluations", "Examples/CourseEvaluations"),
        new("Domain Workflows", "courseevaluationsv2", "V2: Results Summary", "Examples/CourseEvaluationsV2", "Course Evaluations", "Examples/CourseEvaluations"),
        new("Domain Workflows", "courseevaluationsv3", "V3: Template Builder", "Examples/CourseEvaluationsV3", "Course Evaluations", "Examples/CourseEvaluations"),
        new("Domain Workflows", "courseevaluationsv4", "V4: Department Report", "Examples/CourseEvaluationsV4", "Course Evaluations", "Examples/CourseEvaluations"),

        // Domain Workflows — Employee Onboarding
        new("Domain Workflows", "employeeonboarding", "Employee Onboarding", "Examples/EmployeeOnboarding"),
        new("Domain Workflows", "employeeonboardingv1", "V1: Onboarding Setup", "Examples/EmployeeOnboardingV1", "Employee Onboarding", "Examples/EmployeeOnboarding"),
        new("Domain Workflows", "employeeonboardingv2", "V2: Task Tracker", "Examples/EmployeeOnboardingV2", "Employee Onboarding", "Examples/EmployeeOnboarding"),
        new("Domain Workflows", "employeeonboardingv3", "V3: My Onboarding", "Examples/EmployeeOnboardingV3", "Employee Onboarding", "Examples/EmployeeOnboarding"),
        new("Domain Workflows", "employeeonboardingv4", "V4: Department Dashboard", "Examples/EmployeeOnboardingV4", "Employee Onboarding", "Examples/EmployeeOnboarding"),
    };

    /// <summary>The pages that are variants of the page at <paramref name="parentRoute"/>, in list order.</summary>
    public static List<FreeExamplesExamplePageInfo> VariantsOf(string parentRoute)
    {
        return Pages.Where(p => p.ParentRoute == parentRoute).ToList();
    }

    /// <summary>The dropdown pages of the category, in list order.</summary>
    public static List<FreeExamplesExamplePageInfo> DropdownPagesOf(FreeExamplesExampleCategory category)
    {
        return Pages.Where(p => p.Category == category.Title && (category.LandingInDropdown || p.Route != category.LandingRoute)).ToList();
    }

    /// <summary>Every page name that belongs to the category: its extra page names plus its listed pages.</summary>
    public static List<string> PageNamesOf(FreeExamplesExampleCategory category)
    {
        var output = new List<string>(category.ExtraPageNames);
        output.AddRange(Pages.Where(p => p.Category == category.Title).Select(p => p.PageName));
        return output;
    }
}

// ---- Client.ExamplePage ----

/// <summary>
/// Base class for the FreeExamples demo pages. Holds the lifecycle every page used to copy:
/// the TenantCode route parameter, the BlazorDataModel injection, subscribing to Model.OnChange
/// in OnInitialized, and the matching unsubscribe in Dispose.
/// Pages supply their PageName; pages with extra setup or cleanup override OnInitialized / Dispose
/// and call the base first.
/// </summary>
public abstract class FreeExamplesExamplePage : ComponentBase, IDisposable
{
    [Inject] protected BlazorDataModel Model { get; set; } = default!;

    [Parameter] public string? TenantCode { get; set; }

    /// <summary>The Model.View name of the page (for example "backlog").</summary>
    protected abstract string PageName { get; }

    /// <summary>Same value as <see cref="PageName"/>; the name the page markup uses.</summary>
    protected string _pageName => PageName;

    /// <summary>Called when the data model raises OnChange. By default the page re-renders.</summary>
    protected virtual void OnModelChanged() => StateHasChanged();

    protected override void OnInitialized()
    {
        Model.View = _pageName;
        Model.OnChange += OnModelChanged;
    }

    public virtual void Dispose()
    {
        Model.OnChange -= OnModelChanged;
    }
}

/// <summary>
/// Same lifecycle, but the page only re-renders when the data model change is for its own view
/// (the pages that used to declare OnDataModelUpdated).
/// </summary>
public abstract class FreeExamplesFilteredExamplePage : FreeExamplesExamplePage
{
    protected override void OnModelChanged()
    {
        if (Model.View == _pageName) { StateHasChanged(); }
    }
}

// ---- Client.Badges ----

/// <summary>
/// Badge, colour, icon and size helpers that the demo pages used to copy into their own @code blocks.
/// Pages that call them add <c>@using static FreeExamples.Client.FreeExamplesBadges</c>.
/// </summary>
public static class FreeExamplesBadges
{
    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    public static string GetPriorityBadge(DataObjects.TicketPriority p) => p switch { DataObjects.TicketPriority.Critical => "bg-danger", DataObjects.TicketPriority.High => "bg-warning text-dark", DataObjects.TicketPriority.Medium => "bg-info", _ => "bg-secondary" };

    public static string GetSprintBadge(DataObjects.SprintStatus s) => s switch {
        DataObjects.SprintStatus.Active => "bg-success", DataObjects.SprintStatus.Completed => "bg-primary",
        DataObjects.SprintStatus.Cancelled => "bg-dark", _ => "bg-secondary"
    };

    public static string GetStatusBadge(DataObjects.ProjectStatus s) => s switch {
        DataObjects.ProjectStatus.Active => "bg-success",
        DataObjects.ProjectStatus.Completed => "bg-primary",
        DataObjects.ProjectStatus.OnHold => "bg-warning text-dark",
        DataObjects.ProjectStatus.Archived => "bg-dark",
        _ => "bg-secondary"
    };

    public static string GetStatusBadge(DataObjects.SampleItemStatus status) => status switch {
        DataObjects.SampleItemStatus.Draft => "bg-secondary",
        DataObjects.SampleItemStatus.Active => "bg-success",
        DataObjects.SampleItemStatus.Completed => "bg-primary",
        DataObjects.SampleItemStatus.Archived => "bg-dark",
        _ => "bg-secondary",
    };

    public static string GetStatusBadge(DataObjects.TicketStatus s) => s switch { DataObjects.TicketStatus.Done or DataObjects.TicketStatus.Closed => "bg-success", DataObjects.TicketStatus.InProgress => "bg-primary", _ => "bg-secondary" };

    public static string GetTypeIcon(DataObjects.TicketType t) => t switch {
        DataObjects.TicketType.Epic => "fa-solid fa-bolt text-purple", DataObjects.TicketType.Story => "fa-solid fa-book-open text-success",
        DataObjects.TicketType.Bug => "fa-solid fa-bug text-danger", DataObjects.TicketType.Task => "fa-solid fa-check-square text-primary",
        DataObjects.TicketType.Improvement => "fa-solid fa-arrow-up text-info", _ => "fa-solid fa-puzzle-piece text-muted"
    };

    public static string GetUrgencyColor(DataObjects.WorkOrderUrgency u) => u switch { DataObjects.WorkOrderUrgency.Emergency => "#dc3545", DataObjects.WorkOrderUrgency.High => "#fd7e14", DataObjects.WorkOrderUrgency.Normal => "#0d6efd", _ => "#6c757d" };
}
