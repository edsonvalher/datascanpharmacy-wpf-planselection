namespace StandAlonePlan.Features.PlanSelection.Domain.Models
{
    /// <summary>
    /// Maps to SETPLAN.CBL RUN-OPTION linkage parameter.
    /// </summary>
    public enum PlanSelectionMode
    {
        Normal,        // RUN-OPTION = 'N' — 88 MODE-NORMAL     — no Add Plan button
        AllowAdd,      // RUN-OPTION = 'Y' — 88 ALLOW-ADD       — Add Plan button visible
        ShowDeleteAdd  // RUN-OPTION = 'D' — 88 SHOW-DELETE-ADD — Add Plan + show deleted/expired
    }
}
