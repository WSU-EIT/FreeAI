# 0001 — Research: FreeCRM Styling, Latest (The Original Author's Explicit Cleanup)

> **Document ID:** 0001
> **Category:** Research
> **Purpose:** Record what the original FreeCRM author has *explicitly* established as his code style during his recent (~2-week) unification pass, with commit-level evidence — and reconcile it against our previously *inferred* guidance in [0000_research_styling.md](0000_research_styling.md).
> **Audience:** Devs, AI agents, contributors.
> **Outcome:** 📖 Authoritative, evidence-backed style spec sourced from the upstream author's own commits.

---

## Why This Doc Exists

[0000_research_styling.md](0000_research_styling.md) consolidated our style guidance — but most of it was **reverse-engineered from reading code**. Since then the original author has gone through the repo and **made his preferences explicit**, with a flagship commit message that literally says *"for consistency with my programming style."*

That changes the standing of our docs: where 0000 guessed, we can now **confirm, tighten, or correct** against the author's actual hand. **This document is authoritative; where it conflicts with 0000, this wins.**

### Who / What / Where

| | |
|---|---|
| **Original author** | **Brad Wickett** — `wicketbr` on GitHub, `brad.wickett@wsu.edu` |
| **Upstream repo** | `wicketbr/FreeCRM` (the public base template = "FreeCRM-main") |
| **Our repo** | `WSU-EIT/FreeCRM`, a fork that periodically merges `wicketbr:main` |
| **Merge commits pulling the cleanup in** | `1d6571a`, `53284a7`, `fb28085`, `eccf97c` ("Merge branch 'wicketbr:main' into main") |
| **Cleanup window** | ~2026-05-13 → 2026-06-04 |
| **Formal config** | `FreeCRM/.editorconfig` (`root = true`) |

### Commit Timeline (styling-relevant)

| Hash | Date | What it established |
|------|------|---------------------|
| `40cd0cd` | 2026-03-27 | Updated `.editorconfig`; added the `[*.razor]` section with `csharp_new_line_before_open_brace = none` — the config foundation. |
| `a21c80e` | 2026-05-13 | Added `_tokenAutoRenew` / `_tokenDays` private fields (underscore convention). |
| `b4cf7e1` | 2026-05-14 | **Moved app-specific private vars `DataAccess.cs` → `DataAccess.App.cs`**; removed unused usings. |
| `863e16a` | 2026-05-18 | "Upgrade FreeCRM.exe" utility — requires app code already in `.App.` format. |
| `e2ecd8f` | 2026-05-19 | **Removed redundant `Helpers.` qualifier** on local static calls. |
| **`d094f99`** | **2026-05-29** | **FLAGSHIP cleanup — 187 files** (100 `.razor`, 82 `.cs`, 4 `.cshtml`, 1 `.csproj`; +1,836 / −2,075). Braces, `String.Empty`, unused usings, dead code, blank-line discipline, deleted `SnippetsOptions.cs`. |

> **Verification:** `git show --stat d094f99` → *"187 files changed, 1836 insertions(+), 2075 deletions(-)"* (net-negative = a genuine cleanup). `String.Empty` now appears **454× across 48 `.cs` files** (more counting `.razor`). Numbers below were checked against the actual repo at `c:\Users\pepkad\source\repo2\FreeCRM`.

---

## 1. Whitespace

**1a — Strip unused `using` directives** (and the blank line that followed them). `d094f99`, `b4cf7e1`

```diff
- using Microsoft.AspNetCore.Hosting.Server.Features;
- using Microsoft.AspNetCore.Mvc.ViewComponents;
- using System.Diagnostics;
- using System.Runtime.CompilerServices;
-
- namespace CRM;
+ namespace CRM;
```

**1b — No double/triple blank lines; collapse to a single blank line.** `d094f99` (`Helpers.cs`)

**1c — Use ONE blank line as a logical separator** — around guard/return blocks and between members. `d094f99` (`DataAccess.Encryption.cs`)

```diff
  string output = String.Empty;
+
  if (ByteArray != null) {
      output = "0x" + BitConverter.ToString(ByteArray).Replace("-", ",0x");
  }
+
  return output;
```

**1d — Blank line between `case` blocks in a `switch`.** `d094f99` (`Users.razor`)

```diff
      case 1:
          Filter.udf01 = value;
          break;
+
      case 2:
          Filter.udf02 = value;
          break;
```

**1e — No trailing blank line just inside a class/interface body** (before the closing `}`). `d094f99`

**1f — `.razor`: exactly one blank line between the directive block and markup, and before `@code`.** `d094f99`

