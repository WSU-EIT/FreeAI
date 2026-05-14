# Accessibility Fix Pack — localhost

**Generated:** 2026-05-12 13:42:21  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\FreeA11yChecker\_consolebuild\runs\latest\localhost`  

## Summary

- **53** pages scanned
- **513** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **513** real violations remaining
- **14** distinct rule failures
- **2** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 53 scanned pages. **Overall pass rate: 0.0%** (0 of 285 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 285 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `link-name` (axe) | 🟠 serious | 31/53 | 62 | shared component |
| 2 | `input_label_exists` (ibm) | 🟠 serious | 31/53 | 62 | shared component |
| 3 | `div-button` (htmlcheck) | 🟠 serious | 19/53 | 19 | shared component |
| 4 | `aria_eventhandler_role_valid` (ibm) | 🟠 serious | 19/53 | 19 | shared component |
| 5 | `aria_form_label_unique` (ibm) | 🟠 serious | 11/53 | 23 | shared component |
| 6 | `input_label_before` (ibm) | 🟠 serious | 10/53 | 18 | shared component |
| 7 | `color-contrast` (axe) | 🟠 serious | 1/53 | 1 | single page |
| 8 | `text_contrast_sufficient` (ibm) | 🟠 serious | 1/53 | 1 | single page |
| 9 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 50/53 | 160 | site-wide (layout) |
| 10 | `skip-link` (htmlcheck) | 🟡 moderate | 50/53 | 50 | site-wide (layout) |
| 11 | `heading-empty` (htmlcheck) | 🟡 moderate | 31/53 | 62 | shared component |
| 12 | `landmark-main` (htmlcheck) | 🟡 moderate | 31/53 | 31 | shared component |
| 13 | `heading-order` (htmlcheck) | 🟡 moderate | 1/53 | 3 | single page |
| 14 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 1/53 | 2 | single page |

## Per-rule fix instructions

### 🟠 `link-name` (axe) — SERIOUS

- **Pages affected:** 31 of 53
- **Total occurrences:** 62
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

**All affected pages** (62 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/admin` — 2 occurrence(s)
- `/admin/About` — 2 occurrence(s)
- `/admin/ChangePassword` — 2 occurrence(s)
- `/admin/Compliance` — 2 occurrence(s)
- `/admin/Compliance/Rules` — 2 occurrence(s)
- `/admin/Compliance/Scorecard` — 2 occurrence(s)
- `/admin/Compliance/Search` — 2 occurrence(s)
- `/admin/Compliance/Tree` — 2 occurrence(s)
- `/admin/Login` — 2 occurrence(s)
- `/admin/Profile` — 2 occurrence(s)
- `/admin/Reports/Trends` — 2 occurrence(s)
- `/admin/Scans` — 2 occurrence(s)
- `/admin/Settings` — 2 occurrence(s)
- `/admin/Settings/AppSettings` — 2 occurrence(s)
- `/admin/Settings/DeletedRecords` — 2 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 2 occurrence(s)
- `/admin/Settings/Departments` — 2 occurrence(s)
- `/admin/Settings/Files` — 2 occurrence(s)
- `/admin/Settings/Language` — 2 occurrence(s)
- `/admin/Settings/ScanMonitor` — 2 occurrence(s)
- `/admin/Settings/Sites` — 2 occurrence(s)
- `/admin/Settings/Suppressions` — 2 occurrence(s)
- `/admin/Settings/Tags` — 2 occurrence(s)
- `/admin/Settings/Tenants` — 2 occurrence(s)
- `/admin/Settings/UDF` — 2 occurrence(s)
- `/admin/Settings/UserGroups` — 2 occurrence(s)
- `/admin/Settings/Users` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)

---

### 🟠 `input_label_exists` (ibm) — SERIOUS

