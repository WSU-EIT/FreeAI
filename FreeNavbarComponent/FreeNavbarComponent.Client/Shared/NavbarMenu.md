# NavbarMenu

A multi-level nested navbar menu component built entirely from Bootstrap classes,
modelled on the same Blazor/JS-module pattern as `Highcharts.razor`.

Bootstrap has no multi-level dropdown, so submenus are nested `.accordion` blocks
inside one ordinary `.dropdown-menu`. Bootstrap's collapse data API opens each
branch, writes `aria-expanded` back to the button and rotates the chevron, so the
menu itself needs no JavaScript. The companion `NavbarMenu.razor.js` module only
supplies the optional search filter.

`bootstrap.bundle.min.js` is already loaded globally by `App.razor`, so unlike the
Highcharts component there are no external resources to load.

## Usage

```razor
<NavbarMenu BrandText="Tidewater"
            BrandColor="#a60f2d"
            MenuText="Browse catalog"
            MenuHeader="Product catalog"
            Items="_catalog"
            LeadingItems="_leading"
            TrailingItems="_trailing"
            OnItemClicked="ItemClicked"
            OnFilter="FilterChanged" />

@code {
    protected List<NavbarMenu.NavbarMenuItem> _leading = new() {
        new() { Text = "Overview", Url = "/", Active = true }
    };

    protected List<NavbarMenu.NavbarMenuItem> _trailing = new() {
        new() { Text = "Alerts", Disabled = true }
    };

    protected List<NavbarMenu.NavbarMenuItem> _catalog = new() {
        new() {
            Text = "Observations",
            Children = new() {
                new() {
                    Text = "Surface",
                    Children = new() {
                        new() {
                            Text = "Land stations",
                            Children = new() {
                                new() {
                                    Text = "METAR",
                                    Children = new() {
                                        new() { Text = "12Z cycle", Url = "/catalog/metar/12z" },
                                        new() { Text = "18Z cycle", Url = "/catalog/metar/18z" }
                                    }
                                },
                                new() { Text = "Mesonet", Url = "/catalog/mesonet" }
                            }
                        },
                        new() { Text = "Marine buoys", Url = "/catalog/buoys" }
                    }
                },
                new() { Text = "Upper air", Url = "/catalog/upper-air" }
            }
        },
        new() {
            Text = "Water",
            Children = new() {
                new() { Text = "Tide predictions", Url = "/catalog/tides" },
                new() { Text = "Stream gauges", Url = "/catalog/gauges" }
            }
        }
    };

    protected void ItemClicked(NavbarMenu.NavbarMenuItem item)
    {
        Console.WriteLine("Clicked " + item.Text);
    }

    protected void FilterChanged(NavbarMenu.NavbarFilterResult result)
    {
        Console.WriteLine(result.Matches + " match \"" + result.Query + "\"");
    }
}
```

## Parameters

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `BrandColor` | `string?` | – | Navbar background. Emitted as an inline style; Bootstrap has no arbitrary background utility. |
| `BrandText` / `BrandUrl` | `string?` | – | Brand link. Omit `BrandText` to hide it. |
| `ChildContent` | `RenderFragment?` | – | Rendered at the end of the navbar container. |
| `ContainerCssClass` | `string` | `container` | Use `container-fluid` for a full-width bar. |
| `ElementId` | `string?` | generated | Stable id prefix. Supply when you need predictable ids. |
| `Items` | `List<NavbarMenuItem>?` | – | The nested tree. Any depth. |
| `LeadingItems` / `TrailingItems` | `List<NavbarMenuItem>?` | – | Plain nav links before/after the dropdown. |
| `MenuCssClass` / `MenuStyle` | `string?` / `string` | `max-height: 70vh; min-width: 20rem;` | The panel is `.overflow-auto`, so the max-height is what makes a long tree scroll. |
| `MenuHeader` | `string?` | – | Optional `.dropdown-header`. |
| `MenuText` | `string` | `Browse catalog` | The dropdown toggle label. |
| `NavbarCssClass` / `NavbarTheme` | `string?` / `string` | `dark` | The panel is pinned to `light` so the dark scheme does not cascade in. |
| `OnFilter` | `EventCallback<NavbarFilterResult>` | – | Raised when the filter runs. |
| `OnItemClicked` | `EventCallback<NavbarMenuItem>` | – | Raised when a leaf is clicked. |
| `SearchLabel` / `SearchPlaceholder` | `string` | `Search the catalog` | The label is visually hidden but read by screen readers. |
| `ShowFooterCount` | `bool` | `true` | The `aria-live` count line beneath the tree. |
| `ShowSearch` | `bool` | `true` | Set false to drop the search box and its JS wiring. |

### NavbarMenuItem

`Text`, `Url`, `Target`, `Active`, `Disabled`, `Children`, and `Tag` — an arbitrary
value carried through to `OnItemClicked` so the parent can identify an item without
matching on display text. An item is a branch when it has children, otherwise a leaf.

## Behaviour notes

- Items with a `Url` navigate normally and still raise `OnItemClicked`; items
  without one only raise the event.
- Within a branch, nested branches render above that level's own leaves.
- Heading levels track depth (`h2`..`h6`, capped) so the menu keeps a sane
  document outline at any depth.
- `Reset()` clears the search box and collapses every branch.
- Searching expands every surviving branch; a deep hit keeps its ancestors alive.
  Clearing the box restores the full tree, collapsed.
