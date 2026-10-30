# FreeAccessibilityChecker -- Feature Inventory

> **Category:** Features
> **Purpose:** Complete catalog of what this project can do today.

---

## Scanning engines

- [x] axe-core (Deque) -- WCAG 2.0/2.1/2.2 rule engine
- [x] IBM Equal Access -- WCAG 2.1/2.2 with potential and manual checks
- [x] HTML_CodeSniffer (Squiz) -- 84 WCAG rules
- [x] Custom HtmlChecker -- 20 regex-based HTML pattern checks
- [x] Results merged and deduplicated across all four engines

## Screenshot captures

- [x] Login flow (anonymous, credentials entered, post-auth)
- [x] Page load sequence (PNG screenshots at intervals)
- [x] Page with expanded content (details, accordions, tabs)
- [x] 5 analysis overlays: axe, QuickPeek, htmlcs, IBM, structure/landmarks
- [x] Wireframe blueprint view
- [x] 7 color-vision-deficiency simulations (protanopia, deuteranopia, tritanopia, achromatopsia, and anomaly variants)
- [x] Screen reader linearized view
- [x] Reduced motion view
- [x] Forced colors (Windows High Contrast) view

## Crawl / scan modes

- [x] `scan` command -- scan a list of specific pages
- [x] `crawl` command -- seed from .razor @page directives + follow discovered links (configurable depth)
- [x] `--source-path` -- auto-seed from Blazor source routes
- [x] `--seed-ids` -- expand parameterized routes with real IDs
- [x] Per-page real-time artifact write (Ctrl+C preserves data collected so far)

## Reporting

- [x] Per-page markdown report
- [x] Per-page AI handoff document (prioritized fix list with violation counts)
- [x] Crawl summary: HTML report with per-page accordion, screenshot gallery, violations table
- [x] PDF report generation via QuestPDF
- [x] CSV violation export

## Web UI

- [x] Multi-tenant site management (add/edit/delete sites and pages)
- [x] Real-time scan progress via SignalR
- [x] Scheduled scans via NCrontab
- [x] On-demand scan trigger from the UI
- [x] Violation review and remediation tracking
- [x] Static source-code analysis (scan Blazor/Razor/HTML before deployment)

## Infrastructure

- [x] Multi-tenant authentication (cookie, Google, Microsoft, Apple, Facebook, OpenID Connect, Azure AD)
- [x] Roslyn plugin system (Auth, BackgroundProcess, UserUpdate, Example plugin types)
- [x] Azure SignalR Service fallback
- [x] EF Core multi-database (SQL Server, SQLite, PostgreSQL, MySQL, InMemory)
- [x] Load-balancing filter (restrict background service to specific hostname)

## Configuration

| Setting | Description |
|---------|-------------|
| `DatabaseType` | InMemory / SQLServer / PostgreSQL / MySQL / SQLite |
| `ConnectionStrings:AppData` | Database connection string |
| `BackgroundService:Enabled` | Enable/disable scheduled scanning |
| `BackgroundService:IntervalSeconds` | Polling interval (default 60) |
| `BackgroundService:LoadBalancingFilter` | Restrict to named server |
| `AzureSignalRurl` | Azure SignalR endpoint (blank = local) |