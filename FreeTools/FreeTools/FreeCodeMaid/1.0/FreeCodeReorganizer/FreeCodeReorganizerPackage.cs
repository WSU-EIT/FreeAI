using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace FreeCodeReorganizer
{
    /// <summary>
    /// The VS package entry point. An <see cref="AsyncPackage"/> is loaded by Visual Studio when
    /// one of its commands is first invoked (UI-context / on-demand), so startup stays cheap.
    ///
    /// The attributes below are what actually register the extension with VS:
    ///   * [PackageRegistration]        - tells VS this is a package and to use async loading.
    ///   * [InstalledProductRegistration] - the Help > About entry.
    ///   * [ProvideMenuResource]        - binds the compiled .vsct (Menus.ctmenu, resource id 1)
    ///                                    so our Edit-menu / context-menu command shows up.
    ///   * [Guid]                       - MUST equal guidFreeCodeReorganizerPackage in the .vsct.
    ///   * [ProvideOptionPage]          - adds Tools > Options > FreeCodeReorganizer > General.
    ///
    /// VERIFY IN VS: that the [Guid] string here is identical to the package GUID in
    /// VSCommandTable.vsct, and that PackageGuids.FreeCodeReorganizerString (toolkit-generated)
    /// resolves. If you renamed symbols in the .vsct, regenerate by building once in VS.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(
        "FreeCodeReorganizer",
        "Reorganizes C# class members alphabetically and restores the wrapped-parameter `){` brace style.",
        "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    // Tools > Options > FreeCodeReorganizer > General.
    [ProvideOptionPage(
        typeof(Options.GeneralOptions.DialogPageProvider),
        "FreeCodeReorganizer",
        "General",
        0,
        0,
        supportsAutomation: true)]
    public sealed class FreeCodeReorganizerPackage : ToolkitPackage
    {
        /// <summary>
        /// Package GUID. KEEP IN SYNC with guidFreeCodeReorganizerPackage in VSCommandTable.vsct.
        /// </summary>
        public const string PackageGuidString = "fe5de5f2-7387-490d-ada3-531012c2c4db";

        /// <summary>
        /// Runs on (a background-capable) initialization. We register every toolkit
        /// <c>BaseCommand&lt;T&gt;</c> in this assembly in one call; the toolkit matches each
        /// command class to its .vsct id via the [Command(PackageIds.X)] attribute.
        /// </summary>
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress){
            await base.InitializeAsync(cancellationToken, progress);

            // Toolkit helper: discovers all [Command]-decorated BaseCommand<T> types in this
            // assembly and wires them to the menu IDs from the .vsct.
            // VERIFY IN VS: RegisterCommandsAsync() is a Community.VisualStudio.Toolkit
            // extension method on AsyncPackage/ToolkitPackage.
            await this.RegisterCommandsAsync();
        }
    }
}