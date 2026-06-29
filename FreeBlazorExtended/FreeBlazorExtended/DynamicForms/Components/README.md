# DynamicForms Components

UI components that make up the Feature 101 dynamic-form authoring and runtime experience.

## Files in this folder

| File | Purpose |
|---|---|
| `ConditionalField.razor` | Shows or hides child content based on caller-managed visibility rules. |
| `DynamicFormRenderer.razor` | Renders a published form definition into live interactive inputs. |
| `FormBuilder.razor` | Provides the authoring surface for creating and editing form definitions. |

## Usage notes

- These components sit on top of the shared Foundation form patterns.
- Pair them with `FormDefinition.cs` and `FormService.cs` from the parent folder.

---

### 🧭 Plain-English Briefing — The Boss Questions

**In one line:** the three Razor pieces of Feature 101 — `FormBuilder` (define a form), `DynamicFormRenderer` (fill one out at runtime), and `ConditionalField` (show/hide a field based on other answers).

**Why it exists:** to render JSON-defined forms with no recompile. **See the full feature briefing** (how the schema + validation + conditional logic work) in the parent [DynamicForms README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/DynamicForms/README.md).