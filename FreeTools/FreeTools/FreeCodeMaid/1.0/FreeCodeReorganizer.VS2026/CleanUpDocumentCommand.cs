// "Clean Up Document" — applies the project's .editorconfig formatting (whitespace) to the active
// document via dotnet format, WITHOUT reordering members. The real file is left untouched (a throwaway
// sibling file is formatted); the editor buffer is replaced with the result. Placed on the
// FreeCodeReorganizer menu next to Reorganize Document.
namespace FreeCodeReorganizer.VS2026;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Core = FreeCodeReorganizer.Core;

[VisualStudioContribution]
internal class CleanUpDocumentCommand : Command
{
    private readonly TraceSource logger;

    public CleanUpDocumentCommand(TraceSource traceSource)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%FreeCodeReorganizer.CleanUpDocument.DisplayName%")
    {
        Icon = new(ImageMoniker.KnownValues.OfficeWebExtension, IconSettings.IconAndText),
        VisibleWhen = ActivationConstraint.ClientContext(ClientContextKey.Shell.ActiveEditorContentType, ".+"),
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Requires.NotNull(context, nameof(context));

        using ITextViewSnapshot? textView = await context.GetActiveTextViewAsync(cancellationToken);
        if (textView is null)
        {
            return;
        }

        ITextDocumentSnapshot document = textView.Document;
        string path = document.Uri?.LocalPath ?? string.Empty;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Core.ReorderConfig config = await ConfigReader.ReadAsync(this.Extensibility, cancellationToken);
        if (Core.PathExclusion.IsExcluded(path, config.ExcludeCleanupGlobs))
        {
            this.logger.TraceInformation("Clean Up: file excluded from cleanup.");
            return;
        }

        string text = document.Text.CopyToString();

        // Step 1: .editorconfig whitespace cleanup (dotnet format on a throwaway sibling copy).
        string cleaned = await DocumentCleanup.CleanAsync(text, path, cancellationToken);

        // Step 2: house-style formatting ("){", double-tab Razor) — formatting, NOT a reorder.
        Core.ReorderConfig formatOnly = config.Clone();
        formatOnly.ReorderMembers = false;
        string eol = cleaned.Contains("\r\n") ? "\r\n" : "\n";
        bool isRazor = path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                       path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
        if (isRazor)
        {
            Core.RazorReorgResult r = Core.RazorReorganizer.Run(cleaned, formatOnly, eol);
            if (r.Error is null && r.Changed && r.NewText is not null)
            {
                cleaned = r.NewText;
            }
        }
        else if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            Core.ReorgResult r = Core.Reorganizer.Run(cleaned, formatOnly, eol);
            if (r.Error is null && r.Changed && r.NewText is not null)
            {
                cleaned = r.NewText;
            }
        }

        if (string.Equals(cleaned, text, StringComparison.Ordinal))
        {
            this.logger.TraceInformation("Clean Up: no changes.");
            return;
        }

        await this.Extensibility.Editor().EditAsync(
            batch => textView.Document.AsEditable(batch).Replace(textView.Document.Text, cleaned),
            cancellationToken);

        this.logger.TraceInformation("Clean Up: document cleaned.");
    }
}
