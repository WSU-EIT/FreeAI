# 201 — Meeting: Sanity Check — Are These the Right Tools?

> **Document ID:** 201  
> **Category:** Meeting  
> **Purpose:** Validate that our 3-tool approach matches what professionals actually use for ADA audits  
> **Attendees:** [Sanity], [JrDev], [Quality], [Architect]  
> **Date:** 2026-02-17  
> **Predicted Outcome:** Confidence that our tooling matches industry practice (or course-correct if not)  
> **Actual Outcome:** ✅ Confirmed — axe-core is the de facto standard; our approach exceeds typical manual audits  
> **Resolution:** Proceed with doc 200 plan; added manual testing recommendations for Phase 5  

---

## Context

Doc 200 designed a 3-tool architecture (axe-core, htmlcheck, pa11y). Before we build it, [Sanity] wants to ask the uncomfortable question: **are we building this because it's the right approach, or because we designed ourselves into it?**

---

## Discussion

**[Sanity]:** Stop. Before we write a single line of code, I want to run a thought experiment. Forget our plan. Forget our research docs. Pretend none of that exists.

The CEO walks in right now and says: *"Someone filed an ADA complaint. I need to know by Friday if our 120 websites are accessible. Go test them."*

What do you actually do?

**[JrDev]:** Honestly? I'd Google "how to test website ADA compliance" and see what comes up.

**[Sanity]:** Good. Let's do exactly that. What does a person find when they search that in 2026?

**[Quality]:** Here's what you'd find, in the order you'd find it:

### What Google/Industry Actually Recommends

**Step 1: Browser extensions (5 minutes to install, immediate feedback)**

| Tool | Cost | What it does |
|------|------|--------------|
| **axe DevTools** (Deque) | Free | Click a button, see violations with severity + code snippets |
| **WAVE** (WebAIM) | Free | Visual overlay showing issues directly on the page |
| **Lighthouse** (Google) | Free (built into Chrome) | Accessibility score 0-100, list of failing audits |

Every single accessibility guide — WebAIM, Deque, W3C, Section508.gov — recommends axe DevTools as the first tool to try.

**Step 2: Manual keyboard test (zero tools, 2 minutes per page)**

- Press Tab repeatedly. Can you reach every interactive element?
- Can you see where focus is?
- Can you operate menus, modals, forms with just keyboard?
- Press Escape to close things?

**Step 3: Screen reader test (free, 10 minutes per page)**

- NVDA (free, Windows) or VoiceOver (built into Mac)
- Does the page make sense when read aloud?
- Are images described? Are forms labeled? Do headings create a logical outline?

**Step 4: Automated CLI scanning (for scale)**

- `@axe-core/cli` — same engine as the browser extension, but command-line
- `pa11y` — different engine (HTML_CodeSniffer), catches different things
- `lighthouse --only-categories=accessibility` — Google's scoring

**[Sanity]:** OK. So in the "CEO says go test by hand" scenario, the FIRST thing every professional reaches for is **axe**. The exact same engine we're using. That's not a coincidence?

**[Architect]:** It's not a coincidence. axe-core has become the de facto standard for automated accessibility testing. Here's why:

### axe-core Market Position

| Fact | Source |
|------|--------|
| **axe-core is used by Microsoft, Google, and the U.S. government** | Deque's public customer list |
| **axe-core powers the accessibility audits in Chrome Lighthouse** | Google's Lighthouse uses axe-core rules internally |
| **axe-core is the engine behind GitHub's accessibility CI checks** | GitHub Actions marketplace |
| **WCAG conformance testing** — axe rules map 1:1 to WCAG success criteria | Each rule links to specific WCAG 2.1/2.2 criteria |
| **Zero false positives policy** — Deque guarantees no false positives in axe-core | Deque documentation |
| **Open source** — axe-core is MIT licensed, npm package, 10k+ GitHub stars | github.com/dequelabs/axe-core |

**[JrDev]:** Wait — Lighthouse uses axe-core internally? So when someone runs Lighthouse accessibility audit, they're already running axe?

**[Architect]:** Exactly. Lighthouse's accessibility category is literally a subset of axe-core rules. If we run axe-core directly, we get MORE coverage than Lighthouse, plus individual violation details with HTML snippets instead of just a score.

**[Sanity]:** So why didn't we just use Lighthouse then?

**[Architect]:** Because Lighthouse gives you a **score** (0-100) and a short list of failing audits. axe-core gives you **every violation, on every element, with the exact HTML snippet and a link to the fix**. For remediation work — actually fixing things — you need the axe-core detail level.

Think of it this way:

| Tool | Output | Good for |
|------|--------|----------|
| Lighthouse | "Accessibility: 67/100" + 8 audit summaries | Executive dashboard, quick gut-check |
| axe-core | 47 violations, each with element, selector, snippet, fix URL | Engineering fix list, evidence artifacts |

We need the fix list, not just the score.

**[Sanity]:** OK. axe-core is legit. What about our "htmlcheck" — the custom C# parser. That's not a standard tool. Is it real, or are we inventing work?

**[Quality]:** Fair challenge. Let me answer by asking: what does htmlcheck catch that axe-core doesn't?

**[Architect]:** Almost nothing, honestly. axe-core checks everything htmlcheck checks, plus more. The value of htmlcheck is different:

