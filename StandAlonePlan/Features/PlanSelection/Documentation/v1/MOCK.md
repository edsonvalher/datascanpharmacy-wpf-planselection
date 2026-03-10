# Plan Selection — Mock Data Explained
**Version:** 1.0

---

## What Is the Mock?

`MockPlanRepository` is a fake data source.
It pretends to be the COBOL ISAM files the real system reads from.
Instead of opening files or calling COBOL programs, it returns hardcoded data
from in-memory dictionaries.

The mock lives in `Features/PlanSelection/Data/MockPlanRepository.cs`.
It implements `IPlanRepository`, so the rest of the code does not know
or care whether it is talking to the mock or to real COBOL files.

---

## What COBOL Files Does It Replace?

| Mock method | Replaces | Original purpose |
|---|---|---|
| `GetPrimaryPlanCodes(patientNumber)` | **R5FILE** | Holds each patient's up to 3 primary insurance plans |
| `GetPlanMaster(planCode)` | **R11FILE** | Master list of all insurance plans with names and status |
| `GetPatientPlanRecord(patientNumber, planCode)` | **R18FILE** (single read) | Patient-specific plan details: deleted flag, expiration date |
| `GetAllPatientPlanRecords(patientNumber)` | **R18FILE** (sequential scan) | All plans linked to a patient, ordered by plan code |
| `IsCashDisabled()` | **RX1FILE** (`R1A-DISABLE-CASH-ON`) | Pharmacy-level setting that disables the Cash option |

---

## The Three Patient Scenarios

### Patient 1 — Happy Path (Normal mode)

**Purpose:** Shows that a patient with clean, active plans works as expected.

| Plan Code | Plan Name | Status |
|---|---|---|
| `610011` | BCBS Standard | Active |
| `M` | Medicare Part D | Active |
| `MEDCD` | Medicaid | Active |

All three plans load cleanly. No expired dates. Nothing deleted.
The screen shows three rows numbered 1, 2, 3.

---

### Patient 2 — Edge Cases (ShowDeleteAdd mode)

**Purpose:** Shows that the screen handles expired and deleted plans correctly.

| Plan Code | Plan Name | Status | Why |
|---|---|---|---|
| `610011` | BCBS Standard | Active | Normal active plan |
| `EXPD01` | Expired Plan | **EXPIRED** | `ExpirationDate = 2020-01-01` (in the past) |
| `DEL01` | Deleted Plan | **DISABLED** | `IsDeleted = true` in both R11FILE and R18FILE |

In `Normal` mode, only `610011` would appear.
In `ShowDeleteAdd` mode, all three appear so the user can see what is wrong
and decide whether to add a new plan.

---

### Patient 3 — Pagination (Normal mode)

**Purpose:** Shows that the Prev / Next navigation works when there are more than 9 plans.

| Plan Code | Plan Name |
|---|---|
| `PLAN01` | Blue Cross Premier |
| `PLAN02` | Blue Shield Basic |
| `PLAN03` | Aetna Standard |
| `PLAN04` | Cigna Health Plus |
| `PLAN05` | United Health |
| `PLAN06` | Humana Gold |
| `PLAN07` | Tricare Select |
| `PLAN08` | Molina Healthcare |
| `PLAN09` | WellCare Value |
| `PLAN10` | Magellan Health |
| `PLAN11` | Kaiser Silver |
| `PLAN12` | Ambetter Core |

The first page shows PLAN01–PLAN09 (9 items).
The Next button takes you to PLAN10–PLAN12 (3 items).
The Prev button brings you back.

---

## How the Mock Stores Its Data

The mock uses three static dictionaries loaded once when the app starts.
Think of each dictionary as one COBOL ISAM file:

```
PrimaryPlanCodes   →  patient number  →  string[] of up to 3 plan codes
PlanMasters        →  plan code       →  PlanMasterRecord (name, isDeleted)
PatientPlanRecords →  (patient, code) →  PatientPlanRecord (isDeleted, expDate)
```

A fourth dictionary (`AllPatientPlans`) is pre-built from `PatientPlanRecords`
grouped by patient and sorted by plan code. This simulates the sequential scan
of R18FILE that SETPLAN.CBL does in its `START-RX18 / READ-NEXT-RX18` loop.

---

## How to Add a New Test Scenario

1. Pick a new patient number (e.g. `4`).

2. Add an entry to `PrimaryPlanCodes`:
   ```csharp
   [4] = new[] { "MYPLAN", "" , "" }
   ```

3. Add the plan to `PlanMasters`:
   ```csharp
   ["MYPLAN"] = new() { PlanCode = "MYPLAN", Name = "My Custom Plan", IsDeleted = false }
   ```

4. Add the patient-plan record to `PatientPlanRecords`:
   ```csharp
   [(4, "MYPLAN")] = new() { PatientNumber = 4, PlanCode = "MYPLAN", IsDeleted = false }
   ```

5. The static constructor rebuilds `AllPatientPlans` automatically.
   No other changes needed.

6. In the Demo Launcher (`MainWindow.xaml`), add a radio button for Patient 4
   if you want to test it interactively.

---

## How to Replace the Mock with Real COBOL Data

When the real COBOL integration is ready, create a `PlanRepository` class
that also implements `IPlanRepository` and connects to the actual ISAM files.

Then open `App.xaml.cs` and change **one line**:

```csharp
// Before
services.AddSingleton<IPlanRepository, MockPlanRepository>();

// After
services.AddSingleton<IPlanRepository, PlanRepository>();
```

The rest of the codebase — use cases, ViewModel, window, tests — stays exactly the same.
