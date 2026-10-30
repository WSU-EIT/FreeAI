// FreeAccessibilityChecker: one name for the SignalR hub in our own code. FreeCRM's Util generates the hub class name from the
// project name (Hubs/signalrHub.cs); if an upgrade renames it, only this line changes.
global using FreeAccessibilityCheckerHub = FreeAccessibilityChecker.Server.Hubs.freeaccessibilitycheckerHub;
using Microsoft.Extensions.Configuration.Json;
using FreeAccessibilityChecker.Scanner;
using FreeAccessibilityChecker.Scanner.Models;
using FreeAccessibilityChecker.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using NCrontab;
using Microsoft.Playwright;

namespace FreeAccessibilityChecker;

// ---- FreeAccessibilityChecker.App.Program.cs ----
// FreeAccessibilityChecker startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads FreeAccessibilityChecker's own settings files (appsettings.FreeAccessibilityChecker.App.json and its environment variant, e.g.
    /// appsettings.FreeAccessibilityChecker.App.Development.json or .Audit.json), so the stock appsettings files stay untouched.
    /// They go right after the stock appsettings files: they override those, while user secrets (ScanEncryptionKey and the
    /// seed credentials), environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeAccessibilityCheckerAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeAccessibilityChecker.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeAccessibilityChecker.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }

    /// <summary>
    /// Registers the scanner background service and the model-validation logger. Called from AppModifyBuilderEnd.
    /// </summary>
    public static WebApplicationBuilder FreeAccessibilityCheckerAppModifyBuilderEnd(WebApplicationBuilder builder)
    {
        // Register the scanner background service so it starts automatically when the app launches.
        builder.Services.AddHostedService<ScannerBackgroundService>();

        // Log model validation errors for debugging 400 responses.
        builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => {
            options.InvalidModelStateResponseFactory = context => {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .Select(e => $"{e.Key}: {String.Join("; ", e.Value!.Errors.Select(x => x.ErrorMessage + (x.Exception != null ? " (" + x.Exception.Message + ")" : "")))}")
                    .ToList();
                Console.WriteLine("Model validation failed for " + context.HttpContext.Request.Path + ":");
                foreach (var e in errors) {
                    Console.WriteLine("  " + e);
                }
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { errors });
            };
        });

        return builder;
    }

    /// <summary>
    /// Binds the "App:Scan*" settings into the ConfigurationHelperLoader. Called from ConfigurationHelpersLoadApp.
    /// </summary>
    public static ConfigurationHelperLoader FreeAccessibilityCheckerConfigurationHelpersLoadApp(ConfigurationHelperLoader Loader, WebApplicationBuilder Builder)
    {
        ConfigurationHelperLoader output = Loader;

        output.ScanSettleDelayMs = Builder.Configuration.GetValue<int>("App:ScanSettleDelayMs");
        output.ScanTimeoutMs = Builder.Configuration.GetValue<int>("App:ScanTimeoutMs");
        output.ScanMaxConcurrency = Builder.Configuration.GetValue<int>("App:ScanMaxConcurrency");
        output.ScanWcagLevel = String.Empty + Builder.Configuration.GetValue<string>("App:ScanWcagLevel");
        output.ScanEncryptionKey = String.Empty + Builder.Configuration.GetValue<string>("App:ScanEncryptionKey");
        output.ScanHeadless = Builder.Configuration.GetValue<bool>("App:ScanHeadless");
        output.ScanUserAgent = String.Empty + Builder.Configuration.GetValue<string>("App:ScanUserAgent");

        return output;
    }



    /// <summary>
    /// Startup data work: cancels orphaned scans and seeds the default sites, credentials and pages. Called from AppModifyEnd.
    /// </summary>
    public static WebApplication FreeAccessibilityCheckerAppModifyEnd(WebApplication app)
    {
        var output = app;

        // Cancel any in-flight scans from a previous process. The app may have been killed
        // mid-scan; on restart those rows would still show as "Running"/"Queued" in the
        // Scan Monitor view forever. Marks them Cancelled so the UI is clean. Does NOT
        // touch Complete/Failed runs — preserves all real scan data on disk and in DB.
        CancelOrphanedScansOnStartup(output).GetAwaiter().GetResult();

        // Seed default sites if they don't already exist.
        SeedDefaultSites(output).GetAwaiter().GetResult();
        // Seed credentials for login-protected sites from config.
        SeedDefaultSiteCredentials(output).GetAwaiter().GetResult();
        // Seed the known route inventory (with SFS tenant prefix) so the web-app
        // scan covers every page in one shot, mirroring the console crawl behavior.
        SeedDefaultSitePages(output).GetAwaiter().GetResult();

        return output;
    }
}

// ---- FreeAccessibilityChecker.App.Program.Seed.cs ----
// FreeAccessibilityChecker startup data seeding: cancels orphaned scans, then seeds the default sites, credentials and pages.
// Called from FreeAccessibilityCheckerAppModifyEnd in FreeAccessibilityChecker.App.Program.cs.

