using Marketeer.Core;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.Localization;

public class LocalizationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationService, LocalizationService>();
    }
}