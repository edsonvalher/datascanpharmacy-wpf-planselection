using System;
using System.Collections.Generic;
using Moq;
using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.Domain.UseCases
{
    public class GetPatientPlansUseCaseTests
    {
        private readonly Mock<IPlanRepository> _repo = new();
        private readonly GetPatientPlansUseCase _sut;
        private const int Patient = 1;

        public GetPatientPlansUseCaseTests()
        {
            _sut = new GetPatientPlansUseCase(_repo.Object);
            _repo.Setup(r => r.IsCashDisabled()).Returns(false);
            _repo.Setup(r => r.GetAllPatientPlanRecords(Patient)).Returns(Array.Empty<PatientPlanRecord>());
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void SetupPrimary(string code, string name,
                                   bool masterDeleted = false,
                                   bool r18Deleted = false,
                                   DateTime? expDate = null,
                                   bool skipR18 = false)
        {
            _repo.Setup(r => r.GetPlanMaster(code))
                 .Returns(new PlanMasterRecord { PlanCode = code, Name = name, IsDeleted = masterDeleted });

            if (!skipR18)
                _repo.Setup(r => r.GetPatientPlanRecord(Patient, code))
                     .Returns(new PatientPlanRecord { PatientNumber = Patient, PlanCode = code,
                                                      IsDeleted = r18Deleted, ExpirationDate = expDate });
        }

        private void SetupAdditionals(params (string Code, string Name, bool R18Deleted, DateTime? Exp)[] items)
        {
            var records = new List<PatientPlanRecord>();
            foreach (var (code, name, deleted, exp) in items)
            {
                records.Add(new PatientPlanRecord { PatientNumber = Patient, PlanCode = code,
                                                    IsDeleted = deleted, ExpirationDate = exp });
                _repo.Setup(r => r.GetPlanMaster(code))
                     .Returns(new PlanMasterRecord { PlanCode = code, Name = name, IsDeleted = false });
            }
            _repo.Setup(r => r.GetAllPatientPlanRecords(Patient)).Returns(records);
        }

        // ── Primary plan tests ────────────────────────────────────────────────

        // 3 primary codes all active — expect the use case to return exactly 3 plans.
        [Fact]
        public void Execute_ThreeActivePrimaryPlans_ReturnsThreePlans()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01", "P02", "P03" });
            SetupPrimary("P01", "Plan A");
            SetupPrimary("P02", "Plan B");
            SetupPrimary("P03", "Plan C");

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Equal(3, result.Count);
        }

        // Active plan name must have a leading space — mirrors COBOL STRING " " R11-NAME INTO PL-NAME.
        [Fact]
        public void Execute_ActivePrimaryPlan_DisplayNameHasLeadingSpace()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "BCBS Standard");

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Equal(" BCBS Standard", result[0].DisplayName);
        }

        // Cash plan code "C" must be excluded when R1A-DISABLE-CASH-ON is active.
        [Fact]
        public void Execute_PrimaryCashPlan_CashDisabled_Excluded()
        {
            _repo.Setup(r => r.IsCashDisabled()).Returns(true);
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "C" });

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // Cash plan code "C" must be included when Cash is enabled (R1A-DISABLE-CASH-OFF).
        [Fact]
        public void Execute_PrimaryCashPlan_CashEnabled_Included()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "C" });
            SetupPrimary("C", "Cash");

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Single(result);
            Assert.Equal("C", result[0].PlanCode);
        }

        // If GetPlanMaster returns null (R11FILE STATUS-NOT-OK), the plan is skipped entirely.
        [Fact]
        public void Execute_PrimaryPlanMasterNotFound_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "GHOST" });
            _repo.Setup(r => r.GetPlanMaster("GHOST")).Returns((PlanMasterRecord?)null);

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R11-DELETED=true in Normal mode — plan must be excluded (not shown to user).
        [Fact]
        public void Execute_PrimaryMasterDeleted_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Deleted Plan", masterDeleted: true);

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R11-DELETED=true in ShowDeleteAdd mode — plan is included and shows as **DISABLED**.
        [Fact]
        public void Execute_PrimaryMasterDeleted_ShowDeleteAdd_IncludedAsDisabled()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Deleted Plan", masterDeleted: true, r18Deleted: true);

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **DISABLED**", result[0].DisplayName);
        }

        // R18FILE record not found for a primary plan in Normal mode — plan is excluded.
        [Fact]
        public void Execute_PrimaryR18NotFound_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            _repo.Setup(r => r.GetPlanMaster("P01"))
                 .Returns(new PlanMasterRecord { PlanCode = "P01", Name = "Plan A" });
            _repo.Setup(r => r.GetPatientPlanRecord(Patient, "P01"))
                 .Returns((PatientPlanRecord?)null);

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R18-DELETED=true on the patient-plan record in Normal mode — plan is excluded.
        [Fact]
        public void Execute_PrimaryR18Deleted_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A", r18Deleted: true);

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R18-DELETED=true in ShowDeleteAdd mode — plan included and shows as **DISABLED**.
        [Fact]
        public void Execute_PrimaryR18Deleted_ShowDeleteAdd_IncludedAsDisabled()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A", r18Deleted: true);

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **DISABLED**", result[0].DisplayName);
        }

        // R18-EXP-DATE is in the past in Normal mode — expired plan must be excluded.
        [Fact]
        public void Execute_PrimaryR18Expired_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A", expDate: DateTime.Today.AddDays(-1));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R18-EXP-DATE is in the past in ShowDeleteAdd mode — plan included as **EXPIRED**.
        [Fact]
        public void Execute_PrimaryR18Expired_ShowDeleteAdd_IncludedAsExpired()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A", expDate: DateTime.Today.AddDays(-1));

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **EXPIRED**", result[0].DisplayName);
        }

        // ── Additional plan tests ─────────────────────────────────────────────

        // An active plan found in R18FILE sequential scan (not a primary) must be added to the list.
        [Fact]
        public void Execute_AdditionalActivePlan_NotPrimary_Included()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A");
            SetupAdditionals(("P02", "Plan B", false, null));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.PlanCode == "P02");
        }

        // A plan that appears in both R5FILE and R18FILE sequential scan must appear only once.
        [Fact]
        public void Execute_AdditionalDuplicateOfPrimary_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01" });
            SetupPrimary("P01", "Plan A");
            SetupAdditionals(("P01", "Plan A", false, null));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Single(result); // only the primary, no duplicate
        }

        // Cash plan in the R18FILE sequential scan must be excluded when Cash is disabled.
        [Fact]
        public void Execute_AdditionalCash_CashDisabled_Excluded()
        {
            _repo.Setup(r => r.IsCashDisabled()).Returns(true);
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            SetupAdditionals(("C", "Cash", false, null));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R18-DELETED=true on an additional plan in Normal mode — must be excluded.
        [Fact]
        public void Execute_AdditionalR18Deleted_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            SetupAdditionals(("P02", "Plan B", true, null));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // R18-DELETED=true on an additional plan in ShowDeleteAdd — included as **DISABLED**.
        [Fact]
        public void Execute_AdditionalR18Deleted_ShowDeleteAdd_IncludedAsDisabled()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            SetupAdditionals(("P02", "Plan B", true, null));

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **DISABLED**", result[0].DisplayName);
        }

        // Expired additional plan in Normal mode — must be excluded.
        [Fact]
        public void Execute_AdditionalR18Expired_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            SetupAdditionals(("P02", "Plan B", false, DateTime.Today.AddDays(-1)));

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // Expired additional plan in ShowDeleteAdd — included as **EXPIRED**.
        [Fact]
        public void Execute_AdditionalR18Expired_ShowDeleteAdd_IncludedAsExpired()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            SetupAdditionals(("P02", "Plan B", false, DateTime.Today.AddDays(-1)));

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **EXPIRED**", result[0].DisplayName);
        }

        // R11-DELETED=true on an additional plan's master in ShowDeleteAdd — included as **DISABLED**.
        [Fact]
        public void Execute_AdditionalR11Deleted_ShowDeleteAdd_IncludedAsDisabled()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());

            var records = new List<PatientPlanRecord>
            {
                new() { PatientNumber = Patient, PlanCode = "P02", IsDeleted = false }
            };
            _repo.Setup(r => r.GetAllPatientPlanRecords(Patient)).Returns(records);
            _repo.Setup(r => r.GetPlanMaster("P02"))
                 .Returns(new PlanMasterRecord { PlanCode = "P02", Name = "Plan B", IsDeleted = true });

            var result = _sut.Execute(Patient, PlanSelectionMode.ShowDeleteAdd);

            Assert.Single(result);
            Assert.Equal("  **DISABLED**", result[0].DisplayName);
        }

        // R11-DELETED=true on an additional plan's master in Normal mode — must be excluded.
        [Fact]
        public void Execute_AdditionalR11Deleted_NormalMode_Excluded()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());
            var records = new List<PatientPlanRecord>
            {
                new() { PatientNumber = Patient, PlanCode = "P02", IsDeleted = false }
            };
            _repo.Setup(r => r.GetAllPatientPlanRecords(Patient)).Returns(records);
            _repo.Setup(r => r.GetPlanMaster("P02"))
                 .Returns(new PlanMasterRecord { PlanCode = "P02", Name = "Plan B", IsDeleted = true });

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // ── Edge cases ────────────────────────────────────────────────────────

        // No primary codes and no R18FILE records — result must be empty.
        [Fact]
        public void Execute_NoPlansAnywhere_ReturnsEmpty()
        {
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(Array.Empty<string>());

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Empty(result);
        }

        // 31 plans available (3 primary + 28 additional) — result must be capped at PLAN-ARRAY-MAX = 27.
        [Fact]
        public void Execute_MoreThan27Plans_CappedAt27()
        {
            // 3 primary + 28 additional = 31 available → capped at 27
            _repo.Setup(r => r.GetPrimaryPlanCodes(Patient)).Returns(new[] { "P01", "P02", "P03" });
            SetupPrimary("P01", "Plan 01");
            SetupPrimary("P02", "Plan 02");
            SetupPrimary("P03", "Plan 03");

            var additionals = new List<PatientPlanRecord>();
            for (int i = 4; i <= 31; i++)
            {
                var code = $"P{i:D2}";
                additionals.Add(new PatientPlanRecord { PatientNumber = Patient, PlanCode = code });
                _repo.Setup(r => r.GetPlanMaster(code))
                     .Returns(new PlanMasterRecord { PlanCode = code, Name = $"Plan {i:D2}" });
            }
            _repo.Setup(r => r.GetAllPatientPlanRecords(Patient)).Returns(additionals);

            var result = _sut.Execute(Patient, PlanSelectionMode.Normal);

            Assert.Equal(27, result.Count);
        }
    }
}
