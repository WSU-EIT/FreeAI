# FreeGLBA — Engineering Handoff

> **Date:** September 1, 2026
> **Scope:** One extended working session that took FreeGLBA from "ingestion works, most other
> features scaffolded" to a feature-complete, tested, verified compliance product.
> **Status:** All changes are uncommitted in the working tree, awaiting review. Nothing was pushed.
> **Illustrated companion:** the [field report artifact](https://claude.ai/code/artifact/33dbb241-138b-495e-9ab0-49672829afa9)
> shows every feature below running in the live app (19 screenshots, captured by automation).

---

## 1. The mandate

The session started from four asks:

1. **Deep-dive and polish the project** — "make it 100% perfect," close the gaps against
   commercial audit/compliance tools.
2. **Data-ownership tracking** (the boss's-boss request): for every access we record, know *who
   owns the data the request is about* — a point of contact for the data itself, not just who
   requested it — both **as of the time of access** (snapshot) and **now** (live).
3. **Prove it works**: real screenshots from the running app with real test data, a
   conference-style clickable API sample page, genuinely live SignalR updates, accurate stats,
   breadcrumbs/cross-links, and a11y polish. "Simple is better than complex; clear and precise
   elegance is the goal."
4. **Verify every GLBA claim** in the app's help text against primary sources.

## 2. What we built, why, and how

### 2.1 Data-ownership tracking (the headline feature)

**Why:** Compliance staff need a point of contact *for the data*, and auditors ask both "who owned
it when this happened" and "who owns it now." One field can't answer both, so the model has three
parts:

| Part | Where | Answers |
|------|-------|---------|
| **Snapshot** (immutable) | `DataOwnerName/Email/Department` on every `AccessEvent`, captured at ingest — caller-supplied `dataOwner*` fields win, otherwise auto-copied from the source system | "Who owned this data **then**?" |
| **Live** | `DataOwner*` fields (+ phone, assigned-date) on `SourceSystem` | "Who owns it **now** — who do I call today?" |
| **History** | New `DataOwnerships` table: one row per ownership period, `EndedAt == null` = current, stamped with who recorded the change | "Who owned it at **any time T** — even periods with no events?" |

**How:** owner-change detection in `SaveSourceSystemAsync` closes the open history row and opens a
new one; all three event-write paths capture the snapshot; every read path serves snapshot + live
side by side. The UI shows an Ownership History timeline in the source-system editor, a Data
Ownership panel on event details with an *"ownership has changed hands since this access was
recorded"* indicator, a Data Owner column on lists and the dashboard, and a "Not set" warning for
uncovered systems. The API and NuGet client gained optional `dataOwnerName/Email/Department`
fields — existing integrations get snapshots with **zero code changes**.

Files: `FreeGLBA.EFModels/EFModels/FreeGLBA.App.{SourceSystem,AccessEvent,DataOwnership}.cs`,
`FreeGLBA.DataAccess/FreeGLBA.App.DataAccess{,.ExternalApi}.cs`, DTOs/endpoints in
`FreeGLBA.DataObjects`, `EditSourceSystem`/`EditAccessEvent`/list pages, NuGet client model.

### 2.2 Real-time SignalR updates

**Why:** The dashboard claimed to be real-time; nothing published. **How:** new
`GlbaAccessEvent`/`GlbaSourceSystem` update types publish from every write path via a guarded
`NotifyGlbaChangeAsync` (a relay failure can never break ingestion; batches coalesce into one
update). Client pages subscribe through a new `Model.OnGlbaUpdate` event and refresh in place.
**Proof:** an API-posted event appeared at the top of the open Access Events page with no reload
(field report, Exhibit 11).

### 2.3 Compliance report generation (was a stub)

**Why:** The Reports page stored metadata only; the download button was a TODO; the docs claimed
"exportable reports." **How:** `FreeGLBA.App.DataAccess.Reports.cs` (QuestPDF, already licensed):

- **PDF summary** per period — statistics, access-type/category breakdowns, top accessors, source
  systems *with current data owners*, and the 16 CFR 314.4(c)(8) citation.
- **CSV detail** — every event in the period including the owner-snapshot columns.
- **Subject access-history PDF** (DSAR/audit-style) — one click on a data subject produces every
  recorded access to that person, direct *and* via bulk exports, with owner-at-time-of-access.

Generation refreshes the stored statistics/summary JSON and stamps who generated it. All three
were verified by downloading through the real UI buttons and byte-checking the results.

### 2.4 API Explorer (`/ApiExplorer`, admin)

**Why:** Asked for explicitly — a conference-style page of clickable samples. **How:** paste a
source-system API key; samples fire real requests (single event → 201, resend → 409 dedupe, bulk
export, and user-session query calls) and print status, timing, and pretty JSON. It lives in the
app because the samples must reach the server; it doubles as an integration-testing tool.

### 2.5 Dashboard: accurate, complete, visual

- **Fixed stats:** `TotalAccessors`, `ByAccessType`, `ByCategory` existed in the API but were
  never populated; `SubjectsThisMonth` was computed but never displayed. All shown now, in a
  symmetric 8-tile layout.
- **Trend chart:** 30-day daily columns with the export share overlaid — hand-built **inline SVG**
  (`FreeGLBA.App.TrendChart.razor`). We tried the framework's Highcharts component first; its CDN
  (code.highcharts.com) answers 403 and it's a proprietary runtime dependency, so the chart owes
  nothing to third parties and works offline. (Razor gotcha: `<text>` is reserved — SVG text is
  emitted via `MarkupString`.)
- **Needs Attention (anomaly detection):** `GET api/glba/stats/insights` computes per-user volume
  spikes (>3× the user's 30-day daily average), bulk exports touching 50+ subjects, first-time
  accessors, after-hours access, and unowned source systems — each rendered with a severity badge
  and a deep link into the filtered view.

### 2.6 Alerts, GLBA Settings, institution timezone

**Why:** Detection without delivery is half a feature. **How:** a new **GLBA Settings** page
(`/GlbaSettings`, admin) stores `DataObjects.GlbaSettings` in the framework Settings table:
webhook URL + enable switch + bulk threshold + after-hours toggle + send-test button, and the
institution timezone / business hours / weekend policy that after-hours detection judges against.
Large bulk accesses fire a *critical* alert and after-hours events optionally a *warning*, POSTed
as `{"text": …, "severity": …}` — compatible with Slack and Teams incoming webhooks. Delivery is
**detached and best-effort** (`FreeGLBA.App.DataAccess.Alerts.cs`): settings are read while the
request's DbContext is alive, the HTTP send runs on a background task, and no failure can break or
slow ingestion.

### 2.7 Tamper-evident audit trail

**Why:** The UI said "audit records are immutable and cannot be deleted" — but nothing enforced or
even detected changes. **How** (`FreeGLBA.App.DataAccess.Integrity.cs`): every ingested event is
stamped with `ChainSequence`, `PrevRowHash`, and a SHA-256 `RowHash` over its immutable audit
fields, chained per source system under a static `SemaphoreSlim`. One-click verification (the
fingerprint button on Source Systems) detects three tamper classes: **ContentMismatch** (row
modified after ingest), **BrokenLink**, and **SequenceGap** (deletion). Two deliberate choices:
edits do **not** rehash — an edited audit record *should* fail verification; and pre-existing rows
(`ChainSequence 0`) are reported as "unhashed," not as errors. Unit tests include a
direct-database tamper simulation and a deletion-gap case; the live app verified "chain intact,
all 181 events."

### 2.8 Security and correctness fixes (found during the deep-dive)

| Severity | Finding | Fix |
|----------|---------|-----|
| **Critical** | All internal `api/Data/*` GLBA endpoints had **no auth checks** — anyone reaching the server could read the audit trail, insert/delete events, delete source systems | Every endpoint now requires a signed-in enabled user; config/destructive ops require Admin (framework's per-method guard idiom) |
| **Critical** | Deleting a source system **cascade-deleted its entire audit trail** on SQL databases while the UI claimed events were preserved | Deletion refused when events exist; honest UI messaging; deactivate instead |
| High | Deleting an access event **incremented** the subject's access count; `UniqueAccessorCount` frozen at 1 forever; first/last-access dates used ingest time | Subject statistics recomputed from stored events (exact in single paths; monotone floor in bulk paths) |
| High | Dedupe was a full-table-scan check with a race — concurrent retries could double-insert | Indexes on `AccessedAt`/`UserId`/`SubjectId`; filtered **unique** index on `(SourceSystemId, SourceEventId)` (SQL Server/PostgreSQL/SQLite) with the race loser returned as a clean 409 |
| Medium | Accessors' "View Events" link passed a query parameter the target page never read — it silently did nothing | Deep-link filters implemented (`AccessEvents?userId/subjectId/system/search`, `Accessors?search`) |
| Medium | `EFModelOverrides.cs` declares its DbContext partial in the wrong namespace — its Guid-conversion override has been dead code since generation | Documented in place; **deliberately not "fixed"** because reviving it would change column mappings under existing MySQL/PostgreSQL/SQLite databases |
| Low | Source-system editor's lower half unreachable (non-scrollable modal); stray Russian code comments | Fixed |

### 2.9 UX / a11y polish

Breadcrumbs on every GLBA page (shared `FreeGLBA_App_Breadcrumbs` component); cross-links wired
accessors ⇄ events ⇄ subjects ⇄ source systems; skip-to-content link and a real `<main>` landmark
in the layout; all 26 sortable column headers keyboard-operable with `aria-sort` and visible
focus; labeled inputs, `aria-hidden` decorative icons, `aria-live` response regions; the trend
chart carries a spoken summary.

### 2.10 GLBA accuracy verification

Every compliance claim in help text and docs was checked against
[16 CFR 314.4](https://www.law.cornell.edu/cfr/text/16/314.4), the
[FTC's GLBA page](https://www.ftc.gov/business-guidance/privacy-security/gramm-leach-bliley-act),
and [FSA's 2023 enforcement announcement](https://fsapartners.ed.gov/knowledge-center/library/electronic-announcements/2023-02-09/updates-gramm-leach-bliley-act-cybersecurity-requirements).
Confirmed accurate: GLBA is a 1999 law; Title IV institutions are financial institutions under the
Safeguards Rule, enforced through FSA and checked in the federal Single Audit (since June 9, 2023).
**Corrected:** the rule mandates monitoring/logging *who accessed what and when* (314.4(c)(8));
recording the *purpose* is best practice, not a literal requirement — the app said "requires …
and why." Also corrected: a "data subject access request" framing that belongs to GDPR/CCPA, and
vague "must track all systems" phrasing (now cites 314.4(c)(1) asset identification +
(c)(8) logging). The dashboard's About section now links the primary sources.

### 2.11 Test suite

`FreeGLBA.Tests` — **21 xUnit tests, all passing in ~0.5 s**, running the real `DataAccess` over
EF InMemory with no server or API key (`dotnet test FreeGLBA.Tests`). Coverage: ownership
lifecycle (snapshot, history, change detection, no-op saves), dedupe (including the race path),
exact subject statistics on insert/delete, bulk backdating, delete guards, PDF/CSV/subject-history
generation (byte-validated), insights, settings sanitization, and the hash chain (valid across
mixed insert paths; tamper and deletion detection). Constraint to know: the InMemory store name is
hardcoded, so the store is shared process-wide — tests live in one collection, use unique IDs, and
never assume empty tables.

### 2.12 Demo & verification tooling

`FreeGLBA.Showcase` (new project, see its README) drives the real UI headlessly: it produced every
screenshot in the field report and doubles as an end-to-end smoke test — login → create system →
capture API key → API Explorer 201/409 → batch-seed 180 realistic events → ownership transfer →
live SignalR arrival → report/subject-PDF downloads with byte validation → integrity verification.

## 3. How the core flows work now

**Ingest** (`POST /api/glba/events`, `ApiKeyMiddleware` → `ProcessGlbaEventAsync`):
validate → dedupe (unique-index-backed) → **owner snapshot** (caller fields or system's current
owner) → **chain stamp** (under `_chainLock`: sequence, prev-hash, row-hash) → save → subject
statistics recompute → **SignalR publish** (guarded) → **webhook alerts** (detached). The same
snapshot/chain/alert steps run in the internal single-save and bulk paths.

**Ownership change** (`SaveSourceSystemAsync`): normalized comparison of the four owner fields →
if changed, close open `DataOwnerships` row, open a new one stamped with the signed-in user,
update the live fields. Events never change — their snapshots are the historical record.

**Verification** (`VerifyAccessEventChainAsync`): walk a source's chained events in sequence
order; recompute each hash (ContentMismatch), check each link (BrokenLink), check contiguity
(SequenceGap = deletion); report unhashed legacy rows separately.

## 4. What's left to do (prioritized, with why)

1. **Timezone-aware dashboard tiles** — the tiles still use UTC day/week/month boundaries; the
   institution-timezone setting now exists, so this is a contained change in `GetGlbaStatsAsync`.
2. **Email delivery for alerts** — webhook delivery shipped; some offices live in email. The
   framework has email plumbing to reuse.
3. **Full SIEM export** — stream *every* event to Splunk/Sentinel (syslog/CEF), beyond
   threshold-triggered alerts.
4. **Cross-instance chain continuity** — chain sequencing is serialized in-process; multiple app
   instances ingesting concurrently can fork a chain. Needs a database-level sequence or lock.
5. **Access reviews / owner attestation** — periodic "certify who should retain access" campaigns;
   the ownership model was built to enable exactly this.
6. **RBAC for audit viewing** — today any signed-in user reads the audit trail; Admin gates config.
   A "compliance viewer" role is the natural next split.
7. **PDF signing** — the DSAR and compliance exports are unsigned.
8. **Multi-tenancy** — the GLBA tables carry no `TenantId`; single-tenant deployments only.
9. **Retention automation** — auto-archive/purge per policy (note: purging interacts with the hash
   chain; archive-then-truncate-with-anchor is the likely design).
10. **Banner/PeopleSoft subject resolution**, **FERPA companion tracking**, **data lineage** —
    long-term items, unchanged.

## 5. Known limitations & caveats (read before deploying)

- **Existing (non-InMemory) databases need the upgrade SQL** in the main README — the schema comes
  from the EF model via `EnsureCreated()`, which only builds *new* databases. New columns this
  session: `SourceSystems.DataOwner*` (5), `AccessEvents.DataOwner*` (3) +
  `ChainSequence`/`PrevRowHash`/`RowHash`, the `DataOwnerships` table, and four indexes (one
  filtered-unique). The `EFModels/Migrations` folder is vestigial — don't apply or regenerate it.
- **MySQL and InMemory** can't enforce the filtered unique dedupe index; they keep check-then-insert.
- **Bulk-path `UniqueAccessorCount`** is a floor (multi-subject JSON events aren't matched in the
  grouped recompute); single-event paths are exact.
- **Editing an access event breaks its hash on purpose.** If the team wants editable events *and*
  clean verification, that's a product decision to revisit — don't "fix" it casually.
- **Alert webhooks are at-most-once** — no retry/queue; by design they can't slow ingest.
- The framework's **Highcharts component is unused** by GLBA pages (CDN unreliable, proprietary);
  the SVG `TrendChart` component is the pattern to extend.
- `LocalModeUrl` must match the port you browse on (trailing slash), or login and the SignalR
  relay break.

## 6. Where things live

| Area | Files |
|------|------|
| Conventions & gotchas | `CLAUDE.md` (repo root of FreeGLBA) — read first |
| Ownership + entities | `FreeGLBA.EFModels/EFModels/FreeGLBA.App.*.cs` |
| Ingest, insights, stats | `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.ExternalApi.cs` |
| CRUD + ownership history | `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.cs` |
| Reports (PDF/CSV/DSAR) | `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.Reports.cs` |
| Alerts + settings | `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.Alerts.cs` |
| Hash chain | `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.Integrity.cs` |
| Internal API (guarded) | `FreeGLBA/Controllers/FreeGLBA.App.DataController.cs` |
| External API + insights endpoint | `FreeGLBA/Controllers/FreeGLBA.App.GlbaController.cs` |
| Pages & components | `FreeGLBA.Client/Pages/FreeGLBA.App.*.razor`, `Shared/AppComponents/FreeGLBA.App.*.razor` |
| Tests | `FreeGLBA.Tests/GlbaCoreTests.cs` |
| Demo/verification runner | `FreeGLBA.Showcase/` |

## 7. Verification summary

- `dotnet build FreeGLBA/FreeGLBA.csproj` — clean, 0 errors.
- `dotnet test FreeGLBA.Tests` — **21/21 passing**.
- End-to-end via `FreeGLBA.Showcase`: every feature above exercised through the real UI in a
  headless browser, with downloads byte-validated (PDF magic bytes; CSV row counts matched event
  counts exactly) and the SignalR live-arrival captured on screen.
- Screenshots and narrative: the
  [field report](https://claude.ai/code/artifact/33dbb241-138b-495e-9ab0-49672829afa9).

---

*Prepared as an engineering handoff for WSU-EIT. All work uncommitted, in the working tree, for
review. Questions about any decision above: each module carries comments explaining the "why" at
the point of the decision.*
