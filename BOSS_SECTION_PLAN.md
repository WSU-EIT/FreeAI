# Plan: Add a "Boss Questions" briefing section to every README in the FreeAI suite

**Status:** Plan / proposal — nothing has been edited yet.
**Author:** Claude (deep-dive pass)
**Date:** 2026-06-11
**Scope:** 172 README files across 11 projects + the suite root, in the `FreeAI` monorepo.

---

## 0. TL;DR (read this first)

You want each README to answer the questions a non-deep-technical decision-maker (your boss) actually asks, in plain English, with **hard links straight to the exact source files on GitHub** and an **ASCII diagram of the hardest thing the project does**.

I've indexed the whole suite, confirmed the GitHub link format, found the existing documentation rules I need to respect, and written a **fully worked example** (Section 6) so you can see exactly what every README will gain before I touch a single file.

**The one thing I need from you before I start editing:** confirm the four decisions in **Section 8**. My recommended defaults are pre-selected; if you're happy with them, I just go.

---

## 1. What "the boss section" is

A new, consistently-formatted section added near the bottom of each README (just above the existing `## License` / `## About` footer). It answers five questions, in this fixed order:

| # | Boss question | What the section gives them |
|---|---------------|------------------------------|
| 1 | **How does this work?** | A plain-English walkthrough of the mechanism — no jargon without a definition. |
| 2 | **What tech does it use, and *where exactly*?** | A table: *technology → the exact file that uses it*, every file a clickable hard link to GitHub. |
| 3 | **Why does this exist?** | The problem it was built to solve / its origin. |
| 4 | **What does it do that other tools don't?** | Honest differentiation vs. the obvious alternatives. |
| 5 | **Show me — terms & a picture.** | A mini-glossary of project-specific terms, links to live screenshots/docs, **and an ASCII diagram** of the most complex flow: *user → code → the hard part → how it's resolved → fed back to the user.* |

> **Jargon check (for the briefing itself):** a *hard link* here just means a full `https://github.com/...` URL that opens the precise file in the browser — not a relative `./path` link that only works inside the repo. Your boss can click it from an email.

---

## 2. What I found (the facts this plan is built on)

### 2.1 It's one monorepo, not many repos
Locally `FreeAI/` is a single Git repository. Its remote is:

```
origin  https://github.com/WSU-EIT/FreeAI.git   (branch: main)
```

So **every hard link follows one rule** — take the file's path *relative to the `FreeAI/` folder*, swap `\` for `/`, and prefix it:

```
https://github.com/WSU-EIT/FreeAI/blob/main/<relative/path/to/file>
```

Example — the Kanban board source file:
`FreeBlazorExtended\FreeBlazorExtended\KanbanBoard\KanbanBoard.razor`
→ `https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/KanbanBoard/KanbanBoard.razor`

To point at an *exact spot in a file* (a specific method), append `#L120-L140`. I'll use line anchors for the important "where exactly" links and plain file links elsewhere.

> Note: some sub-projects mention older standalone repo URLs (`WSU-EIT/FreeA11yChecker.git`, `WSU-EIT/FreeGLBA`, etc.). Those are the *original* repos these projects were consolidated **from**. The live, authoritative copy your boss should click into is the `FreeAI` monorepo, so all new links target `FreeAI`.

### 2.2 There is already a documentation standard I must not fight
`FreeAI/DOCS_STANDARD.md` defines a numbered `Docs/` set per project (`000_overview.md` … `004_showcase.md`) plus a `showcase/` folder of screenshots. The READMEs are *separate* from that `Docs/` set. **My new section lives in the `README.md` files only** and will *link into* the existing `Docs/` and `showcase/` artifacts for the "can I see it?" part (question 5) instead of duplicating them.

### 2.3 Existing README shape (so I insert cleanly)
Most READMEs end with a `## License` (MIT) and `## About` (WSU-EIT) footer. The boss section goes **immediately before `## License`**, so the footer stays last. READMEs without a footer get the section appended at the end.

### 2.4 The inventory — 172 READMEs, and they vary enormously in size
- **3** are large (250+ lines, e.g. `FreeServices.Installer` at 526)
- **24** are medium (100–250)
- **95** are small (40–100)
- **50** are tiny (<40), and ~10 of those are 9–19-line **stubs** (e.g. `Foundation/Components/Cards/README.md`, the `DynamicBlazorSupport` folders) that are little more than placeholders.

