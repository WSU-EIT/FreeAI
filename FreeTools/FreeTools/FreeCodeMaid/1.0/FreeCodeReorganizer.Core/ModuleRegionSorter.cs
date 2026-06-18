using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreeCodeReorganizer.Core;

/// <summary>
/// Reorganizes a type whose members are wrapped by paired template markers
/// (<c>// {{ModuleItemStart:X}}</c> / <c>// {{ModuleItemEnd:X}}</c>) WITHOUT breaking the pairing.
///
/// Each member is tagged with the stack of regions it lives in. After sorting, the markers are
/// regenerated from those tags: a member always ends up wrapped by the same region(s) it started in,
/// and when the sort drops an outsider into the middle of a region, that region is SPLIT — an extra
/// End+Start pair is emitted around the outsider. If the existing markers are malformed (an End that
/// doesn't match the open Start, or an unbalanced stack — e.g. the already-scrambled DeletedRecordCounts
/// in FreeCRM), this returns null and the caller leaves the type byte-for-byte alone.
/// </summary>
internal static class ModuleRegionSorter
{
    private static readonly Regex MarkerComment = new(
        @"^[ \t]*//[ \t]*\{\{ModuleItem(Start|End):([^}]*)\}\}[ \t]*$",
        RegexOptions.Compiled);

    private static readonly Regex LeadingBlankLines = new(@"\A(?:[ \t]*\r?\n)+", RegexOptions.Compiled);

    /// <summary>
    /// Returns the reorganized type (markers preserved/regenerated), the ORIGINAL type if nothing needs
    /// to move, or null if the markers are malformed / too disruptive to sort safely (caller skips).
    /// </summary>
    public static TypeDeclarationSyntax? TryReorganize(
        TypeDeclarationSyntax type,
        IComparer<MemberDeclarationSyntax> comparer,
        ReorderConfig config,
        string eol)
    {
        List<MemberDeclarationSyntax> members = type.Members.ToList();
        if (members.Count < 2)
        {
            return type;
        }

        // 1. Tag every member with the region stack it sits in; bail on malformed markers.
        List<List<string>>? tags = ComputeTags(type, members);
        if (tags is null)
        {
            return null;
        }

        // 2. Stable sort by the same comparer the normal path uses.
        List<int> ordered = Enumerable.Range(0, members.Count)
            .OrderBy(i => members[i], comparer)
            .ToList();

        bool changed = ordered.Where((idx, pos) => idx != pos).Any();
        if (!changed)
        {
            return type; // already in order — leave byte-for-byte
        }

        // 3. By-purpose guard: if the sort would move too much, treat the type as hand-ordered and skip.
        var doNotSort = new HashSet<string>(config.DoNotSortKinds, StringComparer.OrdinalIgnoreCase);
        int sortable = members.Count(m => !doNotSort.Contains(MemberClassifier.KindOf(m)));
        if (sortable > 0)
        {
            int moved = ordered.Where((idx, pos) => idx != pos).Count();
            if ((double)moved / sortable > config.MaxFractionReordered)
            {
                return null;
            }
        }

        // 4. Each member's text with ONLY its member-level (leading-trivia) markers removed. Markers
        //    INSIDE a member's body (e.g. // {{ModuleItemStart}} wrapping statements in a method) are
        //    left completely untouched — they travel with the member.
        List<string> cleanTexts = members
            .Select(m => m.WithLeadingTrivia(StripMarkerTrivia(m.GetLeadingTrivia())).ToFullString())
            .ToList();
        string indent = DetectIndent(members);

        // 5. Re-emit the body in sorted order, opening/closing regions as the tag stack changes.
        var sb = new StringBuilder();
        var openStack = new List<string>();
        bool first = true;
        foreach (int idx in ordered)
        {
            List<string> tag = tags[idx];
            int common = CommonPrefixLength(openStack, tag);
            for (int i = openStack.Count - 1; i >= common; i--)
            {
                sb.Append(indent).Append("// {{ModuleItemEnd:").Append(openStack[i]).Append("}}").Append(eol);
            }

            for (int i = common; i < tag.Count; i++)
            {
                sb.Append(indent).Append("// {{ModuleItemStart:").Append(tag[i]).Append("}}").Append(eol);
            }

            openStack = new List<string>(tag);

            string text = cleanTexts[idx];
            if (first)
            {
                text = LeadingBlankLines.Replace(text, string.Empty); // don't open the type body with blank lines
                first = false;
            }

            sb.Append(text);
        }

        // Regions still open after the last member close just before the brace.
        List<string> finalEnds = openStack;

        // 6. Parse the regenerated body back into members.
        string wrapperText = "class __FCRWrap__" + eol + "{" + eol + sb + "}";
        if (SyntaxFactory.ParseMemberDeclaration(wrapperText) is not TypeDeclarationSyntax wrapper
            || wrapper.Members.Count != members.Count)
        {
            return null; // safety: don't risk a malformed rebuild
        }

        // 7. Rebuild the type: strip stray markers off the braces, attach the final End markers to the
        //    close brace, and swap in the regenerated members.
        SyntaxTriviaList closeLeading = StripMarkerTrivia(type.CloseBraceToken.LeadingTrivia);
        var closeTrivia = new List<SyntaxTrivia>();
        for (int i = finalEnds.Count - 1; i >= 0; i--)
        {
            closeTrivia.Add(SyntaxFactory.Whitespace(indent));
            closeTrivia.Add(SyntaxFactory.Comment("// {{ModuleItemEnd:" + finalEnds[i] + "}}"));
            closeTrivia.Add(SyntaxFactory.EndOfLine(eol));
        }

        closeTrivia.AddRange(closeLeading);

        TypeDeclarationSyntax newType = type
            .WithOpenBraceToken(type.OpenBraceToken.WithTrailingTrivia(StripMarkerTrivia(type.OpenBraceToken.TrailingTrivia)))
            .WithMembers(wrapper.Members)
            .WithCloseBraceToken(type.CloseBraceToken.WithLeadingTrivia(SyntaxFactory.TriviaList(closeTrivia)));

        // 8. Self-check: the regenerated markers must themselves be well-formed and preserve every
        //    region name. If anything is off, skip rather than risk a broken pairing.
        if (ComputeTags(newType, newType.Members.ToList()) is not { } newTags
            || !SameRegionNames(tags, newTags))
        {
            return null;
        }

        return newType;
    }

