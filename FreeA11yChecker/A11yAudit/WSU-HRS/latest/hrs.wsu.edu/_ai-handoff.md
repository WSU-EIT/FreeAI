# Accessibility Fix Pack — hrs.wsu.edu

**Generated:** 2026-05-19 14:20:16  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\WSU-HRS\hrs.wsu.edu`  

## Summary

- **10** pages scanned
- **285** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **285** real violations remaining
- **13** distinct rule failures
- **7** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 10 scanned pages. **Overall pass rate: 0.0%** (0 of 205 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 205 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `img-alt` (htmlcheck) | 🟠 serious | 10/10 | 30 | site-wide (layout) |
| 2 | `target-size` (axe) | 🟠 serious | 10/10 | 29 | site-wide (layout) |
| 3 | `svg_graphics_labelled` (ibm) | 🟠 serious | 10/10 | 10 | site-wide (layout) |
| 4 | `aria_navigation_label_unique` (ibm) | 🟠 serious | 9/10 | 36 | site-wide (layout) |
| 5 | `text_contrast_sufficient` (ibm) | 🟠 serious | 3/10 | 7 | shared component |
| 6 | `link-name` (axe) | 🟠 serious | 1/10 | 6 | single page |
| 7 | `a_text_purpose` (ibm) | 🟠 serious | 1/10 | 6 | single page |
| 8 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 10/10 | 20 | site-wide (layout) |
| 9 | `aria_child_valid` (ibm) | 🟡 moderate | 9/10 | 63 | site-wide (layout) |
| 10 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 9/10 | 36 | site-wide (layout) |
| 11 | `figure_label_exists` (ibm) | 🟡 moderate | 5/10 | 27 | shared component |
| 12 | `link-pdf` (htmlcheck) | 🟡 moderate | 4/10 | 12 | shared component |
| 13 | `heading-order` (htmlcheck) | 🟡 moderate | 3/10 | 3 | shared component |

## Per-rule fix instructions

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 10 of 10
- **Total occurrences:** 30

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

**All affected pages** (30 total):

- `/` — 3 occurrence(s)
- `/careers` — 3 occurrence(s)
- `/contact` — 3 occurrence(s)
- `/employees` — 3 occurrence(s)
- `/employees/benefits` — 3 occurrence(s)
- `/employees/leave` — 3 occurrence(s)
- `/managers` — 3 occurrence(s)
- `/new-employees` — 3 occurrence(s)
- `/recognition` — 3 occurrence(s)
- `/training` — 3 occurrence(s)

---

### 🟠 `target-size` (axe) — SERIOUS

- **Pages affected:** 10 of 10
- **Total occurrences:** 29
- **How to fix:** Interactive targets should be at least 24x24 CSS pixels (WCAG 2.2 AA).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/target-size?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `#menu-item-2325 > a`
- Message: Fix any of the following:
  Target has insufficient size (104px by 14px, should be at least 24px by 24px)
  Target has insufficient space to its closest neighbors. Safe clickable space has a diameter of 23.8px instead of at least 24px.

```html
<a href="https://hrs.wsu.edu/mission-statement/">Mission Statement</a>
```

**All affected pages** (29 total):

- `/` — 2 occurrence(s)
- `/careers` — 3 occurrence(s)
- `/contact` — 3 occurrence(s)
- `/employees` — 3 occurrence(s)
- `/employees/benefits` — 3 occurrence(s)
- `/employees/leave` — 3 occurrence(s)
- `/managers` — 3 occurrence(s)
- `/new-employees` — 3 occurrence(s)
- `/recognition` — 3 occurrence(s)
- `/training` — 3 occurrence(s)

---

### 🟠 `svg_graphics_labelled` (ibm) — SERIOUS

