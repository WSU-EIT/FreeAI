// "Clean Up Repository" — runs the .editorconfig cleanup (dotnet format) across the whole solution/repo,
// ON DEMAND ONLY (explicit menu action + confirmation). Whitespace or full per the FullCleanup setting.
// Files are rewritten on disk and Visual Studio reloads them. Tucked in the nested "Repository" submenu.
namespace FreeCodeReorganizer.VS2026;

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.Shell;
using Core = FreeCodeReorganizer.Core;

[VisualStudioContribution]
internal class CleanUpRepositoryCommand : Command
{
    private readonly TraceSource logger;

    public CleanUpRepositoryCommand(TraceSource traceSource)
    {
        this.logger = Requires.NotNull(traceSource, nameof(traceSource));
    }

    /// <inheritdoc/>
    public override CommandConfiguration CommandConfiguration => new("%FreeCodeReorganizer.CleanUpRepository.DisplayName%")
    {
        Icon = new(ImageMoniker.KnownValues.OfficeWebExtension, IconSettings.IconAndText),
    };

    /// <inheritdoc/>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        Requires.NotNull(context, nameof(context));

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
                "FreeCodeReorganizer: open a file in the solution you want to clean up, then run this command again.",
                PromptOptions.OK,
                cancellationToken);
            return;
        }

        Core.ReorderConfig config = await ConfigReader.ReadAsync(this.Extensibility, cancellationToken);
        string level = config.FullCleanup ? "FULL (usings + code style + analyzers; restores/builds each project)" : "whitespace";

        bool proceed = await this.Extensibility.Shell().ShowPromptAsync(
            $"Run .editorconfig cleanup ({level}) on every project under:\n\n{root}\n\nThis rewrites files on disk via 'dotnet format'. Commit your work first. Continue?",
            PromptOptions.OKCancel,
            cancellationToken);
        if (!proceed)
        {
            return;
        }

        Core.CleanupResult result = await Task.Run(
            () => Core.CleanupRunner.CleanDirectory(root, config.FullCleanup, config.ExcludeCleanupGlobs),
            cancellationToken);

        // Apply the house-style formatting across the repo too (formatting, not a member reorder).
        Core.ReorderConfig formatOnly = config.Clone();
        formatOnly.ReorderMembers = false;
        await Task.Run(() => Core.BatchReorganizer.RunDirectory(root, formatOnly), cancellationToken);

        this.logger.TraceInformation($"Clean Up Repository: ran={result.Ran} exit={result.ExitCode}");

        string message = result.Error is not null
            ? "FreeCodeReorganizer cleanup could not complete:\n\n" + result.Error
            : "FreeCodeReorganizer: .editorconfig cleanup complete.\n\nVisual Studio will reload any files that changed.";
        await this.Extensibility.Shell().ShowPromptAsync(message, PromptOptions.OK, cancellationToken);
    }
}
