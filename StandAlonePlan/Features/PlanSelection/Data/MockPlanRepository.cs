using System;
using System.Collections.Generic;
using System.Linq;

namespace StandAlonePlan.Features.PlanSelection.Data
{
    /// <summary>
    /// Hardcoded data that simulates the COBOL ISAM files (RX5, RX11, RX18).
    /// Three patient scenarios to cover every UI state.
    /// </summary>
    public class MockPlanRepository : IPlanRepository
    {
        // ── R5FILE: primary plan codes per patient (up to 3) ──────────────────
        // R5-PP(1..3) PIC X(6) OCCURS 3 TIMES
        private static readonly Dictionary<int, string[]> PrimaryPlanCodes = new()
        {
            [1] = new[] { "610011", "M",      "MEDCD"  },   // Patient 1 – Normal
            [2] = new[] { "610011", "EXPD01", "DEL01"  },   // Patient 2 – ShowDeleteAdd
            [3] = new[] { "PLAN01", "PLAN02", "PLAN03" },   // Patient 3 – Pagination
        };

        // ── R11FILE: plan master records ──────────────────────────────────────
        // keyed by R11-PLAN-CODE PIC X(6)
        private static readonly Dictionary<string, PlanMasterRecord> PlanMasters = new()
        {
            ["610011"] = new() { PlanCode = "610011", Name = "BCBS Standard",    IsDeleted = false },
            ["M"]      = new() { PlanCode = "M",      Name = "Medicare Part D",  IsDeleted = false },
            ["MEDCD"]  = new() { PlanCode = "MEDCD",  Name = "Medicaid",         IsDeleted = false },
            ["EXPD01"] = new() { PlanCode = "EXPD01", Name = "Expired Plan",     IsDeleted = false },
            ["DEL01"]  = new() { PlanCode = "DEL01",  Name = "Deleted Plan",     IsDeleted = true  },
            ["PLAN01"] = new() { PlanCode = "PLAN01", Name = "Blue Cross Premier",  IsDeleted = false },
            ["PLAN02"] = new() { PlanCode = "PLAN02", Name = "Blue Shield Basic",   IsDeleted = false },
            ["PLAN03"] = new() { PlanCode = "PLAN03", Name = "Aetna Standard",      IsDeleted = false },
            ["PLAN04"] = new() { PlanCode = "PLAN04", Name = "Cigna Health Plus",   IsDeleted = false },
            ["PLAN05"] = new() { PlanCode = "PLAN05", Name = "United Health",       IsDeleted = false },
            ["PLAN06"] = new() { PlanCode = "PLAN06", Name = "Humana Gold",         IsDeleted = false },
            ["PLAN07"] = new() { PlanCode = "PLAN07", Name = "Tricare Select",      IsDeleted = false },
            ["PLAN08"] = new() { PlanCode = "PLAN08", Name = "Molina Healthcare",   IsDeleted = false },
            ["PLAN09"] = new() { PlanCode = "PLAN09", Name = "WellCare Value",      IsDeleted = false },
            ["PLAN10"] = new() { PlanCode = "PLAN10", Name = "Magellan Health",     IsDeleted = false },
            ["PLAN11"] = new() { PlanCode = "PLAN11", Name = "Kaiser Silver",       IsDeleted = false },
            ["PLAN12"] = new() { PlanCode = "PLAN12", Name = "Ambetter Core",       IsDeleted = false },
        };

        // ── R18FILE: patient-plan relationship records ────────────────────────
        // keyed by (R18-PATRN, R18-PP)
        private static readonly Dictionary<(int, string), PatientPlanRecord> PatientPlanRecords = new()
        {
            // Patient 1 – all active
            [(1, "610011")] = new() { PatientNumber = 1, PlanCode = "610011", IsDeleted = false },
            [(1, "M")]      = new() { PatientNumber = 1, PlanCode = "M",      IsDeleted = false },
            [(1, "MEDCD")]  = new() { PatientNumber = 1, PlanCode = "MEDCD",  IsDeleted = false },

            // Patient 2 – mix of active / expired / disabled
            [(2, "610011")] = new() { PatientNumber = 2, PlanCode = "610011", IsDeleted = false },
            [(2, "EXPD01")] = new() { PatientNumber = 2, PlanCode = "EXPD01", IsDeleted = false,
                                      ExpirationDate = new DateTime(2020, 1, 1) },
            [(2, "DEL01")]  = new() { PatientNumber = 2, PlanCode = "DEL01",  IsDeleted = true  },

            // Patient 3 – 12 plans for pagination testing
            [(3, "PLAN01")] = new() { PatientNumber = 3, PlanCode = "PLAN01", IsDeleted = false },
            [(3, "PLAN02")] = new() { PatientNumber = 3, PlanCode = "PLAN02", IsDeleted = false },
            [(3, "PLAN03")] = new() { PatientNumber = 3, PlanCode = "PLAN03", IsDeleted = false },
            [(3, "PLAN04")] = new() { PatientNumber = 3, PlanCode = "PLAN04", IsDeleted = false },
            [(3, "PLAN05")] = new() { PatientNumber = 3, PlanCode = "PLAN05", IsDeleted = false },
            [(3, "PLAN06")] = new() { PatientNumber = 3, PlanCode = "PLAN06", IsDeleted = false },
            [(3, "PLAN07")] = new() { PatientNumber = 3, PlanCode = "PLAN07", IsDeleted = false },
            [(3, "PLAN08")] = new() { PatientNumber = 3, PlanCode = "PLAN08", IsDeleted = false },
            [(3, "PLAN09")] = new() { PatientNumber = 3, PlanCode = "PLAN09", IsDeleted = false },
            [(3, "PLAN10")] = new() { PatientNumber = 3, PlanCode = "PLAN10", IsDeleted = false },
            [(3, "PLAN11")] = new() { PatientNumber = 3, PlanCode = "PLAN11", IsDeleted = false },
            [(3, "PLAN12")] = new() { PatientNumber = 3, PlanCode = "PLAN12", IsDeleted = false },
        };

        // Pre-computed sequential lists (ordered by PlanCode to mirror ISAM key order)
        private static readonly Dictionary<int, IReadOnlyList<PatientPlanRecord>> AllPatientPlans;

        static MockPlanRepository()
        {
            AllPatientPlans = PatientPlanRecords
                .GroupBy(kv => kv.Key.Item1)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<PatientPlanRecord>)g
                             .Select(kv => kv.Value)
                             .OrderBy(r => r.PlanCode)
                             .ToList()
                );
        }

        public string[] GetPrimaryPlanCodes(int patientNumber)
            => PrimaryPlanCodes.TryGetValue(patientNumber, out var codes)
               ? codes
               : Array.Empty<string>();

        public PlanMasterRecord? GetPlanMaster(string planCode)
            => PlanMasters.TryGetValue(planCode.Trim(), out var r) ? r : null;

        public PatientPlanRecord? GetPatientPlanRecord(int patientNumber, string planCode)
            => PatientPlanRecords.TryGetValue((patientNumber, planCode.Trim()), out var r) ? r : null;

        public IReadOnlyList<PatientPlanRecord> GetAllPatientPlanRecords(int patientNumber)
            => AllPatientPlans.TryGetValue(patientNumber, out var records)
               ? records
               : Array.Empty<PatientPlanRecord>();

        public bool IsCashDisabled() => false;
    }
}