```diff
  @inject BlazorDataModel Model
+
  @if (Model.View == _pageName) {
  ...
  }
+
  @code {
```

**1g — No final newline at end of file** (`insert_final_newline = false`).

> Note: `.editorconfig` sets `dotnet_style_allow_multiple_blank_lines_experimental = true`, so the formatter does **not** flag multiple blanks — the single-blank discipline is enforced **by hand**, not by the tool.

---

## 2. Brace / Bracket Placement (the most distinctive dimension)

Driven by `.editorconfig`:
- `.cs` → `csharp_new_line_before_open_brace = types,methods`
- `.razor` → `csharp_new_line_before_open_brace = none`

**2a — Method & type opening brace → its OWN new line.** `d094f99`

```diff
- public async Task<...> DeleteAppointmentNote(Guid AppointmentNoteId, ... bool ForceDeleteImmediately = false){
+ public async Task<...> DeleteAppointmentNote(Guid AppointmentNoteId, ... bool ForceDeleteImmediately = false)
+ {
```

**2b — Property opening brace → SAME line as the declaration.** This is the author's exact stated rule: *"all opening brackets are on a new line for methods but on the same line for properties."* `d094f99`

```diff
- protected bool AllowLoginTypeCustom
- {
+ protected bool AllowLoginTypeCustom {
      get {
```
`DataModel.cs`: `public string Theme {` / `get { return _Theme; }` — same line.

**2c — Control-flow braces stay on the same line (K&R), with a space after the keyword;** `} catch` / `} else` / `} finally` stay together (`csharp_new_line_before_catch/else/finally = false`). `d094f99`

```diff
- if(rec == null) {
+ if (rec == null) {
- }catch (Exception ex) {
+ } catch (Exception ex) {
- foreach(var apptUser in rec.AppointmentUsers) {
+ foreach (var apptUser in rec.AppointmentUsers) {
```

**2d — DISTINCTIVE wrapped parameter-list style.** When a parameter list wraps, the opening `(` drops to its **own line**, each parameter is one-per-line indented, and the closing `)` collapses with the opening `{` as **`){`** on one line. `d094f99`

```diff
- public static void Init(
+ public static void Init
+ (
      IJSRuntime jSRuntime,
      ...
      NavigationManager navigationManager
- )
- {
+ ){
```
Settled form (current `DataAccess` ctor):
```csharp
public DataAccess(
    string ConnectionString = "",
    ...
    bool UseBackgroundService = false
){
```

**2e — `.razor` multi-line component attributes:** continuation attributes indented one level deeper than the first; the self-closing `/>` collapses onto the **last attribute line** (no line of its own). `d094f99`

```diff
  <PagedRecordset ActionHandlers="ActionHandlers"
-     CenterItems="CenterItems"
+         CenterItems="CenterItems"
-     CheckboxToggleAllAriaLabel="@Helpers.Text("ToggleAllCheckboxes")"
- />
+         CheckboxToggleAllAriaLabel="@Helpers.Text("ToggleAllCheckboxes")" />
```

> These were **hand-edits**: a leftover artifact `protected bool AllowLoginTypeOpenId{` (missing space before `{`) survives in `Login.razor`, proving the formatter wasn't the sole driver.

---

## 3. Capitalization

**3a — Use the BCL type `String` (capital S) for static members** — `String.Empty`, `String.IsNullOrWhiteSpace`, `String.IsNullOrEmpty` — **even though `.editorconfig` would prefer the lowercase keyword** (see §10). `d094f99`

**3b — Method parameters are PascalCase** for domain/business methods: `AppointmentNoteId`, `CurrentUser`, `ForceDeleteImmediately`, `DepartmentGroupId`. (Not 100% uniform — a few utility params remain camelCase, e.g. `compressedByteArray` — but PascalCase is the dominant, deliberate convention.)

**3c — DI constructor parameters are camelCase:** `daInjection`, `httpContextAccessor`, `auth`, `hubContext`, `configHelper`, `diPlugins`, `serviceProvider`. (Value/config ctor params like `ConnectionString`, `DatabaseType` stay PascalCase; injected *services* are camelCase.)

**3d — Local variables are camelCase:** `adminMenuItems`, `output`, `optionsBuilder`, `tenantSettings`, `tenantId`.

**3e — Properties & methods are PascalCase** (`non_field_members_should_be_pascal_case`).

**3f — Hub class/interface are camelCase (deliberate legacy exception):** `public partial class crmHub : Hub<IsrHub>`, `public partial interface IsrHub`, file `signalrHub.cs`; the hub's private field `tenants` carries **no** underscore.

---

## 4. Parameters, Ordering & Sorting

