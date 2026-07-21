using Dalamud.Interface.Windowing;
using Marketeer.API.Features;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Services;
using Marketeer.UI.MarketListings.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.MarketListings;

public class MarketListingsFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Core Logic
        services.AddSingleton<IMarketListingTrackerService, MarketListingTrackerService>();

        // UI Views
        services.AddSingleton<IRetainerDetailsView, RetainerDetailsView>();

        services.AddSingleton<RetainerDetailsWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<RetainerDetailsWindow>());
    }
}