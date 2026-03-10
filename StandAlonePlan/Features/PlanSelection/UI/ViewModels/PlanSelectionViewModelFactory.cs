using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;

namespace StandAlonePlan.Features.PlanSelection.UI.ViewModels
{
    /// <summary>
    /// Concrete factory resolved by DI.
    /// </summary>
    public class PlanSelectionViewModelFactory : IPlanSelectionViewModelFactory
    {
        private readonly IPlanRepository        _repository;
        private readonly GetPatientPlansUseCase _getPlans;
        private readonly SelectPlanUseCase      _selectPlan;
        private readonly AddPlanUseCase         _addPlan;
        private readonly PaginatePlansUseCase   _paginate;

        public PlanSelectionViewModelFactory(
            IPlanRepository        repository,
            GetPatientPlansUseCase getPlans,
            SelectPlanUseCase      selectPlan,
            AddPlanUseCase         addPlan,
            PaginatePlansUseCase   paginate)
        {
            _repository = repository;
            _getPlans   = getPlans;
            _selectPlan = selectPlan;
            _addPlan    = addPlan;
            _paginate   = paginate;
        }

        public PlanSelectionViewModel Create(int patientNumber,
                                             PlanSelectionMode mode,
                                             string? currentSelectedPlan = null)
            => new(_getPlans, _selectPlan, _addPlan, _paginate,
                   _repository, patientNumber, mode, currentSelectedPlan);
    }
}
