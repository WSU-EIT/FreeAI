# FreeTools — Workspace Analysis & Testing Suite

A set of small .NET 10 CLI tools that analyse, exercise, and document a Blazor web
application, plus a .NET Aspire orchestrator that runs them together as one pipeline.

> **What this folder actually contains.** Three unrelated groups of code share this
> directory. Only the first is a suite in any meaningful sense:
>
> 1. **The analysis pipeline** — `AppHost` plus six tools built on `FreeTools.Core`. Coherent,
>    orchestrated, and the only group with a real dependency graph.
> 2. **[FreeCodeMaid](FreeCodeMaid/)** — a separate Roslyn code-reorganiser product with its own
>    solution, its own core library (`FreeCodeReorganizer.Core`, *not* `FreeTools.Core`), and four
>    front-ends. It shares no code with the pipeline.
> 3. **Two standalone utilities** — `ForkCRM` and `AppExtractor`. Neither references
>    `FreeTools.Core`, and `AppExtractor` is in no solution file.

---

## The pipeline

`FreeTools.AppHost` is a .NET Aspire app host. It starts the target web application, then fans
out the analysis tools and finally builds a report.

```
Phase 0   AppHost starts the target web app (default: FreeExamples, fixed ports 7271/5111)
             │
Phase 1   ├─► EndpointMapper ──────► pages.csv
          └─► WorkspaceInventory ──► workspace-inventory.csv
             │
Phase 2   ├─► EndpointPoker ───────► snapshots/*.html          ┐
          ├─► BrowserSnapshot ─────► snapshots/*.png + json    ├ all three run in parallel;
          └─► AccessibilityScanner ► a11y reports              ┘ each waits only on EndpointMapper
             │
Phase 3   └─► WorkspaceReporter ───► {Project}-Report.md       (waits for all five to finish)

Outputs:  Docs/runs/{Project}/{Branch}/latest/
```

The branch name is read by parsing `.git/HEAD` directly (`GetGitBranch`, `AppHost/Program.cs`).

### Tools

| Tool | Purpose | LOC |
|------|---------|----:|
| **[FreeTools.AppHost](FreeTools.AppHost/)** | Aspire orchestrator for the pipeline | 326 |
| **[FreeTools.Core](FreeTools.Core/)** | Shared CLI args, console output, route parsing, path helpers. No NuGet dependencies. | 294 |
| **[FreeTools.EndpointMapper](FreeTools.EndpointMapper/)** | Regex-scans `.razor` for `@page` and `[Authorize]` → `pages.csv` | 153 |
| **[FreeTools.EndpointPoker](FreeTools.EndpointPoker/)** | Parallel HTTP GET over every route; saves response bodies | 269 |
| **[FreeTools.BrowserSnapshot](FreeTools.BrowserSnapshot/)** | Playwright screenshots, two-pass (anonymous then authenticated) | 1,097 |
| **[FreeTools.WorkspaceInventory](FreeTools.WorkspaceInventory/)** | Roslyn codebase scan → CSV of files, types, routes | 496 |
| **[FreeTools.AccessibilityScanner](FreeTools.AccessibilityScanner/)** | Multi-engine WCAG audit (axe-core, HTML_CodeSniffer, IBM ACE, in-house rules) | 4,762 |
| **[FreeTools.WorkspaceReporter](FreeTools.WorkspaceReporter/)** | Aggregates the CSVs into a markdown dashboard | 1,140 |

### Standalone

| Tool | Purpose | LOC |
|------|---------|----:|
| **[FreeTools.ForkCRM](FreeTools.ForkCRM/)** | Clone FreeCRM, strip modules, rename. Shells out to two prebuilt `.exe` files in [FreeCRM-utilities/](FreeCRM-utilities/) whose source is not in this repo. | 386 |
| **[FreeTools.AppExtractor](FreeTools.AppExtractor/)** | Copies the `.App.*` customisation layer out of a FreeCRM fork. **Not in any solution file.** | 216 |

### Separate product

| | |
|---|---|
| **[FreeCodeMaid](FreeCodeMaid/)** | Roslyn C#/Razor member reorganiser. `1.0/` is current; `0.0/` is frozen. Has its own solution and the only unit tests in this tree. |

---

## Quick start

```bash
# Full pipeline against the default target (FreeExamples)
cd FreeTools.AppHost
dotnet run

# Against a different project
dotnet run -- --target YourProjectName

# Results
ls Docs/runs/FreeExamples/main/latest/
```

> **`--target` is only partly parameterised.** `AppHost/Program.cs` hardcodes a 27-entry list of
> FreeExamples-specific routes (`extraPages`) that is passed to the crawlers regardless of target.
> Pointing the pipeline at another project will still probe FreeExamples' route names.

### Running tools individually

Every tool reads configuration from **environment variables first, then positional arguments**
(`CliArgs.GetEnvOrArg`). The argument forms below are what AppHost does not use:

