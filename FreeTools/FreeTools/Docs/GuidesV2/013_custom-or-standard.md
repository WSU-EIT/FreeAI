# 013 — Custom Helper or Standard Call?

> **Document ID:** 013  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** A side-by-side decision guide mapping each stock .NET API to the framework helper to reach for instead.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | What "helper" vs "stock call" means and why the choice is not cosmetic |
| 2 | [The Mental Model](#mental-model) | How the `Helpers` class wraps four stock .NET APIs |
| 3 | [Side-by-Side Mapping Table](#mapping-table) | Each stock .NET API paired with the helper to use instead |
| 4 | [How to Decide](#how-to-decide) | A short checklist for picking helper or stock |
| 5 | [When Stock .NET Still Wins](#stock-wins) | The narrow cases where the standard call is correct |
| 6 | [Common Pitfalls](#pitfalls) | Real mistakes from mixing the two approaches |
| 7 | [Where to Go Next](#next-steps) | Related guides and deeper references |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

When you write a feature in this codebase, you constantly reach for plumbing: navigate to a page, call an API, show a label, turn an object into JSON. The .NET runtime ships built-in tools for every one of those jobs. So does this project. The question this doc answers is: **for any given job, do you call the built-in .NET tool, or the project's own helper?**

Two terms first, in plain language:

- A **stock call** (also "standard call") is the tool that comes straight out of the box with .NET — for example `NavigationManager.NavigateTo(...)` for moving between pages, or `HttpClient.GetAsync(...)` for fetching data. Stock tools know nothing about *this* application. They do exactly what the framework documentation says, no more.
- A **helper** is a method on the project's own `Helpers` class (the file `CRM.Client/Helpers.cs`, namespace `CRM.Client`, declared `public static partial class Helpers`). A helper usually *wraps* a stock call — it does the same underlying work, but first folds in things this app always needs: the current tenant, the user's auth token, the active language, and safe error handling.

Why the choice matters, and why it is not just style: in a **multi-tenant** app — one deployment serving many separate customer organizations, each called a *tenant* — almost every operation has to be scoped to "the tenant this user belongs to." A raw `HttpClient.GetAsync` sends no tenant identity and no auth token, so the server cannot tell who is asking. A raw label like `"Save"` ignores the tenant's chosen language and any custom wording. The helpers exist precisely so you do not have to remember all of that on every call. Reach for the stock tool by reflex and you will ship code that compiles, runs on your machine, and then silently does the wrong thing for real tenants.

So the default is simple: **if a helper exists for the job, use it.** This doc shows you which helper maps to which stock call, and the few honest exceptions.

---

<a id="mental-model"></a>
## 2. The Mental Model

Picture the `Helpers` class as a **front desk** sitting between your component and the raw .NET framework. You hand the front desk a simple request ("navigate to the settings page"); the desk attaches everything the building requires — your ID badge, your floor, the right language — and *then* talks to the framework on your behalf.

That is exactly how `Helpers` is built. It is a static class that is handed all the shared services once, at startup, through its `Init` method:

```csharp
public static void Init
(
    IJSRuntime jSRuntime,
    BlazorDataModel model,
    HttpClient httpClient,
    ILocalStorageService localStorage,
    Radzen.DialogService dialogService,
    Radzen.TooltipService tooltipService,
    NavigationManager navigationManager
){ ... }
```

After `Init` runs, the helpers hold private references to the real `HttpClient`, the real `NavigationManager`, and the app's `BlazorDataModel` (the in-memory `Model` that knows the current user, tenant, and language). Every helper then leans on those references so you never have to pass them yourself.

The wrap follows one of three shapes, and it helps to know which you are looking at:

1. **Wrap and enrich** — the helper calls the stock API but injects context first. `Helpers.NavigateTo` and `Helpers.GetOrPost` are this shape. This is where the real value lives.
2. **Wrap and harden** — the helper calls the stock API but adds null-safety and try/catch so a bad value returns a sensible default instead of throwing. `Helpers.SerializeObject` and `Helpers.DeserializeObject<T>` are this shape.
3. **Replace entirely** — the helper does something stock .NET has no concept of at all. `Helpers.Text` is this shape: there is no .NET API that knows about a tenant's custom phrase list.

The point of the model: the helpers are not a different framework. They are the *same* .NET framework with this app's context already filled in. When you call one, the stock call still happens — it just happens correctly.

---

<a id="mapping-table"></a>
## 3. Side-by-Side Mapping Table

Four families of stock .NET API have project helpers. Use the right column unless [section 5](#stock-wins) explicitly says otherwise.

| Job | Stock .NET (do NOT reach for first) | Helper to use instead | What the helper adds |
|-----|-------------------------------------|------------------------|----------------------|
| Move between pages | `NavigationManager.NavigateTo(url)` | `Helpers.NavigateTo(subUrl, forceReload = false)` | Prepends the tenant-aware `Model.ApplicationUrlFull` for relative URLs; passes full `http:`/`https:` URLs straight through. Also `NavigateToRoot`, `NavigateToLogin`, `NavigateToRootDefault`. |
| Call an API endpoint | `HttpClient.GetAsync` / `PostAsJsonAsync` + `ReadFromJsonAsync<T>` | `Helpers.GetOrPost<T>(url, post = null, logResults = false)` | Auto-attaches `TenantId`, `Token` (auth), and `Fingerprint` headers; picks GET vs POST automatically; deserializes into `T`; catches and logs errors instead of throwing. |
| Show a label / message | `IStringLocalizer["Key"]` (or a hard-coded string) | `Helpers.Text(text, ReplaceSpaces, ReplaceValues, MarkUndefinedStrings, textCase)` | Resolves against the tenant's `Model.Language.Phrases`, then `Model.DefaultLanguage.Phrases`; supports `{0}` value substitution and `TextCase` formatting; humanizes unknown keys. |
| Object → JSON | `JsonSerializer.Serialize(obj)` | `Helpers.SerializeObject(obj, formatOutput = false)` | Null-safe (returns `""` for null); `formatOutput: true` gives indented JSON. |
| JSON → Object | `JsonSerializer.Deserialize<T>(json)` | `Helpers.DeserializeObject<T>(json)` | Sets `PropertyNameCaseInsensitive = true`; wraps the call in try/catch so malformed JSON returns `default(T)` rather than throwing. |

A few signatures copied faithfully from `CRM.Client/Helpers.cs`, so you can see what they actually accept:

```csharp
public static void NavigateTo(string subUrl, bool forceReload = false)

public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)

public static string Text(
    string? text,
    bool ReplaceSpaces = false,
    List<string>? ReplaceValues = null,
    bool MarkUndefinedStrings = true,
    TextCase textCase = TextCase.Normal)

public static string SerializeObject(object? Object, bool formatOutput = false)

public static T? DeserializeObject<T>(string? SerializedObject)
```

And here is what "wrap and enrich" looks like inside `NavigateTo` — note it never just forwards a relative URL blindly:

```csharp
if (subUrl.ToLower().StartsWith("http:") || subUrl.ToLower().StartsWith("https:")) {
    NavManager.NavigateTo(subUrl, forceReload);
} else {
    NavManager.NavigateTo(Model.ApplicationUrlFull + subUrl, forceReload);
}
```

The same idea, much bigger, lives in `GetOrPost` — before the request goes out it clears the headers and re-adds the tenant and auth context:

```csharp
Http.DefaultRequestHeaders.Clear();
Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());
// ...resolves and adds the auth "Token" and "Fingerprint" headers...
if (post != null) {
    response = await Http.PostAsJsonAsync(url, post);
} else {
    response = await Http.GetAsync(url);
}
```

That header block is the single most important reason not to call `HttpClient` directly: skip it and the server has no idea which tenant or user is asking.

---

<a id="how-to-decide"></a>
## 4. How to Decide

You almost never need to deliberate. Walk this short checklist top to bottom and stop at the first "yes":

1. **Does a helper for this job exist?** Check the table in [section 3](#mapping-table). For the big four — navigation, API calls, labels, JSON — the answer is yes. Use the helper. Done.
2. **Does the job touch tenant, user, auth, or language in any way?** If a request must be scoped to the current customer, or a string a human will read must honor the tenant's wording/language, you *must* go through the helper. Stock calls carry none of that context.
3. **Are you inside a Blazor component or page (`.razor`) or anywhere `Helpers.Init` has already run?** If yes, the helpers are wired up and ready — there is no setup cost to using them.
4. **Only if all of the above are "no"** — the job is genuinely generic, touches no app context, and a helper does not exist — reach for the stock .NET call. See [section 5](#stock-wins).

A blunt rule of thumb that is right far more often than it is wrong: **if a human will read the output, or a server will receive the request, use the helper.** Labels are read by humans; API calls are received by a tenant-aware server. Both need context the stock tools cannot supply.

Real usage from the codebase shows how natural this is in practice — a single page mixes a label, an API call, and a navigation, all through helpers:

```csharp
errors.Add(Helpers.Text("NewPasswordAndConfirmDontMatch"));
var reset = await Helpers.GetOrPost<DataObjects.BooleanResponse>("api/Data/ResetUserPassword", _passwordReset);
Helpers.NavigateTo("PasswordChanged");
```

Notice the API path is the relative `"api/Data/ResetUserPassword"` — you never build the full URL yourself, because `GetOrPost` and the server resolve tenant context for you.

---

<a id="stock-wins"></a>
## 5. When Stock .NET Still Wins

Helpers are the default, not a religion. The standard call is the right choice when the job is genuinely context-free. Honest cases:

- **There is no helper, and the work touches no app context.** Plenty of `System.*` work — `string.Split`, `DateTime` math, `Path.GetExtension`, LINQ — has no tenant or auth dimension. Call it directly. (Note: even some of these have *convenience* helpers like `Helpers.CsvToListOfString`, but the stock call is not wrong here.)
- **You are below the helper layer.** Inside `Helpers.cs` itself, the helpers call the stock APIs directly — that *is* the wrap. Likewise, server-side code that establishes tenant context (rather than consuming it) works with the raw `HttpClient`/`HttpContext` plumbing.
- **You truly need behavior the helper hides.** `GetOrPost` deliberately swallows exceptions and returns `default(T)` on failure. If you specifically need to inspect the raw `HttpResponseMessage`, observe a non-success status code, or handle a streaming response, the helper is the wrong tool and a direct `HttpClient` call is correct. That is a rare, deliberate decision — not a default.
- **One-off serialization with custom options.** `SerializeObject`/`DeserializeObject<T>` use fixed `JsonSerializerOptions`. If you need a custom converter, a non-default naming policy, or to *not* swallow parse errors, call `System.Text.Json.JsonSerializer` directly.

The common thread: stock wins only when the context the helper would inject is irrelevant or actively in your way. If you are reaching for stock to "keep it simple," reconsider — the helper is usually simpler *and* correct.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls

These are the mistakes that compile cleanly and then bite in production:

- **Calling `HttpClient` directly and getting silent auth/tenant failures.** A raw `GetAsync`/`PostAsJsonAsync` sends no `TenantId`, no `Token`, no `Fingerprint`. The server cannot identify the caller, so it returns empty, unauthorized, or another tenant's view. Always go through `Helpers.GetOrPost<T>`.
- **Building absolute API URLs by hand.** Pass the *relative* path (e.g. `"api/Data/SaveUser"`) to `GetOrPost`, and the relative sub-page to `NavigateTo`. Hand-concatenating a base URL fights the tenant-aware `Model.ApplicationUrlFull` resolution and breaks under tenant-code-in-URL setups.
- **Hard-coding human-readable strings instead of using `Helpers.Text`.** A literal `"Save"` ignores the tenant's language and any custom phrasing. Route every user-facing label through `Helpers.Text("Save")` so it resolves against `Model.Language.Phrases`. (If a key is undefined, `Text` humanizes it to ALL CAPS by default — that uppercase label in the UI is your signal that a phrase is missing, not a styling choice.)
- **Assuming `DeserializeObject<T>` throws on bad JSON.** It does not — it catches the exception and returns `default(T)` (often `null`). Always null-check the result; do not wrap it in your own try/catch expecting an exception that never comes.
- **Forgetting `JsonSerializer.Deserialize` is case-sensitive but `DeserializeObject<T>` is not.** The helper sets `PropertyNameCaseInsensitive = true`. Code that works through the helper can break if you "optimize" it to a raw `Deserialize` call, because casing that the helper tolerated now fails to map.
- **Mixing approaches in one flow.** Using `Helpers.GetOrPost` for the call but a hard-coded string for the resulting message, or `Helpers.Text` for the label but a raw `NavigationManager` for the redirect, leaves half your flow context-aware and half blind. Pick the helper consistently across the whole operation.

---

<a id="next-steps"></a>
## 7. Where to Go Next

You now have the decision rule: helper by default, stock only when the job is genuinely context-free. To go deeper:

- Read **[011 — Why We Wrap the Framework](011_wrapper-philosophy.md)** for the reasoning behind wrapping at all — the "why" beneath this doc's "which."
- Read **[012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md)** for a full tour of each of the four helper families and exactly how each one injects tenant context and auth.
- Browse `CRM.Client/Helpers.cs` directly. It is one large `public static partial class Helpers`; skim the method names to learn what else is already wrapped for you (file handling, formatting, querystring access, clipboard, local storage, and more) before you write anything from scratch.
- Look at real components such as `CRM.Client/Pages/Profile.razor` and `CRM.Client/Pages/ChangePassword.razor` to see helpers used the way the team uses them.

---

<a id="related-docs"></a>
## 8. Related Docs

- [011 — Why We Wrap the Framework](011_wrapper-philosophy.md) — the philosophy behind the choice
- [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md) — the helper families to choose from

---
*GuidesV2 · 013 · drafted from source (`CRM.Client/Helpers.cs`, `CRM.Client/DataModel.cs`) · 2026-06-05.*
