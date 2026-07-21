using Marketeer.Core;
using Marketeer.Features.Dashboard.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.UndercutTracking.Providers;
using Marketeer.Features.UndercutTracking.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.UndercutTracking;

public class UndercutTrackingFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, UndercutLocalizationProvider>();

        services.AddSingleton<UndercutsTab>();
        services.AddSingleton<IDashboardTab>(provider => provider.GetRequiredService<UndercutsTab>());
    }
}