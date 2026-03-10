using System.Linq;
using StandAlonePlan.Features.PlanSelection.Data;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.Data
{
    public class MockPlanRepositoryTests
    {
        private readonly MockPlanRepository _sut = new();

        // ── GetPrimaryPlanCodes ───────────────────────────────────────────────

        // Patient 1 (Happy Path) must return exactly 3 active codes: 610011, M, MEDCD.
        [Fact]
        public void GetPrimaryPlanCodes_Patient1_ReturnsThreeActiveCodes()
        {
            var codes = _sut.GetPrimaryPlanCodes(1);

            Assert.Equal(3, codes.Length);
            Assert.Contains("610011", codes);
            Assert.Contains("M", codes);
            Assert.Contains("MEDCD", codes);
        }

        // Patient 2 (Edge Cases) must include EXPD01 (expired) and DEL01 (deleted) in its primary codes.
        [Fact]
        public void GetPrimaryPlanCodes_Patient2_ContainsExpiredAndDeleted()
        {
            var codes = _sut.GetPrimaryPlanCodes(2);

            Assert.Contains("EXPD01", codes);
            Assert.Contains("DEL01", codes);
        }

        // Patient 3 (Pagination) has 3 primary codes to start the 12-plan scenario.
        [Fact]
        public void GetPrimaryPlanCodes_Patient3_HasThreePrimaries()
        {
            var codes = _sut.GetPrimaryPlanCodes(3);

            Assert.Equal(3, codes.Length);
        }

        // An unknown patient number must return an empty array — no exception thrown.
        [Fact]
        public void GetPrimaryPlanCodes_UnknownPatient_ReturnsEmpty()
        {
            var codes = _sut.GetPrimaryPlanCodes(999);

            Assert.Empty(codes);
        }

        // ── GetPlanMaster ─────────────────────────────────────────────────────

        // A known active plan code returns the correct master record with expected name and IsDeleted=false.
        [Fact]
        public void GetPlanMaster_ExistingCode_ReturnsMaster()
        {
            var master = _sut.GetPlanMaster("610011");

            Assert.NotNull(master);
            Assert.Equal("BCBS Standard", master!.Name);
            Assert.False(master.IsDeleted);
        }

        // DEL01 is registered as IsDeleted=true in R11FILE — the record must be returned with that flag set.
        [Fact]
        public void GetPlanMaster_DeletedCode_ReturnsDeletedRecord()
        {
            var master = _sut.GetPlanMaster("DEL01");

            Assert.NotNull(master);
            Assert.True(master!.IsDeleted);
        }

        // A plan code not in the master dictionary must return null — equivalent to STATUS-NOT-FOUND.
        [Fact]
        public void GetPlanMaster_UnknownCode_ReturnsNull()
        {
            var master = _sut.GetPlanMaster("GHOST");

            Assert.Null(master);
        }

        // ── GetAllPatientPlanRecords ──────────────────────────────────────────

        // Patient 3 has 12 plan records in R18FILE — the sequential scan must return all 12.
        [Fact]
        public void GetAllPatientPlanRecords_Patient3_Returns12Records()
        {
            var records = _sut.GetAllPatientPlanRecords(3);

            Assert.Equal(12, records.Count);
        }

        // Records must be ordered by PlanCode — mirrors ISAM sequential read order (R18-PATRN + R18-PP).
        [Fact]
        public void GetAllPatientPlanRecords_Patient3_OrderedByPlanCode()
        {
            var records = _sut.GetAllPatientPlanRecords(3).ToList();
            var sorted  = records.OrderBy(r => r.PlanCode).ToList();

            Assert.Equal(sorted.Select(r => r.PlanCode), records.Select(r => r.PlanCode));
        }

        // Patient 2's R18FILE records must include EXPD01 with a non-null ExpirationDate.
        [Fact]
        public void GetAllPatientPlanRecords_Patient2_ContainsExpiredRecord()
        {
            var records = _sut.GetAllPatientPlanRecords(2);

            var expired = records.FirstOrDefault(r => r.PlanCode == "EXPD01");
            Assert.NotNull(expired);
            Assert.NotNull(expired!.ExpirationDate);
        }

        // An unknown patient number must return an empty list — no exception thrown.
        [Fact]
        public void GetAllPatientPlanRecords_UnknownPatient_ReturnsEmpty()
        {
            var records = _sut.GetAllPatientPlanRecords(999);

            Assert.Empty(records);
        }

        // ── IsCashDisabled ────────────────────────────────────────────────────

        // The mock always returns false — Cash is enabled in all demo scenarios.
        [Fact]
        public void IsCashDisabled_ReturnsFalse()
        {
            Assert.False(_sut.IsCashDisabled());
        }
    }
}
