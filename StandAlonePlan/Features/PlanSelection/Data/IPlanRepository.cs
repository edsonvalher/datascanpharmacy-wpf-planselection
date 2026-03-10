using System;
using System.Collections.Generic;

namespace StandAlonePlan.Features.PlanSelection.Data
{
    // ─── Raw records that mirror COBOL file structures ─────────────────────────

    /// <summary>R11FILE – Plan master record.</summary>
    public class PlanMasterRecord
    {
        public string PlanCode { get; init; } = "";  // R11-PLAN-CODE PIC X(6)
        public string Name { get; init; } = "";       // R11-NAME PIC X(25)
        public bool IsDeleted { get; init; }          // R11-DELETED PIC X
    }

    /// <summary>R18FILE – Patient-plan relationship record.</summary>
    public class PatientPlanRecord
    {
        public int PatientNumber { get; init; }        // R18-PATRN PIC 9(9) COMP-3
        public string PlanCode { get; init; } = "";    // R18-PP PIC X(6)
        public bool IsDeleted { get; init; }           // R18-DELETED PIC X
        public DateTime? ExpirationDate { get; init; } // R18-EXP-DATE PIC 9(8)
    }

    // ─── Repository contract ────────────────────────────────────────────────────

    public interface IPlanRepository
    {
        /// <summary>
        /// Returns the patient's up to 3 primary plan codes (mirrors R5FILE R5-PP 1-3).
        /// </summary>
        string[] GetPrimaryPlanCodes(int patientNumber);

        /// <summary>
        /// Reads a plan master record by plan code (mirrors R11FILE, AIX1 read).
        /// Returns null if not found.
        /// </summary>
        PlanMasterRecord? GetPlanMaster(string planCode);

        /// <summary>
        /// Reads a patient-plan relationship record (mirrors R18FILE prime key read).
        /// Returns null if not found.
        /// </summary>
        PatientPlanRecord? GetPatientPlanRecord(int patientNumber, string planCode);

        /// <summary>
        /// Returns all patient-plan records for a patient ordered by plan code
        /// (mirrors R18FILE sequential read by R18-PATRN + R18-PP).
        /// </summary>
        IReadOnlyList<PatientPlanRecord> GetAllPatientPlanRecords(int patientNumber);

        /// <summary>
        /// Returns true when Cash plan 'C' is disabled in pharmacy config
        /// (mirrors R1A-DISABLE-CASH-ON flag from RX1FILE).
        /// </summary>
        bool IsCashDisabled();
    }
}
