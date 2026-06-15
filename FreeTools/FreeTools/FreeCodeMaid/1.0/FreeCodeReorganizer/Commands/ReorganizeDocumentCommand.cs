using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using FreeCodeReorganizer.Options;
using Core = FreeCodeReorganizer.Core;

namespace FreeCodeReorganizer.Commands
{
    /// <summary>
    /// The "Reorganize Document" command. Bound to the .vsct button via
    /// [Command(PackageIds.ReorganizeDocument)] — the toolkit's
    /// <see cref="BaseCommand{T}"/> handles registration; we only implement
    /// <see cref="ExecuteAsync"/>.
    ///
    /// Flow:
    ///   1. get the active document view + its text buffer;
    ///   2. bail out if there's no document or it isn't a .cs file;
    ///   3. read the whole buffer text and detect its newline style;
    ///   4. build a ReorderConfig from the Tools > Options page;
    ///   5. run the Roslyn engine OFF the UI thread (it's pure CPU work);
    ///   6. back ON the UI thread, if it changed, replace the buffer in one edit;
    ///   7. surface any engine error as a warning message box.
    ///
    /// VERIFY IN VS: the toolkit calls used here — VS.Documents.GetActiveDocumentViewAsync(),
    /// docView.TextBuffer, VS.MessageBox.ShowWarningAsync — are the documented Community toolkit
    /// API, but the exact member names/shapes could not be compiled in the scaffold environment.
    /// </summary>
    [Command(PackageGuids.FreeCodeReorganizerCmdSet, PackageIds.ReorganizeDocument)]
    internal sealed class ReorganizeDocumentCommand : BaseCommand<ReorganizeDocumentCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            // We start on the UI thread (commands are invoked there). Getting the active document
            // view is a UI-thread operation.
            DocumentView? docView = await VS.Documents.GetActiveDocumentViewAsync();

            // 2. Guard: no active document, or not a C# file.
            if (docView?.TextBuffer is null) {
                await VS.MessageBox.ShowWarningAsync(
                    "FreeCodeReorganizer",
                    "No active document to reorganize.");
                return;
            }

            // The file path comes from the document; only operate on .cs files.
            string? filePath = docView.Document?.FilePath ?? docView.FilePath;
            if (string.IsNullOrEmpty(filePath) ||
                !filePath!.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) {
                await VS.MessageBox.ShowWarningAsync(
                    "FreeCodeReorganizer",
                    "FreeCodeReorganizer only works on C# (.cs) files.");
                return;
            }

            // 3. Load the saved options.
            GeneralOptions options = await GeneralOptions.GetLiveInstanceAsync();

            // 3a. EDITORCONFIG FIRST. Run the editor's own "Format Document" — which respects the
            //     repo's .editorconfig — before we touch anything. Standard formatting is OWNED by
            //     .editorconfig (and is locally overridable by whoever owns the repo); FreeCodeReorganizer
            //     then layers ONLY the gaps .editorconfig can't express: member ordering + the "){"
            //     brace. This mirrors the CLI's "dotnet format whitespace, then reorganize" pipeline.
            //     VERIFY IN VS: "Edit.FormatDocument" is the built-in editorconfig-aware format command.
            if (options.RunEditorConfigFormatFirst) {
                try { await VS.Commands.ExecuteAsync("Edit.FormatDocument"); }
                catch { /* formatting is best-effort; reorganize either way */ }
            }

            ITextBuffer buffer = docView.TextBuffer;

            // 4. Read the full text from the (now editorconfig-formatted) snapshot and detect EOL.
            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            string text = snapshot.GetText();
            string eol = DetectEol(text);
            Core.ReorderConfig config = options.ToReorderConfig();

            // 5. Run the Roslyn engine off the UI thread — parsing/rewriting is pure CPU work and
            //    can be a few hundred ms on a big file; we don't want to freeze the editor.
            Core.ReorgResult result = await Task.Run(() => Core.Reorganizer.Run(text, config, eol));

            // 6. Back on the UI thread for any buffer edit / message box.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // 6a. Engine reported an error -> warn and stop (buffer untouched).
            if (result.Error is not null) {
                await VS.MessageBox.ShowWarningAsync(
                    "FreeCodeReorganizer",
                    "Could not reorganize this document: " + result.Error);
                return;
            }

            // 6b. Nothing to do (already in order / no NewText).
            if (!result.Changed || result.NewText is null) {
                await VS.StatusBar.ShowMessageAsync("FreeCodeReorganizer: already organized — no changes.");
                return;
            }

            // 6c. Replace the WHOLE buffer in a single edit. Re-read the snapshot in case it
            //     changed while we were on the background thread, and replace its full span.
            ITextSnapshot current = buffer.CurrentSnapshot;
            buffer.Replace(new Span(0, current.Length), result.NewText);

            await VS.StatusBar.ShowMessageAsync(
                $"FreeCodeReorganizer: reorganized {result.TypesReordered} type(s), collapsed {result.BracesCollapsed} brace(s).");
        }

        /// <summary>
        /// Detects the dominant newline style of the source so the engine emits matching line
        /// endings. Defaults to the document's first newline; falls back to the OS newline.
        /// </summary>
        private static string DetectEol(string text)
        {
            int idx = text.IndexOf('\n');
            if (idx > 0 && text[idx - 1] == '\r') {
                return "\r\n";
            }
            if (idx >= 0) {
                return "\n";
            }
            return Environment.NewLine;
        }
    }
}