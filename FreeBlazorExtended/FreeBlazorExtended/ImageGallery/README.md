# ImageGallery

> Responsive thumbnail grid with full-screen lightbox overlay (prev/next + keyboard nav).

## What this component does
Renders a responsive Bootstrap grid of images. Clicking a thumbnail opens a full-screen dark overlay showing the image at full size with prev/next arrows, a close button, an "N of M" counter, and a thumbnail strip. Esc closes; ArrowLeft / ArrowRight navigate. Click the backdrop to dismiss. No JS sidecar.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `ImageGallery.razor` | Component + nested `GalleryImage` type | ~165 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 utility classes + FontAwesome 6 icons
- **Cross-feature dependencies:** none
- **JS:** none (keyboard handled with `@onkeydown` on the overlay)

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/ImageGallery/` folder.
2. Add `@using FreeBlazorExtended.ImageGallery` to your `_Imports.razor`.
3. Ensure Bootstrap 5 and FontAwesome 6 are loaded in your host.

## Public API
| Member | Type | Default | Description |
|---|---|---|---|
| `Images` | `List<GalleryImage>` | `new()` | Images to display |
| `ColumnCount` | `int` | `3` | Columns at `md+` breakpoint (mobile is always 1, sm is 2) |
| `EnableLightbox` | `bool` | `true` | Show overlay on thumbnail click |
| `ThumbnailClass` | `string` | `""` | Extra classes for thumbnail grid wrapper |
| `OnImageClick` | `EventCallback<GalleryImage>` | — | Optional override; fires on thumbnail click |

`GalleryImage`: `Id`, `Url` (full size), `ThumbnailUrl?` (falls back to `Url`), `Alt?`, `Caption?`, `Category?`.

## Usage
```razor
@using FreeBlazorExtended.ImageGallery

<ImageGallery Images="_pics" ColumnCount="4" />

@code {
    private List<ImageGallery.GalleryImage> _pics = new() {
        new() { Id="1", Url="/img/dog.jpg",   ThumbnailUrl="/img/dog-thumb.jpg",   Caption="Buddy",  Category="Dogs"  },
        new() { Id="2", Url="/img/cat.jpg",   Caption="Whiskers",                   Category="Cats"  },
        new() { Id="3", Url="/img/horse.jpg", Caption="Jet",                        Category="Horses"},
    };
}
```

To handle clicks yourself (e.g. route to a detail page) and skip the lightbox:
```razor
<ImageGallery Images="_pics" EnableLightbox="false"
              OnImageClick="@(img => Nav.NavigateTo($"/photo/{img.Id}"))" />
```

## Status
- Implementation: **REAL** — extracted from FreeExamples ImageGallery demo
- Known gaps: no pinch-zoom on mobile; no preloading of next image

## Effort to integrate
**S** — single self-contained `.razor` file, no JS, no DI.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A responsive thumbnail grid; clicking a thumbnail opens a full-screen dark lightbox with prev/next arrows, an "N of M" counter, and a thumbnail strip. Esc closes, arrow keys navigate — all without any JavaScript.

**What tech & where?** One file — [ImageGallery.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/ImageGallery/ImageGallery.razor) (Bootstrap 5 grid + FontAwesome icons).

**Why does this exist?** A photo grid with a lightbox, without taking a dependency on a JS lightbox library.

**What does it beat?** Keyboard navigation is handled with Blazor's `@onkeydown` (**no JS**), and you can set `EnableLightbox="false"` + `OnImageClick` to route to your own detail page instead. *(Honest: no pinch-zoom, no next-image preloading.)*

**Terminology:** **Lightbox** — the full-screen overlay that shows one image at a time.

**The hard part, drawn:**
```
  thumbnail grid ─▶ click ─▶ lightbox overlay (full size) ─▶ ←/→ navigate · Esc close · backdrop dismiss
```
