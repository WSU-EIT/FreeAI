# FreeAI Documentation Standard

Every project area under `FreeAI/` follows this structure.

## Directory layout

```
{ProjectArea}/
  Docs/
    000_overview.md       — What it is, why it exists, quick start
    001_features.md       — Complete inventory of working features (checkboxed)
    002_roadmap.md        — Future plans, gaps, ideas (prioritized, checkboxed)
    003_architecture.md   — Internal structure, design decisions, data flows
    004_showcase.md       — Gallery index linking into showcase/ artifacts
    showcase/
      screenshots/        — PNG captures (Blazor/web projects via FreeTools)
      runs/               — Console transcript files (CLI tools)
      logs/               — Service log excerpts (background services)
      examples/           — Example usage output (libraries)
      executions/         — Plugin run output (plugin projects)
```

## Document numbering

| Range | Purpose |
|-------|---------|
| 000–009 | Core identity: overview, features, roadmap, architecture, showcase |
| 010–049 | Component and module deep-dives |
| 050–099 | Patterns and conventions specific to this project |
| 100–199 | API reference (if applicable) |
| 200–299 | Meeting notes and decision records |
| 300–399 | Research |

## Feature inventory conventions (`001_features.md`)

- `[x]` — implemented and working
- `[-]` — partially implemented
- `[ ]` — planned (move to `002_roadmap.md` if not near-term)

## Showcase capture by project type

| Project type | Tool | Output location |
|--------------|------|-----------------|
| Blazor web app | FreeTools AppHost or dedicated ShowcaseTool | `showcase/screenshots/{Route}/default.png` |
| Console / CLI | Redirect stdout: `dotnet run > showcase/runs/run-YYYY-MM-DD.txt` | `showcase/runs/` |
| Windows service | Capture log output or Event Log | `showcase/logs/` |
| Class library | Example program output | `showcase/examples/` |
| Plugin | Plugin execution output | `showcase/executions/` |

## Existing patterns to reference

- **FreeTools** — pipeline that auto-generates screenshots, route maps, and workspace reports
  (`FreeAI/FreeTools/FreeTools/Docs/000_overview.md`)
- **FreeBlazorExtended.ShowcaseTool** — Playwright + Magick.NET screenshot and GIF generator
  (`FreeAI/FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample.ShowcaseTool/`)
- **FreeA11yChecker Docs** — full numbered doc set with architecture, patterns, and component guides
  (`FreeAI/FreeA11yChecker/Docs/`)
