# Foundation Services

Shared runtime services that support UI-level behavior across FreeBlazorExtended hosts.

## Files in this folder

| File | Purpose |
|---|---|
| `NotificationService.cs` | In-memory notification queue for toast-style success, info, warning, and error messages. |

## Usage notes

- Register these services in DI from the host application.
- Keep this folder focused on generic services that can be reused by multiple features.

---

### 🧭 Plain-English Briefing — The Boss Questions

**In one line:** shared runtime services — today, `NotificationService.cs`, an in-memory queue for toast-style success/info/warning/error messages — registered in DI by the host and reused across features.

**Why it exists:** so any component can raise a toast without each feature building its own notification system. **See** the parent [Foundation README](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Foundation/README.md) for how the services fit the library.