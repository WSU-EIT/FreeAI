# RenderFiles

> Inline tile grid for showing attached files (with optional click + delete actions).

## What this component does
Displays a flex grid of file tiles — thumbnail (inline base64, remote URL, or FA icon), filename, and a friendly-size tooltip. When `OnClick` is wired, tiles get a pointer cursor; when `OnDelete` is wired, each tile shows a delete control, optionally guarded by the FreeBlazor library's `<DeleteConfirmation>` component.

The original FreeExamples version injected the host `BlazorDataModel` for image URLs and used host `Helpers` + `<Language>` tags for labels. This refactor is host-agnostic — image source comes from the `FileItem` itself, and labels are passed in as plain strings.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `RenderFiles.razor` | Component markup + code-behind | ~135 |

The `FileItem` type lives in `FreeBlazorExtended/SelectFile/FileItem.cs` (namespace `FreeBlazorExtended.SelectFile`) and is shared with the `SelectFile` component. Add **both** namespaces to your `_Imports.razor` so the type and the component resolve.

## Dependencies
- **NuGet packages:** none directly (transitively `FreeBlazor` for `<DeleteConfirmation>`)
- **CSS:** Bootstrap 5 utility classes
- **Icons:** FontAwesome 6 (`fa-solid`)
- **Cross-feature dependencies:** `FreeBlazorExtended.SelectFile` (for `FileItem`); `FreeBlazor.DeleteConfirmation` (already in the @using chain)
- **JS:** none

## Cherry-pick instructions
1. Copy both `FreeBlazorExtended/RenderFiles/` and `FreeBlazorExtended/SelectFile/FileItem.cs`.
2. Add to your `_Imports.razor`:
   ```razor
   @using FreeBlazorExtended.RenderFiles
   @using FreeBlazorExtended.SelectFile
   ```
3. Ensure Bootstrap 5 and FontAwesome 6 are loaded in your host.

## Public API
| Member | Type | Default | Description |
|---|---|---|---|
| `Files` | `List<FileItem>` | `new()` | Files to render |
| `OnClick` | `EventCallback<FileItem>` | — | Click handler; presence enables clickable styling |
| `OnDelete` | `EventCallback<FileItem>` | — | Delete handler; presence shows a delete control |
| `UseDeleteConfirmation` | `bool` | `true` | Wrap delete in a `<DeleteConfirmation>` guard |
| `DeleteText` | `string` | `"Delete"` | Localized Delete label |
| `CancelText` | `string` | `"Cancel"` | Localized Cancel label (confirmation step) |
| `ConfirmDeleteText` | `string` | `"Confirm"` | Localized Confirm label (confirmation step) |
| `EmptyText` | `string` | `"No files to show."` | Shown when `Files` is empty |

## Usage
```razor
@using FreeBlazorExtended.RenderFiles
@using FreeBlazorExtended.SelectFile

<RenderFiles Files="_attachments"
             OnClick="OpenFile"
             OnDelete="DeleteFile" />

@code {
    private List<FileItem> _attachments = new();
    private Task OpenFile(FileItem f)   { /* navigate / preview */ return Task.CompletedTask; }
    private Task DeleteFile(FileItem f) { _attachments.Remove(f); return Task.CompletedTask; }
}
```

## Status
- Implementation: **REAL** — refactored from FreeExamples `Shared/RenderFiles.razor`
- Removed couplings: `BlazorDataModel`, `Helpers.*`, `<Language>`
- Retained dependency: `<DeleteConfirmation>` from the FreeBlazor library (allowed)
- Known gaps: visual hover effect is minimal (cursor only) — pile on additional CSS in the host if you want a stronger signal.

## Effort to integrate
**S** — single `.razor` plus a shared POCO from the SelectFile feature.