**4a — Private fields are ordered alphabetically** (by name after the `_`). `DataAccess.cs`: `_authenticationProviders, _connectionString, _cookiePrefix, data, _databaseType, _firstInit, _guid1, _guid2, _httpContext, …`. `DataModel.cs`: `_AllTenants, _AppOnline, _AppSettings, _ApplicationUrl, _AuthenticationProviders, …`.

**4b — `using` order: app/library namespaces and `System.*` in one alphabetical group, NOT system-first** (`dotnet_sort_system_directives_first = false`, `dotnet_separate_import_directive_groups = false`). `Helpers.cs`: `CRM.Client.Shared`, `Humanizer`, `Microsoft.*`, `Plugins`, `Radzen`, then `System.*`.

**4c — DTO members grouped logically** (ids → data fields → trailing audit block), **not** strictly alphabetical. `Tag`: `TagId, TenantId, Name, Style, Enabled, …` then `Added, AddedBy, LastModified, LastModifiedBy, Deleted, DeletedAt`.

**4d — Method params: required first, then optional-with-defaults;** `CurrentUser` typically last among the service-style params: `DeleteAppointmentNote(Guid AppointmentNoteId, DataObjects.User? CurrentUser = null, bool ForceDeleteImmediately = false)`.

---

## 5. Underscore Conventions (when `_`, when not)

**5a — DI-injected service/context fields get NO underscore (camelCase).** `DataController.cs`: `private HttpContext? context;`, `private IDataAccess da;`, `private IConfigurationHelper configurationHelper;`, `private Plugins.IPlugins plugins;`. `DataAccess.cs`: the EF context is `private EFDataModel data;` (no underscore).

**5b — `CurrentUser` and `TenantId` are PascalCase with NO underscore**, even though private: `private DataObjects.User CurrentUser;`, `private Guid TenantId = Guid.Empty;`.

**5c — Every other private/protected field gets `_`,** with the post-`_` casing matching the field's natural casing:
- `_camelCase` in server/DataAccess: `_fingerprint`, `_returnCodeAccessDenied`, `_signalR`, `_connectionString`, `_cookiePrefix`, `_tokenAutoRenew`, `_tokenDays`.
- `_PascalCase` in the client `BlazorDataModel`: `_AllTenants`, `_AppSettings`, `_CultureCode`, `_Theme`, `_View`, `_Version`.
- `.razor` `@code` fields are a mix: backing-style fields get `_` (`_afterLoadedCalled`, `_plugin`, `_promptValues`); simple transient view-state locals sometimes omit it (`email`, `password`, `confirmPassword`).

---

## 6. `String.Empty` vs `""`

**Rule — Systematic, repo-wide replacement of the empty-string literal `""` with `String.Empty`.** `d094f99`

Applied to DTO defaults, local inits, assignments, **and even LINQ predicates**:

```diff
- public string Id { get; set; } = "";
+ public string Id { get; set; } = String.Empty;
- private string _ApplicationUrl = "";
+ private string _ApplicationUrl = String.Empty;
```
```diff
  // inside a .Where(...) predicate, DataAccess.Encryption.cs
- ... && x.Password != null && x.Password != "");
+ ... && x.Password != null && x.Password != String.Empty);
```

**Scope (verified):** `String.Empty` appears **454× across 48 `.cs` files** (and more in `.razor`). Empty-string `= ""` defaults are now effectively gone from `.cs`. **Only the *empty* string was converted** — non-empty literals (`"local"`, `".PDF"`, route names, etc.) are intentionally left alone.

---

## 7. Redundant Qualifier Removal

**7a — Drop the `Helpers.` prefix on static calls made from inside the `Helpers` class itself.** `e2ecd8f`

```diff
- return Helpers.Text("Cancel");
+ return Text("Cancel");
- var result = await Helpers.GetOrPost<PluginExecuteResult>("api/Data/ExecutePlugin", request);
+ var result = await GetOrPost<PluginExecuteResult>("api/Data/ExecutePlugin", request);
```
(External callers in `.razor` still write `Helpers.…` — the rule is "drop the qualifier only when the call is local to the type.")

**7b — Drop redundant namespace qualifiers on BCL types when a `using` already covers them.** `d094f99`

```diff
- System.Text.StringBuilder output = new System.Text.StringBuilder();
+ StringBuilder output = new StringBuilder();
```

**7c — No `this.` qualifiers** (`dotnet_style_qualification_for_field/property/method/event = false`).

---

## 8. Partial Classes

