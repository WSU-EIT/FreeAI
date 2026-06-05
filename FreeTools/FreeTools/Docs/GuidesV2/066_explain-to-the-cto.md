# 066 — Explaining to the Intern-CTO

> **Document ID:** 066  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** The mechanism for re-explaining anything technical for an authority-holding, intern-level reader with the decision spelled out.
> **Audience:** Decision-makers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)
> **Skill candidate:** this process should also ship as the `/explain-to-cto` slash-command.

---

## In This Doc

| # | Section | What it covers |
| --- | --- | --- |
| 1 | [Why This Matters & The Intern-CTO Model](#why-it-matters) | What the intern-CTO reader is, the key terms, and why re-explaining for them is the whole point |
| 2 | [When to Use This Process](#when-to-use) | The triggers — a decision to hand up, a term someone bounced off, a brief going to the top |
| 3 | [Inputs You Need First](#inputs) | The raw material to gather before you re-explain: the technical truth, the audience, and the actual decision |
| 4 | [The Re-Explanation Steps](#steps) | The ordered procedure: name the decision, strip the jargon, lead with why, spell out the call |
| 5 | [Roles & Who Does What](#roles) | Explainer, decider, and reviewer — who translates, who decides, who checks the translation is honest |
| 6 | [The Output Format](#output) | The shape of the finished brief, modeled on the real CTO brief in the `Guides/` set |
| 7 | [Pitfalls & Quality Checks](#pitfalls) | The ways re-explanation goes wrong, and the checklist that catches each one |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Matters & The Intern-CTO Model

Start with the term, because the whole document hangs on it. The **intern-CTO** is the imagined reader every GuidesV2 doc is written for: *a person who holds the authority to decide, but has only intern-level technical depth.* Picture a brand-new chief technology officer on their first day — smart, trusted, allowed to make the call, but not yet fluent in the codebase's vocabulary. They can say yes or no; they cannot yet tell `String.Empty` from a hole in the ground. (The index calls this "the house standard"; this doc is that standard turned into a repeatable procedure.)

**Re-explaining** simply means taking something already written for engineers — a research finding, an architecture choice, a tooling tradeoff — and rewriting it so the intern-CTO can act on it without first earning a CS degree. It is translation, not summarizing. A summary makes a technical thing *shorter*; a re-explanation makes it *decidable by a non-engineer*.

**Why it matters:** authority you cannot exercise is useless. If the person empowered to decide cannot understand what they are deciding, one of two bad things happens — they rubber-stamp whatever the engineers say (so the authority is fake), or they freeze and the decision stalls. The intern-CTO model fixes both by making the explainer do the hard work of translation up front, so the decision-maker can apply real judgment to a clearly-stated choice. As the GuidesV2 index puts it bluntly: *"This is not 'dumbing down' — it's making sure the person with the authority can actually exercise it."*

Three rules define the voice, and they come straight from the house reader model:

- **Define each term the first time it appears.** Not "the tenant boundary enforces row scoping" but "a *tenant* — one customer's isolated slice of data — keeps each customer's rows fenced off from every other customer's."
- **Lead with why it matters and what decision it touches, then the detail.** The reader should know *why they should care* before they hit a single technical sentence.
- **Surface the choice plainly,** with the trade-offs named, when there is actually a decision to make. The detail exists to inform a call, not to show off.

A worked example of all three is already in our repo. The real CTO brief — `Docs/GuidesV2/0006_brief.cto_final.md` — opens with a plain-language **TL;DR**, defines the author by name (`Brad Wickett / wicketbr`) the first time it cites him, leads every recommendation with *why this shape*, and ends with exactly one boxed decision labeled **"The One Decision That Needs You."** That document is the template this whole process is reverse-engineered from. When in doubt about voice, go read it.

---

<a id="when-to-use"></a>
## 2. When to Use This Process

Reach for `/explain-to-cto` whenever a technical thing has to cross the line from *people who built it* to *a person who must decide about it*. The concrete triggers:

- **A decision needs to go up.** The team has done the analysis and reached a recommendation, but the final call belongs to someone with less technical depth. This is the headline case — the focus group ([063](063_focus-group.md)) finishes, and its conclusions now have to be handed to the CTO as something they can ratify. The `Guides/` set models this exactly: the focus group (`0005`) fed the CTO brief (`0006`).
- **Someone bounced off a term.** A reader — reviewer, stakeholder, new teammate — read something and clearly did not follow it, or asked "wait, what *is* a wrapper?" That is a signal the original was written at the wrong altitude. Re-explain the term in place.
- **You are writing for a mixed audience.** Anything that will be read by both engineers and non-engineers (a release note, a fork-sync summary, an architecture overview) should pass through this lens so the non-engineers are not left behind.
- **A choice has trade-offs the decider must weigh.** When there is a genuine fork in the road — adopt-or-don't, single-tenant-or-multi, enforce-in-CI-or-by-hand — and the person weighing it is not the person who understands the internals, this process is how you make the trade-offs legible.

**When *not* to use it.** If the audience is fully technical and no decision is leaving the engineering room, skip it — re-explaining to peers who already share the vocabulary just adds length. And do not use it to *avoid* precision: the goal is plain language that is still exactly correct, never plain language that is vague. (The matching idea in the original `Guides/001_roleplay.md` is "match the change size to the approach" — tiny things you just do; you do not convene the whole apparatus for a typo.)

---

<a id="inputs"></a>
## 3. Inputs You Need First

Do not start writing until you have these three things in hand. Re-explanation fails most often because the explainer skipped straight to prose without the raw material.

**1. The technical truth, grounded in something real.** You cannot translate what you do not understand precisely. Before re-explaining, pin the facts to actual source — a file, a commit, a measured number — not to memory or vibe. The CTO brief models this discipline: it does not say "lots of files changed," it says *"`d094f99` = 187 files, +1,836 / −2,075"*, and it draws a hard line between **"Verified facts (not estimates)"** and everything softer. Bring verified facts. If a number is an estimate, label it as one.

**2. A clear picture of the reader.** Who, specifically, is deciding? What do they already know, and where exactly is the depth gap? The same content gets explained differently to a CTO who once wrote code versus one who never has. Naming the audience up front (the brief's header literally lists **Audience: [CTO]**) tells you which terms need defining and which you can assume.

**3. The actual decision — stated as a question.** This is the input people most often miss. Before you explain anything, write down the one question the reader must answer, phrased so it can be answered. The brief boils a fortnight of research down to a single boxed question: *"Should we ever realign Brad's `.editorconfig` so the tooling stops suggesting `string.Empty`...?"* If you cannot phrase the decision as a yes/no or a pick-one, you are not ready to explain it yet — you are still figuring out what the decision *is*.

Supporting inputs that strengthen the explanation: the **options** on the table (with the trade-off of each), the team's **recommendation and its rationale** (the *why this shape*), and any **risks** that ride along with the decision. These map directly onto the sections of the output format in Section 6.

---

<a id="steps"></a>
## 4. The Re-Explanation Steps

An ordered procedure. Each step has one job; do them in order, because each one depends on the last.

**Step 1 — Name the decision first.** Open by stating, in one or two sentences a non-engineer can read, what is being decided and why anyone should care. This is the *why-it-matters lead*. Everything technical comes after. In practice this becomes your TL;DR — the brief's first move is a TL;DR that names the shift ("for years our style was inferred... now we have ground truth") before a single rule is discussed.

**Step 2 — Inventory the jargon.** Read your raw material and circle every term, acronym, file name, and type that the intern-CTO would not know cold. `tenant`, `editorconfig`, `DI`, `SignalR`, `Guid`, `partial class` — each is a tripwire. You are making a list of everything you must either define or remove.

**Step 3 — Define or delete each term, on first use.** For every circled term, either (a) define it in plain language *the first time it appears* — `String.Empty` becomes "a built-in constant for a blank string, written with a capital S" — or (b) cut it if the reader does not actually need it to decide. Prefer cutting. A term the decision does not hinge on is just noise. The ones that remain get a parenthetical or inline definition, exactly once.

**Step 4 — Lead each point with its stakes.** For every claim you keep, restructure it so the *consequence* comes before the *mechanism*. Not "his `String.Empty` habit contradicts his `.editorconfig`" cold, but "here's a latent foot-gun the decider should know about: his tooling disagrees with his own code." The brief's phrasing — *"his tooling is a latent foot-gun"* — is a model: it tells the reader why to care, then explains.

**Step 5 — Lay out the options and the recommendation.** Present the genuine choices with the trade-off of each spelled out, then state what the team recommends and — critically — *why this shape*. The brief gives a one-line reason for its whole posture: *"the goal isn't to be philosophically 'right'... it's to minimize merge friction with the upstream we don't control."* A recommendation without a "why this shape" is an opinion; with one, it is a defensible position the CTO can ratify or overrule on the merits.

**Step 6 — Spell out the one decision that needs them.** Separate cleanly what is *theirs to decide* from what the team already settled under its own authority. Box the single call you need, give the team's lean, and give the honest case for the other side. The brief does this in a dedicated **"The One Decision That Needs You"** block and ends with *"Everything else in the recommendation is within the team's authority and is ready to execute on your nod."* That sentence is the goal: a decider who has exactly one thing to think about.

**Step 7 — Read it back as the intern-CTO.** Final pass: read the whole thing as if you were the non-engineer decider. Did any undefined term survive? Did any paragraph lead with mechanism instead of stakes? Is the decision unambiguous? Fix what fails. (This is the same skeptical final pass the [Sanity] role applies in roleplay; see Section 5.)

---

<a id="roles"></a>
## 5. Roles & Who Does What

Re-explanation has three jobs. On a small task one person wears all three hats; on a real decision they should be different people, because the explainer is too close to the material to be their own honesty check. The role names match the named roleplay team in [061](061_roleplay-team.md).

| Role | Who, typically | Their job |
|------|----------------|-----------|
| **Explainer** | The author / **[Architect]** or **[Quality]** | Owns the translation. Gathers the inputs (Section 3), runs the steps (Section 4), and produces the brief. They are responsible for the explanation being both *plain* and *correct*. |
| **Decider** | **[CTO]** — the authority-holding reader | Receives the brief and makes the one call. In the roleplay model the CTO is explicitly *"You, the human — final decisions."* Their only obligation is to actually be able to decide from what they were handed; if they can't, the explainer failed. |
| **Reviewer** | **[Sanity]** (and sometimes **[JrDev]**) | Reads the draft *as the intern-CTO would* before it ships. [Sanity] asks "are we overcomplicating this? what did we miss?"; [JrDev] is the built-in "wait, why are we doing X?" voice — the person allowed to admit they don't know a term, which makes them the perfect detector for an undefined one. |

**The hand-off in one line:** the Explainer translates, the Reviewer verifies the translation is honest and jargon-free, the Decider decides. The brief itself records this trail — it is *"Maintained by: [Quality]"* and routed to *"Audience: [CTO]"*, with the focus-group team ([Architect], [Backend], [Frontend], [Quality], [Sanity], [JrDev]) as the source of the analysis behind it.

A note on honesty, because it is the reviewer's real value: the temptation in re-explaining is to make the recommendation sound more certain than it is. The reviewer's job is to keep the *uncertainty* in the translation — to make sure "the team's lean is NO, but here's the genuine case for YES" survives into the final draft, rather than being flattened into "do this." A decider handed only one side of a trade-off is not deciding; they are being steered.

---

<a id="output"></a>
## 6. The Output Format

The output is a **one-page brief** the decider can read top-to-bottom and act on. The canonical example is `Docs/GuidesV2/0006_brief.cto_final.md`; its section order is the format. Reproduce this shape:

1. **Header block.** Purpose, Audience (name the decider), and a **Predicted / Actual Outcome** pair so the brief records both what you expected and what actually happened after the call. The real brief's header reads *"Predicted Outcome: CTO ratifies the team's recommendation and rules on the one open tie."*
2. **TL;DR.** The why-it-matters lead from Step 1 — the entire situation and recommendation in a few plain sentences, before any detail. Bold the one or two phrases the reader must not miss.
3. **What We Did** *(optional but recommended).* A short table of the work behind the recommendation, separating **verified facts** from estimates. This is what earns the reader's trust that the recommendation is grounded.
4. **The findings, condensed.** The technical truth, translated — each term defined on first use, each point led by its stakes. Tables work well here for "dimension → the rule."
5. **The recommendation, with "why this shape."** What the team advises and the single reason the whole posture makes sense. List the sub-decisions the team already made under its own authority.
6. **The one decision that needs them.** A visually-set-off block (the real brief uses a `>` blockquote) containing: the question, the team's lean, the honest case for the other side, and why it is *their* call specifically. End with the "everything else is ready on your nod" line.
7. **Risks & Mitigations,** and **Effort.** Two short tables: what could go wrong (and the guard against it), and roughly how much work the recommendation is. These let the decider weigh the downside and the cost without doing their own analysis.
8. **Trail.** A one-line breadcrumb of the documents that led here (e.g. *"`0003` → `0004` → `0005` → `0006`"*), so anyone can retrace the reasoning.

You do not need every section for every brief — a small decision might be just TL;DR + the one decision. The non-negotiable parts are the **plain-language lead**, the **defined terms**, and the **single clearly-stated call.** Those three are the reader model; the rest is scaffolding.

---

<a id="pitfalls"></a>
## 7. Pitfalls & Quality Checks

The common ways re-explanation fails, each paired with the check that catches it. Run the checks as your final pass (Step 7).

| Pitfall | What it looks like | The check |
|---------|--------------------|-----------|
| **Undefined jargon survives** | A term, acronym, file name, or type appears with no first-use definition. | Re-read as the intern-CTO: can you point to where every technical term was defined? If not, define or delete it. |
| **Mechanism before stakes** | A paragraph opens with *how it works* and the reader never learns *why they should care.* | Every section should lead with the consequence. If the first sentence is technical, you buried the lead. |
| **The decision is fuzzy** | The "decision" cannot be answered yes/no or pick-one; it is really a discussion. | Can the reader give a one-word answer? If not, you have not isolated the decision yet — go back to Inputs. |
| **One-sided trade-off (steering, not informing)** | Only the recommended option is presented; the honest case for the alternative is missing. | Is the *case for the other side* in the brief? The decider must see both leans. This is the reviewer's primary job (Section 5). |
| **Estimates dressed as facts** | A guessed number or claim is stated with the confidence of a measured one. | Is every hard claim grounded in real source — a file, a commit, a count? Label estimates as estimates, the way the brief separates *"Verified facts (not estimates)."* |
| **Over-explaining / dumping** | Every internal detail is included "to be thorough," burying the decision under noise. | Does each piece of detail change the decision? If cutting it would not change the call, cut it. Plain ≠ exhaustive. |
| **Authority blur** | The decider cannot tell what is theirs to decide versus what the team already settled. | Is there exactly *one* boxed "this is yours" call, with the rest marked "ready on your nod"? |
| **False simplicity** | Plain language that is now subtly *wrong* — accuracy traded for readability. | Would an engineer who knows the material agree the simplified version is still true? Plain language must stay correct. |

The meta-check, borrowed from the [Sanity] role: after the draft is done, ask *"are we overcomplicating this, and what did we miss?"* If the brief is longer than the decision warrants, or if a teammate reading it still has to ask "so what do you actually need from me?", it has not done its job yet.

---

<a id="related-docs"></a>
## 8. Related Docs

- [081 — The Fit Test: Is This Framework Right for Us?](081_is-it-for-us.md) — the fit test applies this lens
- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — the brief that carries the decision
- [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) — the CTO role on the team

---
*GuidesV2 066 · Explaining to the Intern-CTO · drafted 2026-06-04 from source (the intern-CTO reader model in `Docs/GuidesV2/000_index.md`, the CTO brief `Docs/GuidesV2/0006_brief.cto_final.md`, the CTO pre-brief `0003`, the focus group `0005`, and the roleplay roles in `Docs/Guides/001_roleplay.md`).*
