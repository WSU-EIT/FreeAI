# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 17:40:28  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeBlazorExtended  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeBlazorExtended\Docs\showcase\runs\latest\localhost`  

## Summary

- **110** pages scanned
- **110** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **110** real violations remaining
- **1** distinct rule failures
- **1** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 110 scanned pages. **Overall pass rate: 0.0%** (0 of 110 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 110 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **151** routes. We scanned **110** pages (110 of 110 static routes = **73% static coverage**).

### Parameterized routes that need IDs (41)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/Settings/EditDepartment/{departmentid}`
- `/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/Settings/EditTag/{id}`
- `/Settings/EditTenant/{tenantid}`
- `/Settings/EditUser/{userid}`
- `/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}`
- `/{TenantCode}/About`
- `/{TenantCode}/Authorization/AccessDenied`
- `/{TenantCode}/Authorization/InvalidUser`
- `/{TenantCode}/Authorization/NoLocalAccout`
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
- `/{TenantCode}/Settings/Language`
- `/{TenantCode}/Settings/Tags`
- `/{TenantCode}/Settings/Tenants`
- `/{TenantCode}/Settings/UDF`
- `/{TenantCode}/Settings/UserGroups`
- `/{TenantCode}/Settings/Users`

Example `seed-ids.json`:
```json
{
  "/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/About": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/AccessDenied": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/InvalidUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/NoLocalAccout": ["REPLACE-WITH-REAL-ID"],
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
| 1 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 110/110 | 110 | site-wide (layout) |

## Per-rule fix instructions

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 110 of 110
- **Total occurrences:** 110

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a class="skip-link" href="#main-content">
```

**All affected pages** (110 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/assign-roles` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/change-password` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/demo` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/feedback` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/records` — 1 occurrence(s)
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
- `/Settings/Language` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/showcase` — 1 occurrence(s)
- `/showcase/aboutsection/v1` — 1 occurrence(s)
- `/showcase/aboutsection/v2` — 1 occurrence(s)
- `/showcase/aboutsection/v3` — 1 occurrence(s)
- `/showcase/autocomplete` — 1 occurrence(s)
- `/showcase/autogrowtext` — 1 occurrence(s)
- `/showcase/carousel/v1` — 1 occurrence(s)
- `/showcase/carousel/v2` — 1 occurrence(s)
- `/showcase/carousel/v3` — 1 occurrence(s)
- `/showcase/catalog` — 1 occurrence(s)
- `/showcase/commandpalette/v1` — 1 occurrence(s)
- `/showcase/commandpalette/v2` — 1 occurrence(s)
- `/showcase/commandpalette/v3` — 1 occurrence(s)
- `/showcase/confirmationcode` — 1 occurrence(s)
- `/showcase/datetimepicker` — 1 occurrence(s)
- `/showcase/deleteconfirmation` — 1 occurrence(s)
- `/showcase/examplenav/v1` — 1 occurrence(s)
- `/showcase/examplenav/v2` — 1 occurrence(s)
- `/showcase/examplenav/v3` — 1 occurrence(s)
- `/showcase/feature101-dynamic-forms` — 1 occurrence(s)
- `/showcase/feature102-multi-view-sync` — 1 occurrence(s)
- `/showcase/feature103-calendar` — 1 occurrence(s)
- `/showcase/feature104-user-preferences` — 1 occurrence(s)
- `/showcase/feature105-agent-monitoring` — 1 occurrence(s)
- `/showcase/feature107-hierarchical-tree` — 1 occurrence(s)
- `/showcase/generic-git` — 1 occurrence(s)
- `/showcase/getinput` — 1 occurrence(s)
- `/showcase/github-repo` — 1 occurrence(s)
- `/showcase/imagegallery/v1` — 1 occurrence(s)
- `/showcase/imagegallery/v2` — 1 occurrence(s)
- `/showcase/imagegallery/v3` — 1 occurrence(s)
- `/showcase/infotip/v1` — 1 occurrence(s)
- `/showcase/infotip/v2` — 1 occurrence(s)
- `/showcase/infotip/v3` — 1 occurrence(s)
- `/showcase/kanbanboard/v1` — 1 occurrence(s)
- `/showcase/kanbanboard/v2` — 1 occurrence(s)
- `/showcase/kanbanboard/v3` — 1 occurrence(s)
- `/showcase/multiselect` — 1 occurrence(s)
- `/showcase/networkchart/v1` — 1 occurrence(s)
- `/showcase/networkchart/v2` — 1 occurrence(s)
- `/showcase/networkchart/v3` — 1 occurrence(s)
- `/showcase/pagedrecordset` — 1 occurrence(s)
- `/showcase/pipelinetracker/v1` — 1 occurrence(s)
- `/showcase/pipelinetracker/v2` — 1 occurrence(s)
- `/showcase/pipelinetracker/v3` — 1 occurrence(s)
- `/showcase/renderfiles/v1` — 1 occurrence(s)
- `/showcase/renderfiles/v2` — 1 occurrence(s)
- `/showcase/renderfiles/v3` — 1 occurrence(s)
- `/showcase/selectfile/v1` — 1 occurrence(s)
- `/showcase/selectfile/v2` — 1 occurrence(s)
- `/showcase/selectfile/v3` — 1 occurrence(s)
- `/showcase/signature/v1` — 1 occurrence(s)
- `/showcase/signature/v2` — 1 occurrence(s)
- `/showcase/signature/v3` — 1 occurrence(s)
- `/showcase/smartsheet` — 1 occurrence(s)
- `/showcase/stringlist` — 1 occurrence(s)
- `/showcase/timeline/v1` — 1 occurrence(s)
- `/showcase/timeline/v2` — 1 occurrence(s)
- `/showcase/timeline/v3` — 1 occurrence(s)
- `/showcase/timer/v1` — 1 occurrence(s)
- `/showcase/timer/v2` — 1 occurrence(s)
- `/showcase/timer/v3` — 1 occurrence(s)
- `/showcase/toastcontainer/v1` — 1 occurrence(s)
- `/showcase/toastcontainer/v2` — 1 occurrence(s)
- `/showcase/toastcontainer/v3` — 1 occurrence(s)
- `/showcase/togglepassword` — 1 occurrence(s)
- `/showcase/wizard/v1` — 1 occurrence(s)
- `/showcase/wizard/v2` — 1 occurrence(s)
- `/showcase/wizard/v3` — 1 occurrence(s)
- `/verify` — 1 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
