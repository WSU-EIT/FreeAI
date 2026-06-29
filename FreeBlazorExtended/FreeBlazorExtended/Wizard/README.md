# Wizard

> Three-piece wizard primitive: a numbered stepper, reusable card-header for navigation, and a selection summary.

## What this folder provides
Building blocks for multi-step wizards. Compose them yourself — there is no `<Wizard>` orchestrator component; you pick the pieces you want.

| Component | Purpose |
|---|---|
| `WizardStepper` | Numbered step circles with connector lines, click-to-go-back, per-step "selected value" preview |
| `WizardStepHeader` | Reusable card-header with Start Over / Back / Next / Finish buttons + optional spinner |
| `WizardSummary` | Compact alert showing previously-made selections as labeled badges |

## Files in this folder
| File | Purpose | LoC |
|---|---|---|
| `WizardStepper.razor` | Numbered stepper visualization | 78 |
| `WizardStepHeader.razor` | Step header with nav buttons | 69 |
| `WizardSummary.razor` | Selection-summary alert | 35 |

## Dependencies
- **NuGet packages:** none
- **CSS:** Bootstrap 5
- **Icons:** FontAwesome (`fa-check`, `fa-arrow-left`, `fa-arrow-right`, `fa-refresh`)
- **JS:** none (pure CSS/Razor)
- **Cross-feature dependencies:** none

## Cherry-pick instructions
1. Copy the `FreeBlazorExtended/Wizard/` folder.
2. Add `@using FreeBlazorExtended.Wizard` to your `_Imports.razor`.
3. Compose your wizard logic page-side; this folder gives you the visual primitives.

## Usage
```razor
<WizardStepper Steps="_steps" CurrentStep="_step"
               OnStepClick="@(i => _step = i)" />
<WizardSummary Selections="_selections" />
<div class="card">
    <WizardStepHeader StepNumber="@(_step + 1)" Title="@_steps[_step].Name"
                      ShowBack="@(_step > 0)" OnBack="@(() => _step--)"
                      ShowNext="@(_step < _steps.Count - 1)" OnNext="@(() => _step++)"
                      ShowFinish="@(_step == _steps.Count - 1)" OnFinish="OnFinish" />
    <div class="card-body">
        @* current step content *@
    </div>
</div>
```

## Status
- Implementation: **REAL** — direct port from FreeExamples (originally adapted from FreeCICD Pipeline Wizard)
- Known gaps: no orchestrator component — you wire step state yourself

## Effort to integrate
**S** — pure Razor, no JS, no external deps beyond Bootstrap + FontAwesome.

---

## 🧭 Plain-English Briefing — The Boss Questions

**How does this work?** Three composable wizard *pieces*, not a black-box wizard: `WizardStepper` (numbered step circles with a per-step value preview), `WizardStepHeader` (a card header with Start Over / Back / Next / Finish buttons), and `WizardSummary` (a badge list of choices made so far). You hold the step state and snap the pieces together.

**What tech & where?** [WizardStepper.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Wizard/WizardStepper.razor) · [WizardStepHeader.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Wizard/WizardStepHeader.razor) · [WizardSummary.razor](https://github.com/WSU-EIT/FreeAI/blob/main/FreeBlazorExtended/FreeBlazorExtended/Wizard/WizardSummary.razor).

**Why does this exist?** For multi-step flows (setup, checkout, onboarding) where you want the standard wizard chrome but full control over the step logic.

**What does it beat?** It's **primitives, not an orchestrator** — you keep control of step order and validation (no fighting a rigid wizard component), while getting consistent, accessible visuals for free.

**Terminology:** **Stepper** — the numbered progress circles; **orchestrator** — the (deliberately absent) component that would own step state.

**The hard part, drawn:**
```
  you own:  int _step + step list
        WizardStepper(steps, _step) ─▶ ①──②──③ progress (click a done step to go back)
        WizardStepHeader ─▶ Back/Next/Finish buttons drive _step
        WizardSummary ─▶ badges of prior choices      (you wire the body of each step)
```
