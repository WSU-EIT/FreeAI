# FreeTools.EndpointMapper — Route Discovery Tool

> **Purpose:** Scans Blazor projects for @page directives and [Authorize] attributes, generating a CSV file of all routes for testing tools.  
> **Last Reviewed:** 2025-07-26  
> **Version:** 2.2

---

## Overview

The **EndpointMapper** tool is a development utility that:

- **Scans .razor files:** Finds all `@page` directives in Blazor projects
- **Extracts routes:** Builds a list of all routable pages
- **Detects auth requirements:** Identifies pages with `[Authorize]` attributes
- **Outputs CSV:** Generates a structured file for EndpointPoker and BrowserSnapshot

---

## ⚠️ Scope Limitations

This tool discovers **Blazor pages only**:

| Discovered | Not Discovered |
|------------|----------------|
| ✅ `@page` directives in `.razor` files | ❌ API controller routes (`[Route]`, `[HttpGet]`) |
| ✅ `@attribute [Authorize]` | ❌ Minimal API routes (MapGet/MapPost) |
| | ❌ Programmatic route registration |

For API endpoint documentation, use Swagger/OpenAPI.

---

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime framework |

---

## Usage

### Via Aspire AppHost (Recommended)

The tool runs automatically when starting FreeTools.AppHost:

```bash
cd tools/FreeTools.AppHost
dotnet run
```

### Standalone

```bash
cd tools/FreeTools.EndpointMapper
dotnet run [rootToScan] [csvOutputPath] [--clean]
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `CLEAN_OUTPUT_DIRS` | Set to "true" to clean output directories | `false` |
| `OUTPUT_DIR` | Directory to clean before generating CSV | `page-snapshots` |

---

## Output Format

The tool generates a CSV file with the following columns:

```csv
FilePath,Route,RequiresAuth,Project
Components/Pages/Home.razor,/,false,CRM
Components/Pages/Settings.razor,/Settings,true,CRM
```

---

## Integration

```
┌─────────────────────────────────────────────────────────────────┐
│                      FreeTools Pipeline                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. EndpointMapper     2. EndpointPoker    3. BrowserSnapshot   │
│  ┌────────────┐        ┌────────────┐      ┌────────────────┐   │
│  │ Scan .razor│───────►│ HTTP GET   │      │ Playwright     │   │
│  │ files      │        │ each route │      │ screenshots    │   │
│  └────────────┘        └────────────┘      └────────────────┘   │
│        │                     │                    │             │
│        ▼                     ▼                    ▼             │
│   pages.csv            *.html files          *.png files        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📬 About

**FreeTools** is developed and maintained by **[Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/meet-our-staff/)** at **Washington State University**.

📧 Questions or feedback? Visit our [team page](https://em.wsu.edu/eit/meet-our-staff/) or open an issue on [GitHub](https://github.com/WSU-EIT/FreeTools/issues)

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** Phase 1 of the pipeline. It scans `.razor` files for `@page` directives (and `[Authorize]` attributes) and writes a `pages.csv` listing every route and whether it needs login — the route list the rest of the pipeline tests and screenshots.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.EndpointMapper/Program.cs) (plain .NET file scan; no external deps).

**Why does this exist?** The downstream tools (Poker, BrowserSnapshot) need to know what pages exist — this discovers them automatically from source.

**What does it beat?** It derives the route list **from the actual `.razor` source**, not a hand-kept list, so it never drifts. *(Honest scope: Blazor `@page` routes only — not API/minimal-API routes; use Swagger for those.)*

**Terminology:** **`@page` directive** — the line in a `.razor` file that declares its URL.

**The hard part, drawn:**
```
  scan **/*.razor ─▶ find @page + [Authorize] ─▶ pages.csv (FilePath, Route, RequiresAuth, Project)
        └─▶ feeds EndpointPoker + BrowserSnapshot
```
