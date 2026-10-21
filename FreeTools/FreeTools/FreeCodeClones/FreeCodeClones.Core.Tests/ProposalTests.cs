using System.Text.RegularExpressions;
using FreeCodeClones.Core;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FreeCodeClones.Core.Tests;

/// <summary>
/// Fix proposals: a mechanical case gets a helper and a patch whose result parses and no longer has the clone;
/// anything that would change behaviour, or that FreeCRM means to be repeated, gets Review or Leave and no patch.
/// </summary>
public class ProposalTests
{
    [Fact]
    public void Block_with_a_value_used_after_it_becomes_a_returning_helper()
    {
        const string src =
            "class A\n{\n" +
            "    void First(string prefix, int value)\n" +
            "    {\n" +
            "        int doubled = value * 2;\n" +
            "        var builder = new StringBuilder();\n" +
            "        builder.Append(prefix);\n" +
            "        builder.Append(':');\n" +
            "        builder.Append(value);\n" +
            "        var text = builder.ToString();\n" +
            "        Console.WriteLine(text + doubled);\n" +
            "    }\n\n" +
            "    void Second(string pre, int val)\n" +
            "    {\n" +
            "        Save(val);\n" +
            "        var builder = new StringBuilder();\n" +
            "        builder.Append(pre);\n" +
            "        builder.Append(':');\n" +
            "        builder.Append(val);\n" +
            "        var text = builder.ToString();\n" +
            "        Console.WriteLine(text);\n" +
            "    }\n" +
            "}\n";

        FixProposal p = Assert.Single(Propose(("Demo.App.Text.cs", src)));

        Assert.Equal(ProposalVerdict.Extract, p.Verdict);
        Assert.True(p.Helper!.Contains("private static string CreateText(string pre, int val)"), p.Helper);
        Assert.Contains("return text;", p.Helper);
        string rewritten = p.Rewritten["Demo.App.Text.cs"];
        Assert.Contains("var text = CreateText(prefix, value);", rewritten);
        Assert.Contains("var text = CreateText(pre, val);", rewritten);
        AssertParsesAndCloneIsGone("Demo.App.Text.cs", rewritten);
        AssertHunksAreConsistent(p.Patch!);
    }

    [Fact]
    public void Block_that_returns_from_the_middle_is_sent_to_review()
    {
        static string Method(string name, string before) =>
            $"    void {name}(int x)\n    {{\n        {before}\n" +
            "        var a = x * 2;\n" +
            "        if (a > 10) { return; }\n" +
            "        Save(a);\n" +
            "        Save(a + 1);\n" +
            "        Save(a + 2, a);\n" +
            "    }\n";
        string src = "class A\n{\n" + Method("One", "Log(x);") + Method("Two", "Trace(x, x);") + "}\n";

        FixProposal p = Assert.Single(Propose(("Demo.App.Flow.cs", src)));

        Assert.Equal(ProposalVerdict.Review, p.Verdict);
        Assert.Contains(p.Reasons, r => r.Contains("return"));
        Assert.Null(p.Patch);
        Assert.NotNull(p.LineDiff);
    }

    [Fact]
    public void FreeCRM_hook_stubs_are_left_alone()
    {
        static string Stub(string entity) =>
            $"        if (Rec is EFModels.{entity} && DataObject is DataObjects.{entity}) {{\n" +
            $"            var rec = Rec as EFModels.{entity};\n" +
            $"            var item = DataObject as DataObjects.{entity};\n\n" +
            "            if (rec != null && item != null) {\n" +
            "                //item.Property = rec.Property;\n" +
            "            }\n\n" +
            "            return;\n" +
            "        }\n\n";
        string src = "public partial class DataAccess\n{\n    private void GetDataApp(object Rec, object DataObject)\n    {\n" +
            Stub("Department") + Stub("Tag") + Stub("User") + "    }\n}\n";

        List<FixProposal> proposals = Propose(("FreeDemo.DataAccess/DataAccess.App.cs", src));

        Assert.NotEmpty(proposals);
        Assert.All(proposals, p => Assert.Equal(ProposalVerdict.Leave, p.Verdict));
        Assert.All(proposals, p => Assert.Null(p.Patch));
    }

