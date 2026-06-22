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
        // No direct Placements: this command is placed via the FreeCodeReorganizer menu (ReorganizerMenus).
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
        string path = document.Uri?.LocalPath ?? string.Empty;
        bool isRazor = path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
        bool isCs = path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        if (!isCs && !isRazor)
        {
            this.logger.TraceInformation("Reorganize: active document is not a .cs / .razor / .cshtml file.");
            return;
        }

        string text = document.Text.CopyToString();

        Core.ReorderConfig config = await this.BuildConfigAsync(cancellationToken);

        // Leave .editorconfig-declared generated code completely alone.
        if (config.RespectGeneratedCode && Core.GeneratedCodeDetector.IsFileGenerated(path))
        {
            this.logger.TraceInformation("Reorganize: file marked generated_code in .editorconfig — left alone.");
            return;
        }

        // Per-path exclusions: an "exclude from reorganize" file keeps its member order (e.g. EF models);
        // an "exclude from cleanup" file gets no house-style formatting.
        Core.ReorderConfig effective = Core.BatchReorganizer.EffectiveConfigFor(path, config);

        // Optional: run the .editorconfig cleanup (dotnet format, whitespace) first so editorconfig
        // formatting is applied and the house style is layered on top. The real file is untouched —
        // DocumentCleanup formats a throwaway sibling copy and returns the cleaned text.
        if (config.RunCleanupBeforeReorganize && !Core.PathExclusion.IsExcluded(path, config.ExcludeCleanupGlobs))
        {
            text = await DocumentCleanup.CleanAsync(text, path, cancellationToken);
        }

        string eol = text.Contains("\r\n") ? "\r\n" : "\n";

        // Razor files: only the @code blocks are reorganized; all markup stays byte-identical.
        string? error;
        bool changed;
        string? newText;
        if (isRazor)
        {
            Core.RazorReorgResult r = Core.RazorReorganizer.Run(text, effective, eol);
            error = r.Error;
            changed = r.Changed;
            newText = r.NewText;
        }
        else
        {
            Core.ReorgResult r = Core.Reorganizer.Run(text, effective, eol);
            error = r.Error;
            changed = r.Changed;
            newText = r.NewText;
        }

        if (error is not null)
        {
            this.logger.TraceEvent(TraceEventType.Warning, 0, "Reorganize failed: " + error);
            return;
        }

        if (!changed || newText is null)
        {
            this.logger.TraceInformation("Reorganize: already organized — no changes.");
            return;
        }

        // Replace the whole document in one edit. document.Text is the full-document range.
        await this.Extensibility.Editor().EditAsync(
            batch => textView.Document.AsEditable(batch).Replace(textView.Document.Text, newText!),
            cancellationToken);

        this.logger.TraceInformation("Reorganize: document reorganized.");
    }

    /// <summary>Reads the native settings into a Core.ReorderConfig (shared with the repository command).</summary>
    private Task<Core.ReorderConfig> BuildConfigAsync(CancellationToken cancellationToken)
        => ConfigReader.ReadAsync(this.Extensibility, cancellationToken);
}
