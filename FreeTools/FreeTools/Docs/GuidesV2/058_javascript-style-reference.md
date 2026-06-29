# 058 — The JavaScript Style Reference

> **Document ID:** 058  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Document the entire hand-written JavaScript surface — a handful of collocated `*.razor.js` interop modules — and the code-style rules they follow, so a new contributor can read or add one safely.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why There's Barely Any JavaScript](#why-it-matters) | The Blazor-WebAssembly framing, why we write almost no JS, and why there is deliberately no TypeScript |
| 2 | [The `.razor.js` Interop-Module Convention](#convention) | Collocation, `export`ed functions, file naming and location — the four real modules |
| 3 | [How C# Loads and Calls a Module](#interop) | Importing the module, calling `InvokeVoidAsync`/`InvokeAsync<T>`, and `DotNetObjectReference` callbacks back into C# |
| 4 | [JavaScript Code Style](#code-style) | `var` vs `const`/`let`, function declarations vs arrows, casing, semicolons, quotes, indentation, comments, error handling |
| 5 | [Quick Reference](#quick-reference) | Side-by-side ✗/✓ cheat sheet you can copy |
| 6 | [FAQ](#faq) | The real questions a new contributor asks about this tiny surface |
| 7 | [Related Docs](#related-docs) | Sibling style docs and the rich-components doc that uses these modules |

---

<a id="why-it-matters"></a>
## 1. Why There's Barely Any JavaScript

Here is the single most important thing to understand before you read another line: **in a Blazor WebAssembly app, the C# runs in the browser, so we write almost no JavaScript.**

Some terms, defined on first use:

- **Blazor WebAssembly** is a way to build a web app's front end in C# instead of JavaScript. **WebAssembly** (often "Wasm") is a low-level format that browsers can run at near-native speed; Blazor compiles the .NET runtime to WebAssembly so your C# executes *inside the browser tab*, the same place JavaScript normally runs.
- An **ES module** (ECMAScript module) is a self-contained JavaScript file that explicitly `export`s the functions other code may call. Code outside the file can only reach the names it exports; everything else stays private to the file.
- **JS interop** ("interoperation") is how Blazor's C# calls into JavaScript and back. It is the bridge you use when C# needs to do something only JavaScript can do.

**Why this matters:** because the C# runs in the browser, the things you would normally reach for JavaScript to do — handle a click, update the page, call an API, hold state — are all done in C# here. You do **not** write JavaScript for ordinary feature work. The only time hand-written JavaScript appears is when C# physically cannot reach a browser or third-party-library feature on its own, and a thin JS shim has to wrap it.

That makes the hand-written JavaScript surface genuinely tiny. In the entire `CRM.Client` project there are exactly **four** hand-written `*.razor.js` files, totaling 469 lines — and roughly 90% of that is a single charting module. The full inventory (everything else under `wwwroot/js` and `wwwroot/lib` is *vendored*, meaning third-party libraries like bootstrap, jquery, sortablejs, and the Highcharts library — not our code and not covered here):

| File | Lines | Wired into a component? |
|---|---|---|
| [`FreeCRM/CRM.Client/Shared/Highcharts.razor.js`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js) | 424 | Yes — `Highcharts.razor` |
| [`FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js) | 26 | Yes — `MonacoEditor.razor` |
| [`FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js) | 15 | Yes — `UserDefinedFields.razor` |
| [`FreeCRM/CRM.Client/Shared/UploadFile.razor.js`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js) | 4 | **No — orphaned dead code** (see §2) |

Be honest with yourself about scale as you read this doc: this is a small, **interop-only** surface. Three of these modules wrap something C# can't touch directly — a charting library (Highcharts), a code-editor library (Monaco), and reading the selected value out of a raw HTML radio-button group. The rules below are documented from that real code; nothing is padded.

**Why there is no TypeScript — and why that's the right call.** **TypeScript** is a typed superset of JavaScript: you write `.ts` files with type annotations, and a *build step* (a "transpiler") compiles them down to plain `.js` before the browser can run them. Its payoff is catching type mistakes at build time and giving editors better autocomplete. This project has **zero** TypeScript: no `.ts` files and no `tsconfig.json` anywhere in `CRM.Client` (verified by glob — zero results for `**/*.ts` and `**/tsconfig*.json`).

That is a deliberate, reasonable choice, not an oversight. The reason: these modules are tiny, interop-only glue whose whole job is to call one browser or library API and hand a result back to C#. The type safety you actually care about lives on the **C# side**, which already provides strong typing at the call boundary (`InvokeAsync<string>(...)`, typed parameters). Adding a TypeScript build pipeline — a transpiler, config, and an extra step in every build — to maintain a few ~15-line DOM shims would be pure overhead with no payoff. For a surface this small and this stable, plain ES modules are simpler and entirely adequate.
> Proof: plain ES modules with `export function` and `var`, no build step — e.g. the entire [`UserDefinedFields.razor.js:1-15`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L1-L15). Absence of TypeScript verified by glob (`**/*.ts` → 0, `**/tsconfig*.json` → 0).

---

<a id="convention"></a>
## 2. The `.razor.js` Interop-Module Convention

When a component genuinely needs JavaScript, the project follows Blazor's standard **JavaScript isolation** convention: the JS lives in an ES module collocated next to the component that uses it.

**Rule 2a — Collocate the module beside its component, named `<Component>.razor.js`.** Each component's interop JavaScript lives in the same folder as the component, with the component's filename plus a `.js` suffix. So `MonacoEditor.razor` pairs with `MonacoEditor.razor.js`, sitting right next to each other.

**Why:** this is a framework-driven convention, not a free house choice. A module placed at `<path>/<Component>.razor.js` is served by Blazor as a scoped *static web asset* that the component imports by relative path. The benefit is physical proximity: the JavaScript that belongs to one component lives right beside that component, so you never hunt for it.

```
FreeCRM/CRM.Client/Shared/MonacoEditor.razor
FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js
FreeCRM/CRM.Client/Shared/Highcharts.razor
FreeCRM/CRM.Client/Shared/Highcharts.razor.js
```

> Proof: a glob of `FreeCRM/CRM.Client/**/*.razor.js` returns exactly the four files, each adjacent to its `.razor` — e.g. [`MonacoEditor.razor.js`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js) next to `MonacoEditor.razor`. All four happen to live in `Shared/` because they are shared widgets; that folder is incidental, not part of the rule.

**Rule 2b — Expose interop entry points with `export function`; keep helpers un-exported.** Every function the C# side will call must be a top-level named `export function`. Helpers that only other JavaScript calls stay un-exported (a plain `function`).

**Why:** the component imports the file *as an ES module*, and only `export`ed names are reachable through that module reference. Un-exported functions are module-private helpers — invisible to C# by design.

```javascript
export function SetDotNetHelper(value) {
    dotNetHelper = value;
}

export function RenderChart_Column(elementId, chartTitle, chartSubtitle, yAxisText, seriesCategories, seriesData) {
```

A private helper, **not** exported, because only other JS calls it:

```javascript
function LoadScriptResource(url, callback) {
    var head = document.getElementsByTagName('head')[0];
    var script = document.createElement('script');
```

> Proof: exported `SetDotNetHelper` at [`Highcharts.razor.js:113-115`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L113-L115) and exported `RenderChart_Column` opening at [`Highcharts.razor.js:117`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L117); private `LoadScriptResource` at [`Highcharts.razor.js:102-111`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L102-L111). Every C#-invoked name across the four files is exported; the only un-exported functions (`waitForHighcharts`, `FileNameFromUrl`, `LoadCssResource`, `LoadScriptResource`, `withZoomHint`) are purely internal.

**Rule 2c — Name exported (C#-facing) functions in PascalCase to match the C# call string.** Exported functions use **PascalCase** (first letter of every word capitalized: `RenderChart_Column`, `RadioOptionValue`), identical to the string literal C# passes when it calls them.

**Why:** JavaScript normally favors **camelCase** (lowercase first letter), but FreeCRM names these exports to mirror the C# method-name style, so the call reads the same on both sides. The only *hard* requirement is that the JS function name matches the C# call string exactly, character for character — the PascalCase choice is the house style that makes that easy to eyeball.

```javascript
export function RadioOptionValue(name) {
```

The C# side, passing the same name as a string:

```csharp
value += await jsModule.InvokeAsync<string>("RadioOptionValue", element);
```

> Proof: JS at [`UserDefinedFields.razor.js:1`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L1); C# call at [`UserDefinedFields.razor:106`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L106). Private internal helpers do *not* follow PascalCase — e.g. `waitForHighcharts` ([`Highcharts.razor.js:42`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L42)) and `withZoomHint` ([`Highcharts.razor.js:223`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L223)) are camelCase. PascalCase applies to the *exported* functions; private helpers are looser.

**Rule 2d — Verify a `.razor.js` is actually imported before you trust it.** The collocated-file naming convention does **not** guarantee the file is wired up. A `.razor.js` can drift into dead code.

The live example: `UploadFile.razor.js` exports `CopyPasswordToClipboard(password)`, but **nothing imports it.** `UploadFile.razor` has no `import` of its own `.razor.js` at all, and the real password-copy path (`GeneratePasswordDialog.razor`) uses a C# helper, `Helpers.CopyToClipboard(...)`, not this JS.

```javascript
export function CopyPasswordToClipboard(password) {
    navigator.clipboard.writeText(password);
    console.log("Password Copied to Clipboard");
}
```

**Why this matters for you:** before trusting or editing a `.razor.js`, confirm the sibling `.razor` actually does `InvokeAsync<IJSObjectReference>("import", ...)` for it (see §3). If nothing imports it, your edits do nothing.

> Proof: the orphaned export is the whole of [`UploadFile.razor.js:1-4`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js#L1-L4). `UploadFile.razor` contains no `import`/`IJSObjectReference`; the same-named **C#** method in [`GeneratePasswordDialog.razor:59`](FreeCRM/CRM.Client/Shared/GeneratePasswordDialog.razor#L59) calls `Helpers.CopyToClipboard(_password)` ([`:61`](FreeCRM/CRM.Client/Shared/GeneratePasswordDialog.razor#L61)) instead. A grep for `InvokeAsync<IJSObjectReference>("import"` across `CRM.Client` returns only the three live components.

---

<a id="interop"></a>
## 3. How C# Loads and Calls a Module

This is the half that lives in the `.razor` component (C#). You will write more of *this* than of the JavaScript itself. The flow has four beats: import the module, store the reference, call its functions, and — if JS needs to talk back — pass a callback handle.

**Rule 3a — Import the module on first render via `import`, storing the result in an `IJSObjectReference`.** In the component, on first render only, import the collocated module and keep the returned reference in a field.

Terms: `IJSRuntime` is the service Blazor injects so C# can run JavaScript. `IJSObjectReference` is a C# handle to a loaded JS module — once you hold it, every later call hits the already-loaded module. `OnAfterRenderAsync(bool firstRender)` is the component lifecycle method that runs after the component has rendered; `firstRender` is `true` only the very first time.

**Why:** `import` is JavaScript's dynamic-import that returns the module object. Doing it once on first render (not on every render) avoids re-importing the file repeatedly, and holding the reference lets all later method calls reuse the loaded module.

```csharp
protected IJSObjectReference? jsModule;
...
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender) {
        dotNetHelper = DotNetObjectReference.Create(this);
        jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./Shared/Highcharts.Razor.js?v=" + Guid.NewGuid().ToString().Replace("-", ""));
        await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);
```

> Proof: field at [`Highcharts.razor:56`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L56), import on first render at [`Highcharts.razor:65-70`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L65-L70). Same pattern at [`MonacoEditor.razor:112-114`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L112-L114) and [`UserDefinedFields.razor:84-88`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L84-L88). The `dotNetHelper`/`SetDotNetHelper` lines are Highcharts-only (see Rule 3d); Monaco and UserDefinedFields import the module but set up no callback handle.

A few real inconsistencies in the import string are worth knowing so you don't read significance into them:

- **Cache-buster (`?v=<guid>`) is the dominant but not universal habit.** Two of the three live imports append `?v=` followed by a dashless GUID. **Why:** it defeats browser caching of the `.razor.js` file, so a newly deployed version always loads instead of a stale cached copy. `UserDefinedFields.razor` omits it. Prefer including it. Proof: cache-buster at [`MonacoEditor.razor:114`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L114) and [`Highcharts.razor:69`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L69); omitted at [`UserDefinedFields.razor:87-88`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L87-L88).
- **Path casing (`.Razor.js` vs `.razor.js`) varies and is not significant.** Some imports write `./Shared/Highcharts.Razor.js` (capital R); others write `./Shared/UserDefinedFields.razor.js` (lowercase). The on-disk file is *always* lowercase `.razor.js`; the capital-R versions only resolve because static-web-asset serving is case-insensitive. **Treat lowercase (matching the file) as the safe choice.** Proof: capital `Razor` at [`Highcharts.razor:69`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L69) and [`MonacoEditor.razor:114`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L114); lowercase at [`UserDefinedFields.razor:88`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L88).

**Rule 3b — Call into the module with `InvokeVoidAsync` (no return) or `InvokeAsync<T>` (returns a value); null-check `jsModule` first.** Use `jsModule.InvokeVoidAsync("ExportedName", args...)` when the JS function returns nothing, and `jsModule.InvokeAsync<string>("ExportedName", args...)` when you need its return value marshaled back to C# as type `T`. Always guard with `if (jsModule != null)` first.

**Why:** these are the real interop semantics — `InvokeVoidAsync` discards the JS result; `InvokeAsync<T>` marshals the JS return value back to C#. The null-check guards against calling before the first-render import has completed.

A void call, guarded:

```csharp
if (jsModule != null) {
    _chartRendered = true;
    await jsModule.InvokeVoidAsync("RenderChart_Column", _elementId, _chartTitle, _chartSubtitle, _yAxisText, SeriesCategories, SeriesDataArrayItems);
}
```

A value-returning call:

```csharp
if (jsModule != null) {
    value += await jsModule.InvokeAsync<string>("RadioOptionValue", element);
}
```

> Proof: void call at [`Highcharts.razor:220-223`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L220-L223); value-returning call at [`UserDefinedFields.razor:105-107`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L105-L107). The matching JS that builds and `return`s a value is [`UserDefinedFields.razor.js:1-15`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L1-L15). Nuance: `MonacoEditor.razor` wraps its `InvokeVoidAsync("SetAriaLabel", ...)` in a `Helpers.SetTimeout(..., 300)` delay because the editor DOM isn't ready immediately ([`MonacoEditor.razor:116-118`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L116-L118)) — interop calls may need to wait for a third-party widget to mount.

**Rule 3c — For callbacks JS→C#, pass a `DotNetObjectReference` into the module and call `invokeMethodAsync`; mark the C# target `[JSInvokable]`.** When JavaScript needs to call *back* into the component (e.g. a chart slice was clicked), the component creates a `DotNetObjectReference.Create(this)`, hands it to the module through an exported setter, the module stores it and calls `dotNetHelper.invokeMethodAsync("MethodName", args)`, and the C# method is decorated `[JSInvokable]`.

Terms: a `DotNetObjectReference` is a handle that lets JavaScript invoke instance methods on a specific C# object. `[JSInvokable]` is an attribute marking a C# method as callable from JavaScript — without it, JS cannot reach the method.

**Why:** this is a real interop requirement, not a style choice. JS can only invoke C# instance methods through a `DotNetObjectReference`, and only methods explicitly marked `[JSInvokable]` are reachable.

JS side — store the reference and call back:

```javascript
var dotNetHelper;
...
export function SetDotNetHelper(value) {
    dotNetHelper = value;
}
...
click: function (event) {
    dotNetHelper.invokeMethodAsync("ChartItemClicked", this.index);
}
```

C# side — create, pass, mark invokable:

```csharp
dotNetHelper = DotNetObjectReference.Create(this);
...
await jsModule.InvokeVoidAsync("SetDotNetHelper", dotNetHelper);
...
[JSInvokable]
public void ChartItemClicked(int index)
```

> Proof: JS `var dotNetHelper;` at [`Highcharts.razor.js:1`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L1), setter at [`Highcharts.razor.js:113-115`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L113-L115), the click handler at [`Highcharts.razor.js:147-153`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L147-L153) (the same `invokeMethodAsync("ChartItemClicked", this.index)` recurs at `:214`, `:266`, `:331`, `:368`); the load-complete callback `invokeMethodAsync("OnHighchartsLoaded")` at [`Highcharts.razor.js:9`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L9) and [`:29`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L29). C# create/pass at [`Highcharts.razor:68-70`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L68-L70), the invokable target at [`Highcharts.razor:200-201`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L200-L201). Only `Highcharts` uses the JS→C# direction; `MonacoEditor` and `UserDefinedFields` are one-way (C#→JS only) and create no `DotNetObjectReference`. So apply this rule only when a callback is genuinely needed.

**Rule 3d — Dispose the `DotNetObjectReference` in `Dispose()`; be aware the module reference itself is not disposed here.** The `DotNetObjectReference` should be released in the component's `Dispose()` to avoid leaking the .NET object (`dotNetHelper?.Dispose()`). Separately, note a real, consistent gap in this codebase: **none** of the three live components dispose the `jsModule` `IJSObjectReference`.

**Why flag the gap:** following Blazor docs, you might expect an `await jsModule.DisposeAsync()` in `Dispose`/`DisposeAsync`. This codebase does not do that — `Highcharts` disposes only its `dotNetHelper`, `MonacoEditor` disposes only its timer, and `UserDefinedFields` has an empty `Dispose()` body. So don't assume a module-disposal call already exists in surrounding code; it doesn't.

> Proof: a grep for `jsModule?.Dispose`/`jsModule?.DisposeAsync` across `CRM.Client` returns **zero** results. `Highcharts` disposes only `dotNetHelper` at [`Highcharts.razor:60-63`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L60-L63); `MonacoEditor` disposes only `_timer` at [`MonacoEditor.razor:107-110`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L107-L110); `UserDefinedFields.Dispose()` is empty at [`UserDefinedFields.razor:80-82`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L80-L82). One more minor inconsistency: the `jsModule` field is declared `protected` in two components ([`Highcharts.razor:56`](FreeCRM/CRM.Client/Shared/Highcharts.razor#L56), [`MonacoEditor.razor:105`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor#L105)) and `private` in one ([`UserDefinedFields.razor:78`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor#L78)) — these components aren't subclassed, so the modifier carries no meaning. Don't read significance into it.

---

<a id="code-style"></a>
## 4. JavaScript Code Style

These are the conventions *inside* the `.razor.js` files. They are documented from the real four modules, so where a convention shows up in only one file (almost everything substantial is in `Highcharts.razor.js`), that is noted. Be aware up front: this code is mid-modernization, and a few rules below codify a habit (like `var`) rather than a best practice — match the surrounding code rather than "improving" it.

**Rule 4a — Use `var`, not `const`/`let`, for local variables.** Declare locals with `var`. The live hand-written modules use `let` nowhere, and use `const` only inside commented-out (dead) code.

**Why:** honest answer — house habit/legacy, not a technical reason. Modern JavaScript would prefer `const`/`let`, but every line of *live* hand-written code here is uniformly `var`. Matching it keeps these files internally consistent.

```javascript
function FileNameFromUrl(url) {
    var output = url;
    if (url != undefined && url != null && url != "") {
        var lastSlash = url.lastIndexOf("/");
        if (lastSlash > -1) {
            output = url.substring(lastSlash + 1);
        }
    }
    return output;
}
```

> Proof: [`Highcharts.razor.js:53-62`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L53-L62); also `var dotNetHelper;` at [`Highcharts.razor.js:1`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L1), `var el = ...` at [`MonacoEditor.razor.js:2`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L2), `var output = "";` at [`UserDefinedFields.razor.js:2`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L2). The only `const` usage is inside a fully commented-out helper at [`MonacoEditor.razor.js:14-26`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L14-L26) (`const targetContainer`, `const allEditors`, `const domNode`) — dead code, not a live exception. No `let` appears anywhere.

**Rule 4b — Named `function` declarations for module-level code; arrow functions only as inline callbacks.** Define module-level and exported functions as named `function` declarations. Use arrow functions (`() => {}`) only for short inline callbacks passed to other functions.

An **arrow function** is JavaScript's shorthand function syntax (`x => x + 1`); it does *not* have its own `this`, which is exactly why it is wrong for the Highcharts event handlers below.

**Why:** the `export function` form is what Blazor's module loading calls into, and named declarations read clearly at the top level. Arrows are reserved for terse callback bodies.

Arrows as nested callbacks:

```javascript
LoadCssResource("lib/highcharts/highcharts.css", "highcharts-light", () => {
```
```javascript
setTimeout(() => waitForHighcharts(callback, attempts + 1), 100);
```

But Highcharts' own event/formatter callbacks are classic anonymous `function` expressions, **not** arrows, because they rely on `this` (the Highcharts point/chart context):

```javascript
click: function (event) {
```
```javascript
formatter: function () {
```

> Proof: every top-level function in all four files is a `function` declaration (e.g. [`UploadFile.razor.js:1`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js#L1), [`MonacoEditor.razor.js:1`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L1), [`UserDefinedFields.razor.js:1`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L1)). Arrow callbacks at [`Highcharts.razor.js:17`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L17) and [`:47`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L47); classic `function` event callbacks at [`Highcharts.razor.js:149`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L149) and [`:186`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L186) (these use `this`, so they cannot be arrows).

**Rule 4c — Exported functions PascalCase; locals and parameters camelCase.** Exported (C#-facing) functions use PascalCase (covered in §2c). Local variables and function parameters use standard JavaScript camelCase: `elementId`, `seriesData`, `lastSlash`.

> Proof: PascalCase exports + camelCase params at [`Highcharts.razor.js:159-166`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L159-L166) (`RenderChart_Pie(elementId, chartTitle, chartSubtitle, seriesData)`). The `RenderChart_Column` / `RenderChart_Pie` family deliberately mixes PascalCase with an underscore to separate the chart-type suffix. Private helpers `waitForHighcharts` ([`:42`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L42)) and `withZoomHint` ([`:223`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L223)) are camelCase.

**Rule 4d — Always terminate statements with a semicolon.** End every statement with a `;`. Don't rely on automatic semicolon insertion.

```javascript
export function CopyPasswordToClipboard(password) {
    navigator.clipboard.writeText(password);
    console.log("Password Copied to Clipboard");
}
```

> Proof: [`UploadFile.razor.js:1-4`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js#L1-L4); consistent across all files, e.g. `dotNetHelper = value;` at [`Highcharts.razor.js:114`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L114), `return output;` at [`UserDefinedFields.razor.js:14`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L14). (Trailing commas inside Highcharts config objects are separate from statement termination — see Rule 4i.)

**Rule 4e — Double quotes for your own code; single quotes only inside Highcharts config objects.** Default to double quotes (`"..."`) for string literals. The Highcharts chart-configuration object literals use single quotes (`'...'`).

**Why:** double quotes are the house default, seen in all the DOM, console, and URL code. The single quotes inside Highcharts configs mirror Highcharts' own documentation style for those option objects — a localized convention, not project-wide.

House default — double quotes:

```javascript
    if (typeof (Highcharts) == "object") {
        dotNetHelper.invokeMethodAsync("OnHighchartsLoaded");
    } else {
```

Highcharts config — single quotes:

```javascript
        chart: {
            type: 'column',
            styledMode: true
        },
        credits: { enabled: false },
```

> Proof (double quotes): [`Highcharts.razor.js:8-10`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L8-L10); also [`UploadFile.razor.js:3`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js#L3) and [`MonacoEditor.razor.js:2`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L2). Proof (single quotes): [`Highcharts.razor.js:121-125`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L121-L125); e.g. `type: 'pie'` ([`:173`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L173)), `'<b>{point.name}</b>'` ([`:203`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L203)). The single-quote habit also extends slightly into the DOM-creation helpers, e.g. `document.getElementsByTagName('head')[0]` and `document.createElement('script')` at [`Highcharts.razor.js:103-104`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L103-L104).

**Rule 4f — Four-space indentation.** Indent with four spaces per level.

```javascript
export function RadioOptionValue(name) {
    var output = "";

    var el = document.getElementsByName(name);

    if (el != undefined && el != null && el.length > 0) {
        for (var i = 0; i < el.length; i++) {
            if (el[i].checked) {
                output = el[i].value;
            }
        }
    }
```

> Proof: [`UserDefinedFields.razor.js:1-12`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L1-L12); same four-space indentation throughout, e.g. the nested callback ladder at [`Highcharts.razor.js:17-36`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L17-L36).

**Rule 4g — Comments use `//` and explain *why* / workarounds; dead code is commented out, not deleted.** Use `//` line comments to document non-obvious behavior, race conditions, and workarounds. There are no `/* */` block comments in hand-written code. Disabled or experimental code is left in place, commented out.

**Why:** the substantive comments capture real, hard-won knowledge — an AMD-loader conflict, an onload-vs-init race. Keeping dead code commented out is a house habit for preserving experiments (untidy, but real).

```javascript
    } else {
        // AMD Workaround: Monaco's vs/loader.js creates a global AMD environment.
        // Highcharts detects AMD and registers as a module instead of setting window.Highcharts.
        // We temporarily hide the AMD loader so Highcharts falls back to browser globals.
        var tempDefine = window.define;
        window.define = undefined;
```

> Proof: the workaround comment at [`Highcharts.razor.js:10-15`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L10-L15); a race-condition note at [`:40-41`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L40-L41) and [`:46`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L46). Commented-out dead code at [`MonacoEditor.razor.js:6-26`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L6-L26), and disabled debug logging like `//console.log("RenderChart_Column", ...)` at [`Highcharts.razor.js:118`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L118) and `//console.log("RenderChart_Pie", ...)` at [`:160`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L160). Live status `console.log` does appear (e.g. [`UploadFile.razor.js:3`](FreeCRM/CRM.Client/Shared/UploadFile.razor.js#L3)), but debug traces are left commented out.

**Rule 4h — Guard values with verbose loose `!= undefined && != null` checks.** Before using a value that might be missing, check it explicitly against both `undefined` and `null` (and often `""`) using loose `!=`/`==`, rather than a truthy/falsy shortcut or strict `!==`.

**Why:** defensive interop — values arriving from Blazor and the DOM can be `undefined` or `null`, and the verbose guard makes the intent explicit. It's the established house defensive-coding pattern.

```javascript
function LoadScriptResource(url, callback) {
    var head = document.getElementsByTagName('head')[0];
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = url;
    if (callback != undefined && callback != null) {
        script.onload = callback;
    }
    head.appendChild(script);
}
```

> Proof: [`Highcharts.razor.js:102-111`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L102-L111); same idiom at [`MonacoEditor.razor.js:3`](FreeCRM/CRM.Client/Shared/MonacoEditor.razor.js#L3) and [`UserDefinedFields.razor.js:6`](FreeCRM/CRM.Client/Shared/UserDefinedFields.razor.js#L6), with the `!= ""` variant at [`Highcharts.razor.js:55`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L55) and [`:70`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L70). Strict `===` appears only in Highcharts feature detection — `idx === 0` ([`:391`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L391)), `document.ontouchstart === undefined` ([`:225`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L225)) — and type checks use loose `==`: `typeof (Highcharts) == "object"` ([`:8`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L8), [`:43`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L43)).

**Rule 4i — Trailing commas are tolerated in Highcharts config objects, not a standard.** Inside Highcharts configuration objects, a trailing comma after the last property sometimes appears. This is inconsistency, not a deliberate rule — harmless in modern JS, but don't go out of your way to imitate it.

```javascript
        subtitle: {
            text: chartSubtitle,
            x: -20,
            useHTML: true,
        },
        xAxis: {
            categories: seriesCategories,
            crosshair: true,
        },
```

> Proof: trailing commas at [`Highcharts.razor.js:127-135`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L127-L135). The very similar subtitle block in `RenderChart_LineTimeSeries` omits the trailing comma (`useHTML: true` at [`:240`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L240)). Treat as tolerated noise.

**Rule 4j — DOM idioms: vanilla DOM by default; one jQuery exception.** Select elements with `document.querySelector` / `getElementsByName` / `getElementsByTagName`; build and inject `<link>`/`<script>` tags via `document.createElement(...)`, set properties directly, then `appendChild` to `head`. This is how the project loads the Highcharts library and its CSS at runtime, and how it reads form state for Blazor.

```javascript
        var head = document.getElementsByTagName('head')[0];
        var link = document.createElement("link");
        link.href = url;
        link.type = "text/css";
        link.rel = "stylesheet";
        if (callback != undefined && callback != null) {
            link.onload = callback;
        }
        head.appendChild(link);
```

> Proof: [`Highcharts.razor.js:90-98`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L90-L98) (parallel script-loader at [`:102-111`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L102-L111)). jQuery is used in exactly one spot — iterating existing `<script>` tags to detect already-loaded resources: `$("script").each(...)` at [`Highcharts.razor.js:72-73`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L72-L73). That is the only jQuery usage in any hand-written module; everything else is vanilla DOM.

**Rule 4k — Error handling is minimal: guard-then-skip plus, at most, a bounded poll with one `console.error`. No try/catch.** There is no `try`/`catch`/`finally` anywhere in hand-written modules. "Error handling" amounts to (a) the defensive null/undefined guards from Rule 4h, and (b) a bounded retry/poll that logs a single `console.error` if it ultimately fails.

**Why:** honest answer — the code is thin DOM/interop glue where failures are non-fatal and a guard is enough. The one genuine failure path (the Highcharts library never loads) is handled with a capped poll and an error log rather than throwing.

```javascript
function waitForHighcharts(callback, attempts = 0) {
    if (typeof (Highcharts) == "object") {
        callback();
    } else if (attempts < 50) {
        // Retry up to 50 times (5 seconds max with 100ms intervals)
        setTimeout(() => waitForHighcharts(callback, attempts + 1), 100);
    } else {
        console.error("Highcharts failed to load after multiple attempts");
    }
}
```

> Proof: [`Highcharts.razor.js:42-51`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L42-L51). This is the only `console.error` and the only retry/timeout in the codebase; the other three files have no error handling beyond the Rule 4h guards. Note `attempts = 0` here is the only default-parameter usage in hand-written code.

**Rule 4l — Newer chart helpers default arrays with `(arr || [])` and use `.map()`; older ones use index `for` loops. Both are live.** This codebase is mid-modernization. In the newer chart renderers (marked `// NEW:`), possibly-missing arrays are defaulted with `(arr || [])` before mapping, and data is reshaped with `.map()`/`.sort()` using classic `function` callbacks. The older `RenderChart_Pie` reshapes data with an index `for` loop instead. You will see both in the same file; match whichever style you are editing near.

```javascript
    var rootData = (rootItems || []).map(function (x) {
        return { name: x.name, y: Number(x.data), drilldown: x.name };
    });

    if (sortDescending) {
        rootData.sort(function (a, b) { return b.y - a.y; });
    }
```

> Proof: [`Highcharts.razor.js:287-293`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L287-L293); same `(... || []).map(...)` idiom at [`:296-302`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L296-L302) and [`:389-407`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L389-L407). Older index-`for` style at [`:164-166`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L164-L166). The `// NEW:` markers sit at [`:231`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L231), [`:284`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L284), [`:342`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L342), [`:386`](FreeCRM/CRM.Client/Shared/Highcharts.razor.js#L386).

---

<a id="quick-reference"></a>
## 5. Quick Reference

Side-by-side cheat sheet. The left column is wrong for this codebase; the right column matches the real source. All right-column forms are copied from or consistent with the four `.razor.js` modules.

**Module shape and interop**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| A `.ts` file + `tsconfig` + build step | Plain ES module, `<Component>.razor.js`, no build step |
| JS file far from its component | JS collocated beside its `.razor` (e.g. in `Shared/`) |
| `function doThing()` for a C#-called function | `export function DoThing()` (exported + PascalCase) |
| JS name ≠ C# call string | JS export name matches the `"DoThing"` string exactly |
| Importing on every render | Import once under `if (firstRender)` in `OnAfterRenderAsync` |
| Calling `jsModule` unguarded | `if (jsModule != null) { await jsModule.InvokeVoidAsync(...) }` |
| `InvokeVoidAsync` when you need a return value | `InvokeAsync<string>("Name", args)` for a return value |
| JS calling C# directly | Pass `DotNetObjectReference.Create(this)`, call `dotNetHelper.invokeMethodAsync("Method", arg)`, mark target `[JSInvokable]` |
| Trusting any `.razor.js` is wired up | Confirm the sibling `.razor` actually `import`s it |

**Code style inside the module**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `const`/`let` for locals | `var output = "";` |
| Arrow function for a top-level/exported function | Named `function` declaration; arrows only for inline callbacks |
| Arrow for a Highcharts handler that needs `this` | `click: function (event) { ... this.index ... }` |
| `camelCase` for an exported function | `PascalCase` for exported; `camelCase` locals/params |
| Omitting semicolons (relying on ASI) | `;` after every statement |
| Single quotes in your own DOM/console code | `"double quotes"` (single quotes only inside Highcharts configs) |
| Tabs / 2-space indent | 4-space indentation |
| `if (x)` truthy shortcut on interop values | `if (x != undefined && x != null)` |
| `try { } catch { }` for thin glue | Guard-then-skip; at most a capped poll + one `console.error` |
| `/* block comments */`; deleting experiments | `//` line comments explaining *why*; dead code left commented |

---

<a id="faq"></a>
## 6. FAQ

**Q: Why is there so little JavaScript?**
Because this is a Blazor WebAssembly app — the C# runs in the browser. The work you'd normally do in JavaScript (clicks, DOM, state, API calls) is all done in C#. Hand-written JavaScript appears only when C# physically can't reach a browser or third-party-library feature, which is rare. The whole hand-written surface is four small `.razor.js` files (469 lines, ~90% of it one charting module). See §1.

**Q: Why no TypeScript?**
It's a deliberate, reasonable choice for a surface this small. TypeScript needs a build/transpile step to produce browser-runnable `.js`, and its payoff — type checking and editor help — would apply to a few ~15-line DOM shims. The type safety that matters lives on the C# side at the interop boundary (`InvokeAsync<string>`, typed parameters). Adding a TS pipeline here would be pure overhead. There are zero `.ts` files and no `tsconfig.json` in `CRM.Client`. See §1.

**Q: When should I write JavaScript instead of doing it in C#?**
Almost never. Default to C#. Reach for a `.razor.js` module only when you must wrap a browser API or a JS-only library that C# can't call directly — exactly the three real cases: a charting library (Highcharts), a code editor (Monaco), and reading the checked value out of a raw HTML radio group. If C# can do it, do it in C#.

**Q: Where does a new interop module go, and how do I name it?**
Right next to the component that uses it, named `<Component>.razor.js`. So `MyWidget.razor` pairs with `MyWidget.razor.js` in the same folder. The framework serves it as a scoped static web asset the component imports by relative path. See §2a.

**Q: How does C# call my JS function?**
First the component imports the module once on first render and stores the handle: `jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./Shared/MyWidget.razor.js?v=...")`. Then it calls your exported function by name: `await jsModule.InvokeVoidAsync("DoThing", arg)` if it returns nothing, or `await jsModule.InvokeAsync<string>("DoThing", arg)` if you need its return value. Always `if (jsModule != null)` first. Your JS function must be an `export function` whose name matches that string exactly. See §3a–3b.

**Q: How does my JS call back into C#?**
The component passes a callback handle in: it creates `DotNetObjectReference.Create(this)` and hands it to your module via an exported setter (the convention is `SetDotNetHelper`). Your module stores it in a module-level `var dotNetHelper;` and calls back with `dotNetHelper.invokeMethodAsync("MethodName", args)`. The C# method must be marked `[JSInvokable]`. Only `Highcharts` does this today (chart clicks). See §3c.

**Q: `const` or `let`?**
Neither — use `var`. Every line of live hand-written code here uses `var`; `let` appears nowhere, and `const` shows up only in commented-out dead code. It's a house habit rather than a best practice, but matching it keeps these files internally consistent. See §4a.

**Q: Should I use an arrow function?**
Only for short inline callbacks. Top-level and exported functions are named `function` declarations. And never use an arrow for a Highcharts event/formatter handler — those need `this` to point at the chart/point context, which arrow functions don't provide, so they're written as classic `function () { ... }`. See §4b.

**Q: Double or single quotes?**
Double quotes for all your own code (DOM, console, URLs, interop strings). Single quotes only inside Highcharts configuration objects, where they mirror Highcharts' own docs. See §4e.

**Q: Do I need a try/catch?**
No. There's no exception handling anywhere in these modules. Error handling here is the verbose `!= undefined && != null` guards, plus at most a bounded poll that logs one `console.error` if a library never loads. Thin glue is allowed to fail quietly. See §4h, §4k.

**Q: Should I dispose the JS module reference in the component?**
The existing code doesn't — none of the three live components dispose their `jsModule` `IJSObjectReference`; they dispose only the `DotNetObjectReference` (Highcharts) or a timer (Monaco). So don't assume a disposal call already exists when editing. Do dispose the `DotNetObjectReference` (`dotNetHelper?.Dispose()`) in `Dispose()` if you add a callback. See §3d.

**Q: I found a `.razor.js` file — can I assume it's used?**
No. `UploadFile.razor.js` exports a function nothing imports; it's orphaned dead code, and the real feature uses a C# helper instead. Before trusting or editing any `.razor.js`, confirm the sibling `.razor` actually does `InvokeAsync<IJSObjectReference>("import", ...)` for it. See §2d.

**Q: Should I "fix" the inconsistencies I see (missing cache-buster, `.Razor.js` casing, `protected` vs `private`)?**
Not as a drive-by. The `?v=` cache-buster is used in 2 of 3 imports, the `.Razor.js`/`.razor.js` casing varies, and the `jsModule` field is `protected` in two files and `private` in one. None of these carry meaning. Prefer the safe forms in new code (include `?v=`, match the lowercase on-disk filename), but don't churn existing files chasing consistency. See §3a, §3d.

---

<a id="related-docs"></a>
## 7. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the C# braces/casing/empty-string/field-naming rules; this doc is the JavaScript counterpart in the same band.
- [055 — The C# Style Reference](055_csharp-style-reference.md) — the C# language conventions that the interop call-site code in §3 follows.
- [056 — The Razor / Blazor / HTML Style Reference](056_razor-blazor-style-reference.md) — the `.razor` component conventions; every `.razor.js` module here is paired with a `.razor` component.
- [057 — The CSS Style Reference](057_css-style-reference.md) — styling conventions; note `Highcharts.razor.js` loads `highcharts.css` at runtime and uses Highcharts' `styledMode`.
- [033 — Charts, Code Editors, Rich Text, and PDFs](033_rich-components.md) — the rich components (Monaco, Highcharts) whose interop modules this doc documents.
- [047 — Growing the Shared Library](047_custom-components.md) — authoring new shared components, where a new interop module might originate.

---
*GuidesV2 058 · The JavaScript Style Reference · drafted 2026-06-05 from verified source evidence over the four hand-written interop modules (`FreeCRM/CRM.Client/Shared/Highcharts.razor.js`, `MonacoEditor.razor.js`, `UserDefinedFields.razor.js`, `UploadFile.razor.js`) and their paired `.razor` components (`Highcharts.razor`, `MonacoEditor.razor`, `UserDefinedFields.razor`, `GeneratePasswordDialog.razor`). Every rule is citation-verified; no TypeScript exists in `CRM.Client`.*
