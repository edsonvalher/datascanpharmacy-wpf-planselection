using System.Collections.Generic;
using System.Linq;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using Xunit;

namespace StandAlonePlan.Tests.Features.PlanSelection.Domain.UseCases
{
    public class PaginatePlansUseCaseTests
    {
        private readonly PaginatePlansUseCase _sut = new();

        private static IReadOnlyList<Plan> Plans(int count)
            => Enumerable.Range(1, count)
                         .Select(i => new Plan { PlanCode = $"P{i:D2}", Name = $"Plan {i}" })
                         .ToList().AsReadOnly();

        // Zero plans — page must be empty and both navigation flags must be false.
        [Fact]
        public void Execute_EmptyList_ReturnsEmptyPageNoPrevNoNext()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(0), 0);

            Assert.Empty(page);
            Assert.False(hasPrev);
            Assert.False(hasNext);
        }

        // 5 plans fit in one page — all 5 returned, no navigation needed.
        [Fact]
        public void Execute_FivePlans_PageStart0_ReturnsFiveItems()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(5), 0);

            Assert.Equal(5, page.Count);
            Assert.False(hasPrev);
            Assert.False(hasNext);
        }

        // Exactly 9 plans fills one full page (PageSize=9) — no Prev, no Next needed.
        [Fact]
        public void Execute_ExactlyNinePlans_PageStart0_ReturnsNineNoPrevNoNext()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(9), 0);

            Assert.Equal(9, page.Count);
            Assert.False(hasPrev);
            Assert.False(hasNext);
        }

        // 10 plans on page 0 — first 9 are returned and HasNext must be true (1 more plan exists).
        [Fact]
        public void Execute_TenPlans_PageStart0_ReturnsNineAndHasNext()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(10), 0);

            Assert.Equal(9, page.Count);
            Assert.False(hasPrev);
            Assert.True(hasNext);
        }

        // 10 plans on page 9 (second page) — only 1 plan remains, HasPrev must be true.
        [Fact]
        public void Execute_TenPlans_PageStart9_ReturnsOneAndHasPrev()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(10), 9);

            Assert.Single(page);
            Assert.True(hasPrev);
            Assert.False(hasNext);
        }

        // 27 plans (PLAN-ARRAY-MAX) on the middle page (start=9) — both Prev and Next must be true.
        [Fact]
        public void Execute_TwentySevenPlans_PageStart9_HasBothPrevAndNext()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(27), 9);

            Assert.Equal(9, page.Count);
            Assert.True(hasPrev);
            Assert.True(hasNext);
        }

        // 27 plans on the last page (start=18) — 9 plans returned, HasPrev=true, HasNext=false.
        [Fact]
        public void Execute_TwentySevenPlans_PageStart18_LastPageHasNineAndHasPrev()
        {
            var (page, hasPrev, hasNext) = _sut.Execute(Plans(27), 18);

            Assert.Equal(9, page.Count);
            Assert.True(hasPrev);
            Assert.False(hasNext);
        }

        // Negative pageStart must be clamped to 0 — should not throw and must return from the beginning.
        [Fact]
        public void Execute_NegativePageStart_ClampedToZero()
        {
            var (page, hasPrev, _) = _sut.Execute(Plans(5), -5);

            Assert.Equal(5, page.Count);
            Assert.False(hasPrev);
        }

        // First page of 12 plans — verifies the correct plan codes are at positions 0 and 8.
        [Fact]
        public void Execute_CorrectPlanCodesOnFirstPage()
        {
            var (page, _, _) = _sut.Execute(Plans(12), 0);

            Assert.Equal("P01", page[0].PlanCode);
            Assert.Equal("P09", page[8].PlanCode);
        }

        // Second page of 12 plans (start=9) — verifies the correct plan codes at positions 0 and 2.
        [Fact]
        public void Execute_CorrectPlanCodesOnSecondPage()
        {
            var (page, _, _) = _sut.Execute(Plans(12), 9);

            Assert.Equal("P10", page[0].PlanCode);
            Assert.Equal("P12", page[2].PlanCode);
        }
    }
}
