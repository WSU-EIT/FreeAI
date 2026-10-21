using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeCodeClones.Core;

/// <summary>What one scan found, plus what the reports need to describe it.</summary>
public sealed class CloneResult
{
    internal CloneResult(string projectName, CloneOptions options, SourceSet set, int units, IReadOnlyList<CloneClass> classes)
    {
        ProjectName = projectName;
        Options = options;
        Files = set.Files;
        Set = set;
        Units = units;
        Classes = classes;
    }

    /// <summary>The parsed sources, which fix proposals read declarations from.</summary>
    internal SourceSet Set { get; }

    public string ProjectName { get; }

    public CloneOptions Options { get; }

    public IReadOnlyList<SourceFile> Files { get; }

    /// <summary>Member units plus statement windows that met the size threshold.</summary>
    public int Units { get; }

    /// <summary>Clone classes, largest (tokens x occurrences) first.</summary>
    public IReadOnlyList<CloneClass> Classes { get; }
}

/// <summary>The one entry point: select files, normalise, match.</summary>
public static class CloneDetector
{
    /// <summary>Scans <see cref="CloneOptions.Root"/> on disk (read only).</summary>
    public static CloneResult Run(CloneOptions options)
    {
        string root = Path.GetFullPath(options.Root);
        string project = options.ProjectName ?? SourceSet.DetectProjectName(root);
        List<(string, string)> files = SourceSet.SelectFiles(root, project, options)
            .Select(rel => (rel, File.ReadAllText(Path.Combine(root, rel))))
            .ToList();
        return Run(files, options, project);
    }

    /// <summary>Scans in-memory files (path, text). No include/exclude filtering is applied.</summary>
    public static CloneResult Run(IReadOnlyList<(string Path, string Text)> files, CloneOptions options, string? projectName = null)
    {
        SourceSet set = SourceSet.Parse(files);
        var normalizer = new Normalizer(options, set.TypeNames);
        UnitSet units = normalizer.Extract(set.Parts);
        List<CloneClass> classes = CloneMatcher.Find(units, normalizer, options);
        return new CloneResult(projectName ?? options.ProjectName ?? string.Empty, options, set, units.Count, classes);
    }
}