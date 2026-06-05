# 000 — Index: GuidesV2 (Cleanroom Rebuild)

> **Document ID:** 000
> **Category:** Index
> **Purpose:** The master plan and table of contents for GuidesV2 — a from-scratch documentation set for FreeCRM, its house conventions, and FreeTools.
> **Audience:** Everyone — written so a non-engineer with decision authority can follow it (see "How These Docs Are Written").
> **Status:** 🟢 Live. As of 2026-06-05, all 62 docs are ✅ **drafted** — full content written and grounded in the real FreeCRM/FreeTools source (review & polish ongoing). The newest additions are the five per-language deep references `055`–`059`, each rule linked to real source.

---

## What GuidesV2 Is (and Why It Exists)

GuidesV2 is a **clean rebuild** of our documentation — designed fresh from the subject matter, not reshaped from the old `Guides/` folder. No text was copied from the previous docs; the structure, titles, and explanations here are original. (The old set was read only as background inspiration; this is a genuine cleanroom rewrite.)

The goal of the rebuild: one **learning path** a person can walk from "I just cloned this" to "I steward it for the team," where every idea is introduced exactly once, at the moment you first need it — instead of a pile of reference docs you have to already understand to navigate.

> **Plain-language note (a "framework" = a pre-built foundation you build your app on top of):** This documents three things that work together — **FreeCRM** (the app framework), the **house conventions** (how we write and decide), and **FreeTools** (command-line helpers that inspect an app).

---

## How These Docs Are Written — The Reader Model

**Every GuidesV2 doc is written for an intelligent reader who holds the authority to decide but has only intern-level technical depth.** That is the house standard. Concretely, each doc:

- **Defines a term the first time it appears** (e.g., "*tenant* = one customer's isolated slice of data"), instead of assuming you know it.
- **Leads with why it matters and what decision it affects**, then the detail.
- **Never assumes prior framework, language, or tooling knowledge** beyond basic computer-science ideas.
- **Surfaces the choice plainly** when there's a decision to make, with the trade-offs spelled out.

This is not "dumbing down" — it's making sure the person with the authority can actually exercise it. Deeper technical appendices are allowed, but they sit *below* a plain-language explanation, clearly marked.

---

## How GuidesV2 Is Organized

A single journey, in nine bands, from first login to long-term stewardship:

| Band | Theme | You are… |
|------|-------|----------|
| **00x** | Landing Zone: from clone to login | Brand new |
| **01x** | Mental Models: how this differs from stock .NET | Gaining your footing |
| **02x** | The Data Stack: where records live | Building features |
| **03x** | Core Craft: everyday screens & components | Building features |
| **04x** | Extending Without Breaking: the live runtime | Going advanced |
| **05x** | The House Style: code conventions | Contributing |
| **06x** | The Team Operating System: how we decide | Collaborating |
| **07x** | Analyzing Apps with FreeTools | Checking quality |
| **08x** | Operate, Deploy & Steward | Running it for real |

