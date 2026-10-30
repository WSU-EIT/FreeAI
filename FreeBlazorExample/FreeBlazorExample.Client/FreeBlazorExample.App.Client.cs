using FreeBlazorExtended.ExampleNav;

namespace FreeBlazorExample.Client;

// ---- Helpers ----

// FreeBlazorExample helper methods. Kept in this partial class so Helpers.App.cs stays a stock FreeCRM hook file.

public static partial class Helpers
{
    public static string BuildShowcaseAnchorUrl(string? anchorId)
    {
        var normalizedAnchor = NormalizeShowcaseAnchor(anchorId);

        if (string.IsNullOrWhiteSpace(normalizedAnchor)) {
            return BuildShowcaseUrl();
        }

        return BuildUrl("showcase#" + normalizedAnchor);
    }

    public static string BuildShowcaseCatalogAnchorUrl(string? anchorId)
    {
        var normalizedAnchor = NormalizeShowcaseAnchor(anchorId);

        if (string.IsNullOrWhiteSpace(normalizedAnchor)) {
            return BuildShowcaseCatalogUrl();
        }

        return BuildUrl("showcase/catalog#" + normalizedAnchor);
    }

    public static string BuildShowcaseCatalogUrl(string? subUrl = null)
    {
        if (string.IsNullOrWhiteSpace(subUrl)) {
            return BuildUrl("showcase/catalog");
        }

        return BuildUrl("showcase/catalog/" + subUrl.Trim().TrimStart('/'));
    }

    public static string BuildShowcaseUrl(string? subUrl = null)
    {
        if (string.IsNullOrWhiteSpace(subUrl)) {
            return BuildUrl("showcase");
        }

        return BuildUrl("showcase/" + subUrl.Trim().TrimStart('/'));
    }

    private static string NormalizeShowcaseAnchor(string? anchorId)
    {
        return (anchorId ?? string.Empty).Trim().TrimStart('#');
    }
}

// ---- Program ----

// FreeBlazorExample WebAssembly service registrations. The Client project's Program.cs has no FreeCRM App seam,
// so it carries one tagged line that calls AddFreeBlazorExampleApp (catalog gap: ask FreeCRM for a Client Program.App.cs).

public static class FreeBlazorExampleAppServices
{
    /// <summary>
    /// Registers the FreeBlazorExtended showcase services in the WebAssembly host.
    /// </summary>
    public static IServiceCollection AddFreeBlazorExampleApp(this IServiceCollection services)
    {
        // FreeBlazorExtended showcase services (Features 100-114)
        services.AddSingleton<FreeBlazorExtended.Foundation.Services.NotificationService>();
        services.AddSingleton<FreeBlazorExtended.DynamicForms.FormService>();
        services.AddSingleton<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
        services.AddSingleton<FreeBlazorExtended.Calendar.CalendarEventService>();
        services.AddSingleton<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();
        services.AddSingleton<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();
        services.AddSingleton<FreeBlazorExtended.HierarchicalTree.TreeService>();
        // NOTE: GitHubRepoService, GitRepoBrowserService, SmartsheetService removed.
        // Components now call server-proxy endpoints (/api/Showcase/*/...) via HttpClient.
        // Credentials are held server-side in FreeBlazorExampleExternalApiSessionStore, keyed by HttpOnly cookie.

        return services;
    }
}

// ---- Client.Showcase.Manifest ----

