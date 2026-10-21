using FreeCodeClones.Core;
using Xunit;

namespace FreeCodeClones.Core.Tests;

/// <summary>
/// The detector's contract: renamed copies match, literals and invoked method names are treated as the modes say,
/// pasted statement runs are found once as maximal windows, near misses survive an inserted statement, Razor line
/// numbers map back, the baseline gates, and the output is deterministic.
/// </summary>
public class CloneDetectorTests
{
    private const string SumA =
        "class A\n{\n" +
        "    int Sum(int[] values)\n" +
        "    {\n" +
        "        int total = 0;\n" +
        "        foreach (var v in values)\n" +
        "        {\n" +
        "            if (v > 0) { total += v; }\n" +
        "        }\n" +
        "        return total;\n" +
        "    }\n" +
        "}\n";

    private const string SumB =
        "class B\n{\n" +
        "    public int Add(int[] items)\n" +
        "    {\n" +
        "        int sum = 0;\n" +
        "        foreach (var item in items)\n" +
        "        {\n" +
        "            if (item > 0) { sum += item; }\n" +
        "        }\n" +
        "        return sum;\n" +
        "    }\n" +
        "}\n";

    [Fact]
    public void Baseline_accepts_known_ids_and_fails_on_a_new_one()
    {
        CloneResult r = Scan(null, ("A.cs", SumA), ("B.cs", SumB));
        List<BaselineEntry> accepted = Baseline.Read(Baseline.Write(r.Classes));

        BaselineCheck pass = Baseline.Check(r.Classes, accepted);
        Assert.Empty(pass.New);
        Assert.Equal(0, pass.ExitCode);

        BaselineCheck fail = Baseline.Check(r.Classes, [new BaselineEntry("0000000000000000", "an older duplicate")]);
        Assert.Single(fail.New);
        Assert.Single(fail.Stale);
        Assert.Equal(1, fail.ExitCode);
    }

    [Fact]
    public void Baseline_entry_without_a_reason_is_rejected()
    {
        Assert.Throws<FormatException>(() => Baseline.Read("{ \"accepted\": [ { \"id\": \"abc\" } ] }"));
        Assert.Throws<FormatException>(() => Baseline.Read("{ \"accepted\": [ { \"id\": \"abc\", \"reason\": \" \" } ] }"));
    }

    [Fact]
    public void Different_invoked_method_is_a_clone_only_in_blind_mode()
    {
        static string Clean(string cls, string method) =>
            $"class {cls}\n{{\n" +
            "    string Clean(string text, char a, char b)\n" +
            "    {\n" +
            $"        var result = text.{method}(a, b);\n" +
            $"        result = result.{method}(b, a);\n" +
            "        if (result.Length == 0) { return text; }\n" +
            "        return result;\n" +
            "    }\n" +
            "}\n";

        (string, string)[] files = [("A.cs", Clean("A", "Replace")), ("B.cs", Clean("B", "Trim"))];

        Assert.Empty(Scan(null, files).Classes);

        CloneClass blind = Assert.Single(Scan(new CloneOptions { Mode = CloneMode.Blind }, files).Classes);
        Assert.Equal(CloneKind.Exact, blind.Kind);
        Assert.Equal(2, blind.Members.Count);
    }

    [Fact]
    public void Equivalence_reports_where_two_prefix_strippers_disagree()
    {
        static string ByReplace(string s) => s.Replace("refs/heads/", "");
        static string ByPrefix(string s) => s.StartsWith("refs/heads/") ? s.Substring("refs/heads/".Length) : s;

        var differences = Equivalence.Compare<string?, string>(
            s => ByReplace(s!), s => ByPrefix(s!), Inputs.Strings(Inputs.AroundPrefix("refs/heads/")));

        Assert.Contains(differences, d => d.Input == "x/refs/heads/y");
        Assert.DoesNotContain(differences, d => d.Input is null); // both throw NullReferenceException: same outcome
        Assert.DoesNotContain(differences, d => d.Input == "refs/heads/x");
    }

    [Fact]
    public void Exhaustive_inputs_cover_every_combination()
    {
        Assert.Equal(2 * 3 * 3, Inputs.Exhaustive<bool, DayKind, bool?>().Count());
        Assert.Throws<ArgumentException>(() => Inputs.Exhaustive<int>().ToList());
    }

    [Fact]
    public void Literal_only_difference_is_exact_unless_literals_are_kept()
    {
        static string Check(string cls, string field) =>
            $"class {cls}\n{{\n" +
            "    void Check(string name)\n" +
            "    {\n" +
            $"        if (string.IsNullOrEmpty(name)) {{ throw new ArgumentException(\"{field} is required\"); }}\n" +
            $"        if (name.Length > 50) {{ throw new ArgumentException(\"{field} is too long\"); }}\n" +
            "        Console.WriteLine(name);\n" +
            "        Log(name);\n" +
            "        Save(name);\n" +
            "    }\n" +
            "}\n";

        (string, string)[] files = [("A.cs", Check("A", "Name")), ("B.cs", Check("B", "Title"))];

        CloneClass byDefault = Assert.Single(Scan(null, files).Classes);
        Assert.Equal(CloneKind.Exact, byDefault.Kind);

        Assert.DoesNotContain(Scan(new CloneOptions { KeepLiterals = true }, files).Classes, c => c.Kind == CloneKind.Exact);
    }

