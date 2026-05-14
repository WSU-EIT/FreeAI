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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
