using System.Text;
using FreeCodeReorganizer.Core;

// Parse config flags (shared by both modes), an optional --dir <path>, and --razor.
var config = new ReorderConfig();
string? dir = null;
bool razor = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--razor": razor = true; break;
        case "--dir" when i + 1 < args.Length: dir = args[++i]; break;
        case "--sort-alphabetically" when i + 1 < args.Length: config.SortAlphabetically = ParseBool(args[++i], true); break;
        case "--ignore-underscore" when i + 1 < args.Length: config.IgnoreLeadingUnderscoreInSort = ParseBool(args[++i], true); break;
        case "--group-by-visibility" when i + 1 < args.Length: config.GroupByVisibility = ParseBool(args[++i], false); break;
        case "--static-first" when i + 1 < args.Length: config.StaticMembersFirst = ParseBool(args[++i], false); break;
        case "--collapse-brace" when i + 1 < args.Length: config.CollapseWrappedParameterBrace = ParseBool(args[++i], true); break;
        case "--max-percent" when i + 1 < args.Length:
            if (int.TryParse(args[++i], out int p))
            {
                config.MaxFractionReordered = Math.Clamp(p, 0, 100) / 100.0;
            }

            break;
    }
}

// ---- Repo-wide mode: reorganize every .cs and .razor file under --dir in place. ----
if (dir is not null)
{
    try
    {
        BatchReorgResult batch = BatchReorganizer.RunDirectory(dir, config);
        foreach (string f in batch.ChangedFiles)
        {
            Console.Out.WriteLine("  reorganized: " + f);
        }

        Console.Out.WriteLine($"FreeCodeReorganizer: {batch.Changed} of {batch.Scanned} file(s) reorganized" +
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

string output;
string? error;
if (razor)
{
    RazorReorgResult r = RazorReorganizer.Run(input, config, eol);
    error = r.Error;
    output = (r.Changed && r.NewText is not null) ? r.NewText : input;
}
else
{
    ReorgResult r = Reorganizer.Run(input, config, eol);
    error = r.Error;
    output = (r.Changed && r.NewText is not null) ? r.NewText : input;
}

if (error is not null)
{
    Console.Error.WriteLine(error);
    return 1;
}

using (var stdout = Console.OpenStandardOutput())
{
    byte[] bytes = new UTF8Encoding(false).GetBytes(output);
    stdout.Write(bytes, 0, bytes.Length);
    stdout.Flush();
}

return 0;

static bool ParseBool(string s, bool fallback) => bool.TryParse(s, out bool b) ? b : fallback;
