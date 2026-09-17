using Dalamud.Interface.Windowing;
using Marketeer.Core.Framework;
using Marketeer.UI.Command.Contracts;
using Marketeer.UI.Guidance.Commands;
using Marketeer.UI.Guidance.Providers;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Contracts;
using Marketeer.UI.RetainerOverlays.Services;
using Marketeer.UI.RetainerOverlays.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.UI.Guidance;

public class GuidanceFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, GuidanceLocalizationProvider>();
        services.AddSingleton<IWindowGeometryProvider, WindowGeometryProvider>();

        // Guidance Engine
        services.AddSingleton<GuidanceEngineService>();
        services.AddSingleton<IGuidanceInstructionProvider>(provider => provider.GetRequiredService<GuidanceEngineService>());

        services.AddSingleton<MarketeerGuideWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MarketeerGuideWindow>());

        // Commands
        services.AddSingleton<ICommand, GuidanceCommand>();
    }
}