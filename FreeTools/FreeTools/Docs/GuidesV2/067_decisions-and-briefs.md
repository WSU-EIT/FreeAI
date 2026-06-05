# 067 — Decision Records and the CTO Brief

> **Document ID:** 067  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Capture why a choice was made and the one-page brief that hands a decision to the CTO.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-it-matters) | What a decision record and a CTO brief are, and why writing down the *why* pays off |
| 2 | [When to Use Each](#when-to-use) | Choosing a quick record versus escalating a one-page brief |
| 3 | [Anatomy of a Decision Record](#decision-record) | The fields every decision capture needs — context, options, choice, rationale, consequences |
| 4 | [The One-Page CTO Brief](#cto-brief) | The structure of the handoff document, section by section, from a real example |
| 5 | [Roles and Responsibilities](#roles) | Who writes, who reviews, and who actually decides |
| 6 | [The Workflow Step by Step](#workflow) | From making a choice to handing it off, in order |
| 7 | [Output, Storage, and Follow-Up](#output) | Where the artifacts live, how they're numbered, and what closes the loop |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Matters

**What a decision record is, in plain language.** A *decision record* is a short document that captures one choice and — crucially — *why* it was made. It answers four questions for anyone who reads it later: what problem were we facing, what options did we weigh, which one did we pick, and what was the reasoning. The most common formal name for one is an **ADR**, short for *Architecture Decision Record* — but the idea applies to any decision worth remembering, not just architecture. Think of it as the receipt for a choice: not the choice itself, but the proof of why you made it.

**What a CTO brief is.** A *CTO brief* is a single page that hands a finished recommendation to the person with the authority to ratify it. "CTO" here means *Chief Technology Officer* — in our model, the human who holds final sign-off (see [061](061_roleplay-team.md), where the CTO is "you, the human," and [066](066_explain-to-the-cto.md) for how we pitch the explanation at an intern-level reader who happens to hold the authority). The brief is *not* the place where the decision gets debated — that already happened in a focus group ([063](063_focus-group.md)). The brief's job is to summarize the work, state the team's recommendation, and isolate the **one** thing that genuinely needs a human ruling, so the CTO can read it in two minutes and say yes.

**Why writing the *why* down matters.** A choice with no recorded rationale is a landmine for your future self. Six months on, someone sees an "odd" decision — say, that we deliberately write `String.Empty` with a capital `S` even though the project's own auto-formatter would prefer lowercase `string.Empty` — and, lacking the reasoning, "fixes" it. That single innocent fix can detonate a 454-line diff and a merge conflict with the upstream project. (That exact scenario is what our flagship brief, [`0006_brief.cto_final.md`](0006_brief.cto_final.md), exists to prevent.) The decision record is the thing that stops the re-litigation: it says *we already thought about this, here's why, don't undo it.* Rationale is the expensive part to reconstruct and the cheap part to write down at the moment you decide.

**The two-document split.** These are two artifacts with two jobs. The **decision record** preserves the reasoning permanently, for the team and for the future. The **CTO brief** moves a single decision *up* to someone who can ratify it. Often you write a record and never need a brief. Occasionally you write a brief because exactly one call isn't yours to make. Section 2 draws that line.

<a id="when-to-use"></a>
## 2. When to Use Each

The two are not interchangeable. A decision record *captures* a choice; a CTO brief *escalates* one. Most decisions need only the first.

**Write a decision record when:**

- The choice is **non-obvious** or has trade-offs a reader would later question ("why didn't they just run `dotnet format`?").
- It sets a **convention** others will follow (a naming rule, a file-layout pattern, an enforcement policy).
- You **rejected** a reasonable-looking alternative and want to stop someone re-proposing it later.
- It's the natural **write-up of a focus group** — the conclusions a multi-voice debate reached, made permanent (the focus-group doc [`0005`](0005_meeting.team_focus_group.md) *is* a decision record in this sense).

You do **not** need a record for trivial, reversible, or self-evident choices. Recording "we named the variable `output`" is noise. The test: *would a competent person six months from now be confused or tempted to undo this without the reasoning?* If yes, record it.

**Escalate a CTO brief when — and only when — the team's authority runs out.** The team can decide almost everything inside its own remit. A brief is for the rare item where one specific call belongs to the human with final sign-off. In our flagship example that single call was crisp:

> **Should we ever realign Brad's `.editorconfig`** so the tooling stops suggesting `string.Empty` against the `String.Empty` convention?

That's an *upstream-relationship* question — it touches a file the original author owns and that rides into our repo on every merge — so it's the CTO's to ratify, not the team's. Everything else in that same brief was within the team's authority and simply marked "ready to execute on your nod."

**The litmus test for a brief:** *Is there exactly one decision here that isn't ours to make?* If the answer is "no, we can just do it," write a record and proceed. If "yes, one call needs an authority we don't hold," write the brief — and make that one call impossible to miss. A brief with five open questions is a failed brief; the discipline is to reduce it to one.

<a id="decision-record"></a>
## 3. Anatomy of a Decision Record

A decision record has a small, fixed skeleton. The point of a fixed shape is that readers know exactly where to look, and writers can't accidentally skip the part that matters most (the rationale). The template library ([068](068_template-library.md)) ships two sizes: a full ADR and a one-paragraph inline version.

**The full record — five required fields:**

1. **Context** — *Why did we need to decide?* The problem, the constraint, what changed. Without this, the decision is unmoored: a reader can't tell whether it still applies. In the flagship case, the context was concrete and verified: the original author (Brad Wickett, `wicketbr` upstream) unified his own code style in commit `d094f99` — *"187 files, +1,836 / −2,075"* — giving us, for the first time, ground truth instead of inferred guesses.
2. **Options Considered** — the realistic alternatives, each with honest pros and cons. Listing the *rejected* options is not busywork; it's what stops someone re-proposing them later. "Run a CI gate," "mass-reformat our code," and "edit the author's config" were all live options that got weighed and dropped.
3. **Decision** — the choice, stated plainly: *"We chose **X** because …"* One sentence, unambiguous.
4. **Rationale** — *why this option won.* This is the load-bearing field. The flagship rationale is a single clean idea worth copying: *"the goal isn't to be philosophically 'right' about style — it's to **minimize merge friction with the upstream we don't control.**"* A good rationale gives a future reader a *principle* they can apply to the next, similar decision.
5. **Consequences** — what the choice commits us to, split into positive, negative, and neutral. Naming the negative trade-off out loud is what keeps the record honest and stops "but nobody told me" later.

**The inline mini-record**, for a decision made inside a PR or meeting note where a full doc would be overkill — copied faithfully from the template library:

```markdown
### ADR: {Title}

**Context:** {Why we needed to decide}
**Decision:** {What we chose}
**Rationale:** {Why this option}
**Consequences:** {What this means}
**Alternatives:** {What we didn't choose}
```

Five lines. Same fields, compressed. Use it when the decision is real but small enough that spinning up a numbered document would be ceremony.

**The one field people skip — and shouldn't:** *Alternatives / rejected options.* A record that only states the winner reads like it was inevitable, and inevitable-looking decisions get casually overturned. Showing the work — *we considered the CI gate and rejected it because it would fail on the author's own code* — is what makes the decision durable.

<a id="cto-brief"></a>
## 4. The One-Page CTO Brief

The brief is a *handoff*, not a debate transcript. Its single design constraint is **one page**: a busy decision-maker should absorb the whole thing in a couple of minutes and act. Everything in its structure serves that constraint. The canonical example is [`0006_brief.cto_final.md`](0006_brief.cto_final.md); the sections below mirror its real layout.

**Header — frame the ask up front.** The brief uses the *meeting/decision* header format, whose distinguishing feature is three outcome lines that turn a static doc into a closeable loop:

```markdown
> **Predicted Outcome:** {What we expect}
> **Actual Outcome:** {What happened — update when done}
> **Resolution:** {Action taken — PR link, decision, next doc}
```

In the real brief these read: *Predicted Outcome — "CTO ratifies the team's recommendation and rules on the one open tie"*; *Actual Outcome — "Awaiting CTO sign-off"*; *Resolution — "Pending."* The CTO knows the ask before reading a word of body.

**TL;DR — the whole story in a paragraph.** "TL;DR" means *too long, didn't read* — a deliberate, up-front summary for someone who may only read this one block. If the reader stops here, they should still know what happened and what's being recommended. The flagship TL;DR does exactly that: it states that our style guidance used to be *inferred* from reading the author's code, that the author has now *explicitly* unified it, and that the team's recommendation is to "adopt the author's explicit style … and do almost nothing destructive."

**What We Did** — a short table proving the work is real, not hand-waved. The discipline here is **verified facts, not estimates.** The brief is explicit about it: *"Verified facts (not estimates): `d094f99` = 187 files, +1,836 / −2,075 … `String.Empty` now appears 454× across 48 `.cs` files."* Hard numbers earn the CTO's trust faster than adjectives.

**The Findings / What Changed** — the substance, condensed to a scan-able table. The brief does not re-derive anything; it points at the authoritative research doc and summarizes the conclusions in one row each (braces, empty-string convention, naming, ordering, and so on).

**The Recommendation — decisions the team already made.** This is the team flexing its own authority. The flagship lists *five* decisions the team reached and owns, ending each with the *why*. It then names "the headline nuance" honestly — the author's `String.Empty` habit *contradicts his own `.editorconfig`* — because hiding the awkward part would undermine the whole brief.

**The One Decision That Needs You — the entire point of the brief.** Visually set off (a blockquote in the real doc), it states the single question, the team's lean *with reasoning*, the strongest case *against* that lean, and a clear recommendation:

```markdown
> **Should we ever realign Brad's `.editorconfig`** …?
>
> - **Team's lean: NO** — don't touch his file; it rides in on merges …
> - **The case for YES:** a one-line change … would stop the IDE fighting the convention …
>
> This is an upstream-relationship call, which is why it's yours. **Recommended: ratify NO.**
```

Note the fairness: the brief argues the *other* side too. A brief that hides the counter-argument isn't a decision aid, it's a sales pitch — and a sharp CTO will distrust it.

**Risks & Mitigations, Effort, Trail** — the closing reassurances. A small risk table (each risk paired with the decision that mitigates it), an honest effort estimate (*"roughly a half-day of doc work. No code migration"*), and a **Trail** line that cites the lineage of docs that produced the brief so the reasoning is auditable.

**The golden rule of the brief:** the team has already done the deciding; the brief surfaces the *one* residual call and makes saying "yes" easy. If your draft brief has the CTO weighing five things, you haven't finished the team's job yet.

<a id="roles"></a>
## 5. Roles and Responsibilities

Three responsibilities, drawn straight from how the flagship example actually ran. In our roleplay model ([061](061_roleplay-team.md)) these map to named voices, but the duties are what matter — the same person can wear more than one hat on a small change.

**The Author — writes it.** Usually the **Team Lead** (the `[Architect]` voice acted as lead in the flagship) or whoever ran the focus group. The author's job is to compress the debate into the record or the brief without distorting it: state the decision, preserve the rationale, and — for a brief — ruthlessly reduce the open questions to one. In the example, *"Write the CTO decision brief"* was assigned to `[Architect]` as a P1 task.

**The Reviewer — checks it before it ships.** The **Quality** role owns this in our model; nearly every template in the library ends with *"Maintained by: [Quality]."* The reviewer verifies two things: that the *facts are real* (Quality literally re-ran the numbers against the working tree before the brief went out — *"so we're not building on sand"*) and that the *rationale is honest* (rejected options shown, counter-arguments not buried). A second voice catches the load-bearing assumption the author stopped seeing.

**The Decider — rules on it.** For a decision record, the decider is the **team** itself, by consensus inside the focus group, with the **Sanity** voice empowered to "pull the cord" on overcomplication. For a CTO brief, the decider is the **CTO** — the human with final sign-off. The decider's job on a brief is narrow by design: ratify the recommendation and rule on the single open call. The brief should make that a two-minute job.

**Why the separation matters.** Collapsing author and reviewer into one person is how unverified numbers and one-sided briefs slip through — the author is the worst-placed person to catch their own blind spot. And collapsing decider into the team on a call that *isn't theirs* is exactly the failure the brief exists to prevent: the flagship flagged the editorconfig question *up* precisely because it was "an upstream-relationship call … which is why it's yours."

<a id="workflow"></a>
## 6. The Workflow Step by Step

The path from "we have a choice to make" to "the CTO has ruled" runs in a fixed order. The flagship example walked exactly this sequence — its **Trail** line records it: `0003 → 0004 → 0005 → 0006`.

1. **Surface the decision.** Someone names a choice worth deciding deliberately rather than by reflex. (In the example: *what do we do now that the author has explicitly unified his style?*)
2. **Pre-brief the lead (optional).** For a larger topic, the CTO hands context to the team lead one-on-one first, so the lead can frame it for the team — this was doc `0003`, *"the informal hallway conversation before the real meeting."* Skip it for smaller decisions.
3. **Introduce it to the team.** The lead brings the framed topic to the team (doc `0004`), setting the agenda and the specific questions to answer.
4. **Run the focus group.** The team debates to a conclusion with sanity checks throughout — the deep process in [063](063_focus-group.md). This is where the *deciding* happens. The flagship focus group ([`0005`](0005_meeting.team_focus_group.md)) reached **five decisions and flagged one for the CTO.**
5. **Write the decision record.** Capture the conclusions — context, options, choice, rationale, consequences. The focus-group write-up itself serves as this record.
6. **Distill the CTO brief — if escalation is needed.** Compress the record to one page, state the recommendation, and isolate the single decision that isn't the team's to make. This was doc `0006`.
7. **Hand off and get the ruling.** The CTO reads the brief, ratifies the recommendation, and rules on the one open call.
8. **Close the loop.** Update the brief's **Actual Outcome** and **Resolution** lines with what the CTO decided (and any PR link), then execute the team's already-authorized items. Until those lines are filled, the decision is still officially open.

**The shortcut for everyday decisions:** most choices skip steps 2, 3, 6, and 7 entirely — you surface the choice, decide it (sometimes with a quick [single-role consult](065_ask-one-role.md) instead of a full focus group), write a record, and proceed. The full chain above is for the consequential decisions where a permanent rationale and a human ratification genuinely earn their cost.

<a id="output"></a>
## 7. Output, Storage, and Follow-Up

**What the artifacts are.** A finished decision produces one or two Markdown files: a **decision record** (a numbered `.md` doc, or an inline ADR block inside a PR), and — when escalation is needed — a **CTO brief** (a one-page numbered `.md` doc). Both are plain text, version-controlled, and live alongside the code they govern.

**Where they live.** In this repository the worked example sits in `FreeAI/FreeTools/FreeTools/Docs/Guides/` — the focus-group record at [`0005_meeting.team_focus_group.md`](0005_meeting.team_focus_group.md) and the brief at [`0006_brief.cto_final.md`](0006_brief.cto_final.md). They are committed to git, so the rationale travels with the codebase forever and is auditable.

**How they're numbered and named.** Docs carry a numeric ID prefix and a short descriptive slug; the **Category** in the header (`Decision`, `Meeting`, `Brief`) tells you the type at a glance. The numbering links related docs into a chain — `0003 → 0004 → 0005 → 0006` reads as one connected story, and each doc's **Trail** line spells the lineage out so a reader can walk it.

**The follow-up that actually closes a decision.** The outcome lines in the header are the closing mechanism — a decision is *not* done until they're filled:

- **Actual Outcome** — what really happened (updated from the predicted outcome once the CTO rules).
- **Resolution** — the concrete action: a PR link, the ratified decision, or the next doc in the chain.

The flagship brief is, at the time of writing, still honestly open — its Resolution reads *"Pending — see 'The One Decision That Needs You,'"* and its status line is *"Awaiting CTO sign-off."* That visible "open" state is a feature: anyone can see at a glance that the loop hasn't closed yet.

**The closed-loop payoff.** Once ratified, the brief's outcome lines get updated, the team executes the items that were "ready to execute on your nod," and the decision record stands as the permanent answer to "why is it like this?" That permanent answer is the entire return on the effort — it's what stops the next contributor from innocently re-litigating a choice the team already paid to make. The reusable skeletons for both the record and the brief live in the [template library](068_template-library.md); grab them, fill the placeholders, and don't skip the rationale.

---

<a id="related-docs"></a>
## 8. Related Docs

- [063 — Running a Focus Group](063_focus-group.md) — the focus group that feeds it
- [068 — The Documentation Template Library](068_template-library.md) — templates for the write-up
- [066 — Explaining to the Intern-CTO](066_explain-to-the-cto.md) — the CTO brief format

---
*GuidesV2 067 · drafted from source (`Docs/GuidesV2/0006_brief.cto_final.md`, `0005_meeting.team_focus_group.md`, `0003_meeting.cto_premeeting_briefing.md`, `003_templates.md`) on 2026-06-05.*
