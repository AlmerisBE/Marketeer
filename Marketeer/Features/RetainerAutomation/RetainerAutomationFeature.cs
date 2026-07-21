using Marketeer.Core;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerAutomation.Providers;
using Marketeer.Features.RetainerAutomation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerAutomation;

public class RetainerAutomationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, RetainerAutomationLocalizationProvider>();

        services.AddSingleton<RetainerAutomationService>();
        services.AddSingleton<IRetainerAutomationService>(provider => provider.GetRequiredService<RetainerAutomationService>());

        services.AddSingleton<IPriceUpdateAutomationService, PriceUpdateAutomationService>();
    }
}