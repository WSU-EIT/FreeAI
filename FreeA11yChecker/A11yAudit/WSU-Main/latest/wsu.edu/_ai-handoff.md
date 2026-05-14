# Accessibility Fix Pack — wsu.edu

**Generated:** 2026-05-13 10:18:02  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\FreeA11yChecker\A11yAudit\runs\WSU-Main\wsu.edu`  

## Summary

- **7** pages scanned
- **214** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **214** real violations remaining
- **13** distinct rule failures
- **9** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 7 scanned pages. **Overall pass rate: 0.0%** (0 of 165 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 165 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `aria_navigation_label_unique` (ibm) | 🟠 serious | 7/7 | 31 | site-wide (layout) |
| 2 | `img-alt` (htmlcheck) | 🟠 serious | 7/7 | 21 | site-wide (layout) |
| 3 | `aria_contentinfo_label_unique` (ibm) | 🟠 serious | 7/7 | 14 | site-wide (layout) |
| 4 | `button-empty` (htmlcheck) | 🟠 serious | 7/7 | 7 | site-wide (layout) |
| 5 | `text_contrast_sufficient` (ibm) | 🟠 serious | 3/7 | 21 | shared component |
| 6 | `link-empty` (htmlcheck) | 🟠 serious | 2/7 | 9 | shared component |
| 7 | `table_headers_exists` (ibm) | 🟠 serious | 1/7 | 5 | single page |
| 8 | `figure_label_exists` (ibm) | 🟡 moderate | 7/7 | 50 | site-wide (layout) |
| 9 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 7/7 | 30 | site-wide (layout) |
| 10 | `label-missing` (htmlcheck) | 🟡 moderate | 7/7 | 7 | site-wide (layout) |
| 11 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 7/7 | 7 | site-wide (layout) |
| 12 | `aria_child_valid` (ibm) | 🟡 moderate | 7/7 | 7 | site-wide (layout) |
| 13 | `table-header` (htmlcheck) | 🟡 moderate | 1/7 | 5 | single page |

## Per-rule fix instructions

### 🟠 `aria_navigation_label_unique` (ibm) — SERIOUS

- **Pages affected:** 7 of 7
- **Total occurrences:** 31

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[2]/nav[1]`
- Message: Multiple elements with "navigation" role do not have unique labels

```html
<nav class="wsu-header-system__nav">
```

**All affected pages** (31 total):

- `/` — 2 occurrence(s)
- `/about` — 5 occurrence(s)
- `/academics` — 5 occurrence(s)
- `/admissions` — 5 occurrence(s)
- `/athletics` — 5 occurrence(s)
- `/campuses` — 5 occurrence(s)
- `/research` — 4 occurrence(s)

---

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 7 of 7
- **Total occurrences:** 21

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

**All affected pages** (21 total):

- `/` — 3 occurrence(s)
- `/about` — 3 occurrence(s)
- `/academics` — 3 occurrence(s)
- `/admissions` — 3 occurrence(s)
- `/athletics` — 3 occurrence(s)
- `/campuses` — 3 occurrence(s)
- `/research` — 3 occurrence(s)

---

### 🟠 `aria_contentinfo_label_unique` (ibm) — SERIOUS

- **Pages affected:** 7 of 7
- **Total occurrences:** 14

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]`
- Message: Multiple elements with "contentinfo" role do not have unique labels

```html
<footer class="wsu-footer-site">
```

**All affected pages** (14 total):

- `/` — 2 occurrence(s)
- `/about` — 2 occurrence(s)
- `/academics` — 2 occurrence(s)
- `/admissions` — 2 occurrence(s)
- `/athletics` — 2 occurrence(s)
- `/campuses` — 2 occurrence(s)
- `/research` — 2 occurrence(s)

---

### 🟠 `button-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 7 of 7
- **Total occurrences:** 7

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Button has no text content or accessible name

```html
<button class="wsu-search__submit" aria-lable="Submit Search"></button>
```

**All affected pages** (7 total):

- `/` — 1 occurrence(s)
- `/about` — 1 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/admissions` — 1 occurrence(s)
- `/athletics` — 1 occurrence(s)
- `/campuses` — 1 occurrence(s)
- `/research` — 1 occurrence(s)

---

### 🟠 `text_contrast_sufficient` (ibm) — SERIOUS

- **Pages affected:** 3 of 7
- **Total occurrences:** 21

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/section[2]/div[1]/div[1]/article[1]/div[2]/h3[1]/a[1]`
- Message: Text contrast of 1.12 with its background is less than the WCAG AA minimum requirements for text of size 24px and weight of 600