- **Pages affected:** 10 of 10
- **Total occurrences:** 10

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[1]/main[1]/footer[1]/div[1]/div[1]/svg[1]`
- Message: The SVG element has no accessible name

```html
<svg viewBox="7.481 7.416 50.428 50" height="50" width="50.428" xmlns="http://www.w3.org/2000/svg" class="wsu-cougar-head">
```

**All affected pages** (10 total):

- `/` — 1 occurrence(s)
- `/careers` — 1 occurrence(s)
- `/contact` — 1 occurrence(s)
- `/employees` — 1 occurrence(s)
- `/employees/benefits` — 1 occurrence(s)
- `/employees/leave` — 1 occurrence(s)
- `/managers` — 1 occurrence(s)
- `/new-employees` — 1 occurrence(s)
- `/recognition` — 1 occurrence(s)
- `/training` — 1 occurrence(s)

---

### 🟠 `aria_navigation_label_unique` (ibm) — SERIOUS

- **Pages affected:** 9 of 10
- **Total occurrences:** 36

**Sample violation:**

- Page: `/careers`
- Selector: `/html[1]/body[1]/div[1]/div[1]/div[1]/div[1]/section[2]/nav[1]`
- Message: Multiple elements with "navigation" role do not have unique labels

```html
<nav class="spine-sitenav" id="spine-sitenav">
```

**All affected pages** (36 total):

- `/careers` — 4 occurrence(s)
- `/contact` — 4 occurrence(s)
- `/employees` — 4 occurrence(s)
- `/employees/benefits` — 4 occurrence(s)
- `/employees/leave` — 4 occurrence(s)
- `/managers` — 4 occurrence(s)
- `/new-employees` — 4 occurrence(s)
- `/recognition` — 4 occurrence(s)
- `/training` — 4 occurrence(s)

---

### 🟠 `text_contrast_sufficient` (ibm) — SERIOUS

- **Pages affected:** 3 of 10
- **Total occurrences:** 7

**Sample violation:**

- Page: `/careers`
- Selector: `/html[1]/body[1]/div[1]/div[1]/main[1]/section[1]/div[1]/article[1]/div[1]/div[1]/div[1]/p[1]`
- Message: Text contrast of 1.04 with its background is less than the WCAG AA minimum requirements for text of size 20.25px and weight of 400

```html
<p class="has-text-align-center has-medium-font-size">
```

**All affected pages** (7 total):

- `/careers` — 1 occurrence(s)
- `/contact` — 3 occurrence(s)
- `/new-employees` — 3 occurrence(s)

---

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 1 of 10
- **Total occurrences:** 6
- **How to fix:** Add visible text or `aria-label` to the link.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI>

**Sample violation:**

- Page: `/training`
- Selector: `.wp-block-columns.wp-container-core-columns-is-layout-9d6595d7.wp-block-columns-is-layout-flex:nth-child(4) > .wp-block-column.is-layout-flow.wp-block-column-is-layout-flow > figure > a`
- Message: Fix all of the following:
  Element is in tab order and does not have accessible text

Fix any of the following:
  Element does not have text that is visible to screen readers
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute

```html
<a href="https://share.percipio.com/cd/BCzZ8uz6F">
```

**All affected pages** (6 total):

- `/training` — 6 occurrence(s)

---

### 🟠 `a_text_purpose` (ibm) — SERIOUS

- **Pages affected:** 1 of 10
- **Total occurrences:** 6

**Sample violation:**

- Page: `/training`
- Selector: `/html[1]/body[1]/div[1]/div[1]/main[1]/section[1]/div[1]/article[1]/div[1]/div[3]/div[1]/figure[1]/a[1]`
- Message: Hyperlink has no link text, label or image with a text alternative

```html
<a href="https://share.percipio.com/cd/BCzZ8uz6F">
```

**All affected pages** (6 total):

- `/training` — 6 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 10 of 10
- **Total occurrences:** 20

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a class="screen-reader-shortcut" href="#wsuwp-main">
```

**All affected pages** (20 total):

