using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeCodeClones.Core;

/// <summary>
/// Console table, JSON and Markdown renderings of a <see cref="CloneResult"/>. All three are byte-identical across
/// runs: paths are root-relative with forward slashes, classes and locations are sorted, numbers use the invariant
/// culture, and nothing time-dependent is written unless the caller passes a timestamp.
/// </summary>
public static class CloneReport
{
    private const int ExcerptLines = 12;
    private const int NormalizedChars = 400;

    public static string Kind(CloneKind kind) => kind == CloneKind.Exact ? "exact" : "near";

    public static string ToJson(CloneResult r, string? timestamp = null)
    {
        var j = new JsonWriter();
        j.Open('{');
        j.Property("tool", "FreeCodeClones");
        if (timestamp is not null) {
            j.Property("generated", timestamp);
        }

        j.Property("project", r.ProjectName);
        j.Property("mode", r.Options.Mode == CloneMode.Blind ? "blind" : "structural");
        j.Raw("keepLiterals", r.Options.KeepLiterals ? "true" : "false");
        j.Raw("minTokens", Num(r.Options.MinTokens));
        j.Raw("minStatements", Num(r.Options.MinStatements));
        j.Raw("similarity", Num(r.Options.Similarity));
        j.Raw("files", Num(r.Files.Count));
        j.Raw("units", Num(r.Units));
        j.Raw("exact", Num(r.Classes.Count(c => c.Kind == CloneKind.Exact)));
        j.Raw("near", Num(r.Classes.Count(c => c.Kind == CloneKind.Near)));
        j.Key("classes");
        j.Open('[');
        foreach (CloneClass c in r.Classes) {
            j.Open('{');
            j.Property("id", c.Id);
            j.Property("kind", Kind(c.Kind));
            j.Raw("similarity", Num(c.Similarity));
            j.Raw("tokens", Num(c.Tokens));
            j.Raw("statements", Num(c.Statements));
            j.Raw("occurrences", Num(c.Members.Count));
            j.Raw("size", Num(c.Size));
            j.Property("normalized", Truncate(c.Normalized, NormalizedChars));
            j.Key("locations");
            j.Open('[');
            foreach (CodeUnit u in c.Members) {
                j.Open('{');
                j.Property("path", u.Path);
                j.Raw("startLine", Num(u.StartLine));
                j.Raw("endLine", Num(u.EndLine));
                j.Property("unit", u.Kind);
                j.Close('}');
            }

            j.Close(']');
            j.Close('}');
        }

        j.Close(']');
        j.Close('}');
        return j.ToString();
    }

    public static string ToMarkdown(CloneResult r, string? timestamp = null)
    {
        var sb = new StringBuilder();
        sb.Append("# Clone report: ").Append(r.ProjectName).Append('\n').Append('\n');
        if (timestamp is not null) {
            sb.Append("Generated ").Append(timestamp).Append(".\n\n");
        }

        sb.Append(Settings(r)).Append('\n').Append('\n');
        sb.Append(Summary(r)).Append('\n').Append('\n');
        if (r.Classes.Count == 0) {
            return sb.ToString();
        }

        sb.Append("| # | Kind | Similarity | Tokens | Statements | Occurrences | Size | Id |\n");
        sb.Append("|--:|------|-----------:|-------:|-----------:|------------:|-----:|----|\n");
        for (int i = 0; i < r.Classes.Count; i++) {
            CloneClass c = r.Classes[i];
            sb.Append($"| {i + 1} | {Kind(c.Kind)} | {Num(c.Similarity, "0.00")} | {c.Tokens} | {c.Statements} | {c.Members.Count} | {c.Size} | `{c.Id}` |\n");
        }

        var files = r.Files.ToDictionary(f => f.Path, StringComparer.Ordinal);
        for (int i = 0; i < r.Classes.Count; i++) {
            CloneClass c = r.Classes[i];
            sb.Append('\n').Append($"## {i + 1}. {Kind(c.Kind)}, {c.Tokens} tokens x {c.Members.Count} (`{c.Id}`)\n\n");
            if (c.Kind == CloneKind.Near) {
                sb.Append($"Similarity {Num(c.Similarity, "0.00")} (lowest pair in the class).\n\n");
            }

            foreach (CodeUnit u in c.Members) {
                sb.Append("**`").Append(u.Location).Append("`** (").Append(u.Kind).Append(")\n\n");
                sb.Append("```").Append(u.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ? "razor" : "csharp").Append('\n');
                sb.Append(Excerpt(files[u.Path], u.StartLine, u.EndLine));
                sb.Append("```\n\n");
            }
        }

        return sb.ToString();
    }

