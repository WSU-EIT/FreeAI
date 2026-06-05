# 0003 — Meeting: CTO → Team Lead Pre-Brief (FreeCRM Styling Research)

> **Document ID:** 0003
> **Category:** Meeting
> **Purpose:** The CTO hands the team lead the context and the ask before the team meeting — a 1:1 pre-brief.
> **Attendees:** [CTO], [Architect] *(acting Team Lead)*
> **Date:** 2026-06-04
> **Predicted Outcome:** Team lead has enough context to introduce the topic to the team and run a focus group.
> **Actual Outcome:** ✅ Lead aligned; agenda for the team kickoff (doc 0004) set.
> **Resolution:** Proceed to 0004 (team intro) → 0005 (focus group) → 0006 (CTO brief).

---

## Context

This is the informal hallway conversation before the real meeting. The CTO has been driving a research thread solo and now wants to hand it to the team. Nothing here is a decision yet — it's the download.

---

## Pre-Brief

**[CTO]:** Alright, before I pull the whole team in, let me give you the story so you can frame it for them. Here's roughly what I've been doing the last little while.

First, I had everything cloned down — every public repo from the `wsu-eit` GitHub org into our local folder. Four repos: `FreeAI` (the big one, ~2 GB, has FreeTools and all our docs), `FreeCRM`, `FreeCICD`, `FreeQEMU`. Then I pointed at the `FreeTools/Docs/Guides` folder — you know, the pile of docs we've built up about the FreeCRM framework, the styling, the patterns, all of it.

**[Architect]:** Right, the 26-or-so guide docs. The `004_styleguide.md`, the comment-voice one, the architecture overviews, the component guides.

**[CTO]:** Exactly those. Here's the thing I want the team to really sit with. Go look at how those docs were written. We *reverse-engineered* the original author's style. We read his code across 30-plus projects and wrote down what we *thought* his preferences were. The docs literally say so — "consolidated from analysis," "derived from analysis of 500-plus comments." It was educated guessing.

**[Architect]:** Inference, not a spec. We never had the author tell us "this is my style."

**[CTO]:** Until now. That's the whole point. The original author — Brad Wickett, `wicketbr` on GitHub, the guy whose repo our `WSU-EIT/FreeCRM` is forked from — went through the repo about two weeks ago and **explicitly cleaned up the styling himself.** Unified it. There's a flagship commit, `d094f99`, and the message literally says he did it "for consistency with my programming style." 187 files. He merged it into the line we pull from.

**[Architect]:** So the guessing game is over. We now have ground truth straight from the author's own hands.

**[CTO]:** That's it. So I had the research done. Two new docs already exist in the Guides folder:

- **`0000_research_styling.md`** — everything *our* docs currently recommend, consolidated, but flagged honestly as inferred. It ends with a list of eight open questions where our guidance was silent or only-by-example.
- **`0001_freecrm_styling_latest_research.md`** — the forensic deep dive of what Brad *explicitly* did, commit by commit, with before/after diffs. It resolves all eight of 0000's open questions and supersedes 0000 wherever they conflict.

**[Architect]:** Let me make sure I have the headline findings straight before I take this to the team. What actually changed?

**[CTO]:** The big ones:

1. **Braces** — methods and types get the opening brace on a new line; **properties get it on the same line**; control-flow stays same-line. That property rule is one of the things our docs never pinned down.
2. **`String.Empty` everywhere** instead of `""` — and here's the spicy part: that *deliberately contradicts his own `.editorconfig`*, which would prefer the lowercase `string.Empty`. He overrode his own tooling by hand, 450-plus times.
3. **Underscore rules** — DI service fields and `CurrentUser`/`TenantId` get no underscore; everything else private does.
4. The **`.App.` partial-file convention** is the load-bearing thing that lets us take his framework updates without losing our customizations.

**[Architect]:** And what do you actually want out of the team? A rubber stamp on the research, or a decision?

**[CTO]:** A decision-grade recommendation. The research is done and I trust it. What I don't have is the "so what." I want the team to focus-group it and answer: now that we know his *real* style, what do we *do*? Do we rewrite `004_styleguide.md` to match? Do we leave 0000 and 0001 as standalone research? What do we do about that `String.Empty`-vs-editorconfig contradiction — follow him or follow the config? Do we try to enforce any of this automatically, or is that a trap? And how do we stay in sync, since he's clearly going to keep tidying his repo?

**[Architect]:** Understood. So the team's job is the operationalization, not re-litigating the findings.

**[CTO]:** Correct. Take the findings as solid — Quality can sanity-check them, that's healthy — but spend the energy on what we change and who does it. I want it to come back to me as a clean brief I can act on.

**[Architect]:** One clarifying question. When there's tension between "what the author does" and "what the tooling says," which wins by default?

**[CTO]:** Our own docs already answer that — the authoritative ordering puts **FreeCRM-main first**, and FreeCRM-main *is* Brad's upstream. So the author's hand wins over the editorconfig when they fight. But I want the team to say that out loud and decide whether we also realign the editorconfig so the tooling stops fighting us. That's exactly the kind of call I want their read on.

**[Architect]:** Good. That's enough for me to introduce it cleanly. I'll frame it for the team as: *the inference era is over, here's the author's real style, our job is to decide how we adopt and maintain it.* I'll assign angles — Backend takes the C# conventions, Frontend takes the Razor rules, Quality validates the findings and owns the doc updates, Sanity keeps us from over-engineering enforcement, JrDev keeps us honest on the assumptions.

**[CTO]:** Perfect. Run it. Bring me the brief.

---

## What the Lead Is Walking Away With

| Item | Detail |
|------|--------|
| **The shift** | We *inferred* Brad Wickett's style from code; he has now made it **explicit** (commit `d094f99`, ~2 weeks ago). |
| **Artifacts in hand** | `0000_research_styling.md` (inferred baseline), `0001_freecrm_styling_latest_research.md` (author's explicit cleanup, authoritative). |
| **The ask** | Not "is the research right" — it's "**what do we do with it**." |
| **Open calls for the team** | (1) Update `004` or keep 0000/0001 standalone? (2) Follow `String.Empty` or the editorconfig? (3) Automated enforcement — yes/no/how? (4) Stay-in-sync process with upstream. |
| **Default tiebreaker** | Author's hand > editorconfig (FreeCRM-main is the #1 authoritative source). |
| **Deliverable back to CTO** | A clean decision brief (doc 0006). |

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Introduce the topic to the team | [Architect] | P1 — see doc 0004 |
| Run the focus group | Full team | P1 — see doc 0005 |
| Write the CTO brief | [Architect] / [Quality] | P1 — see doc 0006 |

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
