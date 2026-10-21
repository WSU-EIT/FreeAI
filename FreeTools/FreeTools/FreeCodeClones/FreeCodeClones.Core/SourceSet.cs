using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FreeCodeClones.Core;

/// <summary>One scanned file: its root-relative path (forward slashes), its lines for excerpts, and its C# parts.</summary>
public sealed class SourceFile
{
    internal SourceFile(string path, string text)
    {
        Path = path;
        Text = text;
        Eol = text.Contains("\r\n") ? "\r\n" : "\n";
        Lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
    }

    public string Path { get; }

    /// <summary>The file exactly as read, so fix proposals can be written as patches against it.</summary>
    public string Text { get; }

    /// <summary>The file's line ending ("\r\n" or "\n"), used for any lines a proposal adds.</summary>
    public string Eol { get; }

    public IReadOnlyList<string> Lines { get; }

    public List<SourcePart> Parts { get; } = new List<SourcePart>();
}

/// <summary>
/// One parsed piece of C#: a whole .cs file, or one <c>@code</c>/<c>@functions</c> block of a .razor file wrapped
/// in a throwaway class. <see cref="LineOffset"/> maps the wrapped tree's lines back onto the .razor file.
/// </summary>
public sealed class SourcePart
{
    private readonly SourceText _text;

    internal SourcePart(int id, SourceFile file, SyntaxTree tree, int lineOffset, int positionOffset)
    {
        Id = id;
        File = file;
        Tree = tree;
        LineOffset = lineOffset;
        PositionOffset = positionOffset;
        _text = tree.GetText();
        file.Parts.Add(this);
    }

    /// <summary>Add to a position in this part's tree to get the position in <see cref="SourceFile.Text"/>.</summary>
    public int PositionOffset { get; }

    public int Id { get; }

    public SourceFile File { get; }

    public SyntaxTree Tree { get; }

    public int LineOffset { get; }

    /// <summary>The 1-based line in the original file of a position in this part's tree.</summary>
    public int LineOf(int position) => _text.Lines.GetLineFromPosition(position).LineNumber + LineOffset + 1;
}

/// <summary>
/// Picks the files to scan, parses them (extracting Razor code blocks), and collects the names the scanned code
/// uses as types, which the structural normaliser needs to tell <c>Type.Member</c> from <c>local.Member</c>.
/// </summary>
public sealed class SourceSet
{
    // Folders never scanned. A segment matches when it equals the name or ends in ".{name}" (FreeCICD.EFModels).
    private static readonly string[] AlwaysExcluded = { "bin", "obj", ".git", ".vs", "node_modules", "Migrations", "EFModels" };

    private static readonly Regex CodeDirective = new(@"@(?:code|functions)\b", RegexOptions.Compiled);

