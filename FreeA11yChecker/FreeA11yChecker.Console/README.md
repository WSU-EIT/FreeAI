# FreeA11yChecker.Console

Standalone CLI for FreeA11yChecker. Thin wrapper around `FreeA11yChecker.Scanner` — all scanning logic lives in that library; this project handles argument parsing, configuration loading, interactive menus, log tee-ing, and output formatting. Shares the same `UserSecretsId` as the web host so credentials only need to be set once.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | `Exe` |
| User Secrets ID | `0cbc4331-d04d-4e10-97b7-24a8798f626c` (shared with web host) |

## What It Does

- Reads scan configuration from `appsettings.json` and user secrets, with command-line args as overrides.
- Routes to one of five actions: `scan`, `crawl`, `analyze-source`, `handoff`, `report`, or falls back to an interactive menu.
- `scan` — runs `ScannerEngine.ScanAll` against sites defined in config, or against a single `--url` with optional `--pages`, `--user`, and `--pass` arguments.
- `crawl` — iteratively scans a site by following same-host links discovered on each page, up to `--max-depth` iterations. Supports `--source-path` to pre-seed the crawl from Blazor `@page` directives found in source files.
- `analyze-source` — static source analysis: scans `.razor`, `.cshtml`, and `.html` files for ARIA, image alt, form label, and structural issues before deployment (via `SourceAnalysis.CodebaseInventory`).
- `handoff` — generates a markdown AI handoff document summarizing violations, evidence paths, and remediation guidance.
- `report` — prints a summary of the last scan results from the output directory.
- Tees all console output to a timestamped log file under `{OutputDir}/_logs/` for automated evidence collection.
- Interactive menu (when invoked with no arguments) provides numbered options for common operations.

## Key Classes / Methods

| Class / Method | Purpose |
|---|---|
| `RunAction` | CLI dispatcher; routes the first non-flag argument to the appropriate handler |
| `RunScanFromArgs` | Parses `--url`, `--pages`, `--user`, `--pass`; builds a `ScanConfig` and calls `ScannerEngine.ScanAll` |
| `RunCrawlFromArgs` | Iterative crawl mode; seeds from `@page` directives when `--source-path` is provided; enforces same-host filtering |
| `RunInteractive` | Text-based menu loop for interactive use |
| `ColorOut` (`ColorOut.cs`) | Console output helpers with color coding for steps, success, warnings, and errors |
| `RunLogger` (`RunLogger.cs`) | Tees `Console.Out` to a timestamped file in `{OutputDir}/_logs/` |
| `SourceAnalysis.CodebaseInventory` (`SourceAnalysis/`) | Walks a source tree, extracts all `@page` route directives, and inventories ARIA/alt/form issues in static markup |

## Project References

| Project | Role |
|---|---|
| `FreeA11yChecker.Scanner` | All scanning, overlay, and report generation logic |

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.Configuration` | Config builder |
| `Microsoft.Extensions.Configuration.Json` | `appsettings.json` support |
| `Microsoft.Extensions.Configuration.CommandLine` | CLI arg overrides |
| `Microsoft.Extensions.Configuration.UserSecrets` | Shared user secrets |

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
A thin command-line wrapper around the Scanner library. You type `scan`, `crawl`, `analyze-source`, `handoff`, or `report` (or nothing, for an interactive menu); it parses the arguments, loads config (sharing the *same* user secrets as the web app), calls `ScannerEngine`, and tees all output to a timestamped log for evidence. `crawl` follows same-site links it discovers as it goes; `analyze-source` checks `.razor`/`.html` source files for issues *before* you even deploy.

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| FreeA11yChecker.Scanner | All scanning/overlay/report logic | [ScannerEngine.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ScannerEngine.cs) |
| .NET config + user secrets | Args, `appsettings.json`, shared secrets | [the Console project](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Console) |
| Static source analysis | Find a11y issues in `.razor`/`.html` pre-deploy | [Console project › SourceAnalysis](https://github.com/WSU-EIT/FreeAI/tree/main/FreeA11yChecker/FreeA11yChecker.Console) |

**Why does this exist?**
So a developer or a CI pipeline can scan any URL with zero web UI — using the exact same engine and the same stored credentials as the website.

**What does it accomplish that other tools don't?**
- **Same engine as the web app** — local check and scheduled audit can't disagree.
- **Static source analysis** — catches missing alt text / labels in source before anything ships.
- An **AI "handoff" document** — a markdown fix-list ready to hand to a developer or an AI agent.

**Terminology & "can I see it?"**
- **Tee** — write console output to a file *and* the screen at once (for an evidence log).
- **Crawl vs scan** — `scan` hits a fixed page list; `crawl` discovers same-site links and follows them.
- **User secrets** — credentials stored outside the repo, shared with the web host by ID.

**The hard part, drawn** — one command, full evidence trail:

```
  you ▶ dotnet run -- scan --url …
          │ parse args + load config (shared user secrets)
          ▼
     RunAction ──▶ ScannerEngine.ScanAll ──▶ (Playwright + the 4 engines)
          │                                     │ one PageScanResult per page
          └── tee everything ──▶ _logs/scan.log ◀┘  (+ screenshots / JSON / markdown to disk)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
