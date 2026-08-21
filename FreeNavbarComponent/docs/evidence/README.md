# NavbarMenu — Test Evidence

Captured with Playwright (Chromium, headless) against the running Blazor WASM app
at `/NavbarSample`. **17/17 assertions passed, no browser console errors.**

The harness follows the WASM boot discipline used by `FreeTools.BrowserSnapshot`:
navigate with `NetworkIdle`, then wait for a real rendered element (up to 60s for a
cold start) rather than sleeping a fixed amount, because Blazor WASM downloads
assemblies and boots the runtime before any page markup exists.

![Demo](navbar-demo.gif)

## What was verified

| Scenario | Assertions |
| --- | --- |
| WASM boot | Page renders; component's dropdown toggle present |
| Five-level drill | All levels expand; deep leaf visible; 4 branches open; **panel not clipped** |
| Deep search (`12Z`) | 1 leaf survives; all 4 ancestors kept; count reads "1 product matches" |
| Filter callback | `[JSInvokable]` round-trip reaches Blazor ("1 matching for \"12Z\"") |
| Sibling search (`Water`) | 2 leaves, 1 branch; siblings correctly dropped |
| Clear | All 7 leaves restored, every branch collapsed |
| Leaf click | `OnItemClicked` fires with text and `Tag` ("Tide predictions (tides)") |
| Reset | `Reset()` collapses all branches and clears the search box |

## Stills

| File | Shows |
| --- | --- |
| `01-page-loaded.png` | Page after WASM boot |
| `03-five-levels.png` | All five levels expanded and unclipped |
| `04-deep-search.png` | Deep hit keeping its ancestors |
| `05-sibling-search.png` | Sibling filtering |
| `07-leaf-clicked.png` | Click callback reported on the page |
| `08-after-reset.png` | State after `Reset()` |

## Bugs this run caught

Two defects that a green build had not surfaced:

1. **`ObjectDisposedException` on navigation during load.** A page that redirects
   while the component is still importing its JS module disposed the component
   mid-`await`, and the disposed `DotNetObjectReference` was then used. Fixed with
   a `_disposed` guard re-checked after every `await`, plus a `SafeInvoke` helper
   that tolerates teardown.

2. **Dropdown panel clipped.** The sample page wrapped the navbar in
   `overflow-hidden`, which cut the menu off mid-row. The DOM assertions all
   passed while this was broken — only the screenshot revealed it, so a
   bounding-box check ("panel not clipped") now guards against it.

A third issue was a framework limitation, not a component bug: `MainLayout.razor`
hardcodes which pages may be viewed anonymously, so a page's own
`RequireLogin = false` was ignored and the sample redirected to login. A
`NAVBARSAMPLE` case was added alongside the existing `HOME` one.
