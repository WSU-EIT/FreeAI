# Accessibility Fix Pack — admission.wsu.edu

**Generated:** 2026-05-19 14:14:13  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\WSU-Admissions\admission.wsu.edu`  

## Summary

- **6** pages scanned
- **227** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **227** real violations remaining
- **25** distinct rule failures
- **15** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 6 scanned pages. **Overall pass rate: 0.0%** (0 of 167 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 167 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `aria-allowed-attr` (axe) | 🔴 critical | 5/6 | 5 | site-wide (layout) |
| 2 | `img-alt` (htmlcheck) | 🟠 serious | 6/6 | 20 | site-wide (layout) |
| 3 | `aria_banner_single` (ibm) | 🟠 serious | 6/6 | 11 | site-wide (layout) |
| 4 | `aria_navigation_label_unique` (ibm) | 🟠 serious | 5/6 | 15 | site-wide (layout) |
| 5 | `text_contrast_sufficient` (ibm) | 🟠 serious | 5/6 | 12 | site-wide (layout) |
| 6 | `color-contrast` (axe) | 🟠 serious | 5/6 | 11 | site-wide (layout) |
| 7 | `aria_banner_label_unique` (ibm) | 🟠 serious | 5/6 | 10 | site-wide (layout) |
| 8 | `aria_contentinfo_label_unique` (ibm) | 🟠 serious | 5/6 | 10 | site-wide (layout) |
| 9 | `link-empty` (htmlcheck) | 🟠 serious | 5/6 | 5 | site-wide (layout) |
| 10 | `button-empty` (htmlcheck) | 🟠 serious | 5/6 | 5 | site-wide (layout) |
| 11 | `label_name_visible` (ibm) | 🟠 serious | 3/6 | 29 | shared component |
| 12 | `table_headers_exists` (ibm) | 🟠 serious | 1/6 | 3 | single page |
| 13 | `target-size` (axe) | 🟠 serious | 1/6 | 2 | single page |
| 14 | `frame-title` (axe) | 🟠 serious | 1/6 | 2 | single page |
| 15 | `frame_title_exists` (ibm) | 🟠 serious | 1/6 | 2 | single page |
| 16 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 6/6 | 16 | site-wide (layout) |
| 17 | `aria_child_valid` (ibm) | 🟡 moderate | 6/6 | 7 | site-wide (layout) |
| 18 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 5/6 | 20 | site-wide (layout) |
| 19 | `label-missing` (htmlcheck) | 🟡 moderate | 5/6 | 5 | site-wide (layout) |
| 20 | `aria_attribute_redundant` (ibm) | 🟡 moderate | 5/6 | 5 | site-wide (layout) |
| 21 | `figure_label_exists` (ibm) | 🟡 moderate | 4/6 | 26 | shared component |
| 22 | `table-header` (htmlcheck) | 🟡 moderate | 1/6 | 3 | single page |
| 23 | `link-suspicious` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |
| 24 | `element_tabbable_role_valid` (ibm) | 🟡 moderate | 1/6 | 1 | single page |
| 25 | `heading-order` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |

## Per-rule fix instructions

### 🔴 `aria-allowed-attr` (axe) — CRITICAL

- **Pages affected:** 5 of 6
- **Total occurrences:** 5
- **How to fix:** Remove the ARIA attribute that isn't allowed on this role.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/aria-allowed-attr?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `#wsu-navigation-vertical`
- Message: Fix all of the following:
  ARIA attribute is not allowed: aria-expanded="true"

```html
<div id="wsu-navigation-vertical" class="wsu-slide-in-panel wsu-navigation-vertical wsu-slide-in-panel--position-left wsu-slide-in-panel--overlay-none wsu-slide-in-panel--position-left wsu-slide-in-panel--width-vertical-nav wsu-slide-in-panel--style-crimson-mark" aria-expanded="true" aria-haspopup="true" aria-label="Site Navigation">
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 6 of 6
- **Total occurrences:** 20

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

**All affected pages** (20 total):

- `/` — 5 occurrence(s)
- `/academics` — 3 occurrence(s)
- `/apply` — 3 occurrence(s)
- `/cost-and-aid` — 3 occurrence(s)
- `/life` — 3 occurrence(s)
- `/visits` — 3 occurrence(s)

---

### 🟠 `aria_banner_single` (ibm) — SERIOUS

- **Pages affected:** 6 of 6
- **Total occurrences:** 11

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[2]`
- Message: Multiple elements with "banner" role found on the page

```html
<header class="wsu-header-global  wsu-header-global--style-system">
```

**All affected pages** (11 total):

- `/` — 2 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/apply` — 2 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 2 occurrence(s)
- `/visits` — 2 occurrence(s)

---

### 🟠 `aria_navigation_label_unique` (ibm) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 15

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/nav[1]`
- Message: Multiple elements with "navigation" role do not have unique labels

```html
<nav class="wsu-navigation-audience ">
```

