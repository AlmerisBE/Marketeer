using Marketeer.Core.Framework;
using Marketeer.Core.RetainerAutomation.Contracts;
using Marketeer.Core.RetainerAutomation.Services;
using Marketeer.UI.Localization.Contracts;
using Marketeer.UI.RetainerAutomation.Commands;
using Marketeer.UI.RetainerAutomation.Providers;
using Marketeer.UI.RetainerOverlays.Services;
using Marketeer.UI.Shell.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.RetainerAutomation;

public class RetainerAutomationFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<ILocalizationProvider, RetainerAutomationLocalizationProvider>();

        services.AddSingleton<IClientRetainerService, ClientRetainerService>();
        services.AddSingleton<IRetainerUiInteractionService, RetainerUiInteractionService>();
        services.AddSingleton<IRetainerOrchestratorService, RetainerOrchestratorService>();

        // Injection de notre nouveau service
        services.AddSingleton<IRetainerSwitcherService, RetainerSwitcherService>();

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

        services.AddSingleton<HybridAutomationService>();
        services.AddSingleton<IHybridAutomationService>(provider => provider.GetRequiredService<HybridAutomationService>());

        services.AddSingleton<ICommand, RetainerCommand>();
        services.AddSingleton<ICommand, RetainerMenuCommand>();
    }

    public void Initialize(IServiceProvider provider) {
        provider.GetRequiredService<RetainerContextMenuService>();
        provider.GetRequiredService<IRetainerSwitcherService>();
    }
}