public partial class Program
{
    private static async Task CancelOrphanedScansOnStartup(WebApplication app)
    {
        try {
            using var scope = app.Services.CreateScope();
            var da = scope.ServiceProvider.GetRequiredService<IDataAccess>();
            int cancelled = await da.CancelOrphanedScanRuns();
            if (cancelled > 0) {
                Console.WriteLine($"Cancelled {cancelled} orphaned scan run(s) from previous process.");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error cancelling orphaned scan runs: " + ex.Message);
        }
    }

    // Default seed list. Seeder is idempotent — only adds sites whose BaseUrl isn't
    // already present for the tenant. Edits here apply on next launch (in-memory DB
    // wipes; for persistent DB, missing URLs get added without disturbing existing).
    private static readonly (string Url, string Name, bool Enabled)[] DefaultSites = {
        ("https://flex.em.wsu.edu", "Flex", true),
        ("https://flex.em.wsu.edu/Touchpoints", "Flex Touchpoints", true),
        ("https://em.wsu.edu", "EM WSU", true),
    };

    private static async Task SeedDefaultSites(WebApplication app)
    {
        var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000002");

        try {
            using var scope = app.Services.CreateScope();
            var da = scope.ServiceProvider.GetRequiredService<IDataAccess>();

            var existingSites = await da.GetSites(null, defaultTenantId);
            var existingUrls = new HashSet<string>(
                existingSites.Select(s => s.BaseUrl.TrimEnd('/')),
                StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<DataObjects.Site>();
            foreach (var (url, name, enabled) in DefaultSites) {
                if (!existingUrls.Contains(url.TrimEnd('/'))) {
                    toAdd.Add(new DataObjects.Site {
                        SiteId = Guid.Empty,
                        TenantId = defaultTenantId,
                        Name = name,
                        BaseUrl = url,
                        Enabled = enabled,
                        PublicVisible = true,
                        MaxConcurrency = 5,
                    });
                }
            }

            if (toAdd.Any()) {
                await da.SaveSites(toAdd);
                Console.WriteLine($"Seeded {toAdd.Count} default site(s): {String.Join(", ", toAdd.Select(s => s.BaseUrl))}");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error seeding default sites: " + ex.Message);
        }
    }

    /// <summary>
    /// Per-site credential descriptor. Add a new entry to <see cref="CredentialSeeds"/>
    /// to auto-populate a SiteCredential row whenever the matching Site exists in the DB.
    /// Each entry pulls its username / password from the supplied config keys (typically
    /// user-secrets) — passwords are NEVER hard-coded. If the password key isn't set the
    /// entry is skipped silently (matching site can still have its credential added by
    /// hand via the EditSite UI).
    /// </summary>
    private record CredentialSeed(
        string MatchBaseUrl,
        string UsernameKey,
        string PasswordKey,
        string DefaultLoginUrl,
        string DefaultUsername = "admin",
        string DefaultUsernameSelector = "#login-email",
        string DefaultPasswordSelector = "#login-password",
        string DefaultSubmitSelector = "button.btn.btn-primary",
        string Label = "Admin");

    /// <summary>
    /// Credentials to seed on app startup. Each entry maps a Site (by exact BaseUrl,
    /// case-insensitive) to a username/password pulled from configuration. To add a new
    /// site's auth: add an entry here and run:
    ///     dotnet user-secrets set "App:&lt;Name&gt;AdminUsername" "&lt;user&gt;" --project FreeAccessibilityChecker
    ///     dotnet user-secrets set "App:&lt;Name&gt;AdminPassword" "&lt;pass&gt;" --project FreeAccessibilityChecker
    ///
    /// Touchpoints uses its own LOCAL admin login (not the Flex cookie) — the scanner
    /// hits /Touchpoints/{TenantCode}/Login, which is a standard multi-provider
    /// login form. Same selectors as Flex (#login-email, #login-password, btn-primary).
    /// The "local" provider button (#login-button-local) may need to be clicked first to
    /// reveal the local credential fields — handle in AuthHandler if it becomes a problem.
    /// </summary>
    private static readonly CredentialSeed[] CredentialSeeds = {
        new CredentialSeed(
            MatchBaseUrl: "https://flex.em.wsu.edu",
            UsernameKey: "App:FlexAdminUsername",
            PasswordKey: "App:FlexAdminPassword",
            DefaultLoginUrl: "https://flex.em.wsu.edu/Login"),
        new CredentialSeed(
            MatchBaseUrl: "https://flex.em.wsu.edu/Touchpoints",
            UsernameKey: "App:TouchpointsAdminUsername",
            PasswordKey: "App:TouchpointsAdminPassword",
            DefaultLoginUrl: "https://flex.em.wsu.edu/Touchpoints/" + DefaultTenantCode + "/Login"),
    };

    /// <summary>
    /// Seeds SiteCredential rows for every entry in <see cref="CredentialSeeds"/> whose
    /// password key resolves to a non-empty config value. Idempotent: skips any site
    /// that already has a credential. Logs per-site OK/SKIP/FAIL so failures aren't silent.
    /// </summary>
    private static async Task SeedDefaultSiteCredentials(WebApplication app)
    {
        var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000002");

        try {
            using var scope = app.Services.CreateScope();
            var da = scope.ServiceProvider.GetRequiredService<IDataAccess>();
            var config = app.Configuration;
            var allSites = await da.GetSites(null, defaultTenantId);

            int totalSeeded = 0, totalSkipped = 0, totalFailed = 0, totalNoSecret = 0, totalNoSite = 0;

            foreach (var seed in CredentialSeeds) {
                string password = config.GetValue<string>(seed.PasswordKey) ?? "";
                if (String.IsNullOrWhiteSpace(password)) {
                    totalNoSecret++;
                    Console.WriteLine($"  [NO-SECRET] {seed.MatchBaseUrl} — {seed.PasswordKey} not configured. " +
                        $"Set via: dotnet user-secrets set \"{seed.PasswordKey}\" \"<value>\" --project FreeAccessibilityChecker");
                    continue;
                }

                string username = config.GetValue<string>(seed.UsernameKey) ?? seed.DefaultUsername;
                string loginUrl = config.GetValue<string>(seed.UsernameKey + "LoginUrl") ?? seed.DefaultLoginUrl;
                string usernameSelector = config.GetValue<string>(seed.UsernameKey + "UsernameSelector") ?? seed.DefaultUsernameSelector;
                string passwordSelector = config.GetValue<string>(seed.UsernameKey + "PasswordSelector") ?? seed.DefaultPasswordSelector;
                string submitSelector = config.GetValue<string>(seed.UsernameKey + "SubmitSelector") ?? seed.DefaultSubmitSelector;

                // Find the matching site by exact (case-insensitive) BaseUrl.
                var site = allSites.FirstOrDefault(s =>
                    s.BaseUrl.TrimEnd('/').Equals(seed.MatchBaseUrl, StringComparison.OrdinalIgnoreCase));
                if (site == null) {
                    totalNoSite++;
                    Console.WriteLine($"  [NO-SITE] {seed.MatchBaseUrl} — no matching site row in DB; nothing to attach credential to.");
                    continue;
                }

                var existing = await da.GetSiteCredentials(null, site.SiteId);
                if (existing != null && existing.Any()) {
                    totalSkipped++;
                    Console.WriteLine($"  [SKIP] {site.Name} ({site.BaseUrl}) — already has {existing.Count} credential(s).");
                    continue;
                }

                var cred = new DataObjects.SiteCredential {
                    SiteCredentialId = Guid.Empty,
                    SiteId = site.SiteId,
                    TenantId = site.TenantId,
                    Label = seed.Label,
                    Username = username,
                    PasswordEncrypted = password, // SaveSiteCredentials encrypts before storing
                    AuthType = "Generic",
                    // FlexCRM tenant chooser — picks the [SFS] row before the login form
                    // appears. AuthHandler reads this and clicks the matching Select button.
                    TenantCode = DefaultTenantCode,
                    LoginUrl = loginUrl,
                    UsernameSelector = usernameSelector,
                    PasswordSelector = passwordSelector,
                    SubmitSelector = submitSelector,
                };
                var result = await da.SaveSiteCredentials(new List<DataObjects.SiteCredential> { cred });
                // SaveSiteCredentials swallows exceptions into ActionResponse.Messages on the
                // returned object — surface them so we know if encryption / DB save actually failed.
                bool ok = result != null && result.Any() && (result[0].ActionResponse == null
                    || result[0].ActionResponse.Messages == null || !result[0].ActionResponse.Messages.Any());
                if (ok) {
                    totalSeeded++;
                    Console.WriteLine($"  [OK]   {site.Name} ({site.BaseUrl}) — credential saved as user '{username}', login at {loginUrl}");
                } else {
                    totalFailed++;
                    string errMsgs = result != null && result.Any() && result[0].ActionResponse?.Messages != null
                        ? String.Join("; ", result[0].ActionResponse.Messages) : "(no error messages returned)";
                    Console.WriteLine($"  [FAIL] {site.Name} ({site.BaseUrl}) — save returned errors: {errMsgs}");
                }
            }

            Console.WriteLine($"SeedDefaultSiteCredentials: {totalSeeded} seeded, {totalSkipped} already-present, " +
                $"{totalFailed} failed, {totalNoSecret} skipped-no-secret, {totalNoSite} skipped-no-matching-site.");
        } catch (Exception ex) {
            Console.WriteLine("Error seeding site credentials: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    // Default tenant code used in URL prefixes for tenant-aware routing in Flex + Touchpoints.
    // The user-tenant context is "SFS" (System Family Services / actual prod tenant) — using
    // the tenant code in the URL surfaces tenant-scoped content during automated scans.
    private const string DefaultTenantCode = "SFS";

    // Flex routes mapped from `Websites/100_research.md`. Both the unprefixed AND
    // /SFS/-prefixed variants are seeded so the scan covers tenant-scoped surfaces
    // when Flex routing accepts a tenant code, and falls back to global routes when
    // it doesn't (the latter return the same content via tenant resolution).
    private static readonly string[] FlexRoutes = {
        "/", "/Login",
        "/CRM", "/ChangePassword", "/Profile", "/Reports", "/PluginTesting", "/Test",
        "/Settings/Departments", "/Settings/AddDepartment", "/Settings/DepartmentGroups",
        "/Settings/Files", "/Settings/ManageFile",
        "/Settings/KnownCallers", "/Settings/EditKnownCaller",
        "/Settings/Media", "/Settings/EditMedia",
        "/Settings/Queues",
        "/Settings/Tags", "/Settings/EditTag",
        "/Settings/Tenants", "/Settings/EditTenant", "/Settings/TenantQueues",
        "/Settings/Users", "/Settings/UserGroups", "/Settings/EditUserGroup",
        "/Settings/AppSettings", "/Settings/DeletedRecords", "/Settings/Languages",
        "/Settings/Settings", "/Settings/Setup", "/Settings/UDF",
    };

    // Touchpoints routes — stored as PATHS-FROM-ROOT (start with /Touchpoints/...).
    // The scanner combines the site's origin (https://flex.em.wsu.edu) with these
    // absolute paths to produce the correct URL. We can't store these as plain
    // tenant-relative paths like "/SFS/About" because the scanner's URL builder
    // would resolve them against the origin (giving https://flex.em.wsu.edu/SFS/About,
    // wrong) instead of the touchpoints sub-app. The {SFS} placeholder is substituted
    // at seed time.
    private static readonly string[] TouchpointsRoutesWithTenant = {
        "/Touchpoints/{SFS}/",
        "/Touchpoints/{SFS}/About",
        "/Touchpoints/{SFS}/Profile",
        "/Touchpoints/{SFS}/ChangePassword",
        "/Touchpoints/{SFS}/Search",
        "/Touchpoints/{SFS}/StudentInterventions",
        "/Touchpoints/{SFS}/ApiTokens",
        "/Touchpoints/{SFS}/Debug",
        "/Touchpoints/{SFS}/Settings/Tenants",
        "/Touchpoints/{SFS}/Settings/Users",
        "/Touchpoints/{SFS}/Settings/UserGroups",
        "/Touchpoints/{SFS}/Settings/Departments",
        "/Touchpoints/{SFS}/Settings/DepartmentGroups",
        "/Touchpoints/{SFS}/Settings/Files",
        "/Touchpoints/{SFS}/Settings/SourceTenantMapping",
        "/Touchpoints/{SFS}/Settings/DataBrowser",
        "/Touchpoints/{SFS}/Settings/AppSettings",
        "/Touchpoints/{SFS}/Settings/DeletedRecords",
        "/Touchpoints/{SFS}/Settings/Languages",
        "/Touchpoints/{SFS}/Settings/Setup",
        "/Touchpoints/{SFS}/Settings/UDF",
        "/Touchpoints/{SFS}/test",
    };

    /// <summary>
    /// Seeds the full route inventory for Flex and Touchpoints as SitePages, so a single
    /// scan-trigger from the web UI covers every known page rather than just the auto-created
    /// "/" root. Idempotent — only adds paths missing for each site.
    /// </summary>
    private static async Task SeedDefaultSitePages(WebApplication app)
    {
        var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000002");

        try {
            using var scope = app.Services.CreateScope();
            var da = scope.ServiceProvider.GetRequiredService<IDataAccess>();

            var allSites = await da.GetSites(null, defaultTenantId);
            int totalSeeded = 0;

            foreach (var site in allSites) {
                // OrdinalIgnoreCase comparison preserves canonical /Touchpoints casing
                // (the WSU server is case-sensitive on this path) while still matching
                // even if the seed URL drift later. .ToLower() previously hid the capital
                // T from the matcher and Touchpoints got 0 seeded routes.
                string normalized = site.BaseUrl.TrimEnd('/');
                string[]? routes = null;

                if (normalized.Equals("https://flex.em.wsu.edu", StringComparison.OrdinalIgnoreCase)) {
                    routes = FlexRoutes;
                } else if (normalized.Equals("https://flex.em.wsu.edu/Touchpoints", StringComparison.OrdinalIgnoreCase)) {
                    // Substitute the tenant code into the Touchpoints route template.
                    routes = TouchpointsRoutesWithTenant
                        .Select(r => r.Replace("{SFS}", DefaultTenantCode))
                        .ToArray();
                }

                if (routes == null || routes.Length == 0) continue;

                var existing = await da.GetSitePages(null, site.SiteId);
                var existingPaths = new HashSet<string>(
                    existing.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

                int sortOrder = existing.Count;
                var toAdd = new List<DataObjects.SitePage>();
                foreach (string path in routes) {
                    if (existingPaths.Contains(path)) continue;
                    sortOrder++;
                    toAdd.Add(new DataObjects.SitePage {
                        SitePageId = Guid.Empty,
                        SiteId = site.SiteId,
                        Path = path,
                        Title = string.Empty,
                        Enabled = true,
                        IncludeInScan = true,
                        SortOrder = sortOrder,
                    });
                }

                if (toAdd.Any()) {
                    await da.SaveSitePages(toAdd);
                    totalSeeded += toAdd.Count;
                    Console.WriteLine($"Seeded {toAdd.Count} SitePages for {site.Name} ({site.BaseUrl}).");
                }
            }

            if (totalSeeded == 0) {
                Console.WriteLine("All default SitePages already present — nothing to seed.");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error seeding default site pages: " + ex.Message);
        }
    }
}

// ---- FreeAccessibilityChecker.App.Services.Scanner.cs ----
/// <summary>
/// Background service that polls for pending scans and scheduled site scans.
/// Runs on a 5-minute polling loop, checks cron schedules, orchestrates scans,
/// and pushes real-time progress via SignalR.
/// </summary>
public partial class ScannerBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScannerBackgroundService> _logger;

    private static readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Semaphore used to wake the polling loop immediately when a scan is manually triggered.
    /// TriggerScan broadcasts a SignalR "ScanQueued" message; the service subscribes and releases this.
    /// </summary>
    public static readonly SemaphoreSlim WakeSignal = new SemaphoreSlim(0, 1);

    public ScannerBackgroundService(IServiceProvider ServiceProvider, ILogger<ScannerBackgroundService> Logger)
    {
        _serviceProvider = ServiceProvider;
        _logger = Logger;
    }

    /// <summary>
    /// Broadcasts scan completion to connected SignalR clients.
    /// </summary>
    private async Task BroadcastComplete(IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext,
        DataObjects.ScanRun ScanRun, string SiteName){
        try {
            DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
                TenantId = ScanRun.TenantId,
                ItemId = ScanRun.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanComplete,
                Message = "ScanComplete",
                Object = ScanRun,
            };

            if (ScanRun.TenantId != Guid.Empty) {
                await HubContext.Clients.Group(ScanRun.TenantId.ToString()).SignalRUpdate(update);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error broadcasting scan completion");
        }
    }

    /// <summary>
    /// Broadcasts scan failure to connected SignalR clients.
    /// </summary>
    private async Task BroadcastFailed(IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext,
        DataObjects.ScanRun ScanRun, string ErrorMessage){
        try {
            DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
                TenantId = ScanRun.TenantId,
                ItemId = ScanRun.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanFailed,
                Message = ErrorMessage,
                Object = ScanRun,
            };

            if (ScanRun.TenantId != Guid.Empty) {
                await HubContext.Clients.Group(ScanRun.TenantId.ToString()).SignalRUpdate(update);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error broadcasting scan failure");
        }
    }

    /// <summary>
    /// Broadcasts a detailed log entry to connected SignalR clients.
    /// Used to stream real-time console-like output to the browser.
    /// </summary>
    private async Task BroadcastLog(IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext,
        DataObjects.ScanRun ScanRun, string Level, string Category, string Message){
        try {
            DataObjects.ScanLogEntry logEntry = new DataObjects.ScanLogEntry {
                ScanRunId = ScanRun.ScanRunId,
                SiteId = ScanRun.SiteId,
                Timestamp = DateTime.UtcNow,
                Level = Level,
                Category = Category,
                Message = Message,
            };

            DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
                TenantId = ScanRun.TenantId,
                ItemId = ScanRun.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanLog,
                Message = Message,
                Object = logEntry,
            };

            if (ScanRun.TenantId != Guid.Empty) {
                await HubContext.Clients.Group(ScanRun.TenantId.ToString()).SignalRUpdate(update);
            }
        } catch {
            // Log broadcasting is best-effort.
        }
    }

    /// <summary>
    /// Broadcasts scan progress to connected SignalR clients.
    /// </summary>
    private async Task BroadcastProgress(IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext,
        DataObjects.ScanRun ScanRun, string SiteName, int CurrentPage, int TotalPages, string CurrentUrl, string Message,
        int PagesScanned = 0, int TotalViolations = 0, int CriticalCount = 0, int SeriousCount = 0,
        int ModerateCount = 0, int MinorCount = 0, DataObjects.PageScanResult? CompletedPageResult = null){
        try {
            DataObjects.ScanProgress progress = new DataObjects.ScanProgress {
                ScanRunId = ScanRun.ScanRunId,
                SiteId = ScanRun.SiteId,
                SiteName = SiteName,
                CurrentPage = CurrentPage,
                TotalPages = TotalPages,
                CurrentUrl = CurrentUrl,
                Message = Message,
                PagesScanned = PagesScanned,
                TotalViolations = TotalViolations,
                CriticalCount = CriticalCount,
                SeriousCount = SeriousCount,
                ModerateCount = ModerateCount,
                MinorCount = MinorCount,
                CompletedPageResult = CompletedPageResult,
            };

            DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
                TenantId = ScanRun.TenantId,
                ItemId = ScanRun.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanProgress,
                Message = "ScanProgress",
                Object = progress,
            };

            if (ScanRun.TenantId != Guid.Empty) {
                await HubContext.Clients.Group(ScanRun.TenantId.ToString()).SignalRUpdate(update);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error broadcasting scan progress");
        }
    }

    /// <summary>
    /// Broadcasts scan started (Queued→Running transition) to connected SignalR clients.
    /// </summary>
    private async Task BroadcastStarted(IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext,
        DataObjects.ScanRun ScanRun, string SiteName){
        try {
            ScanRun.SiteName = SiteName;
            DataObjects.SignalRUpdate update = new DataObjects.SignalRUpdate {
                TenantId = ScanRun.TenantId,
                ItemId = ScanRun.ScanRunId,
                UpdateType = DataObjects.SignalRUpdateType.ScanStarted,
                Message = "ScanStarted",
                Object = ScanRun,
            };

            if (ScanRun.TenantId != Guid.Empty) {
                await HubContext.Clients.Group(ScanRun.TenantId.ToString()).SignalRUpdate(update);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error broadcasting scan started");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScannerBackgroundService started");

        // Startup delay — let other services initialize before scanning.
        await Task.Delay(_startupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using (IServiceScope scope = _serviceProvider.CreateScope()) {
                    IDataAccess da = scope.ServiceProvider.GetRequiredService<IDataAccess>();
                    IConfigurationHelper config = scope.ServiceProvider.GetRequiredService<IConfigurationHelper>();
                    IHubContext<FreeAccessibilityCheckerHub, IsrHub> hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<FreeAccessibilityCheckerHub, IsrHub>>();

                    // Process manually triggered scans (Status = "Queued").
                    List<DataObjects.ScanRun> pendingScans = await GetPendingScans(da);
                    foreach (DataObjects.ScanRun scanRun in pendingScans) {
                        if (stoppingToken.IsCancellationRequested) { break; }
                        await RunScan(scanRun, da, config, hubContext, stoppingToken);
                    }

                    // Process scheduled scans based on cron expressions.
                    List<DataObjects.Site> dueForScan = await GetSitesDueForScan(da);
                    foreach (DataObjects.Site site in dueForScan) {
                        if (stoppingToken.IsCancellationRequested) { break; }

                        DataObjects.ScanRun newRun = new DataObjects.ScanRun {
                            SiteId = site.SiteId,
                            TenantId = site.TenantId,
                            Status = "Queued",
                            TriggeredBy = "Scheduled",
                            StartedAt = DateTime.UtcNow,
                        };
                        DataObjects.ScanRun savedRun = await da.SaveScanRun(newRun);
                        if (savedRun.ActionResponse.Result) {
                            await RunScan(savedRun, da, config, hubContext, stoppingToken);
                        }
                    }
                }
            } catch (OperationCanceledException) {
                // Graceful shutdown.
                break;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in ScannerBackgroundService polling loop");
            }

            // Wait before next check — or wake immediately when TriggerScan signals.
            try {
                await WakeSignal.WaitAsync(_pollingInterval, stoppingToken);
            } catch (OperationCanceledException) {
                break;
            }
        }

        _logger.LogInformation("ScannerBackgroundService stopped");
    }

    /// <summary>
    /// Gets scan runs that were manually triggered and have Status = "Queued".
    /// </summary>
    private async Task<List<DataObjects.ScanRun>> GetPendingScans(IDataAccess Da)
    {
        List<DataObjects.ScanRun> output = new List<DataObjects.ScanRun>();

        try {
            output = await Da.GetPendingScanRuns();
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error getting pending scans");
        }

        return output;
    }

    /// <summary>
    /// Gets all enabled sites whose cron schedule indicates they are due for a scan.
    /// </summary>
    private async Task<List<DataObjects.Site>> GetSitesDueForScan(IDataAccess Da)
    {
        List<DataObjects.Site> output = new List<DataObjects.Site>();

        try {
            // Load all sites across all tenants by passing Guid.Empty.
            // The DataAccess will return enabled sites.
            List<DataObjects.Site> allSites = await Da.GetSites(null, Guid.Empty);
            foreach (DataObjects.Site site in allSites) {
                if (IsSiteDueForScan(site)) {
                    output.Add(site);
                }
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error getting sites due for scan");
        }

        return output;
    }

    /// <summary>
    /// Evaluates whether a site's cron schedule indicates it is due for a new scan.
    /// Compares cron expression against LastScanAt to determine if a new scan should start.
    /// </summary>
    private bool IsSiteDueForScan(DataObjects.Site Site)
    {
        if (!Site.Enabled) { return false; }
        if (String.IsNullOrWhiteSpace(Site.ScanScheduleCron)) { return false; }

        try {
            CrontabSchedule schedule = CrontabSchedule.Parse(Site.ScanScheduleCron);
            DateTime lastScan = Site.LastScanAt ?? DateTime.MinValue;
            DateTime nextOccurrence = schedule.GetNextOccurrence(lastScan);
            return nextOccurrence <= DateTime.UtcNow;
        } catch (Exception) {
            // Invalid cron expression — skip this site.
            return false;
        }
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (String.IsNullOrWhiteSpace(message)) return "";
        message = message.Replace("\n", " ").Replace("\r", "").Trim();
        if (message.Length <= maxLength) return message;
        return message[..maxLength] + "...";
    }
}

// ---- FreeAccessibilityChecker.App.Services.Scanner.Run.cs ----
/// <summary>The scan run itself: opens each page, runs the tools, saves results and artifacts.</summary>
public partial class ScannerBackgroundService
{
    private static string GetContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch {
            ".jpeg" or ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".json" => "application/json",
            ".html" or ".htm" => "text/html",
            ".md" => "text/markdown",
            ".log" or ".txt" => "text/plain",
            _ => "application/octet-stream",
        };
    }

    /// <summary>
    /// Orchestrates a full scan run: loads site config from DB, builds a ScanConfig
    /// for the shared Scanner library, delegates scanning to ScannerEngine, stores
    /// results, and broadcasts SignalR progress.
    /// </summary>
    private async Task RunScan(DataObjects.ScanRun ScanRun, IDataAccess Da, IConfigurationHelper Config,
        IHubContext<FreeAccessibilityCheckerHub, IsrHub> HubContext, CancellationToken StoppingToken){
        Microsoft.Playwright.IBrowser? browser = null;

        try {
            await BroadcastLog(HubContext, ScanRun, "info", "system", "Starting scan run " + ScanRun.ScanRunId.ToString()[..8] + "...");

            // Load site config from database.
            List<DataObjects.Site> sites = await Da.GetSites(new List<Guid> { ScanRun.SiteId }, ScanRun.TenantId);
            if (!sites.Any()) {
                _logger.LogWarning("Site {SiteId} not found for scan run {ScanRunId}", ScanRun.SiteId, ScanRun.ScanRunId);
                await BroadcastLog(HubContext, ScanRun, "error", "system", "Site not found in database. Aborting scan.");
                ScanRun.Status = "Failed";
                ScanRun.CompletedAt = DateTime.UtcNow;
                await Da.SaveScanRun(ScanRun);
                return;
            }
            DataObjects.Site site = sites.First();
            await BroadcastLog(HubContext, ScanRun, "info", "system", "Loaded site configuration for " + site.Name + " (" + site.BaseUrl + ")");

            // Load site pages from database.
            List<DataObjects.SitePage> pages = await Da.GetSitePages(null, ScanRun.SiteId);
            List<DataObjects.SitePage> enabledPages = pages.Where(p => p.IncludeInScan).ToList();

            if (!enabledPages.Any()) {
                _logger.LogWarning("No enabled pages for site {SiteId}", ScanRun.SiteId);
                await BroadcastLog(HubContext, ScanRun, "warning", "system", "No pages configured for scanning. Marking scan as complete.");
                ScanRun.Status = "Complete";
                ScanRun.CompletedAt = DateTime.UtcNow;
                await Da.SaveScanRun(ScanRun);
                return;
            }
            await BroadcastLog(HubContext, ScanRun, "info", "system", "Found " + enabledPages.Count + " page(s) to scan");

            // Load credentials from database.
            List<DataObjects.SiteCredential> credentials = await Da.GetSiteCredentials(null, ScanRun.SiteId);
            if (credentials.Any()) {
                await BroadcastLog(HubContext, ScanRun, "info", "auth", "Credentials found — will attempt authentication");
            }

            // Update total pages.
            ScanRun.TotalPages = enabledPages.Count;
            ScanRun.Status = "Running";
            await Da.SaveScanRun(ScanRun);
            await BroadcastStarted(HubContext, ScanRun, site.Name);

            // Launch Playwright browser.
            await BroadcastLog(HubContext, ScanRun, "info", "system", "Launching headless browser...");
            Microsoft.Playwright.IPlaywright playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions {
                Headless = Config.ScanHeadless,
            });
            await BroadcastLog(HubContext, ScanRun, "success", "system", "Browser launched (Chromium, headless=" + Config.ScanHeadless + ")");

            Microsoft.Playwright.BrowserNewContextOptions contextOptions = new Microsoft.Playwright.BrowserNewContextOptions();
            if (!String.IsNullOrWhiteSpace(Config.ScanUserAgent)) {
                contextOptions.UserAgent = Config.ScanUserAgent;
                await BroadcastLog(HubContext, ScanRun, "detail", "system", "Custom User-Agent: " + Config.ScanUserAgent);
            }

            Microsoft.Playwright.IBrowserContext context = await browser.NewContextAsync(contextOptions);

            // Authenticate if credentials exist (still uses local ScannerAuth for DB-backed credential decryption).
            if (credentials.Any()) {
                DataObjects.SiteCredential credential = credentials.First();
                await BroadcastLog(HubContext, ScanRun, "info", "auth", "Authenticating with " + credential.AuthType + " credentials...");
                bool authResult = await ScannerDbAuth.AuthenticateAsync(context, site, credential, Da, Config);
                if (authResult) {
                    await BroadcastLog(HubContext, ScanRun, "success", "auth", "Authentication successful");
                } else {
                    _logger.LogWarning("Authentication failed for site {SiteName}", site.Name);
                    await BroadcastLog(HubContext, ScanRun, "warning", "auth", "Authentication failed — continuing without credentials");
                }
            }

            // Build shared Scanner library config.
            ScanConfig scanConfig = new ScanConfig {
                SettleDelayMs = Config.ScanSettleDelayMs > 0 ? Config.ScanSettleDelayMs : 5000,
                TimeoutMs = Config.ScanTimeoutMs > 0 ? Config.ScanTimeoutMs : 30000,
                Headless = Config.ScanHeadless,
                WcagLevel = !String.IsNullOrWhiteSpace(Config.ScanWcagLevel) ? Config.ScanWcagLevel : "wcag21aa",
                MaxConcurrency = site.MaxConcurrency > 0 ? site.MaxConcurrency : Config.ScanMaxConcurrency,
            };
            if (scanConfig.MaxConcurrency <= 0) { scanConfig.MaxConcurrency = 5; }

            await BroadcastLog(HubContext, ScanRun, "detail", "system",
                "Scan config: timeout=" + scanConfig.TimeoutMs + "ms, settle=" + scanConfig.SettleDelayMs + "ms, " +
                "wcag=" + scanConfig.WcagLevel + ", concurrency=" + scanConfig.MaxConcurrency);

            // Build shared Scanner library site config from DB objects.
            Scanner.Models.SiteConfig siteConfig = new Scanner.Models.SiteConfig {
                BaseUrl = site.BaseUrl,
                Pages = enabledPages.Select(p => new PageConfig { Path = p.Path }).ToList(),
            };

            await BroadcastLog(HubContext, ScanRun, "info", "system", "Beginning scan of " + enabledPages.Count + " pages...");
            await BroadcastLog(HubContext, ScanRun, "detail", "system", "─────────────────────────────────────────");

            // Scan pages using the shared Scanner library, with SignalR progress callback.
            int currentPage = 0;
            int totalViolations = 0;
            int totalCritical = 0;
            int totalSerious = 0;
            int totalModerate = 0;
            int totalMinor = 0;
            SemaphoreSlim semaphore = new SemaphoreSlim(scanConfig.MaxConcurrency, scanConfig.MaxConcurrency);

            foreach (DataObjects.SitePage sitePage in enabledPages) {
                if (StoppingToken.IsCancellationRequested) { break; }

                await semaphore.WaitAsync(StoppingToken);
                try {
                    currentPage++;
                    // Build full URL.
                    //   SitePage.Path = "/" → scan the BaseUrl itself (the site's "home").
                    //   SitePage.Path starts with "/..." (anything other than just "/") → absolute
                    //     from the domain root (origin + path), not appended to BaseUrl. This is
                    //     what the scanner stores when it discovers same-host links via
                    //     ScannerEngine.DiscoverLinks (which returns Uri.AbsolutePath).
                    //   Anything else → relative to BaseUrl.
                    string baseUrlTrimmed = site.BaseUrl.TrimEnd('/');
                    Uri baseUri = new Uri(baseUrlTrimmed);
                    string pagePath = sitePage.Path;

                    string fullUrl;
                    if (string.IsNullOrEmpty(pagePath) || pagePath == "/") {
                        fullUrl = baseUrlTrimmed + "/";
                    } else if (pagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                               pagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                        fullUrl = pagePath;
                    } else if (pagePath.StartsWith("/")) {
                        fullUrl = baseUri.GetLeftPart(UriPartial.Authority) + pagePath;
                    } else {
                        fullUrl = baseUrlTrimmed + "/" + pagePath;
                    }

                    // Broadcast SignalR progress.
                    await BroadcastProgress(HubContext, ScanRun, site.Name, currentPage, enabledPages.Count, fullUrl, "Scanning " + sitePage.Path);
                    await BroadcastLog(HubContext, ScanRun, "info", "nav",
                        "[" + currentPage + "/" + enabledPages.Count + "] Navigating to " + fullUrl);

                    // Scan the page using the shared Scanner library.
                    // No disk output directory needed — everything stays in memory.
                    string pageOutputDir = "";

                    Scanner.Models.ScanConfig pageScanConfig = new Scanner.Models.ScanConfig {
                        TimeoutMs = scanConfig.TimeoutMs,
                        SettleDelayMs = scanConfig.SettleDelayMs,
                        WcagLevel = scanConfig.WcagLevel,
                        OutputDir = pageOutputDir,
                        Headless = scanConfig.Headless,
                        MaxConcurrency = 1,
                    };

                    // Call the SHARED ScannerEngine.ScanPage() — the canonical scanning logic.
                    // Create a fresh page per URL from the authenticated browser context.
                    Microsoft.Playwright.IPage page = await context.NewPageAsync();
                    Scanner.Models.PageScanResult scanOutput;
                    try {
                        scanOutput = await Scanner.ScannerEngine.ScanPage(
                            page, pageScanConfig, fullUrl, pageOutputDir);
                    } finally {
                        await page.CloseAsync();
                    }

                    // Log navigation result.
                    if (scanOutput.StatusCode > 0) {
                        string statusLevel = scanOutput.StatusCode >= 400 ? "warning" : "success";
                        await BroadcastLog(HubContext, ScanRun, statusLevel, "nav",
                            "  HTTP " + scanOutput.StatusCode + " — \"" + (scanOutput.PageTitle ?? "Untitled") + "\" (" +
                            ScanPaths.FormatBytes(scanOutput.HtmlSizeBytes) + ", " + scanOutput.DurationMs + "ms)");
                    } else {
                        await BroadcastLog(HubContext, ScanRun, "error", "nav",
                            "  Failed to load page (status code: " + scanOutput.StatusCode + ")");
                    }

                    // Log tool results from the shared library's merged summary.
                    if (scanOutput.Summary.AxeCount > 0) {
                        await BroadcastLog(HubContext, ScanRun, "warning", "axe",
                            "  axe-core: " + scanOutput.Summary.AxeCount + " violation(s) found");
                    } else {
                        await BroadcastLog(HubContext, ScanRun, "success", "axe",
                            "  axe-core: No violations found ✓");
                    }

                    if (scanOutput.Summary.HtmlCheckCount > 0) {
                        await BroadcastLog(HubContext, ScanRun, "warning", "htmlcheck",
                            "  HTML checks: " + scanOutput.Summary.HtmlCheckCount + " issue(s) found");
                    } else {
                        await BroadcastLog(HubContext, ScanRun, "success", "htmlcheck",
                            "  HTML checks: All checks passed ✓");
                    }

                    if (scanOutput.Summary.HtmlCsCount > 0) {
                        await BroadcastLog(HubContext, ScanRun, "info", "htmlcs",
                            "  HTML_CodeSniffer: " + scanOutput.Summary.HtmlCsCount + " issue(s) found");
                    }

                    if (scanOutput.Summary.IbmCount > 0) {
                        await BroadcastLog(HubContext, ScanRun, "info", "ibm",
                            "  IBM Equal Access: " + scanOutput.Summary.IbmCount + " issue(s) found");
                    }

                    // Collect all violations from all tools into a flat list for DB storage.
                    List<Scanner.Models.A11yIssue> allIssues = new List<Scanner.Models.A11yIssue>();
                    allIssues.AddRange(scanOutput.AxeIssues);
                    allIssues.AddRange(scanOutput.HtmlCheckIssues);
                    allIssues.AddRange(scanOutput.HtmlCsIssues);
                    allIssues.AddRange(scanOutput.IbmIssues);

                    // Log violation summary for this page.
                    int pageTotal = allIssues.Count;
                    int pageCritical = allIssues.Count(v => v.Severity == "critical");
                    int pageSerious = allIssues.Count(v => v.Severity == "serious");
                    int pageModerate = allIssues.Count(v => v.Severity == "moderate");
                    int pageMinor = allIssues.Count(v => v.Severity == "minor");

                    if (pageTotal > 0) {
                        string summary = "  Summary: " + pageTotal + " violation(s) —";
                        if (pageCritical > 0) summary += " " + pageCritical + " critical";
                        if (pageSerious > 0) summary += " " + pageSerious + " serious";
                        if (pageModerate > 0) summary += " " + pageModerate + " moderate";
                        if (pageMinor > 0) summary += " " + pageMinor + " minor";
                        string level = pageCritical > 0 ? "error" : pageSerious > 0 ? "warning" : "info";
                        await BroadcastLog(HubContext, ScanRun, level, "result", summary);

                        // Log individual critical/serious violations as detail lines.
                        foreach (var v in allIssues.Where(v => v.Severity == "critical" || v.Severity == "serious").Take(5)) {
                            await BroadcastLog(HubContext, ScanRun, v.Severity == "critical" ? "error" : "warning", "result",
                                "    [" + v.Severity.ToUpper() + "] " + v.RuleId + ": " + TruncateMessage(v.Message, 120));
                        }
                        int remainingCritSerious = allIssues.Count(v => v.Severity == "critical" || v.Severity == "serious") - 5;
                        if (remainingCritSerious > 0) {
                            await BroadcastLog(HubContext, ScanRun, "detail", "result",
                                "    ... and " + remainingCritSerious + " more critical/serious violation(s)");
                        }
                    } else {
                        await BroadcastLog(HubContext, ScanRun, "success", "result",
                            "  No violations found on this page ✓");
                    }

                    totalViolations += pageTotal;
                    totalCritical += pageCritical;
                    totalSerious += pageSerious;
                    totalModerate += pageModerate;
                    totalMinor += pageMinor;

                    // Save page result to database.
                    DataObjects.PageScanResult pageResult = new DataObjects.PageScanResult {
                        ScanRunId = ScanRun.ScanRunId,
                        SitePageId = sitePage.SitePageId,
                        Url = fullUrl,
                        StatusCode = scanOutput.StatusCode,
                        PageTitle = scanOutput.PageTitle,
                        HtmlSizeBytes = scanOutput.HtmlSizeBytes,
                        AxeViolationCount = scanOutput.Summary.AxeCount,
                        HtmlCheckViolationCount = scanOutput.Summary.HtmlCheckCount,
                        OverlayViolationCount = scanOutput.Summary.HtmlCsCount + scanOutput.Summary.IbmCount,
                        TotalViolations = allIssues.Count,
                        CriticalCount = pageCritical,
                        SeriousCount = pageSerious,
                        ModerateCount = pageModerate,
                        MinorCount = pageMinor,
                        ScanDurationMs = scanOutput.DurationMs,
                        ErrorMessage = !String.IsNullOrEmpty(scanOutput.ErrorMessage) ? scanOutput.ErrorMessage : null,
                        Language = !String.IsNullOrEmpty(scanOutput.Language) ? scanOutput.Language : null,
                        ResponseHeaders = scanOutput.ResponseHeaders.Any()
                            ? System.Text.Json.JsonSerializer.Serialize(scanOutput.ResponseHeaders) : null,
                        AxePassCount = scanOutput.AxePassCount,
                        AxeIncompleteCount = scanOutput.AxeIncompleteCount,
                        AxeInapplicableCount = scanOutput.AxeInapplicableCount,
                        IbmPassCount = scanOutput.IbmPassCount,
                        IbmPotentialCount = scanOutput.IbmPotentialCount,
                        IbmManualCount = scanOutput.IbmManualCount,
                        HtmlCheckPassCount = scanOutput.HtmlCheckPassCount,
                        HtmlCheckTotalChecks = scanOutput.HtmlCheckTotalChecks,
                        HeadingsJson = !String.IsNullOrEmpty(scanOutput.HeadingsJson) ? scanOutput.HeadingsJson : null,
                        LandmarksJson = !String.IsNullOrEmpty(scanOutput.LandmarksJson) ? scanOutput.LandmarksJson : null,
                        MediaJson = !String.IsNullOrEmpty(scanOutput.MediaJson) ? scanOutput.MediaJson : null,
                        AriaLiveRegionsJson = !String.IsNullOrEmpty(scanOutput.AriaLiveRegionsJson) ? scanOutput.AriaLiveRegionsJson : null,
                        TargetSizeJson = !String.IsNullOrEmpty(scanOutput.TargetSizeJson) ? scanOutput.TargetSizeJson : null,
                        PerformanceJson = !String.IsNullOrEmpty(scanOutput.PerformanceJson) ? scanOutput.PerformanceJson : null,
                        AccessibilityTreeJson = !String.IsNullOrEmpty(scanOutput.AccessibilityTreeJson) ? scanOutput.AccessibilityTreeJson : null,
                        KeyboardNavJson = !String.IsNullOrEmpty(scanOutput.KeyboardNavJson) ? scanOutput.KeyboardNavJson : null,
                        FocusTrapsJson = !String.IsNullOrEmpty(scanOutput.FocusTrapsJson) ? scanOutput.FocusTrapsJson : null,
                        TextSpacingJson = !String.IsNullOrEmpty(scanOutput.TextSpacingJson) ? scanOutput.TextSpacingJson : null,
                        ReadingLevelJson = !String.IsNullOrEmpty(scanOutput.ReadingLevelJson) ? scanOutput.ReadingLevelJson : null,
                        AutocompleteJson = !String.IsNullOrEmpty(scanOutput.AutocompleteJson) ? scanOutput.AutocompleteJson : null,
                        FixedElementsJson = !String.IsNullOrEmpty(scanOutput.FixedElementsJson) ? scanOutput.FixedElementsJson : null,
                        MobileViewportsJson = !String.IsNullOrEmpty(scanOutput.MobileViewportsJson) ? scanOutput.MobileViewportsJson : null,
                    };
                    DataObjects.PageScanResult savedPageResult = await Da.SavePageScanResult(pageResult);

                    // Map shared library A11yIssue → DataObjects.A11yViolation for DB storage.
                    if (savedPageResult.ActionResponse.Result && allIssues.Any()) {
                        List<DataObjects.A11yViolation> violations = allIssues.Select(issue => new DataObjects.A11yViolation {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            RuleId = issue.RuleId,
                            CanonicalRuleId = issue.CanonicalRuleId,
                            Severity = issue.Severity,
                            Message = issue.Message,
                            Selector = issue.Selector,
                            HtmlSnippet = issue.Snippet,
                            HelpUrl = issue.HelpUrl,
                            WcagCriteria = issue.WcagCriteria,
                            Tool = issue.Tool,
                            ContrastForeground = issue.ContrastForeground,
                            ContrastBackground = issue.ContrastBackground,
                            ContrastRatio = issue.ContrastRatio,
                            ContrastExpected = issue.ContrastExpected,
                            ContrastFontSize = issue.ContrastFontSize,
                            ContrastFontWeight = issue.ContrastFontWeight,
                        }).ToList();
                        await SaveViolations(Da, violations);
                    }

                    // Persist screenshots to DB (bytes stored in Data column).
                    if (savedPageResult.ActionResponse.Result && scanOutput.Screenshots.Any()) {
                        List<DataObjects.ScanScreenshot> screenshots = scanOutput.Screenshots.Select(s => new DataObjects.ScanScreenshot {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            Path = !string.IsNullOrEmpty(s.Path) ? Path.GetFileName(s.Path) : (s.Label ?? "screenshot") + ".jpeg",
                            Label = s.Label,
                            SizeBytes = s.SizeBytes,
                            ContentType = !String.IsNullOrEmpty(s.ContentType) ? s.ContentType : GetContentType(s.Path),
                            Data = s.Data,
                        }).ToList();
                        await Da.SaveScreenshots(screenshots);
                    }

                    // Persist JSON/HTML/MD artifacts to DB (bytes stored in Data column).
                    if (savedPageResult.ActionResponse.Result && scanOutput.Artifacts.Any()) {
                        List<DataObjects.ScanArtifact> artifacts = scanOutput.Artifacts.Select(a => new DataObjects.ScanArtifact {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            FileName = a.FileName,
                            Path = string.Empty,
                            ContentType = a.ContentType,
                            Data = a.Data,
                            SizeBytes = a.Data != null ? a.Data.Length : 0,
                        }).ToList();
                        await Da.SaveArtifacts(artifacts);
                    }

                    // Persist image catalog (alt text audit) to DB.
                    if (savedPageResult.ActionResponse.Result && scanOutput.Images.Any()) {
                        List<DataObjects.ScanImage> images = scanOutput.Images.Select(i => new DataObjects.ScanImage {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            Url = i.Url,
                            AltText = i.AltText,
                            HasAlt = i.HasAlt,
                        }).ToList();
                        await Da.SaveImages(images);
                    }

                    // Persist SSL certificate info to DB.
                    if (savedPageResult.ActionResponse.Result && scanOutput.Certificate != null) {
                        DataObjects.ScanCertificate cert = new DataObjects.ScanCertificate {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            Subject = scanOutput.Certificate.Subject,
                            Issuer = scanOutput.Certificate.Issuer,
                            Expiry = scanOutput.Certificate.Expiry,
                            SubjectAlternativeNames = scanOutput.Certificate.SubjectAlternativeNames != null
                                ? String.Join(", ", scanOutput.Certificate.SubjectAlternativeNames) : null,
                        };
                        await Da.SaveCertificate(cert);
                    }

                    // Persist cross-tool consensus ranked rules to DB.
                    if (savedPageResult.ActionResponse.Result && scanOutput.Summary.RankedRules.Any()) {
                        List<DataObjects.ScanRankedRule> rankedRules = scanOutput.Summary.RankedRules.Select(r => new DataObjects.ScanRankedRule {
                            PageScanResultId = savedPageResult.PageScanResultId,
                            CanonicalRuleId = r.CanonicalRuleId,
                            Severity = r.Severity,
                            Consensus = r.Consensus,
                            Confidence = r.Confidence,
                            ToolsFound = String.Join(", ", r.ToolsFound),
                        }).ToList();
                        await Da.SaveRankedRules(rankedRules);
                    }

                    // Broadcast progress with running totals and completed page result.
                    await BroadcastProgress(HubContext, ScanRun, site.Name, currentPage, enabledPages.Count, fullUrl,
                        "Completed " + sitePage.Path,
                        currentPage, totalViolations, totalCritical, totalSerious, totalModerate, totalMinor,
                        savedPageResult);

                    // Save discovered links from the page scan.
                    // Same-host paths and cross-host URLs both become new disabled top-level Sites,
                    // so the user can opt-in via bulk-enable on the Sites list.
                    if (scanOutput.DiscoveredPaths.Any()) {
                        await SaveDiscoveredPages(Da, site, scanOutput.DiscoveredPaths);
                    }
                    if (scanOutput.DiscoveredExternalUrls.Any()) {
                        await SaveDiscoveredSites(Da, site.TenantId, scanOutput.DiscoveredExternalUrls, site.BaseUrl);
                    }
                } finally {
                    semaphore.Release();
                }
            }

            // Mark scan as complete — copy accumulated counts into the DTO
            // so SaveScanRun doesn't overwrite the DB with zeros.
            ScanRun.Status = "Complete";
            ScanRun.CompletedAt = DateTime.UtcNow;
            ScanRun.PagesScanned = currentPage;
            ScanRun.TotalViolations = totalViolations;
            ScanRun.CriticalCount = totalCritical;
            ScanRun.SeriousCount = totalSerious;
            ScanRun.ModerateCount = totalModerate;
            ScanRun.MinorCount = totalMinor;
            await Da.SaveScanRun(ScanRun);

            // Log final summary.
            await BroadcastLog(HubContext, ScanRun, "detail", "system", "─────────────────────────────────────────");
            string finalLevel = totalCritical > 0 ? "error" : totalSerious > 0 ? "warning" : totalViolations > 0 ? "info" : "success";
            await BroadcastLog(HubContext, ScanRun, finalLevel, "system",
                "Scan complete — " + enabledPages.Count + " page(s) scanned, " + totalViolations + " total violation(s)" +
                (totalCritical > 0 ? " (" + totalCritical + " critical)" : "") +
                (totalSerious > 0 ? " (" + totalSerious + " serious)" : ""));

            TimeSpan elapsed = ScanRun.CompletedAt.Value - ScanRun.StartedAt;
            await BroadcastLog(HubContext, ScanRun, "success", "system",
                "Finished in " + elapsed.TotalSeconds.ToString("F1") + "s");

            // Broadcast completion.
            await BroadcastComplete(HubContext, ScanRun, site.Name);

            _logger.LogInformation("Scan completed for site {SiteName} ({ScanRunId})", site.Name, ScanRun.ScanRunId);
        } catch (Exception ex) {
            _logger.LogError(ex, "Scan failed for ScanRun {ScanRunId}", ScanRun.ScanRunId);

            await BroadcastLog(HubContext, ScanRun, "error", "system", "SCAN FAILED: " + ex.Message);

            ScanRun.Status = "Failed";
            ScanRun.CompletedAt = DateTime.UtcNow;
            try {
                await Da.SaveScanRun(ScanRun);
                await BroadcastFailed(HubContext, ScanRun, ex.Message);
            } catch (Exception innerEx) {
                _logger.LogError(innerEx, "Error updating failed scan status for {ScanRunId}", ScanRun.ScanRunId);
            }
        } finally {
            if (browser != null) {
                try { await browser.CloseAsync(); } catch { }
            }
        }
    }
}

// ---- FreeAccessibilityChecker.App.Services.Scanner.Discovery.cs ----
/// <summary>Saves the pages and external sites a scan discovers, and the violations it finds.</summary>
public partial class ScannerBackgroundService
{
    /// <summary>
    /// "EM EIT" + "https://em.wsu.edu/eit/help-desk" -> "EM EIT / help-desk".
    /// </summary>
    private static string BuildDiscoveredSiteName(string parentName, string fullUrl)
    {
        try {
            Uri u = new Uri(fullUrl);
            string lastSegment = u.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? u.Host;
            return string.IsNullOrWhiteSpace(parentName)
                ? lastSegment
                : parentName + " / " + lastSegment;
        } catch {
            return string.IsNullOrWhiteSpace(parentName) ? "Discovered" : parentName + " / discovered";
        }
    }

    /// <summary>
    /// Combines a site BaseUrl with a discovered path. Discovered paths come from
    /// <see cref="Uri.AbsolutePath"/> (see ScannerEngine.DiscoverLinks) so anything starting
    /// with "/" is absolute-from-domain-root and must be combined with the origin only —
    /// NEVER appended to the parent BaseUrl's path component.
    /// </summary>
    private static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return baseUrl.TrimEnd('/');

        try {
            Uri baseUri = new Uri(baseUrl.TrimEnd('/'));
            string origin = baseUri.GetLeftPart(UriPartial.Authority); // https://em.wsu.edu

            // Already-absolute URL.
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                return path.TrimEnd('/');
            }

            // Absolute-from-domain-root path — combine with origin only.
            // Bug fix: previously this would do `baseUrl + path` and produce things like
            // `https://em.wsu.edu/eit/eit-help-desk/eit/` from parent `/eit/eit-help-desk`
            // + discovered `/eit/`. The scanner returns AbsolutePath, never relative paths.
            if (path.StartsWith("/")) {
                return (origin + path).TrimEnd('/');
            }

            // Truly relative path (rare for discovered links). Resolve against base.
            return new Uri(baseUri, path).AbsoluteUri.TrimEnd('/');
        } catch {
            return string.Empty;
        }
    }

    /// <summary>
    /// Saves each discovered same-host path as a new top-level <see cref="DataObjects.Site"/>
    /// record with <c>Enabled = false</c>. The user uses bulk-enable tools on the Sites list to
    /// opt in to scanning each newly-found URL. Duplicates (against any existing Site BaseUrl
    /// in the tenant) are skipped.
    /// </summary>
    private async Task SaveDiscoveredPages(IDataAccess Da, DataObjects.Site ParentSite, List<string> DiscoveredPaths)
    {
        // Same-host links discovered during a scan belong to the parent Site as new
        // SitePages — NOT as new sibling Sites. The previous version of this method
        // created Site rows, polluting SiteList with one row per discovered URL and
        // breaking the mental model (the user expects "site = origin/host/sub-app",
        // "page = path within that site"). Discovered paths come from
        // ScannerEngine's link-extraction loop already classified as same-host.
        try {
            // Load existing pages for the parent so we don't add duplicates.
            List<DataObjects.SitePage> existing = await Da.GetSitePages(null, ParentSite.SiteId);
            HashSet<string> existingPaths = new HashSet<string>(
                existing.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

            int sortOrder = existing.Count;
            List<DataObjects.SitePage> newPages = new List<DataObjects.SitePage>();

            // The parent's own path-prefix (origin + base path) — used to convert an
            // absolute discovered path into the path RELATIVE to the parent's BaseUrl.
            // Example: parent BaseUrl = https://flex.em.wsu.edu/Touchpoints, discovered
            // absolute path "/Touchpoints/SFS/About" becomes "/SFS/About" stored on the
            // SitePage. For root-domain parents (BaseUrl = https://em.wsu.edu) the
            // discovered path is already correct as-is.
            string parentBasePath = "/";
            try { parentBasePath = new Uri(ParentSite.BaseUrl.TrimEnd('/')).AbsolutePath.TrimEnd('/'); } catch { }
            if (string.IsNullOrEmpty(parentBasePath)) parentBasePath = "";

            foreach (string discovered in DiscoveredPaths) {
                if (string.IsNullOrWhiteSpace(discovered)) continue;

                // discovered is AbsolutePath from ScannerEngine.DiscoverLinks (always
                // starts with "/"). Strip the parent's base-path prefix (if present)
                // so the stored Path is relative to the parent's BaseUrl, matching
                // how the seed routes + the URL builder in ScannerEngine.ScanSite work.
                string relativePath = discovered;
                if (!string.IsNullOrEmpty(parentBasePath)
                    && discovered.StartsWith(parentBasePath, StringComparison.OrdinalIgnoreCase)) {
                    relativePath = discovered.Substring(parentBasePath.Length);
                    if (string.IsNullOrEmpty(relativePath)) relativePath = "/";
                }
                if (!relativePath.StartsWith("/")) relativePath = "/" + relativePath;

                // Skip the parent's own root and any duplicates.
                if (relativePath == "/" && existing.Any(p => p.Path == "/")) continue;
                if (existingPaths.Contains(relativePath)) continue;
                existingPaths.Add(relativePath); // dedupe within batch

                sortOrder++;
                newPages.Add(new DataObjects.SitePage {
                    SitePageId = Guid.Empty,
                    SiteId = ParentSite.SiteId,
                    Path = relativePath,
                    Title = string.Empty,
                    Enabled = false, // user opts in — same convention as seeded routes
                    RequiresAuth = false,
                    IncludeInScan = false,
                    SortOrder = sortOrder,
                });
            }

            if (newPages.Any()) {
                await Da.SaveSitePages(newPages);
                _logger.LogInformation("Discovered {Count} new pages under site {ParentName} ({ParentSiteId}) — added disabled, awaiting opt-in",
                    newPages.Count, ParentSite.Name, ParentSite.SiteId);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error saving discovered pages under {ParentSiteId}", ParentSite.SiteId);
        }
    }

    /// <summary>
    /// Saves discovered external links as new Site records with Enabled=false.
    /// These sites are added for awareness but NOT automatically scanned.
    /// Skips URLs that match the current site's own BaseUrl.
    /// </summary>
    private async Task SaveDiscoveredSites(IDataAccess Da, Guid TenantId, List<string> ExternalUrls, string CurrentSiteBaseUrl)
    {
        try {
            // Load all existing sites for this tenant to check for duplicates.
            List<DataObjects.Site> existingSites = await Da.GetSites(null, TenantId);
            HashSet<string> existingBaseUrls = new HashSet<string>(
                existingSites.Select(s => s.BaseUrl.TrimEnd('/').ToLower()), StringComparer.OrdinalIgnoreCase);

            string currentNormalized = CurrentSiteBaseUrl.TrimEnd('/').ToLower();

            List<DataObjects.Site> newSites = new List<DataObjects.Site>();

            foreach (string externalUrl in ExternalUrls) {
                string normalized = externalUrl.TrimEnd('/').ToLower();

                // Skip if it's the current site or already exists.
                if (normalized == currentNormalized) { continue; }
                if (existingBaseUrls.Contains(normalized)) { continue; }
                existingBaseUrls.Add(normalized); // prevent duplicates within same batch

                // Extract a readable name from the hostname.
                string name = "Discovered";
                try {
                    Uri uri = new Uri(externalUrl);
                    name = uri.Host;
                } catch { }

                newSites.Add(new DataObjects.Site {
                    SiteId = Guid.Empty,
                    TenantId = TenantId,
                    Name = name,
                    BaseUrl = externalUrl,
                    Enabled = false,  // NOT auto-scanned
                    PublicVisible = false,
                    MaxConcurrency = 5,
                });
            }

            if (newSites.Any()) {
                await Da.SaveSites(newSites);
                _logger.LogInformation("Discovered {Count} new external sites from scan", newSites.Count);
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Error saving discovered external sites");
        }
    }

    /// <summary>
    /// Saves a batch of violations to the database.
    /// </summary>
    private async Task SaveViolations(IDataAccess Da, List<DataObjects.A11yViolation> Violations)
    {
        await Da.SaveViolations(Violations);
    }
}

// ---- FreeAccessibilityChecker.App.Services.Scanner.Auth.cs ----
/// <summary>
/// Thin wrapper that decrypts DB-stored credentials and delegates to the shared
/// Scanner library's AuthHandler for actual authentication logic.
/// </summary>
public static class ScannerDbAuth
{
    public static async Task<bool> AuthenticateAsync(IBrowserContext Context, DataObjects.Site Site,
        DataObjects.SiteCredential Credential, IDataAccess Da, IConfigurationHelper Config){
        try {
            string plainPassword = Da.DecryptString(Credential.PasswordEncrypted, Config.ScanEncryptionKey);
            if (String.IsNullOrEmpty(plainPassword)) {
                return false;
            }

            var cred = new CredentialConfig {
                Username = Credential.Username,
                Password = plainPassword,
                AuthType = Credential.AuthType,
                TenantCode = Credential.TenantCode,
                LoginUrl = !String.IsNullOrWhiteSpace(Credential.LoginUrl)
                    ? Credential.LoginUrl
                    : Site.BaseUrl,
                UsernameSelector = Credential.UsernameSelector,
                PasswordSelector = Credential.PasswordSelector,
                SubmitSelector = Credential.SubmitSelector,
            };

            IPage page = Context.Pages.Count > 0 ? Context.Pages[0] : await Context.NewPageAsync();

            if (Credential.AuthType == "FreeCRM" || Site.IsFreeCRMApp) {
                return await AuthHandler.AuthenticateFreeCRM(page, cred);
            } else {
                return await AuthHandler.AuthenticateGeneric(page, cred);
            }
        } catch {
            return false;
        }
    }
}
