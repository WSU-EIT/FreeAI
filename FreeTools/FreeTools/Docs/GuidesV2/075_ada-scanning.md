# 075 — Sweeping for ADA and Accessibility Gaps

> **Document ID:** 075  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Cover the accessibility scanner and how to turn its ADA findings into actionable fixes.
> **Audience:** Quality and tooling users  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Accessibility Scanning Matters](#why-it-matters) | Plain-language overview; ADA, WCAG, and a11y defined |
| 2 | [When to Run the Scanner](#when-to-use) | Triggers, cadence, and how scope is set in `appsettings.json` |
| 3 | [Running an Accessibility Scan](#running-a-scan) | The three tools, configuration, and `dotnet run` |
| 4 | [Reading the Findings Report](#reading-findings) | Severity, rule IDs, the cross-tool table, and consensus ranking |
| 5 | [Triaging Findings into Fixes](#triaging-fixes) | Using confidence and severity to prioritize work |
| 6 | [Applying and Verifying Fixes](#applying-fixes) | Common remediation patterns and re-scanning to confirm |
| 7 | [Roles and Handoffs](#roles) | Who owns scanning, triage, fixes, and signoff |
| 8 | [Pitfalls and False Positives](#pitfalls) | Automation limits, regex caveats, and manual checks still needed |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Accessibility Scanning Matters

**Why it matters first:** if a public website cannot be used by someone who is blind, has low vision, or navigates with a keyboard instead of a mouse, that is both a real harm to that person and a legal exposure for the organization. The `FreeTools.AccessibilityScanner` exists so you can find those gaps automatically, across hundreds of pages, and produce evidence you actually checked.

A few terms you will see constantly — defined in plain language:

- **Accessibility** — designing a page so people with disabilities can perceive, understand, and operate it. Think: a screen reader can announce every image, every button can be reached with the Tab key, text has enough contrast to read.
- **a11y** — shorthand for "accessibility." It is the word *accessibility* with the middle 11 letters replaced by `11`. The scanner's output files are all named `a11y-*.json`, so the abbreviation is worth knowing.
- **ADA** — the Americans with Disabilities Act, the U.S. law that requires services (increasingly including websites) to be usable by people with disabilities. ADA is the *why*; it does not list specific code rules.
- **WCAG** — the Web Content Accessibility Guidelines, the technical rulebook everyone actually tests against. It comes in versions (2.0, 2.1, 2.2) and conformance levels (A, AA, AAA). "WCAG 2.1 AA" is the common target — it is what the scanner aims at by default. Each rule has a numbered **success criterion** like `1.1.1` (images need text alternatives) or `1.4.3` (text needs enough contrast).

The scanner's job is to take a rendered web page and check it against WCAG rules, then report each violation with enough detail to fix it: which rule, how serious, which element on the page, and a link to documentation. Critically, it runs **three independent checkers** and compares them, because no single automated tool catches everything — and the report itself says so: *"Automated scanning catches ~30-60% of WCAG issues. Manual keyboard and screen reader testing is still required for full compliance."*

---

<a id="when-to-use"></a>
## 2. When to Run the Scanner

**Why it matters:** accessibility regresses silently. A content editor pastes an image without alt text, a redesign drops the contrast ratio, a new form ships without labels — none of these break the build, so nothing flags them. Scheduled scanning is how you catch drift before an auditor or a complaint does.

Good triggers for a run:

- **On a cadence** — monthly or quarterly across all public sites, to track whether the violation count is trending down. The scanner deletes the previous run and writes a fresh `runs/latest/` each time, so each run is a clean snapshot you can archive.
- **Before a launch or redesign** — scan the new pages before they go live.
- **After a templating or theme change** — one shared header or footer change can fix (or break) every page at once.
- **When responding to a specific complaint** — point the scanner at the page in question for a fast, detailed readout.

**Scope is set entirely in `appsettings.json`** under the `Scanner` section. You list each site as a URL key, and under it a `Pages` array of paths to scan. The root page (`/`) of every site is **always** scanned in addition to whatever you list:

```json
"Sites": {
  "https://yoursite.example.com/": {
    "Credentials": [],
    "Pages": [ "/about/", "/contact/" ]
  }
}
```

A site with an empty `Pages` array still gets its root page scanned. `Credentials` lets you supply a username/password so the scanner can log in and scan pages behind authentication; leave it empty (`[]`) for public pages. The shipped config scans well over a hundred WSU sites this way, so "scope" in practice means deciding which sites and paths belong in that map.

---

<a id="running-a-scan"></a>
## 3. Running an Accessibility Scan

**Why it matters:** the scanner is a real browser driving real pages, so understanding what it does per page tells you why a run takes time and what the output means.

### The three checkers

The scanner runs up to three independent accessibility engines on every page and normalizes their results into one shared format. They catch different things on purpose:

| Tool name | What it is | How it runs | Needs |
|-----------|-----------|-------------|-------|
| `axe` | **axe-core**, the industry-standard engine (~90 WCAG rules including contrast and ARIA) | JavaScript injected into the live page via Playwright, then `axe.run()` | Internet (downloads `axe.min.js` once) |
| `htmlcheck` | A **custom C# checker** built into the tool | Pure C# string/regex parsing of the saved HTML | Nothing — no network, no install |
| `wave` | **WAVE API** by WebAIM, a hosted third-party engine | An HTTPS call to `wave.webaim.org` | A paid API key in config |

> **Note on the third tool:** the shipped code uses **WAVE API** as the optional third result engine. The Pa11y/HTML_CodeSniffer engine still appears in the tool — but as a *screenshot overlay* (`htmlcs-overlay`), not as a result file.

**axe-core delivery.** axe-core is JavaScript. The tool downloads it once from a CDN and caches it next to the project as `axe.min.js`, then reuses the cached copy on every later run:

```csharp
private const string AxeCdnUrl = "https://cdn.jsdelivr.net/npm/axe-core@4.10.2/axe.min.js";
```

**WAVE is optional.** If `WaveApiKey` is blank, the tool simply records WAVE as `"skipped"` and runs with axe + htmlcheck. The startup banner confirms this with `WAVE API: disabled (no key)`.

### Configure `appsettings.json`

The `Scanner` block controls everything (defaults shown reflect the real `ScannerConfig`):

| Key | Default | Meaning |
|-----|---------|---------|
| `SettleDelayMs` | `3000` | How long to wait after load before scanning, so dynamic content settles |
| `TimeoutMs` | `10000` | Page navigation timeout |
| `MaxConcurrency` | `5` | How many sites to scan in parallel |
| `Headless` | `true` | Run the browser invisibly; set `false` to watch it |
| `WcagLevel` | `"wcag21aa"` | Which WCAG version/level axe targets |
| `WaveApiKey` | `""` | WAVE API key — empty disables WAVE |
| `Sites` | — | The site → pages map from Section 2 |

`WcagLevel` maps to the set of rule tags axe runs. For example `wcag21aa` runs `['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']`; `wcag22aa` adds `wcag22aa` on top. Lower the level to `wcag2a` for a smaller, stricter-blockers-only scan.

### Run it

```bash
cd FreeTools/FreeTools.AccessibilityScanner
dotnet run
```

On first run the tool installs the Chromium browser for Playwright automatically. Then, per page, it: navigates and waits out the settle delay, takes a screenshot, expands collapsed accordions/tabs (so hidden content gets scanned too), saves the rendered HTML to `page.html`, catalogs images, runs the three checkers, and finally captures a series of accessibility **overlay screenshots** (axe highlights, a WAVE-style icon view, the HTML_CodeSniffer and IBM Equal Access engines, a structure/landmark view, seven color-blindness simulations, and a linearized screen-reader text view). The console prints a live per-page tally like `A11y: 30 total violations (axe:17 htmlcheck:17 wave:0)`.

---

<a id="reading-findings"></a>
## 4. Reading the Findings Report

**Why it matters:** a raw count of "30 violations" tells you nothing actionable. The report is structured so you can see *which rules*, *how confident the tools are*, and *what to look at first*. Each scanned page produces a `report.md`, plus machine-readable JSON.

### The files per page

| File | What's in it |
|------|--------------|
| `a11y-axe.json` | axe-core's raw violations (one of these per result tool) |
| `a11y-htmlcheck.json` | The C# checker's violations |
| `a11y-wave.json` | WAVE's violations, or `"status": "skipped"` |
| `a11y-summary.json` | The **merged** view: totals, per-tool counts, and the consensus ranking |
| `report.md` | The human-readable page report with the `♿ Accessibility` section |

### The unified issue

Every finding, no matter which tool produced it, is normalized to the same `A11yIssue` shape so they can be compared:

```csharp
internal class A11yIssue
{
    public string Tool { get; set; }              // "axe" | "htmlcheck" | "wave"
    public string RuleId { get; set; }            // tool's own rule name
    public string CanonicalRuleId { get; set; }   // shared name for cross-tool matching
    public string Severity { get; set; }          // critical | serious | moderate | minor
    public string Message { get; set; }
    public string? Selector { get; set; }         // CSS selector for the element
    public string? Snippet { get; set; }          // the offending HTML
    public string? HelpUrl { get; set; }
    public string? WcagCriteria { get; set; }     // e.g. "1.1.1"
}
```

**Severity** is the first thing to read. The four levels, in order of urgency:

- 🔴 **critical** — blocks access outright (e.g., an image with no alt text that conveys meaning)
- 🟠 **serious** — a major barrier (e.g., low contrast text, an unlabeled form field)
- 🟡 **moderate** — degrades the experience (e.g., a skipped heading level)
- 🔵 **minor** — a smaller issue (e.g., a missing `<nav>` landmark)

For axe, severity comes straight from axe's own `impact` field. For htmlcheck and WAVE the tool assigns severity per rule (for example, WAVE's `error` and `contrast` categories map to `serious`, its `alert` category to `moderate`).

**CanonicalRuleId** is the clever bit. Each tool names rules differently — htmlcheck calls a missing-alt issue `img-alt`, axe calls it `image-alt`, WAVE calls it `alt_missing`. The tool maps all of them to one canonical id (`image-alt`) so it can tell when two tools found *the same problem*.

### The cross-tool table and consensus ranking

In `report.md`, the `♿ Accessibility` section first shows a severity summary with one column per tool, then a **Violations by Confidence** table. Confidence answers: "how much should I trust this finding?" The tool computes it by checking how many of the *capable* tools agreed:

```
score = (tools that found the rule) / (tools capable of checking that rule)
confidence = score >= 0.8 ? "high" : score >= 0.5 ? "medium" : "low"
```

The "capable" denominator matters because not every tool can check every rule — htmlcheck cannot compute color contrast, for instance, so it is excluded from the denominator for `color-contrast`. A rule flagged by every tool that *can* check it gets 🟢 high confidence; a rule only one of three tools caught gets 🔵 low. The table is sorted high-confidence-first, then by severity, then by how many instances were found, and each row links out to a rules legend (`a11y-rules.md`) explaining the rule.

A run also produces site-level and run-level rollups so you can see top issues across many pages, plus a single `a11y-rules.md` legend describing every rule that appeared.

---

<a id="triaging-fixes"></a>
## 5. Triaging Findings into Fixes

**Why it matters:** a full scan can surface thousands of issues. Triage is how you turn that pile into a short, ordered list of work that actually moves compliance forward. The report is already built to support this — you mostly read it in the right order.

A practical triage order:

1. **Sort by confidence, then severity.** The `Violations by Confidence` table already does this. Start at the top: 🟢 high-confidence 🔴 critical rows are findings multiple independent engines agree on *and* that block access. These are the safest, highest-value fixes — almost never false positives.
2. **Group by rule, not by instance.** The ranking groups by `CanonicalRuleId` and shows `TotalInstances`. One rule like `image-alt` might appear 89 times across a site. That is usually *one* root cause (a template, a content pattern) — fix it once and clear dozens of instances.
3. **Prefer template-level fixes.** Because the same shared header, footer, or component appears on every page, a single fix often resolves the issue site-wide. Check whether a top rule appears on most pages (the site rollup's "Pages" column, rendered as N/total, tells you) — broad spread points to a shared component.
4. **Defer low-confidence findings to manual review.** A 🔵 low-confidence row (only one tool flagged it, and other capable tools did not) is a candidate for a human to confirm before any code changes. It is not necessarily wrong — it may be something only that engine checks — but it warrants a look rather than a blind fix.

Each finding already carries everything a work item needs: the rule id, the WCAG success criterion, a `HelpUrl` to documentation, the CSS `Selector`, and an HTML `Snippet`. Copying those into a ticket gives an engineer a self-contained task.

---

<a id="applying-fixes"></a>
## 6. Applying and Verifying Fixes

**Why it matters:** the value is in the fix, not the report. Most violations map to a small set of well-known remediation patterns, and the same scanner you used to find them is how you prove they are gone.

### Common patterns, by canonical rule

These are the rules the scanner actually checks (the canonical ids it emits) and how each is typically resolved:

| Canonical rule | What's wrong | Typical fix |
|----------------|--------------|-------------|
| `image-alt` | `<img>` has no alt text | Add `alt="..."` describing the image; use `alt=""` only for purely decorative images |
| `color-contrast` | Text is too faint against its background | Darken the text or lighten the background to meet the WCAG ratio (axe-only check) |
| `label` | A form input has no associated label | Add a `<label for="id">` or an `aria-label` |
| `link-name` | A link has no readable text | Add link text, or an `aria-label` for icon-only links |
| `button-name` | A button has no readable text | Same — add text content or `aria-label` |
| `html-has-lang` | `<html>` has no `lang` | Add `lang="en"` (or the correct language) to the `<html>` tag |
| `heading-order` | Heading levels skip (e.g. `<h1>`→`<h3>`) | Don't skip levels; use CSS for size, headings for structure |
| `page-has-heading-one` | Page has headings but no `<h1>` | Give each page exactly one `<h1>` |
| `skip-link` | No "skip to content" link near the top | Add a skip link as the first focusable element |
| `landmark-one-main` | No `<main>` region | Wrap the primary content in `<main>` |
| `div-button` | A `<div onclick>` acts like a button but isn't one | Use a real `<button>`, or add `role="button"` and keyboard handling |
| `td-has-header` | Data-table cells have no header cells | Add `<th>` headers (and a `<caption>`) |

The htmlcheck engine specifically checks: image alt, heading order, missing `<h1>`, `<html lang>`, form labels, empty links, empty buttons, skip links, `<main>` and `<nav>` landmarks, `<div onclick>` buttons, positive `tabindex`, `<meta http-equiv="refresh">`, and table headers. axe adds the deeper checks htmlcheck cannot do from raw HTML — color contrast and most ARIA validity rules.

### Verify by re-scanning

After deploying fixes, run the scanner again on the affected pages. Because each run wipes and rewrites `runs/latest/`, the simplest verification is: the rule that had a violation count now reads `✅` (zero), or the page's `♿ Accessibility` section shows `✅ No violations detected`. For trend tracking across runs, archive the `a11y-*.json` files from each run before the next one overwrites them — they are the source of truth and the report is derived from them.

---

<a id="roles"></a>
## 7. Roles and Handoffs

**Why it matters:** accessibility work stalls when "everyone's job" becomes no one's. A clean handoff chain keeps findings moving from detection to signoff.

| Role | Owns |
|------|------|
| **Tooling / Quality** | Configuring `appsettings.json` (which sites and pages), running the scan, and obtaining/rotating the WAVE API key |
| **Quality / Triage** | Reading the report, sorting by confidence and severity, grouping by rule, and writing work items from the rule id + selector + snippet |
| **Engineering / Content** | Applying fixes — engineers for template, component, and code issues; content editors for per-page problems like missing alt text |
| **Quality (again)** | Re-scanning affected pages to confirm the violation count dropped to zero |
| **Accessibility lead / Approver** | Signing off, and owning the manual checks (keyboard navigation, screen reader testing) that automation cannot cover |

The natural handoff points are: a triaged, ranked list goes from Quality to the people who fix things; the fixed pages come back to Quality for a confirmation re-scan; and anything the scanner flagged as low confidence — or anything in the "manual still required" bucket — goes to the accessibility lead.

---

<a id="pitfalls"></a>
## 8. Pitfalls and False Positives

**Why it matters:** trusting the scanner *too much* is its own risk. Knowing where automated checks fall short keeps you from declaring victory prematurely — and from chasing findings that aren't real.

- **Automation only catches ~30–60% of WCAG issues.** The report says this in every page with violations, and it is the single most important caveat. A clean scan does **not** mean an accessible page. Keyboard-only navigation, screen reader testing, and judgment calls (is this alt text *meaningful*, or just present?) all require a human.
- **htmlcheck is regex, not a full parser.** The C# checker matches tags with regular expressions over the HTML string. That is fast and dependency-free, but it can misread unusual or malformed markup. It also cannot evaluate anything that depends on rendered styling — most importantly **it cannot check color contrast** (only axe can, because axe runs in the live page with computed styles).
- **Tools disagree, and that's the point — but it creates noise.** A finding from only one of three engines (low confidence) may be a true issue that engine uniquely checks, *or* a quirk of that engine. Don't auto-fix low-confidence rows; route them to manual review. The confidence column exists precisely to separate "definitely real" from "look closer."
- **Decorative vs. meaningful alt text.** The scanner flags missing `alt` attributes, but it cannot tell whether an image is decorative (correctly `alt=""`) or meaningful (needs a description). Both decisions are human.
- **WAVE may be absent.** If no API key is configured, WAVE is skipped and its column shows `—`/skipped. That isn't a failure, but it means you are running on two engines, not three, which can lower the consensus denominator for some rules.
- **Snapshot, not history.** Each run deletes the previous `runs/latest/`. If you want to show a trend ("missing-alt down from 1189 to 340"), you must archive the JSON artifacts yourself between runs — the tool does not retain history automatically.
- **Dynamic content timing.** The scanner waits `SettleDelayMs` and expands collapsed sections before scanning, but content that loads very late (or only on interaction the scanner doesn't perform) may not be evaluated. If a page is heavily dynamic, sanity-check it manually.

---

<a id="related-docs"></a>
## 9. Related Docs

- [074 — Screenshots Without a Browser Window](074_headless-screenshots.md) — the captured pages it scans
- [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md) — the a11y baseline it checks against
- [076 — Building On the FreeTools Core Library](076_tooling-core-library.md) — shared scanning utilities

---
*GuidesV2 075 · drafted from source (`FreeTools.AccessibilityScanner`) · 2026-06-05.*