**8a — The core types are one logical `partial class` each, split across many files by domain** (interfaces are partial too):
- `DataObjects.*` — `DataObjects.cs`, `.Appointments.cs`, `.Invoices.cs`, `.Tags.cs`, `.SignalR.cs`, `.App.cs`, …
- `DataAccess.*` — `DataAccess.cs` (ctor/EF setup), `.Authenticate.cs`, `.Encryption.cs`, `.Users.cs`, `.Utilities.cs`, `.App.cs`, … (split documented in `CRM.DataAccess/README.md`)
- `DataController.*` — `DataController.cs`, `.Users.cs`, `.Tenants.cs`, `.App.cs`, … (22 files)
- `public partial interface IDataAccess` / `IsrHub` — each domain file contributes its own signatures.

**8b — Move app-specific code OUT of framework partials into `.App.` partials.** `b4cf7e1` relocated app-tunable private vars `DataAccess.cs → DataAccess.App.cs`. Style nuance: when moved, **inline trailing comments were promoted to full-line comments placed above** the field.

```diff
  // in DataAccess.App.cs after the move:
+ // If true, a new token will be sent to the client to keep the token automatically renewed.
+ private bool _tokenAutoRenew = true;
```

---

## 9. The `.App.` File-Naming Convention

**Rule — Every customizable area has a paired `*.App.*` partial holding the app-specific overrides; the framework file holds the built-in implementation.** This isolates customizations so upstream framework files can be replaced wholesale on upgrade (the `863e16a` "Upgrade FreeCRM.exe" utility requires the app already be in `.App.` form).

Representative pairs in the current tree: `DataModel.cs`/`DataModel.App.cs`, `Helpers.cs`/`Helpers.App.cs`, `MainLayout.razor`/`MainLayout.App.razor`, `DataAccess.cs`/`DataAccess.App.cs`, `GraphAPI.cs`/`GraphAPI.App.cs`, `DataController.cs`/`DataController.App.cs`, `Program.cs`/`Program.App.cs`, plus `Shared/AppComponents/*.App.razor`.

**Durability:** these `.App.` files survive structural churn as `R100` (100%-similarity) renames — through the .NET 9→10 upgrade, the folder restructure, and the `ConfigurationHelper.App.cs` move from `CRM` → `CRM.DataObjects`. The naming convention is what makes "find all my code" and "upgrade the framework underneath me" both work.

---

## 10. `.editorconfig` — and Where the Author's HAND Style DIVERGES From It

`FreeCRM/.editorconfig` (`root = true`, last touched `40cd0cd`) encodes the mechanical rules:

| Setting | Effect |
|---------|--------|
| `indent_style = space`, `indent_size = 4`, `end_of_line = crlf`, `insert_final_newline = false` | Layout basics |
| `[*.cs] csharp_new_line_before_open_brace = types,methods` | types/methods → brace new line; everything else same line |
| `[*.razor] csharp_new_line_before_open_brace = none` | ALL braces same line in razor (incl. properties) |
| `csharp_new_line_before_catch/else/finally = false` | `} catch` / `} else` stay together |
| `csharp_style_var_* = false` | prefer explicit types over `var` |
| `dotnet_style_qualification_for_* = false` | no `this.` |
| `csharp_space_after_keywords_in_control_flow_statements = true` | `if (`, `foreach (` |
| `dotnet_sort_system_directives_first = false` | usings not system-first |
| `non_field_members_should_be_pascal_case`, `interface_should_be_begins_with_i` | naming (severity: suggestion) |
| `dotnet_style_allow_multiple_blank_lines_experimental = true` | multiple blanks NOT auto-flagged |

### Divergences — where the author overrides his own config by hand

1. **`String.Empty` (the headline).** `.editorconfig` sets `dotnet_style_predefined_type_for_member_access = true` (and `…for_locals_parameters_members = true`), which would prefer the **lowercase keyword** `string.Empty`. The author instead uses **`String.Empty` / `String.IsNullOrWhiteSpace`** (capital-S BCL type) **everywhere** — the exact opposite, applied 450+ times. **The hand convention wins; follow `String.Empty`.**
2. **Method-parameter casing.** No editorconfig rule governs it; the de-facto rule is **PascalCase for domain method params** — non-standard vs typical .NET camelCase.
3. **Hub camelCase.** `crmHub` / `IsrHub` / `signalrHub.cs` violate `types_should_be_pascal_case` (only a *suggestion*, so not enforced) — a deliberate legacy exception.
4. **Wrapped param `){` collapse (§2d)** is a hand convention — the formatter does not produce it; the missing-space artifact proves it's manual.
5. **Single-blank-line discipline** is enforced by hand despite `allow_multiple_blank_lines = true`.

---

## 11. Other Consistent Rules

