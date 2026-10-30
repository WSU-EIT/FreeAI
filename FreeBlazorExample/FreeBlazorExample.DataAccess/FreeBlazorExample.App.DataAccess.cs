using FreeBlazorExtended.UserPreferences;

namespace FreeBlazorExample;

// ---- DataAccess.UserPreferences ----

public partial class DataAccess
{
    /// <summary>
    /// Hard-deletes the UserPreferences row for the given (tenant, user).
    /// Used by <see cref="UserPreferencesService.ResetUserPreferences"/> so a
    /// reset really clears state rather than leaving stale audit fields.
    /// </summary>
    public async Task DeleteUserPreferencesRow(Guid TenantId, Guid UserId)
    {
        var existing = await data.UserPreferences
            .FirstOrDefaultAsync(p => p.TenantId == TenantId && p.UserId == UserId);

        if (existing != null) {
            data.UserPreferences.Remove(existing);
            await data.SaveChangesAsync();
        }
    }
    /// <summary>
    /// Loads the FreeBlazorExtended Feature 104 UserPreferences row for the
    /// given (tenant, user). Returns null if no row exists yet.
    /// AsNoTracking — callers mutate and re-save via <see cref="SaveUserPreferencesRow"/>.
    /// </summary>
    public async Task<UserPreferences?> GetUserPreferencesRow(Guid TenantId, Guid UserId)
    {
        return await data.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == TenantId && p.UserId == UserId);
    }

    /// <summary>
    /// Upserts a UserPreferences row. Look-up is by (TenantId, UserId)
    /// because UserPreferencesId is generated client-side and may differ
    /// between the in-memory copy and the persisted one.
    /// </summary>
    public async Task<UserPreferences> SaveUserPreferencesRow(UserPreferences prefs)
    {
        var existing = await data.UserPreferences
            .FirstOrDefaultAsync(p => p.TenantId == prefs.TenantId && p.UserId == prefs.UserId);

        if (existing == null) {
            if (prefs.UserPreferencesId == Guid.Empty) {
                prefs.UserPreferencesId = Guid.NewGuid();
            }
            if (prefs.Added == default) {
                prefs.Added = DateTime.UtcNow;
            }
            data.UserPreferences.Add(prefs);
        } else {
            existing.Theme = prefs.Theme;
            existing.CultureCode = prefs.CultureCode;
            existing.Zoom = prefs.Zoom;
            existing.Density = prefs.Density;
            existing.LayoutPanelLeftWidth = prefs.LayoutPanelLeftWidth;
            existing.LayoutPanelRightWidth = prefs.LayoutPanelRightWidth;
            existing.SidebarCollapsed = prefs.SidebarCollapsed;
            existing.PerEntityJson = prefs.PerEntityJson;
            existing.RecentItemsJson = prefs.RecentItemsJson;
            existing.LastModified = prefs.LastModified == default ? DateTime.UtcNow : prefs.LastModified;
            existing.LastModifiedBy = prefs.LastModifiedBy;
            existing.Deleted = prefs.Deleted;
            // Reflect the canonical PK back to the caller's instance.
            prefs.UserPreferencesId = existing.UserPreferencesId;
        }

        await data.SaveChangesAsync();
        return prefs;
    }
}

/// <summary>
/// EF Core-backed <see cref="IUserPreferencesStore"/> for Feature 104.
/// Wraps the project's existing <see cref="IDataAccess"/> so we reuse its
/// configured <see cref="EFDataModel"/> (connection string, provider, retry
/// policies) instead of registering a second DbContext.
///
/// Falls back to in-memory state if the underlying database can't be reached
/// (e.g. before first-run setup has supplied a connection string) so the
/// showcase keeps working in dev. The fallback only kicks in on connection
/// failures, NOT on row-level errors.
/// </summary>
public class FreeBlazorExampleEFUserPreferencesStore : IUserPreferencesStore
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly InMemoryUserPreferencesStore _fallback = new();

    public FreeBlazorExampleEFUserPreferencesStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DeleteAsync(Guid TenantId, Guid UserId)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            await _fallback.DeleteAsync(TenantId, UserId);
            return;
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    await concrete.DeleteUserPreferencesRow(TenantId, UserId);
                    return;
                }
            }
        } catch {
            await _fallback.DeleteAsync(TenantId, UserId);
        }
    }

    public async Task<UserPreferences?> GetAsync(Guid TenantId, Guid UserId)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            return await _fallback.GetAsync(TenantId, UserId);
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    return await concrete.GetUserPreferencesRow(TenantId, UserId);
                }
            }
        } catch {
            return await _fallback.GetAsync(TenantId, UserId);
        }

        return null;
    }

    private IDataAccess? GetDataAccess()
    {
        try {
            // IDataAccess is registered as Transient — resolve a fresh instance
            // per call so we get a fresh DbContext (DataAccess owns its own
            // EFDataModel internally and is IDisposable).
            return _serviceProvider.GetService(typeof(IDataAccess)) as IDataAccess;
        } catch {
            return null;
        }
    }

    public async Task SaveAsync(UserPreferences prefs)
    {
        var da = GetDataAccess();
        if (da == null || !da.DatabaseOpen) {
            await _fallback.SaveAsync(prefs);
            return;
        }

        try {
            using (da as IDisposable) {
                if (da is DataAccess concrete) {
                    await concrete.SaveUserPreferencesRow(prefs);
                    return;
                }
            }
        } catch {
            await _fallback.SaveAsync(prefs);
        }
    }
}

