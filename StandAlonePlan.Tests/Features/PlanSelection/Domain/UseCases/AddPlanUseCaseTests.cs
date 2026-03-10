using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.Domain.UseCases
{
    public class AddPlanUseCaseTests
    {
        private readonly AddPlanUseCase _sut = new();

        // Normal mode has no Add Plan button — pressing it is a no-op, result must be Cancelled.
        // Defensive guard: the button is hidden in Normal mode but the use case protects against misuse.
        [Fact]
        public void Execute_ModeNormal_ReturnsCancelled()
        {
            var result = _sut.Execute(PlanSelectionMode.Normal);

            Assert.True(result.Cancelled);
            Assert.False(result.AddPlanRequested);
        }

        // AllowAdd mode ('Y') shows the Add Plan button — pressing it signals HIGH-VALUES to the caller.
        [Fact]
        public void Execute_ModeAllowAdd_ReturnsAddPlanRequested()
        {
            var result = _sut.Execute(PlanSelectionMode.AllowAdd);

            Assert.True(result.AddPlanRequested);
            Assert.False(result.Cancelled);
        }

        // ShowDeleteAdd mode ('D') also shows the Add Plan button — same HIGH-VALUES signal as AllowAdd.
        [Fact]
        public void Execute_ModeShowDeleteAdd_ReturnsAddPlanRequested()
        {
            var result = _sut.Execute(PlanSelectionMode.ShowDeleteAdd);

            Assert.True(result.AddPlanRequested);
            Assert.False(result.Cancelled);
        }
    }
}
