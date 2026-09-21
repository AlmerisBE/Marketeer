using Marketeer.Core.Framework;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketPricing;

public class MarketPricingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IMarketPriceCacheService, MarketPriceCacheService>();
        services.AddSingleton<IPriceCalculationService, PriceCalculationService>();
    }
}