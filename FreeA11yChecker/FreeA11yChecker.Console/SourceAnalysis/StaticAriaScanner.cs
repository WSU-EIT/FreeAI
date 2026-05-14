using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static partial class StaticAriaScanner
{
    private static readonly string[] SkipDirs = { "bin", "obj", "node_modules", ".git", ".vs" };
    private const long MaxFileSize = 1_000_000;
    private const int SnippetMax = 200;

    private static readonly string[] FormTags =
    {
        "input", "select", "textarea", "button"
    };

    [GeneratedRegex(@"<button\b[^>]*\baria-hidden\s*=\s*(?:""true""|'true')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ButtonAriaHiddenRegex();

    [GeneratedRegex(@"<a\b(?=[^>]*\bhref\s*=)[^>]*\baria-hidden\s*=\s*(?:""true""|'true')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AnchorAriaHiddenRegex();

    [GeneratedRegex(@"<input\b[^>]*\baria-hidden\s*=\s*(?:""true""|'true')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputAriaHiddenRegex();

    [GeneratedRegex(@"\btype\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex TypeAttrRegex();

    [GeneratedRegex(@"<button\b[^>]*\brole\s*=\s*(?:""button""|'button')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ButtonRoleButtonRegex();

    [GeneratedRegex(@"<a\b[^>]*\brole\s*=\s*(?:""link""|'link')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AnchorRoleLinkRegex();

    [GeneratedRegex(@"<[a-zA-Z][^>]*\baria-label\s*=\s*(?:""\s*""|'\s*')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyAriaLabelRegex();

    [GeneratedRegex(@"<[a-zA-Z][a-zA-Z0-9]*\b[^>]*\baria-labelledby\s*=\s*(?:""([^""]+)""|'([^']+)')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AriaLabelledByRegex();

    [GeneratedRegex(@"\bid\s*=\s*(?:""([^""]+)""|'([^']+)')", RegexOptions.IgnoreCase)]
    private static partial Regex IdAttrRegex();

    [GeneratedRegex(@"<(button|a|input|select|textarea)\b[^>]*\brole\s*=\s*(?:""(?:presentation|none)""|'(?:presentation|none)')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex PresentationOnInteractiveRegex();

    [GeneratedRegex(@"<([a-zA-Z][a-zA-Z0-9]*)\b[^>]*\baria-required\s*=\s*(?:""true""|'true')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex AriaRequiredRegex();

    [GeneratedRegex(@"<span\b[^>]*\brole\s*=\s*(?:""img""|'img')[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex SpanRoleImgRegex();

    [GeneratedRegex(@"\baria-label\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex AriaLabelAttrRegex();

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

                ScanAriaHiddenFocusable(content, relative, findings);
                ScanRedundantRoles(content, relative, findings);
                ScanEmptyAriaLabel(content, relative, findings);
                ScanAriaLabelledBy(content, relative, findings);
                ScanPresentationOnInteractive(content, relative, findings);
                ScanAriaRequiredOnNonForm(content, relative, findings);
                ScanSpanRoleImg(content, relative, findings);
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

    private static void ScanAriaHiddenFocusable(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        const string issue = "aria-hidden on focusable element hides it from screen reader but keeps it in tab order";

        foreach (Match m in ButtonAriaHiddenRegex().Matches(content))
        {
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "serious", issue, Truncate(m.Value)));
        }

        foreach (Match m in AnchorAriaHiddenRegex().Matches(content))
        {
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "serious", issue, Truncate(m.Value)));
        }

        foreach (Match m in InputAriaHiddenRegex().Matches(content))
        {
            var tag = m.Value;
            var typeMatch = TypeAttrRegex().Match(tag);
            if (typeMatch.Success)
            {
                var type = (typeMatch.Groups[1].Success ? typeMatch.Groups[1].Value : typeMatch.Groups[2].Value).ToLowerInvariant();
                if (type == "hidden") continue;
            }
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "serious", issue, Truncate(tag)));
        }
    }

    private static void ScanRedundantRoles(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in ButtonRoleButtonRegex().Matches(content))
        {
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "minor", "redundant role=\"button\" on <button>", Truncate(m.Value)));
        }

        foreach (Match m in AnchorRoleLinkRegex().Matches(content))
        {
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "minor", "redundant role=\"link\" on <a>", Truncate(m.Value)));
        }
    }

    private static void ScanEmptyAriaLabel(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in EmptyAriaLabelRegex().Matches(content))
        {
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "moderate", "aria-label is empty", Truncate(m.Value)));
        }
    }

    private static void ScanAriaLabelledBy(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match idMatch in IdAttrRegex().Matches(content))
        {
            var idVal = idMatch.Groups[1].Success ? idMatch.Groups[1].Value : idMatch.Groups[2].Value;
            if (!string.IsNullOrWhiteSpace(idVal))
            {
                ids.Add(idVal.Trim());
            }
        }

        foreach (Match m in AriaLabelledByRegex().Matches(content))
        {
            var value = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            var tokens = value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (!ids.Contains(token))
                {
                    var line = LineOf(content, m.Index);
                    findings.Add((relative, line, "moderate", $"aria-labelledby references id \"{token}\" not found in same file", Truncate(m.Value)));
                }
            }
        }
    }

    private static void ScanPresentationOnInteractive(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in PresentationOnInteractiveRegex().Matches(content))
        {
            var tag = m.Value;
            if (tag.StartsWith("<input", StringComparison.OrdinalIgnoreCase))
            {
                var typeMatch = TypeAttrRegex().Match(tag);
                if (typeMatch.Success)
                {
                    var type = (typeMatch.Groups[1].Success ? typeMatch.Groups[1].Value : typeMatch.Groups[2].Value).ToLowerInvariant();
                    if (type == "hidden") continue;
                }
            }
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "serious", "role=\"presentation\" or role=\"none\" on focusable interactive element", Truncate(tag)));
        }
    }

    private static void ScanAriaRequiredOnNonForm(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in AriaRequiredRegex().Matches(content))
        {
            var tagName = m.Groups[1].Value.ToLowerInvariant();
            if (Array.Exists(FormTags, t => t == tagName)) continue;
            var line = LineOf(content, m.Index);
            findings.Add((relative, line, "minor", $"aria-required=\"true\" on non-form element <{tagName}>", Truncate(m.Value)));
        }
    }

    private static void ScanSpanRoleImg(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in SpanRoleImgRegex().Matches(content))
        {
            var tag = m.Value;
            if (!AriaLabelAttrRegex().IsMatch(tag))
            {
                var line = LineOf(content, m.Index);
                findings.Add((relative, line, "serious", "<span role=\"img\"> missing aria-label", Truncate(tag)));
            }
        }
    }
}
