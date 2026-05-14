# FreeA11yChecker.Scanner

Core accessibility scanning engine for FreeA11yChecker. Uses Playwright to drive a real browser (Microsoft Edge) and runs four accessibility analysis tools on every page: axe-core, IBM Equal Access, HTML CodeSniffer, and a custom HtmlChecker. Results are merged and deduplicated by a consensus scorer. Produces screenshots, visual overlays, color vision deficiency simulations, markdown reports, and structured result objects. Has no dependency on the web host or database — it is a pure library invoked by both the server background service and the console CLI.

## Build Details

| Property | Value |
|----------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Target framework | `net10.0` |
| Output type | Class library |
| Root namespace | `FreeA11yChecker.Scanner` |

## Key Classes / Methods

| Class | Purpose |
|---|---|
| `ScannerEngine` | Static orchestrator. `ScanAll(ScanConfig)` iterates all configured sites; `ScanSite(IBrowser, SiteConfig, ...)` creates a browser context, authenticates if needed (with storage-state cookie reuse), and scans each page; `ScanPage(IPage, PageConfig, ...)` runs the full 23-step pipeline on one URL |
| `AxeCoreRunner` | Downloads axe-core 4.10.2 from CDN on first run (cached locally), injects it into the Playwright page via `EvaluateAsync`, and parses the violations JSON into `A11yIssue` objects |
| `IbmEqualAccessRunner` | Injects IBM Equal Access checker JS and collects violations with WCAG mapping |
| `HtmlCodeSnifferRunner` | Injects HTML CodeSniffer and collects WCAG 2.x violations |
| `HtmlChecker` | Parses the page DOM with regex and structural checks: missing alt text, missing `<html lang>`, empty links/buttons, heading hierarchy, `<main>` landmark, skip-link presence, `<table>` header associations |
| `ConsensusScorer` | Normalizes rule IDs from all four tools to canonical axe-core IDs, merges duplicates, and assigns a consensus confidence score based on how many tools flagged each issue |
| `ReportGenerator` | Pure static class. `GeneratePageReport(PageScanResult)`, `GenerateSiteReport(SiteScanResult)`, `GenerateRunReport(RunScanResult)` — all return markdown strings with summary tables, screenshot galleries, and ranked violation lists |
| `AuthHandler` | Playwright-based authentication flow supporting form login, WSU SSO, and Blazor Identity forms; saves Playwright storage state to disk for reuse across crawl iterations |

### Other scanning modules

| Class | Purpose |
|---|---|
| `CvdSimulator` | Renders 7 color vision deficiency types as side-by-side screenshots |
| `QuickPeekOverlay` | Generates annotated screenshots with numbered violation markers |
| `StructureOverlay` | Renders heading hierarchy and landmark structure as a visual overlay |
| `ScreenReaderView` | Produces a linearized text representation of the page as a screen reader would encounter it |
| `KeyboardNavSimulator` | Simulates Tab-key navigation and records the focus order |
| `MobileViewportScanner` | Re-scans pages at common mobile viewport sizes |
| `ReadingLevelAnalyzer` | Computes Flesch-Kincaid and related readability scores for page text |
| `TextSpacingTest` | Applies WCAG 1.4.12 text-spacing overrides and checks for content loss |
| `CrossPageConsistency` | Compares heading levels, skip-link targets, and landmark patterns across all pages in a site |
| `CertAnalyzer` | Checks SSL certificate expiry and configuration for each site |

## Models

| Class | Purpose |
|---|---|
| `ScanConfig` | Top-level config: sites dictionary, output directory, WCAG level, concurrency, headless flag, timeout |
| `SiteConfig` | Per-site config: base URL, page list, credentials, cron schedule |
| `PageScanResult` | Full per-page result: status code, violations by engine, screenshots, images, ranked rules, overlays, performance metrics |
| `RunScanResult` | Aggregated result across all sites in a scan run |

## Project References

None — this library has no project references.

## Notable NuGet Packages

| Package | Purpose |
|---|---|
| `Microsoft.Playwright` | Headless browser automation (Chromium via Microsoft Edge) |

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