- **Pages affected:** 31 of 53
- **Total occurrences:** 62

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/header[1]/nav[1]/div[1]/div[1]/form[1]/ul[1]/li[1]/a[1]`
- Message: Form control with "button" role has no associated label

```html
<a aria-expanded="false" data-bs-toggle="dropdown" role="button" id="themeDropdown" href="#" class="nav-link dropdown-toggle">
```

**All affected pages** (62 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/admin` — 2 occurrence(s)
- `/admin/About` — 2 occurrence(s)
- `/admin/ChangePassword` — 2 occurrence(s)
- `/admin/Compliance` — 2 occurrence(s)
- `/admin/Compliance/Rules` — 2 occurrence(s)
- `/admin/Compliance/Scorecard` — 2 occurrence(s)
- `/admin/Compliance/Search` — 2 occurrence(s)
- `/admin/Compliance/Tree` — 2 occurrence(s)
- `/admin/Login` — 2 occurrence(s)
- `/admin/Profile` — 2 occurrence(s)
- `/admin/Reports/Trends` — 2 occurrence(s)
- `/admin/Scans` — 2 occurrence(s)
- `/admin/Settings` — 2 occurrence(s)
- `/admin/Settings/AppSettings` — 2 occurrence(s)
- `/admin/Settings/DeletedRecords` — 2 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 2 occurrence(s)
- `/admin/Settings/Departments` — 2 occurrence(s)
- `/admin/Settings/Files` — 2 occurrence(s)
- `/admin/Settings/Language` — 2 occurrence(s)
- `/admin/Settings/ScanMonitor` — 2 occurrence(s)
- `/admin/Settings/Sites` — 2 occurrence(s)
- `/admin/Settings/Suppressions` — 2 occurrence(s)
- `/admin/Settings/Tags` — 2 occurrence(s)
- `/admin/Settings/Tenants` — 2 occurrence(s)
- `/admin/Settings/UDF` — 2 occurrence(s)
- `/admin/Settings/UserGroups` — 2 occurrence(s)
- `/admin/Settings/Users` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)

---

### 🟠 `div-button` (htmlcheck) — SERIOUS

- **Pages affected:** 19 of 53
- **Total occurrences:** 19

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: ``
- Message: Interactive div/span is missing role="button"

```html
<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()" b-qa4lg9le9h="">
```

