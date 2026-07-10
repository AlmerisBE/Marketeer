using Marketeer.Core;
using Marketeer.Features.GameEvents.Contracts;
using Marketeer.Features.GameEvents.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.GameEvents;

public class GameEventsFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IGameEventService, GameEventService>();
    }
}