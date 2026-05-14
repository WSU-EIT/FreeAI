# Accessibility Fix Pack — localhost

**Generated:** 2026-05-14 14:14:43  
**Source root:** *(not provided — no source cross-references)*  
**Scan output:** `C:\Users\pepkad\source\repos\WSU-EIT\FreeAI\FreeBlazorExtended\Docs\showcase\runs\2026-05-14\localhost`  

## Summary

- **1** pages scanned
- **1** raw violations across all 4 tools
- **0** IBM PASS-style noise items filtered out at report time
- **1** real violations remaining
- **1** distinct rule failures
- **1** rules affect ≥70% of pages (likely shared layout/nav — fix in MainLayout / NavMenu first)

## ✅ What's working — pass rate per tool

Aggregate across all 1 scanned pages. **Overall pass rate: 0.0%** (0 of 1 rule checks passed).

| Tool | Rules passed | Total checks | Pass rate |
|------|-------------:|-------------:|----------:|
| **axe** | 0 | 0 | 🔴 0.0% |
| **ibm** | 0 | 1 | 🔴 0.0% |
| **htmlcheck** | 0 | 0 | 🔴 0.0% |
| **htmlcs** | 0 | 0 | 🔴 0.0% |

> Note: pass-rate counts every distinct rule × applicable element. A page with 100 elements that all pass the same 5 rules counts as 500 passing checks. Failed rules are subtracted on the same per-element basis.

## Suggested fix order (highest impact first)

Fix in this order — each rule's fix likely cascades to clear all listed occurrences in one edit.

| # | Rule | Severity | Pages | Occurrences | Likely scope |
|---|------|----------|-------|-------------|--------------|
| 1 | `aria_content_in_landmark` (ibm) | 🟡 moderate | 1/1 | 1 | site-wide (layout) |

## Per-rule fix instructions

### 🟡 `aria_content_in_landmark` (ibm) — MODERATE

- **Pages affected:** 1 of 1
- **Total occurrences:** 1

**Sample violation:**

- Page: `/`
- Selector: `/html[1]/body[1]/a[1]`
- Message: Content is not within a landmark element

```html
<a class="skip-link" href="#main-content">
```

---

## Instructions for the fixing agent

1. Start with the rules at the top — site-wide ones cascade to many pages with one edit.
2. For each rule, open the highest-confidence source location and apply the fix per the `How to fix` line.
3. After each batch of fixes, search the source for ALL hits on the same selector pattern (not just the exemplar) and fix every match.
4. Skip rules where the source cross-reference is empty or low-confidence — those need a re-scan with verbose snippets to triangulate.
5. Don't refactor for readability while you're at it. Minimal diffs only — every change must be defensible against the rule it's fixing.