This size spread is why the section must **scale by tier** (Section 4) rather than be one rigid block — a 526-line installer guide and a 9-line placeholder should not get the same treatment.

---

## 3. Full index, grouped by project, in the order I'll deep-dive them

Ordering principle: **establish a reusable template on the best-documented project first, get your sign-off, then fan out** — going fastest where projects share the same skeleton, and saving the sprawling ones (component library, CLI suite) for after the pattern is proven.

> **Why this order saves work:** 7 of the 11 projects are built on the same "FreeCRM scaffold" — the same `Client / DataAccess / DataObjects / EFModels / Plugins` sub-project skeleton. Nail the briefing for that skeleton once (Phase 0–1) and the rest become fill-in-the-blanks.

### Phase 0 — Golden template (get your sign-off here)
| Project | READMEs | Why first |
|---|---|---|
| **FreeA11yChecker** | 14 | The flagship: best-documented, full `Docs/` set, and its "4-engine accessibility scanner" is a perfect first ASCII diagram. It exercises the full FreeCRM scaffold, so its sub-project briefings become the template for everything in Phase 1. |

### Phase 1 — Scaffold apps (same skeleton → fast)
| Project | READMEs | The "most complex topic" to diagram |
|---|---|---|
| **ChatWithAI** | 8 | Azure OpenAI token-budgeted chat loop |
| **FreeLLM** | 7 | Token-balanced prompt-package assembly |
| **FreeSmartsheets** | 11 | Smartsheet API → workspace/sheet inventory sync |
| **FreeManager** | 8 | Code-generation: Entity Wizard → scaffolded project |
| **FreeGLBA** | 12 | GLBA compliance tracking + NuGet client integration |
| **FreeServicesHub** | 15 | SignalR hub receiving live agent heartbeats |

### Phase 2 — Component library (distinct pattern: many per-component READMEs)
| Project | READMEs | Notes |
|---|---|---|
| **FreeBlazorExtended** | 43 | ~20 self-contained UI components, each with its own README. Needs a dedicated **component-README sub-template** (lighter than a full app). The Windows-service `Agent` + `AgentMonitoring` pair is the complex topic to diagram. |

### Phase 3 — Plugin system
| Project | READMEs | Most complex topic |
|---|---|---|
| **FreePlugins** | 19 | Roslyn **runtime compilation** — C# source compiled and loaded while the app runs. The headline diagram of the whole suite. |

### Phase 4 — Tooling / CLI suite
| Project | READMEs | Notes |
|---|---|---|
| **FreeTools** | 29 | ~15 independent CLI tools (AppHost, BrowserSnapshot, WorkspaceInventory, AccessibilityScanner, FreeCodeMaid…). Each tool README is its own mini-briefing; the accessibility-scanner pipeline is the marquee diagram. |

### Phase 5 — Background services
| Project | READMEs | Most complex topic |
|---|---|---|
| **FreeServices** | 4 | The biggest individual READMEs (356–526 lines). Windows-service install + heartbeat lifecycle. |

### Phase 6 — Suite root & reference (done last, because they summarize everything)
| File | READMEs | Notes |
|---|---|---|
| **`README.md`** (suite root) | 1 | Gets a *suite-level* boss section: one ASCII map of how all 11 projects relate. |
| **`_codemaid-reference`** | 1 | Reference material; light-touch section or pointer. |

**Total: 172 READMEs across 7 phases.**

---

## 4. How the section scales by README tier

Not every README earns a full essay. Four tiers:

