using Marketeer.API.GameInterop.Contracts;
using Marketeer.API.GameInterop.Providers;
using Marketeer.API.GameInterop.Services;
using Marketeer.Core.CompetitionTracking.Contracts;
using Marketeer.Core.Framework;
using Marketeer.Core.MarketWatch.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.API.GameInterop;

public class GameInteropFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGameEventService, GameEventService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IRetainerProvider, RetainerProvider>();
        services.AddSingleton<IMarketListingProvider, MarketListingProvider>();
        services.AddSingleton<IWorldInteractionService, WorldInteractionService>();
        services.AddSingleton<ILocalMarketViewScanner, LocalMarketViewScanner>();
        services.AddSingleton<IMarketWatchPlayerContext, PlayerContextService>();
        services.AddSingleton<ICompetitionPlayerContext, PlayerContextService>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<IGameEventService>();
        var marketScanner = provider.GetRequiredService<ILocalMarketViewScanner>();
        marketScanner.Enable();
    }
}