# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 15:19:36  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\ChatWithAI  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\ChatWithAI\Docs\showcase\runs\latest\localhost`  

## Summary

- **38** pages scanned
- **484** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **484** real violations remaining
- **10** distinct rule failures
- **7** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 38 scanned pages. **Overall pass rate: 0.0%** (0 of 234 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 234 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **81** routes. We scanned **38** pages (38 of 38 static routes = **47% static coverage**).

### Parameterized routes that need IDs (43)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/Settings/EditDepartment/{departmentid}`
- `/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/Settings/EditTenant/{tenantid}`
- `/Settings/EditUser/{userid}`
- `/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}`
- `/{TenantCode}/About`
- `/{TenantCode}/Authorization/AccessDenied`
- `/{TenantCode}/Authorization/InvalidUser`
- `/{TenantCode}/Authorization/NoLocalAccout`
- `/{TenantCode}/ChangePassword`
- `/{TenantCode}/DoubleClick`
- `/{TenantCode}/Login`
- `/{TenantCode}/Logout`
- `/{TenantCode}/Monaco`
- `/{TenantCode}/PasswordChanged`
- `/{TenantCode}/Plugins`
- `/{TenantCode}/ProcessLogin`
- `/{TenantCode}/Profile`
- `/{TenantCode}/ServerUpdated`
- `/{TenantCode}/Settings`
- `/{TenantCode}/Settings/AddDepartment`
- `/{TenantCode}/Settings/AddDepartmentGroup`
- `/{TenantCode}/Settings/AddTenant`
- `/{TenantCode}/Settings/AddUser`
- `/{TenantCode}/Settings/AddUserGroup`
- `/{TenantCode}/Settings/AppSettings`
- `/{TenantCode}/Settings/DeletedRecords`
- `/{TenantCode}/Settings/DepartmentGroups`
- `/{TenantCode}/Settings/Departments`
- `/{TenantCode}/Settings/EditDepartment/{departmentid}`
- `/{TenantCode}/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/{TenantCode}/Settings/EditTenant/{tenantid}`
- `/{TenantCode}/Settings/EditUser/{userid}`
- `/{TenantCode}/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}/Settings/Files`
- `/{TenantCode}/Settings/Language`
- `/{TenantCode}/Settings/Tenants`
- `/{TenantCode}/Settings/UDF`
- `/{TenantCode}/Settings/UserGroups`
- `/{TenantCode}/Settings/Users`
- `/{TenantCode}/SortTest`
- `/{TenantCode}/TimerTest`

Example `seed-ids.json`:
```json
{
  "/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/About": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/AccessDenied": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/InvalidUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/NoLocalAccout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ChangePassword": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/DoubleClick": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Login": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Logout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Monaco": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/PasswordChanged": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Plugins": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ProcessLogin": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Profile": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ServerUpdated": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddDepartment": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddDepartmentGroup": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddTenant": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUserGroup": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AppSettings": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/DeletedRecords": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/DepartmentGroups": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Departments": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Files": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Language": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Tenants": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/UDF": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/UserGroups": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Users": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/SortTest": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/TimerTest": ["REPLACE-WITH-REAL-ID"]
}
```

Run with: `crawl --url ... --seed-ids seed-ids.json`

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `image-alt` (axe) | 🔴 critical | 11/38 | 11 | shared component |
| 2 | `link-name` (axe) | 🟠 serious | 38/38 | 76 | site-wide (layout) |
| 3 | `input_label_exists` (ibm) | 🟠 serious | 38/38 | 76 | site-wide (layout) |
| 4 | `img-alt` (htmlcheck) | 🟠 serious | 11/38 | 11 | shared component |
| 5 | `img_alt_valid` (ibm) | 🟠 serious | 11/38 | 11 | shared component |
| 6 | `heading-empty` (htmlcheck) | 🟡 moderate | 38/38 | 76 | site-wide (layout) |
| 7 | `skip-link` (htmlcheck) | 🟡 moderate | 38/38 | 38 | site-wide (layout) |
| 8 | `landmark-main` (htmlcheck) | 🟡 moderate | 38/38 | 38 | site-wide (layout) |
| 9 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 37/38 | 120 | site-wide (layout) |
| 10 | `aria_child_valid` (ibm) | 🟡 moderate | 27/38 | 27 | site-wide (layout) |

## Per-rule fix instructions

### 🔴 `image-alt` (axe) — CRITICAL

- **Pages affected:** 11 of 38
- **Total occurrences:** 11
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
<img class="user-menu-icon" src="http://localhost:5100/File/View/8517a412-eb39-46ed-9ada-a5ebf6309bcc">
```

**Likely source location(s)** (highest confidence first):

- `FreeAI.Client/Shared/NavigationMenu.razor:136` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `FreeAI.Client/Helpers.cs:5467` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `Docs/showcase/runs/latest/localhost/About/page.html:462` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**

**All affected pages** (11 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)

---

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 38 of 38
- **Total occurrences:** 76
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

**All affected pages** (76 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 38 of 38
- **Total occurrences:** 76

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**All affected pages** (76 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟠 `img-alt` (htmlcheck) — SERIOUS

- **Pages affected:** 11 of 38
- **Total occurrences:** 11

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Image missing alt attribute

```html
<img class="user-menu-icon" src="http://localhost:5100/File/View/8517a412-eb39-46ed-9ada-a5ebf6309bcc">
```

**Likely source location(s)** (highest confidence first):

- `FreeAI.Client/Shared/NavigationMenu.razor:136` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `FreeAI.Client/Helpers.cs:5467` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `Docs/showcase/runs/latest/localhost/About/page.html:462` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**

**All affected pages** (11 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)

---

### 🟠 `img_alt_valid` (ibm) — SERIOUS

- **Pages affected:** 11 of 38
- **Total occurrences:** 11

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[2]/a[1]/img[1]`
- Message: The image has neither an accessible name nor is marked as decorative or redundant

```html
<img src="http://localhost:5100/File/View/8517a412-eb39-46ed-9ada-a5ebf6309bcc" class="user-menu-icon">
```

**Likely source location(s)** (highest confidence first):

- `FreeAI.Client/Shared/NavigationMenu.razor:136` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `FreeAI.Client/Helpers.cs:5467` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**
- `Docs/showcase/runs/latest/localhost/About/page.html:462` — matched on class:'user-menu-icon', 13 hit(s) — confidence: **low**

**All affected pages** (11 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 38 of 38
- **Total occurrences:** 76

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**Likely source location(s)** (highest confidence first):

- `FreeAI.Client/Shared/OffcanvasPopoutMenu.razor:4` — matched on id:'offcanvasQuickActionLabel', 41 hit(s) — confidence: **low**
- `FreeAI.Client/Shared/OffcanvasPopoutMenu.razor:6` — matched on id:'offcanvasQuickActionLabel', 41 hit(s) — confidence: **low**
- `Docs/showcase/runs/2026-05-14/localhost/_root/page.html:440` — matched on id:'offcanvasQuickActionLabel', 41 hit(s) — confidence: **low**

**All affected pages** (76 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 38 of 38
- **Total occurrences:** 38
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (38 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/Monaco` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTenant` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/SortTest` — 1 occurrence(s)
- `/TimerTest` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 38 of 38
- **Total occurrences:** 38

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (38 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/Monaco` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTenant` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/SortTest` — 1 occurrence(s)
- `/TimerTest` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 37 of 38
- **Total occurrences:** 120

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

**All affected pages** (120 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 6 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 4 occurrence(s)
- `/MissingTenantCode` — 4 occurrence(s)
- `/Monaco` — 4 occurrence(s)
- `/PasswordChanged` — 4 occurrence(s)
- `/Plugins` — 4 occurrence(s)
- `/Profile` — 4 occurrence(s)
- `/ServerUpdated` — 4 occurrence(s)
- `/Settings` — 4 occurrence(s)
- `/Settings/AddDepartment` — 4 occurrence(s)
- `/Settings/AddDepartmentGroup` — 4 occurrence(s)
- `/Settings/AddTenant` — 4 occurrence(s)
- `/Settings/AddUser` — 4 occurrence(s)
- `/Settings/AddUserGroup` — 4 occurrence(s)
- `/Settings/AppSettings` — 4 occurrence(s)
- `/Settings/DeletedRecords` — 4 occurrence(s)
- `/Settings/DepartmentGroups` — 4 occurrence(s)
- `/Settings/Departments` — 4 occurrence(s)
- `/Settings/Files` — 4 occurrence(s)
- `/Settings/Language` — 4 occurrence(s)
- `/Settings/Tenants` — 4 occurrence(s)
- `/Settings/UDF` — 4 occurrence(s)
- `/Settings/UserGroups` — 4 occurrence(s)
- `/Settings/Users` — 4 occurrence(s)
- `/Setup` — 4 occurrence(s)
- `/SortTest` — 4 occurrence(s)
- `/TimerTest` — 4 occurrence(s)

---

### 🟡 `aria_child_valid` (ibm) — MODERATE

- **Pages affected:** 27 of 38
- **Total occurrences:** 27

**Sample violation:**

- Page: `/Logout`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/ul[1]`
- Message: The element with role "list" does not own any child element with any of the following role(s): "listitem"

```html
<ul class="navbar-nav me-auto">
```

**All affected pages** (27 total):

- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/Monaco` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTenant` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
- `/Settings/AppSettings` — 1 occurrence(s)
- `/Settings/DeletedRecords` — 1 occurrence(s)
- `/Settings/DepartmentGroups` — 1 occurrence(s)
- `/Settings/Departments` — 1 occurrence(s)
- `/Settings/Files` — 1 occurrence(s)
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/SortTest` — 1 occurrence(s)
- `/TimerTest` — 1 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
