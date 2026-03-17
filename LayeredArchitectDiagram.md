# Layered Architecture Diagram — Winpharm / POS System

This diagram represents the static structural organization of the Winpharm and POS systems, divided into horizontal layers. Each layer has a defined responsibility and communicates only with the layers directly adjacent to it. It is intended to help both technical stakeholders understand where each technology lives, how the legacy COBOL stack connects to the modern C# .NET interfaces, and which third-party dependency (ScreenIO / GS Runtime) sits between the user-facing UI and the underlying business logic.

---

```mermaid
%%{init: {'theme':'base', 'themeVariables': {
'background':'#0f172a',
'primaryColor':'#1e293b',
'primaryTextColor':'#e2e8f0',
'primaryBorderColor':'#334155',
'lineColor':'#94a3b8',
'fontFamily':'JetBrains Mono, monospace'
}}}%%

flowchart TD

    User(["👤 User"])

    subgraph UI["UI Layer"]
        COBOL_SCREEN["COBOL Screen\n~300 ScreenIO screens"]
        WPF_SCREEN["WPF Screen\nNew UI — C# .NET"]
        WINFORMS["WinForms POS\n~100 POS screens"]
    end

    subgraph THIRDPARTY["Third-Party Layer"]
        SCREENIO["⚠️ ScreenIO / GS Runtime\nGreenhouse Software — 3rd party"]
    end

    subgraph BRIDGE["Bridge Layer"]
        INTEROP["CobolInteropService\nDllImport → COBOL .dll"]
        SHARED["SharedCobolLib\nShared bridge"]
        POSLIB["POSCobolLib\nPOS bridge"]
    end

    subgraph INTEGRATION["Integration Layer — compiled from COBOL (.CBL → .dll)"]
        DCSGUIINT["DCSGUIINT.dll"]
        INTCHG["INTCHG.dll"]
        WORKFLOW["WORKFLOW.dll"]
        INTINTERAC["INTINTERAC.dll"]
        MORE["+ 56 more DLLs"]
    end

    subgraph COBOL_LAYER["COBOL Layer"]
        BUSINESS["COBOL Business Logic\nnewsourc/ — ~400 programs"]
        PANELS["Screen Definitions\nPANELS/ — ~300 .COB files"]
    end

    subgraph DATA["Data Layer"]
        ISAM["ISAM Files\nShared on-disk data"]
        MYSQL["MySQL\nMigration target"]
    end

    User --> COBOL_SCREEN
    User --> WPF_SCREEN
    User --> WINFORMS

    COBOL_SCREEN <--> SCREENIO
    SCREENIO --> INTEROP

    WPF_SCREEN --> INTEROP
    WINFORMS --> POSLIB

    INTEROP --> DCSGUIINT
    INTEROP --> INTCHG
    INTEROP --> WORKFLOW
    INTEROP --> INTINTERAC
    INTEROP --> MORE

    SHARED --> DCSGUIINT
    SHARED --> WORKFLOW
    POSLIB --> WORKFLOW
    POSLIB --> MORE

    DCSGUIINT --> BUSINESS
    INTCHG --> BUSINESS
    WORKFLOW --> BUSINESS
    INTINTERAC --> BUSINESS
    MORE --> BUSINESS

    PANELS --> SCREENIO

    BUSINESS --> ISAM
    WPF_SCREEN -.->|future| MYSQL


    %% UI
    classDef ui fill:#1d4ed8,color:#ffffff,stroke:#3b82f6,stroke-width:2px;

    %% third party
    classDef third fill:#b91c1c,color:#ffffff,stroke:#ef4444,stroke-width:2px;

    %% bridge
    classDef bridge fill:#7c3aed,color:#ffffff,stroke:#a78bfa,stroke-width:2px;

    %% integration
    classDef integration fill:#0ea5e9,color:#ffffff,stroke:#38bdf8,stroke-width:2px;

    %% cobol
    classDef cobol fill:#059669,color:#ffffff,stroke:#10b981,stroke-width:2px;

    %% data
    classDef data fill:#92400e,color:#ffffff,stroke:#f59e0b,stroke-width:2px;

    %% user
    classDef user fill:#475569,color:#ffffff,stroke:#94a3b8,stroke-width:2px;

    class User user

    class COBOL_SCREEN,WPF_SCREEN,WINFORMS ui
    class SCREENIO third
    class INTEROP,SHARED,POSLIB bridge
    class DCSGUIINT,INTCHG,WORKFLOW,INTINTERAC,MORE integration
    class BUSINESS,PANELS cobol
    class ISAM,MYSQL data
```

---

## Layer and component reference

| Level | Layer | Component | Description |
|-------|-------|-----------|-------------|
| 1 | **UI Layer** | COBOL Screen | The legacy graphical interface. Each screen is defined by a `.COB` file in `PANELS/`. Rendered entirely by ScreenIO — no standard Win32 controls. |
| 1 | **UI Layer** | WPF Screen | The new modern interface built in C# .NET using WPF and Clean Architecture. This is the migration target for all COBOL screens. |
| 1 | **UI Layer** | WinForms POS | The Point-of-Sale interface. ~100 forms built in Windows Forms (.NET). Partially being migrated to WPF. |
| 2 | **Third-Party Layer** | ScreenIO / GS Runtime | Proprietary UI engine by Greenhouse Software. Acts as the graphical runtime for COBOL screens — renders windows, captures input, and fires numeric event IDs back to COBOL. Without it, COBOL has no GUI. |
| 3 | **Bridge Layer** | CobolInteropService | C# service that calls compiled COBOL DLLs via `DllImport`. It is the primary entry point from the .NET side into COBOL functionality. |
| 3 | **Bridge Layer** | SharedCobolLib | Shared interop library consumed by both Winpharm and POS. Exposes common COBOL functions to .NET. |
| 3 | **Bridge Layer** | POSCobolLib | POS-specific interop bridge. Exposes COBOL functions required by the POS WinForms layer. |
| 4 | **Integration Layer** | DCSGUIINT.dll | Handles label printing, claim transmission, and display response screens. Compiled from `DCSGUIINT.CBL`. |
| 4 | **Integration Layer** | INTCHG.dll | Opens the Rx Edit screen (Fill Prescription flow). Compiled from `INTCHG.CBL`. |
| 4 | **Integration Layer** | WORKFLOW.dll | Manages workflow state across both Winpharm and POS. Shared dependency between both systems. |
| 4 | **Integration Layer** | INTINTERAC.dll | Performs drug interaction checks. Compiled from `INTINTERAC.CBL`. |
| 4 | **Integration Layer** | +56 more DLLs | Additional compiled COBOL modules in `Integration/`. Source code available as `.CBL` files — not black boxes. |
| 5 | **COBOL Layer** | COBOL Business Logic | Core business rules of the pharmacy system. ~400 programs in `newsourc/` covering prescriptions, insurance plans, patients, drugs, claims, workflow, and more. |
| 5 | **COBOL Layer** | Screen Definitions | ScreenIO panel definitions in `PANELS/`. Each `.COB` file defines the layout, fields, and event IDs for one screen. This inventory equals the full migration backlog. |
| 6 | **Data Layer** | ISAM Files | Legacy indexed sequential access files stored on disk. Shared between Winpharm and POS. No SQL engine — direct file I/O from COBOL. |
| 6 | **Data Layer** | MySQL | The target database for the migrated system. New WPF screens write directly to MySQL, bypassing COBOL entirely. |
