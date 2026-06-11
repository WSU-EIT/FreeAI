using System;
using System.Collections.Generic;
using System.Linq;
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

        // Pass 1: reorder members.
        var rewriter = new Rewriter(config);
        var afterReorg = rewriter.Visit(originalRoot);
        if (afterReorg is null) {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, 0, "rewrite produced null");
        }

        // Pass 2: collapse wrapped-parameter braces to "){" (the author's hand style).
        int bracesCollapsed = 0;
        SyntaxNode finalRoot = afterReorg;
        if (config.CollapseWrappedParameterBrace) {
            var braceRewriter = new BraceCollapseRewriter();
            finalRoot = braceRewriter.Visit(afterReorg) ?? afterReorg;
            bracesCollapsed = braceRewriter.BracesCollapsed;
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
    /// When a method/constructor/operator/local-function parameter list is wrapped across multiple
    /// lines, glue the closing ")" and the body's opening "{" together as "){" on one line — the
    /// FreeCRM author's hand style. `dotnet format` splits these to ")" + "{"; this restores them.
    /// Only fires when the parameters are ALREADY wrapped onto multiple lines, so single-line
    /// declarations keep their normal brace-on-new-line (Allman) form.
    /// </summary>
    private sealed class BraceCollapseRewriter : CSharpSyntaxRewriter
    {
        public int BracesCollapsed { get; private set; }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var v = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
            return v.Body is not null && ShouldCollapse(v.ParameterList, v.Body) ? Collapse(v, v.ParameterList, v.Body) : v;
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var v = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
            return v.Body is not null && ShouldCollapse(v.ParameterList, v.Body) ? Collapse(v, v.ParameterList, v.Body) : v;
        }

        public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            var v = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!;
            return v.Body is not null && ShouldCollapse(v.ParameterList, v.Body) ? Collapse(v, v.ParameterList, v.Body) : v;
        }

        public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            var v = (ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!;
            return v.Body is not null && ShouldCollapse(v.ParameterList, v.Body) ? Collapse(v, v.ParameterList, v.Body) : v;
        }

        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var v = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
            return v.Body is not null && ShouldCollapse(v.ParameterList, v.Body) ? Collapse(v, v.ParameterList, v.Body) : v;
        }

        private SyntaxNode Collapse(SyntaxNode node, ParameterListSyntax pl, BlockSyntax body)
        {
            BracesCollapsed++;
            var cp = pl.CloseParenToken;
            var ob = body.OpenBraceToken;
            return node.ReplaceTokens(new[] { cp, ob }, (orig, rewritten) => {
                if (orig == cp) return rewritten.WithTrailingTrivia();
                if (orig == ob) return rewritten.WithLeadingTrivia();
                return rewritten;
            });
        }

        private static bool ShouldCollapse(ParameterListSyntax? pl, BlockSyntax body)
        {
            if (pl is null) {
                return false;
            }
            // Only when the parameter list is wrapped across multiple lines.
            if (!pl.ToString().Contains('\n')) {
                return false;
            }
            var cp = pl.CloseParenToken;
            var ob = body.OpenBraceToken;
            bool onSeparateLines = cp.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia))
                                || ob.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (!onSeparateLines) {
                return false; // already ")" + "{" on the same line
            }
            // Never collapse across a comment sitting between ) and {.
            if (cp.TrailingTrivia.Any(IsComment) || ob.LeadingTrivia.Any(IsComment)) {
                return false;
            }
            return true;
        }

        private static bool IsComment(SyntaxTrivia t)
            => t.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private readonly MemberComparer _comparer;

        public int TypesReordered { get; private set; }
        public int TypesSkipped { get; private set; }

        public Rewriter(ReorderConfig config)
        {
            _comparer = new MemberComparer(config);
            _config = config;
            _doNotSort = new HashSet<string>(config.DoNotSortKinds, StringComparer.OrdinalIgnoreCase);
        }

        private readonly ReorderConfig _config;
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

            if (_config.SkipTypesWithDirectives && HasMemberLevelDirectives(type)) {
                TypesSkipped++;
                return type;
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
    }
}