using Marketeer.API.Features;
using Marketeer.API.GameInterop.Contracts;
using Marketeer.Core.GameInterop.Providers;
using Marketeer.Core.GameInterop.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.GameInterop;

public class GameInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGameEventService, GameEventService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IRetainerProvider, RetainerProvider>();
        services.AddSingleton<IMarketListingProvider, MarketListingProvider>();
        services.AddSingleton<IWorldInteractionService, WorldInteractionService>();
        services.AddSingleton<ILocalMarketViewScanner, LocalMarketViewScanner>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<IGameEventService>();
        var marketScanner = provider.GetRequiredService<ILocalMarketViewScanner>();
        marketScanner.Enable();
    }
}