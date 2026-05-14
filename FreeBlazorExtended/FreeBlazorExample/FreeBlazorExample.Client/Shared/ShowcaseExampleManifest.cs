using FreeBlazorExtended.ExampleNav;

namespace FreeBlazorExample.Client.Shared;

public static class ShowcaseExampleManifest
{
    public static string FirstExampleRoute => BuildPages().First().Route;

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
}