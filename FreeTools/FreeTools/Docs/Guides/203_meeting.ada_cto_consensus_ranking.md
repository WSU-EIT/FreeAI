# 203 — Meeting: CTO Direction — Consensus-Ranked Output

> **Document ID:** 203  
> **Category:** Meeting  
> **Purpose:** CTO clarifies the core goal — cross-tool consensus ranking — and team validates the plan covers it  
> **Attendees:** [CTO], [Architect], [Backend], [Quality], [Sanity]  
> **Date:** 2026-02-17  
> **Predicted Outcome:** Confirm plan covers consensus ranking, or identify gaps  
> **Actual Outcome:** ⚠️ Gap found — plan had per-tool columns but no consensus scoring or CSV export. Fixed.  
> **Resolution:** Added consensus scoring + CSV export + N-tool scalability to Phase 5 in doc 202  

---

## CTO Direction

**[CTO]:** Let me make sure the point of this is clear. I don't care that we run three tools. I care about what comes OUT the other end.

If we run three tools and all three say "this image has no alt text" — that's a real problem. High confidence. Fix it.

If we run three tools and only one says "this link text is vague" — maybe that's real, maybe it's a false positive. Lower confidence. Review it.

If we had ten tools and six of them flag the same thing? That's a flashing red light.

The output I want is a **single ranked list**: worst problems first, scored by how many tools agree. Not three separate reports. One list. Exportable. Sortable. Something I can hand to a web team and say "fix these, top to bottom."

Does the plan do that?

---

## Team Response

**[Architect]:** Honest answer: partially. Let me show what we have and what's missing.

### What the plan already covers (docs 200-202)

| Feature | Status | Where |
|---------|--------|-------|
| Run 3 tools per page | ✅ Designed | Doc 202, Phases 2-4 |
| Normalize all results to unified `A11yIssue` schema | ✅ Designed | Doc 200, schema table |
| Per-tool columns in report tables | ✅ Designed | Doc 200, report mockups |
| Per-page `a11y-summary.json` merging all tools | ✅ Designed | Doc 202, Task 5.1 |
| Severity counts by tool | ✅ Designed | Doc 200, metadata.json |
| "Tool Agreement" aggregate stat at run level | ✅ Designed | Doc 200, run report |

### What's missing — the gap

| Feature | Status | Gap |
|---------|--------|-----|
| **Per-issue consensus score** (3/3, 2/3, 1/3) | ❌ Missing | Merge step combines but doesn't score by agreement |
| **Ranked output sorted by consensus first** | ❌ Missing | Tables sort by severity, not by tool agreement |
| **CSV export** of the ranked list | ❌ Missing | Only JSON and markdown outputs exist |
| **N-tool scalability** in the scoring | ❌ Missing | Tables are hardcoded to 3 columns |
| **Confidence labels** (high/medium/low) | ❌ Missing | No concept of confidence based on tool count |

**[CTO]:** So the infrastructure is right — unified schema, per-tool results — but the *output ranking* doesn't exist yet. The last mile is missing.

**[Architect]:** Exactly. The fix is straightforward. The `MergeA11yResults` step already has all the data in memory. We just need to:

1. Group issues by `ruleId` + `selector` (what makes two issues "the same problem")
2. Count how many distinct tools flagged each group
3. Score: `consensus = toolsFound / toolsRun` (e.g., 3/3 = 1.0, 2/3 = 0.67, 1/3 = 0.33)
4. Sort: consensus descending → severity descending → instance count descending
5. Write ranked output as both JSON and CSV

**[Backend]:** The matching logic needs to be smart but not over-complicated. Two issues from different tools are "the same problem" when:

```
Match criteria:
  MUST match:  ruleId (normalized)    e.g., "image-alt"
  SHOULD match: selector              e.g., "img.hero-banner"
  
  If ruleId matches but selectors differ → still the same RULE, different INSTANCES
  Group at two levels:
    1. Rule-level:  "image-alt found by 3/3 tools, 47 total instances"
    2. Element-level: "img.hero-banner found by 2/3 tools"
```

**[Quality]:** The rule-level grouping is what the CTO wants for the ranked list. The element-level detail goes in the expandable section when you drill in.

**[CTO]:** Exactly. Top-level: ranked list of rules. Drill in: specific elements.

---

## Rule ID Normalization

**[Skeptic]:** The three tools use different rule IDs for the same thing. How do you match them?

**[Backend]:** Good catch. Here are the key mappings:

