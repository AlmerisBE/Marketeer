using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.CompetitionTracking.Services;
using Marketeer.Core.Framework;
using Marketeer.UI.CompetitionTracking.Commands;
using Marketeer.UI.CompetitionTracking.Contracts;
using Marketeer.UI.CompetitionTracking.Providers;
using Marketeer.UI.CompetitionTracking.UI;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.CompetitionTracking;

public class CompetitionTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, CompetitionLocalizationProvider>();

        services.AddSingleton<IRetainerStateService, RetainerStateService>();

        services.AddSingleton<CompetitionStateService>();
        services.AddSingleton<ICompetitionStateService>(provider => provider.GetRequiredService<CompetitionStateService>());
        services.AddSingleton<ICompetitionStateMutator>(provider => provider.GetRequiredService<CompetitionStateService>());
        services.AddSingleton<ICompetitionMonitorService, CompetitionMonitorService>();
        services.AddSingleton<ICompetitionEvaluatorService, CompetitionEvaluatorService>();
        services.AddSingleton<IWhitelistManagerService, WhitelistManagerService>();

        services.AddSingleton<CompetitionMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionMenu>());

        services.AddSingleton<CompetitionConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionConfigMenu>());

        services.AddSingleton<CompetitionWhitelistMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CompetitionWhitelistMenu>());

        services.AddSingleton<ICommand, PriceCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        var monitorService = provider.GetRequiredService<ICompetitionMonitorService>();
        monitorService.StartMonitoring();
    }
}