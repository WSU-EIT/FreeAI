# Foundation Services

Shared runtime services that support UI-level behavior across FreeBlazorExtended hosts.

## Files in this folder

| File | Purpose |
|---|---|
| `NotificationService.cs` | In-memory notification queue for toast-style success, info, warning, and error messages. |

## Usage notes

- Register these services in DI from the host application.
- Keep this folder focused on generic services that can be reused by multiple features.