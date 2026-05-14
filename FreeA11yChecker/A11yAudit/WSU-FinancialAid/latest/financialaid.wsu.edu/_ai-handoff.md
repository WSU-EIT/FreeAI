# Accessibility Fix Pack — financialaid.wsu.edu

**Generated:** 2026-05-13 10:05:48  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\FreeA11yChecker\A11yAudit\runs\WSU-FinancialAid\financialaid.wsu.edu`  

## Summary

- **6** pages scanned
- **90** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **90** real violations remaining
- **11** distinct rule failures
- **6** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 6 scanned pages. **Overall pass rate: 0.0%** (0 of 45 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 45 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `img-alt` (htmlcheck) | 🟠 serious | 6/6 | 18 | site-wide (layout) |
| 2 | `aria_banner_single` (ibm) | 🟠 serious | 6/6 | 6 | site-wide (layout) |
| 3 | `aria_main_label_unique` (ibm) | 🟠 serious | 4/6 | 8 | shared component |
| 4 | `input_checkboxes_grouped` (ibm) | 🟠 serious | 4/6 | 8 | shared component |
| 5 | `table_headers_exists` (ibm) | 🟠 serious | 1/6 | 1 | single page |
| 6 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 6/6 | 12 | site-wide (layout) |
| 7 | `fieldset-missing` (htmlcheck) | 🟡 moderate | 6/6 | 10 | site-wide (layout) |
| 8 | `aria_child_valid` (ibm) | 🟡 moderate | 6/6 | 6 | site-wide (layout) |
| 9 | `table-header` (htmlcheck) | 🟡 moderate | 5/6 | 13 | site-wide (layout) |
| 10 | `aria_main_label_visible` (ibm) | 🟡 moderate | 4/6 | 4 | shared component |
| 11 | `link-pdf` (htmlcheck) | 🟡 moderate | 1/6 | 4 | single page |

## Per-rule fix instructions

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 6 of 6
- **Total occurrences:** 18

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img height="1" width="1" style="display:none" src="https://www.facebook.com/tr?id=352489839123111&amp;ev=PageView&amp;noscript=1">
```

**All affected pages** (18 total):

- `/` — 3 occurrence(s)
- `/contact-us` — 3 occurrence(s)
- `/cost-of-attendance` — 3 occurrence(s)
- `/forms` — 3 occurrence(s)
- `/how-to-apply` — 3 occurrence(s)
- `/types-of-aid` — 3 occurrence(s)

---

### 🟠 `aria_banner_single` (ibm) — SERIOUS

- **Pages affected:** 6 of 6
- **Total occurrences:** 6

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]`
- Message: Multiple elements with "banner" role found on the page

```html
<header aria-label="WSU system" class="wsu-header-global  wsu-header-global--style-system">
```

**All affected pages** (6 total):

- `/` — 1 occurrence(s)
- `/contact-us` — 1 occurrence(s)
- `/cost-of-attendance` — 1 occurrence(s)
- `/forms` — 1 occurrence(s)
- `/how-to-apply` — 1 occurrence(s)
- `/types-of-aid` — 1 occurrence(s)

---

### 🟠 `aria_main_label_unique` (ibm) — SERIOUS

- **Pages affected:** 4 of 6
- **Total occurrences:** 8

**Sample violation:**

- Page: `/contact-us`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]`
- Message: Multiple elements with "main" role do not have unique labels

```html
<main class="wsu-wrapper-main" id="wsu-content" role="main">
```

**All affected pages** (8 total):

- `/contact-us` — 2 occurrence(s)
- `/cost-of-attendance` — 2 occurrence(s)
- `/how-to-apply` — 2 occurrence(s)
- `/types-of-aid` — 2 occurrence(s)

---

### 🟠 `input_checkboxes_grouped` (ibm) — SERIOUS

- **Pages affected:** 4 of 6
- **Total occurrences:** 8

**Sample violation:**

- Page: `/contact-us`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/div[1]/main[1]/section[1]/div[1]/form[1]/div[2]/input[1]`
- Message: Radio input and others with the name "search_context" are not grouped together

```html
<input style="" checked="checked" value="site" name="search_context" id="wsu-search__search-toggle-site" class="wsu-search__search-toggle" type="radio">
```

**All affected pages** (8 total):

- `/contact-us` — 2 occurrence(s)
- `/cost-of-attendance` — 2 occurrence(s)
- `/how-to-apply` — 2 occurrence(s)
- `/types-of-aid` — 2 occurrence(s)

---

### 🟠 `table_headers_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/forms`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/article[1]/div[1]/figure[1]/table[1]`
- Message: Table has no headers identified

