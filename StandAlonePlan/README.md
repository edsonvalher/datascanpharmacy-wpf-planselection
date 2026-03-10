# StandAlonePlan — Functional Requirements

## Overview

StandAlonePlan is a WPF dialog that asks the pharmacist which insurance plan to use for a patient.
It is called from Fill Prescription and Patient Maintenance when a plan selection is needed.
The patient may have up to 27 plans — the dialog loads, filters, and paginates them so the pharmacist
can select one, request a new plan to be added, or cancel.

This project is a COBOL-to-C# migration of `SETPLAN.CBL` from the Winpharm system,
following Clean Architecture with MVVM pattern and Dependency Injection.


## What Are We Building?

A standalone WPF window that lets pharmacy staff pick an insurance plan for a patient.
This screen was originally written in COBOL.Rebuilding it in .NET 4.8 / WPF
so it can run and be tested without any COBOL dependencies.

---

## Requirements

| #  | Requirement | COBOL Source | C# Implementation | Layer | Type | Status |
|----|-------------|--------------|-------------------|-------|------|--------|
| 01 | The dialog is opened by Fill Prescription or Patient Maintenance passing patient number, operation mode, and current selected plan | `SETPLAN.CBL` · `CALL 'SETPLAN'` · LINKAGE SECTION: `PATIENT-RN` · `RUN-OPTION` · `SELECTED-PLAN` | `MainWindow.xaml.cs` · `OpenPlanSelection_Click()` · `factory.Create(patientNumber, mode)` · `ShowDialog()` | UI | Functional | Implemented |
| 02 | If the caller puts HIGH-VALUES in SELECTED-PLAN, cancel is allowed via ESC or F2 | `SETPLAN.CBL` · `ALLOW-BLANK-CODE` · `HIGH-VALUES` check on entry | `PlanSelectionViewModel.cs` · `_allowBlankCode` field · set in constructor when `currentSelectedPlan == null` | ViewModel | Functional | Implemented |
| 03 | Check whether Cash plan is disabled for this pharmacy before loading plans | `SETPLAN.CBL` · `CALL RX1FILE` · flag `R1A-DISABLE-CASH-ON` | `MockPlanRepository.cs` · `IsCashDisabled()` · `IPlanRepository` contract | Data | Functional | Mock |
| 04 | Load up to 3 primary plans assigned to the patient | `SETPLAN.CBL` · `CALL RX5FILE` · `R5-PP(1)` `R5-PP(2)` `R5-PP(3)` | `MockPlanRepository.cs` · `GetPrimaryPlanCodes(int)` · `IPlanRepository` contract | Data | Functional | Mock |
| 05 | Validate each plan exists in the master catalog | `SETPLAN.CBL` · `CALL RX11FILE` · alternate index `AIX1` · `STATUS-NOT-FOUND` check | `MockPlanRepository.cs` · `GetPlanMaster(string)` · returns `PlanMasterRecord?` · null = not found | Data | Functional | Mock |
| 06 | Validate each plan is active for the specific patient checking deleted flag and expiration date | `SETPLAN.CBL` · `CALL RX18FILE` · prime key `R18-PATRN + R18-PP` · `R18-DELETED` · `R18-EXP-DATE` | `MockPlanRepository.cs` · `GetPatientPlanRecord(int, string)` · returns `PatientPlanRecord?` | Data | Functional | Mock |
| 07 | Scan all additional plans beyond the 3 primary ones using sequential file access | `SETPLAN.CBL` · `START RX18FILE` · `READ-NEXT` loop until `R18-PATRN` changes | `MockPlanRepository.cs` · `GetAllPatientPlanRecords(int)` · pre-sorted index built in static constructor | Data | Functional | Mock |
| 08 | Merge master catalog data and patient-plan relationship into a single plan entity | `SETPLAN.CBL` · `LOAD-PLAN-ARRAY` · merge `R11FILE` + `R18FILE` fields into `PL-CODE` `PL-NAME` | `GetPatientPlansUseCase.cs` · `BuildPlan()` · merges `PlanMasterRecord` + `PatientPlanRecord` → `Plan` | Domain | Technical | Implemented |
| 09 | Mark plans as ACTIVE, EXPIRED, or DISABLED based on deleted flag and expiration date | `SETPLAN.CBL` · `R11-DELETED` · `R18-DELETED` · `R18-EXP-DATE < SYS-DATE` · strings `**EXPIRED**` `**DISABLED**` | `Plan.cs` · computed property `Status` → `PlanStatus` enum · computed property `DisplayName` | Domain | Functional | Implemented |
| 10 | In Normal mode show only active plans with no Add Plan button | `SETPLAN.CBL` · `88 MODE-NORMAL` · `RUN-OPTION = 'N'` | `PlanSelectionMode.cs` · `PlanSelectionMode.Normal` · `GetPatientPlansUseCase.Execute()` filters deleted and expired | Domain | Functional | Implemented |
| 11 | In AllowAdd mode show only active plans and display the Add Plan button | `SETPLAN.CBL` · `88 ALLOW-ADD` · `RUN-OPTION = 'Y'` | `PlanSelectionMode.cs` · `PlanSelectionMode.AllowAdd` · `IsAddPlanVisible = true` in `PlanSelectionViewModel.cs` | Domain | Functional | Implemented |
| 12 | In ShowDeleteAdd mode show all plans including expired and deleted, display Add Plan button | `SETPLAN.CBL` · `88 SHOW-DELETE-ADD` · `RUN-OPTION = 'D'` | `PlanSelectionMode.cs` · `PlanSelectionMode.ShowDeleteAdd` · `showDelAdd = true` in `GetPatientPlansUseCase.Execute()` | Domain | Functional | Implemented |
| 13 | Limit the total plan list to a maximum of 27 plans | `SETPLAN.CBL` · `PLAN-ARRAY-MAX VALUE 27` | `GetPatientPlansUseCase.cs` · `MaxPlans = 27` constant | Domain | Functional | Implemented |
| 14 | Display a maximum of 9 plans per page | `SETPLAN.CBL` · `PLAN-1` to `PLAN-9` · 9 panel display fields | `PaginatePlansUseCase.cs` · `PageSize = 9` constant · `Execute()` returns `Skip(start).Take(9)` | Domain | Functional | Implemented |
| 15 | Navigate to previous page of plans | `SETPLAN.CBL` · `PLANSLCT-SEARCH-PREV` · event-id `4` · `SET PLAX DOWN BY MAX-LINE` | `PlanSelectionViewModel.cs` · `PrevCommand` · `ExecutePrev()` · `_pageStart -= PageSize` · `RefreshPage()` | ViewModel | Functional | Implemented |
| 16 | Navigate to next page of plans | `SETPLAN.CBL` · `PLANSLCT-SEARCH-NEXT` · event-id `5` · `SET PLAX UP BY MAX-LINE` | `PlanSelectionViewModel.cs` · `NextCommand` · `ExecuteNext()` · `_pageStart += PageSize` · `RefreshPage()` | ViewModel | Functional | Implemented |
| 17 | Select a plan by typing a number 1 through 9 — auto-submit on first character | `SETPLAN.CBL` · `PLAN-SELECT-H` · event-id `1010` · field-full hot-return | `PlanSelectionWindow.xaml.cs` · `SelectTextBox_TextChanged()` · fires `SelectCommand` · `SelectPlanUseCase.Execute()` | UI | Functional | Implemented |
| 18 | Select a plan by clicking a row in the list | `SETPLAN.CBL` · `PLAN-1-S` to `PLAN-9-S` · event-ids `6001-6009` | `PlanSelectionWindow.xaml.cs` · `PlanListView_MouseLeftButtonUp()` · `PlanSelectionViewModel.SelectByRow()` | UI | Functional | Implemented |
| 19 | Select Cash plan by typing C or A | `SETPLAN.CBL` · `PLAN-SELECT = 'C'` or `'A'` · `WHEN PLAN-SELECT-H` | `SelectPlanUseCase.cs` · `Execute()` · key == "C" or "A" branch · returns `SelectedPlan = key` | Domain | Functional | Implemented |
| 20 | Select Coupon by typing P | `SETPLAN.CBL` · `PLAN-SELECT = 'P'` · `MOVE 'COUPON' TO SELECTED-PLAN` | `SelectPlanUseCase.cs` · `Execute()` · key == "P" branch · returns `SelectedPlan = "COUPON"` | Domain | Functional | Implemented |
| 21 | Request adding a new plan via Add Plan button | `SETPLAN.CBL` · `WHEN PLANSLCT-ADD-PLAN` · `MOVE HIGH-VALUES TO SELECTED-PLAN` | `AddPlanUseCase.cs` · `Execute()` · returns `AddPlanRequested = true` · `PlanSelectionViewModel.AddPlanCommand` | Domain | Functional | Implemented |
| 22 | Cancel the dialog via ESC or F2 only when cancel is allowed | `SETPLAN.CBL` · `PLANSLCT-ESC` event-id `1` · `PLANSLCT-F2` event-id `2` · `ALLOW-BLANK-CODE = 'Y'` check | `PlanSelectionWindow.xaml.cs` · `OnKeyDown()` · `CancelCommand.CanExecute` checks `_allowBlankCode` | UI | Functional | Implemented |
| 23 | Return selected plan code and selected character to the caller | `SETPLAN.CBL` · LINKAGE SECTION · `SELECTED-PLAN PIC X(6)` · `SELECTED-CHAR PIC X` | `PlanSelectionResult.cs` · `SelectedPlan` · `SelectedChar` · read by caller in `MainWindow.OpenPlanSelection_Click()` | Domain | Functional | Implemented |
| 24 | Display plan rows in monospaced format matching COBOL 35-char field layout | `SETPLAN.CBL` · `PLAN-n PIC X(35)` · REDEFINES: number(3) + code(6) + filler(1) + name(25) | `PlanSelectionViewModel.cs` · `PlanPageItem.FullDisplay` · format `{Number}. {PlanCode,-6} {nameDisplay}` · Courier New | UI | Technical | Implemented |


