# 061 — The Roleplay Team and Its Roles

> **Document ID:** 061  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Introduce the named team and how multi-voice discussion surfaces blind spots before code.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)
> **Skill candidate:** this process should also ship as the `/roleplay` slash-command.

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-it-matters) | What "roleplay" means here and why we argue before we code |
| 2 | [When to Use Roleplay](#when-to-use) | Matching the size of the change to the size of the conversation |
| 3 | [Meet the Team](#the-roles) | The six named voices, plus you (the CTO), and what each one guards |
| 4 | [Running a Session](#running-a-session) | The ordered flow from framing to decisions |
| 5 | [Surfacing Blind Spots](#blind-spots) | How disagreement between voices exposes hidden risk |
| 6 | [The Output](#the-output) | The decision artifact a session produces before any code is written |
| 7 | [Pitfalls and Anti-Patterns](#pitfalls) | Failure modes that hollow out a roleplay |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Matters

**The short version:** before we write code for anything non-trivial, we hold a short, structured argument with ourselves — playing several named characters, each with one job. Catching a bad assumption in a five-minute conversation is far cheaper than catching it in a pull request, a merge conflict, or production.

A few terms, in plain language:

- **Roleplay** — One person (or one AI session) speaks *as* several different characters in turn, each with a fixed point of view. You are not pretending to have a real team; you are deliberately forcing yourself to look at the same problem through several different lenses so you don't miss the obvious thing your own lens hides.
- **Multi-voice discussion** — The written back-and-forth between those characters. Each voice is tagged in square brackets, like `[Architect]:` or `[Sanity]:`, so the transcript reads like meeting minutes.
- **"Before code"** — The whole point is that this happens *before* implementation. The output is a decision, not a diff. If you're already typing code, you've skipped the cheap step.

Why bother instead of just deciding? Because a single mind, working alone, tends to fall in love with its first idea. It under-weights testing, forgets the deployment story, and over-engineers because over-engineering feels productive. Assigning those concerns to *named, separate* voices makes them impossible to skip — the `[Quality]` voice will always ask "how do we test this?", even when you'd rather not think about it, because that is literally the only thing `[Quality]` is allowed to care about.

The real focus-group transcript in `Docs/GuidesV2/0005_meeting.team_focus_group.md` shows this in action: six voices turned a pile of styling research into five concrete decisions and one item flagged for the human to break a tie — all on paper, before a single file was edited.

---

<a id="when-to-use"></a>
## 2. When to Use Roleplay

**Why it matters:** roleplay is overhead. Spending it on a typo is waste; *not* spending it on a new feature is how you get a week of rework. The skill is matching the size of the conversation to the size of the change.

The project's own guidance (`Docs/Guides/001_roleplay.md`) lines this up directly:

| Change size | Examples | Approach |
|-------------|----------|----------|
| **Tiny** | Fix a typo, update a comment | Just do it — no roleplay |
| **Small** | Add a field, simple bug fix | A quick one-off AI question |
| **Medium** | New endpoint, new UI component | **Planning checklist** (see [062](062_discussion-planning-modes.md)) |
| **Large / unclear** | New feature, architecture change | **Full discussion** — a roleplay session |

**Rule of thumb:** if you don't yet know *how* to approach it, run a discussion first. If you already know what to do but don't want to miss anything, run a planning checklist instead.

Concrete signals that a problem has earned a full session:

- **More than one reasonable approach exists** and you're not sure which is right.
- **The blast radius is wide** — "blast radius" means how much else breaks if this is wrong. Touching a shared base class is high blast radius; tweaking one private method is not.
- **You keep going back and forth in your own head** — that internal tug-of-war is exactly what the voices are designed to externalize.
- **The decision is hard to reverse** — schema changes, public APIs, and anything that ships to users.

If none of those are true, skip it. A roleplay on a trivial change just adds ceremony.

---

<a id="the-roles"></a>
## 3. Meet the Team

**Why it matters:** each voice is a *single, narrow concern* made into a character. The value isn't the personality — it's that nothing important falls through the cracks, because every important concern has an owner who can't be talked out of caring about it.

These are the six discussion voices plus the human, taken directly from `Docs/Guides/001_roleplay.md`:

| Role | What it guards | The question it always asks |
|------|----------------|----------------------------|
| **[Architect]** | System design, patterns, boundaries — and usually runs the session | "How does this fit? What's the blast radius?" |
| **[Backend]** | Data, APIs, services, storage | "What's the schema? What endpoints?" |
| **[Frontend]** | UI, components, user experience, state | "What's the user flow? What about loading states?" |
| **[Quality]** | Tests, security, documentation | "How do we test this? What could break?" |
| **[Sanity]** | Reality checks, fighting complexity | "Are we overcomplicating this?" |
| **[JrDev]** | Clarifying questions, naming the unsaid | "Wait — why are we doing X?" |
| **[CTO]** | **You, the human** | Final decisions and tie-breaks |

A few notes that make these click:

- **[Architect]** is the team lead. In a session it frames the problem first and steers, but it does *not* get the final word — that's the CTO's job.
- **[Sanity]** is the most valuable and most under-used voice. Its entire job is to push back on cleverness. In the real 0005 transcript, `[Sanity]` repeatedly threatens to "pull the cord" if the plan balloons — and that pressure is what kept the team from rewriting a big file when a small edit would do.
- **[JrDev]** is permission to ask the dumb question. In 0005 it's `[JrDev]` who asks whether the team is treating a *habit* as if it were a *rule* — a question that reframed the whole discussion. The naive question is often the load-bearing one.
- **[CTO] is you.** The AI never decides the things that aren't its to decide. When the team is split or the call is high-stakes, the session pauses and hands the decision up to the human (see Section 4).

**Adapt the cast to the project.** The roles above fit a Blazor/CRM web app, but the same idea scales. `001_roleplay.md` suggests swaps — for an API-only project: `[API]`, `[Database]`, `[Consumer]`, `[Ops]`; for a library: `[PublicAPI]`, `[Internals]`, `[Perf]`, `[Docs]`. The principle is constant: pick a small set of voices that, between them, cover every concern that could sink the change.

---

<a id="running-a-session"></a>
## 4. Running a Session

**Why it matters:** an unstructured argument goes in circles. A structured one reaches a decision. The flow below is the cheapest path from "fuzzy problem" to "decision on paper."

The ordered flow, straight from `001_roleplay.md`:

1. **[Architect] frames the problem** — states what's being decided and the constraints, so everyone is arguing about the same thing.
2. **Specialists weigh in** — `[Backend]`, `[Frontend]`, `[Quality]` each give their perspective on the part they own.
3. **[Sanity] mid-check** — partway through, `[Sanity]` interrupts with "Are we overcomplicating this?" to catch scope creep before it sets.
4. **Discussion continues** — voices respond to each other; disagreement is good here (see Section 5).
5. **[Sanity] final check** — "Did we miss anything?" — a last sweep for gaps before closing.
6. **Summary** — the session ends with explicit decisions and next steps, not a vibe.

**Pausing for the CTO.** When the team is genuinely split, requirements are ambiguous, or the decision is high-impact, the session *stops* and asks the human. The transcript uses a clear, copy-pasteable block for this — reproduced faithfully from `001_roleplay.md`:

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

Notice how the real 0005 session uses every step: `[Quality]` opens with a validation pass (round 0), specialists work through four numbered questions, `[Sanity]` does a mid-check ("are we overcomplicating this?") and a final "did we miss anything?" sweep, and one genuinely upstream decision is flagged for the CTO rather than decided by the team.

---

<a id="blind-spots"></a>
## 5. Surfacing Blind Spots

**Why it matters:** a blind spot is, by definition, something you can't see by yourself. The mechanism that reveals it is *tension between voices* — one character wanting something another character resists. The friction is the feature.

How the disagreement does the work:

- **One voice wants it clean; another wants it cheap.** In 0005, `[Backend]` argued for a single authoritative styling doc, while `[Quality]` warned that silently rewriting a big, bookmarked file would create a *new* source of drift. The collision produced a better answer than either: keep the authoritative findings *and* surgically patch the day-to-day guide with a pointer. Neither voice alone would have landed there.
- **[Sanity] vs. everyone's cleverness.** Complexity feels like progress, so a lone author rarely catches their own over-build. A dedicated skeptic does. In 0005, `[Sanity]`'s mid-check explicitly listed what the team *didn't* do — no mass reformat, no CI gate, no full rewrite — and that negative list was the real win.
- **[JrDev] vs. unstated assumptions.** The "why are we doing X?" question forces hidden premises into the open. The 0005 transcript turns on `[JrDev]` noticing that "the author's explicit choices" were sometimes just muscle-memory habits — which changed how the whole team reasoned about the rules.

The healthy pattern is **disagree, then converge.** If every voice nods along immediately, the session isn't surfacing anything — it's theater (see Section 7). Real value shows up as a documented tension that ends in a decision, ideally with the *rejected* option written down too, so the next person understands why.

---

<a id="the-output"></a>
## 6. The Output

**Why it matters:** a session that ends in a feeling produces nothing reusable. A session that ends in a *written artifact* produces a decision you can point at, hand off, and revisit. The artifact — not the conversation — is the deliverable.

A roleplay produces a **meeting document**. The basic shape, from `001_roleplay.md`:

```markdown
# {NUM} — Meeting: {Topic}

> **Document ID:** {NUM}
> **Category:** Meeting
> **Purpose:** {what we're deciding}
> **Predicted Outcome:** {expected result}
> **Actual Outcome:** {what happened — fill in after}
> **Resolution:** {action taken — PR, decision, etc.}

---

## Discussion
[transcript]

## Decisions
- Decision 1
- Decision 2

## Next Steps
| Action | Owner | Priority |
|--------|-------|----------|
| Task | [Role] | P1 |
```

Two details that separate a real artifact from a chat log:

- **Predicted vs. Actual outcome.** The header asks you to write down what you *expect* before the discussion and what *actually* happened after. That gap is how the process learns — and it's a built-in honesty check. The real 0005 header reads: *Predicted — "Clear answers to the four questions";* *Actual — "✅ Five decisions reached; one item flagged for the CTO to break the tie."*
- **Decisions and next steps are explicit and owned.** Every decision is a bullet; every action has an owner (a `[Role]`) and a priority. 0005's "Next Steps" table assigns each follow-up to a specific voice — `[Quality]` owns the doc edits, `[Architect]` writes the contributing note, and so on.

For decisions worth preserving on their own, the same source offers a lightweight ADR — "Architecture Decision Record," a tiny note capturing *context, decision, rationale, consequences, and the alternatives you rejected*. The full templates live in [068](068_template-library.md), and the decision/brief workflow is covered in [067](067_decisions-and-briefs.md).

---

<a id="pitfalls"></a>
## 7. Pitfalls and Anti-Patterns

**Why it matters:** roleplay is easy to fake and easy to bloat. Both failures waste the time the process was meant to save. Watch for these:

- **Yes-man theater.** Every voice agrees instantly, the transcript is all nods, and no tension is recorded. If nobody pushed back, the session found nothing — and you'd have been faster just deciding. A good session has at least one real disagreement that gets resolved.
- **Roleplaying the trivial.** Running a full six-voice session on a typo or a one-line fix. Match the conversation to the change (Section 2); over-process is its own form of waste.
- **No decision at the end.** The discussion is interesting, everyone learned something — and the document ends without a Decisions list. An open-ended conversation isn't an output. If you can't write the decision down, the session isn't finished.
- **Skipping the human on a split.** The team is genuinely divided or the call is high-stakes, and the AI picks a side anyway instead of pausing for the CTO. Some decisions aren't the team's to make — flag them up.
- **Letting [Sanity] go quiet.** When nobody plays the skeptic, scope quietly balloons and complexity creeps in unchallenged. The mid-check and final-check exist precisely so this can't happen silently — use them.
- **Personality over substance.** Spending effort making the voices "sound like" distinct people instead of making them *cover distinct concerns*. The square-bracket tags are bookkeeping, not characters. The value is the coverage, not the costume.

The simplest health test: at the end, can you point to a decision that's *better* than what you'd have produced alone, and name the disagreement that produced it? If yes, the roleplay earned its keep. If no, you ran theater.

---

<a id="related-docs"></a>
## 8. Related Docs

- [062 — Discussion Mode, Planning Mode](062_discussion-planning-modes.md) — exploring a problem versus running an execution checklist
- [063 — Running a Focus Group](063_focus-group.md) — the deeper, multi-round decision format
- [064 — The Sanity Check](064_sanity-check.md) — the dedicated skeptic voice and its mid/final checks
- [065 — Consulting a Single Role](065_ask-one-role.md) — when you only need one voice, not the whole team
- [066 — Explaining to the Intern-CTO](066_explain-to-the-cto.md) — translating decisions into plain language
- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — recording the outcome of a session
- [068 — The Documentation Template Library](068_template-library.md) — the meeting, ADR, and planning templates

---
*GuidesV2 061 · drafted from source (`Docs/Guides/001_roleplay.md`, `Docs/GuidesV2/0005_meeting.team_focus_group.md`) · 2026-06-05.*
