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
