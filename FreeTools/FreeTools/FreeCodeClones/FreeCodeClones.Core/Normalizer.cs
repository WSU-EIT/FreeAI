using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FreeCodeClones.Core;

/// <summary>
/// A flattened code unit: a member (method, constructor, accessor, operator, local function, lambda) or a window
/// of consecutive statements. <see cref="Tokens"/> is the normalised stream; <see cref="Hash"/> is its SHA-256.
/// </summary>
public sealed class CodeUnit
{
    internal CodeUnit(SourcePart part, string kind, int start, int end, string[] tokens, string[]? nearTokens, int statements,
        Tok[] toks, SyntaxNode? member, StatementSyntax[]? window)
    {
        Part = part;
        Kind = kind;
        Start = start;
        End = end;
        Tokens = tokens;
        NearTokens = nearTokens;
        Statements = statements;
        Toks = toks;
        Member = member;
        Window = window;
        StartLine = part.LineOf(start);
        EndLine = part.LineOf(Math.Max(start, end - 1));
        Hash = Hashing.Sha256Hex(string.Join("\u001F", tokens));
    }

    public SourcePart Part { get; }

    /// <summary>method, constructor, operator, accessor, local-function, lambda or window.</summary>
    public string Kind { get; }

    public int Start { get; }

    public int End { get; }

    public int StartLine { get; }

    public int EndLine { get; }

    public IReadOnlyList<string> Tokens { get; }

    public int Statements { get; }

    public string Hash { get; }

    public string Path => Part.File.Path;

    public string Location => $"{Path}:{StartLine}-{EndLine}";

    /// <summary>The stream used for near-miss shingles: like <see cref="Tokens"/> but every renamed identifier is "$".</summary>
    internal string[]? NearTokens { get; }

    /// <summary>One per entry of <see cref="Tokens"/>: where it came from and its original text (for fix proposals).</summary>
    internal Tok[] Toks { get; }

    /// <summary>The declaration, for a member unit.</summary>
    internal SyntaxNode? Member { get; }

    /// <summary>The statements, for a window unit.</summary>
    internal StatementSyntax[]? Window { get; }

    public bool Contains(CodeUnit other) => Part == other.Part && Start <= other.Start && other.End <= End;

    public bool Overlaps(CodeUnit other) => Part == other.Part && Start < other.End && other.Start < End;
}

/// <summary>Every unit of a scan: members fully flattened, windows kept as cheap hashed candidates.</summary>
public sealed class UnitSet
{
    public List<CodeUnit> Members { get; } = new List<CodeUnit>();

    internal List<StatementBlock> Blocks { get; } = new List<StatementBlock>();

    internal List<WindowCandidate> Windows { get; } = new List<WindowCandidate>();

    /// <summary>Members plus qualifying statement windows.</summary>
    public int Count => Members.Count + Windows.Count;
}

/// <summary>
/// A token as the normaliser sees it: fixed text, or an identifier to rename by first appearance. It remembers its
/// source token (default for a collapsed interpolated string), span and original text, so the copies of a clone can
/// be lined up token by token to see what differs.
/// </summary>
internal readonly struct Tok
{
    public Tok(string text, bool rename, SyntaxToken token, TextSpan span, string original)
    {
        Text = text;
        Rename = rename;
        Hash = rename ? 0 : Hashing.Of(text);
        Token = token;
        Span = span;
        Original = original;
    }

    public string Text { get; }

    public bool Rename { get; }

    public ulong Hash { get; }

    public SyntaxToken Token { get; }

    public TextSpan Span { get; }

    public string Original { get; }
}

/// <summary>A block's statements, each pre-described once so every window over them is a cheap loop.</summary>
internal sealed class StatementBlock
{
    public StatementBlock(SourcePart part, SyntaxList<StatementSyntax> statements, List<Tok>[] toks, int[] counts)
    {
        Part = part;
        Statements = statements;
        Toks = toks;
        Counts = counts;
    }

    public SourcePart Part { get; }

    public SyntaxList<StatementSyntax> Statements { get; }

    public List<Tok>[] Toks { get; }

    public int[] Counts { get; }
}

/// <summary>Statements First..Last of block <see cref="Block"/>, with a 64-bit hash of their normalised stream.</summary>
internal readonly struct WindowCandidate
{
    public WindowCandidate(int block, int first, int last, ulong hash, int statements)
    {
        Block = block;
        First = first;
        Last = last;
        Hash = hash;
        Statements = statements;
    }

    public int Block { get; }

    public int First { get; }

    public int Last { get; }

    public ulong Hash { get; }

    public int Statements { get; }
}

