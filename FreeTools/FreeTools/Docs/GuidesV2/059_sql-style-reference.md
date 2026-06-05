# 059 — SQL and Database Coding Style: EF-First, Idempotent Migrations

> **Document ID:** 059  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Define how we touch the database — Entity Framework by default, disciplined raw SQL only for schema migrations and a few targeted queries — and the exact idempotent, injection-safe patterns that make it work.
> **Audience:** Contributors writing data-access or migration code  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [How to Read This Doc + the EF-First Principle](#ef-first) | What the key terms mean, and when raw SQL is (and is not) allowed |
| 2 | [The Migration System: Four Engine Files + One Runner](#migration-system) | The `DataMigrations.*.cs` files, the `DataMigration` carrier, and that they run on startup |
| 3 | [The Core Rule: Idempotent DDL](#idempotent-ddl) | Never `CREATE`/`ALTER` blindly — guard every schema change so re-running is a no-op |
| 4 | [Per-Dialect Guard Idioms](#guard-idioms) | `IF OBJECT_ID` vs `IF NOT EXISTS`, FK guards, index guards — the same change in multiple dialects |
| 5 | [Adding a New Migration and How the Runner Applies It](#adding-migrations) | How to author a migration and how the runner tracks/applies them in order |
| 6 | [Running Raw SQL Through EF Safely](#raw-sql-safely) | `ExecuteSqlRaw`/`SqlQueryRaw` and parameterization to stop SQL injection |
| 7 | [How We Write SQL Strings in C#](#sql-strings) | Raw string literals, keyword casing, formatting, comments |
| 8 | [Quick Reference](#quick-reference) | The ✗/✓ cheat sheet you can copy |
| 9 | [FAQ](#faq) | The real questions you will have on day one |
| 10 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="ef-first"></a>
## 1. How to Read This Doc + the EF-First Principle

Before any rules, learn the four terms that the rest of this document leans on:

- ***Entity Framework* (*EF*)** — the library that maps C# objects to database rows, so you rarely write SQL by hand. When you write `await data.Locations.AddAsync(rec)`, EF figures out the `INSERT` for you. We use EF Core.
- ***DDL*** — *Data Definition Language*: the SQL that **creates or changes the shape** of the database (`CREATE TABLE`, `ALTER TABLE`, `CREATE INDEX`). Contrast with *DML* (Data Manipulation Language) — `INSERT`/`UPDATE`/`DELETE`, which change the data inside the tables.
- ***Idempotent*** — safe to run repeatedly. An idempotent operation done twice has the same effect as doing it once. "Create this table if it does not already exist" is idempotent; a bare "create this table" is not (the second run errors).
- ***SQL injection*** — an attack where user-supplied input is smuggled into a query and executed as *code* instead of being treated as *data*. If a user types `'; DROP TABLE Users;--` into a search box and your code pastes it straight into a SQL string, you have just handed them a way to delete your tables.

**The single most important framing — EF-first.** Normal data access in this codebase is done through Entity Framework in C#, **not** hand-written SQL. EF gives you change-tracking, type safety, and queries that work the same across database engines, which covers roughly 95% of what you will ever do. You should reach for raw SQL only for the small set of things EF can't express well:

1. **Schema migrations (DDL)** — creating and altering tables/indexes/constraints. This is the big one, and it is why most of this doc is about the migration system.
2. **Set-based bulk writes** — "update every row where X" or "delete all child rows for this parent" — done in a single SQL statement instead of loading thousands of rows into memory just to flip one column.
3. **A couple of targeted reads** — querying a system table that isn't mapped as an EF entity (in practice, exactly one query: reading the migration-history table).

Everything else is EF. The proof that this is real and not aspirational: a search of the whole `CRM.DataAccess` project found raw SQL reached through **exactly two EF APIs** (`ExecuteSqlRaw`/`ExecuteSqlRawAsync` for writes, `SqlQueryRaw<T>` for that one read) and **zero** string-concatenated or `$"..."`-interpolated SQL anywhere — [DataAccess.Migrations.cs:25](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L25), [DataAccess.Migrations.cs:55](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L55).

So this doc is mostly about (a) the migration system and (b) the disciplined, safe way we drop to raw SQL when we must.

---

<a id="migration-system"></a>
## 2. The Migration System: Four Engine Files + One Runner

**Why it matters:** FreeCRM is designed so the *same* application can run on five different database back-ends — SQL Server, SQLite, PostgreSQL, MySQL, or an in-memory test database. Each of those engines speaks a slightly different dialect of SQL (different quote characters, different type names, different "create only if absent" syntax). EF Core ships its own migration system, but FreeCRM deliberately bypasses it and uses a **hand-written, multi-engine migration system** so the team has full control over the SQL each engine runs.

That system lives entirely in `FreeCRM/CRM.DataAccess`. It is **four hand-written migration files, one per database engine, plus one shared runner** that picks the right file and executes it:

| Engine | File |
|---|---|
| SQL Server | `FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs` |
| SQLite | `FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs` |
| PostgreSQL | `FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs` |
| MySQL | `FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs` |

**One partial class, one shared shape.** Each file is a `partial class DataMigrations` (a *partial class* is one class whose definition is split across several files) with its own `GetMigrations<Engine>()` method, and all four return the **same** type — `List<DataObjects.DataMigration>`. The SQL inside differs per engine, but the container is identical, which lets one dialect-agnostic runner work with all of them.

```csharp
public partial class DataMigrations
{
    public List<DataObjects.DataMigration> GetMigrationsSqlServer()
    {
        List<DataObjects.DataMigration> output = new List<DataObjects.DataMigration>();

        List<string> m1 = new List<string>();
```

— [DataMigrations.SQLServer.cs:3](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L3-L9). The other three are structurally identical: `GetMigrationsSQLite()` at [DataMigrations.SQLite.cs:3](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L3-L9), `GetMigrationsPostgreSQL()` at [DataMigrations.PostgreSQL.cs:3](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L3-L9), `GetMigrationsMySQL()` at [DataMigrations.MySQL.cs:3](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L3-L9).

> **A casing quirk to know about.** The SQL Server method is spelled `GetMigrationsSqlServer()` — lowercase "ql" — while the other three capitalize the whole engine acronym (`GetMigrationsSQLite`, `GetMigrationsPostgreSQL`, `GetMigrationsMySQL`). This matters because the runner's `switch` (Section 5) has to match the method name exactly. Don't "fix" it.

**The `DataMigration` carrier shape.** A migration is a trivial object: an ordered list of SQL strings plus a string id.

```csharp
public partial class DataMigration
{
    public List<string> Migration { get; set; } = new List<string>();
    public string MigrationId { get; set; } = String.Empty;
}
```

— [DataObjects.cs:163](FreeCRM/CRM.DataObjects/DataObjects.cs#L163-L167). The `Migration` list is the ordered SQL **steps**; `MigrationId` is the id used to track whether this migration has already been applied.

**One migration per file, today.** Each of the four files currently defines exactly **one** migration, with `MigrationId = "001"` (one `output.Add(...)` per file — [DataMigrations.SQLServer.cs:766](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L766), [DataMigrations.SQLite.cs:592](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L592), [DataMigrations.PostgreSQL.cs:615](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L615), [DataMigrations.MySQL.cs:611](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L611)). Inside that one migration, `m1` is a long list of individual SQL statements. Future migrations would be added as `m2`, `m3`, … and appended to `output` (see Section 5).

**They run on startup.** The runner is invoked when the database is opened at application startup — only when the database is not in-memory and migrations are enabled — `if (!_inMemoryDatabase && _useMigrations)` at [DataAccess.cs:93](FreeCRM/CRM.DataAccess/DataAccess.cs#L93-L94), and again in the `EnsureCreated()` fallback branch at [DataAccess.cs:103](FreeCRM/CRM.DataAccess/DataAccess.cs#L103-L104). That "run on every startup" fact is the reason the next section exists.

---

<a id="idempotent-ddl"></a>
## 3. The Core Rule: Idempotent DDL

**This is the single most important rule in the document. Read it twice.**

> **Never `CREATE` or `ALTER` blindly. Guard every schema change with an existence check, so re-running the migration on a database that already has that object is a no-op.**

**Why it matters (this is a correctness requirement, not a style preference):** the migrations run on **every application startup** (Section 2), against databases that may be brand-new *or* years old and fully built. If a migration contained a bare `CREATE TABLE [DepartmentGroups]`, then the *first* startup would create the table fine — and the *second* startup would throw "table already exists" and could abort. Guarding every statement so that "already done" means "do nothing" is what makes it safe to run the same migration over and over forever.

Here is the guarded form on SQL Server. The `IF OBJECT_ID(...) IS NULL` test asks "does this table not exist yet?" and only then runs the `CREATE TABLE`:

```sql
IF OBJECT_ID(N'[DepartmentGroups]') IS NULL
BEGIN
    CREATE TABLE [dbo].[DepartmentGroups](
        [DepartmentGroupId] [uniqueidentifier] NOT NULL,
        [DepartmentGroupName] [nvarchar](200) NULL,
        [TenantId] [uniqueidentifier] NOT NULL,
        [Added] [datetime] NOT NULL,
        ...
        CONSTRAINT [PK_DepartmentGroups] PRIMARY KEY CLUSTERED ([DepartmentGroupId] ASC)
        WITH (PAD_INDEX = OFF, ...) ON [PRIMARY]
    ) ON [PRIMARY]
END
```

— [DataMigrations.SQLServer.cs:119](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L119-L134). (The C# wrapper — `m1.Add(` and the opening `"""` — sits on lines 117–118; the SQL proper is lines 119–134.)

Every `CREATE TABLE` in all four files is guarded this way. There are no exceptions for tables. The one DDL category that is **not** self-guarded in the SQL is standalone `CREATE INDEX`, which relies on the runner's per-step try/catch instead — covered in Section 4 and Section 5, because it is the exception that proves the rule.

The guard *syntax* differs by engine — that's the next section.

---

<a id="guard-idioms"></a>
## 4. Per-Dialect Guard Idioms

The intent ("only if it doesn't already exist") is identical across all four engines, but the syntax is dialect-specific. Learning the four shapes below is most of what makes migration code feel readable.

### 4a. Create-table guard: `IF OBJECT_ID … IS NULL` (SQL Server) vs `CREATE TABLE IF NOT EXISTS` (the other three)

**Why the split:** SQL Server's T-SQL `CREATE TABLE` has **no** `IF NOT EXISTS` clause (in the version targeted here), so you must test separately with `OBJECT_ID` inside a procedural `IF … BEGIN … END` block. SQLite, PostgreSQL, and MySQL all support `IF NOT EXISTS` directly, so the guard collapses into the statement itself.

Here is the **same table** (`__EFMigrationsHistory`, the migration-tracking table) in all four dialects. Watch the quote characters and type names change while the intent stays the same.

**SQL Server** — procedural guard, `[bracket]` quoting:
```sql
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    )
END
```
— [DataMigrations.SQLServer.cs:12](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L12-L19).

**PostgreSQL** — inline `IF NOT EXISTS`, `"double-quote"` quoting:
```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
```
— [DataMigrations.PostgreSQL.cs:12](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L12-L17).

**MySQL** — inline `IF NOT EXISTS`, `` `backtick` `` quoting, plus a storage-engine suffix:
```sql
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
```
— [DataMigrations.MySQL.cs:12](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L12-L17).

**SQLite** — inline `IF NOT EXISTS`, `"double-quote"` quoting, `TEXT` columns, inline PK:
```sql
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
)
```
— [DataMigrations.SQLite.cs:12](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L12-L15).

> **Quoting and types differ per engine.** Identifiers are quoted with `[brackets]` (SQL Server), `` `backticks` `` (MySQL), and `"double quotes"` (PostgreSQL & SQLite). Type names differ too: a GUID (globally-unique id) is `uniqueidentifier` on SQL Server ([DataMigrations.SQLServer.cs:122](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L122)), `char(36)` on MySQL ([DataMigrations.MySQL.cs:22](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L22)), `uuid` on PostgreSQL ([DataMigrations.PostgreSQL.cs:22](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L22)), and `TEXT` on SQLite ([DataMigrations.SQLite.cs:21](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L21)). A boolean is `bit` / `tinyint(1)` / `boolean` / `INTEGER` respectively.

### 4b. Foreign-key / constraint guards

A *foreign key* (FK) is a column that points at the primary key of another table, enforcing "this row must reference a real parent." These also have to be guarded so re-running doesn't try to add a constraint that already exists.

**SQL Server adds FKs in a separate `ALTER TABLE` pass** (because the referenced table may not exist yet when this table is first created), so each FK gets its own catalog-view guard — `IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='…')`:

```sql
IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_Invoices_Tenants')
BEGIN
	ALTER TABLE [dbo].[Invoices]  WITH CHECK ADD  CONSTRAINT [FK_Invoices_Tenants] FOREIGN KEY([TenantId])
    REFERENCES [dbo].[Tenants] ([TenantId])
    ALTER TABLE [dbo].[Invoices] CHECK CONSTRAINT [FK_Invoices_Tenants]
END
```
— [DataMigrations.SQLServer.cs:648](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L648-L654).

**The other three engines fold the FK *inside* the guarded `CREATE TABLE`**, so it inherits the table's `IF NOT EXISTS` guard and needs no separate check:

```sql
CONSTRAINT "PK_Payments" PRIMARY KEY ("PaymentId"),
CONSTRAINT "FK_Payments_Invoices" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("InvoiceId"),
CONSTRAINT "FK_Payments_Tenants" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("TenantId"),
CONSTRAINT "FK_Payments_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId")
```
— [DataMigrations.PostgreSQL.cs:493](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L493-L496). SQLite does the same inline, e.g. [DataMigrations.SQLite.cs:323](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L323-L324) and [DataMigrations.SQLite.cs:344](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L344).

> **⚠ A real defect to know about (and not copy).** The **first six** SQL Server FK blocks are *inverted* — they use `IF EXISTS(...)` instead of `IF NOT EXISTS(...)`, which means those FKs are only added when they already exist (i.e. effectively never, on a fresh database). They live in the `Appointments` module at lines [560](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L560), 571, 583, 593, 604, and 614:
> ```sql
> IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_AppointmentNotes_Appointments')
> BEGIN
>     ALTER TABLE [dbo].[AppointmentNotes]  WITH CHECK ADD  CONSTRAINT [FK_AppointmentNotes_Appointments] ...
> ```
> Everything from line [625](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L625) onward correctly uses `IF NOT EXISTS`. The **intended** idiom is `IF NOT EXISTS`; the inverted blocks are a bug, not a pattern to follow.

### 4c. Column-existence checks before `ALTER … ADD`

The same "guard before you change" principle applies to adding a column to an *existing* table: you check whether the column is already present before running `ALTER TABLE … ADD`. On SQL Server this is the procedural catalog-check idiom (the same family as the FK guard above — `IF NOT EXISTS(... INFORMATION_SCHEMA ...)`); on the other engines the column is declared inside the guarded `CREATE TABLE IF NOT EXISTS`. The principle is identical to 4a/4b: never add a schema object that might already be there.

### 4d. Index guards — the deliberate exception

Plain `CREATE INDEX` statements are emitted **bare**, with no existence check:

```sql
CREATE INDEX "IX_Users_DepartmentId" ON "Users" ("DepartmentId");
```
— [DataMigrations.SQLite.cs:575](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L575). The same bare pattern appears in MySQL ([DataMigrations.MySQL.cs:545](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L545)) and PostgreSQL ([DataMigrations.PostgreSQL.cs:547](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L547)).

**Why no guard here?** A uniformly-available `CREATE INDEX IF NOT EXISTS` isn't guaranteed across all dialects the same way, and guarding each one would clutter the file. So the index statements lean on the **runner**, which wraps every step in try/catch and swallows the "already exists" error (Section 5). The net effect is still idempotent — a second run *does* throw on each index, but the error is caught. This is the one place the idempotency guarantee comes from the runner rather than from the SQL itself.

> SQL Server emits **no** standalone `CREATE INDEX` steps at all — it relies on its FK/constraint `ALTER` blocks instead.

### Dialect cheat-sheet (same guard, four engines)

| Concern | SQL Server | SQLite | PostgreSQL | MySQL |
|---|---|---|---|---|
| Create table if absent | `IF OBJECT_ID(N'[T]') IS NULL BEGIN CREATE TABLE … END` | `CREATE TABLE IF NOT EXISTS "T"` | `CREATE TABLE IF NOT EXISTS "T"` | `CREATE TABLE IF NOT EXISTS \`T\`` |
| Identifier quoting | `[Name]` | `"Name"` | `"Name"` | `` `Name` `` |
| Add FK if absent | guarded `ALTER TABLE` via `INFORMATION_SCHEMA.TABLE_CONSTRAINTS` | inline `CONSTRAINT … FOREIGN KEY` in `CREATE TABLE` | inline `CONSTRAINT … FOREIGN KEY` in `CREATE TABLE` | inline in `CREATE TABLE` |
| Create index | (none standalone; via guarded `ALTER`/constraint) | bare `CREATE INDEX` | bare `CREATE INDEX` | bare `CREATE INDEX` |
| Idempotent history insert | `IF NOT EXISTS(… WHERE MigrationId='001') INSERT …` | `INSERT … EXCEPT SELECT …` (unquoted ids) | `INSERT … EXCEPT SELECT …` (quoted ids) | `INSERT IGNORE INTO …` |

---

<a id="adding-migrations"></a>
## 5. Adding a New Migration and How the Runner Applies It

### How a migration records itself

The **last** step of every migration inserts its `MigrationId` into the `__EFMigrationsHistory` tracking table — and that insert is itself idempotent, so re-running it does nothing. This is the same "make it a safe no-op" idea applied to a row insert, and each engine uses its native safe-insert idiom.

**SQL Server** — `IF NOT EXISTS` guard around the `INSERT` (plus a guard that the history table exists):
```sql
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
BEGIN
	IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId]=N'001')
	BEGIN
		INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
		VALUES (N'001', N'1.0.0')
	END
END
```
— [DataMigrations.SQLServer.cs:755](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L755-L762).

**SQLite** — `INSERT … EXCEPT SELECT` (insert only the rows not already present), unquoted identifiers in the `SELECT`:
```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('001', '1.0.0')
EXCEPT
SELECT * FROM __EFMigrationsHistory WHERE MigrationId='001'
```
— [DataMigrations.SQLite.cs:585](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L585-L588).

**PostgreSQL** — the same `INSERT … EXCEPT SELECT` idiom, but with **fully quoted** identifiers and a trailing semicolon:
```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('001', '1.0.0')
EXCEPT
SELECT * FROM "__EFMigrationsHistory" WHERE "MigrationId"='001';
```
— [DataMigrations.PostgreSQL.cs:608](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L608-L611).

**MySQL** — `INSERT IGNORE` (a duplicate-key insert is silently dropped by the primary key):
```sql
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('001', '1.0.0');
```
— [DataMigrations.MySQL.cs:605](FreeCRM/CRM.DataAccess/DataMigrations.MySQL.cs#L605-L606).

After all the steps are assembled, the migration is packaged with its id and returned:
```csharp
output.Add(new DataObjects.DataMigration {
    MigrationId = "001",
    Migration = m1
});

return output;
```
— [DataMigrations.SQLServer.cs:765](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L765-L770); identical packaging in all four files ([DataMigrations.SQLite.cs:591](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L591-L596), [DataMigrations.PostgreSQL.cs:614](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L614-L619)).

> `__EFMigrationsHistory` is borrowed from EF Core's own convention, but here it is **hand-managed**, not managed by EF. The `ProductVersion` is hard-coded `'1.0.0'`.

### To add a new migration (the recipe)

1. In **each** of the four `DataMigrations.<Engine>.cs` files, build a new ordered `List<string>` (the next one would be `m2`), authoring the SQL in that engine's dialect.
2. **Order the steps so dependencies come first** — tables, then constraints/indexes, then the history-insert last. The runner executes steps in `m1.Add(...)` order, so authoring order *is* execution order.
3. **Guard every step** (Section 3/4) so it is a safe no-op on re-run.
4. End with the idempotent `__EFMigrationsHistory` insert that records the new id (e.g. `"002"`).
5. `output.Add(new DataObjects.DataMigration { MigrationId = "002", Migration = m2 });`

### How the runner applies and tracks migrations

A single `DatabaseApplyLatestMigrations()` does four things: reads which ids are already recorded, gets the engine-specific migration list, **skips any migration whose id is already recorded**, and runs each remaining step inside its own try/catch.

```csharp
var appliedMigrations = DatabaseGetAppliedMigrations();

var migrations = DatabaseGetMigrations();
if (migrations.Any()) {
    foreach (var migration in migrations) {
        if (!appliedMigrations.Contains(migration.MigrationId)) {
            if (migration.Migration.Any()) {
                foreach (var step in migration.Migration) {
                    try {
                        data.Database.ExecuteSqlRaw(step);
                    } catch (Exception ex) {
                        output.Messages.Add("Error Executing Migration " + migration.MigrationId.ToString());
                        output.Messages.AddRange(RecurseException(ex));
                    }
                }
            }
        }
    }
}
```
— [DataAccess.Migrations.cs:16](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L16-L34).

The right engine file is chosen from `_databaseType`:
```csharp
switch (_databaseType.ToLower()) {
    case "inmemory":
        break;

    case "mysql":
        output = migrations.GetMigrationsMySQL();
        break;

    case "postgresql":
        output = migrations.GetMigrationsPostgreSQL();
        break;

    case "sqlite":
        output = migrations.GetMigrationsSQLite();
        break;

    case "sqlserver":
        output = migrations.GetMigrationsSqlServer();
        break;
}
```
— [DataAccess.Migrations.cs:76](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L76-L95). The `"inmemory"` case is a deliberate no-op — an in-memory test database needs no migrations.

The applied-ids read uses raw SQL against the history table, with a PostgreSQL-specific schema-qualified variant:
```csharp
string query = "SELECT MigrationId FROM __EFMigrationsHistory";

if (_databaseType.ToLower() == "postgresql") {
    query =
        """
        SELECT "MigrationId"
        FROM public."__EFMigrationsHistory";
        """;
}
```
— [DataAccess.Migrations.cs:45](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L45-L53). This whole read is wrapped in a try/catch that swallows errors at [DataAccess.Migrations.cs:63](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L63-L65), so a missing history table on a brand-new database just yields an empty applied-list and all migrations run.

**Two design consequences worth internalizing:**

1. **Skip-by-id is per-*migration*, not per-*step*.** Within a not-yet-recorded migration, *every* step is attempted on every startup until the history row finally lands. That is exactly why each step must be individually idempotent (or its error must be catchable) — see the index exception in Section 4d.
2. **A failing step does not stop the others.** Errors are collected into `output.Messages`, and the overall result is "success only if zero messages" (`output.Result = output.Messages.Count() == 0;` at [DataAccess.Migrations.cs:36](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L36)). One bad step is logged, but later migrations still run.

---

<a id="raw-sql-safely"></a>
## 6. Running Raw SQL Through EF Safely

Outside the migration files, the *only* time you write raw SQL is for set-based bulk writes and the one history read. When you do, two rules govern everything.

### 6a. Stay in EF; drop to raw SQL only for bulk/cross-table writes

Use normal EF LINQ (`AddAsync` / `Remove` + `SaveChangesAsync`) for ordinary work. Switch to `ExecuteSqlRawAsync` only when you need to update or delete *many rows at once* without loading them into memory first.

```csharp
// If this is being set as the default location and it wasn't previously then update other records.
if (output.DefaultLocation == true && rec.DefaultLocation != true) {
    await data.Database.ExecuteSqlRawAsync("UPDATE Locations SET DefaultLocation=0 WHERE TenantId={0}", output.TenantId);
}
```
— [DataAccess.Locations.cs:198](FreeCRM/CRM.DataAccess/DataAccess.Locations.cs#L198-L201).

**Why not just use EF here?** This statement clears the "default" flag on every *other* location for a tenant in one server-side `UPDATE`. Loading every row into EF's change tracker just to flip one column would be slow and pointless. EF LINQ and raw SQL live **side-by-side in the same method** — the raw call is the deliberate exception, not the default. You can see the pairing clearly in user deletion, where `data.Users.Remove(rec)` at [DataAccess.Users.cs:227](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L227) sits beside `ExecuteSqlRawAsync("DELETE FROM AppointmentUsers ...")` at [DataAccess.Users.cs:231](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L231), and in the purge utility at [DataAccess.Utilities.cs:673](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L673). When a user is deleted, roughly 30 such `UPDATE`s run in a row to re-stamp audit columns across every table ([DataAccess.Users.cs:148](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L148-L210)).

### 6b. Always parameterize: `{0}`/`{1}` placeholders, never string concatenation

**This is the SQL-injection defense, and it is followed 100% in this codebase.** Put a numbered placeholder (`{0}`, `{1}`, …) where each value goes, and pass the actual variables as the following arguments. **Never** glue a variable into the SQL string with `+` or `$"..."`.

```csharp
await data.Database.ExecuteSqlRawAsync("UPDATE Appointments SET LastModifiedBy={0} WHERE LastModifiedBy={1}", displayName, UserId.ToString());
await data.Database.ExecuteSqlRawAsync("UPDATE Appointments SET AddedBy={0} WHERE AddedBy={1}", displayName, UserId.ToString());
```
— [DataAccess.Users.cs:150](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L150-L151).

**Why it works:** EF turns each `{0}` into a real bound *parameter* (a `DbParameter`) at the database driver level. The value travels separately from the SQL text, so it can **never** be interpreted as SQL — that closes the injection door entirely. (It also lets the database reuse a cached query plan.) Argument order matters: above, `displayName` fills `{0}` (the `SET` value) and `UserId.ToString()` fills `{1}` (the `WHERE` filter).

Two precision points:

- **Placeholders are bare `{0}` with no surrounding quotes**, even for text columns (`SET LastModifiedBy={0}`). EF supplies the parameter *and* its quoting; adding your own quotes would be wrong.
- **Literal constants are written directly** — `SET LocationId = NULL`, `SET DefaultLocation=0` — because `NULL`/`0`/`1` are constants in your code, not user data. Only *data* goes through placeholders. ([DataAccess.Locations.cs:200](FreeCRM/CRM.DataAccess/DataAccess.Locations.cs#L200), [DataAccess.Utilities.cs:673](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L673).)

A verified search of the whole project found **zero** raw-SQL strings built with concatenation or interpolation — this rule has no exceptions in runtime code. (The migration DDL strings in Section 2–5 are *not* parameterized, but that is safe for a different reason: they are hard-coded literals containing no user input at all.)

### 6c. Read raw scalars back with `SqlQueryRaw<T>`

When you must run a `SELECT` that EF's entity model doesn't cover, use `data.Database.SqlQueryRaw<T>(query)` and loop the results as plain objects, picking `T` to match the column type.

```csharp
string query = "SELECT MigrationId FROM __EFMigrationsHistory";

if (_databaseType.ToLower() == "postgresql") {
    query =
        """
        SELECT "MigrationId"
        FROM public."__EFMigrationsHistory";
        """;
}

var recs = data.Database.SqlQueryRaw<string>(query);
if (recs != null) {
    foreach (var rec in recs) {
        if (!String.IsNullOrEmpty(rec)) {
            output.Add(rec);
        }
    }
}
```
— [DataAccess.Migrations.cs:45](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L45-L62). This is the **only** `SqlQueryRaw` call in the project. The history table isn't a mapped EF entity, so LINQ over `DbSet`s can't reach it; `SqlQueryRaw<string>` maps the single returned column straight into `string` values with no hand-written `DbDataReader` code. It takes no user input, so a hard-coded literal is fine, and the whole thing is wrapped in try/catch because on a brand-new database the history table may not exist yet.

> **Not used anywhere in this codebase (verified by search):** `FromSqlRaw`, `FromSqlInterpolated`, `ExecuteSqlInterpolated`/`ExecuteSqlInterpolatedAsync`, `Database.GetDbConnection`, raw `CreateCommand`, and any string-concatenated or interpolated SQL. The two APIs above are the entire toolkit.

---

<a id="sql-strings"></a>
## 7. How We Write SQL Strings in C#

### 7a. Use raw string literals (`"""`), never verbatim `@"..."`

Multiline SQL goes inside C# 11 *raw string literals* — triple-quote delimiters `"""` — not the older verbatim `@"..."` form.

```csharp
List<string> m1 = new List<string>();
m1.Add(
    """
    IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
    BEGIN
        CREATE TABLE [__EFMigrationsHistory] (
            [MigrationId] nvarchar(150) NOT NULL,
            [ProductVersion] nvarchar(32) NOT NULL,
            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
        )
    END
    """);
```
— [DataMigrations.SQLServer.cs:9](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L9-L20).

**Why:** inside `"""..."""` you don't have to double-up quote characters, so quoted identifiers like `"__EFMigrationsHistory"` stay readable; and the compiler automatically strips the common leading indentation, so the SQL can line up under the C# without leaking whitespace into the query. A scan of the whole `CRM.DataAccess` project found **zero** `@"..."` SQL strings (the only `@"..."` literals in the project are two regexes and one mask, none of them SQL). Short, single-line parameterized statements (Section 6) are written as ordinary one-line `"..."` strings — the `"""` form is for multiline blocks.

### 7b. SQL keyword casing follows each engine's native convention

These migration scripts are essentially captured, normalized output of each provider's own DDL generator, so each file matches what that provider emits. Reserved **keywords** are UPPER (`CREATE TABLE`, `IF NOT EXISTS`, `PRIMARY KEY`, `NOT NULL`), while column **types** use each engine's canonical casing (`nvarchar(150)`, `varchar(150)`, `character varying(150)`, `TEXT`).

```sql
IF OBJECT_ID(N'[DepartmentGroups]') IS NULL
BEGIN
    CREATE TABLE [dbo].[DepartmentGroups](
        [DepartmentGroupId] [uniqueidentifier] NOT NULL,
        [DepartmentGroupName] [nvarchar](200) NULL,
        [TenantId] [uniqueidentifier] NOT NULL,
```
— [DataMigrations.SQLServer.cs:119](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L119-L124). PostgreSQL and SQLite even mix casing within one column block (`uuid NOT NULL`, `TIMESTAMP NOT NULL`, `character varying(200)`, `boolean`) — they follow the provider, not a single house style ([DataMigrations.PostgreSQL.cs:22](FreeCRM/CRM.DataAccess/DataMigrations.PostgreSQL.cs#L22-L30)). The takeaway: **do not impose one casing rule across engines — match the dialect the rest of that file uses.**

### 7c. Comments: C# `//` markers outside the SQL, not `--` inside it

Annotations around SQL blocks are written as C# `//` comments **outside** the string — never as SQL `--` or `/* */` comments inside it. The distinctive ones are `{{ModuleItem...}}` markers that bracket which statements belong to an optional feature module so they can be included or stripped as a unit:

```csharp
// {{ModuleItemStart:Services}}
m1.Add(
    """
    IF OBJECT_ID(N'[AppointmentServices]') IS NULL
    BEGIN
        CREATE TABLE [dbo].[AppointmentServices](
            [AppointmentServiceId] [uniqueidentifier] NOT NULL,
```
…closed later by:
```csharp
    END
    """
    );
// {{ModuleItemEnd:Services}}
```
— [DataMigrations.SQLServer.cs:75](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L75-L96). These markers are tooling delimiters, so they must live in the C# host code, not inside the executed SQL. A search of `DataMigrations.SQLServer.cs` found **no** `--` or `/* */` SQL comments at all.

Plain explanatory `//` comments *do* appear above raw-SQL runtime calls to say *why* EF is being bypassed — e.g. `// Clear out this location in any appointments` at [DataAccess.Utilities.cs:672](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L672) and the default-location comment at [DataAccess.Locations.cs:198](FreeCRM/CRM.DataAccess/DataAccess.Locations.cs#L198). Comments explain intent in C#; they never go inside the SQL string.

---

<a id="quick-reference"></a>
## 8. Quick Reference

The left column is wrong for this codebase; the right column matches the convention. All right-column forms are copied from or consistent with the real source.

**Data access — which tool**

| ✗ Avoid | ✓ Convention |
|---------|---------------|
| Hand-writing SQL for ordinary single-entity reads/writes | EF LINQ: `await data.Locations.AddAsync(rec)` / `Remove` + `SaveChangesAsync()` |
| Looping thousands of entities to flip one column | One set-based `ExecuteSqlRawAsync("UPDATE … WHERE …={0}", value)` |
| `FromSqlRaw` / `ExecuteSqlInterpolated` / raw `CreateCommand` | `ExecuteSqlRaw(Async)` for writes, `SqlQueryRaw<T>` for the one history read |

**Schema changes — guard everything**

| ✗ Avoid | ✓ Convention |
|---------|---------------|
| `CREATE TABLE [DepartmentGroups](...)` (bare) | `IF OBJECT_ID(N'[DepartmentGroups]') IS NULL BEGIN CREATE TABLE … END` (SQL Server) |
| `CREATE TABLE "T" (...)` (bare, non-SQL-Server) | `CREATE TABLE IF NOT EXISTS "T" (...)` |
| `ALTER TABLE … ADD CONSTRAINT FK_…` (bare) | wrap in `IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME='…')` (SQL Server) or declare inline in the guarded `CREATE TABLE` (others) |
| `IF EXISTS(...)` around a fresh-DB FK add | `IF NOT EXISTS(...)` — the inverted form is the known bug at `SQLServer.cs:560–614` |
| Bare `INSERT` into `__EFMigrationsHistory` | dialect-idempotent insert: `IF NOT EXISTS … INSERT` / `INSERT … EXCEPT SELECT` / `INSERT IGNORE` |

**Injection safety**

| ✗ Avoid | ✓ Convention |
|---------|---------------|
| `ExecuteSqlRawAsync("UPDATE T SET c='" + value + "'")` | `ExecuteSqlRawAsync("UPDATE T SET c={0}", value)` |
| `ExecuteSqlRawAsync($"… WHERE id={id}")` (interpolated) | `ExecuteSqlRawAsync("… WHERE id={0}", id)` |
| Quoting the placeholder: `SET c='{0}'` | bare placeholder: `SET c={0}` (EF supplies the quoting) |

**SQL strings in C#**

| ✗ Avoid | ✓ Convention |
|---------|---------------|
| `@"CREATE TABLE ..."` (verbatim) | `"""` raw string literal for multiline SQL |
| `-- comment` inside the SQL string | `// {{ModuleItemStart:X}}` / `//` comments outside the string |
| Forcing one keyword casing across engines | UPPER keywords + each engine's native type/quote casing |

---

<a id="faq"></a>
## 9. FAQ

**Q1. When do I use EF versus raw SQL?**
EF for everything by default — single-entity reads and writes, normal queries. Drop to raw SQL only for (a) schema migrations/DDL, (b) a set-based bulk `UPDATE`/`DELETE` across many rows, or (c) querying a table EF doesn't map (in practice, just the migration-history read). If you're loading rows only to change one column on all of them, that's a bulk `ExecuteSqlRawAsync`.

**Q2. Why guard every `CREATE TABLE`?**
Because migrations run on **every** application startup ([DataAccess.cs:93](FreeCRM/CRM.DataAccess/DataAccess.cs#L93-L94)), against both brand-new and fully-built databases. A bare `CREATE TABLE` would succeed the first time and throw "table already exists" on the second startup. The existence guard makes the second-and-later runs a harmless no-op.

**Q3. How do I add a column to an existing table safely?**
Guard it. Check whether the column already exists before `ALTER TABLE … ADD` — on SQL Server with the procedural `IF NOT EXISTS(... INFORMATION_SCHEMA ...)` catalog check (same family as the FK guard at [DataMigrations.SQLServer.cs:648](FreeCRM/CRM.DataAccess/DataMigrations.SQLServer.cs#L648-L654)); on the other engines by declaring the column inside the guarded `CREATE TABLE IF NOT EXISTS`. Never run a bare `ALTER … ADD`.

**Q4. Why are there four migration files?**
One per database engine — SQL Server, SQLite, PostgreSQL, MySQL — because each speaks a different SQL dialect (different quote characters, type names, and "create if absent" syntax). The SQL can't be shared, so it's split by engine; but all four return the same `List<DataObjects.DataMigration>` ([DataObjects.cs:163](FreeCRM/CRM.DataObjects/DataObjects.cs#L163-L167)) so one runner stays dialect-agnostic.

**Q5. How do I add a new migration?**
In each of the four engine files, build a new `List<string>` (e.g. `m2`) of guarded SQL steps in that dialect, ordered tables→constraints/indexes→history-insert; end with the idempotent insert recording the new id; then `output.Add(new DataObjects.DataMigration { MigrationId = "002", Migration = m2 });`. See Section 5. Authoring order is execution order.

**Q6. How do I stop SQL injection?**
Never concatenate or interpolate values into a SQL string. Use positional placeholders and pass values as arguments: `ExecuteSqlRawAsync("UPDATE T SET c={0} WHERE id={1}", value, id)` ([DataAccess.Users.cs:150](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L150-L151)). EF binds each `{n}` as a real parameter, so the value can never be executed as SQL.

**Q7. Where and when do migrations actually run?**
At database open/startup, only when the database isn't in-memory and migrations are enabled — `if (!_inMemoryDatabase && _useMigrations)` at [DataAccess.cs:93](FreeCRM/CRM.DataAccess/DataAccess.cs#L93-L94), with a fallback in the `EnsureCreated()` branch at [DataAccess.cs:103](FreeCRM/CRM.DataAccess/DataAccess.cs#L103-L104). The runner is `DatabaseApplyLatestMigrations()` in [DataAccess.Migrations.cs:16](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L16-L34).

**Q8. How does the runner know what's already been applied?**
It reads the recorded `MigrationId`s from `__EFMigrationsHistory` ([DataAccess.Migrations.cs:45](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L45-L62)) and skips any migration whose id is already present. If the history table doesn't exist yet (brand-new DB), the read is caught and returns an empty list, so everything runs.

**Q9. Can I just edit the database directly?**
No. Schema changes must go through the migration files so they're reproducible across all five back-ends and applied consistently on every startup. A hand-edit on one database would not exist on the others and would drift from `__EFMigrationsHistory`. Add a migration instead.

**Q10. Why aren't indexes guarded like tables are?**
`CREATE INDEX` is emitted bare ([DataMigrations.SQLite.cs:575](FreeCRM/CRM.DataAccess/DataMigrations.SQLite.cs#L575)) because a uniform `CREATE INDEX IF NOT EXISTS` isn't available the same way across dialects. It stays idempotent because the runner wraps each step in try/catch and swallows the "already exists" error ([DataAccess.Migrations.cs:20](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L20-L33)). It's the one DDL category whose safety comes from the runner, not the SQL.

**Q11. What's `__EFMigrationsHistory` and why is it hand-managed?**
It's the small table that records which migrations have run. The name is borrowed from EF Core's own convention, but FreeCRM manages it by hand (its own runner inserts the rows), because FreeCRM bypasses EF's built-in migration system to support multiple engines.

**Q12. Why use `"""` raw strings instead of `@"..."`?**
Inside `"""` you don't have to escape/double-up quote characters, so quoted identifiers stay readable, and the compiler strips the common indentation so the SQL doesn't carry stray whitespace. The project has zero `@"..."` SQL strings. See Section 7a.

**Q13. The migration insert uses `INSERT IGNORE` / `EXCEPT SELECT` / `IF NOT EXISTS` — why three different forms?**
Same goal ("record this id only if it isn't already there"), three dialects: SQL Server has no `CREATE TABLE IF NOT EXISTS` and uses procedural `IF NOT EXISTS`; MySQL leans on `INSERT IGNORE`; PostgreSQL and SQLite use `INSERT … EXCEPT SELECT` (PostgreSQL quotes the identifiers and ends with `;`, SQLite doesn't). All four are idempotent.

**Q14. A migration step failed on startup — did it break everything?**
No. Each step runs in its own try/catch; a failure is logged into `output.Messages` (overall result is "success only if zero messages", [DataAccess.Migrations.cs:36](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L36)) and later steps and migrations still run. But note skip-by-id is per-*migration*: until the history row for that migration lands, every step is re-attempted on each startup — which is exactly why each step must be idempotent or its error catchable.

---

<a id="related-docs"></a>
## 10. Related Docs

- [055 — C# Language Conventions](055_csharp-style-reference.md) — the C# patterns these SQL strings are embedded in (raw string literals, `String.Empty`, naming).
- [025 — EF Models and the Records That Never Truly Vanish](025_ef-models-soft-delete.md) — the EF entity model and soft-delete strategy that is the *default* this doc tells you to prefer.
- [023 — The Partial Data-Access Layer](023_partial-data-access.md) — how `DataAccess` is split into partial files (where the raw-SQL call sites and the migration runner live).
- [021 — Anatomy of the Data Stack](021_data-stack-anatomy.md) — the big-picture map of how a request reaches the database.
- [026 — The Standard Result Object](026_standard-result.md) — the `output.Result`/`output.Messages` shape the migration runner reports through.
- [051 — The Author House Style](051_house-code-style.md) — braces, casing, and `String.Empty` rules that the C# wrapping this SQL follows.

---
*GuidesV2 059 · SQL and Database Coding Style · drafted 2026-06-05 from citation-verified source evidence (`FreeCRM/CRM.DataAccess/DataMigrations.{SQLServer,SQLite,PostgreSQL,MySQL}.cs`, `DataAccess.Migrations.cs`, `DataAccess.cs`, `DataAccess.Users.cs`, `DataAccess.Locations.cs`, `DataAccess.Utilities.cs`, and `CRM.DataObjects/DataObjects.cs`).*
