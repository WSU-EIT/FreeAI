# 026 — The Standard Pass/Fail Result

> **Document ID:** 026  ·  **Category:** Reference  ·  **Status:** ✅ Drafted (content complete)
> **Purpose:** Define the shared boolean-response result type every operation returns so callers handle outcomes uniformly.
> **Audience:** Practitioners building features  ·  **Reader model:** intern-CTO — define jargon on first use, lead with "why it matters."
> **Part of:** GuidesV2 · Band 02x (The Data Stack) · [↑ Back to Index](000_index.md)

---

## In This Doc

| # | Section | What it covers |
|---|---------|--------------------|
| 1 | [Why a Shared Result Type](#why-it-matters) | The problem `BooleanResponse` solves and why every operation uses it |
| 2 | [Anatomy of the Result](#anatomy) | The two fields, plus the `ActionResponseObject` wrapper that embeds it in DTOs |
| 3 | [Field Reference](#field-reference) | `Result` and `Messages`: exact types, defaults, and meaning |
| 4 | [Reading a Result as a Caller](#reading-results) | Branching on `Result` and surfacing `Messages` |
| 5 | [Returning a Result from an Operation](#returning-results) | The "build empty, set true on success, add messages on failure" pattern |
| 6 | [Worked Examples](#examples) | A real delete and a real save, copied from the source |
| 7 | [Pitfalls and Anti-Patterns](#pitfalls) | Ignoring `Result`, swallowing `Messages`, and other traps |
| 8 | [Related Docs](#related-docs) | Parent, sibling, prerequisite, and next-step docs |

---

<a id="why-it-matters"></a>
## 1. Why a Shared Result Type

**Why it matters:** When hundreds of operations each invent their own way of saying "it worked" or "it failed," every caller has to learn a new dialect. One method throws an exception, another returns `null`, a third returns `-1`, a fourth returns a tuple. Callers get sloppy, errors get swallowed, and users see a blank screen instead of a useful message. FreeCRM avoids all of that by making nearly every operation answer the same two questions in the same shape: *Did it succeed?* and *If not, what should I tell the user?*

That shape is a small class called **`BooleanResponse`** (think of "boolean" as just a yes/no flag — `true` means success, `false` means failure). It is defined once, in the data-objects layer, and reused everywhere. A delete returns it. A save tucks it inside the record it returns. A controller hands it straight back to the browser as JSON.

Here is the entire definition — it is deliberately tiny:

```csharp
public partial class BooleanResponse
{
    public List<string> Messages { get; set; } = new List<string>();
    public bool Result { get; set; }
}
```

That is the whole contract. `Result` is the yes/no. `Messages` is a plain list of human-readable strings explaining what happened (usually only populated on failure). Because the type never changes, a caller who knows how to read one `BooleanResponse` knows how to read all of them. That uniformity is the entire point: predictable success/failure handling, with error text that is ready to show a user, at zero per-operation cost.

It lives in the `CRM` namespace, nested inside the partial class `DataObjects`, so its fully-qualified name in code is `DataObjects.BooleanResponse`.

<a id="anatomy"></a>
## 2. Anatomy of the Result

**Why it matters:** There are really only two things to understand here, and a third that explains how the result rides along inside bigger objects. Once you have these three, you can read or write any result in the codebase.

**The result itself — `BooleanResponse`.** Two fields, nothing more:

- `Result` — the pass/fail flag.
- `Messages` — the list of explanations.

**The wrapper — `ActionResponseObject`.** Many operations do not just want to say "it worked." They want to return the actual record (an appointment, a tenant, a tag) *and* report success or failure at the same time. Returning two separate values is awkward, so FreeCRM defines a base class that carries a `BooleanResponse` as a property:

```csharp
public partial class ActionResponseObject
{
    public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
}
```

Any data-transfer object (DTO — a plain class used to move data between layers) that needs to report its own outcome simply **inherits** from `ActionResponseObject`. For example, the `Appointment` DTO is declared as:

```csharp
public partial class Appointment : ActionResponseObject
```

Now an `Appointment` *is* a normal appointment record, but it also has an `ActionResponse` property of type `BooleanResponse` baked in for free. A save method can fill in all the appointment fields *and* set `ActionResponse.Result = true`, returning one object that is both the data and its verdict. The same is true of `ApplicationSettings`, `ConnectionStringConfig`, and many other DTOs.

So the mental model is:

- Standalone yes/no answer → return a `BooleanResponse` directly (e.g., a delete).
- Return-a-record-plus-a-verdict → return a DTO that inherits `ActionResponseObject`, and read its `.ActionResponse`.

<a id="field-reference"></a>
## 3. Field Reference

**Why it matters:** Two fields, but the defaults and conventions around them are what make the type safe to use without null checks.

### `BooleanResponse.Result`

| Property | Value |
|----------|-------|
| **Type** | `bool` |
| **Default** | `false` |
| **Means** | `true` = the operation succeeded; `false` = it did not. |

Because the default is `false`, a freshly-constructed `BooleanResponse` is *already* in the failure state. This is intentional and defensive: an operation must **explicitly opt in** to success by setting `output.Result = true` only after the work has actually completed. If anything goes wrong before that line — an early return, a thrown exception, a missing record — the result stays `false` automatically. You never accidentally report success.

### `BooleanResponse.Messages`

| Property | Value |
|----------|-------|
| **Type** | `List<string>` |
| **Default** | `new List<string>()` (an empty list — never `null`) |
| **Means** | Human-readable explanations, primarily for the failure case. |

Two things to internalize. First, `Messages` is initialized to an empty list, so you can always call `.Any()`, `.Add(...)`, or iterate it without a null check. Second, it is a *list*, not a single string — a single failure can produce several lines (for example, a top-level "Error Saving Appointment" line followed by the unwound details of an exception). On success, `Messages` is usually left empty.

### `ActionResponseObject.ActionResponse`

| Property | Value |
|----------|-------|
| **Type** | `BooleanResponse` |
| **Default** | `new BooleanResponse()` (always present, never `null`) |
| **Means** | The embedded pass/fail verdict for a DTO that returns data *and* status. |

Because it is initialized to a new instance, `someDto.ActionResponse.Result` and `someDto.ActionResponse.Messages` are always safe to touch — you never have to null-check the wrapper before reading the verdict.

<a id="reading-results"></a>
## 4. Reading a Result as a Caller

**Why it matters:** Reading a result is the same two steps every time, so once you have the habit you handle every operation correctly. Step one: check `Result`. Step two: if it is `false`, surface `Messages`.

**Case A — a method returns a `BooleanResponse` directly** (typical of deletes):

```csharp
var result = await da.DeleteTag(id, CurrentUser);
if (result.Result) {
    // success — proceed
} else {
    // failure — result.Messages explains why
}
```

**Case B — a method returns a DTO that inherits `ActionResponseObject`** (typical of saves and loads). The verdict lives one level down, on `.ActionResponse`:

```csharp
if (file.ActionResponse.Result) {
    // the file loaded/saved successfully — use it
}
```

This pattern recurs all over the real codebase. Logged-in checks read `user.ActionResponse.Result`; preview generation reads `preview.ActionResponse.Result`; file storage reads `fileStorage.ActionResponse.Result`. The shape is identical regardless of what the DTO actually carries.

**Surfacing the messages.** When `Result` is `false`, the strings in `Messages` are already written for a human, so you can show them directly. A useful real-world touch: if an operation failed but produced no message, a caller can supply a sensible fallback rather than showing nothing. The authentication flow does exactly this:

```csharp
if (!output.ActionResponse.Result) {
    if (!output.ActionResponse.Messages.Any()) {
        output.ActionResponse.Messages.Add("User Not Logged In");
    }
}
```

The rule of thumb: **never act on the data until you have confirmed `Result` is `true`**, and **never discard `Messages` on failure** — they are the only explanation the user will get.

<a id="returning-results"></a>
## 5. Returning a Result from an Operation

**Why it matters:** If everyone who *produces* a result follows the same recipe, everyone who *reads* one can trust it. The recipe is short and it is followed almost verbatim across the data-access layer.

The pattern has four beats:

1. **Build an empty result.** It starts as a failure (`Result == false`) by default — good, that is the safe state.
2. **Bail early on guard failures.** If a precondition is not met (record missing, not authorized), add a message and return immediately. `Result` stays `false`.
3. **Set `Result = true` only after the real work succeeds.**
4. **On exceptions, add a message and unwind the exception into more messages**, leaving `Result` as `false`.

Here is that recipe in real code — the construction and the early-return guard from `DeleteTag`:

```csharp
var output = new DataObjects.BooleanResponse();

var rec = await data.Tags.FirstOrDefaultAsync(x => x.TagId == TagId);
if (rec == null) {
    output.Messages.Add("Error Deleting Tag '" + TagId.ToString() + "' - Record No Longer Exists");
    return output;
}
```

And the success-and-exception half of the same method:

```csharp
try {
    data.Tags.Remove(rec);
    await data.SaveChangesAsync();

    output.Result = true;
} catch (Exception ex) {
    output.Messages.Add("Error Deleting Tag " + TagId.ToString());
    output.Messages.AddRange(RecurseException(ex));
}
```

Two details worth calling out for a non-engineer:

- **`output.Result = true;` is the last thing inside the `try`.** It runs only if every line above it succeeded. If `SaveChangesAsync()` throws, control jumps to the `catch` and `Result` is never set — so it stays `false`. Success is earned, not assumed.
- **`RecurseException(ex)` returns a `List<string>`**, and `Messages.AddRange(...)` appends all of them. This is how a single failure produces several explanatory lines: a friendly summary first, then the unwound technical detail.

**Returning a DTO instead of a bare result.** When the operation returns a record, the same recipe applies but you set the flag on the embedded wrapper. From a real appointment save:

```csharp
output.ActionResponse.Result = true;
```

…and on failure:

```csharp
output.ActionResponse.Messages.Add("Error Saving Appointment " + output.AppointmentId.ToString() + ":");
output.ActionResponse.Messages.AddRange(RecurseException(ex));
```

Same four beats — the only difference is `output.ActionResponse.X` instead of `output.X`.

**Crossing the wire.** Controllers do not transform the result; they pass it straight through, and the framework serializes it to JSON. A complete controller action is essentially two lines:

```csharp
public async Task<ActionResult<DataObjects.BooleanResponse>> DeleteTag(Guid id)
{
    var output = await da.DeleteTag(id, CurrentUser);
    return Ok(output);
}
```

Because `BooleanResponse` is just two simple properties, it serializes to a small, predictable JSON object that the browser-side code reads exactly the way the server-side code does.

<a id="examples"></a>
## 6. Worked Examples

**Why it matters:** Seeing the success and failure shapes concretely makes the contract click. Both examples below are the JSON shape that a `BooleanResponse` produces over the wire.

### Example 1 — a successful delete

The tag existed and was removed. `Result` flips to `true`; nothing needs explaining, so `Messages` stays empty:

```json
{
  "messages": [],
  "result": true
}
```

A caller reads `result === true` and moves on.

### Example 2 — a failed delete (record already gone)

The guard clause fired because the record no longer exists. `Result` stays `false`, and `Messages` carries the explanation that was added before the early return:

```json
{
  "messages": ["Error Deleting Tag '3f2a...e91' - Record No Longer Exists"],
  "result": false
}
```

A caller reads `result === false` and shows the single message to the user.

### Example 3 — a save returning a DTO, with the verdict embedded

A save returns the record itself (here an appointment) with the verdict nested under `actionResponse`. On success the appointment fields are populated and the embedded result reads `true`:

```json
{
  "appointmentId": "a17c...82b",
  "title": "Site visit",
  "actionResponse": {
    "messages": [],
    "result": true
  }
}
```

On failure the same DTO comes back, but the caller reads `actionResponse.result === false` and surfaces the strings in `actionResponse.messages` — which, after `RecurseException`, may contain several lines: a summary followed by exception detail.

<a id="pitfalls"></a>
## 7. Pitfalls and Anti-Patterns

**Why it matters:** Every one of these defeats the reason the shared result exists. Avoiding them keeps error handling honest and keeps useful messages in front of users.

- **Using the data before checking `Result`.** The default is `false` and stays `false` on any failure path, so trusting the payload without first confirming `Result` (or `ActionResponse.Result`) means acting on a record that may be empty or stale. Always branch on the flag first.

- **Swallowing `Messages` on failure.** If you detect `Result == false` but throw away the `Messages` list, you have discarded the only human-readable explanation the operation produced. Show it, log it, or both — never silently drop it.

- **Setting `Result = true` too early.** Set it *after* the work succeeds, as the last statement inside the `try`. Setting it up front (and hoping to flip it back on error) inverts the safe default and risks reporting success when an exception was swallowed.

- **Catching an exception and forgetting to add a message.** A bare `catch` that leaves `Messages` empty produces a result that says "it failed" with no reason. Follow the source convention: add a summary line, then `Messages.AddRange(RecurseException(ex))`.

- **Null-checking `Messages` or `ActionResponse`.** Both are initialized at construction — `Messages` to an empty list, `ActionResponse` to a new `BooleanResponse`. Treating them as possibly-`null` adds noise and signals a misunderstanding of the type. Call `.Any()` / `.Add(...)` directly.

- **Confusing the two access paths.** A bare `BooleanResponse` exposes `.Result` and `.Messages` directly; a DTO that inherits `ActionResponseObject` exposes them under `.ActionResponse`. Reading `dto.Result` when you meant `dto.ActionResponse.Result` will not compile if you are lucky and will mislead you if the DTO happens to have its own `Result`-like field. Know which one you hold.

- **Replacing the message list instead of adding to it.** Use `Messages.Add(...)` / `Messages.AddRange(...)` so earlier explanatory lines survive. Reassigning the whole list can erase context that an earlier step recorded.

---

<a id="related-docs"></a>
## 8. Related Docs

- [021 — Anatomy of the Layered Data Stack](021_data-stack-anatomy.md) — the full stack overview
- [035 — Validated, Translated, and Reachable](035_validation-localization-a11y.md) — validation reports through it
- [022 — Shaping Records With Nested Partial DTOs](022_nested-partial-dtos.md) — it is itself a DTO

---
*GuidesV2 026 · drafted from source (`CRM.DataObjects/DataObjects.cs`, with usage in `CRM.DataAccess` and `CRM/Controllers`) · 2026-06-05.*