- **Delete commented-out / dead code.** `d094f99` stripped old commented implementations and deleted the unused `CRM.Client/Models/SnippetsOptions.cs` entirely. **Exceptions kept verbatim:** template/module markers `// {{ModuleItemStart:X}}` / `// {{ModuleItemEnd:X}}` (they drive the "Remove Modules" tool) and a few deliberate `// Example:` snippets.
- **Prefer `Try*` parse idioms over try/catch for validation.** `d094f99`: `IsDate` → `return DateTime.TryParse(value, out DateTime tempDate);`, `IsGuid` → `Guid.TryParse(value, out _)`, `IsInt` → `int.TryParse(value, out _)` — using discard `out _` when the value is unused.
- **File-scoped namespaces are standard** (`namespace CRM;`) — ~62 `.cs` files file-scoped vs ~10 block-scoped holdouts (hub + a few legacy files).
- **LocalStorage removed** (architectural, but rode in on `d094f99`): theme & culture moved from local storage into `UserPreferences`; no built-in code uses LocalStorage now (the helper methods remain for opt-in use).

---

## 12. Reconciliation — Resolving 0000's Open Questions

0000 listed 8 spots where our inferred guidance was silent or example-only. The author's cleanup answers them:

| # | 0000 open question | **0001 resolution (authoritative)** |
|---|--------------------|--------------------------------------|
| 1 | Property-body brace placement | **Same line** (`public X Foo {`). Methods/types only get the new-line brace. (§2a/§2b) |
| 2 | `""` vs `String.Empty` | **Always `String.Empty`** for the empty string — repo-wide, even in LINQ. (§6) |
| 3 | Blank-line / whitespace policy | **Exactly one** blank line as a separator; collapse doubles; strip dead blanks; blank line between switch `case`s; one blank before `@code`. Enforced by hand. (§1) |
| 4 | Redundant `Helpers.` qualifier | **Drop it** when the call is local to the `Helpers` class. (§7a) |
| 5 | `table-dark` on table headers | Author removed it from some headers "for consistency" (`bb6494f`); treat `table-dark` as optional, not default — match surrounding UI. |
| 6 | Multi-line method-parameter formatting | **`(` on its own line, one param per line, `){` collapsed.** (§2d) |
| 7 | Member/param ordering & sorting | **Private fields alphabetical; usings not system-first; DTO members grouped ids→data→audit; method params required-then-optional with `CurrentUser` last.** (§4) |
| 8 | `.editorconfig` as source of truth | It's the mechanical baseline — but **hand-style overrides it** in 5 documented spots, `String.Empty` being the big one. (§10) |

### Net guidance when writing new FreeCRM code

> Follow `.editorconfig` for the mechanics, **plus** the hand conventions it doesn't capture: `String.Empty` (capital S), method-params PascalCase, DI fields without `_`, `CurrentUser`/`TenantId` without `_`, alphabetical private fields, methods-brace-new-line / properties-brace-same-line / control-flow-brace-same-line, wrapped params as `( … ){`, one-blank-line discipline, `.App.` partials for everything custom, and no dead code/usings/`Helpers.`-self-qualifiers.

---

## Key Source Files (for re-verification)

| File | Demonstrates |
|------|--------------|
| `FreeCRM/.editorconfig` | canonical mechanical config |
| `CRM.Client/Helpers.cs` | wrapped `( … ){`, `String.Empty`, `Try*` idioms, qualifier removal |
| `CRM.Client/DataModel.cs` | `_PascalCase` alphabetical fields, same-line property braces |
| `CRM.DataAccess/DataAccess.cs` + `DataAccess.App.cs` | `_camelCase` fields, no-underscore `data`, partial split, `.App` segregation |
| `CRM/Controllers/DataController.cs` | no-underscore DI fields; `CurrentUser`/`TenantId` PascalCase |
| `CRM/Hubs/signalrHub.cs` | `crmHub`/`IsrHub` camelCase exception |
| `CRM.Client/Pages/Authorization/Login.razor`, `…/Settings/Users/Users.razor` | razor brace/attribute/blank-line rules |
| `CRM.DataAccess/README.md` | documents the `.cs` / `.App.cs` partial split |

*Evidence basis: forensic diff review of commits `40cd0cd`, `a21c80e`, `b4cf7e1`, `863e16a`, `e2ecd8f`, `d094f99` on `wicketbr:main`, verified against the working tree at `c:\Users\pepkad\source\repo2\FreeCRM` (2026-06-04).*

---

*Created: 2026-06-04*
*Maintained by: [Quality]*
*Supersedes (where conflicting): the inferred guidance in [0000_research_styling.md](0000_research_styling.md).*
