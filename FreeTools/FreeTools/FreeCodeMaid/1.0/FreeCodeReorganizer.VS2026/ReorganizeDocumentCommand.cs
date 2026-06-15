// The "Reorganize Document" command (VisualStudio.Extensibility async model). Placed on the
// Extensions menu. Reads the active .cs document, runs the shared Core engine (member reordering +
// the "){" brace), and replaces the buffer in one edit. Settings come from the native Setting.*
// contributions via one-off reads.
namespace FreeCodeReorganizer.VS2026;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Settings;
using Core = FreeCodeReorganizer.Core;

#pragma warning disable VSEXTPREVIEW_SETTINGS // Reading the preview Settings API.

/// <summary>
/// Reorganizes the members of the active C# document. Unlike the classic front-end this does NOT run
/// "Format Document" first: the new out-of-process model intentionally can't invoke VSCT commands like
/// Edit.FormatDocument, so editorconfig formatting is left to VS itself (format-on-save / Ctrl+K,Ctrl+D).
/// This command layers only what .editorconfig can't express — member ordering + the "){" brace.
/// </summary>
[VisualStudioContribution]
internal class ReorganizeDocumentCommand : Command
{
    private readonly TraceSource logger;

    public ReorganizeDocumentCommand(TraceSource traceSource)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%FreeCodeReorganizer.ReorganizeDocument.DisplayName%")
    {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
        Icon = new(ImageMoniker.KnownValues.OfficeWebExtension, IconSettings.IconAndText),
        // Default chord: Ctrl+K, Ctrl+R. Pairs with VS's built-in Ctrl+K, Ctrl+D (Format Document)
        // for the "format-then-reorganize" two-keystroke flow.
        Shortcuts = [new CommandShortcutConfiguration(ModifierKey.Control, Key.K, ModifierKey.Control, Key.R)],
        // Only enabled/visible when there's an active editor.
        VisibleWhen = ActivationConstraint.ClientContext(ClientContextKey.Shell.ActiveEditorContentType, ".+"),
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Requires.NotNull(context, nameof(context));

        using ITextViewSnapshot? textView = await context.GetActiveTextViewAsync(cancellationToken);
        if (textView is null)
        {
            this.logger.TraceInformation("Reorganize: no active text view.");
            return;
        }

        ITextDocumentSnapshot document = textView.Document;
        if (document.Uri is null ||
            !document.Uri.LocalPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            this.logger.TraceInformation("Reorganize: active document is not a .cs file.");
            return;
        }

        string text = document.Text.CopyToString();
        string eol = text.Contains("\r\n") ? "\r\n" : "\n";

        Core.ReorderConfig config = await this.BuildConfigAsync(cancellationToken);

        Core.ReorgResult result = Core.Reorganizer.Run(text, config, eol);
        if (result.Error is not null)
        {
            this.logger.TraceEvent(TraceEventType.Warning, 0, "Reorganize failed: " + result.Error);
            return;
        }

        if (!result.Changed || result.NewText is null)
        {
            this.logger.TraceInformation("Reorganize: already organized — no changes.");
            return;
        }

        // Replace the whole document in one edit. document.Text is the full-document range.
        await this.Extensibility.Editor().EditAsync(
            batch => textView.Document.AsEditable(batch).Replace(textView.Document.Text, result.NewText!),
            cancellationToken);

        this.logger.TraceInformation(
            $"Reorganize: {result.TypesReordered} type(s) reordered, {result.BracesCollapsed} brace(s) collapsed.");
    }

    /// <summary>Reads the native settings into a Core.ReorderConfig (engine defaults for everything else).</summary>
    private async Task<Core.ReorderConfig> BuildConfigAsync(CancellationToken cancellationToken)
    {
        SettingsExtensibility settings = this.Extensibility.Settings();

        bool sortAlphabetically = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.SortAlphabetically, cancellationToken)).ValueOrDefault(true);
        bool ignoreUnderscore = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.IgnoreLeadingUnderscoreInSort, cancellationToken)).ValueOrDefault(true);
        bool groupByVisibility = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.GroupByVisibility, cancellationToken)).ValueOrDefault(false);
        bool staticMembersFirst = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.StaticMembersFirst, cancellationToken)).ValueOrDefault(false);
        bool collapseBrace = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.CollapseWrappedParameterBrace, cancellationToken)).ValueOrDefault(true);
        int maxPercent = (await settings.ReadEffectiveValueAsync(ReorganizerSettings.MaxPercentReordered, cancellationToken)).ValueOrDefault(35);

        return new Core.ReorderConfig
        {
            SortAlphabetically = sortAlphabetically,
            IgnoreLeadingUnderscoreInSort = ignoreUnderscore,
            GroupByVisibility = groupByVisibility,
            StaticMembersFirst = staticMembersFirst,
            CollapseWrappedParameterBrace = collapseBrace,
            MaxFractionReordered = maxPercent / 100.0,
        };
    }
}
