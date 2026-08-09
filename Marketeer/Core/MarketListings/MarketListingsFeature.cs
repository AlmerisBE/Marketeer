using Dalamud.Interface.Windowing;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.MarketListings.Contracts;
using Marketeer.Core.MarketListings.Services;
using Marketeer.UI.MarketListings.Providers;
using Marketeer.UI.MarketListings.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.MarketListings;

public class MarketListingsFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Localization
        services.AddSingleton<ILocalizationProvider, MarketListingsLocalizationProvider>();

        // Core Logic
        services.AddSingleton<IMarketListingTrackerService, MarketListingTrackerService>();
        services.AddSingleton<IListingOptimizationService, ListingOptimizationService>();

        // Native UI Enhancements
        services.AddSingleton<NativeListingHighlighterService>();

        // UI Views
        services.AddSingleton<IRetainerDetailsView, RetainerDetailsView>();

        services.AddSingleton<RetainerDetailsWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<RetainerDetailsWindow>());

        // Dashboard Nodes
        services.AddSingleton<VendorAlertsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<VendorAlertsMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<NativeListingHighlighterService>();
    }
}