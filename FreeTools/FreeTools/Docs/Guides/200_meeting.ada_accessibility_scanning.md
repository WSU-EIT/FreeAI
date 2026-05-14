# 200 — Meeting: Multi-Tool ADA Accessibility Scanning

> **Document ID:** 200  
> **Category:** Meeting  
> **Purpose:** Design the ADA/WCAG accessibility scanning feature for AccessibilityScanner  
> **Attendees:** [Architect], [Backend], [Quality], [Skeptic], [Sanity]  
> **Date:** 2026-02-17  
> **Predicted Outcome:** Approved architecture for 3+ ADA scanning tools, unified output schema, cross-tool comparison reports  
> **Actual Outcome:** ✅ Architecture approved — consensus-ranked output added per doc 203  
> **Resolution:** Implementation started — see docs 201 (sanity check), 202 (phases/tasks), 203 (consensus ranking)  

---

## Context

WSU's AccessibilityScanner tool currently scans 120 sites (502 pages), capturing screenshots, downloading images, checking for missing alt text, and recording JS errors. It produces per-page, per-site, and per-run markdown reports with image galleries and progress bars.

**What it does NOT do:** Run any actual WCAG/ADA accessibility rule checks. No heading order validation, no contrast checking, no ARIA validation, no form label checks, no landmark detection, no skip-link detection.

The tool has two research docs (`ada_compliance_research_1.md` and `ada_compliance_research_2.md`) that lay out the tool landscape and even sketch a unified issue schema. None of that is implemented.

**Why now:** WSU needs to demonstrate ADA compliance across all public-facing sites. We need automated scanning that can be run repeatedly, produce evidence artifacts, and track progress over time.

---

## Discussion

**[Architect]:** Let me frame the constraints. We already have Playwright opening every page, rendering it, saving the HTML, and downloading images. The scanning infrastructure is built. The gap is: after the page is loaded, we don't inspect it for accessibility issues.

The key architectural question is: do we run one tool or many? And how do we make the output comparable?

**[Backend]:** Three tools make sense based on the research docs. Each catches different things:

1. **axe-core** — Industry standard. ~90 WCAG 2.1 AA rules. Runs as injected JavaScript in the live Playwright page. Catches contrast, ARIA, labels, landmarks, headings, and more. This is the heavyweight.

2. **HTML checker** — Custom C# parser that reads our already-saved `page.html`. Zero external dependencies. Catches the "obvious" stuff: missing alt text, heading order, missing `lang` attribute, empty links, empty buttons, missing form labels, missing skip-link, div-as-button anti-patterns. Fast, simple, no network needed.

3. **Pa11y** — External CLI tool (`npm install -g pa11y`). Uses HTML_CodeSniffer under the hood — a different rules engine than axe. Catches things axe misses and vice versa. Optional — gracefully skipped if not installed.

Each tool has a different native output format. We normalize everything to a unified schema so they can be compared side-by-side.

**[Architect]:** Right. The per-page output structure currently looks like:

```
{site}/{page}/
├── 01-page-loaded.png
├── page.html              ← 210KB average rendered DOM
├── metadata.json
├── report.md
├── images/
├── errors.log, warnings.log, info.log, actions.log
```

We add a11y files alongside — same folder, predictable names:

```
{site}/{page}/
├── ... (everything above, unchanged) ...
├── a11y-axe.json           ← axe-core raw results
├── a11y-htmlcheck.json     ← custom HTML checker results
├── a11y-pa11y.json         ← pa11y CLI results (or skipped)
└── a11y-summary.json       ← merged cross-tool comparison
```

**[Quality]:** The naming convention `a11y-{toolname}.json` is good. Discoverable — `find . -name "a11y-*.json"`. And the report generator can glob for them without knowing the tool list in advance.

**[Skeptic]:** What does the unified issue schema look like? If each tool has different fields, how do you actually compare them?

**[Backend]:** The research docs already sketched this. Every issue, regardless of source tool, normalizes to:

```json
{
    "tool": "axe",
    "ruleId": "image-alt",
    "severity": "critical",
    "message": "Images must have alternate text",
    "selector": "img.hero-banner",
    "snippet": "<img src=\"banner.jpg\">",
    "helpUrl": "https://dequeuniversity.com/rules/axe/4.10/image-alt",
    "wcagCriteria": "1.1.1"
}
```

Fields:

