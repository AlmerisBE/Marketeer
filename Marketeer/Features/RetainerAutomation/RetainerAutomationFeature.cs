using Marketeer.Core;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerAutomation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerAutomation;

public class RetainerAutomationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IClientRetainerService, ClientRetainerService>();
        services.AddSingleton<IUiInteractionService, UiInteractionService>();
        services.AddSingleton<RetainerAutomationService>();
        services.AddSingleton<IRetainerAutomationService>(provider => provider.GetRequiredService<RetainerAutomationService>());
    }
}