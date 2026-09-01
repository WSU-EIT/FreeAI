# FreeGLBA -- Feature Inventory

> **Category:** Features
> **Purpose:** Complete catalog of what this project can do today.

---

## Core features

- [x] GLBA access event logging (who, what, when, why)
- [x] Data-ownership tracking -- live data owner per source system, immutable
      owner-at-time-of-access snapshot per event, and a full `DataOwnerships`
      history table ("who owned this data at time T"), with change indicators in the UI
- [x] Bulk access tracking -- log access to multiple subjects in one event
- [x] Real-time dashboard with access pattern statistics (live SignalR refresh)
- [x] 30-day access-volume trend chart (dependency-free inline SVG)
- [x] Anomaly detection -- volume spikes, large bulk exports, first-time accessors, and
      ownership-coverage gaps surfaced on the dashboard with deep links
- [x] Webhook alerts (Slack/Teams-compatible) for large bulk accesses and after-hours access,
      configured on the GLBA Settings page with institution timezone and business hours
- [x] Tamper-evident audit trail -- per-source SHA-256 hash chain on every event with one-click
      verification (detects modified rows, broken links, and deletions)
- [x] xUnit test suite (`FreeGLBA.Tests`, 21 tests, no server required)
- [x] Compliance reports -- downloadable QuestPDF summary and full CSV detail export per period
- [x] REST API for external system integration
- [x] In-app API Explorer with clickable sample requests (`/ApiExplorer`)
- [x] NuGet client library (`FreeGLBA.NugetClient`) for easy integration
- [x] Multi-tenant authentication (cookie, Google, Microsoft, Apple, Facebook, OpenID Connect)
- [x] User and department management
- [x] Roslyn dynamic plugin system
- [x] SignalR real-time notifications
- [x] EF Core multi-database (SQL Server, PostgreSQL, SQLite, InMemory)
- [x] Test clients (project reference and NuGet package variants)

## Configuration

| Setting | Description |
|---------|-------------|
| `DatabaseType` | InMemory / SQLServer / PostgreSQL / SQLite |
| `ConnectionStrings:AppData` | Database connection string |
| `AzureSignalRurl` | Azure SignalR endpoint (blank = local) |

## Known limitations

- The GLBA tables carry no `TenantId`; single-tenant deployments only
- No unique constraint on `(SourceSystemId, SourceEventId)`; concurrent retries of the same
  event can race past deduplication