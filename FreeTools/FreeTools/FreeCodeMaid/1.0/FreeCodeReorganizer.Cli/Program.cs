using System.Text;
using FreeCodeReorganizer.Core;

// Parse config flags (shared by all modes), the target (--file / --dir / stdin), and --razor.
var config = new ReorderConfig();
string? dir = null;
string? file = null;
string? stdinPath = null;   // logical path for stdin mode, so per-path exclusions can apply
bool razor = false;
string cleanup = "none";    // none | whitespace | full  (runs dotnet format before reorganizing)
bool doReorganize = true;   // --reorganize false => cleanup/format only (no member reordering)

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--razor": razor = true; break;
        case "--dir" when i + 1 < args.Length: dir = args[++i]; break;
        case "--file" when i + 1 < args.Length: file = args[++i]; break;
        case "--path" when i + 1 < args.Length: stdinPath = args[++i]; break;
        case "--cleanup" when i + 1 < args.Length: cleanup = args[++i].ToLowerInvariant(); break;
        case "--reorganize" when i + 1 < args.Length: doReorganize = ParseBool(args[++i], true); break;
        case "--exclude-reorganize" when i + 1 < args.Length: config.ExcludeReorganizeGlobs.Add(args[++i]); break;
        case "--exclude-cleanup" when i + 1 < args.Length: config.ExcludeCleanupGlobs.Add(args[++i]); break;
        case "--respect-generated" when i + 1 < args.Length: config.RespectGeneratedCode = ParseBool(args[++i], true); break;
        case "--sort-alphabetically" when i + 1 < args.Length: config.SortAlphabetically = ParseBool(args[++i], true); break;
        case "--ignore-underscore" when i + 1 < args.Length: config.IgnoreLeadingUnderscoreInSort = ParseBool(args[++i], true); break;
        case "--group-by-visibility" when i + 1 < args.Length: config.GroupByVisibility = ParseBool(args[++i], false); break;
        case "--static-first" when i + 1 < args.Length: config.StaticMembersFirst = ParseBool(args[++i], false); break;
        case "--collapse-brace" when i + 1 < args.Length: config.CollapseWrappedParameterBrace = ParseBool(args[++i], true); break;
        case "--razor-attrs" when i + 1 < args.Length: config.IndentWrappedRazorAttributes = ParseBool(args[++i], true); break;
        case "--respect-regions" when i + 1 < args.Length: config.RespectRegions = ParseBool(args[++i], true); break;
        case "--max-percent" when i + 1 < args.Length:
            if (int.TryParse(args[++i], out int p))
            {
                config.MaxFractionReordered = Math.Clamp(p, 0, 100) / 100.0;
            }

            break;
        case "--max-width" when i + 1 < args.Length:
            if (int.TryParse(args[++i], out int w))
            {
                config.MaxLineWidth = Math.Max(20, w);
            }

            break;
    }
}

bool doCleanup = cleanup is "whitespace" or "full";
bool fullCleanup = cleanup == "full";
string cleanupLabel = fullCleanup ? "full" : "whitespace";

// --reorganize false => formatting/cleanup only (the engine still applies the house style, which is
// formatting, but never reorders members).
config.ReorderMembers = doReorganize;

// ---- Single file, in place: optional .editorconfig cleanup, then formatting (+ reorder). ----
if (file is not null)
{
    try
    {
        // Leave .editorconfig-declared generated code completely alone.
        if (config.RespectGeneratedCode && GeneratedCodeDetector.IsFileGenerated(file))
        {
            Console.Out.WriteLine("  (generated code, left alone): " + file);
            return 0;
        }

        bool cleanupExcluded = PathExclusion.IsExcluded(file, config.ExcludeCleanupGlobs);
        if (doCleanup && !cleanupExcluded)
        {
            CleanupResult c = CleanupRunner.CleanFile(file, fullCleanup);
            if (c.Error is not null)
            {
                Console.Error.WriteLine("cleanup: " + c.Error);
            }
            else
            {
                Console.Out.WriteLine($"FreeCodeReorganizer: cleaned ({cleanupLabel}) {file}");
            }
        }

        // House style (formatting) + member reorder, each gated by the per-file exclusions.
        if (BatchReorganizer.TryReorganizeFile(file, BatchReorganizer.EffectiveConfigFor(file, config)))
        {
            Console.Out.WriteLine("  changed: " + file);
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

// ---- Repo-wide mode: optional cleanup, then formatting (+ reorder) of every .cs/.razor under --dir. ----
if (dir is not null)
{
    try
    {
        if (doCleanup)
        {
            CleanupResult c = CleanupRunner.CleanDirectory(dir, fullCleanup, config.ExcludeCleanupGlobs);
            if (c.Error is not null)
            {
                Console.Error.WriteLine("cleanup: " + c.Error);
            }
            else
            {
                Console.Out.WriteLine($"FreeCodeReorganizer: cleaned ({cleanupLabel}) {dir}");
            }
        }

        // RunDirectory derives a per-file effective config (exclusions) internally.
        BatchReorgResult batch = BatchReorganizer.RunDirectory(dir, config);
        foreach (string f in batch.ChangedFiles)
        {
            Console.Out.WriteLine("  changed: " + f);
        }

        Console.Out.WriteLine($"FreeCodeReorganizer: {batch.Changed} of {batch.Scanned} file(s) changed" +
            (batch.Failed > 0 ? $", {batch.Failed} skipped on error." : "."));

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

// ---- Single-file mode: stdin (C# or Razor source) -> stdout (reorganized). ----
string input;
using (var stdin = Console.OpenStandardInput())
using (var reader = new StreamReader(stdin, new UTF8Encoding(false)))
{
    input = reader.ReadToEnd();
}

string eol = input.Contains("\r\n") ? "\r\n" : "\n";

// Leave .editorconfig-declared generated code alone (when we know the buffer's logical path).
if (stdinPath is not null && config.RespectGeneratedCode && GeneratedCodeDetector.IsFileGenerated(stdinPath))
{
    WriteStdout(input);
    return 0;
}

// Apply per-path exclusions when the caller passed a logical --path for the piped buffer.
ReorderConfig stdinConfig = stdinPath is not null
    ? BatchReorganizer.EffectiveConfigFor(stdinPath, config)
    : config;

string output;
string? error;
if (razor)
{
    RazorReorgResult r = RazorReorganizer.Run(input, stdinConfig, eol);
    error = r.Error;
    output = (r.Changed && r.NewText is not null) ? r.NewText : input;
}
else
{
    ReorgResult r = Reorganizer.Run(input, stdinConfig, eol);
    error = r.Error;
    output = (r.Changed && r.NewText is not null) ? r.NewText : input;
}

if (error is not null)
{
    Console.Error.WriteLine(error);
    return 1;
}

WriteStdout(output);
return 0;

static bool ParseBool(string s, bool fallback) => bool.TryParse(s, out bool b) ? b : fallback;

static void WriteStdout(string text)
{
    using var stdout = Console.OpenStandardOutput();
    byte[] bytes = new UTF8Encoding(false).GetBytes(text);
    stdout.Write(bytes, 0, bytes.Length);
    stdout.Flush();
}
