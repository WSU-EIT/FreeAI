using System.ComponentModel;
using Community.VisualStudio.Toolkit;
using Core = FreeCodeReorganizer.Core;

namespace FreeCodeReorganizer.Options
{
    /// <summary>
    /// User-configurable settings, shown under Tools &gt; Options &gt; FreeCodeReorganizer &gt; General.
    ///
    /// Built on the toolkit's <see cref="BaseOptionModel{T}"/>:
    ///   * it persists each public property to the VS settings store automatically;
    ///   * <see cref="BaseOptionModel{T}.GetLiveInstanceAsync"/> (used by the command) returns the
    ///     current, saved values;
    ///   * the nested <see cref="DialogPageProvider"/> is the <see cref="DialogPage"/> that
    ///     [ProvideOptionPage(typeof(GeneralOptions.DialogPageProvider), ...)] points at, so VS
    ///     renders these properties (with the Category/DisplayName/Description below) in the grid.
    ///
    /// Each property maps to a field of <see cref="Core.ReorderConfig"/>; see
    /// <see cref="ToReorderConfig"/> for the mapping. Defaults mirror ReorderConfig's own defaults
    /// (which reproduce the FreeCRM house style).
    ///
    /// VERIFY IN VS: BaseOptionModel&lt;T&gt;/BaseOptionPage&lt;T&gt; live in
    /// Community.VisualStudio.Toolkit; the nested provider pattern is the toolkit's documented way
    /// to expose a model as an options page.
    /// </summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        private const string CatPipeline = "Pipeline";
        private const string CatSorting = "Sorting";
        private const string CatBraceStyle = "Brace style";
        private const string CatSafety = "Safety";

        [Category(CatPipeline)]
        [DisplayName("Run editorconfig format first")]
        [Description("Before reorganizing, run the editor's Format Document (which respects this repo's .editorconfig) so standard formatting is applied first. FreeCodeReorganizer then layers only the gaps .editorconfig can't express (member ordering + the \"){\" brace). Turn off for reorganize-only.")]
        [DefaultValue(true)]
        public bool RunEditorConfigFormatFirst { get; set; } = true;

        [Category(CatSorting)]
        [DisplayName("Sort alphabetically")]
        [Description("Sort members alphabetically within each kind group (properties, indexers and methods are interleaved into one alphabetical list).")]
        [DefaultValue(true)]
        public bool SortAlphabetically { get; set; } = true;

        [Category(CatSorting)]
        [DisplayName("Ignore leading underscore in sort")]
        [Description("When comparing names, ignore a single leading underscore so _connectionString sorts as \"connectionString\".")]
        [DefaultValue(true)]
        public bool IgnoreLeadingUnderscoreInSort { get; set; } = true;

        [Category(CatSorting)]
        [DisplayName("Group by visibility")]
        [Description("Group members by accessibility (public first) before sorting alphabetically. Off by default — FreeCRM interleaves public/private in pure alphabetical order.")]
        [DefaultValue(false)]
        public bool GroupByVisibility { get; set; } = false;

        [Category(CatSorting)]
        [DisplayName("Static members first")]
        [Description("Within a kind group, place static members before instance members. Off by default to match FreeCRM (purely alphabetical).")]
        [DefaultValue(false)]
        public bool StaticMembersFirst { get; set; } = false;

        [Category(CatBraceStyle)]
        [DisplayName("Collapse wrapped-parameter brace")]
        [Description("When a parameter list is wrapped across multiple lines, glue the closing ) and the body's opening { together as \"){\" on one line — the FreeCRM author's hand style.")]
        [DefaultValue(true)]
        public bool CollapseWrappedParameterBrace { get; set; } = true;

        [Category(CatSafety)]
        [DisplayName("Max fraction reordered")]
        [Description("If sorting would move more than this fraction (0..1) of a type's sortable members, the type is treated as deliberately ordered by purpose and left untouched. Set 1.0 to always sort.")]
        [DefaultValue(0.35)]
        public double MaxFractionReordered { get; set; } = 0.35;

        /// <summary>
        /// Projects these UI options onto a <see cref="Core.ReorderConfig"/> for the engine.
        /// Only the fields surfaced in the UI are overridden; everything else (KindOrder,
        /// DoNotSortKinds, VisibilityOrder, SkipTypesWithDirectives) keeps the engine's defaults.
        /// </summary>
        public Core.ReorderConfig ToReorderConfig()
        {
            return new Core.ReorderConfig {
                SortAlphabetically = SortAlphabetically,
                IgnoreLeadingUnderscoreInSort = IgnoreLeadingUnderscoreInSort,
                GroupByVisibility = GroupByVisibility,
                StaticMembersFirst = StaticMembersFirst,
                CollapseWrappedParameterBrace = CollapseWrappedParameterBrace,
                MaxFractionReordered = MaxFractionReordered,
            };
        }

        /// <summary>
        /// The <see cref="DialogPage"/> that backs the Tools &gt; Options page. The toolkit's
        /// <see cref="BaseOptionPage{T}"/> renders <see cref="GeneralOptions"/>' properties; the
        /// [ProvideOptionPage] attribute on the package references this exact type.
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(true)]
        public class DialogPageProvider : BaseOptionPage<GeneralOptions> { }
    }
}