using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.CraftingProfit.Providers;
using Marketeer.Core.CraftingProfit.Repositories;
using Marketeer.Core.CraftingProfit.Services;
using Marketeer.Core.CraftingProfit.UI;
using Marketeer.Core.Framework;
using Marketeer.UI.Dashboard.Contracts;
using Marketeer.UI.Localization.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Marketeer.Core.CraftingProfit;

public class CraftingProfitFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Localization
        services.AddSingleton<ILocalizationProvider, CraftingProfitLocalizationProvider>();

        // Core Domain
        services.AddSingleton<ICraftingProfitRepository, CraftingProfitRepository>();
        services.AddSingleton<ICraftingCostEvaluator, CraftingCostEvaluator>();
        services.AddSingleton<ICraftingProfitStateService, CraftingProfitStateService>();

        // UI Node
        services.AddSingleton<CraftingProfitMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CraftingProfitMenu>());
    }

    public void Initialize(IServiceProvider provider) {
        // Instantiate the service to trigger the event subscriptions
        provider.GetRequiredService<ICraftingProfitStateService>();
    }
}