| Reason | Why it matters |
|--------|---------------|
| **Runs offline** | Can analyze saved `page.html` without re-visiting the site |
| **Zero dependencies** | Pure C# string parsing — no JS engine, no browser, no npm |
| **Validation** | Confirms axe-core findings with a second implementation |
| **Speed** | Milliseconds vs seconds — useful for CI gates |
| **Transparency** | We wrote the rules, we understand exactly what they check |
| **Future: batch re-analysis** | Re-scan 502 saved HTML files without hitting live sites |

**[Sanity]:** So htmlcheck is a "trust but verify" layer, not a primary scanner?

**[Architect]:** Exactly. If axe-core says 13 images missing alt text, and htmlcheck also says 13 images missing alt text, confidence is high. If the numbers differ, that's a signal to investigate.

**[Sanity]:** And pa11y?

**[Quality]:** Pa11y uses a completely different rules engine — HTML_CodeSniffer by Squiz. It's the only major alternative to axe-core that's open source and CLI-friendly. Having two independent rules engines checking the same pages is how professional audit firms work.

Here's how the major accessibility audit firms actually operate:

### How Professional ADA Auditors Work

| Step | What they do | Our equivalent |
|------|-------------|----------------|
| 1. Automated scan with axe | Run axe DevTools or axe CLI on every page | ✅ `RunAxeCoreAsync` — same engine |
| 2. Automated scan with second tool | Run WAVE or pa11y for corroboration | ✅ `RunPa11yAsync` + `RunHtmlCheckAsync` |
| 3. Manual keyboard test | Tab through every page | ❌ Not automated (can't be — requires human judgment) |
| 4. Screen reader test | Navigate with NVDA/VoiceOver | ❌ Not automated (can't be) |
| 5. Color contrast spot-check | Use contrast checker on key elements | ✅ axe-core checks computed contrast automatically |
| 6. Report generation | VPAT or custom report with violations + evidence | ✅ Our report.md with tables + snippets |

**[Sanity]:** So our automated approach covers steps 1, 2, 5, and 6. Steps 3 and 4 require humans. That's... actually what the research doc said: "automation catches 30-60% of issues."

**[Quality]:** Right. And that's not a weakness — it's the known limitation of ALL automated testing. No tool on Earth can tell you if alt text is *meaningful* or if a keyboard flow is *pleasant*. What automation does is:

1. **Catch the obvious stuff at scale** — 502 pages × 90 rules = 45,000+ checks, instantly
2. **Track progress over time** — "We had 1,189 missing alt texts in January. Now we have 340."
3. **Produce evidence** — Auditors want artifacts. JSON files with timestamps are evidence.
4. **Free up humans** — Don't waste screen reader time on pages with broken headings. Fix the automated findings first.

**[Sanity]:** Let me ask the opposite question. Is there a tool we're NOT using that we should be?

**[Architect]:** The only notable omission is **WAVE** (WebAIM). It's the other major accessibility tool alongside axe. But WAVE's API is paid ($100/month for 10,000 page credits), and its CLI isn't open source. For automated batch scanning of 500+ pages, WAVE isn't practical without a budget line item.

The free/open-source landscape for automated accessibility is essentially:
- **axe-core** (Deque) — dominant
- **HTML_CodeSniffer** (Squiz, via pa11y) — the alternative
- **Lighthouse** (Google) — uses axe-core internally
- Everything else is paid or niche

We're using the top two free engines. That's the right call.

**[JrDev]:** What about the IBM Equal Access toolkit? I saw that mentioned somewhere.

**[Architect]:** IBM's tool is solid but has much smaller community adoption. If we added a fourth tool someday, that'd be the one. But three is already more than most organizations run. Two is typical. One is common. Three is thorough.

---

## Verdict

**[Sanity]:** OK. I'm satisfied. Here's my summary:

### ✅ What We're Doing Right

1. **axe-core is THE industry standard** — same engine used by Google, Microsoft, GitHub, and every major accessibility consultancy
2. **Multiple engines** — running both axe-core and HTML_CodeSniffer (via pa11y) is how professional auditors work
3. **Evidence artifacts** — JSON files with timestamps are exactly what compliance auditors want
4. **Scale** — 120 sites × 502 pages is impossible to test manually; automation is the only path
5. **Unified schema** — normalizing across tools isn't overengineering; it's how you compare findings

### ⚠️ What We Must Acknowledge

1. **Automation catches ~30-60% of issues** — we need to be honest about this in reports
2. **No substitute for keyboard + screen reader testing** — add manual test recommendations to reports
3. **axe-core has a "zero false positives" policy** — it UNDER-reports rather than over-reports. If axe says it's a problem, it definitely is. But absence of violations ≠ accessible.

### 📋 Recommendation: Add Phase 5

| Phase | What |
|-------|------|
| Phase 1-4 | *(as designed in doc 200)* |
| **Phase 5** | Add "Manual Testing Checklist" section to each page report — keyboard tab order, screen reader coherence, zoom to 200% — as prompts for human testers to follow up |

---

## Decisions

1. **Confirmed:** axe-core is the correct primary tool — industry standard, not just our preference
2. **Confirmed:** pa11y provides genuine additional value via a different rules engine
3. **Confirmed:** htmlcheck adds offline re-analysis capability and cross-validation
4. **Added:** Phase 5 — manual testing checklist in reports (future)
5. **Acknowledged:** Reports must state that automated scanning covers ~30-60% of WCAG criteria; manual testing is still required

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Proceed with doc 200 implementation plan | [Implementer] | P1 |
| Add automation coverage disclaimer to report templates | [Quality] | P2 |
| Design manual testing checklist for Phase 5 | [Quality] | P3 |

---

*Created: 2026-02-17*  
*Maintained by: [Quality]*
