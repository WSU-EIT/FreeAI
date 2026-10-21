using System.Diagnostics;
using System.Globalization;
using System.Text;
using FreeCodeClones.Core;

// Parse flags onto CloneOptions, scan, print the clone classes, write the optional reports, apply the baseline.
var options = new CloneOptions();
string? root = null;
string? jsonPath = null;
string? markdownPath = null;
string? baselinePath = null;
string? writeBaselinePath = null;
string? proposalsDir = null;
bool failOnNew = false;
bool timestamp = false;

for (int i = 0; i < args.Length; i++) {
    switch (args[i]) {
        case "--root" when i + 1 < args.Length: root = args[++i]; break;
        case "--project" when i + 1 < args.Length: options.ProjectName = args[++i]; break;
        case "--include" when i + 1 < args.Length: options.Includes.Add(args[++i]); break;
        case "--include-all": options.IncludeAll = true; break;
        case "--exclude" when i + 1 < args.Length: options.Excludes.Add(args[++i]); break;
        case "--include-tests": options.IncludeTests = true; break;
        case "--keep-literals": options.KeepLiterals = true; break;
        case "--json" when i + 1 < args.Length: jsonPath = args[++i]; break;
        case "--markdown" when i + 1 < args.Length: markdownPath = args[++i]; break;
        case "--baseline" when i + 1 < args.Length: baselinePath = args[++i]; break;
        case "--write-baseline" when i + 1 < args.Length: writeBaselinePath = args[++i]; break;
        case "--proposals" when i + 1 < args.Length: proposalsDir = args[++i]; break;
        case "--fail-on-new": failOnNew = true; break;
        case "--timestamp": timestamp = true; break;
        case "--mode" when i + 1 < args.Length:
            string mode = args[++i].ToLowerInvariant();
            if (mode is not ("structural" or "blind")) {
                return Usage($"--mode is structural or blind, not '{mode}'.");
            }

            options.Mode = mode == "blind" ? CloneMode.Blind : CloneMode.Structural;
            break;
        case "--min-tokens" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out int tokens) || tokens < 1) {
                return Usage("--min-tokens needs a positive number.");
            }

            options.MinTokens = tokens;
            break;
        case "--min-statements" when i + 1 < args.Length:
            if (!int.TryParse(args[++i], out int statements) || statements < 1) {
                return Usage("--min-statements needs a positive number.");
            }

            options.MinStatements = statements;
            break;
        case "--similarity" when i + 1 < args.Length:
            if (!double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out double similarity) || similarity <= 0 || similarity > 1) {
                return Usage("--similarity needs a number in (0, 1], e.g. 0.85.");
            }

            options.Similarity = similarity;
            break;
        case "-h" or "--help" or "/?":
            return Usage(null);
        default:
            return Usage($"Unknown or incomplete argument '{args[i]}'.");
    }
}

if (root is null) {
    return Usage("--root is required.");
}

if (!Directory.Exists(root)) {
    Console.Error.WriteLine($"FreeCodeClones: --root '{root}' does not exist.");
    return 2;
}

options.Root = root;

// Proposals are for review: they never go inside the scanned repo, so the scan can't change what it reads.
if (proposalsDir is not null) {
    string fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
    string fullProposals = Path.TrimEndingDirectorySeparator(Path.GetFullPath(proposalsDir)) + Path.DirectorySeparatorChar;
    if (fullProposals.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) {
        Console.Error.WriteLine($"FreeCodeClones: --proposals must be outside the scanned repo ('{root}'), so nothing is written into it.");
        return 2;
    }
}

List<BaselineEntry>? baseline = null;
if (baselinePath is not null) {
    try {
        baseline = Baseline.Read(File.ReadAllText(baselinePath));
    } catch (Exception ex) when (ex is FormatException or IOException) {
        Console.Error.WriteLine($"FreeCodeClones: baseline '{baselinePath}': {ex.Message}");
        return 2;
    }
}

var watch = Stopwatch.StartNew();
CloneResult result = CloneDetector.Run(options);
watch.Stop();
string? stamp = timestamp ? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture) : null;

if (jsonPath is not null) {
    WriteFile(jsonPath, CloneReport.ToJson(result, stamp));
}

if (markdownPath is not null) {
    WriteFile(markdownPath, CloneReport.ToMarkdown(result, stamp));
}

if (writeBaselinePath is not null) {
    WriteFile(writeBaselinePath, Baseline.Write(result.Classes));
}

