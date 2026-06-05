# 062 — Discussion Mode, Planning Mode

> **Document ID:** 062  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain when to explore a problem with the team versus run a tight execution checklist.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Two Modes Matter](#why-modes) | What discussion and planning mode are, and why keeping them apart prevents bad work |
| 2 | [When to Use Discussion Mode](#when-discussion) | The signals that a problem still needs open exploration |
| 3 | [When to Use Planning Mode](#when-planning) | The signals that the thinking is done and work is ready to execute |
| 4 | [Running a Discussion Session](#run-discussion) | How to facilitate open exploration that converges |
| 5 | [Running a Planning Session](#run-planning) | How to turn decisions into an owned, sized checklist |
| 6 | [Roles and Responsibilities](#roles) | Who frames, contributes, challenges, and decides in each mode |
| 7 | [Switching Between Modes](#switching) | How to tell the modes apart and move between them cleanly |
| 8 | [Outputs and What Good Looks Like](#outputs) | The artifacts each mode produces and the quality bar for each |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-modes"></a>
## 1. Why Two Modes Matter

**Why it matters first:** most bad project work comes from doing the wrong *kind* of thinking at the wrong time. Someone starts writing a checklist before anyone agrees what the problem is, or the team keeps "exploring" long after the answer is obvious and the real cost is now the delay. Naming two distinct modes — and being deliberate about which one you are in — is the cheapest way we have to avoid both failures.

Two plain-language definitions:

- **Discussion mode** is *open exploration*. The goal is to understand the problem, surface options, and find disagreement you didn't know you had. Output is **understanding and decisions**, not tasks. You are allowed to change your mind, follow a tangent, and say "I don't actually know yet."
- **Planning mode** is *tight execution*. The thinking is finished; now you turn decisions into a list of concrete, owned, sized steps. Output is a **checklist** — who does what, in what order, how big it is. You are *not* allowed to re-open settled questions here; that is a different meeting.

The whole reason we separate them: the two modes reward opposite behaviors. Discussion rewards *divergence* (more options, more challenge, more doubt). Planning rewards *convergence* (fewer open ends, clear owners, no surprises). Mix them and you get the worst of both — a "plan" full of unresolved arguments, or a "discussion" where nobody will float a risky idea because it might become their assigned task.

Our own styling-research workflow is the clean example, and it is worth grounding this in real docs you can open:

- The exploration lived in `Docs/GuidesV2/0003_meeting.cto_premeeting_briefing.md` (the CTO hands the problem off), `0004_meeting.lead_team_introduction.md` (the lead frames it), and the debate rounds inside `0005_meeting.team_focus_group.md`. That is **discussion mode** — the "habit vs. rule" question is left genuinely open and argued, options are weighed, nothing is assigned yet.
- The execution lived in the **Decisions** list and the **Next Steps** table (with `Owner` and `Priority` columns) at the bottom of `0005`, and in the CTO brief `0006_brief.cto_final.md`. That is **planning mode** — five numbered decisions, each with an owner, sized as "roughly a half-day of doc work."

Same problem, two modes, done in order. That ordering is the point of this whole doc.

---

<a id="when-discussion"></a>
## 2. When to Use Discussion Mode

**Why it matters:** discussion mode is *more* expensive in the moment (it takes people's time and produces no tasks), so the temptation is to skip it. But skipping it on a problem that wasn't ready is how teams build the wrong thing efficiently. Use these signals to decide whether the problem still needs exploring.

Reach for discussion mode when **any** of these are true:

- **You can't state the question in one sentence.** If "what are we even deciding?" gets three different answers, you are not ready to plan. In `0004` the lead spends the whole doc just *bounding* the decision down to four crisp questions before anyone debates them.
- **There's a real fork in the road.** Multiple defensible options exist and the choice has consequences. The `String.Empty`-vs-`.editorconfig` question in `0005` is the model: follow the author's hand, or follow his tooling? Both are arguable; that's a discussion.
- **You suspect a hidden assumption.** Someone needs to ask the "dumb-smart" question. In `0005`, JrDev flags that some of the author's choices might be **habit, not law** — there's even a leftover `AllowLoginTypeOpenId{` with no space proving the formatter never ran. That reframing changed how the whole team treated the findings. You only get that in open discussion.
- **The cost of being wrong is high or hard to reverse.** Mass-reformatting the codebase, editing an upstream-owned file, or adding a CI gate are all hard to undo. Anything that "detonates a 454-line diff" (the real risk named in `0006`) deserves exploration first.
- **People disagree and you don't yet know why.** Disagreement is information. Discussion mode exists to mine it before it becomes a production incident.

The negative test is just as useful — do **not** open a discussion when the answer is already obvious, when the question is purely mechanical, or when you're really just looking for permission. Those are planning-mode (or one-person) jobs.

---

<a id="when-planning"></a>
## 3. When to Use Planning Mode

**Why it matters:** planning mode is where decisions become reality. Entering it too early bakes in unexamined choices; entering it too late wastes everyone's time re-discussing what's already settled. The skill is recognizing the moment the thinking is genuinely *done*.

Switch to planning mode when **all** of these are true:

- **The decisions are made and written down.** Not "we kind of agreed" — actually recorded, like the five numbered decisions at the end of `0005`. If you can't point to the decision, you're still in discussion.
- **The remaining work is "how," not "whether."** Once the team chose "surgically update `004_styleguide.md`, no full rewrite," the open questions became *which six edits* and *who makes them* — pure execution.
- **You can name owners.** Every real plan item has a single accountable person. The `0005` Next Steps table assigns each action to `[Quality]`, `[Backend]`, `[Architect]`, or `[Frontend]`. No owner means it won't happen.
- **You can size it.** Rough is fine. `0006` sizes the whole effort as "roughly a half-day of doc work. No code migration." A plan you can't size is a discussion wearing a plan's clothes.
- **There's a sequence.** Some steps depend on others. The styling work has a clear order: update the guide, then write the rule, then add the contributing note, then write the sync checklist.

One explicit guardrail from our own process: planning mode is *not* the place to relitigate findings. In `0005` the team treats the research as solid and spends its energy on "what do we *do*." If new doubt appears mid-plan that genuinely invalidates a decision, you don't argue it in the plan — you drop back into discussion mode for that one item (see [§7](#switching)).

---

<a id="run-discussion"></a>
## 4. Running a Discussion Session

**Why it matters:** an unstructured discussion wanders and an over-structured one suppresses the disagreement you're there to find. The goal is a session that stays open long enough to surface every real risk, then *converges* on decisions. Here is the flow our team actually uses, drawn from `0003`–`0005`.

1. **Hand off the context (the pre-brief).** Before the group meets, the person closest to the problem does a low-stakes "download" — the hallway version. This is exactly what `0003` is: the CTO tells the lead the story, the artifacts in hand, and the *ask*, with nothing decided yet. It saves the group from re-deriving context live.
2. **Frame, don't debate.** Open by stating the problem and the decision surface, then stop. In `0004` the lead's whole job is framing: "the inference era is over, here's the author's real style, our job is to decide how we adopt and maintain it," followed by the four bounded questions. Framing is not the argument.
3. **Bound the decision surface.** Turn a vague topic into a finite list of questions. `0004` lands on exactly four. Bounded questions are what keep discussion from becoming "rewrite everything."
4. **Assign angles.** Give each contributor a lens so coverage is deliberate, not random — e.g., "[Backend] takes the C# conventions, [Frontend] takes the Razor rules, [Quality] validates the findings, [Sanity] keeps us from over-engineering, [JrDev] keeps us honest on assumptions" (`0004`). Angles guarantee blind spots get a designated owner.
5. **Validate the inputs fast.** If the discussion rests on research or data, sanity-check it *first* so you're "not building on sand" — Quality's Round 0 pass in `0005` re-runs the numbers (`187 files`, `String.Empty` `454×`) before any decision is debated.
6. **Run the rounds, one question at a time.** Debate each bounded question to a conclusion before moving on. Encourage the challenge and the dumb-smart question; that's where the habit-vs-rule insight came from.
7. **Insert a sanity check.** Pause mid-way and at the end to ask "are we overcomplicating this?" In `0005` the [Sanity] mid-check explicitly lists what the team *didn't* decide (no mass reformat, no CI gate, no rewrite) as evidence the altitude is right.
8. **Converge — record each decision as you settle it.** The moment a question is answered, write the decision down in plain language. Don't carry "I think we agreed" into the next round.

The output of this session is **decisions**, not tasks. Tasks come next, in planning mode.

---

<a id="run-planning"></a>
## 5. Running a Planning Session

**Why it matters:** a decision that never becomes an owned, sized, sequenced step is just a nice conversation. Planning mode is the conversion machine. It is shorter and more mechanical than discussion — that's a feature.

1. **Start from the recorded decisions.** Copy the settled decisions in verbatim and treat them as fixed. The `0005` **Decisions** list (five numbered items) is the input; planning does not edit it.
2. **Decompose each decision into concrete actions.** "Surgically update `004`" becomes specific edits: add the property-brace rule, promote `String.Empty` to a mandate, add the wrapped-param convention, add the ordering rules, note the `Helpers.` hygiene. Each is a thing a person can actually do.
3. **Assign a single owner per action.** One name, not a committee. The `0005` Next Steps table does this — every row has exactly one owner (`[Quality]`, `[Backend]`, etc.). Shared accountability is no accountability.
4. **Set priority and sequence.** Mark what's urgent and what depends on what. `0005` uses `P1`/`P2`; `0006` orders the work "this sprint" vs. "next sprint." If item B needs item A first, say so.
5. **Size the effort, roughly.** A range is fine. `0006` sizes the whole thing as "roughly a half-day of doc work" and tags each line `tiny` / `small` / `~2 dozen lines`. Sizing exposes a plan that's secretly enormous.
6. **Name risks and mitigations.** For anything irreversible, attach the guardrail. `0006` pairs each risk with a mitigation — e.g., "Someone runs `dotnet format` and detonates a 454-line diff" → "explicit 'do not `dotnet format`' note in contributing docs." This is the planning-mode version of paranoia.
7. **Flag what's not yours to decide.** If a step needs sign-off above the team, isolate it. `0006` carves out exactly one call for the CTO ("should we ever realign Brad's `.editorconfig`?") with the team's lean stated, so the plan can proceed on a single nod.

The output is a **checklist** the team can execute without re-thinking — owners, order, size, risks. If you find yourself arguing *whether* during planning, you've slipped back into discussion; stop and see [§7](#switching).

---

<a id="roles"></a>
## 6. Roles and Responsibilities

**Why it matters:** the same people behave differently in each mode, and confusion about who decides is what turns a discussion into a stalemate or a plan into a free-for-all. These roles are the named voices from [061 — The Roleplay Team](061_roleplay-team.md); here's how each shows up per mode.

| Role | In Discussion mode (explore) | In Planning mode (execute) |
|------|------------------------------|----------------------------|
| **[Architect] / Lead** | Frames the problem, bounds the questions, assigns angles, breaks ties | Sequences the work, owns cross-cutting items, decides "this sprint vs. next" |
| **[Backend] / [Frontend]** | Bring domain expertise to their angle; flag what's impractical | Own and size the concrete actions in their area |
| **[Quality]** | Validates the inputs first so the group isn't building on sand | Owns the diff, keeps changes small and cited, writes the warnings |
| **[Sanity]** | Asks "are we overcomplicating this?"; lists what we *didn't* decide | Pulls the cord if the plan balloons past its sized budget |
| **[JrDev]** | Asks the dumb-smart question; surfaces hidden assumptions ("habit, not rule") | Keeps the checklist honest — "is this actually doable as written?" |
| **The decider (CTO / owner)** | Sets the default tiebreaker; receives the decisions | Ratifies the plan; rules only on the one call escalated to them |

Two rules that hold across both modes:

- **There is exactly one decider, and they are explicit.** In our workflow the CTO sets the default tiebreaker up front ("when the author's hand and the tooling disagree, the author wins") so the team can resolve most things without escalating. Only the genuinely-upstream call goes back up.
- **Challenge is a job, not an attitude.** [Sanity] and [JrDev] are *assigned* to push back. That makes disagreement safe and expected rather than personal — which is exactly what keeps discussion productive.

---

<a id="switching"></a>
## 7. Switching Between Modes

**Why it matters:** the most common process failure isn't choosing the wrong mode — it's not noticing you've drifted out of the one you intended. Knowing the tells lets you correct in seconds instead of wasting a whole session.

**How to tell which mode you're actually in.** Listen to the verbs:

- Discussion sounds like *"what if… why… I'm not sure… it depends… have we considered…"* (divergence).
- Planning sounds like *"who… by when… how big… what's the order… what could go wrong…"* (convergence).

**Discussion → Planning** (the normal, healthy transition). Move when the recorded decisions exist, the remaining questions are all "how" not "whether," and [Sanity] has signed off that you're not overcomplicating it. The clean handoff in our docs is the moment `0005` stops debating and starts listing **Decisions** and **Next Steps** — same document, deliberate gear change.

**Planning → Discussion** (the emergency exit, used sparingly). Drop back when, mid-plan, someone surfaces a real reason a settled decision is wrong — not a preference, an actual flaw. The discipline is to **scope the regression**: re-open only the *one* affected decision, not the whole topic. Note that `0005` deliberately refuses to "reopen the findings" — re-litigation is the failure this rule guards against. Re-explore the single broken item, re-decide it, then return to planning.

**Anti-patterns to catch yourself doing:**

- **Planning too early.** Writing the task list before the fork in the road is resolved. Tell: the checklist has a step that secretly assumes an unmade decision.
- **Discussing forever.** Re-opening settled questions because deciding feels risky. Tell: the same argument recurs across sessions with no new information. [Sanity]'s "I went in expecting to pull the cord" check exists to catch this.
- **Blending the modes in one breath.** Trying to decide *and* assign in the same sentence, so people won't float bold ideas (they'll get assigned) and the plan inherits unresolved debate.

When in doubt, ask the room one question: *"Are we still deciding what to do, or are we listing how to do it?"* The answer names the mode.

---

<a id="outputs"></a>
## 8. Outputs and What Good Looks Like

**Why it matters:** a mode is only "done" when it produces its artifact at the right quality. These are the concrete deliverables and the bar for each — grounded in the real documents the styling workflow produced.

**Discussion mode → a decision record.**

- *Artifact:* a written set of decisions in plain language, each tied to its rationale and any dissent. The model is the **Decisions** section of `0005` — five numbered decisions, plus an explicit "Flagged for CTO (tie not ours to break)" line capturing the one unresolved call.
- *Good looks like:* every bounded question has an answer; the answers cite their source ("the author's hand wins — that's the CTO's tiebreaker"); dissent and open ties are recorded, not buried; and a [Sanity] pass confirms the scope didn't balloon. If a reader can tell *what was decided and why* without having been in the room, the record is good.

**Planning mode → an executable checklist (and, when it goes up, a brief).**

- *Artifact:* a Next-Steps table with **Owner** and **Priority** per action, rough sizing, and risks-with-mitigations — exactly the shape of the `0005` Next Steps table and the `0006` Effort / Risks tables. When the plan needs sign-off above the team, it's packaged as a **CTO brief** (`0006`): a TL;DR, what changed, the recommendation, the single decision that needs the decider, and the trail of how you got there.
- *Good looks like:* every action has one owner; the whole thing is sized (ours was "roughly a half-day"); irreversible steps carry a named mitigation; and exactly the escalation-worthy item — no more — is flagged up the chain. A good brief lets the decider act on a single nod, because everything within the team's authority is already settled.

The shared quality bar across both: **someone who wasn't present can act correctly from the artifact alone.** That is the real test of whether the mode finished its job. For the full decision-record and brief formats, see [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md).

---

<a id="related-docs"></a>
## 9. Related Docs

- [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) — the team and its roles
- [063 — Running a Focus Group](063_focus-group.md) — a deep decision focus group
- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — recording the outcome

---
*GuidesV2 062 · drafted from source (Docs/GuidesV2/0003–0006 workflow) · 2026-06-04.*
