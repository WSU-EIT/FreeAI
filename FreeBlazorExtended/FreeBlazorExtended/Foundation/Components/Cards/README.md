# Foundation Cards

Card-style presentation primitives for dashboards, summaries, and showcase surfaces.

## Files in this folder

| File | Purpose |
|---|---|
| `FeatureCard.razor` | Highlights a feature or capability with icon, description, and feature list support. |
| `MetricCard.razor` | Displays a primary metric with optional trend and supplemental text. |
| `ProgressCard.razor` | Shows percent-complete state with optional metrics and subtitle. |
| `StatusCard.razor` | Presents labeled status information with detail rows and visual state styling. |

## Usage notes

- Use these cards when you need lightweight, reusable presentation surfaces without feature-specific logic.
- Styling assumes Bootstrap-friendly spacing and Font Awesome icons when an icon slot is used.

---

### 🧭 Briefing — **In one line:** four reusable card surfaces — `MetricCard` (a KPI + trend), `StatusCard` (labeled status + detail rows), `ProgressCard` (percent-complete), `FeatureCard` (icon + description + feature list) — generic presentation blocks with no feature logic. **Why:** so dashboards/showcases reuse one set of cards. See the parent [Foundation Components README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/Components/README.md).