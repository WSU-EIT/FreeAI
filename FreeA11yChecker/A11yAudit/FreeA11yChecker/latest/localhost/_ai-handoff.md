# Accessibility Fix Pack — localhost

**Generated:** 2026-05-19 17:30:02  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\FreeA11yChecker\localhost`  

## Summary

- **41** pages scanned
- **129** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **129** real violations remaining
- **8** distinct rule failures
- **3** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 41 scanned pages. **Overall pass rate: 0.0%** (0 of 83 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 83 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `image-alt` (axe) | 🔴 critical | 1/41 | 1 | single page |
| 2 | `link-name` (axe) | 🟠 serious | 41/41 | 41 | site-wide (layout) |
| 3 | `input_label_exists` (ibm) | 🟠 serious | 41/41 | 41 | site-wide (layout) |
| 4 | `scrollable-region-focusable` (axe) | 🟠 serious | 1/41 | 1 | single page |
| 5 | `img-alt` (htmlcheck) | 🟠 serious | 1/41 | 1 | single page |
| 6 | `img_alt_valid` (ibm) | 🟠 serious | 1/41 | 1 | single page |
| 7 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 41/41 | 41 | site-wide (layout) |
| 8 | `heading-order` (htmlcheck) | 🟡 moderate | 1/41 | 2 | single page |

## Per-rule fix instructions

### 🔴 `image-alt` (axe) — CRITICAL

- **Pages affected:** 1 of 41
- **Total occurrences:** 1
- **How to fix:** Add a meaningful `alt` attribute. Use `alt=""` for decorative images.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/image-alt?application=axeAPI>

**Sample violation:**

- Page: `/Plugins`
- Selector: `img`
- Message: Fix any of the following:
  Element does not have an alt attribute
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute
  Element's default semantics were not overridden with role="none" or role="presentation"

```html
<img class="user-menu-icon" src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a">
```

---

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 41 of 41
- **Total occurrences:** 41
- **How to fix:** Add visible text or `aria-label` to the link.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `a[data-bs-toggle="offcanvas"]`
- Message: Fix all of the following:
  Element is in tab order and does not have accessible text

Fix any of the following:
  Element does not have text that is visible to screen readers
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute

```html
<a class="nav-link" data-bs-toggle="offcanvas" href="#offcanvasUserMenu" role="button" aria-controls="offcanvasUserMenu"><!--!--><i class="icon icon-fa fa-solid fa-circle-info"></i></a>
```

**All affected pages** (41 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTag` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/EditTenant/00000000-0000-0000-0000-000000000002` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 41 of 41
- **Total occurrences:** 41

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[2]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-controls="offcanvasUserMenu" role="button" href="#offcanvasUserMenu" data-bs-toggle="offcanvas" class="nav-link">
```

**All affected pages** (41 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTag` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/EditTenant/00000000-0000-0000-0000-000000000002` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `scrollable-region-focusable` (axe) — SERIOUS

- **Pages affected:** 1 of 41
- **Total occurrences:** 1
- **How to fix:** Scrollable regions must be keyboard focusable (e.g., `tabindex="0"`).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/scrollable-region-focusable?application=axeAPI>

**Sample violation:**

- Page: `/About`
- Selector: `#main-content`
- Message: Fix any of the following:
  Element should have focusable content
  Element should be focusable

```html
<main role="main" id="main-content">
```

---

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 1 of 41
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Plugins`
- Selector: ``
- Message: Image missing alt attribute

```html
<img class="user-menu-icon" src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a">
```

---

### 🟠 `img_alt_valid` (ibm) — SERIOUS

- **Pages affected:** 1 of 41
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Plugins`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[2]/a[1]/img[1]`
- Message: The image has neither an accessible name nor is marked as decorative or redundant

```html
<img src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a" class="user-menu-icon">
```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 41 of 41
- **Total occurrences:** 41

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#main-content" class="visually-hidden-focusable">
```

**All affected pages** (41 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTag` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/EditTenant/00000000-0000-0000-0000-000000000002` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `heading-order` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 41
- **Total occurrences:** 2
- **How to fix:** Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.

**Sample violation:**

- Page: `/Plugins`
- Selector: ``
- Message: Heading level skipped: h1 to h3

```html

```

**All affected pages** (2 total):

- `/Plugins` — 2 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
