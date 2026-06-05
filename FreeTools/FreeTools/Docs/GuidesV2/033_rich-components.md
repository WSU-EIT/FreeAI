# 033 — Charts, Code Editors, Rich Text, and PDFs

> **Document ID:** 033  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Guide use of the rich components: charts, code editor, rich-text editor, and PDF viewer.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 03x (Core Craft: Everyday Screens and Components) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Rich Components Matter](#why-it-matters) | Plain-language overview and key terms defined |
| 2 | [Charts: Visualizing Data](#charts) | The `Highcharts` component — chart types, data shapes, when to use |
| 3 | [Code Editor: In-App Editing](#code-editor) | The `MonacoEditor` component — language, read/write, diff mode |
| 4 | [Rich-Text Editor](#signatures) | The `HtmlEditor2` component and `HtmlEditorDialog` — inline and pop-up WYSIWYG |
| 5 | [PDF Viewer](#network-graphs) | The `PDF_Viewer` dialog — opening, downloading, where files come from |
| 6 | [Performance and Data Volume](#performance) | Loading heavyweight JS once and keeping big views responsive |
| 7 | [Common Pitfalls and Recipes](#pitfalls) | Frequent mistakes and ready-made solutions per component |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Rich Components Matter

**Why it matters:** Most of the shared shelf (covered in [032](032_shared-components.md)) is lightweight — buttons, indicators, small dialogs. A handful of components are different: they wrap a large third-party JavaScript library, load extra files into the browser, and bridge between C# and JavaScript. These are the "heavyweight" components. They give you capabilities you would never want to build by hand — interactive charts, a full code editor, a word-processor-style text box, a PDF reader — but each one comes with rules about how it loads and how data flows in and out. This doc explains the four that ship in `CRM.Client/Shared` so you can drop them into a feature without surprises.

**The four components, in plain language:**

- **A chart** is a picture of numbers — bars, pie slices, lines over time. The `Highcharts.razor` component renders these using the Highcharts JavaScript library. Reach for it whenever a table of numbers would be clearer as a shape.
- **A code editor** is the same kind of text box you are reading this in if you use VS Code — line numbers, syntax coloring, a tiny overview map on the right. The `MonacoEditor.razor` component embeds Monaco (the editor engine that powers VS Code). Reach for it when users edit code, HTML, SQL, JSON, or any structured text, or when you want to show two versions side by side.
- **A rich-text editor** is a "what you see is what you get" (WYSIWYG — pronounced "wizzy-wig") box where bold looks bold and you never see raw HTML tags. The `HtmlEditor2` component (from the `FreeBlazor` package) provides this, and `HtmlEditorDialog.razor` wraps it in a pop-up. Reach for it when non-technical users compose formatted content like email templates.
- **A PDF viewer** displays a PDF document inside the app instead of forcing a download. The `PDF_Viewer.razor` dialog does this. Reach for it to preview generated documents or uploaded files.

**The one term to learn first — JS interop.** "JS interop" (JavaScript interoperability) is how C# code running in the browser talks to JavaScript code, and vice versa. Blazor — the framework these apps are built on — runs your C# in the browser, but libraries like Highcharts and Monaco are written in JavaScript. The bridge is an injected service called `IJSRuntime`. You will see it at the top of every heavyweight component:

```razor
@inject Microsoft.JSInterop.IJSRuntime jsRuntime
```

You rarely call it yourself — the components do — but knowing it exists explains why these components behave a little differently from pure-C# ones (for example, why they only fully initialize *after* the page first renders).

---

<a id="charts"></a>
## 2. Charts: Visualizing Data

**Why it matters:** A column of revenue numbers is hard to read; a bar chart of the same numbers is instant. `Highcharts.razor` turns C# data into interactive charts (hover tooltips, click-to-drill, zoom) without you writing any JavaScript.

**The mental model.** You give the component three things: an **element id** (a unique string so the chart knows which spot on the page to draw into), a **chart type**, and the **data** in the shape that type expects. The component loads the Highcharts library the first time, then redraws whenever your data changes.

**Chart types** come from an enum right inside the component, so the compiler stops typos:

```csharp
public enum ChartTypes
{
    Column,
    Pie,
    LineTimeSeries,
    PieWithDrilldown,
    ColumnTimeSeries,
    AreaTotals
}
```

**Each type wants a different data shape.** This is the part people get wrong, so match them carefully. The component exposes small classes for each:

- **Column** (categories with bars) — set `SeriesCategories` (the labels along the bottom) plus `SeriesDataArrayItems`, where each series is a name and an array of numbers:
  ```csharp
  public class SeriesDataArray {
      public string name { get; set; } = String.Empty;
      public decimal[] data { get; set; } = [];
  }
  ```
- **Pie** — set `SeriesDataItems`, where each slice is a name and a single value:
  ```csharp
  public class SeriesData {
      public string name { get; set; } = String.Empty;
      public decimal data { get; set; }
      public string tooltip { get; set; } = String.Empty;
  }
  ```
- **LineTimeSeries / ColumnTimeSeries / AreaTotals** (values over time) — set `SeriesDataXYArrayItems`, where each point is an `[epochMs, value]` pair. "Epoch ms" means the time expressed as milliseconds since 1 Jan 1970 — the standard way JavaScript handles dates:
  ```csharp
  public class SeriesDataXYArray {
      public string name { get; set; } = String.Empty;
      public long[][] data { get; set; } = [];
  }
  ```
- **PieWithDrilldown** — a pie whose slices can be clicked to reveal a child pie. Set `PieRootItems` (the parent slices) and `PieDrilldownItems`, where each drilldown's `id` must match a root slice's `name`.

**A minimal pie example:**

```razor
<Highcharts ElementId="revenue-by-region"
            ChartType="Highcharts.ChartTypes.Pie"
            ChartTitle="Revenue by Region"
            SeriesDataItems="@_pieData" />
```

```csharp
Highcharts.SeriesData[] _pieData = new[] {
    new Highcharts.SeriesData { name = "North", data = 1200 },
    new Highcharts.SeriesData { name = "South", data = 900 },
};
```

**Reacting to clicks.** Pass a delegate to `OnItemClicked` and the component calls it with the index of the clicked item (the JavaScript side calls back into C# via the `[JSInvokable]` method `ChartItemClicked`). This is how you wire "click a bar to filter the grid below."

**Two parameters worth knowing:**

- `PreloadResources` — set this `true` on a hidden `<Highcharts PreloadResources="true" />` to load the Highcharts library early, before any real chart renders. Useful when a page has several charts and you want the first one to appear instantly instead of waiting on the download.
- `ShowZoomHint` — defaults to `true`; shows the "drag to zoom" hint on the time-series charts.

**Important loading note.** The chart will not draw until `ElementId` and `ChartType` are set. If either is missing, the component quietly logs `Missing the Required Parameter ElementId` (or `ChartType`) to the browser console and does nothing — so if a chart is blank, check the console first.

---

<a id="code-editor"></a>
## 3. Code Editor: In-App Editing

**Why it matters:** When users edit anything structured — HTML, SQL, JSON, a script — a plain `<textarea>` is painful. `MonacoEditor.razor` gives them the real VS Code editing experience (syntax colors, line numbers, word wrap, an overview minimap) inside your page, and hands the text back to your C# code.

**The mental model.** You bind a string to the editor with `@bind-Value`, choose a `Language` for syntax coloring, and read or write the text with helper methods. Under the hood it wraps the `BlazorMonaco` package (version 3.4.0) and its `StandaloneCodeEditor`.

**Languages are typo-proof.** Rather than passing a raw string like `"csharp"`, use the constants in the nested `MonacoLanguage` class — there are dozens (`html`, `json`, `sql`, `csharp`, `javascript`, `markdown`, `xml`, `yaml`, `python`, and many more). The default is `plaintext`.

**A real example, copied from the in-repo test page** (`Pages/TestPages/Monaco.razor`):

```razor
<MonacoEditor @ref="_monacoEditor"
        Id="my-monaco-editor"
        Language="@MonacoEditor.MonacoLanguage.html"
        PlaceholderText="Enter Your HTML Here"
        ReadOnly="@_monacoReadOnly"
        ValueToDiff="@_monacoContentToDiff"
        @bind-Value="_monacoContent" />
```

**What the key parameters do:**

- `@bind-Value` — two-way binding to your string. Note: the editor does not update your value on every keystroke. It uses a debounce **`Timeout`** (default 1000 ms) — a short pause after typing stops — so your `Value` and `ValueChanged` only fire once typing settles. This keeps a large document from re-rendering on every character.
- `Language` — syntax coloring (use a `MonacoLanguage` constant).
- `ReadOnly` — set `true` to show but not allow edits; the component updates the live editor when this flag changes.
- `PlaceholderText` — grey hint text shown when empty.
- `Id` — a stable element id; if you leave it blank a random one is generated.
- `Class` / `MinHeight` — sizing. `Class` defaults to `default-editor`; `MinHeight` defaults to `300px`.

**Diff mode — showing two versions side by side.** Set `ValueToDiff` to the "original" text and `Value` to the "modified" text. When `ValueToDiff` is non-empty, the component swaps to a `StandaloneDiffEditor` and renders the classic red/green comparison. Leave `ValueToDiff` empty for a normal single editor. The example above passes `ValueToDiff` so the test page can toggle into diff mode.

**Useful methods on the component** (call them through your `@ref`):

- `GetValue()` / `SetValue(text)` — read or replace the whole document.
- `InsertValue(text)` — insert at the cursor; it even re-indents multi-line inserts to match the current line.
- `Focus()` — put the cursor in the editor.
- `GetEditorSelection()` / `EditorCursorPosition` — find what the user has selected or where the cursor is.

**Accessibility is on by default.** The component sets an `AriaLabel` ("Monaco Code Editor" unless you override it) and accessibility options so screen readers work — you do not need to add anything.

---

<a id="signatures"></a>
## 4. Rich-Text Editor

> **Note on scope:** This section covers the real heavyweight text component in the source — the WYSIWYG **rich-text (HTML) editor**. There is no separate signature-capture component in the codebase; "signature" appears only as a FontAwesome icon name. The rich-text editor is the component this slot describes.

**Why it matters:** Non-technical users should never see raw HTML tags. When someone writes an email template or a formatted note, they want a toolbar with **Bold**, lists, and links — like a word processor. `HtmlEditor2` (from the `FreeBlazor` package, version 2.0.6) provides exactly that, and the app wraps it two ways: **inline** on a page, or in a **pop-up dialog** via `HtmlEditorDialog.razor`.

**Inline usage.** Drop `HtmlEditor2` straight onto a page, give it a `Config` and an `OnUpdate` callback. From the in-repo test page (`Pages/TestPages/HtmlEditor.razor`):

```razor
<HtmlEditor2 @ref="_editor"
        Config="_config"
        OnUpdate="InlineEditorUpdated" />
```

The `Configuration` object controls the editor's behavior. A real config from that page:

```csharp
return new HtmlEditor2.Configuration {
    BeautifyOutput = true,                 // tidy the generated HTML
    DarkMode = Helpers.StringLower(Model.User.UserPreferences.Theme) == "dark",
    SimpleView = _simpleView,              // a reduced toolbar
    UseMonacoForSourceView = true,         // use Monaco when the user views raw HTML
};
```

Notice `UseMonacoForSourceView` — when the user switches to "see the HTML source," the editor uses the same Monaco editor from Section 3. The two heavyweight components cooperate.

**Pop-up usage — the easy path.** For most features you do not place the dialog yourself. You call the static helper `Helpers.HtmlEditor(...)`, which opens `HtmlEditorDialog` for you and calls your delegate with the final HTML when the user clicks OK:

```csharp
Delegate onEditComplete = async (string? html) => {
    _html = html ?? "";
    await _editor.SetHTML(_html);
    StateHasChanged();
};

await Helpers.HtmlEditor(onEditComplete, _html, "Edit HTML", new HtmlEditor2.Configuration {
    UseMonacoForSourceView = true,
});
```

Behind the scenes, `Helpers.HtmlEditor` packages your callback and config into dialog parameters and calls Radzen's `DialogService.OpenAsync<HtmlEditorDialog>(...)` with sensible defaults (95% width, near-full-screen height, top offset of 80px). The dialog itself is small: a Cancel/OK button bar, optional instructions, and the `HtmlEditor2`. On OK it reads the HTML with `await _htmlEditor.GetHTML()` and hands it to your `OnEditCompleted` delegate.

**A gotcha worth flagging — source-view mode.** The component carries an `OnModeChanged` callback for a reason. When the user is in "Source" (raw HTML) view, the editor does **not** push its HTML back to the bound value until they switch back to the visual view. If you save while the user is still in source view, you can capture stale HTML. If your feature has its own Save button, listen to `OnModeChanged` and block saving until the editor is back in WYSIWYG mode. The dialog avoids this because its OK button reads the HTML explicitly via `GetHTML()`.

---

<a id="network-graphs"></a>
## 5. PDF Viewer

> **Note on scope:** This section covers the real heavyweight viewing component in the source — the **PDF viewer**. There is no network-graph (nodes-and-edges) component in the codebase. The PDF viewer is the component this slot describes.

**Why it matters:** When the app generates an invoice or a user uploads a contract, you want to *show* it, not force a download to disk. `PDF_Viewer.razor` opens a PDF in a dialog right inside the app, with an optional download button.

**The mental model.** `PDF_Viewer` is a dialog. It renders `BlazorBootstrap.PdfViewer` (from the BlazorBootstrap package, which does the actual rendering) pointed at a URL, plus a Close button and — when allowed — a Download button. You almost never place it yourself; you call the static helper `Helpers.PdfViewer(...)`.

**The component itself is small:**

```razor
<div class="mt-2 mb-2">
    <button type="button" class="btn btn-dark" @onclick="Close">
        <Language Tag="Close" IncludeIcon="true" />
    </button>
    @if (AllowDownload && FileId.HasValue) {
        <button type="button" class="btn btn-primary" @onclick="Download">
            <Language Tag="DownloadPDF" IncludeIcon="true" />
        </button>
    }
</div>
<BlazorBootstrap.PdfViewer Url="@PdfFile" />
```

**Opening it — the easy path.** Call `Helpers.PdfViewer(fileId)` with the `Guid` of a stored file:

```csharp
await Helpers.PdfViewer(fileId, AllowDownload: true);
```

The helper builds the file URL for you, including the auth token, in the form
`{ApplicationUrl}File/View/{FileId}?TenantId={...}&Token={...}` — that is how the viewer reads a tenant-scoped, authenticated file. It then opens the dialog through Radzen's `DialogService` at 95% width and near-full-screen height, matching the HTML-editor dialog.

**Download honors permissions.** The `AllowDownload` flag controls whether the Download button appears. In the generic file-open path (`Helpers.OpenFile` → `PdfViewer`), downloading is gated to admins:

```csharp
await PdfViewer(file.FileId, "", "", "", Model.User.Admin);
```

When the Download button is clicked, the component calls `Helpers.DownloadFile(fileId)` to stream the original file to the browser.

**Where it fits.** `Helpers.OpenFile` inspects a file and routes images to an image viewer, text to a text viewer, and PDFs here — so in most features you call `OpenFile` and the PDF viewer appears automatically for PDF content. Reach for `Helpers.PdfViewer` directly only when you already know the file is a PDF.

---

<a id="performance"></a>
## 6. Performance and Data Volume

**Why it matters:** These four components each pull a large JavaScript library into the browser. Loaded carelessly, a page can stutter or show a blank chart for a second. The good news: the components are already built to load efficiently — your job is mostly to not fight them.

**1. Heavy JS loads once, lazily.** Each component imports its JavaScript module in `OnAfterRenderAsync(firstRender)` — that is, only after the page has rendered once, and only on the first render. Highcharts goes further: its JS checks whether the library is already present (`typeof (Highcharts) == "object"`) before loading anything, so a second chart on the same page reuses the first load instead of downloading again.

**2. Preload before a chart-heavy page.** If a dashboard has several charts, render one hidden `<Highcharts PreloadResources="true" />` first. With `PreloadResources` set, the component loads the library and then returns early without trying to draw — so the real charts that follow render immediately. This trades a tiny upfront cost for a snappier first paint.

**3. The code editor debounces typing.** `MonacoEditor` only pushes its value back to C# after the `Timeout` (default 1000 ms) of no typing. On a large document this is what keeps the page from re-rendering on every keystroke. Do not lower `Timeout` to a tiny value on big documents, and avoid forcing your own `StateHasChanged()` on every change.

**4. Send charts only the data they need.** Highcharts re-renders whenever its serialized data changes (the component compares the serialized series to detect changes). Pre-aggregate on the server — send monthly totals, not every raw row. A time-series with tens of thousands of points will feel slow; bucket it first.

**5. Let dialogs unload heavy views.** The PDF viewer and HTML-editor dialog live only while open. Closing the dialog (Radzen's `DialogService.Close()`) tears down the heavyweight view and frees its memory. Prefer the dialog helpers (`Helpers.PdfViewer`, `Helpers.HtmlEditor`) over keeping these components mounted permanently on a page.

**6. Watch the Monaco + Highcharts interaction.** There is a real, documented quirk: Monaco's loader creates a global "AMD" module environment that confuses Highcharts into not attaching itself to the page the normal way. The Highcharts JS works around this by temporarily hiding the loader while it loads, then restoring it. You do not need to do anything — but if you ever see Highcharts fail to load *only* on pages that also use Monaco, this is the cause, and the fix already lives in `Highcharts.razor.js`.

---

<a id="pitfalls"></a>
## 7. Common Pitfalls and Recipes

**Blank chart, no error dialog → check the browser console.** When a required parameter is missing, `Highcharts.razor` does not throw; it writes a message like `Missing Required Parameter SeriesDataItems` to the console and renders nothing. Open the browser dev tools console first. The usual cause is a data shape mismatch — e.g., passing `SeriesDataItems` (pie) to a `Column` chart, which wants `SeriesCategories` + `SeriesDataArrayItems`.

**Wrong data class for the chart type.** Recipe: match the type to its class — `Column` → `SeriesDataArray[]` (+ `SeriesCategories`), `Pie` → `SeriesData[]`, the time-series types → `SeriesDataXYArray[]` (pairs of `[epochMs, value]`), `PieWithDrilldown` → `PieRootItems` + `PieDrilldownItems` whose `id` matches a root `name`.

**Editor value looks "one keystroke behind."** This is the debounce, not a bug. `MonacoEditor` only updates after `Timeout` ms of no typing. If you need the very latest text at a specific moment (e.g., a Save button), call `await editor.GetValue()` directly instead of relying on the bound `Value`.

**Saving stale HTML from the rich-text editor.** If a user is in "Source" view, `HtmlEditor2` has not pushed its HTML back yet. Recipe: subscribe to `OnModeChanged` and disable Save until the editor is back in WYSIWYG, or read the HTML explicitly with `await _editor.GetHTML()` at save time (which is exactly what `HtmlEditorDialog` does on OK).

**Forgetting `ElementId` on a chart.** It is required and must be unique on the page. Two charts sharing an id will collide. Recipe: derive ids from a stable key, e.g. `ElementId="@($"chart-{item.Id}")"`.

**Placing the PDF or HTML dialog by hand.** You rarely need to. Recipe: call `Helpers.PdfViewer(fileId)` or `Helpers.HtmlEditor(callback, html, title, config)` — the helpers build URLs/parameters and size the dialog for you. Reach for the raw components only when the helpers genuinely do not fit.

**Download button missing on a PDF.** The button only shows when `AllowDownload` is `true` **and** a `FileId` is supplied. In the generic file path, download is admin-gated (`Model.User.Admin`). If a non-admin reports a missing button, that is by design.

**Diff editor not showing the comparison.** `MonacoEditor` only switches to diff mode when `ValueToDiff` is non-empty. Recipe: set `ValueToDiff` to the original text and `Value` to the new text; leave `ValueToDiff` empty/null for a normal single editor.

**A chart flickers or reloads on every render.** Highcharts re-renders when its serialized data changes. If you rebuild the data array on every render (e.g., `new[] { ... }` inside the markup), it always looks "changed." Recipe: hold the data in a field and only rebuild it when the underlying numbers actually change.

---

<a id="related-docs"></a>
## 8. Related Docs

- [032 — Building From the Shared Component Shelf](032_shared-components.md) — the lighter shared shelf
- [034 — Leading Users Through a Multi-Step Wizard](034_multistep-wizard.md) — the multi-step wizard

---
*GuidesV2 · 033 · drafted 2026-06-05 from FreeCRM source (`CRM.Client/Shared`: Highcharts.razor, MonacoEditor.razor, HtmlEditorDialog.razor, PDF_Viewer.razor).*