// ---- DataMigrations ----

// FreeBlazorExample database migrations, kept out of the stock DataMigrations.{SQLServer,MySQL,PostgreSQL,SQLite}.cs files.
// The UserPreferences table (FreeBlazorExtended Feature 104) is created today by EnsureCreated: the InMemory database builds
// its schema from the EF model (FreeBlazorExample.App.EFDataModel.cs) and a new SQL database is created from the model too.
// These methods are NOT wired in: _useMigrations is false, and DataAccess.Migrations.cs (a framework file) has no seam for
// app migrations. If the app ever runs migrations on SQL, replace them with an EF migration in FreeBlazorExample.EFModels/Migrations/.

public partial class DataMigrations
{
    /// <summary>
    /// The FreeBlazorExtended Feature 104 (UserPreferences) migration for SQLServer.
    /// </summary>
    public List<DataObjects.DataMigration> GetFreeBlazorExampleMigrationsSqlServer()
    {
        List<DataObjects.DataMigration> output = new List<DataObjects.DataMigration>();

        // -----------------------------------------------------------------
        // Migration 002 — FreeBlazorExtended Feature 104 UserPreferences.
        // Adds one row per (tenant, user) for the showcase preferences API.
        // -----------------------------------------------------------------
        List<string> m2 = new List<string>();
        m2.Add(
            """
            IF OBJECT_ID(N'[UserPreferences]') IS NULL
            BEGIN
                CREATE TABLE [dbo].[UserPreferences](
                    [UserPreferencesId] [uniqueidentifier] NOT NULL,
                    [TenantId] [uniqueidentifier] NOT NULL,
                    [UserId] [uniqueidentifier] NOT NULL,
                    [Theme] [int] NOT NULL,
                    [CultureCode] [nvarchar](20) NOT NULL,
                    [Zoom] [int] NOT NULL,
                    [Density] [int] NOT NULL,
                    [LayoutPanelLeftWidth] [decimal](18,2) NOT NULL,
                    [LayoutPanelRightWidth] [decimal](18,2) NOT NULL,
                    [SidebarCollapsed] [bit] NOT NULL,
                    [PerEntityJson] [nvarchar](max) NOT NULL,
                    [RecentItemsJson] [nvarchar](max) NOT NULL,
                    [Added] [datetime] NOT NULL,
                    [AddedBy] [nvarchar](100) NULL,
                    [LastModified] [datetime] NOT NULL,
                    [LastModifiedBy] [nvarchar](100) NULL,
                    [Deleted] [bit] NOT NULL,
                    CONSTRAINT [PK_UserPreferences] PRIMARY KEY CLUSTERED ([UserPreferencesId] ASC)
                ) ON [PRIMARY]
            END
            """);

        m2.Add(
            """
            IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name='IX_UserPreferences_TenantId_UserId' AND object_id = OBJECT_ID('[dbo].[UserPreferences]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_UserPreferences_TenantId_UserId] ON [dbo].[UserPreferences] ([TenantId], [UserId])
            END
            """);

        m2.Add(
            """
            IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
            BEGIN
                IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId]=N'002')
                BEGIN
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (N'002', N'1.0.0')
                END
            END
            """);

        output.Add(new DataObjects.DataMigration {
            MigrationId = "002",
            Migration = m2
        });

        return output;
    }

    /// <summary>
    /// The FreeBlazorExtended Feature 104 (UserPreferences) migration for MySQL.
    /// </summary>
    public List<DataObjects.DataMigration> GetFreeBlazorExampleMigrationsMySQL()
    {
        List<DataObjects.DataMigration> output = new List<DataObjects.DataMigration>();

        // -----------------------------------------------------------------
        // Migration 002 — FreeBlazorExtended Feature 104 UserPreferences.
        // -----------------------------------------------------------------
        List<string> m2 = new List<string>();
        m2.Add(
            """
            CREATE TABLE IF NOT EXISTS `UserPreferences` (
                `UserPreferencesId` CHAR(36) NOT NULL,
                `TenantId` CHAR(36) NOT NULL,
                `UserId` CHAR(36) NOT NULL,
                `Theme` INT NOT NULL,
                `CultureCode` VARCHAR(20) NOT NULL,
                `Zoom` INT NOT NULL,
                `Density` INT NOT NULL,
                `LayoutPanelLeftWidth` DECIMAL(18,2) NOT NULL,
                `LayoutPanelRightWidth` DECIMAL(18,2) NOT NULL,
                `SidebarCollapsed` TINYINT(1) NOT NULL,
                `PerEntityJson` LONGTEXT NOT NULL,
                `RecentItemsJson` LONGTEXT NOT NULL,
                `Added` DATETIME NOT NULL,
                `AddedBy` VARCHAR(100) NULL,
                `LastModified` DATETIME NOT NULL,
                `LastModifiedBy` VARCHAR(100) NULL,
                `Deleted` TINYINT(1) NOT NULL,
                CONSTRAINT `PK_UserPreferences` PRIMARY KEY (`UserPreferencesId`)
            );
            """);

        m2.Add(
            """
            CREATE UNIQUE INDEX `IX_UserPreferences_TenantId_UserId` ON `UserPreferences` (`TenantId`, `UserId`);
            """);

        m2.Add(
            """
            INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
            VALUES ('002', '1.0.0');
            """);

        output.Add(new DataObjects.DataMigration {
            MigrationId = "002",
            Migration = m2
        });

        return output;
    }

