using System;
using System.Collections.Generic;
using System.Linq;
using StandAlonePlan.Features.PlanSelection.Domain.Models;

namespace StandAlonePlan.Features.PlanSelection.Domain.UseCases
{
    /// <summary>
    /// Returns a page slice of up to 9 plans from the full plan array.
    /// Mirrors SETPLAN.CBL LOAD-DISPLAY + PLANSLCT-SEARCH-PREV/NEXT logic.
    /// Max 9 plans visible at once (panel fields PLAN-1 to PLAN-9).
    /// </summary>
    public class PaginatePlansUseCase
    {
        public const int PageSize = 9; // PLAN-1 to PLAN-9: max 9 panel display fields

        public (IReadOnlyList<Plan> Page, bool HasPrev, bool HasNext) Execute(
            IReadOnlyList<Plan> allPlans, int pageStart)
        {
            int start = Math.Max(0, Math.Min(pageStart, Math.Max(0, allPlans.Count - 1)));
            var page  = allPlans.Skip(start).Take(PageSize).ToList().AsReadOnly();
            return (page, start > 0, start + PageSize < allPlans.Count);
        }
    }
}
