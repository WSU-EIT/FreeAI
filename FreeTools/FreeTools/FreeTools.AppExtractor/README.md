# FreeTools.AppExtractor — FreeCRM Customization Extractor

> **Purpose:** Extracts your `.App.*` customization layer from a FreeCRM fork so you can safely back it up, migrate it, or re-apply it after a framework upgrade.  
> **Version:** 1.0  
> **Last Updated:** 2025-07-25

---

## Overview

**AppExtractor** is a standalone CLI tool that:

- **Scans a FreeCRM fork** for all files that belong to your customization layer
- **Copies them** to an output directory, preserving folder structure
- **Includes supporting files** — config, docs, migration scripts
- **Supports dry-run** — preview what would be copied without writing anything

---

## What Gets Extracted

| Category | Rule | Example |
|----------|------|---------|
| **App files** | Any filename containing `.App.` | `DataController.App.FreeExamples.cs` |
| **Whole directories** | Configured folders copied in full | `FreeExamples.Docs/` |
| **Explicit files** | Named files in project roots | `appsettings.json` (1-level deep only) |
| **Root files** | Solution/config files at repo root | `.slnx`, `.sln`, `.gitignore` |

### Default Skip Directories

`bin`, `obj`, `.vs`, `.git`, `node_modules`, `wwwroot/lib`, `Migrations`, `runs`

---

## Why This Exists

FreeCRM follows a base-template + extension pattern. Your custom code lives in `.App.*` files — the framework code does not. When FreeCRM releases an update, you:

1. Run `AppExtractor` to capture your customizations
2. Pull the updated FreeCRM base
3. Re-apply your extracted files

This keeps your work clearly separated and upgrades safe.

---

## Usage

### Command Line

```bash
cd FreeTools/FreeTools.AppExtractor
dotnet run -- --source "C:\repos\MyFreeCRMFork" --output "C:\repos\MyFreeCRMFork-extracted"
```

### Dry Run (preview only)

```bash
dotnet run -- --source "C:\repos\MyFreeCRMFork" --output "C:\repos\extracted" --dry-run true
```

### Via appsettings.json

```json
{
  "AppExtractor": {
    "SourceRoot": "C:\\repos\\MyFreeCRMFork",
    "OutputRoot": "C:\\repos\\extracted",
    "DryRun": false
  }
}
```

---

## Command Line Arguments

| Argument | Description |
|----------|-------------|
| `--source` | Path to the FreeCRM fork to scan (required) |
| `--output` | Path to write extracted files to (required) |
| `--dry-run` | Set to `true` to preview without copying (default: `false`) |

---

## Configuration (appsettings.json)

| Key | Default | Description |
|-----|---------|-------------|
| `AppExtractor:SourceRoot` | `""` | Source repo root |
| `AppExtractor:OutputRoot` | `""` | Output directory |
| `AppExtractor:DryRun` | `false` | Preview mode |
| `AppExtractor:FilePattern` | `.App.` | Pattern to match app files |
| `AppExtractor:SkipDirectories` | see above | Directories to skip |
| `AppExtractor:WholeDirectories` | `["FreeExamples.Docs"]` | Folders to copy entirely |
| `AppExtractor:ExplicitFiles` | `["appsettings.json", ...]` | Specific filenames to include |
| `AppExtractor:RootFileExtensions` | `[".slnx", ...]` | Root-level file extensions to include |

---

## Output

Files are written to `OutputRoot` with the same relative paths as the source. A summary is printed grouped by project directory:

```
📁 FreeExamples (3 files)
     FreeExamples\DataController.App.FreeExamples.cs
     FreeExamples\appsettings.json

📁 FreeExamples.Client (5 files)
     FreeExamples.Client\FreeExamples.App.Dashboard.razor
     FreeExamples.Client\FreeExamples.App.Dashboard.razor.cs
     ...
```

Green = `.App.` files. Gray = supporting files (config, docs, etc.).

---

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime |
| Microsoft.Extensions.Configuration | 10.0.1 | `appsettings.json` + CLI arg binding |

---

Part of the FreeTools solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** It scans a FreeCRM fork and copies out **just your customization layer** — every file containing `.App.` (plus configured whole folders, named files, and root solution/config files) — preserving folder structure. Supports a dry-run to preview first.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeTools/FreeTools/FreeTools.AppExtractor/Program.cs) (config-driven via `appsettings.json` + CLI flags).

**Why does this exist?** FreeCRM keeps your code in `.App.*` files separate from framework files. When FreeCRM updates, you extract your `.App.*` layer, pull the new base, and re-apply — making upgrades clean.

**What does it beat?** It **mechanically separates your code from the framework's** by the `.App.` naming convention, so a framework upgrade doesn't mean diffing every file.

**Terminology:** **`.App.` layer** — your customizations, kept in separately-named files the framework never touches.

**The hard part, drawn:**
```
  scan FreeCRM fork ─▶ match `.App.` files (+ whole dirs + named/root files) ─▶ copy to output (same paths)
        --dry-run ─▶ preview only      → safe upgrade: extract → pull new base → re-apply
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
