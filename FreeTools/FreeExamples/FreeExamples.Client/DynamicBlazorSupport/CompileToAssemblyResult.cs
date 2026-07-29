namespace Try.Core
{
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    public class CompileToAssemblyResult
    {
        private Assembly? _Assembly = null;
        public byte[]? AssemblyBytes { get; set; }
        public Compilation? Compilation { get; set; }
        public bool Compiled => AssemblyBytes != null && AssemblyBytes.Length > 0;
        public IEnumerable<CompilationDiagnostic> Diagnostics { get; set; } = [];
        
        public Assembly? LoadAssembly()
        {
            _Assembly ??= AssemblyBytes == null ? null : System.AppDomain.CurrentDomain.Load(AssemblyBytes);
            return _Assembly;
        }
    }
}
