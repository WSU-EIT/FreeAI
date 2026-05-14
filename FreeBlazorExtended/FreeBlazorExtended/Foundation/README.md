# Foundation

Shared infrastructure layer for FreeBlazorExtended. This folder holds the cross-cutting models, helpers, services, and reusable UI primitives that the feature folders build on.

## What this folder provides

- Shared DTOs and response types used across the library.
- Common table configuration and metadata models.
- Small helper methods reused by services and components.
- Reusable UI primitives grouped under Cards, Feedback, Forms, Layout, and Tables.
- Shared runtime services that support host applications.

## Files in this folder

| File | Purpose |
|---|---|
| `DataObjects.cs` | Shared response and user DTOs used across FreeBlazorExtended features. |
| `DataTableOptions.cs` | Shared option and metadata types for Foundation data-table components. |
| `Helpers.cs` | Utility helpers reused across FreeBlazorExtended services and components. |

## Subfolders

| Folder | Purpose |
|---|---|
| `Components/` | Shared UI primitives grouped by concern. |
| `Services/` | Shared runtime services such as notifications. |

## Dependencies

- Bootstrap classes for layout, spacing, and status styling.
- Font Awesome icons for visual state indicators in several components.
- Consumed by most top-level feature folders in FreeBlazorExtended.