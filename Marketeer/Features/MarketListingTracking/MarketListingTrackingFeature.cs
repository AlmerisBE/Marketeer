using Marketeer.Core;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.MarketListingTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.MarketListingTracking;

public class MarketListingTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IMarketListingTrackerService, MarketListingTrackerService>();
    }
}