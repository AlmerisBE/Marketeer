using Marketeer.Core;
using Marketeer.Features.RetainerTracking.Contracts;
using Marketeer.Features.RetainerTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerTracking;

public class RetainerTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // This service must be instantiated at startup to hook the CharacterForgotten event properly
        services.AddSingleton<IRetainerTrackerService, RetainerTrackerService>();
    }
}