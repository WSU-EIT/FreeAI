using FreeCodeReorganizer.Core;
using Xunit;

namespace FreeCodeReorganizer.Core.Tests;

/// <summary>
/// Verifies the reusable engine behaves the way the FreeCRM author's conventions require:
/// methods sorted alphabetically (ignoring public/private), fields left alone, by-purpose types
/// skipped, and the wrapped-parameter "){" brace restored. These run from the CLI / CI and don't
/// need Visual Studio.
/// </summary>
public class ReorganizerTests
{
    private static ReorgResult Run(string src, ReorderConfig? cfg = null)
        => Reorganizer.Run(src, cfg ?? new ReorderConfig(), "\r\n");

    [Fact]
    public void Methods_sort_alphabetically_ignoring_visibility()
    {
        // 8 methods, one LOCAL swap (private Foxtrot sits after public Golf). Sorting moves only those
        // two (25% < the 35% by-purpose threshold), so the type IS tidied and Foxtrot lands before Golf
        // — proving the sort is alphabetical and ignores public/private.
        var src =
            "class C\r\n{\r\n" +
            "    public void Alpha() { }\r\n\r\n" +
            "    public void Bravo() { }\r\n\r\n" +
            "    public void Charlie() { }\r\n\r\n" +
            "    public void Delta() { }\r\n\r\n" +
            "    public void Echo() { }\r\n\r\n" +
            "    public void Golf() { }\r\n\r\n" +
            "    private void Foxtrot() { }\r\n\r\n" +
            "    public void Hotel() { }\r\n" +
            "}\r\n";

        var r = Run(src);

        Assert.True(r.Changed);
        Assert.Null(r.Error);
        Assert.NotNull(r.NewText);
        // private Foxtrot now precedes public Golf — visibility is NOT a grouping key
        Assert.True(r.NewText!.IndexOf("Foxtrot") < r.NewText.IndexOf("Golf"));
    }

    [Fact]
    public void Already_ordered_is_left_unchanged()
    {
        var src =
            "class C\r\n{\r\n" +
            "    public void Alpha() { }\r\n\r\n" +
            "    public void Beta() { }\r\n" +
            "}\r\n";

        Assert.False(Run(src).Changed);
    }

    [Fact]
    public void Fields_are_never_reordered()
    {
        // zebra before apple — fields are by-purpose, so the engine leaves them put
        var src = "class C\r\n{\r\n    int zebra;\r\n    int apple;\r\n}\r\n";
        Assert.False(Run(src).Changed);
    }

    [Fact]
    public void By_purpose_type_is_skipped_when_too_unsorted()
    {
        // a deliberately reverse-ordered type: sorting would move most members, so it's treated
        // as hand-ordered and left alone (MaxFractionReordered guard).
        var src =
            "class C\r\n{\r\n" +
            "    public void Zulu() { }\r\n\r\n" +
            "    public void Yankee() { }\r\n\r\n" +
            "    public void Xray() { }\r\n\r\n" +
            "    public void Whiskey() { }\r\n" +
            "}\r\n";

        Assert.False(Run(src).Changed); // > 35% would move -> skipped
    }

    [Fact]
    public void Wrapped_parameter_brace_is_collapsed_to_paren_brace()
    {
        var src =
            "class C\r\n{\r\n" +
            "    void M(\r\n" +
            "        int a,\r\n" +
            "        int b)\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        var r = Run(src);

        Assert.True(r.Changed);
        Assert.Null(r.Error);
        Assert.Equal(1, r.BracesCollapsed);
        Assert.Contains("){", r.NewText);
    }

    [Fact]
    public void Single_line_parameters_keep_brace_on_own_line()
    {
        var src = "class C\r\n{\r\n    void M(int a)\r\n    {\r\n    }\r\n}\r\n";
        var r = Run(src);
        Assert.Equal(0, r.BracesCollapsed);
        Assert.False(r.Changed);
    }

    [Fact]
    public void Type_with_region_directive_is_skipped()
    {
        var src =
            "class C\r\n{\r\n" +
            "    #region stuff\r\n" +
            "    public void Beta() { }\r\n" +
            "    public void Alpha() { }\r\n" +
            "    #endregion\r\n" +
            "}\r\n";

        Assert.False(Run(src).Changed); // SkipTypesWithDirectives
    }
}