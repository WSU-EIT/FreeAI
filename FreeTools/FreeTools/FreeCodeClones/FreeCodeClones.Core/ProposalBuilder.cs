using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FreeCodeClones.Core;

/// <summary>What a person reviewing the clone is advised to do with it.</summary>
public enum ProposalVerdict
{
    /// <summary>A shared helper can be written mechanically; a patch is attached.</summary>
    Extract,

    /// <summary>Maybe worth fixing, but it needs a person: the reasons say why no patch was written.</summary>
    Review,

    /// <summary>Probably intended (framework file, template stub): baseline it with a reason.</summary>
    Leave
}

/// <summary>One thing that differs between the copies of a clone (or flows into them), and what a helper does with it.</summary>
public sealed class CloneHole
{
    internal CloneHole(string role, string parameter, string? type, bool byRef, IReadOnlyList<string> values)
    {
        Role = role;
        Parameter = parameter;
        Type = type;
        ByRef = byRef;
        Values = values;
    }

    /// <summary>input, literal, local (a name used only inside), name (the member's own name), member or type.</summary>
    public string Role { get; }

    /// <summary>The helper parameter it becomes; empty when it does not become one.</summary>
    public string Parameter { get; }

    /// <summary>The parameter type, or null when the syntax alone doesn't say (declared with var, say).</summary>
    public string? Type { get; }

    public bool ByRef { get; }

    /// <summary>The text in each copy, in the order of the clone's locations.</summary>
    public IReadOnlyList<string> Values { get; }
}

/// <summary>A proposed fix for one clone class: a verdict, the reasons, and for Extract a helper and a patch.</summary>
public sealed class FixProposal
{
    internal FixProposal(int number, CloneClass clone)
    {
        Number = number;
        Clone = clone;
    }

    public int Number { get; }

    public CloneClass Clone { get; }

    public ProposalVerdict Verdict { get; internal set; } = ProposalVerdict.Review;

    /// <summary>low or medium for Extract; empty otherwise.</summary>
    public string Risk { get; internal set; } = string.Empty;

    public string Title { get; internal set; } = "Review by hand";

    /// <summary>Why this verdict: the checks that passed, or the ones that stopped the proposal.</summary>
    public List<string> Reasons { get; } = new List<string>();

    /// <summary>Things the reviewer must check or fill in.</summary>
    public List<string> Notes { get; } = new List<string>();

    public List<CloneHole> Holes { get; } = new List<CloneHole>();

    /// <summary>The helper as the patch adds it.</summary>
    public string? Helper { get; internal set; }

    /// <summary>A unified diff against the scanned files (paths relative to the root), for <c>git apply</c>.</summary>
    public string? Patch { get; internal set; }

    /// <summary>For proposals without a patch: how the first two copies differ, line by line.</summary>
    public string? LineDiff { get; internal set; }

    /// <summary>Other proposals whose patches touch the same lines.</summary>
    public List<int> Overlaps { get; } = new List<int>();

    /// <summary>Each changed file's full text after the patch (path to text).</summary>
    internal Dictionary<string, string> Rewritten { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    internal List<Edit> Edits { get; } = new List<Edit>();

    internal void Stop(ProposalVerdict verdict, string title, params string[] reasons)
    {
        Verdict = verdict;
        Title = title;
        Reasons.Clear();
        Reasons.AddRange(reasons);
        Edits.Clear();
        Helper = null;
        Risk = string.Empty;
    }
}

/// <summary>Replace characters Start..End of a file's text.</summary>
internal sealed class Edit
{
    public Edit(SourceFile file, int start, int end, string text)
    {
        File = file;
        Start = start;
        End = end;
        Text = text;
    }

    public SourceFile File { get; }

    public int Start { get; }

    public int End { get; }

    public string Text { get; }
}

/// <summary>
/// Turns clone classes into fix proposals a person can review. It never writes to the scanned code: the output is
/// a verdict, the reasons, and for mechanical cases a helper plus a patch to try on a branch.
///
/// The copies of an exact clone have identical token streams, so they line up token by token. Where every copy has
/// the same text, the helper keeps it. Where they differ, a value (a local, field or literal) becomes a helper
/// parameter; a different member or type means no plain helper exists, so the proposal goes to review. Data flow is
/// judged from syntax: locals declared before the block and used in it become parameters (by ref when the block
/// assigns them), and a local declared in the block and used after it becomes the return value. The proposal stops
/// when control leaves the block (return, break, continue, goto, yield), when more than one value flows out, when the
/// copies live in different classes, or when a copy is in a framework file. Types come from declarations; one
/// declared with var gets TODO_Type for the reviewer to fill in, and the patch then does not compile until they do.
/// </summary>
public static class ProposalBuilder
{
    private const int MaxDifferingLiterals = 3;
    private const int MaxParameters = 6;

    private static readonly Regex HookFile = new(@"^[A-Za-z0-9_]+\.App\.(cs|razor)$", RegexOptions.Compiled);

    public static List<FixProposal> Build(CloneResult result)
    {
        var context = new Context(result);
        var proposals = new List<FixProposal>();
        for (int i = 0; i < result.Classes.Count; i++) {
            var p = new FixProposal(i + 1, result.Classes[i]);
            if (Leaves(p, context)) {
                // framework file or template stubs: nothing to propose
            } else if (p.Clone.Kind == CloneKind.Near) {
                Review(p, $"Near copies (similarity {p.Clone.Similarity.ToString("0.00", CultureInfo.InvariantCulture)}): they differ in more than " +
                    "names and literals, so a person has to decide the shared shape (a parameter, a delegate, or one calling the other).");
            } else {
                Exact(p, context);
            }

            if (p.Verdict == ProposalVerdict.Extract) {
                Finish(p);
            }

            proposals.Add(p);
        }

        MarkOverlaps(proposals);
        return proposals;
    }

    // ---------------------------------------------------------------- exact clones

    /// <summary>Framework files and FreeCRM's comment-only hook stubs are left alone, whether the copies are exact or near.</summary>
    private static bool Leaves(FixProposal p, Context ctx)
    {
        IReadOnlyList<CodeUnit> copies = p.Clone.Members;
        foreach (CodeUnit u in copies) {
            if (FileName(u.Path).IndexOf(".App.", StringComparison.Ordinal) < 0) {
                p.Stop(ProposalVerdict.Leave, "Leave it: framework file",
                    $"{u.Path} is a framework file (no .App. in its name). FreeCRM upgrades replace it, so it should not be edited.");
                return true;
            }
        }

        if (copies.Any(IsTemplateStub)) {
            p.Stop(ProposalVerdict.Leave, "Leave it: template stubs",
                "The copies contain comment-only blocks, the shape of FreeCRM's per-entity hook stubs. They are meant to be " +
                "filled in one by one; add the class to the baseline.");
            return true;
        }

        return false;
    }

    private static void Exact(FixProposal p, Context ctx)
    {
        IReadOnlyList<CodeUnit> copies = p.Clone.Members;
        if (copies.Any(u => HookFile.IsMatch(FileName(u.Path)) && !FileName(u.Path).StartsWith(ctx.Project + ".", StringComparison.Ordinal))) {
            p.Notes.Add("A copy is in a FreeCRM hook file (Name.App.cs). Hook files are diffed by hand on framework upgrades; keep edits there small.");
        }

        if (copies.All(u => u.Window is not null)) {
            Window(p, ctx);
        } else if (copies.All(u => u.Member is MethodDeclarationSyntax)) {
            Methods(p, ctx);
        } else {
            Review(p, $"The copies are {string.Join(", ", copies.Select(u => u.Kind).Distinct())}; proposals are written for " +
                "statement blocks and methods only.");
        }
    }

