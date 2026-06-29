# 065 — Consulting a Single Role

> **Document ID:** 065  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Put a question to one team member instead of the whole room.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)
> **Skill candidate:** this process should also ship as the `/ask-role` slash-command.

---

## In This Doc

| # | Section | What it will cover |
| --- | --- | --- |
| 1 | [Why This Matters](#why-this-matters) | What "ask one role" means and why a single voice often beats the whole room |
| 2 | [When to Use It](#when-to-use) | The signals that point to one role instead of a full discussion |
| 3 | [Choosing the Right Role](#choose-role) | Matching the question to the role that owns that concern |
| 4 | [How to Ask](#how-to-ask) | The exact phrasing and a step-by-step request procedure |
| 5 | [What the Role Owns](#role-scope) | Each role's focus, key questions, and the edge of its authority |
| 6 | [The Answer You Get Back](#the-output) | The shape of a single-role answer and how to act on it |
| 7 | [Pitfalls and Edge Cases](#pitfalls) | Misrouted asks, escalation, and ambiguous ownership |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

Most of this band (the 06x docs) is about a deliberately theatrical habit: when we make a decision, we pretend a small **team** of named specialists is in the room and let them argue. That practice is called **roleplay** — one human (you, the CTO) prompts the AI to answer *as if* it were several different experts, each with one job to worry about. The full roster lives in [061 — The Roleplay Team and Its Roles](061_roleplay-team.md), which is the in-repo source of truth for who's on the team.

The full team is powerful, but it is also expensive. Convening every voice for a five-second question is like calling an all-hands meeting to ask "is this method name okay?" You get a wall of cross-talk, mid-discussion sanity checks, and a written summary — for something that needed one sentence from one person.

**Consulting a single role** is the cheap, surgical alternative. Instead of "let's roleplay this," you say *"ask [Backend]: is this the right place to put the validation?"* and you get one focused answer, in that role's voice, scoped to that role's concern. No debate, no summary doc, no ceremony.

Why it matters in plain terms:

- **Speed.** One voice answers in one breath. You skip the round-robin where every role weighs in even when only one has standing.
- **Less noise.** A targeted ask keeps the AI from over-thinking. The [Sanity] role exists precisely because the team tends to over-engineer; a single-role consult sidesteps that risk by keeping the question small.
- **Right expertise, on the record.** You get the answer from the role that actually *owns* the concern, framed through that role's lens — which is more useful than a blurred "the team thinks…" average.

The rule of thumb for this whole band: **match the size of the ceremony to the size of the question.** A typo gets no roleplay at all. A new feature gets the full team (see [063 — Running a Focus Group](063_focus-group.md)). A single, well-bounded question with a clear owner gets *exactly one role* — that's this doc.

<a id="when-to-use"></a>
## 2. When to Use It

Reach for a single-role consult when **all** of these are true:

- **The question has one clear owner.** It lands squarely in one specialist's lane — "what should the schema look like?" is obviously [Backend]; "what does the loading state do?" is obviously [Frontend].
- **You want a perspective, not a verdict.** You're checking *one* angle, not deciding a contested tradeoff. Tradeoffs with multiple valid answers belong to the full team.
- **The answer won't ripple far.** Low "blast radius" — a term [Architect] uses for *how much else a change could break*. If the answer can only touch one corner of the code, one role can field it.
- **You already roughly know the shape of the answer** and just want it confirmed or refined by the owner.

Concretely, this maps onto the "Change Size" ladder from [061 — The Roleplay Team and Its Roles](061_roleplay-team.md):

| Change Size | Examples | Approach |
|-------------|----------|----------|
| **Tiny** | Fix typo, update comment | Just do it — no role needed |
| **Small** | Add a field, simple bug fix | **A single-role question** (this doc) |
| **Medium** | New endpoint, UI component | Planning checklist (see [062](062_discussion-planning-modes.md)) |
| **Large/Unclear** | New feature, architecture change | Full discussion / focus group |

**Use the full team instead when:** requirements are ambiguous, several valid approaches exist, the decision is high-impact, or two concerns are in tension (e.g. [Backend] wants one schema, [Frontend] needs another). The first three echo the "Pause for CTO" conditions from the roleplay guide — the team is genuinely split, requirements are ambiguous, or the decision is high-impact — and any of them is a sign you've outgrown a single-role ask. When in doubt, start with Discussion mode; you can always narrow to one role once you know who owns the question.

<a id="choose-role"></a>
## 3. Choosing the Right Role

Picking the right role is most of the work. Ask: *whose job is it to worry about this?* The standard team, taken verbatim from [061 — The Roleplay Team and Its Roles](061_roleplay-team.md), maps cleanly onto question types:

| If your question is about… | Ask this role | Because its focus is… |
|----------------------------|---------------|-----------------------|
| System design, how a change fits, what it could break | **[Architect]** | System design, patterns, boundaries |
| Data shape, database tables, APIs, services, storage | **[Backend]** | Data, APIs, services, storage |
| The screen, components, user flow, loading/error states | **[Frontend]** | UI, components, UX, state |
| Tests, security, documentation, "what could break?" | **[Quality]** | Tests, security, docs |
| "Are we overcomplicating this?" / a reality check | **[Sanity]** | Reality checks, complexity |
| "Wait, why are we even doing X?" / a naive clarifying question | **[JrDev]** | Clarifying questions |

Two notes that matter:

- **[CTO] is you, the human.** It is not a role the AI plays — it's the seat that makes final decisions. You don't "ask [CTO]"; you *are* the [CTO] doing the asking.
- **The roster adapts to the project.** The names above are the defaults for a web app like FreeCRM. The roleplay guide explicitly says to swap roles to fit the work — e.g. an API-only project might use `[API]`, `[Database]`, `[Consumer]`, `[Ops]`; a library might use `[PublicAPI]`, `[Internals]`, `[Perf]`, `[Docs]`. Pick the role that owns the concern *in your project's vocabulary*.

If two roles could plausibly own the question, that is itself a signal — see [§7 Pitfalls](#pitfalls). Often the cleaner move is to ask **[Architect]** first ("who owns this?") or to fall back to the full team.

<a id="how-to-ask"></a>
## 4. How to Ask

The mechanics are simple — the discipline is in the framing. A good single-role ask is **named, scoped, and answerable in a breath.**

**Step by step:**

1. **Name the role explicitly.** Put it in brackets so the AI knows to answer in-character: `ask [Backend]: …`. The brackets are the same convention used throughout [061 — The Roleplay Team and Its Roles](061_roleplay-team.md).
2. **State the one question.** One concern, not three. If you have three, you probably want the team.
3. **Give just enough context.** Point at the file or area in question. Use the project's real file names — for example a code partial like `FreeManager.App.EntityWizard.State.cs`, following the mandatory `{ProjectName}.App.{Feature}.{Extension}` naming rule from the style guide.
4. **Say what "answered" looks like.** A recommendation? A yes/no? A short list of risks? Telling the role what you need keeps the reply tight.

**The phrasing pattern:**

```
ask [Role]: {one specific question} — context: {file or area}
```

**Worked examples:**

```
ask [Backend]: should the tenant-scoping filter live in DataAccess or in the controller? — context: the new export endpoint
ask [Frontend]: what should this show while the list is loading? — context: FreeManager.App.EntityWizard.razor
ask [Sanity]: are we overcomplicating this retry logic, or is it justified?
ask [Quality]: what's the one test most likely to catch a regression here?
```

If, partway through the answer, the role hits something genuinely contested — multiple valid approaches, or a call above its pay grade — it should stop and hand it back to you using the **CTO Input Needed** block from the roleplay guide rather than guessing:

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

That escape hatch is what keeps a one-role ask honest: a single voice answers what it owns, and explicitly punts what it doesn't.

<a id="role-scope"></a>
## 5. What the Role Owns

Each role has a **focus** (the thing it cares about) and a set of **key questions** (the lens it brings). These are not invented for this doc — they're lifted straight from the team table in [061 — The Roleplay Team and Its Roles](061_roleplay-team.md). Knowing them tells you both *what to expect back* and *where the role's authority ends.*

| Role | What it owns (focus) | The questions it asks |
|------|----------------------|-----------------------|
| **[Architect]** | System design, patterns, boundaries | "How does this fit? What's the blast radius?" |
| **[Backend]** | Data, APIs, services, storage | "What's the schema? What endpoints?" |
| **[Frontend]** | UI, components, UX, state | "What's the user flow? Loading states?" |
| **[Quality]** | Tests, security, docs | "How do we test this? What could break?" |
| **[Sanity]** | Reality checks, complexity | "Are we overcomplicating this?" |
| **[JrDev]** | Clarifying questions | "Wait, why are we doing X?" |
| **[CTO]** | **You, the human** | Final decisions |

**Where authority ends.** A single role advises within its lane; it does not get the final say. That seat belongs to **[CTO]** — you. The roles surface a perspective; you decide. So a single-role consult produces a *recommendation,* not a *ruling.* If a role starts making a cross-cutting call — say [Backend] unilaterally dictating the whole UX — that's a sign the question was bigger than one role and should go to the team.

A useful mental model: each role is a **specialist consultant**, not a **manager**. You bring them a question in their specialty, they give you their best read, and you own what happens next.

<a id="the-output"></a>
## 6. The Answer You Get Back

A single-role consult is intentionally lightweight, so the output is too. Unlike a focus group — which produces a written meeting doc with a transcript, decisions, and next steps (see the template in [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) and [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md)) — a one-role ask usually returns just **a short, in-character answer.** No formal artifact is required.

What a good answer looks like:

- **In the role's voice and lane.** [Backend] answers in terms of schema and endpoints; [Frontend] in terms of flow and states. The framing is part of the value.
- **Direct.** It leads with the recommendation or the yes/no, then gives the reasoning — not the other way around.
- **Honest about its limits.** If the question brushed against something outside the role's scope, the answer says so and either names the role that *does* own it or escalates to you via the `⏸️ CTO Input Needed` block.

**How to use it.** Because the answer is a recommendation, not a decision, *you* close the loop. Three common moves:

1. **Act on it** — the ask was small, the answer is clear, you proceed.
2. **Write it down — but only if it's load-bearing.** Most single-role answers are too small to record. If the answer settles something that future-you (or a teammate) will need the *why* for, capture it as a short decision record / ADR. (ADR = "Architecture Decision Record," a few lines noting the context, the choice, and why — template in [067](067_decisions-and-briefs.md).)
3. **Escalate** — if the single voice revealed that the question is actually contested, take it to the full team or run a focus group.

The discipline here is restraint: don't manufacture a meeting doc for a one-sentence answer, and don't bury a genuinely important decision in a throwaway chat reply.

<a id="pitfalls"></a>
## 7. Pitfalls and Edge Cases

**Asking the wrong role (misrouting).** The answer will be confidently in-lane but irrelevant — you'll get a clean [Frontend] answer to what was really a [Backend] question. Fix: before asking, sanity-check *whose job* the question is (see [§3](#choose-role)). If you're unsure, ask **[Architect]** "who owns this?" first.

**A question that secretly has two owners.** If you find yourself wanting to ask two roles the *same* question — or one role keeps deferring to another — the question isn't single-role at all. That's the tell that two concerns are in tension, which is the team's territory, not one voice's. Promote it to a discussion or focus group.

**Treating the answer as a ruling.** A single role *advises*; it does not *decide.* The decision seat is [CTO] — you. Taking one role's recommendation as gospel skips the human judgment the whole system is built around. The answer is an input to your call, not the call itself.

**Skipping the team when you should have convened it.** Single-role asks are seductive because they're fast, and it's tempting to chain a dozen of them instead of one honest discussion. If you're firing off many related single-role questions about one change, stop — that change wanted a planning checklist or a focus group from the start. The "pause for CTO" conditions from [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) are your guardrail: the team is genuinely split, requirements are ambiguous, or the decision is high-impact.

**Over-ceremony in the other direction.** The opposite failure: writing a full meeting doc or ADR for a trivial confirmation. Match the artifact to the weight of the answer. Most single-role answers leave no paper trail, and that's correct.

**The escape hatch is not a failure.** When a role stops and hands the question back to you via `⏸️ CTO Input Needed`, that's the system working — a single voice correctly recognizing it has hit a decision above its lane. Don't pressure a role to "just pick one" when it has flagged a real tradeoff.

---

<a id="related-docs"></a>
## 8. Related Docs

- [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) — the team and its roles
- [062 — Discussion Mode, Planning Mode](062_discussion-planning-modes.md) — when one voice beats the room

---
*GuidesV2 · 065 · drafted from source ([061 — The Roleplay Team and Its Roles](061_roleplay-team.md) and the 06x band) · 2026-06-05.*
