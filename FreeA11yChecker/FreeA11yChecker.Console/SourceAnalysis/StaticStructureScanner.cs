using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static partial class StaticStructureScanner
{
    private static readonly string[] SkipDirs = { "bin", "obj", "node_modules", ".git", ".vs" };
    private const long MaxFileSize = 1_000_000;
    private const int SnippetMax = 200;

    [GeneratedRegex(@"<main\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex MainTagRegex();

    [GeneratedRegex(@"<html\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\blang\s*=", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex LangAttrRegex();

    [GeneratedRegex(@"<h([1-6])\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<button\b[^>]*\btype\s*=\s*(?:""submit""|'submit')[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex SubmitButtonRegex();

    [GeneratedRegex(@"<form\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex FormTagRegex();

    [GeneratedRegex(@"</form\s*>", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex FormCloseTagRegex();

    [GeneratedRegex(@"<table\b[^>]*>([\s\S]*?)</table\s*>", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex TableRegex();

    [GeneratedRegex(@"<caption\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex CaptionTagRegex();

    [GeneratedRegex(@"<th\b[^>]*\bscope\s*=", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex ThScopeRegex();

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

                ScanMainLandmark(content, relative, findings);
                ScanHtmlLang(content, relative, findings);
                ScanHeadings(content, relative, findings);
                ScanSubmitButtons(content, relative, findings);
                ScanTables(content, relative, findings);
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

    private static bool LooksLikeTopLevelPage(string content)
    {
        // Heuristic: a page if it has an <html> tag, a <body> tag, or a Razor @page directive.
        if (Regex.IsMatch(content, @"<html\b", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(content, @"<body\b", RegexOptions.IgnoreCase)) return true;
        if (Regex.IsMatch(content, @"^\s*@page\b", RegexOptions.IgnoreCase | RegexOptions.Multiline)) return true;
        return false;
    }

    private static void ScanMainLandmark(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        if (!LooksLikeTopLevelPage(content)) return;
        if (MainTagRegex().IsMatch(content)) return;

        var snippet = content.Length > SnippetMax ? content.Substring(0, SnippetMax) : content;
        findings.Add((relative, 1, "serious", "Missing <main> landmark", Truncate(snippet)));
    }

    private static void ScanHtmlLang(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in HtmlTagRegex().Matches(content))
        {
            var tag = m.Value;
            if (!LangAttrRegex().IsMatch(tag))
            {
                var line = LineOf(content, m.Index);
                findings.Add((relative, line, "serious", "<html> tag missing lang attribute", Truncate(tag)));
            }
        }
    }

    private static void ScanHeadings(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        int previousLevel = 0;
        int h1Count = 0;
        foreach (Match m in HeadingRegex().Matches(content))
        {
            if (!int.TryParse(m.Groups[1].Value, out var level)) continue;
            var line = LineOf(content, m.Index);

            if (level == 1)
            {
                h1Count++;
                if (h1Count == 2)
                {
                    findings.Add((relative, line, "moderate", "Multiple <h1> in same file", Truncate(m.Value)));
                }
            }

            if (previousLevel > 0 && level > previousLevel + 1)
            {
                findings.Add((relative, line, "moderate",
                    $"Skipped heading level (<h{previousLevel}> followed by <h{level}>)", Truncate(m.Value)));
            }

            previousLevel = level;
        }
    }

    private static void ScanSubmitButtons(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        // Collect form spans.
        var formSpans = new List<(int Start, int End)>();
        var opens = FormTagRegex().Matches(content);
        var closes = FormCloseTagRegex().Matches(content);
        int closeIndex = 0;
        foreach (Match open in opens)
        {
            while (closeIndex < closes.Count && closes[closeIndex].Index < open.Index)
            {
                closeIndex++;
            }
            if (closeIndex < closes.Count)
            {
                var close = closes[closeIndex];
                formSpans.Add((open.Index, close.Index + close.Length));
                closeIndex++;
            }
            else
            {
                formSpans.Add((open.Index, content.Length));
            }
        }

        foreach (Match m in SubmitButtonRegex().Matches(content))
        {
            var inForm = false;
            foreach (var span in formSpans)
            {
                if (m.Index >= span.Start && m.Index < span.End)
                {
                    inForm = true;
                    break;
                }
            }
            if (!inForm)
            {
                var line = LineOf(content, m.Index);
                findings.Add((relative, line, "minor",
                    "<button type=\"submit\"> outside a <form>", Truncate(m.Value)));
            }
        }
    }

    private static void ScanTables(string content, string relative, List<(string, int, string, string, string)> findings)
    {
        foreach (Match m in TableRegex().Matches(content))
        {
            var full = m.Value;
            var line = LineOf(content, m.Index);
            var hasCaption = CaptionTagRegex().IsMatch(full);
            var hasThScope = ThScopeRegex().IsMatch(full);
            if (!hasCaption && !hasThScope)
            {
                findings.Add((relative, line, "moderate",
                    "<table> without <caption> or <th scope=>", Truncate(full)));
            }
        }
    }
}
