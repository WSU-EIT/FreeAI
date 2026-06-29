# 0004 — Meeting: Team Lead Introduces the Topic (FreeCRM Styling Alignment)

> **Document ID:** 0004
> **Category:** Meeting
> **Purpose:** The team lead frames the task for the team and sets the agenda for the focus group.
> **Attendees:** [Architect] *(Team Lead)*, [Backend], [Frontend], [Quality], [Sanity], [JrDev]
> **Date:** 2026-06-04
> **Predicted Outcome:** Team understands the problem, owns their angle, and is ready to focus-group it.
> **Actual Outcome:** ✅ Angles assigned; focus group convened (doc 0005).
> **Resolution:** Proceed to doc 0005 (focus group).

---

## Context

Carried over from the CTO pre-brief (doc 0003). The lead now opens the topic to the team. This is the framing, not the debate — the debate is doc 0005.

---

## The Lead Frames It

**[Architect]:** Thanks for jumping on. Short version of why we're here: for years our style guidance for FreeCRM was **inferred**. We read the original author's code across a pile of projects and wrote down what we *thought* his conventions were. That's what `004_styleguide.md` and the comment guide and the architecture overviews are — best-effort reverse engineering.

That era is over. The original author — Brad Wickett, `wicketbr`, whose repo our `WSU-EIT/FreeCRM` forks — went through and **explicitly unified his own style** about two weeks ago. Flagship commit `d094f99`, 187 files, and the commit message says he did it "for consistency with my programming style." It's already merged into the branch we pull from.

The research is already written up. Two docs in our Guides folder:

- **`0000_research_styling.md`** — our current (inferred) recommendations, consolidated, honestly flagged as guesswork, ending with eight open questions.
- **`0001_freecrm_styling_latest_research.md`** — the forensic, commit-backed deep dive of what Brad *actually* did. It answers all eight open questions and is authoritative where it conflicts with 0000.

**[JrDev]:** So are we re-checking the research, or taking it as given?

**[Architect]:** Take the findings as solid — Quality will give them a sanity pass, that's fair — but the CTO does not want us re-litigating the research. He wants the **"so what."** Now that we know his real style, what do *we* change, and who does it?

**[Sanity]:** Before anyone gets excited and proposes a big migration: what's the actual decision surface here? I want to scope this so we don't invent work.

**[Architect]:** Four questions, straight from the CTO:

1. **Do we update `004_styleguide.md` to match the author's explicit rules, or keep 0000/0001 as standalone research that supersedes it by reference?**
2. **The `String.Empty` problem** — Brad uses `String.Empty` everywhere, by hand, 450-plus times, which *contradicts his own `.editorconfig`* (it'd prefer lowercase `string.Empty`). Do we follow his hand or follow the config? And do we realign the config so tooling stops fighting the convention?
3. **Enforcement** — do we automate any of this (CI format gate, analyzers), or is that a trap given the hand-conventions the formatter can't express?
4. **Sync** — Brad clearly keeps tidying his repo. How do we stay aligned on each `wicketbr:main` merge without re-doing this every time?

**[Sanity]:** Good. That's bounded. Four questions, not "rewrite everything."

---

## The Findings, In One Breath (so everyone's working from the same facts)

Pulled from `0001`. The author's **explicit** rules:

| Dimension | The rule |
|-----------|----------|
| **Braces** | Methods/types → brace on a **new line**. Properties → brace on the **same line**. Control-flow (`if`/`foreach`/`try`) → same line, space after keyword, `} else`/`} catch` together. |
| **Wrapped params** | `(` drops to its own line, one param per line, then `){` collapsed onto one line. Distinctive and hand-made. |
| **`String.Empty`** | Always, for the empty string — even in LINQ predicates. **Overrides his own editorconfig.** |
| **Underscores** | DI service fields + `CurrentUser`/`TenantId` → **no** `_`. All other private/protected → `_`. |
| **Param casing** | Domain method params **PascalCase**; DI ctor params camelCase; locals camelCase. |
| **Ordering** | Private fields **alphabetical**; usings not system-first; DTO members grouped ids → data → audit. |
| **Partials + `.App.`** | Core types are partial-by-domain; everything custom lives in paired `*.App.*` files (survives framework upgrades). |
| **Hygiene** | Strip unused usings, dead/commented code, redundant `Helpers.` self-qualifiers and BCL namespace qualifiers; one-blank-line discipline; `Try*` parse idioms over try/catch. |

The mechanical baseline lives in `FreeCRM/.editorconfig` (`.cs` → `csharp_new_line_before_open_brace = types,methods`; `.razor` → `none`). The interesting part is the handful of places where Brad's **hand-style overrides his own config** — `String.Empty` being the headline.

---

## Angles — Who Owns What in the Focus Group

| Role | Your lens going in |
|------|--------------------|
| **[Backend]** | The C# conventions — braces, `String.Empty`, underscores, params, partials, DataAccess/DataObjects patterns. Is anything impractical? |
| **[Frontend]** | The Razor rules — `brace = none`, attribute wrapping, the `table-dark` removal, component-authoring patterns. What changes for page authors? |
| **[Quality]** | Validate 0000/0001 against the repo. Own the question of *which doc becomes authoritative* and what gets updated. Tests/enforcement risk. |
| **[Sanity]** | Keep us from over-engineering enforcement and from a pointless mass-reformat of our own code. |
| **[JrDev]** | Ask the dumb-smart questions. Surface every place we're assuming Brad's "habit" is a "rule." |
| **[Architect]** | Tie-breaks, the sync process, and making sure the recommendation is something the CTO can act on. |

**Default tiebreaker (CTO-set):** when the author's hand and the tooling disagree, **the author wins** — FreeCRM-main is our #1 authoritative source, and that *is* Brad's upstream.

---

## Agenda for the Focus Group (doc 0005)

1. Quality's validation pass on the findings (fast).
2. Q1 — update `004` vs keep 0000/0001 standalone.
3. Q2 — `String.Empty` vs editorconfig (and whether to realign the config).
4. Q3 — enforcement: automate or not.
5. Q4 — staying in sync with upstream.
6. [Sanity] mid-check and final check.
7. Decisions + next steps → feed the CTO brief (0006).

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
