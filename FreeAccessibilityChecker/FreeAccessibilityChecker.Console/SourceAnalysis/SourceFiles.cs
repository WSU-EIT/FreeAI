using System.Text.RegularExpressions;

namespace FreeAccessibilityChecker.Console.SourceAnalysis;

/// <summary>
/// Helpers the static scanners share: walking a source tree for .razor and .html files, turning a character
/// offset into a line number, and the attribute patterns more than one scanner reads.
/// </summary>
internal static partial class SourceFiles
{
    private static readonly string[] SkipDirs = { "bin", "obj", "node_modules", ".git", ".vs" };

    [GeneratedRegex(@"\baria-label\s*=", RegexOptions.IgnoreCase)]
    public static partial Regex AriaLabelAttrRegex();

    [GeneratedRegex(@"\btype\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    public static partial Regex TypeAttrRegex();

    /// <summary>Every .razor and .html file under root, skipping build output and package folders.</summary>
    public static IEnumerable<string> EnumerateMarkupFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] subs;
            try { subs = Directory.GetDirectories(dir); }
            catch { continue; }

            foreach (var sub in subs)
            {
                var name = Path.GetFileName(sub);
                if (Array.Exists(SkipDirs, d => string.Equals(d, name, StringComparison.OrdinalIgnoreCase)))
                    continue;
                stack.Push(sub);
            }

            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch { continue; }

            foreach (var f in files)
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".razor", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase))
                {
                    yield return f;
                }
            }
        }
    }

    /// <summary>The 1-based line number of the character at index.</summary>
    public static int LineOf(string content, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < content.Length; i++)
        {
            if (content[i] == '\n') line++;
        }
        return line;
    }
}