**All affected pages** (19 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟠 `aria_eventhandler_role_valid` (ibm) — SERIOUS

- **Pages affected:** 19 of 53
- **Total occurrences:** 19

**Sample violation:**

- Page: `/Account/AccessDenied`
- Selector: `/html[1]/body[1]/div[1]/div[1]/div[2]`
- Message: The <div> element with 'onclick' does not have a valid ARIA role specified

```html
<div b-qa4lg9le9h="" onclick="document.querySelector('.navbar-toggler').click()" class="nav-scrollable">
```

**All affected pages** (19 total):

- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟠 `aria_form_label_unique` (ibm) — SERIOUS

- **Pages affected:** 11 of 53
- **Total occurrences:** 23

**Sample violation:**

- Page: `/Account/ForgotPassword`
- Selector: `/html[1]/body[1]/div[1]/div[1]/div[2]/nav[1]/div[6]/form[1]`
- Message: Multiple elements with "form" role do not have unique labels

```html
<form b-qa4lg9le9h="" method="post" action="Account/Logout">
```

**All affected pages** (23 total):

- `/Account/ForgotPassword` — 2 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/Account/Manage` — 2 occurrence(s)
- `/Account/Manage/ChangePassword` — 2 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 2 occurrence(s)
- `/Account/Manage/Email` — 3 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 2 occurrence(s)
- `/Account/Manage/Passkeys` — 2 occurrence(s)
- `/Account/Manage/PersonalData` — 2 occurrence(s)
- `/Account/Register` — 2 occurrence(s)
- `/Account/ResendEmailConfirmation` — 2 occurrence(s)

---

### 🟠 `input_label_before` (ibm) — SERIOUS

- **Pages affected:** 10 of 53
- **Total occurrences:** 18

**Sample violation:**

- Page: `/Account/ForgotPassword`
- Selector: `/html[1]/body[1]/div[1]/main[1]/article[1]/div[1]/div[1]/form[1]/div[1]/input[1]`
- Message: Label text is located after its associated text input or <select> element

```html
<input style="" value="" class="form-control valid" name="Input.Email" placeholder="name@example.com" aria-required="true" autocomplete="username" id="Input.Email">
```

**All affected pages** (18 total):

- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/Account/Manage` — 2 occurrence(s)
- `/Account/Manage/ChangePassword` — 3 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 2 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Register` — 3 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/auth` — 2 occurrence(s)

---

### 🟠 `color-contrast` (axe) — SERIOUS

- **Pages affected:** 1 of 53
- **Total occurrences:** 1
- **How to fix:** Increase color contrast. Need 4.5:1 for body text, 3:1 for large text.
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/color-contrast?application=axeAPI>

**Sample violation:**

- Page: `/About`
- Selector: `.text-warning`
- Message: Fix any of the following:
  Element has insufficient color contrast of 1.63 (foreground color: #ffc107, background color: #ffffff, font size: 24.0pt (32px), font weight: normal). Expected contrast ratio of 3:1

```html
<div class="h2 text-warning mb-1">22</div>
```

---

### 🟠 `text_contrast_sufficient` (ibm) — SERIOUS

- **Pages affected:** 1 of 53
- **Total occurrences:** 1

**Sample violation:**

- Page: `/About`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/div[6]/div[2]/div[1]/div[2]/div[1]/div[1]/div[1]`
- Message: Text contrast of 1.63 with its background is less than the WCAG AA minimum requirements for text of size 32px and weight of 500

```html
<div class="h2 text-warning mb-1">
```

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 50 of 53
- **Total occurrences:** 160

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/div[3]/div[1]/div[1]/div[2]/h1[1]`
- Message: Content is not within a landmark element

```html
<h1 class="page-title">
```

**All affected pages** (160 total):

- `/` — 4 occurrence(s)
- `/About` — 21 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/admin` — 4 occurrence(s)
- `/admin/About` — 4 occurrence(s)
- `/admin/ChangePassword` — 4 occurrence(s)
- `/admin/Compliance` — 4 occurrence(s)
- `/admin/Compliance/Rules` — 4 occurrence(s)
- `/admin/Compliance/Scorecard` — 4 occurrence(s)
- `/admin/Compliance/Search` — 4 occurrence(s)
- `/admin/Compliance/Tree` — 4 occurrence(s)
- `/admin/Login` — 4 occurrence(s)
- `/admin/Profile` — 4 occurrence(s)
- `/admin/Reports/Trends` — 4 occurrence(s)
- `/admin/Scans` — 4 occurrence(s)
- `/admin/Settings` — 4 occurrence(s)
- `/admin/Settings/AppSettings` — 4 occurrence(s)
- `/admin/Settings/DeletedRecords` — 4 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 4 occurrence(s)
- `/admin/Settings/Departments` — 4 occurrence(s)
- `/admin/Settings/Files` — 4 occurrence(s)
- `/admin/Settings/Language` — 4 occurrence(s)
- `/admin/Settings/ScanMonitor` — 4 occurrence(s)
- `/admin/Settings/Sites` — 4 occurrence(s)
- `/admin/Settings/Suppressions` — 4 occurrence(s)
- `/admin/Settings/Tags` — 4 occurrence(s)
- `/admin/Settings/Tenants` — 4 occurrence(s)
- `/admin/Settings/UDF` — 4 occurrence(s)
- `/admin/Settings/UserGroups` — 4 occurrence(s)
- `/admin/Settings/Users` — 4 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/Login` — 4 occurrence(s)
- `/Setup` — 4 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `skip-link` (htmlcheck) — MODERATE

- **Pages affected:** 50 of 53
- **Total occurrences:** 50
- **How to fix:** Ensure the skip link's target exists and is focusable.

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No skip-to-content link found

```html

```

**All affected pages** (50 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/Account/AccessDenied` — 1 occurrence(s)
- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 1 occurrence(s)
- `/Account/Lockout` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 1 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/ExternalLogins` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)
- `/admin` — 1 occurrence(s)
- `/admin/About` — 1 occurrence(s)
- `/admin/ChangePassword` — 1 occurrence(s)
- `/admin/Compliance` — 1 occurrence(s)
- `/admin/Compliance/Rules` — 1 occurrence(s)
- `/admin/Compliance/Scorecard` — 1 occurrence(s)
- `/admin/Compliance/Search` — 1 occurrence(s)
- `/admin/Compliance/Tree` — 1 occurrence(s)
- `/admin/Login` — 1 occurrence(s)
- `/admin/Profile` — 1 occurrence(s)
- `/admin/Reports/Trends` — 1 occurrence(s)
- `/admin/Scans` — 1 occurrence(s)
- `/admin/Settings` — 1 occurrence(s)
- `/admin/Settings/AppSettings` — 1 occurrence(s)
- `/admin/Settings/DeletedRecords` — 1 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 1 occurrence(s)
- `/admin/Settings/Departments` — 1 occurrence(s)
- `/admin/Settings/Files` — 1 occurrence(s)
- `/admin/Settings/Language` — 1 occurrence(s)
- `/admin/Settings/ScanMonitor` — 1 occurrence(s)
- `/admin/Settings/Sites` — 1 occurrence(s)
- `/admin/Settings/Suppressions` — 1 occurrence(s)
- `/admin/Settings/Tags` — 1 occurrence(s)
- `/admin/Settings/Tenants` — 1 occurrence(s)
- `/admin/Settings/UDF` — 1 occurrence(s)
- `/admin/Settings/UserGroups` — 1 occurrence(s)
- `/admin/Settings/Users` — 1 occurrence(s)
- `/auth` — 1 occurrence(s)
- `/counter` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)
- `/weather` — 1 occurrence(s)

---

### 🟡 `heading-empty` (htmlcheck) — MODERATE

- **Pages affected:** 31 of 53
- **Total occurrences:** 62

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: Heading element is empty

```html
<h5 class="offcanvas-title" id="offcanvasQuickActionLabel"></h5>
```

**All affected pages** (62 total):

- `/` — 2 occurrence(s)
- `/About` — 2 occurrence(s)
- `/admin` — 2 occurrence(s)
- `/admin/About` — 2 occurrence(s)
- `/admin/ChangePassword` — 2 occurrence(s)
- `/admin/Compliance` — 2 occurrence(s)
- `/admin/Compliance/Rules` — 2 occurrence(s)
- `/admin/Compliance/Scorecard` — 2 occurrence(s)
- `/admin/Compliance/Search` — 2 occurrence(s)
- `/admin/Compliance/Tree` — 2 occurrence(s)
- `/admin/Login` — 2 occurrence(s)
- `/admin/Profile` — 2 occurrence(s)
- `/admin/Reports/Trends` — 2 occurrence(s)
- `/admin/Scans` — 2 occurrence(s)
- `/admin/Settings` — 2 occurrence(s)
- `/admin/Settings/AppSettings` — 2 occurrence(s)
- `/admin/Settings/DeletedRecords` — 2 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 2 occurrence(s)
- `/admin/Settings/Departments` — 2 occurrence(s)
- `/admin/Settings/Files` — 2 occurrence(s)
- `/admin/Settings/Language` — 2 occurrence(s)
- `/admin/Settings/ScanMonitor` — 2 occurrence(s)
- `/admin/Settings/Sites` — 2 occurrence(s)
- `/admin/Settings/Suppressions` — 2 occurrence(s)
- `/admin/Settings/Tags` — 2 occurrence(s)
- `/admin/Settings/Tenants` — 2 occurrence(s)
- `/admin/Settings/UDF` — 2 occurrence(s)
- `/admin/Settings/UserGroups` — 2 occurrence(s)
- `/admin/Settings/Users` — 2 occurrence(s)
- `/Login` — 2 occurrence(s)
- `/Setup` — 2 occurrence(s)

---

### 🟡 `landmark-main` (htmlcheck) — MODERATE

- **Pages affected:** 31 of 53
- **Total occurrences:** 31

**Sample violation:**

- Page: `/`
- Selector: ``
- Message: No <main> landmark found

```html

```

**All affected pages** (31 total):

- `/` — 1 occurrence(s)
- `/About` — 1 occurrence(s)
- `/admin` — 1 occurrence(s)
- `/admin/About` — 1 occurrence(s)
- `/admin/ChangePassword` — 1 occurrence(s)
- `/admin/Compliance` — 1 occurrence(s)
- `/admin/Compliance/Rules` — 1 occurrence(s)
- `/admin/Compliance/Scorecard` — 1 occurrence(s)
- `/admin/Compliance/Search` — 1 occurrence(s)
- `/admin/Compliance/Tree` — 1 occurrence(s)
- `/admin/Login` — 1 occurrence(s)
- `/admin/Profile` — 1 occurrence(s)
- `/admin/Reports/Trends` — 1 occurrence(s)
- `/admin/Scans` — 1 occurrence(s)
- `/admin/Settings` — 1 occurrence(s)
- `/admin/Settings/AppSettings` — 1 occurrence(s)
- `/admin/Settings/DeletedRecords` — 1 occurrence(s)
- `/admin/Settings/DepartmentGroups` — 1 occurrence(s)
- `/admin/Settings/Departments` — 1 occurrence(s)
- `/admin/Settings/Files` — 1 occurrence(s)
- `/admin/Settings/Language` — 1 occurrence(s)
- `/admin/Settings/ScanMonitor` — 1 occurrence(s)
- `/admin/Settings/Sites` — 1 occurrence(s)
- `/admin/Settings/Suppressions` — 1 occurrence(s)
- `/admin/Settings/Tags` — 1 occurrence(s)
- `/admin/Settings/Tenants` — 1 occurrence(s)
- `/admin/Settings/UDF` — 1 occurrence(s)
- `/admin/Settings/UserGroups` — 1 occurrence(s)
- `/admin/Settings/Users` — 1 occurrence(s)
- `/Login` — 1 occurrence(s)
- `/Setup` — 1 occurrence(s)

---

### 🟡 `heading-order` (htmlcheck) — MODERATE

- **Pages affected:** 1 of 53
- **Total occurrences:** 3
- **How to fix:** Don't skip heading levels. After `<h1>` use `<h2>`, then `<h3>`, etc.

**Sample violation:**

- Page: `/About`
- Selector: ``
- Message: Heading level skipped: h2 to h6

```html

```

**All affected pages** (3 total):

- `/About` — 3 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 1 of 53
- **Total occurrences:** 2

**Sample violation:**

- Page: `/Account/Manage/Email`
- Selector: `/html[1]/body[1]/div[1]/main[1]/article[1]/div[1]/div[1]/div[2]/div[1]/div[1]/form[1]`
- Message: Multiple elements with "form" landmarks within the same parent region are not distinguished from one another because they have the same "" label

```html
<form action="/Account/Manage/Email" method="post" id="send-verification-form">
```

**All affected pages** (2 total):

- `/Account/Manage/Email` — 2 occurrence(s)

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
