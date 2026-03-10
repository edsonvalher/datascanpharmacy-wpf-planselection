namespace StandAlonePlan.Features.PlanSelection.Domain.Models
{
    /// <summary>
    /// Returned when the Plan Selection window closes.
    /// Maps to SETPLAN.CBL SELECTED-PLAN / SELECTED-CHAR output parameters.
    /// </summary>
    public class PlanSelectionResult
    {
        public string SelectedPlan { get; init; } = ""; // SELECTED-PLAN PIC X(6) — LINKAGE output
        public string SelectedChar { get; init; } = ""; // SELECTED-CHAR PIC X    — LINKAGE output

        /// <summary>True when Add Plan button was pressed. COBOL: MOVE HIGH-VALUES TO SELECTED-PLAN.</summary>
        public bool AddPlanRequested { get; init; }

        /// <summary>True when ESC/F2 pressed and ALLOW-BLANK-CODE = 'Y' (SELECTED-PLAN was HIGH-VALUES on entry).</summary>
        public bool Cancelled { get; init; }
    }
}