| Concept | axe-core rule ID | htmlcheck rule ID | Pa11y code |
|---------|-----------------|-------------------|------------|
| Image missing alt | `image-alt` | `img-alt` | `WCAG2AA.1_1.1_1_1.H37` |
| Heading order skip | `heading-order` | `heading-order` | `WCAG2AA.1_3.1_3_1_A.G141` |
| Missing lang | `html-has-lang` | `html-lang` | `WCAG2AA.3_1.3_1_1.H57.2` |
| Missing form label | `label` | `label-missing` | `WCAG2AA.1_3.1_3_1.F68` |
| Empty link | `link-name` | `link-empty` | `WCAG2AA.4_1.4_1_2.H91.A.Empty` |
| Empty button | `button-name` | `button-empty` | `WCAG2AA.4_1.4_1_2.H91.Button.Name` |
| Color contrast | `color-contrast` | *(can't check)* | `WCAG2AA.1_4.1_4_3.G18` |
| Skip link | `skip-link` | `skip-link-missing` | *(doesn't check)* |
| Main landmark | `landmark-one-main` | `landmark-main` | *(doesn't check)* |

**[Backend]:** We need a normalization map — a dictionary that maps each tool's native ID to a canonical ID. I'll use axe-core IDs as the canonical form since they're the industry standard:

```csharp
// Canonical rule ID mapping
static Dictionary<string, string> RuleNormalization = new() {
    // htmlcheck → canonical (axe) IDs
    ["img-alt"] = "image-alt",
    ["html-lang"] = "html-has-lang",
    ["label-missing"] = "label",
    ["link-empty"] = "link-name",
    ["button-empty"] = "button-name",
    ["skip-link-missing"] = "skip-link",
    ["landmark-main"] = "landmark-one-main",
    // heading-order stays the same
    
    // Pa11y WCAG codes → canonical IDs
    ["WCAG2AA.1_1.1_1_1.H37"] = "image-alt",
    ["WCAG2AA.1_3.1_3_1_A.G141"] = "heading-order",
    ["WCAG2AA.3_1.3_1_1.H57.2"] = "html-has-lang",
    ["WCAG2AA.1_3.1_3_1.F68"] = "label",
    ["WCAG2AA.1_4.1_4_3.G18"] = "color-contrast",
    // ... etc
};
```

Issues with unmapped IDs keep their original ID. They just won't match across tools — which is correct, because they're tool-specific checks.

---

## The Ranked Output

**[Architect]:** Here's what the consensus-ranked output looks like. This is what the CTO is asking for.

### Page-level: `a11y-ranked.json` (new file, per page)

```json
{
    "toolsRun": ["axe", "htmlcheck", "pa11y"],
    "toolsCompleted": ["axe", "htmlcheck"],
    "toolsSkipped": ["pa11y"],
    "rankedRules": [
        {
            "rank": 1,
            "canonicalRuleId": "image-alt",
            "severity": "critical",
            "consensus": "2/2",
            "confidenceScore": 1.0,
            "confidence": "high",
            "toolsFound": ["axe", "htmlcheck"],
            "toolsMissed": [],
            "totalInstances": 13,
            "message": "Images must have alternate text",
            "wcagCriteria": "1.1.1",
            "helpUrl": "https://dequeuniversity.com/rules/axe/4.10/image-alt",
            "instances": [
                {
                    "selector": "img.hero-banner",
                    "snippet": "<img src=\"banner.jpg\" class=\"hero-banner\">",
                    "foundBy": ["axe", "htmlcheck"]
                }
            ]
        },
        {
            "rank": 2,
            "canonicalRuleId": "heading-order",
            "severity": "moderate",
            "consensus": "2/2",
            "confidenceScore": 1.0,
            "confidence": "high",
            "toolsFound": ["axe", "htmlcheck"],
            "toolsMissed": [],
            "totalInstances": 3,
            "message": "Heading levels should increase by one",
            "instances": [ ... ]
        },
        {
            "rank": 3,
            "canonicalRuleId": "color-contrast",
            "severity": "serious",
            "consensus": "1/2",
            "confidenceScore": 0.5,
            "confidence": "medium",
            "toolsFound": ["axe"],
            "toolsMissed": ["htmlcheck"],
            "message": "Elements must meet color contrast ratio",
            "note": "htmlcheck cannot check contrast (requires computed styles)",
            "instances": [ ... ]
        }
    ]
}
```

**[CTO]:** That `consensus` field — "2/2", "1/2" — that's the denominator based on tools that actually *can* check that rule, not tools that ran?

**[Backend]:** Exactly. If pa11y was skipped AND htmlcheck can't check contrast, then color-contrast found by axe alone is "1/1" — full consensus, not "1/3." We track two things:

| Concept | Meaning |
|---------|---------|
| `toolsRun` | Tools that executed (completed or skipped) |
| `toolsCapable` | Tools that CAN check this specific rule |
| `toolsFound` | Tools that DID find this violation |
| `consensus` | `toolsFound / toolsCapable` |

So an issue found by the only tool capable of checking it still gets high confidence.

**[Quality]:** That's critical. Otherwise contrast issues would always be "low confidence" since only axe can check them. That would be wrong — contrast is one of the most important checks.

### Run-level: `a11y-ranked.csv` (new file, root of run)

```csv
Rank,Rule,Severity,Confidence,Consensus,Sites,Pages,Instances,WCAG,Message
1,image-alt,critical,high,118/120,98,389,1189,1.1.1,"Images must have alternate text"
2,color-contrast,serious,high,87/87,87,312,856,1.4.3,"Elements must meet color contrast ratio"
3,link-name,serious,high,112/120,76,278,634,4.1.2,"Links must have discernible text"
4,heading-order,moderate,high,115/120,71,245,489,1.3.1,"Heading levels should increase by one"
5,html-has-lang,serious,high,120/120,12,12,12,3.1.1,"<html> must have a lang attribute"
6,label,serious,medium,89/120,45,123,234,1.3.1,"Form elements must have labels"
7,skip-link-missing,moderate,medium,78/120,34,89,89,2.4.1,"Page should have a skip link"
8,landmark-one-main,moderate,medium,67/120,23,56,56,1.3.1,"Page should have one main landmark"
```

**[CTO]:** That's it. That CSV is the deliverable. Sort it, hand it to web teams, track it month over month.

### Report markdown: ranked table

```markdown
## ♿ Accessibility — Ranked by Confidence

| # | Rule | Sev | Confidence | Sites | Pages | Instances | WCAG |
|--:|------|:---:|:----------:|:-----:|:-----:|:---------:|:----:|
| 1 | image-alt | 🔴 | 🟢 HIGH (118/120) | 98 | 389 | 1,189 | 1.1.1 |
| 2 | color-contrast | 🟠 | 🟢 HIGH (87/87) | 87 | 312 | 856 | 1.4.3 |
| 3 | link-name | 🟠 | 🟢 HIGH (112/120) | 76 | 278 | 634 | 4.1.2 |
| 4 | heading-order | 🟡 | 🟢 HIGH (115/120) | 71 | 245 | 489 | 1.3.1 |
| 5 | label | 🟠 | 🟡 MED (89/120) | 45 | 123 | 234 | 1.3.1 |
| 6 | div-button | 🟡 | 🔵 LOW (12/120) | 8 | 12 | 15 | 4.1.2 |
```

---

## N-Tool Scalability

**[CTO]:** One more thing. I said "if we have 10 tools and 6 hit." Don't hardcode this for three tools.

**[Architect]:** The design already handles this. The unified schema doesn't know the tool count — it just records `tool` as a string on each issue. The merge/ranking step:

1. Discovers tools from the `a11y-*.json` files present in the page dir
2. Builds `toolsRun` list dynamically
3. Computes consensus as `found / capable` — works for any N

Adding a fourth tool (say, IBM Equal Access) means:
1. Write a `RunIbmAccessAsync` method
2. Output `a11y-ibmaccess.json`
3. Add to the normalization map
4. Everything else — merging, ranking, reports, CSV — works automatically

```
Tool discovery:
  files = Directory.GetFiles(pageDir, "a11y-*.json")
  // Excludes a11y-summary.json and a11y-ranked.json
  // Each file is one tool's results
  
  tools = files
    .Where(f => !f.Contains("summary") && !f.Contains("ranked"))
    .Select(f => Path.GetFileNameWithoutExtension(f).Replace("a11y-", ""))
    .ToList()
  // Result: ["axe", "htmlcheck", "pa11y"]
```

**[Sanity]:** So adding a tool is: one method + one JSON file + a few normalization entries. The ranking, reports, and CSV all adapt automatically?

**[Architect]:** Correct. Zero changes to the merge/report/CSV code.

---

## Gap Analysis: Are We Good to Go?

**[Quality]:** Let me walk through the acceptance criteria from doc 200 plus the CTO's new requirements.

### Original acceptance criteria (doc 200) — all covered ✅

| # | Criterion | Status |
|---|-----------|--------|
| 1 | axe-core runs on every page, produces `a11y-axe.json` | ✅ Doc 202, Phase 2 |
| 2 | htmlcheck runs on every page, produces `a11y-htmlcheck.json` | ✅ Doc 202, Phase 3 |
| 3 | pa11y optional, produces `a11y-pa11y.json` or skipped | ✅ Doc 202, Phase 4 |
| 4 | `a11y-summary.json` merges all tool results | ✅ Doc 202, Phase 5 |
| 5 | `metadata.json` includes accessibility counts | ✅ Doc 202, Task 5.2 |
| 6 | Page report has ♿ section | ✅ Doc 202, Task 6.1 |
| 7 | Site report has rollup | ✅ Doc 202, Task 6.2 |
| 8 | Run report has dashboard | ✅ Doc 202, Task 6.3 |
| 9 | Existing reports unchanged | ✅ By design |
| 10 | WCAG level configurable | ✅ Doc 202, Task 1.5 |

### New CTO requirements — adding now

| # | Criterion | Status | Action |
|---|-----------|--------|--------|
| 11 | Rule ID normalization across tools | ❌ Was missing | Add normalization map to Phase 5 |
| 12 | Per-issue consensus score (N/M tools) | ❌ Was missing | Add to `MergeA11yResults` |
| 13 | Confidence labels (high/medium/low) | ❌ Was missing | Derive from consensus score |
| 14 | Ranked output sorted by consensus first | ❌ Was missing | Add `a11y-ranked.json` per page |
| 15 | CSV export at run level | ❌ Was missing | Add `a11y-ranked.csv` to run dir |
| 16 | N-tool scalability (not hardcoded to 3) | ❌ Was missing | Dynamic tool discovery from files |
| 17 | "Capable" vs "ran" distinction for consensus | ❌ Was missing | Add capability map per rule |

### Updated task list (additions to doc 202)

| Task | Phase | Description | Est. Lines |
|------|-------|-------------|:----------:|
| **5.3** | 5 | Add rule ID normalization map | ~40 |
| **5.4** | 5 | Add consensus scoring to `MergeA11yResults` | ~30 |
| **5.5** | 5 | Write `a11y-ranked.json` per page | ~10 |
| **5.6** | 5 | Add tool capability map (which tools can check which rules) | ~25 |
| **6.5** | 6 | Write `a11y-ranked.csv` at run level | ~30 |
| **6.6** | 6 | Add consensus-ranked table to run `report.md` | ~25 |
| | | **Additional lines** | **~160** |

**Total revised estimate:** ~650 original + ~160 new = **~810 lines**

---

## Final Verdict

**[Sanity]:** Let me run the checklist one last time.

| Question | Answer |
|----------|--------|
| Are we using the right tools? | ✅ Yes — confirmed in doc 201 |
| Does the plan produce the ranked list the CTO wants? | ✅ Now it does — consensus scoring + CSV added |
| Does it scale to N tools? | ✅ Dynamic discovery from `a11y-*.json` files |
| Does it handle "tool can't check this rule" correctly? | ✅ Capable vs ran distinction |
| Is existing functionality preserved? | ✅ A11y is additive — all existing output unchanged |
| Can we start building? | ✅ Yes |

**[CTO]:** Good. Build it. Phase 1 first — get axe-core running and producing output. I'd rather see real data from one tool than a perfect plan for three.

**[Architect]:** Agreed. Phase 1 (axe-core + models + page report) ships first. The consensus ranking starts working the moment we add the second tool in Phase 2. By Phase 3, we have three-way consensus.

**[Quality]:** One more thing — the CSV should include a column for "first detected" date so we can track how long issues have been open. That's a future enhancement but the data structure should accommodate it.

**[CTO]:** Good thinking. Don't build it now, but don't design it out.

---

## Decisions

1. **Core output is a consensus-ranked list** — sorted by tools-agreed first, severity second, instance count third
2. **Consensus denominator is "capable tools"** — not "tools that ran." If only axe can check contrast, 1/1 = high confidence.
3. **Rule ID normalization map** — maps each tool's native IDs to canonical (axe) IDs for cross-tool matching
4. **CSV export at run level** — `a11y-ranked.csv` in the run root directory, one row per rule across all sites
5. **N-tool scalability** — dynamic tool discovery from `a11y-*.json` files, no hardcoded tool count
6. **Confidence labels** — high (≥ 0.8), medium (≥ 0.5), low (< 0.5)
7. **Phase 1 ships first** — real data from axe-core before perfecting the multi-tool pipeline

---

## Updated Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| **Implement Phase 1** (axe-core + models + basic report) | [Implementer] | P1 — do now |
| **Implement Phase 2** (htmlcheck + normalization map) | [Implementer] | P1 — immediately after |
| **Implement Phase 5** (consensus scoring + ranked JSON + CSV) | [Implementer] | P1 — with Phase 2 |
| Implement Phase 3 (pa11y optional) | [Implementer] | P2 |
| Implement Phase 4 (cross-tool comparison tables) | [Implementer] | P2 |
| Run full scan, review ranked CSV | [Quality] | P1 |
| Deliver CSV to web teams | [CTO] | P1 |

---

*Created: 2026-02-17*  
*Maintained by: [Quality]*
