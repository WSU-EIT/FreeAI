# 057 — The CSS Style Reference

> **Document ID:** 057  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Document the small surface of hand-written CSS in FreeCRM — where each stylesheet lives, what belongs in it, and the formatting conventions the existing code actually follows.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [How to Read This Doc + the Bootstrap-First Reality](#how-to-read) | What CSS is, why FreeCRM writes almost none of it, and the ours-vs-vendored line you must never cross |
| 2 | [The Stylesheet Map](#stylesheet-map) | The four global files — `site.css`, `site.App.css`, `tags.css`, `themes.css` — what belongs where, and why the `.App` split exists |
| 3 | [Component-Scoped CSS](#scoped-css) | `*.razor.css` files and Blazor CSS isolation — what they are, and why we barely use them |
| 4 | [CSS Code Style](#code-style) | Selectors, class naming, braces, indentation, one-property-per-line, ordering, comments |
| 5 | [Theming with CSS Custom Properties](#theming) | The `themes.css` variable system, units, and colors |
| 6 | [Media Queries and Responsive](#media-queries) | Feature/preference queries — and why there are no width breakpoints |
| 7 | [Quick Reference](#quick-reference) | Side-by-side correct and incorrect snippets you can copy |
| 8 | [FAQ](#faq) | The real questions you will actually have on day one |
| 9 | [Related Docs](#related-docs) | Parent, sibling, and next-step docs |

---

<a id="how-to-read"></a>
## 1. How to Read This Doc + the Bootstrap-First Reality

**CSS** ("Cascading Style Sheets") is the language that controls how a web page *looks* — colors, spacing, fonts, layout. A **stylesheet** is a `.css` file full of rules; each rule says "for this kind of element, set these visual properties." This doc is about *which* stylesheets FreeCRM hand-writes, and the conventions the existing CSS follows so your additions look like everyone else's.

**The single most important thing to internalize first: FreeCRM is Bootstrap-first.** **Bootstrap** is a popular third-party CSS framework that ships hundreds of ready-made *utility classes* (a *class* is a reusable style label you attach to an HTML element, like `class="btn"`). The overwhelming majority of FreeCRM's styling comes from those Bootstrap utility classes, plus a few other component libraries (Radzen, MudBlazor). We write **relatively little custom CSS of our own.** When you need to style something, your *first* instinct should be "is there a Bootstrap class for this?" — not "let me write CSS." This doc covers the small hand-written surface that remains.

**How small is "small"?** There are exactly **five** hand-written CSS files in the entire client project, and one of them is empty and one is barely two dozen lines:

| File | Lines | What it is |
|------|-------|-----------|
| [`site.css`](FreeCRM/CRM.Client/wwwroot/css/site.css) | 1466 | The main stylesheet — roughly 95% of all hand-written CSS |
| [`themes.css`](FreeCRM/CRM.Client/wwwroot/css/themes.css) | 494 | The dark/colored theme system |
| [`tags.css`](FreeCRM/CRM.Client/wwwroot/css/tags.css) | 679 | Almost entirely repetitive `.tag-<color>` rules |
| [`UploadFile.razor.css`](FreeCRM/CRM.Client/Shared/UploadFile.razor.css) | 21 | The only component-scoped stylesheet |
| [`site.App.css`](FreeCRM/CRM.Client/wwwroot/css/site.App.css) | 3 | A comment-only placeholder for your overrides |

That's the whole inventory. This is an honest, complete picture — not a teaser for a larger system. The files physically live under [`CRM.Client/wwwroot/css/`](FreeCRM/CRM.Client/wwwroot/css/) (the four global ones) and next to a component (the scoped one). Verified at [`App.razor:50-53`](FreeCRM/CRM/Components/App.razor#L50-L53) and by directory listing — the `css/` folder contains exactly these four `.css` files and no others.

### Ours vs. vendored — the line you must NEVER cross

**Vendored** means "code we copied in from someone else and do not maintain." You must never edit vendored CSS, because the next upgrade will overwrite your changes and you will have nothing to show for it. Here is the rule:

> **Anything under `fontawesome/`, `js/bootstrap/`, or `lib/highcharts/`, any `_content/...` bundle, the bootstrap-icons CDN link, or any `*.min.*` file is VENDORED. Do not edit it. It is not "our style" and nothing in it should be treated as a convention to copy.**

Concretely, the vendored CSS we do **not** touch lives under [`CRM.Client/wwwroot/fontawesome/css/*`](FreeCRM/CRM.Client/wwwroot/fontawesome/css/), [`CRM.Client/wwwroot/js/bootstrap/css/*`](FreeCRM/CRM.Client/wwwroot/js/bootstrap/css/), and [`CRM.Client/wwwroot/lib/highcharts/*`](FreeCRM/CRM.Client/wwwroot/lib/highcharts/), plus the package-served bundles referenced as `_content/...` (Blazor.Bootstrap, FreeBlazor, MudBlazor, Radzen) and the CDN bootstrap-icons stylesheet linked at [`App.razor:43`](FreeCRM/CRM/Components/App.razor#L43). If you want to *change* how Bootstrap or a library looks, you do it by overriding its variables in *our* files (Section 5) — never by editing the library file.

One more honest note up front: **there is no linter, no Prettier/Stylelint config, and no formal CSS style guide** anywhere in this project. Everything in Sections 4–6 is convention *inferred from the actual code*. The style is hand-maintained Visual Studio output (4-space indent, same-line braces), and it is **not perfectly consistent.** Where the code contradicts itself, this doc says so plainly so you don't waste time hunting for a rule that isn't there.

---

<a id="stylesheet-map"></a>
## 2. The Stylesheet Map: site.css vs site.App.css vs tags.css vs themes.css

There are exactly four hand-written *global* stylesheets (global = applies to the whole app, as opposed to scoped to one component — Section 3). Each has a single, clear job. **Don't invent new global CSS files** — put your CSS in the right one of these four.

The four files are linked together, in this order, from the host project's `App.razor`:

```css
    <link rel="stylesheet" href="@Assets["css/themes.css"]" />
    <link rel="stylesheet" href="@Assets["css/site.css"]" />
    <link rel="stylesheet" href="@Assets["css/site.App.css"]" />
    <link rel="stylesheet" href="@Assets["css/tags.css"]" />
```

Verified verbatim at [`App.razor:50-53`](FreeCRM/CRM/Components/App.razor#L50-L53). (`@Assets[...]` is a Blazor helper that turns a local file path into a fingerprinted, cache-busting URL. Note that these four physically live in `CRM.Client/wwwroot/css/` but are *linked* from the **host** project `CRM/Components/App.razor` — there is no `index.html` or `App.razor` inside `CRM.Client` itself.)

**Load order is not cosmetic — it encodes the layering.** In CSS, when two rules have equal *specificity* (a measure of how targeted a selector is — more on that in Section 4), the one loaded **later** wins. The full order is **vendored → `themes.css` → `site.css` → `site.App.css` → `tags.css`** (vendored links sit above ours at [`App.razor:46-51`](FreeCRM/CRM/Components/App.razor#L46-L51), e.g. MudBlazor at 46, Radzen at 47, fontawesome at 48, bootstrap.min at 49). Our CSS always loads *after* the libraries, which is exactly what lets a plain selector like `nav.navbar` (at [`site.css:265`](FreeCRM/CRM.Client/wwwroot/css/site.css#L265)) or `.toast` (at [`site.css:447`](FreeCRM/CRM.Client/wwwroot/css/site.css#L447)) override Bootstrap's defaults without needing `!important` everywhere.

Here is what belongs in each file.

### `site.css` — the catch-all main stylesheet

General app styling goes here. It opens with a guidance comment and the app's font imports, then runs as plain, low-specificity rules keyed off semantic class names or element-plus-class selectors (e.g. `.toast`, `nav a.nav-link.active`, `table.table tbody tr.action-row`), sized in `em`/`rem`, with shared variable defaults declared in a single `:root` block.

```css
/*  
    If you wish to use different fonts for your layout sepecify your font imports here and update the font-family tags as needed.
*/
@import url('https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200');
@import url('https://fonts.googleapis.com/css2?family=Lora:wght@400;700&family=Merriweather:wght@300;400;700&family=Open+Sans:wght@300;400&family=PT+Sans+Narrow:wght@400;700&family=Roboto:wght@300;400;500;700&display=swap');
```

Verified at [`site.css:1-5`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1-L5). The cited example selectors all exist: `.toast` at [line 447](FreeCRM/CRM.Client/wwwroot/css/site.css#L447), `nav a.nav-link.active` at [line 179](FreeCRM/CRM.Client/wwwroot/css/site.css#L179), `table.table tbody tr.action-row` at [line 371](FreeCRM/CRM.Client/wwwroot/css/site.css#L371). The shared `:root` variable-defaults block sits at [`site.css:952-978`](FreeCRM/CRM.Client/wwwroot/css/site.css#L952-L978), and Blazor framework boilerplate (`#blazor-error-ui`, `.blazor-error-boundary`, `.loading-progress`) lives at [`site.css:106-166`](FreeCRM/CRM.Client/wwwroot/css/site.css#L106-L166).

`site.css` is also the **one place where `!important` and library-internal class names are allowed to leak in**, specifically to bend third-party widgets — e.g. the long generated Radzen button selector at [`site.css:940-942`](FreeCRM/CRM.Client/wwwroot/css/site.css#L940-L942), the Highcharts SVG rules at [`site.css:980-1005`](FreeCRM/CRM.Client/wwwroot/css/site.css#L980-L1005), and `.rz-slot { color: #000 !important; }` at [`site.css:944-946`](FreeCRM/CRM.Client/wwwroot/css/site.css#L944-L946). It also still contains a few empty placeholder rules, e.g. `.marked-as-deleted-notes { }` at [`site.css:890-892`](FreeCRM/CRM.Client/wwwroot/css/site.css#L890-L892).

### `site.App.css` — the local override layer (and why the `.App` split exists)

This is the most important file to *understand* even though it's the smallest. The entire file is three lines:

```css
/*
    Add any app-specific CSS here.
*/
```

Verified verbatim at [`site.App.css:1-3`](FreeCRM/CRM.Client/wwwroot/css/site.App.css#L1-L3). It ships **intentionally empty.**

**Why does this file exist?** Because FreeCRM is a *template you build on top of*, and it periodically pulls in new code from its original author (the "fork sync" process — see [051](051_house-code-style.md) and [054](054_fork-sync-discipline.md)). `site.App.css` is loaded immediately *after* `site.css` (positions 51 and 52 in the link list above), so anything you put here overrides `site.css` **without modifying the shared file.** When the next upstream update overwrites `site.css`, your overrides in `site.App.css` survive untouched. This is the same `.App.` convention used elsewhere in the codebase to mark "your local layer that upgrades won't clobber" — it is the upgrade-safe split (see [041](041_upgrade-safe-model.md)).

So: **don't put framework or shared styles here, and don't edit `site.css` for one-off app tweaks** — put those tweaks in `site.App.css`.

### `tags.css` — the colored-tag palette

This file is one tiny class per CSS named color, used for colored tag/label swatches. A structural `.tag` base sits at the top; each color class only flips two properties (`background-color` + matching `border-color`), adding `color:#fff` only when the swatch is dark enough to need light text.

```css
.tag {
    display: inline-block;
    padding: .1em .3em 0 .3em;
    border: solid 1px;
    border-radius: .2em;
    border-color: #0d6efd;
    background-color: #0d6efd;
    color: #000;
    font-size: 0.9em;
    margin: 0 .2em .2em 0;
}
```

```css
.tag-crimson {
    background-color: crimson;
    border-color: crimson;
    color: #fff;
}
```

Verified: the base at [`tags.css:1-11`](FreeCRM/CRM.Client/wwwroot/css/tags.css#L1-L11), and the dark-swatch override `.tag-crimson` at [`tags.css:51-55`](FreeCRM/CRM.Client/wwwroot/css/tags.css#L51-L55). Only `.tag` and the `.tag-selector` helpers are structural (lines 1-29); everything from `.tag-lightcoral` at [line 31](FreeCRM/CRM.Client/wwwroot/css/tags.css#L31) through `.tag-black` at [line 675](FreeCRM/CRM.Client/wwwroot/css/tags.css#L675) is pure color. The file is long (679 lines) but trivially uniform — it's effectively a generated palette.

### `themes.css` — theme overrides

This file holds **only** theme overrides, keyed by the `[data-bs-theme=...]` attribute, and is mostly Bootstrap CSS-variable reassignments. It's covered in detail in Section 5; the short version for the map is: *any rule that changes appearance for a specific theme goes here, nowhere else.*

---

<a id="scoped-css"></a>
## 3. Component-Scoped CSS: `*.razor.css` and Blazor CSS Isolation

**Blazor CSS isolation** is a feature where you put a CSS file *next to* a component file and name it `<Component>.razor.css` (so `UploadFile.razor` pairs with `UploadFile.razor.css`). Blazor automatically *scopes* those styles to that one component — it rewrites the rules with a hidden per-component attribute so they cannot leak out and affect anything else on the page. It then bundles all such files into a single auto-generated file (`CRM.Client.styles.css`) and injects it for you. **You never write a manual `<link>` for it** — that's the point.

Here is the one scoped file that exists, in full at its opening:

```css
table.password-generator {
    width:100%;
}

table.password-generator tbody tr td {
    padding:.5em;
    vertical-align:top;
}

table.password-generator tbody tr td:first-child {
    width:99%;
}
```

Verified at [`UploadFile.razor.css:1-12`](FreeCRM/CRM.Client/Shared/UploadFile.razor.css#L1-L12). This is the **only** `*.razor.css` file in the entire `CRM.Client` project (confirmed by glob — exactly one match).

**Two honest warnings about this one example:**

1. **It is effectively orphaned.** Its `.password-generator` / `#generated-password` markup is *not used by `UploadFile.razor` at all* — that markup actually lives in [`MainLayout.razor`](FreeCRM/CRM.Client/Layout/MainLayout.razor). So this scoped file is misplaced relative to its host component. Do not treat it as a model to imitate.

2. **It is the exception, not the norm.** `UploadFile.razor` styles itself entirely with **global** classes from `site.css` — for example `hidden` (defined at [`site.css:936-938`](FreeCRM/CRM.Client/wwwroot/css/site.css#L936-L938)), `no-opacity` ([`site.css:1436-1438`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1436-L1438)), `drag-and-drop-instructions` ([`site.css:832-834`](FreeCRM/CRM.Client/wwwroot/css/site.css#L832-L834)), and `drag-and-drop-upload` ([`site.css:819-825`](FreeCRM/CRM.Client/wwwroot/css/site.css#L819-L825)).

**The takeaway:** CSS isolation is *available* and is the technically-correct tool when one component needs genuinely private styles — but **this codebase overwhelmingly favors global classes in `site.css`.** When in doubt, follow the house pattern: add a well-named class to `site.css`, not a new `.razor.css` file. If you *do* need true isolation, add the `<Component>.razor.css` next to the `.razor` and let Blazor bundle it — never hand-link it.

---

<a id="code-style"></a>
## 4. CSS Code Style

Everything below is convention inferred from the live code. Remember: **no formatter or linter enforces any of it**, so the single most reliable rule is *match the style of the rule directly above the one you're adding.*

### 4.1 Selectors and specificity

A **selector** is the part of a CSS rule that decides *which* HTML elements the style applies to (the bit before the `{`). **Specificity** is CSS's scoring system for "how targeted is this selector?" — when two rules collide, the more specific one wins (and if they tie, load order breaks the tie, per Section 2).

The house pattern: write selectors as a class, often *qualified by the element tag* (like `table.table` or `img.user-icon`), plus descendant combinators (a space between two parts means "this inside that"). **Only use an ID selector** (`#name`) for a true page singleton. There is no methodology like BEM here — just plain, readable, low-specificity selectors.

```css
table.table thead tr th,
table.table thead tr td,
table.table tbody tr th,
table.table tbody tr td,
table.table tfoot tr th,
table.table tfoot tr td {
    vertical-align: middle;
}
```

Verified at [`site.css:338-345`](FreeCRM/CRM.Client/wwwroot/css/site.css#L338-L345). Keeping specificity *low* is deliberate: it lets our rules sit just above the vendored libraries (which they override by load order) without an arms race of `!important`.

**Nuance:** IDs *are* used as styling hooks, but only for unique elements — `#page-area`, `#users-on-same-page`, `#blazor-error-ui`, `#generated-password` (e.g. [`site.css:33`](FreeCRM/CRM.Client/wwwroot/css/site.css#L33), [`site.css:106`](FreeCRM/CRM.Client/wwwroot/css/site.css#L106)). A few selectors get deep (`table.first-column-nowrap table tr th:first-child` at [`site.css:784`](FreeCRM/CRM.Client/wwwroot/css/site.css#L784)), and the theme file leans on the attribute selector `[data-bs-theme=dark]` for every rule ([`themes.css:1`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L1)).

When a rule applies to several selectors, write **one selector per line, each comma-terminated**, with the `{` after the final selector:

```css
.menu-user-name,
.menu-user-title,
.menu-user-dept,
.menu-user-username,
.menu-user-email {
    display: block;
}
```

Verified at [`site.css:591-597`](FreeCRM/CRM.Client/wwwroot/css/site.css#L591-L597). (A handful of rules deliberately keep several short selectors on one line, e.g. the `.offcanvas*` variants at [`site.css:743`](FreeCRM/CRM.Client/wwwroot/css/site.css#L743), but one-per-line is the dominant convention.)

### 4.2 Class naming — kebab-case

Name your own classes in **all-lowercase, hyphen-separated words** (`drag-and-drop-upload`). This is called *kebab-case*. Never camelCase, never snake_case (underscores). It matches Bootstrap's own naming so our classes sit alongside `.btn` and `.navbar` without clashing.

```css
.drag-and-drop-upload {
    background-color:#555;
    border:2px dashed #222;
    border-radius:1em;
    padding:.5em;
    text-align:center;
}
```

Verified at [`site.css:819-825`](FreeCRM/CRM.Client/wwwroot/css/site.css#L819-L825). A search for underscore-style class names in `site.css` returns **zero** matches — underscores are never used. Very short utility classes are single words: `.note` ([`site.css:210`](FreeCRM/CRM.Client/wwwroot/css/site.css#L210)), `.pointer` ([`site.css:225`](FreeCRM/CRM.Client/wwwroot/css/site.css#L225)), `.center` ([`site.css:604`](FreeCRM/CRM.Client/wwwroot/css/site.css#L604)), `.bold` ([`site.css:846`](FreeCRM/CRM.Client/wwwroot/css/site.css#L846)), `.hidden` ([`site.css:936`](FreeCRM/CRM.Client/wwwroot/css/site.css#L936)).

### 4.3 Braces and indentation

Put the `{` on the **same line** as the selector (preceded by one space), indent each declaration by **4 spaces**, and close with `}` alone on the final line.

```css
.keyword-search {
    display: inline-flex;
    padding-left: 10px;
    position: relative;
    vertical-align: middle;
}
```

Verified at [`site.css:309-314`](FreeCRM/CRM.Client/wwwroot/css/site.css#L309-L314). This is the default formatting Visual Studio applies to CSS, so it falls out of the editor automatically.

**Nuance:** one-liner utility rules are deliberately collapsed onto a single line — `.min-50 {min-width:50px;}` and `.min-100 {min-width:100px;}` at [`site.css:331-332`](FreeCRM/CRM.Client/wwwroot/css/site.css#L331-L332). And in `tags.css`, the nested `.tag-selector > .tag` child is indented an *extra* level (8 spaces) at [`tags.css:22-24`](FreeCRM/CRM.Client/wwwroot/css/tags.css#L22-L24) even though CSS has no real nesting — that's a stray inconsistency, not a rule.

### 4.4 One property per line, trailing semicolon

Each declaration gets its **own line** and always ends with a **semicolon** — including the last one in the block. The trailing semicolon avoids breakage when properties are later appended.

```css
.login-button {
    width: 100%;
    min-width: 100px;
    min-height: 50px;
    border-color: rgba(20, 20, 20, .3);
}
```

Verified at [`site.css:680-685`](FreeCRM/CRM.Client/wwwroot/css/site.css#L680-L685). (The collapsed one-liners still terminate their single property with a semicolon.)

**Space-after-colon is INCONSISTENT.** Both `property: value` (spaced) and `property:value` (no space) appear, frequently in the same file and even the same block — for example the dense unspaced style at [`site.css:836-840`](FreeCRM/CRM.Client/wwwroot/css/site.css#L836-L840) versus the spaced `.loading-progress` at [`site.css:135-141`](FreeCRM/CRM.Client/wwwroot/css/site.css#L135-L141). This is accidental drift from hand-editing without a formatter — **don't read meaning into it; just match the surrounding block.**

### 4.5 Property ordering — there isn't one

**Do not expect alphabetical or grouped (positioning → box → typography) property ordering.** Properties appear in whatever order the author typed them. No tooling enforces ordering and the files show no consistent scheme — see `.keyword-search` at [`site.css:309-314`](FreeCRM/CRM.Client/wwwroot/css/site.css#L309-L314) (display, then padding, then position, then vertical-align). A few blocks happen to land near-alphabetical (`#blazor-error-ui` at [`site.css:106-116`](FreeCRM/CRM.Client/wwwroot/css/site.css#L106-L116)) but that's coincidence, not a rule.

### 4.6 Comments

Use standard `/* ... */` comment blocks for file headers and notes. File-top comments document intent (e.g. how to swap fonts). Commented-out declarations are frequently left **inline** as a record of "what we tried" rather than deleted — e.g. `/*margin-bottom:.2em;*/` at [`site.css:504`](FreeCRM/CRM.Client/wwwroot/css/site.css#L504) and a whole commented block at [`site.css:39-40`](FreeCRM/CRM.Client/wwwroot/css/site.css#L39-L40). It's an informal habit, not a documented rule; it does clutter the files. (There's even an unfixed typo — "sepecify" — in the header comment at [`site.css:1-4`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1-L4), a sign comments aren't reviewed closely.)

### 4.7 `!important` and vendor prefixes — pragmatic, not default

Reach for `!important` (a CSS flag that forces a declaration to win regardless of specificity) **only** when you must override a third-party library's high-specificity or late-applied styles. Include legacy *vendor prefixes* (`-webkit-`, `-moz-`, `-ms-` — older browser-specific spellings of a property) alongside the standard property where broad browser support matters.

```css
.prevent-select {
    -webkit-user-select: none; /* Safari */
    -ms-user-select: none; /* IE 10 and IE 11 */
    user-select: none; /* Standard syntax */
}
```

Verified at [`site.css:1334-1338`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1334-L1338). `!important` shows up mainly in the Highcharts/Radzen variable overrides and a few utilities ([`site.css:182`](FreeCRM/CRM.Client/wwwroot/css/site.css#L182), [`site.css:945`](FreeCRM/CRM.Client/wwwroot/css/site.css#L945), [`site.css:974-977`](FreeCRM/CRM.Client/wwwroot/css/site.css#L974-L977); [`themes.css:227-229`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L227-L229)). Use it the same way: as a deliberate override of vendored CSS, not as your default reach.

---

<a id="theming"></a>
## 5. Theming with CSS Custom Properties

A **CSS custom property** — written `--name` — is a reusable variable you define once and read elsewhere with `var(--name)`. Bootstrap 5's entire theming system is built on these: it prefixes its variables `--bs-*`, and switches a theme by reading a `data-bs-theme` attribute on a container element. FreeCRM's theming follows that exactly.

**The rule:** implement themes by **overriding CSS custom properties scoped to a `[data-bs-theme=<name>]` attribute selector.** Define app-wide variable *defaults* in `:root` (`:root` is a selector for the document root — the place to declare global variables). **Do not hard-fork stylesheets per theme.**

**Why:** the page's `<body>` carries a `data-bs-theme` attribute (at [`App.razor:63`](FreeCRM/CRM/Components/App.razor#L63)), so an attribute-prefixed selector is how a theme is switched at runtime. Reassigning the framework's *own* variables (rather than rewriting rules) makes the override automatically flow through every component that already reads them — Bootstrap (`--bs-*`), Radzen (`--rz-*`), MudBlazor (`--mud-*`), and Highcharts (`--highcharts-*`).

```css
[data-bs-theme=blue] {
    color-scheme: blue;
    --bs-body-color: #084298;
    --bs-body-color-rgb: 8, 66, 152;
    --bs-tertiary-bg: #084298;
    --bs-tertiary-bg-rgb: 8, 66, 152;
    --bs-border-color: #084298;
    --bs-emphasis-color: #084298;
    --bs-emphasis-color-rgb: 255, 255, 255;
}
```

Verified at [`themes.css:247-256`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L247-L256). The dark theme does the same for buttons:

```css
[data-bs-theme=dark] .btn-dark {
    --bs-btn-bg: #111111;
    --bs-btn-border-color: #000;
    --bs-btn-hover-bg: #000;
    --bs-btn-hover-border-color: #111;
}
```

Verified at [`themes.css:69-74`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L69-L74).

The colored themes — blue, indigo, purple, pink, red, orange, yellow, green, teal, cyan, gray — are **near-identical templates copy-pasted per theme name** across [`themes.css:247-494`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L247-L494) (e.g. the `blue` block at [247-264](FreeCRM/CRM.Client/wwwroot/css/themes.css#L247-L264), the `gray` block at [477-494](FreeCRM/CRM.Client/wwwroot/css/themes.css#L477-L494)). Each theme is roughly a 10-line variable block plus a `nav.navbar` rule and a `.dropdown-menu` rule. App-level variable *defaults* live in the `:root` block in `site.css` at [`site.css:952-978`](FreeCRM/CRM.Client/wwwroot/css/site.css#L952-L978). **To add a new theme color, copy an existing `[data-bs-theme=<name>]` block, rename it, and adjust the values** — that is the established pattern, even if copy-paste isn't elegant.

A couple of real breaks from the "prefix everything with the attribute" pattern, both in `themes.css`: `body.dark { ... }` uses a *class* hook instead of the attribute at [`themes.css:226-230`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L226-L230) (for the `--highcharts-*` variables), and there's a `:root` / `[data-bs-theme=dark]` combo split across two lines at [`themes.css:139-140`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L139-L140) (almost certainly an accidental newline).

### Units

Use **relative units** (`em`, `rem` — sizes relative to font-size) for padding, margins, and type; raw `px` for things that must be an exact pixel size (icon widths, fixed-width helpers, navbar height); and `%`/`vw`/`vh` for fluid dimensions. Relative units scale with the user's font-size setting (the root is fixed at `16px` at [`site.css:38`](FreeCRM/CRM.Client/wwwroot/css/site.css#L38)).

```css
.btn-xs {
    padding: .2rem .2rem;
    font-size: .8rem !important;
    line-height: .6rem;
    border-radius: .2rem;
}
```

Verified at [`site.css:189-194`](FreeCRM/CRM.Client/wwwroot/css/site.css#L189-L194). Fixed-pixel examples: `min-width:20px` ([`site.css:58`](FreeCRM/CRM.Client/wwwroot/css/site.css#L58)), the `.fixed-50`…`.fixed-300` width helpers ([`site.css:708-741`](FreeCRM/CRM.Client/wwwroot/css/site.css#L708-L741)), `min-height: 59px` navbar ([`site.css:267`](FreeCRM/CRM.Client/wwwroot/css/site.css#L267)). Fluid: `width: 50vw` ([`site.css:449`](FreeCRM/CRM.Client/wwwroot/css/site.css#L449)), `height:calc(100vh - 125px)` ([`site.css:1038`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1038)). **Leading zeros on sub-1 decimals are inconsistent** — `.8rem` (no zero, [`site.css:191`](FreeCRM/CRM.Client/wwwroot/css/site.css#L191)) vs `0.9em` (with zero, [`tags.css:9`](FreeCRM/CRM.Client/wwwroot/css/tags.css#L9)). Match the local block; don't agonize.

### Colors

Write colors as **hex** (lowercase — 3-digit shorthand like `#fff`, `#555`, `#000` for greys; 6-digit for brand colors like `#0d6efd`); use **CSS color keywords** (`red`, `crimson`, and the whole `.tag-*` palette) where the name *is* the meaning; and reserve **`rgba()`** (a color with an alpha/transparency channel) for shadows and translucency only.

```css
.tag-crimson {
    background-color: crimson;
    border-color: crimson;
    color: #fff;
}
```

Verified at [`tags.css:51-55`](FreeCRM/CRM.Client/wwwroot/css/tags.css#L51-L55). Hex is consistently lowercase; 3-digit shorthand dominates for greyscale (`#eee`, `#ddd`, `#ccc`, `#888`, `#555`, `#222`, `#fff`, `#000` — e.g. [`site.css:266`](FreeCRM/CRM.Client/wwwroot/css/site.css#L266), [`site.css:279`](FreeCRM/CRM.Client/wwwroot/css/site.css#L279)). Full 6-digit appears for brand colors (`#dc3545` at [`site.css:417`](FreeCRM/CRM.Client/wwwroot/css/site.css#L417), `#0d6efd` at [`site.css:953`](FreeCRM/CRM.Client/wwwroot/css/site.css#L953)). `rgba()` appears **exactly 5 times** ([`site.css:109`](FreeCRM/CRM.Client/wwwroot/css/site.css#L109), [`site.css:684`](FreeCRM/CRM.Client/wwwroot/css/site.css#L684), [`site.css:1180-1182`](FreeCRM/CRM.Client/wwwroot/css/site.css#L1180-L1182)), all for box-shadows or a translucent border — and even its own spacing is inconsistent (`rgba(0,0,0, .7)` vs `rgba(0, 0, 0, 0.7)` on adjacent lines). The `-rgb` theme variables store bare comma-separated channels for Bootstrap (`--bs-body-color-rgb: 8, 66, 152;` at [`themes.css:250`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L250)).

---

<a id="media-queries"></a>
## 6. Media Queries and Responsive

A **media query** (`@media (...)`) wraps a block of CSS that only applies when a condition is true — historically "the screen is at least this wide," but modern queries can also detect device *capabilities* and user *preferences*. FreeCRM's hand-written media queries are all of the capability/preference kind. Indent the rules inside one level (4 spaces) under the `@media`.

```css
@media (any-pointer: fine) {
    .sticky-menu-icon {
        display: inline-block;
        cursor: pointer;
        opacity:0.2;
        font-size:.9rem;
        vertical-align:top;
    }
```

Verified at [`site.css:47-54`](FreeCRM/CRM.Client/wwwroot/css/site.css#L47-L54). This `(any-pointer: fine)` query asks "does the device have a precise pointer (a mouse)?" — used to decide whether to show the sticky-menu pin icon. The other media queries are `prefers-color-scheme` probes that write a hidden `content: 'dark'`/`'light'` marker into `body::after` so JavaScript can read the OS theme ([`site.css:8-20`](FreeCRM/CRM.Client/wwwroot/css/site.css#L8-L20)).

**There are NO width-based responsive breakpoints in the hand-written CSS.** Responsive layout is delegated *entirely* to Bootstrap's grid and utility classes. This is the Bootstrap-first reality made concrete: when you need a layout to adapt to screen size, you reach for Bootstrap's responsive classes (`col-md-*`, `d-none d-md-block`, and so on), not a hand-written `@media (max-width: ...)`. (Spacing inside the query parens is itself inconsistent — `(any-pointer: fine)` is spaced, `(prefers-color-scheme:dark)` at [`site.css:8`](FreeCRM/CRM.Client/wwwroot/css/site.css#L8) is not.)

---

<a id="quick-reference"></a>
## 7. Quick Reference

The left column is wrong for this codebase; the right column matches the existing style. All right-column forms are copied from or consistent with the real source.

**Where does my CSS go?**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| Edit `bootstrap.min.css` / fontawesome / highcharts | Override their variables in *our* files |
| Add a one-off app tweak to `site.css` | Put app-specific overrides in `site.App.css` |
| Create a new global `.css` file | Use one of the four: `themes.css` / `site.css` / `site.App.css` / `tags.css` |
| Hand-write a `<link>` for a `.razor.css` | Let Blazor bundle scoped CSS automatically |
| Reach for a `.razor.css` by default | Add a named class to `site.css` (the house norm) |

**Selectors and naming**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `.dragAndDropUpload` (camelCase) | `.drag-and-drop-upload` (kebab-case) |
| `.drag_and_drop_upload` (underscores) | `.drag-and-drop-upload` (hyphens) |
| An ID for a reusable style | An ID only for a true singleton (`#page-area`) |
| `.a, .b, .c { ... }` for a hand-typed group | one selector per line, comma-terminated |

**Formatting**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `.foo`<br>`{` (brace on new line) | `.foo {` (brace same line) |
| Tabs / 2-space indent | 4-space indent |
| `width: 100%` (missing semicolon) | `width: 100%;` (always semicolon-terminated) |
| `!important` as a default | `!important` only to override vendored CSS |

**Theming**

```css
/* ✗ Avoid — hard-coding a color for a theme */
[data-bs-theme=blue] .navbar { background: #084298; }

/* ✓ House style — reassign the framework variable */
[data-bs-theme=blue] {
    --bs-body-color: #084298;
    --bs-body-color-rgb: 8, 66, 152;
}
```

---

<a id="faq"></a>
## 8. FAQ

**Q: Should I write custom CSS or use a Bootstrap class?**
Use a Bootstrap class. FreeCRM is Bootstrap-first — the vast majority of styling is Bootstrap utility classes, and we write very little CSS of our own. Write custom CSS only when no Bootstrap (or Radzen/Mud) class does the job.

**Q: Where do I put a one-off style — `site.css` or `site.App.css`?**
For app-specific overrides, use `site.App.css`. It loads after `site.css` (so it wins) and survives upstream updates because it's the local override layer. Put genuinely shared/framework styling in `site.css`; put *your app's* tweaks in `site.App.css`.

**Q: What is `site.App.css` for?**
It's the intentionally-empty local override layer. FreeCRM is a template that periodically syncs new code from its author. Editing `site.css` directly means your changes get overwritten on the next sync; putting them in `site.App.css` means they survive, because nothing upstream touches that file. It's the `.App.` upgrade-safe convention used across the codebase. Verified empty at [`site.App.css:1-3`](FreeCRM/CRM.Client/wwwroot/css/site.App.css#L1-L3).

**Q: Can I edit `bootstrap.css` / fontawesome / highcharts?**
No. Those are vendored — copied in and not maintained by us. Any edit you make gets wiped on the next upgrade. To change how they look, override their CSS variables in *our* files (Section 5). Anything under `fontawesome/`, `js/bootstrap/`, `lib/highcharts/`, any `_content/...` bundle, or any `*.min.*` is off-limits.

**Q: How do scoped `.razor.css` files work?**
You put a file named `<Component>.razor.css` next to the component's `.razor` file. Blazor automatically scopes those rules to that one component (so they can't leak) and bundles them into `CRM.Client.styles.css`, which it injects for you. You never hand-write a `<link>` for it. That said, this codebase uses scoped CSS almost never — exactly one such file exists ([`UploadFile.razor.css`](FreeCRM/CRM.Client/Shared/UploadFile.razor.css#L1-L12)), and it's even orphaned from its host. Prefer global classes in `site.css`.

**Q: How do I add a new theme color?**
Copy an existing `[data-bs-theme=<name>]` block in `themes.css` (e.g. the `blue` block at [`themes.css:247-264`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L247-L264)), rename the attribute value, and adjust the `--bs-*` variable values. Copy-paste-per-theme is the established pattern across [`themes.css:247-494`](FreeCRM/CRM.Client/wwwroot/css/themes.css#L247-L494).

**Q: How do I name a new class?**
kebab-case: all lowercase, words joined by hyphens (`drag-and-drop-upload`). Never camelCase or underscores. This matches Bootstrap's naming so our classes coexist with theirs. Verified at [`site.css:819-825`](FreeCRM/CRM.Client/wwwroot/css/site.css#L819-L825).

**Q: Spaces after the colon — `color: #fff` or `color:#fff`?**
Both appear; there's no enforced rule and no formatter. Don't read meaning into it — match whatever the rule directly above yours does. Same answer for leading zeros on decimals (`.5em` vs `0.5em`) and property ordering: there is no convention, so match the local block.

**Q: When is `!important` acceptable?**
Only to override a third-party library (Bootstrap/Radzen/MudBlazor/Highcharts) whose styles have high specificity or are applied late. It's a pragmatic override tool, not a default. See [`site.css:944-946`](FreeCRM/CRM.Client/wwwroot/css/site.css#L944-L946).

**Q: Do we have responsive breakpoints / media queries?**
The only hand-written media queries are *feature/preference* queries (`prefers-color-scheme`, `any-pointer`) at [`site.css:8-54`](FreeCRM/CRM.Client/wwwroot/css/site.css#L8-L54). There are **no width breakpoints** — responsive layout is entirely Bootstrap's grid and utility classes.

**Q: Is there a linter or formatter I should run?**
No. There is no Stylelint/Prettier config and no formal CSS style guide. The style is whatever Visual Studio's default CSS formatting produces (4-space indent, same-line braces). When in doubt, copy the rule directly above the one you're adding.

**Q: Why is the CSS not perfectly consistent?**
Because it's hand-maintained without a formatter. Space-after-colon, leading zeros, property ordering, and `rgba()` spacing all drift. That's expected — don't try to derive a hidden rule from the inconsistency. Match locally and move on.

---

<a id="related-docs"></a>
## 9. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the C#/brace/casing house conventions this doc mirrors for CSS
- [055 — The C# Coding Style Reference](055_csharp-style-reference.md) — the equivalent reference for server- and client-side C#
- [056 — The Razor Coding Style Reference](056_razor-blazor-style-reference.md) — markup and `.razor` component conventions (where most styling classes are applied)
- [058 — The JavaScript Coding Style Reference](058_javascript-style-reference.md) — the sibling reference for our hand-written JS (which reads the theme markers this CSS writes)
- [041 — The Upgrade-Safe Model](041_upgrade-safe-model.md) — why the `.App.` override split exists, of which `site.App.css` is one instance
- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — the fork-sync process that makes the override layer necessary

---
*GuidesV2 057 · The CSS Style Reference · drafted 2026-06-05 from source (`FreeCRM/CRM.Client/wwwroot/css/{site.css, site.App.css, tags.css, themes.css}`, `FreeCRM/CRM.Client/Shared/UploadFile.razor.css`, and `FreeCRM/CRM/Components/App.razor`). Every rule citation-verified against the live source; vendored CSS (bootstrap/fontawesome/highcharts) excluded by design.*
