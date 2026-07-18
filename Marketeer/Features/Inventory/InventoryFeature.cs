using Marketeer.Core;
using Marketeer.Features.Inventory.Contracts;
using Marketeer.Features.Inventory.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Inventory;

public class InventoryFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IInventoryService, InventoryService>();
    }
}