    /// <summary>A duplicated run of statements: extract it into a helper and call it from each copy.</summary>
    private static void Window(FixProposal p, Context ctx)
    {
        IReadOnlyList<CodeUnit> copies = p.Clone.Members;
        CodeUnit u0 = copies[0];
        string? stop = SameClass(copies) ?? copies.Select(u => ControlLeaves(u.Window!)).FirstOrDefault(s => s is not null);
        if (stop is not null) {
            Review(p, stop);
            return;
        }

        List<Dictionary<string, string?>> outer = copies.Select(OuterLocals).ToList();
        List<Slot> slots = Line(p, copies, ctx, outer, out List<string> problems);

        // A local declared at the block's top level and read after it flows out and must be returned.
        var outflow = new List<int>();
        foreach (SyntaxToken declared in TopLevelDeclarations(u0.Window!)) {
            int k = Array.FindIndex(u0.Toks, t => t.Span == declared.Span);
            if (k >= 0 && copies.Any(u => UsedAfter(u.Window!, u.Toks[k].Text))) {
                outflow.Add(k);
            }
        }

        if (outflow.Count > 1) {
            problems.Add($"{outflow.Count} values are used after the block ({string.Join(", ", outflow.Select(k => "`" + u0.Toks[k].Text + "`"))}); " +
                "a helper returns one value, so this needs a person (a tuple, or a smaller block).");
        }

        if (problems.Count > 0) {
            Review(p, problems.ToArray());
            return;
        }

        SyntaxNode member = EnclosingMember(u0.Window![0]);
        bool isAsync = u0.Window!.Any(HasAwait);
        bool isStatic = Modifiers(member).Any(SyntaxKind.StaticKeyword) || !UsesInstance(u0, slots, outer[0], ctx);
        string? outName = outflow.Count == 1 ? u0.Toks[outflow[0]].Original : null;
        string? outType = outflow.Count == 1 ? DeclaredType(u0.Toks[outflow[0]].Token) : null;
        string name = outName is not null ? "Create" + Pascal(outName) : StepName(copies, u0);
        if (isAsync && !name.EndsWith("Async", StringComparison.Ordinal)) {
            name += "Async";
        }

        name = Unique(ctx, ClassKey(u0), name);
        string eol = u0.Part.File.Eol;
        string memberIndent = IndentAt(u0.Part.File.Text, member.SpanStart + u0.Part.PositionOffset);
        string bodyIndent = memberIndent + "    ";
        int start = u0.Window![0].SpanStart;
        int end = u0.Window![u0.Window!.Length - 1].Span.End;
        string body = Reindent(Substitute(u0, start, end, slots), u0, bodyIndent, eol);
        string returnType = outName is null ? "void" : outType ?? "TODO_Type";
        if (isAsync) {
            returnType = outName is null ? "Task" : $"Task<{returnType}>";
        }

        var helper = new StringBuilder();
        helper.Append(memberIndent).Append("private ").Append(isStatic ? "static " : "").Append(isAsync ? "async " : "")
            .Append(returnType).Append(' ').Append(name).Append('(').Append(ParameterList(slots)).Append(')').Append(eol)
            .Append(memberIndent).Append('{').Append(eol)
            .Append(bodyIndent).Append(body).Append(eol);
        if (outName is not null) {
            helper.Append(bodyIndent).Append("return ").Append(outName).Append(';').Append(eol);
        }

        helper.Append(memberIndent).Append('}');
        p.Helper = helper.ToString();
        for (int i = 0; i < copies.Count; i++) {
            CodeUnit u = copies[i];
            string call = (isAsync ? "await " : "") + name + "(" + Arguments(slots, i) + ")";
            string text = outName is not null ? $"var {u.Toks[outflow[0]].Original} = {call};" : call + ";";
            int off = u.Part.PositionOffset;
            p.Edits.Add(new Edit(u.Part.File, u.Window![0].SpanStart + off, u.Window![u.Window!.Length - 1].Span.End + off, text));
        }

        int insertAt = member.Span.End + u0.Part.PositionOffset;
        p.Edits.Add(new Edit(u0.Part.File, insertAt, insertAt, eol + eol + p.Helper));
        p.Verdict = ProposalVerdict.Extract;
        p.Title = $"Extract {name}({string.Join(", ", slots.Select(s => s.Name))}) and call it from {copies.Count} places";
        p.Reasons.Add("No return, break, continue, goto or yield leaves the block, so it can move into a method unchanged.");
        p.Reasons.Add(outName is null ? "Nothing declared in the block is used after it." : $"`{outName}` is used after the block, so the helper returns it.");
        if (isAsync) {
            p.Reasons.Add("The block awaits; the helper is async and each call site awaits it, so the awaits happen in the same order.");
        }

        p.Reasons.Add(isStatic ? "The block uses no instance members, so the helper is static." : "The block uses members of the class, so the helper is an instance method next to the copies.");
        NoteComments(p, copies.Select(u => (u, u.Window![0].SpanStart, u.Window![u.Window!.Length - 1].Span.End)).ToList());
        Rate(p, slots, outName is not null && outType is null);
    }

