// "Reorganize Repository" — reorganizes every .cs and .razor file under the current solution/repo root,
// ON DEMAND ONLY. It never runs automatically (no save hook, no shortcut); it only does anything when the
// user explicitly invokes it from the Extensions menu AND confirms the prompt.
namespace FreeCodeReorganizer.VS2026;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Shell;
using Core = FreeCodeReorganizer.Core;

[VisualStudioContribution]
internal class ReorganizeRepositoryCommand : Command
{
    private readonly TraceSource logger;

    public ReorganizeRepositoryCommand(TraceSource traceSource)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%FreeCodeReorganizer.ReorganizeRepository.DisplayName%")
    {
        // No direct Placements: placed via the nested "Repository" submenu (ReorganizerMenus).
        Icon = new(ImageMoniker.KnownValues.OfficeWebExtension, IconSettings.IconAndText),
        // Deliberately NO keyboard shortcut: a repo-wide rewrite should be an explicit menu action,
        // never something a stray keystroke can trigger.
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Requires.NotNull(context, nameof(context));

        // Determine the root by walking up from the active document to the .sln (or .git) folder.
        string? root = null;
        using (ITextViewSnapshot? textView = await context.GetActiveTextViewAsync(cancellationToken))
        {
            string? path = textView?.Document.Uri?.LocalPath;
            if (path is not null)
            {
                root = RepoRoot.Find(path);
            }
        }

        if (root is null)
        {
            await this.Extensibility.Shell().ShowPromptAsync(
                "FreeCodeReorganizer: open a file in the solution you want to reorganize, then run this command again.",
                PromptOptions.OK,
                cancellationToken);
            return;
        }

        bool proceed = await this.Extensibility.Shell().ShowPromptAsync(
            $"Reorganize EVERY .cs and .razor file under:\n\n{root}\n\nThis rewrites files on disk (bin/obj and generated files are skipped; Razor markup is left untouched). Commit your work first. Continue?",
            PromptOptions.OKCancel,
            cancellationToken);
        if (!proceed)
        {
            return;
        }

        Core.ReorderConfig config = await ConfigReader.ReadAsync(this.Extensibility, cancellationToken);

        // Optional: run the .editorconfig cleanup (dotnet format) across the repo first, so the house
        // style is layered on top of editorconfig formatting.
        if (config.RunCleanupBeforeReorganize)
        {
            await Task.Run(() => Core.CleanupRunner.CleanDirectory(root, config.FullCleanup, config.ExcludeCleanupGlobs), cancellationToken);
        }

        // The reorganize is pure CPU + file I/O — run it off the calling thread.
        Core.BatchReorgResult result = await Task.Run(
            () => Core.BatchReorganizer.RunDirectory(root, config),
            cancellationToken);

        this.logger.TraceInformation($"Reorganize Repository: {result.Changed} of {result.Scanned} file(s) changed.");

        await this.Extensibility.Shell().ShowPromptAsync(
            $"FreeCodeReorganizer: reorganized {result.Changed} of {result.Scanned} file(s)" +
            (result.Failed > 0 ? $" ({result.Failed} skipped on error)." : ".") +
            "\n\nVisual Studio will reload any files that changed.",
            PromptOptions.OK,
            cancellationToken);
    }
}
