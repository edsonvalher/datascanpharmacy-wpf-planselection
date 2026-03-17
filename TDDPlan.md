# Test Plan — Datascan Pharmacy Migration
**Scope:** StandAlonePlan (POC) → Full Migration (Winpharm + POS)
**Date:** 2026-03-16

---

## 1. Overview

This test plan defines the testing strategy for the progressive migration of Winpharm and POS
from COBOL + ScreenIO + WinForms to a modern C# WPF Clean Architecture stack.

StandAlonePlan serves as the proof of concept for both the migration architecture and the
testing methodology. Every decision made here is validated first in StandAlonePlan and then
applied to each subsequent migrated feature.

---

## 2. Testing Strategy — Outside-In TDD with Strangler Fig

The core strategy combines two proven approaches:

**Outside-In TDD:** Tests are written before implementation. The COBOL source code is the
specification. Each COBOL behavior becomes a test first, then C# code is written to make
it pass.

**Strangler Fig:** The system is never fully rewritten at once. Each feature is migrated
independently while COBOL continues running. Once a feature is fully migrated and tested,
its COBOL DLL is removed.

### The TDD Cycle per Feature

```
COBOL source → defines expected behavior (specification)
      ↓
Write requirements (README per feature)
      ↓
Write tests against MockRepository (Red)
      ↓
Implement C# until tests pass (Green)
      ↓
Refactor
      ↓
Define IPlanRepository contract (Contract Tests)
      ↓
Implement RealRepository against real DB
      ↓
Run same tests against RealRepository (must pass equally)
      ↓
Remove corresponding COBOL DLL
```

### Why MockRepository First

| Reason | Benefit |
|---|---|
| No infrastructure dependency | Develop and test without ISAM files or real database |
| Same tests, two implementations | Contract Tests guarantee Mock and Real behave identically |
| Zero risk migration | COBOL continues running while C# is being built |
| One-line switch | Changing Mock to Real is a single DI registration change |
| Proven by StandAlonePlan | `MockPlanRepository` → `RealPlanRepository` is exactly that switch |

### The Feature Cycle

```
Feature: [Screen Name]

Red    → Test fails using MockRepository
Green  → C# implementation passes using MockRepository
Real   → RealRepository implemented against MySQL / mapped ISAM
Switch → One line in DI container: Mock → Real
Delete → Corresponding COBOL DLL removed
```

---

## 3. Test Types

| # | Test Type | Scope | Objective | How It Is Implemented |
|---|---|---|---|---|
| 1 | **Unit Test** | A single class or method with mocked dependencies | Verify that the business logic of each layer is correct in isolation | xUnit + Moq. Class is instantiated directly, dependencies are mocked with `Mock<T>`, method is called and result is verified with `Assert` |
| 2 | **Integration Test** | Two or more layers connected with real DI, without mocking everything | Verify that the wiring between layers works correctly | xUnit + `Microsoft.Extensions.DependencyInjection`. Real container is built, dependencies are resolved and the full cross-layer flow is executed |
| 3 | **Contract Test** | The `IPlanRepository` interface against any implementation | Guarantee that Mock and Real implementations behave identically under the same contract | xUnit with `[Theory]`. An abstract base class defines the contract tests and is inherited once per implementation (Mock, Real) |
| 4 | **E2E Test** | Full flow from the real UI using FlaUI | Verify that the user can complete their tasks from the interface | FlaUI + Page Object Model. `AppFixture` launches the app, `MainWindowPage` and `PlanSelectionWindowPage` abstract the controls, each test executes a real user flow |
| 5 | **Regression Test** | All previously migrated features | Guarantee that adding a new feature does not break any previously working feature | FlaUI + xUnit. Each test carries `[Trait("Requirement", "req#XX")]` mapped to README. Runs on every merge as a validation suite |

---

## 4. Coverage Tolerance