**All affected pages** (15 total):

- `/` — 3 occurrence(s)
- `/apply` — 3 occurrence(s)
- `/cost-and-aid` — 3 occurrence(s)
- `/life` — 3 occurrence(s)
- `/visits` — 3 occurrence(s)

---

### 🟠 `text_contrast_sufficient` (ibm) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 12

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[13]/div[2]/div[3]/span[1]`
- Message: Text contrast of 3.67 with its background is less than the WCAG AA minimum requirements for text of size 16px and weight of 400

```html
<span>
```

**All affected pages** (12 total):

- `/` — 3 occurrence(s)
- `/apply` — 2 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 3 occurrence(s)
- `/visits` — 2 occurrence(s)

---

### 🟠 `color-contrast` (axe) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 11
- **How to fix:** Increase color contrast. Need 4.5:1 for body text, 3:1 for large text.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `a[href="tel:5095535450"]`
- Message: Fix any of the following:
  Element has insufficient color contrast of 1.64 (foreground color: #a60f2d, background color: #333333, font size: 13.5pt (18px), font weight: normal). Expected contrast ratio of 4.5:1

```html
<a href="tel:5095535450">509-553-5450</a>
```

**All affected pages** (11 total):

- `/` — 2 occurrence(s)
- `/apply` — 2 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 3 occurrence(s)
- `/visits` — 2 occurrence(s)

---

### 🟠 `aria_banner_label_unique` (ibm) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 10

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[2]`
- Message: Multiple elements with "banner" role do not have unique labels

```html
<header class="wsu-header-global  wsu-header-global--style-system">
```

**All affected pages** (10 total):

- `/` — 2 occurrence(s)
- `/apply` — 2 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 2 occurrence(s)
- `/visits` — 2 occurrence(s)

---

### 🟠 `aria_contentinfo_label_unique` (ibm) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 10

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]`
- Message: Multiple elements with "contentinfo" role do not have unique labels

```html
<footer class="wsu-footer-site wsu-footer-site--dark">
```

**All affected pages** (10 total):

- `/` — 2 occurrence(s)
- `/apply` — 2 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 2 occurrence(s)
- `/visits` — 2 occurrence(s)

---

### 🟠 `link-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 5

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Link has no text content

```html
<a href="https://admission.wsu.edu?s=" class="wsu-button-ui-search" title="Search WSU"></a>
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟠 `button-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 5

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Button has no text content or accessible name

```html
<button class="wsu-search__submit" aria-lable="Submit Search"></button>
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟠 `label_name_visible` (ibm) — SERIOUS

- **Pages affected:** 3 of 6
- **Total occurrences:** 29

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[4]/article[1]/div[2]/span[1]/div[1]/a[1]`
- Message: Accessible name does not match or contain the visible label text

```html
<a class="wsu-button " href="https://admission.wsu.edu/apply/first-year-students/" aria-label="More First-Year Student Information">
```

**All affected pages** (29 total):

- `/` — 13 occurrence(s)
- `/apply` — 12 occurrence(s)
- `/visits` — 4 occurrence(s)

---

### 🟠 `table_headers_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 3

**Sample violation:**

- Page: `/life`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[4]/div[1]/div[2]/div[1]/div[2]/figure[1]/table[1]`
- Message: Table has no headers identified

```html
<table>
```

**All affected pages** (3 total):

- `/life` — 3 occurrence(s)

---

### 🟠 `target-size` (axe) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 2
- **How to fix:** Interactive targets should be at least 24x24 CSS pixels (WCAG 2.2 AA).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/target-size?application=axeAPI>

**Sample violation:**

- Page: `/academics`
- Selector: `.wsu-degree-finder__degrees-grid-control-grid`
- Message: Fix any of the following:
  Target has insufficient size (12px by 2px, should be at least 24px by 24px)
  Target has insufficient space to its closest neighbors. Safe clickable space has a diameter of 16px instead of at least 24px.

```html
<button class="wsu-degree-finder__degrees-grid-control-grid" role="radio" aria-checked="true" aria-label="Change degree list layout to a grid"><i class="fa-sharp fa-solid fa-grid fa-2xl"></i></button>
```

**All affected pages** (2 total):

- `/academics` — 2 occurrence(s)

---

### 🟠 `frame-title` (axe) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 2
- **How to fix:** Add a `title` attribute to the `<iframe>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/frame-title?application=axeAPI>

**Sample violation:**

- Page: `/visits`
- Selector: `.wsu-column:nth-child(1) > .wp-block-embed.is-type-video.is-provider-youtube > .wp-block-embed__wrapper > .embed-youtube > .youtube-player[type="text/html"][width="640"]`
- Message: Fix any of the following:
  Element has no title attribute
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element's default semantics were not overridden with role="none" or role="presentation"

