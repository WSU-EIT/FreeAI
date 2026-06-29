# 011 — Why We Wrap the Framework

> **Document ID:** 011  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the wrapper philosophy and the discipline of choosing tenant-aware helpers over raw .NET calls.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | What a "wrapper" is, what "tenant-aware" means, and why raw .NET calls are risky here |
| 2 | [The Mental Model](#mental-model) | The `Helpers` static class as the single layer between your code and .NET |
| 3 | [An Analogy](#analogy) | The hotel concierge: one trusted desk that knows the house rules |
| 4 | [The Discipline: Wrappers Over Raw Calls](#the-discipline) | The habit of reaching for `Helpers.*` first, with the three flagship examples |
| 5 | [What the Wrappers Protect](#what-wrappers-protect) | Tenant isolation, header/auth consistency, and localization safety |
| 6 | [Finding the Right Wrapper](#finding-wrappers) | How to locate the helper you need inside `Helpers.cs` |
| 7 | [Common Pitfalls](#common-pitfalls) | Mistakes newcomers make, and how to avoid them |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

**A "wrapper" is a small piece of our own code that stands in front of a stock framework call and adds the rules this application always needs.** Instead of calling .NET directly, you call our helper, and the helper calls .NET for you — after it has done the housekeeping.

Three plain-language terms before we go further:

- **Stock .NET** means the plumbing that Microsoft ships in Blazor (the framework we build the browser UI with). Examples are `NavigationManager` for changing pages, `HttpClient` for talking to the server, and ordinary string handling for showing text on screen. These are powerful but *generic* — they know nothing about *our* app.
- **Tenant** means one customer's isolated slice of the system. This product is **multi-tenant**: many separate organizations share the same running application, but each one must only ever see its own data and its own settings. A "tenant-aware" piece of code is one that automatically respects which tenant the current user belongs to.
- **Tenant-awareness** is the thing the wrappers add. A raw .NET call has no idea which tenant is active. Our wrapper does, and it quietly stitches that context into every call.

**Why raw calls are risky here:** if you call stock .NET directly, you get a call with *no tenant context attached*. A page navigation can land on the wrong tenant's URL. A server request can go out *without* the `TenantId` and authentication token the backend expects, so it gets rejected — or worse, behaves as if no tenant were set. On-screen text shows up as a raw code key instead of the customer's chosen language. None of these fail loudly at compile time; they fail later, in ways that are hard to trace. The wrappers exist so you cannot forget this housekeeping, because it is done for you.

Concretely, the wrappers live in one file: `CRM.Client/Helpers.cs`, a `public static partial class Helpers` in the `CRM.Client` namespace. You will see calls like `Helpers.NavigateTo(...)`, `Helpers.GetOrPost<T>(...)`, and `Helpers.Text(...)` throughout the pages — over ninety such calls in the client code already. That ubiquity is the point: it is the normal way to do these things here.

<a id="mental-model"></a>
## 2. The Mental Model

Picture three layers, top to bottom:

```
Your page / component  (the code you write)
        │   calls
        ▼
Helpers.*  (our wrapper layer — Helpers.cs)
        │   calls, after adding app context
        ▼
Stock .NET (NavigationManager, HttpClient, string/Humanizer)
```

You almost never talk to the bottom layer directly. You talk to the middle layer, and it talks to the bottom for you.

The middle layer is one static class that has been handed references to the framework pieces it needs. At startup, an `Init(...)` method stores those references in private fields so every helper can reach them:

```csharp
public static partial class Helpers
{
    private static HttpClient Http = null!;
    private static BlazorDataModel Model = null!;
    private static NavigationManager NavManager = null!;
    // ... DialogService, jsRuntime, LocalStorage, Tooltips
}
```

Two of those fields matter most for this doc:

- `NavManager` is the stock .NET `NavigationManager` (how Blazor changes the page).
- `Http` is the stock .NET `HttpClient` (how the browser talks to the server).
- `Model` is the `BlazorDataModel` — our in-memory snapshot of "who is logged in, which tenant, what language." This is the source of tenant-awareness; the helpers read context from `Model` and apply it.

So the mental model is: **the wrapper layer is a single, shared front desk that already holds the framework tools and already knows the current tenant, user, and language. Your job is to ask the front desk, not to operate the tools yourself.**

<a id="analogy"></a>
## 3. An Analogy

Think of a large hotel that hosts several companies' events at once (the tenants). You are a guest who needs a taxi, a courier, and a sign printed in your company's language.

You *could* walk out to the street and flag a cab yourself (raw .NET). It might work — or you might give the wrong address, forget which company you're with, and end up at a different firm's venue.

Instead, you go to the **concierge desk** (the `Helpers` layer). The concierge already knows which company you're with, which floor your event is on, and what language to print your sign in. You just say "I need a taxi to my venue," and the concierge fills in the correct address, attaches your company's account number, and hands the right instructions to the driver.

The concierge isn't doing anything magical — they're still using ordinary taxis and printers. They are simply the one place that *remembers the house rules so you don't have to.* That is exactly what the wrappers do: same underlying .NET tools, but with your tenant, your auth token, and your language filled in automatically.

<a id="the-discipline"></a>
## 4. The Discipline: Wrappers Over Raw Calls

The discipline is a habit: **when you need to navigate, call the server, or show text, reach for `Helpers.*` first.** Only drop to raw .NET when you have a deliberate reason and you understand what context you are giving up. Here are the three flagship wrappers, copied faithfully from `Helpers.cs`, with *why each one exists*.

### `NavigateTo` — page changes that respect the tenant

Why it matters: a page URL in a multi-tenant app may need a tenant code in front of it. `NavigateTo` adds that prefix for you, and it leaves true external links (`http:`/`https:`) untouched.

```csharp
public static void NavigateTo(string subUrl, bool forceReload = false)
{
    if (subUrl.ToLower().StartsWith("http:") || subUrl.ToLower().StartsWith("https:")) {
        NavManager.NavigateTo(subUrl, forceReload);
    } else {
        NavManager.NavigateTo(Model.ApplicationUrlFull + subUrl, forceReload);
    }
}
```

The interesting part is `Model.ApplicationUrlFull`. That property is the application root, and — when the app is configured to put the tenant code in the URL — it appends the current tenant's code:

```csharp
public string ApplicationUrlFull {
    get {
        string output = ApplicationUrl;
        if (_UseTenantCodeInUrl) {
            if (!output.EndsWith("/")) { output += "/"; }
            if (!String.IsNullOrWhiteSpace(_Tenant.TenantCode)) {
                output += _Tenant.TenantCode + "/";
            } else if (!String.IsNullOrWhiteSpace(_TenantCodeFromUrl)) {
                output += _TenantCodeFromUrl + "/";
            }
        }
        return output;
    }
}
```

If you called `NavManager.NavigateTo("Profile")` directly, you would skip this prefix and could land outside the current tenant's space. `Helpers.NavigateTo("Profile")` gets it right every time.

### `GetOrPost<T>` — one server call that always carries identity

Why it matters: every request to our API must announce *which tenant* and *which authenticated user* is asking. `GetOrPost<T>` is a single generic method (`<T>` means "tell me the type you expect back and I'll deserialize it for you") that attaches these headers before sending, picks GET vs POST automatically, and turns the JSON response into your type:

```csharp
public static async Task<T?> GetOrPost<T>(string url, object? post = null, bool logResults = false)
{
    T? output = default(T);
    if (Http != null) {
        try {
            HttpResponseMessage? response = null;
            Http.DefaultRequestHeaders.Clear();

            if (Model != null) {
                Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());
                if (String.IsNullOrWhiteSpace(Model.User.AuthToken)) {
                    Model.User.AuthToken = await Token();
                }
                if (!String.IsNullOrWhiteSpace(Model.User.AuthToken)) {
                    if (Model.User.AuthToken != "na") {
                        Http.DefaultRequestHeaders.Add("Token", Model.User.AuthToken);
                    }
                } else {
                    Model.User.AuthToken = "na";
                }
                if (!String.IsNullOrWhiteSpace(Model.Fingerprint)) {
                    Http.DefaultRequestHeaders.Add("Fingerprint", Model.Fingerprint);
                }
            }

            if (post != null) {
                response = await Http.PostAsJsonAsync(url, post);
            } else {
                response = await Http.GetAsync(url);
            }
            // ...read content, deserialize into T, log/handle errors...
        } catch (Exception ex) {
            await ConsoleLog("An Exception Occurred Calling '" + url + "' - " + ex.Message);
        }
    }
    return output;
}
```

Notice three things the wrapper does that a raw `HttpClient` call would not: it adds the `TenantId`, `Token`, and `Fingerprint` headers from `Model`; it chooses POST when you pass a body and GET when you don't; and it wraps the whole thing in error handling so a failed call logs and returns a safe default instead of throwing. A typical call site reads simply:

```csharp
var items = await GetOrPost<List<DataObjects.Location>>("api/Data/GetLocations");
```

### `Text` — on-screen wording that respects the customer's language

Why it matters: the same label may need to read differently for different tenants and languages, and undefined labels should stand out rather than crash. `Text` looks the key up in the current tenant's language phrases first, then the default language, and falls back to a humanized version if nothing matches:

```csharp
public static string Text
(
    string? text,
    bool ReplaceSpaces = false,
    List<string>? ReplaceValues = null,
    bool MarkUndefinedStrings = true,
    TextCase textCase = TextCase.Normal
){
    string output = !String.IsNullOrWhiteSpace(text) ? text : "";
    bool foundTag = false;

    if (Model.Language.Phrases.Any()) {
        var phrase = Model.Language.Phrases.FirstOrDefault(x => x.Id != null && x.Id.ToLower() == output.ToLower());
        if (phrase != null) {
            foundTag = true;
            output = !String.IsNullOrWhiteSpace(phrase.Value) ? phrase.Value : String.Empty;
        }
    }
    // ...then DefaultLanguage.Phrases, optional TextCase, ReplaceValues, ReplaceSpaces...
    if (!foundTag) {
        if (MarkUndefinedStrings) {
            output = output.Humanize(LetterCasing.AllCaps);   // unknown keys shout in ALL CAPS
        } else {
            output = output.Humanize(LetterCasing.Title);
        }
    }
    return output;
}
```

`Humanize(...)` comes from the **Humanizer** library (a third-party helper for turning code-style strings into readable text). The `MarkUndefinedStrings` default of `true` is a deliberate safety net: any label you forgot to define shows up loudly in capitals on screen, so it gets noticed and fixed rather than silently shipping a raw key. Raw string concatenation would give you none of this — no per-tenant language, no default-language fallback, no "this key is undefined" signal.

<a id="what-wrappers-protect"></a>
## 5. What the Wrappers Protect

Each wrapper guards a specific guarantee. Lose the wrapper, lose the guarantee.

- **Tenant isolation.** `NavigateTo` keeps you inside the active tenant's URL space via `Model.ApplicationUrlFull`. `GetOrPost<T>` stamps every request with `TenantId` from `Model.User.TenantId`, so the server knows exactly whose data to return. Skip these and a navigation or request can cross tenant boundaries — the single most serious mistake possible in a multi-tenant system.
- **Authentication consistency.** `GetOrPost<T>` resolves and attaches the user's `Token` (calling `Token()` if one isn't loaded yet) and an optional `Fingerprint` on *every* call. You never have to remember to add auth headers, and they can never drift out of sync between call sites, because there is only one place they are set.
- **Predictable request shape.** The same wrapper decides GET vs POST, serializes your body as JSON (`PostAsJsonAsync`), deserializes the reply into your `T`, and contains errors so a failure returns a default value and a log entry instead of an unhandled exception. Your page code stays short and uniform.
- **Localization and presentation safety.** `Text` ensures on-screen wording flows through the tenant's language, then the default language, then a humanized fallback — with undefined keys surfaced in all-caps so nothing ships as a mystery code. Casing options (`TextCase.Normal`, `Lowercase`, `Uppercase`, `Sentence`, `Title`) and `{0}`-style value replacement are handled in one consistent place.

The common thread: **the guarantee lives in the wrapper, not in your discipline at each call site.** That is why we wrap — so correctness does not depend on every developer remembering every rule, every time.

<a id="finding-wrappers"></a>
## 6. Finding the Right Wrapper

Almost everything lives in one place: `CRM.Client/Helpers.cs` (the class is `partial`, so a few companions like `Helpers.App.cs` exist, but `Helpers.cs` is the main file). To find a helper:

1. **Search the `Helpers` class for the verb you have in mind.** Navigation helpers cluster around `NavigateTo` (`NavigateToLogin`, `NavigateToRoot`, `NavigateToRootDefault`, `NavigateToViaJavascript`). Server calls are `GetOrPost<T>` and its relatives. Text/localization is `Text`.
2. **Read the XML doc comment above the method.** Each public helper has a `/// <summary>` describing what it does and what each parameter means — that comment is the fastest way to confirm you have the right one.
3. **Copy an existing call site.** Because there are dozens of real usages, grepping for `Helpers.GetOrPost`, `Helpers.NavigateTo`, or `Helpers.Text` (or the bare names inside the client, since `Helpers` is often imported) shows you working examples to model yours on. For instance, list endpoints follow the pattern `await GetOrPost<List<DataObjects.X>>("api/Data/GetX")`.
4. **If nothing fits, that's a signal — not a license to go raw.** A missing wrapper usually means the helper should be added (or a sibling extended), so the next person gets the same protection. See doc 013 for the decision.

<a id="common-pitfalls"></a>
## 7. Common Pitfalls

- **Calling `NavManager.NavigateTo(...)` directly.** This skips the tenant-aware `ApplicationUrlFull` prefix and can send the user outside the current tenant's URL. Use `Helpers.NavigateTo(...)` unless you are intentionally going to a full external `http(s)` link.
- **Reaching for raw `HttpClient` / `PostAsJsonAsync` / `GetAsync`.** A hand-rolled request will not carry `TenantId`, `Token`, or `Fingerprint`, so the server can reject it or mishandle the tenant context — and you also lose the built-in error handling and JSON deserialization. Use `Helpers.GetOrPost<T>(...)`.
- **Building display strings by hand instead of using `Text`.** Concatenated or hard-coded labels bypass per-tenant language, the default-language fallback, and the all-caps "undefined key" warning. Route user-facing wording through `Helpers.Text(...)`.
- **Forgetting that `Text` marks unknown keys in ALL CAPS by default.** If a label suddenly shows up shouting in capitals, that is the system telling you the key isn't defined in the language phrases — add the phrase, don't pass `MarkUndefinedStrings: false` to hide it.
- **Expecting `GetOrPost<T>` to throw on failure.** It is designed to log and return `default(T)` (often `null`) instead. Always check the result for null/empty before using it, rather than assuming a value came back.
- **Assuming a wrapper doesn't exist and going raw "just this once."** It almost certainly exists — search `Helpers` first. If it genuinely doesn't, the right move is usually to add it, not to bypass the layer.

---

<a id="related-docs"></a>
## 8. Related Docs

- [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md) — the four helper families themselves
- [013 — Custom Helper or Standard Call?](013_custom-or-standard.md) — a chooser: helper versus standard call

---
*GuidesV2 · 011 · drafted from source (`CRM.Client/Helpers.cs`, `CRM.Client/DataModel.cs`) · 2026-06-05.*
