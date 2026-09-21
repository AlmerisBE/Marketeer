using Marketeer.Core.Framework;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.Shell.Contracts;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Providers;
using Marketeer.UI.Themes.Services;
using Marketeer.UI.Themes.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.Themes;

public class ThemesFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IThemeRepository, ThemeRepository>();
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddSingleton<ILocalizationProvider, ThemesLocalizationProvider>();

        services.AddSingleton<ThemeConfigMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<ThemeConfigMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<IThemeService>();
    }
}