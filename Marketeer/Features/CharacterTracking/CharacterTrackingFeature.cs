using Marketeer.Core;
using Marketeer.Features.CharacterTracking.Contracts;
using Marketeer.Features.CharacterTracking.Providers;
using Marketeer.Features.CharacterTracking.Services;
using Marketeer.Features.CharacterTracking.UI;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.CharacterTracking;

public class CharacterTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, CharacterTrackingLocalizationProvider>();
        services.AddSingleton<ICharacterTrackerService, CharacterTrackerService>();
        services.AddSingleton<IDashboardWidget, CharacterListWidget>();
    }
}