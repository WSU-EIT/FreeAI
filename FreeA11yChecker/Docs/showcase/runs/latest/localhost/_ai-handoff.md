# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 18:31:00  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeA11yChecker  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeA11yChecker\Docs\showcase\runs\latest\localhost`  

## Summary

- **79** pages scanned
- **553** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **553** real violations remaining
- **7** distinct rule failures
- **7** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 79 scanned pages. **Overall pass rate: 0.0%** (0 of 158 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 158 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **150** routes. We scanned **79** pages (79 of 79 static routes = **53% static coverage**).

### Parameterized routes that need IDs (71)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/Account/Manage/RenamePasskey/{Id}`
- `/Compliance/Page/{PageResultId}`
- `/Compliance/Rule/{RuleId}`
- `/Compliance/Status/{SiteId}`
- `/Compliance/{SiteId}/{ScanRunId}`
- `/Pages/{PageResultId}`
- `/Reports/Audit/{SiteId}`
- `/Scans/{ScanRunId}`
- `/Settings/EditDepartment/{departmentid}`
- `/Settings/EditDepartmentGroup/{departmentgroupid}`
- `/Settings/EditSite/{SiteId}`
- `/Settings/EditTag/{id}`
- `/Settings/EditTenant/{tenantid}`
- `/Settings/EditUser/{userid}`
- `/Settings/EditUserGroup/{groupid}`
- `/Settings/ManualChecks/{SiteId}`
- `/Settings/SiteRuns/{SiteId}`
- `/{TenantCode}`
- `/{TenantCode}/About`
- `/{TenantCode}/Authorization/AccessDenied`
- `/{TenantCode}/Authorization/InvalidUser`
- `/{TenantCode}/Authorization/NoLocalAccout`
- `/{TenantCode}/ChangePassword`
- `/{TenantCode}/Compliance`
- `/{TenantCode}/Compliance/Rule/{RuleId}`
- `/{TenantCode}/Compliance/Rules`
- `/{TenantCode}/Compliance/Scorecard`
- `/{TenantCode}/Compliance/Search`
- `/{TenantCode}/Compliance/Status/{SiteId}`
- `/{TenantCode}/Compliance/Tree`
- `/{TenantCode}/Login`
- `/{TenantCode}/Logout`
- `/{TenantCode}/Pages/{PageResultId}`
- `/{TenantCode}/PasswordChanged`
- `/{TenantCode}/ProcessLogin`
- `/{TenantCode}/Profile`
- `/{TenantCode}/Reports/Audit/{SiteId}`
- `/{TenantCode}/Reports/Trends`
- `/{TenantCode}/Scans`
- `/{TenantCode}/Scans/{ScanRunId}`
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
- `/{TenantCode}/Settings/EditSite/{SiteId}`
- `/{TenantCode}/Settings/EditTag/{id}`
- `/{TenantCode}/Settings/EditTenant/{tenantid}`
- `/{TenantCode}/Settings/EditUser/{userid}`
- `/{TenantCode}/Settings/EditUserGroup/{groupid}`
- `/{TenantCode}/Settings/Files`
- `/{TenantCode}/Settings/Language`
- `/{TenantCode}/Settings/ManualChecks/{SiteId}`
- `/{TenantCode}/Settings/ScanMonitor`
- `/{TenantCode}/Settings/SiteRuns/{SiteId}`
- `/{TenantCode}/Settings/Sites`
- `/{TenantCode}/Settings/Suppressions`
- `/{TenantCode}/Settings/Tags`
- `/{TenantCode}/Settings/Tenants`
- `/{TenantCode}/Settings/UDF`
- `/{TenantCode}/Settings/UserGroups`
- `/{TenantCode}/Settings/Users`

Example `seed-ids.json`:
```json
{
  "/Account/Manage/RenamePasskey/{Id}": ["REPLACE-WITH-REAL-ID"],
  "/Compliance/Page/{PageResultId}": ["REPLACE-WITH-REAL-ID"],
  "/Compliance/Rule/{RuleId}": ["REPLACE-WITH-REAL-ID"],
  "/Compliance/Status/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/Compliance/{SiteId}/{ScanRunId}": ["REPLACE-WITH-REAL-ID"],
  "/Pages/{PageResultId}": ["REPLACE-WITH-REAL-ID"],
  "/Reports/Audit/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/Scans/{ScanRunId}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartment/{departmentid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditDepartmentGroup/{departmentgroupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditSite/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/ManualChecks/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/Settings/SiteRuns/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/About": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/AccessDenied": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/InvalidUser": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Authorization/NoLocalAccout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ChangePassword": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Rule/{RuleId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Rules": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Scorecard": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Search": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Status/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Compliance/Tree": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Login": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Logout": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Pages/{PageResultId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/PasswordChanged": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/ProcessLogin": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Profile": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Reports/Audit/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Reports/Trends": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Scans": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Scans/{ScanRunId}": ["REPLACE-WITH-REAL-ID"],
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
  "/{TenantCode}/Settings/EditSite/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditTag/{id}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditTenant/{tenantid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUser/{userid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/EditUserGroup/{groupid}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Files": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Language": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/ManualChecks/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/ScanMonitor": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/SiteRuns/{SiteId}": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Sites": ["REPLACE-WITH-REAL-ID"],
  "/{TenantCode}/Settings/Suppressions": ["REPLACE-WITH-REAL-ID"],
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
| 1 | `document-title` (axe) | 🟠 serious | 79/79 | 79 | site-wide (layout) |
| 2 | `title-missing` (htmlcheck) | 🟠 serious | 79/79 | 79 | site-wide (layout) |
| 3 | `page_title_exists` (ibm) | 🟠 serious | 79/79 | 79 | site-wide (layout) |
| 4 | `skip_main_exists` (ibm) | 🟠 serious | 79/79 | 79 | site-wide (layout) |
| 5 | `skip-link` (htmlcheck) | 🟡 moderate | 79/79 | 79 | site-wide (layout) |
| 6 | `landmark-main` (htmlcheck) | 🟡 moderate | 79/79 | 79 | site-wide (layout) |
| 7 | `landmark-nav` (htmlcheck) | 🟡 moderate | 79/79 | 79 | site-wide (layout) |

## Per-rule fix instructions

### 🟠 `document-title` (axe) — SERIOUS

- **Pages affected:** 79 of 79
- **Total occurrences:** 79
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

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟠 `title-missing` (htmlcheck) — SERIOUS

- **Pages affected:** 79 of 79
- **Total occurrences:** 79

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Page is missing a <title> element

```html

```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟠 `page_title_exists` (ibm) — SERIOUS

- **Pages affected:** 79 of 79
- **Total occurrences:** 79

**Sample violation:**

- Page: `/`
- Selector: `/html[1]`
- Message: Missing <title> element in <head> element

```html
<html lang="en">
```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 79 of 79
- **Total occurrences:** 79

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body class="" data-bs-theme="" id="body-element">
```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 79 of 79
- **Total occurrences:** 79
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 79 of 79
- **Total occurrences:** 79

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 79 of 79
- **Total occurrences:** 79

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <nav> landmark found

```html

```

**All affected pages** (79 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/LoginWith2fa` — 1 occurrence(s)
- `/Account/LoginWithRecoveryCode` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Disable2fa` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/GenerateRecoveryCodes` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/ResetAuthenticator` — 1 occurrence(s)
- `/Account/Manage/SetPassword` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/RegisterConfirmation` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/Account/ResetPassword` — 1 occurrence(s)
- `/Account/ResetPasswordConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/Authorization/AccessDenied` — 1 occurrence(s)
- `/Authorization/InvalidUser` — 1 occurrence(s)
- `/Authorization/NoLocalAccount` — 1 occurrence(s)
- `/ChangePassword` — 1 occurrence(s)
- `/Compliance` — 1 occurrence(s)
- `/Compliance/Rules` — 1 occurrence(s)
- `/Compliance/Scorecard` — 1 occurrence(s)
- `/Compliance/Search` — 1 occurrence(s)
- `/Compliance/Tree` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
- `/InvalidTenantCode` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Logout` — 1 occurrence(s)
- `/MissingTenantCode` — 1 occurrence(s)
- `/not-found` — 1 occurrence(s)
- `/PasswordChanged` — 1 occurrence(s)
- `/ProcessLogin` — 1 occurrence(s)
- `/Profile` — 1 occurrence(s)
- `/Reports/Trends` — 1 occurrence(s)
- `/Scans` — 1 occurrence(s)
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
- `/Settings/ScanMonitor` — 1 occurrence(s)
- `/Settings/Sites` — 1 occurrence(s)
- `/Settings/Suppressions` — 1 occurrence(s)
- `/Settings/Tags` — 1 occurrence(s)
- `/Settings/Tenants` — 1 occurrence(s)
- `/Settings/UDF` — 1 occurrence(s)
- `/Settings/UserGroups` — 1 occurrence(s)
- `/Settings/Users` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
