# 082 — One Tenant or Many

> **Document ID:** 082  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Compare single-tenant and multi-tenant routing and deployment so a team picks the right topology.
> **Audience:** Operators and architects  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why Topology Matters](#why-it-matters) | Plain-language definitions of tenant, routing, and isolation, and the stakes of the choice |
| 2 | [The Two Topologies Side by Side](#topologies-compared) | URL-code routing versus per-tenant host routing, and what each costs |
| 3 | [How Requests Get Routed](#routing) | How a request finds its tenant: the `UseTenantCodeInUrl` switch and the `ValidateUrl` path |
| 4 | [Deployment and Isolation Models](#deployment-isolation) | Why data is always isolated by `TenantId` no matter which topology you pick |
| 5 | [Choosing Your Topology](#decision-procedure) | A short decision procedure with real criteria |
| 6 | [Verifying the Setup](#verification) | Concrete checks that routing and isolation actually work |
| 7 | [Migration and Rollback](#migration-rollback) | Flipping the switch safely and reverting if it goes wrong |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Topology Matters

**Why this matters first:** the choice you make here decides what your customers' URLs look like, how many things you have to deploy, and how easy it is to onboard the next customer. Get it right once and onboarding a new customer is a single database row. Get it wrong and every new customer means new infrastructure.

Three words appear constantly in this doc, so let's pin them down in plain language:

- **Tenant** — one isolated customer (or organization) inside the application. Think "Acme Corp" and "Globex Inc" both using the same installed software but never seeing each other's data. In the code, a tenant is the `Tenant` object and every tenant has a stable `Guid TenantId` plus a short human-friendly `TenantCode` (for example `acme`).
- **Routing** — the act of taking an incoming web request and deciding *which tenant it belongs to* before any data is loaded. The framework has to answer "who is this request for?" on every single page load.
- **Isolation** — the guarantee that tenant A can never read or write tenant B's data. In this framework isolation is **not** a topology choice — it is always on, enforced by `TenantId` on the data. Topology only changes *how the request announces which tenant it is*, not whether tenants are kept apart.

Here is the single most important fact in this whole document, and it surprises most people:

> **This framework is always multi-tenant under the hood.** Every record is partitioned by `TenantId`. There is no "single-tenant build." What you are actually choosing is the **routing topology** — how a browser request is mapped to a tenant — not whether the software supports multiple tenants.

So "one tenant or many" is really a question about the front door, not the building. A one-customer deployment still runs the exact same multi-tenant engine; it just happens to have one tenant in the table and a simpler way of finding it.

The two front doors the framework supports are:

1. **Tenant code in the URL** — every tenant lives at a path like `https://app.example.com/acme/`. One running app, one domain, many tenants distinguished by the code in the path.
2. **Per-tenant host** — each tenant has its own URL (for example `https://acme.example.com/`), configured per tenant. The tenant is identified by *which address the request came in on*.

The rest of this doc explains how each works, what each costs, and how to choose.

---

<a id="topologies-compared"></a>
## 2. The Two Topologies Side by Side

**Why this matters:** the two front doors have very different costs. One is "add a row to a table." The other is "stand up DNS, a certificate, and a tenant setting per customer." Knowing the trade-off up front stops you from picking the heavy option for a problem that didn't need it.

The master switch is a single application-wide setting, `UseTenantCodeInUrl`, a boolean exposed on the data model:

```csharp
/// <summary>
/// Indicates if the app is configured to use tenant codes in the URL
/// </summary>
public bool UseTenantCodeInUrl {
    get { return _UseTenantCodeInUrl; }
    ...
}
```

It is a *global* setting (stored under the empty-Guid global scope, not per tenant), so it applies to the whole installation. Operators flip it from **Settings → App Settings**, where it appears as the `UseTenantCodeInUrl` toggle.

| Concern | Tenant code in URL (`UseTenantCodeInUrl = true`) | Per-tenant host (`UseTenantCodeInUrl = false`) |
|---|---|---|
| What a tenant's address looks like | `https://app.example.com/acme/` | `https://acme.example.com/` (set per tenant) |
| How the tenant is identified | The `{TenantCode}` segment in the URL path | The host/URL the request arrives on, matched against each tenant's `TenantSettings.ApplicationUrl` |
| Onboarding a new tenant | Add the tenant; the code instantly works in the path | Add the tenant **and** configure DNS, a TLS certificate, and the tenant's `ApplicationUrl` |
| Number of deployable units | One app, one domain | Often still one app, but one hostname/cert per tenant to manage |
| Cross-tenant switching for shared users | Cheap — change the path segment; `SwitchTenant` re-points the model | Requires navigating to the other tenant's host |
| Branding by domain (vanity URLs) | Not possible — everyone shares the domain | Natural — each tenant gets its own domain |
| Operational burden | Low | Higher (per-tenant DNS + certs + URL config) |
| Best fit | SaaS with many tenants, fast onboarding | Few high-value tenants who want their own branded domain |

Two important nuances that the table can't capture:

- **A tenant can have its own `ApplicationUrl` even in code-in-URL mode.** Each tenant carries a `TenantSettings.ApplicationUrl`. When set, the framework uses it as that tenant's base address and still appends the tenant code if `UseTenantCodeInUrl` is on. So the two modes are not strictly exclusive — per-tenant URLs are a tenant-level override layered on top of the global switch.
- **Data isolation is identical either way.** Neither mode changes how data is separated. Both route a request to a `TenantId` and then every query is scoped to that id. Topology is purely about the *door*, never the *walls*.

---

<a id="routing"></a>
## 3. How Requests Get Routed

**Why this matters:** routing is the step where a raw URL becomes "this is Acme's request." If routing is wrong, a user lands on the wrong tenant or a "tenant not found" page. Understanding the path makes those failures obvious instead of mysterious.

### 3.1 Reading the tenant code off the URL

Pages that can carry a tenant code declare it as an optional route parameter. The home page is the clearest example:

```razor
@page "/"
@page "/{TenantCode}"
```

That second route means `https://app.example.com/acme/` parses `acme` into a `TenantCode` parameter. The framework stashes whatever it found into the model property `TenantCodeFromUrl`:

```csharp
/// <summary>
/// Gets or sets the TenantCode that was in the URL {TenantCode} parameter.
/// </summary>
public string? TenantCodeFromUrl { get; set; }
```

When the browser first loads, the client asks the server for its data using whichever identity it has. If a tenant code was in the URL but the user is not yet logged in, it calls a tenant-code-specific endpoint:

```csharp
} else if (!String.IsNullOrWhiteSpace(Model.TenantCodeFromUrl)) {
    blazorDataModelLoader = await GetOrPost<DataObjects.BlazorDataModelLoader>(
        "api/Data/GetBlazorDataModelByTenantCode/" + Model.TenantCodeFromUrl);
}
```

On the server, `GetBlazorDataModelByTenantCode(string TenantCode)` returns a limited model for just that one tenant — enough to render a login screen branded for the right tenant without exposing anything else.

### 3.2 The validation gate: `ValidateUrl`

The real routing logic lives in one method, `Helpers.ValidateUrl`. Its own summary describes the rules precisely:

> Validates the current URL based on the `UseTenantCodeInUrl` setting. If the app is configured to use a Tenant Code in the URL, but no Tenant Code is present, then the default code is used if configured. Otherwise, the user is redirected to a page to let them know a Tenant Code is required. If a Tenant Code was included in the URL but the app is not configured to use Tenant Codes in the URL, the user is redirected back to the home page with no Tenant Code in the URL.

In plain terms, four cases:

1. **Code mode ON, no code in URL** → if a `DefaultTenantCode` is configured, redirect there; otherwise send the user to the **MissingTenantCode** page.

   ```csharp
   if (!String.IsNullOrWhiteSpace(Model.AppSettings.DefaultTenantCode)) {
       NavManager.NavigateTo(Model.ApplicationUrl + Model.AppSettings.DefaultTenantCode, true);
   } else {
       NavManager.NavigateTo(Model.ApplicationUrl + "MissingTenantCode");
   }
   ```

2. **Code mode ON, code present but unknown** → send the user to the **InvalidTenantCode** page.

   ```csharp
   var tenant = Model.TenantList.FirstOrDefault(x => x.TenantCode.ToLower() == TenantCode.ToLower());
   if (tenant == null) {
       NavManager.NavigateTo(Model.ApplicationUrl + "InvalidTenantCode");
   }
   ```

3. **Code mode ON, code present and valid, but not the current tenant** → switch to it via `SwitchTenant(tenant.TenantId)`.

4. **Code mode OFF, but a code somehow appeared in the URL** → redirect back to the clean root, forcing a full reload so no stale code lingers:

   ```csharp
   if (!String.IsNullOrWhiteSpace(TenantCode)) {
       NavManager.NavigateTo(Model.ApplicationUrl, true);
   }
   ```

### 3.3 Building outgoing URLs

When the framework writes links back out, `ApplicationUrlFull` reassembles the address, adding the tenant code only when the mode is on:

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

So the same switch that *reads* the code on the way in also *writes* it on the way out. Login links, redirects, and tenant-switch links all go through this and stay consistent.

### 3.4 Helping lost visitors

If a visitor hits the bare domain in code mode with no tenant, the **MissingTenantCode** page can optionally list every tenant so they can pick one. That list only appears when the operator enables `ShowTenantListingWhenMissingTenantCode`:

```razor
@if (Model.ShowTenantListingWhenMissingTenantCode) {
    await GetTenantCodes();
}
```

Each entry is a link to `Model.ApplicationUrl + item.TenantCode`. Leave this off for a private SaaS where you do not want to advertise your customer list; turn it on for an open directory of tenants.

---

<a id="deployment-isolation"></a>
## 4. Deployment and Isolation Models

**Why this matters:** people often assume "single-tenant" means "safer, more isolated." In this framework that assumption is wrong, and acting on it leads to over-engineering. Isolation here does not come from separate deployments — it comes from the data layer, and it is always on.

### 4.1 One engine, many tenants

No matter which topology you choose, you typically deploy **one** application. The same binaries serve every tenant. There is no per-tenant code build and no per-tenant process required. The `UseTenantCodeInUrl` switch and per-tenant `TenantSettings.ApplicationUrl` values are configuration, not separate deployments.

### 4.2 Isolation is by `TenantId`, always

Every tenant has a `Guid TenantId`. That id is the spine of isolation: data belongs to exactly one tenant, and queries are scoped to the current tenant's id. Routing's whole job is to land on the right `TenantId`; once it does, the data layer keeps tenants apart automatically.

This is why the topology choice does not affect security posture. Whether Acme is reached at `/acme/` or at `acme.example.com`, the request resolves to Acme's `TenantId`, and from that point the isolation is identical. Doc [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) covers the tenant-aware data discipline that makes this hold.

### 4.3 Where per-tenant configuration does live

Even though there is one deployment, tenants are not identical. Each tenant carries a `TenantSettings` object that can override behavior — including its own `ApplicationUrl`, allowed file types, authentication options, branding, and more. So "shared infrastructure" does not mean "uniform experience": tenants differ through settings, not through separate servers.

### 4.4 The narrow case for truly separate deployments

If a customer contractually requires their data in a physically separate database or region, you would run a **second installation** of the whole framework for them — a separate deployment with its own database, which itself contains one tenant. That is an infrastructure decision outside the routing switch, and it is the only sense in which "single-tenant deployment" is a real thing here. Reach for it only when a compliance or data-residency requirement forces it, because it multiplies operational cost.

---

<a id="decision-procedure"></a>
## 5. Choosing Your Topology

**Why this matters:** the default that fits most teams is the cheap one, and the expensive options are easy to talk yourself into without need. A short procedure keeps the decision honest.

Work top to bottom and stop at the first match:

1. **Does a customer contract require a physically separate database or data region?**
   → Run a separate installation for that customer (see §4.4). For everyone else, continue with one installation.

2. **Do your tenants need their own branded domains (vanity URLs like `acme.example.com`)?**
   → Use **per-tenant host** routing: leave `UseTenantCodeInUrl` off and set each tenant's `TenantSettings.ApplicationUrl`. Accept the per-tenant DNS + certificate + URL-configuration cost.

3. **Do you onboard tenants frequently and value the cheapest possible setup?**
   → Use **tenant code in URL**: turn `UseTenantCodeInUrl` on. New tenants work the moment they exist, at `https://app.example.com/{code}/`.

4. **Single-customer deployment, no plans for more?**
   → Either mode works. Simplest is `UseTenantCodeInUrl` on with a `DefaultTenantCode` set, so the bare domain silently routes to the one tenant and users never see or type a code.

Two supporting decisions once you have picked:

- **Set a `DefaultTenantCode`** if you want the bare domain to route somewhere instead of showing the MissingTenantCode page.
- **Decide on `ShowTenantListingWhenMissingTenantCode`** — on for a public directory of tenants, off to keep your tenant list private.

When in doubt, pick **tenant code in URL** with a sensible default. It is the lowest-cost option and you can layer per-tenant `ApplicationUrl` overrides on individual tenants later without changing the global mode.

---

<a id="verification"></a>
## 6. Verifying the Setup

**Why this matters:** routing bugs are quiet — the app still loads, just for the wrong tenant or onto an error page. Run these checks after any topology change so a silent misroute does not reach a customer.

### Tenant code in URL mode (`UseTenantCodeInUrl = true`)

1. **Valid code routes correctly.** Visit `https://app.example.com/<validcode>/`. You should land in that tenant. Confirm the page shows that tenant's name/branding, not another's.
2. **Unknown code is rejected.** Visit `https://app.example.com/not-a-real-code/`. You should be redirected to the **InvalidTenantCode** page, never silently dropped into some default tenant.
3. **Missing code behaves as configured.** Visit the bare domain `https://app.example.com/`.
   - With a `DefaultTenantCode` set → you should be redirected to that default tenant.
   - Without one → you should see the **MissingTenantCode** page (and, if enabled, the tenant listing).
4. **Outgoing links carry the code.** Log in, then inspect a generated link (for example the login or a navigation URL). It should include `/<code>/`, proving `ApplicationUrlFull` is appending the code.
5. **Tenant switching works.** For a user with access to more than one tenant, switch tenants and confirm the URL's code segment changes to match.

### Per-tenant host mode (`UseTenantCodeInUrl = false`)

1. **Each host lands on its tenant.** Visit each tenant's configured `TenantSettings.ApplicationUrl` and confirm it resolves to the right tenant.
2. **No stray codes survive.** Manually append a code segment to a URL (for example `/acme/`). The app should redirect you back to the clean root with a full reload — confirming the "code mode off but code present" branch fired.
3. **Certificates and DNS resolve.** Each tenant host should serve a valid TLS certificate. A cert mismatch is the most common per-tenant-host failure.

### Isolation check (both modes)

1. **Cross-tenant read is impossible.** As a user in tenant A, attempt to reach a record or URL belonging to tenant B. You must not see B's data. Because isolation is by `TenantId`, this should hold regardless of topology — but verify it after every routing change, since routing is what selects the id.

---

<a id="migration-rollback"></a>
## 7. Migration and Rollback

**Why this matters:** the topology switch is global and changes everyone's URLs at once. A careless flip can send every user to a "missing tenant" page. The good news: because it is a single setting and data never moves, both the change and the rollback are fast and low-risk if you sequence them right.

### Switching from per-tenant host to tenant code in URL

1. **Pick and reserve codes.** Confirm every tenant has a sensible, unique `TenantCode` — that is what will appear in the path.
2. **Set a `DefaultTenantCode` (optional but recommended).** This keeps the bare domain working during the transition instead of showing MissingTenantCode.
3. **Decide on the tenant listing.** Turn `ShowTenantListingWhenMissingTenantCode` on or off per your privacy needs.
4. **Flip the switch.** Turn `UseTenantCodeInUrl` on in **Settings → App Settings**. The change is global; the model reloads and the app re-points to root so the new routing takes effect (the layout calls `Helpers.NavigateToRoot(true)` when this setting changes).
5. **Communicate new URLs.** Existing per-tenant host links keep working only if those hosts still point at the app; otherwise users must move to `https://app.example.com/<code>/`. Notify tenants of their new path before the flip.
6. **Run the §6 code-mode checks.**

### Switching from tenant code in URL to per-tenant host

1. **Configure each tenant's `ApplicationUrl`** under its `TenantSettings`, and stand up DNS + a TLS certificate for each host **before** flipping.
2. **Flip the switch off.** Turn `UseTenantCodeInUrl` off. Any lingering `/<code>/` URLs now redirect to the clean root automatically.
3. **Run the §6 host-mode checks**, paying special attention to certificates.

### Rollback

Because the topology is a single global setting and **no data is migrated**, rollback is simply flipping `UseTenantCodeInUrl` back and clearing or restoring any `DefaultTenantCode` you changed. There is no schema change to undo. Keep a note of the prior values (the switch, the default code, the listing flag) before you change them so you can restore the exact previous state in seconds. The only thing that does not self-heal is external DNS and certificates you provisioned for per-tenant hosts — leave those in place until you are certain the rollback is permanent, then retire them.

> **Safety note:** test the flip in a non-production environment first if you can. The switch is instant and affects every tenant simultaneously, so there is no partial rollout — practice the sequence once before doing it live.

---

<a id="related-docs"></a>
## 8. Related Docs

- [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) — the tenant-aware mindset
- [083 — Shipping It to Production](083_deployment-shapes.md) — how each topology deploys
- [081 — The Fit Test: Is This Framework Right for Us?](081_is-it-for-us.md) — the fit decision

---
*GuidesV2 · 082 · drafted from source 2026-06-05.*
