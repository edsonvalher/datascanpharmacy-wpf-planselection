using System.Linq;
using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using StandAlonePlan.Features.PlanSelection.UI.ViewModels;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.UI.ViewModels
{
    public class PlanSelectionViewModelTests
    {
        // ── Builder ───────────────────────────────────────────────────────────

        private static PlanSelectionViewModel Build(
            int patientNumber = 1,
            PlanSelectionMode mode = PlanSelectionMode.Normal,
            string? currentSelectedPlan = null)
        {
            var repo = new MockPlanRepository();
            return new PlanSelectionViewModel(
                new GetPatientPlansUseCase(repo),
                new SelectPlanUseCase(),
                new AddPlanUseCase(),
                new PaginatePlansUseCase(),
                repo,
                patientNumber,
                mode,
                currentSelectedPlan);
        }

        // ── Constructor / initial state ───────────────────────────────────────

        // Patient 1 has 3 active plans — PlanItems must be populated with exactly 3 rows on open.
        [Fact]
        public void Constructor_Patient1_LoadsThreePlanItems()
        {
            var vm = Build(1, PlanSelectionMode.Normal);

            Assert.Equal(3, vm.PlanItems.Count);
        }

        // Patient 3 has 12 plans — first page must show exactly 9 rows (PageSize limit).
        [Fact]
        public void Constructor_Patient3_FirstPageHasNineItems()
        {
            var vm = Build(3, PlanSelectionMode.Normal);

            Assert.Equal(9, vm.PlanItems.Count);
        }

        // Normal mode ('N') hides the Add Plan button — IsAddPlanVisible must be false.
        [Fact]
        public void Constructor_ModeNormal_AddPlanNotVisible()
        {
            var vm = Build(1, PlanSelectionMode.Normal);

            Assert.False(vm.IsAddPlanVisible);
        }

        // AllowAdd mode ('Y') shows the Add Plan button — IsAddPlanVisible must be true.
        [Fact]
        public void Constructor_ModeAllowAdd_AddPlanVisible()
        {
            var vm = Build(1, PlanSelectionMode.AllowAdd);

            Assert.True(vm.IsAddPlanVisible);
        }

        // ShowDeleteAdd mode ('D') also shows the Add Plan button — IsAddPlanVisible must be true.
        [Fact]
        public void Constructor_ModeShowDeleteAdd_AddPlanVisible()
        {
            var vm = Build(2, PlanSelectionMode.ShowDeleteAdd);

            Assert.True(vm.IsAddPlanVisible);
        }

        // Patient 1 has only one page — PrevCommand must not be executable on open.
        [Fact]
        public void Constructor_Patient1_PrevCanExecuteIsFalse()
        {
            var vm = Build(1);

            Assert.False(vm.PrevCommand.CanExecute(null));
        }

        // Patient 3 has 12 plans (2 pages) — NextCommand must be executable on open.
        [Fact]
        public void Constructor_Patient3_NextCanExecuteIsTrue()
        {
            var vm = Build(3);

            Assert.True(vm.NextCommand.CanExecute(null));
        }

        // Patient 1 fits in one page — CanGoNext observable must be false.
        [Fact]
        public void Constructor_Patient1_CanGoNextIsFalse()
        {
            var vm = Build(1);

            Assert.False(vm.CanGoNext);
        }

        // ── SelectCommand ─────────────────────────────────────────────────────

        // Valid input "1" — Result must be set with a plan code and CloseRequested must fire.
        [Fact]
        public void SelectCommand_ValidInput_SetsResultAndFiresClose()
        {
            var vm        = Build(1);
            bool fired    = false;
            vm.CloseRequested += () => fired = true;

            vm.SelectInput = "1";
            vm.SelectCommand.Execute(null);

            Assert.True(fired);
            Assert.NotNull(vm.Result);
            Assert.False(vm.Result!.Cancelled);
        }

        // Input "1" — SelectedPlan must match the PlanCode of the first item in PlanItems.
        [Fact]
        public void SelectCommand_ValidInput_SelectedPlanMatchesFirstItem()
        {
            var vm = Build(1);
            var expectedCode = vm.PlanItems[0].Plan.PlanCode;

            vm.SelectInput = "1";
            vm.SelectCommand.Execute(null);

            Assert.Equal(expectedCode, vm.Result!.SelectedPlan);
        }

        // Invalid input "X" — CloseRequested must NOT fire, Result stays null, SelectInput is cleared.
        [Fact]
        public void SelectCommand_InvalidInput_ResultIsNullAndInputCleared()
        {
            var vm     = Build(1);
            bool fired = false;
            vm.CloseRequested += () => fired = true;

            vm.SelectInput = "X";
            vm.SelectCommand.Execute(null);

            Assert.False(fired);
            Assert.Null(vm.Result);
            Assert.Equal("", vm.SelectInput);
        }

        // Input "P" selects Coupon — SelectedPlan must be "COUPON".
        [Fact]
        public void SelectCommand_InputP_ResultIsCoupon()
        {
            var vm = Build(1);
            vm.CloseRequested += () => { };

            vm.SelectInput = "P";
            vm.SelectCommand.Execute(null);

            Assert.Equal("COUPON", vm.Result!.SelectedPlan);
        }

        // ── SelectByRow ───────────────────────────────────────────────────────

        // Clicking a row directly (mouse) — Result must use the row's PlanCode and Number as SelectedChar.
        [Fact]
        public void SelectByRow_ValidPlan_SetsResultAndFiresClose()
        {
            var vm     = Build(1);
            bool fired = false;
            vm.CloseRequested += () => fired = true;
            var item = vm.PlanItems.First();

            vm.SelectByRow(item);

            Assert.True(fired);
            Assert.Equal(item.Plan.PlanCode, vm.Result!.SelectedPlan);
            Assert.Equal("1", vm.Result.SelectedChar);
        }

        // ── NextCommand / PrevCommand ─────────────────────────────────────────

        // Patient 3 has 12 plans — after Next, the second page must show 3 items (12 - 9 = 3).
        [Fact]
        public void NextCommand_Patient3_LoadsSecondPageWithThreeItems()
        {
            var vm = Build(3);

            vm.NextCommand.Execute(null);

            Assert.Equal(3, vm.PlanItems.Count);  // 12 - 9 = 3 remaining
        }

        // After navigating to the second page — CanGoPrev must be true and CanGoNext must be false.
        [Fact]
        public void NextCommand_Patient3_EnablesPrevDisablesNext()
        {
            var vm = Build(3);

            vm.NextCommand.Execute(null);

            Assert.True(vm.CanGoPrev);
            Assert.False(vm.CanGoNext);
        }

        // After Next then Prev — must return to the first page with 9 items and CanGoPrev=false.
        [Fact]
        public void PrevCommand_AfterNext_RestoresFirstPageNineItems()
        {
            var vm = Build(3);
            vm.NextCommand.Execute(null);

            vm.PrevCommand.Execute(null);

            Assert.Equal(9, vm.PlanItems.Count);
            Assert.False(vm.CanGoPrev);
        }

        // ── AddPlanCommand ────────────────────────────────────────────────────

        // AllowAdd mode — pressing Add Plan must set AddPlanRequested=true and fire CloseRequested.
        [Fact]
        public void AddPlanCommand_ModeAllowAdd_SetsAddPlanRequestedAndCloses()
        {
            var vm     = Build(1, PlanSelectionMode.AllowAdd);
            bool fired = false;
            vm.CloseRequested += () => fired = true;

            vm.AddPlanCommand.Execute(null);

            Assert.True(fired);
            Assert.True(vm.Result!.AddPlanRequested);
        }

        // Normal mode hides the button — AddPlanCommand.CanExecute must return false.
        [Fact]
        public void AddPlanCommand_ModeNormal_CanExecuteIsFalse()
        {
            var vm = Build(1, PlanSelectionMode.Normal);

            Assert.False(vm.AddPlanCommand.CanExecute(null));
        }

        // ── CancelCommand ─────────────────────────────────────────────────────

        // No pre-selected plan (SELECTED-PLAN = HIGH-VALUES on entry) — cancel is allowed.
        // Pressing Cancel must set Cancelled=true and fire CloseRequested.
        [Fact]
        public void CancelCommand_AllowBlankCode_SetsResultCancelledAndCloses()
        {
            // currentSelectedPlan=null → _allowBlankCode=true
            var vm     = Build(1, PlanSelectionMode.Normal, currentSelectedPlan: null);
            bool fired = false;
            vm.CloseRequested += () => fired = true;

            vm.CancelCommand.Execute(null);

            Assert.True(fired);
            Assert.True(vm.Result!.Cancelled);
        }

        // A pre-selected plan was passed (SELECTED-PLAN != HIGH-VALUES) — cancel is not allowed.
        // CancelCommand.CanExecute must return false so ESC/F2 are ignored.
        [Fact]
        public void CancelCommand_NotAllowBlankCode_CanExecuteIsFalse()
        {
            // currentSelectedPlan not null → _allowBlankCode=false
            var vm = Build(1, PlanSelectionMode.Normal, currentSelectedPlan: "610011");

            Assert.False(vm.CancelCommand.CanExecute(null));
        }
    }
}