public static class FreeBlazorExampleShowcaseManifest
{
    private static IEnumerable<ExampleNav.NavPage> BuildCoreDemoPages()
    {
        var category = "Core Demos";
        var categoryRoute = Helpers.BuildShowcaseAnchorUrl("core-components");

        return
        [
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("autocomplete"), "AutoComplete"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("autogrowtext"), "AutoGrow Text"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("confirmationcode"), "Confirmation Code"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("datetimepicker"), "Date Time Picker"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("deleteconfirmation"), "Delete Confirmation"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("getinput"), "GetInput"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("multiselect"), "MultiSelect"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("pagedrecordset"), "Paged Recordset"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("stringlist"), "StringList"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("togglepassword"), "Toggle Password"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("github-repo"), "GitHub Repo Browser"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("generic-git"), "Git Repo Browser"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("smartsheet"), "Smartsheet Viewer"),
        ];
    }

    private static IEnumerable<ExampleNav.NavPage> BuildFeaturePages()
    {
        var category = "Full Features";
        var categoryRoute = Helpers.BuildShowcaseAnchorUrl("full-features");

        return
        [
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature101-dynamic-forms"), "Feature 101 Dynamic Forms"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature102-multi-view-sync"), "Feature 102 Multi-View Sync"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature103-calendar"), "Feature 103 Calendar & Scheduling"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature104-user-preferences"), "Feature 104 User Preferences"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature105-agent-monitoring"), "Feature 105 Agent Monitoring"),
            BuildPage(category, categoryRoute, Helpers.BuildShowcaseUrl("feature107-hierarchical-tree"), "Feature 107 Hierarchical Tree"),
        ];
    }

    private static ExampleNav.NavPage BuildPage(string category, string categoryRoute, string route, string title, string? parentTitle = null, string? parentRoute = null)
    {
        return new ExampleNav.NavPage
        {
            Category = category,
            CategoryRoute = categoryRoute,
            Route = route,
            Title = title,
            ParentTitle = parentTitle,
            ParentRoute = parentRoute,
        };
    }

    public static List<ExampleNav.NavPage> BuildPages()
    {
        var pages = new List<ExampleNav.NavPage>();

        pages.AddRange(BuildFeaturePages());
        pages.AddRange(BuildCoreDemoPages());
        pages.AddRange(BuildVariantFamilyPages(
            category: "Tier 1",
            categoryAnchor: "tier1-variants",
            families:
            [
                ("Signature", "signature"),
                ("InfoTip", "infotip"),
                ("NetworkChart", "networkchart"),
                ("Wizard", "wizard"),
                ("AboutSection", "aboutsection"),
            ]));
        pages.AddRange(BuildVariantFamilyPages(
            category: "Tier 2A",
            categoryAnchor: "tier2a-variants",
            families:
            [
                ("CommandPalette", "commandpalette"),
                ("Timer", "timer"),
                ("PipelineTracker", "pipelinetracker"),
                ("Timeline", "timeline"),
                ("ImageGallery", "imagegallery"),
            ]));
        pages.AddRange(BuildVariantFamilyPages(
            category: "Tier 2B",
            categoryAnchor: "tier2b-variants",
            families:
            [
                ("Carousel", "carousel"),
                ("KanbanBoard", "kanbanboard"),
                ("ExampleNav", "examplenav"),
                ("SelectFile", "selectfile"),
                ("RenderFiles", "renderfiles"),
            ]));

        return pages;
    }

    private static IEnumerable<ExampleNav.NavPage> BuildVariantFamilyPages(string category, string categoryAnchor, IEnumerable<(string Title, string Segment)> families)
    {
        foreach (var family in families) {
            for (var version = 1; version <= 3; version++) {
                yield return BuildPage(
                    category,
                    Helpers.BuildShowcaseAnchorUrl(categoryAnchor),
                    Helpers.BuildShowcaseUrl($"{family.Segment}/v{version}"),
                    $"{family.Title} V{version}",
                    family.Title,
                    Helpers.BuildShowcaseCatalogAnchorUrl(family.Segment));
            }
        }
    }
    public static string FirstExampleRoute => BuildPages().First().Route;

    /// <summary>
    /// Variant demo registry: "family/variant" (lower case) to the component that renders it.
    /// A non-null variant is passed to the component as its Variant parameter (folded families);
    /// a null variant means the component is a separate per-variant file.
    /// </summary>
    private static readonly Dictionary<string, (Type Type, string? Variant)> _variantComponents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aboutsection/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_AboutSection), "v1"),
        ["aboutsection/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_AboutSection), "v2"),
        ["aboutsection/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_AboutSection), "v3"),
        ["carousel/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Carousel), "v1"),
        ["carousel/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Carousel), "v2"),
        ["carousel/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Carousel), "v3"),
        ["commandpalette/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_CommandPalette), "v1"),
        ["commandpalette/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_CommandPalette), "v2"),
        ["commandpalette/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_CommandPalette), "v3"),
        ["examplenav/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ExampleNav), "v1"),
        ["examplenav/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ExampleNav), "v2"),
        ["examplenav/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ExampleNav), "v3"),
        ["imagegallery/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ImageGallery), "v1"),
        ["imagegallery/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ImageGallery), "v2"),
        ["imagegallery/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ImageGallery), "v3"),
        ["infotip/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_InfoTip), "v1"),
        ["infotip/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_InfoTip), "v2"),
        ["infotip/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_InfoTip), "v3"),
        ["kanbanboard/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_KanbanBoard_V1), null),
        ["kanbanboard/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_KanbanBoard_V2), null),
        ["kanbanboard/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_KanbanBoard_V3), null),
        ["networkchart/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_NetworkChart), "v1"),
        ["networkchart/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_NetworkChart), "v2"),
        ["networkchart/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_NetworkChart), "v3"),
        ["pipelinetracker/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_PipelineTracker), "v1"),
        ["pipelinetracker/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_PipelineTracker), "v2"),
        ["pipelinetracker/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_PipelineTracker), "v3"),
        ["renderfiles/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_RenderFiles), "v1"),
        ["renderfiles/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_RenderFiles), "v2"),
        ["renderfiles/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_RenderFiles), "v3"),
        ["selectfile/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_SelectFile), "v1"),
        ["selectfile/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_SelectFile), "v2"),
        ["selectfile/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_SelectFile), "v3"),
        ["signature/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Signature), "v1"),
        ["signature/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Signature), "v2"),
        ["signature/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Signature), "v3"),
        ["timeline/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timeline), "v1"),
        ["timeline/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timeline), "v2"),
        ["timeline/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timeline), "v3"),
        ["timer/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timer), "v1"),
        ["timer/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timer), "v2"),
        ["timer/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Timer), "v3"),
        ["toastcontainer/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ToastContainer), "v1"),
        ["toastcontainer/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ToastContainer), "v2"),
        ["toastcontainer/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_ToastContainer), "v3"),
        ["wizard/v1"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Wizard), "v1"),
        ["wizard/v2"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Wizard), "v2"),
        ["wizard/v3"] = (typeof(Shared.AppComponents.FreeBlazorExample_App_UI_Showcase_Variants_Wizard_V3), null),
    };

    public static (Type Type, Dictionary<string, object?>? Parameters)? ResolveVariant(string family, string variant)
    {
        if (!_variantComponents.TryGetValue($"{family}/{variant}", out var entry)) {
            return null;
        }

        return (entry.Type, entry.Variant is null ? null : new Dictionary<string, object?> { ["Variant"] = entry.Variant });
    }

    // ---- Component Catalog card data (moved from the catalog component) ----

    public static List<FreeBlazorExampleShowcaseSectionLink> BuildCatalogSectionLinks()
    {
        return new List<FreeBlazorExampleShowcaseSectionLink>
        {
            new() { AnchorId = "showcase-surfaces", Title = "Surfaces" },
            new() { AnchorId = "core-inputs", Title = "Core Inputs" },
            new() { AnchorId = "selection-collection", Title = "Selection + Collection" },
            new() { AnchorId = "data-actions", Title = "Data + Actions" },
            new() { AnchorId = "full-features", Title = "Feature Modules" },
            new() { AnchorId = "variant-families", Title = "Variant Families" },
        };
    }

    public static List<FreeBlazorExampleShowcaseCardItem> BuildCatalogSurfaceCards()
    {
        return new List<FreeBlazorExampleShowcaseCardItem>
        {
            new()
            {
                BadgeText = "Shared",
                Title = "FreeBlazorExample.App.UI.Showcase.Home",
                Summary = "Reusable landing surface for the main showcase route.",
                Detail = "This is the shared component behind /showcase. The page host stays thin and the surface can be embedded elsewhere.",
                CodePath = "FreeBlazorExample.Client/Shared/AppComponents/FreeBlazorExample.App.UI.Showcase.Home.razor",
                Links = new List<FreeBlazorExampleShowcaseCardLink>
                {
                    new() { Label = "Open Showcase", Href = Helpers.BuildShowcaseUrl() },
                },
            },
            new()
            {
                BadgeText = "Shared",
                Title = "FreeBlazorExample.App.UI.Showcase.Catalog",
                Summary = "Reusable catalog surface for the component index route.",
                Detail = "This shared component now owns the catalog layout, section index, embedded demos, and demo-directory cards.",
                CodePath = "FreeBlazorExample.Client/Shared/AppComponents/FreeBlazorExample.App.UI.Showcase.Catalog.razor",
                Links = new List<FreeBlazorExampleShowcaseCardLink>
                {
                    new() { Label = "Open Catalog", Href = Helpers.BuildShowcaseCatalogUrl() },
                },
            },
        };
    }

    public static List<FreeBlazorExampleShowcaseCardItem> BuildCatalogFeatureCards()
    {
        return new List<FreeBlazorExampleShowcaseCardItem>
        {
            CatalogFeatureCard("101", "Dynamic Forms", "JSON-schema-driven form builder and renderer.", "Use this for forms that vary by customer, workflow, or survey definition.", "FreeBlazorExtended/DynamicForms/", "feature101-dynamic-forms"),
            CatalogFeatureCard("102", "Multi-View Sync", "SignalR-backed master and slave session for synchronized views.", "Use this for guided presentations and shared screens.", "FreeBlazorExtended/MultiViewSync/", "feature102-multi-view-sync"),
            CatalogFeatureCard("103", "Calendar and Scheduling", "Calendar, recurrence, reservation, and conflict-detection surface.", "Use this for appointment booking and resource scheduling.", "FreeBlazorExtended/Calendar/", "feature103-calendar"),
            CatalogFeatureCard("104", "User Preferences", "Server-persisted per-user settings with roaming preference state.", "Use this for settings that survive cache clears and follow users across devices.", "FreeBlazorExtended/UserPreferences/", "feature104-user-preferences"),
            CatalogFeatureCard("105", "Agent Monitoring", "Distributed Windows host monitoring and remote command surface.", "Use this for central monitoring of services, IIS pools, and host telemetry.", "FreeBlazorExtended/AgentMonitoring/", "feature105-agent-monitoring"),
            CatalogFeatureCard("107", "Hierarchical Tree", "Recursive tree with drag-to-reparent and cycle prevention.", "Use this for org charts, folder structures, category trees, and menu builders.", "FreeBlazorExtended/HierarchicalTree/", "feature107-hierarchical-tree"),
        };
    }

    private static FreeBlazorExampleShowcaseCardItem CatalogFeatureCard(string badgeText, string title, string summary, string detail, string codePath, string route)
    {
        return new FreeBlazorExampleShowcaseCardItem
        {
            BadgeText = badgeText,
            Title = title,
            Summary = summary,
            Detail = detail,
            CodePath = codePath,
            Links = new List<FreeBlazorExampleShowcaseCardLink>
            {
                new() { Label = "Open Demo", Href = Helpers.BuildShowcaseUrl(route) },
            },
        };
    }

    public static List<FreeBlazorExampleShowcaseCardGroup> BuildCatalogVariantGroups()
    {
        return new List<FreeBlazorExampleShowcaseCardGroup>
        {
            new()
            {
                Title = "Tier 1",
                BadgeText = "15 pages",
                Description = "Foundational extracted components with focused V1, V2, and V3 routes.",
                Items = new List<FreeBlazorExampleShowcaseCardItem>
                {
                    CatalogVariantCard("Signature", "jSignature pad with binding, remount round-trip, and programmatic clear.", "FreeBlazorExtended/Signature/", "signature"),
                    CatalogVariantCard("InfoTip", "Inline explanation popover for labels, forms, and embedded snippets.", "FreeBlazorExtended/InfoTip/", "infotip"),
                    CatalogVariantCard("NetworkChart", "vis.js network graph with seeded layouts, callbacks, and ref-driven controls.", "FreeBlazorExtended/NetworkChart/", "networkchart"),
                    CatalogVariantCard("Wizard", "Stepper, header, and summary primitives composed into simple and advanced flows.", "FreeBlazorExtended/Wizard/", "wizard"),
                    CatalogVariantCard("AboutSection", "Collapsible contextual explainer card with rich child content.", "FreeBlazorExtended/AboutSection/", "aboutsection"),
                },
            },
            new()
            {
                Title = "Tier 2A",
                BadgeText = "15 pages",
                Description = "Search, timing, pipeline, timeline, and gallery families with isolated demos.",
                Items = new List<FreeBlazorExampleShowcaseCardItem>
                {
                    CatalogVariantCard("ToastContainer", "Global notification stack driven by NotificationService — success, error, warning, info with auto-dismiss.", "FreeBlazorExtended/Toast/", "toastcontainer"),
                    CatalogVariantCard("CommandPalette", "Ctrl+K overlay with categories, hotkeys, recent history, and callback routing.", "FreeBlazorExtended/CommandPalette/", "commandpalette"),
                    CatalogVariantCard("Timer", "Countdown timer with defaults, Pomodoro styling, and chained work and break behavior.", "FreeBlazorExtended/Timer/", "timer"),
                    CatalogVariantCard("PipelineTracker", "Horizontal process tracker with explicit states, details panel, and click handling.", "FreeBlazorExtended/PipelineTracker/", "pipelinetracker"),
                    CatalogVariantCard("Timeline", "Vertical event feed with actors, timestamps, rich details, and drill-in state.", "FreeBlazorExtended/Timeline/", "timeline"),
                    CatalogVariantCard("ImageGallery", "Grid gallery with lightbox mode and a custom click-handler mode.", "FreeBlazorExtended/ImageGallery/", "imagegallery"),
                },
            },
            new()
            {
                Title = "Tier 2B",
                BadgeText = "15 pages",
                Description = "Carousel, board, navigation, file-picker, and file-rendering families with composed demo routes.",
                Items = new List<FreeBlazorExampleShowcaseCardItem>
                {
                    CatalogVariantCard("Carousel", "Hero-slider pattern with autoplay, dots, filmstrip, and ref-driven controls.", "FreeBlazorExtended/Carousel/", "carousel"),
                    CatalogVariantCard("KanbanBoard", "Controlled drag-drop board with WIP limits, custom cards, and move logging.", "FreeBlazorExtended/KanbanBoard/", "kanbanboard"),
                    CatalogVariantCard("ExampleNav", "Breadcrumb plus Prev, All, and Next strip for demo-family navigation.", "FreeBlazorExtended/ExampleNav/", "examplenav"),
                    CatalogVariantCard("SelectFile", "Host-agnostic file picker, loading states, and composed attachment flow.", "FreeBlazorExtended/SelectFile/", "selectfile"),
                    CatalogVariantCard("RenderFiles", "Inline file tile renderer with click handlers, deletion, and mixed file types.", "FreeBlazorExtended/RenderFiles/", "renderfiles"),
                },
            },
        };
    }

    private static FreeBlazorExampleShowcaseCardItem CatalogVariantCard(string title, string summary, string codePath, string routeSegment)
    {
        return new FreeBlazorExampleShowcaseCardItem
        {
            Title = title,
            Summary = summary,
            Detail = "Open V1 for the default slice, V2 for the configured scenario, and V3 for the advanced interaction case.",
            CodePath = codePath,
            Links = new List<FreeBlazorExampleShowcaseCardLink>
            {
                new() { Label = "V1", Href = Helpers.BuildShowcaseUrl(routeSegment + "/v1") },
                new() { Label = "V2", Href = Helpers.BuildShowcaseUrl(routeSegment + "/v2") },
                new() { Label = "V3", Href = Helpers.BuildShowcaseUrl(routeSegment + "/v3") },
            },
        };
    }
}

public sealed class FreeBlazorExampleShowcaseSectionLink
{
    public string AnchorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public sealed class FreeBlazorExampleShowcaseCardGroup
{
    public string BadgeText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FreeBlazorExampleShowcaseCardItem> Items { get; set; } = new();
    public string Title { get; set; } = string.Empty;
}

public sealed class FreeBlazorExampleShowcaseCardItem
{
    public string BadgeText { get; set; } = string.Empty;
    public string CodePath { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public List<FreeBlazorExampleShowcaseCardLink> Links { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public sealed class FreeBlazorExampleShowcaseCardLink
{
    public string Href { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
