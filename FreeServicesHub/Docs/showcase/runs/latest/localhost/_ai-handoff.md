# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 16:46:01  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeServicesHub  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeServicesHub\Docs\showcase\runs\latest\localhost`  

## Summary

- **3** pages scanned
- **31** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **31** real violations remaining
- **11** distinct rule failures
- **2** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 3 scanned pages. **Overall pass rate: 0.0%** (0 of 14 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 14 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **90** routes. We scanned **3** pages (3 of 41 static routes = **3% static coverage**).

### Static routes we MISSED (38)

These routes exist in the source but weren't scanned. Re-run with `--pages` to add them, or fix link discovery so they're auto-found:

- `/`
- `/About`
- `/AgentDashboard`
- `/AgentManagement`
- `/AgentSettings`
- `/Authorization/AccessDenied`
- `/Authorization/InvalidUser`
- `/Authorization/NoLocalAccount`
- `/BackgroundServices`
- `/ChangePassword`
- `/DatabaseOffline`
- `/Error`
- `/InvalidTenantCode`
- `/Login`
- `/Logout`
- `/MissingTenantCode`
- `/not-found`
- `/PasswordChanged`
- `/ProcessLogin`
- `/Profile`
- `/ServerUpdated`
- `/Settings`
- `/Settings/AddDepartment`
- `/Settings/AddDepartmentGroup`
- `/Settings/AddTag`
- `/Settings/AddTenant`
- `/Settings/AddUser`
- `/Settings/AddUserGroup`
- `/Settings/ApiKeys`
- `/Settings/AppSettings`
- `/Settings/DeletedRecords`
- `/Settings/DepartmentGroups`
- `/Settings/Departments`
- `/Settings/Files`
- `/Settings/Language`
- `/Settings/Tags`
- `/Settings/Tenants`
- `/Settings/UDF`

Quick re-run command:
```
--pages "/,/About,/AgentDashboard,/AgentManagement,/AgentSettings,/Authorization/AccessDenied,/Authorization/InvalidUser,/Authorization/NoLocalAccount,/BackgroundServices,/ChangePassword,/DatabaseOffline,/Error,/InvalidTenantCode,/Login,/Logout,/MissingTenantCode,/not-found,/PasswordChanged,/ProcessLogin,/Profile,/ServerUpdated,/Settings,/Settings/AddDepartment,/Settings/AddDepartmentGroup,/Settings/AddTag,/Settings/AddTenant,/Settings/AddUser,/Settings/AddUserGroup,/Settings/ApiKeys,/Settings/AppSettings,/Settings/DeletedRecords,/Settings/DepartmentGroups,/Settings/Departments,/Settings/Files,/Settings/Language,/Settings/Tags,/Settings/Tenants,/Settings/UDF"
```

### Parameterized routes that need IDs (49)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/AgentDetail/{agentid}`
- `/Settings/EditDepartment/{departmentid}`
- `/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/Settings/EditTag/{id}`
- `/Settings/EditTenant/{tenantid}`
- `/Settings/EditUser/{userid}`
- `/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}`
- `/{TenantCode}/About`
- `/{TenantCode}/AgentDashboard`
- `/{TenantCode}/AgentDetail/{agentid}`
- `/{TenantCode}/AgentManagement`
- `/{TenantCode}/AgentSettings`
- `/{TenantCode}/Authorization/AccessDenied`
- `/{TenantCode}/Authorization/InvalidUser`
- `/{TenantCode}/Authorization/NoLocalAccout`
- `/{TenantCode}/BackgroundServices`
- `/{TenantCode}/ChangePassword`
- `/{TenantCode}/Login`
- `/{TenantCode}/Logout`
- `/{TenantCode}/PasswordChanged`
- `/{TenantCode}/ProcessLogin`
- `/{TenantCode}/Profile`
- `/{TenantCode}/ServerUpdated`
- `/{TenantCode}/Settings`
- `/{TenantCode}/Settings/AddDepartment`
- `/{TenantCode}/Settings/AddDepartmentGroup`
- `/{TenantCode}/Settings/AddTag`
- `/{TenantCode}/Settings/AddTenant`
- `/{TenantCode}/Settings/AddUser`
- `/{TenantCode}/Settings/AddUserGroup`
- `/{TenantCode}/Settings/ApiKeys`
- `/{TenantCode}/Settings/AppSettings`
- `/{TenantCode}/Settings/DeletedRecords`
- `/{TenantCode}/Settings/DepartmentGroups`
- `/{TenantCode}/Settings/Departments`
- `/{TenantCode}/Settings/EditDepartment/{departmentid}`
- `/{TenantCode}/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/{TenantCode}/Settings/EditTag/{id}`
- `/{TenantCode}/Settings/EditTenant/{tenantid}`
- `/{TenantCode}/Settings/EditUser/{userid}`
- `/{TenantCode}/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}/Settings/Files`
- `/{TenantCode}/Settings/Language`
- `/{TenantCode}/Settings/Tags`
- `/{TenantCode}/Settings/Tenants`
- `/{TenantCode}/Settings/UDF`
- `/{TenantCode}/Settings/UserGroups`
- `/{TenantCode}/Settings/Users`

Example `seed-ids.json`:
```json
{
  "/AgentDetail/{agentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/About": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AgentDashboard": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AgentDetail/{agentid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AgentManagement": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AgentSettings": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/AccessDenied": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/InvalidUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/NoLocalAccout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/BackgroundServices": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ChangePassword": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Login": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Logout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/PasswordChanged": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ProcessLogin": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Profile": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ServerUpdated": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddDepartment": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddDepartmentGroup": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddTag": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddTenant": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUserGroup": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/ApiKeys": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AppSettings": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/DeletedRecords": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/DepartmentGroups": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Departments": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Files": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Language": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Tags": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Tenants": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/UDF": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/UserGroups": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Users": ["REPLACE-WITH-REAL-ID"]
}
```

Run with: `crawl --url ... --seed-ids seed-ids.json`

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `link-name` (axe) | 🟠 serious | 2/3 | 4 | shared component |
| 2 | `input_label_exists` (ibm) | 🟠 serious | 2/3 | 4 | shared component |
| 3 | `document-title` (axe) | 🟠 serious | 1/3 | 1 | single page |
| 4 | `title-missing` (htmlcheck) | 🟠 serious | 1/3 | 1 | single page |
| 5 | `page_title_exists` (ibm) | 🟠 serious | 1/3 | 1 | single page |
| 6 | `skip_main_exists` (ibm) | 🟠 serious | 1/3 | 1 | single page |
| 7 | `skip-link` (htmlcheck) | 🟡 moderate | 3/3 | 3 | site-wide (layout) |
| 8 | `landmark-main` (htmlcheck) | 🟡 moderate | 3/3 | 3 | site-wide (layout) |
| 9 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 2/3 | 8 | shared component |
| 10 | `heading-empty` (htmlcheck) | 🟡 moderate | 2/3 | 4 | shared component |
| 11 | `landmark-nav` (htmlcheck) | 🟡 moderate | 1/3 | 1 | single page |

## Per-rule fix instructions

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 2 of 3
- **Total occurrences:** 4
- **How to fix:** Add visible text or `aria-label` to the link.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/link-name?application=axeAPI>

**Sample violation:**

- Page: `/Settings/UserGroups`
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

**Likely source location(s)** (highest confidence first):

- `FreeServicesHub/FreeServicesHub.Client/Shared/NavigationMenu.razor:123` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**
- `FreeServicesHub/FreeServicesHub.Client/Shared/NavigationMenu.razor:138` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**
- `Docs/showcase/runs/latest/localhost/_crawl-summary.html:890` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**

**All affected pages** (4 total):

- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 2 of 3
- **Total occurrences:** 4

**Sample violation:**

- Page: `/Settings/UserGroups`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**Likely source location(s)** (highest confidence first):

- `FreeServicesHub/FreeServicesHub.Client/Shared/NavigationMenu.razor:123` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**
- `FreeServicesHub/FreeServicesHub.Client/Shared/NavigationMenu.razor:138` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**
- `Docs/showcase/runs/latest/localhost/_crawl-summary.html:890` — matched on id:'themeDropdown', 10 hit(s) — confidence: **medium**

**All affected pages** (4 total):

- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)

