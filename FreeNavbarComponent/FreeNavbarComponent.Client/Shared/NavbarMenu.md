# NavbarMenu

A multi-level nested navbar menu built entirely from Bootstrap classes. Follows the
FreeBlazor component conventions: flat parameters, `Value`/`ValueChanged`,
`Delegate?` callbacks, `Id` and `Class`.

Bootstrap has no multi-level dropdown, so submenus are nested `.accordion` blocks
inside one ordinary `.dropdown-menu`. Bootstrap's collapse data API opens each
branch, writes `aria-expanded` back to the button and rotates the chevron, so
opening and closing needs no JavaScript from us. The companion
`NavbarMenu.razor.js` module supplies only the search filter, the screen-edge
guard, and the arrow-key navigation.

## Installation

The component depends on nothing but Bootstrap 5.3 and `IJSRuntime` — no FreeCRM
`Helpers`, no `BlazorDataModel`, no NuGet package. `bootstrap.bundle.min.js` is
already loaded globally by FreeCRM's `App.razor`, so there is nothing to add to
the layout.

1. Copy these four files into your Client project's `Shared` folder:

   | File | What it is |
   | --- | --- |
   | `NavbarMenu.razor` | The component, with its scoped `<style>` block |
   | `NavbarMenu.razor.js` | Search filter, edge guard, keyboard navigation |
   | `NavbarMenuItem.cs` | One entry in the tree |
   | `NavbarFilterResult.cs` | What `OnFilter` receives |

2. Set the namespace in the two `.cs` files to match your Client project. They
   ship as `FreeNavbarComponent.Client.Shared`; in a solution renamed to `CRM`
   it becomes `CRM.Client.Shared`:

   ```powershell
   Get-ChildItem CRM.Client\Shared\Navbar*.cs |
       ForEach-Object {
           (Get-Content $_ -Raw) -replace 'FreeNavbarComponent\.Client\.Shared', 'CRM.Client.Shared' |
               Set-Content $_ -Encoding utf8
       }
   ```

   The `.razor` file needs no edit — Razor derives its namespace from the project
   and folder — and the JS module is found by path, not by namespace.

3. `_Imports.razor` in a stock FreeCRM already has
   `@using <YourRoot>.Client.Shared`, so pages can use `<NavbarMenu>` and
   `NavbarMenuItem` with no further wiring.