/// <summary>
/// Flattens code into a normalised token stream. Trivia (whitespace, comments, regions, directives) and attribute
/// lists are dropped; keywords, operators and punctuation are kept exactly; string literals become S (unless
/// KeepLiterals), char literals C, numbers N; identifiers are renamed a, b, c... by first appearance in the unit.
///
/// Structural mode keeps an identifier's real name when, judged from syntax alone (there is no semantic model):
///   * it is the method being invoked: <c>x.Replace(...)</c>, <c>x?.Trim()</c>, <c>Helper(...)</c>; or
///   * it is the member in <c>x.Name</c> and x is "type-like": a predefined type (<c>string.Empty</c>), a call
///     result (<c>Get().Name</c>), a name the scanned code uses as a type or namespace somewhere, a known
///     framework type, or a PascalCase member of one of those (<c>DataObjects.SignalRUpdateType.Loading</c>).
/// Limits: a local or property that shares a type's name is treated as the type, and a member read off a
/// variable (<c>user.Enabled</c>) is always renamed, even when the variable's type is a framework type.
/// </summary>
public sealed class Normalizer
{
    // Contextual keywords the parser leaves as identifiers. They are language, not names, so never renamed.
    private static readonly HashSet<string> Contextual = new(StringComparer.Ordinal) { "var", "nameof", "dynamic", "_" };

    private readonly CloneOptions _options;
    private readonly ISet<string> _typeNames;
    private readonly List<string> _placeholders = new();
    private readonly List<ulong> _placeholderHashes = new();

    public Normalizer(CloneOptions options, ISet<string> typeNames)
    {
        _options = options;
        _typeNames = typeNames;
    }

    /// <summary>Collects the member units and statement-window candidates of every part.</summary>
    public UnitSet Extract(IEnumerable<SourcePart> parts)
    {
        var set = new UnitSet();
        foreach (SourcePart part in parts) {
            foreach (SyntaxNode node in part.Tree.GetRoot().DescendantNodes()) {
                if (node is BlockSyntax block && block.Statements.Count > 0) {
                    AddWindows(part, block, set);
                }

                // Code that does not parse (Razor markup inside an @code RenderFragment) is not C# to compare.
                string? kind = MemberKind(node, out SyntaxTokenList modifiers);
                if (kind is null || node.ContainsDiagnostics) {
                    continue;
                }

                List<Tok> toks = Describe(node, modifiers);
                int statements = node.DescendantNodes().Count(IsCountedStatement);
                if (!Qualifies(toks.Count, statements)) {
                    continue;
                }

                // Start at the first token after any attribute lists, so locations begin at the declaration.
                int start = node.DescendantTokens(n => !(n is AttributeListSyntax)).First().SpanStart;
                string[] near = toks.Select(t => t.Rename ? "$" : t.Text).ToArray();
                set.Members.Add(new CodeUnit(part, kind, start, node.Span.End, Flatten(toks), near, statements, toks.ToArray(), node, null));
            }
        }

        return set;
    }

    /// <summary>Builds the full unit for a window candidate (only done for candidates whose hash recurs).</summary>
    internal CodeUnit Materialize(UnitSet set, WindowCandidate w)
    {
        StatementBlock block = set.Blocks[w.Block];
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        var tokens = new List<string>();
        var toks = new List<Tok>();
        for (int i = w.First; i <= w.Last; i++) {
            foreach (Tok t in block.Toks[i]) {
                tokens.Add(t.Rename ? Placeholder(Rename(names, t.Text)) : t.Text);
                toks.Add(t);
            }
        }

        StatementSyntax[] window = Enumerable.Range(w.First, w.Last - w.First + 1).Select(i => block.Statements[i]).ToArray();
        return new CodeUnit(block.Part, "window", block.Statements[w.First].SpanStart, block.Statements[w.Last].Span.End,
            tokens.ToArray(), null, w.Statements, toks.ToArray(), null, window);
    }

    private static bool IsCountedStatement(SyntaxNode node) => node is StatementSyntax && !(node is BlockSyntax);

    private static bool IsInvoked(ExpressionSyntax e) => e.Parent is InvocationExpressionSyntax inv && inv.Expression == e;

