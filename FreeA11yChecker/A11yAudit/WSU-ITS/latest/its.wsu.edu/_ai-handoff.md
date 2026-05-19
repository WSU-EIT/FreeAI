# Accessibility Fix Pack — its.wsu.edu

**Generated:** 2026-05-19 14:23:30  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\WSU-ITS\its.wsu.edu`  

## Summary

- **8** pages scanned
- **84** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **84** real violations remaining
- **12** distinct rule failures
- **4** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 8 scanned pages. **Overall pass rate: 0.0%** (0 of 40 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 40 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `img-alt` (htmlcheck) | 🟠 serious | 8/8 | 24 | site-wide (layout) |
| 2 | `aria_banner_single` (ibm) | 🟠 serious | 8/8 | 8 | site-wide (layout) |
| 3 | `link-empty` (htmlcheck) | 🟠 serious | 1/8 | 9 | single page |
| 4 | `aria_id_unique` (ibm) | 🟠 serious | 1/8 | 5 | single page |
| 5 | `link-name` (axe) | 🟠 serious | 1/8 | 1 | single page |
| 6 | `scrollable-region-focusable` (axe) | 🟠 serious | 1/8 | 1 | single page |
| 7 | `aria_complementary_labelled` (ibm) | 🟠 serious | 1/8 | 1 | single page |
| 8 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 8/8 | 16 | site-wide (layout) |
| 9 | `fieldset-missing` (htmlcheck) | 🟡 moderate | 8/8 | 8 | site-wide (layout) |
| 10 | `element_tabbable_role_valid` (ibm) | 🟡 moderate | 1/8 | 7 | single page |
| 11 | `figure_label_exists` (ibm) | 🟡 moderate | 1/8 | 3 | single page |
| 12 | `link-pdf` (htmlcheck) | 🟡 moderate | 1/8 | 1 | single page |

## Per-rule fix instructions

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 8 of 8
- **Total occurrences:** 24

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

**All affected pages** (24 total):

- `/` — 3 occurrence(s)
- `/about-its` — 3 occurrence(s)
- `/crimson-service-desk` — 3 occurrence(s)
- `/how-can-we-help-contact-its` — 3 occurrence(s)
- `/information-security-services` — 3 occurrence(s)
- `/its-scheduled-maintenance` — 3 occurrence(s)
- `/news` — 3 occurrence(s)
- `/services-a-z` — 3 occurrence(s)

---

### 🟠 `aria_banner_single` (ibm) — SERIOUS

- **Pages affected:** 8 of 8
- **Total occurrences:** 8

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]`
- Message: Multiple elements with "banner" role found on the page

```html
<header aria-label="WSU system" class="wsu-header-global  wsu-header-global--style-system">
```

**All affected pages** (8 total):

- `/` — 1 occurrence(s)
- `/about-its` — 1 occurrence(s)
- `/crimson-service-desk` — 1 occurrence(s)
- `/how-can-we-help-contact-its` — 1 occurrence(s)
- `/information-security-services` — 1 occurrence(s)
- `/its-scheduled-maintenance` — 1 occurrence(s)
- `/news` — 1 occurrence(s)
- `/services-a-z` — 1 occurrence(s)

---

### 🟠 `link-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 1 of 8
- **Total occurrences:** 9

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Link has no text content

```html
<a class="wsu-card__link" href="https://its.wsu.edu/2025/11/24/reduced-wsu-microsoft-storage-allocations/" aria-hidden="true" tabindex="-1"></a>
```

**All affected pages** (9 total):

- `/` — 9 occurrence(s)

---

### 🟠 `aria_id_unique` (ibm) — SERIOUS

- **Pages affected:** 1 of 8
- **Total occurrences:** 5

**Sample violation:**

