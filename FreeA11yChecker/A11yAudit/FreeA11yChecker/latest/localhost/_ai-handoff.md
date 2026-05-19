# Accessibility Fix Pack — localhost

**Generated:** 2026-05-19 14:11:08  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\FreeA11yChecker\localhost`  

## Summary

- **19** pages scanned
- **184** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **184** real violations remaining
- **20** distinct rule failures
- **6** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 19 scanned pages. **Overall pass rate: 0.0%** (0 of 88 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 88 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `image-alt` (axe) | 🔴 critical | 19/19 | 20 | site-wide (layout) |
| 2 | `select-name` (axe) | 🔴 critical | 2/19 | 3 | shared component |
| 3 | `aria-valid-attr-value` (axe) | 🔴 critical | 1/19 | 4 | single page |
| 4 | `button-name` (axe) | 🔴 critical | 1/19 | 2 | single page |
| 5 | `input_label_exists` (ibm) | 🟠 serious | 19/19 | 24 | site-wide (layout) |
| 6 | `img-alt` (htmlcheck) | 🟠 serious | 19/19 | 20 | site-wide (layout) |
| 7 | `img_alt_valid` (ibm) | 🟠 serious | 19/19 | 20 | site-wide (layout) |
| 8 | `link-name` (axe) | 🟠 serious | 19/19 | 19 | site-wide (layout) |
| 9 | `text_contrast_sufficient` (ibm) | 🟠 serious | 2/19 | 8 | shared component |
| 10 | `color-contrast` (axe) | 🟠 serious | 2/19 | 7 | shared component |
| 11 | `label_ref_valid` (ibm) | 🟠 serious | 2/19 | 2 | shared component |
| 12 | `aria_id_unique` (ibm) | 🟠 serious | 1/19 | 4 | single page |
| 13 | `aria_navigation_label_unique` (ibm) | 🟠 serious | 1/19 | 3 | single page |
| 14 | `table_headers_exists` (ibm) | 🟠 serious | 1/19 | 2 | single page |
| 15 | `scrollable-region-focusable` (axe) | 🟠 serious | 1/19 | 1 | single page |
| 16 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 19/19 | 19 | site-wide (layout) |
| 17 | `heading-order` (htmlcheck) | 🟡 moderate | 9/19 | 18 | shared component |
| 18 | `aria_accessiblename_exists` (ibm) | 🟡 moderate | 3/19 | 4 | shared component |
| 19 | `table-header` (htmlcheck) | 🟡 moderate | 1/19 | 2 | single page |
| 20 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 1/19 | 2 | single page |

## Per-rule fix instructions

### 🔴 `image-alt` (axe) — CRITICAL

- **Pages affected:** 19 of 19
- **Total occurrences:** 20
- **How to fix:** Add a meaningful `alt` attribute. Use `alt=""` for decorative images.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/image-alt?application=axeAPI>

**Sample violation:**

- Page: `/`
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

**All affected pages** (20 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 2 occurrence(s)
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

### 🔴 `select-name` (axe) — CRITICAL

- **Pages affected:** 2 of 19
- **Total occurrences:** 3
- **How to fix:** Give the `<select>` an accessible name via `<label>` or `aria-label`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/select-name?application=axeAPI>

**Sample violation:**

- Page: `/Settings`
- Selector: `#allowUsersToManageBasicProfileInfoElements`
- Message: Fix any of the following:
  Element does not have an implicit (wrapped) <label>
  Element does not have an explicit <label>
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute
  Element's default semantics were not overridden with role="none" or role="presentation"

```html
<select id="allowUsersToManageBasicProfileInfoElements" class="form-select" multiple="" size="8">
```

**All affected pages** (3 total):

- `/Settings` — 1 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)

---

### 🔴 `aria-valid-attr-value` (axe) — CRITICAL

- **Pages affected:** 1 of 19
- **Total occurrences:** 4
- **How to fix:** Set the `aria-*` attribute to a value allowed by the spec.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/aria-valid-attr-value?application=axeAPI>

