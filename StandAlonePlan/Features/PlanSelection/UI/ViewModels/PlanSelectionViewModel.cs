using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;

namespace StandAlonePlan.Features.PlanSelection.UI.ViewModels
{
    /// <summary>
    /// Observable row item for the plan list (one per screen line, max 9).
    /// </summary>
    public class PlanPageItem
    {
        public int Number { get; }  // position 1-9 in PLAN-ARRAY (PLAX index)
        public Plan Plan { get; }

        // Legacy: kept for existing tests that reference CodeDisplay
        public string CodeDisplay => $"{Number}. {Plan.PlanCode.Trim()}";

        // PLAN-n PIC X(35): REDEFINES → PLAN-NUMBER(3) + PLAN-CODE(6) + PLAN-FILLER(1) + PLAN-NAME(25)
        public string FullDisplay
        {
            get
            {
                // Normal plans get a leading space before the name,
                // matching COBOL: STRING ' ' R11-NAME INTO PL-NAME
                var nameDisplay = Plan.Status == PlanStatus.Active
                    ? " " + Plan.Name
                    : Plan.DisplayName; // "  **EXPIRED**" / "  **DISABLED**" already spaced
                return $"{Number}. {Plan.PlanCode.Trim(),-6} {nameDisplay}";
            }
        }

        public PlanPageItem(int number, Plan plan)
        {
            Number = number;
            Plan   = plan;
        }
    }

    /// <summary>
    /// ViewModel for PlanSelectionWindow.
    /// Orchestrates the use cases and exposes observable state to the View.
    /// </summary>
    public partial class PlanSelectionViewModel : ObservableObject
    {
        private readonly GetPatientPlansUseCase _getPlans;
        private readonly SelectPlanUseCase      _selectPlan;
        private readonly AddPlanUseCase         _addPlan;
        private readonly PaginatePlansUseCase   _paginate;
        private readonly bool                   _cashDisabled;   // R1A-DISABLE-CASH-ON PIC X
        private readonly bool                   _allowBlankCode; // ALLOW-BLANK-CODE PIC X — 'Y' = cancel allowed

        private IReadOnlyList<Plan> _allPlans = Array.Empty<Plan>();
        private int _pageStart; // PLAN-ARRAY-PTR: index of first visible plan in PLAN-ARRAY

        // ── Observable state ─────────────────────────────────────────────────

        // PLAN-1..PLAN-9 PIC X(35) OCCURS 9 — the visible plan rows
        public ObservableCollection<PlanPageItem> PlanItems { get; } = new();

        [ObservableProperty]
        public partial string SelectInput { get; set; } // PLAN-SELECT PIC X — hot-return field

        [ObservableProperty]
        public partial bool IsAddPlanVisible { get; set; } // PLANSLCT-DIS-ADD-PLAN — visible when RUN-OPTION='Y' or 'D'

        [ObservableProperty]
        public partial bool CanGoPrev { get; set; } // PLANSLCT-SEARCH-PREV event = 4

        [ObservableProperty]
        public partial bool CanGoNext { get; set; } // PLANSLCT-SEARCH-NEXT event = 5

        [ObservableProperty]
        public partial PlanPageItem? SelectedPlanItem { get; set; }

        // ── Commands ──────────────────────────────────────────────────────────

        public IRelayCommand SelectCommand { get; }
        public IRelayCommand AddPlanCommand { get; }
        public IRelayCommand PrevCommand    { get; }
        public IRelayCommand NextCommand    { get; }
        public IRelayCommand CancelCommand  { get; }

        // ── Output ────────────────────────────────────────────────────────────

        public PlanSelectionMode Mode          { get; } // RUN-OPTION PIC X ('N'/'Y'/'D')
        public int               PatientNumber { get; } // PATIENT-RN PIC 9(9) COMP-3
        public PlanSelectionResult? Result     { get; private set; }

        /// <summary>View subscribes to this to close the window.</summary>
        public event Action? CloseRequested;

        // ── Constructor ───────────────────────────────────────────────────────

