using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreeCodeMaid;

/// <summary>Outcome of reorganizing a single source file.</summary>
public readonly record struct ReorgResult(
    string? NewText,
    bool Changed,
    int TypesReordered,
    int TypesSkipped,
    string? Error);

/// <summary>
/// Reorganizes the members of every type in a C# source string using Roslyn, moving each member
/// together with its attached comment / XML-doc block. It only rewrites a type when the member
/// ORDER actually changes, leaves whitespace-only concerns to the formatter, and refuses to write
/// any result that fails the safety check (a member went missing, or a syntax error was introduced).
/// </summary>
public sealed class Reorganizer
{
    public static ReorgResult Run(string sourceText, ReorderConfig config, string eol)
    {
        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(sourceText);
        }
        catch (Exception ex)
        {
            return new ReorgResult(null, false, 0, 0, "parse error: " + ex.Message);
        }

        var originalRoot = tree.GetRoot();

        // If the original file already has syntax errors, don't touch it.
        bool originalHadErrors = tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

        var rewriter = new Rewriter(config, eol);
        var newRoot = rewriter.Visit(originalRoot);
        if (newRoot is null)
        {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, "rewrite produced null");
        }

        if (rewriter.TypesReordered == 0)
        {
            // Nothing was reordered (already in order, or every type was skipped).
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped, null);
        }

        var newText = newRoot.ToFullString();

        // Safety net 1: the result must parse, and must not have NEW errors.
        var newTree = CSharpSyntaxTree.ParseText(newText);
        bool newHasErrors = newTree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
        if (newHasErrors && !originalHadErrors)
        {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped,
                "aborted: reorder would introduce a syntax error");
        }

        // Safety net 2: the exact same set of members must still be present (none lost/duplicated).
        if (!MembersPreserved(originalRoot, newTree.GetRoot()))
        {
            return new ReorgResult(null, false, 0, rewriter.TypesSkipped,
                "aborted: member set changed (safety check failed)");
        }

        return new ReorgResult(newText, true, rewriter.TypesReordered, rewriter.TypesSkipped, null);
    }

    private static bool MembersPreserved(SyntaxNode oldRoot, SyntaxNode newRoot)
    {
        var a = CollectSignatures(oldRoot);
        var b = CollectSignatures(newRoot);
        if (a.Count != b.Count)
        {
            return false;
        }
        a.Sort(StringComparer.Ordinal);
        b.Sort(StringComparer.Ordinal);
        for (int i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }

    // Order-independent signature so reordering does not flag a difference, but a dropped or
    // duplicated member does.
    private static List<string> CollectSignatures(SyntaxNode root)
    {
        var list = new List<string>();
        foreach (var m in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            var sig = MemberClassifier.KindOf(m) + "|" + MemberClassifier.NameOf(m);
            if (m is BaseMethodDeclarationSyntax bm)
            {
                sig += "|(" + string.Join(",", bm.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? "?")) + ")";
            }
            list.Add(sig);
        }
        return list;
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        private readonly ReorderConfig _config;
        private readonly SyntaxTrivia _newline;
        private readonly MemberComparer _comparer;

        public int TypesReordered { get; private set; }
        public int TypesSkipped { get; private set; }

        public Rewriter(ReorderConfig config, string eol)
        {
            _config = config;
            _newline = SyntaxFactory.EndOfLine(eol);
            _comparer = new MemberComparer(config);
        }

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
            if (members.Count < 2)
            {
                return type;
            }

            if (_config.SkipTypesWithDirectives && HasMemberLevelDirectives(type))
            {
                TypesSkipped++;
                return type;
            }

            // Stable sort: equal members keep their original relative order.
            var ordered = members.OrderBy(m => m, _comparer).ToList();
            if (members.SequenceEqual(ordered))
            {
                return type; // already in the desired order
            }

            var rebuilt = new List<MemberDeclarationSyntax>(ordered.Count);
            for (int i = 0; i < ordered.Count; i++)
            {
                var m = ordered[i];
                var kept = ExtractKeptLeading(m);

                SyntaxTriviaList lead;
                if (i == 0)
                {
                    lead = kept;
                }
                else
                {
                    int blanks = BlankLinesBefore(ordered[i - 1], m);
                    lead = SyntaxFactory.TriviaList(Enumerable.Repeat(_newline, blanks).Concat(kept));
                }

                rebuilt.Add(NormalizeTrailing(m.WithLeadingTrivia(lead)));
            }

            TypesReordered++;
            return type.WithMembers(SyntaxFactory.List(rebuilt));
        }

        private int BlankLinesBefore(MemberDeclarationSyntax prev, MemberDeclarationSyntax cur)
        {
            // Keep consecutive single-line members of the same kind dense (the field block, and
            // simple auto-property DTOs). Everything else gets the normal between-member gap.
            bool dense = IsCompact(prev) && IsCompact(cur)
                && MemberClassifier.KindOf(prev) == MemberClassifier.KindOf(cur);
            return dense ? _config.BlankLinesBetweenFields : _config.BlankLinesBetweenMembers;
        }

        private MemberDeclarationSyntax NormalizeTrailing(MemberDeclarationSyntax m)
        {
            var trail = m.GetTrailingTrivia();
            int end = trail.Count;
            while (end > 0 &&
                   (trail[end - 1].IsKind(SyntaxKind.EndOfLineTrivia) || trail[end - 1].IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                end--;
            }
            var kept = trail.Take(end).Append(_newline);
            return m.WithTrailingTrivia(SyntaxFactory.TriviaList(kept));
        }

        // Keep the member's attached comment/doc block (with its indentation) and its own indent,
        // but drop the leading blank-line separators — those are regenerated as the gap between members.
        private static SyntaxTriviaList ExtractKeptLeading(MemberDeclarationSyntax m)
        {
            var lead = m.GetLeadingTrivia();

            int firstComment = -1;
            for (int i = 0; i < lead.Count; i++)
            {
                if (IsComment(lead[i]))
                {
                    firstComment = i;
                    break;
                }
            }

            if (firstComment >= 0)
            {
                int start = firstComment;
                if (start > 0 && lead[start - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    start--; // include the indentation in front of the comment
                }
                return SyntaxFactory.TriviaList(lead.Skip(start));
            }

            // No comment: keep only the trailing whitespace run (the member's own indentation).
            int wsStart = lead.Count;
            while (wsStart > 0 && lead[wsStart - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                wsStart--;
            }
            return SyntaxFactory.TriviaList(lead.Skip(wsStart));
        }

        private static bool HasMemberLevelDirectives(TypeDeclarationSyntax type)
        {
            if (type.OpenBraceToken.TrailingTrivia.Any(t => t.IsDirective))
            {
                return true;
            }
            if (type.CloseBraceToken.LeadingTrivia.Any(t => t.IsDirective))
            {
                return true;
            }
            foreach (var m in type.Members)
            {
                if (m.GetLeadingTrivia().Any(t => t.IsDirective) || m.GetTrailingTrivia().Any(t => t.IsDirective))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCompact(MemberDeclarationSyntax m)
            => m.ToString().IndexOf('\n') < 0;

        private static bool IsComment(SyntaxTrivia t)
            => t.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
    }
}