**Sample violation:**

- Page: `/Settings`
- Selector: `#tabGeneralButton`
- Message: Fix all of the following:
  Invalid ARIA attribute value: aria-controls="home"

```html
<button class="nav-link active" id="tabGeneralButton" data-bs-toggle="tab" data-bs-target="#tabGeneral" type="button" role="tab" aria-controls="home" aria-selected="true"><!--!--><i class=""><!--!-->General</i></button>
```

**All affected pages** (4 total):

- `/Settings` — 4 occurrence(s)

---

### 🔴 `button-name` (axe) — CRITICAL

- **Pages affected:** 1 of 19
- **Total occurrences:** 2
- **How to fix:** Add visible text or `aria-label` to the button.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/button-name?application=axeAPI>

**Sample violation:**

- Page: `/Settings/Users`
- Selector: `.data-element-enabled > .btn-dark.btn-xs.nowrap`
- Message: Fix any of the following:
  Element does not have inner text that is visible to screen readers
  aria-label attribute does not exist or is empty
  aria-labelledby attribute does not exist, references elements that do not exist or references elements that are empty
  Element has no title attribute
  Element does not have an implicit (wrapped) <label>
  Element does not have an explicit <label>
  Element's default semantics were not overridden with role="none" or role="presentation"

```html
<button type="button" class="btn btn-xs nowrap btn-dark"><i><!--!--><i class="icon fa-solid fa-user-check"></i></i></button>
```

**All affected pages** (2 total):

- `/Settings/Users` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 24

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[2]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-controls="offcanvasUserMenu" role="button" href="#offcanvasUserMenu" data-bs-toggle="offcanvas" class="nav-link">
```

**All affected pages** (24 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Settings` — 2 occurrence(s)
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
- `/Settings/Users` — 5 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 20

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img class="user-menu-icon" src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a">
```

**All affected pages** (20 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 2 occurrence(s)
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

### 🟠 `img_alt_valid` (ibm) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 20

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[2]/a[1]/img[1]`
- Message: The image has neither an accessible name nor is marked as decorative or redundant

```html
<img src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a" class="user-menu-icon">
```

**All affected pages** (20 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/Profile` — 2 occurrence(s)
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

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 19 of 19
- **Total occurrences:** 19
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
<a class="nav-link" data-bs-toggle="offcanvas" href="#offcanvasUserMenu" role="button" aria-controls="offcanvasUserMenu"><img class="user-menu-icon" src="http://localhost:5111/File/View/c77bf246-5a9c-4a06-a538-632eae2be15a"></a>
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

### 🟠 `text_contrast_sufficient` (ibm) — SERIOUS

- **Pages affected:** 2 of 19
- **Total occurrences:** 8

**Sample violation:**

- Page: `/Settings/AppSettings`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/div[3]/div[1]/div[1]/i[1]`
- Message: Text contrast of 3.65 with its background is less than the WCAG AA minimum requirements for text of size 12.8px and weight of 400

```html
<i class="">
```

**All affected pages** (8 total):

- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/Users` — 7 occurrence(s)

---

### 🟠 `color-contrast` (axe) — SERIOUS

- **Pages affected:** 2 of 19
- **Total occurrences:** 7
- **How to fix:** Increase color contrast. Need 4.5:1 for body text, 3:1 for large text.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI>

**Sample violation:**

- Page: `/Settings/AppSettings`
- Selector: `.alert-dark > .form-check.form-switch.mb-2 > .note > i`
- Message: Fix any of the following:
  Element has insufficient color contrast of 3.65 (foreground color: #646a71, background color: #ced4da, font size: 9.6pt (12.8px), font weight: normal). Expected contrast ratio of 4.5:1

```html
<i class=""><!--!-->When the application is in maintenance mode only application admin users can interact with the application. All other users will see the maintenance mode message.</i>
```

**All affected pages** (7 total):

- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/Users` — 6 occurrence(s)

---

### 🟠 `label_ref_valid` (ibm) — SERIOUS

- **Pages affected:** 2 of 19
- **Total occurrences:** 2

**Sample violation:**

- Page: `/Profile`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/div[2]/div[2]/div[5]/label[1]`
- Message: The value "edit-profile-phone" of the 'for' attribute is not the 'id' of a valid element

```html
<label for="edit-profile-phone">
```

**All affected pages** (2 total):

- `/Profile` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)

---

### 🟠 `aria_id_unique` (ibm) — SERIOUS

- **Pages affected:** 1 of 19
- **Total occurrences:** 4

**Sample violation:**

- Page: `/Settings`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/div[1]/ul[1]/li[1]/button[1]`
- Message: The 'id' "home" specified for the ARIA property 'aria-controls' value is not valid

```html
<button aria-selected="true" aria-controls="home" role="tab" type="button" data-bs-target="#tabGeneral" data-bs-toggle="tab" id="tabGeneralButton" class="nav-link active">
```

**All affected pages** (4 total):

- `/Settings` — 4 occurrence(s)

---

### 🟠 `aria_navigation_label_unique` (ibm) — SERIOUS

- **Pages affected:** 1 of 19
- **Total occurrences:** 3

**Sample violation:**

- Page: `/Settings/Users`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]`
- Message: Multiple elements with "navigation" role do not have unique labels

```html
<nav class="navbar fixed-top navbar-expand-lg navbar-expand-md border-bottom box-shadow mb-3">
```

**All affected pages** (3 total):

- `/Settings/Users` — 3 occurrence(s)

---

### 🟠 `table_headers_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 19
- **Total occurrences:** 2

