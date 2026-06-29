# 0006 — Brief: CTO Decision Brief (FreeCRM Styling Alignment)

> **Document ID:** 0006
> **Category:** Decision
> **Purpose:** Hand the CTO a clean, actionable summary of the styling research and the team's recommendation — with the one decision that needs the CTO.
> **Audience:** [CTO]
> **Predicted Outcome:** CTO ratifies the team's recommendation and rules on the one open tie.
> **Actual Outcome:** _Awaiting CTO sign-off._
> **Resolution:** _Pending — see "The One Decision That Needs You."_

---

## TL;DR

For years our FreeCRM style guidance was **inferred** from reading the author's code. Two weeks ago the original author (**Brad Wickett / `wicketbr`**, our `WSU-EIT/FreeCRM` upstream) **explicitly unified his own style** — commit `d094f99`, 187 files, *"for consistency with my programming style."* We now have ground truth. It's documented in **[0001_freecrm_styling_latest_research.md](0001_freecrm_styling_latest_research.md)** (authoritative) against the old inferred baseline **[0000_research_styling.md](0000_research_styling.md)**.

The team focus-grouped *what to do about it* and converged fast. **Recommendation: adopt the author's explicit style, update our day-to-day guide surgically, enforce via docs + review (not CI), and add a cheap upstream-sync habit. Do almost nothing destructive.** One call is yours to make (below).

---

## What We Did

| Step | Outcome |
|------|---------|
| Cloned all 4 public `wsu-eit` repos into `repo2` | `FreeAI`, `FreeCRM`, `FreeCICD`, `FreeQEMU` |
| Read all 26 guide docs in full | Source for the "inferred" baseline |
| Forensic git deep-dive of the author's ~2-week cleanup | `d094f99` + 5 supporting commits, verified against the working tree |
| Wrote `0000` (inferred) + `0001` (authoritative) | The research of record |
| Ran the roleplay process | Pre-brief (0003) → team intro (0004) → focus group (0005) → this brief (0006) |

**Verified facts (not estimates):** `d094f99` = **187 files, +1,836 / −2,075** (net-negative = real cleanup). `String.Empty` now appears **454× across 48 `.cs` files**. Brace rules and the editorconfig divergence confirmed by direct diff inspection.

---

## What Changed In His Style (the findings, condensed)

| Dimension | The author's explicit rule |
|-----------|----------------------------|
| **Braces** | Methods/types → new line. **Properties → same line.** Control-flow → same line. |
| **Wrapped params** | `(` on its own line, one param per line, `){` collapsed. |
| **Empty string** | **`String.Empty`** (capital-S `String.` statics) — everywhere. |
| **Underscores** | DI service fields + `CurrentUser`/`TenantId` → **no** `_`; all other private → `_`. |
| **Param casing** | Domain method params **PascalCase**; DI ctor params camelCase. |
| **Ordering** | Private fields **alphabetical**; DTOs grouped ids → data → audit. |
| **`.App.` partials** | Everything custom lives in paired `*.App.*` files — this is what lets us take his updates cleanly. |
| **Hygiene** | Strip unused usings, dead code, redundant `Helpers.` self-qualifiers; one-blank-line discipline; `Try*` over try/catch. |

**The headline nuance:** his `String.Empty` habit **contradicts his own `.editorconfig`**, which would suggest lowercase `string.Empty`. His hand wins (FreeCRM-main is our #1 authoritative source) — but his tooling is a latent foot-gun.

---

## The Team's Recommendation (5 decisions, already made)

1. **Three-layer docs.** Keep `0000` (historical/inferred) and `0001` (authoritative). **Surgically update `004_styleguide.md`** — six specific edits + a banner pointing to `0001`. *No full rewrite.*
2. **Follow `String.Empty` / capital-S `String.` statics.** **Do not** edit Brad's `.editorconfig`. **Do not** run blanket `dotnet format` (it would flip 454 occurrences against the convention and collide with his next merge). Document the divergence loudly.
3. **No automated style gate.** The distinctive rules aren't formatter-expressible (and a `dotnet format` gate would *fail on his own code*). Use the existing editorconfig for in-IDE mechanics + docs + code review.
4. **Lightweight upstream-sync checklist**, triggered on each `wicketbr:main` merge (grep his commits for style keywords; if he did another cleanup pass, refresh `0001` + the `004` deltas). ~10 min most merges.
5. **No mass reformat of our own code.** New-and-touched code follows the rules; leave working code alone — same philosophy `004` already uses for legacy `var`.

**Why this shape:** the goal isn't to be philosophically "right" about style — it's to **minimize merge friction with the upstream we don't control.** Matching his hand makes his merges land clean; the destructive options (mass reformat, CI gate, editing his config) all *create* the drift we're trying to kill.

---

## The One Decision That Needs You

> **Should we ever realign Brad's `.editorconfig`** so the tooling stops suggesting `string.Empty` against the `String.Empty` convention?
>
> - **Team's lean: NO** — don't touch his file; it rides in on merges and editing it creates drift. Document the divergence and tell people not to auto-fix.
> - **The case for YES:** a one-line change (`dotnet_style_predefined_type_for_member_access = false`) would stop the IDE fighting the convention for *our* developers.
>
> This is an upstream-relationship call, which is why it's yours. **Recommended: ratify NO.**

Everything else in the recommendation is within the team's authority and is ready to execute on your nod.

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Someone runs `dotnet format` and detonates a 454-line diff | Explicit "do not `dotnet format`" note in contributing docs (Decision 3) |
| `004` and `0001` drift back into two truths | `004` banner cites `0001` as authoritative; sync checklist keeps both current (Decisions 1, 4) |
| Next upstream cleanup silently reintroduces drift | Merge-triggered sync checklist (Decision 4) |
| Scope creep into a repo-wide reformat | Explicitly rejected; new-and-touched-only rule (Decision 5) |

---

## Effort

| Work | Size | When |
|------|------|------|
| `004` edits (6 + banner) | ~2 dozen lines | This sprint |
| `String.Empty` rule + divergence warning | small | This sprint |
| "No `dotnet format`" contributing note | tiny | This sprint |
| Upstream-sync checklist doc | 1 page | Next sprint |
| `table-dark` → "optional" note | tiny | Next sprint |

Total: roughly a half-day of doc work. No code migration.

---

## Trail

`0003` (CTO pre-brief) → `0004` (team intro) → `0005` (focus group, full decisions + transcript) → **`0006`** (this brief). Source research: `0000`, `0001`.

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
*Status: Awaiting CTO sign-off on "The One Decision That Needs You."*
