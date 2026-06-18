using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreeCodeReorganizer.Core;

/// <summary>Outcome of reorganizing a single source file.</summary>
public readonly record struct ReorgResult(
    string? NewText,
    bool Changed,
    int TypesReordered,
    int TypesSkipped,
    int BracesCollapsed,
    string? Error);

/// <summary>
/// Reorganizes the members of every type in a C# source string using Roslyn. It is built for
/// MINIMAL change: it only reorders the kinds the rules say to sort (by default properties +
/// methods, interleaved alphabetically), it leaves every member's original trivia (comments,
/// XML docs, and the author's blank-line spacing) untouched, and it only rewrites a type when the
/// member order actually changes. Members that don't move stay byte-identical. Hard safety rails
/// abort any file that would lose a member or introduce a syntax error.
/// </summary>
public sealed class Reorganizer
{
    public static ReorgResult Run(string sourceText, ReorderConfig config, string eol)
    {
        SyntaxTree tree;
        try {
            tree = CSharpSyntaxTree.ParseText(sourceText);
        } catch (Exception ex) {
            return new ReorgResult(null, false, 0, 0, 0, "parse error: " + ex.Message);
        }

        var originalRoot = tree.GetRoot();
        bool originalHadErrors = tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

        // Pass 1: reorder members — skipped entirely when ReorderMembers is off (cleanup-only / a file
        // excluded from reorganize), leaving only the formatting pass below.
        var rewriter = new Rewriter(config, eol);
        SyntaxNode afterReorg = originalRoot;
        if (config.ReorderMembers) {
            afterReorg = rewriter.Visit(originalRoot) ?? originalRoot;
        }

        // Pass 2: reformat wrapped parameter lists into the author's hand style ("(" alone, params one
        // level deeper, ")" glued to the body "{" as "){"), or collapse short ones back to a single line.
        int bracesCollapsed = 0;
        SyntaxNode finalRoot = afterReorg;
        if (config.CollapseWrappedParameterBrace) {
            var paramRewriter = new WrappedParameterRewriter(config, eol);
            finalRoot = paramRewriter.Visit(afterReorg) ?? afterReorg;
            bracesCollapsed = paramRewriter.SignaturesReformatted;
        }

        var newText = finalRoot.ToFullString();
        if (string.Equals(newText, sourceText, StringComparison.Ordinal)) {
            // Nothing actually changed (already in order, braces already collapsed).
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, 0, null);
        }

