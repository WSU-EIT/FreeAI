# 088 — Becoming a Steward

> **Document ID:** 088  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain how a mature practitioner contributes fixes and features upstream while honoring the conventions.
> **Audience:** Operators and maintainers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Stewardship Matters](#why-stewardship) | What "upstream," "stewardship," and "conventions" mean, and why pushing a fix back pays off |
| 2 | [Deciding to Contribute Upstream](#decide-upstream) | When a change belongs in the framework versus your own `.App.` files |
| 3 | [Preparing a Conformant Change](#prepare-change) | Branching off a clean sync, matching the house style, and proving the build passes |
| 4 | [Submitting and Shepherding the PR](#submit-pr) | Opening the pull request, describing it the way the author does, and handling review |
| 5 | [Verifying Acceptance](#verify-acceptance) | Reading the green CI checks and confirming the merge lands cleanly |
| 6 | [Recovering from Rejection](#recover-rejection) | What to do when a change is reworked, deferred, or declined |
| 7 | [Ongoing Maintainer Duties](#maintainer-duties) | Caring for merged work after it ships |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-stewardship"></a>
## 1. Why Stewardship Matters

**Stewardship** is the practice of caring for code you did not originally write as if it were a shared garden rather than your private plot. In this project, that means contributing your fixes and improvements *back* to the place they came from, instead of hoarding them only in your own copy.

To make sense of that, three plain-language terms first.

- **Upstream** is the original source of the code you build on. FreeCRM is a *template*: you start from someone else's complete application and grow your own product on top of it. The upstream here is the public base repository **`wicketbr/FreeCRM`**, maintained by the original author **Brad Wickett** (`wicketbr` on GitHub). Your team works in a **fork** — a personal copy of that repository — which in this organization is **`WSU-EIT/FreeCRM`** (its git remote `origin` points at `https://github.com/WSU-EIT/FreeCRM.git`). The relationship between upstream, the fork, and your local machine is the subject of [054 — Living on a Fork](054_fork-sync-discipline.md); this doc is about the *opposite* direction of flow — sending a change up rather than pulling one down.
- **A pull request (PR)** is the standard GitHub mechanism for proposing a change: you put your edits on a branch, open a request to merge that branch into the main code, and the maintainer reviews it before it becomes part of the shared codebase.
- **Conventions** are the project's agreed-on rules for how code should look and be organized — brace placement, name casing, where customizations live. They are documented in [051 — The Author House Style](051_house-code-style.md). Honoring them is not cosmetic fussiness; it is the price of admission for upstream code.

**Why it matters.** When you fix a real framework bug only in your fork, you take on a hidden tax: every future upstream sync risks re-colliding with the very lines you patched, and you pay to re-resolve the same conflict forever. Contributing the fix upstream retires that tax permanently — once Brad merges it, the next sync simply *brings your own fix back to you*, already reconciled. You also stop being the only person who benefits: every other fork built from the same template (and there are many — `FlexCRM`, `FreeAI`, `FreeCICD`, `FreeManager`, and more all branch from this base) inherits the fix too. Stewardship is therefore the most leveraged thing a senior practitioner does. A local patch helps one app once; an upstreamed fix helps every app forever.

There is a real, observable model for what "good stewardship" looks like in this project. In a single cleanup pass in mid-2026, the author swept 187 files in one commit (`d094f99`) — not to add features, but to make the whole codebase consistent with his own stated style. That commit message ends with the line *"for consistency with my programming style."* That sentence is the spirit of this doc: a steward leaves the codebase **more consistent than they found it**, not just functionally correct.

---

<a id="decide-upstream"></a>
## 2. Deciding to Contribute Upstream

Not every change you make belongs upstream. The first job of a steward is judgment: deciding whether a change is a *framework* concern (push it up) or an *application* concern (keep it local). Getting this wrong in either direction is costly — pushing app-specific code upstream pollutes the template for everyone, while hoarding a genuine framework fix dooms you to re-resolving it on every sync.

The single most useful tool for this decision already exists in the codebase: **the `.App.` file-naming convention.**

**What `.App.` means.** The framework is deliberately split so that almost every customizable area has *two* files: a framework file holding the built-in implementation, and a paired `*.App.*` file holding your application's overrides. You see this pairing all over the tree — `DataModel.cs` / `DataModel.App.cs`, `Helpers.cs` / `Helpers.App.cs`, `DataAccess.cs` / `DataAccess.App.cs`, `GraphAPI.cs` / `GraphAPI.App.cs`, `Program.cs` / `Program.App.cs`, `MainLayout.razor` / `MainLayout.App.razor`. The whole point of the split is so the framework files can be *replaced wholesale* during an upgrade without touching your custom code. (The bundled `Upgrade FreeCRM.exe` utility depends on this — it can only swap framework files out cleanly because your code lives in the `.App.` partners.)

That split gives you a clean decision rule:

| If the change lives in… | …it is a | …so you should |
|---|---|---|
| A framework file (e.g. `Helpers.cs`, `DataAccess.Users.cs`, a base `.razor`) | Framework concern | **Contribute it upstream** |
| An `.App.` file (e.g. `Helpers.App.cs`, `DataAccess.App.cs`) | Application concern | **Keep it local** — it is yours by design |

**Concrete signals that a change belongs upstream:**

- **It fixes a defect in built-in behavior** — the framework does the wrong thing for *everyone*, not just for your app's data. A miscalculated date, a wrong CSS class on a sort column, an accessibility gap in a shared component. (Real upstream commits show exactly these: fixing the auto-hiding column class so the current sort column never hides, and adding an `AriaLabel` to the Monaco editor component.)
- **It improves a shared component or helper** that every fork uses.
- **It corrects a style or consistency slip** in framework code — a missing space before a brace, a stray `""` that should be `String.Empty`, an unused `using`. These are small, but they are exactly what the author himself sweeps.

**Concrete signals to keep it local:**

- It encodes *your* business rules, branding, schema, or tenant logic.
- It only makes sense for your deployment.
- It belongs in, or can be moved into, an `.App.` file. If a useful change currently sits in a framework file but is really app-specific, the steward's move is to **relocate it into the `.App.` partner** rather than upstream it — exactly what commit `b4cf7e1` did when it moved app-tunable private fields out of `DataAccess.cs` into `DataAccess.App.cs`.

When the decision is genuinely close — say a fix is generally useful but also a little opinionated — that is precisely when you write it down. Capture the context, the options, and your reasoning in a decision record before you act, as described in [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md). A two-paragraph record now saves an hour of "why did we do this?" later.

---

<a id="prepare-change"></a>
## 3. Preparing a Conformant Change

A **conformant** change is one that the maintainer can merge without having to fix it first. The whole goal of this section is to make your contribution boring to accept: it builds, it matches the style, and it touches only what it should. Three steps get you there.

**Step 1 — Branch from a freshly synced base.** Before you write a line, make sure your fork's `main` is even with upstream, then create a working branch off it. **A branch** is a named, isolated line of work, so your contribution can be reviewed on its own without entangling unrelated edits.

```text
# bring your fork up to date with upstream first (see doc 054 for the full discipline)
git checkout main
git pull origin main

# create a focused branch for this one change
git checkout -b fix/sort-column-autohide
```

Branching from a current base is what keeps your PR small and conflict-free. If you branch off a stale `main`, your change will arrive carrying weeks of unrelated drift, and the maintainer has to untangle it. The full mechanics of staying synced — remotes, fetch, rebase, protecting local work — live in [054 — Living on a Fork](054_fork-sync-discipline.md). Treat a clean sync as a prerequisite, not an optional nicety.

**Step 2 — Match the house style exactly.** This is where contributions most often get bounced, and it is entirely avoidable. The full rulebook is [051 — The Author House Style](051_house-code-style.md); the load-bearing points to re-check before you commit:

- **Braces:** opening `{` on its *own new line* for methods and types, but on the *same line* for properties and for control-flow keywords (`if (x) {`, `} catch`, `} else` stay together, with a space after the keyword).
- **Empty strings:** always `String.Empty` (capital `S`), never `""`. This is enforced by hand — the formatter will not do it for you. The author applied this 450+ times across the repo.

```csharp
// ✗ what a tool might leave
public string Id { get; set; } = "";

// ✓ house style — capital-S String.Empty
public string Id { get; set; } = String.Empty;
```

- **Casing:** business-method parameters are PascalCase (`AppointmentNoteId`, `CurrentUser`), but dependency-injected constructor parameters stay camelCase (`httpContextAccessor`, `configHelper`).
- **No dead weight:** delete unused `using` directives, commented-out code, and extra blank lines — the codebase uses exactly *one* blank line as a separator. (One exception: never delete the template markers `// {{ModuleItemStart:X}}` / `// {{ModuleItemEnd:X}}`. Those drive the framework's module-removal tooling and must survive verbatim.)
- **Leave the deliberate quirks alone.** The SignalR hub is intentionally camelCase (`crmHub`, `IsrHub`, in `signalrHub.cs`). Do not "fix" it; that is not a bug, it is a documented legacy exception.

Most of the mechanical layout (4-space indent, CRLF, no final newline, the methods/types brace rule) is applied automatically when you reformat in the IDE, because the repo ships an `.editorconfig`. But the hand-enforced items above — `String.Empty`, the single-blank-line discipline, the wrapped-parameter `){` shape — are exactly the ones automation misses, so make a deliberate manual pass before committing.

**Step 3 — Prove it builds.** A word on testing in this project, because it shapes what "proving" means here. **There is no separate unit-test suite to run** — despite the CI workflow being named "Build and Test," the verification it performs *is the build itself*. The check that matters is: does the solution restore, build, and publish cleanly in both Debug and Release? Run that locally before you push:

```text
dotnet restore CRM.slnx
dotnet build CRM.slnx --configuration Debug --no-restore
dotnet build CRM.slnx --configuration Release --no-restore
```

If both configurations build green locally, you have reproduced exactly what the automated check will do (covered next in Section 5). Keep each commit focused on one logical change with a clear message — the author's own commit messages are full sentences that explain *what* and *why* (for example, the Monaco-editor commit spells out the accessibility reasoning), and matching that habit makes your contribution feel native.

---

<a id="submit-pr"></a>
## 4. Submitting and Shepherding the PR

With a conformant branch in hand, you open the pull request and then *shepherd* it — stay with it through review rather than tossing it over the wall. The framework ships no formal `CONTRIBUTING.md` or PR template, so the bar is set by the author's own observable habits, not by a checklist. Match those habits and acceptance gets easy.

**Push your branch and open the PR.**

```text
git push origin fix/sort-column-autohide
```

Then open the pull request on GitHub from your branch into the upstream `main` (`gh pr create` works well from the command line). Because the base repository is the public `wicketbr/FreeCRM`, your PR is a proposal *to the original author*; treat it as a courteous request, not a demand.

**Write the description the way the author writes commits.** Look at any real upstream commit message and you will see the pattern: a plain-English statement of *what changed* followed immediately by *why*. Mirror that in your PR description:

- **What** the change does, in one or two sentences a non-engineer could follow.
- **Why** it is needed — the user-visible symptom or the consistency gap it closes.
- **Scope** — which files, and confirmation that you stayed inside framework files (not `.App.` files) and matched the house style.
- **Verification** — that the solution builds clean in Debug and Release.

A description that already answers the maintainer's questions removes the friction that causes back-and-forth.

**Respond to review like a collaborator.** **Review** is the maintainer reading your change and asking for adjustments before merging. The etiquette is simple but it is what separates a steward from a drive-by contributor:

- Treat every comment as a request to *learn the house style better*, not as criticism. The most common asks will be exactly the hand-enforced items from Section 3 (`String.Empty`, brace placement, an extra blank line) — fix them quickly and without debate.
- Keep the PR scoped. If a reviewer notices a *different* problem, resist the urge to fix it in the same PR; note it and open a separate one. Small PRs merge; sprawling ones stall.
- Push follow-up commits to the same branch — the open PR updates automatically, so the reviewer sees your fixes in context.
- Defer to the author on judgment calls about framework direction. It is their template; your job is to make it easy to say yes.

---

<a id="verify-acceptance"></a>
## 5. Verifying Acceptance

**Acceptance** has two halves: the automated checks pass, and the change actually lands in the upstream `main`. A steward confirms *both* — green checks alone are not a merge.

**Half 1 — Read the CI checks.** **CI** ("continuous integration") is the automation that runs on a server every time you push or open a PR, so a human does not have to manually verify the basics. This project's CI is the `build-test.yml` GitHub Actions workflow named **"Build and Test."** It runs on a `windows-latest` machine using **.NET 10** and does exactly three meaningful things, in order:

```text
dotnet restore CRM.slnx
dotnet build   CRM.slnx --configuration <Debug | Release> --no-restore
dotnet publish CRM/CRM.csproj --configuration <Debug | Release>
```

A few details worth knowing so the green checkmarks are not a mystery:

- It runs a **matrix** of two configurations — **Debug *and* Release** — as separate legs. Both must go green; a change can compile in Debug yet trip a Release-only warning-as-error, so do not declare victory on one leg.
- A **preflight** job runs first (verifying git, that the commit exists on the remote, and a clean workspace) before the build job runs at all.
- The publish step produces downloadable **artifacts** named like `CRM-Debug` and `CRM-Release`, plus a one-time `RepoSnapshot` zip for traceability. Their presence is your proof the publish actually succeeded, not just the compile.

When the PR page shows the "Build and Test" check passing for both configurations, the automated half is satisfied. If a leg is red, open it, read the failing step's log, fix locally, and push again to the same branch — the check re-runs automatically.

**Half 2 — Confirm the clean merge.** Green CI means "this *could* be merged"; it does not mean it *was*. The change is genuinely accepted only when the maintainer merges your PR into upstream `main`. You can see this directly: the PR flips to a merged state, and the upstream history gains your commit.

The satisfying final confirmation comes on your *next* sync. When your fork pulls upstream again, your own contribution arrives back to you as part of a merge commit — you can see these in the fork's history as commits like `1d6571a` (*"Merge branch 'wicketbr:main' into main"*). At that moment the fix is reconciled into your fork permanently, and the maintenance tax described in Section 1 is gone for good. That round trip — your change going up, then coming back down already merged — is the whole payoff of stewardship made visible.

---

<a id="recover-rejection"></a>
## 6. Recovering from Rejection

Not every PR is merged as-is, and that is normal, not a failure. A steward handles a declined or reworked change without drama and without losing the value of the work. There are three outcomes short of a clean merge; each has a calm response.

**Outcome A — "Please rework it."** The maintainer wants the change but not in its current shape — different approach, smaller scope, more style polish. This is the *most common* outcome and the easiest: make the requested edits as new commits on the same branch and push. The open PR updates in place; there is nothing to reopen. Reworking is collaboration succeeding, not failing.

**Outcome B — "Not now."** The change is fine but the timing or priority is wrong — perhaps it overlaps work the author already has in flight. Keep your branch alive, keep your fork in sync so it does not rot, and revisit when the maintainer signals readiness. In the meantime your fix is not stuck in limbo: you can carry it locally by moving it into the appropriate `.App.` file (Section 2), so your application gets the benefit today while the upstream conversation continues. This is the safety valve of the `.App.` convention — you are never *forced* to choose between "merged upstream" and "I have my fix."

**Outcome C — "Declined."** The author decides the change does not belong in the framework — it is too app-specific, too opinionated, or conflicts with a direction you could not see. Respect the call; it is their template to shape. Then do two things. First, relocate the change into your fork's `.App.` file so it lives on as a clean, isolated local customization rather than a divergence inside a framework file — that keeps future syncs friction-free. Second, **write a short decision record** capturing what you proposed, that it was declined, and why, per [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md). That record is what stops a teammate (or future you) from re-proposing the same rejected change six months later.

**A note on reverts.** A **revert** is a commit that undoes a previously merged change. If something you contributed has to be reverted upstream after the fact (it caused a regression that only surfaced later), treat it exactly like Outcome A: understand the regression, fix the root cause, and submit a fresh, corrected PR. A revert is the project protecting its users in the moment; it is not a verdict on you. The steward's response to any of these outcomes is the same posture — keep the work safe in an `.App.` file or a branch, keep the history honest with a decision record, and keep the relationship with the maintainer easy.

---

<a id="maintainer-duties"></a>
## 7. Ongoing Maintainer Duties

Merging is the beginning of a contribution's life, not the end. Once your change is part of the upstream framework, you have quietly become a **co-maintainer** of that code — every fork now depends on it. The duties are light but real.

- **Keep syncing, so you live with your own change.** The most important ongoing duty is simply to keep pulling upstream into your fork on the cadence described in [054 — Living on a Fork](054_fork-sync-discipline.md). This is how you discover, early and on your own machine, whether your merged change interacts badly with later framework evolution — before it becomes someone else's surprise.
- **Stand behind it if it breaks.** If a regression traces back to your contribution, own the follow-up fix promptly (see Section 6's note on reverts). Code you put into the framework is code you implicitly agreed to support.
- **Preserve the consistency you inherited.** When future framework changes pass near your code, hold the line on the house style — `String.Empty`, brace placement, the single-blank-line discipline, no dead `using`s. The author's flagship cleanup (`d094f99`) was *net-negative* — it deleted more lines than it added — because a healthy codebase trends toward *less* clutter over time. A steward continues that trend; they do not reverse it.
- **Protect the `.App.` boundary.** Make sure later edits keep application concerns in `.App.` files and framework concerns in framework files. That boundary is what lets the `Upgrade FreeCRM.exe` utility swap the framework underneath every fork without destroying anyone's customizations. Honoring it is how you keep "find all my code" and "upgrade the framework beneath me" both possible — for yourself and for everyone downstream.
- **Write down the non-obvious.** When you make a maintenance decision whose reasoning is not self-evident from the code, leave a decision record ([067](067_decisions-and-briefs.md)). The codebase that explains *why* it is shaped the way it is stays maintainable far longer than one that only shows *what* it does.

Do these consistently and you complete the loop the whole band is about: you took, you gave back, and now you tend what you gave — leaving the template, in the author's own words, more consistent than you found it.

---

<a id="related-docs"></a>
## 8. Related Docs

- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — the fork-sync discipline
- [051 — The Author House Style](051_house-code-style.md) — the conventions to honor
- [067 — Decision Records and the CTO Brief](067_decisions-and-briefs.md) — recording your decisions

---
*GuidesV2 088 · Becoming a Steward · drafted 2026-06-05 from source (`wicketbr/FreeCRM` ↔ `WSU-EIT/FreeCRM` remotes, commit `d094f99`, `.github/workflows/build-test.yml`, the `.App.` convention, `CRM.DataAccess/README.md`, and styling research doc 0001).*
