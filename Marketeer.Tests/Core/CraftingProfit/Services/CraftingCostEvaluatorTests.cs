using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.CraftingProfit.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CraftingProfit.Services;

public class CraftingCostEvaluatorTests {
    private IMarketPriceCacheService priceProvider;
    private IRecipeDataService recipeDataService;
    private ILoggerService logger;
    private CraftingCostEvaluator evaluator;

    public CraftingCostEvaluatorTests() {
        this.priceProvider = Substitute.For<IMarketPriceCacheService>();
        this.recipeDataService = Substitute.For<IRecipeDataService>();
        this.logger = Substitute.For<ILoggerService>();

        this.evaluator = new CraftingCostEvaluator(this.priceProvider, this.recipeDataService, this.logger);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIgnoreCost_WhenIgnoreCostFlagIsTrue() {
        // Arrange (Item 10 needs 2x Item 11 and 1x Item 12. Item 11 is self-sourced / free)
        var config = new CraftingItemConfig {
            ItemId = 10,
            TargetSellPrice = 1200,
            Components = new Dictionary<uint, ComponentConfig> {
                { 11, new ComponentConfig { ItemId = 11, IgnoreCost = true } }
            }
        };

        var recipe = new RecipeInfo {
            RecipeId = 1,
            ResultItemId = 10,
            ResultQuantity = 1,
            Ingredients = new List<RecipeIngredient> {
                new RecipeIngredient { ItemId = 11, Quantity = 2 },
                new RecipeIngredient { ItemId = 12, Quantity = 1 }
            }
        };

        this.recipeDataService.GetPrimaryRecipe(10).Returns(recipe);

        this.priceProvider.GetLowestPriceAsync(10, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 1000 }));
        this.priceProvider.GetLowestPriceAsync(11, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 500 }));
        this.priceProvider.GetLowestPriceAsync(12, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 300 }));

        // Act
        var result = await this.evaluator.EvaluateAsync(config, 1);

        // Assert
        Assert.Equal(10u, result.ItemId);
        Assert.Equal(1000u, result.CurrentMarketPrice);

        // Batch cost: (2 * 0 [ignored]) + (1 * 300) = 300
        Assert.Equal(300u, result.BatchCraftingCost);
        Assert.Equal(300u, result.TotalCraftingCost);

        Assert.True(result.ComponentEvaluations[0].IsCostIgnored);
        Assert.Equal(0u, result.ComponentEvaluations[0].TotalCost);

        Assert.Equal(700, result.Profit); // 1000 - 300
    }
}