# 0005 — Meeting: Focus Group — Operationalizing the FreeCRM Styling Findings

> **Document ID:** 0005
> **Category:** Meeting
> **Purpose:** Full-team roleplay discussion to turn the styling research (0000/0001) into decisions and next steps.
> **Attendees:** [Architect] *(Team Lead)*, [Backend], [Frontend], [Quality], [Sanity], [JrDev]
> **Date:** 2026-06-04
> **Predicted Outcome:** Clear answers to the four questions + an action list that feeds the CTO brief.
> **Actual Outcome:** ✅ Five decisions reached; one item flagged for the CTO to break the tie.
> **Resolution:** Decisions below; CTO brief written as doc 0006.

---

## What We're Validating / Deciding

Take the findings in [0001_freecrm_styling_latest_research.md](0001_freecrm_styling_latest_research.md) as solid. Decide: (Q1) update `004` vs keep 0000/0001 standalone, (Q2) `String.Empty` vs editorconfig, (Q3) enforcement, (Q4) staying in sync. Default tiebreaker: author's hand > tooling.

---

## Discussion

### Round 0 — Quality's validation pass

**[Quality]:** Fast pass first, so we're not building on sand. I re-ran the numbers against the working tree. `d094f99` is genuinely **187 files, +1,836 / −2,075** — net-negative, which is the signature of a real cleanup, not a feature. `String.Empty` shows up **454 times across 48 `.cs` files**, and the old `= ""` empty defaults are essentially gone. The editorconfig has `csharp_new_line_before_open_brace = types,methods` for `.cs` and `none` for `.razor`, exactly as 0001 says. I spot-checked the property-brace and `String.Empty` diffs in `DataModel.cs` myself. The research holds. I'm comfortable building on it.

**[Sanity]:** Good. Then we don't reopen the findings. We decide what we do.

**[JrDev]:** One thing I want to flag before we move on, because it's going to color everything: 0001 keeps saying these are Brad's *explicit* choices. But some of them are clearly **habit**, not law — there's literally a leftover artifact, `AllowLoginTypeOpenId{` with no space, that proves he hand-edited and the formatter wasn't even run. So how confident are we that, say, "properties brace on the same line" is a *rule* he'd defend versus just how his fingers move?

**[Architect]:** Fair, and that's the right question to hold the whole time. My answer: it doesn't matter whether he'd philosophically defend it. He's the upstream. Our job isn't to be *right* about style in the abstract — it's to **not create merge friction with his repo.** If our code matches his fingers, his merges land clean. So "habit" and "rule" collapse into the same practical instruction: match it.

**[JrDev]:** That actually clears it up. Match the hand because the hand is what we merge against.

### Round 1 — Q1: Update `004_styleguide.md`, or keep 0000/0001 standalone?

**[Backend]:** My vote: don't let `004` and `0001` drift as two sources of truth. That's how you get the exact problem we just escaped — people reading the old inferred doc and writing `""` because `004` never said otherwise. One authoritative doc.

**[Quality]:** Agreed in principle, but be careful *how*. `004` is big and load-bearing and people have it bookmarked. I don't want to silently rewrite it and have it disagree with git history. I'd rather: **keep 0001 as the cited source of record, and surgically update `004`** to (a) fix the spots where it was wrong or silent and (b) add a banner pointing to 0001 as authoritative.

**[Backend]:** Surgical, not a rewrite. The specific edits to `004`:
- Add the **property brace = same line** rule (it only covered types/methods/control-flow).
- Promote **`String.Empty`** from "shown in examples" to an explicit mandate.
- Add the **wrapped-param `( … ){`** convention.
- Add the **alphabetical private fields** and **DTO grouping** ordering rules.
- Note the **`Helpers.` self-qualifier removal** and dead-code/using hygiene.

**[Architect]:** So the model is: **0000 = the inferred baseline (historical), 0001 = the authoritative findings, 004 = the practical day-to-day guide, updated to agree with 0001 and pointing at it.** Three layers, no contradictions. Everyone good?

**[Sanity]:** I'll allow it *if* the `004` edits are small and cite 0001 inline. The moment it becomes "rewrite 004 from scratch," I'm pulling the cord — that's a week of work for marginal gain over a banner + six edits.

**[Quality]:** Small and cited. I'll own the diff and keep it under, say, a couple dozen lines of change.

**Decision Q1:** Keep 0000 (historical) and 0001 (authoritative). **Surgically update `004_styleguide.md`** with the six specific corrections and a banner pointing to 0001. No full rewrite. *(Owner: [Quality])*

### Round 2 — Q2: `String.Empty` vs the editorconfig

**[Backend]:** This is the interesting one. Brad uses `String.Empty` 450-plus times, by hand, but his own `.editorconfig` sets `dotnet_style_predefined_type_for_member_access = true`, which means the IDE will *suggest the opposite* — `string.Empty`, lowercase keyword. His tooling actively disagrees with his code.

