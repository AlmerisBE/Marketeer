using Marketeer.API.Features;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.InventoryTracking.Services;
using Marketeer.UI.InventoryTracking.Providers;
using Marketeer.UI.InventoryTracking.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.InventoryTracking;

public class InventoryTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, InventoryLocalizationProvider>();

        services.AddSingleton<IInventoryDiffService, InventoryDiffService>();
        services.AddSingleton<IInventorySnapshotService, InventorySnapshotService>();

        services.AddSingleton<RetainerInventoryTrackerService>();
        services.AddSingleton<PlayerInventoryTrackerService>();

        services.AddSingleton<InventoryView>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<RetainerInventoryTrackerService>();
        provider.GetRequiredService<PlayerInventoryTrackerService>();
    }
}