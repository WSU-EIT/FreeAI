# 024 — API Controllers: The Tenant-Aware Request Surface

> **Document ID:** 024  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Document how controllers expose data operations, bridge the client wrappers, and enforce tenant scoping.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Controllers Matter](#why-controllers) | Plain-language overview and key term definitions |
| 2 | [Controller Anatomy & Conventions](#anatomy) | The single partial class, routing attributes, naming rules |
| 3 | [Standard Endpoints Reference](#endpoints) | The GET/POST patterns and the typed responses they return |
| 4 | [Bridging to Client Wrappers](#client-bridge) | How `GetOrPost` on the client calls these endpoints |
| 5 | [Tenant Scoping Enforcement](#tenant-scoping) | How `CurrentUser` and `TenantId` are resolved per request |
| 6 | [Errors, Status Codes & Validation](#errors) | `Ok`, `Unauthorized`, the access-denied sentinel, response shapes |
| 7 | [Worked Examples](#examples) | Three end-to-end request walkthroughs with real code |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-controllers"></a>
## 1. Why Controllers Matter

**Why this matters:** the controller is the front door of the server. Every time the browser app wants data — a list of users, a saved file, a login — it sends an HTTP request, and a controller is the first piece of your code that runs. If the front door is sloppy about *who* is knocking and *which company's data* they are allowed to see, every security guarantee downstream falls apart. So this layer is small, repetitive, and boring on purpose: the less it does, the fewer places a mistake can hide.

A few terms, defined once:

- **Controller** — a C# class whose methods (called **actions**) handle incoming HTTP requests. In this codebase there is essentially one: `DataController`. It lives in `FreeCRM\CRM\Controllers\` and is split across many files.
- **Endpoint / route** — the URL that maps to an action method, e.g. `~/api/Data/GetUser/{id}`. The `{id}` part is a placeholder filled in from the URL.
- **Tenant** — one customer/organization in this multi-tenant system. Many companies share one database; a tenant is how their data is kept separate. "Multi-tenant" just means "many customers, one running app." (See [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md).)
- **Tenant scoping** — the rule that a request can only touch data belonging to *its own* tenant. Controllers are where that rule gets enforced at the edge.
- **Client wrapper** — a helper method on the browser side that hides the raw HTTP call so feature code can just say "get me this user." Here that wrapper is `GetOrPost<T>` in `CRM.Client\Helpers.cs`.
- **Data access (`da`)** — the layer below the controller that actually talks to the database. The controller's job is to authenticate, check permissions, then hand off to `da`.

The mental model: **controller = thin guard at the door; data access = the work happening in the back room.** The controller checks your badge, confirms which company you belong to, then passes your request along. It rarely contains business logic itself.

---

<a id="anatomy"></a>
## 2. Controller Anatomy & Conventions

**Why this matters:** if you know the shape of one endpoint, you know the shape of all of them. The repetition is the feature — it makes the surface easy to audit and hard to get subtly wrong.

### One class, many files

There is a single controller class, declared `partial` so it can be spread across many files. The "spine" is `DataController.cs`; each feature area gets its own partial file (`DataController.Users.cs`, `DataController.Tags.cs`, `DataController.FileStorage.cs`, and so on). A **partial class** just means "this one class is written across multiple files and the compiler stitches them together." The spine declares the class and its shared fields:

```csharp
namespace CRM.Server.Controllers;

[ApiController]
public partial class DataController : ControllerBase
{
    private IDataAccess da;
    private DataObjects.User CurrentUser;
    private Guid TenantId = Guid.Empty;
    // ...
}
```

- `[ApiController]` — an attribute that tells ASP.NET Core "this is a Web API controller," turning on JSON handling and automatic model binding.
- `ControllerBase` — the framework base class that gives you helpers like `Ok(...)` and `Unauthorized(...)`.
- `CurrentUser` and `TenantId` — two private fields, resolved once when the controller is constructed (see §5). Every action reads them; no action has to re-do authentication.

### The shape of an action

Almost every endpoint follows the same four-line pattern:

```csharp
[HttpGet]
[Authorize]
[Route("~/api/Data/GetTags")]
public async Task<ActionResult<List<DataObjects.Tag>>> GetTags()
{
    var output = await da.GetTags(CurrentUser.TenantId, CurrentUser);
    return Ok(output);
}
```

Reading it top to bottom:

- `[HttpGet]` / `[HttpPost]` — which HTTP verb this answers. **GET** is used for "fetch" and "delete-by-id" calls (no body, data comes from the URL). **POST** is used when the client sends an object in the request body (saving, filtering, signing in).
- `[Authorize]` — "you must be a logged-in user to call this." Variations narrow it further (see below). `[AllowAnonymous]` means the opposite: no login required (used for login, signup, password reset, public file views).
- `[Route("~/api/Data/...")]` — the URL. The leading `~/` anchors it to the site root so the path is exactly `/api/Data/GetTags` regardless of routing conventions.
- `ActionResult<T>` — the return type. `T` is the typed object that will be serialized to JSON. Here it's `List<DataObjects.Tag>`.
- `await da....` then `return Ok(output)` — the action calls the data layer and wraps the result in `Ok(...)`, which produces an HTTP 200 with the object as JSON.

### Authorization policies

`[Authorize]` can carry a **policy** — a named permission requirement. The names live in one place, the `Policies` class in `FreeCRM\CRM\Classes\CustomAuthenticationHandler.cs`:

```csharp
public static class Policies
{
    public const string Admin = "Admin";
    public const string AppAdmin = "AppAdmin";
    public const string CanBeScheduled = "CanBeScheduled";
    public const string ManageAppointments = "ManageAppointments";
    public const string ManageFiles = "ManageFiles";
    public const string PreventPasswordChange = "PreventPasswordChange";
}
```

So `[Authorize(Policy = Policies.Admin)]` means "logged in **and** an admin." You'll see this on destructive or sensitive endpoints like `DeleteUser` and `SaveTag`.

### Naming conventions

Method name, route name, and verb intent all line up:

| Prefix | Verb | Meaning | Example |
|--------|------|---------|---------|
| `Get…` | GET | fetch one or many | `GetUser`, `GetTags` |
| `Save…` | POST | create or update | `SaveUser`, `SaveTag` |
| `Delete…` | GET | soft-delete by id | `DeleteUser`, `DeleteTag` |
| `Undelete…` | GET | restore by id | `UndeleteFileStorage` |

The route path almost always mirrors the method name: `SaveUser` → `~/api/Data/SaveUser/`. That predictability is why the client wrapper can be so thin.

---

<a id="endpoints"></a>
## 3. Standard Endpoints Reference

**Why this matters:** you rarely need to read a whole partial file. Once you recognize the handful of patterns below, you can predict what any endpoint does from its signature alone.

### Pattern A — GET by id (fetch or delete)

The id arrives in the URL via `{id}` and binds to a `Guid` parameter:

```csharp
[HttpGet]
[Authorize(Policy = Policies.Admin)]
[Route("~/api/Data/DeleteTag/{id}")]
public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteTag(Guid id)
{
    var output = await da.DeleteTag(id, CurrentUser);
    return Ok(output);
}
```

`DataObjects.BooleanResponse` is the standard "did it work?" reply — a `bool Result` plus a `List<string> Messages` (defined in `CRM.DataObjects\DataObjects.cs`).

### Pattern B — GET list (no parameters)

Fetches a collection scoped to the caller's tenant. Note it passes `CurrentUser.TenantId`, never a tenant id from the URL:

```csharp
[HttpGet]
[Authorize]
[Route("~/api/Data/GetTags")]
public async Task<ActionResult<List<DataObjects.Tag>>> GetTags()
{
    var output = await da.GetTags(CurrentUser.TenantId, CurrentUser);
    return Ok(output);
}
```

### Pattern C — POST with a body object

Used for saving and for filtered searches. The body deserializes into a typed parameter:

```csharp
[HttpPost]
[Authorize(Policy = Policies.Admin)]
[Route("~/api/Data/GetUsers")]
public async Task<ActionResult<DataObjects.FilterUsers>> GetUsers(DataObjects.FilterUsers filter)
{
    var output = await da.GetUsersFiltered(filter, CurrentUser);
    return Ok(output);
}
```

`FilterUsers` carries the search/paging criteria *and* receives the results back — a common "filter object round-trips" idiom in this codebase.

### Pattern D — POST anonymous (auth flows)

Login, signup, and password reset are `[AllowAnonymous]` because the caller is not yet logged in:

```csharp
[HttpPost]
[AllowAnonymous]
[Route("~/api/Data/Authenticate")]
public async Task<ActionResult<DataObjects.User>> Authenticate(DataObjects.Authenticate authenticate)
{
    // validates credentials, then issues an AuthToken + cookie
}
```

### Pattern E — raw file streaming

A few endpoints return a file rather than JSON, using `IActionResult` instead of `ActionResult<T>`:

```csharp
[HttpGet]
[AllowAnonymous]
[Route("~/File/View/{id}")]
public async Task<IActionResult> ViewFile(Guid id)
{
    // ...
    return new FileStreamResult(new MemoryStream(fileContent), mimeType);
}
```

### Common response types

| Type | Shape | Used for |
|------|-------|----------|
| `BooleanResponse` | `bool Result` + `List<string> Messages` | delete/save success/failure |
| `SimpleResponse` | `bool Result` + `string? Message` | single short answer (e.g. a display name) |
| `SimplePost` | `string? SingleItem` + `List<string> Items` | tiny generic POST body (e.g. a token) |
| `ActionResponseObject` | base type carrying an `ActionResponse` (a `BooleanResponse`) | rich objects like `User`, `Tenant`, `Tag` |

Because `User`, `Tenant`, and friends inherit `ActionResponseObject`, the client can always check `output.ActionResponse.Result` to know whether the call succeeded, even when the payload is a full object.

---

<a id="client-bridge"></a>
## 4. Bridging to Client Wrappers

**Why this matters:** feature code in the browser never writes raw HTTP. It calls one helper, `GetOrPost<T>`, and that helper is the *only* place the request headers get attached. Understanding it explains how the server knows who you are on every single call.

The wrapper lives in `FreeCRM\CRM.Client\Helpers.cs`. Feature code calls it like this:

```csharp
var loadedUser = await GetOrPost<DataObjects.User>("api/Data/GetUser/" + UserId.ToString());
var result     = await GetOrPost<DataObjects.Tag>("api/Data/GetTag/" + tagId.ToString());
var fromToken  = await GetOrPost<DataObjects.User>(
    "api/Data/GetUserFromToken", new DataObjects.SimplePost { SingleItem = token });
```

The rule is simple: **pass a second argument and it becomes a POST body; omit it and it's a GET.** That mirrors patterns A–D above exactly.

Inside `GetOrPost<T>`, before every call, three headers are set from the in-memory `Model`:

```csharp
Http.DefaultRequestHeaders.Clear();

if (Model != null) {
    Http.DefaultRequestHeaders.Add("TenantId", Model.User.TenantId.ToString());
    // ...
    if (Model.User.AuthToken != "na") {
        Http.DefaultRequestHeaders.Add("Token", Model.User.AuthToken);
    }
    // ...
    Http.DefaultRequestHeaders.Add("Fingerprint", Model.Fingerprint);
}
```

- **`TenantId`** — which tenant the user is currently working in.
- **`Token`** — the user's auth token (a credential proving who they are). `"na"` is a sentinel meaning "no token yet," and it is deliberately not sent.
- **`Fingerprint`** — a value tying the token to this device/session, so a stolen token alone is not enough.

The server reads exactly these three headers when it constructs the controller (§5). This is the whole bridge: the client attaches identity headers, `GetOrPost` chooses GET vs POST by whether there's a body, and the typed `T` matches the endpoint's `ActionResult<T>` so the JSON deserializes cleanly. On any non-success status, `GetOrPost` logs the failure and returns `default` (often `null`), so callers should null-check.

---

<a id="tenant-scoping"></a>
## 5. Tenant Scoping Enforcement

**Why this matters:** this is the security heart of the whole system. Get this wrong and one customer can read another customer's data. The defense is that tenant identity is resolved *once, centrally, from trusted inputs* — not re-derived ad hoc inside each endpoint.

All of it happens in the `DataController` **constructor** in `DataController.cs` — code that runs every time a request creates a controller, before any action method.

### Step 1 — read the tenant from the header or query string

```csharp
string tenantId = HeaderValue("TenantId");
if (String.IsNullOrEmpty(tenantId)) {
    tenantId = QueryStringValue("TenantId");
}
if (!String.IsNullOrEmpty(tenantId)) {
    try {
        TenantId = new Guid(tenantId);
    } catch { }
}
```

The same is done for `Token` and `Fingerprint`. These are the exact three headers the client wrapper set in §4.

### Step 2 — resolve `CurrentUser` from the token

The constructor tries, in order:

1. **App-specific auth** — `Authenticate_App()` (a hook in `DataController.App.cs` for custom token schemes; empty by default).
2. **Cookie auth** — if the request carries an authenticated cookie, it pulls the token, fingerprint, and tenant id out of the cookie's claims and calls `da.GetUserFromToken(...)`.
3. **Header/query token** — if still not resolved and a `Token` was supplied, it loads the user from that token:

```csharp
if (!CurrentUser.ActionResponse.Result && !String.IsNullOrWhiteSpace(Token)) {
    CurrentUser = da.GetUserFromToken(TenantId, Token, _fingerprint).Result;
}
```

The key point: **`CurrentUser` is derived from a verified token, not from anything the action method trusts blindly.** A user who fails all three paths ends up as a blank `DataObjects.User` whose `ActionResponse.Result` is `false`.

### Step 3 — scope every query to `CurrentUser.TenantId`

Tenant-scoped reads pass `CurrentUser.TenantId` (the *trusted* tenant), not a value from the URL:

```csharp
var output = await da.GetTags(CurrentUser.TenantId, CurrentUser);
```

Even if an attacker tampered with the URL, the data layer is told which tenant the *authenticated* user belongs to.

### Step 4 — guard cross-tenant and cross-user access explicitly

Where an endpoint takes an id that *could* belong to another tenant or another user, the action adds an ownership check before doing the work. Two real examples:

```csharp
// A user may only read their own record unless they're an admin:
[Route("~/api/Data/GetUser/{id}")]
public async Task<ActionResult<DataObjects.User>> GetUser(Guid id)
{
    if (CurrentUser.Admin || CurrentUser.UserId == id) {
        return Ok(await da.GetUser(id, false, CurrentUser));
    } else {
        return Unauthorized(_returnCodeAccessDenied);
    }
}

// A tenant may only be read by its own admin (or an app-wide admin):
[Route("~/api/Data/GetTenant/{id}")]
public ActionResult<DataObjects.Tenant> GetTenant(Guid id)
{
    if (CurrentUser.AppAdmin || (CurrentUser.Admin && CurrentUser.TenantId == id)) {
        return Ok(da.GetTenant(id, CurrentUser));
    } else {
        return Unauthorized(_returnCodeAccessDenied);
    }
}
```

Note the **two layers of defense**: the `[Authorize]` policy keeps anonymous and under-privileged callers out, and the in-method `if` enforces "this specific row is yours." Policies handle *roles*; the inline checks handle *ownership of a particular record*.

---

<a id="errors"></a>
## 6. Errors, Status Codes & Validation

**Why this matters:** consistent, predictable failures are what let the thin client wrapper treat every endpoint the same way. There is a small, fixed vocabulary of outcomes.

### Success — `Ok(...)`

`return Ok(output)` produces **HTTP 200** with `output` serialized to JSON. This is the overwhelmingly common path; even "it failed" business outcomes usually return 200 with a `BooleanResponse` whose `Result` is `false` and whose `Messages` explain why. So a 200 does **not** automatically mean the operation succeeded — the client checks `Result` / `ActionResponse.Result`.

### Access denied — `Unauthorized(...)` plus a sentinel

When an in-method ownership check fails, the action returns:

```csharp
return Unauthorized(_returnCodeAccessDenied);
```

`Unauthorized(...)` yields **HTTP 401**. `_returnCodeAccessDenied` is a private constant on the controller:

```csharp
private string _returnCodeAccessDenied = "{{AccessDenied}}";
```

So the body carries a recognizable `{{AccessDenied}}` marker the client can detect. Requests blocked by the `[Authorize]` attribute itself (wrong policy, not logged in) are rejected by the framework before the method body runs, also as 401/403.

### Validation

There is no heavyweight validation framework here. Two lightweight mechanisms do the job:

1. **Guard clauses** at the top of an action — e.g. `Authenticate` only proceeds when both username and password are present:

   ```csharp
   if (authenticate != null && !String.IsNullOrEmpty(authenticate.Username)
       && !String.IsNullOrEmpty(authenticate.Password)) {
       output = await da.Authenticate(authenticate, _fingerprint);
   }
   ```

2. **Messages in the response** — the data layer fills `BooleanResponse.Messages` (or the object's `ActionResponse.Messages`) with human-readable validation errors, which the UI shows to the user.

### The vocabulary in one table

| Outcome | Helper | HTTP status | Body |
|---------|--------|-------------|------|
| Success (or business "no") | `Ok(output)` | 200 | typed JSON; check `Result` |
| Caller doesn't own the record | `Unauthorized(_returnCodeAccessDenied)` | 401 | `{{AccessDenied}}` |
| Blocked by policy / not logged in | (framework, via `[Authorize]`) | 401 / 403 | framework default |
| File not found | `new EmptyResult()` | 200 (empty) | empty |
| File found | `new FileStreamResult(...)` | 200 | raw bytes |

---

<a id="examples"></a>
## 7. Worked Examples

**Why this matters:** seeing a request travel end-to-end ties §4 (client) and §5 (server) together. Here are three real flows, top to bottom.

### Example 1 — load one user (GET with ownership check)

1. **Client** calls `await GetOrPost<DataObjects.User>("api/Data/GetUser/" + UserId)`. No body → it's a GET. `GetOrPost` attaches the `TenantId`, `Token`, and `Fingerprint` headers.
2. **Constructor** reads those headers, resolves `CurrentUser` from the token, sets `TenantId`.
3. **Action** `GetUser(Guid id)` runs. It checks `CurrentUser.Admin || CurrentUser.UserId == id`. A normal user fetching their own record passes; fetching someone else's returns `Unauthorized(_returnCodeAccessDenied)`.
4. On success it calls `await da.GetUser(id, false, CurrentUser)` and returns `Ok(output)` — HTTP 200, the `User` as JSON.
5. **Client** deserializes into `DataObjects.User`; feature code reads `loadedUser.ActionResponse.Result` to confirm.

### Example 2 — save a tag (POST, admin-only)

1. **Client** calls `await GetOrPost<DataObjects.Tag>("api/Data/SaveTag", tag)`. A body is present → POST. The `tag` object is sent as JSON.
2. **Action** is guarded by `[Authorize(Policy = Policies.Admin)]`, so a non-admin is rejected by the framework (401/403) before any code runs.
3. For an admin, the body binds to `DataObjects.Tag tag`, and the action calls `await da.SaveTag(tag, CurrentUser)`:

   ```csharp
   [HttpPost]
   [Authorize(Policy = Policies.Admin)]
   [Route("~/api/Data/SaveTag")]
   public async Task<ActionResult<DataObjects.Tag>> SaveTag(DataObjects.Tag tag)
   {
       var output = await da.SaveTag(tag, CurrentUser);
       return Ok(output);
   }
   ```

4. `CurrentUser` carries the trusted tenant, so the save lands in the right tenant regardless of anything in the payload. Returns `Ok(output)` with the saved tag (its `ActionResponse` reports success or validation messages).

### Example 3 — log in (POST, anonymous)

1. **Client** posts credentials to `~/api/Data/Authenticate`. This endpoint is `[AllowAnonymous]` because no one is logged in yet.
2. **Action** guards that username and password are both present, then calls `da.Authenticate(authenticate, _fingerprint)`.
3. On success it mints an `AuthToken` and registers a sign-in cookie:

   ```csharp
   if (output.ActionResponse.Result && output.Enabled && context != null) {
       if (String.IsNullOrWhiteSpace(output.AuthToken)) {
           output.AuthToken = da.GetUserToken(output.TenantId, output.UserId, _fingerprint, output.Sudo);
       }
       await CustomAuthorization.AddAuthetication(output, context, _fingerprint, "local");
   }
   return Ok(output);
   ```

4. **Client** stores the returned `AuthToken` on `Model.User`, and from then on `GetOrPost` attaches it as the `Token` header on every subsequent call — which is exactly what Example 1 and 2 relied on.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the full stack overview
- [012 — Wrapped Navigation, HTTP, Localization, and Serialization](012_wrapped-plumbing.md) — client wrappers call these endpoints
- [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) — tenant scoping enforced here
- [017 — Following a Click to the Database](017_click-to-database.md) — traces one request through the controller and on to the data layer
- [085 — When Things Go Sideways](085_diagnostics-playbook.md) — diagnosing failed or denied API calls

---
*GuidesV2 024 · drafted from source ( FreeCRM\CRM\Controllers\DataController*.cs ) · 2026-06-05.*
