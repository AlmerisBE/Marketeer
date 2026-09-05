using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Providers;
using Marketeer.API.InventoryTracking.Services;
using Marketeer.API.InventoryTracking.UI;
using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.API.InventoryTracking;

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