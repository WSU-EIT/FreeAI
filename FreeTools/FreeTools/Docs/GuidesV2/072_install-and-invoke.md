# 072 — Up and Scanning: Pointing the CLI at an App

> **Document ID:** 072  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Install the .NET CLI, aim it at a target Blazor app, and explain how to read its output.
> **Audience:** Quality and tooling users  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | Plain-language overview and key term definitions |
| 2 | [Before You Begin](#prerequisites) | The .NET 10 SDK, the sibling-folder layout, and what access you need |
| 3 | [Installing the CLI](#install-cli) | Getting the .NET SDK and restoring/building FreeTools once |
| 4 | [Aiming at a Target App](#aim-target) | The `--target` flag and the sibling-folder rule that makes it work |
| 5 | [Running Your First Scan](#first-scan) | `dotnet run` against the AppHost orchestrator and single tools |
| 6 | [Reading the Output](#read-output) | Console banners, the `runs/.../latest/` folder, and exit codes |
| 7 | [When Something Goes Wrong](#troubleshooting) | The handful of errors you will actually hit, and the fix for each |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

Before FreeTools can tell you anything useful about an app, you have to do two boring-but-load-bearing things: **install the runner** and **point it at the right folder**. Get either wrong and every later guide in this band (route discovery, screenshots, accessibility) fails at step zero with a confusing error. This doc exists so that never happens to you.

A few terms, defined once so the rest of the band reads smoothly:

- **CLI** stands for *command-line interface* — a program you run by typing a command in a terminal instead of clicking buttons. FreeTools is a CLI: there is no window, no menu, just a command and the text it prints back.
- **The .NET SDK** (Software Development Kit) is the toolbox that builds and runs C# programs. FreeTools is written in C#, so the SDK is the one thing you must install before anything works. The command it gives you is `dotnet`.
- **A target app** is the application you want FreeTools to inspect — in this repo, the sample target is a Blazor app called **FreeExamples**. **Blazor** is Microsoft's framework for building web apps in C# instead of JavaScript; its pages are `.razor` files. FreeTools is purpose-built to read Blazor apps.
- **A scan** is one run of FreeTools against a target. It reads the app's files (and optionally drives a browser against the running app), then writes a folder of reports — CSV tables, screenshots, and markdown summaries.

The mental model to hold onto: **install once, then for each app you just point and run.** Pointing is a single flag (`--target`), and running is a single command (`dotnet run`). The rest of this doc is the detail behind those two moves.

<a id="prerequisites"></a>
## 2. Before You Begin

Why this section matters: 90% of "it won't run" problems are a missing SDK or a misplaced folder. Five minutes of checking here saves an hour of guessing later.

**1. The .NET 10 SDK.** Every FreeTools project pins the same target framework. If you open any tool's project file you will see it plainly — for example `FreeTools.EndpointMapper.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

"Target framework" just means *the version of .NET this code is built for*. `net10.0` means you need the **.NET 10 SDK** installed. An older SDK (`net8.0`, `net9.0`) will refuse to build it. Confirm with:

```bash
dotnet --version
```

You want a version starting with `10.`.

**2. The orchestrator needs the Aspire workload.** FreeTools ships a coordinator project, `FreeTools.AppHost`, that runs the whole suite in the right order. It is built on **.NET Aspire** — Microsoft's toolkit for launching several programs together as one app. Its project file declares the dependency:

```xml
<Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0" />
```

The good news: a normal `dotnet restore` (covered in the next section) pulls this down automatically. You do not install Aspire by hand.

**3. The sibling-folder layout.** This is the rule people trip over. The AppHost finds your target app by looking **one level up from the FreeTools folder** and then into a folder named after your target. In its own words (from `FreeTools.AppHost/Program.cs`):

```text
// Define repo root (FreeExamples is a sibling folder to FreeTools)
// toolsRoot = .../FreeTools/FreeTools/  -> go up one level to find FreeExamples and .git
```

So the app you want to analyze must sit **beside** the `FreeTools` folder, not inside it. The default target, `FreeExamples`, is already arranged this way, which is why the out-of-the-box run "just works."

**4. Access.** Static analysis (reading files) needs nothing but read access to the source. Browser-based steps (screenshots, accessibility) additionally launch a real browser against the *running* target app — the AppHost starts that app for you on a fixed local HTTPS port, so you do not need a separate server. No production credentials are required; the sample logs in with the throwaway `admin`/`admin` account.

<a id="install-cli"></a>
## 3. Installing the CLI

Why this matters: this is the one-time setup. Do it correctly and you never think about it again.

**Step 1 — Install the .NET 10 SDK.** Download it from Microsoft's .NET site and install it like any other program. After it finishes, open a fresh terminal and verify:

```bash
dotnet --version
```

If you see a `10.x` number, the toolbox is ready. (There is nothing called "FreeTools.exe" to install separately — FreeTools *is* the C# source you build with this SDK.)

**Step 2 — Get into the tools folder.** All the FreeTools projects live under `FreeTools/FreeTools/`. That nested path is correct, not a typo: the outer `FreeTools` is the repo area, the inner one is the solution folder. Everything below assumes your terminal is in that inner folder:

```bash
cd FreeTools/FreeTools
```

**Step 3 — Restore and build once.** "Restore" downloads the NuGet packages each project needs (including the Aspire pieces from Section 2); "build" compiles the C#. Running the orchestrator does both for you automatically, but doing them explicitly the first time gives a clean, readable success/failure before you add scanning into the mix:

```bash
dotnet restore
dotnet build
```

A clean build with no red errors means the CLI is installed and ready. From here on, "running FreeTools" is just `dotnet run` (Section 5) — the SDK rebuilds anything that changed each time.

<a id="aim-target"></a>
## 4. Aiming at a Target App

Why this matters: the single most common mistake is running the scan and getting reports for the *wrong* app — or no app at all — because the target was never set correctly. Aiming is one flag plus one folder rule.

**The flag is `--target`.** The AppHost defines it explicitly, with a default, in `FreeTools.AppHost/Program.cs`:

```csharp
var targetOption = new Option<string>(
    name: "--target",
    description: "Target project to analyze (default: 'FreeExamples')",
    getDefaultValue: () => "FreeExamples");
```

So if you pass nothing, it analyzes **FreeExamples**, the bundled sample. To analyze a different app, name it:

```bash
dotnet run --project FreeTools.AppHost -- --target MyBlazorApp
```

Two things to notice in that command. The `--project FreeTools.AppHost` part tells `dotnet run` *which* program to run (the orchestrator). The bare `--` is a separator that means "everything after this belongs to FreeTools, not to `dotnet` itself" — without it, `dotnet` would try to interpret `--target` as one of its own options.

**The folder rule is what makes the name resolve.** The AppHost turns your target name into a path by going up one level from the tools folder and into a folder of that name:

```csharp
var repoRoot = Path.GetDirectoryName(toolsRoot) ?? toolsRoot;
var projectRoot = Path.GetFullPath(Path.Combine(repoRoot, target));
```

In plain terms: `--target MyBlazorApp` only works if there is a folder named `MyBlazorApp` sitting **next to** the `FreeTools` folder. If you place your app anywhere else, the name will not resolve and the scan finds nothing to read. This is the sibling-folder rule from Section 2, now in action.

**You usually do not aim the individual tools.** The AppHost computes the target's path, output folders, and the running app's URL, then hands each tool exactly what it needs through environment variables and arguments. For example it gives the route mapper the project root and an output CSV path:

```csharp
var endpointMapper = builder.AddProject<Projects.FreeTools_EndpointMapper>("endpoint-mapper")
    .WithArgs(projectConfig.ProjectRoot, projectConfig.PagesCsv)
    .WithEnvironment("START_DELAY_MS", ToolStartupDelayMs.ToString());
```

That means **aiming happens once, at the AppHost level.** Section 5 shows the normal path (let the AppHost aim) and the advanced path (aim a single tool yourself).

<a id="first-scan"></a>
## 5. Running Your First Scan

Why this matters: this is the payoff. One command produces routes, an inventory, screenshots, accessibility results, and a combined report.

**The recommended way — run the whole suite via the AppHost.** From inside `FreeTools/FreeTools`:

```bash
# Scan the bundled sample (FreeExamples)
dotnet run --project FreeTools.AppHost

# Scan your own app instead
dotnet run --project FreeTools.AppHost -- --target MyBlazorApp
```

What this actually does, in order, is spelled out in the AppHost: it starts the target Blazor app on a fixed local HTTPS port, runs the static analyzers (route mapping and file inventory) that only need the source, then runs the browser-driven tools (endpoint poking, screenshots, accessibility) against the now-running app, and finally generates a combined markdown report. You do not start a server yourself — the AppHost pins the sample to a known port:

```csharp
.WithEndpoint("https", endpoint =>
{
    endpoint.Port = 7271;
    endpoint.IsProxied = false;
})
```

Two retention flags are worth knowing on day one, both defined in the AppHost:

```bash
# Keep the last 5 timestamped backups instead of only 'latest'
dotnet run --project FreeTools.AppHost -- --keep-backups 5

# Don't delete the previous run before starting
dotnet run --project FreeTools.AppHost -- --skip-cleanup
```

By default neither is set: each run wipes the previous `latest/` folder and keeps no backups, so you always see fresh results.

**The advanced way — run a single tool directly.** Each analyzer is its own runnable program and can be invoked on its own. Most read their input from positional arguments *or* environment variables, with sensible defaults. The route mapper, for instance, takes the folder to scan as its first argument and the CSV to write as its second:

```bash
# Map routes in a folder, writing the result to pages.csv
dotnet run --project FreeTools.EndpointMapper -- C:\path\to\MyBlazorApp pages.csv
```

Inside `FreeTools.EndpointMapper/Program.cs` you can see those two positions being read, with sane fallbacks if you omit them:

```csharp
if (cliArgs.Count > 0)
    root = Path.GetFullPath(cliArgs[0]);   // first arg = folder to scan
...
if (cliArgs.Count > 1)
    outFile = Path.GetFullPath(cliArgs[1]); // second arg = output CSV
```

Browser-driven tools lean on **environment variables** instead, because the AppHost normally supplies them. The screenshot tool reads its target URL, route list, and output folder this way (from `FreeTools.BrowserSnapshot/Program.cs`):

```csharp
var baseUrl   = CliArgs.GetEnvOrArg("BASE_URL", args, 0, "https://localhost:5001");
var csvPath   = CliArgs.GetEnvOrArg("CSV_PATH", args, 1, "pages.csv");
var outputDir = CliArgs.GetEnvOrArg("OUTPUT_DIR", args, 2, "page-snapshots");
```

The most common environment variables you might set by hand are:

| Variable | What it means | Example |
|----------|---------------|---------|
| `BASE_URL` | Root URL of the running target app | `https://localhost:7271` |
| `CSV_PATH` | Path to the routes CSV produced by the mapper | `pages.csv` |
| `OUTPUT_DIR` | Where the tool writes its results | `snapshots` |
| `TENANT_CODE` | Tenant segment FreeCRM-style apps need in the URL to log in | `tenant1` |
| `LOGIN_USERNAME` / `LOGIN_PASSWORD` | Credentials for protected pages (sample uses admin/admin) | `admin` |
| `START_DELAY_MS` | Pause before starting, to let the server warm up | `5000` |

Unless you are debugging one tool in isolation, prefer the AppHost — it wires all of these for you and runs the tools in dependency order. Reach for direct invocation only when you want, say, just the route list and nothing else.

<a id="read-output"></a>
## 6. Reading the Output

Why this matters: a scan that "ran fine" but whose results you can't locate or interpret is worthless. There are exactly three things to read — the live console, the output folder, and the exit code.

**1. The live console.** Every tool prints a banner and a short config block so you can confirm, at a glance, that it was aimed correctly before it did any work. The shared helper that draws these lives in `FreeTools.Core/ConsoleOutput.cs`, and the route mapper uses it like this:

```csharp
ConsoleOutput.PrintBanner("EndpointMapper (FreeTools)", "2.0");
ConsoleOutput.PrintConfig("Scanning root", root);
ConsoleOutput.PrintConfig("Will write CSV to", outFile);
```

That produces a boxed title followed by `key: value` lines. **Read the config lines first** — if "Scanning root" points at the wrong app, stop and fix your `--target` before trusting anything downstream. When a full AppHost run finishes, it also nudges you toward the live app:

```text
>> Look for 'Login to the dashboard at' URL in output below <<
```

**2. The output folder.** Results are not scattered around — every run lands in one predictable place, organized by project name and git branch. The AppHost computes it as:

```text
Docs/runs/{ProjectName}/{Branch}/latest/
```

Inside `latest/` you will find the route inventory (`pages.csv`), the file inventory (`workspace-inventory.csv`), a `snapshots/` folder of screenshots and per-page reports, and the combined `{ProjectName}-Report.md`. The "latest" naming means the newest run is always at the same path; older runs only stick around if you used `--keep-backups`.

The route CSV is the simplest artifact to read and a good first thing to open. Its header and a few rows look like this:

```csv
FilePath,Route,RequiresAuth,Project
"Components/Pages/Home.razor","/",false,"Components"
"Components/Pages/Counter.razor","/counter",false,"Components"
```

Each row is one page: where its file lives, the URL route it answers (from the Blazor `@page` directive), whether it requires login, and which project it belongs to. Doc 073 covers this file in depth.

**3. The exit code.** An **exit code** is a single number a program returns when it finishes — `0` means success, anything else means a problem. This is how automation (and you, in a script) tells pass from fail without reading the text. FreeTools follows the convention strictly. The route mapper ends with:

```csharp
return 0;   // success
```

and bails out early with `return 1;` on problems like a missing folder. The accessibility scanner goes a step further and makes the code reflect *partial* success — it returns `0` only if every page scanned cleanly:

```csharp
return grandSuccessCount == grandTotalCount ? 0 : 1;
```

So a non-zero exit code after a scan is your signal that at least one page failed; the console and the per-page reports tell you which one and why.

<a id="troubleshooting"></a>
## 7. When Something Goes Wrong

Why this matters: the failures here are few and predictable. Knowing the cause turns a five-minute panic into a ten-second fix.

**"The required framework 'net10.0' was not found" / build errors about the SDK.**
*Cause:* you have an older .NET SDK. *Fix:* install the .NET 10 SDK and re-check with `dotnet --version` (Section 3). Every FreeTools project targets `net10.0`; nothing older will build it.

**"Workspace root not found" (or the report covers the wrong app / no pages).**
*Cause:* the target folder isn't where the AppHost looks. *Fix:* confirm your app folder sits **next to** the `FreeTools` folder and that `--target` matches its exact name (Section 4). This message comes straight from the route mapper when the resolved path doesn't exist:

```csharp
if (!Directory.Exists(root))
{
    Console.Error.WriteLine("Workspace root not found: " + root);
    return 1;
}
```

**"CSV file not found: pages.csv" when running a browser tool alone.**
*Cause:* the screenshot or poker tool needs the route list that the mapper produces, but you ran it before (or without) the mapper. *Fix:* run the full AppHost suite — it runs the mapper first and waits for it to finish before the browser tools start. If you must run tools individually, run `FreeTools.EndpointMapper` first and point `CSV_PATH` at the `pages.csv` it wrote.

**The scan finished but the exit code is `1`.**
*Cause:* this is not necessarily a crash — for the accessibility scanner it means *not every page succeeded* (see the exit-code logic in Section 6). *Fix:* scroll the console for the page marked failed, then open that page's `report.md` under `snapshots/` for the specific error. A genuine fatal error prints `Fatal error:` and a stack trace.

**Screenshots are blank, tiny, or the page "didn't load."**
*Cause:* Blazor apps render after an initial network round-trip, so a too-early capture catches an empty shell; the server may also still be warming up. *Fix:* the tools already wait — `START_DELAY_MS` pauses for server warmup and the snapshotter auto-retries captures under ~10 KB. If you invoked a tool directly, raise `START_DELAY_MS` (e.g. `5000`) so the server is fully up before navigation. Doc 074 covers the timing model in detail.

**The browser steps need a login you don't have.**
*Cause:* protected pages require credentials, and FreeCRM-style apps also need a tenant code in the URL. *Fix:* the sample uses `admin`/`admin` and `tenant1`, supplied automatically by the AppHost. For your own app, set `LOGIN_USERNAME`, `LOGIN_PASSWORD`, and `TENANT_CODE` to match it (Section 5).

---

<a id="related-docs"></a>
## 8. Related Docs

- [071 — Meet the Analysis Suite](071_freetools-orientation.md) — what the suite does
- [073 — Mapping Every Route](073_route-discovery.md) — the first command to run

---
*GuidesV2 072 · drafted from source · 2026-06-05.*
