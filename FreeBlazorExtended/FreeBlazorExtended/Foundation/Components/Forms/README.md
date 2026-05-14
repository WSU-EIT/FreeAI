# Foundation Forms

Reusable form-building primitives for common structure, actions, and validation output.

## Files in this folder

| File | Purpose |
|---|---|
| `FormActions.razor` | Renders submit, cancel, reset, or custom action content for forms. |
| `FormSection.razor` | Wraps related fields in a titled fieldset with optional description and footer content. |
| `ValidationMessage.razor` | Displays validation feedback with consistent icon and message styling. |

## Usage notes

- These components stay intentionally generic so feature-specific forms can compose them without tight coupling.
- DynamicForms extends these patterns with richer schema-driven components in its own `Components/` folder.