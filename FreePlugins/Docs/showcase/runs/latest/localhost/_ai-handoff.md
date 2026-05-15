# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 16:44:40  
**Source root:** C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreePlugins  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreePlugins\Docs\showcase\runs\latest\localhost`  

## Summary

- **74** pages scanned
- **726** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **726** real violations remaining
- **11** distinct rule failures
- **3** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 74 scanned pages. **Overall pass rate: 0.0%** (0 of 311 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 311 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## 📊 Page coverage vs source

Source declares **122** routes. We scanned **74** pages (74 of 74 static routes = **61% static coverage**).

### Parameterized routes that need IDs (48)

These Edit/Detail pages can't be scanned without real record IDs from the database. Their navigation is handled by `NavManager.NavigateTo(...)` in code, NOT `<a href>`, so the link extractor can't find them automatically. Get a sample ID for each from the running app, save to a JSON file, and pass via `--seed-ids`:

- `/Account/Manage/RenamePasskey/{Id}`
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
  "/Account/Manage/RenamePasskey/{Id}": ["REPLACE-WITH-REAL-ID"],
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
| 1 | `link-name` (axe) | 🟠 serious | 45/74 | 90 | shared component |
| 2 | `input_label_exists` (ibm) | 🟠 serious | 45/74 | 90 | shared component |
| 3 | `document-title` (axe) | 🟠 serious | 29/74 | 29 | shared component |
| 4 | `title-missing` (htmlcheck) | 🟠 serious | 29/74 | 29 | shared component |
| 5 | `page_title_exists` (ibm) | 🟠 serious | 29/74 | 29 | shared component |
| 6 | `skip_main_exists` (ibm) | 🟠 serious | 29/74 | 29 | shared component |
| 7 | `skip-link` (htmlcheck) | 🟡 moderate | 74/74 | 74 | site-wide (layout) |
| 8 | `landmark-main` (htmlcheck) | 🟡 moderate | 74/74 | 74 | site-wide (layout) |
| 9 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 73/74 | 163 | site-wide (layout) |
| 10 | `heading-empty` (htmlcheck) | 🟡 moderate | 45/74 | 90 | shared component |
| 11 | `landmark-nav` (htmlcheck) | 🟡 moderate | 29/74 | 29 | shared component |

## Per-rule fix instructions

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 45 of 74
- **Total occurrences:** 90
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

**All affected pages** (90 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/auth` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/counter` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
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
- `/TimerTest` — 2 occurrence(s)
- `/weather` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 45 of 74
- **Total occurrences:** 90

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**All affected pages** (90 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/auth` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/counter` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
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
- `/TimerTest` — 2 occurrence(s)
- `/weather` — 2 occurrence(s)

---

### 🟠 `document-title` (axe) — SERIOUS

- **Pages affected:** 29 of 74
- **Total occurrences:** 29
- **How to fix:** Add a descriptive, non-empty `<title>` in the `<head>`.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/document-title?application=axeAPI>

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: `html`
- Message: Fix any of the following:
  Document does not have a non-empty <title> element

```html
<html lang="en" style="--blazor-load-percentage: 100%; --blazor-load-percentage-text: &quot;100%&quot;;">
```

**All affected pages** (29 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
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

---

### 🟠 `title-missing` (htmlcheck) — SERIOUS

- **Pages affected:** 29 of 74
- **Total occurrences:** 29

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: ``
- Message: Page is missing a <title> element

```html

```

**All affected pages** (29 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
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

---

### 🟠 `page_title_exists` (ibm) — SERIOUS

- **Pages affected:** 29 of 74
- **Total occurrences:** 29

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: `/html[1]`
- Message: Missing <title> element in <head> element

```html
<html style="--blazor-load-percentage: 100%; --blazor-load-percentage-text: "100%";" lang="en">
```

**All affected pages** (29 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
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

---

### 🟠 `skip_main_exists` (ibm) — SERIOUS

- **Pages affected:** 29 of 74
- **Total occurrences:** 29

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: `/html[1]/body[1]`
- Message: The page does not provide a way to quickly navigate to the main content (ARIA "main" landmark or a skip link)

```html
<body class="" data-bs-theme="" id="body-element">
```

**All affected pages** (29 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
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

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 74 of 74
- **Total occurrences:** 74
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (74 total):

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
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
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
- `/TimerTest` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 74 of 74
- **Total occurrences:** 74

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (74 total):

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
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 1 occurrence(s)
- `/Error` — 1 occurrence(s)
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
- `/TimerTest` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 73 of 74
- **Total occurrences:** 163

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

**All affected pages** (163 total):

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
- `/ChangePassword` — 4 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/DatabaseOffline` — 1 occurrence(s)
- `/DoubleClick` — 4 occurrence(s)
- `/Error` — 1 occurrence(s)
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
- `/TimerTest` — 4 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 45 of 74
- **Total occurrences:** 90

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**Likely source location(s)** (highest confidence first):

- `FreePluginsV1/FreePlugins.Client/Shared/OffcanvasPopoutMenu.razor:4` — matched on id:'offcanvasQuickActionLabel', 48 hit(s) — confidence: **low**
- `FreePluginsV1/FreePlugins.Client/Shared/OffcanvasPopoutMenu.razor:6` — matched on id:'offcanvasQuickActionLabel', 48 hit(s) — confidence: **low**
- `Docs/showcase/runs/2026-05-14/localhost/_root/page.html:359` — matched on id:'offcanvasQuickActionLabel', 48 hit(s) — confidence: **low**

**All affected pages** (90 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/auth` — 2 occurrence(s)
- `/Authorization/AccessDenied` — 2 occurrence(s)
- `/Authorization/InvalidUser` — 2 occurrence(s)
- `/Authorization/NoLocalAccount` — 2 occurrence(s)
- `/ChangePassword` — 2 occurrence(s)
- `/counter` — 2 occurrence(s)
- `/DatabaseOffline` — 2 occurrence(s)
- `/DoubleClick` — 2 occurrence(s)
- `/Error` — 2 occurrence(s)
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
- `/TimerTest` — 2 occurrence(s)
- `/weather` — 2 occurrence(s)

---

### 🟡 `landmark-nav` (htmlcheck) — MODERATE

- **Pages affected:** 29 of 74
- **Total occurrences:** 29

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: ``
- Message: No <nav> landmark found

```html

```

**All affected pages** (29 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ConfirmEmail` — 1 occurrence(s)
- `/Account/ConfirmEmailChange` — 1 occurrence(s)
- `/Account/ExternalLogin` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/InvalidPasswordReset` — 1 occurrence(s)
- `/Account/InvalidUser` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
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

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
