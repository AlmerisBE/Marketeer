using Dalamud.Interface.Windowing;
using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerOverlays.Components;
using Marketeer.UI.RetainerOverlays.Contracts;
using Marketeer.UI.RetainerOverlays.Providers;
using Marketeer.UI.RetainerOverlays.Services;
using Marketeer.UI.RetainerOverlays.UI;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.RetainerOverlays;

public class RetainerOverlaysFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, GuidanceLocalizationProvider>();

        // Services géométriques et moteur d'instructions
        services.AddSingleton<IWindowGeometryProvider, WindowGeometryProvider>();
        services.AddSingleton<GuidanceEngineService>();
        services.AddSingleton<IGuidanceInstructionProvider>(provider => provider.GetRequiredService<GuidanceEngineService>());

        // Fenêtre flottante (Anciennement Guidance)
        services.AddSingleton<MarketeerGuideWindow>();
        services.AddSingleton<Window>(provider => provider.GetRequiredService<MarketeerGuideWindow>());

        // Surlignages et interactions natives
        services.AddSingleton<NativeListingHighlighterService>();
        services.AddSingleton<NativeRetainerListHighlighterService>();
        services.AddSingleton<NativeListingClickInterceptorService>();

        // Menus contextuels
        services.AddSingleton<RetainerContextMenuService>();

        // Injection de l'action dans la Sidebar du Shell
        services.AddSingleton<ISidebarAction, RetainerScanSidebarAction>();
    }

    public void Initialize(IServiceProvider provider) {
        // Initialisation des hooks UI natifs
        provider.GetRequiredService<NativeListingHighlighterService>();
        provider.GetRequiredService<NativeRetainerListHighlighterService>();
        provider.GetRequiredService<NativeListingClickInterceptorService>();
        provider.GetRequiredService<RetainerContextMenuService>();
    }
}