```html
<table>
```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 6 of 6
- **Total occurrences:** 12

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#wsu-site-menu" class="wsu-skip-to-main skip-to-menu-desktop">
```

**All affected pages** (12 total):

- `/` — 2 occurrence(s)
- `/contact-us` — 2 occurrence(s)
- `/cost-of-attendance` — 2 occurrence(s)
- `/forms` — 2 occurrence(s)
- `/how-to-apply` — 2 occurrence(s)
- `/types-of-aid` — 2 occurrence(s)

---

### 🟡 `fieldset-missing` (htmlcheck) — MODERATE

- **Pages affected:** 6 of 6
- **Total occurrences:** 10

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Radio/checkbox group is not wrapped in a fieldset

```html
<input class="wsu-search-options__option-input" type="radio" name="wsu-search-type" value="site" checked="checked" style="">
```

**All affected pages** (10 total):

- `/` — 1 occurrence(s)
- `/contact-us` — 2 occurrence(s)
- `/cost-of-attendance` — 2 occurrence(s)
- `/forms` — 1 occurrence(s)
- `/how-to-apply` — 2 occurrence(s)
- `/types-of-aid` — 2 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 6 of 6
- **Total occurrences:** 6

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[2]/footer[1]/div[1]/ul[2]`
- Message: The element with role "list" does not own any child element with any of the following role(s): "listitem"

```html
<ul class="wsu-social-icons">
```

**All affected pages** (6 total):

- `/` — 1 occurrence(s)
- `/contact-us` — 1 occurrence(s)
- `/cost-of-attendance` — 1 occurrence(s)
- `/forms` — 1 occurrence(s)
- `/how-to-apply` — 1 occurrence(s)
- `/types-of-aid` — 1 occurrence(s)

---

### 🟡 `table-header` (htmlcheck) — MODERATE

- **Pages affected:** 5 of 6
- **Total occurrences:** 13

**Sample violation:**

- Page: `/contact-us`
- Selector: ``
- Message: Data table is missing header cells (<th>)

```html
<table cellspacing="0" cellpadding="0" role="presentation" class="gsc-search-box"><tbody><tr><td class="gsc-input"><div class="gsc-input-box" id="gsc-iw-id1"><table cellspacing="0" cellpadding="0" role="presentation" id="gs_id50" class="gstl_50 gsc-input" style="width: 100%; padding: 0px;"><tbody><tr><td id="gs_tti50" class="gsib_a"><input autocomplete="off" type="text" size="10" class="gsc-inp...
```

**All affected pages** (13 total):

- `/contact-us` — 3 occurrence(s)
- `/cost-of-attendance` — 3 occurrence(s)
- `/forms` — 1 occurrence(s)
- `/how-to-apply` — 3 occurrence(s)
- `/types-of-aid` — 3 occurrence(s)

---

### 🟡 `aria_main_label_visible` (ibm) — MODERATE

- **Pages affected:** 4 of 6
- **Total occurrences:** 4

**Sample violation:**

- Page: `/contact-us`
- Selector: `/html[1]/body[1]`
- Message: Multiple elements with "main" role do not have unique visible labels

```html
<body class="error404 wsu-has--mobile-nav wsu-template--">
```

**All affected pages** (4 total):

- `/contact-us` — 1 occurrence(s)
- `/cost-of-attendance` — 1 occurrence(s)
- `/how-to-apply` — 1 occurrence(s)
- `/types-of-aid` — 1 occurrence(s)

---

### 🟡 `link-pdf` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 4

**Sample violation:**

- Page: `/forms`
- Selector: ``
- Message: Link points to a PDF — verify accessibility of linked document

```html
<a href="https://wpcdn.web.wsu.edu/wp-financialaid/uploads/sites/2322/2025/08/25-26-Consortium-Agreement-Form.pdf" target="_blank" rel="noreferrer noopener">
```

**All affected pages** (4 total):

- `/forms` — 4 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