| Field | Required | Description |
|-------|----------|-------------|
| `tool` | Yes | Which tool found this: `axe`, `htmlcheck`, `pa11y` |
| `ruleId` | Yes | Tool-specific rule identifier |
| `severity` | Yes | `critical` / `serious` / `moderate` / `minor` |
| `message` | Yes | Human-readable description |
| `selector` | No | CSS selector targeting the element |
| `snippet` | No | HTML snippet of the offending element |
| `helpUrl` | No | Link to documentation about the rule |
| `wcagCriteria` | No | WCAG success criterion (e.g., "1.1.1", "2.4.6") |

Severity mapping across tools:

| Tool | Maps to `critical` | Maps to `serious` | Maps to `moderate` | Maps to `minor` |
|------|--------------------|--------------------|---------------------|------------------|
| axe-core | `critical` | `serious` | `moderate` | `minor` |
| htmlcheck | — | `serious` | `moderate` | `minor` |
| pa11y | `error` | `error` | `warning` | `notice` |

**[Quality]:** What does the cross-tool comparison table look like in the report?

**[Architect]:** At the page level:

```markdown
## ♿ Accessibility

### Summary

| Severity | axe | htmlcheck | pa11y | Total Unique |
|----------|:---:|:---------:|:-----:|:------------:|
| 🔴 Critical | 3 | 0 | 2 | 4 |
| 🟠 Serious | 8 | 13 | 6 | 15 |
| 🟡 Moderate | 4 | 3 | 5 | 7 |
| 🔵 Minor | 2 | 1 | 3 | 4 |
| **Total** | **17** | **17** | **16** | **30** |

### Violations by Rule

| Rule | Severity | axe | htmlcheck | pa11y | Example |
|------|----------|:---:|:---------:|:-----:|---------|
| image-alt | 🔴 critical | 13 | 13 | 11 | `<img src="banner.jpg">` |
| heading-order | 🟡 moderate | 2 | 3 | 2 | `<h3>` after `<h1>` |
| color-contrast | 🟠 serious | 8 | — | 6 | `color:#999 on #fff` |
| html-has-lang | 🟠 serious | 0 | 0 | 0 | *(all pass)* |
| link-name | 🟠 serious | 4 | 5 | — | `<a href="#">Click here</a>` |
```

The "—" means that tool cannot check that rule (htmlcheck can't compute contrast; pa11y may not flag certain ARIA patterns). The "Total Unique" column deduplicates across tools by matching on `ruleId` + `selector`.

**[Skeptic]:** This is a lot of data. For 502 pages with potentially 30+ violations each, that's 15,000+ issues. How do you make the site-level and run-level reports useful?

**[Architect]:** Aggregation. At the site level:

```markdown
## ♿ Accessibility Summary

| Severity | axe | htmlcheck | pa11y |
|----------|:---:|:---------:|:-----:|
| 🔴 Critical | 45 | 12 | 38 |
| 🟠 Serious | 120 | 89 | 95 |
| 🟡 Moderate | 67 | 34 | 78 |
| 🔵 Minor | 23 | 15 | 31 |

### Top 10 Issues (this site)

| # | Rule | Severity | Pages Affected | Total Instances |
|--:|------|----------|:--------------:|:---------------:|
| 1 | image-alt | 🔴 critical | 14/16 | 89 |
| 2 | color-contrast | 🟠 serious | 12/16 | 67 |
| 3 | link-name | 🟠 serious | 9/16 | 34 |
...
```

At the run level (120 sites):

```markdown
### Top 20 Issues (all sites)

| # | Rule | Severity | Sites | Pages | Instances |
|--:|------|----------|:-----:|:-----:|:---------:|
| 1 | image-alt | 🔴 critical | 98/120 | 389/502 | 1189 |
| 2 | color-contrast | 🟠 serious | 87/120 | 312/502 | 856 |
...

### Tool Agreement

