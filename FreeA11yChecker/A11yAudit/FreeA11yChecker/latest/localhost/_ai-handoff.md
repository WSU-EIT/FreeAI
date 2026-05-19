# Accessibility Fix Pack — localhost

**Generated:** 2026-05-19 10:27:28  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `c:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeA11yChecker\A11yAudit\FreeA11yChecker\latest\localhost`  

## Summary

- **19** pages scanned
- **133** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **133** real violations remaining
- **7** distinct rule failures
- **7** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 19 scanned pages. **Overall pass rate: 0.0%** (0 of 38 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 38 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `document-title` (axe) | 🟠 serious | 19/19 | 19 | site-wide (layout) |
| 2 | `title-missing` (htmlcheck) | 🟠 serious | 19/19 | 19 | site-wide (layout) |
| 3 | `page_title_exists` (ibm) | 🟠 serious | 19/19 | 19 | site-wide (layout) |
| 4 | `skip_main_exists` (ibm) | 🟠 serious | 19/19 | 19 | site-wide (layout) |
| 5 | `skip-link` (htmlcheck) | 🟡 moderate | 19/19 | 19 | site-wide (layout) |
| 6 | `landmark-main` (htmlcheck) | 🟡 moderate | 19/19 | 19 | site-wide (layout) |
| 7 | `landmark-nav` (htmlcheck) | 🟡 moderate | 19/19 | 19 | site-wide (layout) |

## Per-rule fix instructions

### 🟠 `document-title` (axe) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 19
- **How to fix:** Add a descriptive, non-empty `<title>` in the `<head>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `html`
- Message: Fix any of the following:
  Document does not have a non-empty <title> element

```html
<html lang="en">
```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `title-missing` (htmlcheck) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Page is missing a <title> element

```html

```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `page_title_exists` (ibm) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: `/html[1]`
- Message: Missing <title> element in <head> element

```html
<html lang="en">
```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body class="" data-bs-theme="" id="body-element">
```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 19 of 19
- **Total occurrences:** 19
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <nav> landmark found

```html

```

**All affected pages** (19 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
