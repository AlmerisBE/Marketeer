using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.API.InventoryTracking.Services;
using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.Framework;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.API.InventoryTracking;

public class InventoryTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IInventoryDiffService, InventoryDiffService>();
        services.AddSingleton<IInventorySnapshotService, InventorySnapshotService>();

        services.AddSingleton<RetainerInventoryTrackerService>();
        services.AddSingleton<PlayerInventoryTrackerService>();
        services.AddSingleton<ICraftingInventoryService, CraftingInventoryService>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<RetainerInventoryTrackerService>();
        provider.GetRequiredService<PlayerInventoryTrackerService>();
    }
}