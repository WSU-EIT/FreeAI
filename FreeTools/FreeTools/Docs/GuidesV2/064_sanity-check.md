# 064 — The Sanity Check

> **Document ID:** 064  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** One skeptical pass asking are we overcomplicating this and what did we miss before committing.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)
> **Skill candidate:** this process should also ship as the `/sanity-check` slash-command.

---

## In This Doc

| # | Section | What it will cover |
| --- | --- | --- |
| 1 | [What a Sanity Check Is](#what-it-is) | Plain-language definition, the named [Sanity] voice, and key terms |
| 2 | [When to Run One](#when-to-run) | The two natural moments — mid-work and pre-commit — plus when to skip it |
| 3 | [Who Runs It](#roles) | The [Sanity] mandate, the owner who answers, and how it differs from review |
| 4 | [The Skeptical Questions](#questions) | The standard prompts that surface over-engineering and gaps |
| 5 | [Running the Pass](#running) | The step-by-step procedure, with the two real worked examples |
| 6 | [The Output](#output) | The verdict — "boundaries holding" or a short list of gaps to close |
| 7 | [Avoiding Over- and Under-Checking](#pitfalls) | Theatre, paralysis, and other failure modes to dodge |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="what-it-is"></a>
## 1. What a Sanity Check Is

A **sanity check** is one short, deliberately skeptical pass over a plan or a piece of work that asks two blunt questions before you commit it: **"Are we overcomplicating this?"** and **"What did we miss?"** That is the entire mechanism. It is not a rewrite, not a code review, not a vote. It is a pause where someone whose only job is doubt looks at what the team is about to do and tries to talk them out of the expensive, the unnecessary, and the half-finished.

Why this matters to you, even if you are not an engineer: most costly mistakes in software are not bugs you can see — they are *decisions* that quietly grew too big. A team agrees to "just add a small gate," and three weeks later that gate is a system nobody asked for. Or the opposite: everyone is so focused on the clever part that an obvious case — the deleted record, the second language, the empty field — never gets handled. The sanity check is the cheap, five-minute habit that catches both failure modes *before* they are written into code, when changing course still costs nothing.

A few terms used throughout this doc, defined once here:

- **[Sanity]** — one of the named voices on our roleplay team (see [061 — The Roleplay Team](061_roleplay-team.md)). Each voice is a *perspective you adopt on purpose*, not a real person. [Sanity]'s entire focus is **reality checks and complexity**, and its signature question, taken verbatim from the team roster, is *"Are we overcomplicating this?"* When this doc says "[Sanity] asks," it means: deliberately put on the skeptic's hat and ask from that angle.
- **Over-engineering / overcomplicating** — building more than the problem requires: an automated gate where a one-line note would do, a full rewrite where six edits would do. The most common and most expensive default.
- **Gap** — something the plan silently leaves out: an edge case, an owner nobody assigned, a foot-gun left in the repo. Gaps are the under-doing failure; over-engineering is the over-doing failure. A good sanity check hunts both.
- **Pre-commit** — "commit" here means *commit to a course of action* (lock the decision, start the work, open the pull request), not necessarily a `git commit`. The sanity check happens just before that point of no easy return.

The shape to remember: **one skeptic, two questions, before commit.** Everything else in this doc is detail on how to run it well.

---

<a id="when-to-run"></a>
## 2. When to Run One

The sanity check has **two natural homes**, and in our real meeting transcripts you can see both fire in a single session.

**Home 1 — the mid-check, while you are still deciding.** Partway through a discussion, when the team has roughed out an approach but not locked it, [Sanity] interrupts to ask whether the plan is ballooning. This is exactly step 3 of the standard discussion flow in [061]: *"[Sanity] mid-check: Are we overcomplicating?"* In our styling focus group, this landed as a literal pause:

> **[Sanity]:** Pause. Are we overcomplicating this? ... Notice what we *didn't* decide: we did not approve a mass reformat of our own code, we did not add a CI gate, we did not rewrite `004`, we did not touch Brad's editorconfig. Every decision was "document the truth, follow the author, make a cheap habit." That's the right altitude.

The mid-check is most valuable on **large or unclear changes** — new features, architecture changes, anything where the team is inventing rather than following a template — because those are where scope quietly inflates.

**Home 2 — the final check, just before you commit.** Once the decisions feel done, [Sanity] runs a last sweep asking *"Did we miss anything?"* — step 5 of the discussion flow. This is the gap hunt. In the same focus group it surfaced three concrete loose ends (an unresolved `table-dark` styling question, a "which examples are authoritative" caveat, and an unassigned owner) before anyone was allowed to call the meeting closed.

**As its own standalone pass.** You do not need a full meeting to use it. Before opening any pull request, before locking any design, you can run the two questions solo — that is the `/sanity-check` slash-command this doc backs. You can apply it to anything from a tooling choice to a feature plan: run the two questions, sort the findings, and reach a verdict before any code is written.

**When to skip it.** Match the effort to the change, the same way [061] does:

| Change size | Examples | Sanity check? |
|-------------|----------|---------------|
| Tiny | Fix a typo, update a comment | No — just do it |
| Small | Add a field, simple bug fix | No — or a 10-second gut-check at most |
| Medium | New endpoint, new UI component | A quick solo pass: the two questions |
| Large / unclear | New feature, architecture or process change | Yes — mid-check *and* final check |

The cost of the check should never exceed the cost of the mistake it prevents. On a typo, the mistake is free to fix; on an architecture decision, it is not.

---

<a id="roles"></a>
## 3. Who Runs It

Three parties matter, and keeping them distinct is what makes the check work.

**The skeptic — [Sanity].** This is the voice that runs the pass, and its mandate is narrow and uncomfortable on purpose: **doubt the plan.** [Sanity] is not responsible for designing the solution, writing the code, or being right about the alternative — only for forcing the team to justify complexity and to account for gaps. The mandate includes the authority to **pull the cord**: to stop a decision from being committed until a real concern is answered. You can hear that authority in the transcripts: *"The moment it becomes 'rewrite 004 from scratch,' I'm pulling the cord."* and *"I'm satisfied... Boundaries holding. Continue."* When [Sanity] says continue, the gate opens; until then, it does not.

Crucially, [Sanity] argues against its own bias too. In the styling check it admitted, *"I went in expecting to pull the cord"* — and then, finding the plan was actually lean, let it pass. A good skeptic is not reflexively negative; it is reflexively *honest*.

**The owner — whoever proposed the work.** The owner's job is to **answer the questions**, not to defend their ego. When [Sanity] asks "are we overcomplicating this?", the owner either justifies the complexity with a concrete reason or trims it. When [Sanity] asks "did we miss the deleted-record case?", the owner either shows where it is handled or adds it to the list. In a solo `/sanity-check`, you play both roles: you ask the questions of your own plan and you have to answer them straight.

**The reviewer / [CTO] — the human who ratifies.** Per the team roster, **[CTO] is you, the human, who makes final decisions.** The sanity check does not overrule the human; it *prepares* the human. When the skeptic surfaces a genuine fork that the team cannot settle, the correct move is to escalate it rather than guess — the styling focus group did exactly this, flagging the "should we ever realign Brad's `.editorconfig`?" question **for the CTO to break the tie.** The standard escalation block from [061] looks like this:

```markdown
---
⏸️ **CTO Input Needed**

**Question:** {specific question}

**Options:**
1. Option A — {tradeoff}
2. Option B — {tradeoff}

@CTO — Which way?
---
```

**How this differs from code review.** A code review reads code that is already written and asks "is this correct?" A sanity check reads a *plan or decision* — usually before code exists — and asks "should we even be doing it this way?" Review catches defects; the sanity check catches the more expensive thing, the wrong-sized or incomplete decision, while it is still cheap to change.

---

<a id="questions"></a>
## 4. The Skeptical Questions

The two headline questions each unpack into a short checklist. You do not have to ask all of these every time — they are a menu the skeptic draws from.

**Headline 1 — "Are we overcomplicating this?"** (the over-engineering hunt)

- **What did we *not* decide?** The strongest tell that a plan is healthy is a list of expensive things you chose *not* to do. The styling check passed precisely because that list was long: no mass reformat, no CI gate, no rewrite, no editing the upstream file.
- **Is there a one-line version?** Could a note in the contributing docs replace the automated gate? In the styling check, the highest-leverage "enforcement" turned out to be a passive instruction — *"don't run blanket `dotnet format` on this repo"* — not a new system.
- **Does the standard tool fail on the reference?** A specific, reusable smell: *"If the standard enforcement tool fails on the reference implementation, the tool is wrong, not the code."* When your proposed automation would reject code that already ships and works, that automation is over-built.
- **Are we designing ourselves into it?** Ask whether you are building this because it is the right approach, or because you designed yourselves into it. Forget the plan; ask what a fresh person would actually do.
- **What's the blast radius?** Borrowed from [Architect]'s lens — how many files, people, and future merges does this touch? Big blast radius demands a big justification.

**Headline 2 — "What did we miss?"** (the gap hunt)

- **The edge cases.** The deleted record, the empty field, the second tenant, the second language. Our features carry soft-delete, multi-tenancy, and localization in *every* layer (see [005 — A Guided Tour of One Working Feature](005_first-feature.md)); a plan that ignores them has a gap.
- **The foot-guns left in the repo.** Not just "did we do the work" but "did we leave a trap for the next person." The styling check caught that an unedited `.editorconfig` would actively *suggest the wrong style* to whoever opened the project next — a booby trap that needed a loud warning.
- **The unassigned owner and timing.** A decision with no owner is a wish. [Sanity]'s final sweep explicitly asked *"Who actually does the edits and when — this sprint or backlog?"* and refused to close until both were answered.
- **The honest limitation.** What does this approach *not* cover, stated plainly? For example, the ADA scanning guide (see [075 — ADA Scanning](075_ada-scanning.md)) insists every report admit that automation only catches ~30–60% of issues rather than imply full coverage. Naming the limit is part of not missing it.
- **The tool we are *not* using.** A deliberate inversion: *"Is there a tool we're NOT using that we should be?"* Asking what is absent often finds the real gap faster than re-examining what is present.

---

<a id="running"></a>
## 5. Running the Pass

Here is the procedure, whether you are interrupting a meeting or running `/sanity-check` solo.

1. **State what is about to be committed, in one or two sentences.** "We're about to add a CI gate that runs `dotnet format --verify-no-changes` on every push." If you cannot say it that plainly, that is itself a finding.

2. **Run the over-engineering question.** Ask "are we overcomplicating this?" and walk the first checklist from section 4. Force a real answer for each point of complexity: a concrete reason it must exist, or a trim. List out loud the expensive things you are choosing *not* to do — if that list is empty, be suspicious.

3. **Run the gap question.** Ask "what did we miss?" and walk the second checklist: edge cases, foot-guns, owners, limitations, absent tools. Write each gap down as a discrete item; do not let them blur together.

4. **Sort each finding into one of three buckets.**
   - **Resolve now** — small enough to settle on the spot (e.g. "resolve `table-dark` as *optional, match surrounding UI*").
   - **Assign and track** — real work with a named owner and a priority (e.g. "[Quality] writes the divergence warning, P1, this sprint").
   - **Escalate to [CTO]** — a genuine fork the team should not guess on; emit the `⏸️ CTO Input Needed` block from section 3.

5. **Give a verdict and either pull the cord or open the gate.** If every finding is resolved, assigned, or escalated, the skeptic says some version of *"I'm satisfied — boundaries holding. Continue."* and the work proceeds. If a real concern is unanswered, the skeptic holds: the decision does not commit until it is addressed.

The worked example makes the rhythm concrete:

- **Embedded in a focus group ([0005]).** [Sanity] fired *twice* — a **mid-check** that confirmed the plan had not ballooned ("notice what we *didn't* decide"), then a **final check** that surfaced three gaps (the `table-dark` question, the authoritative-examples caveat, the missing owner), each resolved or assigned before the meeting closed. One escalation went up to the CTO.

---

<a id="output"></a>
## 6. The Output

A sanity check is deliberately lightweight, so its output is small — but it is never *nothing*. A finished pass produces three things:

1. **A verdict.** Pass or hold. The pass reads like *"Then I'm satisfied. Nothing structural missed."* or *"Boundaries holding. Continue."* A hold reads like *"I'm pulling the cord"* — the decision is blocked until a named concern is answered. The verdict is the load-bearing output: it is the gate that either opens or stays shut.

2. **A short findings list, each item sorted.** Every gap or complexity concern lands in exactly one bucket — resolved now, assigned to an owner with a priority, or escalated to the CTO. In the styling check this materialized as concrete next-step rows, for example:

   | Action | Owner | Priority |
   |--------|-------|----------|
   | Resolve `table-dark` as "optional, match surrounding UI" | [Frontend] | P2 |
   | Write the divergence warning (foot-gun note) | [Quality] | P1 |

3. **Optionally, an escalation to the human.** If the pass found a fork the team should not break itself, the output includes a `⏸️ CTO Input Needed` block with the question and the trade-offs laid out — exactly what the next doc, [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md), is built to receive.

A note on weight: when the sanity check is its own meeting, it gets written up like any decision (verdict, decisions, next steps). When it is a mid-check inside a larger session, the output is just a few lines in that session's transcript plus any rows it adds to the next-steps table. **Match the artifact to the stakes** — do not generate a formal report for a five-minute solo pass; do capture the verdict and any assigned gaps for a decision the team will live with.

---

<a id="pitfalls"></a>
## 7. Avoiding Over- and Under-Checking

The sanity check has its own failure modes, on both sides. Watch for these.

**Over-checking — when the skeptic itself becomes the over-engineering.**

- **Analysis paralysis.** Re-litigating a decision endlessly so nothing ever commits. The check is *one* pass with a verdict, not a standing veto. If you have run the two questions and sorted every finding, ship the verdict.
- **Skeptic theatre.** Asking "are we overcomplicating this?" as a ritual phrase and always answering "no." A check that never changes an outcome is decoration. A real check sometimes pulls the cord — and sometimes, honestly, *clears* a plan the skeptic expected to block.
- **Re-opening settled facts.** The skeptic challenges the *plan*, not the validated research under it. In the styling check, [Sanity]'s own rule was *"Then we don't reopen the findings. We decide what we do."* Doubting things that were already proven just burns time.
- **Sanity-checking a typo.** Applying the full pass to tiny changes. Re-read the table in section 2: below "medium," the check costs more than the mistake it could catch.

**Under-checking — when the pass is too shallow to catch anything.**

- **Skipping the "what did we miss?" half.** Many teams remember to ask "are we overcomplicating?" and forget the gap hunt entirely — then ship with the deleted-record case unhandled. Both questions, every real pass.
- **Confirmation, not skepticism.** Running the check to *approve* a plan you have already fallen in love with. The honest move is to discard your own plan first, then ask what you would actually do from scratch.
- **No owner, no verdict.** Surfacing gaps and then not assigning or resolving them. A finding with no bucket is a finding that gets forgotten. Every item must land somewhere before the verdict.
- **Guessing instead of escalating.** When the team hits a genuine fork — an upstream-relationship call, a high-impact trade-off — and quietly picks one rather than flagging it for the [CTO]. If it is the human's call, make it the human's call.

The single most useful instinct, drawn straight from the transcripts: a healthy plan can usually point to a **list of expensive things it chose not to do.** If your sanity check cannot produce that list, you have probably found your answer to "are we overcomplicating this?"

---

<a id="related-docs"></a>
## 8. Related Docs

- [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) — the team that runs it
- [063 — Running a Focus Group](063_focus-group.md) — where it fits in a focus group

---
*GuidesV2 064 · drafted from source 2026-06-05 · the skeptical "are we overcomplicating / what did we miss" pass, grounded in the [Sanity] role and the real focus-group transcript.*
