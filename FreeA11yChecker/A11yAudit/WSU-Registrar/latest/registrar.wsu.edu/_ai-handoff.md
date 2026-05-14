# Accessibility Fix Pack — registrar.wsu.edu

**Generated:** 2026-05-13 10:23:16  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\FreeA11yChecker\A11yAudit\runs\WSU-Registrar\registrar.wsu.edu`  

## Summary

- **6** pages scanned
- **175** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **175** real violations remaining
- **22** distinct rule failures
- **3** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 6 scanned pages. **Overall pass rate: 0.0%** (0 of 35 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 35 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `aria-allowed-attr` (axe) | 🔴 critical | 5/6 | 5 | site-wide (layout) |
| 2 | `label` (axe) | 🔴 critical | 1/6 | 1 | single page |
| 3 | `label_name_visible` (ibm) | 🟠 serious | 5/6 | 5 | site-wide (layout) |
| 4 | `aria_banner_label_unique` (ibm) | 🟠 serious | 3/6 | 6 | shared component |
| 5 | `link-empty` (htmlcheck) | 🟠 serious | 2/6 | 85 | shared component |
| 6 | `link-in-text-block` (axe) | 🟠 serious | 2/6 | 31 | shared component |
| 7 | `listitem` (axe) | 🟠 serious | 1/6 | 8 | single page |
| 8 | `table_headers_exists` (ibm) | 🟠 serious | 1/6 | 3 | single page |
| 9 | `document-title` (axe) | 🟠 serious | 1/6 | 1 | single page |
| 10 | `title-missing` (htmlcheck) | 🟠 serious | 1/6 | 1 | single page |
| 11 | `page_title_exists` (ibm) | 🟠 serious | 1/6 | 1 | single page |
| 12 | `skip_main_exists` (ibm) | 🟠 serious | 1/6 | 1 | single page |
| 13 | `list` (axe) | 🟠 serious | 1/6 | 1 | single page |
| 14 | `input_label_exists` (ibm) | 🟠 serious | 1/6 | 1 | single page |
| 15 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 5/6 | 10 | site-wide (layout) |
| 16 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 3/6 | 6 | shared component |
| 17 | `table-header` (htmlcheck) | 🟡 moderate | 1/6 | 3 | single page |
| 18 | `aria_child_valid` (ibm) | 🟡 moderate | 1/6 | 2 | single page |
| 19 | `skip-link` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |
| 20 | `landmark-main` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |
| 21 | `landmark-nav` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |
| 22 | `label-missing` (htmlcheck) | 🟡 moderate | 1/6 | 1 | single page |

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
<div id="wsu-navigation-vertical" class="wsu-slide-in-panel  wsu-navigation-vertical wsu-slide-in-panel--position-left wsu-slide-in-panel--overlay-none wsu-slide-in-panel--position-left wsu-slide-in-panel--width-vertical-nav wsu-slide-in-panel--style-crimson-mark" aria-expanded="true" aria-haspopup="true" aria-label="Site menu">
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/academic-regulations` — 1 occurrence(s)
- `/commencement` — 1 occurrence(s)
- `/enrollment-verifications` — 1 occurrence(s)
- `/transcripts` — 1 occurrence(s)

---

### 🔴 `label` (axe) — CRITICAL

- **Pages affected:** 1 of 6
- **Total occurrences:** 1
- **How to fix:** Wrap the input in a `<label>` or add `for="..."` matching the input id.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/label?application=axeAPI>

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `#ROARSearch`
- Message: Fix any of the following:
  Element does not have an implicit (wrapped) <label>
  Element does not have an explicit <label>
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute
  Element has no placeholder attribute
  Element's default semantics were not overridden with role="none" or role="presentation"

```html
<input type="text" name="ROARSearch" id="ROARSearch" value="" style="">
```

---

### 🟠 `label_name_visible` (ibm) — SERIOUS

- **Pages affected:** 5 of 6
- **Total occurrences:** 5

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/div[1]/a[1]`
- Message: Accessible name does not match or contain the visible label text

```html
<a aria-label="Go to Washington State University Homepage" href="https://wsu.edu" class="wsu-wordmark">
```

**All affected pages** (5 total):

- `/` — 1 occurrence(s)
- `/academic-regulations` — 1 occurrence(s)
- `/commencement` — 1 occurrence(s)
- `/enrollment-verifications` — 1 occurrence(s)
- `/transcripts` — 1 occurrence(s)

---

### 🟠 `aria_banner_label_unique` (ibm) — SERIOUS

- **Pages affected:** 3 of 6
- **Total occurrences:** 6

**Sample violation:**

- Page: `/commencement`
- Selector: `/html[1]/body[1]/div[1]/header[1]`
- Message: Multiple elements with "banner" role do not have unique labels

```html
<header class="wsu-header-global">
```

**All affected pages** (6 total):

- `/commencement` — 2 occurrence(s)
- `/enrollment-verifications` — 2 occurrence(s)
- `/transcripts` — 2 occurrence(s)

---

### 🟠 `link-empty` (htmlcheck) — SERIOUS

- **Pages affected:** 2 of 6
- **Total occurrences:** 85

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Link has no text content

```html
<a href="mailto:official.transcripts@wsu.edu" title="Email Transcripts"> </a>
```

**All affected pages** (85 total):

- `/` — 1 occurrence(s)
- `/academic-regulations` — 84 occurrence(s)

---

### 🟠 `link-in-text-block` (axe) — SERIOUS

- **Pages affected:** 2 of 6
- **Total occurrences:** 31
- **How to fix:** Distinguish the link from surrounding text with more than color alone (e.g., underline).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-in-text-block?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `a[title="wsu.registration@wsu.edu"]`
- Message: Fix any of the following:
  The link has insufficient color contrast of 1.96:1 with the surrounding text. (Minimum contrast is 3:1, link text: #a60f2d, surrounding text: #262626)
  The link has no styling (such as underline) to distinguish it from the surrounding text

```html
<a href="mailto:wsu.registration@wsu.edu" title="wsu.registration@wsu.edu"> Email WSU Registration</a>
```

**All affected pages** (31 total):

- `/` — 8 occurrence(s)
- `/academic-regulations` — 23 occurrence(s)

---

### 🟠 `listitem` (axe) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 8
- **How to fix:** An `<li>` must be a direct child of `<ul>`, `<ol>`, or `<menu>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/listitem?application=axeAPI>

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `.ro_acad_reg_narrative:nth-child(139) > li:nth-child(4)`
- Message: Fix any of the following:
  List item does not have a <ul>, <ol> parent element

