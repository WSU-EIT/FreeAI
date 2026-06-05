# 035 — Validated, Translated, and Reachable

> **Document ID:** 035  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Cover form validation, tenant-aware localization, and accessibility as the baseline quality every screen must meet.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 03x (Core Craft: Everyday Screens and Components) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Baseline Matters](#why-it-matters) | The three pillars and why each is non-negotiable |
| 2 | [Form Validation Fundamentals](#validation) | `MissingValue`, `MissingRequiredField`, the error list, and focus |
| 3 | [Tenant-Aware Localization](#localization) | `Helpers.Text`, the `<Language>` tag, and per-tenant phrase overrides |
| 4 | [Accessibility Essentials](#accessibility) | Labels, `aria-label`, the WAVE baseline, and Quill |
| 5 | [Wiring the Three Together](#integration) | One field that is required, translated, and reachable |
| 6 | [Common Pitfalls and Anti-Patterns](#pitfalls) | Hardcoded strings, client-only checks, missing labels |
| 7 | [Checklist Before You Ship](#checklist) | A pass/fail gate covering all three pillars |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why This Baseline Matters

Three things make a screen feel finished rather than half-built. They are easy to skip because the screen still "works" without them — and that is exactly why they get skipped, and exactly why this doc exists. They are the floor, not the ceiling: every screen in the app is expected to clear all three.

**Validation** means checking that the data a user typed is good *before* you try to save it. Why it matters: without it, users hit cryptic database errors, or worse, save garbage that breaks a report three weeks later. Good validation catches the problem at the keyboard, points at the exact field, and tells the user in plain language what to fix.

**Localization** (often shortened to "l10n") means showing every piece of text in the user's language, and — critically here — letting each customer reword it. In this app a customer is a **tenant** (one isolated account with its own users and data). Why it matters: the same screen ships to many tenants, and one tenant calls them "Members" while another calls them "Clients." Hardcoding the word "User" into the page makes it wrong for half your customers. So no human-readable string is ever typed directly into a page; every label goes through a lookup that a tenant can override.

**Accessibility** (shortened to "a11y") means the screen is usable by people who don't use a mouse or a screen the way you do — keyboard-only users, and people using a **screen reader** (software that reads the page aloud for blind users). Why it matters: it's a legal requirement in many markets (ADA in the US), it's the right thing to do, and the fixes are cheap if you do them as you build and expensive if you bolt them on later. The app has already been tested with the **WAVE** browser tool (an automated accessibility scanner), so the bar is "don't regress what already passes."

The rest of this doc shows the *actual* helpers and components the codebase uses for each pillar, so you can copy the real pattern instead of inventing your own.

---

<a id="validation"></a>
## 2. Form Validation Fundamentals

Validation here happens in two places, and you need both.

**Client-side** validation runs in the browser, instantly, as the user types or clicks Save. It's for fast feedback: it turns a field red and tells the user "this is required." It is *not* a security boundary — a determined user can bypass anything that runs in their own browser.

**Server-side** validation runs in the API after the data is sent. It's the real gatekeeper. Even if every client check is bypassed, the server re-checks before touching the database, and it returns its answer as the standard pass/fail result (see [026](026_standard-result.md)). Rule of thumb: **the client check is a courtesy; the server check is the law.** This doc focuses on the client-side helpers, because that is where the three pillars meet on screen.

### 2.1 Marking a field red: `Helpers.MissingValue`

`MissingValue` is a helper that returns a CSS class. You bind it to a field's `class` attribute, and it adds a "you missed this" class whenever the value is empty. There is one overload per value type so you can pass a string, an `int?`, a `decimal?`, a `Guid?`, a `DateTime?`, or an `object[]`. Here is the string overload exactly as it lives in `Helpers.cs`:

```csharp
public static string MissingValue(string? value, string? defaultClass = "")
{
    if (String.IsNullOrWhiteSpace(defaultClass)) {
        return String.IsNullOrWhiteSpace(value) ? MissingValueClass : "";
    } else {
        return String.IsNullOrWhiteSpace(value) ? MissingValueClass + " " + defaultClass : defaultClass;
    }
}
```

The class it adds is `MissingValueClass`, which is simply `"m-r"` (short for "missing — required"):

```csharp
public static string MissingValueClass {
    get {
        return "m-r";
    }
}
```

The numeric overloads treat zero as missing — an `int?` or `decimal?` only counts as "present" when `value.HasValue && value.Value > 0`. The `Guid?` overload treats `Guid.Empty` as missing. Knowing this matters: a quantity of `0` will flag as missing by design.

In a page you bind it to the `class`, passing your normal class (usually Bootstrap's `"form-control"`) as the default. From the real invoice editor:

```razor
<input type="text" id="edit-invoice-Title"
       class="@Helpers.MissingValue(_invoice.Title, "form-control")"
       @bind="_invoice.Title" @bind:event="oninput" />
```

When `_invoice.Title` is blank, the input gets `m-r form-control`; once the user types, it falls back to just `form-control`. The `m-r` class is styled in `site.css` to paint the field with a `palevioletred` background and `darkred` border, so the empty field is visibly flagged. The `@bind:event="oninput"` part means the model updates on every keystroke, so the red clears the instant the user starts typing.

### 2.2 The submit-time check: `Helpers.MissingRequiredField`

Colouring a field red is the live hint. When the user actually clicks Save, you also build a list of human-readable error messages. `MissingRequiredField` produces one such message for a named field:

```csharp
public static string MissingRequiredField(string fieldName)
{
    string output = Text("RequiredMissing", false, new List<string> { fieldName });
    return output;
}
```

Notice it does *not* return a hardcoded English sentence. It calls `Text(...)` with the phrase tag `"RequiredMissing"` and passes the field name as a replacement value. The default phrase for `RequiredMissing` is `"{0} is Required"`, so the field name is substituted into `{0}`. That field name is *itself* a phrase tag, so the whole message is translatable end to end — this is the bridge to Section 3.

### 2.3 The submit handler shape

Every edit screen follows the same submit pattern: build an `errors` list, remember the first bad field so you can move the cursor there, and bail out before calling the API if anything is wrong. From the real `ChangePassword.razor`:

```csharp
List<string> errors = new List<string>();
string focus = String.Empty;

if (String.IsNullOrWhiteSpace(_passwordReset.CurrentPassword)) {
    errors.Add(Helpers.MissingRequiredField("CurrentPassword"));
    if (focus == String.Empty) { focus = "change-password-currentPassword"; }
}
// ... more field checks ...

if (errors.Any()) {
    Model.ErrorMessages(errors, false);
    if (focus != String.Empty) {
        await Helpers.DelayedFocus(focus);
    }
    return;
}
```

Three things worth calling out for the reader new to this codebase:

- `Model.ErrorMessages(errors, false)` shows all the collected messages to the user at once (the second argument controls auto-dismiss behaviour). Collecting them into a list and showing them together is friendlier than stopping at the first error.
- `Helpers.DelayedFocus("...")` moves the keyboard cursor to the first offending field by its `id`. That single line is doing double duty: it's a usability nicety *and* an accessibility feature, because a keyboard or screen-reader user is taken straight to the field they need to fix.
- The handler `return`s before any API call. The server still re-validates, but the user never pays a round-trip for an obviously empty form.

---

<a id="localization"></a>
## 3. Tenant-Aware Localization

The golden rule: **no human-readable string is ever typed directly into a page.** Every label, button, message, and validation error is a short *tag* that gets looked up at render time and can be reworded per tenant. The two ways you do that lookup are `Helpers.Text(...)` in C# and the `<Language>` component in markup.

### 3.1 The data shape: phrases per tenant per culture

A language is just a tenant, a culture code, and a list of phrases. From `DataObjects.cs`:

```csharp
public partial class Language
{
    public Guid TenantId { get; set; }
    public string Culture { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public List<DataObjects.OptionPair> Phrases { get; set; } = new List<OptionPair>();
}
```

A **phrase** is an `OptionPair` of `Id` (the tag, e.g. `"InvoiceTitle"`) and `Value` (the displayed text, e.g. `"Invoice Title"`). The shipped defaults live in `DataAccess.Language.cs` as a big dictionary — for example:

```csharp
{ "RequiredMissing", "{0} is Required" },
{ "IndicatesRequiredField", "Indicates a Required Field" },
{ "InvoiceTitle", ... },
```

A tenant can save their own `Language` record (one per culture) whose phrases override these defaults. That override is stored as a tenant Setting and edited from the Settings → Languages screen.

### 3.2 The lookup: `Helpers.Text`

`Helpers.Text` is the single function every string passes through. Its signature:

```csharp
public static string Text
(
    string? text,
    bool ReplaceSpaces = false,
    List<string>? ReplaceValues = null,
    bool MarkUndefinedStrings = true,
    TextCase textCase = TextCase.Normal
)
```

The lookup order is the important part, because it *is* the "tenant-aware" behaviour:

1. **Tenant/current language first.** It searches `Model.Language.Phrases` for a phrase whose `Id` matches your tag (case-insensitively). `Model.Language` is the language loaded for the current tenant and culture, so a tenant's override wins.
2. **Default language fallback.** If the tenant hasn't overridden that tag, it searches `Model.DefaultLanguage.Phrases` — the shipped defaults.
3. **Last-resort humanizing.** If the tag is found in neither list, the raw tag is "humanized." When `MarkUndefinedStrings` is `true` (the default) it is forced to ALL CAPS, so a tag you forgot to define screams at you on screen (`SOMENEWLABEL`) instead of hiding. With it `false`, it's just Title-Cased.

The `ReplaceValues` list fills in `{0}`, `{1}`, and so on — and each replacement value is itself run through `Text`, which is why `MissingRequiredField("CurrentPassword")` translates both the sentence template *and* the field name.

### 3.3 The markup component: `<Language>`

In Razor markup you almost never call `Helpers.Text` directly for a label — you use the `<Language>` component, which wraps it and adds rendering niceties. The required parameter is `Tag`; useful extras include `Required`, `TransformCase`, `IncludeIcon`, and `ReplaceSpaces`. A real label:

```razor
<label for="edit-invoice-Title">
    <Language Tag="InvoiceTitle" Required="true" />
</label>
```

`Required="true"` appends a `<span class='required-flag'></span>`, which CSS renders as a red asterisk. So you get a translated label *and* the standard required marker from one tag. Internally the component calls the same helper: `Helpers.Text(Tag.Trim(), false, null, MarkUndefinedStrings)`.

When you need the translated string inside an attribute (like `aria-label`) rather than as visible content, call `Helpers.Text` directly — see Section 4.

### 3.4 Culture, dates, and numbers

Because the language record carries a `Culture` (e.g. `en-US`), text and culture travel together per tenant. Format dates and numbers through the framework's culture-aware helpers rather than hand-building strings like `month + "/" + day`, so a tenant on a `dd/MM/yyyy` culture sees their format and not yours. The practical rule mirrors the text rule: **don't hardcode a format any more than you'd hardcode a word.**

---

<a id="accessibility"></a>
## 4. Accessibility Essentials

Accessibility is mostly about giving every interactive thing a name that assistive technology can announce, and making sure nothing requires a mouse. The good news: the app already passes the **WAVE** automated scan, so your job is to match the existing patterns and not introduce new gaps.

### 4.1 Every input has a real label

A `<label for="...">` tied to an input's `id` is the single highest-value accessibility feature. A screen reader announces the label when focus lands on the field, and clicking the label focuses the field. The standard pattern pairs the `for`/`id` with a `<Language>` tag so the label is both associated and translated:

```razor
<label for="edit-invoice-Title">
    <Language Tag="InvoiceTitle" Required="true" />
</label>
<input type="text" id="edit-invoice-Title"
       class="@Helpers.MissingValue(_invoice.Title, "form-control")"
       @bind="_invoice.Title" @bind:event="oninput" />
```

### 4.2 When there's no visible label, use `aria-label`

In dense grids (like invoice line items) there isn't room for a visible label on every cell. In that case give the input an `aria-label` — an invisible name read aloud by screen readers — and feed it the *translated* string from `Helpers.Text`. From the real invoice editor:

```razor
aria-label="@Helpers.Text("InvoiceItemDescription")"
```

This is the key integration point: the accessible name is localized, not a hardcoded English word. A blind user on a tenant that renamed the field hears the tenant's wording.

### 4.3 The required-field convention

Required fields are marked two ways, and both are deliberate. The visible red asterisk comes from `Required="true"` on `<Language>` (the `required-flag` span). Because an asterisk alone is meaningless out of context, screens also drop a `<RequiredIndicator />` legend near the top of the form, which renders an asterisk next to translated explanatory text:

```razor
<div class="required-indicator">
    <i class="required-flag"></i>
    <Language Tag="IndicatesRequiredField" />
</div>
```

### 4.4 The known WAVE notes (don't "fix" these)

`Accessibility.md` documents two accepted WAVE *warnings* so you don't waste time chasing them:

- The **PagedRecordset** component repeats text in a `title` attribute. That's intentional: long text is CSS-truncated with an ellipsis (`overflow:hidden; text-overflow: ellipsis`), and the `title` provides the full text on hover. WAVE flags it as redundant; it stays.
- The old **ckEditor** HTML editor used the same text on a toolbar icon and its `title`. The fix here is real: a newer editor is available. **Migrate from `HtmlEditor` to `HtmlEditor2`**, which uses **Quill** instead of ckEditor and passes WAVE with no warnings. Prefer `HtmlEditor2` in new work.

### 4.5 Keyboard reachability

Use real semantic elements — `<input>`, `<button>`, `<a>` — and they are keyboard-focusable and operable for free. Group related controls with `role="group"` where appropriate (the app does this on button groups). The validation focus helper from Section 2, `Helpers.DelayedFocus(...)`, is part of this story too: on a failed save it moves the keyboard caret to the first broken field, so a keyboard-only user isn't left hunting for it.

---

<a id="integration"></a>
## 5. Wiring the Three Together

The three pillars aren't separate chores — on one field they collapse into a single small block of markup plus a few lines in the submit handler. Here is the whole loop for one required field.

**In the markup**, the field is labelled (a11y), the label and required marker are translated (l10n), and the input flags itself red when empty (validation):

```razor
<label for="edit-invoice-Title">
    <Language Tag="InvoiceTitle" Required="true" />
</label>
<input type="text" id="edit-invoice-Title"
       class="@Helpers.MissingValue(_invoice.Title, "form-control")"
       @bind="_invoice.Title" @bind:event="oninput" />
```

**In the submit handler**, the same field contributes a translated error message and a focus target (validation + l10n + a11y all at once):

```csharp
if (String.IsNullOrWhiteSpace(_invoice.Title)) {
    errors.Add(Helpers.MissingRequiredField("InvoiceTitle"));
    if (focus == String.Empty) { focus = "edit-invoice-Title"; }
}
```

Trace one tag, `InvoiceTitle`, through all three:

- **Validation:** `MissingValue` reds the box live; `MissingRequiredField` adds the Save-time message.
- **Localization:** `MissingRequiredField` runs `"RequiredMissing"` *and* `"InvoiceTitle"` through `Helpers.Text`, so a tenant override changes both the sentence and the field name. The visible `<Language Tag="InvoiceTitle">` label uses the same override.
- **Accessibility:** the `<label for>`/`id` pairing names the field for screen readers; `DelayedFocus("edit-invoice-Title")` sends the keyboard straight to it on failure.

One tag, defined once, drives the label, the required marker, the error message, and the accessible name. That is the payoff of routing everything through the framework helpers.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls and Anti-Patterns

These are the mistakes that pass code review because the screen still looks fine in English with a mouse — and then break for a real tenant or a real user.

- **Hardcoding a visible string.** Typing `<label>Invoice Title</label>` instead of `<Language Tag="InvoiceTitle" />` makes the screen impossible for a tenant to reword and impossible to translate. Every human-readable string goes through a tag. If you see a quoted English sentence in markup, it's a bug.

- **Hardcoding a validation message.** Writing `errors.Add("Title is required")` bypasses translation. Use `Helpers.MissingRequiredField("InvoiceTitle")` (or `Helpers.Text("YourTag")`) so the message is a tag, not a sentence.

- **Trusting client validation as security.** Client checks run in the user's browser and can be bypassed. They're for UX. The server must re-validate every required field and rule before saving and return the result via the standard pass/fail object ([026](026_standard-result.md)).

- **An input with no label.** A bare `<input>` is unnamed to a screen reader. Give it a `<label for>`/`id` pair, or — in tight grids — an `aria-label="@Helpers.Text("...")"`. Never leave it nameless.

- **Hardcoding the accessible name.** `aria-label="Description"` defeats the point: localize it with `Helpers.Text`. The screen-reader name must follow the tenant's wording like everything else.

- **Forgetting the `{0}` placeholders.** `RequiredMissing` is `"{0} is Required"`. If you write a custom phrase that should take a field name, include the `{0}` and pass a `ReplaceValues` list, or the substitution silently does nothing.

- **Reaching for the old HTML editor.** New screens should use `HtmlEditor2` (Quill), which passes WAVE cleanly, not the legacy ckEditor-based `HtmlEditor`.

- **Building dates/numbers by hand.** String-concatenating a date ignores the tenant's culture. Format through the culture-aware helpers so each tenant sees their own conventions.

- **Treating zero as a valid number unexpectedly.** Remember `MissingValue(int?)` and `MissingValue(decimal?)` flag `0` (and null) as missing by design. If zero is genuinely valid for a field, don't rely on `MissingValue` to decide presence.

---

<a id="checklist"></a>
## 7. Checklist Before You Ship

Run this gate over every screen before you call it done. Each line is pass/fail.

**Validation**
- [ ] Every required field uses `Helpers.MissingValue(...)` on its `class` so it flags red when empty.
- [ ] The submit handler builds an `errors` list with `Helpers.MissingRequiredField(...)` and shows it via `Model.ErrorMessages(...)`.
- [ ] On a failed save, `Helpers.DelayedFocus(...)` moves focus to the first invalid field.
- [ ] The server re-validates the same rules and returns the standard pass/fail result — the client check is not the only check.

**Localization**
- [ ] No human-readable string is typed directly in markup or C#; every one is a tag through `<Language>` or `Helpers.Text`.
- [ ] Every new tag exists in the default phrase dictionary (`DataAccess.Language.cs`) so it isn't rendered in last-resort ALL-CAPS.
- [ ] Messages with substitutions use `{0}`/`{1}` plus a `ReplaceValues` list.
- [ ] Dates and numbers are formatted through culture-aware helpers, not hand-built.

**Accessibility**
- [ ] Every input has a name: a `<label for>`/`id` pair, or an `aria-label="@Helpers.Text("...")"` in dense grids.
- [ ] Accessible names are localized (via `Helpers.Text`), never hardcoded English.
- [ ] Required fields show the `required-flag` asterisk (`Required="true"`) and the form carries a `<RequiredIndicator />` legend.
- [ ] The screen is fully operable by keyboard; controls are semantic (`<input>`, `<button>`, `<a>`).
- [ ] Rich-text fields use `HtmlEditor2` (Quill), not the legacy `HtmlEditor`.
- [ ] You haven't introduced new WAVE errors (the two documented warnings are accepted).

If any box is unchecked, the screen hasn't met the baseline yet.

---

<a id="related-docs"></a>
## 8. Related Docs

- [031 — List and Edit, the House Pattern](031_crud-templates.md) — the screens this baseline applies to
- [026 — The Standard Pass/Fail Result](026_standard-result.md) — where validation results travel
- [075 — Sweeping for ADA and Accessibility Gaps](075_ada-scanning.md) — tooling that audits accessibility

---
*GuidesV2 035 · validation-localization-a11y · drafted from source 2026-06-05.*
