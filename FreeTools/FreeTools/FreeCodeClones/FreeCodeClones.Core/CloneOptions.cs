using System.Collections.Generic;

namespace FreeCodeClones.Core;

/// <summary>
/// How identifiers are flattened. <see cref="Structural"/> renames locals, parameters, fields, properties and
/// type names but keeps the names of invoked methods and of members read off a type or a call result, so two
/// unrelated loops that merely share a shape do not match. <see cref="Blind"/> renames every identifier.
/// </summary>
public enum CloneMode
{
    Structural,
    Blind
}

/// <summary>Everything the detector needs to know. The CLI maps its flags onto this one-to-one.</summary>
public sealed class CloneOptions
{
    /// <summary>Repository root to scan. Reported paths are relative to it.</summary>
    public string Root { get; set; } = ".";

    /// <summary>
    /// The FreeCRM project name used to build the default <c>{ProjectName}.App.*</c> filters. When null it is
    /// taken from the single .slnx/.sln in <see cref="Root"/>, or failing that the root folder name.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>Include globs (relative to the root, forward slashes). Empty means the .App.* defaults.</summary>
    public List<string> Includes { get; } = new List<string>();

    /// <summary>Scan every .cs and .razor file instead of only the .App.* layer.</summary>
    public bool IncludeAll { get; set; }

    /// <summary>Extra exclude globs, on top of bin, obj, .git, .vs, node_modules, Migrations and EFModels.</summary>
    public List<string> Excludes { get; } = new List<string>();

    /// <summary>Also scan test projects (folders whose name ends in .Tests).</summary>
    public bool IncludeTests { get; set; }

    public CloneMode Mode { get; set; } = CloneMode.Structural;

    /// <summary>Keep string literals verbatim, so blocks that differ only in a message are told apart.</summary>
    public bool KeepLiterals { get; set; }

    /// <summary>A unit is considered when it has at least this many tokens OR <see cref="MinStatements"/>.</summary>
    public int MinTokens { get; set; } = 50;

    public int MinStatements { get; set; } = 5;

    /// <summary>Jaccard similarity (0-1) at or above which two units are a near-miss clone.</summary>
    public double Similarity { get; set; } = 0.85;
}