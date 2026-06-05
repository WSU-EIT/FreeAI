# 054 — Living on a Fork: Staying in Sync Upstream

> **Document ID:** 054  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Teach the discipline of staying synchronized with the upstream fork without drift or lost local work.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | What a fork, upstream, and drift are — and why falling behind hurts |
| 2 | [The Sync Loop Mental Model](#sync-loop) | The three repos (upstream, fork, local) and how a change travels between them |
| 3 | [Routine Sync Procedure](#routine-sync) | The weekly "merge wicketbr:main into main" loop, step by step |
| 4 | [Resolving Drift and Conflicts](#resolving-drift) | Spotting divergence and resolving merge conflicts safely |
| 5 | [Protecting Local Work](#protecting-local-work) | Branches, stashes, and the `.App.` convention that keep your code safe |
| 6 | [Pitfalls and Sync Checklist](#pitfalls-checklist) | The mistakes that lose work, plus a copy-paste pre-sync checklist |
| 7 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

**Why it matters first:** FreeCRM is not software you bought and froze — it is software that *keeps improving under you*. The original author ships fixes, accessibility upgrades, and cleanups almost every day. If you never pull those in, your copy slowly becomes an old, unsupported snapshot: bugs that were fixed upstream stay broken in your build, and when you finally try to catch up, months of changes arrive all at once and collide with your edits. Staying in sync is the cheap, boring habit that prevents an expensive, scary catch-up later.

A few terms, in plain language, that the rest of this doc leans on:

- **Repository (repo).** A folder of code whose full history is tracked by Git. Think of it as the project plus a time machine.
- **Upstream.** The *original* project you are building on top of. Here, upstream is **`wicketbr/FreeCRM`** — Brad Wickett's public base template (the thing everyone forks). When Brad fixes something, it lands upstream first.
- **Fork.** Your own copy of the upstream repo that you are allowed to change freely. Our fork is **`WSU-EIT/FreeCRM`** on GitHub. A fork remembers where it came from, which is what makes pulling future upstream changes possible.
- **Local clone.** The copy of the fork sitting on your machine (here at `c:\Users\pepkad\source\repo2\FreeCRM`). This is where you actually edit, build, and run.
- **Drift.** The gap that opens up when upstream moves forward and your fork does not. Small drift is harmless and easy to close. Large drift is where work gets lost and where merges turn into all-day conflict-resolution marathons.

The whole point of this doc is to keep drift small. The good news: the team already does this well — there is a clean, weekly rhythm of upstream merges (see §3). This doc explains that rhythm so you can join it without breaking anything.

> **Reality check from the actual repo.** The fork's only Git remote is `origin → https://github.com/WSU-EIT/FreeCRM.git`. There is **no** separate `upstream`/`wicketbr` remote wired into local config. Instead, the upstream's `main` branch is merged in directly, and the commit it produces is literally titled `Merge branch 'wicketbr:main' into main`. That naming is the fingerprint of GitHub's "Sync fork" flow, and you'll see it four times in a row on a one-week cadence.

---

<a id="sync-loop"></a>
## 2. The Sync Loop Mental Model

**Why it matters:** if you can picture *where a change lives at each moment*, you'll never be confused about which direction to push or pull. Almost every sync mistake is really a direction mistake.

There are three copies of the project, and a change flows through them like water downhill:

```
  wicketbr/FreeCRM            WSU-EIT/FreeCRM             your laptop
   (UPSTREAM, GitHub)   ⇒      (FORK, GitHub)      ⇄    (LOCAL clone)
   Brad's fixes land here   we merge them here, then    you fetch, edit,
                            our edits live alongside     and push back up
```

Two distinct flows ride this picture:

1. **Sync flow (downhill, the subject of this doc):** upstream → fork → local. Brad's improvements travel *down* into our world. You bring them into the fork's `main` by merging `wicketbr:main`, then everyone pulls that into their local clone.
2. **Contribution flow (uphill, a different discipline):** local → fork → upstream. Sending *your* fix back to Brad is the reverse trip, and it has its own etiquette — that is covered in [088 — Becoming a Steward](088_contributing-back.md), not here.

**A "remote" is just a nickname for a copy that lives somewhere else.** In this repo there is exactly one nickname:

```text
$ git remote -v
origin  https://github.com/WSU-EIT/FreeCRM.git (fetch)
origin  https://github.com/WSU-EIT/FreeCRM.git (push)
```

So `origin` means *our fork on GitHub*. Upstream (`wicketbr/FreeCRM`) is reached at sync time rather than kept as a permanent remote — which is why you won't find it in `git remote`.

**A "branch" is just a named line of history.** Our line is `main`. Upstream's line is also called `main`, and to keep them straight Git/GitHub writes the upstream one as `wicketbr:main` (read it as "the `main` branch belonging to `wicketbr`"). A **merge** is the act of weaving two lines back into one. When you weave upstream's `main` into ours, Git records a **merge commit** — a commit with *two parents* instead of the usual one. That two-parent shape is exactly how you can tell, later, "this is where we caught up with upstream":

```text
commit  1d6571a611ae916b588356539e20940d3ee987fd
parents: 53284a7...  bd48fe3...        ← two parents = a merge
author:  Daniel Pepka
subject: Merge branch 'wicketbr:main' into main
```

The first parent (`53284a7`) is our previous fork tip; the second parent (`bd48fe3`) is Brad Wickett's upstream tip at the moment of the merge. Knowing this lets you answer "how far behind are we?" — it's the distance between our tip and that second parent.

---

<a id="routine-sync"></a>
## 3. Routine Sync Procedure

**Why it matters:** the safest sync is a *frequent, small, routine* one. The team's track record proves it — the last four upstream merges landed almost exactly one week apart:

| Merge commit | Date | Title |
|--------------|------|-------|
| `eccf97c` | 2026-05-14 | Merge branch 'wicketbr:main' into main |
| `fb28085` | 2026-05-21 | Merge branch 'wicketbr:main' into main |
| `53284a7` | 2026-05-28 | Merge branch 'wicketbr:main' into main |
| `1d6571a` | 2026-06-04 | Merge branch 'wicketbr:main' into main |

That weekly heartbeat is the model to copy. A single week of Brad's changes is small enough to read and reason about; a quarter's worth is not.

### The loop, step by step

You can do this two ways. Both end in the same `Merge branch 'wicketbr:main' into main` commit.

**Option A — GitHub's "Sync fork" button (what produced the commits above).** On the fork's page (`WSU-EIT/FreeCRM`), GitHub shows how many commits behind `wicketbr:main` you are and offers **Sync fork → Update branch**. Click it, and GitHub creates the merge commit on the fork's `main` for you. Then everyone pulls it locally:

```text
git fetch origin            # ask GitHub what's new on our fork
git switch main             # make sure you're on main
git pull --ff-only origin main   # fast-forward your local main to match
```

`--ff-only` ("fast-forward only") means *only move my pointer forward if there's nothing of mine to reconcile* — it refuses to invent a merge, so it can never silently entangle your local work. If it refuses, you have local commits to deal with first (see §4).

**Option B — merge upstream from the command line.** When you need to do it locally (e.g., to resolve conflicts on your machine), name the upstream just for this operation:

```text
git fetch https://github.com/wicketbr/FreeCRM.git main   # grab upstream's main
git switch main
git merge FETCH_HEAD                                      # weave it in
```

`FETCH_HEAD` is Git's temporary label for "the thing I just fetched." Merging it produces the same two-parent merge commit. (If you sync from the CLI often, you can give upstream a permanent nickname with `git remote add upstream https://github.com/wicketbr/FreeCRM.git` and then `git fetch upstream` / `git merge upstream/main` — but the current repo deliberately keeps only `origin`, so don't add it without team agreement.)

### What a routine sync actually brings

These merges are not trivial — Brad ships real volume. The June 4 merge (`1d6571a`) summarized as:

```text
200 files changed, 2175 insertions(+), 2163 deletions(-)
```

That single week included the flagship style cleanup `d094f99` ("for consistency with my programming style"), an accessibility pass on the Monaco editor theme, the About page becoming a dialog, and an updated upgrade utility. Pulling weekly means you absorb that as one digestible batch instead of a wall.

### After every sync — rebuild and smoke-test

A merge that *applies cleanly* is not the same as a merge that *works*. Always finish the loop by building and running:

```text
dotnet build
dotnet run --project CRM        # or your usual launch path
```

then click through login and one core page. If upstream changed a shared component or a data contract, this is where you'll catch it — five minutes now versus a confusing bug report later.

---

<a id="resolving-drift"></a>
## 4. Resolving Drift and Conflicts

**Why it matters:** a **conflict** is simply Git saying *"you and upstream both edited the same lines, and I won't guess who's right."* It is not an error and nothing is broken — it's a request for a decision. Handled calmly, conflicts are a five-minute task. Handled in a panic (or with `--force`), they're how work disappears.

### First, measure the drift

Before merging, ask how far apart the two lines are. The left/right count tells you commits unique to each side:

```text
$ git rev-list --left-right --count origin/main...main
0   0
```

`0  0` means local and the fork are perfectly aligned — nothing to reconcile. A number on the right means you have local commits the fork hasn't seen; a number on the left means the fork is ahead of you. To see how far behind *upstream* you are, fetch upstream first and compare against `FETCH_HEAD`.

### When a merge stops on a conflict

Git pauses and marks the disputed files. List them and open each one:

```text
git status        # files under "Unmerged paths" are the conflicts
```

Inside a conflicted file you'll see Git's three-part marker:

```text
<<<<<<< HEAD
    output = String.Empty;     // OUR version (the fork)
=======
    output = string.Empty;     // THEIR version (upstream)
>>>>>>> FETCH_HEAD
```

You decide the final lines, **delete all three marker lines** (`<<<<<<<`, `=======`, `>>>>>>>`), save, then:

```text
git add <file>          # tell Git this one is resolved
git commit              # complete the merge (keep the default merge message)
```

**A house-style tie-breaker you'll hit often:** the upstream author's hand convention is `String.Empty` (capital-S BCL type), *not* the lowercase `string.Empty` the `.editorconfig` would otherwise prefer — it's applied 450+ times across the repo. So when a conflict is purely a `String.Empty` vs `string.Empty` style clash, take the capital-S form to match the house style. See [051 — The Author House Style](051_house-code-style.md) for the full set of these tie-breakers.

### If a merge goes sideways

Because a merge that hasn't been committed yet is fully reversible, you have a clean undo:

```text
git merge --abort       # throw the whole in-progress merge away, back to before
```

Nothing of yours is lost — `--abort` only discards the half-finished merge state. This is your safety valve: when a conflict looks bigger than you expected, abort, take a breath, read the upstream changes, and try again deliberately.

---

<a id="protecting-local-work"></a>
## 5. Protecting Local Work

**Why it matters:** the cardinal rule of living on a fork is *never let an upstream sync overwrite work you haven't safely recorded.* Git makes lost work almost impossible — *if* your changes are committed. Uncommitted edits are the only thing genuinely at risk. So the discipline is mostly "commit (or stash) before you sync."

### Commit first, then sync

A **commit** is a permanent, recoverable save point. Once your work is committed on a branch, a merge can never silently destroy it — at worst it conflicts, and conflicts are recoverable (§4). The single most protective habit is: finish your thought, commit it, *then* run the sync loop.

### Stash for in-flight edits

If you're mid-change and not ready to commit, a **stash** is a labeled "pocket" you tuck dirty edits into so your working tree is clean enough to sync:

```text
git stash push -m "wip: invoice filter"   # set work aside
git pull --ff-only origin main            # sync onto a clean tree
git stash pop                             # bring your work back on top
```

This keeps the sync clean and replays your edits afterward, surfacing any conflict at a moment you control.

### Do real work on branches

Doing every change directly on `main` invites collisions with every sync. Branch instead — a **branch** is a cheap, throwaway side-line of history:

```text
git switch -c feature/quote-pdf   # new branch off main
# ...edit, commit, repeat...
```

`main` stays a clean mirror of "fork + upstream," which makes each weekly sync a trivial fast-forward. You merge `main` *into your feature branch* whenever you want upstream's latest, on your own schedule — never the reverse until you're done.

### The structural safety net: the `.App.` convention

The deepest protection isn't a Git command at all — it's *where you put your code*. FreeCRM splits each core type into a framework file and a paired `*.App.*` partial (a **partial class** is one class whose body is spread across several files). The framework file holds Brad's built-in implementation; the `.App.` file holds *your* customizations. The current tree carries roughly **28** such `.App.` files (e.g. `DataAccess.App.cs`, `DataModel.App.cs`, `Helpers.App.cs`, `Program.App.cs`, `MainLayout.App.razor`).

Why this matters for syncing: because your edits live in `.App.` files that upstream *never touches*, an upstream sync can replace the framework files wholesale and **your customizations sail through untouched** — no conflict at all. The upstream `Upgrade FreeCRM.exe` utility (shipped at the repo root, alongside `Rename FreeCRM.exe` and `Remove Modules from FreeCRM.exe`) is built on exactly this assumption: it can swap framework files only because your code is segregated into `.App.` partials. Put your code there and most of the "protecting local work" problem disappears before it starts. See [042 — The File-Naming Law](042_file-naming-law.md) and [041 — The Upgrade-Safe Model](041_upgrade-safe-model.md) for the full convention.

---

<a id="pitfalls-checklist"></a>
## 6. Pitfalls and Sync Checklist

**Why it matters:** nearly every "Git ate my work" story traces to one of a small set of avoidable moves. Know them, and you'll never star in one.

### Pitfalls

- **Syncing with a dirty tree.** Uncommitted edits are the only work Git can actually lose. Commit or `git stash` *before* you sync — never after a merge has started.
- **Reaching for `--force`.** `git push --force` overwrites the fork's history with yours and can erase teammates' commits. On a shared fork, treat `--force` as off-limits; if you think you need it, you almost certainly want `git merge --abort` or a fresh branch instead.
- **Letting drift pile up.** A week of upstream is readable; a quarter is a conflict avalanche. The cure is cadence — keep the weekly rhythm (§3).
- **Editing framework files directly.** Changing Brad's built-in files (instead of the paired `.App.` partial) guarantees a conflict on the next sync and can be wiped by the upgrade utility. Put custom code in `.App.` files (§5).
- **Resolving a conflict by deleting the "wrong" half blindly.** Read both sides. For style-only clashes, apply the house rule (e.g. keep `String.Empty`, capital-S) rather than coin-flipping.
- **Forgetting to build and smoke-test after the merge.** A clean merge still ships behavior changes. Always `dotnet build` + click through one core page.
- **Confusing the two directions.** Pulling upstream *in* (this doc) is not the same as pushing your work *up* to Brad (that's [088](088_contributing-back.md)). Mixing them up is how a sync turns into an accidental pull request.

### Pre-sync checklist

Run down this list every time, before you click "Sync fork" or `git merge`:

- [ ] **Working tree clean?** `git status` shows nothing to commit — or you've stashed (§5).
- [ ] **On the right branch?** `git switch main` (sync onto `main`, not a feature branch).
- [ ] **Know your drift?** `git rev-list --left-right --count origin/main...main` checked.
- [ ] **Custom code in `.App.` files?** Anything you wrote lives in a `*.App.*` partial, not a framework file.
- [ ] **Backup point exists?** Your work is committed on a branch (recoverable no matter what).
- [ ] **Time budgeted to finish?** Don't start a merge you can't see through to a green build.

After the merge:

- [ ] **Conflicts resolved and committed** with the default merge message (§4).
- [ ] **`dotnet build` is green.**
- [ ] **Smoke test passed** — logged in and exercised one core page.
- [ ] **Pushed** the synced `main` so the team is on the same line of history.

---

<a id="related-docs"></a>
## 7. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the conventions you keep in sync, and the tie-breakers for style conflicts
- [084 — Riding the Framework Forward](084_performing-upgrades.md) — upgrades apply the same discipline at framework scale
- [053 — The Machine Referee: editorconfig and What It Enforces](053_editorconfig-enforcement.md) — the editorconfig you re-pull every sync
- [088 — Becoming a Steward](088_contributing-back.md) — the reverse trip: contributing your fixes back upstream

---
*GuidesV2 054 · drafted from source (`WSU-EIT/FreeCRM` git history + upstream `wicketbr:main` merges) · 2026-06-04.*
