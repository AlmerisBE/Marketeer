using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Framework;
using Marketeer.Core.MarketPricing.Contracts;
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
        services.AddSingleton<MarketAnomalyDetectorService>();
        services.AddSingleton<IMarketAnomalyDetector>(provider => provider.GetRequiredService<MarketAnomalyDetectorService>());
        services.AddSingleton<IPriceCrashProtector>(provider => provider.GetRequiredService<MarketAnomalyDetectorService>());
        services.AddSingleton<PricingRuleResolver>();
        services.AddSingleton<ICompetitionRuleProvider>(provider => provider.GetRequiredService<PricingRuleResolver>());
        services.AddSingleton<IPriceRuleProvider>(provider => provider.GetRequiredService<PricingRuleResolver>());

        // UI Nodes
        services.AddSingleton<PricingStrategyConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<PricingStrategyConfigMenu>());

        // Localization Providers
        services.AddSingleton<ILocalizationProvider, MarketStrategyLocalizationProvider>();
    }
}