using StandAlonePlan.Features.PlanSelection.Domain.Models;

namespace StandAlonePlan.Features.PlanSelection.Domain.UseCases
{
    /// <summary>
    /// Handles the Add Plan button press.
    /// In SETPLAN.CBL: MOVE HIGH-VALUES TO SELECTED-PLAN → caller interprets as "add new plan".
    /// Here we use the AddPlanRequested flag on the result instead of HIGH-VALUES.
    /// </summary>
    public class AddPlanUseCase
    {
        public PlanSelectionResult Execute(PlanSelectionMode mode)
        {
            if (mode == PlanSelectionMode.AllowAdd || mode == PlanSelectionMode.ShowDeleteAdd)
                return new PlanSelectionResult { AddPlanRequested = true }; // MOVE HIGH-VALUES TO SELECTED-PLAN

            return new PlanSelectionResult { Cancelled = true };
        }
    }
}
