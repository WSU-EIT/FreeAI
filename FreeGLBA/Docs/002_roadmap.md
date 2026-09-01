# FreeGLBA -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Recently completed

- [x] **Data-ownership tracking** — live owner fields + `DataOwnerships` history on source systems,
      owner-at-time-of-access snapshot on every event (caller-supplied or auto-captured), ownership
      history in the Source System editor, snapshot-vs-current comparison with a "changed hands"
      indicator on event detail views, and optional `dataOwner*` fields on the ingest API and
      NuGet client
- [x] Real-time dashboard push — `GlbaAccessEvent` / `GlbaSourceSystem` SignalR update types publish
      from all event-write paths (batches coalesce into one update); the dashboard and list pages
      refresh in place
- [x] Database indexes on `AccessEvents` — `AccessedAt`, `UserId`, `SubjectId`, and a non-unique
      `(SourceSystemId, SourceEventId)` index for the dedupe lookup
- [x] Fixed `DataSubject` statistics — `UniqueAccessorCount` is recalculated from events (exact in
      single-event paths, monotone floor in bulk paths); `First/LastAccessedAt` now use real event
      times instead of ingest time; deleting an event recomputes instead of incrementing
- [x] Dashboard stats completeness — `TotalAccessors`, `ByAccessType`, and `ByCategory` are now
      populated and displayed
- [x] Source-system delete guard — systems with recorded events can no longer be deleted (the FK
      cascade would have silently destroyed the audit trail); deactivate instead
- [x] API Explorer page — interactive sample requests with a pasted API key, dedupe (409) demo,
      and live-updating query samples
- [x] Breadcrumbs on all GLBA pages, deep-linkable filters
      (`AccessEvents?userId=…&subjectId=…&system=…`, `Accessors?search=…`), and cross-links between
      accessors, subjects, events, and source systems
- [x] InMemory seeding, Access Event create/edit UI, bulk test-data generation, bulk insert API

- [x] **Compliance report generation** — the Compliance Reports page now downloads a QuestPDF
      summary (statistics, breakdowns, top accessors, source systems with current data owners)
      and a full CSV detail export of every event in the period; generation refreshes the stored
      statistics and summary JSON and stamps who generated it
- [x] **Endpoint authorization** — all `api/Data/*` GLBA endpoints now require a signed-in,
      enabled user; configuration and destructive operations require Admin (previously these
      endpoints had no auth checks at all)

- [x] **Anomaly detection & insights** — `GET api/glba/stats/insights` computes per-user volume
      spikes (>3× the user's 30-day daily average), large bulk exports (50+ subjects), first-time
      accessors, and source systems with no data owner; surfaced on the dashboard as a
      "Needs Attention" card with severity badges and deep links
- [x] **Access-volume trend chart** — 30-day daily columns (with the export/download portion
      overlaid) on the dashboard, rendered as dependency-free inline SVG
      (`FreeGLBA.App.TrendChart.razor`) after code.highcharts.com proved unreliable to load
- [x] **Unique constraint on `(SourceSystemId, SourceEventId)`** — filtered unique index on
      SQL Server/PostgreSQL/SQLite (`OnModelCreatingPartial`); a concurrent duplicate now surfaces
      as a clean 409 instead of a 500 (MySQL and InMemory keep the non-unique index because they
      cannot enforce a filtered unique index safely)
- [x] **xUnit test suite** — `FreeGLBA.Tests` runs 15 tests against the InMemory DataAccess with
      no server or API key: ownership lifecycle, dedupe, subject statistics, delete guards,
      PDF/CSV generation, and insights (`dotnet test FreeGLBA.Tests`)
- [x] Keyboard-accessible sorting — all sortable table headers on the GLBA list pages are
      focusable, respond to Enter/Space, and expose `aria-sort`
- [x] **Webhook alert delivery** — large bulk accesses and (optionally) after-hours events POST
      immediately to a configured Slack/Teams-compatible webhook; configured on the new
      GLBA Settings page with a send-test button; delivery is detached and best-effort
- [x] **Institution timezone + after-hours detection** — configurable timezone, business hours,
      and weekend policy on GLBA Settings; a weekly after-hours insight on the dashboard and an
      optional immediate alert
- [x] **Tamper-evident hash chain** — every ingested event is stamped with `ChainSequence`,
      `PrevRowHash`, and a SHA-256 `RowHash` per source system; one-click verification from
      Source Systems detects modified rows (ContentMismatch), broken links, and deletions
      (SequenceGap); covered by unit tests including a direct-database tamper simulation.
      Known limitation: sequence assignment is serialized in-process, so multiple app instances
      ingesting concurrently can fork a chain
- [x] **Subject access-history export** — one click on a data subject's detail panel produces a
      DSAR/audit-style PDF of every recorded access to that person's data (direct and via bulk
      exports), including the data owner at the time of each access

## Near-term

- [ ] Dashboard period tiles still use UTC day/week/month boundaries — switch them to the
      institution timezone now that the setting exists

## Medium-term (features common in commercial audit/compliance tools)

- [ ] Role-based access control for who can view audit logs vs. configure the system
- [ ] Email delivery for alerts (webhook delivery shipped — see Recently completed)
- [ ] SIEM export — stream every event to Splunk/Sentinel via syslog/CEF, beyond the
      threshold-triggered webhook alerts that exist today
- [ ] Cross-instance chain continuity — sequence assignment for the tamper-evident chain is
      serialized in-process; multiple app instances need a database-level sequence or lock
- [ ] Periodic access reviews / attestation campaigns (owner certifies who should retain access)
- [ ] Digital signing of exported PDFs (the subject access-history export itself shipped — see
      Recently completed)
- [ ] Integration with Banner / PeopleSoft for automatic subject resolution
- [ ] Multi-tenancy for the GLBA tables — `AccessEvent`, `SourceSystem`, `DataSubject`, and
      `ComplianceReport` have no `TenantId`, so reads are not tenant-filtered

## Long-term

- [ ] FERPA access tracking alongside GLBA (combined compliance dashboard)
- [ ] Retention policy automation (auto-purge or archive events older than N years per policy)
- [ ] Data lineage — track where a subject's data flows between systems, not just who viewed it