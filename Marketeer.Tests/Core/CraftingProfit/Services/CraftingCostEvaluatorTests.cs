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
    public async Task EvaluateAsync_ShouldCalculateDirectPurchaseCosts() {
        // Arrange
        var config = new CraftingItemConfig { ItemId = 10, TargetSellPrice = 5000 };

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

        // Mock Universalis
        this.priceProvider.GetLowestPriceAsync(10, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 4500 }));
        this.priceProvider.GetLowestPriceAsync(11, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 500 }));
        this.priceProvider.GetLowestPriceAsync(12, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 1000 }));

        // Act
        var result = await this.evaluator.EvaluateAsync(config, 1);

        // Assert
        Assert.Equal(10u, result.ItemId);
        Assert.Equal(4500u, result.CurrentMarketPrice);

        // Cost: (2 * 500) + (1 * 1000) = 2000
        Assert.Equal(2000u, result.TotalCraftingCost);
        Assert.Equal(2, result.ComponentEvaluations.Count);
        Assert.Equal(2500, result.Profit); // 4500 - 2000
    }

    [Fact]
    public async Task EvaluateAsync_ShouldCalculateRecursiveCraftingCosts() {
        // Arrange
        var config = new CraftingItemConfig {
            ItemId = 20,
            TargetSellPrice = 10000,
            Components = new Dictionary<uint, ComponentConfig> {
                { 21, new ComponentConfig { ItemId = 21, CraftRecursively = true } } // Request recursive craft for item 21
            }
        };

        // Main Recipe (Item 20 needs 1x Item 21)
        var mainRecipe = new RecipeInfo {
            RecipeId = 2,
            ResultItemId = 20,
            ResultQuantity = 1,
            Ingredients = new List<RecipeIngredient> { new RecipeIngredient { ItemId = 21, Quantity = 1 } }
        };

        // Sub Recipe (Item 21 needs 3x Item 22. Produces 1x Item 21)
        var subRecipe = new RecipeInfo {
            RecipeId = 3,
            ResultItemId = 21,
            ResultQuantity = 1,
            Ingredients = new List<RecipeIngredient> { new RecipeIngredient { ItemId = 22, Quantity = 3 } }
        };

        this.recipeDataService.GetPrimaryRecipe(20).Returns(mainRecipe);
        this.recipeDataService.GetPrimaryRecipe(21).Returns(subRecipe);

        // Prices: Final item = 10000, Sub-component (Item 22) = 200
        this.priceProvider.GetLowestPriceAsync(20, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 10000 }));
        this.priceProvider.GetLowestPriceAsync(22, 1, false).Returns(Task.FromResult<LowestPriceResult?>(new LowestPriceResult { Price = 200 }));

        // Act
        var result = await this.evaluator.EvaluateAsync(config, 1);

        // Assert
        Assert.Single(result.ComponentEvaluations);
        var subEval = result.ComponentEvaluations[0];

        Assert.True(subEval.IsCraftedRecursively);
        Assert.Equal(600u, subEval.UnitCost); // 3 * 200
        Assert.Equal(600u, subEval.TotalCost); // Needs 1x Item 21, which costs 600 to craft

        Assert.Equal(600u, result.TotalCraftingCost);
    }
}