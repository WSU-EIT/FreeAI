# FreeGLBA -- Roadmap

> **Category:** Roadmap
> **Purpose:** Planned and potential future work.

---

## Recently completed

- [x] InMemory seeding — `SeedTestData()` creates the admin user and tenants on first run;
      `admin`/`admin` works out of the box
- [x] Access Event create/edit UI — the `EditAccessEvent` dialog is now wired into the
      Access Events page (it existed but was referenced by nothing)
- [x] Bulk test-data generation — **Generate Test Events** creates randomized events across a
      configurable date range and subject pool
- [x] Bulk insert API — `SaveAccessEventsAsync` / `POST api/Data/SaveAccessEvents` writes up to
      1,000 events per round trip with aggregated statistics updates

## Near-term

- [ ] CSV and PDF export of audit logs — the `ComplianceReports` table stores metadata, but
      nothing generates report content; QuestPDF is referenced and licensed but unused
- [ ] Real-time dashboard push — add a GLBA `SignalRUpdateType`, publish from the event-processing
      path, and handle it in the empty `ProcessSignalRUpdateApp` hook
- [ ] Database indexes on `AccessEvents` — `AccessedAt`, `UserId`, `SubjectId`, plus a **unique**
      index on `(SourceSystemId, SourceEventId)` to close the de-duplication race and remove the
      full table scan on every ingest
- [ ] Access pattern anomaly detection (flag unusual access volumes)
- [ ] Fix `DataSubject.UniqueAccessorCount`, which is set to 1 on creation and never recalculated

## Medium-term

- [ ] Integration with Banner / PeopleSoft for automatic subject resolution
- [ ] Role-based access control for who can view audit logs vs. configure the system
- [ ] Webhook notifications when access thresholds are exceeded
- [ ] Multi-tenancy for the GLBA tables — `AccessEvent`, `SourceSystem`, `DataSubject`, and
      `ComplianceReport` have no `TenantId`, so reads are not tenant-filtered
- [ ] Replace the console-app "test clients" with a real xUnit/NUnit suite that can run in CI
      without a live server

## Long-term

- [ ] FERPA access tracking alongside GLBA (combined compliance dashboard)
- [ ] Retention policy automation (auto-purge events older than N years per policy)