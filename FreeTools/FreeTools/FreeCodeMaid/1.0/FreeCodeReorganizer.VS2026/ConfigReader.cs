// Shared: reads the native VS settings into a Core.ReorderConfig. Used by both the document command
// and the repository command so they always reorganize with identical rules.
namespace FreeCodeReorganizer.VS2026;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Settings;
using Core = FreeCodeReorganizer.Core;

#pragma warning disable VSEXTPREVIEW_SETTINGS // Reading the preview Settings API.

internal static class ConfigReader
{
    public static async Task<Core.ReorderConfig> ReadAsync(VisualStudioExtensibility extensibility, CancellationToken ct)
    {
        SettingsExtensibility settings = extensibility.Settings();

        bool sortAlphabetically = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.SortAlphabetically, ct)).ValueOrDefault(true);
        bool ignoreUnderscore = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.IgnoreLeadingUnderscoreInSort, ct)).ValueOrDefault(true);
        bool groupByVisibility = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.GroupByVisibility, ct)).ValueOrDefault(false);
        bool staticMembersFirst = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.StaticMembersFirst, ct)).ValueOrDefault(false);
        bool collapseBrace = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.CollapseWrappedParameterBrace, ct)).ValueOrDefault(true);
        bool razorAttrs = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.IndentWrappedRazorAttributes, ct)).ValueOrDefault(true);
        bool respectRegions = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.RespectRegions, ct)).ValueOrDefault(true);
        int maxPercent = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.MaxPercentReordered, ct)).ValueOrDefault(35);
        int maxLineWidth = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.MaxLineWidth, ct)).ValueOrDefault(120);
        bool cleanupFirst = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.RunCleanupBeforeReorganize, ct)).ValueOrDefault(false);
        bool fullCleanup = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.FullCleanup, ct)).ValueOrDefault(false);
        string excludeReorganize = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.ExcludeReorganize, ct)).ValueOrDefault(string.Empty);
        string excludeCleanup = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.ExcludeCleanup, ct)).ValueOrDefault(string.Empty);

        return new Core.ReorderConfig
        {
            SortAlphabetically = sortAlphabetically,
            IgnoreLeadingUnderscoreInSort = ignoreUnderscore,
            GroupByVisibility = groupByVisibility,
            StaticMembersFirst = staticMembersFirst,
            CollapseWrappedParameterBrace = collapseBrace,
            IndentWrappedRazorAttributes = razorAttrs,
            RespectRegions = respectRegions,
            MaxFractionReordered = maxPercent / 100.0,
            MaxLineWidth = maxLineWidth,
            RunCleanupBeforeReorganize = cleanupFirst,
            FullCleanup = fullCleanup,
            ExcludeReorganizeGlobs = Split(excludeReorganize),
            ExcludeCleanupGlobs = Split(excludeCleanup),
        };
    }

    // Split a semicolon- or newline-separated setting string into a trimmed, non-empty list.
    private static List<string> Split(string value)
        => string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => s.Length > 0)
                   .ToList();
}