    /// <summary>The file name of a proposal's patch inside the patches folder.</summary>
    public static string PatchFileName(FixProposal p) => p.Number.ToString("00", CultureInfo.InvariantCulture) + "-" + p.Clone.Id + ".patch";

    /// <summary>
    /// The fix-proposal report: one section per clone class with the verdict, the reasons, what differs between the
    /// copies, the proposed helper and patch (or a line diff when no patch is offered), and a reviewer checklist.
    /// </summary>
    public static string ProposalsToMarkdown(CloneResult r, IReadOnlyList<FixProposal> proposals, string patchFolder, string? timestamp = null)
    {
        var sb = new StringBuilder();
        int extract = proposals.Count(p => p.Verdict == ProposalVerdict.Extract);
        int review = proposals.Count(p => p.Verdict == ProposalVerdict.Review);
        int leave = proposals.Count(p => p.Verdict == ProposalVerdict.Leave);
        sb.Append("# Fix proposals: ").Append(r.ProjectName).Append("\n\n");
        if (timestamp is not null) {
            sb.Append("Generated ").Append(timestamp).Append(".\n\n");
        }

        sb.Append("Nothing in the scanned code has been changed. Each section below is a suggestion for a person to review: ")
            .Append("**Extract** comes with a patch to try on a branch, **Review** explains why no patch was written, and ")
            .Append("**Leave** marks duplication that is probably intended (add it to the baseline with a reason).\n\n");
        sb.Append($"{proposals.Count} clone class(es): {extract} extract, {review} review, {leave} leave. {Settings(r)}\n\n");
        sb.Append("## Trying a patch\n\n");
        sb.Append("Run these from the root of the scanned repo. Each patch is made against the files as they were scanned and stands alone. ")
            .Append("If two patches touch the same lines (see \"Overlaps\"), apply one, rescan, and generate the proposals again.\n\n");
        sb.Append("```powershell\n");
        sb.Append("git switch -c clones/<number>\n");
        sb.Append($"git apply --check <path-to>/{patchFolder}/<file>.patch   # does it still apply? changes nothing\n");
        sb.Append($"git apply <path-to>/{patchFolder}/<file>.patch\n");
        sb.Append("dotnet build\n");
        sb.Append("dotnet test\n");
        sb.Append("# then rescan: the clone class should be gone, and no new one should appear\n");
        sb.Append("```\n\n");
        sb.Append("To undo an applied patch: `git apply -R <file>.patch`, or `git checkout -- .` on the branch.\n\n");
        sb.Append("| # | Verdict | Risk | Proposal | Copies | Id |\n");
        sb.Append("|--:|---------|------|----------|-------:|----|\n");
        foreach (FixProposal p in proposals) {
            sb.Append($"| {p.Number} | {Verdict(p.Verdict)} | {(p.Risk.Length > 0 ? p.Risk : "-")} | {Cell(p.Title)} | {p.Clone.Members.Count} | `{p.Clone.Id}` |\n");
        }

        foreach (FixProposal p in proposals) {
            CloneClass c = p.Clone;
            sb.Append('\n').Append($"## {p.Number}. {p.Title}\n\n");
            sb.Append($"**{Verdict(p.Verdict)}.** Clone `{c.Id}`, {Kind(c.Kind)}, {c.Tokens} tokens x {c.Members.Count}");
            if (c.Kind == CloneKind.Near) {
                sb.Append($", similarity {Num(c.Similarity, "0.00")}");
            }

            sb.Append(p.Risk.Length > 0 ? $". Risk: **{p.Risk}**." : ".");
            if (p.Patch is not null) {
                sb.Append($" Patch: `{patchFolder}/{PatchFileName(p)}`.");
            }

            sb.Append("\n\n");
            foreach (CodeUnit u in c.Members) {
                sb.Append("- `").Append(u.Location).Append("` (").Append(u.Kind).Append(")\n");
            }

            sb.Append("\n**Why**\n\n");
            foreach (string reason in p.Reasons) {
                sb.Append("- ").Append(reason).Append('\n');
            }

            if (p.Holes.Count > 0) {
                sb.Append("\n**What differs or flows in**\n\n");
                sb.Append("| Role | " + string.Join(" | ", c.Members.Select((m, i) => "Copy " + (i + 1))) + " | Becomes |\n");
                sb.Append("|------|" + string.Concat(c.Members.Select(_ => "------|")) + "---------|\n");
                foreach (CloneHole h in p.Holes) {
                    string becomes = h.Parameter.Length == 0
                        ? (h.Role is "local" or "name" ? "kept as in copy 1 (only used inside)" : "no plain parameter: see Why")
                        : $"parameter `{(h.ByRef ? "ref " : "")}{h.Type ?? "TODO_Type"} {h.Parameter}`";
                    sb.Append($"| {h.Role} | {string.Join(" | ", h.Values.Select(v => "`" + Cell(Truncate(v, 60)) + "`"))} | {becomes} |\n");
                }
            }

            if (p.Notes.Count > 0) {
                sb.Append("\n**Check before merging**\n\n");
                foreach (string note in p.Notes) {
                    sb.Append("- ").Append(note).Append('\n');
                }
            }

            if (p.Overlaps.Count > 0) {
                sb.Append($"\n**Overlaps** proposal(s) {string.Join(", ", p.Overlaps.Select(o => "#" + o))}: apply one, rescan, regenerate.\n");
            }

            if (p.Helper is not null) {
                sb.Append("\n**Proposed helper**\n\n```csharp\n").Append(Dedent(p.Helper)).Append("\n```\n");
            }

            if (p.Patch is not null) {
                sb.Append("\n**Patch**\n\n```diff\n").Append(p.Patch.Replace("\r", "")).Append("```\n");
                sb.Append("\n**Reviewer checklist**\n\n");
                sb.Append("- [ ] The helper's name and parameter names read well\n");
                sb.Append("- [ ] `git apply --check` passes, then the solution builds\n");
                sb.Append("- [ ] Existing tests pass\n");
                sb.Append("- [ ] Behaviour is unchanged: each call site passes what its copy used (check the table above)\n");
                sb.Append($"- [ ] A rescan no longer lists `{c.Id}` and lists nothing new\n");
            } else if (p.LineDiff is not null) {
                sb.Append($"\n**How copy 1 and copy 2 differ** (`-` only in `{c.Members[0].Location}`, `+` only in `{c.Members[1].Location}`)\n\n```diff\n")
                    .Append(p.LineDiff.Replace("```", "` ` `")).Append("```\n");
            }

            if (p.Verdict == ProposalVerdict.Leave) {
                sb.Append($"\nTo accept it, add `{{ \"id\": \"{c.Id}\", \"reason\": \"...\" }}` to the baseline.\n");
            }
        }

        return sb.ToString();

        static string Cell(string s) => s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");

        static string Verdict(ProposalVerdict v) => v == ProposalVerdict.Extract ? "Extract" : v == ProposalVerdict.Review ? "Review" : "Leave";

        static string Dedent(string code)
        {
            string[] lines = code.Replace("\r", "").Split('\n');
            int indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
            return string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()));
        }
    }

    /// <summary>A summary table of clone classes, largest first, each location as path:start-end.</summary>
    public static void WriteConsole(TextWriter w, CloneResult r, IReadOnlyList<CloneClass> classes, string? heading = null)
    {
        w.WriteLine($"FreeCodeClones: {r.ProjectName}. {Settings(r)}");
        w.WriteLine(heading ?? Summary(r));
        if (classes.Count == 0) {
            return;
        }

        w.WriteLine();
        w.WriteLine("    #  Kind   Sim   Tokens  Stmts  Occ    Size  Id");
        for (int i = 0; i < classes.Count; i++) {
            CloneClass c = classes[i];
            w.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,5}  {1,-5}  {2:0.00}  {3,6}  {4,5}  {5,3}  {6,6}  {7}",
                i + 1, Kind(c.Kind), c.Similarity, c.Tokens, c.Statements, c.Members.Count, c.Size, c.Id));
            foreach (CodeUnit u in c.Members) {
                w.WriteLine("         " + u.Location);
            }
        }
    }

    private static string Excerpt(SourceFile file, int startLine, int endLine)
    {
        int last = Math.Min(endLine, startLine + ExcerptLines - 1);
        var lines = new List<string>();
        for (int n = startLine; n <= last && n <= file.Lines.Count; n++) {
            lines.Add(file.Lines[n - 1].Replace("\t", "    "));
        }

        int indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        var sb = new StringBuilder();
        foreach (string l in lines) {
            sb.Append(l.Length >= indent ? l.Substring(indent) : l.TrimStart()).Append('\n');
        }

        if (endLine > last) {
            sb.Append($"// ... {endLine - last} more line(s)\n");
        }

        return sb.ToString().Replace("```", "` ` `");
    }

    private static string Num(double value, string format = "0.####") => value.ToString(format, CultureInfo.InvariantCulture);

    private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Settings(CloneResult r)
    {
        CloneOptions o = r.Options;
        return $"Mode {(o.Mode == CloneMode.Blind ? "blind" : "structural")}, literals {(o.KeepLiterals ? "kept" : "normalised")}, " +
            $"units of {o.MinTokens}+ tokens or {o.MinStatements}+ statements, near-miss at similarity {Num(o.Similarity, "0.00")}+.";
    }

    private static string Summary(CloneResult r)
    {
        int exact = r.Classes.Count(c => c.Kind == CloneKind.Exact);
        return $"{r.Files.Count} files, {r.Units} units: {r.Classes.Count} clone class(es), {exact} exact and {r.Classes.Count - exact} near.";
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + " ...";

    /// <summary>Just enough JSON writing for the report and the baseline: two-space indent, "\n" line ends.</summary>
    internal sealed class JsonWriter
    {
        private readonly StringBuilder _sb = new();
        private readonly Stack<bool> _hasItems = new();
        private bool _afterKey;

        public static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s) {
                switch (c) {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.Append('"').ToString();
        }

        public void Close(char bracket)
        {
            bool had = _hasItems.Pop();
            if (had) {
                NewLine();
            }

            _sb.Append(bracket);
            if (_hasItems.Count == 0) {
                _sb.Append('\n');
            }
        }

        public void Key(string name)
        {
            Item();
            _sb.Append(Quote(name)).Append(": ");
            _afterKey = true;
        }

        public void Open(char bracket)
        {
            Item();
            _sb.Append(bracket);
            _hasItems.Push(false);
        }

        public void Property(string name, string value)
        {
            Key(name);
            Value(Quote(value));
        }

        public void Raw(string name, string literal)
        {
            Key(name);
            Value(literal);
        }

        public override string ToString() => _sb.ToString();

        public void Value(string literal)
        {
            Item();
            _sb.Append(literal);
        }

        private void Item()
        {
            if (_afterKey) {
                _afterKey = false;
                return;
            }

            if (_hasItems.Count == 0) {
                return;
            }

            if (_hasItems.Pop()) {
                _sb.Append(',');
            }

            _hasItems.Push(true);
            NewLine();
        }

        private void NewLine() => _sb.Append('\n').Append(' ', _hasItems.Count * 2);
    }
}