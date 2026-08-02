using Marketeer.API.Features;
using Marketeer.API.MarketWatch.Contracts;
using Marketeer.Core.MarketWatch.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketWatch;

public class MarketWatchFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Data Repositories
        services.AddSingleton<IMarketWatchRepository, MarketWatchRepository>();

        // Future services for MarketWatch (Universalis Analysis, Background Polling, UI Menus)
        // will be registered here as we build them.
    }
}