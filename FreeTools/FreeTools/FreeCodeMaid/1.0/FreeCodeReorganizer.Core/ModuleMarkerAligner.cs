using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FreeCodeReorganizer.Core;

/// <summary>
/// Re-aligns the indentation of FreeCRM template markers so every <c>// {{ModuleItemEnd:X}}</c> sits at
/// the same indentation as its paired <c>// {{ModuleItemStart:X}}</c>. The whitespace cleanup
/// (<c>dotnet format</c>) re-indents these comments to the nearest code statement — which keeps a Start
/// (it precedes a <c>case</c>/statement) but pushes the matching End to the previous statement's deeper
/// indent, breaking the visual pairing the module-extraction tooling relies on. This pass only ever
/// rewrites the leading whitespace of an End-marker line, and only when it can be cleanly paired to an
/// open Start of the same name (malformed/unbalanced markers are left untouched).
/// </summary>
public static class ModuleMarkerAligner
{
    private static readonly Regex Marker = new(
        @"^([ \t]*)//[ \t]*\{\{ModuleItem(Start|End):([^}]*)\}\}[ \t]*$",
        RegexOptions.Compiled);

    public static string Align(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf("{{ModuleItem", StringComparison.Ordinal) < 0)
        {
            return text;
        }

        string[] lines = text.Split('\n');
        var open = new List<(string Name, string Indent)>();
        bool changed = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            string cr = raw.Length > 0 && raw[raw.Length - 1] == '\r' ? "\r" : string.Empty;
            string content = cr.Length > 0 ? raw.Substring(0, raw.Length - 1) : raw;

            Match m = Marker.Match(content);
            if (!m.Success)
            {
                continue;
            }

            string indent = m.Groups[1].Value;
            string name = m.Groups[3].Value.Trim();

            if (m.Groups[2].Value == "Start")
            {
                open.Add((name, indent));
                continue;
            }

            // End marker: pair with the innermost open Start of the same name.
            if (open.Count > 0 && string.Equals(open[open.Count - 1].Name, name, StringComparison.Ordinal))
            {
                string startIndent = open[open.Count - 1].Indent;
                open.RemoveAt(open.Count - 1);

                if (!string.Equals(startIndent, indent, StringComparison.Ordinal))
                {
                    lines[i] = startIndent + content.Substring(indent.Length) + cr;
                    changed = true;
                }
            }

            // Mismatched name / no open Start => malformed; leave it exactly as-is.
        }

        return changed ? string.Join("\n", lines) : text;
    }
}
