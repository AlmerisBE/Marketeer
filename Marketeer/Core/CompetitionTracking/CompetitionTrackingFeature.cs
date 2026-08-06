using Marketeer.API.Command.Contracts;
using Marketeer.API.CompetitionTracking.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.UI.CompetitionTracking.Commands;
using Marketeer.UI.CompetitionTracking.Providers;
using Marketeer.UI.CompetitionTracking.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.CompetitionTracking;

public class CompetitionTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, CompetitionLocalizationProvider>();

        services.AddSingleton<IRetainerStateService, RetainerStateService>();
        services.AddSingleton<ICompetitionStateService, CompetitionStateService>();
        services.AddSingleton<ICompetitionMonitorService, CompetitionMonitorService>();

        services.AddSingleton<CompetitionMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionMenu>());

        services.AddSingleton<ICommand, PriceCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        var monitorService = provider.GetRequiredService<ICompetitionMonitorService>();
        monitorService.StartMonitoring();
    }
}