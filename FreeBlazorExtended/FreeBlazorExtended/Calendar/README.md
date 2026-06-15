# Feature 103 — Calendar

> Events with recurrence rules (daily/weekly/monthly/yearly), resource-conflict detection, and range queries — without bringing in a third-party calendar library.

## What this feature does
Stores `CalendarEvent` records and answers range queries. Recurrence rules are expanded on-demand: pick a `RecurrenceFrequency` plus optional `Until` date or `Count` cap, and the service materializes the occurrences inside a requested window. Conflict detection inspects same-resource overlap on a given range. Soft-delete keeps historical events without breaking referential integrity.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `CalendarEventService.cs` | Range queries, recurrence expansion, conflict detection, soft-delete | 143 |
| `CalendarEvent.cs` | `CalendarEvent`, `RecurrenceRule`, `RecurrenceFrequency`, `EventStatus` | 85 |

## Dependencies
- **NuGet packages:** `System.Text.Json` (BCL)
- **Cross-feature dependencies:** none; uses `Foundation/Helpers.cs` and `Foundation/DataObjects.cs`
- **SignalR:** not used
- **EF Core:** not used (in-memory `ConcurrentDictionary<Guid, CalendarEvent>`)

## DI registration
Add this to your `Program.cs`:
```csharp
builder.Services.AddScoped<FreeBlazorExtended.Calendar.CalendarEventService>();
```
(For Blazor WASM client also add a Singleton variant — see `FreeBlazorExample/FreeBlazorExample.Client/Program.cs` line 30 for the pattern.)

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/Calendar/` folder into your project.
2. Also copy `Foundation/Helpers.cs` and `Foundation/DataObjects.cs` if not already present.
3. Add the DI registration above to server `Program.cs` (line 31 in the example) and the Singleton variant to WASM client `Program.cs` (line 30).
4. There are no Razor components in this folder; render the events inline in your own pages (the showcase page demonstrates a simple month-grid renderer).
5. EF Core migration not applicable today.

## Showcase
The interactive demo lives at `/showcase/feature103-calendar` in the FreeBlazorExample app:
- Page: `FreeBlazorExample/FreeBlazorExample.Client/Pages/Showcase/Feature103_Calendar.razor`

## Status
- Implementation: **REAL** (in-memory)
- Persistence: in-memory only — needs EF migration before production use
- Known gaps: no Razor components shipped (renderer is inline in showcase page); no iCal/.ics import or export; no time-zone handling beyond `DateTime` UTC convention.

## Effort to integrate
**S** — two C# files, one DI line, no Razor components to wire, no external services.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A service that stores events and answers "what's happening between these two dates?" — expanding recurrence rules (daily/weekly/monthly/yearly, with an `Until` date or a `Count` cap) on demand and flagging same-resource conflicts. In-memory today.

**What tech & where?** [CalendarEventService.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Calendar/CalendarEventService.cs) (range queries, recurrence, conflicts) · [CalendarEvent.cs](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Calendar/CalendarEvent.cs) (the models).

**Why does this exist?** To get recurring events and conflict detection without pulling in a heavyweight third-party calendar library.

**What does it beat?** Recurrence is **expanded on demand within the window you ask for** (not pre-materialized), and it detects resource double-booking — with soft-delete so history stays intact. *(Honest: ships no Razor UI — render inline; no iCal import/export; in-memory only.)*

**Terminology:** **Recurrence expansion** — turning one "every Monday" rule into the individual dated occurrences inside a date range.

**The hard part, drawn:**
```
  query(range) ─▶ expand each recurrence rule INTO that window ─▶ check same-resource overlaps ─▶ events (+ conflicts)
```