| Test Type | Minimum Coverage | Ideal Coverage | Reason |
|---|---|---|---|
| **Unit Test** | 80% | 90% | Business logic is the core — must be well covered |
| **Integration Test** | 70% | 80% | Covers the main wiring flows between layers |
| **Contract Test** | 100% | 100% | Every method of IPlanRepository must be validated without exception |
| **Smoke Test** | 100% | 100% | Few tests — if they exist they must always pass |
| **E2E Test** | 70% | 85% | Covers happy path and main sad paths — not every edge case |
| **Regression Test** | 100% | 100% | Every requirement in README must have its test — no exceptions |

### Coverage Rule

```
Contract + Smoke + Regression = 100% always
Unit + Integration            = minimum 80%
E2E                           = minimum 70%
```

---

## 5. Why Regression Must Be 100%

Each requirement in the README maps directly to a COBOL behavior in the original source.
If a requirement has no regression test, there is no guarantee that the migration is
behaviorally equivalent to COBOL. In a migration context, that is unacceptable.

The traceability chain is:

```
COBOL source behavior (SETPLAN.CBL)
  → Requirement in README (req #01 to #24)
    → Unit test in StandAlonePlan.Tests
      → Regression test in StandAlonePlan.E2E.Tests [Trait("Requirement", "req#XX")]
```

---

## 6. Test Projects Structure

```
StandAlonePlan/
  StandAlonePlan.Tests/                   ← existing
    Features/PlanSelection/
      Domain/UseCases/                    Unit Tests (existing)
      Data/                               Unit Tests (existing)
      UI/ViewModels/                      Unit Tests (existing)
      Integration/                        Integration Tests (new)
      Contracts/                          Contract Tests (new)

  StandAlonePlan.E2E.Tests/               ← new project
    Infrastructure/
      AppFixture.cs
      MainWindowPage.cs
      PlanSelectionWindowPage.cs
      ResultMessageBoxHelper.cs
      ScreenshotOnFailureHelper.cs
    Smoke/                                Smoke Tests
    E2E/                                  E2E Tests
    Regression/                           Regression Tests
```

---

## 7. Execution Strategy per Environment

| Trigger | Tests to Run | Max Duration |
|---|---|---|
| Every commit | Smoke Tests | < 2 min |
| Every PR | Unit + Integration + Contract + Smoke | < 10 min |
| Merge to main | Full suite — all types | < 30 min |
| Release candidate | Full suite + manual exploratory | No limit |
| New feature migrated | Regression suite — all previous features | < 30 min |

---

## 8. MockRepository to RealRepository Switch

The switch from Mock to Real is a single line change in the DI composition root:

**Mock (development and testing):**
```csharp
services.AddSingleton<IPlanRepository, MockPlanRepository>();
```

**Real (production):**
```csharp
services.AddSingleton<IPlanRepository, RealPlanRepository>();
```

Contract Tests guarantee both implementations are equivalent before the switch is made.
No test needs to be modified when switching from Mock to Real.

---

## 9. Tools

| Tool | Purpose |
|---|---|
| **xUnit** | Test runner for Unit, Integration, Contract and Regression tests |
| **Moq** | Mocking framework for Unit tests |
| **FlaUI** | WPF UI automation for Smoke, E2E and Regression tests |
| **Microsoft.Extensions.DependencyInjection** | Real DI container for Integration tests |
| **CommunityToolkit.Mvvm** | Observable properties and commands in ViewModel |

---

## 10. Definition of Done per Migrated Feature

A feature is considered fully migrated when:

- [ ] COBOL behavior is documented as requirements in README
- [ ] Unit tests written and passing against MockRepository (TDD Green)
- [ ] Integration tests written and passing with real DI container
- [ ] Contract tests written and passing against MockRepository
- [ ] RealRepository implemented and Contract tests passing against it
- [ ] E2E tests written and passing against real UI
- [ ] Regression tests tagged with `[Trait("Requirement", "req#XX")]` and passing
- [ ] DI registration switched from Mock to Real
- [ ] Corresponding COBOL DLL removed or marked for removal
- [ ] Full regression suite passes — no previously working feature is broken