```html
<iframe class="youtube-player" type="text/html" width="640" height="390" src="https://www.youtube.com/embed/jWqiWEUERO8?version=3&amp;rel=1&amp;fs=1&amp;autohide=2&amp;showsearch=0&amp;showinfo=1&amp;iv_load_policy=1&amp;wmode=transparent" frameborder="0" allowfullscreen="true"></iframe>
```

**All affected pages** (2 total):

- `/visits` — 2 occurrence(s)

---

### 🟠 `frame_title_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 2

**Sample violation:**

- Page: `/visits`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[5]/div[1]/figure[1]/div[1]/div[1]/iframe[1]`
- Message: Inline frame does not have a 'title' attribute

```html
<iframe allowfullscreen="true" frameborder="0" src="https://www.youtube.com/embed/jWqiWEUERO8?version=3&rel=1&fs=1&autohide=2&showsearch=0&showinfo=1&iv_load_policy=1&wmode=transparent" height="390" width="640" type="text/html" class="youtube-player">
```

**All affected pages** (2 total):

- `/visits` — 2 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 6 of 6
- **Total occurrences:** 16

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#menu-site-navigation" class="wsu-skip-to-main">
```

**All affected pages** (16 total):

- `/` — 3 occurrence(s)
- `/academics` — 2 occurrence(s)
- `/apply` — 3 occurrence(s)
- `/cost-and-aid` — 2 occurrence(s)
- `/life` — 3 occurrence(s)
- `/visits` — 3 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 6 of 6
- **Total occurrences:** 7

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]/div[1]/ul[1]`
- Message: The element with role "list" does not own any child element with any of the following role(s): "listitem"

```html
<ul class="wsu-social-icons">
```

**All affected pages** (7 total):

- `/` — 1 occurrence(s)
- `/academics` — 2 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 5 of 6
- **Total occurrences:** 20

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[2]`
- Message: Multiple elements with "banner" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<header class="wsu-header-global  wsu-header-global--style-system">
```

**All affected pages** (20 total):

- `/` — 4 occurrence(s)
- `/apply` — 4 occurrence(s)
- `/cost-and-aid` — 4 occurrence(s)
- `/life` — 4 occurrence(s)
- `/visits` — 4 occurrence(s)

---

### 🟡 `label-missing` (htmlcheck) — MODERATE

- **Pages affected:** 5 of 6
- **Total occurrences:** 5

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Form input may be missing a label

```html
<input class="wsu-search__input" type="text" aria-lable="Search input" placeholder="Search" name="s" style="">
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟡 `aria_attribute_redundant` (ibm) — MODERATE

- **Pages affected:** 5 of 6
- **Total occurrences:** 5

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/form[1]/div[1]/select[1]`
- Message: The ARIA attribute "aria-required" is redundant with the HTML attribute "required"

```html
<select aria-required="true" required="required" name="form_campus" class="counselor-form-control" id="form_campus">
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/apply` — 1 occurrence(s)
- `/cost-and-aid` — 1 occurrence(s)
- `/life` — 1 occurrence(s)
- `/visits` — 1 occurrence(s)

---

### 🟡 `figure_label_exists` (ibm) — MODERATE

- **Pages affected:** 4 of 6
- **Total occurrences:** 26

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/section[1]/div[2]/div[2]/div[1]/figure[1]`
- Message: The <figure> element does not have an associated label

```html
<figure class="wp-block-image size-large wsu-image--style-framed wsu-zindex--level-2">
```

**All affected pages** (26 total):

- `/` — 5 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/life` — 8 occurrence(s)
- `/visits` — 12 occurrence(s)

---

### 🟡 `table-header` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 3

**Sample violation:**

- Page: `/life`
- Selector: ``
- Message: Data table is missing header cells (<th>)

```html
<table><tbody><tr><td>48</td><td>U.S. states represented</td></tr><tr><td>101</td><td>Countries represented</td></tr><tr><td>36%</td><td>of students are multicultural</td></tr><tr><td>34%</td><td>of students are first in their families to attend college</td></tr><tr><td>Top 40</td><td>Universities nationally for policies supportive of LGBTQ+ students (Campus Pride Index, 2021)</td></tr></tbody>...
```

**All affected pages** (3 total):

- `/life` — 3 occurrence(s)

---

### 🟡 `link-suspicious` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academics`
- Selector: ``
- Message: Suspicious link text (not descriptive)

```html
<a href="https://futurecoug.wsu.edu/register/form?id=4ce22d6e-d886-44cf-be68-c808578f4285">here</a>
```

---

### 🟡 `element_tabbable_role_valid` (ibm) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academics`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[1]/div[2]/div[1]/div[1]/div[3]/div[1]/div[1]/div[1]/label[1]`
- Message: The tabbable element does not have a valid widget role

```html
<label tabindex="0" class="wsu-degree-finder__filter-search-submit">
```

---

### 🟡 `heading-order` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1
- **How to fix:** Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.

**Sample violation:**

- Page: `/apply`
- Selector: ``
- Message: Heading level skipped: h1 to h3

```html

```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