    /// <summary>Whole methods with the same body: one helper, and each method's body becomes a call to it.</summary>
    private static void Methods(FixProposal p, Context ctx)
    {
        IReadOnlyList<CodeUnit> copies = p.Clone.Members;
        List<MethodDeclarationSyntax> methods = copies.Select(u => (MethodDeclarationSyntax)u.Member!).ToList();
        MethodDeclarationSyntax m0 = methods[0];
        string? stop = SameClass(copies)
            ?? (methods.Any(m => m.TypeParameterList is not null) ? "The methods are generic; type parameters need a person." : null)
            ?? (methods.Any(m => m.ParameterList.Parameters.Any(x => x.Modifiers.Any(SyntaxKind.ThisKeyword))) ? "Extension methods are not rewritten." : null)
            ?? (methods.Select(m => m.Modifiers.Any(SyntaxKind.StaticKeyword)).Distinct().Count() > 1 ? "Some copies are static and some are not." : null);
        if (stop is not null) {
            Review(p, stop);
            return;
        }

        var none = copies.Select(_ => new Dictionary<string, string?>(StringComparer.Ordinal)).ToList();
        List<Slot> slots = Line(p, copies, ctx, none, out List<string> problems);
        if (problems.Count > 0) {
            Review(p, problems.ToArray());
            return;
        }

        CodeUnit u0 = copies[0];
        string eol = u0.Part.File.Eol;
        string indent = IndentAt(u0.Part.File.Text, m0.SpanStart + u0.Part.PositionOffset);
        bool isAsync = m0.Modifiers.Any(SyntaxKind.AsyncKeyword);
        string target;
        int firstCaller;
        if (slots.Count == 0) {
            // Identical apart from names: keep the first, the others call it.
            target = m0.Identifier.ValueText;
            firstCaller = 1;
            p.Title = $"Make {string.Join(", ", methods.Skip(1).Select(m => m.Identifier.ValueText))} call {target}";
            p.Reasons.Add("The methods are identical apart from their names, so the others can call the first.");
        } else {
            target = Unique(ctx, ClassKey(u0), CommonName(methods.Select(m => m.Identifier.ValueText), withSuffix: true));
            firstCaller = 0;
            string signature = string.Join(", ", slots.Select(s => (s.ByRef ? "ref " : "") + (s.Type ?? "TODO_Type") + " " + s.Name)
                .Concat(m0.ParameterList.Parameters.Select(x => x.WithAttributeLists(default).ToString().Trim())));
            string rest = Substitute(u0, m0.ParameterList.Span.End, m0.Span.End, slots);
            p.Helper = indent + "private " + (m0.Modifiers.Any(SyntaxKind.StaticKeyword) ? "static " : "") + (isAsync ? "async " : "")
                + m0.ReturnType + " " + target + "(" + signature + ")" + rest;
            int insertAt = m0.Span.End + u0.Part.PositionOffset;
            p.Edits.Add(new Edit(u0.Part.File, insertAt, insertAt, eol + eol + p.Helper));
            p.Title = $"Extract {target}({string.Join(", ", slots.Select(s => s.Name))}) and have the {copies.Count} methods call it";
            p.Reasons.Add("The method bodies are the same apart from the values listed below, which become helper parameters.");
        }

        string returnType = m0.ReturnType.ToString();
        bool returnsValue = returnType != "void" && !(isAsync && (returnType == "Task" || returnType == "ValueTask"));
        for (int i = firstCaller; i < copies.Count; i++) {
            MethodDeclarationSyntax m = methods[i];
            CodeUnit u = copies[i];
            string args = string.Join(", ", slots.Select(s => (s.ByRef ? "ref " : "") + s.Values[i])
                .Concat(m.ParameterList.Parameters.Select(x =>
                    string.Concat(x.Modifiers.Where(t => !t.IsKind(SyntaxKind.ParamsKeyword)).Select(t => t.Text + " ")) + x.Identifier.Text)));
            string call = (returnsValue ? "return " : "") + (m.Modifiers.Any(SyntaxKind.AsyncKeyword) ? "await " : "") + target + "(" + args + ");";
            string mIndent = IndentAt(u.Part.File.Text, m.SpanStart + u.Part.PositionOffset);
            int bodyStart = (m.Body?.SpanStart ?? m.ExpressionBody!.SpanStart) + u.Part.PositionOffset;
            p.Edits.Add(new Edit(u.Part.File, bodyStart, m.Span.End + u.Part.PositionOffset, "{" + eol + mIndent + "    " + call + eol + mIndent + "}"));
        }

        p.Verdict = ProposalVerdict.Extract;
        p.Reasons.Add("Each method keeps its name, signature, attributes and doc comments; only its body changes.");
        NoteComments(p, copies.Select(u => (u, ((MethodDeclarationSyntax)u.Member!).ParameterList.Span.End, u.Member!.Span.End)).ToList());
        Rate(p, slots, false);
    }

    /// <summary>
    /// Lines the copies up token by token. Each renamed identifier and each literal that differs between copies, and
    /// each outer local the block reads, gets a slot: a helper parameter. Problems are reasons no helper can be written.
    /// </summary>
    private static List<Slot> Line(FixProposal p, IReadOnlyList<CodeUnit> copies, Context ctx,
        List<Dictionary<string, string?>> outer, out List<string> problems)
    {
        problems = new List<string>();
        CodeUnit u0 = copies[0];
        var slots = new List<Slot>();
        var letters = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (int k = 0; k < u0.Toks.Length; k++) {
            if (u0.Toks[k].Rename) {
                if (!letters.TryGetValue(u0.Tokens[k], out List<int>? list)) {
                    list = new List<int>();
                    letters[u0.Tokens[k]] = list;
                }

                list.Add(k);
            }
        }

        // Names declared inside are kept from the first copy, so parameter names must not collide with them.
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (List<int> positions in letters.Values) {
            if (positions.Any(k => Role(u0.Toks[k]) is "declare" or "self")) {
                used.Add(u0.Toks[positions[0]].Text);
            }
        }

        foreach (KeyValuePair<string, List<int>> letter in letters.OrderBy(l => l.Value[0])) {
            List<int> positions = letter.Value;
            string[] keys = copies.Select(u => u.Toks[positions[0]].Text).ToArray();
            string[] values = copies.Select(u => u.Toks[positions[0]].Original).ToArray();
            bool differ = keys.Distinct(StringComparer.Ordinal).Count() > 1;
            var roles = new HashSet<string>(positions.Select(k => Role(u0.Toks[k])));
            if (roles.Contains("declare") || roles.Contains("self")) {
                if (differ) {
                    p.Holes.Add(new CloneHole(roles.Contains("self") ? "name" : "local", "", null, false, values));
                }

                continue;
            }

            if (roles.Contains("type") || roles.Contains("member")) {
                if (differ) {
                    string role = roles.Contains("type") ? "type" : "member";
                    p.Holes.Add(new CloneHole(role, "", null, false, values));
                    problems.Add(role == "type"
                        ? $"The copies use different types ({Join(values)}); that needs a generic helper or overloads."
                        : $"The copies use different members ({Join(values)}); that needs a delegate or a lookup, not a plain parameter.");
                }

                continue;
            }

            bool isOuter = copies.Select((u, i) => outer[i].ContainsKey(keys[i])).Any(b => b);
            if (!differ && !isOuter) {
                continue; // the same field, property or method in every copy: the helper uses it directly
            }

            bool written = positions.Any(k => IsWritten(u0.Toks[k].Token));
            string? type = TypeFor(copies, keys, outer, ctx, problems);
            if (written && !isOuter) {
                for (int i = 0; i < copies.Count; i++) {
                    ctx.MemberType(ClassKey(copies[i]), keys[i], out string kind);
                    if (kind != "field") {
                        problems.Add($"The block assigns `{values[i]}`, which is " +
                            (kind == "property" ? "a property" : kind == "readonly" ? "a readonly field" : "not a field this tool can find") +
                            ", so it can't be passed by ref.");
                    }
                }
            }

            string name = UniqueName(differ ? CommonParameterName(keys) : keys[0], used);
            slots.Add(new Slot(name, type, written, values, positions));
            p.Holes.Add(new CloneHole("input", name, type, written, values));
        }

        // Outer locals read only inside an interpolated string (flattened to one S) still have to be passed in.
        if (u0.Window is not null) {
            var covered = new HashSet<string>(slots.Select(s => u0.Toks[s.Positions[0]].Text), StringComparer.Ordinal);
            foreach (InterpolatedStringExpressionSyntax interpolation in u0.Window.SelectMany(s => s.DescendantNodes()).OfType<InterpolatedStringExpressionSyntax>()) {
                foreach (IdentifierNameSyntax id in interpolation.DescendantNodes().OfType<IdentifierNameSyntax>().Where(IsValueName)) {
                    string key = id.Identifier.ValueText;
                    if (outer[0].ContainsKey(key) && covered.Add(key)) {
                        string name = UniqueName(key, used);
                        string[] same = copies.Select(_ => id.Identifier.Text).ToArray();
                        slots.Add(new Slot(name, outer[0][key], false, same, new List<int>()));
                        p.Holes.Add(new CloneHole("input", name, outer[0][key], false, same));
                    }
                }
            }
        }

        // Literals that differ become parameters; the same values at several places share one.
        var literalGroups = new Dictionary<string, Slot>(StringComparer.Ordinal);
        for (int k = 0; k < u0.Toks.Length; k++) {
            if (!IsLiteral(u0.Toks[k])) {
                continue;
            }

            string[] values = copies.Select(u => u.Toks[k].Original).ToArray();
            if (values.Distinct(StringComparer.Ordinal).Count() == 1) {
                continue;
            }

            if (u0.Toks[k].Token.IsKind(SyntaxKind.InterpolatedStringTextToken)) {
                problems.Add("The text inside an interpolated string differs; rerun without --keep-literals for a proposal.");
                break;
            }

            string key = string.Join("\u001f", values);
            if (literalGroups.TryGetValue(key, out Slot? existing)) {
                existing.Positions.Add(k);
                continue;
            }

            string name = UniqueName(LiteralName(u0, u0.Toks[k]), used);
            var slot = new Slot(name, LiteralType(u0.Toks[k]), false, values, new List<int> { k });
            literalGroups[key] = slot;
            slots.Add(slot);
            p.Holes.Add(new CloneHole("literal", name, slot.Type, false, values));
        }

        if (literalGroups.Count > MaxDifferingLiterals) {
            problems.Add($"{literalGroups.Count} different literal values: this looks like data written out as code. " +
                "A table, or a loop over the values, may fit better than a helper.");
        }

        if (slots.Count > MaxParameters) {
            problems.Add($"A helper would need {slots.Count} parameters, which is usually harder to read than the copies.");
        }

        return slots;
    }

