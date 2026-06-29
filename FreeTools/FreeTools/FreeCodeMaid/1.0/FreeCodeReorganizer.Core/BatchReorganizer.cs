using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FreeCodeReorganizer.Core;

/// <summary>Summary of a repo-wide reorganize run.</summary>
public readonly record struct BatchReorgResult(
    int Scanned,
    int Changed,
    int Failed,
    IReadOnlyList<string> ChangedFiles);

/// <summary>
/// Reorganizes every C# (.cs) and Razor (.razor/.cshtml) file under a directory, in place. Shared by
/// the CLI's directory mode and the Visual Studio "Reorganize Repository" command so both behave
/// identically. .cs files go through <see cref="Reorganizer"/>; Razor files through
/// <see cref="RazorReorganizer"/> (which only touches @code blocks). Generated files and build output
/// are skipped. File BOM and newline style are preserved; files that don't change are not rewritten.
/// </summary>
public sealed class BatchReorganizer
{
    private static readonly string[] SkipSegments =
        ["\\bin\\", "\\obj\\", "\\.git\\", "\\.vs\\", "\\node_modules\\"];

    public static BatchReorgResult RunDirectory(string root, ReorderConfig config)
    {
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException(root);
        }

        var changedFiles = new List<string>();
        int scanned = 0, failed = 0;

        // Build the generated-code detector once for the whole tree (parses the applicable .editorconfig).
        GeneratedCodeDetector? generated = config.RespectGeneratedCode ? GeneratedCodeDetector.ForRoot(root) : null;

        foreach (string file in EnumerateSourceFiles(root))
        {
            scanned++;
            try
            {
                // Leave .editorconfig-declared generated code completely alone.
                if (generated is not null && generated.IsGenerated(file))
                {
                    continue;
                }

                if (TryReorganizeFile(file, EffectiveConfigFor(file, config)))
                {
                    changedFiles.Add(file);
                }
            }
            catch
            {
                failed++;
            }
        }

        return new BatchReorgResult(scanned, changedFiles.Count, failed, changedFiles);
    }

    /// <summary>
    /// Derives the per-file config: a file matching "exclude from reorganize" keeps its member order
    /// (only formatting applies); a file matching "exclude from cleanup" gets no house-style formatting
    /// (the external dotnet format pass excludes it separately). Files matching neither use the base config.
    /// </summary>
    public static ReorderConfig EffectiveConfigFor(string file, ReorderConfig config)
    {
        bool reorganizeExcluded = PathExclusion.IsExcluded(file, config.ExcludeReorganizeGlobs);
        bool cleanupExcluded = PathExclusion.IsExcluded(file, config.ExcludeCleanupGlobs);
        if (!reorganizeExcluded && !cleanupExcluded)
        {
            return config;
        }

        ReorderConfig eff = config.Clone();
        if (reorganizeExcluded)
        {
            eff.ReorderMembers = false;
        }

        if (cleanupExcluded)
        {
            eff.CollapseWrappedParameterBrace = false;
            eff.IndentWrappedRazorAttributes = false;
        }

        return eff;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            string norm = file.Replace('/', '\\');

            bool isCs = norm.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            bool isRazor = norm.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                           norm.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
            if (!isCs && !isRazor)
            {
                continue;
            }

            bool skip = false;
            foreach (string seg in SkipSegments)
            {
                if (norm.IndexOf(seg, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    skip = true;
                    break;
                }
            }

            if (skip)
            {
                continue;
            }

            // Never touch generated code.
            if (norm.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                norm.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
                norm.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
                norm.IndexOf(".generated.", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>Reorganizes one file in place; returns true if it changed.</summary>
    public static bool TryReorganizeFile(string file, ReorderConfig config)
    {
        byte[] raw = File.ReadAllBytes(file);
        bool hadBom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
        string text = new UTF8Encoding(false).GetString(hadBom ? Slice(raw, 3) : raw);
        string eol = text.Contains("\r\n") ? "\r\n" : "\n";

        bool isRazor = file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                       file.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

        string? newText;
        if (isRazor)
        {
            RazorReorgResult r = RazorReorganizer.Run(text, config, eol);
            newText = (r.Error is null && r.Changed) ? r.NewText : null;
        }
        else
        {
            ReorgResult r = Reorganizer.Run(text, config, eol);
            newText = (r.Error is null && r.Changed) ? r.NewText : null;
        }

        if (newText is null || newText == text)
        {
            return false;
        }

        File.WriteAllText(file, newText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: hadBom));
        return true;
    }

    private static byte[] Slice(byte[] source, int start)
    {
        var dst = new byte[source.Length - start];
        Array.Copy(source, start, dst, 0, dst.Length);
        return dst;
    }
}
