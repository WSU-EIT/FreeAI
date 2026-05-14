# Feature 101 — DynamicForms

> JSON-defined form schemas with a builder, a renderer, and conditional-visibility/validation rules — no recompile to add a field.

## What this feature does
Stores `FormDefinition` records (a list of `FormField` POCOs) and submissions in memory, then renders them at runtime. Validation rules (min/max length, regex) live on each field as data, not code. A small expression evaluator drives `ConditionalField` so fields can show/hide based on the current values of other fields. Useful any time end-users (not developers) need to define questionnaires, intake forms, or per-tenant custom data capture.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `FormService.cs` | In-memory CRUD, validation engine, conditional-visibility evaluator | 202 |
| `FormDefinition.cs` | `FormDefinition`, `FormField`, `FormSubmission` POCOs | 69 |
| `Components/FormBuilder.razor` | UI for defining fields, types, and rules | n/a |
| `Components/DynamicFormRenderer.razor` | Runtime renderer for an end-user filling out a form | n/a |
| `Components/ConditionalField.razor` | Wraps a field; consults the evaluator to decide visibility | n/a |

## Dependencies
- **NuGet packages:** `System.Text.Json`, `System.Text.RegularExpressions` (both BCL — no extra references)
- **Cross-feature dependencies:** none; uses `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` (root namespace `FreeBlazorExtended`) for `BooleanResponse` and shared DTOs
- **SignalR:** not used
- **EF Core:** not used (currently in-memory `ConcurrentDictionary<Guid, FormDefinition>` with soft-delete via `Deleted` flag)

## DI registration
Add this to your `Program.cs`:
```csharp
builder.Services.AddScoped<FreeBlazorExtended.DynamicForms.FormService>();
```
(For Blazor WASM client also add a Singleton variant — see `FreeBlazorExample/FreeBlazorExample.Client/Program.cs` line 28 for the pattern.)

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/DynamicForms/` folder into your project (includes `Components/` subfolder).
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present (every feature uses these).
3. Add the DI registration above to server `Program.cs` (line 29 in the example) and the Singleton variant to WASM client `Program.cs` (line 28).
4. Either copy a feature-local `_Imports.razor` or add `@using FreeBlazorExtended.DynamicForms` to the target project's existing `_Imports.razor` so the Razor components resolve their `@inject`.
5. EF Core migration not applicable today — see Status.

## Showcase
The interactive demo lives at `/showcase/feature101-dynamic-forms` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature101_DynamicForms.razor`

## Status
- Implementation: **REAL** (in-memory)
- Persistence: in-memory only — needs EF migration before production use
- Known gaps: no `DbSet<FormDefinition>` exists in `FreeBlazorExample.EFModels/EFModels/EFDataModel.cs`; submissions are lost on app restart

## Effort to integrate
**S** — two C# files, three Razor components, one DI line, no external services or hub.
