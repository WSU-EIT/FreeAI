using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FreeCodeMaid;

/// <summary>
/// Classifies a member declaration into the facets the ordering rules care about:
/// its kind, sort name, accessibility, and static-ness.
/// </summary>
public static class MemberClassifier
{
    public static string KindOf(MemberDeclarationSyntax m) => m switch
    {
        FieldDeclarationSyntax f when f.Modifiers.Any(t => t.IsKind(SyntaxKind.ConstKeyword)) => "Const",
        FieldDeclarationSyntax => "Field",
        ConstructorDeclarationSyntax => "Constructor",
        DestructorDeclarationSyntax => "Destructor",
        DelegateDeclarationSyntax => "Delegate",
        EventDeclarationSyntax or EventFieldDeclarationSyntax => "Event",
        EnumDeclarationSyntax => "Enum",
        InterfaceDeclarationSyntax => "Interface",
        PropertyDeclarationSyntax => "Property",
        IndexerDeclarationSyntax => "Indexer",
        ConversionOperatorDeclarationSyntax => "ConversionOperator",
        OperatorDeclarationSyntax => "Operator",
        MethodDeclarationSyntax => "Method",
        RecordDeclarationSyntax => "Record",
        StructDeclarationSyntax => "Struct",
        ClassDeclarationSyntax => "Class",
        _ => "Other"
    };

    public static string NameOf(MemberDeclarationSyntax m) => m switch
    {
        FieldDeclarationSyntax f => FirstVariable(f.Declaration),
        EventFieldDeclarationSyntax e => FirstVariable(e.Declaration),
        PropertyDeclarationSyntax p => p.Identifier.Text,
        EventDeclarationSyntax ev => ev.Identifier.Text,
        MethodDeclarationSyntax me => me.Identifier.Text,
        ConstructorDeclarationSyntax c => c.Identifier.Text,
        DelegateDeclarationSyntax d => d.Identifier.Text,
        EnumDeclarationSyntax en => en.Identifier.Text,
        OperatorDeclarationSyntax o => o.OperatorToken.Text,
        IndexerDeclarationSyntax => "this[]",
        BaseTypeDeclarationSyntax bt => bt.Identifier.Text, // class / struct / interface / record
        _ => ""
    };

    public static bool IsStatic(MemberDeclarationSyntax m)
        => m.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword));

    public static string Visibility(MemberDeclarationSyntax m)
    {
        var mods = m.Modifiers;
        bool pub = mods.Any(t => t.IsKind(SyntaxKind.PublicKeyword));
        bool priv = mods.Any(t => t.IsKind(SyntaxKind.PrivateKeyword));
        bool prot = mods.Any(t => t.IsKind(SyntaxKind.ProtectedKeyword));
        bool intl = mods.Any(t => t.IsKind(SyntaxKind.InternalKeyword));

        if (pub) return "public";
        if (prot && intl) return "protected internal";
        if (priv && prot) return "private protected";
        if (intl) return "internal";
        if (prot) return "protected";
        return "private"; // explicit private or the default for a type member
    }

    private static string FirstVariable(VariableDeclarationSyntax decl)
        => decl.Variables.Count > 0 ? decl.Variables[0].Identifier.Text : "";
}

/// <summary>
/// Orders members by (kind, [visibility], [static], [name]) according to the active config.
/// Returns 0 when two members are interchangeable so a stable sort preserves their original order.
/// </summary>
public sealed class MemberComparer : IComparer<MemberDeclarationSyntax>
{
    private readonly ReorderConfig _config;
    private readonly Dictionary<string, int> _kindRank;
    private readonly Dictionary<string, int> _visRank;
    private readonly HashSet<string> _doNotSort;

    public MemberComparer(ReorderConfig config)
    {
        _config = config;
        _kindRank = Rank(config.KindOrder);
        _visRank = Rank(config.VisibilityOrder);
        _doNotSort = new HashSet<string>(config.DoNotSortKinds, StringComparer.OrdinalIgnoreCase);
    }

    public int Compare(MemberDeclarationSyntax? x, MemberDeclarationSyntax? y)
    {
        if (x is null || y is null) return 0;

        var kindX = MemberClassifier.KindOf(x);
        var kindY = MemberClassifier.KindOf(y);

        int c = KindRank(kindX).CompareTo(KindRank(kindY));
        if (c != 0) return c;

        // Same kind from here on.
        if (_config.GroupByVisibility)
        {
            c = VisRank(x).CompareTo(VisRank(y));
            if (c != 0) return c;
        }

        if (_config.StaticMembersFirst)
        {
            int sx = MemberClassifier.IsStatic(x) ? 0 : 1;
            int sy = MemberClassifier.IsStatic(y) ? 0 : 1;
            c = sx.CompareTo(sy);
            if (c != 0) return c;
        }

        if (_config.SortAlphabetically && !_doNotSort.Contains(kindX))
        {
            c = string.Compare(SortName(x), SortName(y), StringComparison.OrdinalIgnoreCase);
            if (c != 0) return c;
            c = string.Compare(SortName(x), SortName(y), StringComparison.Ordinal);
            if (c != 0) return c;
        }

        return 0; // interchangeable -> stable sort keeps original order
    }

    private static Dictionary<string, int> Rank(List<string> list)
    {
        // Each entry may list several kinds separated by commas; all kinds in one entry share the
        // same rank, so they sort together (e.g. "Property,Indexer,Method" interleaves alphabetically).
        var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < list.Count; i++)
        {
            foreach (var kind in list[i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                d[kind] = i;
            }
        }
        return d;
    }

    private int KindRank(string kind) => _kindRank.TryGetValue(kind, out var r) ? r : int.MaxValue;

    private int VisRank(MemberDeclarationSyntax m)
        => _visRank.TryGetValue(MemberClassifier.Visibility(m), out var r) ? r : int.MaxValue;

    private string SortName(MemberDeclarationSyntax m)
    {
        var n = MemberClassifier.NameOf(m);
        if (_config.IgnoreLeadingUnderscoreInSort)
        {
            n = n.TrimStart('_');
        }
        return n;
    }
}
