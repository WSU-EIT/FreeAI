# Accessibility Guidelines — Standards FreeA11yChecker Tests Against

Maintained by [WSU Enrollment Information Technology (EIT)](https://em.wsu.edu/eit/).

This document explains every accessibility standard, law, and policy that drives what
FreeA11yChecker scans for. Written for the Washington State context (WSU + state agency
sites) but the standards themselves are global.

> **TL;DR for our deadline:**
> WSU is bound by WCAG 2.1 Level AA today, with WCAG 2.2 Level AA expected by
> July 1, 2026 under Washington State Technology Policy USER-01. The DOJ Title II rule
> compliance date was extended to **April 26, 2027** for entities of 50,000+ population
> (which includes WSU), but WSU's internal guidance is explicit: *"this IS NOT a grace
> period for accessible content."* All non-archived content must already comply.

---

## 1. The compliance pyramid (from federal down to WSU)

```
┌─────────────────────────────────────────────────────────────────────┐
│  FEDERAL — Americans with Disabilities Act (ADA) Title II           │
│  + DOJ Final Rule (April 24, 2024) — mandates WCAG 2.1 Level AA     │
│  for state and local government web content + mobile apps.          │
│  Compliance: April 26, 2027 (50K+ entities, WSU included)           │
│              April 26, 2028 (smaller jurisdictions, special dist.)  │
├─────────────────────────────────────────────────────────────────────┤
│  FEDERAL — Section 508 of the Rehabilitation Act                    │
│  Applies to federal procurement; aligned with WCAG 2.0 AA via the   │
│  2018 Section 508 Refresh. Often referenced for state procurement.  │
├─────────────────────────────────────────────────────────────────────┤
│  STATE — Washington State Technology Policy USER-01                 │
│  Digital Accessibility Policy, owned by WaTech. Mandates WCAG 2.1   │
│  Level AA at minimum for ALL state agency digital services.         │
│  Adopts WCAG 2.2 Level AA effective July 1, 2026.                   │
│  All-job-tools-and-public-access deadline: July 1, 2029.            │
├─────────────────────────────────────────────────────────────────────┤
│  INSTITUTIONAL — WSU UPPM 10.45 (formerly EP07 + BPPM 85.55)        │
│  WSU Electronic and Information Technology (EIT) Accessibility      │
│  Policy. Merged effective September 2025. Requires every employee   │
│  who creates or publishes digital content to ensure it's accessible.│
│  Annual training mandatory for all WSU web publishers.              │
└─────────────────────────────────────────────────────────────────────┘
```

### What this means for WSU web properties

WSU digital services operated by Enrollment Information Technology (EIT) and other
departments are subject to UPPM 10.45,
USER-01, and ADA Title II. The applicable conformance target is **WCAG 2.1 Level AA**
today, **WCAG 2.2 Level AA** as of July 1, 2026.

---

## 2. WCAG 2.1 Level AA — the canonical checklist

WCAG 2.1 (W3C Recommendation, June 5, 2018) is organized under **four principles**:
**P**erceivable, **O**perable, **U**nderstandable, **R**obust ("POUR"). Each principle
contains guidelines, each guideline contains testable success criteria. Level A is the
minimum; Level AA is the legal default. Level AAA is aspirational and not required by
any law cited above.

To be Level AA conformant, content MUST pass every Level A AND every Level AA criterion.
Below is every criterion at A and AA, the level you must hit, and a one-line summary.

### Principle 1 — Perceivable
Information and UI components must be presentable in ways users can perceive.

| #     | Title                              | Lvl | Requirement |
|-------|------------------------------------|-----|-------------|
| 1.1.1 | Non-text Content                   | A   | Every image, icon, control, captcha has a text alternative |
| 1.2.1 | Audio-only and Video-only (Prerec) | A   | Provide a transcript for audio; description for video       |
| 1.2.2 | Captions (Prerecorded)             | A   | Synchronized captions for prerecorded video                 |
| 1.2.3 | Audio Description or Media Alt     | A   | Audio description OR full-text alternative for video        |
| 1.2.4 | Captions (Live)                    | AA  | Synchronized captions for live video streams                |
| 1.2.5 | Audio Description (Prerecorded)    | AA  | Audio description track for prerecorded video               |
| 1.3.1 | Info and Relationships             | A   | Structure (headings, lists, tables, form labels) is in markup, not just visual |
| 1.3.2 | Meaningful Sequence                | A   | DOM/reading order matches visual order                      |
| 1.3.3 | Sensory Characteristics            | A   | Don't rely on shape/color/position alone ("click the round button") |
| 1.3.4 | Orientation                        | AA  | Don't lock to portrait or landscape unless essential        |
| 1.3.5 | Identify Input Purpose             | AA  | Form fields collecting user info use HTML autocomplete tokens |
| 1.4.1 | Use of Color                       | A   | Color is not the ONLY means of conveying information        |
| 1.4.2 | Audio Control                      | A   | Auto-playing audio over 3s has pause/stop/volume controls   |
| 1.4.3 | Contrast (Minimum)                 | AA  | Text contrast ≥ 4.5:1; large text (18pt or 14pt bold) ≥ 3:1 |
| 1.4.4 | Resize Text                        | AA  | User can zoom to 200% without breaking layout/functionality |
| 1.4.5 | Images of Text                     | AA  | Use real text instead of images of text (with limited exceptions) |
| 1.4.10| Reflow                             | AA  | Content reflows for 320px viewport without 2D scrolling     |
| 1.4.11| Non-text Contrast                  | AA  | UI components and graphical objects have ≥ 3:1 contrast vs adjacent colors |
| 1.4.12| Text Spacing                       | AA  | No content loss when users override line/word/letter spacing |
| 1.4.13| Content on Hover or Focus          | AA  | Tooltips/popovers are dismissible, hoverable, persistent    |

### Principle 2 — Operable
UI components and navigation must be operable.

| #     | Title                          | Lvl | Requirement |
|-------|--------------------------------|-----|-------------|
| 2.1.1 | Keyboard                       | A   | All functionality reachable and operable via keyboard alone |
| 2.1.2 | No Keyboard Trap               | A   | Keyboard focus can always move away from any component      |
| 2.1.4 | Character Key Shortcuts        | A   | Single-key shortcuts can be turned off or remapped          |
| 2.2.1 | Timing Adjustable              | A   | Time limits are adjustable, extendable, or removable        |
| 2.2.2 | Pause, Stop, Hide              | A   | Auto-moving / blinking / scrolling content can be paused    |
| 2.3.1 | Three Flashes or Below         | A   | No content flashes more than 3 times per second             |
| 2.4.1 | Bypass Blocks                  | A   | "Skip to main content" link or equivalent                   |
| 2.4.2 | Page Titled                    | A   | Every page has a `<title>` describing topic or purpose      |
| 2.4.3 | Focus Order                    | A   | Tab order matches visual / logical reading order            |
| 2.4.4 | Link Purpose (In Context)      | A   | Link text alone (or with surrounding context) makes purpose clear |
| 2.4.5 | Multiple Ways                  | AA  | Multiple ways to find each page (nav, search, sitemap)      |
| 2.4.6 | Headings and Labels            | AA  | Headings and labels describe topic/purpose                  |
| 2.4.7 | Focus Visible                  | AA  | Visible keyboard focus indicator on every focusable element |
| 2.5.1 | Pointer Gestures               | A   | Multi-point/path-based gestures have single-point alternative |
| 2.5.2 | Pointer Cancellation           | A   | Single-pointer events can be cancelled (no surprise actions on down-event) |
| 2.5.3 | Label in Name                  | A   | Visible label is contained in the accessible name           |
| 2.5.4 | Motion Actuation               | A   | Device-motion features (shake-to-undo) have alternatives    |

### Principle 3 — Understandable
Information and UI operation must be understandable.

| #     | Title                                 | Lvl | Requirement |
|-------|---------------------------------------|-----|-------------|
| 3.1.1 | Language of Page                      | A   | `<html lang="en">` (or correct language) on every page      |
| 3.1.2 | Language of Parts                     | AA  | Language changes within a page are marked with `lang="..."` |
| 3.2.1 | On Focus                              | A   | Focusing an element doesn't unexpectedly change context     |
| 3.2.2 | On Input                              | A   | Changing a setting doesn't unexpectedly change context      |
| 3.2.3 | Consistent Navigation                 | AA  | Repeated nav appears in same relative order across pages    |
| 3.2.4 | Consistent Identification             | AA  | Same component identified the same way across the site      |
| 3.3.1 | Error Identification                  | A   | Form errors are described to the user in text               |
| 3.3.2 | Labels or Instructions                | A   | Form inputs have visible labels or instructions             |
| 3.3.3 | Error Suggestion                      | AA  | Suggest corrections for known errors (when possible)        |
| 3.3.4 | Error Prevention (Legal/Financial)    | AA  | Reversible / verifiable / confirmable submissions for high-stakes actions |

### Principle 4 — Robust
Content must be robust enough to work with current and future user agents (browsers, AT).

| #     | Title              | Lvl | Requirement |
|-------|--------------------|-----|-------------|
| 4.1.2 | Name, Role, Value  | A   | Every UI control exposes its name, role, and current state to AT |
| 4.1.3 | Status Messages    | AA  | Status updates announced to screen readers without taking focus |

> **Note:** WCAG 2.1's 4.1.1 Parsing was made obsolete and removed in WCAG 2.2 — modern
> HTML parsers no longer fail on the kinds of issues this rule was designed to catch.

---

## 3. WCAG 2.2 — what's new (effective July 1, 2026 in WA)

WCAG 2.2 (W3C Recommendation, October 5, 2023) keeps everything from 2.1 and adds nine
new criteria. Five of them are at Level AA (so they become required for WSU on July 1,
2026). Two are at Level A. Three are AAA (still aspirational).

| #     | Title                                | Lvl | Requirement |
|-------|--------------------------------------|-----|-------------|
| 2.4.11| Focus Not Obscured (Minimum)         | AA  | When an element receives focus, it's at least partially visible (not entirely hidden behind a sticky header / cookie banner / sidebar) |
| 2.4.12| Focus Not Obscured (Enhanced)        | AAA | When focused, fully visible (no overlap at all) |
| 2.4.13| Focus Appearance                     | AAA | Focus indicator meets explicit minimum size + contrast |
| 2.5.7 | Dragging Movements                   | AA  | Anything done by dragging has a single-pointer alternative (e.g. tap to reorder) |
| 2.5.8 | Target Size (Minimum)                | AA  | Click/tap targets ≥ 24×24 CSS pixels (with adequate spacing if smaller) |
| 3.2.6 | Consistent Help                      | A   | "Contact support" / help mechanisms appear in the same location across pages |
| 3.3.7 | Redundant Entry                      | A   | Don't ask the user to enter the same info twice in the same session (or auto-fill it) |
| 3.3.8 | Accessible Authentication (Minimum)  | AA  | Login can't require solving puzzles, recalling info, or transcribing (no captcha-only logins) |
| 3.3.9 | Accessible Authentication (Enhanced) | AAA | Stricter version: no object recognition, no user-supplied media identification |

> **Removed in 2.2:** 4.1.1 Parsing — the old "no duplicate IDs / valid markup" rule —
> is officially obsolete and no longer required. Modern browsers handle the cases it
> was meant to flag.

---

## 4. The four scanning engines we run, and what each catches

FreeA11yChecker is a **clean-room implementation** (no commercial tooling), but
it composes four well-known open-source / free engines and computes consensus across them.

| Tool | What it covers | Strengths | Limitations |
|------|---------------|-----------|-------------|
| **axe-core** (Deque, MIT-licensed) | ~90 WCAG 2.1/2.2 rules, primary scanner | Highest signal/noise ratio; rich `HelpUrl` per rule; minimal false positives | Doesn't catch contrast on dynamic backgrounds; doesn't flag missing skip-link |
| **IBM Equal Access** (IBM, EPL-licensed) | ~150 rules including detailed ARIA validation, color-contrast, table semantics | Catches things axe misses (e.g., aria-required-children); reports manual-check items separately | High volume of "informational" notices that aren't failures; required custom filtering |
| **HTML CodeSniffer** (Squiz, BSD-licensed) | WCAG 2.0 AA rule set focusing on structural/markup issues | Very fast; runs entirely in-browser; clean rule mapping | Slightly older WCAG 2.0 baseline; less coverage than axe/IBM for ARIA |
| **HtmlChecker** (custom regex audit, this repo) | Pattern checks not covered by the above: missing `<main>`, missing skip-link, `<button>` with no name, `<img>` without `alt`, heading hierarchy, `<table>` without `<th>` | Fills gaps the JS-based scanners miss; runs against raw HTML so reaches things hidden during scan | Regex-based — won't catch DOM-reachability nuances |

We also generate **clean-room QuickPeek overlays** (visual outline of regions / errors),
**Color Vision Deficiency (CVD) simulations** for 7 visions, **screen reader view**
captures, **forced-colors / reduced-motion** screenshots, plus a designer-style
**wireframe blueprint** showing landmarks color-coded.

### Coverage gaps we ACKNOWLEDGE

These WCAG criteria CANNOT be reliably auto-tested by any of these engines and require
manual review:

- 1.2.1 — 1.2.5 video/audio criteria (we don't analyze media content)
- 1.3.1 / 1.3.2 — depends on DOM/visual order alignment (partial coverage)
- 1.4.1 — color-as-only-indicator can't be detected programmatically
- 2.1.1 / 2.1.2 — keyboard traps require interactive testing (we have a Phase-3 keyboard nav check, opt-in)
- 2.4.4 — link purpose-in-context needs human judgment of surrounding text
- 3.3.1 / 3.3.3 — error message quality requires manual review
- All of WCAG 2.2's 3.3.x authentication criteria — we test login but don't grade its accessibility

For these, the scanner prints a "manual review needed" note in the report so a human
auditor knows what to verify.

---

## 5. Compliance deadlines, ranked by urgency

| Date | Deadline | Source |
|------|----------|--------|
| **NOW** | All non-archived WSU content must already comply with WCAG 2.1 AA per UPPM 10.45 | WSU policy |
| **July 1, 2026** | WA state agencies expected to meet WCAG 2.2 Level AA | WA Tech Policy USER-01 |
| **April 26, 2027** | DOJ Title II rule compliance for state/local govt entities ≥ 50,000 population (incl. WSU) | DOJ 2024 final rule, extended April 2026 |
| **April 26, 2028** | DOJ Title II compliance for smaller jurisdictions and special districts | Same |
| **July 1, 2029** | All tools necessary for job performance or public access must be accessible | WA USER-01 |

> The DOJ extension does NOT relax the underlying ADA obligation — it relaxes the rule's
> specific WCAG-conformance enforcement timeline. ADA Title II requires equal access
> today; "not yet WCAG-compliant" is not a defense against an individual complaint.

---

## 6. Practical "what to fix first" priority

When triaging a scan report, fix in this order — each tier has dramatically more impact
than the next:

1. **Site-wide structural issues** — missing `<main>` / `<html lang>` / skip-link,
   empty headings in shared layout. Fix once in `MainLayout`, fixes every page.
2. **Repeated component issues** — form controls without `<label>`, buttons without
   accessible name, links without text, missing alt attributes on shared icons.
   Fix once in the component, fixes every instance.
3. **Color contrast** — when it appears site-wide it's a CSS/theme issue. One token
   change typically fixes hundreds of violations.
4. **Tables** — every data table needs `<th scope="col">` headers and a `<caption>`.
5. **ARIA misuse** — invalid attributes, mismatched roles, `aria-hidden` on focusable
   elements. Per-component fixes.
6. **Page-specific oddities** — one-off forms, custom interactions. Last because they
   don't cascade.

---

## Sources

- [W3C WCAG 2.1 (Level A and AA quickref)](https://www.w3.org/WAI/WCAG21/quickref/?versions=2.1&levels=a%2Caa)
- [W3C WCAG 2.2 — what's new](https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/)
- [ADA.gov — Web Accessibility Guidance](https://www.ada.gov/resources/web-guidance/)
- [ADA.gov — 2024 Title II Web Rule fact sheet](https://www.ada.gov/resources/2024-03-08-web-rule/)
- [WaTech — Accessibility (Washington State)](https://watech.wa.gov/accessibility)
- [WaTech — Digital Accessibility Policy USER-01](https://watech.wa.gov/policies/digital-accessibility-policy)
- [WSU — Digital Accessibility (canonical entry point)](https://wsu.edu/digital-accessibility/)
- [WSU — Policies (UPPM 10.45 entry)](https://wsu.edu/digital-accessibility/policies/)
- [WSU Web Communication — DOJ extension guidance (April 2026)](https://web.wsu.edu/blog/2026/04/20/guidance-on-the-dojs-extension-of-accessibility-compliance-dates/)
- [WSU CAHNRS — Digital Accessibility Resources](https://communications.cahnrs.wsu.edu/digital-accessibility-resources/)
- [WSU HRS — Digital Accessibility: Empowering Everyone to Participate](https://hrs.wsu.edu/digital-accessibility-empowering-everyone-to-participate/)
- [WSU CRMO — Digital Accessibility Subcommittee](https://crmo.wsu.edu/digital-accessibility-subcommittee/)
- [WSU Access — Communications Guidelines](https://access.wsu.edu/guidelines-and-policies/communications-guidelines/)

---

*Last updated: 2026-04-23. This document is informational, not legal advice. The official
text of UPPM 10.45 and USER-01 controls in any conflict.*

---

*© Washington State University — Enrollment Information Technology*  
*github.com/WSU-EIT*