- `/` — 2 occurrence(s)
- `/careers` — 2 occurrence(s)
- `/contact` — 2 occurrence(s)
- `/employees` — 2 occurrence(s)
- `/employees/benefits` — 2 occurrence(s)
- `/employees/leave` — 2 occurrence(s)
- `/managers` — 2 occurrence(s)
- `/new-employees` — 2 occurrence(s)
- `/recognition` — 2 occurrence(s)
- `/training` — 2 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 9 of 10
- **Total occurrences:** 63

**Sample violation:**

- Page: `/careers`
- Selector: `/html[1]/body[1]/div[1]/div[1]/div[1]/div[1]/section[2]/nav[1]/ul[1]/li[4]/ul[1]`
- Message: The element with role "list" does not own any child element with any of the following role(s): "listitem"

```html
<ul class="sub-menu">
```

**All affected pages** (63 total):

- `/careers` — 6 occurrence(s)
- `/contact` — 6 occurrence(s)
- `/employees` — 9 occurrence(s)
- `/employees/benefits` — 8 occurrence(s)
- `/employees/leave` — 9 occurrence(s)
- `/managers` — 6 occurrence(s)
- `/new-employees` — 9 occurrence(s)
- `/recognition` — 5 occurrence(s)
- `/training` — 5 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 9 of 10
- **Total occurrences:** 36

**Sample violation:**

- Page: `/careers`
- Selector: `/html[1]/body[1]/div[1]/div[1]/div[1]/div[1]/section[2]/nav[1]`
- Message: Multiple elements with "navigation" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<nav class="spine-sitenav" id="spine-sitenav">
```

**All affected pages** (36 total):

- `/careers` — 4 occurrence(s)
- `/contact` — 4 occurrence(s)
- `/employees` — 4 occurrence(s)
- `/employees/benefits` — 4 occurrence(s)
- `/employees/leave` — 4 occurrence(s)
- `/managers` — 4 occurrence(s)
- `/new-employees` — 4 occurrence(s)
- `/recognition` — 4 occurrence(s)
- `/training` — 4 occurrence(s)

---

### 🟡 `figure_label_exists` (ibm) — MODERATE

- **Pages affected:** 5 of 10
- **Total occurrences:** 27

**Sample violation:**

- Page: `/careers`
- Selector: `/html[1]/body[1]/div[1]/div[1]/main[1]/section[1]/div[1]/article[1]/div[1]/figure[1]`
- Message: The <figure> element does not have an associated label

```html
<figure class="wp-block-embed alignfull is-type-video is-provider-youtube wp-block-embed-youtube wp-embed-aspect-16-9 wp-has-aspect-ratio">
```

**All affected pages** (27 total):

- `/careers` — 2 occurrence(s)
- `/employees/benefits` — 6 occurrence(s)
- `/new-employees` — 11 occurrence(s)
- `/recognition` — 7 occurrence(s)
- `/training` — 1 occurrence(s)

---

### 🟡 `link-pdf` (htmlcheck) — MODERATE

- **Pages affected:** 4 of 10
- **Total occurrences:** 12

**Sample violation:**

- Page: `/contact`
- Selector: ``
- Message: Link points to a PDF — verify accessibility of linked document

```html
<a href="https://s3.wp.wsu.edu/uploads/sites/746/2018/07/WSUSCampusMapJuly2018_download.pdf">
```

**All affected pages** (12 total):

- `/contact` — 1 occurrence(s)
- `/employees/benefits` — 1 occurrence(s)
- `/employees/leave` — 9 occurrence(s)
- `/training` — 1 occurrence(s)

---

### 🟡 `heading-order` (htmlcheck) — MODERATE

- **Pages affected:** 3 of 10
- **Total occurrences:** 3
- **How to fix:** Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading level skipped: h1 to h3

```html

```

**All affected pages** (3 total):

- `/` — 1 occurrence(s)
- `/careers` — 1 occurrence(s)
- `/employees/leave` — 1 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
