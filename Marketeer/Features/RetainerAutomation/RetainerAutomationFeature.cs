using Marketeer.Core;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Localization.Contracts;
using Marketeer.Features.RetainerAutomation.Commands;
using Marketeer.Features.RetainerAutomation.Contracts;
using Marketeer.Features.RetainerAutomation.Providers;
using Marketeer.Features.RetainerAutomation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Features.RetainerAutomation;

public class RetainerAutomationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, RetainerAutomationLocalizationProvider>();

        services.AddSingleton<IClientRetainerService, ClientRetainerService>();
        services.AddSingleton<IRetainerUiInteractionService, RetainerUiInteractionService>();

        services.AddSingleton<RetainerAutomationService>();
        services.AddSingleton<IRetainerAutomationService>(provider => provider.GetRequiredService<RetainerAutomationService>());

        services.AddSingleton<PriceUpdateAutomationService>();
        services.AddSingleton<IPriceUpdateAutomationService>(provider => provider.GetRequiredService<PriceUpdateAutomationService>());

        // Imported commands from the deleted Retainers feature
        services.AddSingleton<ICommand, RetainerCommandAction>();
        services.AddSingleton<ICommand, RetainerMenuCommandAction>();
    }
}