**[JrDev]:** So if I write FreeCRM code in Visual Studio today and hit "fix all," it'll "correct" my `String.Empty` to `string.Empty` and now I'm fighting both the config and the convention at once?

**[Backend]:** Worse — if anyone ever runs `dotnet format`, it'll flip all 454 of them and produce a giant diff that conflicts with Brad's next merge. That's the trap.

**[Sanity]:** Then the question answers itself in two parts. **Part one:** which do we write? The author's hand wins — that's the CTO's tiebreaker and it's the merge-friction argument. Write `String.Empty`. **Part two:** do we leave the editorconfig fighting us? No. That's a booby trap sitting in the repo. Either flip that one setting so the tooling stops suggesting the wrong thing, or — safer — don't touch his editorconfig at all and just **never run blanket `dotnet format`.**

**[Architect]:** I lean toward *not* editing his `.editorconfig`. It's his file; it rides in on merges; if we change it we create the exact drift we're trying to avoid. Better to document "the editorconfig and the hand-style diverge here; follow `String.Empty`; do not run `dotnet format` to 'fix' it" and move on.

**[Quality]:** I want that written down loudly, because it's a foot-gun for the next person. It goes in the `004` banner: *"Known divergence — `.editorconfig` would prefer `string.Empty`; the convention is `String.Empty`. Do not auto-fix."*

**[Backend]:** One nuance — this isn't only `String.Empty`. It's the whole capital-S BCL family: `String.IsNullOrWhiteSpace`, `String.IsNullOrEmpty`. Same rule, same divergence. Document it as "capital-S `String.` statics," not just `.Empty`.

**Decision Q2:** Follow the **author's hand — `String.Empty` / capital-S `String.` statics.** **Do not edit Brad's `.editorconfig`** and **do not run blanket `dotnet format`** (it would flip 454 occurrences against the convention). Document the divergence prominently. *(Owners: [Backend] writes the rule, [Quality] writes the warning)*

### Round 3 — Q3: Enforcement — automate or not?

**[Quality]:** The dream is a CI gate that just enforces the style. The reality is most of Brad's distinctive rules are things the formatter **can't express or actively gets wrong**: the `String.Empty` divergence we just covered, the `( … ){` wrapped-param collapse (no formatter produces that), the alphabetical-fields ordering, the property-brace nuance. A `dotnet format --verify-no-changes` gate would *fail on his own committed code.*

**[Sanity]:** Which is the tell. If the standard enforcement tool fails on the reference implementation, the tool is wrong, not the code. So: **no blanket format gate.** That's settled.

**[Backend]:** There's a narrower, safe version though. The `.editorconfig` mechanics it *does* agree with — indentation, `crlf`, no-final-newline, control-flow spacing, brace-new-line-for-methods — those are genuinely enforceable and Brad's code passes them. We could run a check scoped to *only* those rules. But honestly the value is low and the maintenance is real.

**[Architect]:** I'd rather spend the enforcement budget on **review, not CI.** Concretely: the `004` quick-reference card and the 0001 findings become the checklist reviewers actually use. Cheap, catches the human-judgment stuff (param casing, underscore rules, `.App.` placement) that no analyzer catches anyway.

**[JrDev]:** Could we at least give people something that *helps* rather than *gates*? Like, the editorconfig already does the live IDE hints for the mechanical stuff. The hand-conventions just need to be... known.

**[Quality]:** That's the right framing — **enable, don't gate.** The editorconfig already enables the mechanical 80% in-IDE. The remaining 20% is documentation + review. No new CI.

**[Sanity]:** Final word on this one: the single highest-leverage "enforcement" is **don't run `dotnet format` on this repo.** Put that in the README/contributing notes. A passive "don't do the destructive thing" beats an active gate here.

**Decision Q3:** **No automated style gate.** Rely on the existing `.editorconfig` for in-IDE mechanics + documentation (0001 / updated `004`) + code review for the hand-conventions. Add an explicit **"do not run blanket `dotnet format`"** note. *(Owner: [Architect] adds the contributing note)*

### Round 4 — Q4: Staying in sync with upstream

**[Frontend]:** This is the one that bites us long-term. Brad just did a 187-file pass. He'll do another. If we don't have a habit, our next `wicketbr:main` merge silently reintroduces drift and nobody notices until a giant conflict.

**[Architect]:** So make it a ritual tied to the merge, not a calendar reminder. On each `wicketbr:main` merge: skim his commit messages for style/cleanup language ("consistency," "cleanup," "formatting," "style") and, if he did another pass, re-run the lightweight version of this research — diff the new commits, update 0001's findings table, propagate any new rule into `004`.