    /// <summary>
    /// The FreeBlazorExtended Feature 104 (UserPreferences) migration for PostgreSQL.
    /// </summary>
    public List<DataObjects.DataMigration> GetFreeBlazorExampleMigrationsPostgreSQL()
    {
        List<DataObjects.DataMigration> output = new List<DataObjects.DataMigration>();

        // -----------------------------------------------------------------
        // Migration 002 — FreeBlazorExtended Feature 104 UserPreferences.
        // -----------------------------------------------------------------
        List<string> m2 = new List<string>();
        m2.Add(
            """
            CREATE TABLE IF NOT EXISTS "UserPreferences" (
                "UserPreferencesId" uuid NOT NULL,
                "TenantId" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Theme" integer NOT NULL,
                "CultureCode" varchar(20) NOT NULL,
                "Zoom" integer NOT NULL,
                "Density" integer NOT NULL,
                "LayoutPanelLeftWidth" numeric(18,2) NOT NULL,
                "LayoutPanelRightWidth" numeric(18,2) NOT NULL,
                "SidebarCollapsed" boolean NOT NULL,
                "PerEntityJson" text NOT NULL,
                "RecentItemsJson" text NOT NULL,
                "Added" timestamp NOT NULL,
                "AddedBy" varchar(100) NULL,
                "LastModified" timestamp NOT NULL,
                "LastModifiedBy" varchar(100) NULL,
                "Deleted" boolean NOT NULL,
                CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("UserPreferencesId")
            );
            """);

        m2.Add(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserPreferences_TenantId_UserId" ON "UserPreferences" ("TenantId", "UserId");
            """);

        m2.Add(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('002', '1.0.0')
            EXCEPT
            SELECT * FROM "__EFMigrationsHistory" WHERE "MigrationId"='002';
            """);

        output.Add(new DataObjects.DataMigration {
            MigrationId = "002",
            Migration = m2
        });

        return output;
    }

    /// <summary>
    /// The FreeBlazorExtended Feature 104 (UserPreferences) migration for SQLite.
    /// </summary>
    public List<DataObjects.DataMigration> GetFreeBlazorExampleMigrationsSQLite()
    {
        List<DataObjects.DataMigration> output = new List<DataObjects.DataMigration>();

        // -----------------------------------------------------------------
        // Migration 002 — FreeBlazorExtended Feature 104 UserPreferences.
        // -----------------------------------------------------------------
        List<string> m2 = new List<string>();
        m2.Add(
            """
            CREATE TABLE IF NOT EXISTS "UserPreferences" (
                "UserPreferencesId" TEXT NOT NULL CONSTRAINT "PK_UserPreferences" PRIMARY KEY,
                "TenantId" TEXT NOT NULL,
                "UserId" TEXT NOT NULL,
                "Theme" INTEGER NOT NULL,
                "CultureCode" TEXT NOT NULL,
                "Zoom" INTEGER NOT NULL,
                "Density" INTEGER NOT NULL,
                "LayoutPanelLeftWidth" TEXT NOT NULL,
                "LayoutPanelRightWidth" TEXT NOT NULL,
                "SidebarCollapsed" INTEGER NOT NULL,
                "PerEntityJson" TEXT NOT NULL,
                "RecentItemsJson" TEXT NOT NULL,
                "Added" TEXT NOT NULL,
                "AddedBy" TEXT NULL,
                "LastModified" TEXT NOT NULL,
                "LastModifiedBy" TEXT NULL,
                "Deleted" INTEGER NOT NULL
            );
            """);

        m2.Add(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserPreferences_TenantId_UserId" ON "UserPreferences" ("TenantId", "UserId");
            """);

        m2.Add(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('002', '1.0.0')
            EXCEPT
            SELECT * FROM __EFMigrationsHistory WHERE MigrationId='002'
            """);

        output.Add(new DataObjects.DataMigration {
            MigrationId = "002",
            Migration = m2
        });

        return output;
    }
}