    /// <summary>Walks the markers as a stack; returns each member's region stack, or null if malformed.</summary>
    private static List<List<string>>? ComputeTags(TypeDeclarationSyntax type, List<MemberDeclarationSyntax> members)
    {
        var stack = new List<string>();
        var tags = new List<List<string>>(members.Count);

        if (!ProcessMarkers(type.OpenBraceToken.TrailingTrivia, stack))
        {
            return null;
        }

        foreach (MemberDeclarationSyntax m in members)
        {
            if (!ProcessMarkers(m.GetLeadingTrivia(), stack))
            {
                return null;
            }

            tags.Add(new List<string>(stack));
        }

        if (!ProcessMarkers(type.CloseBraceToken.LeadingTrivia, stack) || stack.Count != 0)
        {
            return null;
        }

        return tags;
    }

    private static bool ProcessMarkers(SyntaxTriviaList trivia, List<string> stack)
    {
        foreach (SyntaxTrivia t in trivia)
        {
            if (!t.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            Match m = MarkerComment.Match(t.ToString());
            if (!m.Success)
            {
                continue;
            }

            string name = m.Groups[2].Value.Trim();
            if (m.Groups[1].Value == "Start")
            {
                stack.Add(name);
            }
            else
            {
                if (stack.Count == 0 || !string.Equals(stack[stack.Count - 1], name, StringComparison.Ordinal))
                {
                    return false; // End without a matching open Start — malformed
                }

                stack.RemoveAt(stack.Count - 1);
            }
        }

        return true;
    }

    private static bool SameRegionNames(List<List<string>> a, List<List<string>> b)
    {
        var sa = new SortedSet<string>(a.SelectMany(x => x), StringComparer.Ordinal);
        var sb = new SortedSet<string>(b.SelectMany(x => x), StringComparer.Ordinal);
        return sa.SetEquals(sb);
    }

    private static int CommonPrefixLength(List<string> a, List<string> b)
    {
        int n = Math.Min(a.Count, b.Count), i = 0;
        while (i < n && string.Equals(a[i], b[i], StringComparison.Ordinal))
        {
            i++;
        }

        return i;
    }

    private static string DetectIndent(List<MemberDeclarationSyntax> members)
    {
        SyntaxTriviaList lead = members[0].GetLeadingTrivia();
        for (int i = lead.Count - 1; i >= 0; i--)
        {
            if (lead[i].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return lead[i].ToString();
            }
        }

        return "        ";
    }

    private static SyntaxTriviaList StripMarkerTrivia(SyntaxTriviaList trivia)
    {
        var result = new List<SyntaxTrivia>();
        for (int i = 0; i < trivia.Count; i++)
        {
            SyntaxTrivia t = trivia[i];
            if (t.IsKind(SyntaxKind.SingleLineCommentTrivia) && MarkerComment.IsMatch(t.ToString()))
            {
                if (result.Count > 0 && result[result.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    result.RemoveAt(result.Count - 1);
                }

                if (i + 1 < trivia.Count && trivia[i + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    i++;
                }

                continue;
            }

            result.Add(t);
        }

        return SyntaxFactory.TriviaList(result);
    }
}
