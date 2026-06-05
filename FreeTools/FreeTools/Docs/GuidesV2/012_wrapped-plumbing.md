# 012 — Wrapped Navigation, HTTP, Localization, and Serialization

> **Document ID:** 012  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Document the four core helper families and how each injects tenant context and auth automatically.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Wrapped Plumbing Matters](#why-it-matters) | Plain-language overview and key term definitions |
| 2 | [The Mental Model: Plumbing That Knows Who You Are](#mental-model) | How ambient tenant + auth context rides through every helper |
| 3 | [Wrapped Navigation](#wrapped-navigation) | `NavigateTo` / `BuildUrl` / `ValidateUrl` and how they carry tenant context into URLs |
| 4 | [Wrapped HTTP](#wrapped-http) | `GetOrPost<T>` and the headers it auto-attaches (TenantId, Token, Fingerprint) |
| 5 | [Wrapped Localization](#wrapped-localization) | The `Text` helper, `Model.Language`, and how phrases resolve per tenant |
| 6 | [Wrapped Serialization](#wrapped-serialization) | `SerializeObject` / `DeserializeObject` / `DuplicateObject<T>` and their shared JSON options |
| 7 | [How Injection Actually Happens](#injection-mechanism) | The shared `Model` static field that feeds context into all four families |
| 8 | [Pitfalls and Anti-Patterns](#pitfalls) | What breaks when you bypass the wrappers |
| 9 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Wrapped Plumbing Matters

**Plumbing** is the boring-but-essential code that moves you between pages, calls the server, turns labels into the user's language, and converts objects to and from text for storage or transmission. Stock .NET and Blazor ship perfectly good plumbing for all of this. So why does FreeCRM wrap it?

Because FreeCRM is **multi-tenant**. A single running copy of the application serves many separate customers — each customer is a **tenant**, identified by a `TenantId` (a `Guid`). Tenant A must never see Tenant B's data, links, translations, or settings. That means almost every piece of plumbing needs to know *who the current tenant is* and *who the current user is* before it can do its job correctly.

If every page had to remember to attach the tenant ID to every link, every API call, and every translation lookup, two things would happen: developers would forget (a data leak), and the code would be drowning in repetitive boilerplate.

The fix is a set of **helper families** — small static methods that wrap the raw framework calls and quietly fold in the current context for you. They all live in one file:

`c:\Users\pepkad\source\repo2\FreeCRM\CRM.Client\Helpers.cs`

declared as one big partial class:

```csharp
namespace CRM.Client;

public static partial class Helpers
{
    private static HttpClient Http = null!;
    private static BlazorDataModel Model = null!;
    private static NavigationManager NavManager = null!;
    // ...
}
```

Key terms, defined once:

- **Helper family** — a small group of related static methods on `Helpers` that wrap one concern (navigation, HTTP, localization, or serialization).
- **Tenant context** — the identity of the current customer, primarily `Model.User.TenantId`, plus tenant settings like the tenant's URL and whether a tenant code appears in the address bar.
- **Auth** (authentication / authorization) — proving who the user is. Here it is an **auth token** (a string credential read from a cookie) that the server checks on each request.
- **`Model`** — a single shared `BlazorDataModel` object that holds the current tenant, user, language, and settings. It is the source every helper reads context from.

The payoff: a developer writes `Helpers.NavigateTo("Reports")` or `await Helpers.GetOrPost<MyType>("api/Data/Thing")` and the tenant prefix, auth token, and fingerprint are attached automatically — correctly, every time, without a single line of boilerplate at the call site.

---

<a id="mental-model"></a>
## 2. The Mental Model: Plumbing That Knows Who You Are

Picture a hotel with one set of pipes serving hundreds of rooms. Plain framework plumbing is like a pipe that delivers water but has no idea which room it is feeding — you'd have to hand-write the room number onto every drop. FreeCRM's wrapped plumbing is like a pipe that already knows it is connected to *your* room. You just turn the tap; the routing to the right room is built in.

The "room number" here is the **shared `Model`**. Every helper family reaches into the same static `Model` field to learn the current tenant and user, then does the framework call on your behalf:

- **Navigation** reads `Model.ApplicationUrlFull` so links land inside the current tenant's URL space.
- **HTTP** reads `Model.User.TenantId`, `Model.User.AuthToken`, and `Model.Fingerprint` and stamps them onto outgoing request headers.
- **Localization** reads `Model.Language` (the current tenant/user language) to translate labels.
- **Serialization** uses one consistent set of JSON rules so data shaped on the client matches what the server expects.

Because the context lives in one place and the helpers are the only sanctioned way to use the plumbing, "knowing who you are" is **ambient** — it is always present in the background, and you get it for free as long as you call the wrapper instead of the raw framework method. The rest of this doc walks each family and shows the exact code that makes this happen.

---

<a id="wrapped-navigation"></a>
## 3. Wrapped Navigation

**Why it matters:** In a multi-tenant app, a link to `Reports` is ambiguous — *whose* reports? Blazor's built-in `NavigationManager.NavigateTo` would happily send the user to a root-level `/Reports` with no tenant. The navigation helpers make sure every internal link is rooted inside the current tenant's URL space.

**`BuildUrl`** is the simplest. It takes a relative sub-URL and prepends the current tenant's full base URL:

```csharp
public static string BuildUrl(string? subUrl = "")
{
    return Model.ApplicationUrlFull + subUrl;
}
```

`Model.ApplicationUrlFull` is the load-bearing part. When the app is configured to put a **tenant code** (a short text identifier for a tenant) in the address bar, this property appends it automatically:

```csharp
public string ApplicationUrlFull {
    get {
        string output = ApplicationUrl;

        if (_UseTenantCodeInUrl) {
            if (!output.EndsWith("/")) {
                output += "/";
            }

            if (!String.IsNullOrWhiteSpace(_Tenant.TenantCode)) {
                output += _Tenant.TenantCode + "/";
            }
            // ...
        }

        return output;
    }
}
```

So `BuildUrl("Reports")` becomes something like `https://app.example.com/acme/Reports` for tenant "acme" — without the caller knowing or caring whether tenant codes are even in use.

**`NavigateTo`** is what you call to actually move the user. It is smart about absolute vs. relative links: a full `http:`/`https:` URL is passed through untouched, but a relative path is rooted into the tenant URL first:

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

There are siblings for common destinations — `NavigateToRoot`, `NavigateToLogin`, `NavigateToRootDefault` — each of which applies the same tenant-aware base.

**`ValidateUrl`** runs the other direction: instead of building a link, it checks whether the tenant code currently in the URL is legitimate, and corrects the route if not. It only does work when tenant codes are in use (`Model.UseTenantCodeInUrl`):

```csharp
public static async Task ValidateUrl(string? TenantCode, bool AutoRedirect = false)
{
    if (_validatingUrl) {
        return;
    }
    _validatingUrl = true;

    if (Model.UseTenantCodeInUrl) {
        if (String.IsNullOrWhiteSpace(TenantCode)) {
            // No code present: redirect to the default tenant or the MissingTenantCode page.
            // ...
        } else {
            // A code was supplied: confirm it exists in the tenant list...
            var tenant = Model.TenantList.FirstOrDefault(x => x.TenantCode.ToLower() == TenantCode.ToLower());
            if (tenant == null) {
                NavManager.NavigateTo(Model.ApplicationUrl + "InvalidTenantCode");
            } else {
                // ...and switch to it if it isn't already the current tenant.
                if (StringValue(Model.Tenant.TenantCode).ToLower() != TenantCode.ToLower()) {
                    await SwitchTenant(tenant.TenantId);
                }
            }
        }
    }
    // ...
    _validatingUrl = false;
}
```

Notice the `_validatingUrl` guard — a private flag that prevents the method from re-entering itself while a redirect or tenant switch is already in progress. The takeaway: navigation is never "go to this path"; it is always "go to this path *for this tenant*, and make sure the tenant in the URL is valid."

---

<a id="wrapped-http"></a>
## 4. Wrapped HTTP

**Why it matters:** The browser side of FreeCRM talks to the server over HTTP. Every one of those calls has to prove which tenant and user it belongs to, or the server cannot safely return data. The HTTP helper makes that proof automatic, so no page ever forgets to attach credentials.

The single workhorse is **`GetOrPost<T>`**. One method handles both reads and writes: if you pass a `post` object it does an HTTP POST (sending that object as JSON); if you don't, it does a GET. `T` is the type you expect back, and the method deserializes the JSON response into it for you.

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
            // ... read and deserialize the response ...
        } catch (Exception ex) {
            await ConsoleLog("An Exception Occurred Calling '" + url + "' - " + ex.Message);
        }
    }

    return output;
}
```

Walk through what it folds in automatically, every single call:

- **`TenantId` header** — `Model.User.TenantId.ToString()`. This is how the server knows which customer's data to scope the request to. It is *always* attached.
- **`Token` header** — the auth token. If the in-memory token is empty, the helper fetches it (via `Token()`, which reads the `user-token` cookie) before sending. The literal string `"na"` is used as a sentinel meaning "no token available," and is deliberately *not* sent as a header.
- **`Fingerprint` header** — a device/browser fingerprint, attached when present, used by the server for additional request validation.

It also clears headers first (`Http.DefaultRequestHeaders.Clear()`) so stale values from a previous call can't bleed into this one — important because the shared `HttpClient` is reused.

On the response side, success reads the body and either returns the raw string (when `T` is `string`) or deserializes the JSON into `T`. Failures and exceptions are logged via `ConsoleLog` and the method simply returns `default(T)` rather than throwing — so a failed call yields `null` for reference types, which callers check for. A typical call site is dead simple:

```csharp
var items = await GetOrPost<List<DataObjects.Department>>("api/Data/GetDepartments");
```

No headers, no token handling, no tenant plumbing at the call site — all of it is inside the wrapper.

---

<a id="wrapped-localization"></a>
## 5. Wrapped Localization

**Why it matters:** Different tenants (and different users) can run the app in different languages, and even within one language a tenant may want to rename a label — calling "Departments" "Teams," for example. **Localization** (turning a fixed key into the right display text) therefore has to be tenant- and user-aware, not hard-coded in the markup.

The entry point is the **`Text`** helper. You give it a key (a "language tag") and it returns the display string for the current language:

```csharp
public static string Text(
    string? text,
    bool ReplaceSpaces = false,
    List<string>? ReplaceValues = null,
    bool MarkUndefinedStrings = true,
    TextCase textCase = TextCase.Normal)
```

The resolution order is the important part. It looks up the key in the **current** language first, then falls back to the **default** language:

```csharp
if (Model.Language.Phrases.Any()) {
    var phrase = Model.Language.Phrases
        .FirstOrDefault(x => x.Id != null && x.Id.ToLower() == output.ToLower());
    if (phrase != null) {
        foundTag = true;
        output = !String.IsNullOrWhiteSpace(phrase.Value) ? phrase.Value : String.Empty;
    }
}

if (!foundTag && Model.DefaultLanguage.Phrases.Any()) {
    var phrase = Model.DefaultLanguage.Phrases
        .FirstOrDefault(x => x.Id != null && x.Id.ToLower() == output.ToLower());
    // ...
}
```

Both `Model.Language` and `Model.DefaultLanguage` are `DataObjects.Language` objects. That type carries the tenant binding and the phrase list:

```csharp
public partial class Language
{
    public Guid TenantId { get; set; }
    public string Culture { get; set; } = String.Empty;
    public string Description { get; set; } = String.Empty;
    public List<DataObjects.OptionPair> Phrases { get; set; } = new List<OptionPair>();
}
```

Each phrase is an `OptionPair` of `Id` (the key, matched case-insensitively) and `Value` (the display text). Because `Language` is loaded per tenant from the server — for the default set via `LoadDefaultLanguage()`, which calls `GetOrPost<DataObjects.Language>("api/Data/GetDefaultLanguage")` — the same key can resolve to different text for different tenants. That is what makes localization "scoped per tenant."

`Text` does more than a straight lookup:

- **`ReplaceValues`** fills numbered placeholders. If the resolved phrase contains `{0}`, `{1}`, etc., each value in the list is substituted in order (and each value is itself run through `Text`, so placeholders can also be language tags).
- **`textCase`** applies casing (lowercase, uppercase, sentence, or title case) using Humanizer.
- **`MarkUndefinedStrings`** controls the fallback when no phrase is found: when `true`, the raw key is rendered in ALL CAPS (a visible signal to developers that a tag is missing), otherwise it is converted to readable Title Case.
- **`ReplaceSpaces`** swaps spaces for `&nbsp;` for non-wrapping labels.

A few early-boot keys (`APPTITLE`, `LOADINGWAIT`) are special-cased so the UI shows something sensible before the language model has fully loaded. The net effect: markup says `Text("Departments")`, and what the user actually sees is whatever *their* tenant and language define for that key.

---

<a id="wrapped-serialization"></a>
## 6. Wrapped Serialization

**Why it matters:** **Serialization** is converting an in-memory object to text (JSON) and back. FreeCRM does this constantly — sending objects to the API, caching them, and making copies. If different parts of the app used different JSON rules, an object serialized in one place might fail to deserialize in another. The serialization helpers give the whole client one consistent set of rules.

All three helpers wrap `System.Text.Json` (the built-in .NET JSON library) so callers never construct serializer options by hand.

**`SerializeObject`** turns an object into a JSON string, with an option to pretty-print:

```csharp
public static string SerializeObject(object? Object, bool formatOutput = false)
{
    string output = String.Empty;

    if (Object != null) {
        if (formatOutput) {
            output += System.Text.Json.JsonSerializer.Serialize(Object,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        } else {
            output += System.Text.Json.JsonSerializer.Serialize(Object);
        }
    }

    return output;
}
```

**`DeserializeObject<T>`** is the inverse. The load-bearing detail is `PropertyNameCaseInsensitive = true` — JSON whose property names differ in casing from the C# type still binds correctly, which matters because data arrives from many sources:

```csharp
public static T? DeserializeObject<T>(string? SerializedObject)
{
    var output = default(T);

    if (!String.IsNullOrWhiteSpace(SerializedObject)) {
        try {
            var d = System.Text.Json.JsonSerializer.Deserialize<T>(SerializedObject,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d != null) {
                output = d;
            }
        } catch { }
    }

    return output;
}
```

Note the `try { } catch { }`: malformed JSON yields `default(T)` (usually `null`) instead of throwing — the same fail-soft contract as `GetOrPost`.

**`DuplicateObject<T>`** is a deep-copy convenience built *on top of* serialization. To make a fully independent copy of an object (no shared references), it serializes the original and immediately deserializes it into a brand-new instance:

```csharp
public static T? DuplicateObject<T>(object? o)
{
    T? output = default(T);

    if (o != null) {
        // To make a new copy serialize the object and then deserialize it back to a new object.
        var serialized = System.Text.Json.JsonSerializer.Serialize(o);
        if (!String.IsNullOrEmpty(serialized)) {
            try {
                var duplicate = System.Text.Json.JsonSerializer.Deserialize<T>(serialized,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (duplicate != null) {
                    output = duplicate;
                }
            } catch { }
        }
    }

    return output;
}
```

This is the idiomatic way to get an editable copy of a record — for example, cloning a loaded entity into a form so that cancelling an edit doesn't mutate the original still bound elsewhere in the UI. (A related helper, `DeserializeJsonDocumentObject<T>`, handles the case where the input is a `JsonElement` rather than a string, using the same case-insensitive options.) Because every one of these routes through the same options, an object serialized anywhere on the client round-trips cleanly anywhere else.

---

<a id="injection-mechanism"></a>
## 7. How Injection Actually Happens

You have now seen four different families read context. The thing that makes them feel like one coherent system is that they **all read from the same place**: the private static `Model` field on `Helpers`.

```csharp
public static partial class Helpers
{
    private static HttpClient Http = null!;
    private static BlazorDataModel Model = null!;
    private static NavigationManager NavManager = null!;
    // ...
}
```

These are filled once, at startup, by `Helpers.Init(...)`, which receives the live `BlazorDataModel`, `HttpClient`, and `NavigationManager` from the app's dependency-injection container and stashes them in these static fields. (**Dependency injection** is the framework practice of handing an object the services it needs rather than making it construct them.) After `Init`, every helper has ambient access to the same shared state.

That is the whole trick:

- Navigation reads `Model.ApplicationUrlFull`, `Model.UseTenantCodeInUrl`, `Model.Tenant`.
- HTTP reads `Model.User.TenantId`, `Model.User.AuthToken`, `Model.Fingerprint`.
- Localization reads `Model.Language` and `Model.DefaultLanguage`.
- Serialization needs no tenant context, but shares the same option conventions so its output interoperates with everything else.

When the user switches tenants, `Model` is updated in one place and **every** helper immediately starts producing tenant-correct results — links rebuild against the new tenant URL, HTTP calls carry the new `TenantId`, and `Text` resolves against the new language — with no per-call changes anywhere in the codebase. The "injection" is not magic middleware; it is a single shared model that the wrappers consistently consult. Centralizing it is exactly what makes the context impossible to forget.

---

<a id="pitfalls"></a>
## 8. Pitfalls and Anti-Patterns

The wrappers only protect you if you use them. The common mistakes all amount to going around the plumbing:

- **Calling `NavManager.NavigateTo` directly.** Skipping `Helpers.NavigateTo`/`BuildUrl` means you lose the `Model.ApplicationUrlFull` prefix — links land outside the current tenant's URL space and, when tenant codes are in use, drop the code entirely. Always navigate through the helpers.

- **Using a raw `HttpClient` instead of `GetOrPost<T>`.** A hand-rolled request will not carry the `TenantId`, `Token`, or `Fingerprint` headers. Best case the server rejects it; worse case you build a path that silently bypasses tenant scoping. Route all API calls through `GetOrPost`.

- **Hard-coding display text in markup.** Writing the literal "Departments" instead of `Text("Departments")` defeats per-tenant relabeling and translation. If you see a string the user can read, it should almost always be a language tag.

- **Assuming `GetOrPost` throws on failure.** It does not — it logs and returns `default(T)`. Callers that ignore a possible `null`/empty result will misbehave when the server errors. Check the result before using it (the load helpers' `items != null && items.Any()` pattern is the model to follow).

- **Constructing your own `JsonSerializerOptions`.** Rolling your own serializer settings risks casing mismatches and inconsistent round-trips. Use `SerializeObject` / `DeserializeObject` so everything shares `PropertyNameCaseInsensitive = true`.

- **Shallow-copying when you mean to clone.** Assigning an object to a new variable shares references; editing the "copy" mutates the original still bound in the UI. Use `DuplicateObject<T>` for a true independent copy.

- **Mutating `Model` outside the sanctioned flow.** Because every helper reads `Model`, poking tenant or user fields directly (rather than through the proper switch/login paths) can leave the four families disagreeing about who the current tenant is. Treat `Model` as the single source of truth and let the established helpers update it.

The through-line: the wrappers exist so tenant and auth context is automatic and correct. Every anti-pattern above is some flavor of "I touched the raw framework and lost the context the wrapper would have supplied."

---

<a id="related-docs"></a>
## 9. Related Docs

- [011 — Why We Wrap the Framework](011_wrapper-philosophy.md) — why we wrap at all
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — the controllers the HTTP helper talks to

---
*GuidesV2 · 012 · drafted from source (`CRM.Client/Helpers.cs`, `CRM.Client/DataModel.cs`, `CRM.DataObjects/DataObjects.cs`) · 2026-06-05.*