    [Fact]
    public void One_inserted_statement_in_a_20_statement_copy_is_a_near_clone()
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

        CloneResult r = Scan(null, ("A.cs", Steps("A", -1)), ("B.cs", Steps("B", 10)));

        // The two exact halves sit inside the near pair, so the near class is all that is reported.
        CloneClass near = Assert.Single(r.Classes);
        Assert.Equal(CloneKind.Near, near.Kind);
        Assert.True(near.Similarity >= 0.85, $"similarity {near.Similarity}");
        Assert.Equal(["A.cs", "B.cs"], near.Members.Select(m => m.Path));
    }

    [Fact]
    public void Output_is_deterministic()
    {
        (string, string)[] files = [("B.cs", SumB), ("A.cs", SumA), ("C.cs", SumA.Replace("class A", "class C"))];

        string first = CloneReport.ToJson(Scan(null, files));
        string second = CloneReport.ToJson(Scan(null, files.Reverse().ToArray()));

        Assert.Equal(first, second);
        Assert.Equal(CloneReport.ToMarkdown(Scan(null, files)), CloneReport.ToMarkdown(Scan(null, files)));
    }

    [Fact]
    public void Pasted_block_inside_two_larger_methods_is_one_window_clone()
    {
        const string first =
            "class A\n{\n" +
            "    void First(string prefix, int value)\n" +
            "    {\n" +
            "        int doubled = value * 2;\n" +
            "        if (doubled > 10) { Console.Write(doubled); }\n" +
            "        var builder = new StringBuilder();\n" +          // line 7
            "        builder.Append(prefix);\n" +
            "        builder.Append(':');\n" +
            "        builder.Append(value);\n" +
            "        var text = builder.ToString();\n" +
            "        Console.WriteLine(text);\n" +                     // line 12
            "        Save(doubled);\n" +
            "    }\n" +
            "}\n";
        const string second =
            "class B\n{\n" +
            "    bool Second(string pre, string val, List<int> list)\n" +
            "    {\n" +
            "        list.Clear();\n" +
            "        foreach (var i in list) { Debug.Print(i); }\n" +
            "        while (list.Count > 3) { list.RemoveAt(0); }\n" +
            "        var sb = new StringBuilder();\n" +               // line 8
            "        sb.Append(pre);\n" +
            "        sb.Append(':');\n" +
            "        sb.Append(val);\n" +
            "        var line = sb.ToString();\n" +
            "        Console.WriteLine(line);\n" +                     // line 13
            "        return list.Count == 0;\n" +
            "    }\n" +
            "}\n";

        CloneClass c = Assert.Single(Scan(null, ("A.cs", first), ("B.cs", second)).Classes);

        Assert.Equal(CloneKind.Exact, c.Kind);
        Assert.Equal(6, c.Statements);
        Assert.All(c.Members, m => Assert.Equal("window", m.Kind));
        Assert.Equal(["A.cs:7-12", "B.cs:8-13"], c.Members.Select(m => m.Location));
    }

    [Fact]
    public void Razor_code_block_lines_map_back_to_the_razor_file()
    {
        string razor = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Sample.App.razor"));

        CloneClass c = Assert.Single(Scan(null, ("Pages/Sample.App.razor", razor)).Classes);

        Assert.Equal(["Pages/Sample.App.razor:10-18", "Pages/Sample.App.razor:20-28"], c.Members.Select(m => m.Location));
    }

    [Fact]
    public void Renamed_copy_is_an_exact_clone()
    {
        CloneClass c = Assert.Single(Scan(null, ("A.cs", SumA), ("B.cs", SumB)).Classes);

        Assert.Equal(CloneKind.Exact, c.Kind);
        Assert.All(c.Members, m => Assert.Equal("method", m.Kind));
        Assert.Equal(["A.cs:3-11", "B.cs:3-11"], c.Members.Select(m => m.Location));
    }

    [Fact]
    public void Default_filters_select_the_app_layer()
    {
        Assert.True(SourceSet.GlobMatch("**/FreeCICD.App.*.cs", "FreeCICD/Controllers/FreeCICD.App.API.cs", false));
        Assert.True(SourceSet.GlobMatch("**/*.App.cs", "FreeCICD.DataAccess/DataAccess.App.cs", false));
        Assert.True(SourceSet.GlobMatch("**/FreeCICD.App.*.razor", "FreeCICD.App.UI.razor", false));
        Assert.False(SourceSet.GlobMatch("**/*.App.cs", "FreeCICD.DataAccess/DataAccess.cs", false));
        Assert.True(SourceSet.GlobMatch("Generated", "Src/Generated/X.App.cs", true));
    }

    private static CloneResult Scan(CloneOptions? options, params (string Path, string Text)[] files)
        => CloneDetector.Run(files, options ?? new CloneOptions());

    private enum DayKind
    {
        Weekday,
        Weekend,
        Holiday
    }
}