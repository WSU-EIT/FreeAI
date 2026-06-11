# 074 — Screenshots Without a Browser Window

> **Document ID:** 074  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Show how the Playwright-driven headless capture produces visual snapshots across discovered routes.
> **Audience:** Quality and tooling users  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Headless Screenshots Matter](#why-it-matters) | Plain-language intro and key term definitions |
| 2 | [How the Capture Pipeline Works](#pipeline) | Playwright driving a browser with no window, in two passes |
| 3 | [Route Discovery and Coverage](#route-discovery) | How routes come from a CSV and which ones get visited |
| 4 | [Running a Capture Pass](#running) | Commands and environment variables to trigger a run |
| 5 | [Output: Files, Naming, and Layout](#output) | Folder layout, file names, and the metadata sidecar |
| 6 | [Verifying and Reviewing Snapshots](#verifying) | Confirming captures look correct and complete |
| 7 | [Troubleshooting and Common Pitfalls](#troubleshooting) | Fixing blank, flaky, or missing shots |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Headless Screenshots Matter

**Why it matters first:** a web app can pass every unit test and still look broken — a button off-screen, a panel that never loaded, a login page that shows a spinner forever. The only way to catch "it compiles but looks wrong" is to actually *render* every page and look at it. Doing that by hand across dozens of routes, twice (logged out and logged in), is tedious and easy to skip. This tool does it automatically.

Let's define the three terms in the title before going further:

- **Headless** means a real web browser engine runs, loads pages, and runs JavaScript — but with no visible window on screen. It is the full browser minus the picture frame. That matters because it runs the same way on a developer's laptop and on a build server that has no monitor at all.
- **Playwright** is Microsoft's library for controlling a browser from code. Your program tells Playwright "go to this URL, wait until it's done loading, take a picture," and Playwright drives a real Chromium, Firefox, or WebKit engine to do exactly that. In this tool it arrives as the NuGet package `Microsoft.Playwright` (version `1.56.0`, per the project file).
- **Snapshot** here means a full-page PNG image of what a route looks like once it has fully rendered — not just the part you'd see on screen, but the entire scrollable page top to bottom.

The tool that does all this is a small console program named **`FreeTools.BrowserSnapshot`** (namespace `FreeTools.BrowserSnapshot`, entry point `Program.cs`). Its console banner identifies it as `BrowserSnapshot (FreeTools)` version `3.0`.

Why this is valuable to a non-engineer running quality checks:

- **You see every page at once.** One run produces an image per route, so you can scroll through a folder and spot the broken ones.
- **It checks two states.** It captures each page logged out *and* logged in, so you catch problems that only appear behind a login.
- **It flags suspicious results for you.** If an image comes out tiny (under 10 KB), that usually means the page rendered blank — the tool notices, retries, and marks it, so you don't have to eyeball file sizes.

<a id="pipeline"></a>
## 2. How the Capture Pipeline Works

**Why it matters:** understanding the shape of the pipeline tells you what to expect in the output and where things can go wrong. The capture is a **two-pass** process — it walks the whole list of pages once without logging in, then logs in once and walks the whole list again.

Here is the run, step by step, as the program prints it (`[1/5]` through `[5/5]`):

**Startup wait.** Before anything else, the tool pauses to let the web app finish booting (default 5 seconds, set by `START_DELAY_MS`):

```csharp
Console.WriteLine($"Waiting {startupDelay}ms for server to be ready...");
await Task.Delay(startupDelay);
```

**[1/5] and [2/5] — get the browser ready.** It makes sure the Playwright browser engine is downloaded (it auto-installs on first run), then starts Playwright:

```csharp
await EnsurePlaywrightBrowsersInstalledAsync(browserName);
using var playwright = await Playwright.CreateAsync();
```

**[3/5] — launch the browser, windowless.** It picks the engine (`chromium` by default, or `firefox`/`webkit`) and launches it with `Headless = true` — that one flag is what makes it run without a window:

```csharp
await using var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true
});
```

**[4/5] — Pass 1: capture every page logged out.** For each route, it opens a fresh **browser context** (think of a context as a private, cookie-free incognito session), navigates to the URL, waits, and saves a `default.png`. The waiting is the important part. It does not screenshot the instant the page arrives — it waits for the network to go quiet, then waits a little longer for JavaScript to finish drawing:

```csharp
var response = await page.GotoAsync(url, new PageGotoOptions
{
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 60000
});
...
await page.WaitForTimeoutAsync(settleDelay);
```

- **`NetworkIdle`** means "wait until the page has stopped making network requests." This is the key trick for modern apps. FreeCRM pages are **Blazor WASM** — meaning the browser downloads the app's code and *then* builds the page in the browser — so the HTML arrives almost empty and fills in moments later. Screenshotting on plain HTML load would catch a blank page; waiting for network idle catches the finished one.
- **Settle delay** is an extra pause after the network goes quiet (default 3000 ms, set by `PAGE_SETTLE_DELAY_MS`), giving any last rendering time to settle.

The actual screenshot is taken full-page:

```csharp
await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
```

If that image comes out under the 10 KB threshold (`SuspiciousFileSizeThreshold = 10 * 1024`), the page probably rendered blank, so the tool waits an extra 3 seconds (`RetryExtraDelayMs = 3000`) and shoots once more before giving up.

**The login step (between the passes).** After Pass 1, the tool logs in exactly once and saves the session so it doesn't have to log in for every page. It navigates to the login route, waits for the Blazor login UI to appear, handles a two-step provider-selection screen if present (clicking "Local Account"), fills the username and password, submits, and then confirms success by watching the login panel disappear:

```csharp
await page.WaitForSelectorAsync(".login-page", new PageWaitForSelectorOptions
{
    State = WaitForSelectorState.Hidden,
    Timeout = 15000
});
```

If login works, it saves the **storage state** — the cookies and local storage that prove "this browser is signed in" — for reuse:

```csharp
var storageState = await context.StorageStateAsync();
```

Along the way it saves three diagnostic shots of the login itself, into an `_auth-flow` folder: `1-initial.png` (the login page), `2-filled.png` (form filled in), and `3-result.png` (after submit).

**[5/5] — Pass 2: capture every page logged in.** Using the saved session, it walks the whole route list again and saves a `logged-in.png` per page. For pages that *require* auth, it also grabs a `login-redirect.png` showing what a logged-out visitor sees instead (usually the login screen they get bounced to).

**Speed:** both passes run in parallel. A `SemaphoreSlim` caps how many pages are captured at once (default 10, set by `MAX_THREADS`), so it's fast but doesn't overwhelm the server:

```csharp
var semaphore = new SemaphoreSlim(maxThreads);
```

If login fails, Pass 2 is skipped entirely and you still keep all the logged-out shots.

<a id="route-discovery"></a>
## 3. Route Discovery and Coverage

**Why it matters:** this tool does not find pages on its own. It captures exactly the list of routes handed to it in a CSV file, so the coverage of your screenshots is only as good as that list. Knowing where the list comes from tells you why a page might be missing.

**Where the list comes from.** The CSV is produced by the route-mapping tool covered in [073 — Mapping Every Route](073_route-discovery.md). BrowserSnapshot just reads it. The default file name is `pages.csv` (override with `CSV_PATH`), and the expected columns are:

```
FilePath,Route,RequiresAuth,Project
```

Only two columns drive the capture: **Route** (column 2, which page to visit) and **RequiresAuth** (column 3, whether it lives behind a login).

**Routes with parameters are skipped.** Many routes contain a placeholder — for example `/Customer/{itemid}` — where `{itemid}` would need a real value to load a real page. The tool can't invent that value, so it skips any route that still contains `{` and `}` after substitution and prints a `[SKIP]` line:

```csharp
public static bool HasParameter(string route)
    => route.Contains('{') && route.Contains('}');
```

**One placeholder is special: the tenant code.** FreeCRM apps are multi-tenant — the tenant (the customer organization) is named in the URL, like `/tenant1/About`. Routes in the CSV use a `{TenantCode}` placeholder, and the tool fills it in with the value of `TENANT_CODE` (default `tenant1`) *before* the "has parameter" check, so those routes survive instead of being skipped:

```csharp
route = route.Replace("{TenantCode}", tenantCode, StringComparison.OrdinalIgnoreCase);
```

**Deduplication.** A page can appear twice in the CSV — once bare (`/About`) and once tenant-prefixed (`/tenant1/About`). They are the same page, so the tool keeps only the tenant-code version (it ensures the app resolves the right tenant) and drops the bare duplicate. This keeps the output free of redundant shots.

**Coverage summary on screen.** After parsing, the tool tells you exactly how many routes it will capture and how that splits between public and auth-required pages:

```csharp
Console.WriteLine($"Found {routeInfos.Count} routes ({publicCount} public, {authCount} auth) with {maxThreads} parallel workers.");
```

If you expected a page and don't see it, the usual reasons are: it wasn't in the CSV at all, it had an unfilled `{parameter}` and got skipped, or it was deduplicated away in favor of its tenant-prefixed twin.

<a id="running"></a>
## 4. Running a Capture Pass

**Why it matters:** you need the web app running first, then point the tool at it. There are two ways to run it.

**Option A — via the AppHost (recommended).** The tool is wired into the FreeTools orchestrator, which starts the web app, waits for it, and runs the capture automatically:

```bash
cd FreeTools/FreeTools.AppHost
dotnet run
```

**Option B — standalone.** Run the project directly. The first run auto-downloads the browser engine, so it may take a minute:

```bash
cd FreeTools/FreeTools.BrowserSnapshot
dotnet run [baseUrl] [csvPath] [outputDir] [maxThreads]
```

All four arguments are optional and each also has an environment-variable form. The tool reads each setting from the environment variable first, then the positional argument, then a built-in default (that order is handled by `CliArgs.GetEnvOrArg`). The settings that matter:

| Variable | Positional arg | What it controls | Default |
|----------|----------------|------------------|---------|
| `BASE_URL` | 1st | Address of the running app | `https://localhost:5001` |
| `CSV_PATH` | 2nd | The route list to capture | `pages.csv` |
| `OUTPUT_DIR` | 3rd | Where images are written | `page-snapshots` |
| `MAX_THREADS` | 4th | Pages captured in parallel | `10` |
| `SCREENSHOT_BROWSER` | — | `chromium`, `firefox`, or `webkit` | `chromium` |
| `SCREENSHOT_VIEWPORT` | — | Window size as `WIDTHxHEIGHT` (e.g. `1920x1080`) | Playwright default |
| `PAGE_SETTLE_DELAY_MS` | — | Extra wait after network idle | `3000` |
| `START_DELAY_MS` | — | Startup wait for server warmup | `5000` |
| `LOGIN_USERNAME` | — | Account for the authenticated pass | `admin` |
| `LOGIN_PASSWORD` | — | Password for that account | `admin` |
| `TENANT_CODE` | — | Tenant for login URL and `{TenantCode}` | `tenant1` |

A typical standalone run against a local app, in PowerShell:

```powershell
$env:BASE_URL = "https://localhost:5001"
$env:CSV_PATH = "pages.csv"
$env:OUTPUT_DIR = "page-snapshots"
dotnet run
```

When it starts, the tool echoes its configuration so you can confirm it's pointed where you intended — `BASE_URL`, `CSV_PATH`, `OUTPUT_DIR`, `BROWSER`, `VIEWPORT`, `MAX_THREADS`, `SETTLE_DELAY`, `LOGIN_USER`, and `TENANT_CODE`. The program returns exit code `0` on success and `1` on a fatal error (for example, the CSV file isn't found).

<a id="output"></a>
## 5. Output: Files, Naming, and Layout

**Why it matters:** knowing the folder shape lets you find a specific page's shots fast and lets other tools (like the report generator) read the results.

**One folder per route.** Each route becomes a sub-folder under the output directory, with slashes turned into folder separators. The home page (`/`) lands in a folder named `root`. This mapping is done by `PathSanitizer`:

```csharp
var safePath = route.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
return string.IsNullOrEmpty(safePath) ? "root" : safePath;
```

**The files inside each route folder:**

- **`default.png`** — the page captured logged out (Pass 1). Always present.
- **`logged-in.png`** — the page captured logged in (Pass 2). Present only if login succeeded.
- **`login-redirect.png`** — for auth-required pages only: what a logged-out visitor sees (typically the login screen).
- **`metadata.json`** — a small data file describing the capture (see below).

There is also one special **`_auth-flow`** folder holding the three login diagnostic shots: `1-initial.png`, `2-filled.png`, `3-result.png`.

So the layout looks like:

```
page-snapshots/
├── _auth-flow/
│   ├── 1-initial.png
│   ├── 2-filled.png
│   └── 3-result.png
├── root/
│   ├── default.png
│   ├── logged-in.png
│   └── metadata.json
└── tenant1/
    └── About/
        ├── default.png
        ├── logged-in.png
        └── metadata.json
```

**The metadata sidecar.** Beside each route's images sits a `metadata.json` with the facts a human or a report would want without opening the image: which URL was hit, the HTTP status code, the file size, whether it was flagged suspiciously small, whether a retry happened, any JavaScript console errors recorded during load, when it was captured, and the auth-pass results. It is written by `WriteMetadataAsync` from the `ScreenshotMetadata` record. A trimmed example:

```json
{
  "Route": "/Account/Login",
  "Url": "https://localhost:5001/Account/Login",
  "StatusCode": 200,
  "FileSize": 45231,
  "IsSuspiciouslySmall": false,
  "RetryAttempted": false,
  "ConsoleErrors": [],
  "IsSuccess": true
}
```

**Images are full-page PNGs.** Every shot uses `FullPage = true`, so the image is the entire scrollable page, not just the visible window — long pages produce tall images.

<a id="verifying"></a>
## 6. Verifying and Reviewing Snapshots

**Why it matters:** a screenshot run that "completed" is not the same as a run that looks right. Two minutes of review catches the cases the tool can only suspect.

**Start with the on-screen summary.** At the end of a run the tool prints how many of each pass succeeded:

```csharp
Console.WriteLine($"  Pass 1 complete: {pass1Success}/{totalCount} successful, {pass1Suspicious} suspicious");
```

and a final block:

```
Two-pass screenshot capture complete for N routes:
  Pass 1 (public):  X/N successful
  Pass 2 (auth):    Y/N successful
```

What the words mean:

- **Successful** = the page returned an HTTP status in the 2xx or 3xx range (`StatusCode >= 200 && < 400`). It loaded without an error code.
- **Suspicious** = the saved image was under 10 KB, which usually means a blank or half-rendered page. The number to the right of "successful" is your watch-list.
- **Pass 2 SKIPPED** = login failed, so there are no `logged-in.png` files. Fix login (Section 7) and rerun.

**Then eyeball the folder.** Sort the route folders by file size and open the smallest `default.png` and `logged-in.png` first — those are the most likely to be blank. Confirm:

- The page actually shows its content (header, nav, the page's main panel), not a spinner or empty white.
- Logged-in pages show the post-login experience, not the login screen (if they show login, the saved session didn't stick).
- For auth-required routes, `login-redirect.png` correctly shows the login screen — that's the *expected* logged-out behavior, not a bug.

**Check the login diagnostics if Pass 2 looks wrong.** Open `_auth-flow/1-initial.png`, `2-filled.png`, and `3-result.png` in order. They show exactly what the tool saw while logging in: whether the login page rendered, whether the form got filled, and whether the result page moved past login. This is the fastest way to see *why* authenticated shots are missing or wrong.

**Cross-check the metadata.** For any questionable page, open its `metadata.json`. A non-2xx `statusCode`, a tiny `fileSize`, `isSuspiciouslySmall: true`, or entries in `consoleErrors` each point at the specific problem.

<a id="troubleshooting"></a>
## 7. Troubleshooting and Common Pitfalls

**Why it matters:** most failures are one of a handful of predictable causes. Match the symptom to the fix below.

**Blank or tiny screenshots (under 10 KB).** The page didn't finish rendering before the shot. The tool already retries once with extra delay, but if blanks persist:
- Increase `PAGE_SETTLE_DELAY_MS` (try `5000` or higher) to give Blazor WASM more time to draw.
- Check `consoleErrors` in that page's `metadata.json` — a JavaScript error during boot can stop the page from rendering at all, and no amount of waiting fixes a crash.

**"Navigation timed out."** The page took longer than the 60-second navigation limit, or the network never went quiet. Common causes: the app isn't fully up yet (raise `START_DELAY_MS`), or a page makes a request that never completes so `NetworkIdle` is never reached. The error is recorded per page; other pages still capture normally.

**Login failed — Pass 2 skipped.** The single biggest source of "half" runs. The tool prints why and saves the `_auth-flow` shots so you can see the failure point. Check, in order:
- **Credentials:** `LOGIN_USERNAME` / `LOGIN_PASSWORD` (default `admin`/`admin`). If `3-result.png` still shows the login form, the credentials were rejected.
- **Tenant code:** login goes to `/{TENANT_CODE}/Login`. A wrong `TENANT_CODE` means the app can't resolve the tenant and the login page won't behave. Verify it matches a real tenant.
- **WASM didn't boot:** if `1-initial.png` is blank, the login page itself never rendered. The tool dumps whether the HTML contains the Blazor markers (`_framework/blazor.web.js`) and any console errors — a missing marker means the app isn't serving the Blazor bootstrap, which is an app/server problem, not a tool problem.

**A page you expected is missing entirely.** It was never captured. Re-read Section 3: it was probably not in the CSV, was skipped for having an unfilled `{parameter}`, or was deduplicated in favor of its `/{tenantCode}/...` version. The `[SKIP]` lines and the "Found N routes" line at the start confirm which.

**Certificate / HTTPS warnings on localhost.** Not an issue — every context is created with `IgnoreHTTPSErrors = true`, so the self-signed local development certificate won't block captures.

**Browser won't install or run.** Playwright auto-installs the engine on first run; if that's blocked (offline, proxy, permissions), install it manually, e.g. on Windows:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

**Run is slow.** Lower `MAX_THREADS` if the server is struggling, or raise it (carefully) if the server can take the load — it controls how many pages capture at once.

---

<a id="related-docs"></a>
## 8. Related Docs

- [073 — Mapping Every Route](073_route-discovery.md) — the routes it captures
- [075 — Sweeping for ADA and Accessibility Gaps](075_ada-scanning.md) — accessibility runs alongside

---
*GuidesV2 074 · drafted from source (`FreeTools.BrowserSnapshot/Program.cs`, `FreeTools.Core` helpers) · 2026-06-05.*
