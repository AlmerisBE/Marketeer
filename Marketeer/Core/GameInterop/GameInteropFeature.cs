using Marketeer.API.Features;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.GameInterop.Providers;
using Marketeer.Core.GameInterop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.GameInterop;

public class GameInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGameEventService, GameEventService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IRetainerProvider, RetainerProvider>();
        services.AddSingleton<IMarketListingProvider, MarketListingProvider>();
    }
}