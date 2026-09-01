# FreeGLBA — notes for Claude

GLBA (Gramm-Leach-Bliley Act) access-audit system: Blazor WASM client + ASP.NET Core host on
.NET 10, generated from the FreeCRM framework template. External apps post access events with an
API key; compliance staff get a live dashboard, ownership tracking, and report exports.

## Layout and conventions

- App-specific code lives in `FreeGLBA.App.*` files; unprefixed files are FreeCRM framework code —
  prefer not to modify framework files except where the framework provides explicit hooks
  (`*.App.cs`, `ProcessSignalRUpdateApp`, `OnModelCreatingPartial`, etc.).
- Everything is partial classes: `DataObjects`, `IDataAccess`, `DataAccess`, `DataController`.
  Adding an entity means touching EFModels + DataObjects DTO + IDataAccess + DataAccess +
  DataController + endpoint constants in `FreeGLBA.DataObjects/FreeGLBA.App.Endpoints.cs`.
- **Schema comes from the EF entity model via `EnsureCreated()`** (`_useMigrations = false`).
  The `FreeGLBA.EFModels/Migrations` folder is stale/vestigial — do not regenerate or apply it.
  Schema changes = edit the entity classes; put hand-written upgrade SQL for existing databases
  in the README ("Upgrading an existing database" section).
- Model configuration hook: `OnModelCreatingPartial` implemented in
  `EFModels/FreeGLBA.App.EFDataModel.cs` (namespace is `FreeGLBA.EFModels.EFModels` — note the
  doubled segment). `EFModelOverrides.cs` declares the context in the WRONG namespace and is dead
  code; see the comment in that file before "fixing" it.
- Internal API pattern: `POST api/Data/<Verb><Entity>`; auth is per-method
  `if (!CurrentUser.Enabled) return Unauthorized();` (`CurrentUser.Admin` for destructive/config
  endpoints). There is no class-level `[Authorize]` on `DataController` — never add an endpoint
  without a guard.
- External ingest: `POST /api/glba/events[/batch]` behind `ApiKeyMiddleware` (Bearer key per
  SourceSystem; SHA-256 hash stored). Internal query endpoints on `GlbaController` use
  `[Authorize]`.
- SignalR: publish with `NotifyGlbaChangeAsync` (never let a relay failure break a write path);
  client pages subscribe to `Model.OnGlbaUpdate` and must unsubscribe in `Dispose`.
- Data ownership model: live owner fields on `SourceSystemItem`, immutable snapshot
  (`DataOwnerName/Email/Department`) captured on every `AccessEventItem` at ingest, and a
  `DataOwnerships` history table where `EndedAt == null` marks the current owner.
- Reports: QuestPDF (Community license set in Program.cs and in the test fixture) in
  `FreeGLBA.DataAccess/FreeGLBA.App.DataAccess.Reports.cs`.
- App-wide GLBA settings (`DataObjects.GlbaSettings`: webhook alerts, thresholds, institution
  timezone/business hours) live in the framework Settings table under the name "GlbaSettings" —
  `FreeGLBA.App.DataAccess.Alerts.cs`. Alert HTTP delivery is static/context-free and detached
  (`Task.Run`) so it can never break or slow ingest; settings are read while the DbContext is alive.
- Tamper-evident hash chain (`FreeGLBA.App.DataAccess.Integrity.cs`): every new event gets
  `ChainSequence`/`PrevRowHash`/`RowHash` per source system, assigned under the static
  `_chainLock` semaphore — any code path that inserts AccessEvents MUST assign chain positions
  under that lock and save before releasing. Edits of stored events intentionally do NOT rehash
  (verification flags them). ChainSequence 0 = legacy/unhashed rows. In-process lock only:
  multi-instance ingest can fork chains (roadmap item).
- Razor gotcha: `<text>` is reserved — emit SVG `<text>` elements via `MarkupString`
  (see `FreeGLBA.App.TrendChart.razor`).
- GLBA claims in help text must match the FTC Safeguards Rule precisely: 16 CFR 314.4(c)(8)
  requires monitoring/logging authorized-user activity (who/what/when); recording the *purpose*
  is best practice, not a literal requirement. Cite sections when adding compliance text.

## Build, run, test

```bash
dotnet build FreeGLBA/FreeGLBA.csproj                  # builds everything
dotnet run --project FreeGLBA/FreeGLBA.csproj --launch-profile https
#   -> https://localhost:7271, InMemory DB, login admin/admin (seeded on first run)
dotnet test FreeGLBA.Tests                             # 16 unit tests, no server needed
```

- `LocalModeUrl` in appsettings.Development.json must match the port you browse on (trailing
  slash required) or login redirects and the SignalR relay break.
- A running server locks bin DLLs — stop it before rebuilding.
- InMemory discards data on restart. Repopulate via Source Systems → New (Generate Test Data),
  then API Explorer (`/ApiExplorer`) or Access Events → Generate Test Events.
- The `FreeGLBA.Tests` InMemory store is shared process-wide (the store name is hardcoded);
  tests use unique IDs and never assume empty tables — keep new tests in the same collection.