```html
<li>
```

**All affected pages** (8 total):

- `/academic-regulations` — 8 occurrence(s)

---

### 🟠 `table_headers_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 3

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/div[1]/div[1]/form[1]/div[2]/div[28]/table[1]`
- Message: Table has no headers identified

```html
<table style="border-collapse:collapse;border:none;mso-border-alt:solid windowtext .5pt;
 mso-yfti-tbllook:1184;mso-padding-alt:0in 5.4pt 0in 5.4pt;mso-border-insideh:
 .5pt solid windowtext;mso-border-insidev:.5pt solid windowtext" cellpadding="0" cellspacing="0" border="1" class="MsoNormalTable">
```

**All affected pages** (3 total):

- `/academic-regulations` — 3 occurrence(s)

---

### 🟠 `document-title` (axe) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1
- **How to fix:** Add a descriptive, non-empty `<title>` in the `<head>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI>

**Sample violation:**

- Page: `/academic-calendar`
- Selector: `html`
- Message: Fix any of the following:
  Document does not have a non-empty <title> element

```html
<html lang="en" class="wsu-has-js wsu-reduce-motion" style="--blazor-load-percentage: 64.34108527131784%; --blazor-load-percentage-text: &quot;64%&quot;;">
```

---

### 🟠 `title-missing` (htmlcheck) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-calendar`
- Selector: ``
- Message: Page is missing a <title> element

```html

```

---

### 🟠 `page_title_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-calendar`
- Selector: `/html[1]`
- Message: Missing <title> element in <head> element

```html
<html style="--blazor-load-percentage: 64.34108527131784%; --blazor-load-percentage-text: "64%";" class="wsu-has-js wsu-reduce-motion" lang="en">
```

---

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-calendar`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body class="light" data-bs-theme="" id="body-element">
```

---

### 🟠 `list` (axe) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1
- **How to fix:** List items must be inside `<ul>`, `<ol>`, or `<menu>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/list?application=axeAPI>

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `.ro_acad_reg_narrative:nth-child(95) > ol`
- Message: Fix all of the following:
  List element has direct children that are not allowed: p

```html
<ol>
```

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/div[1]/div[1]/form[1]/div[1]/input[1]`
- Message: Form control element <input> has no associated label

```html
<input style="" value="" id="ROARSearch" name="ROARSearch" type="text">
```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 5 of 6
- **Total occurrences:** 10

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#wsu-site-menu" class="wsu-skip-to-main">
```

**All affected pages** (10 total):

- `/` — 2 occurrence(s)
- `/academic-regulations` — 2 occurrence(s)
- `/commencement` — 2 occurrence(s)
- `/enrollment-verifications` — 2 occurrence(s)
- `/transcripts` — 2 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 3 of 6
- **Total occurrences:** 6

**Sample violation:**

- Page: `/commencement`
- Selector: `/html[1]/body[1]/div[1]/header[1]`
- Message: Multiple elements with "banner" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<header class="wsu-header-global">
```

**All affected pages** (6 total):

- `/commencement` — 2 occurrence(s)
- `/enrollment-verifications` — 2 occurrence(s)
- `/transcripts` — 2 occurrence(s)

---

### 🟡 `table-header` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 3

**Sample violation:**

- Page: `/academic-regulations`
- Selector: ``
- Message: Data table is missing header cells (<th>)

```html
<table class="MsoNormalTable" border="1" cellspacing="0" cellpadding="0" style="border-collapse:collapse;border:none;mso-border-alt:solid windowtext .5pt;
 mso-yfti-tbllook:1184;mso-padding-alt:0in 5.4pt 0in 5.4pt;mso-border-insideh:
 .5pt solid windowtext;mso-border-insidev:.5pt solid windowtext">
    <tbody>
        <tr style="mso-yfti-irow:0;mso-yfti-firstrow:yes">
            <td width="174...
```

**All affected pages** (3 total):

- `/academic-regulations` — 3 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 2

**Sample violation:**

- Page: `/academic-regulations`
- Selector: `/html[1]/body[1]/div[1]/div[2]/div[1]/main[1]/div[1]/div[1]/form[1]/div[2]/div[95]/ol[1]`
- Message: The element with role "list" owns the child element with the role "paragraph" that is not one of the allowed role(s): "listitem"

```html
<ol>
```

**All affected pages** (2 total):

- `/academic-regulations` — 2 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/academic-calendar`
- Selector: ``
- Message: No skip-to-content link found

```html

```

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-calendar`
- Selector: ``
- Message: No <main> landmark found

```html

```

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-calendar`
- Selector: ``
- Message: No <nav> landmark found

```html

```

---

### 🟡 `label-missing` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 6
- **Total occurrences:** 1

**Sample violation:**

- Page: `/academic-regulations`
- Selector: ``
- Message: Form input may be missing a label

```html
<input type="text" name="ROARSearch" id="ROARSearch" value="" style="">
```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
