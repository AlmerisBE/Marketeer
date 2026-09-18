using Marketeer.Core.Framework;
using Marketeer.UI.Themes.Contracts;
using Marketeer.UI.Themes.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.UI.Themes;

public class ThemesFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IThemeRepository, ThemeRepository>();
        services.AddSingleton<IThemeService, ThemeService>();
    }

    public void Initialize(IServiceProvider provider) {
        // Initialize the service so it loads the configured theme at startup
        provider.GetRequiredService<IThemeService>();
    }
}