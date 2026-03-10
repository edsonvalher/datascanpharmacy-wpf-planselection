using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using StandAlonePlan.Features.PlanSelection.UI.ViewModels;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.UI.ViewModels
{
    public class PlanSelectionViewModelFactoryTests
    {
        private static PlanSelectionViewModelFactory BuildFactory()
        {
            var repo = new MockPlanRepository();
            return new PlanSelectionViewModelFactory(
                repo,
                new GetPatientPlansUseCase(repo),
                new SelectPlanUseCase(),
                new AddPlanUseCase(),
                new PaginatePlansUseCase());
        }

        // The factory must pass patientNumber through to the ViewModel correctly.
        [Fact]
        public void Create_ReturnsViewModelWithCorrectPatientNumber()
        {
            var vm = BuildFactory().Create(2, PlanSelectionMode.Normal);

            Assert.Equal(2, vm.PatientNumber);
        }

        // The factory must pass mode through to the ViewModel — AllowAdd makes IsAddPlanVisible=true.
        [Fact]
        public void Create_ReturnsViewModelWithCorrectMode()
        {
            var vm = BuildFactory().Create(1, PlanSelectionMode.AllowAdd);

            Assert.Equal(PlanSelectionMode.AllowAdd, vm.Mode);
            Assert.True(vm.IsAddPlanVisible);
        }

        // The created ViewModel must have plans loaded immediately — PlanItems must not be empty.
        [Fact]
        public void Create_PlanItemsArePopulated()
        {
            var vm = BuildFactory().Create(1, PlanSelectionMode.Normal);

            Assert.NotEmpty(vm.PlanItems);
        }

        // ShowDeleteAdd mode with Patient 2 (has expired + deleted plans) — both statuses must appear in PlanItems.
        [Fact]
        public void Create_ShowDeleteAddMode_IncludesExpiredAndDisabledPlans()
        {
            var vm = BuildFactory().Create(2, PlanSelectionMode.ShowDeleteAdd);

            Assert.Contains(vm.PlanItems, i => i.Plan.DisplayName == "  **EXPIRED**");
            Assert.Contains(vm.PlanItems, i => i.Plan.DisplayName == "  **DISABLED**");
        }
    }
}