    // Framework static types often used only as receivers (Path.Combine, StringComparison.Ordinal), so they may
    // never appear in a type position of the scanned code.
    private static readonly string[] FrameworkTypes =
    {
        "Activator", "Array", "BitConverter", "Console", "Convert", "CultureInfo", "DateTime", "DateTimeOffset",
        "Debug", "Directory", "Encoding", "Enum", "Enumerable", "Environment", "File", "GC", "Guid", "Interlocked",
        "JsonSerializer", "Math", "Nullable", "Path", "Process", "Regex", "Stopwatch", "String", "StringComparer",
        "StringComparison", "Task", "Thread", "TimeSpan", "Type", "Uri", "UriKind"
    };

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);

    private static readonly Dictionary<string, Regex> GlobCache = new(StringComparer.Ordinal);

    /// <summary>The throwaway class a Razor @code block is wrapped in for parsing.</summary>
    internal const string RazorWrapperClass = "__FreeCodeClonesRazor__";

    private SourceSet(List<SourceFile> files, List<SourcePart> parts, HashSet<string> typeNames)
    {
        Files = files;
        Parts = parts;
        TypeNames = typeNames;
    }

    public IReadOnlyList<SourceFile> Files { get; }

    public IReadOnlyList<SourcePart> Parts { get; }

    public HashSet<string> TypeNames { get; }

    /// <summary>
    /// The FreeCRM <c>.App.</c> layer of a project: <c>{Project}.App.{Feature}</c> files (your code),
    /// <c>{Class}.App.cs</c> hook files, and <c>{Class}.App.{Project}</c> project extensions of base classes.
    /// </summary>
    public static IReadOnlyList<string> DefaultIncludes(string projectName) => new[]
    {
        $"**/{projectName}.App.*.cs",
        "**/*.App.cs",
        $"**/*.App.{projectName}.cs",
        $"**/{projectName}.App.*.razor",
        "**/*.App.razor",
        $"**/*.App.{projectName}.razor"
    };

    /// <summary>The project name from the root's single .slnx/.sln (FreeCICD.slnx gives FreeCICD), else the folder name.</summary>
    public static string DetectProjectName(string root)
    {
        foreach (string pattern in new[] { "*.slnx", "*.sln" }) {
            string[] solutions = Directory.GetFiles(root, pattern);
            Array.Sort(solutions, StringComparer.Ordinal);
            if (solutions.Length > 0) {
                return System.IO.Path.GetFileNameWithoutExtension(solutions[0]);
            }
        }

        return new DirectoryInfo(root).Name;
    }

    /// <summary>Root-relative paths (forward slashes, sorted ordinally) of the .cs/.razor files to scan.</summary>
    public static List<string> SelectFiles(string root, string projectName, CloneOptions options)
    {
        IReadOnlyList<string> includes = options.IncludeAll
            ? new[] { "**/*.cs", "**/*.razor" }
            : options.Includes.Count > 0 ? options.Includes : DefaultIncludes(projectName);

        var result = new List<string>();
        Walk(root, string.Empty);
        result.Sort(StringComparer.Ordinal);
        return result;

        void Walk(string dir, string rel)
        {
            foreach (string sub in Directory.GetDirectories(dir)) {
                string name = System.IO.Path.GetFileName(sub);
                if (!IsExcludedFolder(name, options.IncludeTests)) {
                    Walk(sub, rel.Length == 0 ? name : rel + "/" + name);
                }
            }

            foreach (string file in Directory.GetFiles(dir)) {
                string ext = System.IO.Path.GetExtension(file);
                if (!ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".razor", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                string relFile = (rel.Length == 0 ? string.Empty : rel + "/") + System.IO.Path.GetFileName(file);
                if (includes.Any(g => GlobMatch(g, relFile, false)) && !options.Excludes.Any(g => GlobMatch(g, relFile, true))) {
                    result.Add(relFile);
                }
            }
        }
    }

    /// <summary>Parses the given files. Razor files contribute one part per @code/@functions block.</summary>
    public static SourceSet Parse(IEnumerable<(string Path, string Text)> inputs)
    {
        var files = new List<SourceFile>();
        var parts = new List<SourcePart>();
        foreach ((string path, string text) in inputs.OrderBy(i => i.Path.Replace('\\', '/'), StringComparer.Ordinal)) {
            var file = new SourceFile(path.Replace('\\', '/'), text);
            if (path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)) {
                foreach ((int open, int close) in FindCodeBlocks(text)) {
                    // The body starts on the '{' line, so wrapped line 0 is that line of the .razor file.
                    const string prefix = "class " + RazorWrapperClass + "{";
                    string wrapped = prefix + text.Substring(open + 1, close - open - 1) + "}";
                    var tree = CSharpSyntaxTree.ParseText(wrapped, ParseOptions, path);
                    parts.Add(new SourcePart(parts.Count, file, tree, CountNewlines(text, open), open + 1 - prefix.Length));
                }
            } else {
                parts.Add(new SourcePart(parts.Count, file, CSharpSyntaxTree.ParseText(text, ParseOptions, path), 0, 0));
            }

            files.Add(file);
        }

        return new SourceSet(files, parts, CollectTypeNames(parts));
    }

    /// <summary>Finds the (openBraceIndex, closeBraceIndex) of every @code/@functions block.</summary>
    internal static List<(int Open, int Close)> FindCodeBlocks(string text)
    {
        var result = new List<(int, int)>();
        foreach (Match m in CodeDirective.Matches(text)) {
            int i = m.Index + m.Length;
            while (i < text.Length && char.IsWhiteSpace(text[i])) {
                i++;
            }

            if (i >= text.Length || text[i] != '{') {
                continue;
            }

            int close = FindMatchingBrace(text, i);
            if (close > i) {
                result.Add((i, close));
            }
        }

        return result;
    }

    /// <summary>
    /// Matches a root-relative path against a glob: <c>**</c> spans folders, <c>*</c> and <c>?</c> stay within one.
    /// A pattern with no slash matches the file name, or with <paramref name="anySegment"/> any path segment.
    /// </summary>
    internal static bool GlobMatch(string pattern, string path, bool anySegment)
    {
        string p = pattern.Trim().Replace('\\', '/');
        if (p.StartsWith("./", StringComparison.Ordinal)) {
            p = p.Substring(2);
        }

        if (p.Length == 0) {
            return false;
        }

        Regex regex = GlobRegex(p);
        if (p.IndexOf('/') >= 0) {
            return regex.IsMatch(path);
        }

        string[] segments = path.Split('/');
        return anySegment ? segments.Any(regex.IsMatch) : regex.IsMatch(segments[segments.Length - 1]);
    }

    private static HashSet<string> CollectTypeNames(IEnumerable<SourcePart> parts)
    {
        var names = new HashSet<string>(FrameworkTypes, StringComparer.Ordinal);
        foreach (SourcePart part in parts) {
            foreach (SyntaxNode node in part.Tree.GetRoot().DescendantNodes()) {
                switch (node) {
                    case BaseTypeDeclarationSyntax type:
                        names.Add(type.Identifier.ValueText);
                        break;
                    case DelegateDeclarationSyntax del:
                        names.Add(del.Identifier.ValueText);
                        break;
                    case SimpleNameSyntax name when name.Parent is QualifiedNameSyntax
                        || name.Parent is TypeArgumentListSyntax
                        || SyntaxFacts.IsInNamespaceOrTypeContext(name):
                        names.Add(name.Identifier.ValueText);
                        break;
                }
            }
        }

        return names;
    }

    private static int CountNewlines(string text, int end)
    {
        int count = 0;
        for (int i = 0; i < end; i++) {
            if (text[i] == '\n') {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Finds the brace that closes the one at <paramref name="openBraceIndex"/>, using the C# lexer so braces inside
    /// strings, comments, char literals and interpolations don't fool the count.
    /// </summary>
    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        int depth = 0;
        foreach (SyntaxToken token in SyntaxFactory.ParseTokens(text.Substring(openBraceIndex))) {
            if (token.IsKind(SyntaxKind.OpenBraceToken)) {
                depth++;
            } else if (token.IsKind(SyntaxKind.CloseBraceToken)) {
                depth--;
                if (depth == 0) {
                    return openBraceIndex + token.SpanStart;
                }
            }
        }

        return -1;
    }

    private static Regex GlobRegex(string glob)
    {
        lock (GlobCache) {
            if (GlobCache.TryGetValue(glob, out Regex? cached)) {
                return cached;
            }

            var sb = new StringBuilder("^");
            for (int i = 0; i < glob.Length; i++) {
                char c = glob[i];
                if (c == '*' && i + 1 < glob.Length && glob[i + 1] == '*') {
                    i++;
                    if (i + 1 < glob.Length && glob[i + 1] == '/') {
                        i++;
                        sb.Append("(?:.*/)?"); // "**/" is zero or more folders
                    } else {
                        sb.Append(".*");
                    }
                } else if (c == '*') {
                    sb.Append("[^/]*");
                } else if (c == '?') {
                    sb.Append("[^/]");
                } else {
                    sb.Append(Regex.Escape(c.ToString()));
                }
            }

            sb.Append('$');
            var regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            GlobCache[glob] = regex;
            return regex;
        }
    }

    private static bool IsExcludedFolder(string name, bool includeTests)
    {
        foreach (string excluded in AlwaysExcluded) {
            if (name.Equals(excluded, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("." + excluded, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return !includeTests && name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase);
    }
}