```bash
# EndpointMapper  — scan for routes
dotnet run --project FreeTools.EndpointMapper -- <rootToScan> <csvOutputPath> [--clean]

# EndpointPoker   — HTTP GET every route
dotnet run --project FreeTools.EndpointPoker -- <baseUrl> <csvPath> <outputDir> [maxThreads]

# BrowserSnapshot — screenshot every route
dotnet run --project FreeTools.BrowserSnapshot -- <baseUrl> <csvPath> <outputDir> [maxThreads]

# WorkspaceInventory — Roslyn codebase scan
dotnet run --project FreeTools.WorkspaceInventory -- <rootDir> <csvOutputPath> [--noCounts]

# WorkspaceReporter — build the markdown report
dotnet run --project FreeTools.WorkspaceReporter -- <repoRoot> <outputPath>

# AccessibilityScanner — reads appsettings.json when BASE_URL is unset
dotnet run --project FreeTools.AccessibilityScanner

# ForkCRM
dotnet run --project FreeTools.ForkCRM -- --name MyProject --modules all --output "C:\repos\MyProject"

# AppExtractor
dotnet run --project FreeTools.AppExtractor -- --source "<fork>" --output "<dir>" [--dry-run true]
```

---

## Environment variables

### Shared by the crawlers (EndpointPoker, BrowserSnapshot, AccessibilityScanner)

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | — | Base URL of the running web app. For AccessibilityScanner, setting this switches it from `appsettings.json` mode to pipeline mode. |
| `CSV_PATH` | — | Path to `pages.csv` from EndpointMapper |
| `OUTPUT_DIR` | — | Output directory |
| `MAX_THREADS` | `10` | Maximum parallel requests |
| `START_DELAY_MS` | 2000–5000 | Sleep before starting, to let the web app warm up |
| `TENANT_CODE` | — | Substituted into `{TenantCode}` route segments |
| `LOGIN_USERNAME` / `LOGIN_PASSWORD` | `admin` / `admin` | Credentials for the authenticated pass |

### EndpointMapper

| Variable | Default | Description |
|----------|---------|-------------|
| `CLEAN_OUTPUT_DIRS` | `false` | Recursively delete `OUTPUT_DIR` before scanning |
| `OUTPUT_DIR` | `page-snapshots` | Directory to clean |

### BrowserSnapshot

| Variable | Default | Description |
|----------|---------|-------------|
| `PAGE_SETTLE_DELAY_MS` | `3000` | Wait after load before capturing |

### WorkspaceInventory

| Variable | Default | Description |
|----------|---------|-------------|
| `ROOT_DIR` | — | Directory to scan |
| `CSV_PATH` | — | Output CSV path |
| `NO_COUNTS` | `false` | Skip line/character counting |
| `AZDO_ORG_URL`, `AZDO_PROJECT`, `AZDO_REPO` | — | When all three are set, emits Azure DevOps links per file |

### WorkspaceReporter

| Variable | Default | Description |
|----------|---------|-------------|
| `REPO_ROOT` | — | Repository root |
| `OUTPUT_PATH` | — | Output markdown path |
| `WEB_PROJECT_ROOT` | `Web\FreeTools.Web` | Stale default from an earlier layout; set it explicitly |

---

## Shared utilities (FreeTools.Core)

| File | Purpose |
|------|---------|
| `CliArgs.cs` | Argument and environment parsing. **`HasFlag` and `GetOption` mutate the argument list** — they remove what they match. |
| `ConsoleOutput.cs` | Lock-guarded writes, banner/divider/config formatting |
| `RouteParser.cs` | Parses `pages.csv`, substitutes `{TenantCode}`, de-duplicates so tenant-scoped routes win |
| `PathSanitizer.cs` | Route→filesystem path conversion, byte formatting |

`RouteParser` splits CSV lines on `,` without honouring quoted fields, which is why EndpointMapper
rewrites commas inside routes as `?` before writing. A genuinely quoted field containing a comma
would still break it.

---

## Build

```bash
dotnet build FreeTools.slnx
```

Two caveats:

- `FreeCodeMaid/0.0` (explicitly frozen) is in this solution; `FreeCodeMaid/1.0` — the live version —
  is not. Build `FreeCodeMaid/1.0/FreeCodeReorganizer.slnx` separately.
- `FreeTools.AppExtractor` is in no solution and must be built by project path.

---

## Known rough edges

These are real and worth knowing before you rely on the pipeline:

- **Readiness is sleep-based.** Tools wait `START_DELAY_MS` (2–5 s) *in addition to* Aspire's
  `WaitFor`. There are no health checks, so a slow start can produce empty results rather than an error.
- **Credentials are hardcoded** as `admin`/`admin` in `BrowserSnapshot/Program.cs` and
  `AppHost/Program.cs`. Override with `LOGIN_USERNAME` / `LOGIN_PASSWORD`.
- **EndpointPoker disables TLS validation unconditionally.** Fine against localhost; a real problem
  if pointed at anything else.
- **AccessibilityScanner is 4,762 lines in one file**, ships ~1.5 MB of vendored JavaScript
  (`axe.min.js`, `ace.js`, `HTMLCS.js`), and its `appsettings.json` contains 128 hardcoded public
  WSU URLs. It works and is used in earnest, but it is not a small dependency.
- **Dead configuration:** `WORKSPACE_CSHARP_CSV` and `WORKSPACE_RAZOR_CSV` are read by
  WorkspaceReporter but nothing in the pipeline writes them.
- **No tests** anywhere in the pipeline. The only unit tests in this tree belong to
  `FreeCodeMaid/1.0/FreeCodeReorganizer.Core.Tests` (7 facts).

---

## About

**FreeTools** is developed and maintained by
**[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/meet-our-staff/)** at
**Washington State University**.

Questions or feedback? Visit our [team page](https://em.wsu.edu/eit/meet-our-staff/) or open an
issue on [GitHub](https://github.com/WSU-EIT/FreeTools/issues).
