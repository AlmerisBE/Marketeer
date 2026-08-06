using Marketeer.API.CraftingProfit.Contracts;
using Marketeer.API.Dashboard.Contracts;
using Marketeer.API.Features;
using Marketeer.API.Localization.Contracts;
using Marketeer.Core.CraftingProfit.Repositories;
using Marketeer.Core.CraftingProfit.Services;
using Marketeer.UI.CraftingProfit.Providers;
using Marketeer.UI.CraftingProfit.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.Core.CraftingProfit;

public class CraftingProfitFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        // Localization
        services.AddSingleton<ILocalizationProvider, CraftingProfitLocalizationProvider>();

        // Core Domain
        services.AddSingleton<ICraftingProfitRepository, CraftingProfitRepository>();
        services.AddSingleton<ICraftingCostEvaluator, CraftingCostEvaluator>();

        // UI Node
        services.AddSingleton<CraftingProfitMenu>();
        services.AddSingleton<INavigationNode>(provider => provider.GetRequiredService<CraftingProfitMenu>());
    }
}