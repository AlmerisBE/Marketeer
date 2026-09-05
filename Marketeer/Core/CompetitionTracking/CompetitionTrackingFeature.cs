using Marketeer.Core.CompetitionTracking.Commands;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Providers;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.CompetitionTracking.UI;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
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