using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FreeCodeReorganizer.Core;

/// <summary>Result of reorganizing the C# code blocks of a Razor (.razor/.cshtml) file.</summary>
public readonly record struct RazorReorgResult(
    string? NewText,
    bool Changed,
    int BlocksReorganized,
    string? Error);

/// <summary>
/// Reorganizes the members inside a Razor file's <c>@code { }</c> / <c>@functions { }</c> blocks, and
/// NOTHING else. Every byte of markup (HTML, directives, @expressions, whitespace) is preserved, so the
/// rendered output cannot change — the only edits are member reorderings within the C# code blocks.
///
/// It reuses the same <see cref="Reorganizer"/> engine as plain C#, but with a Blazor-tuned profile:
/// properties (i.e. [Parameter]s) form one sorted group and methods form a separate sorted group,
/// instead of the .cs convention of interleaving them. That matches how Razor @code is conventionally
/// laid out (parameters together, then methods together) so reorganizing never buries a method in the
/// middle of the parameter list.
/// </summary>
public sealed class RazorReorganizer
{
    // Matches the @code or @functions directive keyword. The opening brace may follow on the same or a
    // later line, so we scan whitespace to the '{' separately.
    private static readonly Regex CodeDirective = new(@"@(?:code|functions)\b", RegexOptions.Compiled);

    public static RazorReorgResult Run(string razorText, ReorderConfig config, string eol)
    {
        try
        {
            List<(int open, int close)> blocks = FindCodeBlocks(razorText);
            if (blocks.Count == 0)
            {
                return new RazorReorgResult(razorText, false, 0, null);
            }

            ReorderConfig razorConfig = ToRazorProfile(config);

            string text = razorText;
            int reorganized = 0;

            // Splice from the LAST block to the FIRST so earlier offsets stay valid as we edit.
            for (int b = blocks.Count - 1; b >= 0; b--)
            {
                (int open, int close) = blocks[b];
                string body = text.Substring(open + 1, close - open - 1);

                // Wrap the block body in a throwaway type so the C# engine sees real members.
                string wrapped = "class __FreeCodeReorganizerWrap__" + eol + "{" + body + "}";
                ReorgResult r = Reorganizer.Run(wrapped, razorConfig, eol);
                if (r.Error is not null || !r.Changed || r.NewText is null)
                {
                    continue;
                }

                string? newBody = ExtractInner(r.NewText);
                if (newBody is null || newBody == body)
                {
                    continue;
                }

                text = text.Substring(0, open + 1) + newBody + text.Substring(close);
                reorganized++;
            }

            bool changed = text != razorText;
            return new RazorReorgResult(changed ? text : razorText, changed, reorganized, null);
        }
        catch (Exception ex)
        {
            return new RazorReorgResult(razorText, false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Derives a Blazor @code profile from the user's config: same toggles (sort, underscore, brace,
    /// threshold) but Property/Indexer and Method become SEPARATE sorted groups rather than one
    /// interleaved group.
    /// </summary>
    private static ReorderConfig ToRazorProfile(ReorderConfig config)
    {
        return new ReorderConfig
        {
            SortAlphabetically = config.SortAlphabetically,
            IgnoreLeadingUnderscoreInSort = config.IgnoreLeadingUnderscoreInSort,
            GroupByVisibility = config.GroupByVisibility,
            StaticMembersFirst = config.StaticMembersFirst,
            CollapseWrappedParameterBrace = config.CollapseWrappedParameterBrace,
            MaxFractionReordered = config.MaxFractionReordered,
            SkipTypesWithDirectives = config.SkipTypesWithDirectives,
            VisibilityOrder = config.VisibilityOrder,
            KindOrder =
            [
                "Const",
                "Field",
                "Constructor,Destructor",
                "Delegate,Event",
                "Property,Indexer",   // [Parameter]s etc. — their own sorted group
                "Method",             // methods — their own sorted group, AFTER properties
                "Operator,ConversionOperator",
                "Enum,Interface,Struct,Class,Record"
            ],
            // Everything stays put EXCEPT Property, Indexer and Method, which get sorted.
            DoNotSortKinds =
            [
                "Const", "Field", "Constructor", "Destructor", "Delegate", "Event",
                "Operator", "ConversionOperator", "Enum", "Interface", "Struct", "Class", "Record"
            ],
        };
    }

    /// <summary>Finds the (openBraceIndex, closeBraceIndex) of every @code/@functions block.</summary>
    private static List<(int open, int close)> FindCodeBlocks(string text)
    {
        var result = new List<(int, int)>();
        foreach (Match m in CodeDirective.Matches(text))
        {
            int i = m.Index + m.Length;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= text.Length || text[i] != '{')
            {
                continue;
            }

            int close = FindMatchingBrace(text, i);
            if (close > i)
            {
                result.Add((i, close));
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the brace that closes the one at <paramref name="openBraceIndex"/>, using the C# lexer so
    /// braces inside strings, comments, char literals and interpolations don't fool the count.
    /// </summary>
    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        string sub = text.Substring(openBraceIndex);
        int depth = 0;
        foreach (SyntaxToken token in SyntaxFactory.ParseTokens(sub))
        {
            if (token.IsKind(SyntaxKind.OpenBraceToken))
            {
                depth++;
            }
            else if (token.IsKind(SyntaxKind.CloseBraceToken))
            {
                depth--;
                if (depth == 0)
                {
                    return openBraceIndex + token.SpanStart;
                }
            }
        }

        return -1;
    }

    /// <summary>Returns the text between the first '{' and the last '}' of the wrapped output.</summary>
    private static string? ExtractInner(string wrappedOutput)
    {
        int first = wrappedOutput.IndexOf('{');
        int last = wrappedOutput.LastIndexOf('}');
        if (first < 0 || last <= first)
        {
            return null;
        }

        return wrappedOutput.Substring(first + 1, last - first - 1);
    }
}
