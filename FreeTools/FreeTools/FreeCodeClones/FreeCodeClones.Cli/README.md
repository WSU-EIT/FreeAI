# FreeCodeClones — find copy-pasted C# in a FreeCRM project

FreeCodeClones checks whether a C# codebase contains copy-pasted logic, and lists every copy it finds. It
flattens each piece of code into a normalised token stream: identifiers become `a`, `b`, `c` in order of first
appearance, and literals become `S` (string), `C` (char) or `N` (number). It then hashes the streams and reports
every group of code units whose flattened form matches exactly or nearly. A renamed copy is still a copy.

It is a standalone tool with its own solution. It does not depend on FreeCodeReorganizer or FreeTools.Core.

```
FreeCodeClones/
├── FreeCodeClones.slnx          open this in Visual Studio
├── FreeCodeClones.Core/         the engine (netstandard2.0, Roslyn only): normaliser, matcher, reporter,
│                                baseline, fix proposals, and the Equivalence helper
├── FreeCodeClones.Cli/          this console app (net8.0), a thin argument parser over Core
└── FreeCodeClones.Core.Tests/   xunit tests (net10.0)
```

## Run it

**From a terminal**, for example as an AI agent right after cloning FreeAI. The tool only reads the scanned repository.

```powershell
dotnet run --project FreeTools/FreeTools/FreeCodeClones/FreeCodeClones.Cli -c Release --no-launch-profile -- `
    --root C:\path\to\FreeCICD --markdown clones.md --json clones.json
