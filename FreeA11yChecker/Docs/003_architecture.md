# FreeA11yChecker -- Architecture

> **Category:** Architecture
> **Purpose:** How the projects fit together and how data flows.

---

## Project structure

| Project | Role |
|---------|------|
| `FreeA11yChecker` | Blazor Server host; REST API, auth, SignalR hub, scheduled scan background service, PDF delivery |
| `FreeA11yChecker.Client` | Blazor WASM; scan dashboards, issue review, site management, real-time progress |
| `FreeA11yChecker.Scanner` | Core scanning engine; Playwright + 4 a11y engines + overlays + CVD + report generator |
| `FreeA11yChecker.Console` | CLI; `scan`, `crawl`, `handoff`, `analyze-source` commands; `RunLogger` tee to `_logs/scan.log` |
| `FreeA11yChecker.DataAccess` | Business logic; EF Core, PDF (QuestPDF), Azure AD/LDAP auth, MS Graph |
| `FreeA11yChecker.DataObjects` | Shared DTOs; `ScanConfig`, `SiteConfig`, `PageScanResult`, `SystemSnapshot` |
| `FreeA11yChecker.EFModels` | EF Core DbContext; sites, pages, scan runs, violations, users, tenants |
| `FreeA11yChecker.Plugins` | Roslyn dynamic C# plugin runtime |
| `A11yAudit` | Console; existing internal audit runner (separate from the CLI) |

## Scan engine data flow

```
ScannerEngine.ScanAll(config)
  -> foreach site/page:
       -> Playwright launches headless Chromium
       -> auth flow (login if credentials provided)
       -> page.goto(url), wait for Blazor/SPA hydration
       -> extract links for discovery
       -> capture page-load PNG sequence
       -> expand collapsed content (details/accordions/tabs)
       -> run axe-core, HtmlChecker, HTML_CodeSniffer, IBM Equal Access
       -> capture 5 analysis overlays
       -> capture 7 CVD simulations
       -> capture screenreader/reduced-motion/forced-colors views
       -> OnPageComplete callback -> write artifacts to disk (CLI) or EF Core (web)
  -> generate per-site AI handoff document
```

## CLI output structure

```
runs/latest/
  {hostname}/
    _root/          (/ page)
      *.png / *.jpeg   screenshots
      a11y-*.json      per-engine violation JSON
      a11y-summary.json
      report.md
      page.html
    {slug}/         (other pages)
    _crawl-summary.html
    _ai-handoff.md
  _logs/
    scan.log        (fixed name -- overwrites each run; git history is the audit trail)
  scan-log.txt      (orchestrator wrapper log)
```

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*