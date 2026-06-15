# SelectFile

> Fully-controlled file-picker tile grid. Caller owns the file list and selection callback.

## What this component does
Renders a horizontal flex grid of file tiles. Each tile shows a thumbnail (inline base64 image, remote URL, or FontAwesome icon for non-images), the filename, and a tooltip with the human-readable byte size. Clicking a tile fires `OnFileSelected`. Optional Cancel and Refresh buttons sit above the grid; a `Loading` flag swaps the grid for a spinner.

The original FreeExamples version injected the host's `BlazorDataModel` + `Radzen.DialogService` and fetched files itself. This refactor is fully controlled — no DI, no Radzen, no host `Helpers`.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `SelectFile.razor` | Component markup + code-behind | ~145 |
| `FileItem.cs` | Shared `FileItem` POCO (also used by `RenderFiles`) | ~42 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 utility classes
- **Icons:** FontAwesome 6 (`fa-solid`)
- **Cross-feature dependencies:** none
- **JS:** none

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/SelectFile/` folder.
2. Add `@using FreeBlazorExtended.SelectFile` to your `_Imports.razor`.
3. Ensure Bootstrap 5 and FontAwesome 6 are loaded in your host.

## Public API
| Member | Type | Default | Description |
|---|---|---|---|
| `Files` | `List<FileItem>` | `new()` | Files to display |
| `OnFileSelected` | `EventCallback<FileItem>` | — | Fires when a tile is clicked |
| `ShowCancelButton` | `bool` | `false` | Show top-bar Cancel button |
| `CancelButtonText` | `string` | `"Cancel"` | Cancel button label |
| `OnCancel` | `EventCallback` | — | Fires when Cancel is clicked |
| `ShowRefreshButton` | `bool` | `false` | Show top-bar Refresh button |
| `RefreshButtonText` | `string` | `"Refresh"` | Refresh button label |
| `OnRefresh` | `EventCallback` | — | Fires when Refresh is clicked |
| `Loading` | `bool` | `false` | When true, render a spinner instead of the grid |
| `EmptyText` | `string` | `"No files available."` | Shown when `Files` is empty |

`FileItem` (shared with `RenderFiles`): `FileId`, `FileName`, `Extension` (e.g. `".png"`), `Bytes`, `Value?` (inline image bytes), `RemoteImageUrl?`, `ThumbnailIcon?` (FA class for non-image files).

## Usage
```razor
@using FreeBlazorExtended.SelectFile

<SelectFile Files="_files"
            ShowRefreshButton="true"
            OnRefresh="LoadFiles"
            OnFileSelected="@(f => Selected = f)" />

@code {
    private List<FileItem> _files = new();
    private FileItem? Selected;

    private async Task LoadFiles() { _files = await Api.GetFilesAsync(); }
}
```

## Status
- Implementation: **REAL** — refactored from FreeExamples `Shared/SelectFile.razor`
- Removed couplings: `BlazorDataModel`, `Radzen.DialogService`, `Helpers.*`, `<Language>`
- Known gaps: no built-in dialog wrapper — caller decides how to present the picker (inline, in their own modal, etc.). The `OnCancel` callback replaces the original `DialogService.Close()` call.

## Effort to integrate
**S** — single `.razor` + a small POCO, no JS, no DI.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A file-*picker* tile grid: shows files as tiles (thumbnail/icon + name + size), and clicking one fires `OnFileSelected`. Optional Cancel/Refresh buttons and a `Loading` spinner. Fully controlled — you own the file list and the selection callback.

**What tech & where?** [SelectFile.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/SelectFile/SelectFile.razor) (the picker) · [FileItem.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/SelectFile/FileItem.cs) (the shared file shape, also used by RenderFiles).

**Why does this exist?** To let a user pick from a set of existing files (vs. `RenderFiles`, which *displays* them) without coupling to a dialog framework.

**What does it beat?** It's **fully controlled and dependency-free** — the original needed host state + Radzen's dialog service; this one has no DI, no Radzen, and lets *you* decide how to present it (inline, your own modal, etc.).

**Terminology:** **Controlled** — caller owns `Files` and reacts to `OnFileSelected`.

**The hard part, drawn:**
```
  you give: Files (List<FileItem>) ─▶ tile grid ─▶ click a tile ─▶ OnFileSelected(file)
        Loading=true ─▶ spinner     optional Cancel/Refresh buttons ─▶ OnCancel/OnRefresh
```