**Legend:** ✅ drafted (content complete) · 🛠 **skill candidate** — a doc whose process should also ship as a Claude Code slash-command (a saved `/command` you type instead of re-explaining the task). See the [Skill Candidates](#skill-candidates) build list.

---

## The Catalog (every planned doc)

### 00x — Landing Zone: From Clone to Login
| Doc | Title | Covers |
|-----|-------|--------|
| `001_three-pillars.md` ✅ | Three Pillars: CRM, Conventions, and CLI | How FreeCRM, the house conventions, and FreeTools interlock, so a newcomer knows what they're looking at. |
| `002_toolchain-prereqs.md` ✅ | Getting .NET 10 and the Toolchain to Cooperate | Installing the runtime, SDK, and editor tooling so the first build and live-reload work the first time. |
| `003_zero-to-login.md` ✅ | Clone, Build, Seed, and Sign In | The full path: download, compile, set up the database, seed a starter customer, and land on a logged-in screen. |
| `004_reading-the-tree.md` ✅ | Reading the Repository Map | A guided tour of the folders so you can predict where any kind of code or asset lives. |
| `005_first-feature.md` ✅ | Shipping Your First Record Screen | A hands-on exercise that produces one working "list + edit" feature to build early confidence. |
| `006_local-dialect.md` ✅ | Speaking the Local Dialect | The vocabulary (tenant, wrapper, partial segregation, soft-delete, result type), each linked to its deep-dive doc. |

### 01x — Mental Models: How This Differs From Stock .NET
| Doc | Title | Covers |
|-----|-------|--------|
| `011_wrapper-philosophy.md` ✅ | Why We Wrap the Framework | The idea of preferring custom tenant-aware helpers over raw .NET calls — and why. |
| `012_wrapped-plumbing.md` ✅ | Wrapped Navigation, HTTP, Localization, and Serialization | The four core helper families and how each quietly adds customer-context and login info for you. |
| `013_custom-or-standard.md` ✅ | Custom Helper or Standard Call? | A side-by-side "use this, not that" guide mapping each stock .NET call to the helper you should reach for. |
| `014_state-container.md` ✅ | The Living State Container | The single in-browser object that holds the current user, customer, and cached lists — the app's heartbeat. |
| `015_listening-for-change.md` ✅ | Listening for Change Without Leaking | How screens subscribe to "something changed" signals and clean up after themselves so memory doesn't leak. |
| `016_tenant-aware-thinking.md` ✅ | Everything Knows Its Tenant | The multi-customer mindset that runs through navigation, data, and helpers. |
| `017_click-to-database.md` ✅ | Following a Click to the Database | One button press traced end-to-end — screen → helper → server → database and back — to cement the architecture. |

### 02x — The Data Stack: DTOs, Access, Controllers, and Models
| Doc | Title | Covers |
|-----|-------|--------|
| `021_data-stack-anatomy.md` ✅ | Anatomy of the Layered Data Stack | One record followed through every layer from data-shape to database table. |
| `022_nested-partial-dtos.md` ✅ | Shaping Records With Nested Partial DTOs | How the data-shape classes are organized so your fields and the framework's never collide. |
| `023_partial-data-access.md` ✅ | Inside the Partial Data-Access Layer | Where your database queries belong so a framework upgrade doesn't overwrite them. |
| `024_api-controllers.md` ✅ | API Controllers: The Tenant-Aware Request Surface | How the server endpoints expose data and enforce per-customer scoping. |
| `025_ef-models-soft-delete.md` ✅ | EF Models and the Records That Never Truly Vanish | The database-model conventions and the "soft delete" approach that keeps deleted data recoverable. |
| `026_standard-result.md` ✅ | The Standard Pass/Fail Result | The shared success/failure object every operation returns, so callers handle outcomes the same way everywhere. |

### 03x — Core Craft: Everyday Screens and Components
| Doc | Title | Covers |
|-----|-------|--------|
| `031_crud-templates.md` ✅ | List and Edit, the House Pattern | The standard "list page" and "edit page" templates most screens are stamped from. |
| `032_shared-components.md` ✅ | Building From the Shared Component Shelf | The library of ready-made UI pieces and how to compose them instead of reinventing. |
| `033_rich-components.md` ✅ | Charts, Editors, Signatures, and Graphs | The heavyweight components: charts, code editor, signature pad, network diagrams. |
| `034_multistep-wizard.md` ✅ | Leading Users Through a Multi-Step Wizard | The step-by-step flow component, passing state between steps, and validating each step. |
| `035_validation-localization-a11y.md` ✅ | Validated, Translated, and Reachable | Form validation, translation, and accessibility as the baseline every screen must meet. |

### 04x — Extending Without Breaking: The Live Runtime
| Doc | Title | Covers |
|-----|-------|--------|
| `041_upgrade-safe-model.md` ✅ | Code the Framework Can Update Underneath | The partial-class "segregation contract" that lets the framework update beneath your code. |
| `042_file-naming-law.md` ✅ | The Naming Law That Keeps Your Code Yours | The strict file-naming rule that separates your files from framework files. |
| `043_plugin-model.md` ✅ | Pluggable by Design: Authoring Plugins | The plug-in surface and the contract a plug-in fulfils to attach to the framework. |
| `044_auth-plugin.md` ✅ | The Authentication Plugin at the Tenant Edge | How the login plug-in establishes who the user is and which customer they belong to. |
| `045_background-service.md` ✅ | Work That Outlives a Click | Running deferred and recurring jobs via the background-processing plug-in and service. |
| `046_realtime-signalr.md` ✅ | Pushing State Live Over SignalR | How the server pushes live updates into the state container, and how to broadcast your own safely. |
| `047_custom-components.md` ✅ | Growing the Shared Library | Authoring new reusable components that respect the wrapper, state, and style conventions. |

### 05x — The House Style: Code Conventions
*`051`–`054` cover the cross-cutting conventions and how they're enforced; **`055`–`059` are the per-language deep references** (one per language we actually write — C#, Razor/Blazor/HTML, CSS, JavaScript, SQL), each rule shown with a verbatim example linked to the real source line, plus a FAQ. Written for an intern getting caught up on our standards.*
| Doc | Title | Covers |
|-----|-------|--------|
| `051_house-code-style.md` ✅ | The Author House Style | The opinionated rules for braces, capitalization, empty-string handling, and field naming, sourced from the framework's author. |
| `052_files-and-comment-voice.md` ✅ | Where Code Lives and How Comments Sound | File-organization conventions and the consistent voice our code comments use. |
| `053_editorconfig-enforcement.md` ✅ | The Machine Referee: editorconfig and What It Enforces | The shared settings file that auto-applies style — and the few places the author's hand overrides it on purpose. |
| `054_fork-sync-discipline.md` ✅ | Living on a Fork: Staying in Sync Upstream | Keeping our copy aligned with the original author's repo without drift or lost work. |
| `055_csharp-style-reference.md` ✅ | The C# Style Reference | The complete, citation-backed C# standard: whitespace, braces, naming, types/nullability, member ordering, partials & the `.App.` convention, methods/async, control flow, comments, and error handling — every rule linked to real source. |
| `056_razor-blazor-style-reference.md` ✅ | The Razor / Blazor / HTML Style Reference | Writing `.razor` files: file anatomy & directives, the `.App.razor` override layer, markup/HTML, components & binding, and the `@code` lifecycle + DataModel-subscription pattern. |
| `057_css-style-reference.md` ✅ | The CSS Style Reference | The Bootstrap-first reality and our small hand-written CSS surface: the `site`/`themes`/`tags` + `.App` stylesheet layering, scoped `.razor.css`, and code style. |
| `058_javascript-style-reference.md` ✅ | The JavaScript Style Reference | Why there's almost no JS and no TypeScript: the collocated `.razor.js` interop-module convention, how C# loads/calls it, and JS code style. |
| `059_sql-style-reference.md` ✅ | The SQL Style Reference | EF-first, plus the idempotent multi-engine migration system — existence-guarded DDL per dialect (SQL Server/SQLite/PostgreSQL/MySQL) and running raw SQL through EF safely. |

### 06x — The Team Operating System: How We Decide
*The collaboration mechanics. Several are marked 🛠 because their process should ship as a reusable `/command`.*
| Doc | Title | Covers |
|-----|-------|--------|
| `061_roleplay-team.md` ✅ 🛠 `/roleplay` | The Roleplay Team and Its Roles | The named team (Architect, Backend, Frontend, Quality, Sanity, JrDev, CTO) and how multi-voice discussion surfaces blind spots. |
| `062_discussion-planning-modes.md` ✅ | Discussion Mode, Planning Mode | When to explore a problem with the team vs. when to run a tight execution checklist. |
| `063_focus-group.md` ✅ 🛠 `/focus-group` | Running a Focus Group | The deep version: the team debates a specific decision to a conclusion, with sanity checks, and writes it up. |
| `064_sanity-check.md` ✅ 🛠 `/sanity-check` | The Sanity Check | One skeptical pass — "are we overcomplicating this? what did we miss?" — before committing. |
| `065_ask-one-role.md` ✅ 🛠 `/ask-role` | Consulting a Single Role | Putting a question to *one* team member (e.g. "ask Backend about X") instead of the whole room. |
| `066_explain-to-the-cto.md` ✅ 🛠 `/explain-to-cto` | Explaining to the Intern-CTO | The mechanism for re-explaining anything technical for an authority-holding, intern-level reader — jargon defined, decision spelled out. The Reader Model, operationalized. |
| `067_decisions-and-briefs.md` ✅ | Decision Records and the CTO Brief | Capturing *why* a choice was made, and the one-page brief that hands a decision to the CTO. |
| `068_template-library.md` ✅ | The Documentation Template Library | Copy-paste skeletons for meetings, decisions, features, runbooks, and reviews. |

### 07x — Analyzing Apps With FreeTools
| Doc | Title | Covers |
|-----|-------|--------|
| `071_freetools-orientation.md` ✅ | Meet the Analysis Suite | What the FreeTools command-line helpers inspect, and when to reach for each. |
| `072_install-and-invoke.md` ✅ | Up and Scanning: Pointing the CLI at an App | Installing the tool, aiming it at a target app, and reading its output. |
| `073_route-discovery.md` ✅ | Mapping Every Route | The command that lists every reachable page in an app, and what to do with the inventory. |
| `074_headless-screenshots.md` ✅ | Screenshots Without a Browser Window | Capturing visual snapshots of every page automatically, with no human clicking. |
| `075_ada-scanning.md` ✅ | Sweeping for ADA and Accessibility Gaps | Running the accessibility scanner and turning its findings into fixes. (ADA = the U.S. accessibility law.) |
| `076_tooling-core-library.md` ✅ | Building On the FreeTools Core Library | The shared utility code under the commands, and reusing it for your own analysis scripts. |

### 08x — Operate, Deploy, and Steward
| Doc | Title | Covers |
|-----|-------|--------|
| `081_is-it-for-us.md` ✅ | The Fit Test: Is This Framework Right for Us? | A plain-language, honest account of what it does and doesn't solve, plus learning-curve and lock-in risks. |
| `082_tenancy-topology.md` ✅ | One Tenant or Many | Choosing between single-customer and multi-customer setups, and what each costs. |
| `083_deployment-shapes.md` ✅ | Shipping It to Production | Deploying the browser app and the server across the supported setups. |
| `084_performing-upgrades.md` ✅ | Riding the Framework Forward | Taking a framework upgrade safely using the extension model and sync discipline. |
| `085_diagnostics-playbook.md` ✅ | When Things Go Sideways | A "symptom → cause → fix" troubleshooting guide across the common failure areas. |
| `086_performance-at-scale.md` ✅ | Keeping It Fast at Scale | Tuning cached lists, live-update traffic, and queries under heavy multi-customer load. |
| `087_security-and-roadmap.md` ✅ | Trust and Trajectory | The security posture across FreeCRM and FreeTools, plus where the project is headed. |
| `088_contributing-back.md` ✅ | Becoming a Steward | How an experienced practitioner contributes fixes and features back upstream. |

**Total: 62 planned docs across 9 bands** (this index is the 63rd file).

---

## Skill Candidates

These docs describe a repeatable process that should *also* become a Claude Code slash-command, so you can invoke it with one word instead of re-describing it. (A "skill" = a saved instruction set you trigger by typing `/name`.)

| Command | From doc | What typing it would do |
|---------|----------|-------------------------|
| `/roleplay` | `061_roleplay-team.md` | Convene the named team to talk a problem through from multiple angles. |
| `/focus-group` | `063_focus-group.md` | Run the team to a *decision*, with mid- and final sanity checks, then write it up. |
| `/sanity-check` | `064_sanity-check.md` | One skeptical pass to catch over-engineering and gaps before committing. |
| `/ask-role` | `065_ask-one-role.md` | Ask a single named role (e.g. Backend) a targeted question. |
| `/explain-to-cto` | `066_explain-to-the-cto.md` | Re-explain anything technical for an intern-level decision-maker, decision spelled out. |

**These five are the recommended first build batch.** When we build them, one open choice for you (intern-CTO style — here's the decision and why): they can live **project-only** (available just here) or **global** (available in every project on this machine). Global suits the roleplay/explainer ones since you'll likely reuse them everywhere; we can decide per-skill.

---

## Conventions for This Folder

- **Numbering:** band `0Nx`; the band's index/overview ends in `0`, docs count up `1–8`. Gaps are fine; never renumber an existing doc.
- **Filenames:** `0NN_kebab-slug.md`.
- **Originality:** GuidesV2 content is written fresh. Reuse *ideas*, never *text*, from the old `Guides/` set.
- **Reader Model is mandatory:** every doc follows the intern-CTO writing standard above.
- **Status markers:** flip ✅ → 🟢 as each doc is written; add new docs in the same PR that creates them.
- **Adding a skill?** Update the [Skill Candidates](#skill-candidates) table when a doc's process graduates into a `/command`.

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
*Origin: cleanroom design panel (4 independent taxonomies → synthesized backbone), then hardened for the Team-Operating-System and intern-CTO directives.*
