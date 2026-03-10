using System.Collections.Generic;
using System.Linq;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.Domain.UseCases
{
    public class SelectPlanUseCaseTests
    {
        private readonly SelectPlanUseCase _sut = new();

        private static IReadOnlyList<Plan> Page(int count = 3)
            => Enumerable.Range(1, count)
                         .Select(i => new Plan { PlanCode = $"P{i:D2}", Name = $"Plan {i}" })
                         .ToList().AsReadOnly();

        private static IReadOnlyList<Plan> PageWithCashAtIndex(int cashIndex)
        {
            var list = new List<Plan>();
            for (int i = 1; i <= 3; i++)
                list.Add(i == cashIndex
                    ? new Plan { PlanCode = "C", Name = "Cash" }
                    : new Plan { PlanCode = $"P{i:D2}", Name = $"Plan {i}" });
            return list.AsReadOnly();
        }

        // ── Numeric selection ─────────────────────────────────────────────────

        // Typing "1" selects the plan at position 0 (first row). Mirrors COBOL PLAN-SELECT = '1'.
        [Fact]
        public void Execute_Input1_ReturnsFirstPlan()
        {
            var result = _sut.Execute("1", Page(), cashDisabled: false);

            Assert.False(result.Cancelled);
            Assert.Equal("P01", result.SelectedPlan);
            Assert.Equal("1", result.SelectedChar);
        }

        // Typing "3" selects the plan at position 2 (third row).
        [Fact]
        public void Execute_Input3_ReturnsThirdPlan()
        {
            var result = _sut.Execute("3", Page(), cashDisabled: false);

            Assert.Equal("P03", result.SelectedPlan);
        }

        // Typing "9" with a full 9-plan page selects the last row.
        [Fact]
        public void Execute_Input9_With9Plans_ReturnsNinthPlan()
        {
            var result = _sut.Execute("9", Page(9), cashDisabled: false);

            Assert.Equal("P09", result.SelectedPlan);
        }

        // "0" is out of range (rows are 1-9) — result must be Cancelled.
        [Fact]
        public void Execute_Input0_Cancelled()
        {
            var result = _sut.Execute("0", Page(), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // "10" is a two-digit number and exceeds the valid range — result must be Cancelled.
        [Fact]
        public void Execute_Input10_Cancelled()
        {
            var result = _sut.Execute("10", Page(), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // Typing "5" when only 3 plans are on the page is out of range — must be Cancelled.
        [Fact]
        public void Execute_Input5_OnlyThreePlans_Cancelled()
        {
            var result = _sut.Execute("5", Page(3), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // ── Cash plan at row index ─────────────────────────────────────────────

        // Numeric selection of a row that holds a Cash plan when Cash is disabled — must be Cancelled.
        [Fact]
        public void Execute_NumericIndex_PlanIsCash_CashDisabled_Cancelled()
        {
            var page = PageWithCashAtIndex(cashIndex: 2);

            var result = _sut.Execute("2", page, cashDisabled: true);

            Assert.True(result.Cancelled);
        }

        // Numeric selection of a row that holds a Cash plan when Cash is enabled — returns "C".
        [Fact]
        public void Execute_NumericIndex_PlanIsCash_CashEnabled_ReturnsC()
        {
            var page = PageWithCashAtIndex(cashIndex: 1);

            var result = _sut.Execute("1", page, cashDisabled: false);

            Assert.Equal("C", result.SelectedPlan);
        }

        // ── Special characters ─────────────────────────────────────────────────

        // Typing "C" directly selects Cash when Cash is enabled. Mirrors COBOL PLAN-SELECT = 'C'.
        [Fact]
        public void Execute_InputC_CashEnabled_ReturnsCash()
        {
            var result = _sut.Execute("C", Page(), cashDisabled: false);

            Assert.Equal("C", result.SelectedPlan);
            Assert.Equal("C", result.SelectedChar);
        }

        // Typing "A" is an alternate key for Cash — behaves the same as "C".
        [Fact]
        public void Execute_InputA_CashEnabled_ReturnsCash()
        {
            var result = _sut.Execute("A", Page(), cashDisabled: false);

            Assert.Equal("A", result.SelectedPlan);
        }

        // Typing "C" when Cash is disabled — must be Cancelled regardless of direct input.
        [Fact]
        public void Execute_InputC_CashDisabled_Cancelled()
        {
            var result = _sut.Execute("C", Page(), cashDisabled: true);

            Assert.True(result.Cancelled);
        }

        // Typing "P" selects Coupon — SELECTED-PLAN must be set to "COUPON". Mirrors COBOL PLAN-SELECT = 'P'.
        [Fact]
        public void Execute_InputP_ReturnsCoupon()
        {
            var result = _sut.Execute("P", Page(), cashDisabled: false);

            Assert.Equal("COUPON", result.SelectedPlan);
            Assert.Equal("P", result.SelectedChar);
        }

        // ── Invalid / empty input ──────────────────────────────────────────────

        // Empty string input — must be Cancelled (nothing was typed).
        [Fact]
        public void Execute_EmptyInput_Cancelled()
        {
            var result = _sut.Execute("", Page(), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // Whitespace-only input — treated as empty, must be Cancelled.
        [Fact]
        public void Execute_WhitespaceInput_Cancelled()
        {
            var result = _sut.Execute("   ", Page(), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // An unrecognized character like "X" — not numeric, not C/A/P — must be Cancelled.
        [Fact]
        public void Execute_InvalidTextInput_Cancelled()
        {
            var result = _sut.Execute("X", Page(), cashDisabled: false);

            Assert.True(result.Cancelled);
        }

        // Lowercase "c" must be treated as uppercase "C" — the use case normalizes input with ToUpper().
        [Fact]
        public void Execute_LowercaseInput_TreatedAsUppercase()
        {
            var result = _sut.Execute("c", Page(), cashDisabled: false);

            Assert.Equal("C", result.SelectedPlan);
        }
    }
}
