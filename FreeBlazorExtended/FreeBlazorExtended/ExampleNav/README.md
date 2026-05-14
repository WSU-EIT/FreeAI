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
