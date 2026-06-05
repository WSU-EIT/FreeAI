# 071 — Meet the Analysis Suite

> **Document ID:** 071  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Introduce the FreeTools CLI, what each analyzer inspects, and when to reach for it during development.
> **Audience:** Quality and tooling users  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why FreeTools Matters](#why-it-matters) | Plain-language intro and key terms defined |
| 2 | [Running the CLI](#running-the-cli) | The AppHost entry point, flags, and the `--target` idea |
| 3 | [The Tools at a Glance](#analyzers-catalog) | One-line summary of every tool, pipeline and standalone |
| 4 | [What Each Tool Inspects](#what-each-inspects) | Deep dive into each tool's inputs, checks, and outputs |
| 5 | [Reading the Output](#reading-output) | The folder layout, the CSVs, and the generated report |
| 6 | [Choosing the Right Tool](#choosing-analyzer) | Decision guide mapping a question to a tool |
| 7 | [Fitting Into Your Workflow](#dev-workflow) | When to run the suite across the dev cycle |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why FreeTools Matters

**The short version:** FreeTools is a small collection of command-line programs that point themselves at one of your Blazor web apps, walk through every page, and hand you back a folder of facts — a list of routes, a screenshot of each page, an accessibility scan, file-size statistics, and a single Markdown report that ties it all together. You run one command and get a snapshot of the whole app without clicking through it by hand.

Let's define the jargon up front, because the rest of this doc leans on it:

- **Blazor** — Microsoft's framework for building web apps in C# instead of JavaScript. FreeCRM (and its sample fork, FreeExamples) is a Blazor app, which is why these tools are tuned for Blazor's quirks.
- **CLI (command-line interface)** — a program you run by typing a command in a terminal, rather than clicking buttons. Every FreeTool is a CLI.
- **Route** — the URL path that maps to a page, like `/tenant1/Examples/SampleItems`. In Blazor, a page declares its route with an `@page` directive at the top of the file.
- **Analyzer / tool** — one focused console program in the suite. Each does exactly one job (find routes, take screenshots, scan for accessibility issues) and writes its results to disk so the next tool can pick them up.
- **Aspire** — Microsoft's orchestration framework. "Orchestration" here just means *starting several programs in the right order and waiting for each to finish before starting the next.* One tool in the suite, the **AppHost**, uses Aspire to run all the others as a pipeline.

**Why this matters to you:** manually auditing a CRM with dozens of pages is slow and easy to get wrong. You forget pages, you miss a broken route, you never check whether a screen reader can use the screen. FreeTools turns that audit into a repeatable command. Because the output is plain text (CSV and Markdown) and PNG images, the results are easy to diff between branches, easy to commit, and easy to feed to an LLM for review. The suite is built on **.NET 10** and **C# 14**, and it ships with a sample target project (**FreeExamples**) so you can try the whole thing before you point it at your own app.

---

<a id="running-the-cli"></a>
## 2. Running the CLI

**Why this matters:** you almost never run the individual tools by hand. Instead you run *one* program — the **AppHost** — and it runs everything else in the correct order. Think of the AppHost as the conductor and the other tools as the orchestra.

The entry point lives in `FreeTools.AppHost/Program.cs`. It uses **System.CommandLine** (Microsoft's library for parsing command-line flags) to accept three options:

```csharp
var targetOption = new Option<string>(
    name: "--target",
    description: "Target project to analyze (default: 'FreeExamples')",
    getDefaultValue: () => "FreeExamples");
```

So the basic invocation, run from the `FreeTools/FreeTools` folder, is:

```bash
# Analyze the bundled sample app (FreeExamples)
dotnet run --project FreeTools.AppHost

# Analyze your own app instead (must sit as a sibling folder)
dotnet run --project FreeTools.AppHost -- --target MyBlazorApp
```

The `--` separates flags meant for `dotnet` from flags meant for the AppHost. Everything after `--` is passed through to the tool.

**The three flags the AppHost understands:**

| Flag | Default | What it does |
|------|---------|--------------|
| `--target <name>` | `FreeExamples` | The project folder to analyze. It must be a sibling of the `FreeTools` folder — the AppHost goes up one level from itself and looks for a folder with this name. |
| `--keep-backups <n>` | `0` | How many timestamped copies of previous runs to keep. `0` means "keep only the newest run." |
| `--skip-cleanup` | off | Leave old run folders in place instead of clearing them before this run. |

**The `--target` idea, in plain terms:** the AppHost assumes your app lives *next to* FreeTools on disk. It computes a `repoRoot` by going up one directory from the tools, then looks for the target folder inside it. It also reads the current **git branch** (by parsing `.git/HEAD`) so that results for different branches don't overwrite each other.

One important detail: the AppHost has the sample app wired in as a real project reference (`builder.AddProject<Projects.FreeExamples>(...)`) and pins it to a fixed HTTPS port (`https://localhost:7271`) so screenshots and the database URL stay stable between runs. To analyze a *different* app, you also update that project reference — `--target` alone controls where the static-analysis tools scan and where output lands, but the live web app that gets screenshotted is the one the AppHost is compiled against. (Doc [072](072_install-and-invoke.md) walks through that setup.)

Most tools also read **environment variables** for configuration — the AppHost sets these for you, but you can override them if you run a tool standalone. Common ones: `BASE_URL` (the running app's address), `CSV_PATH` (where the route list lives), `OUTPUT_DIR` (where snapshots go), `MAX_THREADS` (how many pages to process in parallel), and `START_DELAY_MS` (how long to wait before starting, so the web app has time to warm up).

---

<a id="analyzers-catalog"></a>
## 3. The Tools at a Glance

The suite has **ten projects.** Two are infrastructure, six form the analysis **pipeline** the AppHost runs end-to-end, and two are **standalone utilities** you run on their own when you need them. Knowing which bucket a tool is in tells you whether the AppHost will run it for you.

| Project | Bucket | One-line purpose |
|---------|--------|------------------|
| **FreeTools.AppHost** | Infrastructure | The conductor — uses Aspire to launch the target app and run the pipeline tools in order. Your entry point. |
| **FreeTools.Core** | Infrastructure | Shared library of helpers (CLI parsing, console output, route parsing, path sanitizing). No `Main`; everyone else references it. |
| **FreeTools.EndpointMapper** | Pipeline | Scans `.razor` files for `@page` routes and `[Authorize]` attributes → `pages.csv`. |
| **FreeTools.WorkspaceInventory** | Pipeline | Measures every source file (size, lines, kind, namespaces, types) → `workspace-inventory.csv`. |
| **FreeTools.EndpointPoker** | Pipeline | Sends an HTTP GET to each route and saves the raw HTML response. |
| **FreeTools.BrowserSnapshot** | Pipeline | Drives a real browser (Playwright) to screenshot each page, logging in when needed → `*.png` + `metadata.json`. |
| **FreeTools.AccessibilityScanner** | Pipeline | Runs accessibility audits (axe-core, HTML_CodeSniffer, optional WAVE) on each page → violation reports + annotated screenshots. |
| **FreeTools.WorkspaceReporter** | Pipeline | Aggregates every CSV, screenshot, and scan into one Markdown report. |
| **FreeTools.ForkCRM** | Standalone | Clones FreeCRM, strips out unwanted modules, renames it, and writes out a brand-new project. |
| **FreeTools.AppExtractor** | Standalone | Copies just your customization files (`*.App.*`) out of a FreeExamples project so you can see your own code in isolation. |

**Why the two buckets matter:** when you run `dotnet run --project FreeTools.AppHost`, only the six pipeline tools (plus the infrastructure) fire. **ForkCRM** and **AppExtractor** are deliberately *not* in the pipeline — they create or extract projects rather than analyze a running one, so you run them by hand when that's the job at hand. Section 4 covers all ten.

---

<a id="what-each-inspects"></a>
## 4. What Each Tool Inspects

This is the heart of the doc. Each tool below lists *what it reads, what it checks, and what it writes.*

### 4.1 FreeTools.Core — the shared toolbox

**Why it matters:** every other tool depends on Core, so its behavior quietly shapes the whole suite. It has no `Main` method; it is a library of four small static helper classes:

- **`CliArgs`** — reads flags and environment variables with sensible fallbacks. The pattern "env var, else positional arg, else default" comes from here:
  ```csharp
  public static string GetEnvOrArg(string envVar, string[] args, int argIndex, string defaultValue)
      => Environment.GetEnvironmentVariable(envVar)
         ?? (args.Length > argIndex ? args[argIndex] : null)
         ?? defaultValue;
  ```
- **`ConsoleOutput`** — thread-safe console writing plus the banner/divider formatting you see at the top of every tool's output (`PrintBanner`, `PrintConfig`, `PrintDivider`).
- **`RouteParser`** — reads `pages.csv` and decides which routes are testable. It substitutes `{TenantCode}` with a real tenant code, and **skips parameterized routes** — any route containing `{` and `}` (like `/EditSampleItem/{id}`) is set aside because the tool has no real ID to plug in.
- **`PathSanitizer`** — turns a route like `/Account/Login` into a safe folder path (`Account/Login`) and formats byte counts into human-readable `KB`/`MB`/`GB`.

### 4.2 FreeTools.EndpointMapper — route discovery

**Why it matters:** you can't test pages you don't know about. EndpointMapper is the first analysis step; everything downstream depends on its output.

- **Reads:** every `.razor` file under the target (skipping `obj`, `bin`, and `repo` folders).
- **Checks:** a regex finds each `@page "..."` directive (the route), and a second regex detects `@attribute [Authorize` (whether the page needs a login).
  ```csharp
  var pageRegex = new Regex(@"@page\s+""(?<route>[^""]+)""", RegexOptions.Compiled);
  var authorizeRegex = new Regex(@"@attribute\s+\[Authorize", RegexOptions.Compiled);
  ```
- **Writes:** `pages.csv` with columns `FilePath,Route,RequiresAuth,Project`. Files with no `@page` are still listed (with a blank route) so the inventory stays complete.

### 4.3 FreeTools.WorkspaceInventory — file metrics

**Why it matters:** large files are hard for both humans and LLMs to work with, and an inventory tells you where the bulk of your code lives.

- **Reads:** source files matching a default pattern set (`*.cs`, `*.razor`, `*.csproj`, `*.sln`, `*.json`, `*.config`, `*.md`, `*.xml`, `*.yaml`, `*.yml`), excluding `bin`, `obj`, `.git`, `node_modules`, and similar.
- **Checks:** for every file it records size, line count, character count, and a **Kind** classification (`RazorPage`, `RazorComponent`, `CSharpSource`, `ProjectFile`, `Config`, `Markdown`, …). For C# files it uses **Roslyn** (`Microsoft.CodeAnalysis.CSharp`, the official C# parser) to extract declared namespaces and type names. For `.razor` files it also pulls out routes and auth, just like the mapper.
- **Writes:** `workspace-inventory.csv` (everything), plus two sorted spin-offs — `workspace-inventory-csharp.csv` and `workspace-inventory-razor.csv` — ordered by line count so the biggest files float to the top. It processes files in parallel (default `MAX_THREADS=10`).

### 4.4 FreeTools.EndpointPoker — HTTP testing

**Why it matters:** before spending time on screenshots, it's worth knowing which pages even respond. The Poker is a fast, cheap health check.

- **Reads:** `pages.csv` (via `RouteParser`), the running app's `BASE_URL`, and a `TENANT_CODE`.
- **Checks:** sends a plain HTTP **GET** to each testable route (parameterized routes are skipped, with a logged `[SKIP]`). It tolerates self-signed dev certificates and counts connection errors, HTTP errors, and successes. Runs in parallel.
- **Writes:** the raw HTML response for each route as a `.html` snapshot in the output directory.

### 4.5 FreeTools.BrowserSnapshot — visual capture

**Why it matters:** an HTTP 200 doesn't prove a page *renders.* Blazor apps build their UI in the browser after the initial load, so the only way to know a page looks right is to open it in a real browser and photograph it.

- **Reads:** `pages.csv`, `BASE_URL`, `TENANT_CODE`, and login credentials (defaults `admin` / `admin`).
- **Checks & captures:** it uses **Playwright** (Microsoft's browser-automation library) to drive a real Chromium, Firefox, or WebKit browser. For pages flagged `RequiresAuth`, it logs in first. Two details make it Blazor-aware:
  - It waits for **NetworkIdle** (no network traffic) plus a configurable **settle delay** (`PAGE_SETTLE_DELAY_MS`, default 3000ms) so the SPA has finished rendering before the photo.
  - If a screenshot comes back suspiciously small (under 10KB — likely a blank page), it **auto-retries** with extra delay.
  ```csharp
  private const int SuspiciousFileSizeThreshold = 10 * 1024; // 10KB
  private const int RetryExtraDelayMs = 3000;
  ```
- **Writes:** a full-page `*.png` per route, plus a `metadata.json` capturing stats and any JavaScript console errors seen during load.

### 4.6 FreeTools.AccessibilityScanner — ADA / a11y sweep

**Why it matters:** "accessibility" (often shortened to **a11y**) means the app works for people using screen readers, keyboard-only navigation, or high-contrast modes. It's a legal and ethical requirement, and it's the easiest thing to forget. This tool checks it automatically.

- **Reads:** `pages.csv`, `BASE_URL`, login credentials, and an `EXTRA_PAGES` list (the AppHost adds parameterized and admin pages the mapper can't auto-discover, like `/{TenantCode}/Settings/Users`).
- **Checks:** for each page it runs up to three independent engines and merges the results:
  - **axe-core** — the industry-standard open-source accessibility rule engine.
  - **HTML_CodeSniffer** — the Pa11y engine, a *different* rule set, so the two catch different problems.
  - **WAVE API** — an optional third opinion that only runs if you supply an API key.
  Violations are bucketed into four severities — **critical, serious, moderate, minor** — and surfaced in the console like this:
  ```
  A11y: 7 violations (🔴2 🟠1 🟡3 🔵1)
  ```
- **Writes:** per-page violation data plus **annotated screenshots** — overlays that draw colored outlines and labels directly on the offending elements (an axe overlay, a WAVE-style icon overlay, and an HTML_CodeSniffer overlay) so you can *see* the problems, not just read about them.

### 4.7 FreeTools.WorkspaceReporter — the report

**Why it matters:** five tools each leave a pile of CSVs and images. The Reporter is what turns that pile into something a human reads in two minutes.

- **Reads:** every artifact the others produced — `workspace-inventory.csv` (and its C#/Razor spin-offs), `pages.csv`, and the `snapshots` directory.
- **Builds:** a single GitHub-flavored Markdown report named `{Project}-Report.md`. It includes a workspace overview, file statistics by category, a **Mermaid diagram** of the route hierarchy, "largest files" tables, warnings for files past LLM-friendly size thresholds, a screenshot health section (success rates, HTTP errors, JS errors), and a screenshot gallery.
- **Why it runs last:** in the pipeline it `WaitForCompletion` on the inventory, mapper, poker, browser, and a11y scanner — it literally cannot start until all five are done.

### 4.8 FreeTools.ForkCRM — spin up a new project *(standalone)*

**Why it matters:** FreeCRM is a starting point, not a finished app. ForkCRM is how you stamp out a fresh, renamed project with only the modules you want — without manually editing hundreds of files.

- **Inputs (three required args):** a new project name, a module selection, and an output directory. Example:
  ```bash
  FreeTools.ForkCRM FreeManager "keep:Tags" ./output
  FreeTools.ForkCRM MyApp "remove:all" C:\Projects\MyApp
  ```
- **What it does:** it clones the FreeCRM repo (`https://github.com/WSU-EIT/FreeCRM.git`) into a temp folder using **LibGit2Sharp** (a Git library for .NET), then runs two official Windows tools — `Remove Modules from FreeCRM.exe` and `Rename FreeCRM.exe` — to strip modules and rename. The valid modules are `Tags, Appointments, Invoices, EmailTemplates, Locations, Payments, Services` (plus `all`). It then deletes `.git`/`.github`/`artifacts`, reads every transformed file into memory, and writes the result to your output directory.
- **Note:** it shells out to `.exe` tools, so it needs Windows (or Wine). It is **not** part of the AppHost pipeline — you run it on its own when you want a new app.

### 4.9 FreeTools.AppExtractor — isolate your customizations *(standalone)*

**Why it matters:** in a FreeExamples-based app, the framework provides most of the code and *you* add a thin customization layer. When you want to review just your work — or hand only your code to a reviewer or an LLM — AppExtractor pulls it out.

- **Inputs:** `--source` (the project root), `--output` (where to copy), and an optional `--dry-run` that lists what *would* be copied without copying. It can also read these from `appsettings.json` under an `AppExtractor` section.
  ```bash
  dotnet run -- --source "C:\...\FreeExamples" --output "C:\...\extracted" --dry-run true
  ```
- **What it does:** it scans the whole project and copies files whose names contain the pattern `.App.` (the customization marker), plus a few configured extras (whole directories like Docs, `appsettings.json` from project roots, solution/root files). It preserves the original folder structure and prints a summary, including *what percentage of the codebase is your customization* — a quick reality check on how much you've actually changed.
- **Note:** like ForkCRM, this is a **standalone** utility, not a pipeline step.

---

<a id="reading-output"></a>
## 5. Reading the Output

**Why it matters:** the suite's value is in its output folder. If you know where things land and what each file is, you can answer almost any question about the app without rerunning anything.

Everything goes under `Docs/runs`, organized by **project** and **git branch** so runs never collide:

```
Docs/runs/
└── {ProjectName}/
    └── {BranchName}/
        └── latest/
            ├── pages.csv                     # routes + auth (EndpointMapper)
            ├── workspace-inventory.csv        # all file metrics (WorkspaceInventory)
            ├── workspace-inventory-csharp.csv # C# files, biggest first
            ├── workspace-inventory-razor.csv  # Razor files, biggest first
            ├── {ProjectName}-Report.md        # the human-readable report (WorkspaceReporter)
            └── snapshots/
                ├── {route}/default.png         # screenshot (BrowserSnapshot)
                ├── {route}/default.html        # raw HTML (EndpointPoker)
                ├── {route}/metadata.json        # capture stats (BrowserSnapshot)
                └── a11y/                         # accessibility results + overlays
```

Three things to know when reading it:

1. **`latest` is always the newest run.** By default the AppHost deletes the previous `latest` before each run so you never see stale screenshots. Pass `--keep-backups 5` to keep timestamped copies alongside it.
2. **The CSVs are the raw facts; the Markdown report is the narrative.** Start with `{ProjectName}-Report.md` for the overview, then drill into a CSV when you need exact numbers (e.g. which file is 1,200 lines long).
3. **Severity in the a11y output uses four levels** — critical (🔴), serious (🟠), moderate (🟡), minor (🔵). Critical and serious are the ones to fix first; the annotated overlay screenshots show you exactly where on the page they live.

Everything is plain text or images, so you can commit the run folder, diff it between branches, or paste a CSV straight into an LLM and ask "what looks wrong here?"

---

<a id="choosing-analyzer"></a>
## 6. Choosing the Right Tool

**Why it matters:** most of the time you just run the whole pipeline. But when you have a *specific* question, going straight to one tool is faster. Use this as a lookup table.

| Your question / situation | Reach for |
|---------------------------|-----------|
| "What pages does this app even have, and which need a login?" | **EndpointMapper** → `pages.csv` |
| "Which files are too big / where is the bulk of the code?" | **WorkspaceInventory** → the `-csharp` / `-razor` CSVs |
| "Are any routes returning errors right now?" | **EndpointPoker** (fast HTTP check) |
| "Does every page actually *render* correctly?" | **BrowserSnapshot** → the PNG gallery |
| "Is the app usable with a screen reader / does it pass accessibility?" | **AccessibilityScanner** |
| "Give me one document that summarizes everything." | run the full pipeline; read `{Project}-Report.md` |
| "I need a brand-new app based on FreeCRM with only some modules." | **ForkCRM** (standalone) |
| "I just want to see *my* customizations, not the framework code." | **AppExtractor** (standalone) |
| "I'm not sure — give me the lot." | the **AppHost** (runs the six pipeline tools for you) |

Rule of thumb: if your question is about an *existing, running app,* the answer is a pipeline tool (or the whole pipeline). If your question is about *creating or extracting* a project, it's one of the two standalone utilities.

---

<a id="dev-workflow"></a>
## 7. Fitting Into Your Workflow

**Why it matters:** a tool you run "someday" never gets run. FreeTools pays off when you wire it into normal development rhythms. Here's where it naturally fits.

- **Starting a new app:** run **ForkCRM** once to stamp out the project with the modules you want. This is a one-time creation step, not part of the regular loop.
- **While building a feature:** run **EndpointMapper** and **WorkspaceInventory** freely — they're pure static analysis, need no running server, and finish fast. They tell you whether your new page registered its route and whether any file has ballooned past a comfortable size.
- **Before a pull request:** run the **full pipeline** (`dotnet run --project FreeTools.AppHost`). Because the output is keyed by git branch, you get a clean before/after picture: new screenshots, new routes, and a fresh accessibility scan for exactly the branch you're about to merge. Commit the run folder (or attach the report) so reviewers can see the visual and a11y impact without checking out your branch.
- **During review:** point a reviewer (human or LLM) at `{Project}-Report.md`, or run **AppExtractor** to hand them *only* your customization files — far less noise than the whole repo.
- **As a regression guard:** because runs are diffable plain text and images, comparing this week's `latest` against last week's surfaces routes that disappeared, pages that went blank, or accessibility violations that crept in.

The mental model: **mapper and inventory are your fast inner loop; the full AppHost run is your pre-merge checkpoint; ForkCRM and AppExtractor are occasional one-off utilities.**

---

<a id="related-docs"></a>
## 8. Related Docs

- [072 — Up and Scanning: Pointing the CLI at an App](072_install-and-invoke.md) — install and run it
- [073 — Mapping Every Route](073_route-discovery.md) — route discovery
- [074 — Screenshots Without a Browser Window](074_headless-screenshots.md) — screenshots
- [075 — Sweeping for ADA and Accessibility Gaps](075_ada-scanning.md) — accessibility scanning
- [076 — Building On the FreeTools Core Library](076_tooling-core-library.md) — the core library

---
*GuidesV2 · 071 · drafted from source · the FreeTools analysis suite (AppHost, Core, and the pipeline + standalone tools).*