| Tier | Which READMEs | What the boss section contains |
|------|---------------|--------------------------------|
| **A — Project root** (11) | `FreeGLBA/README.md`, etc. | Full 5-question section + **big system-level ASCII** (how the whole app fits together). |
| **B — Sub-project root** (~50) | `*.Client`, `*.DataAccess`, `*.Service`, each CLI tool, each major component | Focused 5-question section + **component-level ASCII** (this part's own flow). |
| **C — Leaf component / plugin** (~100) | UI components, example plugins | Condensed section: 1–2 lines per question, the tech→file link table, and a **small flow ASCII**. |
| **D — Stub placeholders** (~10, the 9–19-line ones) | `Foundation/Components/Cards`, `DynamicBlazorSupport` | Minimal: one-line *what + why* and a link **up** to the parent's full briefing. No forced ASCII. (See decision 8.1.) |

---

## 5. The deep-dive method — how I keep it accurate (not invented)

For **each** README, the same disciplined loop. The non-negotiable rule: **claims come from the source, not from the existing README's prose.** Existing READMEs are a starting hint; the code is the source of truth.

1. **Read the folder's actual source**, not just its README — the `.csproj` (to confirm framework, SDK, and real package references), and the key `.cs` / `.razor` files. This is how I verify "what tech, and where."
2. **Locate the exact file(s)** behind each named technology so the link points at real code (e.g. "SignalR → `AgentHub.cs` line 40"). If a claimed tech isn't actually in the code, I flag it rather than link a fiction.
3. **Pick the single most complex topic** for that README's scope and trace its full path: user action → which code blocks fire → the genuinely hard part → how it's resolved → what the user sees back. That trace *becomes* the ASCII diagram.
4. **Write the 5-question section** at the tier-appropriate depth, defining every term on first use.
5. **Build hard links** with the Section 2.1 rule; line-anchor the important ones.
6. **Insert** above `## License` (or append if no footer), matching the file's existing heading style and tone.
7. **Self-verify** before moving on:
   - every link path actually exists on disk (so it'll resolve on GitHub),
   - every tech claim is backed by a file I actually read,
   - the ASCII renders correctly in a fixed-width block,
   - no duplication of the `Docs/` set — I link to it instead.

**Verification at the end of each phase:** a pass that re-checks a sample of links resolve to real paths and that the diagrams render, before starting the next phase. I'll report what I verified and anything I couldn't confirm — I won't claim a link is good if I didn't check the path exists.

---

## 6. Worked example — exactly what gets added (real, not a mock-up)

Below is the section I would add to
`FreeBlazorExtended/FreeBlazorExtended/KanbanBoard/README.md` (a Tier-C leaf component). This is built from the actual source I read, so it shows the real quality bar — links, terminology, and the ASCII flow.

---

> ### 🧭 Plain-English Briefing — *The Boss Questions*
>
> **How does this work?**
> It draws a row of columns (To Do / In Progress / Done) with draggable cards. When you drag a card and drop it, the component doesn't move the card itself — it just *announces* "card X moved from column A to column B at position N" by raising an event called `OnCardMoved`. Whatever page is using the board decides what to do with that news (save it, sync it to other users, etc.) and hands the board an updated list to redraw. This "the board only reports, the page decides" design is called a **controlled component**.
>
> **What technology does it use — and where exactly?**
>
> | Technology | What it's for | Exact location |
> |---|---|---|
> | Blazor (C# in the browser) | The whole component is C#/Razor, no JavaScript | [KanbanBoard.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/KanbanBoard/KanbanBoard.razor) |
> | HTML5 native drag-and-drop | Browser handles the dragging; Blazor surfaces it as C# callbacks (`ondragstart`, `ondrop`) — *no* JS interop | [KanbanBoard.razor (drag handlers)](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/KanbanBoard/KanbanBoard.razor) |
> | Bootstrap 5 + FontAwesome 6 | Styling and the default card icons (drop FontAwesome by supplying your own `CardTemplate`) | host page references |
>
> **Why does this exist?**
> Teams wanted a Trello/Jira-style board inside their own Blazor apps without taking a heavy third-party dependency or writing JavaScript. This is one self-contained `.razor` file with zero NuGet packages beyond the .NET base library.
>
> **What does it do that other tools don't?**
> Commercial board widgets own their data and bury you in callbacks to stay in sync. This one is deliberately *stateless* about your data: your list of cards is the single source of truth, so it can never silently drift out of sync with your database or with other users' screens. Real-time multi-user sync is *your* wiring (e.g. SignalR), not a hidden behavior you have to fight.
>
> **Terminology & "can I see it?"**
> - **Controlled component** — the board renders what you give it and reports events; it never mutates your data.
> - **`OnCardMoved`** — the event carrying `{ CardId, FromColumnId, ToColumnId, NewSortOrder }`.
> - **Source of truth** — your `Cards` list; the board keeps no private copy.
> - *See it live:* the showcase page in `FreeBlazorExample` (`/showcase/kanban`).
>
> **The hard part, drawn** — what happens on a single drag-and-drop:
>
> ```
>        ┌──────────┐   1. drags card        ┌───────────────────────────┐
>        │   USER   │ ─────────────────────▶ │   Browser (HTML5 native    │
>        │ (mouse)  │                         │   drag-and-drop events)    │
>        └────▲─────┘                         └─────────────┬─────────────┘
>             │                                             │ 2. ondragstart / ondrop
>             │ 6. board re-renders                         ▼   surfaced as C# callbacks
>             │    from the NEW list             ┌────────────────────────┐
>             │                                   │   KanbanBoard.razor    │
>             │                                   │  (does NOT move card)  │
>             │                                   └───────────┬────────────┘
>             │                                               │ 3. raises OnCardMoved
>             │                                               ▼   {Card, From, To, Sort}
>             │                                   ┌────────────────────────┐
>             │                                   │  YOUR PAGE (the caller)│
>             │                                   │  THE COMPLEX PART:     │
>             │                                   │  • update card.Column  │
>             │                                   │  • renumber SortOrder  │
>             │                                   │  • persist to DB       │
>             │                                   │  • (optional) SignalR  │
>             │                                   │    broadcast to others │
>             │                                   └───────────┬────────────┘
>             │                                               │ 4. mutate the Cards list
>             │                                               │ 5. pass new Cards back ───┐
>             └───────────────────────────────────────────────────────────────────────────┘
>            The board is a pure projection of Cards — so it can never drift out of sync.
> ```

---

That's the bar for a **leaf** README. A **project-root** README (Tier A) gets the same five questions but a larger diagram spanning Browser → Blazor client → API → DataAccess → EF Core → database, plus the SignalR real-time path.

---

## 7. Deliverables & rollout

1. **Phase 0:** FreeA11yChecker's 14 READMEs done end-to-end → I stop and show you, you approve the format.
2. **Phases 1–6:** the remaining 158, phase by phase, with a link/diagram verification pass at each phase boundary.
3. A short **`BOSS_SECTION_LOG.md`** tracking which READMEs are done, plus any tech claim in an existing README that the source *didn't* support (so you know what was corrected).
4. Nothing is committed or pushed unless you ask — edits sit in the working tree for review.

---

## 8. Decisions to confirm before I start editing

My recommended default is marked ✅. Tell me to change any of them; otherwise I proceed on these.

**8.1 — Coverage of the ~10 stub placeholders (9–19-line READMEs).**
- ✅ **Condense:** give stubs a one-line *what + why* and a link up to the parent's full briefing (no forced ASCII on a placeholder).
- Alternative: force the full 5-question section + ASCII into every stub too (more uniform, but padded and repetitive).

**8.2 — ASCII diagram granularity.**
- ✅ **Tailored per README** at its own scope (leaf gets its own small flow; project root gets the big system map). Stubs reuse/point to the parent diagram.
- Alternative: one big diagram per project only; sub-READMEs link up to it.

**8.3 — Golden-template checkpoint.**
- ✅ **Yes:** finish FreeA11yChecker first and pause for your sign-off before the other 158.
- Alternative: do all 172 in one pass, no mid-point checkpoint.

**8.4 — Section heading / tone (this is a *public* repo).**
- ✅ **"🧭 Plain-English Briefing — *The Boss Questions*"** (keeps your framing, reads fine publicly).
- Alternatives: "Briefing for Decision-Makers" (more corporate) · "How this works, in plain terms" (neutral, no "boss") · or literally "The Boss Questions".

---

## 9. Risks & how I handle them

| Risk | Mitigation |
|---|---|
| A link points at a file that gets moved/renamed later | Links target `blob/main/...`; I verify the path exists on disk at write time. If you rename files often, I can switch to permalinks pinned to a commit SHA (say the word). |
| Existing README claims a tech the code doesn't actually use | I verify against source and log corrections in `BOSS_SECTION_LOG.md` rather than repeating an unverified claim. |
| 172 sections drift in style/quality | Tier templates (Section 4) + the Phase-0 golden template keep them consistent; per-phase verification catches drift. |
| ASCII diagrams break on GitHub | All diagrams live in fenced code blocks (fixed-width) and use only plain ASCII box characters; checked to render. |
| Scope is large | Phased rollout means you get value (and a stop point) after the first 14, not after all 172. |
