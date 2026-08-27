# NavbarMenu

A navbar dropdown menu of `DataObjects.MenuItem` objects with `Children` nested
to any depth, plus an optional search box and keyboard navigation. It renders
the `<li class="nav-item dropdown">` for a Bootstrap `<ul class="navbar-nav">`,
and is what `NavigationMenu.razor` uses for every application menu dropdown
and the Admin menu.

Everything is Razor and C#: the component keeps its own state (open, expanded
branches, search text) so opening, expanding and filtering are ordinary Blazor
renders. Bootstrap's dropdown JavaScript is not used, only its classes. Closing
on an outside click is a backdrop behind the panel. The collocated
`NavbarMenu.razor.js` supplies keyboard navigation and nothing else; the one
CSS rule of our own is the sizing that keeps a nested panel on screen.

## Files

| File | What it is |
| --- | --- |
| `NavbarMenu.razor` | The component |
| `NavbarMenu.razor.js` | Keyboard navigation, one line per key |

There is no new item type. The menu takes the framework's own
`DataObjects.MenuItem` (`Title`, `url`, `Icon`, `TooltipTitle`, `AriaLabel`,
`AppAdminOnly`, `PageNames`, `SortOrder`, `OnClick`, `Children`) exactly as
`Helpers.MenuItems` builds it.

## Usage

```razor
<ul class="navbar-nav">
    <NavbarMenu MenuText="@item.Title" Icon="@item.Icon" Value="@item.Children" ShowSearch="true" />
</ul>
```

An item with `Children` renders as an expandable branch. An item with an
`OnClick` invokes it; otherwise the item links to its `url` and is marked
active when its `PageNames` contain the current view or its `url` is the
current url. `AppAdminOnly` adds the `app-admin-only` class. `Icon` is an
`Icon` component name, as everywhere else in FreeCRM.

A flat list of children renders as plain dropdown items, exactly as the
hand-written markup in `NavigationMenu.razor` did. `NavigationMenu.razor`
turns on `ShowSearch` only for a nested tree, and the component applies its
panel sizing only then, so flat menus are unchanged.

To nest an entry, give a `DataObjects.MenuItem` in `Helpers.MenuItems` or
`Helpers.MenuItemsAdmin` (or a plugin's menu items) a `Children` list. Nothing
else is needed: the stock menus are flat, so out of the box every dropdown
looks as it always has.

## Parameters

| Parameter | Type | Default | Notes |
| --- | --- | --- | --- |
| `Value` | `List<DataObjects.MenuItem>` | empty | The items. Any depth. |
| `MenuText` | `string` | – | Toggle text. |
| `Icon` | `string?` | – | Icon name shown before the toggle text. |
| `ToggleClass` | `string` | `nav-link` | Class on the toggle before `dropdown-toggle`, eg `nav-link active`. |
| `Sort` | `bool` | `true` | Sort every level by `SortOrder` then `Title`. |
| `ShowSearch` | `bool` | `false` | Search box at the top of the panel. Matching rows render with their branches open. |
| `SearchLabel` / `SearchPlaceholder` / `NoMatchesText` | `string` | `Search` / `Search` / `No matches` | Search box text. |
| `AlignEnd` | `bool` | `false` | Right-align the panel (`dropdown-menu-end`) for a menu near the right edge. |
| `DropUp` | `bool` | `false` | Open upward, for a navbar at the bottom of the page. |
| `MenuClass` / `MenuStyle` | `string?` | – | Extra class / inline style for the panel. |
| `Class` | `string?` | – | Extra classes on the nav-item. |
| `Id` | `string` | generated | Stable id prefix for the panel, search box and expanded branches. |

`Reset()` closes the panel, clears the search and collapses every branch.

## Keyboard

Every row is a real button or link in the tab order, so `Tab` alone walks the
whole panel. `NavbarMenu.razor.js` adds:

| Key | Does |
| --- | --- |
| `Down` / `Up` | Next or previous visible row (toggle, search box, rows), wrapping. `Down` on the closed toggle opens the menu. |
| `Home` / `End` | First or last row in the panel. Left alone inside the search box. |
| `Right` | Expands a collapsed branch; on an open branch, moves into it. |
| `Left` | Collapses an open branch; otherwise moves up to the parent branch. Left alone inside the search box. |
| `Enter` / `Space` | Activates the row (native). |
| `Escape` | Closes the panel and returns focus to the toggle. |

The JS does nothing but move focus and click the same buttons a mouse user
would (expanding a branch, closing via the toggle), so the C# side stays the
single owner of state. The listener sits on the menu's own element so it runs
ahead of Bootstrap's document-level dropdown handler, which would otherwise
act on these keys because the panel uses the `.dropdown-menu` class.

## Behaviour and accessibility

- Only one `NavbarMenu` panel is open at a time.
- The outside-click backdrop is `position-fixed … z-n1`: inside the navbar's
  stacking context (FreeCRM's `fixed-top`) it sits behind the navbar's own
  links, which stay clickable, but above the page.
- A branch is a `button.dropdown-item.dropdown-toggle` with `aria-expanded`,
  and `aria-controls` while its list exists; its `<li>` carries `dropup` while
  open, which is how Bootstrap flips the caret. Children render indented
  (`ps-3`) beneath it. Collapsed branches are not in the DOM, so nothing
  hidden is exposed.
- Leaves are links with `aria-current="page"` when active, and carry the
  item's `TooltipTitle` and `AriaLabel`. Every level is a `<ul role="list">`,
  so a screen reader gets group size and position.
- Branch expansion is remembered by position, so a parent that rebuilds
  `Value` every render (as `Helpers.MenuItems` does) keeps open branches open.
- Searching prunes the tree: a row stays when its own title matches (case-
  insensitive "contains"), when a descendant matches (rendered open), or when
  an ancestor matched. Clearing the box restores the full tree, collapsed.
- Clicking a link, clicking an `OnClick` item, clicking outside, opening
  another `NavbarMenu`, or pressing `Escape` all close the panel and reset it.
- In the expanded navbar a nested panel is capped at 70vh (scrolling) and
  between 20rem and `min(92vw, 40rem)` wide. In the collapsed hamburger view
  Bootstrap makes the panel static flow content and the page scrolls instead.
