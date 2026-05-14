# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 16:12:45  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeManager  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeManager\Docs\showcase\runs\latest\localhost`  

## Summary

- **54** pages scanned
- **599** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **599** real violations remaining
- **6** distinct rule failures
- **6** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 54 scanned pages. **Overall pass rate: 0.0%** (0 of 275 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 275 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **111** routes. We scanned **54** pages (54 of 54 static routes = **49% static coverage**).

### Parameterized routes that need IDs (57)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/AppBuilder/Edit/{ProjectId:guid}`
- `/AppBuilder/SetupWizardBuilder/{WizardId:guid}`
- `/Settings/EditDepartment/{departmentid}`
- `/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/Settings/EditTag/{id}`
- `/Settings/EditTenant/{tenantid}`
- `/Settings/EditUser/{userid}`
- `/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}`
- `/{TenantCode}/About`
- `/{TenantCode}/AccessEvents`
- `/{TenantCode}/AppBuilder`
- `/{TenantCode}/AppBuilder/Edit/{ProjectId:guid}`
- `/{TenantCode}/AppBuilder/New`
- `/{TenantCode}/Authorization/AccessDenied`
- `/{TenantCode}/Authorization/InvalidUser`
- `/{TenantCode}/Authorization/NoLocalAccout`
- `/{TenantCode}/ChangePassword`
- `/{TenantCode}/ComplianceReports`
- `/{TenantCode}/DataSubjects`
- `/{TenantCode}/DoubleClick`
- `/{TenantCode}/GlbaDashboard`
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
- `/{TenantCode}/Settings/AddTag`
- `/{TenantCode}/Settings/AddTenant`
- `/{TenantCode}/Settings/AddUser`
- `/{TenantCode}/Settings/AddUserGroup`
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
- `/{TenantCode}/SortTest`
- `/{TenantCode}/SourceSystems`
- `/{TenantCode}/TimerTest`

Example `seed-ids.json`:
```json
{
  "/AppBuilder/Edit/{ProjectId:guid}": ["REPLACE-WITH-REAL-ID"],
  "/AppBuilder/SetupWizardBuilder/{WizardId:guid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/About": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AccessEvents": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AppBuilder": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AppBuilder/Edit/{ProjectId:guid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/AppBuilder/New": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/AccessDenied": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/InvalidUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/NoLocalAccout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ChangePassword": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ComplianceReports": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/DataSubjects": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/DoubleClick": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/GlbaDashboard": ["REPLACE-WITH-REAL-ID"],
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
  "/{TenantCode}/Settings/AddTag": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddTenant": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/AddUserGroup": ["REPLACE-WITH-REAL-ID"],
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
  "/{TenantCode}/Settings/Users": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/SortTest": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/SourceSystems": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/TimerTest": ["REPLACE-WITH-REAL-ID"]
}
```

Run with: `crawl --url ... --seed-ids seed-ids.json`

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `link-name` (axe) | 🟠 serious | 54/54 | 108 | site-wide (layout) |
| 2 | `input_label_exists` (ibm) | 🟠 serious | 54/54 | 108 | site-wide (layout) |
| 3 | `heading-empty` (htmlcheck) | 🟡 moderate | 54/54 | 108 | site-wide (layout) |
| 4 | `skip-link` (htmlcheck) | 🟡 moderate | 54/54 | 54 | site-wide (layout) |
| 5 | `landmark-main` (htmlcheck) | 🟡 moderate | 54/54 | 54 | site-wide (layout) |
| 6 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 53/54 | 167 | site-wide (layout) |

## Per-rule fix instructions

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 54 of 54
- **Total occurrences:** 108
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

**All affected pages** (108 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/AccessEvents` — 2 occurrence(s)
- `/AppBuilder` — 2 occurrence(s)
- `/AppBuilder/Dashboard` — 2 occurrence(s)
- `/AppBuilder/EntityWizard` — 2 occurrence(s)
- `/AppBuilder/New` — 2 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/ComplianceReports` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DataSubjects` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/fm/templates` — 2 occurrence(s)
- `/GlbaDashboard` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/not-found` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTag` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tags` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/SourceSystems` — 2 occurrence(s)
- `/Templates` — 2 occurrence(s)
- `/Templates/Setup` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 54 of 54
- **Total occurrences:** 108

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**All affected pages** (108 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/AccessEvents` — 2 occurrence(s)
- `/AppBuilder` — 2 occurrence(s)
- `/AppBuilder/Dashboard` — 2 occurrence(s)
- `/AppBuilder/EntityWizard` — 2 occurrence(s)
- `/AppBuilder/New` — 2 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/ComplianceReports` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DataSubjects` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/fm/templates` — 2 occurrence(s)
- `/GlbaDashboard` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/not-found` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTag` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tags` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/SourceSystems` — 2 occurrence(s)
- `/Templates` — 2 occurrence(s)
- `/Templates/Setup` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 54 of 54
- **Total occurrences:** 108

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**All affected pages** (108 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/AccessEvents` — 2 occurrence(s)
- `/AppBuilder` — 2 occurrence(s)
- `/AppBuilder/Dashboard` — 2 occurrence(s)
- `/AppBuilder/EntityWizard` — 2 occurrence(s)
- `/AppBuilder/New` — 2 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/ComplianceReports` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DataSubjects` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
- `/fm/templates` — 2 occurrence(s)
- `/GlbaDashboard` — 2 occurrence(s)
- `/InvalidTenantCode` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Logout` — 2 occurrence(s)
- `/MissingTenantCode` — 2 occurrence(s)
- `/Monaco` — 2 occurrence(s)
- `/not-found` — 2 occurrence(s)
- `/PasswordChanged` — 2 occurrence(s)
- `/Plugins` — 2 occurrence(s)
- `/ProcessLogin` — 2 occurrence(s)
- `/Profile` — 2 occurrence(s)
- `/ServerUpdated` — 2 occurrence(s)
- `/Settings` — 2 occurrence(s)
- `/Settings/AddDepartment` — 2 occurrence(s)
- `/Settings/AddDepartmentGroup` — 2 occurrence(s)
- `/Settings/AddTag` — 2 occurrence(s)
- `/Settings/AddTenant` — 2 occurrence(s)
- `/Settings/AddUser` — 2 occurrence(s)
- `/Settings/AddUserGroup` — 2 occurrence(s)
- `/Settings/AppSettings` — 2 occurrence(s)
- `/Settings/DeletedRecords` — 2 occurrence(s)
- `/Settings/DepartmentGroups` — 2 occurrence(s)
- `/Settings/Departments` — 2 occurrence(s)
- `/Settings/Files` — 2 occurrence(s)
- `/Settings/Language` — 2 occurrence(s)
- `/Settings/Tags` — 2 occurrence(s)
- `/Settings/Tenants` — 2 occurrence(s)
- `/Settings/UDF` — 2 occurrence(s)
- `/Settings/UserGroups` — 2 occurrence(s)
- `/Settings/Users` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)
- `/SortTest` — 2 occurrence(s)
- `/SourceSystems` — 2 occurrence(s)
- `/Templates` — 2 occurrence(s)
- `/Templates/Setup` — 2 occurrence(s)
- `/TimerTest` — 2 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 54 of 54
- **Total occurrences:** 54
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (54 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/AccessEvents` — 1 occurrence(s)
- `/AppBuilder` — 1 occurrence(s)
- `/AppBuilder/Dashboard` — 1 occurrence(s)
- `/AppBuilder/EntityWizard` — 1 occurrence(s)
- `/AppBuilder/New` — 1 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/ComplianceReports` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DataSubjects` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/fm/templates` — 1 occurrence(s)
- `/GlbaDashboard` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/Monaco` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTag` — 1 occurrence(s)
- `/Settings/AddTenant` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
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
- `/SortTest` — 1 occurrence(s)
- `/SourceSystems` — 1 occurrence(s)
- `/Templates` — 1 occurrence(s)
- `/Templates/Setup` — 1 occurrence(s)
- `/TimerTest` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 54 of 54
- **Total occurrences:** 54

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (54 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/AccessEvents` — 1 occurrence(s)
- `/AppBuilder` — 1 occurrence(s)
- `/AppBuilder/Dashboard` — 1 occurrence(s)
- `/AppBuilder/EntityWizard` — 1 occurrence(s)
- `/AppBuilder/New` — 1 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/ComplianceReports` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DataSubjects` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/fm/templates` — 1 occurrence(s)
- `/GlbaDashboard` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/Monaco` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/Plugins` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 1 occurrence(s)
- `/Settings/AddDepartment` — 1 occurrence(s)
- `/Settings/AddDepartmentGroup` — 1 occurrence(s)
- `/Settings/AddTag` — 1 occurrence(s)
- `/Settings/AddTenant` — 1 occurrence(s)
- `/Settings/AddUser` — 1 occurrence(s)
- `/Settings/AddUserGroup` — 1 occurrence(s)
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
- `/SortTest` — 1 occurrence(s)
- `/SourceSystems` — 1 occurrence(s)
- `/Templates` — 1 occurrence(s)
- `/Templates/Setup` — 1 occurrence(s)
- `/TimerTest` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 53 of 54
- **Total occurrences:** 167

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

**All affected pages** (167 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/AccessEvents` — 1 occurrence(s)
- `/AppBuilder` — 4 occurrence(s)
- `/AppBuilder/Dashboard` — 4 occurrence(s)
- `/AppBuilder/EntityWizard` — 4 occurrence(s)
- `/AppBuilder/New` — 4 occurrence(s)
- `/AppBuilder/SetupWizardBuilder` — 4 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 4 occurrence(s)
- `/ComplianceReports` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DataSubjects` — 1 occurrence(s)
- `/DoubleClick` — 4 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/fm/templates` — 4 occurrence(s)
- `/GlbaDashboard` — 1 occurrence(s)
- `/InvalidTenantCode` — 4 occurrence(s)
- `/Login` — 4 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 4 occurrence(s)
- `/Monaco` — 4 occurrence(s)
- `/not-found` — 4 occurrence(s)
- `/PasswordChanged` — 4 occurrence(s)
- `/Plugins` — 4 occurrence(s)
- `/Profile` — 4 occurrence(s)
- `/ServerUpdated` — 1 occurrence(s)
- `/Settings` — 4 occurrence(s)
- `/Settings/AddDepartment` — 4 occurrence(s)
- `/Settings/AddDepartmentGroup` — 4 occurrence(s)
- `/Settings/AddTag` — 4 occurrence(s)
- `/Settings/AddTenant` — 4 occurrence(s)
- `/Settings/AddUser` — 4 occurrence(s)
- `/Settings/AddUserGroup` — 4 occurrence(s)
- `/Settings/AppSettings` — 4 occurrence(s)
- `/Settings/DeletedRecords` — 4 occurrence(s)
- `/Settings/DepartmentGroups` — 4 occurrence(s)
- `/Settings/Departments` — 4 occurrence(s)
- `/Settings/Files` — 4 occurrence(s)
- `/Settings/Language` — 4 occurrence(s)
- `/Settings/Tags` — 4 occurrence(s)
- `/Settings/Tenants` — 4 occurrence(s)
- `/Settings/UDF` — 4 occurrence(s)
- `/Settings/UserGroups` — 4 occurrence(s)
- `/Settings/Users` — 4 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/SortTest` — 4 occurrence(s)
- `/SourceSystems` — 1 occurrence(s)
- `/Templates` — 4 occurrence(s)
- `/Templates/Setup` — 4 occurrence(s)
- `/TimerTest` — 4 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
