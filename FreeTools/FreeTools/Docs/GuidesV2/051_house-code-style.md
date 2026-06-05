# 051 — The Author House Style

> **Document ID:** 051  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Codify the opinionated brace, casing, empty-string, and field-naming rules sourced from the framework author.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why House Style Matters](#why-it-matters) | What "house style" is, the key terms, and why following it pays off |
| 2 | [Brace and Block Layout](#braces) | Where the `{` goes: new line for methods/types, same line for properties and control flow |
| 3 | [Casing Conventions](#casing) | Which names are PascalCase vs camelCase for types, members, parameters, and locals |
| 4 | [Empty Strings and Null Handling](#empty-strings) | Why this codebase always writes `String.Empty` (capital S) instead of `""` |
| 5 | [Field and Member Naming](#field-naming) | The underscore rules: which private fields get `_` and which deliberately do not |
| 6 | [Applying and Enforcing the Style](#enforcement) | What the `.editorconfig` formatter handles vs what humans enforce by hand |
| 7 | [Quick Reference and Examples](#quick-reference) | Side-by-side correct and incorrect snippets you can copy |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why House Style Matters

**House style** is the set of formatting and naming rules a particular codebase agrees to follow so that all of its code looks like it was written by one person. "Style" here means the cosmetic choices that do not change what the program *does* — where a curly brace lands, whether a name starts with a capital letter, whether you write `""` or `String.Empty`. The compiler does not care about any of it. Humans care a great deal.

**Why it matters:** FreeCRM is a template you build *on top of*, and it periodically pulls in new code from its original author (a process other docs call "fork sync"). When your code matches the author's style, three good things happen. First, code reviews stay focused on logic instead of arguing about brace placement. Second, automated merges from upstream produce fewer noisy conflicts, because both sides format the same way. Third, a newcomer can read any file and immediately know what kind of thing a name refers to, because the casing rules are consistent.

A note on where these rules come from. Most projects have a written style guide that is somebody's *opinion*. This one is different: the rules below were established by the original author — **Brad Wickett** (`wicketbr` on GitHub) — in an explicit, repo-wide cleanup pass during May–June 2026. The flagship commit (`d094f99`) touched 187 files and was net-negative (more lines deleted than added), which is the signature of a genuine consistency cleanup rather than new features. So these are not guesses; they are the author's actual, observable preferences, checked against the live source at `c:\Users\pepkad\source\repo2\FreeCRM`.

One important subtlety to keep in mind as you read: some of these rules are enforced *mechanically* by a config file (the `.editorconfig`, covered in [053](053_editorconfig-enforcement.md)), and some are enforced *by hand* because the tooling cannot express them — and in a few cases the author's hand style deliberately overrides his own config. Section 6 maps out which is which. When in doubt, **the hand convention wins**.

---

<a id="braces"></a>
## 2. Brace and Block Layout

A **brace** is the curly bracket `{` `}` that opens and closes a block of code. The single most distinctive thing about this codebase is *where the opening brace goes*, and the rule is not uniform — it depends on what kind of thing you are declaring. Memorize these three cases and you have most of the house style.

**Rule 2a — Methods and types: opening brace on its OWN new line.** A **method** is a named block of behavior (a function); a **type** is a class, struct, interface, or enum. For both, the `{` drops to the line below the declaration.

```csharp
public static bool AllowedFileType(string filename)
{
    bool output = false;
    ...
}
```

**Rule 2b — Properties: opening brace on the SAME line.** A **property** is a member that looks like a field but runs code when you read or write it (it has `get`/`set` blocks). For properties, the `{` stays on the declaration line. This is the author's exact stated rule: *"all opening brackets are on a new line for methods but on the same line for properties."*

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

(That snippet is copied verbatim from `CRM.Client/DataModel.cs`. Notice the property brace is same-line, but the inner `get`/`set` accessor braces are also same-line — those are not methods.)

**Rule 2c — Control-flow braces: same line, with a space after the keyword.** **Control flow** means `if`, `else`, `foreach`, `while`, `try`, `catch`, and friends — the keywords that decide *which* code runs. Their braces stay on the same line (a layout known as **K&R style**), there is always a space between the keyword and the `(`, and closing-then-reopening pairs like `} catch`, `} else`, `} finally` stay glued together on one line:

```csharp
if (rec == null) {
    ...
} catch (Exception ex) {
    ...
} else {
    ...
}
```

Common mistakes this rule forbids: `if(rec == null)` (no space), and putting `catch` or `else` on its own new line.

**Rule 2d — Wrapped parameter lists: the distinctive `( … ){` shape.** When a method has so many parameters that the list does not fit on one line, this codebase breaks it in a very specific way. The opening `(` drops to its own line, each parameter sits one-per-line indented, and the closing `)` collapses onto the same line as the method's opening brace, written as `){`. This is copied exactly from `CRM.Client/Helpers.cs`:

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
){
    DialogService = dialogService;
    ...
}
```

This `){` collapse is a *hand* convention — the formatter does not produce it — so you have to type it deliberately.

**Indentation:** four spaces per level, never tabs. This one *is* enforced by the formatter (`indent_size = 4`, `indent_style = space`).

---

<a id="casing"></a>
## 3. Casing Conventions

**Casing** is the capitalization pattern of a name. Two patterns dominate. **PascalCase** capitalizes the first letter of every word (`AppointmentNoteId`). **camelCase** is the same but with a lowercase first letter (`connectionString`). The house rule is mostly standard .NET, with two deliberate twists worth flagging.

**Rule 3a — Types, properties, and methods are PascalCase.** Classes, interfaces (which begin with `I`, e.g. `IDataAccess`), enums, properties, and method names all use PascalCase. This is the conventional .NET default.

**Rule 3b — Local variables are camelCase.** A **local variable** is one declared inside a method body. These use camelCase: `output`, `adminMenuItems`, `tenantSettings`, `tenantId`. Seen in `Helpers.cs`:

```csharp
var adminMenuItems = MenuItemsAdmin.Select(x => x.PageNames).SelectMany(x => x).ToList();
```

**Rule 3c (the first twist) — method PARAMETERS are PascalCase.** A **parameter** is an input listed in a method's signature. In most .NET code parameters are camelCase, but this codebase's domain/business methods use **PascalCase** parameters: `AppointmentNoteId`, `CurrentUser`, `ForceDeleteImmediately`, `DepartmentGroupId`. You can see this in real signatures throughout `DataModel.cs` (`bool AutoHide`, `bool RemovePreviousMessages`, `bool ReplaceLineBreaks`). It is not 100% uniform — a few low-level utility params remain camelCase — but PascalCase is the deliberate, dominant convention. Match it.

**Rule 3d (the exception to 3c) — dependency-injection constructor parameters stay camelCase.** **Dependency injection (DI)** is the pattern where a class is handed its collaborators (a database accessor, a logger, a config helper) through its constructor instead of creating them itself. Those injected-service parameters keep ordinary camelCase: `daInjection`, `httpContextAccessor`, `auth`, `configHelper`, `serviceProvider`. So the rule of thumb is: *business* method params are PascalCase; *injected service* params are camelCase. (Plain value/config constructor params like `ConnectionString` and `DatabaseType` follow the PascalCase rule.)

**Rule 3e (the second twist) — the SignalR hub is camelCase on purpose.** **SignalR** is the library FreeCRM uses for real-time server-to-browser messages. Its hub class, interface, and file break the PascalCase rule as a deliberate legacy exception: `public partial class crmHub`, `public partial interface IsrHub`, in `signalrHub.cs`. The `.editorconfig` only marks the type-casing rule as a *suggestion* (not an error), so this exception is allowed to stand. Do not "fix" it.

---

<a id="empty-strings"></a>
## 4. Empty Strings and Null Handling

This section covers one small, extremely consistent rule that you will type dozens of times a day, so it is worth getting into muscle memory.

**Background terms.** An **empty string** is a string of text with no characters in it. There are two common ways to write it in C#: the literal `""` (two quote marks), or `String.Empty`, a built-in constant that means the same value. They are interchangeable to the compiler. Separately, `null` means "no string at all / no object here," which is different from an empty string — `null` is the *absence* of a value, `""` is a *present* value that happens to be blank.

**Rule 4a — Always write `String.Empty`, never `""`.** Across this codebase the empty-string literal has been systematically replaced with `String.Empty`. This applies everywhere an empty string appears: property defaults, local initializers, assignments, and even inside LINQ query predicates. From `DataModel.cs`:

```csharp
public string Id { get; set; } = String.Empty;
private string _ApplicationUrl = String.Empty;
```

Two details that make this rule precise:

- **Capital `S` in `String`.** The author writes `String.Empty` using the BCL type name (capital S), not the lowercase keyword `string.Empty`. This is actually the *opposite* of what the `.editorconfig` would prefer — see Section 6 — but the hand convention wins. The same capital-S form is used for the related helpers `String.IsNullOrEmpty(...)` and `String.IsNullOrWhiteSpace(...)`, both seen throughout `Helpers.cs`.
- **Only the *empty* string is converted.** Non-empty literals are left completely alone. You still write `".PDF"`, `"local"`, route names, and any other text literal as a normal quoted string. The rule is exclusively about replacing the *blank* `""`.

**Why it matters:** consistency makes the empty-string case instantly visible when reading code, and a search for `String.Empty` reliably finds every default-blank field. (For scale: `String.Empty` appears 450+ times across the `.cs` files in the repo; plain `= ""` defaults are effectively gone.)

**On null handling:** there is no blanket "never use null" rule. Fields that genuinely have no value default to `null` where that is the right meaning (e.g. `private List<string>? _GloballyDisabledModules = null;` in `DataModel.cs` — the `?` marks the type as *nullable*, meaning null is a legal value). The discipline is narrower than "avoid null": it is simply "for an empty *string*, use `String.Empty`."

---

<a id="field-naming"></a>
## 5. Field and Member Naming

A **field** is a variable that belongs to a class (as opposed to a local, which lives inside one method). This codebase has a precise convention for whether a private field's name starts with an underscore (`_`), and it is worth learning because the exceptions are intentional, not sloppiness.

**The default — Rule 5a: private and protected fields get a leading `_`.** Most backing fields start with an underscore, and the casing *after* the underscore matches the natural casing of that part of the codebase:

- In the client model `BlazorDataModel` (`DataModel.cs`), fields are `_PascalCase`: `_AllTenants`, `_AppSettings`, `_Theme`, `_View`, `_Version`. Note these are also kept in **alphabetical order** by the name after the `_` — scroll the field block in `DataModel.cs` and you will see `_ActiveUsers, _AdminCustomLoginProvider, _AllTenants, _AppOnline, _AppSettings, …` running A→Z.
- In server-side code (DataAccess and friends), fields are `_camelCase`: `_connectionString`, `_cookiePrefix`, `_fingerprint`, `_tokenAutoRenew`, `_tokenDays`.

**The two exceptions — when NOT to use `_`:**

**Rule 5b — dependency-injected service/context fields get NO underscore.** The same DI idea from Section 3: when a field holds an injected collaborator, it is named in plain camelCase with no underscore. In `DataController.cs`: `private IDataAccess da;`, `private IConfigurationHelper configurationHelper;`, `private Plugins.IPlugins plugins;`. In `DataAccess.cs`, the Entity Framework database context is `private EFDataModel data;` — no underscore. (Entity Framework, or "EF," is the library that maps C# objects to database tables.)

**Rule 5c — `CurrentUser` and `TenantId` are PascalCase with NO underscore,** even though they are private fields: `private DataObjects.User CurrentUser;` and `private Guid TenantId = Guid.Empty;`. These two carry semantic weight throughout the request, so they are named like the important values they are rather than like ordinary backing fields. (A `Guid` is a globally-unique identifier — a long random ID used as a primary key.)

**A `.razor` nuance.** In Blazor component files (`.razor`), the `@code` block mixes the conventions: backing-style fields get the underscore (`_afterLoadedCalled`, `_plugin`), while short-lived view-state locals sometimes omit it (`email`, `password`, `confirmPassword`). Match the surrounding code.

A quick way to remember the whole section: *underscore is the default for private fields; drop it for injected services and for the two special fields `CurrentUser` / `TenantId`.*

---

<a id="enforcement"></a>
## 6. Applying and Enforcing the Style

Knowing the rules is half the job; knowing *who* enforces each one tells you when the IDE will fix it for you and when you have to be careful by hand.

**The two enforcers.**

1. **The `.editorconfig` (mechanical).** An **`.editorconfig`** is a plain-text file at the repo root (`FreeCRM/.editorconfig`, marked `root = true`) that the C# formatter and analyzers read automatically. It encodes the rules a tool *can* apply: 4-space indentation, CRLF line endings, no final newline at end of file (`insert_final_newline = false`), the methods/types brace-on-new-line behavior (`csharp_new_line_before_open_brace = types,methods`), keeping `} catch`/`} else` together (`csharp_new_line_before_catch = false`, etc.), the space after control-flow keywords, and "do not put System usings first" (`dotnet_sort_system_directives_first = false`). When you reformat a file in the IDE, these get applied for free. [053](053_editorconfig-enforcement.md) walks through this file in detail.

2. **The author's hand (manual).** Several rules cannot be expressed in `.editorconfig`, or actively *contradict* it, so they live in human discipline and code review.

**Where the hand style overrides the config — memorize these, because the tool will not help you:**

- **`String.Empty` (capital S).** The `.editorconfig` sets `dotnet_style_predefined_type_for_member_access = true`, which would prefer the lowercase keyword `string.Empty`. The author uses **`String.Empty`** anyway, 450+ times. The hand convention wins.
- **PascalCase method parameters.** No editorconfig rule governs parameter casing; the PascalCase-for-business-params convention is purely de facto.
- **The wrapped-parameter `){` collapse** (Rule 2d). The formatter does not produce this shape; you type it by hand. (Proof it is manual: a stray `protected bool AllowLoginTypeOpenId{` with a missing space survives in `Login.razor` — a tool would never have left that.)
- **One-blank-line discipline.** The codebase uses exactly one blank line as a separator and never stacks two or three. But the config sets `dotnet_style_allow_multiple_blank_lines_experimental = true`, meaning the tool will *not* flag extra blanks. You keep this tidy by hand.
- **The `crmHub` / `IsrHub` camelCase exception.** Allowed only because the type-casing naming rule is set to severity `suggestion`, not `error`.

**Practical workflow.** Write your code, let the IDE format it (that handles the mechanical layer), then do a manual pass for the five hand items above before you open a pull request. In review, expect those same five points to be the things a reviewer checks first, since they are exactly the ones automation misses.

---

<a id="quick-reference"></a>
## 7. Quick Reference and Examples

Side-by-side cheat sheet. The left column is wrong for this codebase; the right column matches the author's style. All right-column forms are copied from or consistent with the real source.

**Brace placement**

| ✗ Avoid | ✓ House style |
|---------|---------------|
| `public bool Foo(string x) {` (method brace same line) | `public bool Foo(string x)`<br>`{` (method brace new line) |
| `public string Theme`<br>`{` (property brace new line) | `public string Theme {` (property brace same line) |
| `if(x == null) {` (no space) | `if (x == null) {` (space after keyword) |
| `}`<br>`catch (Exception ex) {` (catch on new line) | `} catch (Exception ex) {` (kept together) |

**Wrapped parameter list**

```csharp
// ✓ House style — '(' on its own line, one param per line, ')' collapses onto '){'
public static void Init
(
    IJSRuntime jSRuntime,
    BlazorDataModel model,
    NavigationManager navigationManager
){
    ...
}
```

**Casing**

| Kind | ✓ House style | Example |
|------|---------------|---------|
| Type / property / method | PascalCase | `BlazorDataModel`, `ActiveUsers`, `AllowedFileType` |
| Local variable | camelCase | `output`, `adminMenuItems` |
| Business method parameter | **PascalCase** | `AppointmentNoteId`, `CurrentUser` |
| DI constructor parameter | camelCase | `httpContextAccessor`, `configHelper` |
| SignalR hub (exception) | camelCase | `crmHub`, `IsrHub`, `signalrHub.cs` |

**Empty strings**

```csharp
// ✗ Avoid
public string Id { get; set; } = "";
// ✓ House style — capital-S String.Empty
public string Id { get; set; } = String.Empty;
```

**Field underscores**

| Field role | ✓ House style | Example |
|------------|---------------|---------|
| Ordinary client backing field | `_PascalCase` | `private string _Version = String.Empty;` |
| Ordinary server backing field | `_camelCase` | `private string _connectionString;` |
| Injected service / EF context | no `_`, camelCase | `private IDataAccess da;`, `private EFDataModel data;` |
| `CurrentUser` / `TenantId` | no `_`, PascalCase | `private Guid TenantId = Guid.Empty;` |

**Bonus idiom — prefer `Try*` over try/catch for validation.** When checking whether a string parses, use the framework's `Try*` methods and discard the unused output with `out _`. Copied from `Helpers.cs`:

```csharp
public static bool IsDate(string value)
{
    return DateTime.TryParse(value, out DateTime tempDate);
}
// and, discarding the parsed value when it is not needed:
return Guid.TryParse(value, out _);
return int.TryParse(value, out _);
```

---

<a id="related-docs"></a>
## 8. Related Docs

- [052 — Where Code Lives and How Comments Sound](052_files-and-comment-voice.md) — file layout and comment voice
- [053 — The Machine Referee: editorconfig and What It Enforces](053_editorconfig-enforcement.md) — the editorconfig that enforces it
- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — staying in sync with upstream

---
*GuidesV2 051 · The Author House Style · drafted 2026-06-04 from source (`FreeCRM/.editorconfig`, `CRM.Client/DataModel.cs`, `CRM.Client/Helpers.cs`, and the authoritative styling research doc 0001).*
