using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Repositories;
using Marketeer.UI.MarketWatch.Providers;
using Marketeer.UI.MarketWatch.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketWatch;

public class MarketWatchFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Data Repositories
        services.AddSingleton<IMarketWatchRepository, MarketWatchRepository>();

        // Localization Providers
        services.AddSingleton<ILocalizationProvider, MarketWatchLocalizationProvider>();

        // UI & Navigation
        services.AddSingleton<MarketWatchMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<MarketWatchMenu>());
    }
}