That is the whole install. Build and drop `<NavbarMenu Value="_menu" />` on a page.

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
prefer them — `NavbarMenuItem.Link("Apples", "/apples")` and
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
| `ValueChanged` | `EventCallback<List<NavbarMenuItem>>` | – | Supports `@bind-Value`. |
| `OnValueChanged` | `Delegate?` | – | Raised when the tree changes. |
| `OnItemClicked` | `Delegate?` | – | Receives the clicked `NavbarMenuItem`. |
| `OnFilter` | `Delegate?` | – | Receives a `NavbarFilterResult`. |
| `MenuText` | `string` | `Menu` | The dropdown toggle label. |
| `MenuHeader` | `string?` | – | Optional caption at the top of the panel. Rendered as a `div`, not Bootstrap's `h6`, so it does not break the page's heading order. |
| `AutoAlign` | `bool` | `true` | Measures the panel on each open and flips its alignment to whichever side has room, so a menu near either screen edge never renders off-screen. Ignored when `AlignEnd` is set. |
| `AlignEnd` | `bool` | `false` | Pins the panel right-aligned to its toggle, bypassing auto detection for fixed layouts. |
| `AlignLinksEnd` | `bool` | `false` | Places the nav links at the navbar's right edge without pinning the panel; AutoAlign picks the panel side. Implied by `AlignEnd`. |
| `DropUp` | `bool` | `false` | Opens the panel upward (Bootstrap `dropup`) and flips the toggle's caret. Use for a navbar at the bottom of the screen or page, where a downward panel would run off the bottom edge. Combines with `AlignLinksEnd`/`AutoAlign` for the bottom-right corner. |
| `MenuClass` / `MenuStyle` | `string?` / `string` | `max-height: 70vh; min-width: 20rem; max-width: min(92vw, 40rem);` | The panel is `.overflow-auto`, so max-height makes a long tree scroll and max-width stops deep indentation widening it past the viewport. |
| `BrandText` / `BrandUrl` | `string?` | – | Brand link. Omit `BrandText` to hide it. |
| `BrandEnd` | `bool` | `false` | Renders the brand after the nav links, so the menu toggle sits at the navbar's leading edge. |
| `BrandColor` | `string?` | – | Navbar background; emitted inline, as Bootstrap has no arbitrary background utility. |
| `LeadingItems` / `TrailingItems` | `List<NavbarMenuItem>?` | – | Plain nav links before/after the dropdown. |
| `ShowSearch` | `bool` | `true` | Set false to drop the search box and its JS wiring. |
| `SearchLabel` / `SearchPlaceholderText` | `string` | `Search` | The label is visually hidden but read by screen readers. |
| `ShowFooterCount` | `bool` | `true` | The `aria-live` count line beneath the tree. |
| `Class` | `string?` | – | Extra classes for the navbar element. |
| `ContainerClass` | `string` | `container` | Use `container-fluid` for a full-width bar. |
| `Sticky` | `bool` | `true` | Pins the navbar to the viewport top (Bootstrap `sticky-top`). Set false for navbars embedded in page content, especially multiple on one page — sticky's per-navbar stacking context would paint a later navbar over an earlier one's open panel. |
| `Theme` | `string` | `dark` | Navbar colour scheme. The panel is pinned to `light` so it does not cascade in. Dark navbars raise Bootstrap's 55%-white link colour to 90% so links meet WCAG AA contrast on brand backgrounds. |
| `AriaLabel` | `string?` | BrandText, then MenuText | Accessible name for the `<nav>` landmark so screen readers can tell multiple navbars apart. |
| `Id` | `string` | generated | Stable id prefix. Supply when you need predictable ids. |
| `ChildContent` | `RenderFragment?` | – | Rendered at the end of the navbar container. |

### NavbarMenuItem

`Text`, `Url`, `Icon`, `Target`, `Active`, `Disabled`, `Children`, and `Tag` — an
arbitrary value carried through to `OnItemClicked` so the parent can identify an
item without matching on display text. An item is a branch when it has children,
otherwise a leaf.

`Icon` accepts a CSS class (`"bi bi-folder"`, `"fa-solid fa-folder"`) or raw
markup; a class name is wrapped in an `<i>` element automatically.

## Methods

`Reset()` clears the search box and collapses every branch.

## Keyboard

Every row is a real button or link and stays in the tab order, so `Tab` alone
walks the whole panel. On top of that the menu supports the movement keys a
keyboard user expects, so a deep tree does not have to be crossed one `Tab` at
a time:

