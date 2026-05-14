using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static partial class StaticImageFormScanner
{
    private static readonly string[] SkipDirs = { "bin", "obj", "node_modules", ".git", ".vs" };
    private const long MaxFileSize = 1_000_000;
    private const int SnippetMax = 200;

    private static readonly string[] DecorativePatterns =
    {
        "spacer.gif", "spacer.png", "divider.png", "divider.gif",
        "pixel.gif", "pixel.png", "blank.gif", "blank.png",
        "transparent.gif", "transparent.png", "dot.gif", "dot.png"
    };

    [GeneratedRegex(@"<img\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"\balt\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex AltAttrRegex();

    [GeneratedRegex(@"\balt\s*=\s*(?:""\s*""|'\s*')", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyAltRegex();

    [GeneratedRegex(@"\bsrc\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex SrcAttrRegex();

    [GeneratedRegex(@"<input\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputTagRegex();

    [GeneratedRegex(@"\btype\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex TypeAttrRegex();

    [GeneratedRegex(@"\bid\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex IdAttrRegex();

    [GeneratedRegex(@"\baria-label\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex AriaLabelAttrRegex();

    [GeneratedRegex(@"\bplaceholder\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex PlaceholderAttrRegex();

    [GeneratedRegex(@"<button\b[^>]*>([\s\S]*?)</button>", RegexOptions.IgnoreCase)]
    private static partial Regex ButtonTagRegex();

    [GeneratedRegex(@"<a\b[^>]*\bhref\s*=[^>]*>([\s\S]*?)</a>", RegexOptions.IgnoreCase)]
    private static partial Regex AnchorTagRegex();

    public static List<(string File, int Line, string Severity, string Issue, string Snippet)> Analyze(string sourceRoot)
    {
        var findings = new List<(string, int, string, string, string)>();
        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            return findings;
        }

        var files = EnumerateFiles(sourceRoot);
        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length > MaxFileSize) continue;

                var content = File.ReadAllText(file);
                var relative = Path.GetRelativePath(sourceRoot, file);

                ScanImages(content, relative, findings);
                ScanInputs(content, relative, findings);
                ScanButtons(content, relative, findings);
                ScanAnchors(content, relative, findings);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return findings;
    }

    private static IEnumerable<string> EnumerateFiles(string root)
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

    private static int LineOf(string content, int index)
    {
        int line = 1;
        for (int i = 0; i < index && i < content.Length; i++)
        {
            if (content[i] == '\n') line++;
        }
        return line;
    }

    private static string Truncate(string s)
    {
        if (s.Length <= SnippetMax) return s;
        return s.Substring(0, SnippetMax);
    }

    private static void ScanImages(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in ImgTagRegex().Matches(content))
        {
            var tag = m.Value;
            var line = LineOf(content, m.Index);
            if (!AltAttrRegex().IsMatch(tag))
            {
                findings.Add((relative, line, "serious", "<img> missing alt attribute", Truncate(tag)));
            }
            else if (EmptyAltRegex().IsMatch(tag))
            {
                var srcMatch = SrcAttrRegex().Match(tag);
                if (srcMatch.Success)
                {
                    var src = srcMatch.Groups[1].Success ? srcMatch.Groups[1].Value : srcMatch.Groups[2].Value;
                    if (!IsDecorativeSrc(src))
                    {
                        findings.Add((relative, line, "moderate", "<img> has empty alt but src looks meaningful", Truncate(tag)));
                    }
                }
                else
                {
                    findings.Add((relative, line, "moderate", "<img> has empty alt but src looks meaningful", Truncate(tag)));
                }
            }
        }
    }

    private static bool IsDecorativeSrc(string src)
    {
        if (string.IsNullOrEmpty(src)) return true;
        var lower = src.ToLowerInvariant();
        foreach (var p in DecorativePatterns)
        {
            if (lower.Contains(p)) return true;
        }
        return false;
    }

    private static void ScanInputs(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in InputTagRegex().Matches(content))
        {
            var tag = m.Value;
            var line = LineOf(content, m.Index);
            var typeMatch = TypeAttrRegex().Match(tag);
            if (!typeMatch.Success) continue;
            var type = (typeMatch.Groups[1].Success ? typeMatch.Groups[1].Value : typeMatch.Groups[2].Value).ToLowerInvariant();

            if (type is "text" or "email" or "password" or "tel" or "number" or "date" or "search" or "url")
            {
                if (!IdAttrRegex().IsMatch(tag) && !AriaLabelAttrRegex().IsMatch(tag) && !PlaceholderAttrRegex().IsMatch(tag))
                {
                    findings.Add((relative, line, "serious", $"<input type=\"{type}\"> missing id, aria-label, and placeholder", Truncate(tag)));
                }
            }
            else if (type is "submit" or "button")
            {
                if (!AriaLabelAttrRegex().IsMatch(tag))
                {
                    var valueMatch = Regex.Match(tag, @"\bvalue\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase);
                    var val = valueMatch.Success
                        ? (valueMatch.Groups[1].Success ? valueMatch.Groups[1].Value : valueMatch.Groups[2].Value)
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        findings.Add((relative, line, "serious", $"<input type=\"{type}\"> has no text and no aria-label", Truncate(tag)));
                    }
                }
            }
        }
    }

    private static void ScanButtons(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in ButtonTagRegex().Matches(content))
        {
            var full = m.Value;
            var line = LineOf(content, m.Index);
            var openTagEnd = full.IndexOf('>');
            if (openTagEnd < 0) continue;
            var openTag = full.Substring(0, openTagEnd + 1);
            var inner = m.Groups[1].Value;
            var stripped = Regex.Replace(inner, @"<[^>]+>", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(stripped) && !AriaLabelAttrRegex().IsMatch(openTag))
            {
                findings.Add((relative, line, "serious", "<button> has no inner text and no aria-label", Truncate(full)));
            }
        }
    }

    private static void ScanAnchors(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in AnchorTagRegex().Matches(content))
        {
            var full = m.Value;
            var line = LineOf(content, m.Index);
            var openTagEnd = full.IndexOf('>');
            if (openTagEnd < 0) continue;
            var openTag = full.Substring(0, openTagEnd + 1);
            var inner = m.Groups[1].Value;
            var stripped = Regex.Replace(inner, @"<[^>]+>", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(stripped) && !AriaLabelAttrRegex().IsMatch(openTag))
            {
                findings.Add((relative, line, "serious", "<a href> has empty inner text and no aria-label", Truncate(full)));
            }
        }
    }
}
