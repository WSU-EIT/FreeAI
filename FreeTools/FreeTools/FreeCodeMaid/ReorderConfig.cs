using System.Text.Json;

namespace FreeCodeMaid;

/// <summary>
/// The modular, customizable rule set that controls how FreeCodeMaid reorganizes the members
/// of a C# type. Loaded from a <c>freecodemaid.json</c> file when one is found; otherwise the
/// FreeCRM-tuned defaults baked in here are used.
/// </summary>
public sealed class ReorderConfig
{
    // ---- Sorting behavior -------------------------------------------------

    /// <summary>Sort members alphabetically within each kind group.</summary>
    public bool SortAlphabetically { get; set; } = true;

    /// <summary>
    /// Ignore a single leading underscore when comparing names. Reproduces the FreeCRM field
    /// convention where <c>_connectionString</c> sorts as "connectionString" and the non-underscore
    /// DI field <c>data</c> slots in alphabetically by its bare name.
    /// </summary>
    public bool IgnoreLeadingUnderscoreInSort { get; set; } = true;

    /// <summary>Group members by accessibility (public first) before sorting alphabetically.
    /// Off by default: FreeCRM interleaves public/private methods in pure alphabetical order.</summary>
    public bool GroupByVisibility { get; set; } = false;

    /// <summary>Within a kind group, place static members before instance members.
    /// Off by default to match FreeCRM (purely alphabetical).</summary>
    public bool StaticMembersFirst { get; set; } = false;

    /// <summary>
    /// If sorting would move more than this fraction (0..1) of a type's sortable members, the type is
    /// treated as deliberately ordered BY PURPOSE and left completely untouched. This keeps the tool
    /// from flattening hand-curated orderings (controllers, nested DTOs, plugin examples) while still
    /// tidying types that are already mostly alphabetical. Set to 1.0 to always sort.
    /// </summary>
    public double MaxFractionReordered { get; set; } = 0.35;

    // ---- Brace style ------------------------------------------------------

    /// <summary>
    /// When a method/constructor's parameter list is wrapped across multiple lines, put the closing
    /// ")" and the body's opening "{" together on one line as "){" — the FreeCRM author's hand style.
    /// `dotnet format` (per `csharp_new_line_before_open_brace`) splits these to ")" + "{" on
    /// separate lines and the editorconfig has no rule to reproduce "){", so FreeCodeMaid restores it.
    /// Only affects declarations whose parameters are ALREADY wrapped onto multiple lines; single-line
    /// declarations keep the normal brace-on-its-own-line form. Set false to leave braces alone.
    /// </summary>
    public bool CollapseWrappedParameterBrace { get; set; } = true;

    /// <summary>
    /// Run <c>dotnet format whitespace</c> on the target BEFORE FreeCodeMaid's own passes, so a single
    /// FreeCodeMaid invocation does the whole standardization (whitespace cleanup → member reorder →
    /// restore <c>){</c>). It must run first because <c>dotnet format</c> splits <c>){</c> apart, and
    /// FreeCodeMaid then puts it back. Only runs in <c>--apply</c> mode (the formatter writes files).
    /// Requires the <c>dotnet</c> CLI on PATH and an <c>.editorconfig</c> in the target. If the
    /// formatter can't run, FreeCodeMaid warns and continues with its own passes.
    /// </summary>
    public bool RunFormatterFirst { get; set; } = true;

    // ---- Ordering tables --------------------------------------------------

    /// <summary>
    /// The order member kinds are emitted in (top-to-bottom). Entries may list several kinds
    /// separated by commas; kinds in the same entry share a rank and therefore sort TOGETHER.
    /// The default reproduces the FreeCRM convention: fields stay grouped at the top in their
    /// original (by-purpose) order, and properties + indexers + methods form one interleaved
    /// alphabetical list.
    /// </summary>
    public List<string> KindOrder { get; set; } =
    [
        "Const",
        "Field",
        "Constructor,Destructor",
        "Delegate,Event",
        "Property,Indexer,Method",
        "Operator,ConversionOperator",
        "Enum,Interface,Struct,Class,Record"
    ];

    /// <summary>Accessibility order, used only when <see cref="GroupByVisibility"/> is true.</summary>
    public List<string> VisibilityOrder { get; set; } =
    [
        "public", "internal", "protected internal", "protected", "private protected", "private"
    ];

    /// <summary>
    /// Kinds whose members keep their original relative order (never alphabetized). By default only
    /// properties, indexers and methods are sorted; everything else is preserved as the author wrote
    /// it. Crucially this includes <b>Field</b> and <b>Const</b>: the author groups fields by purpose
    /// (services, then state) rather than alphabetically, so we must not reorder them.
    /// </summary>
    public List<string> DoNotSortKinds { get; set; } =
    [
        "Const", "Field", "Constructor", "Destructor", "Delegate", "Event",
        "Operator", "ConversionOperator", "Enum", "Interface", "Struct", "Class", "Record"
    ];

    // ---- Safety / scope ---------------------------------------------------

    /// <summary>Skip any type that contains <c>#region</c> / <c>#if</c> / <c>#pragma</c> directives
    /// between its members — reordering across those would scramble them.</summary>
    public bool SkipTypesWithDirectives { get; set; } = true;

    /// <summary>Skip files whose header marks them as machine-generated.</summary>
    public bool SkipAutoGenerated { get; set; } = true;

    /// <summary>Directory/file globs that are never even read (build output, vendored, generated).</summary>
    public List<string> Exclude { get; set; } =
    [
        "**/bin/**", "**/obj/**", "**/.git/**", "**/.vs/**",
        "**/node_modules/**", "**/packages/**",
        "**/wwwroot/lib/**", "**/wwwroot/js/**", "**/wwwroot/fontawesome/**",
        "**/EFModels/**", "**/DynamicBlazorSupport/**"
    ];

    /// <summary>Globs for files that ARE valid hand-written source but whose member order is
    /// intentional ("by-purpose") or generated — reported as skipped, never reorganized.</summary>
    public List<string> SkipFiles { get; set; } =
    [
        "**/*.Designer.cs", "**/*.g.cs", "**/*.g.i.cs",
        "**/*.AssemblyInfo.cs", "**/*.GlobalUsings.g.cs", "**/GlobalUsings.cs",
        // FreeCRM by-purpose files: DTOs lead with primary key + TenantId, audit fields grouped;
        // the DataController root holds a purposeful (non-alphabetical) field block.
        "**/DataObjects*.cs", "**/DataController.cs"
    ];

    // ---- Loading ----------------------------------------------------------

    /// <summary>
    /// Resolve the effective config: an explicit path wins; otherwise search for a
    /// <c>freecodemaid.json</c> from the target upward; otherwise use built-in defaults.
    /// </summary>
    public static (ReorderConfig config, string source) Resolve(string? explicitPath, string target)
    {
        if (!string.IsNullOrEmpty(explicitPath))
        {
            return (Load(explicitPath), Path.GetFullPath(explicitPath));
        }

        var dir = File.Exists(target)
            ? Path.GetDirectoryName(Path.GetFullPath(target))
            : Path.GetFullPath(target);

        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "freecodemaid.json");
            if (File.Exists(candidate))
            {
                return (Load(candidate), candidate);
            }
            dir = Path.GetDirectoryName(dir);
        }

        return (new ReorderConfig(), "built-in defaults (FreeCRM-tuned)");
    }

    public static ReorderConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        return JsonSerializer.Deserialize<ReorderConfig>(json, options) ?? new ReorderConfig();
    }
}
