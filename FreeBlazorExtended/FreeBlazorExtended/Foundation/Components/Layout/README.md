# Foundation Layout

Layout primitives for page scaffolding, grid rendering, and sidebar-based compositions.

## Files in this folder

| File | Purpose |
|---|---|
| `PageContainer.razor` | Wraps a page with optional header, main content, and footer regions. |
| `ResponsiveGrid.razor` | Renders collections inside a responsive grid with item or child templates. |
| `ThreeColumnLayout.razor` | Provides a main content area with optional left and right sidebars. |
| `TwoColumnLayout.razor` | Provides a main content area with a single sidebar. |

## Usage notes

- These layouts are useful when feature folders need repeatable scaffolding without duplicating CSS and markup.
- Sidebar regions are optional so a caller can scale from simple to denser page shells.

---

### 🧭 Briefing — **In one line:** four layout scaffolds — `PageContainer` (header/main/footer), `ResponsiveGrid`, `TwoColumnLayout`, and `ThreeColumnLayout` (optional left/right sidebars) — for repeatable page structure without duplicating CSS. **Why:** so feature pages share consistent scaffolding. See the parent [Foundation Components README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Components/README.md).