namespace FreeA11yChecker.Scanner.Models;

/// <summary>
/// Top-level configuration for a scan run. Contains global settings
/// (timeouts, concurrency, output) and a dictionary of sites to scan.
/// Both the web app and console CLI build this object before calling ScannerEngine.
/// </summary>
public class ScanConfig
{
    /// <summary>
    /// Milliseconds to wait after NetworkIdle for JS-heavy pages to finish rendering.
    /// </summary>
    public int SettleDelayMs { get; set; } = 5000;

    /// <summary>
    /// Maximum time in milliseconds to wait for page navigation. 60s matches the
    /// gold-standard pattern in Examples/FreeTools/BrowserSnapshot — Blazor WASM
    /// cold boot + heavy marketing-site analytics can both push past 30s.
    /// </summary>
    public int TimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Maximum number of pages to scan concurrently within a site.
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>
    /// Whether to run the browser in headless mode.
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Root output directory for scan results. Each site gets a subdirectory.
    /// </summary>
    public string OutputDir { get; set; } = "runs/latest";

    /// <summary>
    /// WCAG conformance level to test against (e.g., "wcag21aa", "wcag2a").
    /// Passed to axe-core's runOnly configuration.
    /// </summary>
    public string WcagLevel { get; set; } = "wcag21aa";

    /// <summary>
    /// Sites to scan, keyed by base URL. Each value contains page paths and credentials.
    /// </summary>
    public Dictionary<string, SiteConfig> Sites { get; set; } = new Dictionary<string, SiteConfig>();

    // ================================================================
    // Phase 3 — Expensive / Complex Checks (opt-in)
    // ================================================================

    /// <summary>
    /// Enable keyboard navigation simulation: tab through interactive elements,
    /// detect focus traps, missing focus indicators, and skip-to-content links.
    /// </summary>
    public bool EnableKeyboardNav { get; set; } = false;

    /// <summary>
    /// Enable text spacing override test: inject WCAG 1.4.12 spacing CSS
    /// and detect content clipping or overflow.
    /// </summary>
    public bool EnableTextSpacingTest { get; set; } = false;

    /// <summary>
    /// Enable reading level analysis: extract visible text content and
    /// compute Flesch-Kincaid readability scores (WCAG 3.1.5 AAA).
    /// </summary>
    public bool EnableReadingLevel { get; set; } = false;

    /// <summary>
    /// Enable autocomplete attribute audit: check input elements for
    /// appropriate autocomplete values per WCAG 1.3.5.
    /// </summary>
    public bool EnableAutocompleteAudit { get; set; } = false;

    /// <summary>
    /// Enable fixed/sticky element detection: find positioned elements
    /// that could obscure focused content (WCAG 2.4.11).
    /// </summary>
    public bool EnableFixedElementCheck { get; set; } = false;

    /// <summary>
    /// Enable mobile viewport scanning: re-scan at phone (375×667),
    /// tablet (768×1024), and desktop (1920×1080) viewports.
    /// WARNING: Triples scan time per page.
    /// </summary>
    public bool EnableMobileViewports { get; set; } = false;

    /// <summary>
    /// Maximum number of Tab presses during keyboard navigation simulation.
    /// Prevents infinite loops on pages with many interactive elements.
    /// </summary>
    public int KeyboardNavMaxTabs { get; set; } = 150;
}

/// <summary>
/// Configuration for a single site. Contains the base URL (used as the dictionary key),
/// the list of page paths to scan, and optional login credentials.
/// </summary>
public class SiteConfig
{
    /// <summary>
    /// Base URL for the site (e.g., "https://em.wsu.edu").
    /// Stored as the dictionary key in ScanConfig.Sites, but also available here for convenience.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Page paths to scan, relative to the base URL (e.g., "/", "/about", "/contact").
    /// </summary>
    public List<PageConfig> Pages { get; set; } = new List<PageConfig>();

    /// <summary>
    /// Optional credentials for scanning pages that require authentication.
    /// </summary>
    public CredentialConfig? Credentials { get; set; }
}

/// <summary>
/// Configuration for a single page within a site.
/// </summary>
public class PageConfig
{
    /// <summary>
    /// Relative path from the site base URL (e.g., "/", "/about", "/admin/users").
    /// </summary>
    public string Path { get; set; } = "/";

    /// <summary>
    /// Whether this page requires authentication before scanning.
    /// If true and credentials are configured on the site, auth runs before navigating.
    /// </summary>
    public bool RequiresAuth { get; set; } = false;
}

/// <summary>
/// Login credentials for authenticated scanning. Supports both FreeA11yChecker-native
/// login flows (known URL patterns, known selectors) and generic form detection.
/// </summary>
public class CredentialConfig
{
    /// <summary>
    /// Username or email address for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for login. In the web app this comes from encrypted storage;
    /// in the console CLI it comes from appsettings.json or command line.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Authentication type: "FreeCRM" for known FreeA11yChecker-style login pages,
    /// "Generic" for auto-detected login forms. Defaults to "Generic".
    /// </summary>
    public string AuthType { get; set; } = "Generic";

    /// <summary>
    /// Tenant code for multi-tenant applications. Used to build the login URL
    /// as BaseUrl/{TenantCode}/Login.
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom login URL. Overrides the default login URL detection.
    /// </summary>
    public string LoginUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom CSS selector for the username field.
    /// If empty, the selector cascade is used.
    /// </summary>
    public string UsernameSelector { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom CSS selector for the password field.
    /// If empty, the selector cascade is used.
    /// </summary>
    public string PasswordSelector { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom CSS selector for the submit button.
    /// If empty, the selector cascade is used.
    /// </summary>
    public string SubmitSelector { get; set; } = string.Empty;

    /// <summary>
    /// Site base URL — populated by the scanner engine from <see cref="SiteConfig.BaseUrl"/>
    /// before invoking auth. Required for apps deployed under a virtual sub-path
    /// (e.g. "https://flex.em.wsu.edu/Touchpoints"), where Page.Url's authority alone
    /// would lose the "/Touchpoints" prefix and produce wrong login URLs.
    /// Not part of the user-facing JSON config.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
