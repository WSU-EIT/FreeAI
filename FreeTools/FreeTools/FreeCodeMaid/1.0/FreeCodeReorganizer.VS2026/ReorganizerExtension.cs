// FreeCodeReorganizer.VS2026 — extension entry point (VisualStudio.Extensibility model).
namespace FreeCodeReorganizer.VS2026;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

/// <summary>
/// The extension entry point. The new model discovers contributions ([VisualStudioContribution]
/// commands and settings) via this class and generates the VSIX manifest from the metadata below —
/// there is no .vsixmanifest / .vsct / .pkgdef to hand-author (unlike the classic front-end).
/// </summary>
[VisualStudioContribution]
public class ReorganizerExtension : Extension
{
    /// <inheritdoc/>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "FreeCodeReorganizer.VS2026.b8e4d1c0-5a3f-4e21-9c7b-2d6f8a0e3b14",
            version: this.ExtensionAssemblyVersion,
            publisherName: "WSU-EIT",
            displayName: "FreeCodeReorganizer (2026)",
            description: "Reorganizes C# class members alphabetically and restores the wrapped-parameter \"){\" brace style. Native VS 2026 build on VisualStudio.Extensibility."),
    };

    /// <inheritdoc/>
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
