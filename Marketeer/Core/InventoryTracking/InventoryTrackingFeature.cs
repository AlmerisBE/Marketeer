using Marketeer.API.Features;
using Marketeer.API.InventoryTracking.Contracts;
using Marketeer.Core.InventoryTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.InventoryTracking;

public class InventoryTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IInventoryDiffService, InventoryDiffService>();
        services.AddSingleton<IInventorySnapshotService, InventorySnapshotService>();
    }
}