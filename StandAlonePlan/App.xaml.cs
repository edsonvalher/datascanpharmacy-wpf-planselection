using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.UseCases;
using StandAlonePlan.Features.PlanSelection.UI.ViewModels;

namespace StandAlonePlan
{
    public partial class App : Application
    {
        private ServiceProvider _container = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _container = BuildContainer();

            var mainWindow = _container.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _container.Dispose();
            base.OnExit(e);
        }

        private static ServiceProvider BuildContainer()
        {
            var services = new ServiceCollection();

            // Data — swap MockPlanRepository h
            services.AddSingleton<IPlanRepository, MockPlanRepository>();

            // Domain use cases
            services.AddTransient<GetPatientPlansUseCase>();
            services.AddTransient<SelectPlanUseCase>();
            services.AddTransient<AddPlanUseCase>();
            services.AddTransient<PaginatePlansUseCase>();

            // ViewModel factory (resolves runtime params + injected services)
            services.AddTransient<IPlanSelectionViewModelFactory, PlanSelectionViewModelFactory>();

            // UI — MainWindow gets the factory injected
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
