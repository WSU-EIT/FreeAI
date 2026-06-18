using System;
using System.Collections.Generic;
using System.Text;
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

            // Markup pass: re-indent (or collapse) elements whose attributes are wrapped across lines.
            // This only ever rewrites leading whitespace INSIDE a start tag, so rendered output cannot
            // change; a strict "only whitespace differs" guard backs that up before we accept the result.
            if (config.IndentWrappedRazorAttributes)
            {
                string reindented = ReindentMarkupAttributes(text, config);
                if (StripWhitespace(reindented) == StripWhitespace(text))
                {
                    text = reindented;
                }
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
            MaxLineWidth = config.MaxLineWidth,
            MaxFractionReordered = config.MaxFractionReordered,
            SkipTypesWithDirectives = config.SkipTypesWithDirectives,
            RespectRegions = config.RespectRegions,
            SkipTypesWithModuleMarkers = config.SkipTypesWithModuleMarkers,
            ModuleMarkerTokens = config.ModuleMarkerTokens,
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

    // ---- Markup attribute re-indentation -----------------------------------------------------------

    // An attribute on its own line: name="..." / name='...' / name=value, or a bare boolean attribute,
    // or the closing ">" / "/>" of the start tag. Quote nesting inside the value is irrelevant — we
    // detect lines structurally and only ever rewrite their leading whitespace.
    private static readonly Regex AttrNameEquals = new(@"^[@A-Za-z_:][\w:.\-]*\s*=", RegexOptions.Compiled);
    private static readonly Regex AttrBoolOrName = new(@"^[@A-Za-z_:][\w:.\-]*\s*/?>?$", RegexOptions.Compiled);
    private static readonly Regex TagOpenStart = new(@"^<[A-Za-z][\w.]*", RegexOptions.Compiled);

    /// <summary>
    /// Re-indents the continuation attribute lines of any start tag whose attributes are wrapped across
    /// multiple lines, so each sits at the element's indent + two levels ("double tab"). If the whole tag
    /// would fit on one line within <see cref="ReorderConfig.MaxLineWidth"/>, it is collapsed instead.
    /// Lines inside @code / @functions blocks are left alone. Only leading whitespace is ever changed.
    /// </summary>
    private static string ReindentMarkupAttributes(string text, ReorderConfig config)
    {
        List<(int open, int close)> blocks = FindCodeBlocks(text);
        string[] lines = text.Split('\n');
        int[] starts = new int[lines.Length];
        int offset = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            starts[i] = offset;
            offset += lines[i].Length + 1; // + the '\n' we split on
        }

        bool InCode(int lineIndex)
        {
            int s = starts[lineIndex];
            foreach ((int open, int close) in blocks)
            {
                if (s > open && s < close)
                {
                    return true;
                }
            }

            return false;
        }

        var output = new List<string>(lines.Length);
        int li = 0;
        while (li < lines.Length)
        {
            string raw = lines[li];
            string content = TrimCr(raw, out string cr);

            if (!InCode(li) && IsTagOpener(content, out string baseIndent))
            {
                var group = new List<int>();
                bool closed = false;
                int j = li + 1;
                while (j < lines.Length && !InCode(j))
                {
                    string trimmed = TrimCr(lines[j], out _).Trim();
                    if (!IsAttributeLine(trimmed))
                    {
                        break;
                    }

                    group.Add(j);
                    if (trimmed.EndsWith(">", StringComparison.Ordinal))
                    {
                        closed = true;
                        j++;
                        break;
                    }

                    j++;
                }

                if (closed && group.Count > 0)
                {
                    string unit = baseIndent.Contains("\t") ? "\t" : "    ";

                    var single = new StringBuilder(content.Trim());
                    foreach (int g in group)
                    {
                        single.Append(' ').Append(TrimCr(lines[g], out _).Trim());
                    }

                    if (baseIndent.Length + single.Length <= config.MaxLineWidth)
                    {
                        // Collapse the whole start tag onto one line.
                        output.Add(baseIndent + single + cr);
                    }
                    else
                    {
                        // Keep wrapped; double-tab every continuation line.
                        output.Add(raw);
                        string doubleTab = baseIndent + unit + unit;
                        foreach (int g in group)
                        {
                            string gContent = TrimCr(lines[g], out string gcr);
                            output.Add(doubleTab + gContent.TrimStart() + gcr);
                        }
                    }

                    li = j;
                    continue;
                }
            }

            output.Add(raw);
            li++;
        }

        return string.Join("\n", output);
    }

    /// <summary>A line that opens an element tag and does NOT close it on the same line.</summary>
    private static bool IsTagOpener(string content, out string baseIndent)
    {
        baseIndent = LeadingWhitespace(content);
        string trimmed = content.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '<')
        {
            return false;
        }

        if (trimmed.StartsWith("</", StringComparison.Ordinal)
            || trimmed.StartsWith("<!", StringComparison.Ordinal)
            || trimmed.StartsWith("<?", StringComparison.Ordinal))
        {
            return false;
        }

        if (!TagOpenStart.IsMatch(trimmed))
        {
            return false;
        }

        // If the tag already closes on this line, it is single-line — leave it alone.
        return !trimmed.EndsWith(">", StringComparison.Ordinal);
    }

    private static bool IsAttributeLine(string trimmed)
    {
        if (trimmed == ">" || trimmed == "/>")
        {
            return true;
        }

        return AttrNameEquals.IsMatch(trimmed) || AttrBoolOrName.IsMatch(trimmed);
    }

    private static string LeadingWhitespace(string s)
    {
        int i = 0;
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t'))
        {
            i++;
        }

        return s.Substring(0, i);
    }

    private static string TrimCr(string s, out string cr)
    {
        if (s.Length > 0 && s[s.Length - 1] == '\r')
        {
            cr = "\r";
            return s.Substring(0, s.Length - 1);
        }

        cr = string.Empty;
        return s;
    }

    private static string StripWhitespace(string s) => Regex.Replace(s, @"\s+", string.Empty);
}
