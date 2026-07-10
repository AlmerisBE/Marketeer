using Marketeer.Core;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Services;
using Marketeer.Features.CharacterTracking.UI;
using Marketeer.Features.Dashboard.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.CharacterTracking;

public class CharacterTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ICharacterTrackerService, CharacterTrackerService>();

        services.AddSingleton<IDashboardWidget, CharacterListWidget>();
    }
}