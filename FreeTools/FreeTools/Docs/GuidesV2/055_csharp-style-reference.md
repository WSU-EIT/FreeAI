# 055 — The C# Style Reference

> **Document ID:** 055  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** The definitive, proof-backed reference for how C# is written in FreeCRM — whitespace, naming, types, nullability, members, files, methods, control flow, comments, and error handling.
> **Audience:** New contributors (especially a first-week intern)  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [How to Read This Doc](#how-to-read) | What "style" means, the intern-CTO contract, and that every rule here is proven against real source |
| 2 | [Whitespace, Indentation & Line Endings](#whitespace) | 4 spaces, no tabs, CRLF, no final newline, one blank line between members |
| 3 | [Braces & Block Layout](#braces) | Where `{` goes for each construct, and the `) {` wrapped-condition quirk |
| 4 | [Naming: Types, Methods, Properties, Locals, Constants](#naming-types) | PascalCase for the big stuff, camelCase for locals, `I`-prefixed interfaces |
| 5 | [Naming: Fields & the Underscore System](#naming-fields) | When a private field gets `_`, the client/server casing split, PascalCase parameters |
| 6 | [Types & Literals](#types-literals) | `var` vs explicit types, `String.Empty`, predefined keyword types, concatenation over interpolation |
| 7 | [Nullability](#nullability) | NRT, `?`, `= null!`, the null-forgiving `!`, why `??`/`?.` are rare |
| 8 | [Properties & Fields](#properties-fields) | Auto-properties, initializers, `readonly`, `const`, full backing-field properties |
| 9 | [Member Ordering](#member-ordering) | The alphabetical-vs-by-purpose convention that changes per layer |
| 10 | [Partial Classes, the `.App.` Convention & File Organization](#partials) | One class split across many files, and the upgrade-safe `.App.` extension seam |
| 11 | [Using Directives & Namespaces](#usings) | File-scoped namespaces, alphabetical (System-last) usings, global usings |
| 12 | [Methods: Signatures, Async & Results](#methods) | `async Task`, no `Async` suffix, `output` returns, thin controllers |
| 13 | [Control Flow & Expressions](#controlflow) | `if`/`foreach`/`switch`, no pattern matching, LINQ method syntax, initializers |
| 14 | [Comments, XML Docs, Regions & Attributes](#comments) | `///` summaries, `//` sentences, `#region`, stacked attributes, `[Sensitive]` |
| 15 | [Error Handling & Result Objects](#errors) | `BooleanResponse`, `RecurseException`, when to swallow and when to surface |
| 16 | [Razor / Blazor: the C# parts](#razor) | `@code` member ordering and what lives in the sibling doc 056 |
| 17 | [Quick Reference](#quick-reference) | The most-used rules as ✗/✓ tables you can copy |
| 18 | [FAQ](#faq) | "Wait, why do we do *that*?" — the surprising rules an intern actually asks about |
| 19 | [Related Docs](#related-docs) | Parent, sibling, and forward-link references |

---

<a id="how-to-read"></a>
## 1. How to Read This Doc

**What "style" means.** *Style* is the set of cosmetic choices that do not change what a program *does* — where a curly brace lands, whether a name starts with a capital letter, whether you write `""` or `String.Empty`. The compiler ignores all of it. Humans do not. When every file follows the same style, code reviews stay focused on logic, automated merges from the upstream template produce fewer conflicts, and any file reads like it was written by one person.

**The intern-CTO contract.** This document assumes you are smart and have full authority, but that your C#/.NET depth is shallow. So every term is defined the first time it appears, and every rule leads with *why it matters* before the detail. You should not need to know any C# beyond basic programming to follow along. A few terms we will lean on constantly, defined up front:

- A **field** is a variable that belongs to a class (it lives for the whole life of the object), as opposed to a **local variable**, which is declared inside one method and disappears when that method returns.
- A **property** is a class member that *looks* like a field when you use it but can *run code* when read or written (it has `get`/`set` blocks). An **auto-property** is the short form `{ get; set; }` where the compiler writes that code for you.
- A **method** is a named block of behavior (a function). A **parameter** is an input listed in a method's signature; an **argument** is the value you pass for it.
- A **type** is a class, struct, interface, or enum. An **interface** is a contract — a list of method signatures a class promises to provide.
- **PascalCase** capitalizes the first letter of every word (`AppointmentNoteId`); **camelCase** is the same but with a lowercase first letter (`connectionString`).

**These rules are proven, not opinions.** Most style guides are somebody's preference. This one was reverse-engineered from the real FreeCRM source and then *citation-verified at the byte level* — someone opened every cited file and confirmed each line and snippet matches. The studied projects are the four hand-written ones: `CRM`, `CRM.Client`, `CRM.DataAccess`, and `CRM.DataObjects` (118 hand-written `.cs` files total). Two areas are explicitly **out of scope as style sources** and you should never copy their style:

- **`CRM.EFModels`** — machine-scaffolded Entity Framework model classes. (Entity Framework, or "EF," is the library that maps C# objects to database tables.)
- **`CRM.Client/DynamicBlazorSupport/`** (namespace `Try.Core`) — vendored third-party code adapted from an open-source Blazor REPL. It uses a different brace style (Allman), `init` setters, `switch` expressions, and collection-expression `[]` syntax — none of which are FreeCRM house style.

Throughout this doc, **every rule carries a clickable proof link** to the real source, e.g. [DataAccess.cs:148](FreeCRM/CRM.DataAccess/DataAccess.cs#L148). If a rule is not in here, it is not proven — do not assume it.

---

<a id="whitespace"></a>
## 2. Whitespace, Indentation & Line Endings

These rules are invisible (you cannot see a tab vs. spaces on screen) but they show up in every diff, so getting them right keeps the version-control history clean.

**Rule 2a — Indent with exactly 4 spaces per level; never tabs.** Each nesting level adds 4 spaces. A byte-level histogram of every indented line across the core files found only multiples of 4 (widths 4 through 36), with zero 2- or 3-space anomalies. The few tab characters that exist live *inside string literals* (raw SQL and embedded SVG data), never as code indentation.
[RouteHelper.cs:8-17](FreeCRM/CRM/Classes/RouteHelper.cs#L8-L17)

```csharp
public RouteHelper(HttpContext? httpContext)
{
    _context = httpContext;
    _routeInfo = new RouteInformation();
    string path = String.Empty;

    if (_context != null) {
        try {
            path += _context.Request.Path;
        } catch { }
```

**Rule 2b — Use CRLF (Windows) line endings everywhere.** Every line ends with carriage-return + line-feed (`\r\n`), not a bare `\n`. Verified by byte counts: across all 118 files the CR byte count equals the LF byte count exactly, and zero files contain a bare LF. This is invisible on screen but real in the bytes. (Enforced mechanically by the `.editorconfig`; see [053](053_editorconfig-enforcement.md).)

**Rule 2c — No final newline at end of file.** The file ends immediately after the last line's content (the closing `}`) — there is no trailing blank line. Byte-level: 0 of 118 files end with a newline; the final three bytes of every core file are `CR LF }`.
[RouteHelper.cs:93-99](FreeCRM/CRM/Classes/RouteHelper.cs#L93-L99)

**Rule 2d — Separate members with exactly one blank line.** Put a single blank line between consecutive members (methods, properties, field groups, nested types); never stack two or three. This is the strong dominant convention but is *not* mechanically enforced — 16 of 118 files contain an accidental double (or, at [DataModel.cs:1738](FreeCRM/CRM.Client/DataModel.cs#L1738), triple) blank line. Treat those as drift, not a pattern.
[DataController.Tags.cs:20-33](FreeCRM/CRM/Controllers/DataController.Tags.cs#L20-L33)

```csharp
    public async Task<ActionResult<DataObjects.Tag>> GetTag(Guid id)
    {
        var output = await da.GetTag(id, CurrentUser);
        return Ok(output);
    }

    [HttpGet]
    [Authorize]
    [Route("~/api/Data/GetTags")]
    public async Task<ActionResult<List<DataObjects.Tag>>> GetTags()
    {
        var output = await da.GetTags(CurrentUser.TenantId, CurrentUser);
        return Ok(output);
    }
```

**Rule 2e — `#region` / `#endregion` hug their content.** Put `#region` directly above the first member it contains (no blank line after it) and `#endregion` directly below the last member (no blank line before it); keep one blank line *outside* the region. Regions are rare (see [§14](#comments)).
[DataModel.cs:6-45](FreeCRM/CRM.Client/DataModel.cs#L6-L45)

**Rule 2f — No trailing whitespace.** Do not leave spaces or tabs after the last visible character on a line. Followed in practice (only 7 of 118 files have any, all incidental — a stray space in a doc comment or a whitespace-only line inside a raw SQL block).

---

<a id="braces"></a>
## 3. Braces & Block Layout

A **brace** is the curly bracket `{` `}` that opens and closes a block of code. The single most distinctive thing about FreeCRM is *where the opening brace goes*, and the rule depends on what you are declaring. This is **K&R / "One True Brace" style**: the brace drops to its own line only for the "big" declarations (type, method/constructor), and stays on the same line for everything smaller (properties, control flow, lambdas, initializers).

**Rule 3a — Namespaces are file-scoped (no braces at all).** Declare the namespace with a semicolon at the top of the file: `namespace CRM;`. This is the modern default and removes one indentation level from the whole file.
[DataAccess.cs:1-4](FreeCRM/CRM.DataAccess/DataAccess.cs#L1-L4)

```csharp
namespace CRM;

public partial class DataAccess : IDisposable, IDataAccess
{
```

**Rule 3b — Types (class / enum) put the opening brace on its OWN new line.**
[DataObjects.cs:3-11](FreeCRM/CRM.DataObjects/DataObjects.cs#L3-L11)

```csharp
public class SensitiveAttribute : System.Attribute { }

public partial class DataObjects
{
    public enum DeletePreference
    {
        Immediate,
        MarkAsDeleted,
    }
```

(An empty type body may collapse inline with same-line braces: `public class SensitiveAttribute : System.Attribute { }`. The new-line brace applies when the body is non-empty.)

**Rule 3c — Methods and constructors put the opening brace on their OWN new line.**
[DataAccess.Encryption.cs:140-145](FreeCRM/CRM.DataAccess/DataAccess.Encryption.cs#L140-L145)

```csharp
    public string Encrypt(string? input)
    {
        string output = String.Empty;

        if (!String.IsNullOrEmpty(input)) {
            var e = new Encryption.Encryption(GetEncryptionKey);
```

**Rule 3d — Properties put the opening brace on the SAME line.** This is the deliberate contrast with methods. Auto-property accessors stay inline (`{ get; set; }`); full accessor bodies also open with same-line braces.
[DataObjects.cs:40-45](FreeCRM/CRM.DataObjects/DataObjects.cs#L40-L45) (auto), [DataModel.cs:1338-1347](FreeCRM/CRM.Client/DataModel.cs#L1338-L1347) (full body)

```csharp
    public List<Plugins.Plugin> Plugins {
        get { return _Plugins.Where(x => x.LimitToTenants.Count() == 0 || x.LimitToTenants.Contains(_TenantId)).ToList(); }
        set {
            if (!ObjectsAreEqual(_Plugins, value)) {
                _Plugins = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }
```

**Rule 3e — Control-flow statements put the opening brace on the SAME line, with a space after the keyword.** This covers `if`, `for`, `foreach`, `while`, `using`, `switch`, and `try`. Always a space between the keyword and the `(`.
[DataAccess.Migrations.cs:18-26](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L18-L26)

```csharp
        var migrations = DatabaseGetMigrations();
        if (migrations.Any()) {
            foreach (var migration in migrations) {
                if (!appliedMigrations.Contains(migration.MigrationId)) {
                    if (migration.Migration.Any()) {
                        foreach (var step in migration.Migration) {
                            try {
                                data.Database.ExecuteSqlRaw(step);
                            } catch (Exception ex) {
```

**Rule 3f — `else`, `else if`, and `catch` hug the preceding close brace on one line.** Write `} else {`, `} else if (...) {`, `} catch (...) {` — never on a new line. (No `} finally {` exists in the hand-written code, but by the rule it would follow the same form.)
[DataAccess.cs:96](FreeCRM/CRM.DataAccess/DataAccess.cs#L96), [DataAccess.cs:111](FreeCRM/CRM.DataAccess/DataAccess.cs#L111), [DataAccess.Ajax.cs:40](FreeCRM/CRM.DataAccess/DataAccess.Ajax.cs#L40)

**Rule 3g — Lambdas and initializers put the brace on the SAME line.** A multi-statement lambda opens `=> {` on the same line; an object/collection initializer keeps `{` on the same line as `new ...`, one element per line.
[ChangePassword.razor:157-161](FreeCRM/CRM.Client/Pages/ChangePassword.razor#L157-L161) (lambda), [DataController.cs:38-47](FreeCRM/CRM/Controllers/DataController.cs#L38-L47) (initializer)

```csharp
        Action<string> onPasswordAccepted = (string password) => {
            _passwordReset.NewPassword = password;
            _confirmPassword = password;
            StateHasChanged();
        };
```

**Rule 3h — THE QUIRK: when a multi-line CONDITION wraps, the trailing `)` and `{` collapse onto ONE line (`) {`).** If an `if` condition is split across several lines (one boolean term per line), the closing `)` drops to its own line and the opening brace is pulled up beside it, so the line reads just `) {`.
[DataAccess.ApplicationSettings.cs:106-113](FreeCRM/CRM.DataAccess/DataAccess.ApplicationSettings.cs#L106-L113)

```csharp
        if (
            originalValues.ApplicationURL != updatedValues.ApplicationURL ||
            originalValues.DefaultTenantCode != updatedValues.DefaultTenantCode ||
            originalValues.MaintenanceMode != updatedValues.MaintenanceMode ||
            originalValues.ShowTenantListingWhenMissingTenantCode != updatedValues.ShowTenantListingWhenMissingTenantCode ||
            originalValues.UseTenantCodeInUrl != updatedValues.UseTenantCodeInUrl ||
            originalJson != updatedJson
            ) {
```

**Important counter-point:** this `) {` collapse appears **only on wrapped multi-line `if` conditions, never on a method/constructor signature.** When a *method or constructor's* parameter list wraps, FreeCRM does NOT collapse — it either keeps the last parameter and `)` together with the `{` on its own next line, or puts `)` alone then `{` alone:

```csharp
    public DataAccess(
        string ConnectionString = "",
        ...
        bool UseBackgroundService = false
    )
    {
```
[DataAccess.cs:23-31](FreeCRM/CRM.DataAccess/DataAccess.cs#L23-L31), [DataController.cs:24-30](FreeCRM/CRM/Controllers/DataController.cs#L24-L30)

So: the `) {` one-line collapse is a **control-flow-condition** habit, not a method-signature habit.

**Summary table.**

| Construct | Opening brace | Proof |
|---|---|---|
| namespace | none (file-scoped `;`) | [DataAccess.cs:1](FreeCRM/CRM.DataAccess/DataAccess.cs#L1) |
| class / enum | **new line** | [DataObjects.cs:3-11](FreeCRM/CRM.DataObjects/DataObjects.cs#L3-L11) |
| method / constructor | **new line** | [DataAccess.Encryption.cs:140](FreeCRM/CRM.DataAccess/DataAccess.Encryption.cs#L140) |
| property (and accessors) | **same line** | [DataModel.cs:1338](FreeCRM/CRM.Client/DataModel.cs#L1338) |
| if / for / foreach / while / using / switch / try | **same line** | [DataAccess.Migrations.cs:19](FreeCRM/CRM.DataAccess/DataAccess.Migrations.cs#L19) |
| else / else if / catch | **same line as preceding `}`** | [DataAccess.cs:96](FreeCRM/CRM.DataAccess/DataAccess.cs#L96) |
| lambda (statement body) | **same line as `=>`** | [ChangePassword.razor:157](FreeCRM/CRM.Client/Pages/ChangePassword.razor#L157) |
| object / collection initializer | **same line as `new`** | [DataController.cs:38](FreeCRM/CRM/Controllers/DataController.cs#L38) |
| wrapped multi-line `if` condition | `) {` collapse | [DataAccess.ApplicationSettings.cs:106-113](FreeCRM/CRM.DataAccess/DataAccess.ApplicationSettings.cs#L106-L113) |
| wrapped method params | **does NOT collapse**; `{` on its own line | [DataAccess.cs:23-31](FreeCRM/CRM.DataAccess/DataAccess.cs#L23-L31) |

---

<a id="naming-types"></a>
## 4. Naming: Types, Methods, Properties, Locals, Constants

**Casing** is the capitalization pattern of a name, and FreeCRM is mostly standard .NET with a couple of deliberate twists (the twists are in [§5](#naming-fields)). This section covers the reliable, predictable cases.

**Rule 4a — Classes, structs, and partial classes use PascalCase.** `DataAccess`, `BooleanResponse`, `TenantSettings`. Almost every model class is also declared `partial` (a keyword that lets one class be split across multiple files; see [§10](#partials)), but that does not change the casing.
[DataAccess.cs:3-6](FreeCRM/CRM.DataAccess/DataAccess.cs#L3-L6)

**Rule 4b — Interfaces use PascalCase with an `I` prefix.** `IDataAccess`, `IConfigurationHelper`, `IPlugins`. The `I` makes the type instantly recognizable as a contract.
[DataAccess.Tags.cs:3-9](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L3-L9)

```csharp
public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> DeleteTag(Guid TagId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false);
    Task<DataObjects.Tag> GetTag(Guid TagId, DataObjects.User? CurrentUser = null);
    Task<List<DataObjects.Tag>> GetTags(Guid TenantId, DataObjects.User? CurrentUser = null);
    Task<DataObjects.Tag> SaveTag(DataObjects.Tag tag, DataObjects.User? CurrentUser = null);
}
```

**Rule 4c — Enum types AND enum members use PascalCase.** An **enum** is a fixed list of named values. Both the enum name and each member are PascalCase (not `UPPER_SNAKE_CASE`), one member per line, with no explicit numeric values.
[DataObjects.cs:13-25](FreeCRM/CRM.DataObjects/DataObjects.cs#L13-L25)

```csharp
    public enum SettingType
    {
        Boolean,
        DateTime,
        EncryptedObject,
        EncryptedText,
        Guid,
        NumberDecimal,
        NumberDouble,
        NumberInt,
        Object,
        Text
    }
```

(The trailing comma after the last member is inconsistent — sometimes present, sometimes not — but the casing is always PascalCase.)

**Rule 4d — Methods use PascalCase and are usually verb-first.** `GetTag`, `SaveTag`, `DeleteTag` — the verb makes the action obvious at the call site. Some conversion helpers are named for what they produce (`CamelCase`, `BooleanValue`, `GuidValue`) rather than a leading verb, but they are still PascalCase.
[DataAccess.Tags.cs:13-17](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L13-L17)

**Rule 4e — Properties use PascalCase.** `TenantId`, `FirstName`, `LastModifiedBy`. These names are also serialized to JSON for the Blazor client, so consistent PascalCase keeps the API shape predictable.
[DataObjects.Tags.cs:12-19](FreeCRM/CRM.DataObjects/DataObjects.Tags.cs#L12-L19)

A handful of properties deliberately break this in `DataObjects.cs`: the user-defined-field properties `udf01`…`udf10`, `url` on `MenuItem`, `filterUsers`/`filterInvoices`/`filterFileStorage` on `UserPreferences`. Acronyms are kept uppercase as a trailing token (`ApplicationURL`, `UseSSL`) but DB brand names mix styles (`MySQL_Server`, `PostgreSql_Host`, `SqlServer_Server`) — so for acronym casing, **mirror the nearest existing name rather than assume one rule**.
[DataObjects.cs:132-141](FreeCRM/CRM.DataObjects/DataObjects.cs#L132-L141)

**Rule 4f — Local variables use camelCase.** `output`, `rec`, `now`, `tenantId`, `tenantSettings`. The near-universal name for a method's return value is `output`. Use `var` when the type is obvious from the right-hand side, an explicit type otherwise (see [§6](#types-literals)).
[DataAccess.Tags.cs:15-25](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L15-L25)

```csharp
        var output = new DataObjects.BooleanResponse();

        var rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId);
        if (rec == null) {
            output.Messages.Add("Error Deleting Tag '" + TagId.ToString() + "' - Record No Longer Exists");
            return output;
        }

        var now = DateTime.UtcNow;
        Guid tenantId = GuidValue(rec.TenantId);
        var tenantSettings = GetTenantSettings(tenantId);
```

(There are a few stray violations, e.g. a PascalCase local `int Minutes = ...` at [Utilities.cs:41](FreeCRM/CRM.DataAccess/Utilities.cs#L41) — treat that as drift, not the rule.)

**Rule 4g — Constants (`const`) use PascalCase, NOT `ALL_CAPS`.** A `const` is a fixed compile-time value. `public const string Admin = "Admin";`. The team treats constants like any other member.
[CustomAuthenticationHandler.cs:72-82](FreeCRM/CRM/Classes/CustomAuthenticationHandler.cs#L72-L82)

```csharp
public static class Policies
{
    public const string Admin = "Admin";
    public const string AppAdmin = "AppAdmin";
    // {{ModuleItemStart:Appointments}}
    public const string CanBeScheduled = "CanBeScheduled";
    public const string ManageAppointments = "ManageAppointments";
    // {{ModuleItemEnd:Appointments}}
    public const string ManageFiles = "ManageFiles";
    public const string PreventPasswordChange = "PreventPasswordChange";
}
```

---

<a id="naming-fields"></a>
## 5. Naming: Fields & the Underscore System

This is where the deliberate twists live. The underscore-prefix rule for fields and the PascalCase rule for parameters are *house conventions for consistency* — none is forced by C#. Learn the exceptions, because they are intentional, not sloppiness.

**Rule 5a — Server-side private backing fields use `_camelCase`.** In `CRM` and `CRM.DataAccess`, a private field gets a leading underscore and a lowercase first letter: `_connectionString`, `_appName`, `_fingerprint`. The underscore lets you tell at a glance that a name is a class field, not a local or parameter.
[DataAccess.cs:3-11](FreeCRM/CRM.DataAccess/DataAccess.cs#L3-L11)

```csharp
public partial class DataAccess : IDisposable, IDataAccess
{
    private DataObjects.AuthenticationProviders? _authenticationProviders;
    private string _connectionString;
    private string _cookiePrefix = String.Empty;
    private EFDataModel data;
    private string _databaseType;
    private bool _firstInit = true;
    private Guid _guid1 = new Guid("00000000-0000-0000-0000-000000000001");
```

**Rule 5b — Client-side (Blazor) private backing fields use `_PascalCase`.** In `CRM.Client`, the `BlazorDataModel` fields use a leading underscore plus an *uppercase* first letter — the opposite casing from the server. Each field backs a public property of the same name minus the underscore (`_ActiveUsers` backs `ActiveUsers`), so PascalCase makes the field↔property pairing line up character-for-character.
[DataModel.cs:51-56](FreeCRM/CRM.Client/DataModel.cs#L51-L56), [DataModel.cs:121-130](FreeCRM/CRM.Client/DataModel.cs#L121-L130)

```csharp
private List<DataObjects.ActiveUser> _ActiveUsers = new List<DataObjects.ActiveUser>();
private DataObjects.CustomLoginProvider _AdminCustomLoginProvider = new DataObjects.CustomLoginProvider();
private List<DataObjects.Tenant> _AllTenants = new List<DataObjects.Tenant>();
private bool _AppOnline = true;
private DataObjects.ApplicationSettingsUpdate _AppSettings = new DataObjects.ApplicationSettingsUpdate();
private string _ApplicationUrl = String.Empty;
```

(One field bucks the PascalCase pattern: `_udfLabels`, because its type `udfLabel` is itself lowercase-prefixed at [DataObjects.UDFLabels.cs:5](FreeCRM/CRM.DataObjects/DataObjects.UDFLabels.cs#L5). When the underlying type is lowercase-prefixed, the field mirrors it.)

**Rule 5c — Dependency-injected / EF / service fields carry NO underscore.** **Dependency injection (DI)** is the pattern where a class is handed its collaborators (a database accessor, a config helper) through its constructor instead of building them itself. Those injected fields, plus the EF context, are plain lowercase with no underscore: `da`, `data`, `context`, `configurationHelper`, `plugins`. You call methods on these all over the class, so they read like ordinary collaborators.
[DataController.cs:11-17](FreeCRM/CRM/Controllers/DataController.cs#L11-L17), [DataAccess.cs:8](FreeCRM/CRM.DataAccess/DataAccess.cs#L8)

```csharp
public partial class DataController : ControllerBase
{
    private HttpContext? context;
    private ICustomAuthentication? authenticationProviders;
    private IDataAccess da;
    private DataObjects.User CurrentUser;
    private Guid TenantId = Guid.Empty;
    private IConfigurationHelper configurationHelper;
    private Plugins.IPlugins plugins;
```

**Rule 5d — `CurrentUser` and `TenantId` are PascalCase with NO underscore**, even though they are private fields. These two carry semantic weight throughout the whole request, so they are named like the important values they are. (A `Guid` is a globally-unique identifier — a long random ID used as a primary key.)
[DataController.cs:14-15](FreeCRM/CRM/Controllers/DataController.cs#L14-L15)

**Rule 5e — A few special server fields keep the underscore even though they're framework/infra objects.** In `DataController`, `_signalR` (marked `readonly`, infrastructure), `_fingerprint` (runtime state), and `_returnCodeAccessDenied` (a constant marker) all keep the `_camelCase` underscore. The distinguishing factor: bare service handles you call methods on (`da`, `context`) get no underscore; `readonly` infrastructure and internal state/constants keep it.
[DataController.cs:19-22](FreeCRM/CRM/Controllers/DataController.cs#L19-L22)

```csharp
    private readonly IHubContext<crmHub, IsrHub>? _signalR;

    private string _fingerprint = String.Empty;
    private string _returnCodeAccessDenied = "{{AccessDenied}}";
```

**Rule 5f — Method PARAMETERS are PascalCase for domain values (unusual for C#).** Standard .NET would write `tenantId`; FreeCRM writes `Guid TenantId`, `string Source`, `Guid UserId`, `string JWT`. There's a practical payoff: PascalCase parameters match the PascalCase keys used in HTTP headers, query strings, and JSON (the controller reads `HeaderValue("TenantId")`, `QueryStringValue("Token")`), so the name lines up from the browser request all the way down to the database call.
[DataAccess.JWT.cs:13-23](FreeCRM/CRM.DataAccess/DataAccess.JWT.cs#L13-L23), [DataController.cs:55](FreeCRM/CRM/Controllers/DataController.cs#L55)

```csharp
public string GetSourceJWT(Guid TenantId, string Source)
{
    string output = String.Empty;
    Dictionary<string, object> Payload = new Dictionary<string, object> {
            {"Source", Source }
        };
    output = JwtEncode(TenantId, Payload);
    return output;
}
```

This rule does **not** extend to plumbing locals — inside the method, `output`, `settings`, `jwt`, `rec` stay camelCase. Domain-named things are PascalCase; throwaway locals are camelCase. It is also not 100% universal: DI constructor parameters (`daInjection`, `httpContextAccessor`, `auth`, `configHelper`) stay camelCase, and some signatures mix both (`AddMessage(string message, ..., bool AutoHide = ...)` at [DataModel.cs:140](FreeCRM/CRM.Client/DataModel.cs#L140)).

**Summary table.**

| Where | Kind | Convention | Example | Proof |
|---|---|---|---|---|
| Server | private backing field | `_camelCase` | `_connectionString` | [DataAccess.cs:6](FreeCRM/CRM.DataAccess/DataAccess.cs#L6) |
| Client | private backing field | `_PascalCase` | `_ActiveUsers` | [DataModel.cs:51](FreeCRM/CRM.Client/DataModel.cs#L51) |
| Server | DI / EF / service field | no `_`, lowercase | `da`, `data`, `context` | [DataController.cs:11-17](FreeCRM/CRM/Controllers/DataController.cs#L11-L17) |
| Server | special infra/state field | `_camelCase` (keeps `_`) | `_signalR`, `_fingerprint` | [DataController.cs:19-22](FreeCRM/CRM/Controllers/DataController.cs#L19-L22) |
| Server | domain field on controller | PascalCase, no `_` | `CurrentUser`, `TenantId` | [DataController.cs:14-15](FreeCRM/CRM/Controllers/DataController.cs#L14-L15) |
| Anywhere | parameter (domain) | PascalCase | `Guid TenantId`, `string JWT` | [DataAccess.JWT.cs:13](FreeCRM/CRM.DataAccess/DataAccess.JWT.cs#L13) |
| Anywhere | DI constructor parameter | camelCase | `daInjection`, `configHelper` | [DataController.cs:24-30](FreeCRM/CRM/Controllers/DataController.cs#L24-L30) |
| Anywhere | local variable (plumbing) | camelCase | `output`, `rec`, `settings` | [DataAccess.Tags.cs:15](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L15) |

---

<a id="types-literals"></a>
## 6. Types & Literals

**Rule 6a — Use `var` when the type is apparent from the right-hand side; use an explicit type otherwise.** Note: the common "always prefer explicit types over var" claim is **false** for this codebase — `var` is used heavily (853 `var` locals in `CRM.DataAccess` alone). The `.editorconfig` sets all three `csharp_style_var_*` keys to `false`, meaning the tool suggests neither — it leaves the choice to you. In practice: `var` for a `new`, a cast, or a LINQ chain; an explicit type for scalars and for `null`/`default` initializers (where `var` can't infer a type).
[Helpers.cs:626-638](FreeCRM/CRM.Client/Helpers.cs#L626-L638)

```csharp
    public static List<string> CsvToListOfString(string? csv)
    {
        var output = new List<string>();

        if (!String.IsNullOrWhiteSpace(csv)) {
            var items = csv.Split(",").Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();
            if (items != null && items.Any()) {
                output = items;
            }
        }

        return output;
    }
```

A few lines down, a `null` initializer forces the explicit type: `DateTime? output = null;` at [Helpers.cs:654-656](FreeCRM/CRM.Client/Helpers.cs#L654-L656).

**Rule 6b — Use `String.Empty` (capital S), never `""`, for the empty string.** An **empty string** is a string with no characters in it; `""` and `String.Empty` mean the same value to the compiler, but FreeCRM uses `String.Empty` everywhere (454 occurrences repo-wide). Note the capital `S` — the BCL type name, *not* the lowercase keyword `string.Empty`. This is actually the opposite of what the `.editorconfig` would prefer, which is exactly why the project uses `dotnet format whitespace` (not bare `dotnet format`, which would flip it).
[Helpers.cs:258](FreeCRM/CRM.Client/Helpers.cs#L258), [DataAccess.cs:7](FreeCRM/CRM.DataAccess/DataAccess.cs#L7)

```csharp
    public static string BooleanToIcon(bool? value, string? icon = "")
    {
        string output = String.Empty;

        if (value.HasValue && (bool)value == true) {
            if (!String.IsNullOrWhiteSpace(icon)) {
```

**The exceptions where `""` survives** are forced by the language, not chosen:
- **Default parameter values** must be compile-time constants, and `String.Empty` is a static field (not a constant), so signatures use `""` — e.g. `string? icon = ""` above, and `string ConnectionString = ""` at [DataAccess.cs:24-28](FreeCRM/CRM.DataAccess/DataAccess.cs#L24-L28).
- **A few LINQ equality predicates** use `!= ""` — e.g. `x.Username != ""` at [DataAccess.Users.cs:2402-2404](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L2402-L2404).

The same capital-S form is used for the related helpers `String.IsNullOrEmpty(...)` and `String.IsNullOrWhiteSpace(...)`.

**Rule 6c — Use predefined keyword types (`string`, `int`, `bool`) for declarations, never the BCL names (`String`, `Int32`, `Boolean`).** Enforced by `.editorconfig` (`dotnet_style_predefined_type_for_locals_parameters_members = true`). The capital-S `String` you see is always a *static-member call* (`String.Empty`, `String.IsNullOrEmpty`) on the right side of an expression — never a declaration type.
[DataAccess.cs:6-21](FreeCRM/CRM.DataAccess/DataAccess.cs#L6-L21)

```csharp
    private string _connectionString;
    private EFDataModel data;
    private string _databaseType;
    private bool _firstInit = true;
```

**Rule 6d — Prefer string concatenation with `+`; interpolation (`$"..."`) is used only sparingly.** Build strings with `+` in most code. Interpolation is genuinely rare — only 6 occurrences in all of `CRM.DataAccess` (5 in PDF generation, where date format specifiers like `{Model.IssueDate:d}` are convenient), zero in `CRM.Client/Helpers.cs`, zero in the `CRM` project's `.cs` files.
[Helpers.cs:271-275](FreeCRM/CRM.Client/Helpers.cs#L271-L275) (concatenation), [DataAccess.PDF.cs:227-236](FreeCRM/CRM.DataAccess/DataAccess.PDF.cs#L227-L236) (the rare interpolation)

```csharp
        if (output != String.Empty) {
            if (!output.Contains("<")) {
                output = "<i class=\"" + output + "\"></i>";
            }
        }
```

**Rule 6e — Boolean and numeric literals are plain lowercase `true`/`false` and bare numbers.** Bool fields are often initialized explicitly to `false`/`true` to make the default visible, though leaving them uninitialized (defaulting to `false`) also occurs.
[DataAccess.cs:10](FreeCRM/CRM.DataAccess/DataAccess.cs#L10), [DataAccess.cs:16](FreeCRM/CRM.DataAccess/DataAccess.cs#L16)

```csharp
    private bool _firstInit = true;
    private bool _inMemoryDatabase = false;
    private bool _open;
    private bool _useBackgroundService = false;
```

---

<a id="nullability"></a>
## 7. Nullability

All four hand-written projects compile with **Nullable Reference Types (NRT) enabled** — `<Nullable>enable</Nullable>` in every `.csproj`. NRT means the compiler tracks whether a reference could be null and warns you when you might dereference a null. The discipline below is about satisfying that analyzer cleanly. The golden rule: **never ship nullable warnings.**
[CRM.DataObjects.csproj:3-7](FreeCRM/CRM.DataObjects/CRM.DataObjects.csproj#L3-L7)

**Rule 7a — A reference that can legitimately be missing is marked nullable with `?`.** Add a trailing `?` to the type (`string?`, `List<...>?`, `AuthenticationProviders?`) and do *not* give it an initializer. The `?` tells the analyzer and the reader that null is a valid state.
[DataObjects.cs:40-52](FreeCRM/CRM.DataObjects/DataObjects.cs#L40-L52)

```csharp
    public partial class ActiveUser
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public bool Admin { get; set; }
        public string? TenantName { get; set; }
        public string? FirstName { get; set; }
        public DateTime? LastAccess { get; set; }
        public Guid? Photo { get; set; }
        public UserPreferences UserPreferences { get; set; } = new UserPreferences();
    }
```

**Rule 7b — A non-null reference property gets a real default via an initializer, never left uninitialized.** With NRT on, a non-nullable `string`/`List<>` with no initializer produces a compiler warning. Initialize it: `= String.Empty;` for strings, `= new List<...>();` / `= new SomeType();` for objects.
[DataObjects.cs:95-103](FreeCRM/CRM.DataObjects/DataObjects.cs#L95-L103)

```csharp
        public List<DataObjects.ActiveUser> ActiveUsers { get; set; } = new List<ActiveUser>();
        public string ApplicationUrl { get; set; } = String.Empty;
        public DataObjects.ApplicationSettingsUpdate AppSettings { get; set; } = new ApplicationSettingsUpdate();
        public AuthenticationProviders? AuthenticationProviders { get; set; }
        public string CultureCode { get; set; } = "en-US";
```

**Rule 7c — When a non-null property has no sensible default, suppress the warning with `= null!`.** `null!` is the *null-forgiving operator* applied to the initializer: it tells the compiler "trust me, this will be set before use" (e.g. always loaded from the database), so the property keeps its non-null type without a warning and without fabricating a fake default.
[DataObjects.cs:457-463](FreeCRM/CRM.DataObjects/DataObjects.cs#L457-L463)

```csharp
    public partial class Tenant : ActionResponseObject
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; } = null!;
        public string TenantCode { get; set; } = null!;
        public bool Enabled { get; set; }
        public DateTime Added { get; set; }
```

`= null!` is also the standard for non-null *fields* wired up after construction (DI services, timers, statics): `private static BlazorDataModel Model = null!;` at [Helpers.cs:34-41](FreeCRM/CRM.Client/Helpers.cs#L34-L41), `private System.Timers.Timer processorTimer = null!;` at [BackgroundProcessor.cs:17-18](FreeCRM/CRM/Classes/BackgroundProcessor.cs#L17-L18).

**A redundant-but-tolerated variant:** some nullable collections are written `List<...>? Prop { get; set; } = null!;` — the type is *already* nullable, so the `= null!` adds nothing the analyzer needs; it just signals "starts null, filled later." Don't read extra meaning into it; match the nearby code.
[DataObjects.cs:465-470](FreeCRM/CRM.DataObjects/DataObjects.cs#L465-L470)

**Rule 7d — A field unconditionally assigned in the constructor may be left plain non-nullable** (no `null!`, no initializer) — the analyzer treats it as initialized. `private EFDataModel data;` is assigned in the `DataAccess` constructor.
[DataAccess.cs:5-9](FreeCRM/CRM.DataAccess/DataAccess.cs#L5-L9)

**Rule 7e — Optional constructor parameters are nullable with a `= null` default.** `IServiceProvider? serviceProvider = null`. The `?` makes the `null` default legal under NRT.
[DataAccess.cs:23-30](FreeCRM/CRM.DataAccess/DataAccess.cs#L23-L30)

**Rule 7f — Prefer explicit `!= null` checks over `??` and `?.` in business logic.** `??` is the *null-coalescing* operator (`a ?? b` means "a, unless it's null, then b"); `?.` is *null-conditional* access ("call this only if not null"). In the data layer and model logic, FreeCRM guards nulls with explicit `if (x != null)` blocks or `x != null ? a : b` ternaries instead. The entire `CRM.DataAccess` project (564 `!= null` occurrences) contains **zero** `??` and zero `?.`; `DataModel.cs` has zero `??`.
[DataAccess.Settings.cs:180-183](FreeCRM/CRM.DataAccess/DataAccess.Settings.cs#L180-L183)

```csharp
            if (UserId != null && TenantId != null) {
                rec = data.Settings.FirstOrDefault(x => x.SettingName != null && x.SettingName.ToLower() == SettingName.ToLower() && x.TenantId == TenantId && x.UserId == UserId);
            } else if (TenantId != null) {
                rec = data.Settings.FirstOrDefault(x => x.SettingName != null && x.SettingName.ToLower() == SettingName.ToLower() && x.TenantId == TenantId && x.UserId == null);
```

`??`, `??=`, and `?.` **are** used, but almost exclusively in framework/interop glue and for null-safe **delegate invocation**, not in domain logic:
- Controller HTTP-context glue: `context?.User?.FindFirstValue(...) ?? string.Empty` at [DataController.cs:90](FreeCRM/CRM/Controllers/DataController.cs#L90).
- Null-safe event raising in the model: `OnChange?.Invoke()` at [DataModel.cs:2120](FreeCRM/CRM.Client/DataModel.cs#L2120) — this is an accepted use of `?.`.

**Rule 7g — Apply the null-forgiving `!` to an expression only to assert a known-non-null value at one use site.** When you *know* a nullable value is non-null at a specific spot and the analyzer can't prove it, append `!` there (e.g. `(Guid)x.UserId!` inside a LINQ predicate) rather than redesigning the type. This is rare; explicit `!= null` guards are still preferred.
[DataAccess.Tenants.cs:89](FreeCRM/CRM.DataAccess/DataAccess.Tenants.cs#L89)

**Quick decision guide.**
- Optional reference → `Type?`, no initializer.
- Always-present reference → non-null type + `= String.Empty` / `= new T()`.
- Always-present but filled later → non-null type + `= null!` (or bare field if the constructor assigns it).
- Optional constructor parameter → `Type? p = null`.
- Null handling in your own logic → explicit `if (x != null)` / ternary. Save `??`, `??=`, `?.` for framework glue and `OnX?.Invoke()`.

---

<a id="properties-fields"></a>
## 8. Properties & Fields

This section is the practical "how do I declare data" companion to nullability.

**Rule 8a — DTO data is exposed as public auto-properties `{ get; set; }`, one per line.** A **DTO** ("data transfer object") is a plain class that just carries data, serialized to/from the client. Every piece of data is `public Type Name { get; set; }` on a single line.
[DataObjects.cs:75-81](FreeCRM/CRM.DataObjects/DataObjects.cs#L75-L81)

```csharp
    public partial class Authenticate
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? TenantCode { get; set; }
        public Guid? TenantId { get; set; }
    }
```

**Rule 8b — Non-nullable reference properties get an initializer; value types do not (unless you want a non-default value).** Strings default to `String.Empty`; collections to `= new List<...>()`; objects to `= new Type()`. Value types (`Guid`, `bool`, `int`, `DateTime`, enums) are left bare to use their natural default, and only get an initializer when a *non-default* starting value is wanted (`= "en-US"`, `= MessageType.Dark`, `= Guid.Empty` when intent should be visible). Use the explicit-type `new List<ActiveUser>()` form, not target-typed `new()`.
[DataObjects.cs:235-246](FreeCRM/CRM.DataObjects/DataObjects.cs#L235-L246)

```csharp
    public partial class EmailMessage : ActionResponseObject
    {
        public string From { get; set; } = String.Empty;
        public string? FromDisplayName { get; set; }
        public List<string> To { get; set; } = new List<string>();
        public string Subject { get; set; } = String.Empty;
        public string Body { get; set; } = String.Empty;
        public List<DataObjects.FileStorage>? Files { get; set; }
    }
```

(Caveat: a few nullable `string?` properties *also* carry a `= String.Empty` default, e.g. `ConnectionString` at [DataObjects.cs:252](FreeCRM/CRM.DataObjects/DataObjects.cs#L252) and the `Theme` block at [DataObjects.cs:525-527](FreeCRM/CRM.DataObjects/DataObjects.cs#L525-L527). So the precise rule is: non-nullable references *always* get an initializer; nullable ones *may* still get one at the author's discretion.)

**Rule 8c — Private fields are `_camelCase` (server), one per line, alphabetized, inline-initialized.** Same defaulting rules as properties. The alphabetical ordering is covered in [§9](#member-ordering).
[DataAccess.cs:5-21](FreeCRM/CRM.DataAccess/DataAccess.cs#L5-L21)

**Rule 8d — When a property needs logic on get/set, use a full backing-field property, not an auto-property or `=>`.** If reading or writing must do work (compare old vs. new, fire a change notification, filter), write a private backing field plus explicit `get { ... }` / `set { ... }` blocks. The whole `BlazorDataModel` does this so every setter can call `NotifyDataChanged()` to re-render the Blazor UI.
[DataModel.cs:121-130](FreeCRM/CRM.Client/DataModel.cs#L121-L130)

```csharp
    public List<DataObjects.ActiveUser> ActiveUsers {
        get { return _ActiveUsers; }
        set {
            if (!ObjectsAreEqual(_ActiveUsers, value)) {
                _ActiveUsers = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }
```

**Rule 8e — Constants are `public const string` (PascalCase), grouped in a dedicated class, one per line.** Use `const` for genuinely compile-time-constant values (magic-string identifiers like SignalR update types and authorization policy names), centralized so there's one source of truth with compile-time checking. See [§4 Rule 4g](#naming-types) for the `Policies` example.
[DataObjects.SignalR.cs:5-44](FreeCRM/CRM.DataObjects/DataObjects.SignalR.cs#L5-L44)

**Rule 8f — `readonly` is for fields assigned exactly once and never changed.** Mark a field `readonly` when it's set once — from a constructor parameter (usually DI) or as a one-time computed lookup table — and never reassigned. `const` can't be used for these because the values are computed at runtime, not compile-time. Combine with `static` for shared app-wide tables.
[BackgroundProcessor.cs:7-12](FreeCRM/CRM/Classes/BackgroundProcessor.cs#L7-L12), [Utilities.cs:171](FreeCRM/CRM.DataAccess/Utilities.cs#L171)

```csharp
    private static readonly IDictionary<string, string> _mappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
        #region Big freaking list of mime types
```

`readonly` is applied selectively (intent, not a blanket rule — other DI fields like `IDataAccess da;` are left plain).

**Rule 8g — Do NOT use init-only setters (`{ get; init; }`) in FreeCRM code.** The DTOs are mutated freely after construction, so a settable property is wanted. A repo-wide search finds exactly one `{ get; init; }`, and it's in the vendored `Try.Core` code at [CodeFile.cs:14](FreeCRM/CRM.Client/DynamicBlazorSupport/CodeFile.cs#L14) — out of standard. Recognize it; don't copy it.

**Rule 8h — Use expression-bodied (`=>`) members only for one-line methods, never for data properties.** The `=>` arrow form is for trivial one-line methods/helpers (a forwarding method, an event raiser). Data properties stay as auto-properties or full `get/set` blocks. No expression-bodied *property* exists in the hand-written code.
[DataModel.cs:2120-2122](FreeCRM/CRM.Client/DataModel.cs#L2120-L2122)

```csharp
    private void NotifyDataChanged() => OnChange?.Invoke();

    private void NotifyDotNetHelperHandler() => OnDotNetHelperHandler?.Invoke(_DotNetHelperMessages);
```

---

<a id="member-ordering"></a>
## 9. Member Ordering

Where do you put a new field or method inside a class? **The answer depends on which layer/file you are in** — FreeCRM uses different ordering conventions per architectural layer. This trips up newcomers, so learn the table.

**Rule 9a — DataAccess field block: strictly ALPHABETICAL** (ignoring the leading `_`). Fields with and without underscores interleave by their letter — `data` (no underscore) sits between `_cookiePrefix` and `_databaseType`. Numbered pairs like `_guid1`/`_guid2` stay in numeric order.
[DataAccess.cs:5-21](FreeCRM/CRM.DataAccess/DataAccess.cs#L5-L21)

**Rule 9b — DataAccess methods (and the `IDataAccess` interface): ALPHABETICAL by method name.** The interface block and the implementation block stay in the same alphabetical order. `protected` helpers are slotted in by their own alphabetical position among the publics (not "publics first, then helpers"): `DeleteTag` → `GetTag` → `GetTags` → `GetTagsForItem` → `SaveItemTags` → `SaveTag`.
[DataAccess.Tags.cs:3-9](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L3-L9)

**Rule 9c — DataObjects DTO *members*: BY-PURPOSE, never alphabetical.** A DTO lists its properties in a fixed semantic order: (1) the primary key, (2) `TenantId`, (3) the business/content fields, (4) the audit block `Added`/`AddedBy`/`LastModified`/`LastModifiedBy`, (5) the soft-delete block `Deleted`/`DeletedAt`. This mirrors how rows map to/from the database.
[DataObjects.Tags.cs:12-28](FreeCRM/CRM.DataObjects/DataObjects.Tags.cs#L12-L28)

```csharp
    public partial class Tag : ActionResponseObject
    {
        public Guid TagId { get; set; }
        public Guid TenantId { get; set; }
        public string? Name { get; set; }
        public string? Style { get; set; }
        public bool Enabled { get; set; }
        public bool UseInAppointments { get; set; }
        public bool UseInEmailTemplates { get; set; }
        public bool UseInServices { get; set; }
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
```

Proof it is *not* alphabetical: `Added` comes after `UseInServices`, and `Deleted` is dead last. (The DTO *classes* themselves, on the other hand, are listed alphabetically within a file — `ActionResponseObject`, `ActiveUser`, `ApplicationSettings`, … at [DataObjects.cs:35-125](FreeCRM/CRM.DataObjects/DataObjects.cs#L35-L125).)

**Rule 9d — DataController field block: grouped BY MUTABILITY / PURPOSE, not alphabetical.** First the injected/per-request mutable fields (in roughly constructor-wiring order), then `readonly` fields, then `_`-prefixed string constants, separated by blank lines.
[DataController.cs:11-22](FreeCRM/CRM/Controllers/DataController.cs#L11-L22) (shown in [§5 Rule 5c/5e](#naming-fields))

**Rule 9e — Client `DataModel` & `Helpers` fields: ALPHABETICAL** (within blank-line-delimited purpose groups). `BlazorDataModel` carries ~60 fields, so alphabetical is the only way to find one fast. There are minor local slips (e.g. `_ApplicationUrl` is slightly out of strict order), so treat it as "alphabetical by intent." Code-generation markers `// {{ModuleItemStart:...}}` interrupt the run but the wrapped fields stay in their correct alphabetical slot.
[DataModel.cs:51-116](FreeCRM/CRM.Client/DataModel.cs#L51-L116)

**Rule 9f — `.razor @code` block: BY-PURPOSE in functional waves** — `[Parameter]` properties → private state fields → lifecycle methods (`Dispose`, `OnAfterRenderAsync`, `OnInitialized`, the model-changed handler) → action/CRUD handlers (`Back`, `Delete`, `Load…`, `Save`) → the `SignalRUpdate` handler last. This reads top-to-bottom in the order the component lives its life. (Note: lifecycle methods are grouped alphabetically with `Dispose` pulled to the front, *not* in Blazor runtime order.)
[EditTag.razor:141-367](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L141-L367)

**Summary table.**

| Location | Member order | Rule |
|---|---|---|
| `DataAccess.cs` fields | Alphabetical (ignore `_`) | 9a |
| `DataAccess.*.cs` methods + `IDataAccess` | Alphabetical by name | 9b |
| `DataObjects` DTO **members** | By-purpose: PK → TenantId → business → audit → soft-delete | 9c |
| `DataObjects` DTO **classes** | Alphabetical | 9c |
| `DataController.cs` fields | Grouped by mutability (DI → readonly → constants) | 9d |
| `DataModel.cs` & `Helpers.cs` fields | Alphabetical (within purpose-groups) | 9e |
| `.razor` `@code` | By-purpose: params → state → lifecycle → handlers → SignalR | 9f |

---

<a id="partials"></a>
## 10. Partial Classes, the `.App.` Convention & File Organization

This section explains *why FreeCRM has so many files* and how to add your own code without breaking template upgrades.

**Rule 10a — One big class is split across many partial files, one file per entity/feature.** A large "god class" like `DataAccess`, `DataController`, or `DataObjects` is never one file. It is declared `partial` (the C# keyword that lets the compiler glue pieces of one class back together) and physically split: `DataAccess.Appointments.cs`, `DataAccess.Users.cs`, `DataController.Tags.cs`, and so on. The base/root file (`DataAccess.cs`) holds the type declaration, fields, and constructor; the entity files hold only members.
[DataAccess.cs:1-8](FreeCRM/CRM.DataAccess/DataAccess.cs#L1-L8), [DataAccess.Appointments.cs:24](FreeCRM/CRM.DataAccess/DataAccess.Appointments.cs#L24)

**Rule 10b — The three layers use parallel file names per entity.** `DataAccess.Appointments.cs` (DB logic) ↔ `DataController.Appointments.cs` (API endpoints) ↔ `DataObjects.Appointments.cs` (DTOs) form a predictable triad, so you can navigate the whole stack of one feature by filename.
[DataAccess.Appointments.cs:3-5](FreeCRM/CRM.DataAccess/DataAccess.Appointments.cs#L3-L5), [DataController.Appointments.cs:8-15](FreeCRM/CRM/Controllers/DataController.Appointments.cs#L8-L15)

**Rule 10c — `IDataAccess` is itself partial; the interface signature lives in the same file as its implementation.** Each `DataAccess.<Entity>.cs` declares `public partial interface IDataAccess` (the entity's method signatures) directly above the `public partial class DataAccess` that implements them. Adding a method means editing one file, not two.
[DataAccess.Appointments.cs:1-26](FreeCRM/CRM.DataAccess/DataAccess.Appointments.cs#L1-L26)

**Rule 10d — `.App.` files are the designated, upgrade-safe place for YOUR custom code.** A file named `<Something>.App.<ext>` re-opens a framework `partial` type and is where *you* (the app developer) add custom code, so the non-`.App.` framework files can be regenerated on upgrade without clobbering your work. Most open with a header comment telling you so.
[DataAccess.App.cs:1-12](FreeCRM/CRM.DataAccess/DataAccess.App.cs#L1-L12)

```csharp
namespace CRM;

// Use this file as a place to put any application-specific data access methods.

public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration);
    DataObjects.BooleanResponse YourMethod();
}

public partial class DataAccess
{
```

The verbatim prefix "Use this file as a place to put any application-specific …" appears in [DataObjects.App.cs:3](FreeCRM/CRM.DataObjects/DataObjects.App.cs#L3), [GraphAPI.App.cs:3](FreeCRM/CRM.DataAccess/GraphAPI.App.cs#L3), and [DataController.App.cs:6](FreeCRM/CRM/Controllers/DataController.App.cs#L6) (only the trailing noun phrase differs per file).

**Rule 10e — `.App.` files exist across every layer and span `.cs`, `.razor`, and `.css`.** Wherever the framework has an extensible partial type, there's a matching `<Type>.App.<ext>` next to the base — server, client, data-access, data-objects. Examples confirmed across the solution: `DataAccess.App.cs`, `DataObjects.App.cs`, `DataController.App.cs`, `Program.App.cs`, `DataModel.App.cs`, `Helpers.App.cs`, `MainLayout.App.razor`, and `site.App.css`.
[GraphAPI.App.cs:1-7](FreeCRM/CRM.DataAccess/GraphAPI.App.cs#L1-L7)

**Rule 10f — `.App.` files ship pre-named extension hooks (stub methods) for you to fill in.** Core methods like `GetData`/`SaveData` call out to an `…App` variant (`GetDataApp`, `SaveDataApp`, `Authenticate_App`) so your custom logic runs without you touching the framework method. This is the inversion-of-control seam.
[DataController.App.cs:8-25](FreeCRM/CRM/Controllers/DataController.App.cs#L8-L25), invoked at [DataController.cs:85](FreeCRM/CRM/Controllers/DataController.cs#L85)

```csharp
public partial class DataController
{
    private DataObjects.User Authenticate_App()
    {
        var output = new DataObjects.User();

        // Perform any app-specific authentication logic here.
        // This is called from the DataController instantiation method.
        ...
        return output;
    }
```

**Rule 10g — Hand-written files use a file-scoped namespace with one top-level type.** Start with `namespace CRM;` (or `CRM.Server.Controllers;` for controllers, `CRM.Client;` for the client) and one top-level type — almost always a re-opening of a shared partial. The lone genuine second-top-level-type exception is `public class SensitiveAttribute` at [DataObjects.cs:3](FreeCRM/CRM.DataObjects/DataObjects.cs#L3).

**Rule 10h — DataObjects are grouped multiple-per-file by entity area, NOT one-type-per-file.** This is the deliberate counter-example to the usual "one public type per file" rule. All Appointment-related DTOs (`Appointment`, `AppointmentAttendanceUpdate`, `AppointmentNote`, `AppointmentService`, `AppointmentUser`) live together as sibling `partial class`es inside the `DataObjects` wrapper.
[DataObjects.Appointments.cs:3-39](FreeCRM/CRM.DataObjects/DataObjects.Appointments.cs#L3-L39)

**Rule 10i — DTOs are flat siblings inside the `DataObjects` wrapper; no DTO is nested inside another DTO.** Each DTO must be independently referenceable as `DataObjects.X` from all three layers (a class nested inside another would be `DataObjects.Appointment.Foo`, which won't do). Composition is done via typed properties, not nesting.
[DataObjects.Appointments.cs:22-27](FreeCRM/CRM.DataObjects/DataObjects.Appointments.cs#L22-L27)

**Rule 10j — Almost every shared type is declared `partial` even when not currently split**, so an `.App.` file can later add members without modifying the framework file. A few view-model/helper classes (`HighchartsTooltip`, `HighchartsRow`, `ModuleAction`) and `SensitiveAttribute` are deliberately non-`partial` because they aren't meant to be extended.
[DataObjects.App.cs:5-15](FreeCRM/CRM.DataObjects/DataObjects.App.cs#L5-L15)

---

<a id="usings"></a>
## 11. Using Directives & Namespaces

A **`using` directive** imports a namespace so you can write `List<>` instead of `System.Collections.Generic.List<>`. A **namespace** is the logical grouping a type belongs to.

**Rule 11a — Use file-scoped namespaces.** `namespace CRM;` on one line ending with a semicolon, no braces. Zero block-scoped namespaces exist in `CRM.DataAccess` or `CRM.DataObjects`. A handful of legacy/vendored files still use the old brace form (`Program.cs`, `signalrHub.cs`, `SetupController.cs`, the plugin samples, and the vendored `Try.Core` files) — don't copy that for new code.
[DataAccess.cs:1-7](FreeCRM/CRM.DataAccess/DataAccess.cs#L1-L7)

**Rule 11b — Place `using` directives OUTSIDE (above) the namespace.** All usings go at the very top, before the `namespace` line, never inside the namespace body. (The only violator is the vendored `Try.Core` file at [CodeFile.cs:3-5](FreeCRM/CRM.Client/DynamicBlazorSupport/CodeFile.cs#L3-L5), which puts usings *inside* the namespace — don't copy it.)
[DataController.cs:1-9](FreeCRM/CRM/Controllers/DataController.cs#L1-L9)

```csharp
using CRM.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.Server.Controllers;

[ApiController]
public partial class DataController : ControllerBase
```

**Rule 11c — Sort usings alphabetically, NOT System-first.** `System.*` is *not* hoisted to the top; it sorts where "S" naturally falls, which lands System usings at the bottom after names like `Plugins` and `Radzen`. (This is the Visual Studio default with "Place System directives first" turned off — `dotnet_sort_system_directives_first = false`.)
[Helpers.cs:17-28](FreeCRM/CRM.Client/Helpers.cs#L17-L28)

```csharp
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Plugins;
using Radzen;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
```

**Rule 11d — Rely on ImplicitUsings; shared namespaces are centralized in a `GlobalUsings.cs`.** Every project sets `<ImplicitUsings>enable</ImplicitUsings>`, so the SDK auto-imports the standard namespace set (`System`, `System.Linq`, etc.) — don't restate those per file. On top of that, `CRM.DataAccess` centralizes its own frequently shared namespaces in a single `GlobalUsings.cs` (16 `global using` directives), grouped by purpose (not alphabetized). Because of both mechanisms, many `DataAccess.*.cs` files have *no* file-level usings and start straight with the namespace. Add to `GlobalUsings.cs` rather than restating per file; fully-qualify the occasional one-off type (e.g. `Microsoft.AspNetCore.Http.HttpContext? _httpContext;` at [DataAccess.cs:13](FreeCRM/CRM.DataAccess/DataAccess.cs#L13), because that namespace is deliberately *not* in `GlobalUsings.cs`).
[CRM.DataAccess.csproj:3-7](FreeCRM/CRM.DataAccess/CRM.DataAccess.csproj#L3-L7), [GlobalUsings.cs:1-16](FreeCRM/CRM.DataAccess/GlobalUsings.cs#L1-L16)

**Rule 11e — For the Blazor client, centralize Razor imports in `_Imports.razor`.** Razor component imports use `@using` directives in the single `_Imports.razor` file, applied to every component. Unlike C# usings, these are *grouped by origin* (System, then Microsoft, then app namespaces, then third-party), not alphabetized.
[_Imports.razor:1-14](FreeCRM/CRM.Client/_Imports.razor#L1-L14)

---

<a id="methods"></a>
## 12. Methods: Signatures, Async & Results

**Rule 12a — Any I/O method is `async` and returns `Task` or `Task<T>`; never `async void` for normal logic.** If a method awaits anything (a database query, an HTTP call, JS interop), declare it `async` and return `Task` (no result) or `Task<T>` (with result). `Task` is the awaitable handle that lets a caller wait for completion and observe exceptions; `async void` can't be awaited and swallows exceptions. `async void` IS used, but only for the 4 event handlers / fire-and-forget UI triggers in the whole codebase.
[DataAccess.Users.cs:262-272](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L262-L272)

```csharp
public async Task<DataObjects.BooleanResponse> DeleteUserPhoto(Guid UserId)
{
    DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();

    var rec = await data.FileStorages.FirstOrDefaultAsync(x => x.ItemId == null && x.UserId == UserId);
    if (rec != null) {
        data.FileStorages.Remove(rec);
        Guid tenantId = GuidValue(rec.TenantId);
        try {
            await data.SaveChangesAsync();
            output.Result = true;
```

**Rule 12b — NO `Async` suffix on our own method names.** FreeCRM's async methods are named `GetUser`, `SaveTag`, `DeleteUser` — you tell they're async from the `async Task<...>` return type, not the name. The codebase is overwhelmingly async, so async is the default and names aren't decorated. The `Async` suffix appears *only* on framework/EF methods we call (`FirstOrDefaultAsync`, `ToListAsync`, `SaveChangesAsync`). The hand-written data layer and controllers contain **0** self-named `…Async` methods.
[DataAccess.Tags.cs:68-75](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L68-L75)

**Rule 12c — Always `await` async calls; never `.Result`/`.Wait()` (except the one documented constructor case).** `await` is non-blocking and propagates exceptions correctly; `.Result` can deadlock and hides errors. The sanctioned exception: the `DataController` constructor uses `.Result` because constructors can't be `async` — `CurrentUser = da.GetUserFromToken(TenantId, token, _fingerprint).Result;` at [DataController.cs:99](FreeCRM/CRM/Controllers/DataController.cs#L99). And **never** `.ConfigureAwait(false)` — it appears 0 times in the hand-written code.

**Rule 12d — Methods return a result/response OBJECT, not bare bools or loose error strings.** When an operation can succeed or fail, return `DataObjects.BooleanResponse` (a `Result` bool + `Messages` list) or a full data object that carries an `ActionResponse`. Callers and the Blazor client need both a success flag AND human-readable messages. (Bare `bool`/`string` returns are fine for simple lookups with no failure path, e.g. `Task<bool> UserCanEditUser(...)`.) Full detail in [§15](#errors).
[DataObjects.cs:121-125](FreeCRM/CRM.DataObjects/DataObjects.cs#L121-L125)

**Rule 12e — Build the result into a local named `output`, then `return output` once at the very end.** Almost every method declares `var output = ...;` on the first line, mutates it through the method, and ends with a single `return output;`. Early returns also return `output`.
[DataAccess.Tags.cs:13-21](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L13-L21)

```csharp
public async Task<DataObjects.BooleanResponse> DeleteTag(Guid TagId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false)
{
    var output = new DataObjects.BooleanResponse();

    var rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId);
    if (rec == null) {
        output.Messages.Add("Error Deleting Tag '" + TagId.ToString() + "' - Record No Longer Exists");
        return output;
    }
    ...
    return output;
}
```

**Rule 12f — Parameter ordering: required identifiers first, then required data, then optional params last; `CurrentUser` conventionally trails with `= null`.** Required positional parameters (a `Guid` id, then the main data object) come first; optional parameters (with `= ...` defaults) go at the end. The auth context `DataObjects.User? CurrentUser = null` is conventionally last — though C#'s own rule that all optionals must trail required args sometimes pushes a second optional (`bool ForceDeleteImmediately = false`) after it.
[DataAccess.Tags.cs:5-8](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L5-L8)

**Rule 12g — Public data-layer methods must appear in `partial interface IDataAccess`; privates/protecteds must not.** `DataAccess` is injected via `IDataAccess`, so every callable method must be on the interface. `private`/`protected` helpers (`CopyUser`, `GetExistingUser`, `GetTagsForItem`) are deliberately *not* on it.
[DataAccess.Tags.cs:3-9](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L3-L9)

**Rule 12h — Overload (don't rename) for different input shapes; the lighter overload delegates to the heavier one.** Give the same operation multiple overloads differing by parameter type/count rather than inventing new names; the simpler overload usually just calls the richer one.
[DataAccess.Users.cs:2267-2280](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L2267-L2280)

```csharp
public async Task<DataObjects.User> UpdateUserFromPlugins(Guid userId)
{
    var output = await GetUser(userId);

    if (output.ActionResponse.Result) {
        output = await UpdateUserFromPlugins(output);
    }

    return output;
}

public async Task<DataObjects.User> UpdateUserFromPlugins(DataObjects.User user)
{
    var output = user;
```

**Rule 12i — Full `{ }` bodies for real logic; `=>` only for one-expression trivial members.** Anything with branching or multiple statements gets a full body. The plugin `Properties() => new Dictionary<...> { ... }` shows the rule is "single *expression*," not "single physical line" — a multi-line dictionary literal still qualifies. See [§8 Rule 8h](#properties-fields).

**Rule 12j — Controller actions are thin async wrappers: auth-check, `await da.X(...)`, `return Ok(output)`.** A `DataController` action declares `public async Task<ActionResult<T>>`, does an auth check if needed, awaits the matching `da.` method into `output`, and returns `Ok(output)` (or `Unauthorized(...)` on failure). **No business logic lives in the controller** — that all sits in the injected `IDataAccess da`.
[DataController.Users.cs:10-17](FreeCRM/CRM/Controllers/DataController.Users.cs#L10-L17)

```csharp
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    [Route("~/api/Data/DeleteUser/{id}")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteUser(Guid id)
    {
        var output = await da.DeleteUser(id, CurrentUser);
        return Ok(output);
    }
```

---

<a id="controlflow"></a>
## 13. Control Flow & Expressions

The brace placement for control flow is covered in [§3](#braces); this section covers the *idioms* — how `if`/`switch`/LINQ/initializers are actually written.

**Rule 13a — Guard clauses use early `return` (no deep nesting).** Validate failure conditions at the top and `return` immediately, rather than wrapping the happy path in a big `if`. Combined with the `output` convention, early returns also return `output`.
[DataAccess.Tags.cs:17-21](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L17-L21)

**Rule 13b — Null/empty checks use `== null` / `!= null` and `String.IsNullOrWhiteSpace`, NOT `is null` pattern matching.** Test references with `== null` / `!= null`; test strings with `String.IsNullOrWhiteSpace(...)` / `String.IsNullOrEmpty(...)` (capital-S). Do *not* use `is null`, `is not null`, or other pattern-matching forms — `CRM.DataAccess` has 36+ `== null`/`!= null` usages and zero real `is null` (the only `is null` text is inside an English doc comment).
[DataAccess.Tags.cs:117-119](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L117-L119)

**Rule 13c — Prefer `if` blocks (or a ternary) over `??`.** Covered in [§7 Rule 7f](#nullability). The wrapped ternary form is used for longer either/or selections (typically LINQ ordering inside a `switch`):
[DataAccess.Invoices.cs:362-366](FreeCRM/CRM.DataAccess/DataAccess.Invoices.cs#L362-L366)

```csharp
            case "TITLE":
                recs = Ascending
                    ? recs.OrderBy(x => x.Title).ThenBy(x => x.InvoiceCreated)
                    : recs.OrderByDescending(x => x.Title).ThenByDescending(x => x.InvoiceCreated);
                break;
```

**Rule 13d — Use `switch` STATEMENTS, not `switch` EXPRESSIONS.** Use the classic `switch (x) { case "...": ... break; }` form with a blank line between cases, each body `break`-terminated, and a `default:` when a fallback is needed. The switch value is usually normalized first with `.ToUpper()`/`.ToLower()`. The C# `x switch { ... => ... }` expression form is **not** used in hand-written code (the only one is in vendored `Try.Core`).
[DataAccess.cs:56-64](FreeCRM/CRM.DataAccess/DataAccess.cs#L56-L64)

```csharp
            switch (_databaseType.ToLower()) {
                case "inmemory":
                    optionsBuilder.UseInMemoryDatabase("InMemory");
                    _inMemoryDatabase = true;
                    break;

                case "mysql":
                    optionsBuilder.UseMySQL(_connectionString, options => options.EnableRetryOnFailure());
                    break;
```

**Rule 13e — LINQ uses METHOD (fluent) syntax only — never query syntax.** Write `.Where(...).Select(...).ToListAsync()`; never `from x in ... select`. There is not a single query expression in the hand-written code. When a chain is too long for one line, put each `.Method(...)` on its own line, indented, with the **dot leading**.
[DataAccess.Tags.cs:146-149](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L146-L149), [DataAccess.Invoices.cs:287-292](FreeCRM/CRM.DataAccess/DataAccess.Invoices.cs#L287-L292)

```csharp
        var tags = await data.TagItems
            .Where(x => x.TenantId == TenantId && x.ItemId == ItemId)
            .Select(x => x.TagId)
            .ToListAsync();
```

**Rule 13f — `foreach` over a checked-non-null collection is the standard loop.** Iterate with `foreach (var rec in recs)` after guarding `recs != null && recs.Any()`, using `var` for the loop variable. `continue;` is not used in DataAccess loops. Counting loops use the classic three-part `for (int x = 1; x < 11; x++)` form; indefinite loops use `while (true) { ... if (cond) { break; } }`.
[DataAccess.Tags.cs:117-121](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L117-L121), [DataAccess.UDFLabels.cs:85-89](FreeCRM/CRM.DataAccess/DataAccess.UDFLabels.cs#L85-L89)

**Rule 13g — `using` is written as a parenthesized BLOCK statement, never a `using` declaration.** Wrap disposables in `using (var x = new ...) { ... }` blocks; do NOT use the C# 8 `using var x = ...;` declaration form (zero of those exist outside vendored code). Nested disposables are stacked `using (...) {` blocks.
[Plugins.cs:235-240](FreeCRM/CRM.Plugins/Plugins.cs#L235-L240)

**Rule 13h — Object initializers: one property per line, each ending in a trailing comma (including the last).** The trailing comma on the final property keeps diffs clean when properties are appended/reordered. (A few legacy initializers omit it; the trailing-comma form is preferred.)
[DataAccess.Tags.cs:81-97](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L81-L97)

```csharp
                output = new DataObjects.Tag {
                    ActionResponse = GetNewActionResponse(true),
                    TagId = rec.TagId,
                    TenantId = rec.TenantId,
                    Name = rec.Name,
                    Style = rec.Style,
                    Enabled = rec.Enabled,
```

**Rule 13i — Collections use `new List<T>()` / `new List<T> { ... }`, NOT collection expressions `[ ]`.** Every hand-written collection uses the explicit constructor form. The C# 12 `= []` collection-expression and `new[] { ... }` forms appear only in third-party `DynamicBlazorSupport` / Roslyn-interop code.
[DataAccess.Utilities.cs:228-232](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L228-L232)

---

<a id="comments"></a>
## 14. Comments, XML Docs, Regions & Attributes

### XML Documentation (`///`)

An **XML doc comment** (`///`) is a structured comment the IDE turns into an IntelliSense tooltip for anyone using the member.

**Rule 14a — Put a `///` `<summary>` on every public member of a shared model/helper class.** In behavior-bearing classes like `BlazorDataModel`, every public property and method gets a `/// <summary>`, even trivial getters — these are the app's public surface and drive IntelliSense.
[DataModel.cs:118-130](FreeCRM/CRM.Client/DataModel.cs#L118-L130)

**Rule 14b — Document parameters and return values with `<param>` and `<returns>` tags**, each on its own line after `</summary>`. HTML inside doc text is escaped as XML entities (`&lt;br /&gt;`).
[DataModel.cs:132-140](FreeCRM/CRM.Client/DataModel.cs#L132-L140)

```csharp
    /// <summary>
    /// Adds a Toast message to the user interface.
    /// </summary>
    /// <param name="message">The message to add. And text inside double curly brackets (eg: {{Tag}}) will be replaced with the language tag for that item.</param>
    /// <param name="messageType">The message type, related to a Bootstrap type.</param>
    /// <param name="AutoHide">If true the message will automatically be hidden after 5 seconds.</param>
    public void AddMessage(string message, MessageType messageType = MessageType.Primary, bool AutoHide = true, bool RemovePreviousMessages = false, bool ReplaceLineBreaks = false)
```

**Rule 14c — Document a Blazor component's `[Parameter]` properties with `/// <summary>`** (in the "library"-style shared components; simpler components are inconsistent about it).
[Language.razor:5-14](FreeCRM/CRM.Client/Shared/Language.razor#L5-L14)

**The big exception: DTOs and controller actions are NOT documented.** `DataObjects.cs` (644 lines) contains **zero** `///` comments — plain data carriers are left undocumented. `DataController.*.cs` files also have zero — the attributes + method name + route are considered self-describing. XML docs are reserved for the model/helper/component layer.

### Inline Comments (`//`)

**Rule 14d — Use `//` line comments as full sentences, on their own line above the code.** Inline explanations use `//` (not `/* */`), start with a capital letter, end with a period, and read as documentation of intent. End-of-line `//` comments are allowed for a single statement.
[DataController.cs:54-63](FreeCRM/CRM/Controllers/DataController.cs#L54-L63)

**Rule 14e — Comment out dead/disabled code with `//` rather than deleting it.** Disabled alternatives and "future option" snippets are kept (each line prefixed with `//`) as a reference trail for the next developer.
[DataModel.cs:439-451](FreeCRM/CRM.Client/DataModel.cs#L439-L451)

**Rule 14f — Templating markers `// {{ModuleItemStart:X}}` / `// {{ModuleItemEnd:X}}` are FUNCTIONAL — do not remove or reformat them.** These paired comments are real processing tokens: `Utilities.AddContentToSection` / `GetTextBetweenItems` find code between these markers to add/remove optional feature modules at build time. They are functional, not decorative.
[DataModel.cs:67-69](FreeCRM/CRM.Client/DataModel.cs#L67-L69)

```csharp
    // {{ModuleItemStart:EmailTemplates}}
    private List<DataObjects.EmailTemplate> _EmailTemplates = new List<DataObjects.EmailTemplate>();
    // {{ModuleItemEnd:EmailTemplates}}
```

### Regions (`#region`)

**Rule 14g — `#region` is used sparingly; name it and always close it with `#endregion`.** Regions are *rare* — exactly 5 `.cs` files in the whole solution use one (`DataModel.cs`, `Program.cs`, `DataAccess.SeedTestData.cs`, `Utilities.cs`, `GraphAPI.cs`), one pair each. The unifying purpose is to "wrap a bulky, non-primary block" — top-of-file enums, a class's internals, or a large inline data block mid-method (`#region Big freaking list of mime types`). The large hand-written files use *no* regions. See the hugging rule in [§2 Rule 2e](#whitespace).
[DataModel.cs:6-44](FreeCRM/CRM.Client/DataModel.cs#L6-L44)

### Attributes

An **attribute** is metadata in square brackets attached to a class or member, e.g. `[HttpGet]`.

**Rule 14h — Stack controller action attributes vertically, one per line, in the order `[HttpVerb]` → `[Authorize...]` → `[Route(...)]`.** Never combine into one `[...]`. The fixed order makes every endpoint scannable: verb, then who's allowed, then the URL. Routes are always absolute (`~/...`) in a separate `[Route]` attribute, never inline as `[HttpGet("...")]`.
[DataController.Tags.cs:8-15](FreeCRM/CRM/Controllers/DataController.Tags.cs#L8-L15)

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

**Rule 14i — Put `[ApiController]` on its own line directly above the controller class; routes live on each action, not the class.** `[ApiController]` opts the class into Web API conventions; there is no class-level `[Route]` because every action declares its own absolute route. (The non-API `AuthorizationController` has no `[ApiController]` at all because it returns redirects/views, not JSON.)
[DataController.cs:8-13](FreeCRM/CRM/Controllers/DataController.cs#L8-L13)

**Rule 14j — Custom marker attributes like `[Sensitive]` go on their own line directly above the property they tag.** This one is *functional*: `DataAccess.RemoveSensitiveData` reflects over properties and blanks any marked `[Sensitive]` before sending objects to the client, so secrets (LDAP creds, JWT keys) never leak to the browser. Each tagged property gets its own attribute line even when consecutive. (`[Sensitive]` appears 9 times, all in `TenantSettings`.)
[DataObjects.cs:3](FreeCRM/CRM.DataObjects/DataObjects.cs#L3) (declaration), [DataObjects.cs:504-509](FreeCRM/CRM.DataObjects/DataObjects.cs#L504-L509) (usage), [DataAccess.Utilities.cs:1846](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L1846) (consumer)

```csharp
        [Sensitive]
        public string? LdapLookupRoot { get; set; }
        [Sensitive]
        public string? LdapLookupUsername { get; set; }
        [Sensitive]
        public string? LdapLookupPassword { get; set; }
```

---

<a id="errors"></a>
## 15. Error Handling & Result Objects

The headline: **FreeCRM does not throw exceptions to its callers.** Operations report success or failure inside a returned object, and the API returns a structured JSON payload every time instead of an opaque 500 error.

**Rule 15a — Every DataAccess method returns a data/response object, never throws to the caller.** `throw` is reserved for unrecoverable low-level setup — it appears only twice in the entire `CRM.DataAccess` project (a missing connection string at [DataAccess.cs:148](FreeCRM/CRM.DataAccess/DataAccess.cs#L148), a null extension argument at [Utilities.cs:769](FreeCRM/CRM.DataAccess/Utilities.cs#L769)). Controllers contain zero `throw` statements.

**Rule 15b — `BooleanResponse` is the standard "did it work + why not" object: exactly `Messages` (list) + `Result` (bool).** `Messages` defaults to a new list so callers can always `.Add(...)` without a null check. (A lighter `SimpleResponse` uses a single `string? Message` instead of a list.)
[DataObjects.cs:121-125](FreeCRM/CRM.DataObjects/DataObjects.cs#L121-L125)

```csharp
    public partial class BooleanResponse
    {
        public List<string> Messages { get; set; } = new List<string>();
        public bool Result { get; set; }
    }
```

**Rule 15c — Data DTOs carry their own status by inheriting `ActionResponseObject`** (which holds an `ActionResponse` of type `BooleanResponse`). A "get one record"/"save one record" call returns the populated object *and* its status in one payload, via `obj.ActionResponse.Result` and `obj.ActionResponse.Messages`. `Tag`, `User`, `Tenant`, `Department`, `Invoice`, `Setting`, etc. all inherit it.
[DataObjects.cs:35-38](FreeCRM/CRM.DataObjects/DataObjects.cs#L35-L38)

```csharp
    public partial class ActionResponseObject
    {
        public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
    }
```

**Rule 15d — Wrap the database write (`SaveChangesAsync`) in try/catch and convert any exception into messages via `RecurseException`.** In `catch (Exception ex)`, add a human-readable "Error <doing X> <id>" header, then append the full exception chain with `RecurseException(ex)`. Don't log-and-swallow silently and don't rethrow.
[DataAccess.Tags.cs:239-258](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L239-L258)

```csharp
        try {
            if (newRecord) {
                await data.Tags.AddAsync(rec);
            }
            await data.SaveChangesAsync();

            output.ActionResponse.Result = true;
            // ...SignalR update omitted...
        } catch (Exception ex) {
            output.ActionResponse.Messages.Add("Error Saving Tag " + output.TagId.ToString());
            output.ActionResponse.Messages.AddRange(RecurseException(ex));
        }
```

**Rule 15e — `RecurseException` is the one helper for flattening an exception into messages.** Never read `ex.Message` directly into a response. Call `RecurseException(ex)` for a `List<string>` (one entry per exception in the `InnerException` chain, prefixed by type name), or `RecurseExceptionAsString(ex)` for those joined with `" | "`. This guarantees the root cause (often a DB constraint several levels down) reaches the UI.
[DataAccess.Utilities.cs:1783-1797](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L1783-L1797)

**Rule 15f — Initialize the response with a helper and set success only on the happy path.** Use `GetNewActionResponse()` (defaults `Result = false`) on objects with an `ActionResponse`, or `new DataObjects.BooleanResponse()` for plain returns. Start *failed*, and flip `Result = true` only after the operation actually succeeds — so any early return or unhandled path defaults to a reported failure.
[DataAccess.Utilities.cs:1524-1534](FreeCRM/CRM.DataAccess/DataAccess.Utilities.cs#L1524-L1534)

```csharp
    public DataObjects.BooleanResponse GetNewActionResponse(bool result = false, string? message = null)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse {
            Result = result,
            Messages = new List<string>()
        };
        if (!String.IsNullOrEmpty(message)) {
            output.Messages.Add(message);
        }
        return output;
    }
```

**Rule 15g — Validation and "record not found" errors are surfaced as messages with an early return — not exceptions.** For expected business failures (record missing, duplicate email, missing field), add a descriptive string to `Messages` and `return` immediately, leaving `Result` false. These are normal outcomes, not exceptional ones; try/catch is strictly for the unexpected (the DB write).
[DataAccess.Tags.cs:17-21](FreeCRM/CRM.DataAccess/DataAccess.Tags.cs#L17-L21)

**Rule 15h — API controllers do not catch — they delegate and wrap the result in `Ok(...)`.** The error state is already inside the returned object, so a 200 with `Result = false` is the normal way to report a business failure. The declared return type is `Task<ActionResult<TResponseObject>>`. See [§12 Rule 12j](#methods).

**Rule 15i — Authorization failures (only) are returned as HTTP `Unauthorized` with the `{{AccessDenied}}` sentinel.** When the controller itself must block access (caller is not admin and not the record owner), it returns `Unauthorized(_returnCodeAccessDenied)` instead of `Ok(...)`. `_returnCodeAccessDenied` is the fixed string `"{{AccessDenied}}"`, a placeholder the localization layer swaps for a translated message. (No `BadRequest`/`NotFound`/`StatusCode(...)` is used — only `Ok` and `Unauthorized`.)
[DataController.Invoices.cs:46-55](FreeCRM/CRM/Controllers/DataController.Invoices.cs#L46-L55), [DataController.cs:22](FreeCRM/CRM/Controllers/DataController.cs#L22)

```csharp
    public async Task<ActionResult<DataObjects.Invoice>> GetInvoice(Guid id)
    {
        var output = await da.GetInvoice(id, CurrentUser);

        if (CurrentUser.Admin || CurrentUser.UserId == output.UserId) {
            return Ok(output);
        } else {
            return Unauthorized(_returnCodeAccessDenied);
        }
    }
```

**Rule 15j — "Fire-and-forget / best-effort" code uses an empty `catch { }` to swallow exceptions DELIBERATELY.** For non-critical operations with a safe fallback (parsing an optional header/querystring GUID, reading a claim), use a bare `try { ... } catch { }`. This is the *opposite* of Rule 15d and is allowed ONLY where there's a meaningful fallback and the caller doesn't need to know about the failure. Any database write or user-initiated save must use `catch (Exception ex)` and report via `Messages`.
[DataController.cs:59-63](FreeCRM/CRM/Controllers/DataController.cs#L59-L63)

```csharp
        if (!String.IsNullOrEmpty(tenantId)) {
            try {
                TenantId = new Guid(tenantId);
            } catch { }
        }
```

**Rule 15k — Formatting: K&R one-line `catch`, lowercase `ex`, `output` as the return variable.** Write the catch clause on the same line as the `try`'s closing brace (`} catch (Exception ex) {`), name the caught exception `ex`, and name the return value `output`.
[DataAccess.Users.cs:242-256](FreeCRM/CRM.DataAccess/DataAccess.Users.cs#L242-L256)

---

<a id="razor"></a>
## 16. Razor / Blazor: the C# Parts

A **Razor / Blazor component** (`.razor` file) mixes HTML markup with C# in a `@code { }` block at the bottom. **Most of the Razor/Blazor conventions live in the sibling doc [056](056_razor-blazor-style-reference.md)** — directives, markup, `@bind`, component invocation, the page lifecycle, SignalR handlers, and the `.App.` component pattern. Here we cover only the parts that are genuinely *C#*.

**Rule 16a — The C# style rules apply unchanged inside `@code` blocks.** Same-line control-flow braces, own-line method braces, early `return;`, `== null` checks, `String.IsNullOrWhiteSpace`, object initializers with trailing commas, `new List<T>()` — all of [§3](#braces), [§7](#nullability), and [§13](#controlflow) hold inside `@code`. The Blazor code-behind is held to the identical standard as the server code.
[EditTag.razor:168-172](FreeCRM/CRM.Client/Pages/Settings/Tags/EditTag.razor#L168-L172)

**Rule 16b — `@code` member ordering follows the by-purpose wave** described in [§9 Rule 9f](#member-ordering): `[Parameter]` properties → `protected` backing fields → `Dispose()` → lifecycle overrides → your own handler/CRUD methods → `SignalRUpdate` last. The backing fields are `protected` (not `private`), leading-underscore camelCase (`_loading`, `_pageName`), with `_pageName` conventionally on its own line.
[Tags.razor:122-134](FreeCRM/CRM.Client/Pages/Settings/Tags/Tags.razor#L122-L134)

```csharp
    [Parameter] public string? TenantCode { get; set; }

    protected bool _loadedData = false;
    protected bool _loading = true;
    protected bool _validatedUrl = false;

    protected string _pageName = "tags";

    public void Dispose()
    {
        Model.OnChange -= OnDataModelUpdated;
        Model.OnSignalRUpdate -= SignalRUpdate;
    }
```

**Rule 16c — Expression-bodied (`=>`) members and the no-`Async`-suffix rule apply to `@code` too.** The model's event raisers (`NotifyDataChanged() => OnChange?.Invoke();`) and `[Parameter]` defaults (`= String.Empty`, `= true`) follow the same C# rules covered above.

For everything else about `.razor` files (the `@page`/`@inject` header order, the `Model.View == _pageName` render guard, subscribe-in-`OnInitialized`/unsubscribe-in-`Dispose`, two-way `Value`/`ValueChanged` binding, `@bind:event="oninput"`, and the `*_App` extension components), **see [056 — The Razor / Blazor Reference](056_razor-blazor-style-reference.md).**

---

<a id="quick-reference"></a>
## 17. Quick Reference

The most-used rules at a glance. Left column is wrong *for this codebase*; right column matches the house style.

**Braces**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `public bool Foo(string x) {` (method brace same line) | `public bool Foo(string x)`<br>`{` (new line) |
| `public string Theme`<br>`{` (property brace new line) | `public string Theme {` (same line) |
| `if(x == null) {` (no space) | `if (x == null) {` (space) |
| `}`<br>`catch (Exception ex) {` | `} catch (Exception ex) {` (hugged) |
| wrapped `if`: `)`<br>`{` | wrapped `if`: `) {` (collapsed) |

**Naming**

| Kind | ✓ House style | Example |
|------|---------------|---------|
| Type / interface / method / property | PascalCase (`I`-prefix for interfaces) | `BlazorDataModel`, `IDataAccess`, `GetTag` |
| Local variable | camelCase | `output`, `rec`, `tenantId` |
| `const` | PascalCase (not `ALL_CAPS`) | `Admin`, `RazorFileExtension` |
| Server private field | `_camelCase` | `_connectionString` |
| Client private field | `_PascalCase` | `_ActiveUsers` |
| DI / EF / service field | no `_`, camelCase | `da`, `data`, `context` |
| `CurrentUser` / `TenantId` field | no `_`, PascalCase | `private Guid TenantId` |
| Business method parameter | **PascalCase** | `Guid TenantId`, `string JWT` |
| DI constructor parameter | camelCase | `httpContextAccessor` |

**Types, literals, nullability**

```csharp
// ✗                                  // ✓
public string Id { get; set; } = "";  public string Id { get; set; } = String.Empty;
String s; Int32 i; Boolean b;         string s; int i; bool b;
if (x is null) { }                    if (x == null) { }
var y = x ?? fallback;                if (x != null) { y = x; } else { y = fallback; }
public string Name { get; set; }      public string Name { get; set; } = null!;  // set later
$"Hello {name}"                       "Hello " + name   // concatenation preferred
```

**Methods & errors**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `GetTagAsync(...)` | `GetTag(...)` (async, but no suffix) |
| `.Result` / `.Wait()` / `.ConfigureAwait(false)` | `await` (except `DataController` ctor) |
| `return true;` / `throw` for a failure | `return output;` with `BooleanResponse` |
| logic in the controller | thin controller: `await da.X(...); return Ok(output);` |
| reading `ex.Message` into a response | `output.Messages.AddRange(RecurseException(ex));` |

**Control flow**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `x switch { ... }` expression | `switch (x) { case ...: break; }` statement |
| `from x in list select x` | `list.Where(...).Select(...)` (method syntax) |
| `using var x = ...;` | `using (var x = ...) { }` block |
| `= []` / `new[] { }` | `new List<T>()` / `new List<T> { ... }` |

**Member ordering** — depends on the layer: DataAccess fields/methods = alphabetical; DataObjects DTO members = by-purpose (PK → TenantId → business → audit → soft-delete); DataController fields = grouped by mutability; `.razor @code` = params → state → lifecycle → handlers.

---

<a id="faq"></a>
## 18. FAQ

**Q1. Why `String.Empty` instead of `""`?**
Pure consistency. `""` and `String.Empty` are identical to the compiler, but FreeCRM uses `String.Empty` everywhere (454 times) so the empty-string case is instantly visible and searchable. Note the *capital* `S` — it's the framework type name, deliberately the opposite of what the formatter would prefer, which is why the project runs `dotnet format whitespace` (not bare `dotnet format`, which would flip it). The only place `""` survives is where the language forces it: default parameter values (which must be compile-time constants — `String.Empty` is a static field, not a constant) and a few LINQ equality predicates. See [§6 Rule 6b](#types-literals).

**Q2. Why are method parameters PascalCase — isn't that "wrong" for C#?**
It's unusual but deliberate, and it has a real payoff. PascalCase parameters (`Guid TenantId`, `string JWT`) match the PascalCase keys used in HTTP headers, query strings, and JSON. The controller reads `HeaderValue("TenantId")`, so keeping the parameter PascalCase makes the value's name identical all the way from the browser request down to the database call. It applies to *domain* parameters; DI constructor parameters and throwaway plumbing locals stay camelCase. See [§5 Rule 5f](#naming-fields).

**Q3. Why no `var` rule — I heard "always use explicit types"?**
That's a myth about this codebase. The `.editorconfig` leaves the choice entirely to you (all `csharp_style_var_*` keys are `false`), and `var` is used heavily — 853 `var` locals in `CRM.DataAccess` alone. The convention is: `var` when the type is obvious from the right side (`new`, a cast, a LINQ chain); an explicit type for scalars and for `null`/`default` initializers where `var` can't infer. See [§6 Rule 6a](#types-literals).

**Q4. Where do new methods and fields go in a class?**
It depends on which file you're in — this is the thing that trips up newcomers most. DataAccess fields and methods (and the `IDataAccess` interface) are **alphabetical**. DataObjects DTO members are **by-purpose** (primary key → `TenantId` → business fields → audit block → soft-delete). DataController fields are **grouped by mutability** (DI services → readonly → constants). Client `DataModel`/`Helpers` fields are **alphabetical**. A `.razor @code` block is **by-purpose waves** (params → state → lifecycle → handlers). See the table in [§9](#member-ordering).

**Q5. Why no final newline at the end of files?**
Consistency — it matches the project's default editor save behavior. There's no functional reason; it's enforced by the `.editorconfig` (`insert_final_newline = false`). Files end on the closing `}` with bytes `CR LF }`. See [§2 Rule 2c](#whitespace).

**Q6. When do I put `_` on a field, and when not?**
Default: private backing fields get a leading `_`. Drop it for dependency-injected services and the EF context (`da`, `data`, `context`, `configurationHelper`) — those read like ordinary collaborators. Also drop it for the two special PascalCase fields `CurrentUser` and `TenantId`. But *keep* it for `readonly` infrastructure (`_signalR`) and internal state/constants (`_fingerprint`, `_returnCodeAccessDenied`), even though they're in the same class. Casing after the underscore is `_camelCase` on the server, `_PascalCase` in the Blazor client. See [§5](#naming-fields).

**Q7. Why are some files named `.App.cs`?**
They're your upgrade-safe extension points. FreeCRM is a template you build on top of and periodically pull updates from. The framework's own files (`DataAccess.cs`, etc.) can be regenerated on upgrade. The `.App.` files re-open the same `partial` class and are where *you* add custom code, so an upgrade never clobbers your work. They ship pre-named stub hooks (`SaveDataApp`, `Authenticate_App`) that the core code calls at the right moment. See [§10 Rules 10d–10f](#partials).

**Q8. Why does the codebase avoid `??` and `?.`?**
House preference for explicit, readable null handling in business logic — `if (x != null)` guards and `x != null ? a : b` ternaries instead. `CRM.DataAccess` has 564 `!= null` checks and *zero* `??`/`?.`. `??`/`?.` *are* allowed in framework/interop glue (HTTP-context chains) and for null-safe delegate invocation (`OnChange?.Invoke()`). See [§7 Rule 7f](#nullability).

**Q9. Why do methods return a `BooleanResponse` object instead of just `true`/`false` or throwing?**
Because the Blazor client and API need both a success flag *and* human-readable messages, every time, as a JSON object. A thrown exception would become an opaque HTTP 500; a bare `bool` can't carry an error message. So `BooleanResponse` (a `Result` bool + a `Messages` list) is the standard envelope, and errors are caught and pushed into `.Messages` rather than thrown. Bare `bool`/`string` returns are fine only for trivial lookups with no failure path. See [§15 Rules 15a–15c](#errors).

**Q10. Why is there an empty `catch { }` in places — isn't swallowing exceptions bad?**
It's bad *when it hides a real failure* — and FreeCRM forbids it there (DB writes and saves use `catch (Exception ex)` and report via `Messages`). But an empty `catch { }` is deliberately used for best-effort operations that have a safe fallback, like parsing an optional querystring GUID where a failure just leaves the already-initialized default. The two cases are opposites; the test is "does the caller need to know it failed?" See [§15 Rules 15d & 15j](#errors).

**Q11. Why don't our async methods end in `Async`, like the framework's do?**
Because virtually everything is async, so the team treats async as the default and doesn't decorate every name. You tell a method is async from its `async Task<...>` return type. Only the framework/EF methods we *call* keep their `…Async` names (`SaveChangesAsync`, `ToListAsync`). The hand-written data layer and controllers have 0 self-named `…Async` methods. See [§12 Rule 12b](#methods).

**Q12. Why are all our DTOs and the `DataAccess` class `partial` and split across so many files?**
These are "god classes" with hundreds of members. Splitting `DataAccess` into `DataAccess.Users.cs`, `DataAccess.Tags.cs`, etc. keeps each file small and lets you find "everything about Tags" in one place. `partial` is the C# keyword that lets the compiler reassemble the pieces. The three layers use parallel names per entity (`DataAccess.Tags.cs` ↔ `DataController.Tags.cs` ↔ `DataObjects.Tags.cs`), and types are marked `partial` even when not split so an `.App.` file can extend them. See [§10](#partials).

**Q13. Why is the `crmHub` class lowercase, breaking the PascalCase rule?**
It's a deliberate legacy exception for the SignalR hub (`crmHub`, `IsrHub`, `signalrHub.cs`). SignalR is the library for real-time server-to-browser messages. The `.editorconfig` marks the type-casing rule as a *suggestion* (not an error), so the exception is allowed to stand. Don't "fix" it. (This is documented in [051](051_house-code-style.md).)

**Q14. Why do object initializers have a trailing comma after the *last* property?**
So that appending or reordering a property changes only one line in the diff (you don't have to also edit the previous line to add a comma). It's the same reasoning behind one-property-per-line. A few legacy initializers omit it; the trailing-comma form is preferred. See [§13 Rule 13h](#controlflow).

**Q15. Why do `.razor` pages load data in `OnAfterRenderAsync` instead of `OnInitialized`, and guard with a `_loadedData` flag?**
The shared `BlazorDataModel` finishes loading *after* the component initializes, and these pages need `Model.LoggedIn`, feature flags, and tenant validation to be ready first. `OnAfterRenderAsync` runs after the model is populated; the `_loadedData` boolean stops it re-loading on every re-render. This is a Blazor-lifecycle detail covered fully in [056](056_razor-blazor-style-reference.md); the C# member-ordering side is in [§16](#razor).

---

<a id="related-docs"></a>
## 19. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the opinionated brace/casing/empty-string/field-naming rules and where the hand style overrides the formatter (parent overview)
- [052 — Where Code Lives and How Comments Sound](052_files-and-comment-voice.md) — file layout and comment voice
- [053 — The Machine Referee: editorconfig and What It Enforces](053_editorconfig-enforcement.md) — which of these rules the formatter applies vs. which are enforced by hand
- **Sibling references (being written alongside this one):**
  - [056 — The Razor / Blazor Reference](056_razor-blazor-style-reference.md) — component directives, lifecycle, `@bind`, SignalR handlers, `.App.` components
  - [057 — The CSS Reference](057_css-style-reference.md)
  - [058 — The JavaScript Reference](058_javascript-style-reference.md)
  - [059 — The SQL Reference](059_sql-style-reference.md)

---
*GuidesV2 055 · The C# Style Reference · drafted 2026-06-05 from byte-level citation-verified evidence against the hand-written FreeCRM source (`CRM`, `CRM.Client`, `CRM.DataAccess`, `CRM.DataObjects`). Every rule links to its proof in the real source tree. `CRM.EFModels` (scaffolded) and `CRM.Client/DynamicBlazorSupport/` (vendored `Try.Core`) are excluded as style sources.*