        public PlanSelectionViewModel(
            GetPatientPlansUseCase getPlans,
            SelectPlanUseCase      selectPlan,
            AddPlanUseCase         addPlan,
            PaginatePlansUseCase   paginate,
            IPlanRepository        repository,
            int                    patientNumber,
            PlanSelectionMode      mode,
            string?                currentSelectedPlan = null)
        {
            _getPlans     = getPlans;
            _selectPlan   = selectPlan;
            _addPlan      = addPlan;
            _paginate     = paginate;
            _cashDisabled = repository.IsCashDisabled();

            PatientNumber = patientNumber;
            Mode          = mode;
            SelectInput   = "";

            // SELECTED-PLAN = HIGH-VALUES on entry → ALLOW-BLANK-CODE = 'Y' (cancel allowed)
            _allowBlankCode   = currentSelectedPlan == null;
            IsAddPlanVisible  = mode == PlanSelectionMode.AllowAdd ||
                                mode == PlanSelectionMode.ShowDeleteAdd;

            SelectCommand  = new RelayCommand(ExecuteSelect);
            AddPlanCommand = new RelayCommand(ExecuteAddPlan, () => IsAddPlanVisible);
            PrevCommand    = new RelayCommand(ExecutePrev,    () => CanGoPrev);
            NextCommand    = new RelayCommand(ExecuteNext,    () => CanGoNext);
            CancelCommand  = new RelayCommand(ExecuteCancel,  () => _allowBlankCode);

            LoadAndDisplay(currentSelectedPlan);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void LoadAndDisplay(string? preSelected)
        {
            _allPlans = _getPlans.Execute(PatientNumber, Mode);

            // Navigate to page containing the pre-selected plan
            if (!string.IsNullOrWhiteSpace(preSelected))
            {
                var pre = preSelected!;
                for (int i = 0; i < _allPlans.Count; i++)
                {
                    if (string.Equals(_allPlans[i].PlanCode.Trim(), pre.Trim(),
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        _pageStart = (i / PaginatePlansUseCase.PageSize) * PaginatePlansUseCase.PageSize;
                        break;
                    }
                }
            }

            RefreshPage();
        }

        private void RefreshPage()
        {
            var (page, hasPrev, hasNext) = _paginate.Execute(_allPlans, _pageStart);

            PlanItems.Clear();
            int n = 1;
            foreach (var plan in page)
                PlanItems.Add(new PlanPageItem(n++, plan));

            CanGoPrev = hasPrev;
            CanGoNext = hasNext;

            PrevCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        /// <summary>Called from code-behind on ListView double-click or row selection.</summary>
        public void SelectByRow(PlanPageItem item)
        {
            if (item.Plan.IsCash && _cashDisabled) return;
            Result = new PlanSelectionResult
            {
                SelectedPlan = item.Plan.PlanCode,
                SelectedChar = item.Number.ToString()
            };
            CloseRequested?.Invoke();
        }

        private void ExecuteSelect()
        {
            var page   = PlanItems.Select(i => i.Plan).ToList().AsReadOnly();
            var result = _selectPlan.Execute(SelectInput, page, _cashDisabled);

            if (!result.Cancelled)
            {
                Result = result;
                CloseRequested?.Invoke();
            }
            else
            {
                SelectInput = "";
            }
        }

        private void ExecuteAddPlan()
        {
            Result = _addPlan.Execute(Mode);
            CloseRequested?.Invoke();
        }

        private void ExecutePrev()
        {
            _pageStart = Math.Max(0, _pageStart - PaginatePlansUseCase.PageSize);
            RefreshPage();
        }

        private void ExecuteNext()
        {
            if (_pageStart + PaginatePlansUseCase.PageSize < _allPlans.Count)
            {
                _pageStart += PaginatePlansUseCase.PageSize;
                RefreshPage();
            }
        }

        private void ExecuteCancel()
        {
            Result = new PlanSelectionResult { Cancelled = true };
            CloseRequested?.Invoke();
        }
    }
}