| Key | Does |
| --- | --- |
| `Down` / `Up` | Previous or next visible row, wrapping at the ends. Works from the search box too. |
| `Home` / `End` | First or last visible row. |
| `Right` | Expands a collapsed branch; on an already open branch, moves into it. |
| `Left` | Collapses an open branch; otherwise moves up to the parent row. |
| `a`–`z`, `0`–`9` | Jumps to the next visible row starting with that character. |
| `Enter` / `Space` | Activates the row. |
| `Escape` | Closes the panel and returns focus to the toggle (Bootstrap's own behaviour). |

Rows inside a collapsed branch, and rows the search box has filtered out, are
skipped rather than focused invisibly.

One implementation note worth knowing if you touch this: the handler has to
capture at `window`. Bootstrap registers its dropdown key handling on
`document` in the *capture* phase and calls `stopPropagation()` for `ArrowUp`
and `ArrowDown`, so a listener anywhere inside the document never sees those
two keys — the symptom is every key working except the two that matter.

## Accessibility

The menu implements the WAI-ARIA **disclosure / accordion** pattern, not the
tree pattern. Each branch row is a `button` carrying `aria-expanded` and
`aria-controls`, wrapped in an element with `role="heading"` and an `aria-level`
that tracks nesting depth. Leaves are ordinary links. That is deliberate, and
the trade-offs are worth knowing before anyone changes them:

- **Depth** is carried by the heading level. Indentation is `ps-3`, which is
  invisible to a screen reader, so without the heading levels the tree would
  read as flat.
- **Group size and position** come from real list markup: every level is one
  `<ul>` holding that level's branches and leaves, and each row is an `<li>`.
  `aria-setsize` and `aria-posinset` cannot be used here — they are not allowed
  on `button` or `link` roles, axe reports them as critical violations, and
  browsers discard them. `role="list"` is spelled out explicitly because
  `list-style: none` makes WebKit drop list semantics.
- **Nothing leaks while closed.** Collapsed branches are `display: none`, so
  they are absent from the accessibility tree rather than merely invisible.
  With the menu shut, none of its rows are exposed and it contributes no
  headings to the page outline.
- **Focus is always visible.** `:focus-visible` paints a ring for keyboard and
  assistive-tech focus while leaving pointer clicks clean. Never replace this
  with `box-shadow: none` on `:focus` — that silently fails WCAG 2.4.7.
- **Why not `role="tree"`.** A tree would add set information automatically,
  but it is a single tab stop with roving `tabindex`, so `Tab` would no longer
  walk the rows. The disclosure pattern keeps both `Tab` and the arrow keys.

A deep menu also makes a **skip link** worth having on the host page, so
keyboard users are not forced through every row before reaching content. That
belongs to the layout rather than to this component.

## Fitting on screen

**Vertically it is automatic.** The panel is `max-height: 70vh` and
`.overflow-auto`, so a long tree scrolls inside the panel instead of running off
the bottom of the window.

**Horizontally it is automatic too.** Bootstrap positions navbar dropdowns
*statically*, which means Popper's collision detection never runs — left to
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
bottom edge. All four corners are exercised and asserted on-screen: top-left
(default), top-right (`AlignLinksEnd`, AutoAlign flips the panel left),
bottom-left (`DropUp`), and bottom-right (`DropUp` + `AlignLinksEnd`).

Width is capped at `min(92vw, 40rem)` by default, so a deeply indented tree cannot
grow the panel wider than the viewport. Each nesting level adds ~16px of indent
(`ps-3`), so at 20 levels you are ~320px in; widen the panel via `MenuStyle` if
your labels need the room.

**Phones are handled automatically.** Below 576px the component switches to
Bootstrap's mega-menu pattern: the nav links wrap instead of overflowing, and the
panel stops anchoring to its toggle and spans the viewport with a small inset,
height-capped to `min(70vh, 100dvh - 7rem)`. No parameter needed — `AlignEnd`
becomes irrelevant at these widths because the panel is full-width either way.
Verified by touch-driven runs at 390px, 360px, and 320px. Note this navbar uses
`navbar-expand` — it never collapses into a hamburger; on phones it wraps and
goes full-width instead.

## Behaviour notes

- Items with a `Url` navigate normally and still raise `OnItemClicked`; items
  without one only raise the event.
- Within a branch, nested branches render above that level's own leaves, and
  both share one list so the announced group size is the number of choices
  actually on offer at that level.
- Heading levels track depth (`h2`..`h6`, capped) so the menu keeps a sane
  document outline at any depth.
- Searching expands every surviving branch; a deep hit keeps its ancestors alive.
  Clearing the box restores the full tree, collapsed.
- The component tolerates being disposed mid-load. A page that redirects while
  the JS module is still importing would otherwise throw
  `ObjectDisposedException`; every interop call goes through a guard instead.

## Verification

Everything is checked against a running app in a real browser, never against
the markup alone: Playwright suites cover behaviour (33 assertions) and
keyboard navigation (27), and four independent accessibility engines run over
the same states — axe-core, the WAVE Evaluation Tool extension,
HTML_CodeSniffer via pa11y, and Lighthouse. The current result is zero findings
from all four, including zero WAVE alerts across 22 page and interaction states.
The WAVE harness is itself calibrated against a page with planted defects, so a
zero from it means something.

Worth remembering when changing this component: a clean build has repeatedly
said nothing about whether the menu works. Clipping, edge overflow, a disposal
race, a stack overflow, a missing focus ring, and the arrow keys silently being
eaten by Bootstrap were all found only by driving a browser.
