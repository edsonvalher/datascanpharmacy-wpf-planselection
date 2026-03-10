using StandAlonePlan.Features.PlanSelection.Domain.Models;

namespace StandAlonePlan.Features.PlanSelection.UI.ViewModels
{
    /// <summary>
    /// Factory that creates a PlanSelectionViewModel with runtime parameters
    /// (patientNumber, mode) while all services come from the DI container.
    /// </summary>
    public interface IPlanSelectionViewModelFactory
    {
        PlanSelectionViewModel Create(int patientNumber,
                                     PlanSelectionMode mode,
                                     string? currentSelectedPlan = null);
    }
}
