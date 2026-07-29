namespace Try.Core
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Razor.Language;

    internal class CompileToCSharpResult
    {
        public string Code { get; set; }

        public IEnumerable<CompilationDiagnostic> Diagnostics { get; set; } = [];

        public string FilePath { get; set; }
        public RazorProjectItem ProjectItem { get; set; }
    }
}
