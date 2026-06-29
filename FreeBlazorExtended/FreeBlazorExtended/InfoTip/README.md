# InfoTip

> Inline (i) icon that opens a small explainer popover on click — title, description, optional code snippet.

## What this component does
A lightweight popover/tooltip alternative for educational hints. Click the (i) icon → see a card with a title, description, and optional code block. Auto-aligns left or right based on viewport position.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `InfoTip.razor` | The component | 72 |
| `InfoTip.razor.js` | JS helper for viewport-relative alignment | 5 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 utility classes + FontAwesome 6 icons
- **Cross-feature dependencies:** none
- **SignalR:** not used
- **EF Core:** not used

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/InfoTip/` folder.
2. Add `@using FreeBlazorExtended.InfoTip` to your `_Imports.razor`.
3. Ensure Bootstrap 5 and FontAwesome are loaded.

## Usage
```razor
<InfoTip Title="Why this is here"
         Description="Click the icon to see an explanation. The popover auto-positions to stay on-screen."
         Code="@("<MyComponent Param=\"Value\" />")" />

@* Inline next to a label: *@
<strong>Upload Files</strong> <InfoTip Title="..." Description="..." />
```

## Status
- Implementation: **REAL** — direct port from FreeExamples
- Known gaps: none

## Effort to integrate
**S** — single component + 5-line JS module.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** An inline ⓘ icon you place next to a label; click it and a small card pops up with a title, description, and optional code snippet. A 5-line JS helper nudges the popover left or right so it stays on-screen.

**What tech & where?** [InfoTip.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/InfoTip/InfoTip.razor) (the popover) · [InfoTip.razor.js](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/InfoTip/InfoTip.razor.js) (viewport alignment).

**Why does this exist?** For lightweight, inline "what's this?" hints — explanatory without cluttering the page.

**What does it beat?** Unlike a bare tooltip, it shows a **structured card with an optional code block**, and auto-positions to avoid clipping at screen edges.

**Terminology:** **Popover** — a small floating card anchored to the icon (richer than a one-line tooltip).

**The hard part, drawn:**
```
  click ⓘ ─▶ popover card (title · description · optional code) ─▶ JS aligns left/right to stay on-screen
```
