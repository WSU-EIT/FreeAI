# FreeA11yChecker -- Overview

> **Category:** Overview
> **Purpose:** What this project is, why it exists, and how to get started.

---

## What it is

FreeA11yChecker is an open-source WCAG 2.1 and 2.2 Level AA web accessibility auditing platform built in C# / Blazor WebAssembly / .NET 10. It automatically scans websites for accessibility violations using four independent engines (axe-core, IBM Equal Access, HTML_CodeSniffer, and a custom HtmlChecker), generates rich visual reports with screenshot overlays and color-vision-deficiency simulations, and provides a full multi-tenant web UI for managing scan targets, reviewing findings, and tracking remediation progress.

A standalone CLI tool (`FreeA11yChecker.Console`) lets you scan any URL from the terminal without the web UI, and includes a `crawl` mode that seeds page routes from Blazor `.razor @page` directives and follows discovered links up to a configurable depth.

## Why it exists

WSU-EIT must comply with ADA Title II, Washington State Technology Policy USER-01, and UPPM 10.45. WCAG 2.1 AA is required now; WCAG 2.2 AA is required by July 1, 2026. FreeA11yChecker was built to satisfy those obligations in-house and released as open-source so other institutions can use it without cost.

## Who it is for

- WSU web developers who need to identify and fix accessibility violations
- WSU-EIT compliance staff who need audit-ready reports and remediation tracking
- Any organization that needs a self-hosted, multi-tenant accessibility auditing tool

## Quick start

### Web UI

```bash
cd FreeA11yChecker/FreeA11yChecker
dotnet run
```

Navigate to `https://localhost:5001`.

### CLI -- scan any URL

```bash
cd FreeA11yChecker/FreeA11yChecker.Console
dotnet run -- scan --url https://example.com --user admin --pass admin
dotnet run -- crawl --url https://example.com --source-path ../FreeA11yChecker.Client
```

## Related projects

- [FreeCRM](https://github.com/WSU-EIT/FreeCRM) -- base application framework
- [run-a11y-showcase.ps1](../run-a11y-showcase.ps1) -- orchestrates InMemory showcase scans for all FreeAI projects

---

*Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT).***
*Website: https://em.wsu.edu/eit/ | GitHub: https://github.com/WSU-EIT | MIT License*