# C3 Backlog — Component Diagrams Scope

Decomposition of all containers identified in C2 into their internal components. Organized by AS-IS and TO-BE states. Where a TO-BE C3 replaces an AS-IS C3, the mirror relationship is made explicit.

---

## AS-IS Component Diagrams

| # | Container | Technology | C3 Covers |
|---|---|---|---|
| [C3-AS-01](C3-AS-01.md) | Winpharm Application | COBOL + ScreenIO | Clinical domains broken down by COBOL program groups (Prescriptions, Patients, Insurance, Transmit, Auto-Refill, Compounding, Labels, Nurse/MTM, Workflow) |
| [C3-AS-02](C3-AS-02.md) | POS Application | WinForms .NET | Commercial modules broken down by WinForms screen groups (Checkout, Payments, Inventory, Accounts, Reports, Employees, Shipping, Delivery) |
| [C3-AS-03](C3-AS-03.md) | COBOL DLL Bridge | Compiled COBOL DLLs | 8 compiled DLLs with their responsibilities, entry points, and which layer calls each one |
| C3-AS-04 | Datascan .NET Layer | C# net48 / net8 | 9 C# projects and their Clean Architecture roles (Core / Application / Interop / Database / WPF) |

---

## TO-BE Component Diagrams

| # | Container | Technology | Mirrors | C3 Covers |
|---|---|---|---|---|
| C3-TO-01 | Winpharm WPF Application | C# WPF — Clean Architecture | C3-AS-01 | Clinical domains migrated to WPF — one feature per COBOL screen |
| C3-TO-02 | POS WPF Application | C# WPF — Clean Architecture | C3-AS-02 | Commercial modules migrated to WPF — one feature per WinForms screen |
| C3-TO-03 | Application Services Layer | C# — Use Cases | C3-AS-04 | Use case classes per clinical and commercial domain — replaces COBOL business logic |

