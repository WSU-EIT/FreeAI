# FreeTools.AccessibilityScanner — Accessibility Audit Tool

> **Purpose:** Scans one or more websites for accessibility issues using Playwright for browser rendering and optional WAVE API for WCAG analysis.  
> **Version:** 1.0  
> **Last Updated:** 2025-07-26

---

## Overview

**AccessibilityScanner** is a standalone CLI tool that:

- **Renders pages** using a real Chromium browser via Playwright
- **Captures screenshots** of each page for visual reference
- **Detects accessibility issues** using axe-core or WAVE API
- **Supports authentication** — configurable credentials per site
- **Multi-site** — scan multiple sites in one run with per-site page lists
- **Parallel execution** — configurable concurrency limit
- **Outputs a summary report** with per-page results and a rules legend

---

## Usage

### Configure appsettings.json

```json
{
  "Scanner": {
    "SettleDelayMs": 5000,
    "TimeoutMs": 30000,
    "MaxConcurrency": 25,
    "Headless": true,
    "WcagLevel": "wcag21aa",
    "WaveApiKey": "",
    "Sites": {
      "https://yoursite.example.com/": {
        "Credentials": [],
        "Pages": [
          "/",
          "/about/",
          "/contact/"
        ]
      }
    }
  }
}
```

### Run

```bash
cd FreeTools/FreeTools.AccessibilityScanner
dotnet run
```

---

## Configuration Reference

| Key | Default | Description |
|-----|---------|-------------|
| `Scanner:SettleDelayMs` | `5000` | Wait after page load before scanning (ms) |
| `Scanner:TimeoutMs` | `30000` | Page navigation timeout (ms) |
| `Scanner:MaxConcurrency` | `25` | Max parallel site scans |
| `Scanner:Headless` | `true` | Run browser headless (`false` to watch) |
| `Scanner:WcagLevel` | `wcag21aa` | WCAG conformance level for WAVE API |
| `Scanner:WaveApiKey` | `""` | WAVE API key — leave empty to disable WAVE |
| `Scanner:UserAgent` | Chrome UA | Browser user-agent string |
| `Scanner:Sites` | — | Map of site URL → site config (see below) |

### Per-Site Config

| Key | Description |
|-----|-------------|
| `Sites["url"].Pages` | List of paths to scan (relative to site root) |
| `Sites["url"].Credentials` | Auth credentials for login flows |

---

## Output

All results are written to `runs/latest/` next to the project source:

```
runs/latest/
├── run-report.md           ← Summary across all sites
├── rules-legend.md         ← Index of all accessibility rules found
└── yoursite-example-com/
    ├── index/
    │   ├── screenshot.png
    │   ├── metadata.json
    │   └── issues.json
    └── about/
        ├── screenshot.png
        └── ...
```

### Per-Page Metadata

Each page produces a `metadata.json` with:

```json
{
  "pagePath": "/about/",
  "statusCode": 200,
  "htmlSize": 48210,
  "screenshotSize": 142080,
  "success": true,
  "consoleErrors": [],
  "screenshots": ["screenshot.png"]
}
```

### Console Summary

```
📂 https://yoursite.example.com/ → yoursite-example-com/
   ✅ /
      Status:      200
      HTML:        48.2 KB
      Screenshots: 1 (138.7 KB)
   ✅ /about/
      ...
```

---

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime |
| Microsoft.Playwright | 1.56.0 | Headless browser rendering + screenshots |
| WAVE API | optional | WCAG accessibility analysis |
| FreeTools.Core | local | Console output, path utilities |

---

## Notes

- The root page of each site is always scanned in addition to the `Pages` list.
- Playwright browsers are auto-installed on first run (`chromium` only).
- Previous run output is deleted before each run to keep results fresh.
- Set `Headless: false` in `appsettings.json` to watch the browser during development.

---

Part of the FreeTools solution.

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
