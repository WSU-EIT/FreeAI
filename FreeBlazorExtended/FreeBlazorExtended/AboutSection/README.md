# AboutSection

> Collapsible "About this page" card with a flexible `ChildContent` slot.

## What this component does
A simpler, more flexible alternative to a structured About box. Click the card header to expand/collapse. Caller fills the body with any markup via `ChildContent` — typically a row of column cards explaining the page's purpose, audience, or key features.

This is the FreeGLBA-pattern About card extracted as a reusable component.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `AboutSection.razor` | The component | 49 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 + FontAwesome 6
- **Cross-feature dependencies:** none

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/AboutSection/` folder.
2. Add `@using FreeBlazorExtended.AboutSection` to your `_Imports.razor`.

## Usage
```razor
<AboutSection Title="What are Source Systems?"
              Subtitle="Applications that send access data"
              StartExpanded="false">
    <div class="row g-3">
        <div class="col-md-4">
            <h6>Examples</h6>
            <ul class="small mb-0"><li>Banner</li><li>PeopleSoft</li></ul>
        </div>
        <div class="col-md-4">
            <h6>Why register one?</h6>
            <p class="small mb-0">Each source system gets its own API key…</p>
        </div>
    </div>
</AboutSection>
```

## Comparison to ShowcaseAbout
This repo also has `Shared/ShowcaseAbout.razor` in the FreeBlazorExample showcase pages, which has structured parameters (What/Who/When/Why/Tech/Quick Facts). Use:
- **`AboutSection`** when you want flexibility (any markup in the body).
- **`ShowcaseAbout`** when you want a guided, consistent template across many pages.

## Status
- Implementation: **REAL** — direct port from FreeExamples
- Known gaps: none

## Effort to integrate
**S** — single ~50-line file, no JS, no external deps.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A collapsible "About this page" card: click the header to expand/collapse, and fill the body with any markup you like via `ChildContent`. Pure UI, ~50 lines, no dependencies.

**What tech & where?** One file — [AboutSection.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/AboutSection/AboutSection.razor) (Bootstrap 5 + FontAwesome 6 for styling).

**Why does this exist?** So every page can carry a consistent, friendly "what is this?" explainer without re-writing the markup each time.

**What does it beat?** It's *flexible* (any body content) where the sister `ShowcaseAbout` is *structured* (fixed What/Who/Why fields) — pick flexibility here, consistency there.

**Terminology:** **`ChildContent`** — the caller-supplied body markup Blazor drops inside the card.

**The hard part, drawn:**
```
  click header ─▶ toggle expand/collapse ─▶ renders YOUR ChildContent (any markup)
```