    /// <summary>
    /// True when statements first..last repeat with some period p (each equals the one p later), like a run of
    /// <c>sb.AppendLine(S);</c>. Every slice of such a run matches every other slice, which is repetition inside
    /// one place, not a copy between two; a larger window that also covers the statements around it still counts.
    /// </summary>
    private static bool IsPeriodic(int[][] mismatches, int first, int last)
    {
        int length = last - first + 1;
        for (int p = 1; p < mismatches.Length && p <= length / 2; p++) {
            if (mismatches[p][last - p + 1] - mismatches[p][first] == 0) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// For periods p = 1..16, prefix counts of positions k where statement k differs from statement k + p, each
    /// statement flattened on its own. Lets <see cref="IsPeriodic"/> test a window in O(16).
    /// </summary>
    private static int[][] PeriodMismatches(List<Tok>[] toks)
    {
        int n = toks.Length;
        var shapes = new ulong[n];
        for (int k = 0; k < n; k++) {
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            ulong h = Hashing.Seed;
            foreach (Tok t in toks[k]) {
                h = Hashing.Mix(h, t.Rename ? (ulong)Rename(names, t.Text) + 1 : t.Hash);
            }

            shapes[k] = h;
        }

        var result = new int[Math.Min(17, n / 2 + 1)][];
        for (int p = 1; p < result.Length; p++) {
            result[p] = new int[n - p + 1];
            for (int k = 0; k + p < n; k++) {
                result[p][k + 1] = result[p][k] + (shapes[k] == shapes[k + p] ? 0 : 1);
            }
        }

        return result;
    }

    private static string? MemberKind(SyntaxNode node, out SyntaxTokenList modifiers)
    {
        modifiers = default;
        switch (node) {
            case MethodDeclarationSyntax m when m.Body is not null || m.ExpressionBody is not null:
                modifiers = m.Modifiers;
                return "method";
            case ConstructorDeclarationSyntax or DestructorDeclarationSyntax:
                modifiers = ((BaseMethodDeclarationSyntax)node).Modifiers;
                return "constructor";
            case OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax:
                modifiers = ((BaseMethodDeclarationSyntax)node).Modifiers;
                return "operator";
            case AccessorDeclarationSyntax a when a.Body is not null || a.ExpressionBody is not null:
                modifiers = a.Modifiers;
                return "accessor";
            case PropertyDeclarationSyntax p when p.ExpressionBody is not null:
                modifiers = p.Modifiers;
                return "accessor";
            case IndexerDeclarationSyntax x when x.ExpressionBody is not null:
                modifiers = x.Modifiers;
                return "accessor";
            case LocalFunctionStatementSyntax f:
                modifiers = f.Modifiers;
                return "local-function";
            case AnonymousFunctionExpressionSyntax f when f.Block is not null && f.Block.Statements.Count >= 3:
                return "lambda";
            default:
                return null;
        }
    }

    private static int Rename(Dictionary<string, int> names, string text)
    {
        if (!names.TryGetValue(text, out int index)) {
            index = names.Count;
            names[text] = index;
        }

        return index;
    }

    /// <summary>Hashes every window of consecutive statements in a block, restarting the renaming at each start.</summary>
    private void AddWindows(SourcePart part, BlockSyntax block, UnitSet set)
    {
        SyntaxList<StatementSyntax> statements = block.Statements;
        int n = statements.Count;
        var toks = new List<Tok>[n];
        var counts = new int[n];
        for (int i = 0; i < n; i++) {
            toks[i] = Describe(statements[i], default);
            counts[i] = (IsCountedStatement(statements[i]) ? 1 : 0) + statements[i].DescendantNodes().Count(IsCountedStatement);
        }

        int[][] mismatches = PeriodMismatches(toks);
        int blockIndex = set.Blocks.Count;
        bool any = false;
        for (int first = 0; first < n; first++) {
            var names = new Dictionary<string, int>(StringComparer.Ordinal);
            ulong hash = Hashing.Seed;
            int tokenCount = 0;
            int statementCount = 0;
            for (int last = first; last < n; last++) {
                if (statements[last].ContainsDiagnostics) {
                    break; // a window never spans code that does not parse
                }

                foreach (Tok t in toks[last]) {
                    hash = Hashing.Mix(hash, t.Rename ? PlaceholderHash(Rename(names, t.Text)) : t.Hash);
                }

                tokenCount += toks[last].Count;
                statementCount += counts[last];

                // A lone local function is already a member unit with the same span.
                if (first == last && statements[first] is LocalFunctionStatementSyntax) {
                    continue;
                }

                if (Qualifies(tokenCount, statementCount) && !IsPeriodic(mismatches, first, last)) {
                    set.Windows.Add(new WindowCandidate(blockIndex, first, last, hash, statementCount));
                    any = true;
                }
            }
        }

        if (any) {
            set.Blocks.Add(new StatementBlock(part, statements, toks, counts));
        }
    }

    private void Add(SyntaxToken t, List<Tok> list)
    {
        if (t.IsMissing || t.Span.Length == 0) {
            return;
        }

        switch (t.Kind()) {
            case SyntaxKind.IdentifierToken:
                string text = t.ValueText;
                bool keep = Contextual.Contains(text) || (_options.Mode == CloneMode.Structural && IsKeptName(t));
                list.Add(new Tok(text, !keep, t, t.Span, t.Text));
                break;
            case SyntaxKind.StringLiteralToken:
            case SyntaxKind.Utf8StringLiteralToken:
            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.MultiLineRawStringLiteralToken:
            case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
            case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
            case SyntaxKind.InterpolatedStringTextToken:
                list.Add(new Tok(_options.KeepLiterals ? t.Text : "S", false, t, t.Span, t.Text));
                break;
            case SyntaxKind.CharacterLiteralToken:
                list.Add(new Tok("C", false, t, t.Span, t.Text));
                break;
            case SyntaxKind.NumericLiteralToken:
                list.Add(new Tok("N", false, t, t.Span, t.Text));
                break;
            default:
                list.Add(new Tok(t.Text, false, t, t.Span, t.Text));
                break;
        }
    }

    private List<Tok> Describe(SyntaxNode node, SyntaxTokenList skip)
    {
        var list = new List<Tok>();
        Walk(node, node, skip, list);
        return list;
    }

    private string[] Flatten(List<Tok> toks)
    {
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new string[toks.Count];
        for (int i = 0; i < toks.Count; i++) {
            result[i] = toks[i].Rename ? Placeholder(Rename(names, toks[i].Text)) : toks[i].Text;
        }

        return result;
    }

    private bool IsKeptName(SyntaxToken t)
    {
        if (!(t.Parent is SimpleNameSyntax name)) {
            return false; // a declaration (local, parameter, member, type): always renamed
        }

        switch (name.Parent) {
            case MemberAccessExpressionSyntax access when access.Name == name:
                return IsInvoked(access) || IsTypeLike(access.Expression);
            case MemberBindingExpressionSyntax binding:
                return IsInvoked(binding);
            case InvocationExpressionSyntax invocation when invocation.Expression == name:
                return true;
            default:
                return false;
        }
    }

    private bool IsTypeLike(ExpressionSyntax e)
    {
        switch (e) {
            case PredefinedTypeSyntax:
            case GenericNameSyntax:
            case AliasQualifiedNameSyntax:
            case InvocationExpressionSyntax:
                return true;
            case ParenthesizedExpressionSyntax p:
                ExpressionSyntax inner = p.Expression is AwaitExpressionSyntax aw ? aw.Expression : p.Expression;
                return inner is InvocationExpressionSyntax;
            case IdentifierNameSyntax id:
                return _typeNames.Contains(id.Identifier.ValueText);
            case MemberAccessExpressionSyntax access:
                string member = access.Name.Identifier.ValueText;
                return member.Length > 0 && char.IsUpper(member[0]) && IsTypeLike(access.Expression);
            default:
                return false;
        }
    }

    private string Placeholder(int index)
    {
        while (_placeholders.Count <= index) {
            // Bijective base 26: a..z, aa..az, ba...
            var sb = new StringBuilder();
            for (int i = _placeholders.Count + 1; i > 0; i = (i - 1) / 26) {
                sb.Insert(0, (char)('a' + ((i - 1) % 26)));
            }

            _placeholders.Add(sb.ToString());
            _placeholderHashes.Add(Hashing.Of(sb.ToString()));
        }

        return _placeholders[index];
    }

    private ulong PlaceholderHash(int index)
    {
        Placeholder(index);
        return _placeholderHashes[index];
    }

    private bool Qualifies(int tokens, int statements) => tokens >= _options.MinTokens || statements >= _options.MinStatements;

    private void Walk(SyntaxNode node, SyntaxNode unit, SyntaxTokenList skip, List<Tok> list)
    {
        foreach (SyntaxNodeOrToken child in node.ChildNodesAndTokens()) {
            if (child.IsNode) {
                SyntaxNode n = child.AsNode()!;
                if (n is AttributeListSyntax) {
                    continue;
                }

                // An interpolated string is a literal: one S, holes and all (kept verbatim-ish with KeepLiterals).
                if (n is InterpolatedStringExpressionSyntax && !_options.KeepLiterals) {
                    list.Add(new Tok("S", false, default, n.Span, n.ToString()));
                    continue;
                }

                Walk(n, unit, skip, list);
            } else {
                SyntaxToken t = child.AsToken();
                if (node == unit && skip.IndexOf(t) >= 0) {
                    continue; // the unit's own modifiers: a public and a private copy are still a copy
                }

                Add(t, list);
            }
        }
    }
}

/// <summary>Deterministic hashing: FNV-1a for fast bucketing, SHA-256 for the reported identity.</summary>
internal static class Hashing
{
    public const ulong Seed = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Mix(ulong hash, ulong value)
    {
        hash ^= value;
        hash *= Prime;
        return hash ^ (hash >> 31);
    }

    public static ulong Of(string s)
    {
        ulong h = Seed;
        foreach (char c in s) {
            h ^= c;
            h *= Prime;
        }

        return h;
    }

    public static string Sha256Hex(string s)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes) {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}