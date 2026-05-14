# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 14:04:00  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeGLBA\Docs\showcase\runs\2026-05-14\localhost`  

## Summary

- **1** pages scanned
- **9** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **9** real violations remaining
- **6** distinct rule failures
- **6** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 1 scanned pages. **Overall pass rate: 0.0%** (0 of 3 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 3 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `link-name` (axe) | 🟠 serious | 1/1 | 2 | site-wide (layout) |
| 2 | `input_label_exists` (ibm) | 🟠 serious | 1/1 | 2 | site-wide (layout) |
| 3 | `heading-empty` (htmlcheck) | 🟡 moderate | 1/1 | 2 | site-wide (layout) |
| 4 | `skip-link` (htmlcheck) | 🟡 moderate | 1/1 | 1 | site-wide (layout) |
| 5 | `landmark-main` (htmlcheck) | 🟡 moderate | 1/1 | 1 | site-wide (layout) |
| 6 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 1/1 | 1 | site-wide (layout) |

## Per-rule fix instructions

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 1 of 1
- **Total occurrences:** 2
- **How to fix:** Add visible text or `aria-label` to the link.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `#themeDropdown`
- Message: Fix all of the following:
  Element is in tab order and does not have accessible text

Fix any of the following:
  Element does not have text that is visible to screen readers
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute

```html
<a class="nav-link dropdown-toggle" href="#" id="themeDropdown" role="button" data-bs-toggle="dropdown" aria-expanded="false"><!--!--><i class="icon icon-fa fa-regular fa-sun"></i></a>
```

**All affected pages** (2 total):

- `/` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 1
- **Total occurrences:** 2

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**All affected pages** (2 total):

- `/` — 2 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 1
- **Total occurrences:** 2

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**All affected pages** (2 total):

- `/` — 2 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 1
- **Total occurrences:** 1
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 1
- **Total occurrences:** 1

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 1 of 1
- **Total occurrences:** 1

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
