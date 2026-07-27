using Dalamud.Interface.Windowing;
using Marketeer.API.Features;
using Marketeer.API.Guidance.Contracts;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.Guidance.Services;
using Marketeer.UI.Guidance.Providers;
using Marketeer.UI.Guidance.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Guidance;

public class GuidanceFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, GuidanceLocalizationProvider>();
        services.AddSingleton<IWindowGeometryProvider, WindowGeometryProvider>();

        services.AddSingleton<MarketeerGuideWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MarketeerGuideWindow>());
    }
}