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
        services.AddSingleton<ILocalizationProvider, MarketListingsLocalizationProvider>();

        services.AddSingleton<IMarketListingTrackerService, MarketListingTrackerService>();
        services.AddSingleton<IListingOptimizationService, ListingOptimizationService>();

        services.AddSingleton<NativeListingHighlighterService>();
        services.AddSingleton<NativeRetainerListHighlighterService>(); // <- NOUVEAU SERVICE

        services.AddSingleton<IRetainerDetailsView, RetainerDetailsView>();

        services.AddSingleton<RetainerDetailsWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<RetainerDetailsWindow>());

        services.AddSingleton<VendorAlertsMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<VendorAlertsMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<NativeListingHighlighterService>();
        provider.GetRequiredService<NativeRetainerListHighlighterService>();
    }
}