        // Safety net 1: result must parse, and must not introduce NEW errors.
        var newTree = CSharpSyntaxTree.ParseText(newText);
        bool newHasErrors = newTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
        if (newHasErrors && !originalHadErrors) {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, 0,
                "aborted: change would introduce a syntax error");
        }

        // Safety net 2: the exact same set of members must still be present.
        if (!MembersPreserved(originalRoot, newTree.GetRoot())) {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, 0,
                "aborted: member set changed (safety check failed)");
        }

        return new ReorgResult(newText, true, rewriter.TypesReordered, rewriter.TypesSkipped, bracesCollapsed, null);
    }

    private static bool MembersPreserved(SyntaxNode oldRoot, SyntaxNode newRoot)
    {
        var a = CollectSignatures(oldRoot);
        var b = CollectSignatures(newRoot);
        if (a.Count != b.Count) {
            return false;
        }
        a.Sort(StringComparer.Ordinal);
        b.Sort(StringComparer.Ordinal);
        for (int i = 0; i < a.Count; i++) {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) {
                return false;
            }
        }
        return true;
    }

    // Order-independent signature so reordering doesn't flag a difference, but a dropped or
    // duplicated member does.
    private static List<string> CollectSignatures(SyntaxNode root)
    {
        var list = new List<string>();
        foreach (var m in root.DescendantNodes().OfType<MemberDeclarationSyntax>()) {
            var sig = MemberClassifier.KindOf(m) + "|" + MemberClassifier.NameOf(m);
            if (m is BaseMethodDeclarationSyntax bm) {
                sig += "|(" + string.Join(",", bm.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? "?")) + ")";
            }
            list.Add(sig);
        }
        return list;
    }

    /// <summary>
    /// Reformats a method/constructor/operator/local-function whose parameter list is ALREADY wrapped
    /// across multiple lines into the FreeCRM author's hand style — "(" alone on its own line at the
    /// member's indent, each parameter one level deeper, ")" glued to the body's "{" as "){" — OR, when
    /// the whole signature would fit on a single line within <see cref="ReorderConfig.MaxLineWidth"/>,
    /// collapses it back onto one line (normal Allman brace). Single-line declarations are never wrapped.
    /// Members whose parameter list carries comments or preprocessor directives are left alone.
    /// </summary>
    private sealed class WrappedParameterRewriter : CSharpSyntaxRewriter
    {
        private readonly int _maxWidth;
        private readonly string _eol;

        public WrappedParameterRewriter(ReorderConfig config, string eol)
        {
            _maxWidth = config.MaxLineWidth;
            _eol = eol;
        }

        public int SignaturesReformatted { get; private set; }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            => Apply((MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!);

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            => Apply((ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!);

        public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
            => Apply((OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!);

        public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
            => Apply((ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!);

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            => Apply((LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!);

        private SyntaxNode Apply(SyntaxNode node)
        {
            ParameterListSyntax? pl = ParamsOf(node);
            BlockSyntax? body = BodyOf(node);
            if (pl is null || body is null || pl.Parameters.Count < 1) {
                return node;
            }

            string plText = pl.ToString();
            // Only reformat lists that are ALREADY wrapped, and never touch one carrying comments or
            // directives (we'd risk dropping them).
            if (!plText.Contains("\n")
                || plText.Contains("//") || plText.Contains("/*") || plText.Contains("#")) {
                return node;
            }

            string baseIndent = GetIndent(node);
            string unit = baseIndent.Contains("\t") ? "\t" : "    ";

            int offset = pl.CloseParenToken.Span.End - node.SpanStart;
            string header = node.ToString();
            if (offset > 0 && offset <= header.Length) {
                header = header.Substring(0, offset);
            }
            int singleLineWidth = baseIndent.Length + CollapseWhitespace(header).Length + 2; // + " {"

            SyntaxNode result = singleLineWidth <= _maxWidth
                ? BuildSingleLine(node, pl, body, baseIndent)
                : BuildHouseStyle(node, pl, body, baseIndent, unit);

            if (result.ToFullString() != node.ToFullString()) {
                SignaturesReformatted++;
            }

            return result;
        }

        private SyntaxNode BuildHouseStyle(SyntaxNode node, ParameterListSyntax pl, BlockSyntax body, string baseIndent, string unit)
        {
            var eolT = SyntaxFactory.EndOfLine(_eol);
            var baseWs = SyntaxFactory.Whitespace(baseIndent);
            var paramWs = SyntaxFactory.Whitespace(baseIndent + unit);

            var ps = pl.Parameters.Select(p => p.WithLeadingTrivia(eolT, paramWs).WithTrailingTrivia());
            var newPl = pl
                .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithLeadingTrivia(eolT, baseWs).WithTrailingTrivia())
                .WithParameters(Separate(ps, SyntaxFactory.Token(SyntaxKind.CommaToken)))
                .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithLeadingTrivia(eolT, baseWs).WithTrailingTrivia());

            // Glue ")" to "{" as "){" — unless an initializer (": base(...)") sits between them.
            SyntaxTriviaList braceLeading = HasInitializer(node)
                ? body.OpenBraceToken.LeadingTrivia
                : SyntaxFactory.TriviaList();
            return Recombine(node, pl, newPl, braceLeading);
        }

        private SyntaxNode BuildSingleLine(SyntaxNode node, ParameterListSyntax pl, BlockSyntax body, string baseIndent)
        {
            var ps = pl.Parameters.Select(p => p.WithLeadingTrivia().WithTrailingTrivia());
            var comma = SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space);
            var newPl = pl
                .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken))
                .WithParameters(Separate(ps, comma))
                .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken));

            // Allman brace on its own line at the member's indent (leave it where it is if an initializer
            // follows the closing paren).
            SyntaxTriviaList braceLeading = HasInitializer(node)
                ? body.OpenBraceToken.LeadingTrivia
                : SyntaxFactory.TriviaList(SyntaxFactory.EndOfLine(_eol), SyntaxFactory.Whitespace(baseIndent));
            return Recombine(node, pl, newPl, braceLeading);
        }

        // Swap in the rebuilt parameter list, clear any trailing trivia on the token just before "(",
        // and set the body brace's leading trivia.
        private static SyntaxNode Recombine(SyntaxNode node, ParameterListSyntax oldPl, ParameterListSyntax newPl, SyntaxTriviaList braceLeading)
        {
            SyntaxNode n = node.ReplaceNode(oldPl, newPl);

            SyntaxToken openParen = ParamsOf(n)!.OpenParenToken;
            SyntaxToken prev = openParen.GetPreviousToken();
            if (!prev.IsKind(SyntaxKind.None)) {
                n = n.ReplaceToken(prev, prev.WithTrailingTrivia());
            }

            SyntaxToken brace = BodyOf(n)!.OpenBraceToken;
            n = n.ReplaceToken(brace, brace.WithLeadingTrivia(braceLeading));
            return n;
        }

        private static SeparatedSyntaxList<ParameterSyntax> Separate(IEnumerable<ParameterSyntax> parameters, SyntaxToken comma)
        {
            var list = parameters.ToList();
            var items = new List<SyntaxNodeOrToken>(list.Count * 2);
            for (int i = 0; i < list.Count; i++) {
                items.Add(list[i]);
                if (i < list.Count - 1) {
                    items.Add(comma);
                }
            }

            return SyntaxFactory.SeparatedList<ParameterSyntax>(items);
        }

        private static bool HasInitializer(SyntaxNode node)
            => node is ConstructorDeclarationSyntax c && c.Initializer is not null;

        private static ParameterListSyntax? ParamsOf(SyntaxNode node) => node switch {
            MethodDeclarationSyntax m => m.ParameterList,
            ConstructorDeclarationSyntax c => c.ParameterList,
            OperatorDeclarationSyntax o => o.ParameterList,
            ConversionOperatorDeclarationSyntax v => v.ParameterList,
            LocalFunctionStatementSyntax l => l.ParameterList,
            _ => null,
        };

        private static BlockSyntax? BodyOf(SyntaxNode node) => node switch {
            MethodDeclarationSyntax m => m.Body,
            ConstructorDeclarationSyntax c => c.Body,
            OperatorDeclarationSyntax o => o.Body,
            ConversionOperatorDeclarationSyntax v => v.Body,
            LocalFunctionStatementSyntax l => l.Body,
            _ => null,
        };

        private static string GetIndent(SyntaxNode node)
        {
            SyntaxTriviaList lead = node.GetLeadingTrivia();
            for (int i = lead.Count - 1; i >= 0; i--) {
                if (lead[i].IsKind(SyntaxKind.WhitespaceTrivia)) {
                    return lead[i].ToString();
                }

                if (lead[i].IsKind(SyntaxKind.EndOfLineTrivia)) {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        private static string CollapseWhitespace(string s) => Regex.Replace(s, @"\s+", " ").Trim();
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private readonly MemberComparer _comparer;

        public int TypesReordered { get; private set; }
        public int TypesSkipped { get; private set; }

        public Rewriter(ReorderConfig config, string eol)
        {
            _comparer = new MemberComparer(config);
            _config = config;
            _eol = eol;
            _doNotSort = new HashSet<string>(config.DoNotSortKinds, StringComparer.OrdinalIgnoreCase);
        }

        private readonly ReorderConfig _config;
        private readonly string _eol;
        private readonly HashSet<string> _doNotSort;

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            => Reorder((TypeDeclarationSyntax)base.VisitClassDeclaration(node)!);

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
            => Reorder((TypeDeclarationSyntax)base.VisitStructDeclaration(node)!);

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
            => Reorder((TypeDeclarationSyntax)base.VisitRecordDeclaration(node)!);

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            => Reorder((TypeDeclarationSyntax)base.VisitInterfaceDeclaration(node)!);

        private SyntaxNode Reorder(TypeDeclarationSyntax type)
        {
            var members = type.Members;
            if (members.Count < 2) {
                return type;
            }

            if (HasMemberLevelDirectives(type)) {
                if (OnlyRegionDirectives(type)) {
                    // A type whose only directives are #region / #endregion: honor RespectRegions.
                    if (_config.RespectRegions) {
                        TypesSkipped++;
                        return type;
                    }
                    // else: user opted out of region-respecting — fall through and sort across them.
                } else if (_config.SkipTypesWithDirectives) {
                    // #if / #pragma etc. — reordering across these is unsafe; leave the type alone.
                    TypesSkipped++;
                    return type;
                }
            }

            // MEMBER-LEVEL template markers (e.g. FreeCRM's "// {{ModuleItemStart:X}}" between properties)
            // pin members. Reorganize with regions tracked so every member stays wrapped by the SAME
            // region (splitting a region when an outsider lands in the middle of it); malformed markers
            // fall back to leaving the type byte-for-byte alone. Markers that appear ONLY inside member
            // bodies (e.g. around statements in a method) are not member-level — those types fall through
            // to the normal sort below, which moves whole members carrying their in-body markers intact.
            if (_config.SkipTypesWithModuleMarkers && HasMemberLevelModuleMarkers(type)) {
                var regionResult = ModuleRegionSorter.TryReorganize(type, _comparer, _config, _eol);
                if (regionResult is null) {
                    TypesSkipped++;
                    return type;
                }
                if (!ReferenceEquals(regionResult, type)) {
                    TypesReordered++;
                }
                return regionResult;
            }

            // Stable sort: members the rules treat as equal keep their original relative order.
            var ordered = members.OrderBy(m => m, _comparer).ToList();
            if (members.SequenceEqual(ordered)) {
                return type; // already in the desired order — leave the type byte-for-byte alone
            }

            // If sorting would disturb a large fraction of the sortable members, this type is
            // deliberately ordered by purpose (a controller, a nested DTO, a plugin). Leave it alone.
            int sortable = members.Count(m => !_doNotSort.Contains(MemberClassifier.KindOf(m)));
            if (sortable > 0) {
                int moved = 0;
                for (int i = 0; i < ordered.Count; i++) {
                    if (!ReferenceEquals(members[i], ordered[i])) {
                        moved++;
                    }
                }
                if ((double)moved / sortable > _config.MaxFractionReordered) {
                    TypesSkipped++;
                    return type;
                }
            }

            // MINIMAL change: keep every member's ORIGINAL trivia (comments, XML docs, the author's
            // own blank-line spacing). We only nudge the member that ends up first, stripping any
            // leading blank lines so we don't open the type body with an empty line.
            var rebuilt = new List<MemberDeclarationSyntax>(ordered.Count);
            for (int i = 0; i < ordered.Count; i++) {
                var m = ordered[i];
                if (i == 0) {
                    m = m.WithLeadingTrivia(TrimLeadingBlankLines(m.GetLeadingTrivia()));
                }
                rebuilt.Add(m);
            }

            TypesReordered++;
            return type.WithMembers(SyntaxFactory.List(rebuilt));
        }

        private static SyntaxTriviaList TrimLeadingBlankLines(SyntaxTriviaList lead)
        {
            int i = 0;
            while (i < lead.Count) {
                if (lead[i].IsKind(SyntaxKind.EndOfLineTrivia)) {
                    i++;
                    continue;
                }
                // a blank line written as whitespace + newline
                if (lead[i].IsKind(SyntaxKind.WhitespaceTrivia)
                    && i + 1 < lead.Count && lead[i + 1].IsKind(SyntaxKind.EndOfLineTrivia)) {
                    i += 2;
                    continue;
                }
                break;
            }
            return SyntaxFactory.TriviaList(lead.Skip(i));
        }

        private static bool HasMemberLevelDirectives(TypeDeclarationSyntax type)
        {
            if (type.OpenBraceToken.TrailingTrivia.Any(t => t.IsDirective)) {
                return true;
            }
            if (type.CloseBraceToken.LeadingTrivia.Any(t => t.IsDirective)) {
                return true;
            }
            foreach (var m in type.Members) {
                if (m.GetLeadingTrivia().Any(t => t.IsDirective) || m.GetTrailingTrivia().Any(t => t.IsDirective)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// True if EVERY member-level directive in the type is a <c>#region</c> or <c>#endregion</c>
        /// (no <c>#if</c> / <c>#pragma</c> / etc.). Used to decide whether <see cref="ReorderConfig.RespectRegions"/>
        /// governs the type — regions are safe to honor; conditional-compilation directives are not.
        /// </summary>
        private static bool OnlyRegionDirectives(TypeDeclarationSyntax type)
        {
            if (!AllDirectivesAreRegions(type.OpenBraceToken.TrailingTrivia)
                || !AllDirectivesAreRegions(type.CloseBraceToken.LeadingTrivia)) {
                return false;
            }

            foreach (var m in type.Members) {
                if (!AllDirectivesAreRegions(m.GetLeadingTrivia()) || !AllDirectivesAreRegions(m.GetTrailingTrivia())) {
                    return false;
                }
            }

            return true;
        }

        private static bool AllDirectivesAreRegions(SyntaxTriviaList trivia)
        {
            foreach (var t in trivia) {
                if (t.IsDirective
                    && !t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                    && !t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True if there are module/template markers (e.g. FreeCRM's <c>// {{ModuleItemStart:X}}</c>) at
        /// MEMBER level — between members, or just after the open brace / before the close brace. Markers
        /// that live only inside a member's body are intentionally NOT counted: the reorganizer moves
        /// whole members, so those travel along untouched.
        /// </summary>
        private bool HasMemberLevelModuleMarkers(TypeDeclarationSyntax type)
        {
            if (TriviaHasModuleMarker(type.OpenBraceToken.TrailingTrivia) ||
                TriviaHasModuleMarker(type.CloseBraceToken.LeadingTrivia)) {
                return true;
            }

            foreach (var m in type.Members) {
                if (TriviaHasModuleMarker(m.GetLeadingTrivia())) {
                    return true;
                }
            }

            return false;
        }

        private bool TriviaHasModuleMarker(SyntaxTriviaList trivia)
        {
            var tokens = _config.ModuleMarkerTokens;
            if (tokens == null || tokens.Count == 0) {
                return false;
            }

            foreach (var t in trivia) {
                if (!t.IsKind(SyntaxKind.SingleLineCommentTrivia)) {
                    continue;
                }

                string text = t.ToString();
                foreach (var token in tokens) {
                    if (text.IndexOf(token, System.StringComparison.Ordinal) >= 0) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}