    // ---------------------------------------------------------------- syntax questions

    private static string? ControlLeaves(StatementSyntax[] window)
    {
        bool Inside(SyntaxNode n) => window.Any(s => s.Span.Contains(n.Span));
        foreach (SyntaxNode node in window.SelectMany(s => s.DescendantNodesAndSelf(n => !(n is AnonymousFunctionExpressionSyntax || n is LocalFunctionStatementSyntax)))) {
            switch (node) {
                case ReturnStatementSyntax:
                    return "The block contains a `return`; moving it into a helper would change what returns.";
                case YieldStatementSyntax:
                    return "The block contains `yield`; an iterator can't be split into a helper this way.";
                case GotoStatementSyntax:
                    return "The block contains `goto`.";
                case BreakStatementSyntax or ContinueStatementSyntax:
                    SyntaxNode? target = node.Ancestors().FirstOrDefault(a => a is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
                        or ForEachStatementSyntax || (node is BreakStatementSyntax && a is SwitchStatementSyntax));
                    if (target is null || !Inside(target)) {
                        return "The block contains a `break` or `continue` for a loop outside it.";
                    }

                    break;
            }
        }

        return null;
    }

    private static string? DeclaredType(SyntaxToken identifier) =>
        identifier.Parent is VariableDeclaratorSyntax declarator && declarator.Parent is VariableDeclarationSyntax declaration
            ? TypeOf(declaration.Type, declarator.Initializer?.Value)
            : null;

    private static SyntaxNode EnclosingMember(SyntaxNode node) =>
        node.Ancestors().First(a => a is MemberDeclarationSyntax && a.Parent is TypeDeclarationSyntax);

    private static bool HasAwait(StatementSyntax s) =>
        s.DescendantNodesAndSelf(n => !(n is AnonymousFunctionExpressionSyntax || n is LocalFunctionStatementSyntax))
            .Any(n => n is AwaitExpressionSyntax
                || (n is ForEachStatementSyntax fe && fe.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
                || (n is UsingStatementSyntax us && us.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword))
                || (n is LocalDeclarationStatementSyntax ld && ld.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)));

