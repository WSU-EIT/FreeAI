using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace FreeCodeMaid;

public enum FileAction
{
    Process,
    SkipByPurpose
}

public sealed record ScanItem(string AbsolutePath, string RelativePath, FileAction Action);

/// <summary>
/// Discovers the candidate <c>.cs</c> files for a target (a file, a directory, or a
/// <c>.sln</c>/<c>.slnx</c>/<c>.csproj</c>) and classifies each as "process" or
/// "skip (by-purpose / generated)" using the config's glob rules. Excluded directories
/// (build output, vendored, generated) are never enumerated at all.
/// </summary>
public static class FileScanner
{
    public static (string root, List<ScanItem> items) Scan(string target, ReorderConfig config)
    {
        target = Path.GetFullPath(target);
        string root;
        List<string> relPaths;

        if (File.Exists(target))
        {
            var ext = Path.GetExtension(target).ToLowerInvariant();
            if (ext is ".sln" or ".slnx" or ".csproj")
            {
                root = Path.GetDirectoryName(target)!;
                relPaths = GlobCs(root, config);
            }
            else
            {
                root = Path.GetDirectoryName(target)!;
                relPaths = [Path.GetFileName(target)];
            }
        }
        else if (Directory.Exists(target))
        {
            root = target;
            relPaths = GlobCs(root, config);
        }
        else
        {
            return (target, []);
        }

        var skipMatcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var glob in config.SkipFiles)
        {
            skipMatcher.AddInclude(glob);
        }

        var items = new List<ScanItem>(relPaths.Count);
        foreach (var rel in relPaths)
        {
            var relNorm = rel.Replace('\\', '/');
            var action = skipMatcher.Match(relNorm).HasMatches ? FileAction.SkipByPurpose : FileAction.Process;
            items.Add(new ScanItem(Path.Combine(root, rel), relNorm, action));
        }

        items.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return (root, items);
    }

    private static List<string> GlobCs(string root, ReorderConfig config)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude("**/*.cs");
        foreach (var exclude in config.Exclude)
        {
            matcher.AddExclude(exclude);
        }

        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(root)));
        return result.Files.Select(f => f.Path).ToList();
    }
}
