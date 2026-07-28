using Marketeer.API.Features;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.InventoryTracking.Services;
using Marketeer.UI.InventoryTracking.Providers;
using Marketeer.UI.InventoryTracking.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.InventoryTracking;

public class InventoryTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // UI Providers
        services.AddSingleton<ILocalizationProvider, InventoryLocalizationProvider>();

        // Core Tracking
        services.AddSingleton<IInventoryDiffService, InventoryDiffService>();
        services.AddSingleton<IInventorySnapshotService, InventorySnapshotService>();
        services.AddSingleton<RetainerInventoryTrackerService>();

        // Hidden UI View integration (Not bound to INavigationNode collection)
        services.AddSingleton<InventoryView>();
    }
}