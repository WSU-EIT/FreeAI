# Foundation Feedback

Feedback-state components for loading, empty, success, and error scenarios.

## Files in this folder

| File | Purpose |
|---|---|
| `EmptyState.razor` | Renders a neutral empty-state message with optional action content. |
| `ErrorMessage.razor` | Shows an error alert with title, message, and optional details. |
| `LoadingSpinner.razor` | Displays a loading spinner with optional message and overlay behavior. |
| `SuccessMessage.razor` | Shows a success confirmation block with optional details and dismissal. |

## Usage notes

- These components are intended for consistent state messaging across dashboards, forms, and feature modules.
- Keep caller-provided messages short so the components remain scannable and accessible.