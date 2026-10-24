using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Providers;
using Marketeer.UI.Themes.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.Themes;

public class ThemesFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IThemeRepository, ThemeRepository>();
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddSingleton<ILocalizationProvider, ThemesLocalizationProvider>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<IThemeService>();
    }
}