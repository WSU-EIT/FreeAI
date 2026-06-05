# 053 — The Machine Referee: editorconfig and What It Enforces

> **Document ID:** 053  ·  **Category:** Style  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Explain the shared editorconfig that auto-applies style and the few places the author hand-overrides it.
> **Audience:** Contributors and collaborators  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 05x (The House Style: Code Conventions) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|----------------|
| 1 | [Why a Machine Referee Matters](#why-it-matters) | What `.editorconfig` is and why automated style beats nagging |
| 2 | [How editorconfig Auto-Applies](#mental-model) | File discovery, the `root` flag, glob sections, the cascade |
| 3 | [The Shared Rules, Element by Element](#rules-reference) | The settings that actually shape FreeCRM code, with examples |
| 4 | [Where the Author Hand-Overrides](#overrides) | The deliberate deviations — `String.Empty` being the big one |
| 5 | [Adding or Changing a Rule](#changing-rules) | How to propose and land a config edit without breaking the fork |
| 6 | [Pitfalls and Verifying Your Editor Obeys](#pitfalls) | Common traps and how to confirm your editor is reading the config |
| 7 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why a Machine Referee Matters

**What `.editorconfig` is, in plain language.** An `.editorconfig` is a small plain-text settings file that lives in a code repository. Your code editor reads it automatically and uses it to format code the same way for everyone — how many spaces an indent is, whether lines end with the Windows or Unix newline character, where curly braces go, and so on. Think of it as a referee that knows the house rules and applies them silently as you type, instead of a human reviewer pointing out "you used tabs here" in a code review.

**Why it matters.** Style arguments are expensive and boring. If every contributor's editor enforces the same indentation and brace placement, then two things happen: nobody wastes review time on whitespace, and the *diff* (the list of changed lines a tool shows when you compare two versions of a file) stays small and meaningful. A clean diff is the whole point — if half the changed lines are just someone's editor re-spacing the file, real bugs hide in the noise. The machine referee keeps the noise out so humans can review the substance.

**Where it lives in FreeCRM.** There is exactly one config, at `FreeCRM/.editorconfig`, and it declares `root = true` (explained in §2). It was last meaningfully shaped in commit `40cd0cd` (2026-03-27), which added the special handling for Blazor `.razor` files. It is the *mechanical baseline* for the codebase.

**The one catch you must internalize early.** The `.editorconfig` is necessary but not sufficient. The original author (Brad Wickett, `wicketbr` upstream) deliberately writes a few things *differently* from what the config would auto-apply — most famously, he writes `String.Empty` (capital `S`) when the config would nudge you toward lowercase `string.Empty`. Because of this, you must **never run a blanket `dotnet format`** (the .NET command-line tool that rewrites a whole project to match the config) across FreeCRM. It would "fix" hundreds of intentional `String.Empty` uses into `string.Empty` and produce an enormous, unwanted diff. The config is the floor, not the final word — §4 covers exactly where the two diverge.

<a id="mental-model"></a>
## 2. How editorconfig Auto-Applies

You don't run `.editorconfig`. Your editor reads it. Here is the mental model for *when* and *how* that happens.

**1. Discovery walks upward.** When you open a file — say `CRM.Client/Helpers.cs` — the editor looks in that file's folder for an `.editorconfig`, then the parent folder, then the grandparent, climbing toward the drive root, collecting every `.editorconfig` it finds along the way. This means a config placed near the top of the repo governs everything beneath it.

**2. The `root` flag stops the climb.** The very first line of the FreeCRM config is a comment, and the second is the rule that matters:

```ini
# Remove the line below if you want to inherit .editorconfig settings from higher directories
root = true
```

`root = true` means "stop here — do not keep walking up to look for more configs on the machine." Why this matters: without it, a stray `.editorconfig` sitting in your `C:\Users\you\` home folder could silently change how repo code is formatted on *your* machine but not a teammate's. Declaring `root = true` makes the repo's style self-contained and reproducible — what you see is what everyone sees.

**3. Glob sections target file types.** Inside the file, settings are grouped under headers in square brackets called *globs* (a glob is a wildcard pattern that matches file names). FreeCRM uses three:

```ini
[*.cs]              # all C# files
[*.{cs,vb}]         # C# and Visual Basic (a duplicate/overlap block)
[*.razor]           # Blazor component files
```

`[*.cs]` matches every C# file; `[*.razor]` matches Blazor's component files (the `.razor` files that mix C# and HTML markup). A setting written under `[*.razor]` applies *only* to those files.

**4. Later, more-specific rules win (the cascade).** When more than one matching section sets the same option, the rule encountered later — and the more specific glob — takes precedence. This is exactly how FreeCRM bends one rule for Blazor: the general `[*.cs]` block says put method/type braces on a new line, but the `[*.razor]` block at the bottom of the file overrides brace placement to "none" for those files. Same option, different answer per file type, resolved by the cascade. You'll see the concrete effect of this in §3.

The practical upshot: open any `.cs` or `.razor` file in an editor that understands EditorConfig (Visual Studio, VS Code with the EditorConfig extension, Rider) and the rules apply with no extra step. §6 covers how to confirm your editor is actually obeying.

<a id="rules-reference"></a>
## 3. The Shared Rules, Element by Element

The config sets well over a hundred options, most of them .NET defaults you'll never think about. This section covers the ones that visibly shape FreeCRM code, grouped by what they do. Each is copied faithfully from `FreeCRM/.editorconfig`.

### Layout basics (all files)

```ini
indent_style = space
indent_size = 4
tab_width = 4
end_of_line = crlf
insert_final_newline = false
```

- **`indent_style = space` / `indent_size = 4`** — indent with four spaces, never tab characters. Why it matters: mixed tabs and spaces are the classic cause of code that looks aligned on one machine and ragged on another.
- **`end_of_line = crlf`** — lines end with the Windows-style carriage-return-plus-line-feed pair. Pinning this stops "every line changed" diffs when a Windows and a non-Windows editor disagree about newlines.
- **`insert_final_newline = false`** — do *not* add a blank newline at the very end of a file. (Many tools add one by default; FreeCRM specifically does not want it.)

### Brace placement — the most visible rule

```ini
# in [*.cs]
csharp_new_line_before_open_brace = types,methods
```

In plain terms: for **types** (classes, structs, interfaces, enums) and **methods**, the opening `{` goes on its *own new line*. For everything else — properties, `if`, `foreach`, `try`/`catch` — the brace stays on the *same line*. So a method looks like this:

```csharp
public async Task<bool> DeleteNote(Guid NoteId)
{
    ...
}
```

…but a property and a loop keep the brace attached:

```csharp
protected bool AllowLoginTypeCustom {
    get { return _allow; }
}

foreach (var user in users) {
    ...
}
```

Now the cascade in action. The `[*.razor]` block at the bottom overrides this for Blazor files:

```ini
[*.razor]
csharp_new_line_before_open_brace = none
```

`none` means *every* brace stays on the same line in `.razor` files — including methods and types — because Blazor markup reads better tightly packed. Same option, opposite answer, decided by which glob matched.

### Control-flow keywords stay attached

```ini
csharp_new_line_before_catch = false
csharp_new_line_before_else = false
csharp_new_line_before_finally = false
csharp_space_after_keywords_in_control_flow_statements = true
```

This produces the familiar "K&R" brace style — `} catch`, `} else`, and `} finally` sit on the same line as the closing brace of the block before them, and there's a space after the keyword (`if (`, `foreach (`, not `if(`):

```csharp
if (rec == null) {
    ...
} catch (Exception ex) {
    ...
}
```

### Explicit types over `var`

```ini
csharp_style_var_for_built_in_types = false
csharp_style_var_when_type_is_apparent = false
csharp_style_var_elsewhere = false
```

`var` is C#'s "let the compiler figure out the type" keyword. All three settings are `false`, so the house style prefers writing the type out: `string output = ...` rather than `var output = ...`. Why: explicit types make a quick read of unfamiliar code easier — you see *what* a variable is without inferring it.

### No `this.` qualifiers, no system-first usings

```ini
dotnet_style_qualification_for_field = false
dotnet_style_qualification_for_property = false
dotnet_style_qualification_for_method = false
dotnet_style_qualification_for_event = false

dotnet_sort_system_directives_first = false
dotnet_separate_import_directive_groups = false
```

- The `qualification_for_*` settings being `false` means you write `output` not `this.output` — no redundant `this.` prefix.
- `dotnet_sort_system_directives_first = false` means `using` lines (the imports at the top of a C# file) are **not** sorted with `System.*` first. App namespaces and `System.*` sit in one alphabetical list together. (This surprises people used to the old Visual Studio default of system-first.)

### Naming conventions (severity: suggestion)

```ini
dotnet_naming_rule.interface_should_be_begins_with_i.severity = suggestion
dotnet_naming_rule.types_should_be_pascal_case.severity = suggestion
dotnet_naming_rule.non_field_members_should_be_pascal_case.severity = suggestion
```

Three naming rules: interfaces start with `I` (`IDataAccess`), types are PascalCase, and non-field members (properties, methods, events) are PascalCase. The key word is **`severity = suggestion`**. Severity tells the editor how loudly to complain — `error` blocks the build, `warning` shows a yellow squiggle, and `suggestion` is the gentlest, an easily-missed hint. Because these are only suggestions, FreeCRM can keep deliberate exceptions (the camelCase SignalR hub, see §4) without the build failing.

### The blank-line setting that turns the tool *off*

```ini
dotnet_style_allow_multiple_blank_lines_experimental = true
```

This one matters because of what it does *not* do. `true` here means the formatter **allows** multiple consecutive blank lines — it will not collapse them. So the codebase's tidy "exactly one blank line as a separator" discipline is **not** enforced by the config at all; it's maintained by hand. Don't expect the tool to clean up double blank lines for you — it has been told to leave them alone.

<a id="overrides"></a>
## 4. Where the Author Hand-Overrides

This is the most important section in the doc, because it's where the config and the real code disagree on purpose. The author follows the `.editorconfig` for mechanics but overrides it by hand in a handful of documented spots. If you let a tool "fix" these, you'll fight the entire codebase. There are five.

**1. `String.Empty`, not `string.Empty` — the headline override.** An empty string can be written three ways: the literal `""`, the lowercase keyword form `string.Empty`, or the BCL-type form `String.Empty`. ("BCL" = Base Class Library, the built-in .NET types; `String` with a capital `S` is the actual type, `string` is the lowercase keyword alias for it.) The config contains:

```ini
dotnet_style_predefined_type_for_member_access = true
dotnet_style_predefined_type_for_locals_parameters_members = true
```

Both `true` means the config would prefer the **lowercase keyword** — it wants `string.Empty`. The author does the exact opposite, writing **`String.Empty`** (capital `S`) everywhere — over 450 times across the repo, 122 times in `CRM.Client/Helpers.cs` alone:

```csharp
string output = String.Empty;
if (String.IsNullOrWhiteSpace(className)) {
```

**This is why a blanket `dotnet format` is forbidden.** That command obeys the config, so it would rewrite every `String.Empty` to `string.Empty` and produce a massive, unwanted diff that reverses a deliberate convention. When you write new code, follow the *hand* convention: `String.Empty`, `String.IsNullOrWhiteSpace`, capital `S`. The convention wins over the config.

**2. Method parameters are PascalCase for domain methods.** No `.editorconfig` rule governs parameter casing, so this is purely a hand convention. Business/service method parameters are PascalCase — `DeleteNote(Guid NoteId, DataObjects.User? CurrentUser = null)` — which is unusual versus typical .NET camelCase. (Dependency-injection constructor parameters stay camelCase; the split is covered in 051/052.)

**3. The SignalR hub is camelCase on purpose.** The real-time messaging hub class and interface — `crmHub`, `IsrHub`, in the file `signalrHub.cs` — are lowercase-first, which violates the `types_should_be_pascal_case` rule. This is allowed precisely because that rule's severity is only `suggestion` (§3), not an error. It's a deliberate legacy exception, not a mistake.

**4. The wrapped-parameter `){` collapse is hand-done.** When a method's parameter list is long enough to wrap across multiple lines, the author's style drops the closing `)` onto the same line as the opening brace as `){`. The formatter does not produce this layout — it's typed by hand. (A leftover artifact elsewhere in the codebase, a brace missing its space, proves a tool wasn't the sole driver.)

**5. Single-blank-line discipline is by hand.** As noted in §3, `dotnet_style_allow_multiple_blank_lines_experimental = true` tells the tool *not* to collapse extra blank lines. So the "exactly one blank line between members, around guard clauses, between switch `case`s" rule is maintained manually. The tool will not do it for you.

**Net rule of thumb:** follow `.editorconfig` for the mechanics, then layer these five hand conventions on top — `String.Empty`, PascalCase domain params, the camelCase hub exception, the `){` wrap, and one-blank-line spacing.

<a id="changing-rules"></a>
## 5. Adding or Changing a Rule

Because FreeCRM is a *fork* — a copy of the upstream `wicketbr/FreeCRM` repository that periodically pulls in the original author's changes — editing the shared config has a special hazard the rest of this band doesn't: a careless change here can cause merge conflicts every time you sync upstream. Treat the config as semi-frozen. Here is a safe path.

**Step 1 — Ask whether it belongs in the config at all.** If the thing you want is one of the five *hand* conventions (§4), it does **not** go in `.editorconfig` — it lives in the style docs and in code review, not the config. Putting `String.Empty` enforcement into the config is impossible anyway (the config can only express the opposite preference). Only true mechanical rules belong here.

**Step 2 — Prefer the narrowest glob.** If a rule should only affect Blazor files, add it under `[*.razor]`, not `[*.cs]`. Scoping a change narrowly limits the blast radius and reduces the chance it collides with an upstream edit later.

**Step 3 — Choose severity deliberately.** For naming or style rules, pick `severity` with intent: `suggestion` for "nice to have, don't block anyone," `warning` to make it visible, `error` only when you truly want the build to fail. FreeCRM's existing naming rules are all `suggestion` precisely so the deliberate exceptions in §4 survive.

**Step 4 — Apply locally and inspect the diff before committing.** Change the setting, reformat only the files you're actually touching (your editor does this on save; do **not** run repo-wide `dotnet format`), and read the resulting diff. If the change ripples into files you didn't intend to touch, the rule is too broad — narrow it.

**Step 5 — Expect to defend it at the next upstream sync.** Since the upstream author owns the canonical config, any local divergence you introduce becomes something you must re-reconcile on every merge from `wicketbr:main`. Document *why* in the commit message. When in doubt, propose the change upstream rather than carrying a local fork delta. The fork-sync mechanics are covered in [054](054_fork-sync-discipline.md).

<a id="pitfalls"></a>
## 6. Pitfalls and Verifying Your Editor Obeys

**Pitfall 1 — Running *bare* `dotnet format` repo-wide.** Said three times now because it's the costliest mistake — but the nuance matters. Bare `dotnet format` also runs the *style* pass, which obeys the config and converts hundreds of `String.Empty` into `string.Empty` (and may touch other hand conventions), producing a huge diff that reverses deliberate choices. The **safe** alternative is the `whitespace` subcommand: `dotnet format whitespace` fixes only indentation, braces, and spacing and provably leaves `String.Empty` alone — a whitespace pass over the real repo on 2026-06-05 changed 61 vendored/scaffolded files and **zero** `String.Empty`. So the rule is precise: `dotnet format whitespace` = safe; bare `dotnet format` (or `dotnet format style`) = forbidden. Either way, formatting files individually through your editor's on-save formatting is the everyday path.

**Pitfall 2 — Assuming the tool enforces blank-line spacing.** It doesn't (`allow_multiple_blank_lines_experimental = true`). If you leave double blank lines, no tool flags them — a human reviewer will. Tidy them yourself.

**Pitfall 3 — Expecting `System`-first usings.** `dotnet_sort_system_directives_first = false` means imports are *not* system-first. If your editor auto-sorts with `System.*` on top, it's ignoring the config (see verification below) or using a different "organize usings" command — don't fight the config's ordering.

**Pitfall 4 — A stray `.editorconfig` higher on your disk.** `root = true` is supposed to prevent this, but if you ever copy the repo somewhere unusual or your editor caches an old config, behavior can drift. The repo config is self-contained by design — trust it, and clear caches if formatting looks wrong.

**Pitfall 5 — Editor not reading EditorConfig at all.** Visual Studio and Rider support it natively; **VS Code needs the "EditorConfig for VS Code" extension installed** or it silently ignores the file. No extension, no enforcement.

**How to confirm your editor obeys:**

1. **Indentation test.** Open any `.cs` file, press Tab inside a method. You should get **four spaces**, not a tab character. (Reveal whitespace in your editor to be sure it's spaces.)
2. **Brace test.** Type a new method. The opening `{` should land on its **own line**. Type a new property — its `{` should stay on the **same line**. Open a `.razor` file and the method brace should now stay on the same line too (the cascade override).
3. **Newline test.** Save a file and confirm your editor did **not** add a trailing blank line at the end (`insert_final_newline = false`), and that line endings are CRLF.
4. **Trust the example.** When unsure how something should look, open a recently-touched file like `CRM.Client/Helpers.cs` and match it — the working tree is the most authoritative reference, since it already reflects both the config and the hand overrides.

**Turning on real enforcement (optional but recommended).** The shipped config *applies* these rules when you hit Format Document, but it doesn't *flag* violations — no severity is set on the formatting rule, so a misformatted file stays silent. To make Visual Studio show squiggles (and let CI catch drift), add **one line** to the `[*.cs]` section:

```ini
dotnet_diagnostic.IDE0055.severity = warning   # 'error' for a hard CI gate
```

That single rule, `IDE0055`, is the formatting rule — raising its severity is the only gap between "FreeCRM's config" and "FreeCRM's config that actively enforces itself." A ready-to-use file — a faithful copy of FreeCRM's settings plus this line and a plain-language header — is checked in at the repo root as **`freecrm.editorconfig`** (rename it to `.editorconfig` at a solution root). For a safe, `String.Empty`-preserving CI check, pair it with `dotnet format whitespace --verify-no-changes`.

---

<a id="related-docs"></a>
## 7. Related Docs

- [051 — The Author House Style](051_house-code-style.md) — the parent style charter
- [052 — Where Code Lives and How Comments Sound](052_files-and-comment-voice.md) — file and comment conventions
- [054 — Living on a Fork: Staying in Sync Upstream](054_fork-sync-discipline.md) — sync keeps the config current

---
*GuidesV2 053 · drafted from source (`FreeCRM/.editorconfig`, `0001_freecrm_styling_latest_research.md`) on 2026-06-05.*