    private static bool IsLiteral(Tok t) => t.Token == default
        || t.Token.IsKind(SyntaxKind.StringLiteralToken) || t.Token.IsKind(SyntaxKind.CharacterLiteralToken)
        || t.Token.IsKind(SyntaxKind.NumericLiteralToken) || t.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken)
        || t.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) || t.Token.IsKind(SyntaxKind.InterpolatedStringTextToken);

    /// <summary>A comment-only <c>if</c> body, the shape of FreeCRM's <c>//department.Property = rec.Property;</c> stubs.</summary>
    private static bool IsTemplateStub(CodeUnit u) =>
        (u.Window ?? new[] { u.Member! }).SelectMany(n => n.DescendantNodesAndSelf()).OfType<IfStatementSyntax>()
            .Any(i => i.Statement is BlockSyntax b && b.Statements.Count == 0
                && b.CloseBraceToken.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)));

    /// <summary>A name used as a value (a local, field or property), not a member after a dot, a type, or a named argument.</summary>
    private static bool IsValueName(IdentifierNameSyntax id) =>
        !(id.Parent is MemberAccessExpressionSyntax ma && ma.Name == id)
        && !(id.Parent is MemberBindingExpressionSyntax)
        && !(id.Parent is NameColonSyntax || id.Parent is NameEqualsSyntax || id.Parent is QualifiedNameSyntax || id.Parent is TypeArgumentListSyntax)
        && !(id.Parent is AssignmentExpressionSyntax a && a.Left == id && a.Parent is InitializerExpressionSyntax)
        && !SyntaxFacts.IsInTypeOnlyContext(id) && !SyntaxFacts.IsInNamespaceOrTypeContext(id)
        && id.Identifier.ValueText is not ("var" or "nameof" or "dynamic" or "_");

    private static bool IsWritten(SyntaxToken token)
    {
        if (!(token.Parent is IdentifierNameSyntax name)) {
            return false;
        }

        return name.Parent switch {
            AssignmentExpressionSyntax a => a.Left == name,
            PrefixUnaryExpressionSyntax u => u.IsKind(SyntaxKind.PreIncrementExpression) || u.IsKind(SyntaxKind.PreDecrementExpression),
            PostfixUnaryExpressionSyntax => true,
            ArgumentSyntax arg => arg.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword),
            _ => false
        };
    }

    private static SyntaxTokenList Modifiers(SyntaxNode member) => member switch {
        BaseMethodDeclarationSyntax m => m.Modifiers,
        BasePropertyDeclarationSyntax p => p.Modifiers,
        BaseFieldDeclarationSyntax f => f.Modifiers,
        _ => default
    };

    private static string MemberName(SyntaxNode member) => member switch {
        MethodDeclarationSyntax m => m.Identifier.ValueText,
        PropertyDeclarationSyntax p => p.Identifier.ValueText,
        ConstructorDeclarationSyntax c => c.Identifier.ValueText,
        _ => "Shared"
    };

    /// <summary>Locals and parameters in scope at the start of a window, with their declared type when the syntax says.</summary>
    private static Dictionary<string, string?> OuterLocals(CodeUnit u)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        StatementSyntax anchor = u.Window![0];
        int start = anchor.SpanStart;
        foreach (SyntaxNode scope in anchor.Ancestors()) {
            switch (scope) {
                case BlockSyntax block:
                    foreach (StatementSyntax s in block.Statements.TakeWhile(s => s.Span.End <= start)) {
                        if (s is LocalDeclarationStatementSyntax ld) {
                            foreach (VariableDeclaratorSyntax v in ld.Declaration.Variables) {
                                Add(v.Identifier.ValueText, TypeOf(ld.Declaration.Type, v.Initializer?.Value));
                            }
                        }

                        // out var and pattern variables in a statement's own expressions stay in scope after it.
                        foreach (SingleVariableDesignationSyntax d in s.DescendantNodes(n => !(n is BlockSyntax || n is AnonymousFunctionExpressionSyntax)).OfType<SingleVariableDesignationSyntax>()) {
                            Add(d.Identifier.ValueText, null);
                        }
                    }

                    break;
                case ForEachStatementSyntax fe:
                    Add(fe.Identifier.ValueText, fe.Type.IsVar ? null : fe.Type.ToString());
                    break;
                case ForStatementSyntax f when f.Declaration is not null:
                    foreach (VariableDeclaratorSyntax v in f.Declaration.Variables) {
                        Add(v.Identifier.ValueText, TypeOf(f.Declaration.Type, v.Initializer?.Value));
                    }

                    break;
                case UsingStatementSyntax us when us.Declaration is not null:
                    foreach (VariableDeclaratorSyntax v in us.Declaration.Variables) {
                        Add(v.Identifier.ValueText, TypeOf(us.Declaration.Type, v.Initializer?.Value));
                    }

                    break;
                case CatchClauseSyntax cc when cc.Declaration is not null && !cc.Declaration.Identifier.IsKind(SyntaxKind.None):
                    Add(cc.Declaration.Identifier.ValueText, cc.Declaration.Type.ToString());
                    break;
                case SimpleLambdaExpressionSyntax sl:
                    Add(sl.Parameter.Identifier.ValueText, sl.Parameter.Type?.ToString());
                    break;
                case ParenthesizedLambdaExpressionSyntax pl:
                    AddParameters(pl.ParameterList);
                    break;
                case LocalFunctionStatementSyntax lf:
                    AddParameters(lf.ParameterList);
                    break;
                case BaseMethodDeclarationSyntax m:
                    AddParameters(m.ParameterList);
                    return result;
                case AccessorDeclarationSyntax a:
                    if ((a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration))
                        && a.Parent?.Parent is BasePropertyDeclarationSyntax prop) {
                        Add("value", prop.Type.ToString());
                    }

                    return result;
            }
        }

        return result;

        void Add(string name, string? type)
        {
            if (!result.ContainsKey(name)) {
                result[name] = type;
            }
        }

        void AddParameters(ParameterListSyntax list)
        {
            foreach (ParameterSyntax x in list.Parameters) {
                Add(x.Identifier.ValueText, x.Type?.ToString());
            }
        }
    }

    /// <summary>
    /// The role a renamed identifier plays: value (a local, field or property read or written), member (the name after
    /// a dot, or in an object initializer), type, declare (declared inside the unit) or self (the member's own name).
    /// </summary>
    private static string Role(Tok t)
    {
        switch (t.Token.Parent) {
            case IdentifierNameSyntax id:
                if (id.Parent is InvocationExpressionSyntax call && call.Expression == id) {
                    return "member"; // a method being called (renamed only in blind mode)
                }

                if (IsValueName(id)) {
                    return "value";
                }

                return SyntaxFacts.IsInTypeOnlyContext(id) || SyntaxFacts.IsInNamespaceOrTypeContext(id)
                    || id.Parent is QualifiedNameSyntax || id.Parent is TypeArgumentListSyntax ? "type" : "member";
            case GenericNameSyntax:
            case BaseTypeDeclarationSyntax:
                return "type";
            case VariableDeclaratorSyntax:
            case SingleVariableDesignationSyntax:
            case ForEachStatementSyntax:
            case CatchDeclarationSyntax:
            case ParameterSyntax:
            case LocalFunctionStatementSyntax:
                return "declare";
            default:
                return "self";
        }
    }

    /// <summary>
    /// A helper name for a block that returns nothing: the shared start of the enclosing methods' names
    /// (GetDevOpsProjectAsync and GetDevOpsProjectsAsync give GetDevOpsProjectStep), else the block's first call.
    /// </summary>
    private static string StepName(IReadOnlyList<CodeUnit> copies, CodeUnit u0)
    {
        string shared = CommonName(copies.Select(u => MemberName(EnclosingMember(u.Window![0]))), withSuffix: false);
        if (shared.EndsWith("Async", StringComparison.Ordinal)) {
            shared = shared.Substring(0, shared.Length - "Async".Length);
        }

        if (shared.Length >= 3 && shared != "Shared") {
            return shared + "Step";
        }

        SimpleNameSyntax? call = u0.Window!.SelectMany(s => s.DescendantNodes()).OfType<InvocationExpressionSyntax>()
            .Select(i => i.Expression switch {
                MemberAccessExpressionSyntax ma => ma.Name,
                SimpleNameSyntax sn => sn,
                _ => null
            })
            .FirstOrDefault(n => n is not null);
        string first = call is null ? "Shared" : Pascal(call.Identifier.ValueText);
        if (first.EndsWith("Async", StringComparison.Ordinal)) {
            first = first.Substring(0, first.Length - "Async".Length);
        }

        return first + "Step";
    }

    private static string? SameClass(IReadOnlyList<CodeUnit> copies)
    {
        string[] keys = copies.Select(ClassKey).Distinct(StringComparer.Ordinal).ToArray();
        return keys.Length == 1 ? null
            : $"The copies are in different classes ({string.Join(", ", keys.Select(k => k.StartsWith("razor:", StringComparison.Ordinal) ? FileName(k) : k))}); " +
              "a shared helper needs a home both can reach, which is a design decision.";
    }

    /// <summary>Locals declared by the block's own statements (not in nested blocks), which could be used after it.</summary>
    private static IEnumerable<SyntaxToken> TopLevelDeclarations(StatementSyntax[] window)
    {
        foreach (StatementSyntax s in window) {
            if (s is LocalDeclarationStatementSyntax ld) {
                foreach (VariableDeclaratorSyntax v in ld.Declaration.Variables) {
                    yield return v.Identifier;
                }
            }

            foreach (SingleVariableDesignationSyntax d in s.DescendantNodes(n => !(n is BlockSyntax || n is AnonymousFunctionExpressionSyntax)).OfType<SingleVariableDesignationSyntax>()) {
                yield return d.Identifier;
            }
        }
    }

    private static string? TypeFor(IReadOnlyList<CodeUnit> copies, string[] keys, List<Dictionary<string, string?>> outer, Context ctx, List<string> problems)
    {
        var types = copies.Select((u, i) => outer[i].TryGetValue(keys[i], out string? t) ? t : ctx.MemberType(ClassKey(u), keys[i], out _)).ToList();
        if (types.Any(t => t is null)) {
            return null;
        }

        if (types.Distinct(StringComparer.Ordinal).Count() > 1) {
            problems.Add($"{Join(keys)} have different types in different copies ({Join(types.Select(t => t!))}).");
            return null;
        }

        return types[0];
    }

    private static string? TypeOf(TypeSyntax type, ExpressionSyntax? initializer)
    {
        if (!type.IsVar) {
            return type.ToString();
        }

        return initializer switch {
            ObjectCreationExpressionSyntax creation => creation.Type.ToString(),
            CastExpressionSyntax cast => cast.Type.ToString(),
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.StringLiteralExpression) => "string",
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.CharacterLiteralExpression) => "char",
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.TrueLiteralExpression) || lit.IsKind(SyntaxKind.FalseLiteralExpression) => "bool",
            LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NumericLiteralExpression) => ClrName(lit.Token.Value),
            InterpolatedStringExpressionSyntax => "string",
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ToString" }, ArgumentList.Arguments.Count: 0 } => "string",
            _ => null
        };
    }

    /// <summary>Whether the block reads something only an instance has: this, base, or a field, property or method of the class.</summary>
    private static bool UsesInstance(CodeUnit u, List<Slot> slots, Dictionary<string, string?> outer, Context ctx)
    {
        StatementSyntax[] window = u.Window!;
        var declared = new HashSet<string>(window.SelectMany(s => s.DescendantTokens())
            .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && t.Parent is VariableDeclaratorSyntax or SingleVariableDesignationSyntax
                or ForEachStatementSyntax or CatchDeclarationSyntax or ParameterSyntax or LocalFunctionStatementSyntax)
            .Select(t => t.ValueText), StringComparer.Ordinal);
        var parameters = new HashSet<string>(slots.Where(s => s.Positions.Count > 0).Select(s => u.Toks[s.Positions[0]].Text), StringComparer.Ordinal);
        foreach (SyntaxNode n in window.SelectMany(s => s.DescendantNodesAndSelf())) {
            if (n is ThisExpressionSyntax || n is BaseExpressionSyntax) {
                return true;
            }

            if (n is IdentifierNameSyntax id && IsValueName(id)) {
                string name = id.Identifier.ValueText;
                if (!outer.ContainsKey(name) && !declared.Contains(name) && !parameters.Contains(name) && !ctx.TypeNames.Contains(name)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool UsedAfter(StatementSyntax[] window, string name)
    {
        if (!(window[0].Parent is BlockSyntax block)) {
            return false;
        }

        return block.Statements.SkipWhile(s => s != window[window.Length - 1]).Skip(1)
            .SelectMany(s => s.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Any(n => n.Identifier.ValueText == name && IsValueName(n));
    }

    // ---------------------------------------------------------------- text building

    private static string Arguments(List<Slot> slots, int copy) =>
        string.Join(", ", slots.Select(s => (s.ByRef ? "ref " : "") + s.Values[copy]));

    private static string ClassKey(CodeUnit u)
    {
        SyntaxNode node = (SyntaxNode?)u.Window?[0] ?? u.Member!;
        string name = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "";
        return name == SourceSet.RazorWrapperClass ? "razor:" + u.Path : name;
    }

    private static string ClrName(object? value) => value switch {
        int => "int",
        long => "long",
        uint => "uint",
        ulong => "ulong",
        float => "float",
        double => "double",
        decimal => "decimal",
        _ => "TODO_Type"
    };

    /// <summary>A name for a parameter whose argument differs: the shared word of _coreBuffer and _powerBuffer is "buffer".</summary>
    private static string CommonParameterName(IReadOnlyList<string> keys)
    {
        string[] names = keys.Select(v => v.TrimStart('_')).ToArray();
        string suffix = Reverse(CommonPrefix(names.Select(Reverse).ToArray()));
        int boundary = FirstUpper(suffix);
        if (boundary >= 0 && suffix.Length - boundary >= 3) {
            return LowerFirst(suffix.Substring(boundary));
        }

        if (names.Any(n => n == suffix) && suffix.Length >= 3) {
            return LowerFirst(suffix);
        }

        string prefix = CommonPrefix(names);
        return prefix.Length >= 3 ? LowerFirst(prefix) : LowerFirst(names[0]);
    }

    /// <summary>ParseBitbucketUrl and ParseGitLabUrl share "Parse" and "Url": ParseUrl. Words are cut at capitals.</summary>
    private static string CommonName(IEnumerable<string> names, bool withSuffix)
    {
        string[] all = names.ToArray();
        string prefix = CommonPrefix(all);
        while (prefix.Length > 0 && prefix.Length < all[0].Length && !char.IsUpper(all[0][prefix.Length])) {
            prefix = prefix.Substring(0, prefix.Length - 1);
        }

        string suffix = "";
        if (withSuffix) {
            suffix = Reverse(CommonPrefix(all.Select(Reverse).ToArray()));
            int cut = FirstUpper(suffix);
            suffix = cut >= 0 ? suffix.Substring(cut) : "";
            if (prefix.Length + suffix.Length > all.Min(n => n.Length)) {
                suffix = "";
            }
        }

        string name = prefix + suffix;
        return name.Length >= 3 ? Pascal(name) : "Shared";
    }

    private static string CommonPrefix(IReadOnlyList<string> names)
    {
        string prefix = names[0];
        foreach (string n in names.Skip(1)) {
            int k = 0;
            while (k < prefix.Length && k < n.Length && prefix[k] == n[k]) {
                k++;
            }

            prefix = prefix.Substring(0, k);
        }

        return prefix;
    }

    private static string FileName(string path) => path.Substring(path.LastIndexOf('/') + 1);

    private static int FirstUpper(string s)
    {
        for (int i = 0; i < s.Length; i++) {
            if (char.IsUpper(s[i])) {
                return i;
            }
        }

        return -1;
    }

    private static string IndentAt(string text, int position)
    {
        int lineStart = position == 0 ? 0 : text.LastIndexOf('\n', position - 1) + 1;
        int i = lineStart;
        while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) {
            i++;
        }

        return text.Substring(lineStart, i - lineStart);
    }

    private static string Join(IEnumerable<string> values) => string.Join(" / ", values.Distinct(StringComparer.Ordinal).Select(v => "`" + v + "`"));

    /// <summary>A literal's parameter name from where it sits: <c>Source = "GitLab"</c> gives "source".</summary>
    private static string LiteralName(CodeUnit u, Tok t)
    {
        SyntaxNode? node = t.Token == default ? u.Part.Tree.GetRoot().FindNode(t.Span) : t.Token.Parent;
        while (node is not null && !(node is LiteralExpressionSyntax || node is InterpolatedStringExpressionSyntax)) {
            node = node.Parent;
        }

        switch (node?.Parent) {
            case AssignmentExpressionSyntax a when a.Right == node && a.Left is IdentifierNameSyntax left:
                return LowerFirst(left.Identifier.ValueText);
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax v }:
                return LowerFirst(v.Identifier.ValueText);
            case ArgumentSyntax { NameColon: { } nc }:
                return nc.Name.Identifier.ValueText;
        }

        return t.Token.IsKind(SyntaxKind.NumericLiteralToken) ? "number" : t.Token.IsKind(SyntaxKind.CharacterLiteralToken) ? "character" : "text";
    }

    private static string LiteralType(Tok t)
    {
        if (t.Token == default || t.Token.IsKind(SyntaxKind.StringLiteralToken)
            || t.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || t.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)) {
            return "string";
        }

        return t.Token.IsKind(SyntaxKind.CharacterLiteralToken) ? "char" : ClrName(t.Token.Value);
    }

    private static string LowerFirst(string s) => s.Length == 0 ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

    /// <summary>The patch keeps only the first copy's comments; say so when the others had different ones.</summary>
    private static void NoteComments(FixProposal p, List<(CodeUnit Unit, int Start, int End)> spans)
    {
        string Comments((CodeUnit Unit, int Start, int End) s) => string.Join("\n", s.Unit.Part.Tree.GetRoot()
            .DescendantTrivia(new TextSpan(s.Start, s.End - s.Start))
            .Where(t => t.SpanStart >= s.Start && t.Span.End <= s.End
                && (t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia)))
            .Select(t => t.ToString().Trim()));
        string first = Comments(spans[0]);
        List<string> lost = spans.Skip(1).Where(s => Comments(s).Length > 0 && Comments(s) != first).Select(s => "`" + s.Unit.Location + "`").ToList();
        if (lost.Count > 0) {
            p.Notes.Add($"Comments inside {string.Join(", ", lost)} differ from the first copy's and are removed by the patch. " +
                "The helper keeps the first copy's comments, which may now read as specific to that copy. Reword or move them.");
        }
    }

    private static string ParameterList(List<Slot> slots) =>
        string.Join(", ", slots.Select(s => (s.ByRef ? "ref " : "") + (s.Type ?? "TODO_Type") + " " + s.Name));

    private static string Pascal(string s)
    {
        s = s.TrimStart('@', '_');
        return s.Length == 0 ? "Shared" : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    private static void Rate(FixProposal p, List<Slot> slots, bool unknownReturn)
    {
        List<string> unknown = slots.Where(s => s.Type is null).Select(s => "`" + s.Name + "`").ToList();
        if (unknownReturn) {
            unknown.Add("the return type");
        }

        if (unknown.Count > 0) {
            p.Notes.Add($"Fill in `TODO_Type` for {string.Join(", ", unknown)}: declared with `var`, or not found in the scanned files. " +
                "Until then the patch does not compile.");
        }

        if (slots.Any(s => s.ByRef)) {
            p.Notes.Add("A `ref` parameter is used because the block assigns it. Check the helper really should write to the caller's variable.");
        }

        bool medium = unknown.Count > 0 || slots.Any(s => s.ByRef) || p.Notes.Any(n => n.Contains("hook file"));
        p.Risk = medium ? "medium" : "low";
    }

    /// <summary>Re-indents a block moved into a helper, unless that could change a multi-line string.</summary>
    private static string Reindent(string body, CodeUnit u, string indent, string eol)
    {
        string[] lines = body.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        string original = IndentAt(u.Part.File.Text, u.Window![0].SpanStart + u.Part.PositionOffset);
        bool multiLineString = u.Window!.SelectMany(s => s.DescendantTokens()).Any(t =>
            (t.IsKind(SyntaxKind.StringLiteralToken) || t.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) || t.IsKind(SyntaxKind.InterpolatedStringTextToken))
            && t.Text.IndexOf('\n') >= 0);
        if (!multiLineString && original != indent) {
            for (int i = 1; i < lines.Length; i++) {
                if (lines[i].StartsWith(original, StringComparison.Ordinal)) {
                    lines[i] = indent + lines[i].Substring(original.Length);
                }
            }
        }

        return string.Join(eol, lines);
    }

    private static string Reverse(string s) => new string(s.Reverse().ToArray());

    private static void Review(FixProposal p, params string[] reasons)
    {
        // The title is the first reason's opening clause, so the summary table says why at a glance.
        string first = reasons[0];
        int cut = first.IndexOfAny(new[] { ';', ':' });
        int stop = first.IndexOf(". ", StringComparison.Ordinal);
        cut = cut < 0 ? stop : stop < 0 ? cut : Math.Min(cut, stop);
        string clause = (cut > 0 ? first.Substring(0, cut) : first).TrimEnd('.');
        p.Stop(ProposalVerdict.Review, "Review: " + (clause.Length > 90 ? clause.Substring(0, 87) + "..." : clause), reasons);
        if (p.Clone.Members.Count > 1) {
            p.LineDiff = LineDiff(p.Clone.Members[0], p.Clone.Members[1]);
        }
    }

    /// <summary>The first copy's text between two tree positions, with each parameter's tokens replaced by its name.</summary>
    private static string Substitute(CodeUnit u, int start, int end, List<Slot> slots)
    {
        string text = u.Part.File.Text.Substring(start + u.Part.PositionOffset, end - start);
        var replacements = slots
            .SelectMany(s => s.Positions.Select(k => (Span: u.Toks[k].Span, s.Name)))
            .Where(r => r.Span.Start >= start && r.Span.End <= end && r.Name != text.Substring(r.Span.Start - start, r.Span.Length))
            .OrderByDescending(r => r.Span.Start)
            .ToList();
        foreach ((TextSpan span, string name) in replacements) {
            text = text.Substring(0, span.Start - start) + name + text.Substring(span.End - start);
        }

        return text;
    }

    private static string Unique(Context ctx, string classKey, string name)
    {
        string candidate = name;
        for (int i = 2; ctx.HasMember(classKey, candidate); i++) {
            candidate = name + "Core" + (i == 2 ? "" : i.ToString(CultureInfo.InvariantCulture));
        }

        return candidate;
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        name = name.TrimStart('_');
        if (name.Length == 0 || SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None) {
            name += "Value";
        }

        string candidate = name;
        for (int i = 2; used.Contains(candidate); i++) {
            candidate = name + i.ToString(CultureInfo.InvariantCulture);
        }

        used.Add(candidate);
        return candidate;
    }

    // ---------------------------------------------------------------- patches

    /// <summary>Applies the edits per file, checks the result still parses, and writes the unified diff.</summary>
    private static void Finish(FixProposal p)
    {
        var patch = new StringBuilder();
        foreach (IGrouping<SourceFile, Edit> file in p.Edits.GroupBy(e => e.File).OrderBy(g => g.Key.Path, StringComparer.Ordinal)) {
            List<Edit> edits = file.OrderBy(e => e.Start).ThenBy(e => e.End).ToList();
            for (int i = 1; i < edits.Count; i++) {
                if (edits[i].Start < edits[i - 1].End) {
                    Review(p, "The copies overlap in the file, so one patch can't change them all.");
                    return;
                }
            }

            string text = file.Key.Text;
            var sb = new StringBuilder();
            int at = 0;
            foreach (Edit e in edits) {
                sb.Append(text, at, e.Start - at).Append(e.Text);
                at = e.End;
            }

            sb.Append(text, at, text.Length - at);
            string rewritten = sb.ToString();
            if (!file.Key.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) && Errors(rewritten) > Errors(text)) {
                Review(p, "The rewritten file would not parse, so no patch is offered.");
                return;
            }

            p.Rewritten[file.Key.Path] = rewritten;
            patch.Append(UnifiedDiff(file.Key.Path, text, edits));
        }

        p.Patch = patch.ToString();
    }

    private static int Errors(string text) =>
        CSharpSyntaxTree.ParseText(text).GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error);

    private static void MarkOverlaps(List<FixProposal> proposals)
    {
        List<FixProposal> withPatch = proposals.Where(p => p.Patch is not null).ToList();
        foreach (FixProposal a in withPatch) {
            foreach (FixProposal b in withPatch) {
                if (a != b && a.Edits.Any(x => b.Edits.Any(y => x.File == y.File && x.Start <= y.End && y.Start <= x.End))) {
                    a.Overlaps.Add(b.Number);
                }
            }
        }
    }

    /// <summary>A unified diff (3 lines of context) that <c>git apply</c> accepts, keeping the file's own line endings.</summary>
    internal static string UnifiedDiff(string path, string text, List<Edit> edits)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++) {
            if (text[i] == '\n') {
                starts.Add(i + 1);
            }
        }

        if (starts[starts.Count - 1] != text.Length) {
            starts.Add(text.Length);
        }

        int lineCount = starts.Count - 1;
        string TextLine(int i) => text.Substring(starts[i], starts[i + 1] - starts[i]);
        int LineOf(int pos)
        {
            int i = starts.BinarySearch(pos);
            return Math.Min(i >= 0 ? i : ~i - 1, lineCount - 1);
        }

        // Edits whose changed lines are within 6 lines of each other share a hunk, so contexts never overlap.
        var groups = new List<List<(Edit E, int First, int Last)>>();
        foreach (Edit e in edits) {
            int first = LineOf(e.Start);
            int last = e.End > e.Start ? LineOf(e.End - 1) : first;
            if (groups.Count > 0 && first - groups[groups.Count - 1].Max(g => g.Last) <= 6) {
                groups[groups.Count - 1].Add((e, first, last));
            } else {
                groups.Add(new List<(Edit, int, int)> { (e, first, last) });
            }
        }

        var sb = new StringBuilder();
        sb.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        sb.Append("--- a/").Append(path).Append('\n');
        sb.Append("+++ b/").Append(path).Append('\n');
        int delta = 0;
        foreach (List<(Edit E, int First, int Last)> group in groups) {
            int first = group.Min(g => g.First);
            int last = group.Max(g => g.Last);
            var block = new StringBuilder();
            int at = starts[first];
            foreach ((Edit e, _, _) in group) {
                block.Append(text, at, e.Start - at).Append(e.Text);
                at = e.End;
            }

            block.Append(text, at, starts[last + 1] - at);
            List<string> newLines = SplitKeepingEnds(block.ToString());

            // Lines the edit leaves as they were become context instead of a "-" and "+" pair.
            while (first <= last && newLines.Count > 0 && newLines[0] == TextLine(first)) {
                newLines.RemoveAt(0);
                first++;
            }

            while (first <= last && newLines.Count > 0 && newLines[newLines.Count - 1] == TextLine(last)) {
                newLines.RemoveAt(newLines.Count - 1);
                last--;
            }

            if (first > last && newLines.Count == 0) {
                continue;
            }

            int before = Math.Max(0, first - 3);
            int after = Math.Min(lineCount - 1, last + 3);
            int oldCount = after - before + 1;
            int newCount = (first - before) + newLines.Count + (after - last);
            sb.Append("@@ -").Append(before + 1).Append(',').Append(oldCount)
                .Append(" +").Append(before + 1 + delta).Append(',').Append(newCount).Append(" @@\n");
            for (int i = before; i < first; i++) {
                AppendLine(sb, ' ', TextLine(i));
            }

            for (int i = first; i <= last; i++) {
                AppendLine(sb, '-', TextLine(i));
            }

            foreach (string l in newLines) {
                AppendLine(sb, '+', l);
            }

            for (int i = last + 1; i <= after; i++) {
                AppendLine(sb, ' ', TextLine(i));
            }

            delta += newLines.Count - (last - first + 1);
        }

        return sb.ToString();

        static void AppendLine(StringBuilder sb, char mark, string line)
        {
            sb.Append(mark).Append(line);
            if (!line.EndsWith("\n", StringComparison.Ordinal)) {
                sb.Append("\n\\ No newline at end of file\n");
            }
        }
    }

    private static List<string> SplitKeepingEnds(string s)
    {
        var lines = new List<string>();
        int at = 0;
        for (int i = 0; i < s.Length; i++) {
            if (s[i] == '\n') {
                lines.Add(s.Substring(at, i + 1 - at));
                at = i + 1;
            }
        }

        if (at < s.Length) {
            lines.Add(s.Substring(at));
        }

        return lines;
    }

    /// <summary>A line-by-line diff of two copies (dedented), for the reviewer when no patch is offered.</summary>
    internal static string LineDiff(CodeUnit a, CodeUnit b)
    {
        List<string> la = Dedent(a);
        List<string> lb = Dedent(b);
        var lcs = new int[la.Count + 1, lb.Count + 1];
        for (int i = la.Count - 1; i >= 0; i--) {
            for (int j = lb.Count - 1; j >= 0; j--) {
                lcs[i, j] = la[i].Trim() == lb[j].Trim() ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var sb = new StringBuilder();
        int x = 0;
        int y = 0;
        while (x < la.Count || y < lb.Count) {
            if (x < la.Count && y < lb.Count && la[x].Trim() == lb[y].Trim()) {
                sb.Append("  ").Append(la[x]).Append('\n');
                x++;
                y++;
            } else if (x < la.Count && (y == lb.Count || lcs[x + 1, y] >= lcs[x, y + 1])) {
                sb.Append("- ").Append(la[x]).Append('\n'); // removed lines first, as in a normal diff
                x++;
            } else {
                sb.Append("+ ").Append(lb[y]).Append('\n');
                y++;
            }
        }

        return sb.ToString();

        static List<string> Dedent(CodeUnit u)
        {
            var lines = new List<string>();
            for (int n = u.StartLine; n <= u.EndLine && n <= u.Part.File.Lines.Count && lines.Count < 150; n++) {
                lines.Add(u.Part.File.Lines[n - 1].Replace("\t", "    "));
            }

            int indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
            return lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()).ToList();
        }
    }

    /// <summary>A helper parameter: its name and type, each copy's argument, and the token positions it replaces.</summary>
    private sealed class Slot
    {
        public Slot(string name, string? type, bool byRef, IReadOnlyList<string> values, List<int> positions)
        {
            Name = name;
            Type = type;
            ByRef = byRef;
            Values = values;
            Positions = positions;
        }

        public string Name { get; }

        public string? Type { get; }

        public bool ByRef { get; }

        public IReadOnlyList<string> Values { get; }

        public List<int> Positions { get; }
    }

    /// <summary>What proposals need from the whole scan: the project, type names, and each class's members with their types.</summary>
    private sealed class Context
    {
        private readonly Dictionary<string, Dictionary<string, (string Type, string Kind)>> _members = new(StringComparer.Ordinal);

        public Context(CloneResult result)
        {
            Project = result.ProjectName;
            TypeNames = result.Set.TypeNames;
            foreach (SourcePart part in result.Set.Parts) {
                foreach (TypeDeclarationSyntax type in part.Tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()) {
                    string key = type.Identifier.ValueText == SourceSet.RazorWrapperClass ? "razor:" + part.File.Path : type.Identifier.ValueText;
                    if (!_members.TryGetValue(key, out var members)) {
                        members = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
                        _members[key] = members;
                    }

                    foreach (MemberDeclarationSyntax m in type.Members) {
                        switch (m) {
                            case FieldDeclarationSyntax f:
                                string kind = f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) || f.Modifiers.Any(SyntaxKind.ConstKeyword) ? "readonly" : "field";
                                foreach (VariableDeclaratorSyntax v in f.Declaration.Variables) {
                                    members[v.Identifier.ValueText] = (f.Declaration.Type.ToString(), kind);
                                }

                                break;
                            case PropertyDeclarationSyntax prop:
                                members[prop.Identifier.ValueText] = (prop.Type.ToString(), "property");
                                break;
                            case MethodDeclarationSyntax method:
                                members[method.Identifier.ValueText] = (method.ReturnType.ToString(), "method");
                                break;
                        }
                    }
                }
            }
        }

        public string Project { get; }

        public HashSet<string> TypeNames { get; }

        public bool HasMember(string classKey, string name) => _members.TryGetValue(classKey, out var m) && m.ContainsKey(name);

        public string? MemberType(string classKey, string name, out string kind)
        {
            if (_members.TryGetValue(classKey, out var members) && members.TryGetValue(name, out var found)) {
                kind = found.Kind;
                return found.Type;
            }

            kind = "unknown";
            return null;
        }
    }
}