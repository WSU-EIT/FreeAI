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

## License

Released under the [MIT License](https://opensource.org/licenses/MIT).

## About

Designed, written, and implemented by **Washington State University - Enrollment Information Technology (WSU-EIT)**.

- Website: https://em.wsu.edu/eit/
- GitHub: https://github.com/WSU-EIT
