using Dalamud.Interface.Windowing;
using Marketeer.API.Command.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.Dashboard.Services;
using Marketeer.UI.Dashboard.Commands;
using Marketeer.UI.Dashboard.Providers;
using Marketeer.UI.Dashboard.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Dashboard;

public class DashboardFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Providers
        services.AddSingleton<ILocalizationProvider, DashboardLocalizationProvider>();

        // Core Services
        services.AddSingleton<IDashboardNavigationService, DashboardNavigationService>();

        // Main window registration
        services.AddSingleton<DashboardWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<DashboardWindow>());

        // Commands
        services.AddSingleton<ICommand, MainCommand>();
    }
}