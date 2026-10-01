using Marketeer.Core.Framework;
using Marketeer.Core.MarketStrategy.Contracts;
using Marketeer.Core.MarketStrategy.Services;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.MarketStrategy.Providers;
using Marketeer.UI.MarketStrategy.UI;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketStrategy;

public class MarketStrategyFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Core Business Logic
        services.AddSingleton<IMarketAnomalyDetector, MarketAnomalyDetectorService>();

        // UI Nodes
        services.AddSingleton<MarketStrategyConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<MarketStrategyConfigMenu>());

        // Localization Providers
        services.AddSingleton<ILocalizationProvider, MarketStrategyLocalizationProvider>();
    }
}