// Native VS 2026 Unified Settings for FreeCodeReorganizer. Each [VisualStudioContribution]
// Setting.* appears in Tools > Options / the 2026 settings search UI natively — NO "not migrated"
// banner (the whole reason for this rebuild). Each maps to a field of Core.ReorderConfig; the
// command reads them with Extensibility.Settings().ReadEffectiveValueAsync(...).
namespace FreeCodeReorganizer.VS2026;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;

#pragma warning disable VSEXTPREVIEW_SETTINGS // Settings API is in preview; opt in.

internal static class ReorganizerSettings
{
    /// <summary>The single category that hosts all of our settings.</summary>
    [VisualStudioContribution]
    internal static SettingCategory Category { get; } = new(
        "freeCodeReorganizer",
        "%FreeCodeReorganizer.Settings.Category.DisplayName%")
    {
        Description = "%FreeCodeReorganizer.Settings.Category.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean SortAlphabetically { get; } = new(
        "sortAlphabetically",
        "%FreeCodeReorganizer.Settings.SortAlphabetically.DisplayName%",
        Category,
        defaultValue: true)
    {
        Description = "%FreeCodeReorganizer.Settings.SortAlphabetically.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean IgnoreLeadingUnderscoreInSort { get; } = new(
        "ignoreLeadingUnderscoreInSort",
        "%FreeCodeReorganizer.Settings.IgnoreLeadingUnderscoreInSort.DisplayName%",
        Category,
        defaultValue: true)
    {
        Description = "%FreeCodeReorganizer.Settings.IgnoreLeadingUnderscoreInSort.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean GroupByVisibility { get; } = new(
        "groupByVisibility",
        "%FreeCodeReorganizer.Settings.GroupByVisibility.DisplayName%",
        Category,
        defaultValue: false)
    {
        Description = "%FreeCodeReorganizer.Settings.GroupByVisibility.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean StaticMembersFirst { get; } = new(
        "staticMembersFirst",
        "%FreeCodeReorganizer.Settings.StaticMembersFirst.DisplayName%",
        Category,
        defaultValue: false)
    {
        Description = "%FreeCodeReorganizer.Settings.StaticMembersFirst.Description%",
    };

    [VisualStudioContribution]
    internal static Setting.Boolean CollapseWrappedParameterBrace { get; } = new(
        "collapseWrappedParameterBrace",
        "%FreeCodeReorganizer.Settings.CollapseWrappedParameterBrace.DisplayName%",
        Category,
        defaultValue: true)
    {
        Description = "%FreeCodeReorganizer.Settings.CollapseWrappedParameterBrace.Description%",
    };

    /// <summary>
    /// Maps to Core.ReorderConfig.MaxFractionReordered. Exposed as a 0..100 percent (integer) because
    /// the settings UI is friendlier with whole numbers; the command divides by 100.
    /// </summary>
    [VisualStudioContribution]
    internal static Setting.Integer MaxPercentReordered { get; } = new(
        "maxPercentReordered",
        "%FreeCodeReorganizer.Settings.MaxPercentReordered.DisplayName%",
        Category,
        defaultValue: 35)
    {
        Description = "%FreeCodeReorganizer.Settings.MaxPercentReordered.Description%",
        Minimum = 0,
        Maximum = 100,
    };
}
