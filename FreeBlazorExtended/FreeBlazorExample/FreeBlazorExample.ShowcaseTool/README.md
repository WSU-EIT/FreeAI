# FreeBlazorExample.ShowcaseTool

Console executable that generates static showcase artifacts for the FreeBlazorExtended component library.

Uses Playwright to drive a headless Chromium browser against the running FreeBlazorExample showcase site, captures full-page screenshots of each `/showcase/*` page, records animated GIFs of interactive components (carousel, kanban, timer, etc.), and produces component-level thumbnails. Uses Magick.NET-Q8 for GIF frame assembly and thumbnail resizing. Output is written to `showcase-<date>-<time>/` alongside the tool executable.

## Key classes

| Class | File | Purpose |
|-------|------|---------|
| `Program` | `Program.cs` | Entry point; parses CLI args (target URL, output dir, component list), orchestrates Playwright sessions |

## Notable NuGet packages

| Package | Version | Use |
|---------|---------|-----|
| `Microsoft.Playwright` | 1.44.0 | Headless Chromium browser automation |
| `Magick.NET-Q8-AnyCPU` | 14.13.0 | GIF frame composition and image thumbnailing |

## Build details

| Field | Value |
|-------|-------|
| SDK | `Microsoft.NET.Sdk` |
| Output type | `Exe` |
| Assembly name | `ShowcaseTool` |
| Target framework | net10.0 |
| Nullable | enabled |
| Unsafe blocks | allowed |

Part of the **FreeBlazorExtended** solution.

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A console tool that turns the running showcase site into static visual documentation. It drives a headless Chromium browser (Playwright) to each `/showcase/*` page, takes full-page screenshots, **records animated GIFs** of interactive components (carousel, kanban, timer…), and makes thumbnails — assembling GIF frames and resizing with Magick.NET — into a timestamped output folder.

**What tech & where?** [Program.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExample/FreeBlazorExample.ShowcaseTool/Program.cs) (Playwright for capture, Magick.NET for GIF/thumbnail assembly).

**Why does this exist?** To auto-generate the component library's visual catalog (screenshots + GIFs) instead of recording them by hand — so the docs stay current as components change.

**What does it beat?** It captures **animated GIFs of interactive behavior**, not just static screenshots — the only way to show a drag-and-drop board or a countdown in documentation.

**Terminology:** **Headless browser** — a real browser with no visible window, driven by code.

**The hard part, drawn:**
```
  ShowcaseTool ─▶ Playwright (headless Chromium) ─▶ visit each /showcase/* page
        ├─ full-page screenshot
        └─ record interaction → frames ─▶ Magick.NET assembles GIF + thumbnails ─▶ showcase-<date>/ folder
```

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