```html
<a href="https://news.wsu.edu/news/2026/05/11/wsu-lands-1-4m-doe-grant-to-train-next-wave-of-nuclear-workers/">
```

**All affected pages** (21 total):

- `/` — 7 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/research` — 13 occurrence(s)

---

### 🟠 `link-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 2 of 7
- **Total occurrences:** 9

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Link has no text content

```html
<a class="wsu-card__link" href="https://news.wsu.edu/news/2026/05/11/wsu-lands-1-4m-doe-grant-to-train-next-wave-of-nuclear-workers/" aria-hidden="true" tabindex="-1"></a>
```

**All affected pages** (9 total):

- `/` — 3 occurrence(s)
- `/research` — 6 occurrence(s)

---

### 🟠 `table_headers_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 7
- **Total occurrences:** 5

**Sample violation:**

- Page: `/campuses`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[3]/div[3]/div[2]/div[2]/figure[1]/table[1]`
- Message: Table has no headers identified

```html
<table>
```

**All affected pages** (5 total):

- `/campuses` — 5 occurrence(s)

---

### 🟡 `figure_label_exists` (ibm) — MODERATE

- **Pages affected:** 7 of 7
- **Total occurrences:** 50

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/section[4]/div[1]/div[1]/div[1]/figure[1]`
- Message: The <figure> element does not have an associated label

```html
<figure class="wp-block-image size-large wsu-image--style-framed wsu-position--relative wsu-zindex--level-2">
```

**All affected pages** (50 total):

- `/` — 5 occurrence(s)
- `/about` — 11 occurrence(s)
- `/academics` — 8 occurrence(s)
- `/admissions` — 9 occurrence(s)
- `/athletics` — 6 occurrence(s)
- `/campuses` — 6 occurrence(s)
- `/research` — 5 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 7 of 7
- **Total occurrences:** 30

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]`
- Message: Multiple elements with "contentinfo" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<footer class="wsu-footer-site">
```

**All affected pages** (30 total):

- `/` — 2 occurrence(s)
- `/about` — 5 occurrence(s)
- `/academics` — 4 occurrence(s)
- `/admissions` — 5 occurrence(s)
- `/athletics` — 5 occurrence(s)
- `/campuses` — 5 occurrence(s)
- `/research` — 4 occurrence(s)

---

### 🟡 `label-missing` (htmlcheck) — MODERATE

- **Pages affected:** 7 of 7
- **Total occurrences:** 7

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Form input may be missing a label

```html
<input class="wsu-search__input" type="text" aria-lable="Search input" placeholder="Search" name="q" style="">
```

**All affected pages** (7 total):

- `/` — 1 occurrence(s)
- `/about` — 1 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/admissions` — 1 occurrence(s)
- `/athletics` — 1 occurrence(s)
- `/campuses` — 1 occurrence(s)
- `/research` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 7 of 7
- **Total occurrences:** 7

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#wsu-site-menu" class="wsu-skip-to-main">
```

**All affected pages** (7 total):

- `/` — 1 occurrence(s)
- `/about` — 1 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/admissions` — 1 occurrence(s)
- `/athletics` — 1 occurrence(s)
- `/campuses` — 1 occurrence(s)
- `/research` — 1 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 7 of 7
- **Total occurrences:** 7

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]/div[1]/ul[2]`
- Message: The element with role "list" does not own any child element with any of the following role(s): "listitem"

```html
<ul class="wsu-social-icons">
```

**All affected pages** (7 total):

- `/` — 1 occurrence(s)
- `/about` — 1 occurrence(s)
- `/academics` — 1 occurrence(s)
- `/admissions` — 1 occurrence(s)
- `/athletics` — 1 occurrence(s)
- `/campuses` — 1 occurrence(s)
- `/research` — 1 occurrence(s)

---

### 🟡 `table-header` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 7
- **Total occurrences:** 5

**Sample violation:**

- Page: `/campuses`
- Selector: ``
- Message: Data table is missing header cells (<th>)

```html
<table><tbody><tr><td>Undergraduate</td><td>14,178</td></tr><tr><td>Graduate</td><td>1,543</td></tr><tr><td>Professional</td><td>527</td></tr></tbody></table>
```

**All affected pages** (5 total):

- `/campuses` — 5 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