| Metric | Value |
|--------|-------|
| Rules checked by all 3 tools | 42 |
| Rules only axe catches | 31 |
| Rules only htmlcheck catches | 4 |
| Rules only pa11y catches | 8 |
| Average cross-tool agreement | 78% |
```

**[Sanity]:** Mid-check — are we overcomplicating this? Three tools, unified schema, cross-tool comparison tables, agreement percentages... do we need all of this for v1?

**[Architect]:** Fair point. Let me propose a phased approach:

| Phase | What | Complexity | Value |
|-------|------|-----------|-------|
| **Phase 1** | axe-core only + report tables | Medium | 80% of the value |
| **Phase 2** | Add htmlcheck (C# parser) | Low | Catches obvious stuff, validates axe findings |
| **Phase 3** | Add pa11y (external CLI) | Low | Third perspective, optional install |
| **Phase 4** | Cross-tool comparison tables | Medium | The "compare and contrast" view |

Phase 1 alone gives us professional-grade ADA scanning with the industry standard engine. Phases 2-3 add corroboration. Phase 4 adds the comparison tables.

**[Skeptic]:** I like that. But let me break some things: 

1. **axe-core.min.js is ~400KB.** How do we ship it? Bundle as embedded resource? Download at runtime?
2. **Pa11y requires Node.js.** Not everyone has it. What happens when it's missing?
3. **page.html is the rendered DOM but without computed styles.** Can htmlcheck detect contrast issues?
4. **Running axe on 502 pages sequentially will be slow.** What's the impact on scan time?

**[Backend]:** Good questions.

1. **axe.min.js** — Download from CDN on first run, cache locally. The scanner already has internet access (it's hitting live sites). We download once to `{projectDir}/axe.min.js` and reuse. No need to embed in the binary or track in git.

2. **Pa11y missing** — Run `where pa11y` (Windows) or `which pa11y` (Unix) at startup. If not found, log "pa11y not installed — skipping" and set `status: "skipped"` in `a11y-pa11y.json`. The report shows "—" for that tool column. Zero impact on the rest.

3. **Contrast from HTML** — htmlcheck does NOT check contrast. That's axe-core's job (it has the rendered page with computed styles). htmlcheck focuses on structural/semantic checks: heading order, missing labels, empty links, lang attribute, skip-link presence. Things you can detect from the raw HTML string.

4. **Scan time** — axe.run() takes 2-5 seconds per page. For 502 pages that's ~20-40 minutes at concurrency=1. But we already have MaxConcurrency=5 in config. At 5 parallel: ~4-8 minutes added. Acceptable since the current scan already takes significant time for screenshots + image downloads.

**[Quality]:** What about the existing `metadata.json`? Does it get a11y data too?

**[Backend]:** Yes. We add summary counts to metadata.json so downstream tools can consume it:

```json
{
    "PagePath": "/",
    "Url": "https://wsu.edu/",
    "StatusCode": 200,
    "ImageCount": 38,
    "ImagesMissingAlt": 13,
    "Accessibility": {
        "ToolsRun": ["axe", "htmlcheck"],
        "ToolsSkipped": ["pa11y"],
        "TotalViolations": 30,
        "Critical": 4,
        "Serious": 15,
        "Moderate": 7,
        "Minor": 4,
        "ByTool": {
            "axe": { "Total": 17, "Critical": 3, "Serious": 8, "Moderate": 4, "Minor": 2 },
            "htmlcheck": { "Total": 17, "Critical": 0, "Serious": 13, "Moderate": 3, "Minor": 1 }
        }
    }
}
```

**[Sanity]:** Final check — did we miss anything?

**[Quality]:** Two things:

1. **WCAG version target.** We should configure this. axe-core supports tags like `wcag2a`, `wcag2aa`, `wcag21aa`, `wcag22aa`. Our research says target WCAG 2.1 AA minimum, consider 2.2 AA. Make it configurable in `appsettings.json`.

2. **Historical tracking.** If we keep run artifacts (the `a11y-*.json` files), we can compare runs over time. "Last month: 1189 missing alt. This month: 340." But that's a future feature — just make sure we're saving the raw data.

**[Architect]:** Good. Add `WcagLevel` to `ScannerConfig`. Default to `"wcag21aa"`.

**[Skeptic]:** One more: the `a11y-summary.json` that merges results across tools — when is it written? Can it be generated later from the individual tool files?

**[Backend]:** Yes. The individual `a11y-{tool}.json` files are the source of truth. The summary is derived. We could generate it at scan time (convenient) or as a post-processing step. I'd say scan time — we already have all the data in memory, just write one more file.

---

## Decisions

1. **Three tools:** axe-core (primary, via Playwright injection), htmlcheck (custom C# structural checker), pa11y (external CLI, optional)
2. **Unified schema:** All tools normalize to `A11yIssue` with tool/ruleId/severity/message/selector/snippet/helpUrl
3. **File convention:** `a11y-{toolname}.json` per page, `a11y-summary.json` for merged view
4. **axe-core delivery:** Download `axe.min.js` from CDN on first run, cache in project dir
5. **Pa11y graceful skip:** Detect at startup, skip with `status: "skipped"` if not installed
6. **WCAG target:** Configurable, default `wcag21aa`
7. **Phased implementation:** axe-core first (Phase 1), htmlcheck (Phase 2), pa11y (Phase 3), cross-tool tables (Phase 4)
8. **Reports updated at all 3 levels:** Page gets violation tables, site gets aggregated top-10, run gets cross-site dashboard

### ADR: Tool Selection for ADA Scanning

**Context:** Need automated WCAG/ADA scanning that produces evidence-grade artifacts for 120+ WSU sites  
**Decision:** Use axe-core (injected via Playwright) as primary + htmlcheck (custom C#) as validator + pa11y (external CLI) as optional third perspective  
**Rationale:** axe-core is industry standard (~60% market share in accessibility testing), runs in-process with zero additional dependencies. htmlcheck adds structural validation from saved HTML with no network cost. Pa11y provides a third rules engine (HTML_CodeSniffer) for corroboration.  
**Consequences:** axe-core.min.js must be downloaded/cached. Pa11y requires Node.js (optional). Scan time increases by ~4-8 minutes for 502 pages at concurrency=5.  
**Alternatives rejected:** Lighthouse (scores rather than specific violations — less useful for fix-by-fix remediation), WAVE API (paid), custom-only scanner (reinventing what axe-core already does well)

---

## Planning Checklist

### Summary

- **Problem:** AccessibilityScanner captures pages but runs zero WCAG/ADA rule checks
- **Goal:** Run 3 ADA scanning tools per page, normalize results to a unified schema, produce cross-tool comparison reports at page/site/run levels
- **Non-goals:** Custom rules engine, manual testing automation, fixing violations (only reporting them), VPAT generation

### Acceptance Criteria

- [ ] axe-core runs on every scanned page and produces `a11y-axe.json`
- [ ] htmlcheck runs on every saved `page.html` and produces `a11y-htmlcheck.json`
- [ ] pa11y runs if installed, writes `a11y-pa11y.json` (or `status: "skipped"`)
- [ ] `a11y-summary.json` merges all tool results per page
- [ ] `metadata.json` includes `Accessibility` summary counts
- [ ] Page `report.md` has "♿ Accessibility" section with cross-tool comparison table
- [ ] Site `report.md` has accessibility rollup with top-10 issues
- [ ] Run `report.md` has cross-site accessibility dashboard with top-20 issues
- [ ] Existing screenshot/image/JS-error reporting is unchanged
- [ ] WCAG level is configurable via `appsettings.json`

### Approach

**New models (in `Program.cs` — bottom of file with existing models):**

| Class | Purpose |
|-------|---------|
| `A11yIssue` | Unified issue: tool, ruleId, severity, message, selector, snippet, helpUrl |
| `A11yToolResult` | Per-tool result: toolName, status, issueCount, durationMs, issues list |
| `A11yPageSummary` | Per-page merged: toolResults dict, totalViolations, severity counts |

**New methods (in `Program.cs`):**

| Method | Purpose |
|--------|---------|
| `RunAxeCoreAsync(IPage, string)` | Inject axe.min.js, run `axe.run()`, parse violations, return `A11yToolResult` |
| `RunHtmlCheckAsync(string, string)` | Parse saved HTML, run 15+ structural checks, return `A11yToolResult` |
| `RunPa11yAsync(string, string)` | Shell out to `pa11y --reporter json`, parse output, return `A11yToolResult` |
| `EnsureAxeCoreAsync(string)` | Download axe.min.js if not cached |
| `MergeA11yResults(...)` | Combine tool results into `A11yPageSummary` |
| `WriteA11yReportSection(...)` | Append accessibility section to page report.md |

**Modified files:**

| File | Change |
|------|--------|
| `Program.cs` | Add models, add 6 new methods, call from `ScanPageAsync`, update all 3 report methods |
| `appsettings.json` | Add `WcagLevel` setting |

**No new files needed** — everything stays in `Program.cs` consistent with the existing single-file pattern.

**No new NuGet packages** — axe-core is JavaScript injected via Playwright's existing `EvaluateAsync`. Pa11y is shelled out via `Process.Start`. htmlcheck is pure C# string parsing.

### Implementation Steps

| Step | Description | Est. Lines |
|------|-------------|:----------:|
| 1 | Add `A11yIssue`, `A11yToolResult`, `A11yPageSummary` models | ~60 |
| 2 | Add `WcagLevel` to `ScannerConfig`, update `appsettings.json` | ~5 |
| 3 | Implement `EnsureAxeCoreAsync` — download + cache axe.min.js | ~30 |
| 4 | Implement `RunAxeCoreAsync` — inject JS, parse violations | ~80 |
| 5 | Implement `RunHtmlCheckAsync` — 15+ structural HTML checks | ~150 |
| 6 | Implement `RunPa11yAsync` — shell out, parse JSON, handle missing | ~60 |
| 7 | Implement `MergeA11yResults` — combine and write JSON files | ~40 |
| 8 | Call a11y methods from `ScanPageAsync` after existing scan completes | ~20 |
| 9 | Add accessibility section to page `report.md` | ~80 |
| 10 | Add accessibility rollup to site `report.md` | ~50 |
| 11 | Add accessibility dashboard to run `report.md` | ~60 |
| 12 | Update `metadata.json` output with a11y summary | ~15 |
| **Total** | | **~650** |

### What htmlcheck Validates (15 Rules)

| Rule ID | Severity | What it checks |
|---------|----------|----------------|
| `img-alt` | serious | `<img>` without `alt` attribute |
| `img-alt-empty` | minor | `<img alt="">` on non-decorative images (heuristic) |
| `heading-order` | moderate | Heading levels skip (h1→h3) |
| `heading-missing-h1` | moderate | Page has no `<h1>` |
| `html-lang` | serious | `<html>` missing `lang` attribute |
| `html-lang-valid` | serious | `lang` attribute value not a valid BCP 47 code |
| `label-missing` | serious | `<input>` without associated `<label>` or `aria-label` |
| `link-empty` | serious | `<a>` with no text content and no `aria-label` |
| `button-empty` | serious | `<button>` with no text content and no `aria-label` |
| `skip-link-missing` | moderate | No skip-to-content link found |
| `landmark-main` | moderate | No `<main>` or `role="main"` element |
| `landmark-nav` | minor | No `<nav>` element |
| `div-button` | moderate | `<div>` with `onclick` but no `role="button"` |
| `tabindex-positive` | moderate | `tabindex` > 0 (disrupts natural tab order) |
| `meta-refresh` | moderate | `<meta http-equiv="refresh">` (disorienting) |

### Test Plan

- **Happy path:** Run against existing 120 sites — axe finds contrast/ARIA issues, htmlcheck finds structural issues, both report correctly
- **Edge cases:** Page with zero violations (empty sections render cleanly), page that times out (existing error handling, a11y runs skipped gracefully), site with no images (alt-text checks produce no issues)
- **Regression:** All existing report.md sections unchanged — screenshots, images, JS errors, actions all identical

### Ops & Rollout

- **Config:** New `WcagLevel` in `appsettings.json` (default: `"wcag21aa"`)
- **External deps:** Pa11y optional (`npm install -g pa11y`). axe.min.js auto-downloaded.
- **Rollback:** Remove the a11y method calls from `ScanPageAsync`. All existing functionality untouched.
- **Monitoring:** Console output during scan shows per-tool violation counts

### Docs

- [x] This planning doc (200)
- [ ] Update quickstart if new config keys added
- [ ] ADR recorded above
- [ ] Future: guide doc for reading/interpreting accessibility reports

---

## Open Questions

1. **Should we add a "grade" (A/B/C/D/F) per page?** Could be useful for executive reporting but risks oversimplification. Defer to Phase 4.
2. **Should htmlcheck also check `<table>` for `<th>`/`<caption>`?** Yes — add as rule `table-header-missing`. Good catch.
3. **Should we run axe-core on the post-auth page or pre-auth?** Both — we already take screenshots at multiple auth stages. Run axe on whichever state the page is in when we finish the auth flow.

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Implement Phase 1 (axe-core + models + page report) | [Implementer] | P1 |
| Implement Phase 2 (htmlcheck) | [Implementer] | P1 |
| Implement Phase 3 (pa11y optional) | [Implementer] | P2 |
| Implement Phase 4 (cross-tool comparison tables) | [Implementer] | P2 |
| Run full scan against 120 sites with a11y enabled | [Quality] | P1 |
| Review results, tune htmlcheck rules | [Quality] | P2 |
| Executive summary doc for ADA compliance evidence | [Doc Keeper] | P3 |

---

*Created: 2026-02-17*  
*Maintained by: [Quality]*
