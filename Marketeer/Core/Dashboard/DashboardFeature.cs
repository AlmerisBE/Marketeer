using Dalamud.Interface.Windowing;
using Marketeer.Core.Dashboard.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Commands;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Dashboard.Providers;
using Marketeer.UI.Dashboard.UI;
using Marketeer.UI.Localization.Contracts;
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