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

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?**
This library is the engine room. It exposes one static class, `ScannerEngine`, with three entry points — scan all sites, one site, or one page. For each page it runs a ~26-step pipeline: launch a real browser with Playwright, log in if credentials are supplied (then *save the login cookies so later pages skip the login*), load the page and wait for it to fully render, run the four accessibility engines, merge their output with `ConsensusScorer`, and capture overlays, 7 color-blindness simulations, and a screen-reader view. It writes nothing to a database — it hands a `PageScanResult` back to whoever called it (the web app stores it; the CLI writes it to disk).

**What technology does it use — and where exactly?**

| Technology | What it's for | Exact location |
|---|---|---|
| Playwright (headless Edge) | Drives the real browser | [ScannerEngine.cs#L37-L42](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ScannerEngine.cs#L37-L42) |
| Storage-state auth reuse | Log in once per crawl, not once per page | [ScannerEngine.cs#L91-L130](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ScannerEngine.cs#L91-L130) |
| axe-core / IBM / HTML CodeSniffer / HtmlChecker | The four engines | [AxeCoreRunner.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/AxeCoreRunner.cs) · [IbmEqualAccessRunner.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/IbmEqualAccessRunner.cs) · [HtmlCodeSnifferRunner.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/HtmlCodeSnifferRunner.cs) · [HtmlChecker.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/HtmlChecker.cs) |
| Consensus scoring | Reconcile + rank the four engines | [ConsensusScorer.cs#L75-L156](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ConsensusScorer.cs#L75-L156) |
| CVD simulation (SVG filters) | 7 color-blindness renderings | [CvdSimulator.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/CvdSimulator.cs) |
| Markdown report generation | Per-page / site / run reports | [ReportGenerator.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ReportGenerator.cs) |

**Why does this exist?**
So the *exact same* scanning logic powers both the website and the command-line tool. By keeping zero dependency on the database or web host, one well-tested engine produces identical results whether a developer runs it locally or the server runs it on a schedule.

**What does it accomplish that other tools don't?**
- It runs four engines and reconciles their different "languages" — each tool names the same rule differently (`img-alt`, `H37`, `img_alt_missing` all mean *missing alt text*) — into one canonical, confidence-ranked list. See the normalization map at the top of `ConsensusScorer.cs`.
- Real browser, real rendering: it waits for Blazor/SPA pages to hydrate before checking, so it audits what users actually see, not raw HTML.
- It captures the visual evidence (overlays, CVD, screen-reader view) in the same pass.

**Terminology & "can I see it?"**
- **Pipeline** — the ordered ~26 steps `ScanPage` runs per page (heavily commented in [ScannerEngine.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeA11yChecker/FreeA11yChecker.Scanner/ScannerEngine.cs)).
- **Storage state** — Playwright's saved cookies/localStorage; saved after a successful login and reused so a 5-page crawl logs in once, not five times.
- **Canonical rule ID** — the single ID all four engines' findings get mapped to.

**The hard part, drawn** — four engines, four vocabularies, one ranked list:

```
 ScannerEngine.ScanPage(page)
        │ navigate → wait for hydration → capture the live DOM
        ▼
 ┌──────────── the SAME DOM handed to all four engines ─────────────┐
 │  axe-core      IBM Equal     HTML CodeSniffer     HtmlChecker     │
 │     │              │                │                  │          │
 │  issues         issues           issues            issues        │  each with its OWN rule IDs
 └─────┴──────────────┴────────────────┴──────────────────┴─────────┘
                        │  ConsensusScorer.Merge(axe, htmlcheck, htmlcs, ibm)
                        ▼
   1) NormalizeRuleId():  "img-alt" · "...H37" · "img_alt_missing"  →  "image-alt"
   2) group by canonical id  →  tools that AGREE = consensus count
   3) confidence = agree ÷ (tools that COULD check this rule)
   4) sort by: confidence ↓, then severity ↓, then instance-count ↓
                        ▼
        A11yPageSummary.RankedRules  ──▶  returned to caller (web stores it / CLI writes it)
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
