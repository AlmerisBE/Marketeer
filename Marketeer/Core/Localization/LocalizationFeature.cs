using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.Localization;

public class LocalizationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationService, LocalizationService>();
    }
}