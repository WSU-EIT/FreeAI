# 044 — The Authentication Plugin at the Tenant Edge

> **Document ID:** 044  ·  **Category:** Guide  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Document the auth plugin and how it establishes the current user and tenant for a session.
> **Audience:** Advanced builders and extenders  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 04x (Extending Without Breaking: The Live Runtime) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it will cover |
|---|---------|--------------------|
| 1 | [Why Auth Lives at the Tenant Edge](#why-it-matters) | Plain-language overview and key terms |
| 2 | [The Identity Resolution Flow](#resolution-flow) | How a login request becomes a known user |
| 3 | [Establishing the Tenant Context](#tenant-context) | How the user is pinned to one tenant via the token |
| 4 | [The Session Object and Its Lifetime](#session-object) | The token cookie, what it carries, and how it is read back |
| 5 | [Plugin Extension Points](#extension-points) | The `IPluginAuth` contract: `Login`, `Logout`, `Properties`, prompts |
| 6 | [Common Pitfalls and Failure Modes](#pitfalls) | Mistakes that break login or tenant isolation |
| 7 | [Verifying and Debugging Auth](#verifying) | Confirming the right user and tenant resolved |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why Auth Lives at the Tenant Edge

**Why it matters:** the very first thing the application must figure out about any visitor is two facts — *who are you* and *which company's data are you allowed to see*. Get either one wrong and you either lock out a legitimate user or, far worse, show one customer another customer's records. An **authentication plugin** ("auth plugin" for short) is the swappable piece of code that answers the *who are you* question against whatever identity system a customer happens to use — their own database, a corporate single sign-on, Okta, Azure AD, anything.

Let's define the key terms in plain language, because the rest of this doc leans on them:

- **User** — a person with an account. In code this is a `DataObjects.User` object that carries an `Email`, a `UserId` (a globally unique `Guid`), a `TenantId`, and profile fields like `FirstName`, `LastName`, and `Username`.
- **Tenant** — one customer/organization whose data is fully walled off from every other tenant. Each tenant has its own `TenantId` (a `Guid`). This product is *multi-tenant*: one running server hosts many tenants side by side, so every piece of data and every login is stamped with a tenant.
- **Session** — the period during which a browser is treated as "logged in as this user in this tenant." Here there is no server-side session store; the session is carried entirely by a signed token stored in a browser cookie (covered in Section 4).
- **Plugin** — a small, self-contained piece of C# the host compiles and runs at runtime without rebuilding the whole app. (The general plugin model is Doc 043; this doc is specifically about plugins of `Type == "Auth"`.)

**Why a plugin instead of built-in login?** The product already has built-in username/password login and standard social providers (Apple, Google, Microsoft, OpenID — see `DataObjects.AuthenticationProviders`). The auth plugin exists for everything those don't cover: a customer who insists logins go through *their* corporate identity provider, or who wants to validate credentials against their own API. Because plugins can be scoped to specific tenants (the `LimitToTenants` property), one customer can get a custom login path without touching anyone else's experience. That is what "at the tenant edge" means — the plugin runs at the boundary where an anonymous browser becomes a known user of one specific tenant.

A crucial security note that shapes everything below: **auth plugins run only on the server, never in the browser.** The shipped reference plugin's documentation states it plainly — the `Login` and `Logout` functions "will only be executed at the server." Credentials and identity decisions never live in client-side code.

---

<a id="resolution-flow"></a>
## 2. The Identity Resolution Flow

**Why it matters:** this is the end-to-end journey from "a visitor clicks a login button" to "the server hands back a logged-in session." If you understand this flow you can debug almost any auth problem, because every failure is just one step of it going wrong.

The whole flow is orchestrated by a single Razor page, `CRM/Pages/PluginAuthentication.cshtml`, reachable at the route `/authorization/plugin`. Here is what it does, in order:

1. **Read the request context.** The page wires the current request, response, and `HttpContext` into the `DataAccess` library, then reads two required query-string values — `TenantId` and the plugin `Name` (with underscores turned back into spaces). A `Fingerprint` value is also read; that is an optional browser-identity string used later to bind the token to one device.

2. **Find the matching plugin.** It looks up the plugin by name *and* type, so only auth plugins qualify:

   ```csharp
   var plugin = plugins.AllPlugins.FirstOrDefault(x =>
       x.Name.ToLower() == strPluginName.ToLower() && x.Type.ToLower() == "auth");
   ```

3. **Collect and validate prompts.** If the plugin declares prompts (for example a username and password box), their values arrive *encrypted* in the URL and are decrypted server-side with `data.DecryptObject<List<PluginPromptValue>>(...)`. Any prompt marked `Required` that has no value short-circuits the page with a "Missing Prompt Value" error.

4. **Validate the tenant.** It calls `data.GetTenant(tenantId)` and refuses to continue unless the tenant exists, its `ActionResponse.Result` is true, and it is `Enabled`. This is the first hard gate against logging into a tenant that doesn't exist or has been turned off.

5. **Run the plugin's `Login` method.** It makes a working copy of the plugin, sets the invoker to `"Login"`, and executes it, passing the return URL, the tenant id, and the `HttpContext`:

   ```csharp
   copy.Invoker = "Login";
   var req = new PluginExecuteRequest {
       Plugin = copy,
       Objects = new object[] { returnUrl, tenantId, _httpContext.HttpContext },
   };
   var result = data.ExecutePlugin(req);
   ```

6. **Interpret the returned user.** The plugin returns an object shaped like a `DataObjects.User`. The page deserializes it and checks for a non-empty `Email`. **The email is the identity key** — everything downstream hinges on it.

The plugin itself decides *how* to verify the credential. The reference plugin `LoginWithPrompts` simply forwards the username/password to the built-in authenticator, but the comments in the source make clear this is the seam you replace:

```csharp
// You would replace this with your own
// code to authenticate a user.
// This could perhaps be validating the credentials against some
// database or API endpoint. Or perhaps you would redirect to another
// site for authentication ...
var user = await da.Authenticate(new CRM.DataObjects.Authenticate {
    TenantId = tenantId,
    Username = username,
    Password = password,
}, fingerprint);
```

If the plugin authenticates successfully it returns `(Result: true, ..., Objects: [user])`; otherwise it returns `Result: false` with human-readable messages, which the page renders as the error shown to the visitor.

---

<a id="tenant-context"></a>
## 3. Establishing the Tenant Context

**Why it matters:** authenticating *who* the person is only solves half the problem. The session must also be permanently stamped with *which tenant* they belong to, and it must be impossible for that stamp to be forged or pointed at someone else's tenant. This is the part of auth that protects tenant isolation.

Once the plugin returns a user with an email, `PluginAuthentication.cshtml` resolves that email to a real account **inside the named tenant**:

```csharp
var exists = await data.GetUserByEmailAddress(tenantId, user.Email);
```

Notice the `tenantId` argument — the lookup is scoped. A plugin can claim "this is alice@example.com" but it cannot smuggle Alice into a tenant she has no account in. Three outcomes follow:

- **Account exists in this tenant** → use its `UserId`.
- **No account, and the tenant requires pre-existing accounts** → the login is denied. This is governed by the tenant setting `RequirePreExistingAccountToLogIn`; when it is on, the page returns *"No local account configured. Please contact the application admin."*
- **No account, and the tenant allows just-in-time provisioning** → a new `DataObjects.User` is created from the plugin's returned fields (first name, last name, email, employee id, department, title, username), with `Admin = false`, `Enabled = true`, and `Source` set to the plugin's name so the account's origin is auditable.

With a real `UserId` in hand, the tenant context is locked in by minting a token *for that specific tenant*:

```csharp
string token = data.GetUserToken(tenantId, userId, fingerprint);
```

The tenant binding is not just a value inside the token — it is cryptographic. `GetUserToken` encodes the payload as a **JWT** (JSON Web Token — a compact, signed string that carries a small set of claims) using that tenant's own unique key:

```csharp
Dictionary<string, object> Payload = new Dictionary<string, object> {
    { "UserId", UserId }
};
// ... optional Fingerprint / SudoLogin claims ...
string output = JwtEncode(TenantId, Payload);
```

So the *user* is carried as the `UserId` claim inside the payload, while the *tenant* is carried by *which key signed the token*. That second mechanism is what makes the binding tamper-proof: as Section 4 shows, the server later identifies the tenant by finding the only key that can validate the token. There is no way to take a token issued for Tenant A and have it read as Tenant B.

---

<a id="session-object"></a>
## 4. The Session Object and Its Lifetime

**Why it matters:** "stay logged in" has to work across page loads and browser restarts, but the server keeps no session table. Knowing exactly what represents a session — and how little it is — tells you what can and cannot go wrong with it.

**The session is a cookie, not a server object.** After a successful login the page writes two cookies:

```csharp
data.CookieWrite("user-token", token);
data.CookieWrite("Login-Method", "Plugin:" + plugin.Name);
```

- **`user-token`** is the JWT from Section 3. It *is* the session. It carries the `UserId` claim and, optionally, a `Fingerprint` claim and a `SudoLogin` flag. It does **not** carry a plaintext `TenantId` claim — the tenant is implied by which key signs it.
- **`Login-Method`** is informational, recording how the user logged in (here `"Plugin:<name>"`) so the UI and audit trail can show the source.

The page also calls `data.UpdateUserLastLoginTime(...)`, refreshes the user from any user-update plugins, and redirects either to the originally requested URL (saved in a `requested-url` cookie) or to the app root.

**Reading the session back — how a request re-learns its user and tenant.** On every later request the server has only the `user-token` cookie. It hands that to `GetUserFromToken`, which performs the clever inverse of token minting. Because each tenant's signing key is unique, the server *tries every active tenant's key in turn* and the one that successfully decodes the token to a valid user is, by definition, the right tenant. The source comment says exactly this:

```csharp
// Need to try each active Tenant to see which key can decrypt this token.
// Since all RSA keys are unique, the first that decrypts the token and finds a valid user is the valid tenant.
var tenants = await GetTenants();
foreach (var tenant in tenants.Where(x => x.Enabled == true)) {
    Dictionary<string, object> decrypted = JwtDecode(tenant.TenantId, Token);
    // ... pull "UserId", optional "Fingerprint", optional "SudoLogin" ...
}
```

So both facts — user and tenant — are recovered from one opaque string: the `UserId` from the payload, the tenant from the matching key. This is why the JWT is self-sufficient and no server-side session store is needed.

**Fingerprint binding.** If a `Fingerprint` was baked into the token at login, `GetUserFromToken` requires the request's fingerprint to match before it returns a user; a mismatch yields an empty (anonymous) user. This binds a stolen token to the device it was issued on, limiting replay if a cookie leaks.

**Lifetime.** The session lasts as long as the cookie and the token's validity. **Logout** is the deliberate end: `PluginAuthenticationLogout.cshtml` (route `/Authorization/PluginAuthenticationLogout`) re-finds the auth plugin, sets `plugin.Invoker = "Logout"`, and runs the plugin's `Logout` method so it can clear any external single-sign-on cookies and redirect back to the app root. The reference plugin's `Logout` simply calls `da.Redirect(url)` back to the base URL.

---

<a id="extension-points"></a>
## 5. Plugin Extension Points

**Why it matters:** these are the exact hooks you implement to plug in a custom identity system. If you match this contract, the orchestration in Sections 2–4 calls your code automatically; you never edit the host.

An auth plugin is a class that implements **`IPluginAuth`**, defined in `CRM/PluginsInterfaces.cs`. The interface requires exactly three things (two methods plus the base `Properties`):

```csharp
public interface IPluginAuth : IPluginBase
{
    Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Login(
        DataAccess da, Plugins.Plugin plugin, string url, Guid tenantId,
        Microsoft.AspNetCore.Http.HttpContext httpContext);

    Task<(bool Result, List<string>? Messages, IEnumerable<object>? Objects)> Logout(
        DataAccess da, Plugins.Plugin plugin, string url, Guid tenantId,
        Microsoft.AspNetCore.Http.HttpContext httpContext);
}
```

Both methods receive the same five arguments, in this order: the `DataAccess` library, the plugin object itself, a URL (the current page for `Login`, the base URL for `Logout`), the `tenantId`, and the live `HttpContext`. Both return the standard plugin tuple: a `bool Result`, an optional list of message strings, and an optional array of returned objects.

**`Properties()`** (from `IPluginBase`) is read once at startup and declares the plugin's metadata. For an auth plugin the type must be `"Auth"`, which is what makes the host treat it as a login option and, internally, default its invoker to `"Login"`. The reference plugin shows the shape:

```csharp
public Dictionary<string, object> Properties() =>
    new Dictionary<string, object> {
        { "Id", new Guid("a5a8354d-f5c6-435e-90e6-2bf86f0b4d35") },
        { "Author", "Brad Wickett" },
        { "Name", "Plugin - Login with Prompts" },
        { "Prompts", new List<PluginPrompt> {
            new PluginPrompt { Name = "Username", Type = PluginPromptType.Text },
            new PluginPrompt { Name = "Password", Type = PluginPromptType.Password },
        }},
        { "Type", "Auth" },
        { "Version", "1.0.0" },
        { "LimitToTenants", new List<Guid> { new Guid("00000000-0000-0000-0000-000000000001") }},
        { "ButtonText", "Plugin - Login with Prompts Example" },
        { "ButtonClass", "btn btn-primary" },
        { "ButtonIcon", "<i class=\"icon fa-solid fa-sign-in-alt\"></i>" },
    };
```

The extension points worth knowing:

- **`Prompts`** — a list of `PluginPrompt` objects describing the login fields to render (a `Username` text box and a `Password` box above). The host collects their values, encrypts them in the URL, decrypts them server-side, and exposes them on `plugin.PromptValues` for your `Login` method to read.
- **`Type = "Auth"`** — the switch that routes this plugin to the login page and nowhere else in the UI.
- **`LimitToTenants`** — the per-tenant scoping list. Present and non-empty means the login button appears only for the listed tenants; absent means all tenants. This is how a custom login path is delivered to one customer only.
- **`ButtonText` / `ButtonClass` / `ButtonIcon`** — control how the login button looks on the sign-in page.
- **`Login` return value** — must contain an object shaped like `DataObjects.User` with, at minimum, a populated `.Email`. If the tenant provisions new accounts on first login, also populate `.FirstName`, `.LastName`, and ideally `.Username`, `.EmployeeId`, and `.DepartmentName`, since those become the new account's fields.

**Important:** inheriting `IPluginAuth` is a convenience, not a hard requirement. The shipped docs note the interfaces "are not required. They are just there to help with implementation." The host actually locates your code by method name (`Login` / `Logout`) via reflection, so a plugin only needs methods with the right names and signatures. Implementing the interface is still strongly recommended — it makes the compiler enforce the contract for you.

---

<a id="pitfalls"></a>
## 6. Common Pitfalls and Failure Modes

**Why it matters:** auth is the one area where a quiet bug can become a security incident. These are the mistakes the source code actively guards against — knowing them tells you what *not* to do.

- **Returning a user without an email.** The whole downstream flow keys off `user.Email`. If your `Login` returns success but leaves `Email` empty, the page treats it as an invalid login. Always populate `.Email`.
- **Assuming a new account will be created.** If the tenant has `RequirePreExistingAccountToLogIn` enabled, an unknown email is rejected outright with *"No local account configured."* Don't rely on auto-provisioning unless you know the tenant allows it.
- **Trying to cross tenants.** The email-to-account lookup is scoped by `tenantId` (`GetUserByEmailAddress(tenantId, user.Email)`). You cannot authenticate someone into a tenant where they have no account. Treat the `tenantId` argument as authoritative and never try to override it.
- **Putting secrets or logic in the browser.** Auth plugins execute only on the server. Prompt values travel encrypted and are decrypted server-side. Never assume client-side code will run, and never weaken that by moving credential handling into a Blazor component.
- **Forgetting the fingerprint contract.** If you mint tokens with a fingerprint but the request can't reproduce the same fingerprint, `GetUserFromToken` returns an anonymous user and the visitor appears logged out. Be consistent: either use fingerprints end to end or not at all.
- **Skipping the tenant-enabled check in custom logic.** The host only proceeds when the tenant is found, has `ActionResponse.Result == true`, and is `Enabled`. If you fan out to your own helper methods, mirror that gate; never act on a tenant you haven't confirmed is active.
- **Plugin with no code or a duplicate/empty `Id`.** A plugin whose `Code` is blank is rejected with an error, and at load time a plugin needs a unique, non-empty `Guid` `Id` to register at all. A copy-pasted plugin that reuses another plugin's `Id` silently won't load.
- **Forgetting `Logout` cleanup.** If your identity provider sets its own cookies or server session, your `Logout` method is the only place to clear them and redirect back. Returning success without that cleanup can leave a user able to silently re-authenticate.

---

<a id="verifying"></a>
## 7. Verifying and Debugging Auth

**Why it matters:** when a login misbehaves you need a quick way to tell *which* step failed — credential check, tenant resolution, account lookup, or token issuance. Each leaves an observable trace.

- **Watch the on-page errors.** `PluginAuthentication.cshtml` renders a specific `<h1>`/`<p>` for every failure stage: *"Unable to find a valid Tenant Id"*, *"Missing Plugin Name"*, *"Plugin … Not Found"*, *"Missing Prompt Value"*, *"The Tenant Id passed does not match a current active tenant"*, *"No local account configured"*, and *"Error - Invalid Login"*. The message tells you exactly which gate stopped the flow.
- **Surface messages from your plugin.** The second element of your return tuple is a `List<string>`. Anything you add there is shown to the visitor on failure (and is invaluable while developing). The reference plugin demonstrates this with entries like `"Missing Username"`, `"Calling Authenticate"`, and `"Authentication Failure"`.
- **Confirm the cookies were set.** A successful login writes `user-token` and `Login-Method`. In the browser dev tools, a present `user-token` cookie means a session was minted; a `Login-Method` of `Plugin:<your-name>` confirms it came from your plugin.
- **Verify the resolved user and tenant.** Because the tenant is recovered by trying each tenant's key (`GetUserFromToken`), a token that resolves to the wrong tenant simply won't validate and the visitor appears logged out. If a user is authenticated but seeing no data, confirm their account actually exists in the intended tenant via `GetUserByEmailAddress(tenantId, email)`.
- **Check account provenance.** Accounts created by an auth plugin carry `Source = plugin.Name`. Inspecting a user's `Source` confirms which plugin (if any) created them and helps distinguish plugin-provisioned users from manually created ones.
- **Test logout end to end.** Hit `/Authorization/PluginAuthenticationLogout?Name=<plugin>&TenantId=<id>` and confirm your `Logout` method ran (any external cookies cleared, redirect to the app root). A logout that doesn't clear an external SSO cookie will silently log the user straight back in.

---

<a id="related-docs"></a>
## 8. Related Docs

- [043 — Pluggable by Design: Authoring Plugins](043_plugin-model.md) — the general plugin model
- [016 — Everything Knows Its Tenant](016_tenant-aware-thinking.md) — how tenant is established
- [087 — Trust and Trajectory](087_security-and-roadmap.md) — security posture

---
*GuidesV2 044 · drafted from source (`CRM/Pages/PluginAuthentication.cshtml`, `PluginAuthenticationLogout.cshtml`, `CRM/PluginsInterfaces.cs`, `CRM/PluginFiles/LoginWithPrompts.cs`, `CRM.Plugins/Plugins.cs`, `CRM.DataAccess/DataAccess.Users.cs`).*
