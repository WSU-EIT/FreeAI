# 073 — Mapping Every Route

> **Document ID:** 073  ·  **Category:** Tooling  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the route-discovery command that enumerates an app reachable pages and what to do with the inventory.
> **Audience:** Quality and tooling users  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 07x (Analyzing Apps With FreeTools) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Route Discovery Matters](#why) | What a route is, what the inventory is, and why you need one |
| 2 | [When to Run It](#when) | The moments that call for a fresh route list |
| 3 | [Running the Command](#running) | The two arguments, the `--clean` flag, and the env-var switches |
| 4 | [Reading the Inventory Output](#output) | The four CSV columns and how to read a real row |
| 5 | [Acting on the Results](#acting) | How the downstream tools turn this list into HTTP checks and screenshots |
| 6 | [Common Pitfalls](#pitfalls) | What the scanner can and cannot see |
| 7 | [Reference and Next Steps](#reference) | Full argument/env table and a quick checklist |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why"></a>
## 1. Why Route Discovery Matters

**Why it matters first:** you cannot test, screenshot, or audit a page you do not know exists. Before any of the other FreeTools can poke at an app, *something* has to produce a complete list of the app's pages. That something is the **EndpointMapper**, and this doc is about the list it produces.

A few plain-language terms before we go further:

- **Route** — the part of a web address after the domain that points at one specific page. In `https://app.example.com/Settings`, the route is `/Settings`. It is the page's "street address."
- **Blazor** — the web framework these apps are built with. In Blazor, each page lives in a file ending in `.razor`, and a page declares its own route at the top of the file with a directive that looks like `@page "/Settings"`. ("Directive" just means a special instruction line the framework reads.)
- **Route inventory** — a single flat list of every route in the app, gathered in one place. EndpointMapper writes this list to a plain spreadsheet-style file (a CSV — comma-separated values, the format Excel opens).
- **Reachability** — whether a page can actually be opened. A route only matters if a user (or a test) can navigate to it; the inventory is the starting set of "things that should be reachable."

EndpointMapper builds the inventory by **reading source code, not by crawling a running website.** It scans the project's `.razor` files on disk and pulls out the `@page` lines. The advantage of reading the source is that it finds every declared page even if no link points to it yet — there is no risk of missing a page just because nothing links to it. The trade-off (covered in section 6) is that it only sees pages declared this one specific way.

Concretely, for each `.razor` file the tool looks for two things using simple text patterns:

```csharp
var pageRegex      = new Regex(@"@page\s+""(?<route>[^""]+)""", RegexOptions.Compiled);
var authorizeRegex = new Regex(@"@attribute\s+\[Authorize", RegexOptions.Compiled);
```

The first pattern captures the route string inside `@page "..."`. The second checks whether the page is protected by an `[Authorize]` attribute — Blazor's way of saying "you must be logged in to see this." (An "attribute" is a tag attached to code; `[Authorize]` is the standard one for login-gated pages.) Knowing which pages require a login up front saves the downstream tools from being surprised by a redirect to the sign-in screen.

---

<a id="when"></a>
## 2. When to Run It

Run EndpointMapper whenever the set of pages might have changed, or whenever a downstream tool needs a current list to work from. In practice that means:

- **Before a screenshot or HTTP sweep.** The route list is the *input* to the other tools. If you are about to run the endpoint poker (HTTP checks) or the headless screenshotter, regenerate the inventory first so you are testing today's pages, not last month's.
- **After adding, renaming, or deleting pages.** Any change to a `@page` line — a new page, a moved route, a removed page — only shows up if you re-scan.
- **During an audit or release check.** When you want a defensible, complete answer to "what pages does this app expose, and which ones require a login?", the inventory is that answer in one file.
- **When investigating a regression.** "Regression" means something that used to work and now does not. Diffing a fresh inventory against an older one quickly shows whether a route silently disappeared or was renamed.

The scan is cheap — it just reads files on disk — so there is no penalty for running it often. The common pattern is to regenerate the CSV at the start of every analysis run rather than reusing a stale copy.

---

<a id="running"></a>
## 3. Running the Command

EndpointMapper is a small .NET console program (it targets .NET 10). You run it from its project folder with `dotnet run`. The basic shape is:

```bash
cd tools/FreeTools.EndpointMapper
dotnet run [rootToScan] [csvOutputPath] [--clean]
```

Both positional arguments are **optional**, and the defaults are designed so the tool "just works" inside the FreeTools repo:

- **First argument — `rootToScan`:** the folder to search for `.razor` files. The tool searches it *and all subfolders*. If you omit it, the tool walks up a few levels from its own binary to land on the workspace root, so running it with no arguments scans the whole solution.
- **Second argument — `csvOutputPath`:** where to write the inventory. If you omit it, the tool writes a file named `pages.csv` at the root it just scanned.

So the simplest possible invocation —

```bash
dotnet run
```

— scans the entire workspace and drops `pages.csv` at its root. To scan a specific app and name the output yourself:

```bash
dotnet run "C:\apps\MyCrm" "C:\out\mycrm-pages.csv"
```

**The `--clean` flag.** Passing `--clean` deletes a chosen output directory *before* the scan runs. This is for the screenshot/HTML pipeline, where stale image or HTML files from a previous run would otherwise pile up. The directory it cleans defaults to `page-snapshots` and can be changed (see the env vars below). Cleaning never touches the CSV itself — only the snapshot directory.

**Environment-variable switches.** A few behaviors can also be driven by environment variables, which is how the tool is wired up when it runs automatically as part of the larger pipeline:

| Variable | Effect |
|----------|--------|
| `START_DELAY_MS` | If set to a positive number, the tool waits that many milliseconds before starting. Used to stagger startup when several tools launch at once. |
| `CLEAN_OUTPUT_DIRS` | Set to `true` to turn on clean mode without passing `--clean` on the command line. |
| `OUTPUT_DIR` | The directory that clean mode deletes. Defaults to `page-snapshots`. |

When the tool starts it prints a banner and echoes its configuration, so you can confirm at a glance what it is about to do:

```
============================================================
 EndpointMapper (FreeTools) v2.0
============================================================
Scanning root:     C:\apps\MyCrm
Will write CSV to: C:\out\mycrm-pages.csv
Clean mode:        DISABLED
```

---

<a id="output"></a>
## 4. Reading the Inventory Output

The output is a CSV file. Its first line is always the header naming the four columns, written exactly as:

```csv
FilePath,Route,RequiresAuth,Project
```

Every line after that describes one finding. Here is what each column means:

| Column | What it holds | Why it is there |
|--------|---------------|-----------------|
| `FilePath` | The `.razor` file's path, relative to the scanned root, with forward slashes | Tells you *where* the page is defined so you can open it. Paths are kept relative (not absolute) on purpose, so the file does not leak someone's local machine layout. |
| `Route` | The route string captured from `@page`, e.g. `/Settings`. Empty if the file has no `@page` line. | The actual address you would visit. This is the column the downstream tools read. |
| `RequiresAuth` | `true` or `false` | Whether the page carries `@attribute [Authorize]` — i.e. whether visiting it without logging in will bounce you to a sign-in screen. |
| `Project` | A short label for which part of the codebase the file belongs to | Lets you slice the inventory by area (for example, separating the main web app from identity/account pages). |

Two example rows, taken straight from the tool's own documentation:

```csv
Components/Pages/Home.razor,/,false,CRM
Components/Pages/Settings.razor,/Settings,true,CRM
```

Read the second row as: "The file `Components/Pages/Settings.razor` defines the route `/Settings`; it requires a login; it lives in the CRM project."

A few details worth knowing so nothing surprises you:

- **A single file can produce several rows.** A `.razor` page is allowed to declare more than one `@page` line (a page reachable at two addresses). EndpointMapper emits one row per route, so that file appears more than once.
- **Files with no route still appear, with an empty `Route`.** Many `.razor` files are reusable *components*, not full pages, so they have no `@page` line. The tool still lists them with a blank route. This is deliberate — it keeps the file an honest record of everything scanned — but it means the route column has blanks you need to ignore when you only want navigable pages.
- **The final lines printed to the console are a tally**, not part of the CSV: total `.razor` files scanned, how many had `@page` directives, how many did not, how many require authentication, and the total row count. Use that summary as a quick sanity check that the numbers look right.

---

<a id="acting"></a>
## 5. Acting on the Results

The inventory is a means, not an end. Its whole purpose is to feed the next tools in the chain, which turn the route list into actual checks:

1. **EndpointMapper** scans `.razor` files → writes `pages.csv`.
2. **The endpoint poker** reads `pages.csv` and sends an HTTP GET to each route, recording whether the page responded.
3. **The headless screenshotter** reads the same `pages.csv` and captures an image of each page (no visible browser window needed).

The endpoint poker consumes the CSV through a shared helper in the tooling core library, `RouteParser`; the screenshotter uses an equivalent in-house parser that applies the same rules (and still calls `RouteParser`'s `HasParameter` check). Understanding what that parsing does explains how the raw inventory becomes a clean, testable list:

- **It reads the `Route` column** (column index 1 — the second column) and skips blank routes, so the component files with no `@page` line drop out automatically.
- **It skips parameterized routes by default.** A "parameterized" route has a placeholder in curly braces, like `/User/{id}` — you cannot just open it, because `{id}` has to be filled in with a real value. `RouteParser` recognizes these and sets them aside as *skipped* rather than trying to visit a broken address:

  ```csharp
  public static bool HasParameter(string route)
      => route.Contains('{') && route.Contains('}');
  ```

- **It can substitute a tenant code.** Multi-tenant apps often have routes like `/{TenantCode}/Dashboard`, where `{TenantCode}` stands in for a customer's short code. If you supply a tenant code, `RouteParser` swaps it in (turning `/{TenantCode}/Dashboard` into, say, `/acme/Dashboard`), then deduplicates so the real tenant route wins over the bare template.
- **It returns a sorted, de-duplicated list of visitable routes plus a separate list of the skipped ones**, so you can see at a glance which pages were tested and which were left out and why.

So in practice, "acting on the results" usually means: regenerate the CSV, then hand it to the poker or the screenshotter and let `RouteParser` filter it down. The two outputs you should consciously review are (a) the `RequiresAuth=true` pages — make sure your test run is authenticated if you want those to succeed — and (b) the skipped parameterized routes, which need real example values if you want them covered at all.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls

EndpointMapper is deliberately simple, and its simplicity has edges. Knowing them keeps you from trusting the inventory for more than it can deliver.

- **Blazor pages only — nothing else.** The scanner finds `@page` directives in `.razor` files. It does **not** find API controller routes (the `[Route]` / `[HttpGet]` style), minimal-API endpoints (`MapGet` / `MapPost`), or any route registered in code at runtime. If your app exposes a web API, those endpoints will be absent from the inventory entirely. For API documentation, use Swagger/OpenAPI instead.

- **Parameterized routes are listed but not directly visitable.** As noted above, a route like `/User/{id}` appears in the CSV, but the downstream parser skips it by default because `{id}` is a placeholder. These pages are *not* tested unless you provide real values, so do not read a clean test run as proof that detail pages work.

- **`RequiresAuth` is a coarse signal, not a guarantee.** It is `true` only when the file literally contains `@attribute [Authorize]`. A page can be protected in other ways — authorization applied at a parent layout, in routing configuration, or in code — and the scanner will mark such a page `false`. Treat `RequiresAuth` as "definitely needs login when true," not "definitely public when false."

- **Component files create empty-route rows.** Every `.razor` file without a `@page` line still produces a row with a blank `Route`. If you load the CSV into a spreadsheet and count rows, you are counting components and pages together. Filter out the blank-route rows before reporting a "page count."

- **A route can appear more than once.** Multi-`@page` files and the same page declared in different projects can both produce repeated routes. The downstream `RouteParser` de-duplicates, but the raw CSV does not — so deduplicate yourself if you read the CSV directly.

- **Excluded folders.** The scan ignores generated and vendored folders — anything under `obj`, `bin`, or a `repo` folder is skipped — so it does not pick up build artifacts or copied-in third-party code. This is almost always what you want, but it means a page that only exists inside one of those folders will not be found.

---

<a id="reference"></a>
## 7. Reference and Next Steps

**Arguments (positional, both optional):**

| Position | Name | Default | Meaning |
|----------|------|---------|---------|
| 1 | `rootToScan` | Walks up to the workspace root from the tool's binary | Folder to scan recursively for `.razor` files |
| 2 | `csvOutputPath` | `pages.csv` at the scanned root | Where to write the inventory CSV |

**Flag:**

| Flag | Meaning |
|------|---------|
| `--clean` | Delete the output directory (default `page-snapshots`) before scanning. Equivalent to `CLEAN_OUTPUT_DIRS=true`. Never deletes the CSV. |

**Environment variables:**

| Variable | Default | Meaning |
|----------|---------|---------|
| `START_DELAY_MS` | none | Wait this many milliseconds before starting (used to stagger parallel launches) |
| `CLEAN_OUTPUT_DIRS` | `false` | Set `true` to enable clean mode without `--clean` |
| `OUTPUT_DIR` | `page-snapshots` | Directory that clean mode deletes |

**Output CSV columns:** `FilePath,Route,RequiresAuth,Project`

**Quick checklist for a reliable run:**

1. Regenerate the CSV first — never reuse a stale inventory.
2. Confirm the console tally (file counts, auth count) looks plausible.
3. Filter out blank-route rows before counting pages.
4. Decide how to handle `RequiresAuth=true` pages (authenticate your downstream run) and parameterized routes (supply real values or accept they are skipped).
5. Hand the CSV to the endpoint poker or the screenshotter.

**Next steps:** with the inventory in hand, move on to capturing what each page looks like — see doc 074 below — or feed it into HTTP and accessibility checks described elsewhere in this band.

---

<a id="related-docs"></a>
## 8. Related Docs

- [071 — Meet the Analysis Suite](071_freetools-orientation.md) — the suite overview
- [074 — Screenshots Without a Browser Window](074_headless-screenshots.md) — screenshots use this route list

---
*GuidesV2 · 073 · drafted from source (FreeTools.EndpointMapper / FreeTools.Core) on 2026-06-05.*
