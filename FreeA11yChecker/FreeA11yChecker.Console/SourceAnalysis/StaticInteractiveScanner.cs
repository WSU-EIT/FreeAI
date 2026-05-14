using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static partial class StaticInteractiveScanner
{
    private const long MaxFileSizeBytes = 1024 * 1024;
    private const int SnippetMaxLength = 200;

    private static readonly string[] SkippedDirectoryNames =
    {
        "bin",
        "obj",
        "node_modules",
        ".git",
        ".vs"
    };

    [GeneratedRegex(@"<(?:div|span)\b[^>]*\s@onclick\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex ClickOnNonInteractiveRegex();

    [GeneratedRegex(@"<a\b[^>]*\shref\s*=\s*""\s*(?:javascript:\s*void\s*\(\s*0\s*\)\s*|#)\s""", RegexOptions.IgnoreCase)]
    private static partial Regex DummyHrefRegex();

    [GeneratedRegex(@"<a\b[^>]*\starget\s*=\s*""_blank""[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetBlankAnchorRegex();

    [GeneratedRegex(@"\brel\s*=\s*""[^""]*\bnoopener\b[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex RelNoopenerRegex();

    [GeneratedRegex(@"\baria-label\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex AriaLabelRegex();

    [GeneratedRegex(@"\b(?:new\s+window|new\s+tab|opens\s+in)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NewWindowTextRegex();

    [GeneratedRegex(@"<input\b[^>]*\stype\s*=\s*""image""[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex InputImageRegex();

    [GeneratedRegex(@"\balt\s*=\s*""", RegexOptions.IgnoreCase)]
    private static partial Regex AltAttributeRegex();

    [GeneratedRegex(@"<[a-zA-Z][^>]*\stabindex\s*=\s*""\s*-1\s*""[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TabIndexNegativeRegex();

    [GeneratedRegex(@"<[a-zA-Z][^>]*\stabindex\s*=\s*""\s*(\d+)\s*""[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TabIndexPositiveRegex();

    [GeneratedRegex(@"<button\b[^>]*\stype\s*=\s*""button""[^>]*\s@onclick\s*=[^>]*>(.*?)</button>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ButtonWithOnClickRegex();

    [GeneratedRegex(@"\s+(?:href|onclick|role|tabindex|disabled|hidden|aria-hidden)\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex InteractiveAttrRegex();

    public static List<(string File, int Line, string Severity, string Issue, string Snippet)> Analyze(string sourceRoot)
    {
        var results = new List<(string, int, string, string, string)>();

        if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
        {
            return results;
        }

        var fullRoot = Path.GetFullPath(sourceRoot);

        foreach (var file in EnumerateFiles(fullRoot))
        {
            FileInfo info;
            try
            {
                info = new FileInfo(file);
            }
            catch
            {
                continue;
            }

            if (!info.Exists || info.Length > MaxFileSizeBytes)
            {
                continue;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch
            {
                continue;
            }

            var relative = Path.GetRelativePath(fullRoot, file);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Length == 0)
                {
                    continue;
                }

                var lineNumber = i + 1;
                var snippet = Truncate(line.Trim(), SnippetMaxLength);

                // 1. <div|span ... @onclick=...>
                if (ClickOnNonInteractiveRegex().IsMatch(line))
                {
                    results.Add((relative, lineNumber, "serious",
                        "Click handler on non-interactive element (use <button>)", snippet));
                }

                // 2. <a href="javascript:void(0)"> or <a href="#">
                if (DummyHrefRegex().IsMatch(line))
                {
                    results.Add((relative, lineNumber, "moderate",
                        "Empty/dummy href — use <button> instead", snippet));
                }

                // 3. <a target="_blank"> missing rel=noopener AND missing new-window warning
                foreach (Match anchor in TargetBlankAnchorRegex().Matches(line))
                {
                    var tag = anchor.Value;
                    var hasNoopener = RelNoopenerRegex().IsMatch(tag);
                    var hasAriaWarning = AriaLabelRegex().IsMatch(tag) && NewWindowTextRegex().IsMatch(tag);
                    var hasVisibleWarning = NewWindowTextRegex().IsMatch(line);

                    if (!hasNoopener && !hasAriaWarning && !hasVisibleWarning)
                    {
                        results.Add((relative, lineNumber, "minor",
                            "target=\"_blank\" missing rel=\"noopener\" and new-window warning", snippet));
                    }
                }

                // 4. <input type="image"> missing alt=
                foreach (Match input in InputImageRegex().Matches(line))
                {
                    if (!AltAttributeRegex().IsMatch(input.Value))
                    {
                        results.Add((relative, lineNumber, "serious",
                            "<input type=\"image\"> missing alt attribute", snippet));
                    }
                }

                // 5. tabindex="-1" on visible interactive elements
                foreach (Match tag in TabIndexNegativeRegex().Matches(line))
                {
                    if (InteractiveAttrRegex().IsMatch(tag.Value) ||
                        tag.Value.Contains("@onclick", StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.StartsWith("<a ", StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.StartsWith("<button", StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.StartsWith("<input", StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.StartsWith("<select", StringComparison.OrdinalIgnoreCase) ||
                        tag.Value.StartsWith("<textarea", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add((relative, lineNumber, "moderate",
                            "tabindex=\"-1\" on interactive element (not focusable)", snippet));
                    }
                }

                // 6. tabindex > 0
                foreach (Match tag in TabIndexPositiveRegex().Matches(line))
                {
                    if (int.TryParse(tag.Groups[1].Value, out var tabValue) && tabValue > 0)
                    {
                        results.Add((relative, lineNumber, "moderate",
                            "tabindex > 0 manipulates tab order (anti-pattern)", snippet));
                    }
                }
            }

            // 7. <button type="button"> with @onclick but no aria-label and no text content (may span lines)
            var content = string.Join('\n', lines);
            foreach (Match button in ButtonWithOnClickRegex().Matches(content))
            {
                var openTag = button.Value;
                var gtIndex = openTag.IndexOf('>');
                if (gtIndex < 0)
                {
                    continue;
                }

                var openTagOnly = openTag[..(gtIndex + 1)];
                var inner = button.Groups[1].Value;

                var hasAriaLabel = AriaLabelRegex().IsMatch(openTagOnly);
                var textContent = Regex.Replace(inner, "<[^>]+>", string.Empty).Trim();

                if (!hasAriaLabel && textContent.Length == 0)
                {
                    var lineNumber = content.Take(button.Index).Count(c => c == '\n') + 1;
                    var snippet = Truncate(openTagOnly.Trim(), SnippetMaxLength);
                    results.Add((relative, lineNumber, "serious",
                        "<button> with @onclick has no aria-label and no text content", snippet));
                }
            }
        }

        return results;
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            string[] subdirs;
            try
            {
                subdirs = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }

            foreach (var dir in subdirs)
            {
                var name = Path.GetFileName(dir);
                if (SkippedDirectoryNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                stack.Push(dir);
            }

            string[] files;
            try
            {
                files = Directory.GetFiles(current);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (string.Equals(ext, ".razor", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