**Sample violation:**

- Page: `/Settings/Users`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/table[1]`
- Message: Table has no headers identified

```html
<table class="paged-recordset-navigation paged-recordset-margin-bottom">
```

**All affected pages** (2 total):

- `/Settings/Users` — 2 occurrence(s)

---

### 🟠 `scrollable-region-focusable` (axe) — SERIOUS

- **Pages affected:** 1 of 19
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

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 19 of 19
- **Total occurrences:** 19

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a href="#main-content" class="visually-hidden-focusable">
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

### 🟡 `heading-order` (htmlcheck) — MODERATE

- **Pages affected:** 9 of 19
- **Total occurrences:** 18
- **How to fix:** Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading level skipped: h1 to h3

```html

```

**All affected pages** (18 total):

- `/` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Tags` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)

---

### 🟡 `aria_accessiblename_exists` (ibm) — MODERATE

- **Pages affected:** 3 of 19
- **Total occurrences:** 4

**Sample violation:**

- Page: `/Settings/Language`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/table[1]/thead[1]/tr[1]/th[1]`
- Message: Element <th> with "columnheader" role has no accessible name

```html
<th style="width:1%;">
```

**All affected pages** (4 total):

- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)

---

### 🟡 `table-header` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 19
- **Total occurrences:** 2

**Sample violation:**

- Page: `/Settings/Users`
- Selector: ``
- Message: Data table is missing header cells (<th>)

```html
<table class="paged-recordset-navigation paged-recordset-margin-bottom"><tbody><tr><td class="paged-recordset-left"><span>Showing All 3 Records</span><!--!-->
                        <select class="records-per-page form-select"><option value="1">1</option><option value="5">5</option><option value="10" selected="">10</option><option value="15">15</option><option value="20">20</option><option val...
```

**All affected pages** (2 total):

- `/Settings/Users` — 2 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 1 of 19
- **Total occurrences:** 2

**Sample violation:**

- Page: `/Settings/Users`
- Selector: `/html[1]/body[1]/div[1]/div[3]/main[1]/div[1]/table[1]/tbody[1]/tr[1]/td[3]/nav[1]`
- Message: Multiple elements with "navigation" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<nav>
```

**All affected pages** (2 total):

- `/Settings/Users` — 2 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
