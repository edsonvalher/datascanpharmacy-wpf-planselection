using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StandAlonePlan.Features.PlanSelection.UI.ViewModels;

namespace StandAlonePlan.Features.PlanSelection.UI.Views
{
    public partial class PlanSelectionWindow : Window
    {
        private PlanSelectionViewModel ViewModel => (PlanSelectionViewModel)DataContext;

        public PlanSelectionWindow(PlanSelectionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Subscribe to VM close signal
            viewModel.CloseRequested += () => DialogResult = true;

            // Auto-focus the Select TextBox on open
            Loaded += (_, _) => SelectTextBox.Focus();
        }

        /// <summary>
        /// Auto-submit as soon as 1 character is typed.
        /// Mirrors COBOL PLAN-SELECT PIC X field-full hot-return (PLAN-SELECT-H).
        /// </summary>
        private void SelectTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SelectTextBox.Text.Length == 1)
                ViewModel.SelectCommand.Execute(null);
        }

        /// <summary>
        /// Single left-click on a plan row selects it immediately.
        /// Mirrors COBOL PLANSLCT-SELECTOR-RETURN (PLAN-1-S .. PLAN-9-S events).
        /// </summary>
        private void PlanListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedPlanItem is { } item)
                ViewModel.SelectByRow(item);
        }

        /// <summary>
        /// ESC cancels the dialog (PLANSLCT-ESC event = 1).
        /// F2  cancels the dialog (PLANSLCT-F2  event = 2).
        /// Both only work when ALLOW-BLANK-CODE = 'Y' (CancelCommand.CanExecute).
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape || e.Key == Key.F2)
            {
                if (ViewModel.CancelCommand.CanExecute(null))
                    ViewModel.CancelCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
