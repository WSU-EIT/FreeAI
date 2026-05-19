# Accessibility Fix Pack — localhost

**Generated:** 2026-05-19 14:02:20  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAi\FreeA11yChecker\A11yAudit\runs\BlazorApp1\localhost`  

## Summary

- **20** pages scanned
- **110** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **110** real violations remaining
- **5** distinct rule failures
- **2** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 20 scanned pages. **Overall pass rate: 0.0%** (0 of 90 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 90 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `nested-interactive` (axe) | 🟠 serious | 20/20 | 20 | site-wide (layout) |
| 2 | `aria_form_label_unique` (ibm) | 🟠 serious | 11/20 | 12 | shared component |
| 3 | `input_label_before` (ibm) | 🟠 serious | 9/20 | 16 | shared component |
| 4 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 20/20 | 60 | site-wide (layout) |
| 5 | `aria_landmark_name_unique` (ibm) | 🟡 moderate | 1/20 | 2 | single page |

## Per-rule fix instructions

### 🟠 `nested-interactive` (axe) — SERIOUS

- **Pages affected:** 20 of 20
- **Total occurrences:** 20
- **How to fix:** Don't nest interactive elements (e.g., a button inside a link).
- **Reference:** <https://dequeuniversity.com/rules/axe/4.10/nested-interactive?application=axeAPI>

**Sample violation:**

- Page: `/`
- Selector: `.nav-scrollable`
- Message: Fix any of the following:
  Element has focusable descendants

```html
<div class="nav-scrollable" role="button" tabindex="0" onclick="document.querySelector('.navbar-toggler').click()" b-qa4lg9le9h="">
```

**All affected pages** (20 total):

- `/` — 1 occurrence(s)
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

- **Pages affected:** 11 of 20
- **Total occurrences:** 12

**Sample violation:**

- Page: `/Account/ForgotPassword`
- Selector: `/html[1]/body[1]/div[1]/main[1]/article[1]/div[1]/div[1]/form[1]`
- Message: Multiple elements with "form" role do not have unique labels

```html
<form action="/Account/ForgotPassword" method="post">
```

**All affected pages** (12 total):

- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/Login` — 1 occurrence(s)
- `/Account/Manage` — 1 occurrence(s)
- `/Account/Manage/ChangePassword` — 1 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 2 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Manage/Passkeys` — 1 occurrence(s)
- `/Account/Manage/PersonalData` — 1 occurrence(s)
- `/Account/Register` — 1 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)

---

### 🟠 `input_label_before` (ibm) — SERIOUS

- **Pages affected:** 9 of 20
- **Total occurrences:** 16

**Sample violation:**

- Page: `/Account/ForgotPassword`
- Selector: `/html[1]/body[1]/div[1]/main[1]/article[1]/div[1]/div[1]/form[1]/div[1]/input[1]`
- Message: Label text is located after its associated text input or <select> element

```html
<input style="" value="" class="form-control valid" name="Input.Email" placeholder="name@example.com" aria-required="true" autocomplete="username" id="Input.Email">
```

**All affected pages** (16 total):

- `/Account/ForgotPassword` — 1 occurrence(s)
- `/Account/Login` — 2 occurrence(s)
- `/Account/Manage` — 2 occurrence(s)
- `/Account/Manage/ChangePassword` — 3 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 1 occurrence(s)
- `/Account/Manage/Email` — 2 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 1 occurrence(s)
- `/Account/Register` — 3 occurrence(s)
- `/Account/ResendEmailConfirmation` — 1 occurrence(s)

---

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 20 of 20
- **Total occurrences:** 60

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/div[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a b-qig53hgue4="" href="#main-content" class="visually-hidden-focusable">
```

**All affected pages** (60 total):

- `/` — 3 occurrence(s)
- `/Account/AccessDenied` — 3 occurrence(s)
- `/Account/ForgotPassword` — 3 occurrence(s)
- `/Account/ForgotPasswordConfirmation` — 3 occurrence(s)
- `/Account/Lockout` — 3 occurrence(s)
- `/Account/Login` — 3 occurrence(s)
- `/Account/Manage` — 3 occurrence(s)
- `/Account/Manage/ChangePassword` — 3 occurrence(s)
- `/Account/Manage/DeletePersonalData` — 3 occurrence(s)
- `/Account/Manage/Email` — 3 occurrence(s)
- `/Account/Manage/EnableAuthenticator` — 3 occurrence(s)
- `/Account/Manage/ExternalLogins` — 3 occurrence(s)
- `/Account/Manage/Passkeys` — 3 occurrence(s)
- `/Account/Manage/PersonalData` — 3 occurrence(s)
- `/Account/Manage/TwoFactorAuthentication` — 3 occurrence(s)
- `/Account/Register` — 3 occurrence(s)
- `/Account/ResendEmailConfirmation` — 3 occurrence(s)
- `/auth` — 3 occurrence(s)
- `/counter` — 3 occurrence(s)
- `/weather` — 3 occurrence(s)

---

### 🟡 `aria_landmark_name_unique` (ibm) — MODERATE

- **Pages affected:** 1 of 20
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
