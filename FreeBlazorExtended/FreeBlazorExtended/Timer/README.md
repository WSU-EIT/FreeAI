# Timer

> Drop-in Bootstrap countdown timer with display, progress bar, and Start/Pause/Reset controls. Pure C# — no JS interop.

## What this component does
Counts down from a configurable `Duration` to zero using `System.Timers.Timer` (1-second tick), shows the remaining time as large monospace text, optionally renders a Bootstrap progress bar that turns red in the final 10% of the duration, and exposes Start / Pause / Reset buttons. Fires `OnElapsed` exactly once when the countdown hits zero, plus an optional `OnTick` event each second. The component implements `IDisposable` and detaches/disposes the underlying timer cleanly even if the host disposes it mid-countdown.

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `Timer.razor` | Component + code-behind (display, progress bar, controls, lifecycle) | 165 |

## Dependencies
- **NuGet packages:** `System.Timers` (BCL)
- **Cross-feature dependencies:** none
- **CSS framework:** Bootstrap 5 (uses `progress`, `progress-bar`, `bg-danger`, `btn`, `btn-sm`, `btn-group`)
- **Icons:** Font Awesome (optional — used on control buttons; works without)
- **JS interop:** none

## Cherry-pick instructions
1. Copy the entire `FreeBlazorExtended/Timer/` folder into your project.
2. Add `@using FreeBlazorExtended.Timer` to `_Imports.razor` (or use the fully-qualified tag).
3. Ensure Bootstrap 5 CSS is referenced from your host page.
4. No DI registration, no `Program.cs` changes, no migrations.

## Usage
```razor
@using FreeBlazorExtended.Timer

<Timer @ref="_timer"
       Duration="TimeSpan.FromMinutes(2)"
       AutoStart="false"
       OnElapsed="HandleElapsed"
       OnTick="HandleTick" />

@code {
    private Timer? _timer;

    private void HandleElapsed() {
        // fired exactly once when remaining hits zero
    }

    private void HandleTick(TimeSpan remaining) {
        // optional per-second callback
    }

    // Public API on the @ref:
    // _timer.Start();  _timer.Pause();  _timer.Stop();  _timer.Reset();
}
```

### Parameters
| Parameter | Type | Default | Notes |
|---|---|---|---|
| `Duration` | `TimeSpan` | `5 min` | Total countdown; `Reset()` returns here |
| `ShowProgressBar` | `bool` | `true` | Toggle the Bootstrap progress bar |
| `ShowControls` | `bool` | `true` | Toggle the Start/Pause/Reset buttons |
| `AutoStart` | `bool` | `false` | Begin counting on init |
| `DisplayFormat` | `string` | `@"mm\:ss"` | TimeSpan format for the large display |
| `OnElapsed` | `EventCallback` | — | Fires once when remaining hits zero |
| `OnTick` | `EventCallback<TimeSpan>` | — | Optional per-second callback |
| `Class` | `string` | `""` | Extra CSS class on wrapper |

## Status
- Implementation: **REAL** — extracted from `FreeExamples/Pages/Examples/FreeExamples.App.Pages.TimerDemo.razor` (countdown card)
- Disposal: timer is stopped, event detached, and disposed in `Dispose()`; an internal `_disposed` flag guards mid-tick callbacks
- `OnElapsed` is guarded by `_elapsedFired` so it cannot fire twice in a single countdown
- Known gaps: no millisecond precision (1s tick floor); no audible alert; no laps/intervals — caller can mount multiple instances if needed

## Effort to integrate
**S** — one Razor file, one `@using`, no DI, no JS, no migrations.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** A countdown timer: counts from a `Duration` to zero on a 1-second tick, shows the remaining time big, optionally draws a progress bar (turns red in the final 10%), and has Start/Pause/Reset buttons. Fires `OnElapsed` exactly once at zero (and an optional `OnTick` each second). Pure C#, no JavaScript.

**What tech & where?** One file — [Timer.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Timer/Timer.razor) (uses `System.Timers.Timer`; Bootstrap for the bar).

**Why does this exist?** A reusable countdown (exam timer, session expiry, cooldown) you can drop in and drive with `@ref`.

**What does it beat?** **Correct lifecycle** is the hard part it gets right: it disposes the timer cleanly (a `_disposed` guard blocks mid-tick callbacks) and an `_elapsedFired` flag means `OnElapsed` can't double-fire — common bugs in hand-rolled timers. *(No millisecond precision; no audible alert.)*

**Terminology:** **Tick** — the per-second callback; **elapsed** — the one-shot event at zero.

**The hard part, drawn:**
```
  Start ─▶ System.Timers.Timer (1s) ─▶ decrement remaining ─▶ OnTick(remaining) each second
        progress bar reddens in last 10%
        remaining == 0 ─▶ OnElapsed() ONCE (guarded by _elapsedFired)   Dispose ─▶ clean stop
```
