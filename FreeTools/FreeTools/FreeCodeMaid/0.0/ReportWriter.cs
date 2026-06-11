using System.Text;

namespace FreeCodeMaid;

/// <summary>
/// Renders a human-readable Markdown report of exactly what the reorganizer did: a summary, a
/// plain-language "how to redo this by hand" section, and a per-file breakdown with a side-by-side
/// before/after column for each reordered type plus the list of collapsed braces.
/// </summary>
public static class ReportWriter
{
    // Types larger than this only show moved members plus one unmoved neighbor on each side.
    private const int LargeTypeThreshold = 30;

    public static string Build(
        string target,
        bool apply,
        int filesChanged,
        int typesReordered,
        int bracesCollapsed,
        IReadOnlyList<(string RelativePath, ReorgResult Result)> entries)
    {
        var sb = new StringBuilder();
        string mode = apply ? "applied" : "dry-run";

        sb.AppendLine("# FreeCodeReorganizer — change report");
        sb.AppendLine($"{target}  ·  {mode}  ·  generated this run");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine($"- Files changed: {filesChanged}");
        sb.AppendLine($"- Types reordered: {typesReordered}");
        sb.AppendLine($"- Braces collapsed: {bracesCollapsed}");
        sb.AppendLine();

        sb.AppendLine("## How to verify or redo this by hand");
        sb.AppendLine("- **Member order:** within each type, properties + methods are sorted A→Z by name "
            + "(ignoring a leading `_`, and ignoring public vs private). Fields, constructors, and nested "
            + "types keep their original order. A type is left untouched if sorting would move >35% of its "
            + "members (deliberately by-purpose) or if it contains `#region`/`#if` directives.");
        sb.AppendLine("- **Brace:** when a parameter list is wrapped across multiple lines, the closing `)` "
            + "and the body `{` are joined onto one line as `){`.");
        sb.AppendLine();

        sb.AppendLine("## Per-file changes");

        foreach (var (relativePath, result) in entries)
        {
            sb.AppendLine($"### `{relativePath.Replace('\\', '/')}`");

            foreach (var reorder in result.Reorders)
            {
                int moved = CountMoved(reorder.Before, reorder.After);
                sb.AppendLine($"#### type `{reorder.TypeName}` — {moved} of {reorder.Before.Count} members reordered");
                sb.AppendLine("```text");
                foreach (var line in RenderColumns(reorder.Before, reorder.After))
                {
                    sb.AppendLine(line);
                }
                sb.AppendLine("```");
            }

            if (result.BraceMethods.Count > 0)
            {
                sb.AppendLine("#### braces");
                foreach (var name in result.BraceMethods)
                {
                    sb.AppendLine($"- joined wrapped `)` `{{` into `){{` in `{name}`");
                }
            }
        }

        return sb.ToString();
    }

    private static int CountMoved(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        int rows = Math.Max(before.Count, after.Count);
        int moved = 0;
        for (int i = 0; i < rows; i++)
        {
            if (MovedAt(before, after, i))
            {
                moved++;
            }
        }
        return moved;
    }

    /// <summary>
    /// Produces the monospace before/after block. A row is marked with a leading "→ " when the
    /// member at that index differs between the before and after order (i.e. something moved into
    /// or out of that slot). Unmoved rows get 3 leading spaces so the two columns stay aligned.
    /// </summary>
    private static List<string> RenderColumns(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        int rows = Math.Max(before.Count, after.Count);
        var moved = new bool[rows];
        for (int i = 0; i < rows; i++)
        {
            moved[i] = MovedAt(before, after, i);
        }
        // The same per-slot flag drives both columns: a slot that differs is marked on both sides.
        var beforeMoved = moved;
        var afterMoved = moved;

        // Left column width: longest "before" name + 4 (so the "after" column lines up).
        int longest = 0;
        foreach (var name in before)
        {
            if (name.Length > longest)
            {
                longest = name.Length;
            }
        }
        int leftWidth = longest + 4;

        var lines = new List<string>();

        // Header row (column titles + dashed rule), aligned to the same columns.
        const string headerLeft = "before";
        const string headerRight = "after";
        string dashes = new string('-', Math.Max(headerLeft.Length, 22));
        lines.Add("   " + Pad(headerLeft, leftWidth) + headerRight);
        lines.Add("   " + Pad(dashes, leftWidth) + dashes);

        bool large = rows > LargeTypeThreshold;
        bool[] show = large ? ChooseVisibleRows(rows, beforeMoved, afterMoved) : null!;

        int hidden = 0;
        bool pendingEllipsis = false;
        for (int i = 0; i < rows; i++)
        {
            if (large && !show[i])
            {
                hidden++;
                pendingEllipsis = true;
                continue;
            }
            if (pendingEllipsis)
            {
                lines.Add($"   … ({hidden} unchanged rows hidden)");
                hidden = 0;
                pendingEllipsis = false;
            }

            string left = i < before.Count ? before[i] : "";
            string right = i < after.Count ? after[i] : "";
            string leftPrefix = (i < before.Count && beforeMoved[i]) ? "→ " : "   ";
            string rightPrefix = (i < after.Count && afterMoved[i]) ? "→ " : "   ";
            lines.Add(leftPrefix + Pad(left, leftWidth) + rightPrefix + right);
        }
        if (pendingEllipsis)
        {
            lines.Add($"   … ({hidden} unchanged rows hidden)");
        }

        return lines;
    }

    /// <summary>
    /// For a large type, show only moved rows plus one unmoved neighbor above and below each moved
    /// run, so the reader sees what moved in context without an overwhelming wall of names.
    /// </summary>
    private static bool[] ChooseVisibleRows(int rows, bool[] beforeMoved, bool[] afterMoved)
    {
        var show = new bool[rows];
        for (int i = 0; i < rows; i++)
        {
            bool moved = (i < beforeMoved.Length && beforeMoved[i]) || (i < afterMoved.Length && afterMoved[i]);
            if (moved)
            {
                show[i] = true;
                if (i - 1 >= 0)
                {
                    show[i - 1] = true;
                }
                if (i + 1 < rows)
                {
                    show[i + 1] = true;
                }
            }
        }
        return show;
    }

    private static string Pad(string s, int width) => s.Length >= width ? s : s + new string(' ', width - s.Length);

    // True when slot i holds a different member name in the before vs after order.
    private static bool MovedAt(IReadOnlyList<string> before, IReadOnlyList<string> after, int i)
    {
        string b = i < before.Count ? before[i] : "";
        string a = i < after.Count ? after[i] : "";
        return !string.Equals(b, a, StringComparison.Ordinal);
    }
}