**[Quality]:** I can make that mechanical. A one-page "**upstream sync checklist**" doc: (1) `git log wicketbr:main --grep` for the style keywords since our last merge, (2) eyeball `--stat` for any 50-plus-file cleanup commits, (3) if found, update 0001 + the `004` deltas, (4) note the date. Ten minutes most merges, an hour on the rare big one.

**[Frontend]:** And specifically watch the `.razor` and `.editorconfig` files in those diffs, since that's where the Razor-side conventions and the mechanical baseline live — that's my half of the world.

**[Sanity]:** I like that it's *triggered by the merge we already do*, not a new standing meeting. Lowest-overhead version that actually works.

**Decision Q4:** Add a lightweight **upstream-sync checklist** triggered on each `wicketbr:main` merge — grep his commits for style keywords, check for big cleanup commits, update 0001 + `004` deltas if found. *(Owner: [Quality] writes the checklist; [Frontend] watches the Razor/editorconfig diffs)*

### [Sanity] Mid-Check

**[Sanity]:** Pause. Are we overcomplicating this? ... Actually, no — and I went in expecting to pull the cord. Notice what we *didn't* decide: we did not approve a mass reformat of our own code, we did not add a CI gate, we did not rewrite `004`, we did not touch Brad's editorconfig. Every decision was "document the truth, follow the author, make a cheap habit." That's the right altitude. The only thing I'd double-check: are we sure we shouldn't reformat our *own* existing non-Brad code to match? Because that's the one place this could balloon.

**[Backend]:** Strong no on a mass reformat. Brad's own cleanup was net-*negative* lines and still hand-done over two weeks across 187 files. For us to "catch up" all at once is a huge diff, huge review burden, and it'd collide with his next merge. The convention is **"new and touched code follows the rules; leave working code alone unless you're already in it"** — which, notably, is exactly what `004` already says about not refactoring working `var` code. Apply the same philosophy here.

**[Sanity]:** Good. Boundaries holding. Continue.

### [Sanity] Final Check — Did we miss anything?

**[Sanity]:** Three gaps before we close:

1. **The `table-dark` thing.** 0000 flagged that our Razor templates show `table-dark` on headers but a Brad consistency commit *removes* it in places. We didn't resolve it. [Frontend]?

   **[Frontend]:** Resolve it as "**optional, match surrounding UI**," not "always use it." It's a UI-consistency judgment call, not a hard rule. I'll note it in the `004` Razor section. Low stakes.

2. **The wizard/community-contributed caveat.** 0000 noted some component docs (FreeCICD wizard) are explicitly "not authoritative style." We should make sure nobody mistakes those for Brad's conventions.

   **[Quality]:** Already handled in 0000/0001, but I'll make the `004` banner say "authoritative = FreeCRM-main / Brad; community examples are illustrative only."

3. **Who actually does the `004` edits and when.** We've been saying "[Quality] owns it" — is it this sprint or backlog?

   **[Architect]:** This sprint. It's a couple dozen lines and the cost of *not* doing it is people writing `""` next week. Small, urgent-ish, do it now.

**[Sanity]:** Then I'm satisfied. Nothing structural missed.

---

## Decisions

1. **Three-layer doc model.** `0000` = inferred baseline (historical), `0001` = authoritative findings, `004_styleguide.md` = day-to-day guide, **surgically updated** to agree with 0001 + a banner pointing to it. No full rewrite.
2. **Follow the author's hand: `String.Empty` / capital-S `String.` statics.** Do **not** edit Brad's `.editorconfig`; do **not** run blanket `dotnet format`. Document the divergence as a foot-gun warning.
3. **No automated style gate.** Existing editorconfig (in-IDE) + docs + code review. Add an explicit "don't `dotnet format` this repo" note.
4. **Lightweight upstream-sync checklist**, triggered on each `wicketbr:main` merge.
5. **No mass reformat of our own code.** New-and-touched code follows the rules; leave working code alone (mirrors the existing `var` philosophy).

**Flagged for CTO (tie not ours to break):** whether to ever *realign* Brad's `.editorconfig` (flip the `predefined_type_for_member_access` setting so tooling stops suggesting `string.Empty`). Team leans **no** (don't touch his file), but it's an upstream-relationship call the CTO should ratify.

---

## Next Steps

| Action | Owner | Priority |
|--------|-------|----------|
| Surgically update `004_styleguide.md` (6 edits + banner) | [Quality] | P1 — this sprint |
| Write the explicit `String.Empty` / capital-S rule + divergence warning | [Backend] + [Quality] | P1 |
| Add "do not run blanket `dotnet format`" to contributing notes | [Architect] | P1 |
| Write the upstream-sync checklist doc | [Quality] | P2 |
| Resolve `table-dark` as "optional, match surrounding UI" in `004` | [Frontend] | P2 |
| Write the CTO decision brief | [Architect] | P1 — doc 0006 |

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
