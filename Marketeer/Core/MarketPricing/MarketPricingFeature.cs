using Marketeer.Core.Framework;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Services;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketPricing;

public class MarketPricingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<UniversalisUpdateStateService>();
        services.AddSingleton<IUniversalisUpdateState>(provider => provider.GetRequiredService<UniversalisUpdateStateService>());
        services.AddSingleton<IUniversalisUpdateMutator>(provider => provider.GetRequiredService<UniversalisUpdateStateService>());
        services.AddSingleton<IMarketPriceCacheService, MarketPriceCacheService>();
        services.AddSingleton<IPriceCalculationService, PriceCalculationService>();
    }
}