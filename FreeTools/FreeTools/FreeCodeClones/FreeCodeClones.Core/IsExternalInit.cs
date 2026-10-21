// netstandard2.0 shim: records / init-only setters need this marker type, which the
// netstandard2.0 reference assemblies do not provide. Declaring it here lets the C#
// compiler emit init accessors and positional records.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}