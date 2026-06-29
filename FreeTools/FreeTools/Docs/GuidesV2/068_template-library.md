# 068 — The Documentation Template Library

> **Document ID:** 068  ·  **Category:** Process  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Provide copy-paste skeletons for meetings, decisions, features, runbooks, and reviews.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 06x (The Team Operating System: How We Decide) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Templates Matter](#why-it-matters) | What a template is, and why we standardize the shape of every doc |
| 2 | [When to Use Which Template](#when-to-use) | The "I need to…" selector that maps a situation to the right skeleton |
| 3 | [Meeting & Decision Templates](#meeting-decision) | Design Discussion, Code Review, Bug Investigation, and the Decision Record (ADR) |
| 4 | [Feature & Spec Templates](#feature-spec) | Feature Design, Quick Validation, and the Planning Checklist |
| 5 | [Runbook & Review Templates](#runbook-review) | The operational Runbook and the incident Postmortem |
| 6 | [How to Use & Adapt a Template](#using-templates) | The five rules: copy whole, fill placeholders, update outcome, trim the rest |
| 7 | [Roles & Where Output Lives](#roles-output) | The named voices, who maintains a doc, and the naming/numbering rules |
| 8 | [Contributing New Templates](#contributing) | Proposing, reviewing, and retiring a skeleton without breaking the set |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Templates Matter

**Why it matters:** A blank page is the most expensive thing in a documentation system. Every time someone sits down to write a meeting note, a decision, or a runbook, they re-invent the structure — what goes first, what to call the sections, where the conclusion lives. The result is a pile of documents that all *say* roughly the same kind of thing but *look* nothing alike, so no one can skim them, search them, or trust that the important part is where they expect it. Templates kill that cost.

A **template** here is simply a ready-made skeleton: a complete document with the headings already in place and the variable parts marked as fill-in-the-blanks. In this project the blanks are written in curly braces — `{like this}` — so the spots you must replace are obvious at a glance. You copy the whole thing, swap the braces for real content, delete what doesn't apply, and you are done. No structural decisions required.

There are three concrete payoffs, and they are the reason this band of the guide treats templates as part of the "operating system," not a nice-to-have:

- **Consistency.** Every decision record has the same fields, so a reader who has seen one can read all of them. Every meeting note has a *Predicted Outcome* and an *Actual Outcome* in the same place, so you can scan a folder and see at a glance which discussions actually concluded.
- **Speed.** The author starts from a 90%-finished shape instead of an empty file. The thinking goes into the *content*, not the *container*.
- **Lowered cognitive load.** Because the structure is decided for you, you cannot accidentally forget the "Rollback" section of a runbook or the "Alternatives" line of a decision — the template already asks for them. The skeleton is a checklist in disguise.

This GuidesV2 doc is the canonical, plain-language home for these skeletons: what each template is for, when to reach for it, and how to use one well. (They originated in the project's older guides set — since retired — and the copies reproduced here are the live versions.)

<a id="when-to-use"></a>
## 2. When to Use Which Template

**Why it matters:** Picking the wrong skeleton wastes more time than picking none — you fill in a feature spec when what you needed was a five-minute decision record, or you open a heavyweight meeting doc for a question one person could answer. The library is designed so you never have to guess: it leads with a **selector table** keyed on the sentence *"I need to…"*. You find your intent, and it points you at the template.

Here is that selector, copied faithfully from `003_templates.md`:

| I need to… | Use this template |
|------------|-------------------|
| Start a design discussion | Meeting: Design Discussion |
| Review code with the team | Meeting: Code Review |
| Debug a problem together | Meeting: Bug Investigation |
| Record an architecture decision | Decision Record (ADR) |
| Get quick feedback | Quick Validation |
| Spec out a feature | Feature Design |
| Prepare a PR | Planning Checklist |
| Document a procedure | Runbook |
| Analyze an incident | Postmortem |

Two plain-language clarifications, because a couple of those terms recur throughout the guide:

- **ADR** stands for *Architecture Decision Record* — a short document that captures *why* a technical choice was made, the options you weighed, and the consequences you accepted. It exists so that six months later, when someone asks "why on earth did we do it this way?", the answer is written down instead of lost.
- **PR** stands for *Pull Request* — the unit of proposed code change you submit for review before it merges. The Planning Checklist is the skeleton you paste into a PR (or the issue behind it) to make sure you thought the change through before writing it.

A useful sizing rule sits alongside the selector (it comes from the roleplay guide, `Docs/Guides/001_roleplay.md`): **match the ceremony to the size of the change.** A typo or comment fix needs no template at all — just do it. A small change, like adding one field, is a quick question. A *medium* change (a new endpoint or component) warrants the Planning Checklist. A *large or unclear* change (a new feature or an architecture shift) earns a full discussion and, usually, a decision record. The templates scale down as easily as they scale up; reaching for a lighter one is never wrong when the work is genuinely small.

<a id="meeting-decision"></a>
## 3. Meeting & Decision Templates

**Why it matters:** Most of the team's thinking happens in conversation — designing something, reviewing code, chasing a bug. If that thinking evaporates when the conversation ends, the team relearns the same lessons forever. These four skeletons capture the conversation in a shape you can come back to.

A shared trait first. Meeting and decision docs use a header format with three outcome lines that you fill in over the life of the doc — this is the "before vs. after" honesty that makes the archive trustworthy:

```markdown
> **Predicted Outcome:** {What we expected}
> **Actual Outcome:** {What happened — update when done}
> **Resolution:** {Action taken — PR link, decision, etc.}
```

You write *Predicted Outcome* before the meeting, fill in *Actual Outcome* when it ends, and add *Resolution* (a PR link, a decision, "No action needed") once the dust settles. The outcome line is prefixed with a status emoji from the project's small set — `✅` complete, `⚠️` partial/needs follow-up, `❌` failed/blocked, `📋` informational, `🔄` in progress, `📖` reference. A reader can judge the state of a whole folder by skimming those symbols.

**Meeting: Design Discussion** — *use when exploring how to build something new.* This is the workhorse. After the header it has a **Context** section ("What problem are we solving? Why now?"), a **Discussion** section written as a transcript of named voices, and then the payoff sections: **Decisions**, **Open Questions**, and a **Next Steps** table of `Action / Owner / Priority`. The discussion is where the multi-voice review happens, and the template literally scripts the running order:

```markdown
**[Architect]:** {Frames the problem, constraints, options}
**[Backend]:** {Data and API perspective}
**[Frontend]:** {UI and UX perspective}
**[Quality]:** {Testing and security concerns}
**[Sanity]:** Mid-check — {Are we overcomplicating this?}
{Continue discussion...}
**[Sanity]:** Final check — {Did we miss anything obvious?}
```

Those bracketed names — `[Architect]`, `[Backend]`, `[Frontend]`, `[Quality]`, `[Sanity]` — are the roleplay team's roles (Section 7 lists them all). The two `[Sanity]` checks, one in the middle and one at the end, are deliberate: they force a pause to ask "are we overcomplicating this?" before the decision hardens.

**Meeting: Code Review** — *use when reviewing existing code or a PR.* It records **What We're Reviewing** (the files, the PR link, the context), a short **Discussion** between `[Author]`, `[Reviewer]`, `[Quality]`, and `[Sanity]`, and then graded **Feedback** in four buckets — **Must Fix**, **Should Fix**, **Consider**, **Looks Good** — closing with a one-line **Verdict** (*Approved / Approved with changes / Needs revision*). The four buckets matter: they separate the blocking issues from the nice-to-haves so nobody confuses "consider renaming this" with "this is a security hole."

**Meeting: Bug Investigation** — *use when debugging a problem together.* It opens with **Symptoms** (what happens, what's expected, frequency, environment) and numbered **Repro Steps** ("repro" = reproduce — the exact steps that make the bug appear), then an **Investigation** transcript, then **Findings** with three named fields — *Root cause*, *Contributing factors*, and the quietly important *"Why not caught"* (the gap in testing or monitoring that let it through) — and finally a **Fix** block of *Approach / Risk / Test*.

**Decision Record (ADR)** — *use when making a significant technical decision.* This is the most reused skeleton in the band. Its sections are **Context** (why we had to decide), **Options Considered** (each option with explicit **Pros** and **Cons**), a one-paragraph **Decision** stating which option won and why, and **Consequences** split into *Positive*, *Negative*, and *Neutral*. The Negative and Neutral columns are the point — an honest ADR names the trade-offs it accepted, not just the wins.

For a decision too small to deserve its own file, the library includes an **ADR Mini-Template** you drop inline into a PR or a meeting doc:

```markdown
### ADR: {Title}

**Context:** {Why we needed to decide}
**Decision:** {What we chose}
**Rationale:** {Why this option}
**Consequences:** {What this means}
**Alternatives:** {What we didn't choose}
```

Five lines, and the decision is on the record. The fuller treatment of when to write a record versus when to escalate a one-page brief lives in [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md).

<a id="feature-spec"></a>
## 4. Feature & Spec Templates

**Why it matters:** The cheapest place to fix a feature is *before* it is built. These three skeletons exist to force the thinking up front — what problem, what files, what could break — so you discover the hard parts on paper instead of halfway through coding.

**Feature Design** — *use when speccing out a new feature before building.* "Speccing" is short for writing a *specification*: a description of what you intend to build and how, detailed enough to review before any code exists. The template opens with **Problem** ("what problem are we solving? for whom?") and **Solution** (the high-level approach), then gets concrete in a **Changes** block that asks for the specific files to create or modify, plus **Data/Schema Changes**, **API Changes**, and **UI Changes**. It closes with **Security**, **Testing**, **Rollout**, and **Open Questions**.

One detail in this template is load-bearing for this codebase: the "files to create" line bakes in the house naming law. New files must follow the `{ProjectName}.App.{Feature}` pattern — the `.App.` marker that keeps *your* code separate from the framework's so a future upgrade does not overwrite it. The template spells it out:

```markdown
**New files** (must use `{ProjectName}.App.{Feature}` pattern):
- `FreeManager.App.{Feature}.razor` — {description}
- `FreeManager.App.{Feature}.cs` — {description}
- `FreeManager.App.DataObjects.{Feature}.cs` — {DTOs}
```

`DTO` here means *Data Transfer Object* — a plain data shape used to move information between layers. The deeper rationale for the `.App.` naming convention lives in [042 — The Naming Law That Keeps Your Code Yours](042_file-naming-law.md); the template just makes it impossible to forget.

**Quick Validation** — *use when getting fast feedback without a full meeting.* When you do not need the whole team but want a few perspectives, this lighter skeleton collects them in one table. It states **What We're Validating** in two or three sentences, runs a single **Feedback Round** as a table of personas — *Senior Dev* (technical depth), *New Dev* (newcomer experience), *End User* (user perspective), *Skeptic* (complexity concern) — and ends with a **Summary** bucketed into *Works Well*, *Needs Improvement*, *Quick Wins (< 30 min)*, and *Deferred*. It is the fast lane between "just ask someone" and "convene a design discussion."

**Planning Checklist** — *use when preparing to implement a change*, typically pasted straight into a PR or issue. Unlike the others this one has no document header; it is a body you graft onto an existing PR. Its sections walk you from intent to safety: **Summary** (*Problem / Goal / Non-goals*), checkbox **Acceptance Criteria**, an **Approach** (new files using the `.App.` pattern, modified files, data impact, compatibility notes), a **Test Plan** (*Happy path / Edge cases / Regression*), **Ops & Rollout** (*Config/secrets / Monitoring / Rollback*), and a **Docs** checklist. It even ends with a dedicated **File Naming Checklist** that confirms every new file used the `{ProjectName}.App.{Feature}.{Extension}` pattern and that no short `FM` prefix slipped in. The *Non-goals* line is the unsung hero — writing down what you are deliberately *not* doing is how scope creep gets caught early.

<a id="runbook-review"></a>
## 5. Runbook & Review Templates

**Why it matters:** The first two families capture *thinking*. This pair captures *operations* — the procedures you run and the incidents you survive. Both are written so that a stressed person at an awkward hour can follow them without having to reason from scratch.

**Runbook** — *use when documenting an operational procedure.* A runbook is a step-by-step recipe for a routine but consequential task: deploying, restoring a backup, rotating a secret. Because it is a reference rather than a record of a discussion, it uses the *other* header format — a single **Outcome** line marked `📖` (the reference emoji) instead of the Predicted/Actual/Resolution trio. Its body is built for someone executing under pressure: **When to Use** (the conditions that trigger it), **Prerequisites** (access and tools needed first), numbered **Steps** with copy-paste commands, a **Verification** checklist to confirm it worked, a **Rollback** sequence for when it didn't, and a **Troubleshooting** table of `Symptom / Cause / Fix`. The Rollback and Troubleshooting sections are what separate a real runbook from a wiki note — they answer "what do I do when it goes wrong," which is exactly the moment you reach for the runbook.

```markdown
## Rollback

If something goes wrong:

1. {Rollback step 1}
2. {Rollback step 2}

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| {Problem} | {Why} | {Solution} |
```

**Postmortem** — *use when analyzing an incident after it's resolved.* A "postmortem" (literally "after death") is the blameless write-up of what broke, why, and what you will change so it does not happen again. It is a meeting-style doc (Predicted/Actual/Resolution header) and its structure is deliberately calm and factual: a **Summary** (*what happened / duration / impact / severity*), a minute-by-minute **Timeline** table, the **Root Cause** and **Contributing Factors**, then two balanced lists — **What Went Well** and **What Went Wrong** — an **Action Items** table (`Action / Owner / Status / Due`), and **Lessons Learned**. The "What Went Well" half is intentional: a good postmortem records what saved you, not only what failed you, so the team keeps doing the things that worked. The point of the whole exercise is the Action Items table — analysis that does not produce owned, dated follow-ups is just storytelling.

<a id="using-templates"></a>
## 6. How to Use & Adapt a Template

**Why it matters:** A template only pays off if it is used the way it was designed. The library closes with five short rules, and following them is the difference between a clean, skimmable archive and a folder of half-filled forms. Here they are, in the order the source gives them, with the reasoning behind each:

1. **Copy the whole template — don't skip sections.** Start from the complete skeleton, not a remembered subset. The sections you are tempted to skip (Rollback, Alternatives, Why-not-caught) are usually the ones that matter most under pressure.
2. **Fill in placeholders — look for the `{curly braces}`.** Every blank you must replace is marked. When a doc still has braces in it, it is not finished — that is the at-a-glance "not done" signal.
3. **Update *Actual Outcome* when work completes.** The before/after honesty is the entire value of the meeting-doc header. A *Predicted Outcome* with no *Actual Outcome* is an open loop someone has to chase.
4. **Add *Resolution* — link to PRs and follow-up docs.** This is the thread that ties a discussion to the code or decision it produced, so the archive is navigable instead of orphaned.
5. **Delete unused sections — after filling in, remove what's N/A.** Trim *last*, not first. Fill everything you can, then cut what genuinely does not apply, so trimming is a decision rather than an omission.

Two adaptation notes that keep you from over-thinking it:

- **Adapt the roles to the work.** The default voices (`[Architect]`, `[Backend]`, `[Frontend]`, `[Quality]`, `[Sanity]`) suit a web app, but the roleplay guide explicitly invites swapping them — an API-only effort might use `[API]`, `[Database]`, `[Consumer]`, `[Ops]`; a library might use `[PublicAPI]`, `[Internals]`, `[Perf]`, `[Docs]`. Use the voices that actually have something to say about *your* change.
- **Mind the length budget.** The docs standard targets **≤300 lines** per doc as ideal, treats **500** as a soft maximum ("consider splitting"), and **600** as a hard maximum ("must split or justify"). If filling in a template blows past that, the signal is usually that you are documenting two things in one file — split it.

<a id="roles-output"></a>
## 7. Roles & Where Output Lives

**Why it matters:** A template tells you *what* to write; this section answers *who owns it* and *where it ships* — the two questions that decide whether a doc gets maintained or rots. Get these wrong and even a perfect template produces an orphan.

**The named roles.** Across the meeting and decision templates you will see the same bracketed voices. They are the roleplay team, and each has a single concern (the full treatment is in [061 — The Roleplay Team and Its Roles](061_roleplay-team.md)):

| Role | Focus | The question it always asks |
|------|-------|------------------------------|
| **[Architect]** | System design, patterns, boundaries | "How does this fit? What's the blast radius?" |
| **[Backend]** | Data, APIs, services, storage | "What's the schema? What endpoints?" |
| **[Frontend]** | UI, components, UX, state | "What's the user flow? Loading states?" |
| **[Quality]** | Tests, security, docs | "How do we test this? What could break?" |
| **[Sanity]** | Reality checks, complexity | "Are we overcomplicating this?" |
| **[JrDev]** | Clarifying questions | "Wait, why are we doing X?" |
| **[CTO]** | **You, the human** | Final decisions |

Note the last row: **[CTO] is the human reader** — the person with authority who makes the final call. That is the same intern-CTO this whole guide set is written for, and it is why the templates spell decisions out plainly rather than burying them in jargon.

**Who maintains a doc.** Every meeting, decision, validation, and postmortem template ends with the same two footer lines:

```markdown
*Created: {YYYY-MM-DD}*
*Maintained by: [Quality]*
```

By convention **[Quality]** is the default maintainer of process docs — the role that keeps the archive honest, links resolutions, and archives the obsolete. Feature designs and runbooks may name a different owner (`[Role]` or `[Ops]`), but the principle holds: every doc has a named owner, never "the team" in the abstract.

**Where output lives.** The docs guide sets the filing rules so output is discoverable, not scattered:

- **Filename:** `{NUM}_{CATEGORY}_{TOPIC}.md` — a three-digit number, the doc type as the category (`meeting`, `decision`, `feature`, `runbook`, `postmortem`, `reference`…), and the topic with underscores for spaces.
- **Numbering:** each new doc takes the next available number; **gaps are fine and you never renumber** an existing doc, because other docs link to it by number.
- **Lifecycle:** docs are *versioned with code* — they live in Git next to what they describe, and the rule is *"docs as part of done"*: a PR that changes behavior updates the docs in the same change. Obsolete docs are **moved to an `archive/` folder, keeping their number — never deleted**, so cross-references don't break.

The real-world proof that this works is sitting in the `Docs/GuidesV2/` folder: a single decision ran through the exact chain — pre-meeting briefing (`0003`), team introduction (`0004`), the full focus-group transcript (`0005`), and the one-page CTO brief (`0006`) — each file stamped with the Predicted/Actual/Resolution header these templates prescribe. The library is not theoretical; it is how this project's own decisions are recorded.

<a id="contributing"></a>
## 8. Contributing New Templates

**Why it matters:** A template library is only as good as its restraint. Every new skeleton is a new thing everyone must learn and keep in sync; a sprawling library is as useless as no library, because nobody can find the right one. So the bar for adding a template is high, and the process for changing one is deliberate.

**Before proposing a new template, prove the gap.** The existing nine cover an enormous surface — meetings (design, review, debug), decisions (full ADR and inline mini), features (design, validation, planning), and operations (runbook, postmortem). Most "new" needs are really an existing template with a few sections trimmed (rule 5 from Section 6). Reach for a genuinely new skeleton only when a recurring document type does not map onto any of these — and "recurring" is the operative word: a one-off shape does not earn a permanent template.

**Propose it the way we decide anything.** A new or changed template *is* a decision about how the team works, so it goes through the team's own machinery — discuss it with the relevant roles, and if the change is significant, capture the reasoning in a decision record (the very ADR template from Section 3). The proposal should answer three things: what recurring need it serves, which existing template was insufficient and why, and what its sections are.

**Keep a new template consistent with the set.** To slot in cleanly it must: use one of the two house header formats (the `📖` single-**Outcome** form for reference/runbook docs, or the **Predicted / Actual / Resolution** form for anything that records a discussion or outcome); mark its blanks with `{curly braces}`; end with the standard `*Created:*` / `*Maintained by:*` footer; and respect the same length budget (≤300 ideal, 500 soft cap, 600 hard cap). A template that breaks these conventions trains people to break them too.

**Where it goes, and how it's retired.** New templates are added to the canonical library file (`Docs/Guides/003_templates.md`) and announced in its selector table so they are discoverable from the "I need to…" lane — a template no one can find is no template at all. Retiring one follows the same archive discipline as any doc: don't silently delete it (something may still link to it); mark it superseded and move it aside, keeping its number, so existing references resolve. The whole point of the library is that the *shape* of our documents stays stable enough to trust — so change it slowly, on purpose, and with the reasoning written down.

---

<a id="related-docs"></a>
## 9. Related Docs

- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — decision records use these
- [062 — Discussion Mode, Planning Mode](062_discussion-planning-modes.md) — planning checklists live here

---
*GuidesV2 · 068 · drafted from the in-repo `Docs/GuidesV2/0003`–`0006` roleplay artifacts and the `061`–`067` team-process docs (the original template set was part of the project's now-retired older guides folder).*