- Page: `/crimson-service-desk`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[1]/div[4]/div[2]/article[1]/div[1]/span[1]/p[1]/svg[1]`
- Message: The 'id' "title" specified for the ARIA property 'aria-labelledby' value is not valid

```html
<svg aria-labelledby="title" role="img" title="month" style="fill:#000000;" class="genericon genericond-genericons-neue genericons-neue-month genericond-genericons-neue-3x genericond-rotate-normal">
```

**All affected pages** (5 total):

- `/crimson-service-desk` — 5 occurrence(s)

---

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 1 of 8
- **Total occurrences:** 1
- **How to fix:** Add visible text or `aria-label` to the link.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `.wsu-column.wsu-color-background--white:nth-child(1) > .wsu-color-background--white.wsu-card.wsu-border-top--color-crimson > .wsu-card__content > h3 > a:nth-child(1)`
- Message: Fix all of the following:
  Element is in tab order and does not have accessible text

Fix any of the following:
  Element does not have text that is visible to screen readers
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute

```html
<a href="https://its.wsu.edu/msdata-storage/">				</a>
```

---

### 🟠 `scrollable-region-focusable` (axe) — SERIOUS

- **Pages affected:** 1 of 8
- **Total occurrences:** 1
- **How to fix:** Scrollable regions must be keyboard focusable (e.g., `tabindex="0"`).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/scrollable-region-focusable?application=axeAPI>

**Sample violation:**

- Page: `/its-scheduled-maintenance`
- Selector: `#tablepress-1 > thead > .row-1.odd`
- Message: Fix any of the following:
  Element should have focusable content
  Element should be focusable

```html
<tr class="row-1 odd" style="height: 0px;">
```

---

### 🟠 `aria_complementary_labelled` (ibm) — SERIOUS

- **Pages affected:** 1 of 8
- **Total occurrences:** 1

**Sample violation:**

- Page: `/news`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/aside[1]`
- Message: Element with "complementary" role does not have a label

```html
<aside class="wsu-wrapper-sidebar">
```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 8 of 8
- **Total occurrences:** 16

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#wsu-site-menu" class="wsu-skip-to-main skip-to-menu-desktop">
```

**All affected pages** (16 total):

- `/` — 2 occurrence(s)
- `/about-its` — 2 occurrence(s)
- `/crimson-service-desk` — 2 occurrence(s)
- `/how-can-we-help-contact-its` — 2 occurrence(s)
- `/information-security-services` — 2 occurrence(s)
- `/its-scheduled-maintenance` — 2 occurrence(s)
- `/news` — 2 occurrence(s)
- `/services-a-z` — 2 occurrence(s)

---

### 🟡 `fieldset-missing` (htmlcheck) — MODERATE

- **Pages affected:** 8 of 8
- **Total occurrences:** 8

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Radio/checkbox group is not wrapped in a fieldset

```html
<input class="wsu-search-options__option-input" type="radio" name="wsu-search-type" value="site" checked="checked" style="">
```

**All affected pages** (8 total):

- `/` — 1 occurrence(s)
- `/about-its` — 1 occurrence(s)
- `/crimson-service-desk` — 1 occurrence(s)
- `/how-can-we-help-contact-its` — 1 occurrence(s)
- `/information-security-services` — 1 occurrence(s)
- `/its-scheduled-maintenance` — 1 occurrence(s)
- `/news` — 1 occurrence(s)
- `/services-a-z` — 1 occurrence(s)

---

### 🟡 `element_tabbable_role_valid` (ibm) — MODERATE

- **Pages affected:** 1 of 8
- **Total occurrences:** 7

**Sample violation:**

- Page: `/its-scheduled-maintenance`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[1]/div[1]/div[2]/div[1]/div[1]/table[1]/thead[1]/tr[1]/th[1]`
- Message: The tabbable element's role 'columnheader' is not a widget role

```html
<th aria-label="Service Name: activate to sort column ascending" style="width: 195.609px;" colspan="1" rowspan="1" aria-controls="tablepress-1" tabindex="0" class="column-1 sorting">
```

**All affected pages** (7 total):

- `/its-scheduled-maintenance` — 7 occurrence(s)

---

### 🟡 `figure_label_exists` (ibm) — MODERATE

- **Pages affected:** 1 of 8
- **Total occurrences:** 3

**Sample violation:**

- Page: `/crimson-service-desk`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[1]/div[4]/div[2]/article[1]/div[1]/span[1]/div[1]/figure[1]`
- Message: The <figure> element does not have an associated label

```html
<figure class="aligncenter size-large">
```

**All affected pages** (3 total):

- `/crimson-service-desk` — 3 occurrence(s)

---

### 🟡 `link-pdf` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 8
- **Total occurrences:** 1

**Sample violation:**

- Page: `/about-its`
- Selector: ``
- Message: Link points to a PDF — verify accessibility of linked document

```html
<a href="https://its.wsu.edu/documents/2023/10/its-organizational-chart.pdf" class="wsu-button  wsu-button--style-outline">
```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
