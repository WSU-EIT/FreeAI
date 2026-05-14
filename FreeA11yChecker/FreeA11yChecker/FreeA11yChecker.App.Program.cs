namespace FreeA11yChecker;

public partial class Program
{
    public static ConfigurationHelperLoader LoadAppConfiguration(ConfigurationHelperLoader Loader, WebApplicationBuilder Builder)
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



    public static WebApplication MyAppModifyEnd(WebApplication app)
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
    ///     dotnet user-secrets set "App:&lt;Name&gt;AdminUsername" "&lt;user&gt;" --project FreeA11yChecker
    ///     dotnet user-secrets set "App:&lt;Name&gt;AdminPassword" "&lt;pass&gt;" --project FreeA11yChecker
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
                        $"Set via: dotnet user-secrets set \"{seed.PasswordKey}\" \"<value>\" --project FreeA11yChecker");
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
