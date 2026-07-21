using Marketeer.API.Command.Contracts;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.UI.CompetitionTracking.Providers;
using Marketeer.UI.CompetitionTracking.Commands;
using Marketeer.UI.CompetitionTracking.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.CompetitionTracking;

public class CompetitionTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // UI Providers
        services.AddSingleton<ILocalizationProvider, CompetitionLocalizationProvider>();

        // Core Tracking & Monitor Services
        services.AddSingleton<IRetainerStateService, RetainerStateService>();
        services.AddSingleton<ICompetitionStateService, CompetitionStateService>();
        services.AddSingleton<ICompetitionMonitorService, CompetitionMonitorService>();

        // UI View
        services.AddSingleton<CompetitionMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionMenu>());

        // User Commands
        services.AddSingleton<ICommand, PriceCommand>();
    }
}