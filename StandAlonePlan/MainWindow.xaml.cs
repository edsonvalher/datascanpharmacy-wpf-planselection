using System.Windows;
using StandAlonePlan.Features.PlanSelection.Domain.Models;
using StandAlonePlan.Features.PlanSelection.UI.ViewModels;
using StandAlonePlan.Features.PlanSelection.UI.Views;

namespace StandAlonePlan
{
    public partial class MainWindow : Window
    {
        private readonly IPlanSelectionViewModelFactory _vmFactory;

        // DI constructor — factory is injected by the container
        public MainWindow(IPlanSelectionViewModelFactory vmFactory)
        {
            _vmFactory = vmFactory;
            InitializeComponent();
        }

        private void OpenPlanSelection_Click(object sender, RoutedEventArgs e)
        {
            int patientNumber = rbPatient1.IsChecked == true ? 1
                              : rbPatient2.IsChecked == true ? 2 : 3;

            PlanSelectionMode mode = rbModeNormal.IsChecked   == true ? PlanSelectionMode.Normal
                                   : rbModeAllowAdd.IsChecked == true ? PlanSelectionMode.AllowAdd
                                   : PlanSelectionMode.ShowDeleteAdd;

            // ViewModel is built by the factory — repository comes from the DI container
            var vm     = _vmFactory.Create(patientNumber, mode);
            var window = new PlanSelectionWindow(vm) { Owner = this };
            window.ShowDialog();

            if (vm.Result is null || vm.Result.Cancelled) return;

            string msg = vm.Result.AddPlanRequested ? "Add Plan requested (HIGH-VALUES returned to caller)"
                       : vm.Result.Cancelled        ? "Cancelled — no plan selected"
                       : $"Selected Plan : {vm.Result.SelectedPlan}\nSelected Char : {vm.Result.SelectedChar}";

            MessageBox.Show(msg, "Plan Selection Result",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
