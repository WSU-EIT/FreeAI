# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 16:49:23  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeSmartsheets  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeSmartsheets\Docs\showcase\runs\latest\localhost`  

## Summary

- **42** pages scanned
- **16** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **16** real violations remaining
- **7** distinct rule failures
- **0** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 42 scanned pages. **Overall pass rate: 0.0%** (0 of 8 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 8 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **90** routes. We scanned **42** pages (42 of 42 static routes = **47% static coverage**).

### Parameterized routes that need IDs (48)

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
- `/{TenantCode}/DoubleClick`
- `/{TenantCode}/DynamicComponent`
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
- `/{TenantCode}/TimerTest`

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
  "/{TenantCode}/DoubleClick": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/DynamicComponent": ["REPLACE-WITH-REAL-ID"],
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
  "/{TenantCode}/TimerTest": ["REPLACE-WITH-REAL-ID"]
}
```

Run with: `crawl --url ... --seed-ids seed-ids.json`

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `skip_main_exists` (ibm) | 🟠 serious | 1/42 | 1 | single page |
| 2 | `tabindex` (htmlcheck) | 🟡 moderate | 1/42 | 5 | single page |
| 3 | `element_tabbable_role_valid` (ibm) | 🟡 moderate | 1/42 | 5 | single page |
| 4 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 1/42 | 2 | single page |
| 5 | `skip-link` (htmlcheck) | 🟡 moderate | 1/42 | 1 | single page |
| 6 | `landmark-main` (htmlcheck) | 🟡 moderate | 1/42 | 1 | single page |
| 7 | `landmark-nav` (htmlcheck) | 🟡 moderate | 1/42 | 1 | single page |

## Per-rule fix instructions

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 1 of 42
- **Total occurrences:** 1

**Sample violation:**

- Page: `/DoubleClick`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body>
```

---

### 🟡 `tabindex` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 5
- **How to fix:** Avoid positive tabindex. Use `0` or rely on natural source order.

**Sample violation:**

- Page: `/DoubleClick`
- Selector: ``
- Message: Positive tabindex value disrupts natural tab order

```html
tabindex="1"
```

**All affected pages** (5 total):

- `/DoubleClick` — 5 occurrence(s)

---

### 🟡 `element_tabbable_role_valid` (ibm) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 5

**Sample violation:**

- Page: `/DoubleClick`
- Selector: `/html[1]/body[1]/ul[1]/li[1]`
- Message: The tabbable element's role 'listitem' is not a widget role

```html
<li class="selected" tabindex="1" id="stack">
```

**Likely source location(s)** (highest confidence first):

- `FreeSmartsheets/FreeSmartsheets.Client/Shared/UploadFile.razor:6` — matched on id:'stack', 25 hit(s) — confidence: **low**
- `FreeSmartsheets/FreeSmartsheets.Client/Shared/UploadFile.razor:25` — matched on id:'stack', 25 hit(s) — confidence: **low**
- `Docs/showcase/runs/latest/localhost/DoubleClick/page.html:13` — matched on id:'stack', 25 hit(s) — confidence: **low**

**All affected pages** (5 total):

- `/DoubleClick` — 5 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 2

**Sample violation:**

- Page: `/DoubleClick`
- Selector: `/html[1]/body[1]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1>
```

**All affected pages** (2 total):

- `/DoubleClick` — 2 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 1
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/DoubleClick`
- Selector: ``
- Message: No skip-to-content link found

```html

```

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 1

**Sample violation:**

- Page: `/DoubleClick`
- Selector: ``
- Message: No <main> landmark found

```html

```

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 42
- **Total occurrences:** 1

**Sample violation:**

- Page: `/DoubleClick`
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
