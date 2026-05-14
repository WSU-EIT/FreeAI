using System.Text.RegularExpressions;

namespace FreeA11yChecker.Console.SourceAnalysis;

/// <summary>
/// Maps an HTML snippet from a live scan to candidate .razor / .cs / .html source
/// file locations. Strategy: extract distinctive tokens from the snippet (ids,
/// custom classes, aria-labels, inner text) and grep the source folder for them.
/// Rank candidates by how unique the matching token is — a token that appears in
/// 1 file is a high-confidence match; one that appears in 100 is low.
/// </summary>
public static class SourceMatcher
{
    /// <summary>
    /// One candidate match for a snippet. Lower MatchCount = higher confidence.
    /// </summary>
    public record Candidate(string FilePath, int LineNumber, string MatchedToken, int MatchCount, string Confidence);

    private static readonly Regex IdAttrRx = new(@"\bid\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
    private static readonly Regex ClassAttrRx = new(@"\bclass\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
    private static readonly Regex AriaLabelRx = new(@"\baria-label\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
    private static readonly Regex DataTestIdRx = new(@"\bdata-(?:testid|test-id|cy)\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);
    private static readonly Regex InnerTextRx = new(@">([^<>]{6,80})<", RegexOptions.None);

    private static readonly string[] DefaultExcludeDirs = { "bin", "obj", "node_modules", ".git", ".vs", "wwwroot", "Migrations" };
    private static readonly string[] FileExtensions = { ".razor", ".cs", ".html", ".cshtml" };

    /// <summary>
    /// Find up to 3 best candidate locations in <paramref name="sourceRoot"/> that likely
    /// render the given <paramref name="htmlSnippet"/>. Returns empty list if no plausible
    /// match. Caller should display Confidence so users know how much to trust each entry.
    /// </summary>
    public static List<Candidate> FindCandidates(string sourceRoot, string htmlSnippet, int maxCandidates = 3)
    {
        if (string.IsNullOrWhiteSpace(htmlSnippet) || string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot)) {
            return new List<Candidate>();
        }

        // Extract distinctive tokens, ranked by likely uniqueness (custom IDs first,
        // then aria-labels and data-testids, then specific inner text, then class names).
        var tokens = new List<(string Token, string Kind)>();

        foreach (Match m in IdAttrRx.Matches(htmlSnippet)) {
            string id = m.Groups[1].Value;
            // Skip Blazor-generated dynamic IDs (look like guids or contain colons / underscores+digits).
            if (id.Length < 3 || Regex.IsMatch(id, @"^[a-f0-9-]{12,}$|^_bl_|:")) continue;
            tokens.Add((id, "id"));
        }
        foreach (Match m in DataTestIdRx.Matches(htmlSnippet)) {
            tokens.Add((m.Groups[1].Value, "data-testid"));
        }
        foreach (Match m in AriaLabelRx.Matches(htmlSnippet)) {
            string label = m.Groups[1].Value.Trim();
            if (label.Length >= 3 && label.Length <= 80) tokens.Add((label, "aria-label"));
        }
        foreach (Match m in InnerTextRx.Matches(htmlSnippet)) {
            string text = m.Groups[1].Value.Trim();
            if (text.Length < 6 || text.Length > 80) continue;
            // Skip pure whitespace / numeric-only / common words.
            if (Regex.IsMatch(text, @"^[\s\d.,:;|/\\-]+$")) continue;
            if (IsCommonWord(text)) continue;
            tokens.Add((text, "text"));
        }
        foreach (Match m in ClassAttrRx.Matches(htmlSnippet)) {
            // Use only "custom-looking" classes, not Bootstrap utilities.
            foreach (string cls in m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                if (cls.Length < 4) continue;
                if (IsKnownFrameworkClass(cls)) continue;
                tokens.Add((cls, "class"));
            }
        }

        if (tokens.Count == 0) return new List<Candidate>();

        // Enumerate source files once.
        List<string> sourceFiles = EnumerateSourceFiles(sourceRoot);
        if (sourceFiles.Count == 0) return new List<Candidate>();

        var matches = new List<Candidate>();
        foreach (var (token, kind) in tokens) {
            // Search-quote the token. For text/aria-label tokens we want a substring match;
            // for ids/classes we want a more anchored match.
            string searchPattern = kind == "id" || kind == "class" || kind == "data-testid"
                ? Regex.Escape(token)
                : Regex.Escape(token);

            int totalHits = 0;
            var hitsThisToken = new List<(string File, int Line)>();
            foreach (string file in sourceFiles) {
                try {
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++) {
                        if (lines[i].Contains(token, StringComparison.OrdinalIgnoreCase)) {
                            totalHits++;
                            if (hitsThisToken.Count < 5) hitsThisToken.Add((file, i + 1));
                            if (totalHits > 50) break; // too noisy, skip this token
                        }
                    }
                } catch { /* skip unreadable file */ }
                if (totalHits > 50) break;
            }

            if (totalHits == 0 || totalHits > 50) continue;

            string confidence = totalHits switch {
                1 => "very-high",
                <= 3 => "high",
                <= 10 => "medium",
                _ => "low",
            };

            foreach (var (file, line) in hitsThisToken) {
                matches.Add(new Candidate(
                    FilePath: Path.GetRelativePath(sourceRoot, file).Replace('\\', '/'),
                    LineNumber: line,
                    MatchedToken: $"{kind}:'{Truncate(token, 40)}'",
                    MatchCount: totalHits,
                    Confidence: confidence));
            }
        }

        // Dedupe by file:line, keep best confidence per location.
        return matches
            .GroupBy(c => c.FilePath + ":" + c.LineNumber)
            .Select(g => g.OrderBy(c => c.MatchCount).First())
            .OrderBy(c => c.MatchCount)
            .Take(maxCandidates)
            .ToList();
    }

    private static List<string> EnumerateSourceFiles(string root)
    {
        var files = new List<string>();
        foreach (string ext in FileExtensions) {
            try {
                foreach (string f in Directory.EnumerateFiles(root, "*" + ext, SearchOption.AllDirectories)) {
                    if (DefaultExcludeDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))) continue;
                    var fi = new FileInfo(f);
                    if (fi.Length > 1024 * 1024) continue; // skip >1MB files
                    files.Add(f);
                }
            } catch { /* skip on error */ }
        }
        return files;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";

    private static readonly HashSet<string> _commonWords = new(StringComparer.OrdinalIgnoreCase) {
        "Submit", "Cancel", "OK", "Close", "Save", "Delete", "Edit", "Add", "Remove",
        "Yes", "No", "Back", "Next", "Continue", "Loading", "Loading...", "Please wait",
        "Email", "Password", "Username", "Search", "Settings", "Profile", "Logout", "Login", "Sign in", "Sign out",
    };
    private static bool IsCommonWord(string text) => _commonWords.Contains(text.Trim());

    private static readonly HashSet<string> _frameworkClassPrefixes = new(StringComparer.OrdinalIgnoreCase) {
        "btn", "col", "row", "container", "form", "alert", "card", "modal", "nav", "navbar", "list",
        "text", "bg", "border", "rounded", "shadow", "p-", "px-", "py-", "m-", "mx-", "my-", "mt-", "mb-", "ms-", "me-",
        "fa", "fas", "far", "fab", "fal", "fad", "fa-", "icon", "d-", "flex", "justify", "align",
        "h-", "w-", "min-", "max-", "g-", "gap-", "small", "lead", "dropdown", "show", "active",
        "disabled", "valid", "invalid", "is-", "has-",
    };
    private static bool IsKnownFrameworkClass(string cls)
    {
        foreach (string prefix in _frameworkClassPrefixes) {
            if (cls.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
