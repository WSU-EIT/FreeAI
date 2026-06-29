# 063 — Running a Focus Group

> **Document ID:** 063  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** The deep version: the team debates a specific decision to a conclusion, with sanity checks, and writes it up.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)
> **Skill candidate:** this process should also ship as the `/focus-group` slash-command.

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [What a Focus Group Is](#what-it-is) | Plain-language overview and the key terms defined |
| 2 | [When to Run One](#when-to-use) | Which decisions earn this deep, multi-round treatment |
| 3 | [Roles Around the Table](#roles) | Who frames, who challenges, who breaks ties, who writes it up |
| 4 | [Framing the Decision](#framing) | Reducing the topic to a few bounded, answerable questions |
| 5 | [Running the Debate](#debate) | The round-by-round flow from open question to settled decision |
| 6 | [Sanity Checks](#sanity-checks) | The mid-check and final-check that stress-test the conclusion |
| 7 | [Writing It Up](#write-up) | Capturing decisions, owners, dissent, and the tie for the CTO |
| 8 | [Common Pitfalls](#pitfalls) | The failure modes that hollow out a focus group, and how to dodge them |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="what-it-is"></a>
## 1. What a Focus Group Is

A **focus group** is the deepest of this team's discussion formats: the named roleplay team takes *one specific decision* and argues it, round by round, until they reach a settled conclusion — then they write that conclusion down with its reasoning and its dissent. It is the heavyweight option. Most questions do not need it; a few do, and for those it is the difference between a choice that holds up and a choice that quietly unravels two weeks later.

**Why it matters:** the expensive mistakes in software are almost never typos. They are decisions made too fast, by one person, with one perspective, that nobody stress-tested before everyone built on top of them. A focus group is a deliberate slowdown applied to exactly those moments. By forcing a real debate among voices that each care about a different thing — the backend, the front end, quality, the skeptic, the newcomer — it surfaces objections *before* code is written instead of after it ships.

A few terms used throughout this doc, defined in plain language:

- **The roleplay team** — a fixed cast of named voices (introduced in [061](061_roleplay-team.md)), each representing one concern: `[Architect]` (team lead and tie-breaker), `[Backend]`, `[Frontend]`, `[Quality]`, `[Sanity]` (the professional skeptic), and `[JrDev]` (the junior developer who asks the "dumb-smart" questions). They are roles a single author plays in turn, not separate people.
- **A round** — one pass through a single sub-question, where each relevant voice states its position and they converge or escalate. A focus group is a sequence of rounds.
- **A decision record** — the written artifact a focus group produces: the questions, the answers, who owns each follow-up, and any tie flagged upward. (Covered in depth in [067](067_decisions-and-briefs.md).)
- **The tiebreaker** — a rule, agreed *before* the debate, that settles a deadlock so the group never stalls. In our worked example it was "the author's hand beats the tooling."

The model to hold in your head: a focus group is a *structured argument with a referee and a scribe*, aimed at one decision, that ends in a written conclusion rather than a vibe.

The rest of this doc is grounded in a real focus group this team ran — [`Guides/0005_meeting.team_focus_group.md`](0005_meeting.team_focus_group.md), where the team decided how to align our code style with the FreeCRM upstream author. Every pattern below is illustrated from that actual transcript.

---

<a id="when-to-use"></a>
## 2. When to Run One

The honest answer is *rarely*. A focus group costs real time and attention, so it is reserved for decisions where getting it wrong is expensive and getting it right is non-obvious. Use the lighter formats — a single-role consult ([065](065_ask-one-role.md)), a quick discussion or planning pass ([062](062_discussion-planning-modes.md)) — for everything else.

Run a focus group when **two or more** of these are true:

- **The decision is hard to reverse.** Anything where teams will build on the outcome — a data model shape, a convention everyone must follow, a policy about an external dependency you do not control. The worked example qualified because the choices ("do we mass-reformat our code?", "do we add a CI gate?") would have been painful to walk back.
- **Reasonable people would disagree.** If the answer is obvious, debating it is theater. A focus group earns its keep only when there is genuine tension — for instance, "follow the author's hand-written `String.Empty`" versus "follow his own `.editorconfig`, which prefers lowercase `string.Empty`." Both had a real case; that tension is exactly what a focus group is for.
- **Multiple perspectives are affected.** When a choice touches the back end, the front end, *and* quality at once, no single role can see the whole picture. The styling decision rippled across C# conventions (`[Backend]`), Razor conventions (`[Frontend]`), and enforcement/tests (`[Quality]`) — so all three needed a seat.
- **There is a hidden foot-gun.** Decisions where the naive action is actively dangerous deserve the deep pass. In the example, the trap was that running `dotnet format` (the standard .NET auto-formatter) would have rewritten 454 occurrences of `String.Empty` against the convention and collided with the next upstream merge. A focus group is how you catch that *before* someone runs the command.

**When to skip it.** If the decision is cheap to reverse, uncontroversial, or owned cleanly by one role, do not convene the whole team — that is over-process, and the skeptic will (rightly) pull the cord. The test the team itself used: *"is this a few bounded questions, or 'rewrite everything'?"* If you cannot bound it to a handful of answerable questions (Section 4), it is not ready for a focus group yet.

---

<a id="roles"></a>
## 3. Roles Around the Table

A focus group is not a free-for-all; each voice has a job. Understanding who does what is what keeps the debate productive instead of circular. The full cast is introduced in [061](061_roleplay-team.md); here is how each role *functions inside a focus group specifically*.

**`[Architect]` — facilitator, framer, and tie-breaker.** The team lead opens by reducing the topic to bounded questions (this framing happens one step earlier, in the team-intro step — see [`Guides/0004`](0004_meeting.lead_team_introduction.md)), keeps the rounds moving, and — crucially — *breaks ties using the pre-agreed rule*. In the worked example the Architect set and applied the tiebreaker: "our job isn't to be right about style in the abstract — it's to not create merge friction with [the upstream]. If our code matches his fingers, his merges land clean." The facilitator also decides what is the team's call versus what must be flagged up to the CTO.

**`[Sanity]` — the professional skeptic.** This is the most important role in a focus group and the one that distinguishes it from an ordinary discussion. `[Sanity]`'s mandate is to ask, repeatedly, *"are we overcomplicating this, and what did we miss?"* The skeptic holds a veto ("pulling the cord") against scope creep and runs the formal mid-check and final-check (Section 6). In the example, `[Sanity]` is what kept the outcome small: *"Notice what we didn't decide: we did not approve a mass reformat… we did not add a CI gate… Every decision was 'document the truth, follow the author, make a cheap habit.'"* The sanity check is important enough that it has its own doc, [064](064_sanity-check.md).

**`[JrDev]` — the challenger.** The junior developer asks the questions a polished expert would be embarrassed to ask — and which usually turn out to be the load-bearing ones. `[JrDev]`'s job is to surface unexamined assumptions, especially the gap between *habit* and *rule*: *"some of them are clearly habit, not law… how confident are we that [this] is a rule he'd defend versus just how his fingers move?"* That single question reframed the entire debate.

**`[Backend]` and `[Frontend]` — the domain advocates.** Each owns one slice of the system and argues from that lens: `[Backend]` for the C# conventions and data layer, `[Frontend]` for the Razor/UI side. They are the ones who know whether a proposal is *practical* in their domain — `[Frontend]`, for instance, was the one who flagged that the upstream-sync habit needs to watch the `.razor` and `.editorconfig` files specifically, "since that's where the Razor-side conventions… live."

**`[Quality]` — validator and scribe.** `[Quality]` opens with a fast validation pass so the debate is "not building on sand" (in the example, re-running the numbers against the real working tree before any decision), and typically *owns the write-up* — the decision record and the next-steps table. Putting validation and documentation in the same hands is deliberate: the person who checked the facts is the right person to record the conclusion.

The healthy dynamic is **tension, not hierarchy**. The advocates push for their domain, `[JrDev]` and `[Sanity]` push back, and `[Architect]` resolves. Nobody is trying to "win"; the disagreement *is* the value.

---

<a id="framing"></a>
## 4. Framing the Decision

A focus group lives or dies on its framing. The single biggest cause of a bad session is a vague topic — "let's talk about styling" — that lets the debate sprawl in every direction. The fix is to do the framing work *before* the debate opens and to reduce the topic to a small set of bounded, answerable questions.

**Why it matters:** a bounded question can actually be answered in a round and crossed off. An unbounded one ("should we improve our code?") generates infinite discussion and zero decisions. The skeptic's very first contribution in the worked example was a scoping demand: *"Before anyone gets excited and proposes a big migration: what's the actual decision surface here? I want to scope this so we don't invent work."*

**The framing recipe:**

1. **State the premise as given.** Decide up front what is *not* up for debate so the group does not re-litigate settled facts. In the example: *"Take the findings in [the research] as solid… we don't reopen the findings. We decide what we do."* This one move saved the entire session from collapsing back into the research.
2. **Reduce to a few numbered questions.** The styling focus group was framed as exactly four:
   - **Q1** — Update the day-to-day style guide to match the author's explicit rules, or keep the research docs as standalone sources that supersede it by reference?
   - **Q2** — The `String.Empty` problem: the author writes `String.Empty` by hand 450+ times, which contradicts his own `.editorconfig` (which prefers lowercase `string.Empty`). Follow his hand or follow the config?
   - **Q3** — Enforcement: automate any of this with a CI gate, or is that a trap given the hand-conventions a formatter cannot express?
   - **Q4** — Sync: how do we stay aligned each time we merge from upstream, without redoing the whole investigation?

   `[Sanity]`'s verdict on that framing was the tell that it was good: *"That's bounded. Four questions, not 'rewrite everything.'"*
3. **State the tiebreaker before the argument.** Agree, in advance, on the rule that settles a deadlock — otherwise a tie just stalls the group. Here it was CTO-set: *"when the author's hand and the tooling disagree, the author wins."* Having it pre-agreed meant Q2 could be resolved on principle instead of on stamina.
4. **Assign angles.** Give each role its lens going in, so the debate has built-in coverage instead of everyone arguing the same point. `[Backend]` owns the C# conventions, `[Frontend]` the Razor rules, `[Quality]` the "which doc is authoritative" question and enforcement risk, `[Sanity]` the anti-over-engineering watch, `[JrDev]` the habit-vs-rule questions.

If you cannot complete this recipe — if the topic refuses to reduce to a few bounded questions — that is a signal the decision is not ripe for a focus group. Go back and narrow it first.

---

<a id="debate"></a>
## 5. Running the Debate

With the framing set, the debate runs as an ordered sequence of **rounds** — one per question — bracketed by sanity checks. The structure is deliberately repetitive, because predictability is what lets a multi-voice argument actually converge instead of wandering.

**The standard arc** (mirroring the worked example):

- **Round 0 — Validation pass.** Before deciding anything, `[Quality]` confirms the facts the debate rests on are real. In the example this meant re-checking the numbers against the live working tree — confirming the cleanup commit was genuinely *"187 files, +1,836 / −2,075"* (net-negative, the signature of a real cleanup, not a feature) and that `String.Empty` appears *"454 times across 48 `.cs` files."* This round exists so nobody builds a decision on a wrong premise. `[Sanity]` closes it crisply: *"Then we don't reopen the findings. We decide what we do."*

- **Rounds 1…N — one question each.** Each round takes a single framed question and walks it to a decision:
  1. The owning advocate states a position (`[Backend]`: *"don't let [the two docs] drift as two sources of truth"*).
  2. Other voices test it, often qualifying *how* rather than *whether* (`[Quality]`: *"Agreed in principle, but be careful how"* — pushing for a *surgical* update rather than a risky rewrite).
  3. `[JrDev]` probes the assumption (*"if I write code in Visual Studio today and hit 'fix all,' it'll 'correct' my `String.Empty` to `string.Empty`…?"*).
  4. `[Sanity]` guards the scope, sometimes splitting the question to make it answerable (*"the question answers itself in two parts. Part one: which do we write?… Part two: do we leave the editorconfig fighting us?"*).
  5. `[Architect]` resolves, applying the tiebreaker when needed.
  6. The round **ends with a written `Decision`** stating the conclusion *and naming an owner*. For example: *"Decision Q2: Follow the author's hand — `String.Empty` / capital-S `String.` statics. Do not edit [the] `.editorconfig` and do not run blanket `dotnet format`… Document the divergence prominently. (Owners: [Backend] writes the rule, [Quality] writes the warning)."*

- **Convergence over consensus.** Notice that decisions do not require everyone to *love* the outcome — only to accept it. `[Sanity]` repeatedly grants conditional approval (*"I'll allow it if the edits are small and cite [the source]"*), which is convergence, not unanimity. The tiebreaker exists precisely so a round can close even when a voice still has reservations.

**The cadence rule:** every round produces exactly one written decision with an owner before the group moves on. If a round cannot produce a decision, that is itself the finding — escalate the question (Section 7) rather than looping. A focus group that runs five rounds and emerges with five owned decisions has done its job; one that runs five rounds and emerges with "more discussion needed" has not.

---

<a id="sanity-checks"></a>
## 6. Sanity Checks

What separates a focus group from a long meeting is the **sanity check** — a deliberate skeptical pass, run by `[Sanity]`, whose only purpose is to attack the group's own emerging conclusion before it gets locked in. Because the group has been building momentum toward a decision, it is exactly the moment they are *least* likely to spot a problem on their own. The sanity check is the built-in counterweight. It is important enough to have its own doc, [064](064_sanity-check.md); here is how it functions *inside* a focus group.

There are two of them, at two different moments:

**The mid-check — "are we overcomplicating this?"** Run partway through, after the bulk of the decisions are roughed in, the mid-check asks whether the group is over-engineering. The most useful version of this check *names what the group chose NOT to do*, because that is where scope creep hides. In the worked example:

> *"Pause. Are we overcomplicating this?… Notice what we didn't decide: we did not approve a mass reformat of our own code, we did not add a CI gate, we did not rewrite [the guide], we did not touch [the] editorconfig. Every decision was 'document the truth, follow the author, make a cheap habit.' That's the right altitude."*

A strong mid-check also surfaces the *one* place the decision could still balloon and forces a ruling on it. Here it was: *"are we sure we shouldn't reformat our own existing… code to match?"* — answered with a firm "no, that's a huge diff and review burden; new-and-touched code follows the rules, leave working code alone."

**The final-check — "what did we miss?"** Run just before writing it up, the final-check hunts for gaps: open threads, unresolved sub-points, things raised early and never closed. In the example, `[Sanity]` listed three loose ends and made the group resolve each on the spot — an unresolved UI detail (decided as *"optional, match surrounding UI,"* not a hard rule), a caveat about non-authoritative community examples, and the boringly practical *"who actually does the edits and when"* (answered: this sprint, because the cost of *not* doing it is people writing the wrong thing next week). Only after all three were closed did the skeptic sign off: *"Then I'm satisfied. Nothing structural missed."*

**Why two checks, not one:** the mid-check guards against doing *too much* (over-engineering); the final-check guards against doing *too little* (gaps). They fail in opposite directions, so you need both. A focus group that skips the sanity checks is just a meeting with extra steps — the checks are the part that makes the conclusion trustworthy.

---

<a id="write-up"></a>
## 7. Writing It Up

A focus group that does not produce a written artifact has wasted everyone's time — the conclusion lives only in memory and decays within days. The final step is therefore to capture the outcome in a **decision record** (and, when one question needs the CTO, a one-page brief). The format is covered fully in [067](067_decisions-and-briefs.md); here is what the write-up must contain.

**1. The decisions, numbered, each as a standalone statement.** One line (or short paragraph) per decision, written so it makes sense to someone who was not in the room. From the example:

> *"2. Follow the author's hand: `String.Empty` / capital-S `String.` statics. Do not edit [the] `.editorconfig`; do not run blanket `dotnet format` (it would flip 454 occurrences against the convention and collide with his next merge). Document the divergence as a foot-gun warning."*

Notice each decision records not just *what* but *why* (the merge-collision reasoning) — that rationale is what stops the decision from being silently re-opened later.

**2. The dissent and the tie that is not yours to break.** A focus group is allowed to *not* settle one thing — but it must say so explicitly and hand it up rather than fudge it. The worked example flagged exactly one item for the CTO:

> *"Flagged for CTO (tie not ours to break): whether to ever realign [the] `.editorconfig`… Team leans no (don't touch his file), but it's an upstream-relationship call the CTO should ratify."*

Recording the team's *lean* alongside the open tie is what makes the brief actionable: the CTO gets a recommendation to ratify, not a blank question.

**3. The owners and timing — a next-steps table.** Every decision needs a name attached or it will not happen. The write-up ends with an action table: *Action · Owner · Priority*. This is also where "this sprint vs. backlog" gets pinned down, so a decision does not evaporate into "someday."

**4. The trail.** Link the chain of documents so the reasoning is reconstructable: the focus group feeds a CTO brief, and both cite the source research. In the example the trail ran pre-brief → team intro → **focus group** → CTO brief, with the research docs cited throughout.

A useful discipline: **the scribe writes the record as the rounds close, not from memory afterward.** Each `Decision Qn:` line is captured the moment its round ends, so the final write-up is mostly assembly, not reconstruction.

---

<a id="pitfalls"></a>
## 8. Common Pitfalls

Focus groups fail in recognizable ways. Each of these has a direct countermeasure, and most are the reason a particular role exists.

- **Re-litigating settled facts.** The group keeps reopening the research instead of deciding what to *do* about it. *Countermeasure:* state the premise as given in framing (Section 4) and let `[Sanity]` enforce it — *"we don't reopen the findings. We decide what we do."*

- **Scope creep into "rewrite everything."** A bounded decision metastasizes into a giant migration. *Countermeasure:* the mid-check, plus the skeptic's veto. In the example this is what stopped a tempting-but-wrong mass reformat of the team's own code; the rule landed as *"new and touched code follows the rules; leave working code alone."*

- **The destructive default.** The "obvious" action is the dangerous one. *Countermeasure:* surface the foot-gun explicitly. Here, the naive move (run `dotnet format`) would have flipped 454 occurrences against the convention — so the decision was the *passive* one: don't run it. A passive "don't do the destructive thing" often beats an active gate.

- **Mistaking habit for law.** Treating every observed pattern as an intentional rule that must be defended. *Countermeasure:* this is `[JrDev]`'s entire job. In the example, a leftover artifact (`AllowLoginTypeOpenId{` with a missing space — proof the author hand-edited and never ran the formatter) showed some "rules" were just muscle memory. The reframe — *"match the hand because the hand is what we merge against"* — dissolved the whole habit-vs-rule worry.

- **Building on sand.** Debating numbers nobody verified. *Countermeasure:* the Round 0 validation pass, where `[Quality]` re-checks the facts against the live source before anyone decides.

- **Consensus theater.** Waiting for everyone to enthusiastically agree, which never happens, so nothing closes. *Countermeasure:* aim for *convergence* (acceptance) not unanimity, and lean on the pre-agreed tiebreaker to close deadlocks.

- **No written record / no owners.** The discussion was great and then evaporated. *Countermeasure:* every round ends in a written, *owned* decision, and the session ends in a next-steps table (Section 7). A decision with no name attached is a decision that will not happen.

- **Over-using the format.** Convening the full team for a question that one role could have answered. *Countermeasure:* the gate in Section 2 — if it is cheap to reverse, uncontroversial, or single-owner, use a lighter format ([062](062_discussion-planning-modes.md), [065](065_ask-one-role.md)) instead.

The through-line: most pitfalls are a *missing role doing its job*. Run the full cast, honor the sanity checks, and write it down with owners, and the format defends itself.

---

<a id="related-docs"></a>
## 9. Related Docs

- [061 — The Roleplay Team and Its Roles](061_roleplay-team.md) — the team that runs it
- [064 — The Sanity Check](064_sanity-check.md) — sanity checks inside it
- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — it produces a decision record

---
*GuidesV2 063 · Running a Focus Group · drafted 2026-06-04 from source (the real focus-group transcript `Guides/0005_meeting.team_focus_group.md`, its framing doc `Guides/0004`, and the resulting CTO brief `Guides/0006`).*
