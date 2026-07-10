using Marketeer.Core;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.CharacterTracking;

public class CharacterTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Register as singleton and ensure it's instantiated to subscribe to events
        services.AddSingleton<ICharacterTrackerService, CharacterTrackerService>();

        // We can force instantiation during DI build by pulling it once in Plugin.cs,
        // or by registering it as a hosted service (not natively supported by our simple DI).
        // For now, we will just register it here.
    }
}