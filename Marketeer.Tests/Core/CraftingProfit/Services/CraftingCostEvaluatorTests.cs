using Marketeer.API.CraftingProfit.Models;
using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.API.Logging.Contracts;
using Marketeer.API.Universalis.Contracts;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CraftingProfit.Services;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CraftingProfit.Services;

public class CraftingCostEvaluatorTests {
    private IServerPriceProvider priceProvider;
    private IRecipeDataService recipeDataService;
    private ILoggerService logger;
    private CraftingCostEvaluator evaluator;

    public CraftingCostEvaluatorTests() {
        this.priceProvider = Substitute.For<IServerPriceProvider>();
        this.recipeDataService = Substitute.For<IRecipeDataService>();
        this.logger = Substitute.For<ILoggerService>();

        this.evaluator = new CraftingCostEvaluator(this.priceProvider, this.recipeDataService, this.logger);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldCalculateDirectPurchaseAndYieldCosts() {
        // Arrange (Moqueca recipe yield = 3)
        var config = new CraftingItemConfig {
            ItemId = 10,
            TargetSellPrice = 1200,
            Components = new Dictionary<uint, ComponentConfig> {
                { 11, new ComponentConfig { ItemId = 11, TargetBuyPrice = 400 } }
            }
        };

        var recipe = new RecipeInfo {
            RecipeId = 1,
            ResultItemId = 10,
            ResultQuantity = 3,
            Ingredients = new List<RecipeIngredient> {
                new RecipeIngredient { ItemId = 11, Quantity = 2 },
                new RecipeIngredient { ItemId = 12, Quantity = 1 }
            }
        };

        this.recipeDataService.GetPrimaryRecipe(10).Returns(recipe);

        this.priceProvider.GetLowestPriceAsync(10, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 1001 }));
        this.priceProvider.GetLowestPriceAsync(11, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 382 }));
        this.priceProvider.GetLowestPriceAsync(12, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 441 }));

        // Act
        var result = await this.evaluator.EvaluateAsync(config, 1);

        // Assert
        Assert.Equal(10u, result.ItemId);
        Assert.Equal(3u, result.ResultQuantity);
        Assert.Equal(1001u, result.CurrentMarketPrice);

        // Batch cost: (2 * 382) + (1 * 441) = 764 + 441 = 1205
        Assert.Equal(1205u, result.BatchCraftingCost);
        // Unit cost: Ceil(1205 / 3) = 402
        Assert.Equal(402u, result.TotalCraftingCost);

        // Target Batch cost: (2 * 400) + (1 * 441 [fallback]) = 800 + 441 = 1241
        Assert.Equal(1241u, result.BatchTargetCraftingCost);
        // Target Unit cost: Ceil(1241 / 3) = 414
        Assert.Equal(414u, result.TotalTargetCraftingCost);

        Assert.Equal(599, result.Profit); // 1001 - 402
    }
}