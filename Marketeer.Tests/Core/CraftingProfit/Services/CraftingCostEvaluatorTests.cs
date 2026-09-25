using Marketeer.API.GameData.Contracts;
using Marketeer.API.GameData.Models;
using Marketeer.API.Universalis.Models;
using Marketeer.Core.CraftingProfit.Contracts;
using Marketeer.Core.CraftingProfit.Models;
using Marketeer.Core.CraftingProfit.Services;
using Marketeer.Core.Logging.Contracts;
using Marketeer.Core.MarketPricing.Contracts;
using Marketeer.Core.MarketPricing.Models;
using NSubstitute;
using Xunit;

namespace Marketeer.Tests.Core.CraftingProfit.Services;

public class CraftingCostEvaluatorTests {
    [Fact]
    public async Task EvaluateAsync_CalculatesCostAndMaxCraftsCorrectly() {
        var priceCache = Substitute.For<IMarketPriceCacheService>();
        var recipeData = Substitute.For<IRecipeDataService>();
        var invService = Substitute.For<ICraftingInventoryService>();
        var logger = Substitute.For<ILoggerService>();

        // Final item needs 1 Component A and 1 Component B
        var config = new CraftingItemConfig {
            ItemId = 100,
            TargetSellPrice = 5000,
            Components = new Dictionary<uint, ComponentConfig> {
                { 10, new ComponentConfig { ItemId = 10, CraftRecursively = true } } // Component A is crafted recursively
            }
        };

        // Root recipe
        recipeData.GetPrimaryRecipe(100u).Returns(new RecipeInfo {
            ResultQuantity = 1,
            Ingredients = new List<RecipeIngredient> {
                new RecipeIngredient { ItemId = 10, Quantity = 1 }, // Component A
                new RecipeIngredient { ItemId = 20, Quantity = 1 }  // Component B
            }
        });

        // Sub-recipe for Component A (needs 2x Raw Material C)
        recipeData.GetPrimaryRecipe(10u).Returns(new RecipeInfo {
            ResultQuantity = 1,
            Ingredients = new List<RecipeIngredient> {
                new RecipeIngredient { ItemId = 30, Quantity = 2 } // Raw Material C
            }
        });

        // Simulated Inventory State
        invService.GetTotalOwnedQuantity(100u).Returns(0u); // Final product
        invService.GetTotalOwnedQuantity(10u).Returns(1u);  // Component A in stock: 1
        invService.GetTotalOwnedQuantity(20u).Returns(5u);  // Component B in stock: 5
        invService.GetTotalOwnedQuantity(30u).Returns(4u);  // Raw Material C in stock: 4

        var pricing = new MarketItemPricing {
            ItemId = 100,
            Listings = new List<LowestPriceResult> { new LowestPriceResult { Price = 4500 } }
        };

        priceCache.GetPricingAsync(100u, 73u, false).Returns(pricing);

        var service = new CraftingCostEvaluator(priceCache, recipeData, invService, logger);
        var result = await service.EvaluateAsync(config, 73u);

        Assert.Equal(100u, result.ItemId);
        Assert.Equal(4500u, result.CurrentMarketPrice);

        // Let's verify the simulation!
        // We need 1xA and 1xB per final product.
        // We have 1xA and 5xB in stock. We can immediately craft 1 Final Product.
        // To craft a 2nd Final Product, we need 1xA and 1xB. We have 4xB left, but 0xA left.
        // Can we craft A? We need 2xC per A. We have 4xC. So we can craft 2xA.
        // We craft 1xA. Now we have 1xA, 4xB, and 2xC. We can craft a 2nd Final Product.
        // To craft a 3rd Final Product, we need 1xA and 1xB. We have 3xB left, but 0xA left.
        // Can we craft A? We need 2xC. We have 2xC. So we craft 1xA.
        // Now we have 1xA, 3xB, and 0xC. We can craft a 3rd Final Product.
        // To craft a 4th Final Product, we need 1xA and 1xB. We have 2xB, 0xA, and 0xC.
        // We can't craft A anymore. Simulation stops. Total crafts possible = 3.

        Assert.Equal(3u, result.MaxCraftable);
    }
}