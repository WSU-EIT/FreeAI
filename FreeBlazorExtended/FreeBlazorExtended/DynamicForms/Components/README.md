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