if (proposalsDir is not null) {
    List<FixProposal> proposals = ProposalBuilder.Build(result);
    string patches = Path.Combine(proposalsDir, "patches");
    Directory.CreateDirectory(patches);
    foreach (string old in Directory.GetFiles(patches, "*.patch")) {
        File.Delete(old); // patches from an earlier run would no longer match this scan
    }

    foreach (FixProposal p in proposals.Where(p => p.Patch is not null)) {
        WriteFile(Path.Combine(patches, CloneReport.PatchFileName(p)), p.Patch!);
    }

    WriteFile(Path.Combine(proposalsDir, "proposals.md"), CloneReport.ProposalsToMarkdown(result, proposals, "patches", stamp));
    Console.Error.WriteLine($"FreeCodeClones: {proposals.Count(p => p.Patch is not null)} patch(es) and proposals.md written to {proposalsDir}");
}

int exitCode = 0;
if (baseline is null) {
    CloneReport.WriteConsole(Console.Out, result, result.Classes);

    // With no baseline every class is new.
    exitCode = failOnNew && result.Classes.Count > 0 ? 1 : 0;
} else {
    BaselineCheck check = Baseline.Check(result.Classes, baseline);
    CloneReport.WriteConsole(Console.Out, result, check.New,
        $"{check.New.Count} new clone class(es) not in the baseline; {check.Accepted.Count} accepted; {check.Stale.Count} baseline id(s) no longer found.");
    foreach (BaselineEntry stale in check.Stale) {
        Console.Out.WriteLine($"  stale (no longer found, can be removed): {stale.Id}");
    }

    foreach (BaselineEntry todo in baseline.Where(b => b.Reason == Baseline.TodoReason)) {
        Console.Error.WriteLine($"  baseline entry {todo.Id} still says \"{Baseline.TodoReason}\".");
    }

    exitCode = failOnNew ? check.ExitCode : 0;
}

if (timestamp) {
    Console.Out.WriteLine($"Generated {stamp} in {watch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)}s.");
}

return exitCode;

// Reports are UTF-8 without a BOM; a missing folder is created rather than failing the run.
static void WriteFile(string path, string text)
{
    string? folder = Path.GetDirectoryName(Path.GetFullPath(path));
    if (folder is not null) {
        Directory.CreateDirectory(folder);
    }

    File.WriteAllText(path, text, new UTF8Encoding(false));
}

static int Usage(string? error)
{
    TextWriter w = error is null ? Console.Out : Console.Error;
    if (error is not null) {
        w.WriteLine("FreeCodeClones: " + error);
        w.WriteLine();
    }

    w.WriteLine("""
        FreeCodeClones: finds copy-pasted C# (methods, accessors, lambdas, statement runs, Razor @code).

        Usage: FreeCodeClones.Cli --root <repo> [options]

          --root <dir>            Repository to scan (read only). Required.
          --project <name>        FreeCRM project name for the default filters. Default: the root's .slnx/.sln name.
          --include <glob>        Files to scan, repeatable. Default: the {Project}.App.* layer
                                  (**/{Project}.App.*.cs, **/*.App.cs, **/*.App.{Project}.cs and the .razor ones).
          --include-all           Scan every .cs and .razor file.
          --exclude <glob>        Skip matching files, repeatable. bin, obj, .git, .vs, node_modules, Migrations
                                  and EFModels are always skipped.
          --include-tests         Also scan test projects (folders ending in .Tests).
          --mode <m>              structural (default): keep invoked method names; blind: rename everything.
          --keep-literals         Keep string literals, so blocks differing only in a message are not clones.
          --min-tokens <n>        Unit size threshold, default 50. A unit counts if it meets either threshold.
          --min-statements <n>    Default 5.
          --similarity <x>        Near-miss threshold (Jaccard of 5-token shingles), default 0.85.
          --json <file>           Machine-readable report.
          --markdown <file>       Human-readable report with code excerpts.
          --baseline <file>       Accepted clone classes ({"accepted":[{"id":..,"reason":..}]}); print only new ones.
          --fail-on-new           Exit 1 when a class is not in the baseline (no baseline: when any class exists).
          --write-baseline <file> Write the current classes as a baseline, reason "TODO: justify".
          --proposals <dir>       Write fix proposals for review: <dir>/proposals.md plus <dir>/patches/*.patch
                                  (for git apply). Must be outside --root; the scanned code is never changed.
          --timestamp             Add a generated time to the reports and the elapsed time to the console.

        Exit codes: 0 ok, 1 new clones with --fail-on-new, 2 bad arguments.
        """);
    return error is null ? 0 : 2;
}