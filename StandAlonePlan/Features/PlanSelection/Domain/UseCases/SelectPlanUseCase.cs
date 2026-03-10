using System.Collections.Generic;
using StandAlonePlan.Features.PlanSelection.Domain.Models;

namespace StandAlonePlan.Features.PlanSelection.Domain.UseCases
{
    /// <summary>
    /// Validates and processes a user's plan selection input.
    /// Mirrors SETPLAN.CBL PLANSLCT-DRIVER WHEN PLAN-SELECT-H logic.
    /// </summary>
    public class SelectPlanUseCase
    {
        public PlanSelectionResult Execute(string input,
                                           IReadOnlyList<Plan> currentPage,
                                           bool cashDisabled)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new PlanSelectionResult { Cancelled = true };

            var key = input.Trim().ToUpper();

            // Numeric 1-9: select plan at that position on the current page
            if (int.TryParse(key, out int idx) && idx >= 1 && idx <= currentPage.Count)
            {
                var plan = currentPage[idx - 1];
                if (plan.IsCash && cashDisabled)
                    return new PlanSelectionResult { Cancelled = true };

                return new PlanSelectionResult
                {
                    SelectedPlan = plan.PlanCode,
                    SelectedChar = key
                };
            }

            // PLAN-SELECT = 'C' or 'A' → Cash (when not disabled)
            if ((key == "C" || key == "A") && !cashDisabled)
                return new PlanSelectionResult { SelectedPlan = key, SelectedChar = key };

            // PLAN-SELECT = 'P' → Coupon; MOVE 'COUPON' TO SELECTED-PLAN
            if (key == "P")
                return new PlanSelectionResult { SelectedPlan = "COUPON", SelectedChar = "P" };

            return new PlanSelectionResult { Cancelled = true };
        }
    }
}
