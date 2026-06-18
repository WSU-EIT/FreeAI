// Document-level .editorconfig cleanup for the new out-of-process model. dotnet format needs a file on
// disk for .editorconfig to resolve, but we must NOT disturb the user's open buffer or the real file
// mid-edit. So we write the current buffer text to a throwaway SIBLING file (same folder + extension, so
// the same .editorconfig rules apply), run the whitespace cleanup on it, read it back, and delete it.
// The real file is never written; the caller replaces the editor buffer with the returned text. Full
// cleanup (usings/style/analyzers) is a project/solution operation, so the document level is whitespace.
namespace FreeCodeReorganizer.VS2026;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core = FreeCodeReorganizer.Core;

internal static class DocumentCleanup
{
    /// <summary>Returns the .editorconfig-cleaned text, or the original text if nothing changed/failed.</summary>
    public static async Task<string> CleanAsync(string text, string realPath, CancellationToken cancellationToken)
    {
        string? dir = string.IsNullOrEmpty(realPath) ? null : Path.GetDirectoryName(realPath);
        if (dir is null || !Directory.Exists(dir))
        {
            return text;
        }

        string ext = Path.GetExtension(realPath);
        string tempPath = Path.Combine(dir, "__fcrclean__" + Guid.NewGuid().ToString("N") + ext);
        try
        {
            File.WriteAllText(tempPath, text, new UTF8Encoding(false));

            Core.CleanupResult result = await Task.Run(
                () => Core.CleanupRunner.CleanFile(tempPath, full: false),
                cancellationToken);
            if (!result.Ran)
            {
                return text;
            }

            return File.ReadAllText(tempPath, new UTF8Encoding(false));
        }
        catch
        {
            return text;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Best effort; a stray temp file is harmless.
            }
        }
    }
}
