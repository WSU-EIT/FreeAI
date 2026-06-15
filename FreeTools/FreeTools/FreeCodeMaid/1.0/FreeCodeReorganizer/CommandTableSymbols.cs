using System;

namespace FreeCodeReorganizer
{
    // Hand-authored equivalent of the file the "Extensibility Essentials" VsixSynchronizer would
    // generate from VSCommandTable.vsct *inside* Visual Studio. The Community.VisualStudio.Toolkit
    // NuGet package does NOT generate these — generation is an IDE-only feature of that separate VS
    // extension. We author them by hand so the project builds from the command line (and in a VS
    // without that IDE extension installed).
    //
    // KEEP IN SYNC with the <Symbols> section of VSCommandTable.vsct. If you ever do install
    // Extensibility Essentials and let it regenerate, delete this file to avoid duplicate symbols
    // (the .vsct is declared as <VSCTCompile>, not as a <None> with the VsixSynchronizer custom
    // tool, so it will NOT auto-regenerate during a normal build).

    /// <summary>GUID constants mirroring the &lt;GuidSymbol&gt; entries in VSCommandTable.vsct.</summary>
    internal static class PackageGuids
    {
        public const string FreeCodeReorganizerPackage = "fe5de5f2-7387-490d-ada3-531012c2c4db";
        public const string FreeCodeReorganizerCmdSet = "7b867334-96be-4435-beb6-75c6b8ce660e";

        public static readonly Guid guidFreeCodeReorganizerPackage = new Guid(FreeCodeReorganizerPackage);
        public static readonly Guid guidFreeCodeReorganizerCmdSet = new Guid(FreeCodeReorganizerCmdSet);
    }

    /// <summary>Command/group IDs mirroring the &lt;IDSymbol&gt; entries in VSCommandTable.vsct.</summary>
    internal static class PackageIds
    {
        public const int EditMenuGroup = 0x1020;
        public const int CodeWinContextGroup = 0x1021;
        public const int ReorganizeDocument = 0x0100;
    }
}
