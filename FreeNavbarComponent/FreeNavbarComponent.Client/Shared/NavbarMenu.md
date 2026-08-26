# NavbarMenu

A multi-level nested navbar menu built entirely from Bootstrap classes. Follows the
FreeBlazor component conventions: flat parameters, `Value`/`ValueChanged`,
`Delegate?` callbacks, `Id` and `Class`.

Bootstrap has no multi-level dropdown, so submenus are nested `.accordion` blocks
inside one ordinary `.dropdown-menu`. Bootstrap's collapse data API opens each
branch, writes `aria-expanded` back to the button and rotates the chevron, so the
menu itself needs no JavaScript. The companion `NavbarMenu.razor.js` module only
supplies the optional search filter.

`bootstrap.bundle.min.js` is already loaded globally by `App.razor`, so unlike the
Highcharts component there are no external resources to load.

## Simplest usage

Pass a list of items. That is the whole requirement.

```razor
<NavbarMenu Value="_menu" MenuText="Products" />

@code {
    protected List<NavbarMenuItem> _menu = new() {
        new("Fruit", new NavbarMenuItem("Apples", "/apples"),
                     new NavbarMenuItem("Pears", "/pears")),
        new("Vegetables", new NavbarMenuItem("Carrots", "/carrots"))
    };
}
```

The constructors keep nesting readable: `new(text, url)` makes a link,
`new(text, ...children)` makes a branch. Static helpers read the same way if you
prefer them â€” `NavbarMenuItem.Link("Apples", "/apples")` and
`NavbarMenuItem.Branch("Fruit", child1, child2)`.

Object-initialiser style also works when you need the extra properties:

```razor
protected List<NavbarMenuItem> _menu = new() {
    new() {
        Text = "Fruit",
        Icon = "bi bi-basket",
        Children = new() {
            new() { Text = "Apples", Url = "/apples" },
            new() { Text = "Pears", Tag = 42 }
        }
    }
};
```

## Fuller example

```razor
<NavbarMenu BrandText="Tidewater"
            BrandColor="#a60f2d"
            MenuText="Browse catalog"
            MenuHeader="Product catalog"
            Value="_catalog"
            LeadingItems="_leading"
            TrailingItems="_trailing"
            OnItemClicked="ItemClicked"
            OnFilter="FilterChanged" />

@code {
    protected List<NavbarMenuItem> _leading = new() {
        new() { Text = "Overview", Url = "/", Active = true }
    };

    protected List<NavbarMenuItem> _trailing = new() {
        new() { Text = "Alerts", Disabled = true }
    };

    protected List<NavbarMenuItem> _catalog = new() {
        new("Observations",
            new NavbarMenuItem("Surface",
                new NavbarMenuItem("Land stations",
                    new NavbarMenuItem("METAR",
                        new NavbarMenuItem("12Z cycle", "/catalog/metar/12z"),
                        new NavbarMenuItem("18Z cycle", "/catalog/metar/18z")),
                    new NavbarMenuItem("Mesonet", "/catalog/mesonet")),
                new NavbarMenuItem("Marine buoys", "/catalog/buoys")),
            new NavbarMenuItem("Upper air", "/catalog/upper-air")),
        new("Water",
            new NavbarMenuItem("Tide predictions", "/catalog/tides"),
            new NavbarMenuItem("Stream gauges", "/catalog/gauges"))
    };

    protected void ItemClicked(NavbarMenuItem item)
    {
        Console.WriteLine("Clicked " + item.Text);
    }

    protected void FilterChanged(NavbarFilterResult result)
    {
        Console.WriteLine(result.Matches + " match \"" + result.Query + "\"");
    }
}
```

