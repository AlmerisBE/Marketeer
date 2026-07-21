using Dalamud.Interface.Windowing;
using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Dashboard.Commands;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Dashboard.Providers;
using Marketeer.Features.Dashboard.Services;
using Marketeer.Features.Dashboard.UI;
using Marketeer.Features.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Dashboard;

public class DashboardFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, DashboardLocalizationProvider>();
        services.AddSingleton<IDashboardNavigationService, DashboardNavigationService>();

        // Register the views used by the dynamic nodes
        services.AddSingleton<ICharacterSummaryView, CharacterSummaryView>();
        services.AddSingleton<IRetainerDetailsView, RetainerDetailsView>();

        services.AddSingleton<WelcomeMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<WelcomeMenu>());
        services.AddSingleton<CharacterOverviewMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CharacterOverviewMenu>());

        // Main window registration
        services.AddSingleton<DashboardWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<DashboardWindow>());

        // Details window registration
        services.AddSingleton<RetainerDetailsWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<RetainerDetailsWindow>());

        services.AddSingleton<ICommand, MainCommand>();
    }
}