# ExampleNav

> Breadcrumb + Prev / All / Next navigation strip for stepping through an ordered list of pages.

## What this component does
Renders a single Bootstrap bar with breadcrumbs on the left (Home -> Category -> [Parent ->] Title) and Prev / All / Next buttons on the right. Fully controlled — the caller supplies the full ordered page list and the current route. If the current route doesn't match any page, nothing renders.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `ExampleNav.razor` | The component (markup + `NavPage` nested class) | ~125 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5 utility classes + FontAwesome 6 icons
- **Cross-feature dependencies:** none
- **SignalR:** not used
- **EF Core:** not used

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/ExampleNav/` folder.
2. Add `@using FreeBlazorExtended.ExampleNav` to your `_Imports.razor`.
3. Ensure Bootstrap 5 and FontAwesome are loaded.

## Usage
```razor
@{
    var pages = new List<ExampleNav.NavPage> {
        new() { Category = "Data", Route = "/items",      Title = "Items" },
        new() { Category = "Data", Route = "/items/v1",   Title = "V1: Cards",    ParentTitle = "Items", ParentRoute = "/items" },
        new() { Category = "Data", Route = "/items/v2",   Title = "V2: Timeline", ParentTitle = "Items", ParentRoute = "/items" },
        new() { Category = "Data", Route = "/search",     Title = "Search" },
    };
}

<ExampleNav Pages="pages"
            CurrentRoute="/items/v1"
            HomeUrl="/dashboard"
            HomeLabel="Dashboard"
            HomeIcon="fa-solid fa-grid-2" />
```

The `Route` value is used directly as the `href` — pass relative or absolute URLs as you prefer. Route matching is case-insensitive.

## Status
- Implementation: **REAL** — refactored from `FreeExamples.Client/Shared/ExampleNav.razor` to remove host-app coupling (`BlazorDataModel`, `Helpers.BuildUrl`, hardcoded page list).
- Known gaps: none

## Effort to integrate
**S** — single component, no JS, no services.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A single navigation bar: breadcrumbs on the left (Home → Category → Title) and Prev / All / Next buttons on the right, for stepping through an ordered list of pages. You supply the full page list and the current route; it figures out position. If the route matches no page, it renders nothing.

**What tech & where?** One file — [ExampleNav.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/ExampleNav/ExampleNav.razor) (Bootstrap 5 + FontAwesome).

**Why does this exist?** To give multi-page demos/wizards a consistent "where am I, what's next" strip without hand-coding breadcrumbs on every page.

**What does it beat?** It's **fully controlled and host-agnostic** — the original version was coupled to the host app's state and a hard-coded page list; this one takes everything as parameters, so it drops into any app.

**Terminology:** **Controlled** — the caller owns the page list + current route; the component just renders.

**The hard part, drawn:**
```
  you give: ordered Pages + CurrentRoute ─▶ ExampleNav finds the index ─▶ breadcrumbs + Prev/All/Next
        no match? ─▶ render nothing
```
