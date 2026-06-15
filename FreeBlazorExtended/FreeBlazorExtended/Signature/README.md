# Signature

> Inline signature pad component for capturing handwritten signatures via mouse or touch.

## What this component does
A signature capture control built on the jSignature jQuery plugin. Renders a draw area, captures the signature in base30 format (compact textual representation), and supports two-way binding via `@bind-Value`. Optional clear button included.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `Signature.razor` | The component | 118 |
| `Signature.razor.js` | JS interop (wraps jSignature) | 37 |

## Dependencies
- **NuGet packages:** none directly — relies on the host page including jSignature + jQuery
- **JS libraries (must load before component):** jQuery, [jSignature](https://github.com/willowsystems/jSignature)
  > ⚠️ **Bundle jSignature locally** — do not load it from a CDN. CDN scripts race against Blazor WASM initialization and will cause `$(...).jSignature is not a function` errors. Download `jSignature.min.js` from the [npm package](https://registry.npmjs.org/jsignature/-/jsignature-2.1.3.tgz) and serve it via `@Assets["js/jSignature.min.js"]` alongside the bundled jQuery file.
- **Cross-feature dependencies:** none
- **SignalR:** not used
- **EF Core:** not used

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/Signature/` folder.
2. Add `@using FreeBlazorExtended.Signature` to your `_Imports.razor`.
3. Ensure jQuery and jSignature are loaded in your `_Host.cshtml`/`index.html`.

## Usage
```razor
<Signature @bind-Value="_sig" IncludeClearButton="true" />

@code {
    private string _sig = "";
}
```

The bound value is the base30 string `"image/jsignature;base30,..."`. Persist as-is; render later by re-mounting the component with the same value.

## Status
- Implementation: **REAL** — direct port from FreeExamples
- Persistence: caller's responsibility
- Known gaps: requires jQuery (pre-bundled jSignature dependency)

## Effort to integrate
**S** — drop the folder in, ensure jSignature is loaded, done.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A signature pad: draw with mouse or finger, and it captures the signature as a compact "base30" text string you can two-way bind with `@bind-Value` (persist the string, re-render later by re-mounting with the same value). Built on the jSignature jQuery plugin.

**What tech & where?** [Signature.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Signature/Signature.razor) · [Signature.razor.js](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Signature/Signature.razor.js) (jSignature interop).

**Why does this exist?** To capture handwritten signatures (consent, sign-off) inline in a Blazor form.

**What does it beat?** It stores a **compact text representation** (base30), not a heavy image blob, and binds like any normal form field. *(Gotcha called out in the README: bundle jSignature locally, not from a CDN, or it races Blazor startup.)*

**Terminology:** **base30** — jSignature's small textual encoding of the drawn strokes.

**The hard part, drawn:**
```
  draw (mouse/touch) ─▶ jSignature ─JS interop─▶ base30 string ─▶ @bind-Value (persist as text)
        re-mount with the same value ─▶ signature reappears
```
