using System.Collections.Generic;

namespace FreeCodeReorganizer.Core;

/// <summary>
/// The modular, customizable rule set that controls how the reorganizer orders the members
/// of a C# type. Only the ordering-rule properties live here in the Core engine; file
/// scanning, glob exclusion, and JSON loading stay in the front-ends (CLI / VS extension).
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

    // ---- Brace / wrapping style -------------------------------------------

    /// <summary>
    /// Reformat a method/constructor whose parameter list is ALREADY wrapped across multiple lines into
    /// the FreeCRM author's hand style:
    /// <code>
    ///     public DataController          &lt;- name alone on the declaration line
    ///     (                              &lt;- '(' alone, at the member's indent
    ///         IDataAccess daInjection,   &lt;- each parameter one level deeper
    ///         Plugins.IPlugins diPlugins
    ///     ){                             &lt;- ')' glued to the body's '{' as "){"
    /// </code>
    /// `dotnet format` can't express any of this (it puts '(' on the name line and splits ")" + "{"),
    /// so the reorganizer restores it. Only fires when the parameters are ALREADY on multiple lines —
    /// single-line declarations are never force-wrapped. If the whole signature would instead fit on one
    /// line within <see cref="MaxLineWidth"/>, it is collapsed back to a single line (normal Allman brace).
    /// Set false to leave parameter braces and indentation completely alone.
    /// </summary>
    public bool CollapseWrappedParameterBrace { get; set; } = true;

    /// <summary>
    /// Re-indent the wrapped attributes of a Razor/Blazor element so every continuation attribute sits
    /// at the element's indent PLUS two levels ("double tab"). The first attribute stays on the tag line;
    /// the rest line up at base+2, which keeps them visually deeper than the element's child body (base+1):
    /// <code>
    ///     &lt;PagedRecordset ActionHandlers="ActionHandlers"
    ///             CenterItems="CenterItems"          &lt;- base + 2 levels
    ///             Configuration="Config" /&gt;
    /// </code>
    /// Only re-indents elements whose attributes are ALREADY wrapped across lines, and ONLY touches
    /// leading whitespace inside the start tag — every byte of rendered markup is preserved. If the whole
    /// tag would fit on one line within <see cref="MaxLineWidth"/>, it is collapsed to a single line.
    /// </summary>
    public bool IndentWrappedRazorAttributes { get; set; } = true;

    /// <summary>
    /// The line width (in characters) at or below which an ALREADY-wrapped parameter list or Razor start
    /// tag is collapsed back onto a single line instead of being kept in the multi-line house style.
    /// Anything whose single-line form is wider than this stays wrapped. Never causes a single-line
    /// declaration to be wrapped — the reorganizer does not force-wrap. Default 120.
    /// </summary>
    public int MaxLineWidth { get; set; } = 120;

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

    /// <summary>Skip any type that contains <c>#if</c> / <c>#pragma</c> (and, when
    /// <see cref="RespectRegions"/> is off, <c>#region</c>) directives between its members — reordering
    /// across those would scramble them.</summary>
    public bool SkipTypesWithDirectives { get; set; } = true;

    /// <summary>
    /// Respect <c>#region</c> / <c>#endregion</c> boundaries. When true (the default), a type whose only
    /// directives are regions is left completely untouched so the regions — and the members inside them —
    /// keep their exact layout. When false, region directives are ignored and members may be reordered
    /// across them (a re-parse safety net still aborts anything that would unbalance the region pairing).
    /// </summary>
    public bool RespectRegions { get; set; } = true;

    /// <summary>
    /// Skip any type that contains paired template markers like FreeCRM's
    /// <c>// {{ModuleItemStart:X}}</c> / <c>// {{ModuleItemEnd:X}}</c> comments. Those markers PIN
    /// members to fixed positions — the template/plugin system splices code in and out by them — so
    /// reordering across them would break the Start/End pairing. Any type that has even one such marker
    /// is left entirely byte-for-byte alone. This is a safety rail, on by default; it is NOT a style
    /// choice and shouldn't normally be turned off.
    /// </summary>
    public bool SkipTypesWithModuleMarkers { get; set; } = true;

    /// <summary>The comment substrings that identify a "pinned" template region (see
    /// <see cref="SkipTypesWithModuleMarkers"/>). A type whose comments contain ANY of these is skipped.</summary>
    public List<string> ModuleMarkerTokens { get; set; } =
    [
        "{{ModuleItemStart",
        "{{ModuleItemEnd"
    ];
}