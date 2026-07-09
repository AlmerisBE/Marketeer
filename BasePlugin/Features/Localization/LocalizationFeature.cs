using BasePlugin.Core;
using BasePlugin.Features.Localization.Contracts;
using BasePlugin.Features.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BasePlugin.Features.Localization;

public class LocalizationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationService, LocalizationService>();
    }
}