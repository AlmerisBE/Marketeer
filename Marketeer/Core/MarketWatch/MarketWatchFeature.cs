using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Repositories;
using Marketeer.Core.MarketWatch.Services;
using Marketeer.UI.MarketWatch.Providers;
using Marketeer.UI.MarketWatch.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.MarketWatch;

public class MarketWatchFeature : IFeatureModule, IDisposable {
    private MarketWatchPoller? poller;

    public void RegisterServices(IServiceCollection services) {
        // Data Repositories & State
        services.AddSingleton<IMarketWatchRepository, MarketWatchRepository>();
        services.AddSingleton<IMarketWatchAlertState, MarketWatchAlertState>();

        // Core Business Services
        services.AddSingleton<IMarketWatchAnalysisService, MarketWatchAnalysisService>();
        services.AddSingleton<MarketWatchPoller>();

        // Localization Providers
        services.AddSingleton<ILocalizationProvider, MarketWatchLocalizationProvider>();

        // UI & Navigation
        services.AddSingleton<MarketWatchMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<MarketWatchMenu>());

        services.AddSingleton<MarketWatchAlertsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<MarketWatchAlertsMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        this.poller = provider.GetRequiredService<MarketWatchPoller>();
    }

    public void Dispose() {
        this.poller?.Dispose();
    }
}