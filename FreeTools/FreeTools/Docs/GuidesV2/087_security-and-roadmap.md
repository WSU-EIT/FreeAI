# 087 — Trust and Trajectory

> **Document ID:** 087  ·  **Category:** Operations  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Consolidate the security posture across FreeCRM and FreeTools with the roadmap and upgrade-safety promise.
> **Audience:** Operators and decision-makers  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 08x (Operate, Deploy, and Steward) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why This Matters](#why-this-matters) | Plain-language overview and key term definitions |
| 2 | [What We Protect](#what-we-protect) | Two very different risk profiles: the live app vs. the CLI tools |
| 3 | [Security Controls](#security-controls) | How login, tokens, per-tenant isolation, and encryption actually work |
| 4 | [The Upgrade-Safety Promise](#upgrade-safety-promise) | Why a framework update won't overwrite your code or break you |
| 5 | [Roadmap and Trajectory](#roadmap) | Where the project is heading and how to read its direction |
| 6 | [Operator Decision Guide](#operator-decisions) | When to act, what to verify, and how to roll back |
| 7 | [Incidents and Disclosure](#incidents-disclosure) | Reporting a problem and responsible disclosure |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-this-matters"></a>
## 1. Why This Matters

If you are the person who signs off on running this software for real users, security is the question you cannot delegate away. This doc gives you a plain-language, accurate picture of how the two pieces of this stack protect data — so you can answer "is it safe enough for us?" with evidence instead of a shrug.

A few terms, defined once so the rest reads cleanly:

- **Security posture** = the overall shape of a system's defenses: what it protects, what could go wrong, and what is in place to stop it. "Posture" is the honest summary, including known gaps — not a marketing claim that everything is perfect.
- **Authentication** ("authn") = proving *who you are* (logging in).
- **Authorization** ("authz") = deciding *what you're allowed to do* once you're in.
- **Tenant** = one customer's isolated slice of data. FreeCRM is *multi-tenant*: many customers share one running app, but each one's records are walled off from the others. Most security risk in a multi-tenant app is the risk that wall leaks.
- **Token** = a small signed string the browser carries to prove it already logged in, so it doesn't re-send a password on every request. FreeCRM uses **JWT** ("JSON Web Token"), an industry-standard token format.
- **Upgrade-safety** = the promise that pulling in a newer version of the framework won't silently overwrite the code *you* wrote or break your running app. For a long-lived system this is a security property too: an app you're afraid to update is an app that stays unpatched.

The single most important thing to understand up front: **this stack has two halves with completely different risk levels.** FreeCRM is a live, multi-customer web application that holds real data and real logins — that is where security attention belongs. FreeTools is a set of local command-line helpers that read source code and write reports — its risk is low by design. Treating them as one undifferentiated "the software" leads to either over-worrying about the CLI or under-securing the app. Section 2 separates them.

---

<a id="what-we-protect"></a>
## 2. What We Protect

There are two assets to defend, and they could not be more different.

### 2a. FreeCRM — the live application (the real surface)

This is where the valuable, sensitive things live:

- **User accounts and login secrets.** Local passwords, and the connections to outside login providers (Apple, Google, Microsoft, Facebook, OpenID).
- **Per-customer business data.** Every tenant's records, which must never bleed into another tenant's view.
- **Tenant cryptographic keys.** Each tenant has its *own* unique key pair used to sign its login tokens (Section 3). Those keys are the lock on the tenant wall.
- **Encrypted application settings.** Sensitive configuration values are stored encrypted, not in plain text.

The realistic adversaries here are the ordinary ones for any web app: someone trying to log in as a user they aren't, someone trying to see *another* tenant's data ("cross-tenant access"), someone intercepting a token, and someone reading raw database rows hoping to find plaintext secrets. The controls in Section 3 map directly onto these.

### 2b. FreeTools — the analysis CLI (low risk by design)

FreeTools is a suite of **command-line tools** ("CLI" = command-line interface; programs you run in a terminal, not a website). They inspect a codebase and produce reports. Their risk profile is deliberately small, and this is documented directly in the source security notes:

| Factor | FreeTools status |
|--------|--------|
| Web-facing endpoints | None — CLI tools only |
| Persistent data storage | None — generates files only |
| Authentication system | None — no user accounts |
| Network access | Local only — connects to a `localhost` dev server |
| File-system access | Yes — reads source, writes reports |

Because there is no login, no database, and nothing listening on the public internet, the classic web-app threats simply don't apply. The remaining concerns are about **what ends up in the report files**, not about the tools being attacked:

- **Path leakage** — early versions wrote absolute paths like `C:\Users\username\source\repos\...` into CSV output, exposing machine structure. This is **fixed**: outputs now use relative paths only (e.g. `Components/Pages/Home.razor`).
- **Screenshot content** — the browser-snapshot tool captures whatever is visible on a page, which can include error messages or stack traces. Reviewing screenshots before sharing them is the user's responsibility.
- **HTML/response captures** — the endpoint-poking tool saves page responses, which may contain sensitive content for non-public pages.

The practical takeaway for an operator: **don't commit FreeTools output for sensitive pages.** The source recommends a `.gitignore` covering the run output folder:

```gitignore
# FreeTools outputs (may contain sensitive screenshots)
Docs/runs/
```

The rest of this doc focuses on FreeCRM, because that is where the security decisions that matter actually live.

---

<a id="security-controls"></a>
## 3. Security Controls

Here is how FreeCRM actually defends the assets above. Everything in this section is drawn from the real code, not aspirational.

### 3a. Logging in — many ways to prove who you are

FreeCRM supports **local passwords** plus a menu of outside identity providers. A provider is only switched on if its configuration keys are present — there's no half-configured login lurking by default. From the real provider wiring (`CRM/Classes/CustomAuthIdentity.cs`), each of Apple, Facebook, Google, Microsoft, and OpenID is enabled only when its credentials exist in configuration:

```csharp
if (!String.IsNullOrEmpty(googleClientId) && !String.IsNullOrEmpty(googleClientSecret)) {
    output.Enabled = true;
    output.UseGoogle = true;
}
```

The default sign-in scheme is a **cookie** (a small browser-stored credential), with OpenID Connect as the default challenge scheme:

```csharp
options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
```

**Why this matters to you:** you can offer enterprise customers "log in with your existing Microsoft/Google account" (which means *they* manage password policy, MFA, and offboarding) without forcing every tenant to use it. The provider stays dark until you supply keys.

### 3b. Tokens — proving you're still logged in (and *which* tenant you are)

After login, FreeCRM issues a **JWT** so the browser doesn't keep re-sending a password. The token machinery lives in `CRM.DataAccess/DataAccess.JWT.cs` and uses the `JWTHelpers` NuGet package (version `1.0.1`). The crucial detail is that **each tenant signs tokens with its own unique RSA key pair** — RSA being a standard public/private key scheme where the private key signs and the matching public key verifies:

```csharp
var jwt = new JWTHelper(_appName, StringValue(settings.JwtRsaPublicKey), StringValue(settings.JwtRsaPrivateKey));
var encoded = jwt.Encode(Payload, _tokenDays);
```

Two grounded facts worth knowing as the operator:

- **Tokens expire.** The default lifetime is **7 days** (`_tokenDays = 7`). A stolen token is not valid forever.
- **The token *is* the tenant boundary.** Because every tenant has different keys, a token signed for Tenant A cannot be validated by Tenant B's key. When a request arrives, the code literally tries each active tenant's key until one decrypts the token — the comment in `DataAccess.Users.cs` says it plainly:

```csharp
// Need to try each active Tenant to see which key can decrypt this token.
// Since all RSA keys are unique, the first that decrypts the token and finds a valid user is the valid tenant.
```

The token also carries a `Fingerprint` claim, and `GetUserFromToken` rejects the token if the presented fingerprint doesn't match the one baked in — so a token lifted into a different browser context fails the check. There is also a `SudoLogin` flag for support-style impersonation, which is recorded on the resolved user rather than hidden.

**Why this matters:** the per-tenant key design means tenant isolation isn't just a `WHERE TenantId = ...` filter that a bug could forget — it's enforced cryptographically at the front door. That is a genuinely strong default for a multi-customer system.

### 3c. Encryption at rest — secrets aren't sitting in plaintext

Sensitive values (local passwords, encrypted settings) are encrypted before storage using a symmetric key. The logic is in `CRM.DataAccess/DataAccess.Encryption.cs`, which exposes `Encrypt`, `Decrypt`, `EncryptObject`, and `GetNewEncryptionKey`. The encryption key is read from settings and cached; there is a hard-coded default key as a fallback, which is an important caveat for operators (see Section 6).

The system also supports **key rotation** — changing the encryption key and re-encrypting everything that used the old one. `UpdateApplicationEncryptionKey` walks the encrypted settings *and* every stored user password, decrypts with the old key, and re-encrypts with the new:

```csharp
// Decrypt and re-encrypt all local passwords.
string decrypted = encCurrent.Decrypt(currentValue);
rec.Password = encNew.Encrypt(decrypted);
```

**Why this matters:** if you ever suspect a key is compromised, rotation is a built-in operation, not a manual database surgery.

### 3d. Authorization — what you're allowed to do

Authentication answers *who*; the application then scopes data by tenant and by user. Token resolution is per-tenant (3b), and API access flows through the data-access layer that knows the current tenant — so requests are answered in the caller's tenant context, not a global one. Routes that require a logged-in user are gated with standard `[Authorize]` attributes (FreeTools' route-mapper even detects these to inventory which pages need auth).

---

<a id="upgrade-safety-promise"></a>
## 4. The Upgrade-Safety Promise

Security doesn't end at login — it includes *staying patched without fear*. The biggest practical reason teams run dangerously old software is that upgrading feels risky: "if I pull the new framework version, will it overwrite the code I wrote?" FreeCRM is built so the answer is **no**.

The mechanism (covered in depth in the 04x band) is the **partial-class segregation contract**. In plain terms:

- The framework's files and *your* files are kept physically separate by a strict file-naming rule. Framework code lives in files the framework owns and may replace; your code lives in clearly-named files it will never touch.
- The data-access, data-shape (DTO), and identity classes are **partial classes** — a C# feature where one class is split across multiple files. The framework owns one file; you extend the *same* class in your own file. An upgrade replaces the framework's file and leaves yours intact.

You can see this contract in the very code we've been reading: `DataAccess.JWT.cs`, `DataAccess.Encryption.cs`, and `DataAccess.Users.cs` are all `public partial class DataAccess` — separate files, one logical class. That same pattern is what lets you add your own methods beside them and survive an update.

**Why this matters for security specifically:** an app you can confidently update is an app whose security fixes you'll actually take. The upgrade-safety promise is what converts "we'll patch eventually" into "we patch routinely." The real-world discipline for keeping current is in [054 — Living on a Fork](054_fork-sync-discipline.md) and [084 — Riding the Framework Forward](084_performing-upgrades.md).

---

<a id="roadmap"></a>
## 5. Roadmap and Trajectory

A roadmap is a statement of direction, not a contract of dates. For a framework you intend to live on for years, the trajectory matters more than any single release. Here's how to read where this project is going.

**The shape of the direction:**

- **Upgrade-safety is the load-bearing commitment.** The partial-class/file-naming contract (Section 4) is the project's central promise, which means future framework work is expected to keep arriving *underneath* your code rather than across it. When evaluating any future change, the question to ask is "does this respect the segregation contract?" — if it does, your code is safe by construction.
- **Auth surface is provider-extensible.** The login layer is already structured to add identity providers behind a uniform on/off-by-configuration pattern (Section 3a). New providers slot into the existing shape rather than requiring a redesign.
- **The encryption layer anticipates rotation.** Built-in key rotation (Section 3c) signals an intent to operate over a long horizon, where keys *will* need changing.

**How to track the real trajectory** (rather than guessing): this is a forkable framework, so the authoritative direction signal is the upstream author's repository. The discipline of staying synced with upstream — and reading its changes as the live roadmap — is exactly what [054 — Living on a Fork](054_fork-sync-discipline.md) exists for. Treat upstream commit history and releases as the roadmap of record.

**Honest framing for a decision-maker:** the strongest forward-looking guarantee here is structural (upgrade-safety), not a published feature calendar. If your organization needs contractual delivery dates for specific features, that's a question to settle directly rather than infer from the code. The [081 Fit Test](081_is-it-for-us.md) is the right place to weigh that lock-in and risk before adopting.

---

<a id="operator-decisions"></a>
## 6. Operator Decision Guide

Concrete actions the responsible operator should take, with the *why* attached.

**Before going live with real users:**

1. **Replace the default encryption key.** The encryption layer ships with a hard-coded default key as a fallback. For production, generate a fresh key (`GetNewEncryptionKey` exists for exactly this) and ensure your deployment uses it, not the default. *Why:* a known default key protects nothing.
2. **Confirm each login provider's credentials are real and scoped.** A provider only activates when its config keys are present; make sure the ones you enable use production OAuth apps, not test ones, and that redirect URLs point at your real domain.
3. **Serve everything over HTTPS with valid certificates.** FreeTools talks to `localhost` with *development* certificates by design — that is fine for local analysis but not a model for production. The live app must use proper, trusted TLS. *Why:* tokens and cookies in transit are only as safe as the channel carrying them.
4. **Decide your token lifetime.** The default JWT lifetime is 7 days (`_tokenDays`). Shorten it if your data sensitivity warrants more frequent re-authentication.

**Verify (a quick confidence pass):**

- Try to use a token from one tenant against another — it must fail (the per-tenant RSA design should guarantee this).
- Confirm sensitive settings appear encrypted in the database, not as plaintext.
- Run FreeTools' route inventory and confirm pages you expect to require login actually carry `[Authorize]`.

**Roll back / respond:**

- **Suspect a leaked encryption key?** Use the built-in key rotation (`UpdateApplicationEncryptionKey`) to re-encrypt settings and passwords under a new key.
- **Suspect a stolen token?** Tokens already expire (default 7 days); for immediate effect, rotating the affected tenant's RSA keys invalidates outstanding tokens for that tenant, since they can no longer be verified.
- **Bad framework upgrade?** Because your code is segregated from framework files (Section 4), reverting the framework files to the prior version is a clean rollback that doesn't touch your work. The mechanics are in [084](084_performing-upgrades.md).

---

<a id="incidents-disclosure"></a>
## 7. Incidents and Disclosure

If you or someone else finds a security weakness, how it's reported matters as much as that it's reported. The source guidance for FreeTools states the responsible-disclosure norm, and the same norm applies to FreeCRM:

1. **Do not open a public issue** describing the vulnerability. A public report is a public exploit recipe before there's a fix.
2. **Contact the maintainers directly** and privately.
3. **Provide enough to reproduce it** — details and clear steps — so it can be confirmed and fixed quickly.
4. **Allow time for a fix before any public disclosure.** Coordinated disclosure protects everyone running the software, not just the reporter.

**For your own deployment, decide in advance:**

- *Who* receives a private report (a named owner or shared security inbox), so a finder isn't left guessing.
- *What you publish* from FreeTools runs — keep raw screenshots, HTML captures, and CSVs out of public repos for any non-public page, per the `.gitignore` guidance in Section 2b.
- *Your response loop* — acknowledge, reproduce, fix, then disclose — written down before you need it, not improvised during an incident.

The lowest-drama outcome is a private channel that exists *before* the first report arrives. Set it up now.

---

<a id="related-docs"></a>
## 8. Related Docs

- [044 — The Authentication Plugin at the Tenant Edge](044_auth-plugin.md) — how login establishes who the user is and which tenant they belong to
- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — staying patched, and reading upstream as the roadmap of record
- [081 — The Fit Test: Is This Framework Right for Us?](081_is-it-for-us.md) — risk and lock-in sign-off for adopters
- [084 — Riding the Framework Forward](084_performing-upgrades.md) — taking an upgrade safely and rolling one back
- [088 — Becoming a Steward](088_contributing-back.md) — contributing fixes (including security fixes) back upstream

---
*GuidesV2 087 · Trust and Trajectory · drafted from source 2026-06-05.*
