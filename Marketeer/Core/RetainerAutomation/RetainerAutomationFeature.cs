using Marketeer.API.Command.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.API.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.RetainerAutomation.Commands;
using Marketeer.UI.RetainerAutomation.Providers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.RetainerAutomation;

public class RetainerAutomationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, RetainerAutomationLocalizationProvider>();

        services.AddSingleton<IClientRetainerService, ClientRetainerService>();
        services.AddSingleton<IRetainerUiInteractionService, RetainerUiInteractionService>();
        services.AddSingleton<IRetainerOrchestratorService, RetainerOrchestratorService>();

        services.AddSingleton<RetainerContextMenuService>();

        services.AddSingleton<RetainerAutomationService>();
        services.AddSingleton<IRetainerAutomationService>(provider => provider.GetRequiredService<RetainerAutomationService>());

        services.AddSingleton<CancelListingsAutomationService>();
        services.AddSingleton<ICancelListingsAutomationService>(provider => provider.GetRequiredService<CancelListingsAutomationService>());

        services.AddSingleton<PriceUpdateAutomationService>();
        services.AddSingleton<IPriceUpdateAutomationService>(provider => provider.GetRequiredService<PriceUpdateAutomationService>());

        services.AddSingleton<ItemCancelAndSellService>();
        services.AddSingleton<IItemCancelAndSellService>(provider => provider.GetRequiredService<ItemCancelAndSellService>());

        services.AddSingleton<CancelAndSellAutomationService>();
        services.AddSingleton<ICancelAndSellAutomationService>(provider => provider.GetRequiredService<CancelAndSellAutomationService>());

        services.AddSingleton<ICommand, RetainerCommand>();
        services.AddSingleton<ICommand, RetainerMenuCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<RetainerContextMenuService>();
    }
}