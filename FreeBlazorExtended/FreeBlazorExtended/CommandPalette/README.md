# CommandPalette

> Modal command palette with fuzzy search, category grouping, keyboard navigation, and a global Ctrl/Cmd+K hotkey — the VS Code / Spotlight / Raycast pattern as a reusable Blazor component.

## What this component does
Renders a centered overlay dialog with a search input. The caller supplies a flat list of `CommandItem`s; the component filters by Label/Description/Category, groups results by category, supports arrow-key navigation + Enter to select, and remembers the last few invoked commands. A JS sidecar listens for a global hotkey (default Ctrl+K / Cmd+K) so the palette is always one keystroke away.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `CommandPalette.razor` | The component | 274 |
| `CommandPalette.razor.js` | Global keydown listener (Ctrl/Cmd+hotkey) | 35 |

## Dependencies
- **NuGet packages:** none beyond `Microsoft.AspNetCore.Components.Web`
- **CSS framework:** Bootstrap 5 utility classes (`position-fixed`, `bg-white`, `input-group`, etc.)
- **Icon font:** Font Awesome 6 (`fa-solid fa-magnifying-glass`, etc.) — used for default item icon and the search glyph
- **JS libraries:** none — the sidecar is plain DOM `addEventListener`
- **SignalR / EF Core:** not used

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/CommandPalette/` folder into your project.
2. Add `@using FreeBlazorExtended.CommandPalette` to `_Imports.razor`.
3. Make sure Bootstrap 5 + Font Awesome 6 are loaded by the host page.

## Usage
```razor
<CommandPalette @ref="_palette"
                Commands="_commands"
                OnCommandInvoked="HandleCommand"
                TriggerHotkey="k"
                PlaceholderText="What do you want to do?" />

@code {
    private CommandPalette _palette = null!;

    private List<CommandPalette.CommandItem> _commands = new() {
        new() { Id="new",    Label="New File",      Category="File",   Icon="fa-solid fa-file-circle-plus" },
        new() { Id="open",   Label="Open File...",  Category="File",   Icon="fa-solid fa-folder-open" },
        new() { Id="save",   Label="Save",          Category="File",   Icon="fa-solid fa-floppy-disk" },
        new() { Id="theme",  Label="Toggle Theme",  Category="View",   Icon="fa-solid fa-circle-half-stroke" },
        new() { Id="logout", Label="Sign Out",      Category="Account" },
    };

    private Task HandleCommand(CommandPalette.CommandItem item) {
        // Route to whatever the command should do
        Console.WriteLine($"Picked: {item.Id}");
        return Task.CompletedTask;
    }
}
```
Call `_palette.Open()` / `_palette.Close()` to drive it programmatically (e.g. from a toolbar button).

## Status
- Implementation: **REAL** — extracted from FreeExamples `FreeExamples.App.Pages.CommandPalette.razor`
- Generic over caller-supplied `CommandItem` (no hardcoded sample data)
- Internal recent-command history (`HistorySize`, default 5)

## Known gaps
- **Mac vs Windows hotkey:** the JS listener accepts either Ctrl *or* Cmd as the modifier, so `Ctrl+K` works on Windows/Linux and `Cmd+K` works on macOS without configuration. If you need to distinguish the two (e.g. Cmd-only on Mac), edit `CommandPalette.razor.js`.
- **No fuzzy ranking** — substring match only. Swap the `OnSearchInput` LINQ for a fuzzy scorer if needed.
- **History is in-memory** — cleared on page reload. Persist via `localStorage` or a `UserPreferences` service if you need it sticky.
- **Single instance per page assumed** for the global hotkey; multiple `CommandPalette`s on one page will both listen.
- **No `[Inject]`-based service registration** — caller wires `OnCommandInvoked` to do the routing.

## Effort to integrate
**S** — drop the folder in, supply a `List<CommandItem>`, wire `OnCommandInvoked`.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** The VS Code / Spotlight "Ctrl+K to do anything" palette. You hand it a list of `CommandItem`s; it shows a search overlay that filters and groups them by category, supports arrow-key navigation + Enter, and remembers recent picks. A tiny JS file wires the global hotkey so the palette is always one keystroke away.

**What tech & where?** [CommandPalette.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/CommandPalette/CommandPalette.razor) (the UI) · [CommandPalette.razor.js](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/CommandPalette/CommandPalette.razor.js) (35-line global keydown listener).

**Why does this exist?** To give any app a universal "what do you want to do?" launcher — reusable and generic over the caller's own commands.

**What does it beat?** The caller owns routing (`OnCommandInvoked`), and the hotkey works as **Ctrl on Windows/Linux and Cmd on macOS** with no config. *(Honest: substring match, not fuzzy ranking; recent-history is in-memory.)*

**Terminology:** **`CommandItem`** — one entry (label, category, icon, id) you supply to the palette.

**The hard part, drawn:**
```
  Ctrl/Cmd+K (JS sidecar) ─▶ open overlay ─▶ filter+group your CommandItems ─▶ arrows+Enter ─▶ OnCommandInvoked(item)
                                                                                  └─ you route it
```