    [Fact]
    public void Methods_differing_in_literals_share_a_helper_with_named_parameters()
    {
        static string Parse(string host) =>
            $"    Info Parse{host}(string url, Uri uri)\n    {{\n" +
            "        var path = Split(uri);\n" +
            $"        if (path == null) {{ return new Info {{ Url = url, Source = \"{host}\", Valid = false, Error = \"Bad {host} URL.\" }}; }}\n" +
            $"        return new Info {{ Url = url, Name = path.Name, Owner = path.Owner, Source = \"{host}\", Branch = \"main\", Valid = true }};\n" +
            "    }\n\n";
        string src = "class Validation\n{\n" + Parse("Bitbucket") + Parse("GitLab") + "}\n";

        FixProposal p = Assert.Single(Propose(("Demo.App.Validation.cs", src)));

        Assert.Equal(ProposalVerdict.Extract, p.Verdict);
        Assert.Contains(p.Holes, h => h.Role == "literal" && h.Parameter == "source");
        Assert.Contains(p.Holes, h => h.Role == "literal" && h.Parameter == "error");
        string rewritten = p.Rewritten["Demo.App.Validation.cs"];
        Assert.Contains("return Parse(\"Bitbucket\", \"Bad Bitbucket URL.\", url, uri);", rewritten);
        Assert.Contains("return Parse(\"GitLab\", \"Bad GitLab URL.\", url, uri);", rewritten);
        AssertParsesAndCloneIsGone("Demo.App.Validation.cs", rewritten);
        AssertHunksAreConsistent(p.Patch!);
    }

    [Fact]
    public void Near_copies_get_a_line_diff_and_no_patch()
    {
        static string Steps(string cls, int insertAt)
        {
            var body = new List<string>();
            for (int k = 0; k < 20; k++) {
                if (k == insertAt) {
                    body.Add("        Audit(total);");
                }

                body.Add($"        Step{k}(total, {k});");
            }

            return $"class {cls}\n{{\n    void Run(int total)\n    {{\n{string.Join("\n", body)}\n    }}\n}}\n";
        }

        FixProposal p = Assert.Single(Propose(("A.App.cs", Steps("A", -1)), ("B.App.cs", Steps("B", 10))));

        Assert.Equal(ProposalVerdict.Review, p.Verdict);
        Assert.Null(p.Patch);
        Assert.Contains(p.LineDiff!.Split('\n'), l => l.StartsWith('+') && l.Contains("Audit(total);"));
    }

    [Fact]
    public void Proposals_report_is_deterministic()
    {
        (string, string)[] files = [("Demo.App.Validation.cs", "class V { int A(int[] v) { int t = 0; foreach (var x in v) { if (x > 0) { t += x; } } return t; } int B(int[] w) { int s = 0; foreach (var y in w) { if (y > 0) { s += y; } } return s; } }")];
        CloneResult a = CloneDetector.Run(files, new CloneOptions());
        CloneResult b = CloneDetector.Run(files, new CloneOptions());

        Assert.Equal(
            CloneReport.ProposalsToMarkdown(a, ProposalBuilder.Build(a), "patches"),
            CloneReport.ProposalsToMarkdown(b, ProposalBuilder.Build(b), "patches"));
    }

    private static void AssertHunksAreConsistent(string patch)
    {
        // Each hunk's header counts must match its lines, or git apply rejects it.
        string[] lines = patch.Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            Match m = Regex.Match(lines[i], @"^@@ -\d+,(\d+) \+\d+,(\d+) @@$");
            if (!m.Success) {
                continue;
            }

            int oldCount = 0;
            int newCount = 0;
            for (int j = i + 1; j < lines.Length && !lines[j].StartsWith("@@") && !lines[j].StartsWith("diff "); j++) {
                if (lines[j].StartsWith(' ')) { oldCount++; newCount++; } else if (lines[j].StartsWith('-')) { oldCount++; } else if (lines[j].StartsWith('+')) { newCount++; }
            }

            Assert.Equal(int.Parse(m.Groups[1].Value), oldCount);
            Assert.Equal(int.Parse(m.Groups[2].Value), newCount);
        }
    }

    private static void AssertParsesAndCloneIsGone(string path, string rewritten)
    {
        Assert.Empty(CSharpSyntaxTree.ParseText(rewritten).GetDiagnostics());
        Assert.Empty(CloneDetector.Run([(path, rewritten)], new CloneOptions()).Classes);
    }

    private static List<FixProposal> Propose(params (string Path, string Text)[] files) =>
        ProposalBuilder.Build(CloneDetector.Run(files, new CloneOptions()));
}