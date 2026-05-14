# Carousel

> Single-focus image carousel / slideshow with optional filmstrip, dot indicators, prev/next arrows, and timer-driven auto-play. Pure C# — no JS interop.

## What this component does
Shows one slide at a time with a fixed-height focus image, an optional caption overlay, optional prev/next arrow buttons, an optional horizontal filmstrip of mini-thumbnails for jumping, and optional dot indicators. Auto-play advances slides on a configurable cadence using `System.Timers.Timer` and pauses while the cursor hovers the carousel (configurable). Wraps from last to first when `Loop=true`; when `Loop=false`, auto-play stops at the last slide. Distinct from `ImageGallery` which renders a thumbnail grid + lightbox — use `Carousel` for product-page / hero-slider UX where one image needs the spotlight.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `Carousel.razor` | Component, parameters, public `CarouselSlide` nested class, timer lifecycle | ~290 |

## Dependencies
- **NuGet packages:** `System.Timers` (BCL)
- **Cross-feature dependencies:** none
- **CSS framework:** Bootstrap 5 (`card`, `btn`, `badge`, `position-absolute`, `opacity-*`, etc.)
- **Icons:** Font Awesome (`fa-chevron-left`, `fa-chevron-right`, `fa-circle-info`)
- **JS interop:** none

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/Carousel/` folder into your project.
2. Add `@using FreeBlazorExtended.Carousel` to `_Imports.razor` (or use the fully-qualified tag).
3. Ensure Bootstrap 5 CSS + Font Awesome are referenced from your host page.
4. No DI registration, no `Program.cs` changes, no migrations.

## Usage
```razor
@using FreeBlazorExtended.Carousel

<Carousel Slides="_slides"
          AutoPlay="true"
          AutoPlayInterval="TimeSpan.FromSeconds(4)"
          PauseOnHover="true"
          ShowFilmstrip="true"
          ShowArrows="true"
          ShowDots="false"
          Loop="true"
          OnSlideChanged="HandleSlideChanged" />

@code {
    private List<Carousel.CarouselSlide> _slides = new() {
        new() { Id = "1", ImageUrl = "/img/hero1.jpg", Title = "Mountain", Caption = "Golden hour" },
        new() { Id = "2", ImageUrl = "/img/hero2.jpg", Title = "Ocean",    Caption = "Sunset reflections" },
        new() { Id = "3", ImageUrl = "/img/hero3.jpg", Title = "Forest",   Caption = "Dappled light", LinkUrl = "/products/forest" },
    };

    private void HandleSlideChanged(int index) { /* ... */ }
}
```

### Parameters
| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Slides` | `List<CarouselSlide>` | `[]` | Required — empty list shows a placeholder |
| `InitialSlideIndex` | `int` | `0` | Clamped into range |
| `AutoPlay` | `bool` | `false` | Begin auto-advancing on init |
| `AutoPlayInterval` | `TimeSpan` | `5s` | Cadence for auto-advance |
| `PauseOnHover` | `bool` | `true` | Pause while cursor is over the carousel |
| `ShowFilmstrip` | `bool` | `true` | Horizontal thumbnail row below focus |
| `ShowArrows` | `bool` | `true` | Prev / Next overlay buttons |
| `ShowDots` | `bool` | `false` | Dot indicators (alt to filmstrip) |
| `Loop` | `bool` | `true` | Wrap last↔first; when false, auto-play stops at end |
| `FocusStyle` | `string` | `height:400px; object-fit:cover;` | Inline style on the focus image |
| `OnSlideChanged` | `EventCallback<int>` | — | Fires with new zero-based index |
| `Class` / `AriaLabel` | `string` | `""` / null | Wrapper extras |

### Public methods (via `@ref`)
`Next()`, `Prev()`, `GoTo(int index)`, `StartAutoPlay()`, `StopAutoPlay()`.

## Status
- Implementation: **REAL** — extracted from `FreeExamples/Pages/Examples/FreeExamples.App.Pages.Carousel.razor`
- Disposal: timer is stopped, event detached, and disposed in `Dispose()`; `_disposed` flag guards mid-tick callbacks
- `Loop=false` auto-play correctly halts when the last slide is reached
- Known gaps: no swipe / touch gestures (no JS); no fade/slide CSS transition between images

## Effort to integrate
**M** — one Razor file, one `@using`, no DI, no JS, no migrations.
