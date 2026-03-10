# Plan Selection — Requirements & Scope
**Version:** 1.0
**Feature:** PlanSelection
**Project:** StandAlonePlan (Datascan Pharmacy Interview Task)

---

## What Are We Building?

A standalone WPF window that lets pharmacy staff pick an insurance plan for a patient.
This screen was originally written in COBOL. We are rebuilding it in .NET 4.8 / WPF
so it can run and be tested without any COBOL dependencies.

---

## Original COBOL Source

These two files are the reference we used to understand the logic and the screen layout:

- `Winpharm-main/newsourc/SETPLAN.CBL`
  The main program. Handles plan loading, filtering, pagination, and selection.

- `Winpharm-main/newsourc/PANELS/PLANSLCT.COB`
  The screen definition. Describes the popup window, its fields, buttons, and events.

---

## Scope — What This Feature Does

### Plan Loading
- Reads the patient's 3 primary plans from the patient record (R5FILE).
- For each primary plan, checks the plan master (R11FILE) and the patient-plan
  relationship (R18FILE) to decide if the plan should be shown.
- After the 3 primaries, loads any extra plans linked to the patient from R18FILE.
  Duplicates of the primaries are skipped automatically.
- Maximum of 27 plans can be loaded at once.

### Visual States
- **Active** — shows the plan name normally.
- **Expired** — shows `  **EXPIRED**` when the plan's expiration date is in the past.
- **Disabled** — shows `  **DISABLED**` when the plan is marked as deleted.

### Selection Options
- Type `1` through `9` to pick a plan by its number on screen.
- Type `C` or `A` for Cash (if Cash is not disabled in pharmacy config).
- Type `P` for Coupon (stored internally as `COUPON`).
- Double-click a row to select that plan directly.
- Press Enter in the Select field to confirm.

### Pagination
- The screen shows 9 plans at a time.
- Use **Prev** and **Next** buttons to move through the full list.

### Run Modes
| Mode | Code | Behavior |
|---|---|---|
| Normal | `N` | Standard view. Add Plan button is hidden. |
| Allow Add | `Y` | Add Plan button is visible and active. |
| Show Delete + Add | `D` | Shows expired and disabled plans too. Add Plan visible. |

### Add Plan Button
- Only visible in `AllowAdd` and `ShowDeleteAdd` modes.
- When clicked, signals the caller that a new plan should be added.
  (In the original COBOL this was done by returning `HIGH-VALUES` in `SELECTED-PLAN`.)

### Cancel
- ESC or the Cancel button close the screen without selecting anything.
- Cancel only works when the screen was opened without a pre-selected plan.
  (This maps to `ALLOW-BLANK-CODE = 'Y'` in SETPLAN.CBL.)

---

## Out of Scope (v1)

- Real connection to COBOL ISAM files (R5FILE, R11FILE, R18FILE).
  The mock covers all scenarios for now.
- User authentication or session handling.
- Unit tests for XAML / visual layout.
- Adding or editing a plan (that happens in the calling screen).
- Printing or exporting plan data.

---

## Technology

- Framework: **.NET 4.8**
- UI: **WPF**
- Pattern: **MVVM** with `CommunityToolkit.Mvvm 8.4.0`
- DI: **Microsoft.Extensions.DependencyInjection 8.0.0**
- Language: **C#** with `Nullable enable` and `LangVersion preview`
- Tests: **xUnit 2.9.3** + **Moq 4.20.72**

---

## Inputs and Outputs

**Input (when opening the window):**
| Parameter | Type | Description |
|---|---|---|
| `patientNumber` | `int` | Identifies which patient's plans to load |
| `mode` | `PlanSelectionMode` | Controls which plans and buttons are shown |
| `currentSelectedPlan` | `string?` | Pre-selects a plan. Pass `null` to allow cancel. |

**Output (after the window closes):**
| Field | Type | Description |
|---|---|---|
| `SelectedPlan` | `string` | The plan code the user picked (e.g. `610011`, `C`, `COUPON`) |
| `SelectedChar` | `string` | The key or number the user typed |
| `AddPlanRequested` | `bool` | `true` if the user pressed Add Plan |
| `Cancelled` | `bool` | `true` if the user cancelled without picking |

---

## Acceptance Criteria

### Plan List
- [ ] Patient 1 (Normal mode) shows 3 active plans with their names.
- [ ] Patient 2 (ShowDeleteAdd mode) shows expired plan as `**EXPIRED**`
      and deleted plan as `**DISABLED**`.
- [ ] Patient 3 (Normal mode) shows first 9 of 12 plans. Next button is enabled.
- [ ] Plans above the limit of 27 are never shown.

### Selection
- [ ] Typing `1`–`9` selects the matching plan and closes the window.
- [ ] Typing `C` when Cash is enabled returns `SelectedPlan = "C"`.
- [ ] Typing `P` returns `SelectedPlan = "COUPON"`.
- [ ] Typing an invalid key clears the input and keeps the window open.
- [ ] Double-clicking a plan row selects it immediately.

### Add Plan
- [ ] Add Plan button is hidden in Normal mode.
- [ ] Add Plan button is visible in AllowAdd and ShowDeleteAdd modes.
- [ ] Pressing Add Plan sets `AddPlanRequested = true` and closes the window.

### Pagination
- [ ] Prev button is disabled on the first page.
- [ ] Next button is disabled on the last page.
- [ ] Navigating Next then Prev returns to the original first page.

### Cancel
- [ ] Cancel closes the window with `Cancelled = true` when allowed.
- [ ] Cancel button is disabled when a plan was pre-selected on open.
