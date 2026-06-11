# 076 — Building On the FreeTools Core Library

> **Document ID:** 076  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Describe the shared core utility library underpinning the commands and how to reuse it for custom scripts.
> **Audience:** Tooling contributors  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why a Shared Core Library Matters](#why-core) | What `FreeTools.Core` is, why every tool depends on it, and the key terms |
| 2 | [Mental Model: How the Core Fits In](#mental-model) | The layering, an everyday analogy, and where each tool attaches |
| 3 | [Module Reference](#module-reference) | The four real classes (`CliArgs`, `ConsoleOutput`, `PathSanitizer`, `RouteParser`) with faithful signatures |
| 4 | [Reusing the Core in Custom Scripts](#reuse) | The `.csproj` reference, the `using`, and a minimal working tool |
| 5 | [Conventions and Contracts](#conventions) | Zero dependencies, static-and-stateless, thread safety, config priority |
| 6 | [Common Pitfalls and Anti-Patterns](#pitfalls) | Mutating arg lists, env-var surprises, path assumptions |
| 7 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs (incl. the two un-homed tools) |

---

<a id="why-core"></a>
## 1. Why a Shared Core Library Matters

**Why it matters first.** Every FreeTools command — the route mapper, the screenshot taker, the accessibility scanner, the inventory scanner — has to do the same boring chores: read a setting from the command line *or* an environment variable, print a tidy banner, turn a URL route into a safe folder name, and parse a CSV of routes. If each tool re-invented those chores, you would get four slightly different banner styles, four subtly different ways of reading `--output=`, and four places to fix the same bug. `FreeTools.Core` exists so that the boring chores are written **once**, correctly, and shared.

**What a "core library" is, in plain language.** A *library* is just a bundle of reusable code that other programs borrow instead of writing their own. A *core* (or *shared*) library is the one library that almost everything else in the project depends on — the common foundation. Here, that foundation is a small .NET project named `FreeTools.Core` whose code lives under the namespace `FreeTools.Core`. (A *namespace* is C#'s way of grouping related code under a name so it does not collide with code elsewhere — think of it as a surname for the classes inside.)

**What makes this particular core special.** It is deliberately tiny and self-contained:

| Property | Value | Why you care |
|----------|-------|--------------|
| External dependencies | **None** (pure .NET) | Nothing to download, nothing to break, fast to build |
| Public classes | **4** | Small enough to hold in your head |
| Target framework | **net10.0** | Same as every consuming tool, so no version mismatch |
| Style | All `static`, no stored state | Call a method, get an answer — no setup, no instances |

The four classes are `CliArgs`, `ConsoleOutput`, `PathSanitizer`, and `RouteParser`. Section 3 walks through each. The short version: `CliArgs` reads settings, `ConsoleOutput` prints them nicely, `PathSanitizer` turns routes into safe file paths and formats byte sizes, and `RouteParser` reads routes out of a CSV file.

**The payoff.** Because the chores are centralized, every tool *looks and behaves consistently* — the same banner format, the same "environment variable beats command-line argument beats default" rule for settings, the same human-readable file sizes like `1.5 MB`. When you write your own custom script (Section 4), you inherit all of that for the price of a single project reference.

---

<a id="mental-model"></a>
## 2. Mental Model: How the Core Fits In

**The analogy.** Picture a workshop with several specialist machines — a saw, a drill, a sander. Each machine does its own job, but they all plug into the *same wall of power outlets and the same workbench*. `FreeTools.Core` is that shared wall and workbench. The specialist machines are the individual tools (route discovery, screenshots, accessibility, inventory). They each bring their own blade or bit, but they all draw power from, and rest on, the common core.

**The layering.** There are only two layers, and the arrow always points the same way:

```
┌─────────────────────────────────────────────────────────────┐
│  Tools (each is its own console program with a Main method)  │
│                                                              │
│  EndpointMapper  EndpointPoker  BrowserSnapshot              │
│  AccessibilityScanner  WorkspaceInventory  WorkspaceReporter │
└───────────────────────────┬─────────────────────────────────┘
                            │  depends on (ProjectReference)
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  FreeTools.Core  (no dependencies of its own)                │
│  CliArgs · ConsoleOutput · PathSanitizer · RouteParser       │
└─────────────────────────────────────────────────────────────┘
```

The rule to remember: **tools depend on the core; the core never depends on a tool.** This one-way arrow is what keeps the core reusable. If the core ever reached "up" into a specific tool, that tool would become impossible to remove without breaking the foundation.

**Where a tool "attaches."** Two things connect a tool to the core. First, a line in the tool's project file (`.csproj`) that says "I reference the core project":

```xml
<ItemGroup>
  <ProjectReference Include="..\FreeTools.Core\FreeTools.Core.csproj" />
</ItemGroup>
```

Second, a `using FreeTools.Core;` at the top of the tool's `Program.cs`, which lets the code call `CliArgs`, `ConsoleOutput`, and friends by their short names. That is the entire attachment — no configuration, no registration, no startup wiring.

**A real attachment, end to end.** Here is the actual opening of the WorkspaceInventory tool. Notice it reads settings, prints a banner, and formats a byte size — all using the core — before doing any work of its own:

```csharp
using FreeTools.Core;

ConsoleOutput.PrintBanner("WorkspaceInventory (FreeTools)", "2.0");
ConsoleOutput.PrintConfig("Root directory", root);
ConsoleOutput.PrintConfig("Output CSV", csvPath);
ConsoleOutput.PrintConfig("Max parse size", PathSanitizer.FormatBytes(maxParseSize));
ConsoleOutput.PrintDivider();
```

---

<a id="module-reference"></a>
## 3. Module Reference

This section lists the four classes exactly as they exist in the source (under `FreeTools.Core\`). Every class is `static` — you call its methods directly (`CliArgs.HasFlag(...)`), you never create an instance.

### 3.1 `CliArgs` — reading settings

**Why it matters.** A tool needs to know *what to do*: which folder to scan, how many threads to use, whether to run quietly. Those settings can arrive two ways — typed on the command line, or set as an *environment variable* (a named value the operating system hands to a program, commonly used by CI/CD pipelines and orchestration so a human does not have to type anything). `CliArgs` reads both and applies a consistent priority.

**The real surface.** From `CliArgs.cs`:

```csharp
public static class CliArgs
{
    // Flag detection — returns true if present AND removes it from the list
    public static bool HasFlag(List<string> args, string flag);
    public static bool HasFlag(List<string> args, params string[] flags);

    // Option parsing, e.g. "--output=path" → "path" (also removed from the list)
    public static string? GetOption(List<string> args, string prefix);
    public static string? GetOption(List<string> args, params string[] prefixes);

    // Positional arguments (by index)
    public static string? GetPositional(List<string> args, int index, string? defaultValue = null);
    public static int GetPositionalInt(List<string> args, int index, int defaultValue);
    public static string GetRequired(List<string> args, int index, string name);

    // Environment + CLI combined (priority: env var → arg → default)
    public static string GetEnvOrArg(string envVar, string[] args, int argIndex, string defaultValue);
    public static int GetEnvOrArgInt(string envVar, string[] args, int argIndex, int defaultValue);
    public static bool GetEnvBool(string envVar);
}
```

Two behaviors worth calling out because they surprise people:

- **`HasFlag` and `GetOption` mutate the list.** When a flag or option is found, it is *removed* from the `List<string>` you passed in. That is intentional: after you have pulled out all the named flags and options, whatever remains is your positional arguments. The trade-off is that order matters — pull flags/options *before* reading positionals.
- **`GetEnvBool` is strict.** It returns `true` only when the environment variable equals the literal text `"true"` (case-insensitive). The actual one-liner:

```csharp
public static bool GetEnvBool(string envVar)
{
    var value = Environment.GetEnvironmentVariable(envVar);
    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
```

So `1`, `yes`, or `on` do **not** count as true. Use the word `true`.

**Usage, copied in spirit from the real tools:**

```csharp
var argsList = args.ToList();
var noCounts   = CliArgs.HasFlag(argsList, "--noCounts") || CliArgs.GetEnvBool("NO_COUNTS");
var includeArg = CliArgs.GetOption(argsList, "--include=");
var maxThreads = CliArgs.GetEnvOrArgInt("MAX_THREADS", args, 1, 10);
```

### 3.2 `ConsoleOutput` — printing nicely and safely

**Why it matters.** Tools often run several files in parallel (multiple threads). If two threads call `Console.WriteLine` at the same instant, their output can interleave into garbage. `ConsoleOutput` wraps console writes in a *lock* (a gate that lets only one thread through at a time) so lines never scramble. It also gives every tool the same banner and divider look.

**The real surface.** From `ConsoleOutput.cs`:

```csharp
public static class ConsoleOutput
{
    // Thread-safe writes; isError:true sends to stderr instead of stdout
    public static void WriteLine(string message, bool isError = false);
    public static void Write(string message, bool isError = false);

    // Banner header. Version is optional: "Tool v2.0" or just "Tool"
    public static void PrintBanner(string toolName, string? version = null);

    // Section divider, optionally titled
    public static void PrintDivider(string? title = null);

    // Aligned key: value line (keyWidth pads the label, default 18)
    public static void PrintConfig(string key, string value, int keyWidth = 18);
}
```

The thread-safety is not a claim — here is the actual guard:

```csharp
private static readonly object _lock = new();

public static void WriteLine(string message, bool isError = false)
{
    lock (_lock)
    {
        if (isError)
            Console.Error.WriteLine(message);
        else
            Console.WriteLine(message);
    }
}
```

Note `isError: true` routes to *stderr* (the standard error stream) rather than *stdout* (standard output). That separation lets a pipeline capture normal output to a file while still surfacing errors on screen.

**What the output looks like** (the banner is three lines of `=`, `PrintConfig` pads the label):

```
============================================================
 WorkspaceInventory (FreeTools) v2.0
============================================================
Root directory:    C:\repo
Output CSV:        C:\repo\workspace-inventory.csv
============================================================
```

A subtlety: `WriteLine`/`Write` take the lock, but `PrintBanner`, `PrintDivider`, and `PrintConfig` call `Console.WriteLine` directly. They are meant for the single-threaded setup/summary phase (before and after the parallel work), so they do not contend with the worker threads.

### 3.3 `PathSanitizer` — safe paths and friendly sizes

**Why it matters.** A web route like `/Account/Manage/Email` contains slashes and a leading slash. If you tried to use it as a file name directly, the operating system would choke. `PathSanitizer` converts a route into a safe folder path, makes sure the folder exists, and turns raw byte counts into readable sizes like `11.8 MB`.

**The real surface.** From `PathSanitizer.cs`:

```csharp
public static class PathSanitizer
{
    // "/Account/Login" → "Account\Login" (Windows) / "Account/Login" (Unix).
    // Empty route ("/") becomes the literal "root".
    public static string RouteToDirectoryPath(string route);

    // Combine outputDir + sanitized route + filename into one path
    public static string GetOutputFilePath(string outputDir, string route, string filename);

    // Create the directory for a file path if it does not already exist
    public static void EnsureDirectoryExists(string filePath);

    // 1536 → "1.5 KB", 1048576 → "1.0 MB"; below 1 KB → "<n> bytes"
    public static string FormatBytes(long bytes);
}
```

Two faithful details:

- **The empty route becomes `"root"`, not an empty string.** That avoids writing files at the top level by accident:

```csharp
public static string RouteToDirectoryPath(string route)
{
    var safePath = route.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
    return string.IsNullOrEmpty(safePath) ? "root" : safePath;
}
```

- **`FormatBytes` spells out small sizes as `bytes`.** Under 1 KB you get e.g. `512 bytes`; at or above the thresholds you get one decimal place and `KB` / `MB` / `GB`.

**Usage:**

```csharp
var outputPath = PathSanitizer.GetOutputFilePath(snapshotsDir, "/Account/Manage/Email", "default.png");
// → snapshots\Account\Manage\Email\default.png   (on Windows)
PathSanitizer.EnsureDirectoryExists(outputPath);

Console.WriteLine(PathSanitizer.FormatBytes(12345678));  // "11.8 MB"
```

### 3.4 `RouteParser` — reading routes from a CSV

**Why it matters.** Several tools work from a CSV (comma-separated values) file listing the app's routes. Some routes contain *parameters* — placeholders like `{Id}` that need a real value before the page can load — and those usually cannot be tested blindly. `RouteParser` reads the CSV, optionally drops parameterized routes, and can substitute a tenant code.

**The real surface.** From `RouteParser.cs`. Note both parse methods take a *fourth* parameter, `tenantCode`, that older notes omit:

```csharp
public static class RouteParser
{
    // True if the route contains "{...}"
    public static bool HasParameter(string route);

    // Expected CSV columns: FilePath,Route,RequiresAuth,Project (header row + data rows)
    public static (List<string> routes, List<string> skipped) ParseRoutesFromCsv(
        string[] csvLines,
        int routeColumnIndex = 1,
        bool skipParameterizedRoutes = true,
        string? tenantCode = null);

    public static async Task<(List<string> routes, List<string> skipped)> ParseRoutesFromCsvFileAsync(
        string csvPath,
        int routeColumnIndex = 1,
        bool skipParameterizedRoutes = true,
        string? tenantCode = null);

    // baseUrl + route, trimming the duplicate slash between them
    public static string BuildUrl(string baseUrl, string route);
}
```

What the methods actually do, in plain terms:

- They return a *tuple* (two lists bundled together): the routes you can use, and the ones that were `skipped` because they still contained a parameter.
- When you supply a `tenantCode`, any `{TenantCode}` placeholder in a route is replaced with that value first, and the results are **deduplicated** so a tenant-specific route wins over its bare equivalent. The bare route is removed if a `/{tenantCode}...` version exists.
- `HasParameter` is the same check used elsewhere — for instance WorkspaceReporter uses `RouteParser.HasParameter(route)` to decide which routes to mark as skipped in its report.

**Usage:**

```csharp
var (routes, skipped) = await RouteParser.ParseRoutesFromCsvFileAsync("pages.csv");
// routes:  ["/", "/Account/Login", "/weather"]
// skipped: ["/Account/Manage/RenamePasskey/{Id}"]

foreach (var route in routes)
{
    var url = RouteParser.BuildUrl("https://localhost:5001", route);
    // e.g. https://localhost:5001/Account/Login
}
```

---

<a id="reuse"></a>
## 4. Reusing the Core in Custom Scripts

**Why it matters.** The whole point of a shared core is that *your* one-off tool gets the same polish as the built-in ones, with almost no effort. Here is the complete recipe.

**Step 1 — Reference the core project.** In your tool's `.csproj`, add a `ProjectReference` pointing at the core (adjust the `..\` depth to your folder):

```xml
<ItemGroup>
  <ProjectReference Include="..\FreeTools.Core\FreeTools.Core.csproj" />
</ItemGroup>
```

This is exactly what the existing tools do. There is no NuGet package to install and no version number to pin — you reference the project directly, so it always builds against the same source.

**Step 2 — Add the using.** At the top of your `Program.cs`:

```csharp
using FreeTools.Core;
```

**Step 3 — Write the tool.** A minimal but realistic script that reads a setting, prints a banner, sizes a file, and reports it:

```csharp
using FreeTools.Core;

var argsList = args.ToList();

// Settings: env var beats arg beats default
var root    = CliArgs.GetEnvOrArg("ROOT", args, 0, ".");
var verbose = CliArgs.GetEnvBool("VERBOSE");

ConsoleOutput.PrintBanner("MyTool (FreeTools)", "1.0");
ConsoleOutput.PrintConfig("Root", root);
ConsoleOutput.PrintConfig("Verbose", verbose.ToString());
ConsoleOutput.PrintDivider();

foreach (var file in Directory.EnumerateFiles(root))
{
    var size = PathSanitizer.FormatBytes(new FileInfo(file).Length);
    ConsoleOutput.WriteLine($"  {Path.GetFileName(file)} ({size})");
}

ConsoleOutput.PrintDivider("DONE");
```

**The call pattern in one sentence:** pull flags and options off the args list first, read your settings through `CliArgs` (so env vars and defaults are honored), announce yourself with a `ConsoleOutput` banner, and use `PathSanitizer`/`RouteParser` for any path or route work. That is the same shape every FreeTools tool follows.

---

<a id="conventions"></a>
## 5. Conventions and Contracts

These are the unwritten rules the core upholds. Follow them in your own tools and everything stays consistent.

**1. Zero external dependencies.** The core deliberately pulls in *no* NuGet packages. That keeps builds fast, removes a whole class of version conflicts, and means the core can be dropped into any .NET project. Do not add a dependency to the core to solve a problem that belongs in one tool — put it in the tool instead.

**2. Static and stateless.** Every class is `static` and stores no data between calls. You never write `new CliArgs()`; you call `CliArgs.GetOption(...)` directly. The design avoids hidden state — a method's output depends only on its inputs (plus environment variables, which are explicitly named). The library's own README states the principle plainly — "All utilities are static for simplicity" ([FreeTools.Core/README.md#L147](../../FreeTools.Core/README.md#L147)). In practice that means:

```csharp
// Good: stateless utility
public static class PathSanitizer { }

// Avoided: instance with stored state
public class PathSanitizer { private string _root; }
```

**3. Thread safety where it counts.** Only console writing is shared mutable territory, so only `ConsoleOutput.WriteLine`/`Write` take a lock. Pure functions like `PathSanitizer.FormatBytes` need no lock because they touch nothing shared.

**4. Priority-based configuration.** Every setting resolves in the same order, highest wins:

1. **Environment variable** — for CI/CD and orchestration (no human typing).
2. **Command-line argument** — for manual runs.
3. **Default value** — a sensible fallback so the tool still runs with nothing set.

`GetEnvOrArg` and `GetEnvOrArgInt` encode this order directly; the real tools also layer it manually, e.g. `Environment.GetEnvironmentVariable("ROOT_DIR") ?? CliArgs.GetPositional(...) ?? FindRepoRoot(...)`.

**5. Naming and framework.** Tools are named `FreeTools.<Thing>`, the root namespace matches the assembly name, and everything targets `net10.0`. Keep your custom tool on the same framework so the project reference resolves cleanly.

**Versioning note.** Because tools reference the core *as a project* (not a versioned package), there is no semantic version to bump — a change to the core rebuilds every consumer at once. The contract is therefore "do not break the public method signatures of the four classes." Adding a new optional parameter (as was done with `tenantCode`, which defaults to `null`) is safe; renaming or removing a method is not.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls and Anti-Patterns

**Pitfall 1 — Reading positionals before pulling flags.** Because `HasFlag` and `GetOption` *remove* matched entries from the args list, the indexes of your positional arguments shift as you consume flags. Always extract every flag and option first, then read positionals from what remains. Reversing that order gives you the wrong positional value.

**Pitfall 2 — Expecting `GetEnvBool` to accept `1`/`yes`/`on`.** It only treats the literal word `true` (any casing) as true. A pipeline that exports `VERBOSE=1` will silently run non-verbose. Set the value to `true`.

**Pitfall 3 — Forgetting the environment variable can override your argument.** With `GetEnvOrArg`, a stray environment variable left over from a previous run wins over what you typed on the command line. If a tool "ignores" your argument, check for a same-named env var before assuming a bug.

**Pitfall 4 — Assuming `RouteToDirectoryPath("/")` yields an empty path.** It returns the literal string `"root"`. If you build output paths by hand assuming an empty segment for the home route, you will look in the wrong folder. Let `GetOutputFilePath` do the combining for you.

**Pitfall 5 — Calling `PrintBanner`/`PrintConfig`/`PrintDivider` from worker threads.** Those three helpers write to the console *without* the lock (they are intended for single-threaded setup and summary). Inside a parallel loop, use `ConsoleOutput.WriteLine`, which is locked, to avoid interleaved output.

**Pitfall 6 — Adding a dependency to the core to fix one tool.** The zero-dependency promise is load-bearing. If only one tool needs a package (for example WorkspaceInventory needs Roslyn and the file-system globber), add that package to *that tool's* `.csproj`, never to `FreeTools.Core`.

**Pitfall 7 — Mutating the shared list when you still need the original.** `HasFlag`/`GetOption` mutate in place. If you need the untouched arguments later, copy first (`var working = args.ToList();`) and operate on the copy — which is exactly why the tools call `args.ToList()` before parsing.

---

<a id="related-docs"></a>
## 7. Related Docs

- [071 — Meet the Analysis Suite](071_freetools-orientation.md) — the suite the library powers
- [047 — Growing the Shared Library](047_custom-components.md) — parallels authoring reusable code

**Two un-homed tools that build directly on this core.** A documentation review flagged these as lacking a home in the guide set; they are the clearest real-world examples of consuming `FreeTools.Core`, so they belong here:

- **`FreeTools.WorkspaceInventory`** — a console tool (`OutputType=Exe`, targets `net10.0`) that scans a repository, classifies each file (e.g. `RazorPage`, `RazorComponent`, `CSharpSource`, `ProjectFile`, `Config`, `Markdown`), counts lines and characters, and writes `workspace-inventory.csv` plus C#- and Razor-only variants. It uses the core for everything cross-cutting: `CliArgs` for flags/options, `ConsoleOutput.PrintBanner`/`PrintConfig`/`PrintDivider` for its banner and summary, and `PathSanitizer.FormatBytes` for sizes. Beyond the core it adds two of its own NuGet packages — `Microsoft.CodeAnalysis.CSharp` (Roslyn, to read namespaces and declared types) and `Microsoft.Extensions.FileSystemGlobbing` (to match include/exclude patterns) — a textbook case of keeping tool-specific dependencies out of the core.
- **`FreeTools.WorkspaceReporter`** — a console tool (`OutputType=Exe`, targets `net10.0`) that reads the inventory CSVs (plus a pages CSV and screenshot metadata) and renders a single human-readable `LatestReport.md` with overview tables, large-file warnings, a Mermaid route map, and a screenshot gallery. It references only `FreeTools.Core` (no extra packages) and leans on `ConsoleOutput` for its banner/config lines, `PathSanitizer.FormatBytes` throughout the report, and `RouteParser.HasParameter` to flag parameterized routes as "skipped."

Together they show the intended pattern end to end: WorkspaceInventory *produces* CSVs, WorkspaceReporter *consumes* them, and both stand on the same four core utilities.

---
*GuidesV2 076 · drafted from source (`FreeTools.Core`, `FreeTools.WorkspaceInventory`, `FreeTools.WorkspaceReporter`) · 2026-06-05.*
