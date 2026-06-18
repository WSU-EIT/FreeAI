using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeCodeReorganizer.Core;

/// <summary>Outcome of a <c>dotnet format</c> cleanup pass.</summary>
public readonly record struct CleanupResult(bool Ran, int ExitCode, string Output, string? Error);

/// <summary>
/// Runs the official <c>dotnet format</c> tool to apply the project's <c>.editorconfig</c> cleanup —
/// the same Roslyn formatting/code-style engine Visual Studio's "Format Document" / "Code Cleanup" use,
/// but driven from the command line so it works WITHOUT the IDE (the new out-of-process extension model
/// can no longer invoke the IDE's own cleanup command). Two levels:
/// <list type="bullet">
/// <item><b>Whitespace</b> — <c>dotnet format whitespace --folder</c>: indentation/spacing/newlines per
/// .editorconfig. No project restore, runs on a bare folder or a single file; fast and low-risk.</item>
/// <item><b>Full</b> — <c>dotnet format &lt;solution|project&gt;</c>: also removes/sorts usings and applies
/// code-style (IDExxxx) + analyzer fixes. Needs a project/solution context (restore/build); slower.</item>
/// </list>
/// Always run BEFORE the reorganizer so the house style (the "){" brace, "(" on its own line, double-tab
/// Razor attributes) — none of which .editorconfig can express — survives on top of the cleanup.
/// </summary>
public static class CleanupRunner
{
    /// <summary>Clean a single file in place.</summary>
    public static CleanupResult CleanFile(string file, bool full)
    {
        string dir = Path.GetDirectoryName(Path.GetFullPath(file)) ?? ".";

        if (full)
        {
            string? project = FindNearest(file, "*.csproj");
            if (project is null)
            {
                // No project context to drive analyzers/style — fall back to a whitespace pass.
                return RunWhitespaceFolder(dir, Path.GetFileName(file));
            }

            string projDir = Path.GetDirectoryName(Path.GetFullPath(project)) ?? dir;
            string rel = MakeRelative(projDir, Path.GetFullPath(file));
            return RunDotnet(new[] { "format", project, "--include", rel, "--verbosity", "quiet" }, projDir);
        }

        return RunWhitespaceFolder(dir, Path.GetFileName(file));
    }

    /// <summary>Clean a whole directory tree in place, skipping any paths in <paramref name="excludeGlobs"/>.</summary>
    public static CleanupResult CleanDirectory(string root, bool full, IReadOnlyList<string>? excludeGlobs = null)
    {
        string dir = Path.GetFullPath(root);

        if (full)
        {
            string? solution = FindNearest(dir, "*.slnx") ?? FindNearest(dir, "*.sln");
            if (solution is not null)
            {
                string solDir = Path.GetDirectoryName(Path.GetFullPath(solution)) ?? dir;
                var a = new List<string> { "format", solution };
                AddExcludes(a, excludeGlobs);
                a.Add("--verbosity");
                a.Add("quiet");
                return RunDotnet(a, solDir);
            }

            // No solution: format each project found under the directory.
            string[] projects = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories);
            if (projects.Length == 0)
            {
                return RunWhitespaceFolder(dir, null, excludeGlobs);
            }

            var log = new StringBuilder();
            int worst = 0;
            foreach (string p in projects)
            {
                string projDir = Path.GetDirectoryName(Path.GetFullPath(p)) ?? dir;
                var a = new List<string> { "format", p };
                AddExcludes(a, excludeGlobs);
                a.Add("--verbosity");
                a.Add("quiet");
                CleanupResult r = RunDotnet(a, projDir);
                log.AppendLine(r.Output);
                if (r.ExitCode != 0)
                {
                    worst = r.ExitCode;
                }
            }

            return new CleanupResult(true, worst, log.ToString(), worst == 0 ? null : "dotnet format exit " + worst);
        }

        return RunWhitespaceFolder(dir, null, excludeGlobs);
    }

    private static CleanupResult RunWhitespaceFolder(string dir, string? includeFile, IReadOnlyList<string>? excludeGlobs = null)
    {
        var args = new List<string> { "format", "whitespace", dir, "--folder" };
        if (includeFile is not null)
        {
            args.Add("--include");
            args.Add(includeFile);
        }

        AddExcludes(args, excludeGlobs);
        args.Add("--verbosity");
        args.Add("quiet");
        return RunDotnet(args, dir);
    }

    private static void AddExcludes(List<string> args, IReadOnlyList<string>? excludeGlobs)
    {
        if (excludeGlobs is null || excludeGlobs.Count == 0)
        {
            return;
        }

        args.Add("--exclude");
        foreach (string g in excludeGlobs)
        {
            if (!string.IsNullOrWhiteSpace(g))
            {
                args.Add(g.Trim());
            }
        }
    }

    private static CleanupResult RunDotnet(IEnumerable<string> args, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", args.Select(Quote)),
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                return new CleanupResult(false, -1, string.Empty, "could not start 'dotnet' (is the .NET SDK installed and on PATH?)");
            }

            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            string combined = string.IsNullOrEmpty(error) ? output : output + error;
            return new CleanupResult(true, proc.ExitCode, combined, proc.ExitCode == 0 ? null : "dotnet format exited with code " + proc.ExitCode);
        }
        catch (Exception ex)
        {
            return new CleanupResult(false, -1, string.Empty, ex.Message);
        }
    }

    // Quote an argument for the command line (netstandard2.0 has no ProcessStartInfo.ArgumentList).
    private static string Quote(string arg)
    {
        if (arg.Length > 0 && arg.IndexOf(' ') < 0 && arg.IndexOf('"') < 0)
        {
            return arg;
        }

        return "\"" + arg.Replace("\"", "\\\"") + "\"";
    }

    // Walk up from a file or directory looking for the nearest file matching a pattern.
    private static string? FindNearest(string start, string pattern)
    {
        string? dir = Directory.Exists(start) ? Path.GetFullPath(start) : Path.GetDirectoryName(Path.GetFullPath(start));
        while (!string.IsNullOrEmpty(dir))
        {
            string[] hits = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            if (hits.Length > 0)
            {
                return hits[0];
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    // Relative path of a file under baseDir (netstandard2.0 has no Path.GetRelativePath).
    private static string MakeRelative(string baseDir, string fullFile)
    {
        string b = baseDir.TrimEnd('\\', '/') + "\\";
        if (fullFile.StartsWith(b, StringComparison.OrdinalIgnoreCase))
        {
            return fullFile.Substring(b.Length);
        }

        return fullFile;
    }
}