## Parameters

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `Value` | `List<NavbarMenuItem>` | empty | The nested menu tree. Any depth. |
| `ValueChanged` | `EventCallback<List<NavbarMenuItem>>` | â€“ | Supports `@bind-Value`. |
| `OnValueChanged` | `Delegate?` | â€“ | Raised when the tree changes. |
| `OnItemClicked` | `Delegate?` | â€“ | Receives the clicked `NavbarMenuItem`. |
| `OnFilter` | `Delegate?` | â€“ | Receives a `NavbarFilterResult`. |
| `MenuText` | `string` | `Menu` | The dropdown toggle label. |
| `MenuHeader` | `string?` | â€“ | Optional `.dropdown-header`. |
| `AutoAlign` | `bool` | `true` | Measures the panel on each open and flips its alignment to whichever side has room, so a menu near either screen edge never renders off-screen. Ignored when `AlignEnd` is set. |
| `AlignEnd` | `bool` | `false` | Pins the panel right-aligned to its toggle, bypassing auto detection for fixed layouts. |
| `AlignLinksEnd` | `bool` | `false` | Places the nav links at the navbar's right edge without pinning the panel; AutoAlign picks the panel side. Implied by `AlignEnd`. |
| `DropUp` | `bool` | `false` | Opens the panel upward (Bootstrap `dropup`) and flips the toggle's caret. Use for a navbar at the bottom of the screen or page, where a downward panel would run off the bottom edge. Combines with `AlignLinksEnd`/`AutoAlign` for the bottom-right corner. |
| `MenuClass` / `MenuStyle` | `string?` / `string` | `max-height: 70vh; min-width: 20rem; max-width: min(92vw, 40rem);` | The panel is `.overflow-auto`, so max-height makes a long tree scroll and max-width stops deep indentation widening it past the viewport. |
| `BrandText` / `BrandUrl` | `string?` | â€“ | Brand link. Omit `BrandText` to hide it. |
| `BrandEnd` | `bool` | `false` | Renders the brand after the nav links, so the menu toggle sits at the navbar's leading edge (used by the sample's left-corner demos). |
| `BrandColor` | `string?` | â€“ | Navbar background; emitted inline, as Bootstrap has no arbitrary background utility. |
| `LeadingItems` / `TrailingItems` | `List<NavbarMenuItem>?` | â€“ | Plain nav links before/after the dropdown. |
| `ShowSearch` | `bool` | `true` | Set false to drop the search box and its JS wiring. |
| `SearchLabel` / `SearchPlaceholderText` | `string` | `Search` | The label is visually hidden but read by screen readers. |
| `ShowFooterCount` | `bool` | `true` | The `aria-live` count line beneath the tree. |
| `Class` | `string?` | â€“ | Extra classes for the navbar element. |
| `ContainerClass` | `string` | `container` | Use `container-fluid` for a full-width bar. |
| `Sticky` | `bool` | `true` | Pins the navbar to the viewport top (Bootstrap `sticky-top`). Set false for navbars embedded in page content, especially multiple on one page — sticky's per-navbar stacking context would paint a later navbar over an earlier one's open panel. |
| `Theme` | `string` | `dark` | Navbar colour scheme. The panel is pinned to `light` so it does not cascade in. Dark navbars raise Bootstrap's 55%-white link colour to 90% so links meet WCAG AA contrast on brand backgrounds. |
| `AriaLabel` | `string?` | BrandText, then MenuText | Accessible name for the `<nav>` landmark so screen readers can tell multiple navbars apart. |
| `Id` | `string` | generated | Stable id prefix. Supply when you need predictable ids. |
| `ChildContent` | `RenderFragment?` | â€“ | Rendered at the end of the navbar container. |

### NavbarMenuItem

`Text`, `Url`, `Icon`, `Target`, `Active`, `Disabled`, `Children`, and `Tag` â€” an
arbitrary value carried through to `OnItemClicked` so the parent can identify an
item without matching on display text. An item is a branch when it has children,
otherwise a leaf.

`Icon` accepts a CSS class (`"bi bi-folder"`, `"fa-solid fa-folder"`) or raw
markup; a class name is wrapped in an `<i>` element automatically.

## Methods

`Reset()` clears the search box and collapses every branch.

## Fitting on screen

**Vertically it is automatic.** The panel is `max-height: 70vh` and
`.overflow-auto`, so a long tree scrolls inside the panel instead of running off
the bottom of the window.

**Horizontally it is automatic too.** Bootstrap positions navbar dropdowns
*statically*, which means Popper's collision detection never runs â€” left to
Bootstrap, a left-anchored menu near the right edge hangs off the window and
forces the page to scroll sideways (measured at a 1000px viewport: 182px
off-screen; `data-bs-display="dynamic"` measurably does nothing inside a navbar).
The component compensates itself: `AutoAlign` (on by default) measures the
painted panel each time it opens and applies `.dropdown-menu-end` when the
default anchoring would overflow, preferring whichever side has enough room.

To pin the alignment instead of detecting it, set `AlignEnd="true"`:

```razor
<NavbarMenu Value="_menu" AlignEnd="true" />
```

**Bottom navbars open upward.** Set `DropUp="true"` for a navbar at the bottom
of the screen so the panel opens above the toggle instead of running off the
bottom edge. All four corners are exercised on the sample page's "Four corners"
demo and asserted on-screen by the Playwright suite: top-left (default),
top-right (`AlignLinksEnd`, AutoAlign flips the panel left), bottom-left
(`DropUp`), and bottom-right (`DropUp` + `AlignLinksEnd`).

Width is capped at `min(92vw, 40rem)` by default, so a deeply indented tree cannot
grow the panel wider than the viewport. Each nesting level adds ~16px of indent
(`ps-3`), so at 20 levels you are ~320px in; widen the panel via `MenuStyle` if
your labels need the room.

**Phones are handled automatically.** Below 576px the component switches to
Bootstrap's mega-menu pattern: the nav links wrap instead of overflowing, and the
panel stops anchoring to its toggle and spans the viewport with a small inset,
height-capped to `min(70vh, 100dvh - 7rem)`. No parameter needed â€” `AlignEnd`
becomes irrelevant at these widths because the panel is full-width either way.
Verified by touch-driven Playwright runs at 390px, 360px, and 320px (27/27).
Note this navbar uses `navbar-expand` â€” it never collapses into a hamburger; on
phones it wraps and goes full-width instead.

## Behaviour notes

- Items with a `Url` navigate normally and still raise `OnItemClicked`; items
  without one only raise the event.
- Within a branch, nested branches render above that level's own leaves.
- Heading levels track depth (`h2`..`h6`, capped) so the menu keeps a sane
  document outline at any depth.
- Searching expands every surviving branch; a deep hit keeps its ancestors alive.
  Clearing the box restores the full tree, collapsed.

## Verification

See [docs/evidence](../../docs/evidence/) â€” 17/17 Playwright assertions against the
running app, with screenshots and an animated GIF.