```

**From Visual Studio:** open `FreeCodeClones.slnx`, set **FreeCodeClones.Cli** as the startup project, pick a
launch profile from the Start button's drop-down, and press F5. The profiles are:
- *FreeCICD (cloned next to WSU-EIT)*: edit `--root` in `Properties/launchSettings.json` if your clone is elsewhere.
- *This tool's own source*
- *Help*

Exit codes: `0` for OK, `1` for new clones with `--fail-on-new`, `2` for bad arguments.

## What it scans: the `.App.` layer

FreeCRM projects keep their own code in `{ProjectName}.App.*` files, so that framework upgrades leave it alone
and so you can tell at a glance which code is yours. By default FreeCodeClones scans exactly that layer:

| Pattern | Example |
|---|---|
| `**/{Project}.App.*.cs` / `.razor` | `FreeCICD.App.API.cs`, `FreeCICD.App.UI.Wizard.razor` |
| `**/*.App.cs` / `**/*.App.razor` (hook files) | `DataAccess.App.cs`, `About.App.razor` |
| `**/*.App.{Project}.cs` / `.razor` (project extensions of base classes) | `DataAccess.App.FreeCICD.cs` |

`{Project}` is the name of the root's `.slnx`/`.sln` (FreeCICD.slnx gives FreeCICD). Override it with
`--project`. To choose files yourself, use `--include <glob>` (repeatable), or `--include-all` to scan every `.cs`
and `.razor` file. These folders are always skipped: `bin`, `obj`, `.git`, `.vs`, `node_modules`, `Migrations`
and `*EFModels`. Test projects (folders ending in `.Tests`) are skipped unless you pass `--include-tests`.
`--exclude <glob>` is repeatable. A glob with no slash matches any path segment.

## What it finds

- **Units:** every method, constructor, accessor (including expression-bodied properties), operator, local
  function, and lambda with 3 or more statements.
- **Statement windows:** every run of consecutive statements inside any block, so a chunk pasted into two
  larger, different methods is found. Only maximal windows are reported. If every occurrence of a match sits
  inside a larger match, only the larger one is listed.
- **Razor:** the C# in `@code { }` and `@functions { }` blocks. Braces are matched with the C# lexer, so braces
  in strings and comments don't count. Line numbers point into the `.razor` file.
- **Exact clones:** equal SHA-256 of the normalised token stream.
- **Near-miss clones:** Jaccard similarity of 5-token shingles at or above `--similarity` (default 0.85).
  Candidates are found through winnowed fingerprints, and pairs are grouped transitively into classes. Near-miss
  compares member units (methods and the like), not statement windows. In shingles every renamed identifier is
  `$`, so one inserted statement doesn't shift the a/b/c names of everything after it.
- **Size threshold:** a unit counts when it has at least `--min-tokens` (default 50) tokens **or** at least
  `--min-statements` (default 5) statements.

### What it cannot find

- **Semantic duplicates with different shapes.** A `for` loop and the LINQ query that does the same thing look
  nothing alike as tokens. Use the equivalence helper below to compare their behaviour.
- **Razor markup duplication.** Only the C# in code blocks is compared. Markup written inside `@code` (a
  `RenderFragment` such as `__builder => { <Component ... /> }`) does not parse as C#, so any unit or window that
  contains it is skipped.
- **Repetition inside one place.** A window whose statements repeat with some period (a run of
  `sb.AppendLine("...")`) is skipped, because every slice of it matches every other slice. A larger window
  that also covers the statements around the run is still compared.
- **Small blocks.** Anything below both thresholds is ignored. For example, a 46-token, 2-statement SignalR status
  block repeated 15 times only shows up on its own with `--min-tokens 40`. With the defaults it is found as part
  of larger windows.

## The two modes (`--mode`)

**structural** (default) renames locals, parameters, fields, properties and type names, but keeps the real name
of an identifier when, judged from syntax alone (there is no semantic model):
- it is the method being called: `x.Replace(...)`, `x?.Trim()`, `Helper(...)`; or
- it is the member in `x.Name`, and `x` is type-like: a predefined type (`string.Empty`), a call result
  (`Get().Name`), a name the scanned code uses as a type or namespace somewhere, a common framework type
  (`Path`, `Guid`, `StringComparison`...), or a PascalCase member of one of those
  (`DataObjects.SignalRUpdateType.LoadingDevOpsInfoStatusUpdate`).

So two loops that share a shape but call `Replace` and `Trim` are not reported. Limits: a local or property with
the same name as a type is treated as the type, and a member read from a variable (`user.Enabled`) is always
renamed, even when the variable's type is a framework type.

**blind** renames every identifier. It finds more, with more noise.

In both modes, `--keep-literals` keeps string literals verbatim, so two blocks that differ only in a message
string are told apart. An interpolated string is one `S` by default; with `--keep-literals` its text is kept.

## Output

- **Console:** a summary table of clone classes, largest first (size is tokens × occurrences), with each
  location as `path:startLine-endLine`.
- **`--json <file>`:** each class has `id`, `kind` (exact|near), `similarity`, `tokens`, `statements`,
  `occurrences`, the `normalized` text of its first member (truncated), and its `locations`.
- **`--markdown <file>`:** the same table, plus a code excerpt for each location.

Paths are relative to `--root`, with forward slashes, and sorted. The output is byte-identical across runs:
nothing time-dependent is written unless you pass `--timestamp`.

## Baseline: gating a build

1. `--write-baseline baseline.json` records the current classes, each with the reason `"TODO: justify"`.
2. A human edits each reason, for example "FreeCRM GetDataApp hook template" or "per-entity stub", and deletes
   the entries that should be fixed instead.
3. CI runs `--baseline baseline.json --fail-on-new`. It prints only the classes that are not in the baseline,
   and exits 1 if there are any.

```json
{ "accepted": [ { "id": "893dfd0dec3b20e9", "reason": "FreeCRM DataAccess.App.cs per-entity stubs" } ] }
```

Every entry needs a non-empty `reason`. Ids are content hashes, so an accepted exact class stays accepted when
its code moves. A near class gets a new id when its membership changes. Ids that no longer match anything are
listed, so they can be removed.

## Fix proposals: `--proposals <dir>`

```powershell
dotnet run --project FreeTools/FreeTools/FreeCodeClones/FreeCodeClones.Cli -c Release --no-launch-profile -- `
    --root C:\path\to\FreeCICD --proposals C:\temp\freecicd-proposals
```

This writes `proposals.md`, a report for a person to review, and `patches/NN-<id>.patch`, one `git apply`-ready
patch per mechanical fix. The folder must be outside `--root`. The scanned code is never changed; you decide
what to apply, on a branch.

Each clone class gets one of three verdicts:

| Verdict | Meaning | What you get |
|---|---|---|
| **Extract** | A shared helper can be written mechanically | The helper, a patch, a table of what differs, a reviewer checklist |
| **Review** | Possibly worth fixing, but it needs a person | The reason no patch was written, and a line diff of two copies |
| **Leave** | Probably intended | The reason, and the baseline entry to accept it |

**How a helper is derived.** The copies have identical token streams, so they line up token by token. Text that is
the same in every copy stays in the helper. A local, field or literal that differs becomes a parameter, named from
what the copies share (`_coreBuffer` and `_powerBuffer` give `buffer`, and `Source = "GitLab"` gives `source`).
Locals declared before the block and read in it are passed in, by `ref` if the block assigns them. A local declared
in the block and used after it is returned. A block that awaits gets an async helper, and each call site awaits it.
Whole methods that differ only in values keep their signatures, attributes and doc comments; their bodies call one
shared helper.

**When it stops and asks for review instead:**
- a `return`, `yield`, `goto`, or a `break`/`continue` aimed at a loop outside the block;
- more than one value used after the block;
- copies that use different members or types (that needs a delegate or generics, which is a design choice);
- more than 3 different literal values (data written out as code) or more than 6 parameters;
- copies in different classes;
- a patch that would not parse;
- near-miss copies.

It leaves framework files (no `.App.` in the name) and FreeCRM's comment-only hook stubs alone.

**What to check.** Types are read from declarations. A parameter declared with `var` from a method call gets
`TODO_Type`, the report says so, and the patch does not compile until you fill it in. If the copies had different
comments, the report says which ones the patch removes. Patches are independent and made against the files as
scanned. If two touch the same lines, the report lists them under "Overlaps": apply one, rescan, regenerate.

```powershell
git switch -c clones/13
git apply --check C:\temp\freecicd-proposals\patches\13-828422d14da30ade.patch
git apply C:\temp\freecicd-proposals\patches\13-828422d14da30ade.patch
dotnet build; dotnet test        # then rescan: the class should be gone
```

Checked on FreeCICD's last commit: all 7 patches applied cleanly. Four built as written. The other three built once
their flagged `TODO_Type`s were filled in. Each applied patch removed its clone class on a rescan.

## Behavioural equivalence: `Equivalence.Compare`

Shape matching can't answer "do these two methods actually do the same thing?". `FreeCodeClones.Core` includes
a differential-testing helper for that question. It runs both functions on the same inputs and returns every
input where the outputs differ, or where only one of the two threw. Exceptions count as outputs and are
compared by type.

```csharp
using FreeCodeClones.Core;

static string ByReplace(string s) => s.Replace("refs/heads/", "");
static string ByPrefix(string s) => s.StartsWith("refs/heads/") ? s.Substring("refs/heads/".Length) : s;

var differences = Equivalence.Compare<string?, string>(
    s => ByReplace(s!), s => ByPrefix(s!),
    Inputs.Strings(Inputs.AroundPrefix("refs/heads/")));

foreach (var d in differences)
{
    Console.WriteLine(d);   // "x/refs/heads/y": a "x/y", b "x/refs/heads/y"   (and others)
}
```

- `Inputs.Strings(extra...)` returns edge cases for path- and name-like inputs: null, `""`, whitespace, leading
  and trailing separators, mixed case, unicode, a 10,000-character string, plus your own cases.
- `Inputs.AroundPrefix("refs/heads/")` returns cases built around a prefix: `refs/heads/x`, `REFS/HEADS/x`,
  `x/refs/heads/y`, a doubled prefix, and others.
- `Inputs.Exhaustive<T1>()`, `<T1, T2>()` and `<T1, T2, T3>()` return every combination of bool and enum values
  (nullable types add null): a truth table.

This is **evidence on the inputs you tried, not a proof for all inputs**. In general, deciding whether two
functions are equivalent is undecidable. The exception is a finite domain enumerated exhaustively, such as a
bool/enum truth table: over that domain the result is a proof.