---

### 🟠 `document-title` (axe) — SERIOUS

- **Pages affected:** 1 of 3
- **Total occurrences:** 1
- **How to fix:** Add a descriptive, non-empty `<title>` in the `<head>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI>

**Sample violation:**

- Page: `/Setup`
- Selector: `html`
- Message: Fix any of the following:
  Document does not have a non-empty <title> element

```html
<html lang="en" style="--blazor-load-percentage: 97.6470588235294%; --blazor-load-percentage-text: &quot;97%&quot;;">
```

---

### 🟠 `title-missing` (htmlcheck) — SERIOUS

- **Pages affected:** 1 of 3
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Setup`
- Selector: ``
- Message: Page is missing a <title> element

```html

```

---

### 🟠 `page_title_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 3
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Setup`
- Selector: `/html[1]`
- Message: Missing <title> element in <head> element

```html
<html style="--blazor-load-percentage: 97.6470588235294%; --blazor-load-percentage-text: "97%";" lang="en">
```

---

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 3
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Setup`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body class="" data-bs-theme="" id="body-element">
```

**Likely source location(s)** (highest confidence first):

- `FreeServicesHub/FreeServicesHub/Components/App.razor:63` — matched on id:'body-element', 20 hit(s) — confidence: **low**
- `FreeServicesHub/FreeServicesHub/Components/App.razor:314` — matched on id:'body-element', 20 hit(s) — confidence: **low**
- `FreeServicesHub/FreeServicesHub/Components/App.razor:370` — matched on id:'body-element', 20 hit(s) — confidence: **low**

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 3 of 3
- **Total occurrences:** 3
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/Settings/UserGroups`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (3 total):

- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 3 of 3
- **Total occurrences:** 3

**Sample violation:**

- Page: `/Settings/UserGroups`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (3 total):

- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 2 of 3
- **Total occurrences:** 8

**Sample violation:**

- Page: `/Settings/UserGroups`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[2]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

**All affected pages** (8 total):

- `/Settings/UserGroups` — 4 occurrence(s)
- `/Settings/Users` — 4 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 2 of 3
- **Total occurrences:** 4

**Sample violation:**

- Page: `/Settings/UserGroups`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**Likely source location(s)** (highest confidence first):

- `FreeServicesHub/FreeServicesHub.Client/Shared/OffcanvasPopoutMenu.razor:4` — matched on id:'offcanvasQuickActionLabel', 5 hit(s) — confidence: **medium**
- `FreeServicesHub/FreeServicesHub.Client/Shared/OffcanvasPopoutMenu.razor:6` — matched on id:'offcanvasQuickActionLabel', 5 hit(s) — confidence: **medium**
- `Docs/showcase/runs/2026-05-14/localhost/_root/page.html:361` — matched on id:'offcanvasQuickActionLabel', 5 hit(s) — confidence: **medium**

**All affected pages** (4 total):

- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 3
- **Total occurrences:** 1

**Sample violation:**

- Page: `/Setup`
- Selector: ``
- Message: No <nav> landmark found

```html

```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
