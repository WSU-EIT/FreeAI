# 016 — Everything Knows Its Tenant

> **Document ID:** 016  ·  **Category:** Concept  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Establish the multi-tenant mindset that threads tenant-awareness through navigation, data, and helpers.
> **Audience:** Newcomers gaining competence  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 01x (Mental Models: How This Differs From Stock .NET) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Tenant-Awareness Matters](#why-it-matters) | What a tenant is, what isolation means, and why correctness depends on it |
| 2 | [The Mental Model: Tenant as Ambient Context](#mental-model) | `TenantId` rides along behind every call, on both client and server |
| 3 | [An Analogy: The Apartment Building](#analogy) | Shared structure, locked units, and the master key (AppAdmin) |
| 4 | [Tenant-Awareness in Navigation](#navigation) | Dual `@page` routes, `{TenantCode}` in the URL, and `ValidateUrl` |
| 5 | [Tenant-Awareness in Data](#data) | How the `TenantId` header reaches the server and scopes every request |
| 6 | [Tenant-Awareness in Helpers and Services](#helpers) | `ValidateUrl`, `SwitchTenant`, and tenant-aware URL builders |
| 7 | [Common Pitfalls and Tenant Leaks](#pitfalls) | Missing guards, wrong tenant in the header, and cross-tenant mistakes |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Tenant-Awareness Matters

**Why this matters first:** if you get tenant-awareness wrong, one customer can see another customer's data. That is the single worst kind of bug a business app can have. Everything in this doc exists to prevent it.

Let's define the words before we use them:

- **Tenant** — one isolated customer (an organization, a company, a club) inside a single running copy of the app. FreeCRM does not run a separate website per customer; it runs *one* application that serves *many* tenants. In the code, a tenant is identified by a `TenantId`, which is a **Guid** — a 128-bit globally-unique identifier that looks like `9f1c...`-style hex and is effectively impossible to guess or collide with.
- **Multi-tenancy** — the architecture where that single application instance serves many tenants at once, keeping each one's data and settings separate.
- **Isolation** — the guarantee that tenant A can never read, change, or even notice tenant B's data. Isolation is not automatic; the code has to *enforce* it on every request.

So why does correctness hinge on this? Because in a multi-tenant app, almost every row of data, every user, every setting, and every uploaded file belongs to exactly one tenant. If a query forgets to filter by tenant, or an API endpoint forgets to check "does this caller actually belong to this tenant?", the isolation guarantee breaks silently — no crash, no error, just the wrong data flowing to the wrong customer.

The good news: FreeCRM makes the tenant a first-class, ever-present piece of context. Once you internalize the mental model in the next section, you'll know exactly where to look to confirm a feature is tenant-safe.

<a id="mental-model"></a>
## 2. The Mental Model: Tenant as Ambient Context

**The one idea to hold in your head:** the current tenant is *ambient context* — a value that is always available in the background, so individual pieces of code rarely have to fetch it from scratch. They just read it.

On the **client** (the Blazor app that runs in the browser), that ambient context lives on the shared data model. In `CRM.Client/DataModel.cs` you'll find the tenant identity stored as a handful of properties on the model that every page shares:

```csharp
public Guid TenantId { ... }              // the current tenant, as a Guid
public string? TenantCodeFromUrl { ... }  // the short code that was in the URL
public bool UseTenantCodeInUrl { ... }    // is this app configured to put the code in the URL?
```

Any page or component can read `Model.TenantId` and immediately know "which customer am I working for right now." The user object carries it too — `Model.User.TenantId` — which is what actually gets sent to the server (see Section 5).

On the **server**, the ambient context is rebuilt per request. The `DataController` constructor (`CRM/Controllers/DataController.cs`) declares a private field:

```csharp
private Guid TenantId = Guid.Empty;
```

and fills it in at the very start of every request before any endpoint code runs. So by the time your endpoint method executes, "which tenant is this request for?" has already been answered and is sitting in `TenantId` (and on `CurrentUser.TenantId`).

The takeaway: you almost never *construct* the tenant. You *inherit* it from the ambient context — `Model.TenantId` on the client, `TenantId` / `CurrentUser.TenantId` on the server. Your job as a feature author is to make sure you actually use it, and never bypass it.

<a id="analogy"></a>
## 3. An Analogy: The Apartment Building

If the Guid talk feels abstract, here's the picture to keep.

The application is **one apartment building**. The building has shared structure: one front door, one set of plumbing, one elevator — that's the single running app, its single codebase, and its single database. Everyone uses the same building.

Each **apartment is a tenant**. (The word "tenant" is the same in real estate and in software for exactly this reason.) Inside their unit, residents keep their own furniture and belongings — that's each customer's users, records, settings, and files.

The **apartment key is the `TenantId`**. Your key opens your unit and only your unit. In the app, the `TenantId` on a request is what unlocks that tenant's data and nothing else. Hand someone the wrong key and they walk into the wrong apartment — that is exactly a tenant leak.

There is also a **building superintendent with a master key** — that's the **AppAdmin** (application administrator). The master key can open any unit because someone has to maintain the whole building. You can see this in the code's permission checks, where access is granted if the caller is an AppAdmin *or* a regular admin who belongs to that specific tenant:

```csharp
if (CurrentUser.AppAdmin || (CurrentUser.Admin && CurrentUser.TenantId == id)) {
    // allowed
}
```

Read that as: "the superintendent (AppAdmin) may enter, *or* a resident admin may enter *but only their own unit* (`CurrentUser.TenantId == id`)." That single line is the whole isolation policy in miniature — and you'll meet it again in Section 5.

<a id="navigation"></a>
## 4. Tenant-Awareness in Navigation

**Why navigation comes into it:** in many multi-tenant apps the very first signal of "which tenant" comes from the web address the user typed. FreeCRM optionally supports a **tenant code in the URL** — a short, human-friendly identifier (the `TenantCode`, e.g. `acme`) that sits in the path, like `https://app.example.com/acme/Settings/Users`. Whether the app uses this style at all is controlled by the `UseTenantCodeInUrl` flag.

To support both styles (with and without the code), pages declare **two `@page` routes** — one plain, one with a `{TenantCode}` placeholder. Here is the real top of the Users settings page (`CRM.Client/Pages/Settings/Users/Users.razor`):

```razor
@page "/Settings/Users"
@page "/{TenantCode}/Settings/Users"
```

The `{TenantCode}` in braces is a **route parameter** — Blazor pulls whatever text is in that URL slot and hands it to the page. The page captures it into a property:

```csharp
[Parameter] public string? TenantCode { get; set; }
```

and then does two things on first render: it records the code into the ambient model, and it asks a helper to validate it:

```csharp
Model.TenantCodeFromUrl = TenantCode;
// ...
await Helpers.ValidateUrl(TenantCode);
```

`ValidateUrl` (covered in detail in Section 6) is the gatekeeper. In plain terms it answers: "Given how this app is configured, is this URL's tenant code acceptable, and does it match the tenant we're currently acting as?" If the code is missing when one is required, it redirects to a `MissingTenantCode` page (or a configured default). If the code is present but unknown, it redirects to `InvalidTenantCode`. If the code is valid but different from the current tenant, it switches tenants. If the app *doesn't* use codes in the URL but one was supplied anyway, it navigates back to the clean root.

The model also *builds* tenant-aware URLs going the other way. `ApplicationUrlFull` (in `DataModel.cs`) returns the app root and, only when `UseTenantCodeInUrl` is on, appends the current tenant's code so generated links stay inside the right tenant's URL space:

```csharp
if (_UseTenantCodeInUrl) {
    if (!output.EndsWith("/")) { output += "/"; }
    if (!String.IsNullOrWhiteSpace(_Tenant.TenantCode)) {
        output += _Tenant.TenantCode + "/";
    } else if (!String.IsNullOrWhiteSpace(_TenantCodeFromUrl)) {
        output += _TenantCodeFromUrl + "/";
    }
}
```

So navigation is tenant-aware in both directions: reading the tenant *out* of an incoming URL, and writing the tenant *into* the links the app generates.

<a id="data"></a>
## 5. Tenant-Awareness in Data

**Why this is the most important section:** the URL is a convenience, but it is *not* the thing that protects data. Data isolation is enforced on the **server**, on every single request, using the `TenantId`. A friendly URL with a tenant code is just decoration; the real security comes from the steps below.

**Step 1 — the client tags every request with the tenant.** Before any API call, the client puts the user's tenant into an HTTP request header. From `CRM.Client/Helpers.cs`:

```csharp
Http.DefaultRequestHeaders.Clear();
// ...
Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());
```

A **header** is just a labeled piece of metadata that travels with an HTTP request. Here the label is `TenantId` and the value is the current user's tenant Guid. (The same value can also arrive as a `TenantId` querystring parameter, which is how things like file-view links carry it.)

**Step 2 — the server reads the tenant back out.** The `DataController` constructor (`CRM/Controllers/DataController.cs`) runs for every request and resolves the tenant before any endpoint executes:

```csharp
// See if a TenantId is included in the header or querystring.
string tenantId = HeaderValue("TenantId");
if (String.IsNullOrEmpty(tenantId)) {
    tenantId = QueryStringValue("TenantId");
}
if (!String.IsNullOrEmpty(tenantId)) {
    try { TenantId = new Guid(tenantId); } catch { }
}
```

For authenticated requests the tenant is *also* taken from the signed-in user's identity claims (a `ClaimTypes.GroupSid` claim holding the tenant Guid), and that tenant is used together with the auth token to load the real user — so the tenant a request operates under is anchored to a verified login, not just to whatever header a browser sent:

```csharp
TenantId = Guid.Empty;
Guid.TryParse(tenantIdString, out TenantId);
CurrentUser = da.GetUserFromToken(TenantId, token, _fingerprint).Result;
```

**Step 3 — endpoints scope and guard by that tenant.** Two patterns repeat throughout the controllers:

*Scoping* — methods pass `CurrentUser.TenantId` down to the data layer so the query only ever touches the caller's tenant. For example, deleting the tenant logo never takes a tenant id from the request body; it uses the authenticated user's tenant:

```csharp
var output = await da.DeleteTenantLogo(CurrentUser.TenantId);
```

*Guarding* — when a method does accept an id, it checks ownership before doing anything. `GetTenant` is the textbook case (`CRM/Controllers/DataController.Tenants.cs`):

```csharp
if (CurrentUser.AppAdmin || (CurrentUser.Admin && CurrentUser.TenantId == id)) {
    var output = da.GetTenant(id, CurrentUser);
    return Ok(output);
} else {
    return Unauthorized(_returnCodeAccessDenied);
}
```

That condition is the isolation guarantee made literal: the application superintendent (`AppAdmin`) may read any tenant, but an ordinary admin may only read the tenant they belong to (`CurrentUser.TenantId == id`). Anything else returns `Unauthorized`.

Put together, the data path is: **client tags the request → server re-derives the tenant from header and verified identity → endpoint scopes the query to that tenant and guards any id it's handed.** Isolation lives in those last two steps, server-side, every time.

<a id="helpers"></a>
## 6. Tenant-Awareness in Helpers and Services

**Why helpers matter:** the same tenant logic is needed on dozens of pages. Rather than copy-pasting it (and getting it subtly wrong somewhere), FreeCRM centralizes the tricky tenant decisions in a few static helper methods in `CRM.Client/Helpers.cs`. Pages call these helpers; they don't reinvent the rules.

**`ValidateUrl(string? TenantCode, bool AutoRedirect = false)`** is the navigation gatekeeper from Section 4. Its real branching, lightly paraphrased from the source:

- If the app **uses** codes in the URL (`Model.UseTenantCodeInUrl`):
  - **No code supplied** → redirect to the configured `DefaultTenantCode` if one exists, otherwise to the `MissingTenantCode` page. (With `AutoRedirect`, if we already have a valid tenant it sends the user back to the rooted home URL.)
  - **Code supplied** → look it up in `Model.TenantList`. If it isn't a real code, redirect to `InvalidTenantCode`. If it is real but differs from the current tenant, call `SwitchTenant(tenant.TenantId)`.
- If the app does **not** use codes in the URL but one was supplied anyway → navigate back to the clean `ApplicationUrl`, forcing a full reload.

A nice detail: `ValidateUrl` is guarded by a `_validatingUrl` flag so two near-simultaneous calls can't trip over each other — it returns early if it's already running.

**`SwitchTenant(Guid TenantId)`** is the helper that actually changes which tenant the app is acting as — used when a user belongs to more than one tenant, or when `ValidateUrl` detects a different valid code. In outline it:

```csharp
Model.NotifyTenantChanging();   // tell the app a switch is happening
Model.TenantId = TenantId;      // update the ambient context
Model.Tenant = tenant;          // swap in that tenant's settings
```

It then rebuilds the correct tenant URL (honoring that tenant's own `ApplicationUrl` setting and appending its `TenantCode` when `UseTenantCodeInUrl` is on) and reloads the right language for the user. The point: switching tenants is one well-tested method, not something a page should attempt by hand. It is also guarded by a `_switchingTenant` flag against re-entry.

**Tenant-aware URL builders** (`ApplicationUrlFull`, `ApplicationUrlFullDefault`) round things out — shown in Section 4 — so any code that needs a link can ask the model for a tenant-correct one instead of string-building it manually.

The lesson for a new author: when you need to read the tenant, read `Model.TenantId`; when you need to validate or change it, call the helper. Don't write tenant-routing logic inline on a page.

<a id="pitfalls"></a>
## 7. Common Pitfalls and Tenant Leaks

A **tenant leak** is any path where one tenant's data, settings, or links reach a different tenant. They are usually quiet — no exception, just wrong data. Here are the realistic ways to cause one in this codebase, and how the existing patterns prevent them.

1. **Trusting an id from the request without a guard.** If an endpoint accepts an `id` and calls the data layer without first checking ownership, any signed-in user could request another tenant's record by guessing or copying an id. The fix is the guard you saw twice already: `CurrentUser.AppAdmin || (CurrentUser.Admin && CurrentUser.TenantId == id)`, returning `Unauthorized(_returnCodeAccessDenied)` otherwise. New endpoints that take an id should follow this shape.

2. **Querying without scoping to the tenant.** A data method that filters by some other field but forgets the tenant can return rows from every tenant at once. The codebase avoids this by passing `CurrentUser.TenantId` (or `CurrentUser`) into data-access calls so the query is bounded to the caller's tenant — e.g. `da.DeleteTenantLogo(CurrentUser.TenantId)`. If you add a query, ask: "what restricts this to one tenant?"

3. **Sending the wrong tenant in the header.** The client adds the `TenantId` header from `Model.User.TenantId`. If you call the server *after* changing tenants but *before* the model has finished switching, you could tag a request with the old tenant. That's exactly why `SwitchTenant` updates `Model.TenantId` (and fires `NotifyTenantChanging`) as a coordinated unit and is re-entrancy-guarded — let it finish; don't fire API calls in the middle of a switch.

4. **Treating the URL code as the security boundary.** The `{TenantCode}` in the URL is convenience and routing, not protection — it's a short, human-readable label, while the real key is the `TenantId` Guid validated server-side. Never assume that because the URL "looks right" the request is safe; the server still re-derives and checks the tenant on every call.

5. **Hand-building tenant URLs.** Concatenating paths yourself can drop the tenant code (breaking deep links when `UseTenantCodeInUrl` is on) or add one when the app doesn't use them. Use `Model.ApplicationUrlFull` and the navigation helpers so the code is included exactly when it should be.

6. **Forgetting to record the URL's code on a page.** Pages set `Model.TenantCodeFromUrl = TenantCode` and call `ValidateUrl(TenantCode)` on first render. Skip those and a deep-linked page can act under the wrong tenant or fail validation. Copy the established page pattern when you add a new tenant-routed page.

The unifying rule: **the tenant is enforced on the server, derived from a verified identity, and carried as ambient context everywhere else.** When in doubt, find where `CurrentUser.TenantId` is used and make sure your new code participates in the same pattern.

---

<a id="related-docs"></a>
## 8. Related Docs

- [082 — One Tenant or Many](082_tenancy-topology.md) — choosing one tenant or many
- [024 — API Controllers: The Tenant-Aware Request Surface](024_api-controllers.md) — where tenant scoping is enforced
- [044 — The Authentication Plugin at the Tenant Edge](044_auth-plugin.md) — how a session learns its tenant

---
*GuidesV2 · 016 · drafted from source 2026-06-05 · grounded in CRM.Client/DataModel.cs, CRM.Client/Helpers.cs, and CRM/Controllers/DataController.cs.*
