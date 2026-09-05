using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Services;
using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.Framework;
using Marketeer.Core.MarketWatch.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Marketeer.API.GameData;

public class GameDataFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IWorldDataPresenter, WorldDataPresenter>();
        services.AddSingleton<IMarketItemSearchProvider, MarketItemSearchProvider>();
        services.AddSingleton<IRecipeDataService, RecipeDataService>();
        services.AddSingleton<IItemActionProvider, ItemActionProvider>();
    }
}