---

## Layer Legend

| Layer | Description |
|-------|-------------|
| UI | `MainWindow` · `PlanSelectionWindow` · XAML · Code-Behind |
| ViewModel | `PlanSelectionViewModel` · Commands · Observable State |
| Domain | Use Cases · Models · Business Rules |
| Data | `IPlanRepository` · `MockPlanRepository` · ISAM file abstraction |

## Status Legend

| Status | Description |
|--------|-------------|
| Implemented | Fully working in current codebase |
| Mock | Implemented with hardcoded test data · requires `RealPlanRepository` for production |
| Pending | Not yet implemented |

## Type Legend

| Type | Description |
|------|-------------|
| Functional | What the system does from the user or caller perspective |
| Technical | How the system is built · architecture · patterns · constraints |

## Diagrams

<details>
<summary><strong>Cobol Files Relationship</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%

flowchart LR

    RX1["RX1FILE
    ─────────────────
    Pharmacy Configuration
    Is Cash disabled?
    R1A-DISABLE-CASH-ON/OFF
    ─────────────────
    Read once at startup
    Lines 15, 64"]

    RX5["RX5FILE
    ─────────────────
    Patient Record
    3 primary plan codes
    R5-PP-1, R5-PP-2, R5-PP-3
    ─────────────────
    Lookup by PATIENT-RN
    Lines 16, 83–84"]

    RX11["RX11FILE
    ─────────────────
    Plan Master Catalog
    Plan display name R11-NAME
    Deleted flag R11-DELETED
    ─────────────────
    Lookup by plan code
    Lines 17, 88–89 · called x3"]

    RX18["RX18FILE
    ─────────────────
    Patient-Plan Link
    Expiry date R18-EXP-DATE
    Disabled flag R18-DELETED
    ─────────────────
    Direct read x3 · Lines 91–93
    Sequential scan · Lines 115–160"]

    SETPLAN["SETPLAN.CBL
    ─────────────────
    Builds PLAN-ARRAY
    up to 27 plans
    Shows 9 at a time
    Returns selected plan"]

    RX1 -->|"1) Is Cash off?"| SETPLAN
    RX5 -->|"2) 3 primary plans"| SETPLAN
    RX11 -->|"3) Plan names x3"| SETPLAN
    RX18 -->|"4) Direct read x3"| SETPLAN
    RX18 -.->|"5) Sequential scan"| SETPLAN

    style RX1 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style RX5 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style RX11 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style RX18 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style SETPLAN fill:#102018,stroke:#34d399,color:#ffffff
```

</details>
<details>
<summary><strong>Cobol Components</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%

flowchart LR

    PLANSLCT["PLANSLCT.COB
    ─────────────────────
    Defines screen structure
    Fields · Events · Switches
    PLANSLCT-1  control block
    PLANSLCT-2  binary layout
    PLANSLCT-4  screen fields
    ─────────────────────
    Does nothing alone"]

    COPY["COPY PLANSLCT
    ─────────────────────
    Line 37 of SETPLAN
    Compiler pastes entire
    PLANSLCT.COB here
    at compile time"]

    SETPLAN["SETPLAN.CBL
    ─────────────────────
    Uses the structure to
    read and write data
    Builds plan list
    Handles user events
    ─────────────────────
    Cannot run without PLANSLCT"]

    GS["GS Runtime
    ScreenIO Engine
    ─────────────────────
    Renders the popup window
    Captures user input
    Fires events back"]

    CALLER["Calling Program
    ─────────────────────
    Patient Maintenance
    Fill Prescription
    etc."]

    PLANSLCT -.->|"structure definition"| COPY
    COPY -->|"merged at compile time"| SETPLAN
    SETPLAN -->|"CALL 'GS' display/wait/close"| GS
    GS -->|"EVENT-ID + user input"| SETPLAN
    CALLER -->|"CALL 'SETPLAN'"| SETPLAN
    SETPLAN -->|"GOBACK + SELECTED-PLAN"| CALLER

    style PLANSLCT fill:#080b10,stroke:#fbbf24,color:#ffffff
    style COPY fill:#0b0f14,stroke:#00d4ff,color:#ffffff
    style SETPLAN fill:#102018,stroke:#34d399,color:#ffffff
    style GS fill:#221533,stroke:#a78bfa,color:#ffffff
    style CALLER fill:#0b2230,stroke:#00d4ff,color:#ffffff
```

</details>
<details>
<summary><strong>Cobol logic workflow</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%

flowchart TD

    CALLER["Caller
    ───────────────────────────────
    Patient Maintenance
    Fill Prescription
    ───────────────────────────────
    Provides: patient number
    Provides: execution mode
    Provides: pre-selected plan or none"]

    SETUP["Setup
    ───────────────────────────────
    Read pharmacy config  RX1FILE
    Is Cash plan allowed?
    Set session rules"]

    BUILD1["Step 1 — Primary plans
    ───────────────────────────────
    Read patient record  RX5FILE
    Up to 3 plans directly assigned
    Validate each against master  RX11FILE
    Validate each against patient  RX18FILE
    Tag each: Active / Expired / Disabled"]

    BUILD2["Step 2 — Additional plans
    ───────────────────────────────
    Scan all patient relationships  RX18FILE
    Find plans beyond the 3 primaries
    Same validation applies
    Deduplicate against Step 1
    Stop at 27 plans total"]

    DISPLAY["Show the list
    ───────────────────────────────
    Display 9 plans per page
    User can page forward and back
    Pre-select a plan if caller provided one"]

    WAIT["Wait for user action
    ───────────────────────────────
    Type a number  1-9
    Type a shortcut  C  A  P
    Click a row
    Page forward or back
    Request Add Plan
    Cancel  if allowed"]

    DECIDE{"What did
    the user do?"}

    OUT1["Plan selected
    ─────────────────────────────
    Return plan code to caller
    Workflow continues"]

    OUT2["Add Plan requested
    ─────────────────────────────
    Signal caller: user wants
    a new plan added
    Caller opens Add Plan flow"]

    OUT3["Cancelled
    ─────────────────────────────
    Only possible if no plan
    was pre-selected
    Caller handles no-selection"]

    CALLER --> SETUP
    SETUP   --> BUILD1
    BUILD1  --> BUILD2
    BUILD2  --> DISPLAY
    DISPLAY --> WAIT
    WAIT    --> DECIDE

    DECIDE -- "selected a plan"    --> OUT1
    DECIDE -- "requested add"      --> OUT2
    DECIDE -- "pressed ESC or F2"  --> OUT3

    style CALLER fill:#0b2230,stroke:#00d4ff,color:#ffffff
    style SETUP fill:#080b10,stroke:#fbbf24,color:#ffffff
    style BUILD1 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style BUILD2 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style DISPLAY fill:#0b0f14,stroke:#a78bfa,color:#ffffff
    style WAIT fill:#0b0f14,stroke:#a78bfa,color:#ffffff
    style DECIDE fill:#161b24,stroke:#00d4ff,color:#ffffff
    style OUT1 fill:#102018,stroke:#34d399,color:#ffffff
    style OUT2 fill:#102018,stroke:#34d399,color:#ffffff
    style OUT3 fill:#161b24,stroke:#64748b,color:#ffffff
```

</details>
<details>
<summary><strong>Mock Data</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%

flowchart LR

    subgraph MOCK ["MockPlanRepository : IPlanRepository"]
        direction LR

        subgraph D1 ["PrimaryPlanCodes — RX5FILE"]
            direction TB
            P1["Patient 1 · key=1
            ---
            R5-PP(1) · 610011
            R5-PP(2) · M
            R5-PP(3) · MEDCD"]

            P2["Patient 2 · key=2
            ---
            R5-PP(1) · 610011
            R5-PP(2) · EXPD01
            R5-PP(3) · DEL01"]

            P3["Patient 3 · key=3
            ---
            R5-PP(1) · PLAN01
            R5-PP(2) · PLAN02
            R5-PP(3) · PLAN03"]
        end

        subgraph D2 ["PlanMasters — RX11FILE"]
            direction TB
            M1["PlanMasterRecord · IsDeleted=false
            ---
            PlanCode · Name
            610011 · BCBS Standard
            M · Medicare Part D
            MEDCD · Medicaid
            EXPD01 · Expired Plan
            PLAN01..PLAN12"]

            M2["PlanMasterRecord · IsDeleted=true
            ---
            PlanCode · Name
            DEL01 · Deleted Plan"]
        end

        subgraph D3 ["PatientPlanRecords — RX18FILE direct key"]
            direction TB
            R1["PatientPlanRecord · Patient 1
            ---
            PatientNumber · PlanCode · IsDeleted · ExpirationDate
            1 · 610011 · false · null
            1 · M · false · null
            1 · MEDCD · false · null"]

            R2["PatientPlanRecord · Patient 2
            ---
            PatientNumber · PlanCode · IsDeleted · ExpirationDate
            2 · 610011 · false · null
            2 · EXPD01 · false · 2020-01-01
            2 · DEL01 · true · null"]

            R3["PatientPlanRecord · Patient 3
            ---
            PatientNumber · PlanCode · IsDeleted · ExpirationDate
            3 · PLAN01..PLAN12 · false · null"]
        end

        subgraph D4 ["AllPatientPlans — RX18FILE sequential"]
            direction TB
            IDX["Dictionary<int, IReadOnlyList<PatientPlanRecord>>
            ---
            key · patientNumber
            value · records OrderBy PlanCode
            built in static constructor"]
        end

        subgraph D5 ["IsCashDisabled — RX1FILE"]
            direction TB
            CASH["bool · return false
            ---
            R1A-DISABLE-CASH-ON
            hardcoded · Mock always false"]
        end

    end

    style MOCK fill:#161b2488,stroke:#fbbf24,color:#ffffff

    style D1 fill:#a78bfa10,stroke:#a78bfa,color:#ffffff
    style D2 fill:#00d4ff10,stroke:#00d4ff,color:#ffffff
    style D3 fill:#fbbf2410,stroke:#fbbf24,color:#ffffff
    style D4 fill:#fbbf2410,stroke:#fbbf24,color:#ffffff
    style D5 fill:#fb923c10,stroke:#fb923c,color:#ffffff

    style P1 fill:#080b10,stroke:#34d399,color:#ffffff
    style P2 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style P3 fill:#080b10,stroke:#a78bfa,color:#ffffff

    style M1 fill:#080b10,stroke:#34d399,color:#ffffff
    style M2 fill:#080b10,stroke:#f87171,color:#ffffff

    style R1 fill:#080b10,stroke:#34d399,color:#ffffff
    style R2 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style R3 fill:#080b10,stroke:#a78bfa,color:#ffffff

    style IDX fill:#080b10,stroke:#fbbf24,color:#ffffff
    style CASH fill:#080b10,stroke:#fb923c,color:#ffffff
```

</details>
<details>
<summary><strong>Clean Architecture Overview</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%
flowchart TD

    subgraph CONFIG ["Configuration"]
        direction LR
        APPSETTINGS["appsettings.json · DataSource: Mock or Real"]
        APPSETTINGSCS["AppSettings.cs · reads json · exposes DataSource string"]
        APPSETTINGS --> APPSETTINGSCS
    end

    subgraph ROOT ["Composition Root — App.xaml.cs · BuildContainer()"]
        direction LR
        APPCS["AddSingleton · IPlanRepository
        AddTransient · UseCases x4
        AddTransient · IPlanSelectionViewModelFactory
        AddTransient · MainWindow
        BuildServiceProvider()"]
    end

    subgraph UI ["UI Layer — Features/PlanSelection/UI"]
        direction LR
        MW["MainWindow.xaml + .cs
        ---
        Demo launcher
        patient + mode picker"]

        FACTORY["PlanSelectionViewModelFactory.cs
        ---
        IPlanSelectionViewModelFactory
        bridges DI + runtime params
        patientNumber · mode"]

        VM["PlanSelectionViewModel.cs
        ---
        ObservableObject
        5 commands · 5 observable properties
        CloseRequested event"]

        PSW["PlanSelectionWindow.xaml + .cs
        ---
        3 event handlers
        DataContext = ViewModel
        ShowDialog · modal"]

        MW --> FACTORY --> VM --> PSW
    end

    subgraph DOMAIN ["Domain Layer — Features/PlanSelection/Domain"]
        direction LR

        subgraph MODELS ["Models"]
            direction LR
            PLAN["Plan.cs
            ---
            PlanCode · Name
            Status · DisplayName · IsCash"]

            MODE["PlanSelectionMode.cs
            ---
            Normal
            AllowAdd
            ShowDeleteAdd"]

            RESULT["PlanSelectionResult.cs
            ---
            SelectedPlan · SelectedChar
            AddPlanRequested · Cancelled"]
        end

        subgraph USECASES ["Use Cases"]
            direction LR
            UC1["GetPatientPlansUseCase
            ---
            LOAD-PLAN-ARRAY
            2 loops · 3 filters · max 27"]

            UC2["SelectPlanUseCase
            ---
            PLAN-SELECT-H
            1-9 · C · A · P"]

            UC3["AddPlanUseCase
            ---
            HIGH-VALUES signal
            AddPlanRequested=true"]

            UC4["PaginatePlansUseCase
            ---
            9 per page
            HasPrev · HasNext"]
        end
    end

    subgraph DATA ["Data Layer — Features/PlanSelection/Data"]
        direction LR

        IFACE["IPlanRepository
        ---
        contract · 5 methods
        PlanMasterRecord
        PatientPlanRecord"]

        MOCK["MockPlanRepository.cs
        ---
        3 static dictionaries
        hardcoded ISAM simulation
        3 patient test scenarios"]

        REAL["RealPlanRepository
        ---
        future · one line swap
        CALL RX1 RX5 RX11 RX18FILE"]

        IFACE --> MOCK
        IFACE -.->|"swap: one line in BuildContainer()"| REAL
    end

    APPSETTINGSCS --> ROOT
    ROOT --> UI
    ROOT --> DATA
    UI --> DOMAIN
    DOMAIN --> DATA

    style CONFIG fill:#1a202c,stroke:#64748b,color:#ffffff
    style ROOT fill:#0b2230,stroke:#00d4ff,color:#ffffff
    style UI fill:#221533,stroke:#a78bfa,color:#ffffff
    style DOMAIN fill:#0f1f1a,stroke:#34d399,color:#ffffff
    style DATA fill:#2b2108,stroke:#fbbf24,color:#ffffff

    style MODELS fill:#102018,stroke:#34d399,color:#ffffff
    style USECASES fill:#102018,stroke:#34d399,color:#ffffff

    classDef configNodes fill:#0b0f14,stroke:#64748b,color:#ffffff
    classDef rootNodes fill:#0b0f14,stroke:#00d4ff,color:#ffffff
    classDef uiNodes fill:#0b0f14,stroke:#a78bfa,color:#ffffff
    classDef domainNodes fill:#0b0f14,stroke:#34d399,color:#ffffff
    classDef dataNodes fill:#0b0f14,stroke:#fbbf24,color:#ffffff

    class APPSETTINGS,APPSETTINGSCS configNodes
    class APPCS rootNodes
    class MW,FACTORY,VM,PSW uiNodes
    class PLAN,MODE,RESULT,UC1,UC2,UC3,UC4 domainNodes
    class IFACE,MOCK,REAL dataNodes
```

</details>
<details>
<summary><strong>Dependency Injection Container</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'9px','background':'#080b10'},'flowchart':{'curve':'basis','nodeSpacing':30,'rankSpacing':40}}}%%
flowchart TD

    STARTUP["App.OnStartup()"]

    subgraph BC ["BuildContainer() — ServiceCollection"]
        direction LR

        subgraph SG ["Singleton"]
            S1["IPlanRepository
            → MockPlanRepository"]
        end

        subgraph TG ["Transient"]
            T1["GetPatientPlansUseCase"]
            T2["SelectPlanUseCase"]
            T3["AddPlanUseCase"]
            T4["PaginatePlansUseCase"]
            T5["IPlanSelectionViewModelFactory
            → PlanSelectionViewModelFactory"]
            T6["MainWindow"]
        end
    end

    subgraph RESOLVE ["DI Resolution"]
        direction LR
        R1["MainWindow"]
        R2["IPlanSelectionViewModelFactory"]
        R3["IPlanRepository · Singleton"]
        R4["GetPatientPlansUseCase · Transient"]
        R5["SelectPlanUseCase · Transient"]
        R6["AddPlanUseCase · Transient"]
        R7["PaginatePlansUseCase · Transient"]

        R1 --> R2
        R2 --> R3
        R2 --> R4
        R2 --> R5
        R2 --> R6
        R2 --> R7
    end

    subgraph RUNTIME ["Runtime — User clicks Open Plan Selection"]
        direction LR
        RT1["patientNumber · mode"]
        RT2["factory.Create(patientNumber, mode)"]
        RT3["new PlanSelectionViewModel"]
        RT4["new PlanSelectionWindow(vm)
        .ShowDialog()"]

        RT1 --> RT2 --> RT3 --> RT4
    end

    STARTUP --> BC --> RESOLVE --> RUNTIME

    style BC fill:#161b2488,stroke:#fbbf24,color:#ffffff
    style SG fill:#fbbf2410,stroke:#fbbf24,color:#ffffff
    style TG fill:#a78bfa10,stroke:#a78bfa,color:#ffffff

    style RESOLVE fill:#0b2230,stroke:#00d4ff,color:#ffffff
    style RUNTIME fill:#0f1f1a,stroke:#34d399,color:#ffffff

    style STARTUP fill:#080b10,stroke:#00d4ff,color:#ffffff

    style S1 fill:#080b10,stroke:#fbbf24,color:#ffffff

    style T1 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style T2 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style T3 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style T4 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style T5 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style T6 fill:#080b10,stroke:#a78bfa,color:#ffffff

    style R1 fill:#080b10,stroke:#00d4ff,color:#ffffff
    style R2 fill:#080b10,stroke:#00d4ff,color:#ffffff
    style R3 fill:#080b10,stroke:#fbbf24,color:#ffffff
    style R4 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style R5 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style R6 fill:#080b10,stroke:#a78bfa,color:#ffffff
    style R7 fill:#080b10,stroke:#a78bfa,color:#ffffff

    style RT1 fill:#080b10,stroke:#34d399,color:#ffffff
    style RT2 fill:#080b10,stroke:#34d399,color:#ffffff
    style RT3 fill:#080b10,stroke:#34d399,color:#ffffff
    style RT4 fill:#080b10,stroke:#34d399,color:#ffffff
```

</details>

<details>
<summary><strong>MVVM Structure</strong></summary>

[View](Features/PlanSelection/Documentation/v1/diagrams/mvvm.svg)

</details>

<details>
<summary><strong>Repository Class Diagram</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'11px','background':'#080b10'}}}%%
classDiagram

    class PlanMasterRecord {
        +string PlanCode
        +string Name
        +bool IsDeleted
        R11-PLAN-CODE PIC X(6)
        R11-NAME PIC X(25)
        R11-DELETED PIC X
    }

    class PatientPlanRecord {
        +int PatientNumber
        +string PlanCode
        +bool IsDeleted
        +DateTime? ExpirationDate
        R18-PATRN PIC 9(9) COMP-3
        R18-PP PIC X(6)
        R18-DELETED PIC X
        R18-EXP-DATE PIC 9(8)
    }

    class IPlanRepository {
        <<interface>>
        +GetPrimaryPlanCodes(int) string[]
        +GetPlanMaster(string) PlanMasterRecord?
        +GetPatientPlanRecord(int,string) PatientPlanRecord?
        +GetAllPatientPlanRecords(int) IReadOnlyList
        +IsCashDisabled() bool
        COBOL RX5FILE
        COBOL RX11FILE AIX1
        COBOL RX18FILE prime key
        COBOL RX18FILE sequential
        COBOL RX1FILE flag
    }

    class MockPlanRepository {
        -PrimaryPlanCodes Dict~int,string[]~
        -PlanMasters Dict~string,PlanMasterRecord~
        -PatientPlanRecords Dict~(int,string),PatientPlanRecord~
        -AllPatientPlans Dict~int,IReadOnlyList~
        +GetPrimaryPlanCodes(int) string[]
        +GetPlanMaster(string) PlanMasterRecord?
        +GetPatientPlanRecord(int,string) PatientPlanRecord?
        +GetAllPatientPlanRecords(int) IReadOnlyList
        +IsCashDisabled() bool
        simulates RX5FILE
        simulates RX11FILE
        simulates RX18FILE direct
        simulates RX18FILE sequential
        simulates RX1FILE
    }

    IPlanRepository <|.. MockPlanRepository : implements
    MockPlanRepository ..> PlanMasterRecord : returns
    MockPlanRepository ..> PatientPlanRecord : returns

    style PlanMasterRecord fill:#102018,stroke:#34d399,color:#ffffff
    style PatientPlanRecord fill:#102018,stroke:#34d399,color:#ffffff
    style IPlanRepository fill:#0b2230,stroke:#00d4ff,color:#ffffff
    style MockPlanRepository fill:#2b2108,stroke:#fbbf24,color:#ffffff
```

</details>

<details>
<summary><strong>Domain Core Class Diagram</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'11px','background':'#080b10'}}}%%
classDiagram

    class Plan {
        +string PlanCode
        +string Name
        +DateTime? ExpirationDate
        +bool IsDeleted
        +bool IsCash
        +PlanStatus Status
        +string DisplayName
        PL-CODE PIC X(6)
        PL-NAME PIC X(25)
        R18-EXP-DATE PIC 9(8)
        R11-DELETED or R18-DELETED
        computed R5-PP == C or A
        computed Active/Expired/Disabled
        computed EXPIRED DISABLED strings
    }

    class PlanStatus {
        <<enumeration>>
        Active
        Expired
        Disabled
    }

    class PlanSelectionMode {
        <<enumeration>>
        Normal
        AllowAdd
        ShowDeleteAdd
        RUN-OPTION N
        RUN-OPTION Y
        RUN-OPTION D
    }

    class PlanSelectionResult {
        +string SelectedPlan
        +string SelectedChar
        +bool AddPlanRequested
        +bool Cancelled
        SELECTED-PLAN PIC X(6)
        SELECTED-CHAR PIC X
        HIGH-VALUES signal
        ALLOW-BLANK-CODE + ESC/F2
    }

    Plan --> PlanStatus : has status

    style Plan fill:#102018,stroke:#34d399,color:#ffffff
    style PlanSelectionMode fill:#0b2230,stroke:#00d4ff,color:#ffffff
    style PlanSelectionResult fill:#2b2108,stroke:#fbbf24,color:#ffffff
    style PlanStatus fill:#221533,stroke:#a78bfa,color:#ffffff
```

</details>

<details>
<summary><strong>Use Cases Class Diagram</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'11px','background':'#080b10'}}}%%
classDiagram

    class GetPatientPlansUseCase {
        -IPlanRepository _repository
        -const int MaxPlans = 27
        +Execute(int patientNumber, PlanSelectionMode mode) IReadOnlyList~Plan~
        -BuildPlan(PlanMasterRecord, PatientPlanRecord?, DateTime) Plan
    }

    class SelectPlanUseCase {
        +Execute(string input, IReadOnlyList~Plan~ page, bool cashDisabled) PlanSelectionResult
    }

    class AddPlanUseCase {
        +Execute(PlanSelectionMode mode) PlanSelectionResult
    }

    class PaginatePlansUseCase {
        -const int PageSize = 9
        +Execute(IReadOnlyList~Plan~ allPlans, int pageStart) PageResult
    }

    class IPlanRepository {
        <<interface>>
        +IsCashDisabled() bool
        +GetPrimaryPlanCodes(int) string[]
        +GetPlanMaster(string) PlanMasterRecord?
        +GetPatientPlanRecord(int, string) PatientPlanRecord?
        +GetAllPatientPlanRecords(int) IReadOnlyList~PatientPlanRecord~
    }

    class Plan {
        +string PlanCode
        +string Name
        +PlanStatus Status
        +string DisplayName
        +bool IsCash
    }

    class PlanSelectionResult {
        +string SelectedPlan
        +string SelectedChar
        +bool AddPlanRequested
        +bool Cancelled
    }

    class PageResult {
        +IReadOnlyList~Plan~ Items
        +bool HasPrev
        +bool HasNext
    }

    GetPatientPlansUseCase --> IPlanRepository : uses
    GetPatientPlansUseCase ..> Plan : creates
    SelectPlanUseCase ..> Plan : reads
    SelectPlanUseCase ..> PlanSelectionResult : returns
    AddPlanUseCase ..> PlanSelectionResult : returns
    PaginatePlansUseCase ..> Plan : paginates
    PaginatePlansUseCase ..> PageResult : returns

    style GetPatientPlansUseCase fill:#0f1f1a,stroke:#34d399,color:#ffffff
    style SelectPlanUseCase fill:#0f1f1a,stroke:#34d399,color:#ffffff
    style AddPlanUseCase fill:#0f1f1a,stroke:#34d399,color:#ffffff
    style PaginatePlansUseCase fill:#0f1f1a,stroke:#34d399,color:#ffffff

    style IPlanRepository fill:#0b2230,stroke:#00d4ff,color:#ffffff

    style Plan fill:#102018,stroke:#34d399,color:#ffffff
    style PlanSelectionResult fill:#2b2108,stroke:#fbbf24,color:#ffffff
    style PageResult fill:#2b2108,stroke:#fbbf24,color:#ffffff
```

</details>

<details>
<summary><strong>UI Layer Class Diagram</strong></summary>

```mermaid
%%{init:{'theme':'base','themeVariables':{'primaryColor':'#161b24','primaryTextColor':'#e2e8f0','primaryBorderColor':'#1c2433','lineColor':'#64748b','edgeLabelBackground':'#0f1319','fontFamily':'JetBrains Mono, monospace','fontSize':'11px','background':'#080b10'}}}%%
classDiagram

    class PlanSelectionWindow {
        -PlanSelectionViewModel ViewModel
        +constructor(PlanSelectionViewModel vm)
        #OnKeyDown(KeyEventArgs e)
        -SelectTextBox_TextChanged(sender, e)
        -PlanListView_MouseLeftButtonUp(sender, e)
        DataContext = viewModel
        CloseRequested += DialogResult = true
        Loaded += SelectTextBox.Focus()
    }

    class XamlLayout {
        <<XAML>>
        Title Plan Selection
        Width=470 Height=340
        WindowStyle SingleBorderWindow
        Row0 Header CODE PLAN NAME
        Row1 ListBox PlanItems FullDisplay
        Row2 TextBox SelectInput MaxLength=1
        Row2 Button AddPlan IsAddPlanVisible
        Row3 Help text static
        Row4 Button Prev CanGoPrev
        Row4 Button Next CanGoNext
        Row4 Button Cancel CancelCommand
    }

    class MainWindow {
        -IPlanSelectionViewModelFactory _vmFactory
        +constructor(IPlanSelectionViewModelFactory)
        -OpenPlanSelection_Click(sender, e)
        picks patientNumber from RadioButtons
        picks mode from RadioButtons
        factory.Create(patientNumber, mode)
        new PlanSelectionWindow(vm).ShowDialog()
        shows MessageBox with Result
    }

    MainWindow ..> PlanSelectionWindow : opens via ShowDialog
    PlanSelectionWindow ..> XamlLayout : defined by
    PlanSelectionWindow ..> PlanSelectionViewModel : DataContext

    style PlanSelectionWindow fill:#221533,stroke:#a78bfa,color:#ffffff
    style XamlLayout fill:#161b2488,stroke:#a78bfa,color:#ffffff
    style MainWindow fill:#0b2230,stroke:#00d4ff,color:#ffffff
```

</details>

## Test Coverage

### Summary

| Test Class | Layer | Tests | What It Covers |
|------------|-------|------:|----------------|
| `GetPatientPlansUseCaseTests` | Domain | 23 | Primary plan loading, additional plan scan, cash disable, deleted/expired filtering per mode, max-27 cap |
| `SelectPlanUseCaseTests` | Domain | 16 | Numeric row selection, cash direct input (C/A/P), invalid input, cash-disabled guard, lowercase normalization |
| `AddPlanUseCaseTests` | Domain | 3 | Add Plan signal (HIGH-VALUES) per mode — Normal blocks it, AllowAdd and ShowDeleteAdd allow it |
| `PaginatePlansUseCaseTests` | Domain | 10 | Page slicing (PageSize=9), Prev/Next flags, negative start clamping, exact plan codes per page |
| `MockPlanRepositoryTests` | Data | 12 | R5FILE primary codes, R11FILE master lookup, R18FILE sequential scan, expiration/deletion flags |
| `PlanSelectionViewModelTests` | ViewModel | 20 | Constructor state, all 5 commands (Select/SelectByRow/Next/Prev/AddPlan/Cancel), pagination, CloseRequested |
| `PlanSelectionViewModelFactoryTests` | ViewModel | 4 | Factory wires patientNumber + mode correctly, PlanItems populated on creation |
| **Total** | | **88** | |

---

### GetPatientPlansUseCaseTests — 23 tests

| Test | What It Verifies |
|------|-----------------|
| `Execute_ThreeActivePrimaryPlans_ReturnsThreePlans` | 3 active primary codes → exactly 3 plans returned |
| `Execute_ActivePrimaryPlan_DisplayNameHasLeadingSpace` | Active plan DisplayName starts with a leading space (mirrors COBOL STRING " " R11-NAME) |
| `Execute_PrimaryCashPlan_CashDisabled_Excluded` | Cash plan code "C" excluded when R1A-DISABLE-CASH-ON is true |
| `Execute_PrimaryCashPlan_CashEnabled_Included` | Cash plan "C" included when Cash is enabled |
| `Execute_PrimaryPlanMasterNotFound_Excluded` | Plan skipped when R11FILE returns STATUS-NOT-FOUND (null) |
| `Execute_PrimaryMasterDeleted_NormalMode_Excluded` | R11-DELETED=true hidden in Normal mode |
| `Execute_PrimaryMasterDeleted_ShowDeleteAdd_IncludedAsDisabled` | R11-DELETED=true shown as **DISABLED** in ShowDeleteAdd mode |
| `Execute_PrimaryR18NotFound_NormalMode_Excluded` | No R18FILE record for a primary plan → plan excluded in Normal mode |
| `Execute_PrimaryR18Deleted_NormalMode_Excluded` | R18-DELETED=true on patient-plan record excluded in Normal mode |
| `Execute_PrimaryR18Deleted_ShowDeleteAdd_IncludedAsDisabled` | R18-DELETED=true shown as **DISABLED** in ShowDeleteAdd mode |
| `Execute_PrimaryR18Expired_NormalMode_Excluded` | Expired plan (R18-EXP-DATE in the past) hidden in Normal mode |
| `Execute_PrimaryR18Expired_ShowDeleteAdd_IncludedAsExpired` | Expired plan shown as **EXPIRED** in ShowDeleteAdd mode |
| `Execute_AdditionalActivePlan_NotPrimary_Included` | Active plan found only in R18FILE sequential scan is added to the list |
| `Execute_AdditionalDuplicateOfPrimary_Excluded` | Plan already in primary list not duplicated from sequential scan |
| `Execute_AdditionalCash_CashDisabled_Excluded` | Cash plan in sequential scan excluded when Cash is disabled |
| `Execute_AdditionalR18Deleted_NormalMode_Excluded` | R18-DELETED=true additional plan excluded in Normal mode |
| `Execute_AdditionalR18Deleted_ShowDeleteAdd_IncludedAsDisabled` | R18-DELETED=true additional plan shown as **DISABLED** in ShowDeleteAdd |
| `Execute_AdditionalR18Expired_NormalMode_Excluded` | Expired additional plan hidden in Normal mode |
| `Execute_AdditionalR18Expired_ShowDeleteAdd_IncludedAsExpired` | Expired additional plan shown as **EXPIRED** in ShowDeleteAdd |
| `Execute_AdditionalR11Deleted_ShowDeleteAdd_IncludedAsDisabled` | R11-DELETED=true on additional plan master shown as **DISABLED** in ShowDeleteAdd |
| `Execute_AdditionalR11Deleted_NormalMode_Excluded` | R11-DELETED=true on additional plan master excluded in Normal mode |
| `Execute_NoPlansAnywhere_ReturnsEmpty` | No primary codes and no R18FILE records → empty result, no exception |
| `Execute_MoreThan27Plans_CappedAt27` | 31 available plans (3 primary + 28 additional) capped at PLAN-ARRAY-MAX = 27 |

---

### SelectPlanUseCaseTests — 16 tests

| Test | What It Verifies |
|------|-----------------|
| `Execute_Input1_ReturnsFirstPlan` | "1" selects row index 0 — SelectedPlan = first plan code, SelectedChar = "1" |
| `Execute_Input3_ReturnsThirdPlan` | "3" selects row index 2 |
| `Execute_Input9_With9Plans_ReturnsNinthPlan` | "9" selects the last row on a full 9-plan page |
| `Execute_Input0_Cancelled` | "0" is out of range (rows are 1–9) → Cancelled |
| `Execute_Input10_Cancelled` | Two-digit "10" exceeds valid range → Cancelled |
| `Execute_Input5_OnlyThreePlans_Cancelled` | "5" with only 3 plans on page → Cancelled (index out of range) |
| `Execute_NumericIndex_PlanIsCash_CashDisabled_Cancelled` | Numeric row that holds a Cash plan blocked when Cash is disabled |
| `Execute_NumericIndex_PlanIsCash_CashEnabled_ReturnsC` | Numeric row holding a Cash plan returns "C" when Cash is enabled |
| `Execute_InputC_CashEnabled_ReturnsCash` | Direct "C" key selects Cash — SelectedPlan = "C", SelectedChar = "C" |
| `Execute_InputA_CashEnabled_ReturnsCash` | Direct "A" key is the alternate Cash key — SelectedPlan = "A" |
| `Execute_InputC_CashDisabled_Cancelled` | Direct "C" key blocked when Cash is disabled |
| `Execute_InputP_ReturnsCoupon` | "P" sets SelectedPlan = "COUPON", SelectedChar = "P" |
| `Execute_EmptyInput_Cancelled` | Empty string input → Cancelled |
| `Execute_WhitespaceInput_Cancelled` | Whitespace-only input treated as empty → Cancelled |
| `Execute_InvalidTextInput_Cancelled` | Unrecognized character "X" → Cancelled |
| `Execute_LowercaseInput_TreatedAsUppercase` | Lowercase "c" normalized to "C" — Cash is selected |

---

### AddPlanUseCaseTests — 3 tests

| Test | What It Verifies |
|------|-----------------|
| `Execute_ModeNormal_ReturnsCancelled` | Normal mode has no Add Plan button — use case returns Cancelled as a defensive guard |
| `Execute_ModeAllowAdd_ReturnsAddPlanRequested` | AllowAdd mode ("Y") — returns AddPlanRequested = true (HIGH-VALUES signal) |
| `Execute_ModeShowDeleteAdd_ReturnsAddPlanRequested` | ShowDeleteAdd mode ("D") — same HIGH-VALUES signal as AllowAdd |

---

### PaginatePlansUseCaseTests — 10 tests

| Test | What It Verifies |
|------|-----------------|
| `Execute_EmptyList_ReturnsEmptyPageNoPrevNoNext` | Zero plans → empty page, HasPrev = false, HasNext = false |
| `Execute_FivePlans_PageStart0_ReturnsFiveItems` | 5 plans fit in one page — all 5 returned, no navigation needed |
| `Execute_ExactlyNinePlans_PageStart0_ReturnsNineNoPrevNoNext` | Exactly 9 plans (PageSize) fills one page — no Prev, no Next |
| `Execute_TenPlans_PageStart0_ReturnsNineAndHasNext` | 10 plans on page 0 — 9 returned, HasNext = true |
| `Execute_TenPlans_PageStart9_ReturnsOneAndHasPrev` | 10 plans on page 9 — 1 remaining, HasPrev = true, HasNext = false |
| `Execute_TwentySevenPlans_PageStart9_HasBothPrevAndNext` | 27 plans on middle page — HasPrev = true, HasNext = true |
| `Execute_TwentySevenPlans_PageStart18_LastPageHasNineAndHasPrev` | Last page of 27 — 9 plans, HasPrev = true, HasNext = false |
| `Execute_NegativePageStart_ClampedToZero` | Negative start clamped to 0 — no exception, reads from beginning |
| `Execute_CorrectPlanCodesOnFirstPage` | First page of 12 — P01 at index 0, P09 at index 8 |
| `Execute_CorrectPlanCodesOnSecondPage` | Second page of 12 (start=9) — P10 at index 0, P12 at index 2 |

---

### MockPlanRepositoryTests — 12 tests

| Test | What It Verifies |
|------|-----------------|
| `GetPrimaryPlanCodes_Patient1_ReturnsThreeActiveCodes` | Patient 1 returns exactly 3 active codes: 610011, M, MEDCD |
| `GetPrimaryPlanCodes_Patient2_ContainsExpiredAndDeleted` | Patient 2 includes EXPD01 (expired) and DEL01 (deleted) |
| `GetPrimaryPlanCodes_Patient3_HasThreePrimaries` | Patient 3 has 3 primary codes (pagination scenario) |
| `GetPrimaryPlanCodes_UnknownPatient_ReturnsEmpty` | Unknown patient returns empty array without exception |
| `GetPlanMaster_ExistingCode_ReturnsMaster` | Known code "610011" returns master record with correct name and IsDeleted = false |
| `GetPlanMaster_DeletedCode_ReturnsDeletedRecord` | "DEL01" returns record with IsDeleted = true |
| `GetPlanMaster_UnknownCode_ReturnsNull` | Unknown code "GHOST" returns null (STATUS-NOT-FOUND equivalent) |
| `GetAllPatientPlanRecords_Patient3_Returns12Records` | Patient 3 sequential scan returns all 12 plan records |
| `GetAllPatientPlanRecords_Patient3_OrderedByPlanCode` | Records are ordered by PlanCode (mirrors ISAM sequential read order) |
| `GetAllPatientPlanRecords_Patient2_ContainsExpiredRecord` | Patient 2 includes EXPD01 with a non-null ExpirationDate |
| `GetAllPatientPlanRecords_UnknownPatient_ReturnsEmpty` | Unknown patient returns empty list without exception |
| `IsCashDisabled_ReturnsFalse` | Mock always returns false — Cash is enabled in all demo scenarios |

---

### PlanSelectionViewModelTests — 20 tests

| Test | What It Verifies |
|------|-----------------|
| `Constructor_Patient1_LoadsThreePlanItems` | Patient 1 has 3 active plans — PlanItems populated with exactly 3 rows |
| `Constructor_Patient3_FirstPageHasNineItems` | Patient 3 has 12 plans — first page shows exactly 9 (PageSize limit) |
| `Constructor_ModeNormal_AddPlanNotVisible` | Normal mode ("N") — IsAddPlanVisible = false |
| `Constructor_ModeAllowAdd_AddPlanVisible` | AllowAdd mode ("Y") — IsAddPlanVisible = true |
| `Constructor_ModeShowDeleteAdd_AddPlanVisible` | ShowDeleteAdd mode ("D") — IsAddPlanVisible = true |
| `Constructor_Patient1_PrevCanExecuteIsFalse` | Patient 1 fits in one page — PrevCommand.CanExecute = false on open |
| `Constructor_Patient3_NextCanExecuteIsTrue` | Patient 3 has 12 plans (2 pages) — NextCommand.CanExecute = true on open |
| `Constructor_Patient1_CanGoNextIsFalse` | Patient 1 fits in one page — CanGoNext observable = false |
| `SelectCommand_ValidInput_SetsResultAndFiresClose` | Input "1" — Result set with plan code, CloseRequested fires |
| `SelectCommand_ValidInput_SelectedPlanMatchesFirstItem` | Input "1" — SelectedPlan matches PlanCode of first PlanItem |
| `SelectCommand_InvalidInput_ResultIsNullAndInputCleared` | Input "X" — CloseRequested does not fire, Result stays null, SelectInput cleared |
| `SelectCommand_InputP_ResultIsCoupon` | Input "P" — SelectedPlan = "COUPON" |
| `SelectByRow_ValidPlan_SetsResultAndFiresClose` | Clicking a row — Result uses the row's PlanCode, SelectedChar = its Number |
| `NextCommand_Patient3_LoadsSecondPageWithThreeItems` | After Next on Patient 3 — second page shows 3 items (12 - 9 = 3) |
| `NextCommand_Patient3_EnablesPrevDisablesNext` | After Next — CanGoPrev = true, CanGoNext = false |
| `PrevCommand_AfterNext_RestoresFirstPageNineItems` | After Next then Prev — first page restored with 9 items, CanGoPrev = false |
| `AddPlanCommand_ModeAllowAdd_SetsAddPlanRequestedAndCloses` | AllowAdd mode — Add Plan sets AddPlanRequested = true and fires CloseRequested |
| `AddPlanCommand_ModeNormal_CanExecuteIsFalse` | Normal mode hides the button — AddPlanCommand.CanExecute = false |
| `CancelCommand_AllowBlankCode_SetsResultCancelledAndCloses` | No pre-selected plan (HIGH-VALUES on entry) — Cancel sets Cancelled = true and closes |
| `CancelCommand_NotAllowBlankCode_CanExecuteIsFalse` | Pre-selected plan was passed — CancelCommand.CanExecute = false (ESC/F2 ignored) |

---

### PlanSelectionViewModelFactoryTests — 4 tests

| Test | What It Verifies |
|------|-----------------|
| `Create_ReturnsViewModelWithCorrectPatientNumber` | Factory passes patientNumber through to the ViewModel correctly |
| `Create_ReturnsViewModelWithCorrectMode` | AllowAdd mode wired through — IsAddPlanVisible = true on the created ViewModel |
| `Create_PlanItemsArePopulated` | Plans are loaded immediately on creation — PlanItems is not empty |
| `Create_ShowDeleteAddMode_IncludesExpiredAndDisabledPlans` | Patient 2 in ShowDeleteAdd — both **EXPIRED** and **DISABLED** appear in PlanItems |

---

### Coverage Gaps

| Gap | Reason |
|-----|--------|
| `Plan.IsCash` for code "A" via direct property call | Only tested indirectly through `SelectPlanUseCase`; no isolated unit test on `Plan.IsCash` with "A" |
| ViewModel constructor with a non-null `currentSelectedPlan` beyond cancel guard | `CancelCommand_NotAllowBlankCode_CanExecuteIsFalse` tests the guard, but initial plan pre-selection display is not verified |
| `AppSettings` class | No unit tests — settings are static infrastructure loaded from config |
| `RealPlanRepository` | Not yet implemented — all data tests run against `MockPlanRepository` |